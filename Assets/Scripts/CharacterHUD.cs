using UnityEngine;

namespace Game
{
    /// <summary>
    /// 螢幕操作提示、角色狀態與跑酷關卡 HUD
    /// </summary>
    public class CharacterHUD : MonoBehaviour
    {
        [Header("顯示設定")]
        [Tooltip("是否顯示螢幕操作提示")]
        public bool showHUD = true;

        private PlayerController player;
        private GUIStyle boxStyle;
        private GUIStyle headerStyle;
        private GUIStyle textStyle;
        private GUIStyle badgeStyle;
        private GUIStyle viewModeStyle;
        private GUIStyle timerStyle;
        private GUIStyle bannerStyle;
        private bool isStyleReady = false;

        private void Start()
        {
            player = GetComponent<PlayerController>();
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.hKey.wasPressedThisFrame)
            {
                showHUD = !showHUD;
            }
#else
            if (Input.GetKeyDown(KeyCode.H))
            {
                showHUD = !showHUD;
            }
#endif
        }

        private void SetupStyles()
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = CreateFlatTexture(2, 2, new Color(0.06f, 0.08f, 0.12f, 0.90f));
            boxStyle.padding = new RectOffset(16, 16, 14, 14);

            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 15;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = new Color(0.2f, 0.85f, 1f);

            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 13;
            textStyle.normal.textColor = new Color(0.92f, 0.95f, 0.98f);

            badgeStyle = new GUIStyle(GUI.skin.label);
            badgeStyle.fontSize = 13;
            badgeStyle.fontStyle = FontStyle.Bold;
            badgeStyle.normal.textColor = new Color(0.4f, 1f, 0.4f);

            viewModeStyle = new GUIStyle(GUI.skin.label);
            viewModeStyle.fontSize = 13;
            viewModeStyle.fontStyle = FontStyle.Bold;
            viewModeStyle.normal.textColor = new Color(1f, 0.82f, 0.2f);

            timerStyle = new GUIStyle(GUI.skin.label);
            timerStyle.fontSize = 14;
            timerStyle.fontStyle = FontStyle.Bold;
            timerStyle.normal.textColor = new Color(1f, 0.6f, 0.2f);

            bannerStyle = new GUIStyle(GUI.skin.box);
            bannerStyle.normal.background = CreateFlatTexture(2, 2, new Color(0.04f, 0.06f, 0.1f, 0.95f));
            bannerStyle.fontSize = 17;
            bannerStyle.fontStyle = FontStyle.Bold;
            bannerStyle.alignment = TextAnchor.MiddleCenter;
            bannerStyle.normal.textColor = new Color(1f, 0.9f, 0.3f);

            isStyleReady = true;
        }

        private Texture2D CreateFlatTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            if (!isStyleReady) SetupStyles();

            // 頂部中央橫幅通知 (例如抵達檢查點 / 墜落重生 / 恭喜通關)
            var manager = ParkourCourseManager.Instance;
            if (manager != null && !string.IsNullOrEmpty(manager.latestNotification))
            {
                float bannerWidth = 460f;
                float bannerHeight = 44f;
                Rect bannerRect = new Rect((Screen.width - bannerWidth) / 2f, 25f, bannerWidth, bannerHeight);
                GUI.Box(bannerRect, manager.latestNotification, bannerStyle);
            }

            if (!showHUD) return;

            // 左上角操作與狀態面板
            GUILayout.BeginArea(new Rect(20, 20, 310, 385), boxStyle);

            GUILayout.Label("🎮 角色操控說明 (Controls)", headerStyle);
            GUILayout.Space(4);
            GUILayout.Label("• W / A / S / D ： 前後移動 / 左右橫移", textStyle);
            GUILayout.Label("• Left Shift    ： 衝刺加速 (助跑大跳)", textStyle);
            GUILayout.Label("• 空白鍵 (Space)： 跳躍", textStyle);
            GUILayout.Label("• 滑鼠移動      ： 360° 轉向與上下仰望", textStyle);
            GUILayout.Label("• V 鍵          ： 切換第一 / 第三人稱", textStyle);
            GUILayout.Label("• B 鍵 / Back   ： 返回上一個存檔點 (Checkpoint)", textStyle);
            GUILayout.Label("• R 鍵          ： 重新開始跑酷關卡", textStyle);
            GUILayout.Label("• ESC           ： 釋放滑鼠指標", textStyle);
            GUILayout.Label("• 畫面點擊      ： 重新鎖定指標", textStyle);
            GUILayout.Label("• H 鍵          ： 隱藏/顯示此面板", textStyle);
            GUILayout.Space(6);

            if (player != null)
            {
                string viewModeStr = player.IsFirstPerson ? "👁️ 第一人稱視角" : "🎥 第三人稱視角";
                GUILayout.Label($"當前視角: {viewModeStr}", viewModeStyle);

                string stateStr = "🧍 靜止";
                if (!player.IsGrounded)
                {
                    stateStr = "✈️ 跳躍中";
                }
                else if (player.IsSprinting && player.CurrentSpeed > 4f)
                {
                    stateStr = "🏃‍♂️ 衝刺中";
                }
                else if (player.CurrentSpeed > 0.3f)
                {
                    stateStr = "🚶 移動中";
                }

                GUILayout.Label($"角色狀態: {stateStr} | 速度: {player.CurrentSpeed:F1} m/s", badgeStyle);
            }

            // 跑酷關卡即時進度
            if (manager != null)
            {
                GUILayout.Space(4);
                GUILayout.Label("────────── 🏃 跑酷挑戰 ──────────", textStyle);
                string statusText = manager.isCompleted ? "🏆 已通關！" : (manager.isTimerRunning ? "⏱️ 挑戰進行中" : "🏁 起點等待出發");
                GUILayout.Label($"計時: {manager.GetCurrentTimeString()} ({statusText})", timerStyle);
                GUILayout.Label($"進度: Checkpoint {manager.currentCheckpointIndex} / {Mathf.Max(1, manager.TotalCheckpoints - 1)} | 墜落: {manager.fallCount} 次", textStyle);
                if (manager.bestTime > 0)
                {
                    GUILayout.Label($"最佳紀錄: {manager.GetBestTimeString()}", badgeStyle);
                }
            }

            GUILayout.EndArea();
        }
    }
}
