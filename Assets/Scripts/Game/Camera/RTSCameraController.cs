using UnityEngine;

namespace Assets.Scripts.Camera
{
    public class RTSCameraController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera camera;

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

        private void Awake()
        {
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


            transform.position = position;

            if (Mathf.Abs(zoomInput) > 0.01f)
            {
                ApplyZoom(zoomInput * zoomSpeed);
            }
        }

        private void ApplyZoom(float delta)
        {
            if (camera == null)
            {
                return;
            }

            if (camera.orthographic)
            {
                camera.orthographicSize = Mathf.Clamp(
                    camera.orthographicSize - delta,
                    minZoom,
                    maxZoom
                );
                return;
            }

            Transform cameraTransform = camera.transform;
            Vector3 cameraPosition = cameraTransform.position;
            cameraPosition.z = Mathf.Clamp(cameraPosition.z + (-delta), -maxZoom, -minZoom);
            cameraTransform.position = cameraPosition;
        }
    }
}
