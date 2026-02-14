using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Systems
{
    public class InputControllerSystem : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform cityRoot;
        [SerializeField] private Material whiteBorderMaterial;
        [SerializeField] private float outlineScale = 1.01f;

        private readonly Dictionary<Renderer, GameObject> _outlineByRenderer = new();
        private readonly Dictionary<Renderer, Renderer> _selectionTargetByHitRenderer = new();
        private Renderer _selectedRenderer;

        private static readonly Regex TrailingIndexRegex = new(@"[_\s]*\d+$", RegexOptions.Compiled);

        protected virtual void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (cityRoot == null)
            {
                cityRoot = transform;
            }

            EnsureCollidersAndOutlines();
        }

        protected virtual void Update()
        {
            if (!Input.GetMouseButtonDown(0) || worldCamera == null)
            {
                return;
            }

            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit))
            {
                return;
            }

            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            if (_selectionTargetByHitRenderer.TryGetValue(renderer, out Renderer selectionTarget))
            {
                SelectCity(selectionTarget);
            }
        }

        private void SelectCity(Renderer renderer)
        {
            if (_selectedRenderer != null && _outlineByRenderer.TryGetValue(_selectedRenderer, out GameObject previousOutline))
            {
                previousOutline.SetActive(false);
            }

            _selectedRenderer = renderer;

            if (_outlineByRenderer.TryGetValue(renderer, out GameObject currentOutline))
            {
                currentOutline.SetActive(true);
            }
        }

        private void EnsureCollidersAndOutlines()
        {
            Renderer[] renderers = cityRoot.GetComponentsInChildren<Renderer>(true);
            Dictionary<string, List<Renderer>> rendererGroups = new();

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshFilter meshFilter = renderers[i].GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                MeshCollider collider = renderers[i].GetComponent<MeshCollider>();
                if (collider == null)
                {
                    collider = renderers[i].gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = meshFilter.sharedMesh;
                }

                string cityKey = BuildCityKey(renderers[i].name);
                if (!rendererGroups.TryGetValue(cityKey, out List<Renderer> cityRenderers))
                {
                    cityRenderers = new List<Renderer>();
                    rendererGroups[cityKey] = cityRenderers;
                }

                cityRenderers.Add(renderers[i]);
            }

            foreach (List<Renderer> cityRenderers in rendererGroups.Values)
            {
                Renderer borderRenderer = FindBorderRenderer(cityRenderers);
                if (borderRenderer == null)
                {
                    continue;
                }

                MeshFilter borderMeshFilter = borderRenderer.GetComponent<MeshFilter>();
                if (borderMeshFilter == null || borderMeshFilter.sharedMesh == null)
                {
                    continue;
                }

                GameObject outline = BuildOutlineObject(borderRenderer, borderMeshFilter.sharedMesh);
                _outlineByRenderer[borderRenderer] = outline;

                for (int i = 0; i < cityRenderers.Count; i++)
                {
                    _selectionTargetByHitRenderer[cityRenderers[i]] = borderRenderer;
                }
            }
        }

        private static Renderer FindBorderRenderer(List<Renderer> cityRenderers)
        {
            for (int i = 0; i < cityRenderers.Count; i++)
            {
                string lowerName = cityRenderers[i].name.ToLowerInvariant();
                if (lowerName.Contains("border") || lowerName.Contains("layer"))
                {
                    return cityRenderers[i];
                }
            }

            return cityRenderers.Count > 0 ? cityRenderers[0] : null;
        }

        private static string BuildCityKey(string name)
        {
            string normalized = name.ToLowerInvariant().Replace('"', ' ').Trim();
            normalized = normalized.Replace("layer", string.Empty).Replace("border", string.Empty);
            normalized = TrailingIndexRegex.Replace(normalized, string.Empty);
            return normalized.Trim(' ', '_', '-');
        }

        private GameObject BuildOutlineObject(Renderer targetRenderer, Mesh mesh)
        {
            GameObject outline = new GameObject($"{targetRenderer.gameObject.name}_SelectedBorder");
            outline.transform.SetParent(targetRenderer.transform, false);
            outline.transform.localScale = Vector3.one * outlineScale;

            MeshFilter outlineFilter = outline.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = mesh;

            MeshRenderer outlineRenderer = outline.AddComponent<MeshRenderer>();
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.material = whiteBorderMaterial != null
                ? whiteBorderMaterial
                : BuildFallbackWhiteMaterial();

            outline.SetActive(false);
            return outline;
        }

        private static Material BuildFallbackWhiteMaterial()
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = Color.white;
            return material;
        }
    }
}
