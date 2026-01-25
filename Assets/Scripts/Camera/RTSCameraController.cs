using UnityEngine;

namespace Assets.Scripts.Camera
{
    public class RTSCameraController : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] private float panSpeed = 12f;
        [SerializeField] private float edgePanSpeed = 18f;
        [SerializeField] private float edgeBorderSize = 12f;
        [SerializeField] private Vector2 boundsX = new Vector2(-50f, 50f);
        [SerializeField] private Vector2 boundsZ = new Vector2(-50f, 50f);

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 40f;

        private Vector2 keyboardInput;
        private Vector2 edgeInput;
        private float zoomInput;
        private UnityEngine.Camera cachedCamera;

        private void Awake()
        {
            cachedCamera = GetComponentInChildren<UnityEngine.Camera>();
            if (cachedCamera == null)
            {
                cachedCamera = UnityEngine.Camera.main;
            }
        }

        private void Update()
        {
            keyboardInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            keyboardInput = Vector2.ClampMagnitude(keyboardInput, 1f);

            edgeInput = Vector2.zero;
            Vector3 mousePosition = Input.mousePosition;

            if (mousePosition.x <= edgeBorderSize)
            {
                edgeInput.x -= 1f;
            }
            else if (mousePosition.x >= Screen.width - edgeBorderSize)
            {
                edgeInput.x += 1f;
            }

            if (mousePosition.y <= edgeBorderSize)
            {
                edgeInput.y -= 1f;
            }
            else if (mousePosition.y >= Screen.height - edgeBorderSize)
            {
                edgeInput.y += 1f;
            }

            edgeInput = Vector2.ClampMagnitude(edgeInput, 1f);
            zoomInput = Input.mouseScrollDelta.y;
        }

        private void LateUpdate()
        {
            Vector3 position = transform.position;

            Vector3 keyboardMove = new Vector3(keyboardInput.x, 0f, keyboardInput.y) * panSpeed * Time.deltaTime;
            Vector3 edgeMove = new Vector3(edgeInput.x, 0f, edgeInput.y) * edgePanSpeed * Time.deltaTime;
            position += keyboardMove + edgeMove;

            position.x = Mathf.Clamp(position.x, boundsX.x, boundsX.y);
            position.z = Mathf.Clamp(position.z, boundsZ.x, boundsZ.y);
            transform.position = position;

            if (Mathf.Abs(zoomInput) > 0.01f)
            {
                ApplyZoom(zoomInput * zoomSpeed * Time.deltaTime);
            }
        }

        private void ApplyZoom(float delta)
        {
            if (cachedCamera == null)
            {
                return;
            }

            if (cachedCamera.orthographic)
            {
                cachedCamera.orthographicSize = Mathf.Clamp(
                    cachedCamera.orthographicSize - delta,
                    minZoom,
                    maxZoom
                );
                return;
            }

            Transform cameraTransform = cachedCamera.transform;
            cameraTransform.position += cameraTransform.forward * delta;
        }
    }
}
