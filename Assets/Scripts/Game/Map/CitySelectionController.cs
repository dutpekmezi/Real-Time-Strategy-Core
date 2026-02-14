using System.Collections.Generic;
using UnityEngine;

namespace Game.Map
{
    public class CitySelectionController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform cityRoot;
        [SerializeField] private Material whiteBorderMaterial;
        [SerializeField] private float outlineScale = 1.01f;

        private readonly Dictionary<Renderer, GameObject> _outlineByRenderer = new();
        private Renderer _selectedRenderer;

        private void Awake()
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

        private void Update()
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

            SelectCity(renderer);
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

                GameObject outline = BuildOutlineObject(renderers[i], meshFilter.sharedMesh);
                _outlineByRenderer[renderers[i]] = outline;
            }
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
