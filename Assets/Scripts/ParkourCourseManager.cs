using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game
{
    /// <summary>
    /// 跑酷關卡總管理器 (負責檢查點登記、跌落重生、關卡計時、通關判定與一鍵重開)
    /// </summary>
    public class ParkourCourseManager : MonoBehaviour
    {
        public static ParkourCourseManager Instance { get; private set; }

        [Header("===== 跌落重生設定 =====")]
        [Tooltip("跌落判定 Y 座標 (角色低於此高度時自動重生至最近檢查點)")]
        public float fallThresholdY = -3.5f;

        [Header("===== 關卡狀態 (唯讀觀察) =====")]
        public int currentCheckpointIndex = 0;
        public bool isTimerRunning = false;
        public bool isCompleted = false;
        public float elapsedTime = 0f;
        public float bestTime = 0f;
        public int fallCount = 0;

        [Header("===== 提示訊息 =====")]
        public string latestNotification = "";
        private float notificationTimer = 0f;

        private PlayerController player;
        private List<ParkourCheckpoint> checkpoints = new List<ParkourCheckpoint>();
        private Vector3 defaultStartPos = new Vector3(0, 0.5f, 0);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            RefreshCheckpoints();
        }

        private void Start()
        {
            FindPlayer();
            ResetCourse(teleportPlayer: false);
        }

        public void RefreshCheckpoints()
        {
            checkpoints = GetComponentsInChildren<ParkourCheckpoint>()
                .OrderBy(c => c.checkpointIndex)
                .ToList();

            if (checkpoints.Count > 0)
            {
                defaultStartPos = checkpoints[0].RespawnPosition;
            }
        }

        private void FindPlayer()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }
        }

        private void Update()
        {
            if (player == null)
            {
                FindPlayer();
                if (player == null) return;
            }

            // 更新提示訊息淡出倒數
            if (notificationTimer > 0)
            {
                notificationTimer -= Time.deltaTime;
                if (notificationTimer <= 0)
                {
                    latestNotification = "";
                }
            }

            // 跑酷計時器
            if (isTimerRunning && !isCompleted)
            {
                elapsedTime += Time.deltaTime;
            }

            // 跌落偵測與自動重生
            if (player.transform.position.y < fallThresholdY)
            {
                RespawnPlayer();
            }

            // 按 B / Backspace / K 鍵返回存檔點
            HandleReturnToCheckpointInput();

            // 按 R 鍵隨時重開關卡
            HandleRestartInput();
        }

        private void HandleReturnToCheckpointInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.bKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame))
            {
                ReturnToCheckpoint();
            }
            var pad = Gamepad.current;
            if (pad != null && pad.selectButton.wasPressedThisFrame)
            {
                ReturnToCheckpoint();
            }
#else
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.K))
            {
                ReturnToCheckpoint();
            }
#endif
        }

        private void HandleRestartInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                RestartCourse();
            }
#else
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartCourse();
            }
#endif
        }

        public void OnPlayerReachCheckpoint(ParkourCheckpoint cp)
        {
            if (cp.isGoal)
            {
                if (!isCompleted)
                {
                    isCompleted = true;
                    isTimerRunning = false;
                    if (bestTime <= 0f || elapsedTime < bestTime)
                    {
                        bestTime = elapsedTime;
                    }
                    ShowNotification($"🏆 恭喜通關！總用時：{FormatTime(elapsedTime)} (跌落 {fallCount} 次)", 6f);
                    Debug.Log($"[Parkour] 🏆 玩家成功通關跑酷關卡！用時：{FormatTime(elapsedTime)}");
                }
                return;
            }

            // 首次抵達或前進至更高進度檢查點
            if (cp.checkpointIndex > currentCheckpointIndex)
            {
                currentCheckpointIndex = cp.checkpointIndex;
                ShowNotification($"🚩 抵達【{cp.checkpointName}】(已存檔)", 3f);
                Debug.Log($"[Parkour] 抵達檢查點: {cp.checkpointName} ({cp.checkpointIndex})");

                // 從起點出發時正式啟動計時
                if (!isTimerRunning && !isCompleted)
                {
                    isTimerRunning = true;
                }
            }
            else if (!isTimerRunning && !isCompleted && cp.checkpointIndex == 0)
            {
                // 玩家站在起點
                isTimerRunning = true;
            }
        }

        /// <summary>
        /// 玩家手動按鍵返回存檔點 (不會增加墜落計數)
        /// </summary>
        public void ReturnToCheckpoint()
        {
            Vector3 targetPos = defaultStartPos;
            Quaternion targetRot = Quaternion.identity;

            string cpName = "起點平台";
            if (checkpoints.Count > 0 && currentCheckpointIndex >= 0 && currentCheckpointIndex < checkpoints.Count)
            {
                targetPos = checkpoints[currentCheckpointIndex].RespawnPosition;
                targetRot = checkpoints[currentCheckpointIndex].RespawnRotation;
                cpName = checkpoints[currentCheckpointIndex].checkpointName;
            }

            if (player != null)
            {
                player.Teleport(targetPos, targetRot);
            }

            ShowNotification($"↩️ 已返回存檔點：【{cpName}】", 2.2f);
            Debug.Log($"[Parkour] 玩家手動返回存檔點：{cpName} (Index: {currentCheckpointIndex})");
        }

        public void RespawnPlayer()
        {
            fallCount++;
            ShowNotification("⚠️ 不慎墜落！已返回上一個檢查點", 2.5f);

            Vector3 targetPos = defaultStartPos;
            Quaternion targetRot = Quaternion.identity;

            if (checkpoints.Count > 0 && currentCheckpointIndex >= 0 && currentCheckpointIndex < checkpoints.Count)
            {
                targetPos = checkpoints[currentCheckpointIndex].RespawnPosition;
                targetRot = checkpoints[currentCheckpointIndex].RespawnRotation;
            }

            if (player != null)
            {
                player.Teleport(targetPos, targetRot);
            }
        }

        public void RestartCourse()
        {
            ResetCourse(teleportPlayer: true);
            ShowNotification("🔄 已重置跑酷關卡，重新開始計時！", 2.5f);
        }

        public void ResetCourse(bool teleportPlayer = true)
        {
            currentCheckpointIndex = 0;
            elapsedTime = 0f;
            isTimerRunning = false;
            isCompleted = false;
            fallCount = 0;

            if (teleportPlayer && player != null)
            {
                Vector3 startPos = checkpoints.Count > 0 ? checkpoints[0].RespawnPosition : defaultStartPos;
                Quaternion startRot = checkpoints.Count > 0 ? checkpoints[0].RespawnRotation : Quaternion.identity;
                player.Teleport(startPos, startRot);
            }
        }

        public void ShowNotification(string msg, float duration = 3f)
        {
            latestNotification = msg;
            notificationTimer = duration;
        }

        public static string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            float seconds = timeInSeconds % 60f;
            return $"{minutes:00}:{seconds:05.2f}";
        }

        public string GetCurrentTimeString() => FormatTime(elapsedTime);
        public string GetBestTimeString() => bestTime > 0f ? FormatTime(bestTime) : "--:--.--";
        public int TotalCheckpoints => checkpoints.Count;
    }
}
