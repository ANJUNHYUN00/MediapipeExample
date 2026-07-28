using UnityEngine;

namespace TriageTrace.Presentation
{
    /// <summary>
    /// Lightweight editor/demo camera controller for inspecting the Triage Trace scene in Game View.
    /// Attach this to Main Camera during local Unity simulation only.
    /// </summary>
    public sealed class FirstPersonCameraController : MonoBehaviour
    {
        [SerializeField]
        [Min(0.1f)]
        private float moveSpeed = 4.0f;

        [SerializeField]
        [Min(0.1f)]
        private float sprintMultiplier = 2.0f;

        [SerializeField]
        [Min(0.01f)]
        private float mouseSensitivity = 2.0f;

        [SerializeField]
        private bool requireRightMouseButton = true;

        [SerializeField]
        private bool lockCursorWhileLooking = true;

        [SerializeField]
        private float minPitch = -80.0f;

        [SerializeField]
        private float maxPitch = 80.0f;

        private float yaw;
        private float pitch;
        private bool cursorLockedByController;

        private void Awake()
        {
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizePitch(euler.x);
        }

        private void Update()
        {
            bool looking = !requireRightMouseButton || Input.GetMouseButton(1);
            UpdateCursorState(looking);

            if (looking)
            {
                UpdateLook();
            }

            UpdateMovement();
        }

        private void OnDisable()
        {
            if (cursorLockedByController)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                cursorLockedByController = false;
            }
        }

        private void UpdateLook()
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0.0f);
        }

        private void UpdateMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            float upDown = 0.0f;

            if (Input.GetKey(KeyCode.E))
            {
                upDown += 1.0f;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                upDown -= 1.0f;
            }

            Vector3 direction =
                transform.right * horizontal +
                transform.forward * vertical +
                Vector3.up * upDown;

            if (direction.sqrMagnitude > 1.0f)
            {
                direction.Normalize();
            }

            float speed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                speed *= sprintMultiplier;
            }

            transform.position += direction * speed * Time.deltaTime;
        }

        private void UpdateCursorState(bool looking)
        {
            if (!lockCursorWhileLooking)
            {
                return;
            }

            if (looking && !cursorLockedByController)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                cursorLockedByController = true;
            }
            else if (!looking && cursorLockedByController)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                cursorLockedByController = false;
            }
        }

        private static float NormalizePitch(float angle)
        {
            return angle > 180.0f ? angle - 360.0f : angle;
        }
    }
}
