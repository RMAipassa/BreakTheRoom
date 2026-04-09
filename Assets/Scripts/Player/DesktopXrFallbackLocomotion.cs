using UnityEngine;

namespace BreakTheRoom.Player
{
    public class DesktopXrFallbackLocomotion : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Transform lookPivot;
        [SerializeField] private float moveSpeed = 4.2f;
        [SerializeField] private float sprintMultiplier = 1.7f;
        [SerializeField] private float turnSpeed = 230f;
        [SerializeField] private float lookSpeed = 175f;
        [SerializeField] private float minPitch = -75f;
        [SerializeField] private float maxPitch = 75f;

        private Quaternion _lookPivotBaseRotation;
        private float _pitch;

        private void Awake()
        {
            if (lookPivot == null && head != null && head.parent != null)
            {
                lookPivot = head.parent;
            }

            if (lookPivot != null)
            {
                _lookPivotBaseRotation = lookPivot.localRotation;
            }
        }

        private void Update()
        {
            HandleCursorLock();

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.T))
            {
                return;
            }

            HandleTurn();
            HandlePitchLook();
            HandleMove();
        }

        private void HandleCursorLock()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Input.GetMouseButtonDown(2))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleTurn()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            var yaw = Input.GetAxis("Mouse X") * turnSpeed * Time.deltaTime;
            transform.Rotate(0f, yaw, 0f, Space.World);
        }

        private void HandlePitchLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked || lookPivot == null)
            {
                return;
            }

            var pitchDelta = -Input.GetAxis("Mouse Y") * lookSpeed * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch + pitchDelta, minPitch, maxPitch);
            lookPivot.localRotation = _lookPivotBaseRotation * Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * sprintMultiplier : moveSpeed;

            var basis = head != null ? head : transform;
            var forward = Vector3.ProjectOnPlane(basis.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = transform.forward;
            }

            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var delta = (right * input.x + forward * input.z).normalized * (speed * Time.deltaTime);
            transform.position += delta;
        }
    }
}
