using RobotFacility.Core;
using RobotFacility.Interaction;
using UnityEngine;

namespace RobotFacility.CameraSystem
{
    public enum CameraMode { Free, Follow }

    /// <summary>
    /// Free-fly camera plus a smooth follow mode for the selected robot.
    /// Controls: WASD/QE move, hold RMB to look, Shift = fast, scroll = dolly, F = toggle follow.
    /// Any movement key while following drops back to Free mode, so another robot can be selected.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] private SelectionController selection;

        [Header("Free mode")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float fastMultiplier = 3f;
        [SerializeField] private float lookSensitivity = 2.5f;
        [SerializeField] private float scrollSpeed = 8f;

        [Header("Follow mode")]
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 12f, -14f);
        [SerializeField, Min(0.01f)] private float followSmoothTime = 0.3f;
        [SerializeField, Min(0.1f)] private float lookDamping = 6f;
        [SerializeField] private bool followOnSelect;

        private Vector3 followVelocity;
        private float yaw, pitch;

        public CameraMode Mode { get; private set; } = CameraMode.Free;

        private void Start() => SyncAnglesFromTransform();

        private void OnEnable()
        {
            if (selection != null) selection.SelectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            if (selection != null) selection.SelectionChanged -= OnSelectionChanged;
        }

        /// <summary>Also callable from a UI button.</summary>
        public void ToggleFollow()
        {
            if (Mode == CameraMode.Follow) SetMode(CameraMode.Free);
            else if (selection != null && selection.Selected != null) SetMode(CameraMode.Follow);
        }

        public void SetMode(CameraMode mode)
        {
            if (Mode == mode) return;
            Mode = mode;
            followVelocity = Vector3.zero;
            if (mode == CameraMode.Free) SyncAnglesFromTransform();
        }

        private void OnSelectionChanged(RobotController robot)
        {
            if (robot == null) SetMode(CameraMode.Free);
            else if (followOnSelect) SetMode(CameraMode.Follow);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F)) ToggleFollow();

            Vector3 move = new Vector3(
                Input.GetAxisRaw("Horizontal"),
                (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f),
                Input.GetAxisRaw("Vertical"));

            if (Mode == CameraMode.Follow)
            {
                if (move != Vector3.zero) SetMode(CameraMode.Free); // user took control
                else return;
            }

            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * lookSensitivity;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * lookSensitivity, -89f, 89f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? fastMultiplier : 1f);
            transform.position += transform.rotation * move * (speed * Time.unscaledDeltaTime);
            transform.position += transform.forward * (Input.mouseScrollDelta.y * scrollSpeed);
        }

        private void LateUpdate()
        {
            if (Mode != CameraMode.Follow) return;

            if (selection == null || selection.Selected == null) { SetMode(CameraMode.Free); return; }

            Vector3 targetPos = selection.Selected.transform.position;
            transform.position = Vector3.SmoothDamp(transform.position, targetPos + followOffset,
                ref followVelocity, followSmoothTime);

            Quaternion look = Quaternion.LookRotation(targetPos - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-lookDamping * Time.deltaTime));
        }

        private void SyncAnglesFromTransform()
        {
            Vector3 e = transform.eulerAngles;
            yaw = e.y;
            pitch = e.x > 180f ? e.x - 360f : e.x;
        }
    }
}
