using UnityEngine;

namespace Assets.Scripts.Camera
{
    public class RTSCameraController : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] private float panSpeed = 12f;
        [SerializeField] private Vector2 boundsX = new Vector2(-50f, 50f);
        [SerializeField] private Vector2 boundsY = new Vector2(-50f, 50f);

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 40f;

        private Vector2 keyboardInput;
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

            zoomInput = Input.GetAxis("Mouse ScrollWheel");
        }

        private void LateUpdate()
        {
            Vector3 position = transform.position;

            Vector2 input = keyboardInput * panSpeed;

            Vector3 move = new Vector3(input.x, input.y, 0f) * Time.deltaTime;
            position += move;

            position.x = Mathf.Clamp(position.x, boundsX.x, boundsX.y);
            position.y = Mathf.Clamp(position.y, boundsY.x, boundsY.y);

            transform.position = position;

            if (Mathf.Abs(zoomInput) > 0.01f)
            {
                ApplyZoom(zoomInput * zoomSpeed);
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
            Vector3 cameraPosition = cameraTransform.position;
            cameraPosition.z = Mathf.Clamp(cameraPosition.z + (-delta), -maxZoom, -minZoom);
            cameraTransform.position = cameraPosition;
        }
    }
}
