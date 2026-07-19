using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Classic-RTS orthographic camera: fixed 40° pitch, yaw in 90° steps
    /// (Q/E), WASD/arrow + middle-mouse panning, scroll zoom, clamped to the
    /// map. Attach to the Camera object.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public const float Pitch = 40f;
        public const float PanSpeed = 14f;
        public const float ZoomMin = 4f;
        public const float ZoomMax = 28f;
        public const float CameraDistance = 60f;

        private Camera _camera;
        private Vector3 _pivot;           // point on the ground the camera looks at
        private float _yaw = 45f;
        private float _targetYaw = 45f;
        private float _zoom = 12f;
        private Vector2 _mapSize = new Vector2(64, 64);
        private Vector3 _lastMousePos;

        private void Start()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = 200f;

            var runner = FindFirstObjectByType<GameRunner>();
            if (runner != null)
            {
                runner.WhenReady(() =>
                    _mapSize = new Vector2(runner.Game.World.Map.Width, runner.Game.World.Map.Height));
            }
            _pivot = new Vector3(_mapSize.x * 0.25f, 0f, _mapSize.y * 0.35f);
            Apply();
        }

        private void Update()
        {
            // Pan: WASD / arrows, relative to current yaw.
            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 0.01f)
            {
                var forward = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
                var right = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
                float speedScale = _zoom / 12f;
                _pivot += (right * input.x + forward * input.y) * (PanSpeed * speedScale * Time.deltaTime);
            }

            // Pan: middle mouse drag.
            if (Input.GetMouseButtonDown(2)) _lastMousePos = Input.mousePosition;
            if (Input.GetMouseButton(2))
            {
                var delta = Input.mousePosition - _lastMousePos;
                _lastMousePos = Input.mousePosition;
                var forward = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
                var right = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
                float unitsPerPixel = _zoom * 2f / Screen.height;
                _pivot -= (right * delta.x + forward * delta.y) * unitsPerPixel;
            }

            // Zoom.
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _zoom = Mathf.Clamp(_zoom * (1f - scroll * 0.9f), ZoomMin, ZoomMax);
            }

            // Yaw in 90° steps.
            if (Input.GetKeyDown(KeyCode.Q)) _targetYaw -= 90f;
            if (Input.GetKeyDown(KeyCode.E)) _targetYaw += 90f;
            _yaw = Mathf.LerpAngle(_yaw, _targetYaw, Time.deltaTime * 8f);

            _pivot.x = Mathf.Clamp(_pivot.x, 0f, _mapSize.x);
            _pivot.z = Mathf.Clamp(_pivot.z, 0f, _mapSize.y);

            Apply();
        }

        public void FocusOn(Vector3 worldPoint)
        {
            _pivot = new Vector3(worldPoint.x, 0f, worldPoint.z);
        }

        private void Apply()
        {
            _camera.orthographicSize = _zoom;
            var rotation = Quaternion.Euler(Pitch, _yaw, 0f);
            transform.SetPositionAndRotation(_pivot - rotation * Vector3.forward * CameraDistance, rotation);
        }
    }
}
