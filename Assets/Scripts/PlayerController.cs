using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game
{
    /// <summary>
    /// 角色控制器 (支援第一人稱/第三人稱雙視角、Unity 新舊 Input System、跳躍、重力、衝刺、橫移)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("===== 視角模式 (Perspective) =====")]
        [Tooltip("是否為第一人稱視角 (預設開啟，可按 V 鍵即時切換)")]
        public bool isFirstPerson = true;

        [Header("===== 移動參數 (Movement) =====")]
        [Tooltip("正常行走速度 (米/秒)")]
        public float walkSpeed = 3.5f;

        [Tooltip("衝刺速度 (米/秒)")]
        public float sprintSpeed = 7.0f;

        [Tooltip("第三人稱轉向平滑時間")]
        public float rotationSmoothTime = 0.1f;

        [Tooltip("跳躍高度 (米)")]
        public float jumpHeight = 1.6f;

        [Tooltip("重力加速度 (向下為負)")]
        public float gravity = -20.0f;

        [Header("===== 視角與相機 (Camera) =====")]
        [Tooltip("參考的相機 (決定角色前後左右移動朝向，預設為主相機)")]
        public Transform cameraTransform;

        [Header("===== 動畫組件 (Animator) =====")]
        [Tooltip("動畫控制器 (留空會自動抓取子物件中的 Animator)")]
        public Animator animator;

        // 私有控制變數
        private CharacterController controller;
        private Vector3 verticalVelocityVector;
        private float rotationVelocity;
        private bool isGrounded;
        private float currentSpeed;
        private bool isSprinting;
        private ThirdPersonCamera cameraController;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            cameraController = FindFirstObjectByType<ThirdPersonCamera>();
            if (cameraController != null)
            {
                isFirstPerson = cameraController.isFirstPerson;
            }
        }

        private void Update()
        {
            // 動態取得主相機與相機控制器
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (cameraController == null && Camera.main != null)
            {
                cameraController = Camera.main.GetComponent<ThirdPersonCamera>();
            }

            // 按 V 鍵切換第一人稱 / 第三人稱視角
            HandlePerspectiveToggle();

            // 按 B / Backspace / K 鍵返回存檔點
            HandleCheckpointReturnInput();

            // 著地檢測
            isGrounded = controller.isGrounded;
            if (isGrounded && verticalVelocityVector.y < 0)
            {
                // 給予輕微下壓初速，讓角色在斜坡或小階梯上貼地更穩固
                verticalVelocityVector.y = -2f;
            }

            // 讀取輸入
            Vector2 input = GetMovementInput();
            isSprinting = GetSprintInput();
            bool jumpPressed = GetJumpInput();

            // 計算目標水平速度
            float targetSpeed = 0f;
            if (input.sqrMagnitude > 0.01f)
            {
                targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            }

            // 平滑漸變速度
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 12f);

            // 計算移動向量
            Vector3 moveDirection = Vector3.zero;

            if (isFirstPerson)
            {
                // 第一人稱：前後左右移動 (W: 前, S: 後, A: 左, D: 右)
                // 沿著角色當前水平朝向直接移動
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                moveDirection = (forward * input.y + right * input.x).normalized;
                // 第一人稱模式下，角色的水平旋轉直接由相機即時控制
            }
            else
            {
                // 第三人稱：相對於相機視角的方向移動
                if (input.sqrMagnitude > 0.01f)
                {
                    Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
                    Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;

                    forward.y = 0f;
                    right.y = 0f;
                    forward.Normalize();
                    right.Normalize();

                    moveDirection = (forward * input.y + right * input.x).normalized;

                    // 角色平滑旋轉朝向移動方向
                    float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);
                }
            }

            // 執行水平位移
            controller.Move(moveDirection * (currentSpeed * Time.deltaTime));

            // 跳躍處理
            if (jumpPressed && isGrounded)
            {
                verticalVelocityVector.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null)
                {
                    animator.SetTrigger("Jump");
                }
            }

            // 套用重力加速度
            verticalVelocityVector.y += gravity * Time.deltaTime;
            controller.Move(verticalVelocityVector * Time.deltaTime);

            // 消除頂頭碰撞卡住 bug (跳躍碰到上方障礙時，立即將向上垂直速度歸零，防止黏頂/浮空卡頓)
            if ((controller.collisionFlags & CollisionFlags.Above) != 0 && verticalVelocityVector.y > 0)
            {
                verticalVelocityVector.y = 0f;
            }

            // 更新動畫參數
            UpdateAnimator(input.magnitude);
        }

        private void HandleCheckpointReturnInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.bKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame))
            {
                ReturnToCurrentCheckpoint();
            }
            var pad = Gamepad.current;
            if (pad != null && pad.selectButton.wasPressedThisFrame)
            {
                ReturnToCurrentCheckpoint();
            }
#else
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.K))
            {
                ReturnToCurrentCheckpoint();
            }
#endif
        }

        public void ReturnToCurrentCheckpoint()
        {
            var manager = ParkourCourseManager.Instance;
            if (manager != null)
            {
                manager.ReturnToCheckpoint();
            }
        }

        private void HandlePerspectiveToggle()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.vKey.wasPressedThisFrame)
            {
                TogglePerspective();
            }
#else
            if (Input.GetKeyDown(KeyCode.V))
            {
                TogglePerspective();
            }
#endif
        }

        public void TogglePerspective()
        {
            isFirstPerson = !isFirstPerson;
            if (cameraController != null && cameraController.isFirstPerson != isFirstPerson)
            {
                cameraController.SetPerspective(isFirstPerson);
            }
        }

        private void UpdateAnimator(float inputMagnitude)
        {
            if (animator == null) return;

            bool isMoving = inputMagnitude > 0.05f;
            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("Speed", currentSpeed);
        }

        /// <summary>
        /// 獲取移動輸入 (相容 Unity New Input System 與舊版 Input)
        /// </summary>
        private Vector2 GetMovementInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > input.sqrMagnitude)
                {
                    input = stick;
                }
            }
#else
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
#endif

            return Vector2.ClampMagnitude(input, 1f);
        }

        /// <summary>
        /// 獲取衝刺輸入 (Shift 或手把按鍵)
        /// </summary>
        private bool GetSprintInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            bool kbSprint = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
            var pad = Gamepad.current;
            bool padSprint = pad != null && (pad.leftStickButton.isPressed || pad.rightTrigger.isPressed);
            return kbSprint || padSprint;
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        /// <summary>
        /// 獲取跳躍輸入 (空白鍵或手把按鈕)
        /// </summary>
        private bool GetJumpInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            bool kbJump = kb != null && kb.spaceKey.wasPressedThisFrame;
            var pad = Gamepad.current;
            bool padJump = pad != null && pad.buttonSouth.wasPressedThisFrame;
            return kbJump || padJump;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        // 外部讀取屬性
        public float CurrentSpeed => currentSpeed;
        public bool IsGrounded => isGrounded;
        public bool IsSprinting => isSprinting;
        public bool IsFirstPerson => isFirstPerson;

        /// <summary>
        /// 安全傳送角色至指定位置（防止 CharacterController 干擾坐標更新）
        /// </summary>
        public void Teleport(Vector3 targetPosition, Quaternion targetRotation)
        {
            if (controller != null) controller.enabled = false;
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            verticalVelocityVector = Vector3.zero;
            currentSpeed = 0f;
            if (controller != null) controller.enabled = true;
        }

        public void ResetVelocity()
        {
            verticalVelocityVector = Vector3.zero;
            currentSpeed = 0f;
        }
    }
}
