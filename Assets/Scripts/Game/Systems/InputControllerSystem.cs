using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems
{
    public class InputControllerSystem : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform cityRoot;
        [SerializeField] private Material whiteBorderMaterial;
        [SerializeField] private float outlineScale = 1.01f;
        [SerializeField] private SelectionDetailsCanvas selectionDetailsCanvas;

        private readonly Dictionary<Renderer, GameObject> _outlineByRenderer = new();
        private readonly Dictionary<Renderer, List<Renderer>> _selectionGroupByHitRenderer = new();
        private readonly Dictionary<Renderer, ISelectable> _selectableByHitRenderer = new();
        private readonly List<GameObject> _activeOutlines = new();

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

            if (_selectionGroupByHitRenderer.TryGetValue(renderer, out List<Renderer> selectionGroup))
            {
                SelectCity(selectionGroup);

                if (_selectableByHitRenderer.TryGetValue(renderer, out ISelectable selectable))
                {
                    selectionDetailsCanvas?.ShowSelection(selectable);
                }
            }
        }

        private void SelectCity(List<Renderer> renderers)
        {
            for (int i = 0; i < _activeOutlines.Count; i++)
            {
                _activeOutlines[i].SetActive(false);
            }

            _activeOutlines.Clear();

            for (int i = 0; i < renderers.Count; i++)
            {
                if (_outlineByRenderer.TryGetValue(renderers[i], out GameObject currentOutline))
                {
                    currentOutline.SetActive(true);
                    _activeOutlines.Add(currentOutline);
                }
            }
        }

        private void EnsureCollidersAndOutlines()
        {
            Renderer[] renderers = cityRoot.GetComponentsInChildren<Renderer>(true);
            Dictionary<Transform, List<Renderer>> rendererGroups = new();

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

                Transform groupRoot = GetGroupRoot(renderers[i].transform);
                if (!rendererGroups.TryGetValue(groupRoot, out List<Renderer> cityRenderers))
                {
                    cityRenderers = new List<Renderer>();
                    rendererGroups[groupRoot] = cityRenderers;
                }

                cityRenderers.Add(renderers[i]);
            }

            foreach (List<Renderer> cityRenderers in rendererGroups.Values)
            {
                Transform groupRoot = GetGroupRoot(cityRenderers[0].transform);
                ISelectable selectable = EnsureSelectable(groupRoot.gameObject);

                for (int i = 0; i < cityRenderers.Count; i++)
                {
                    MeshFilter meshFilter = cityRenderers[i].GetComponent<MeshFilter>();
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                    {
                        continue;
                    }

                    GameObject outline = BuildOutlineObject(cityRenderers[i], meshFilter.sharedMesh);
                    _outlineByRenderer[cityRenderers[i]] = outline;
                }

                for (int i = 0; i < cityRenderers.Count; i++)
                {
                    _selectionGroupByHitRenderer[cityRenderers[i]] = cityRenderers;
                    if (selectable != null)
                    {
                        _selectableByHitRenderer[cityRenderers[i]] = selectable;
                    }
                }
            }
        }

        private static ISelectable EnsureSelectable(GameObject target)
        {
            ISelectable selectable = target.GetComponent(typeof(ISelectable)) as ISelectable;
            if (selectable != null)
            {
                return selectable;
            }

            City city = target.AddComponent<City>();
            return city;
        }

        private Transform GetGroupRoot(Transform current)
        {
            Transform last = current;
            while (current != null && current != cityRoot)
            {
                last = current;
                current = current.parent;
            }

            return last;
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
