using System;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game
{
    /// <summary>
    /// 雙視角相機控制器 (預設第一人稱視角，支援按 V 鍵切換第一/第三人稱、滑鼠環繞、滾輪縮放、防穿牆)
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("===== 視角模式 (Perspective) =====")]
        [Tooltip("是否為第一人稱視角 (預設開啟，可按 V 鍵即時切換)")]
        public bool isFirstPerson = true;

        [Header("===== 追蹤目標 (Target) =====")]
        [Tooltip("相機跟隨的目標角色 Transform")]
        public Transform target;

        [Tooltip("第三人稱追蹤中心偏移 (瞄準胸部或頭部高，避免瞄準腳底)")]
        public Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);

        [Header("===== 第一人稱參數 (First-Person) =====")]
        [Tooltip("第一人稱眼部基準高度 (若找到頭部骨骼則優先以骨骼為準)")]
        public float firstPersonEyeHeight = 1.62f;

        [Tooltip("第一人稱眼部向前偏移 (避免視角穿入臉部/鼻樑內部)")]
        public float firstPersonForwardOffset = 0.22f;

        [Tooltip("第一人稱眼部向上偏移")]
        public float firstPersonHeightOffset = 0.05f;

        [Tooltip("第一人稱視角最低俯角 (向下看地面/雙腳)")]
        public float firstPersonMaxPitch = 85f;

        [Tooltip("第一人稱視角最高仰角 (向上仰望天空)")]
        public float firstPersonMinPitch = -85f;

        [Header("===== 第三人稱距離與縮放 (Third-Person Distance) =====")]
        [Tooltip("第三人稱相機與目標的預設距離")]
        public float distance = 4.0f;

        [Tooltip("最近距離")]
        public float minDistance = 1.2f;

        [Tooltip("最遠距離")]
        public float maxDistance = 8.0f;

        [Tooltip("滾輪縮放步進大小")]
        public float zoomStep = 0.6f;

        [Header("===== 旋轉靈敏度 (Sensitivity) =====")]
        [Tooltip("水平旋轉速度")]
        public float sensitivityX = 2.0f;

        [Tooltip("垂直旋轉速度")]
        public float sensitivityY = 1.5f;

        [Tooltip("第三人稱最低仰角限制")]
        public float minPitch = -20f;

        [Tooltip("第三人稱最高俯角限制")]
        public float maxPitch = 65f;

        [Tooltip("相機旋轉平滑度")]
        public float rotationSmooth = 20f;

        [Tooltip("第三人稱相機位置平滑度")]
        public float positionSmooth = 20f;

        [Header("===== 防穿牆檢測 (Collision) =====")]
        [Tooltip("是否啟用防穿牆檢測")]
        public bool enableCollision = true;

        [Tooltip("防穿牆半徑")]
        public float collisionRadius = 0.25f;

        [Tooltip("檢測碰撞的層級 (預設排除玩家)")]
        public LayerMask obstacleMask = ~0;

        // 私有狀態變數
        private float yaw;
        private float pitch = 0f;
        private float targetYaw;
        private float targetPitch = 0f;
        private float currentDistance;
        private Camera cam;
        private Transform headBone;
        private PlayerController playerController;

        private void Start()
        {
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                // 第一人稱視角需要超近 nearClipPlane (0.01m) 徹底杜絕近距離穿透頭部/帽子
                cam.nearClipPlane = 0.01f;
            }

            FindTargetAndReferences();

            currentDistance = distance;
            targetYaw = transform.eulerAngles.y;
            targetPitch = transform.eulerAngles.x;
            yaw = targetYaw;
            pitch = targetPitch;

            // 預設鎖定滑鼠指標以獲得順暢視角體驗
            SetCursorLocked(true);

            // 確保目標角色同步視角模式
            SyncPerspectiveToPlayer();
        }

        private void FindTargetAndReferences()
        {
            if (target == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null)
                {
                    target = pc.transform;
                    playerController = pc;
                }
                else
                {
                    var playerObj = GameObject.FindWithTag("Player");
                    if (playerObj != null)
                    {
                        target = playerObj.transform;
                        playerController = playerObj.GetComponent<PlayerController>();
                    }
                }
            }
            else if (playerController == null)
            {
                playerController = target.GetComponent<PlayerController>();
            }

            FindHeadBone();
        }

        private void FindHeadBone()
        {
            if (target == null) return;
            Transform[] transforms = target.GetComponentsInChildren<Transform>();
            headBone = transforms.FirstOrDefault(t =>
                t.name.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0
                && !t.name.Contains("Top"));
        }

        private void Update()
        {
            HandleCursorToggle();
            ProcessInput();

            if (isFirstPerson && target != null)
            {
                // 第一人稱即時更新角色朝向 (Yaw)
                target.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindTargetAndReferences();
                if (target == null) return;
            }

            if (isFirstPerson)
            {
                UpdateFirstPerson();
            }
            else
            {
                UpdateThirdPerson();
            }
        }

        private void UpdateFirstPerson()
        {
            // 第一人稱：偏航角完全與角色同步，垂直俯仰角平滑內插
            pitch = Mathf.Lerp(pitch, targetPitch, Time.deltaTime * rotationSmooth);
            yaw = targetYaw;

            // 同步角色旋轉
            target.rotation = Quaternion.Euler(0f, yaw, 0f);

            // 取得眼部精確位置
            Vector3 eyePos = GetEyePosition();
            transform.position = eyePos;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void UpdateThirdPerson()
        {
            // 平滑內插旋轉角度
            yaw = Mathf.Lerp(yaw, targetYaw, Time.deltaTime * rotationSmooth);
            pitch = Mathf.Lerp(pitch, targetPitch, Time.deltaTime * rotationSmooth);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focusPoint = target.position + targetOffset;

            // 計算無碰撞時的理想相機位置
            Vector3 desiredPosition = focusPoint - (rotation * Vector3.forward * distance);

            // 射線防穿牆
            float finalDistance = distance;
            if (enableCollision)
            {
                Vector3 rayDir = desiredPosition - focusPoint;
                if (Physics.SphereCast(focusPoint, collisionRadius, rayDir.normalized, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    // 若碰到障礙物，將距離拉近以避開障礙
                    finalDistance = Mathf.Clamp(hit.distance - collisionRadius, minDistance, distance);
                }
            }

            currentDistance = Mathf.Lerp(currentDistance, finalDistance, Time.deltaTime * positionSmooth);
            Vector3 finalPosition = focusPoint - (rotation * Vector3.forward * currentDistance);

            transform.position = finalPosition;
            transform.LookAt(focusPoint);
        }

        private Vector3 GetEyePosition()
        {
            if (headBone == null)
            {
                FindHeadBone();
            }

            if (headBone != null)
            {
                // 以頭部骨骼世界座標為基礎，向前微調至眼睛位置，自然跟隨跑步循環擺動
                return headBone.position + target.forward * firstPersonForwardOffset + Vector3.up * firstPersonHeightOffset;
            }

            return target.position + Vector3.up * firstPersonEyeHeight + target.forward * firstPersonForwardOffset;
        }

        private void ProcessInput()
        {
            Vector2 mouseDelta = Vector2.zero;
            float scroll = 0f;

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                mouseDelta = mouse.delta.ReadValue() * 0.1f;
                scroll = mouse.scroll.ReadValue().y;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.rightStick.ReadValue();
                mouseDelta += stick * 2.5f;
            }

            var kb = Keyboard.current;
            if (kb != null && kb.vKey.wasPressedThisFrame)
            {
                TogglePerspective();
            }
#else
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                mouseDelta.x = Input.GetAxis("Mouse X");
                mouseDelta.y = Input.GetAxis("Mouse Y");
                scroll = Input.GetAxis("Mouse ScrollWheel");
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                TogglePerspective();
            }
#endif

            // 更新目標俯仰與偏航角
            targetYaw += mouseDelta.x * sensitivityX;
            targetPitch -= mouseDelta.y * sensitivityY;

            float currentMinPitch = isFirstPerson ? firstPersonMinPitch : minPitch;
            float currentMaxPitch = isFirstPerson ? firstPersonMaxPitch : maxPitch;
            targetPitch = Mathf.Clamp(targetPitch, currentMinPitch, currentMaxPitch);

            // 滾輪縮放邏輯 (支援滾輪推進切換第一人稱 / 拉遠切換第三人稱)
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float direction = Mathf.Sign(scroll);
                if (isFirstPerson)
                {
                    if (direction < 0)
                    {
                        // 第一人稱向下滾動：切換至第三人稱
                        SetPerspective(false);
                        distance = 2.5f;
                    }
                }
                else
                {
                    if (direction > 0 && distance <= minDistance + 0.1f)
                    {
                        // 第三人稱已滾至最近距離，再滾動一次切換為第一人稱
                        SetPerspective(true);
                    }
                    else
                    {
                        distance = Mathf.Clamp(distance - direction * zoomStep, minDistance, maxDistance);
                    }
                }
            }
        }

        public void TogglePerspective()
        {
            SetPerspective(!isFirstPerson);
        }

        public void SetPerspective(bool firstPerson)
        {
            isFirstPerson = firstPerson;
            if (cam != null && isFirstPerson)
            {
                cam.nearClipPlane = 0.03f;
            }

            SyncPerspectiveToPlayer();
        }

        private void SyncPerspectiveToPlayer()
        {
            if (playerController == null && target != null)
            {
                playerController = target.GetComponent<PlayerController>();
            }

            if (playerController != null)
            {
                playerController.isFirstPerson = isFirstPerson;
            }
        }

        private void HandleCursorToggle()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLocked(true);
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetCursorLocked(false);
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLocked(true);
            }
#endif
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public bool IsFirstPerson => isFirstPerson;
    }
}
