using UnityEngine;

namespace TriageTrace.Presentation
{
    /// <summary>
    /// Grounded first-person controller for inspecting the Triage Trace simulation in Game View.
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
        private bool requireRightMouseButton;

        [SerializeField]
        private bool lockCursorWhileLooking = true;

        [SerializeField]
        private float minPitch = -80.0f;

        [SerializeField]
        private float maxPitch = 80.0f;

        [SerializeField]
        [Tooltip("Enables Q/E vertical free-flight for editor inspection. Disabled by default so normal movement remains grounded.")]
        private bool allowFlyMode;

        [SerializeField]
        [Min(0.1f)]
        private float gravity = 20.0f;

        [SerializeField]
        [Min(0.01f)]
        private float jumpHeight = 1.60f;

        private CharacterController characterController;
        private float yaw;
        private float pitch;
        private float verticalVelocity;
        private bool cursorLockedByController;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                ApplyDefaultCharacterControllerSettings(characterController);
                Debug.LogWarning("FirstPersonCameraController added a missing CharacterController at runtime. Configure it in the prototype scene with Triage Trace > Configure Grounded First-Person Controller.", this);
            }

            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizePitch(euler.x);

            if (lockCursorWhileLooking && !requireRightMouseButton)
            {
                LockCursor();
            }
        }

        private void Update()
        {
            bool looking = UpdateCursorState();

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

            Quaternion yawRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            Vector3 planarDirection =
                yawRotation * Vector3.right * horizontal +
                yawRotation * Vector3.forward * vertical;

            if (planarDirection.sqrMagnitude > 1.0f)
            {
                planarDirection.Normalize();
            }

            float speed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                speed *= sprintMultiplier;
            }

            Vector3 motion = planarDirection * speed;
            if (allowFlyMode)
            {
                float flyDirection = 0.0f;
                if (Input.GetKey(KeyCode.E))
                {
                    flyDirection += 1.0f;
                }

                if (Input.GetKey(KeyCode.Q))
                {
                    flyDirection -= 1.0f;
                }

                verticalVelocity = 0.0f;
                motion.y = flyDirection * speed;
            }
            else
            {
                if (characterController.isGrounded)
                {
                    if (verticalVelocity < 0.0f)
                    {
                        // A small downward value keeps the capsule seated on uneven collider surfaces.
                        verticalVelocity = -2.0f;
                    }

                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        verticalVelocity = Mathf.Sqrt(jumpHeight * 2.0f * gravity);
                    }
                }

                verticalVelocity -= gravity * Time.deltaTime;
                motion.y = verticalVelocity;
            }

            characterController.Move(motion * Time.deltaTime);
        }

        private bool UpdateCursorState()
        {
            if (!lockCursorWhileLooking)
            {
                return !requireRightMouseButton || Input.GetMouseButton(1);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnlockCursor();
                return false;
            }

            if (!cursorLockedByController)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    LockCursor();
                }

                // Do not rotate from the click that reclaims the cursor.
                return false;
            }

            return !requireRightMouseButton || Input.GetMouseButton(1);
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursorLockedByController = true;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cursorLockedByController = false;
        }

        private static float NormalizePitch(float angle)
        {
            return angle > 180.0f ? angle - 360.0f : angle;
        }

        private static void ApplyDefaultCharacterControllerSettings(CharacterController controller)
        {
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0.0f, -0.9f, 0.0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45.0f;
        }
    }
}
