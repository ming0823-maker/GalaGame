using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Game;

namespace Game.Editor
{
    /// <summary>
    /// 一鍵配置角色、第一人稱/第三人稱相機、動畫與多階段跑酷關卡的編輯器工具
    /// 操作角色：Meshy_AI_T_Pose_Boy_Running .fbx
    /// 支援之後導入使用者實拍生成的 3D 地形！
    /// </summary>
    [InitializeOnLoad]
    public class GameSetupEditor : EditorWindow
    {
        private const string MenuPath = "Tools/🎮 遊戲配置/一鍵配置人物與跑酷關卡";
        private const string WindowPath = "Tools/🎮 遊戲配置/開啟遊戲配置面板";

        public const string TargetCharacterFbx = "Assets/Meshy_AI_T_Pose_Boy_Running .fbx";

        static GameSetupEditor()
        {
            EditorApplication.delayCall += CheckAndPromptSetup;
        }

        private static void CheckAndPromptSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.isLoaded)
            {
                return;
            }

            PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
            ParkourCourseManager parkour = Object.FindFirstObjectByType<ParkourCourseManager>();
            string modelPath = GetPlayerModelPath();
            GameObject modelPrefab = !string.IsNullOrEmpty(modelPath) ? AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) : null;

            bool needsSetup = false;
            if (pc == null || parkour == null)
            {
                needsSetup = true;
            }
            else if (parkour.transform.Find("Stage4_Landing_Pad") != null || parkour.transform.Find("Stage4_Spin_1") == null)
            {
                // 檢測到舊版跳台存在或尚未升級為旋轉跳，自動觸發無縫升級
                needsSetup = true;
            }
            else if (modelPrefab != null)
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(pc.gameObject);
                if (source != modelPrefab)
                {
                    Debug.Log("[GameSetup] 檢測到角色模型非 Meshy_AI_T_Pose_Boy_Running .fbx，將自動切換為正確的操作角色...");
                    needsSetup = true;
                }
            }

            if (needsSetup)
            {
                ExecuteSetup(silent: true);
            }
        }

        [MenuItem(MenuPath, priority = 0)]
        public static void RunSetupMenu()
        {
            ExecuteSetup(silent: false);
        }

        [MenuItem(WindowPath, priority = 1)]
        public static void OpenWindow()
        {
            var window = GetWindow<GameSetupEditor>("跑酷關卡配置精靈");
            window.minSize = new Vector2(400, 320);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(12);
            GUILayout.Label("🎮 角色操控與 3D 跑酷關卡配置精靈", EditorStyles.boldLabel);
            GUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "當前操作角色：Meshy_AI_T_Pose_Boy_Running .fbx\n" +
                "當前預設視角：第一人稱視角 (可按 V 鍵切換第三人稱)\n\n" +
                "🏃 跑酷關卡特色 (6 大挑戰階段)：\n" +
                "1. 基礎進階階梯 (Warmup Steps)\n" +
                "2. 懸空浮石跳躍群 (Floating Pillars)\n" +
                "3. 高空折角平衡木 (Narrow Catwalks, 4m 高)\n" +
                "4. 螺旋旋轉跳躍群 (Spiral & Rotating Jump Path，自轉動態跳台)\n" +
                "5. 螺旋垂直攀升塔 (Spiral Observation Tower, 10m 高)\n" +
                "6. 信仰之躍與終點神殿 (Leap of Faith & Victory Sanctuary)\n\n" +
                "💡 跌落自動返回最近檢查點，並提供關卡計時與一鍵 R 鍵重置！\n" +
                "💡 未來可直接將實拍生成的 3D 地形拖入場景中，跑酷結構獨立模組化，方便自由貼齊與對齊！",
                MessageType.Info);

            GUILayout.Space(16);
            if (GUILayout.Button("🚀 立即一鍵配置 / 重新生成跑酷關卡", GUILayout.Height(42)))
            {
                ExecuteSetup(silent: false);
            }
        }

        public static void ExecuteSetup(bool silent = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup Parkour Game");
            int undoGroup = Undo.GetCurrentGroup();

            Debug.Log("[GameSetup] 開始一鍵配置跑酷遊戲場景 (角色：Meshy_AI_T_Pose_Boy_Running .fbx)...");

            // 1. 設置基礎環境地面
            SetupEnvironment();

            // 2. 設置 6 大階段獨立跑酷關卡結構 (可與未來實拍地形整合)
            SetupParkourCourse();

            // 3. 尋找模型並建立 Player (操作角色為 Meshy_AI_T_Pose_Boy_Running .fbx)
            GameObject playerObj = SetupPlayer();

            // 4. 設定動畫與 Animator Controller
            if (playerObj != null)
            {
                SetupAnimations(playerObj);
            }

            // 5. 設定第一人稱/第三人稱相機
            if (playerObj != null)
            {
                SetupCamera(playerObj);
            }

            // 儲存場景
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                if (!string.IsNullOrEmpty(activeScene.path))
                {
                    EditorSceneManager.SaveScene(activeScene);
                    Debug.Log($"[GameSetup] 場景已成功儲存至 {activeScene.path}！");
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[GameSetup] 一鍵配置完成！跑酷關卡與第一人稱操作角色皆已就緒。");

            if (!silent)
            {
                EditorUtility.DisplayDialog(
                    "🎉 跑酷關卡配置完成！",
                    "操作角色：Meshy_AI_T_Pose_Boy_Running .fbx\n" +
                    "預設視角：【👁️ 第一人稱視角】(按 V 可切換)\n" +
                    "關卡結構：6 大跑酷挑戰區域已完整建構！\n\n" +
                    "操作指南：\n" +
                    "• W / A / S / D ： 移動與左右橫移 (Strafe)\n" +
                    "• Left Shift    ： 衝刺加速 (跳躍前助跑極限大跳)\n" +
                    "• 空白鍵 (Space)： 跳躍\n" +
                    "• 滑鼠移動      ： 360° 轉向與上下仰望\n" +
                    "• V 鍵          ： 即時切換第一人稱 / 第三人稱視角\n" +
                    "• B 鍵          ： 即時返回上一個存檔點 (Checkpoint)\n" +
                    "• R 鍵          ： 重新開始跑酷計時與挑戰\n" +
                    "• ESC           ： 釋放滑鼠指標\n\n" +
                    "💡 提示：若不慎跌落或卡住，按 B 鍵即可瞬間回到存檔點！",
                    "太棒了，開始挑戰！");
            }
        }

        private static void SetupEnvironment()
        {
            // 檢查環境根節點
            GameObject env = GameObject.Find("Environment");
            if (env == null)
            {
                env = new GameObject("Environment");
                Undo.RegisterCreatedObjectUndo(env, "Create Environment");
            }

            // 建立主基底地面
            Transform groundTrans = env.transform.Find("Ground");
            GameObject groundObj;
            if (groundTrans == null)
            {
                groundObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                groundObj.name = "Ground";
                groundObj.transform.SetParent(env.transform);
                Undo.RegisterCreatedObjectUndo(groundObj, "Create Ground");
            }
            else
            {
                groundObj = groundTrans.gameObject;
            }

            groundObj.transform.position = new Vector3(0, -1.0f, 40f);
            groundObj.transform.localScale = new Vector3(80f, 1f, 130f);

            // 套用 HDRP Lit 材質
            Material groundMat = GetOrCreateHDRPMaterial("Assets/Ground_Material.mat", new Color(0.18f, 0.22f, 0.22f));
            var groundRenderer = groundObj.GetComponent<Renderer>();
            if (groundRenderer != null && groundMat != null)
            {
                groundRenderer.sharedMaterial = groundMat;
            }

            // 清理 Environment 底下舊版殘留的測試障礙台與階梯，保持純淨基底地面
            for (int i = env.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = env.transform.GetChild(i);
                if (child != null && child.name != "Ground")
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }

        private static void SetupParkourCourse()
        {
            GameObject courseRoot = GameObject.Find("ParkourCourse");
            if (courseRoot == null)
            {
                courseRoot = new GameObject("ParkourCourse");
                Undo.RegisterCreatedObjectUndo(courseRoot, "Create ParkourCourse");
            }

            courseRoot.transform.position = Vector3.zero;
            courseRoot.transform.rotation = Quaternion.identity;

            // 附加跑酷管理器組件
            var manager = courseRoot.GetComponent<ParkourCourseManager>();
            if (manager == null)
            {
                manager = Undo.AddComponent<ParkourCourseManager>(courseRoot);
            }
            manager.fallThresholdY = -3.5f;

            // 清除已廢棄的舊關卡物件 (徹底刪除極限大跳台)
            DestroyIfExists(courseRoot.transform, "Stage4_Landing_Pad");
            DestroyIfExists(courseRoot.transform, "Checkpoint_4_LongJump");
            DestroyIfExists(courseRoot.transform, "Stage3_Launch_Hub");

            // 建立 HDRP 關卡專屬主題材質
            Material matStart = GetOrCreateHDRPMaterial("Assets/Mat_Parkour_Start.mat", new Color(0.12f, 0.65f, 0.42f));   // 翡翠綠 (起點)
            Material matStep = GetOrCreateHDRPMaterial("Assets/Mat_Parkour_Step.mat", new Color(0.20f, 0.55f, 0.85f));       // 天藍 (階梯)
            Material matPillar = GetOrCreateHDRPMaterial("Assets/Mat_Parkour_Pillar.mat", new Color(0.28f, 0.38f, 0.80f)); // 靛青 (浮石)
            Material matBeam = GetOrCreateHDRPMaterial("Assets/Mat_Parkour_Beam.mat", new Color(0.32f, 0.35f, 0.40f));       // 鋼鐵灰 (獨木橋)
            Material matHazard = GetOrCreateHDRPMaterial("Assets/Mat_Parkour_Hazard.mat", new Color(0.95f, 0.45f, 0.10f));   // 警示橘 (旋轉跳)
            Material matSpin = GetOrCreateHDRPMaterial("Assets/Mat_Parkour_Spin.mat", new Color(0.95f, 0.30f, 0.20f));       // 烈焰紅 (自轉跳台)
            Material matTower = GetOrCreateHDRPMaterial("Assets/Mat_Parkour_Tower.mat", new Color(0.50f, 0.25f, 0.70f));    // 紫晶 (攀登塔)
            Material matRamp = GetOrCreateHDRPMaterial("Assets/Mat_Parkour_Ramp.mat", new Color(0.85f, 0.20f, 0.25f));     // 緋紅 (跳台)
            Material matGoal = GetOrCreateHDRPMaterial("Assets/Mat_Parkour_Goal.mat", new Color(0.95f, 0.80f, 0.15f));      // 榮耀金 (終點神殿)

            // ==========================================
            // 起點區域 (Start Area)
            // ==========================================
            CreateBlock(courseRoot.transform, "Start_Platform", new Vector3(0, -0.25f, 0), new Vector3(8f, 0.5f, 8f), matStart);
            CreateCheckpointTrigger(courseRoot.transform, "Checkpoint_0_Start", new Vector3(0, 0.5f, 0), new Vector3(7f, 2f, 7f), 0, "起點平台 (Start)", false);

            // ==========================================
            // 第 1 階段：基礎進階階梯 (Warmup Elevation Steps)
            // ==========================================
            CreateBlock(courseRoot.transform, "Stage1_Step_1", new Vector3(0, 0.25f, 6f), new Vector3(3f, 0.5f, 2f), matStep);
            CreateBlock(courseRoot.transform, "Stage1_Step_2", new Vector3(0, 0.75f, 9.5f), new Vector3(3f, 0.7f, 2f), matStep);
            CreateBlock(courseRoot.transform, "Stage1_Step_3", new Vector3(0, 1.35f, 13f), new Vector3(3f, 0.9f, 2f), matStep);
            CreateBlock(courseRoot.transform, "Stage1_Rest_Platform", new Vector3(0, 1.75f, 17.5f), new Vector3(4.5f, 0.5f, 4.5f), matStep);
            CreateCheckpointTrigger(courseRoot.transform, "Checkpoint_1_Steps", new Vector3(0, 2.3f, 17.5f), new Vector3(4f, 2f, 4f), 1, "第 1 區：階梯中繼站", false);

            // ==========================================
            // 第 2 階段：懸空浮石跳躍群 (Floating Stepping Stones)
            // ==========================================
            CreateBlock(courseRoot.transform, "Stage2_Pillar_1", new Vector3(2.2f, 2.1f, 22f), new Vector3(1.6f, 0.4f, 1.6f), matPillar);
            CreateBlock(courseRoot.transform, "Stage2_Pillar_2", new Vector3(-1.8f, 2.5f, 25.5f), new Vector3(1.5f, 0.4f, 1.5f), matPillar);
            CreateBlock(courseRoot.transform, "Stage2_Pillar_3", new Vector3(2.0f, 2.9f, 29f), new Vector3(1.5f, 0.4f, 1.5f), matPillar);
            CreateBlock(courseRoot.transform, "Stage2_Pillar_4", new Vector3(0f, 3.3f, 32.5f), new Vector3(1.8f, 0.4f, 1.8f), matPillar);
            CreateBlock(courseRoot.transform, "Stage2_Rest_Platform", new Vector3(0f, 3.55f, 37f), new Vector3(5f, 0.5f, 4.5f), matPillar);
            CreateCheckpointTrigger(courseRoot.transform, "Checkpoint_2_Pillars", new Vector3(0f, 4.1f, 37f), new Vector3(4.5f, 2f, 4f), 2, "第 2 區：浮石群島", false);

            // ==========================================
            // 第 3 階段：高空獨木橋與折角平衡木 (4米高空 Catwalk，平整化無顛簸)
            // ==========================================
            CreateBlock(courseRoot.transform, "Stage3_Beam_1", new Vector3(0f, 3.85f, 42.5f), new Vector3(1.0f, 0.3f, 7.0f), matBeam);
            CreateBlock(courseRoot.transform, "Stage3_Turn_Hub", new Vector3(0f, 3.80f, 47.5f), new Vector3(2.6f, 0.4f, 2.6f), matBeam);
            CreateBlock(courseRoot.transform, "Stage3_Beam_2", new Vector3(-4.5f, 3.85f, 47.5f), new Vector3(6.5f, 0.3f, 1.0f), matBeam);
            CreateBlock(courseRoot.transform, "Stage3_End_Hub", new Vector3(-8.5f, 3.80f, 47.5f), new Vector3(2.6f, 0.4f, 2.6f), matBeam);
            CreateCheckpointTrigger(courseRoot.transform, "Checkpoint_3_Beams", new Vector3(-8.5f, 4.35f, 47.5f), new Vector3(2.4f, 0.6f, 2.4f), 3, "第 3 區：高空獨木橋", false);

            // ==========================================
            // 第 4 階段：螺旋旋轉跳躍群 (Spiral & Rotating Jump Path，無頂頭阻礙)
            // ==========================================
            CreateBlock(courseRoot.transform, "Stage4_Spin_1", new Vector3(-9.2f, 4.10f, 51.5f), new Vector3(1.8f, 0.35f, 1.8f), matHazard, Quaternion.Euler(0, 15f, 0));
            CreateRotatingBlock(courseRoot.transform, "Stage4_Spin_2", new Vector3(-11.8f, 4.50f, 55.0f), new Vector3(2.2f, 0.35f, 2.2f), matSpin, 24f);
            CreateBlock(courseRoot.transform, "Stage4_Spin_3", new Vector3(-13.5f, 4.95f, 59.0f), new Vector3(1.8f, 0.35f, 1.8f), matHazard, Quaternion.Euler(0, 55f, 0));
            CreateRotatingBlock(courseRoot.transform, "Stage4_Spin_4", new Vector3(-12.0f, 5.40f, 63.0f), new Vector3(2.8f, 0.35f, 1.2f), matSpin, -20f);
            CreateBlock(courseRoot.transform, "Stage4_Spin_5", new Vector3(-8.8f, 5.85f, 66.0f), new Vector3(2.0f, 0.35f, 2.0f), matHazard, Quaternion.Euler(0, 110f, 0));
            CreateBlock(courseRoot.transform, "Stage4_Spin_Hub", new Vector3(-5.5f, 6.20f, 67.0f), new Vector3(3.2f, 0.4f, 3.2f), matHazard);
            CreateCheckpointTrigger(courseRoot.transform, "Checkpoint_4_Spiral", new Vector3(-5.5f, 6.75f, 67.0f), new Vector3(2.6f, 0.6f, 2.6f), 4, "第 4 區：螺旋旋轉跳", false);

            // ==========================================
            // 第 5 階段：螺旋垂直攀升瞭望塔 (Spiral Ascending Tower，階梯無上方遮蔽)
            // ==========================================
            CreateBlock(courseRoot.transform, "Stage5_Tower_Core", new Vector3(-9.5f, 7.5f, 73.0f), new Vector3(3f, 12f, 3f), matTower);
            CreateBlock(courseRoot.transform, "Stage5_Spiral_1", new Vector3(-6.5f, 6.70f, 70.5f), new Vector3(2.2f, 0.4f, 2.2f), matTower);
            CreateBlock(courseRoot.transform, "Stage5_Spiral_2", new Vector3(-6.5f, 7.40f, 75.0f), new Vector3(2.2f, 0.4f, 2.2f), matTower);
            CreateBlock(courseRoot.transform, "Stage5_Spiral_3", new Vector3(-9.5f, 8.10f, 76.8f), new Vector3(2.2f, 0.4f, 2.2f), matTower);
            CreateBlock(courseRoot.transform, "Stage5_Spiral_4", new Vector3(-12.5f, 8.80f, 75.0f), new Vector3(2.2f, 0.4f, 2.2f), matTower);
            CreateBlock(courseRoot.transform, "Stage5_Spiral_5", new Vector3(-12.5f, 9.50f, 70.5f), new Vector3(2.2f, 0.4f, 2.2f), matTower);
            CreateBlock(courseRoot.transform, "Stage5_Tower_Deck", new Vector3(-9.5f, 10.0f, 73.0f), new Vector3(4.5f, 0.5f, 4.5f), matTower);
            CreateCheckpointTrigger(courseRoot.transform, "Checkpoint_5_Tower", new Vector3(-9.5f, 10.60f, 73.0f), new Vector3(3.8f, 0.6f, 3.8f), 5, "第 5 區：高塔之巔 (10m)", false);

            // ==========================================
            // 第 6 階段：信仰之躍與終點神殿 (Leap of Faith to Victory Island)
            // ==========================================
            CreateBlock(courseRoot.transform, "Stage6_Diving_Runway", new Vector3(-9.5f, 10.15f, 78.5f), new Vector3(2.2f, 0.3f, 6.0f), matRamp);
            CreateBlock(courseRoot.transform, "Stage6_Victory_Island", new Vector3(-9.5f, 1.5f, 94.0f), new Vector3(10f, 0.8f, 10f), matGoal);
            // 終點神殿拱門
            CreateBlock(courseRoot.transform, "Stage6_Pillar_Left", new Vector3(-12.5f, 4.2f, 96.0f), new Vector3(1f, 4.5f, 1f), matGoal);
            CreateBlock(courseRoot.transform, "Stage6_Pillar_Right", new Vector3(-6.5f, 4.2f, 96.0f), new Vector3(1f, 4.5f, 1f), matGoal);
            CreateBlock(courseRoot.transform, "Stage6_Arch_Top", new Vector3(-9.5f, 6.7f, 96.0f), new Vector3(7f, 0.6f, 1.2f), matGoal);
            CreateBlock(courseRoot.transform, "Stage6_Victory_Pedestal", new Vector3(-9.5f, 2.5f, 96.0f), new Vector3(1.8f, 1.2f, 1.8f), matGoal);
            // 終點判定 (isGoal = true)
            CreateCheckpointTrigger(courseRoot.transform, "Goal_Trigger", new Vector3(-9.5f, 3.0f, 95.0f), new Vector3(7f, 3.5f, 7f), 6, "🏆 終點神殿 (Goal)", true);

            manager.RefreshCheckpoints();
            Debug.Log("[GameSetup] 跑酷關卡全部 6 大區域已建構完成 (第 4 區為螺旋旋轉跳)！");
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, Quaternion rot = default)
        {
            Transform existing = parent.Find(name);
            GameObject block;
            if (existing == null)
            {
                block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = name;
                block.transform.SetParent(parent);
                Undo.RegisterCreatedObjectUndo(block, "Create " + name);
            }
            else
            {
                block = existing.gameObject;
            }

            block.transform.position = pos;
            block.transform.rotation = rot == default ? Quaternion.identity : rot;
            block.transform.localScale = scale;

            var r = block.GetComponent<Renderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
            }

            return block;
        }

        private static GameObject CreateRotatingBlock(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, float spinSpeed)
        {
            GameObject block = CreateBlock(parent, name, pos, scale, mat);
            var spinner = block.GetComponent<RotatingPlatform>();
            if (spinner == null)
            {
                spinner = Undo.AddComponent<RotatingPlatform>(block);
            }
            spinner.rotationSpeed = spinSpeed;
            return block;
        }

        private static void DestroyIfExists(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null)
            {
                Undo.DestroyObjectImmediate(t.gameObject);
            }
        }

        private static GameObject CreateCheckpointTrigger(Transform parent, string name, Vector3 pos, Vector3 size, int index, string cpName, bool isGoal)
        {
            Transform existing = parent.Find(name);
            GameObject triggerObj;
            if (existing == null)
            {
                triggerObj = new GameObject(name);
                triggerObj.transform.SetParent(parent);
                Undo.RegisterCreatedObjectUndo(triggerObj, "Create " + name);
            }
            else
            {
                triggerObj = existing.gameObject;
            }

            triggerObj.transform.position = pos;
            triggerObj.transform.rotation = Quaternion.identity;
            triggerObj.transform.localScale = Vector3.one;

            // 確保觸發器物件無多餘的 MeshRenderer / MeshFilter / MeshCollider 造成實體頂頭碰撞
            var meshFilter = triggerObj.GetComponent<MeshFilter>();
            if (meshFilter != null) Undo.DestroyObjectImmediate(meshFilter);
            var meshRenderer = triggerObj.GetComponent<MeshRenderer>();
            if (meshRenderer != null) Undo.DestroyObjectImmediate(meshRenderer);
            var meshCollider = triggerObj.GetComponent<MeshCollider>();
            if (meshCollider != null) Undo.DestroyObjectImmediate(meshCollider);

            var col = triggerObj.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = Undo.AddComponent<BoxCollider>(triggerObj);
            }
            col.isTrigger = true;
            col.size = size;

            var cp = triggerObj.GetComponent<ParkourCheckpoint>();
            if (cp == null)
            {
                cp = Undo.AddComponent<ParkourCheckpoint>(triggerObj);
            }
            cp.checkpointIndex = index;
            cp.checkpointName = cpName;
            cp.isGoal = isGoal;
            cp.respawnOffset = new Vector3(0, 0.2f, 0);

            return triggerObj;
        }

        private static Material GetOrCreateHDRPMaterial(string path, Color baseColor)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("HDRP/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            }
            return mat;
        }

        public static string GetPlayerModelPath()
        {
            if (File.Exists(TargetCharacterFbx))
            {
                return TargetCharacterFbx;
            }

            string found = FindFbxAsset("Running") ?? FindFbxAsset("Meshy_AI_T_Pose_Boy_Running");
            if (!string.IsNullOrEmpty(found))
            {
                return found;
            }

            return FindFbxAsset("Meshy_AI_T_Pose_Boy");
        }

        private static GameObject SetupPlayer()
        {
            string modelPath = GetPlayerModelPath();
            GameObject modelPrefab = !string.IsNullOrEmpty(modelPath) ? AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) : null;

            if (modelPrefab == null)
            {
                Debug.LogError($"[GameSetup] 未能載入指定的操作角色模型 FBX: {modelPath}");
            }

            // 尋找場景中的現有 Player
            PlayerController existingController = Object.FindFirstObjectByType<PlayerController>();
            GameObject playerObj = existingController != null ? existingController.gameObject : GameObject.Find("Player");

            Vector3 spawnPosition = new Vector3(0, 0.5f, 0);
            Quaternion spawnRotation = Quaternion.identity;

            // 檢查現有角色是否已為 Meshy_AI_T_Pose_Boy_Running .fbx
            if (playerObj != null)
            {
                GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(playerObj);
                if (modelPrefab != null && sourcePrefab != modelPrefab)
                {
                    Debug.Log($"[GameSetup] 現有場景角色模型非 Meshy_AI_T_Pose_Boy_Running .fbx，替換為指定模型...");
                    spawnPosition = playerObj.transform.position;
                    spawnRotation = playerObj.transform.rotation;
                    Undo.DestroyObjectImmediate(playerObj);
                    playerObj = null;
                }
            }

            if (playerObj == null)
            {
                if (modelPrefab != null)
                {
                    playerObj = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
                    playerObj.name = "Player";
                    playerObj.transform.position = spawnPosition;
                    playerObj.transform.rotation = spawnRotation;
                    Undo.RegisterCreatedObjectUndo(playerObj, "Instantiate Player Model (Meshy_AI_T_Pose_Boy_Running)");
                }
                else
                {
                    Debug.LogWarning("[GameSetup] 未找到角色模型 FBX，建立基本膠囊體作為替代。");
                    playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    playerObj.name = "Player";
                    playerObj.transform.position = spawnPosition;
                    Undo.RegisterCreatedObjectUndo(playerObj, "Create Player Capsule");
                }
            }

            // 確保設定 Tag 為 Player
            try
            {
                playerObj.tag = "Player";
            }
            catch
            {
            }

            // 計算模型尺寸並調整 CharacterController
            Renderer[] renderers = playerObj.GetComponentsInChildren<Renderer>();
            float characterHeight = 1.8f;
            float characterRadius = 0.35f;
            Vector3 characterCenter = new Vector3(0, 0.9f, 0);

            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                if (bounds.size.y > 0.5f && bounds.size.y < 5f)
                {
                    characterHeight = bounds.size.y;
                    characterCenter = new Vector3(0, characterHeight / 2f, 0);
                    characterRadius = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z) / 2f, 0.25f, 0.5f);
                }
            }

            // 加上 CharacterController
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = Undo.AddComponent<CharacterController>(playerObj);
            }
            cc.height = characterHeight;
            cc.radius = characterRadius;
            cc.center = characterCenter;
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.4f;
            cc.minMoveDistance = 0f;

            // 加上 PlayerController
            PlayerController pc = playerObj.GetComponent<PlayerController>();
            if (pc == null)
            {
                pc = Undo.AddComponent<PlayerController>(playerObj);
            }
            pc.walkSpeed = 3.5f;
            pc.sprintSpeed = 7.0f;
            pc.jumpHeight = 1.6f;
            pc.gravity = -20f;
            pc.isFirstPerson = true;

            // 加上 HUD 提示組件
            CharacterHUD hud = playerObj.GetComponent<CharacterHUD>();
            if (hud == null)
            {
                hud = Undo.AddComponent<CharacterHUD>(playerObj);
            }

            return playerObj;
        }

        private static void SetupAnimations(GameObject playerObj)
        {
            string runningFbxPath = GetPlayerModelPath();
            if (string.IsNullOrEmpty(runningFbxPath))
            {
                Debug.LogWarning("[GameSetup] 未找到帶有 'Running' 的動畫 FBX，跳過跑步動畫設定。");
                return;
            }

            EnsureAnimationLoops(runningFbxPath);

            AnimationClip runClip = AssetDatabase.LoadAllAssetsAtPath(runningFbxPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

            if (runClip == null)
            {
                Debug.LogWarning("[GameSetup] FBX 中未找到 AnimationClip。");
                return;
            }

            string controllerPath = "Assets/PlayerAnimatorController.controller";
            AnimatorController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            if (animatorController == null)
            {
                animatorController = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            EnsureParameter(animatorController, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(animatorController, "IsMoving", AnimatorControllerParameterType.Bool);
            EnsureParameter(animatorController, "IsGrounded", AnimatorControllerParameterType.Bool);
            EnsureParameter(animatorController, "Jump", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = animatorController.layers[0].stateMachine;

            AnimatorState idleState = rootStateMachine.states.FirstOrDefault(s => s.state.name == "Idle").state;
            if (idleState == null)
            {
                idleState = rootStateMachine.AddState("Idle");
            }

            AnimatorState runState = rootStateMachine.states.FirstOrDefault(s => s.state.name == "Run").state;
            if (runState == null)
            {
                runState = rootStateMachine.AddState("Run");
            }

            runState.motion = runClip;
            rootStateMachine.defaultState = idleState;

            if (!HasTransition(idleState, runState))
            {
                var trans = idleState.AddTransition(runState);
                trans.hasExitTime = false;
                trans.duration = 0.15f;
                trans.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            }

            if (!HasTransition(runState, idleState))
            {
                var trans = runState.AddTransition(idleState);
                trans.hasExitTime = false;
                trans.duration = 0.15f;
                trans.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            }

            EditorUtility.SetDirty(animatorController);
            AssetDatabase.SaveAssets();

            Animator animator = playerObj.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = Undo.AddComponent<Animator>(playerObj);
            }
            animator.runtimeAnimatorController = animatorController;
            EditorUtility.SetDirty(animator);

            Debug.Log("[GameSetup] 動畫控制器設定完成，操作角色 Meshy_AI_T_Pose_Boy_Running 已連結跑步循環動畫！");
        }

        private static void SetupCamera(GameObject playerObj)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindFirstObjectByType<Camera>();
            }

            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
                Undo.RegisterCreatedObjectUndo(camObj, "Create Main Camera");
            }

            ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
            if (tpc == null)
            {
                tpc = Undo.AddComponent<ThirdPersonCamera>(cam.gameObject);
            }

            tpc.target = playerObj.transform;
            tpc.isFirstPerson = true;
            tpc.firstPersonEyeHeight = 1.62f;
            tpc.firstPersonForwardOffset = 0.22f;
            tpc.targetOffset = new Vector3(0, 1.4f, 0);
            tpc.distance = 4.2f;

            cam.nearClipPlane = 0.01f;
            cam.transform.position = playerObj.transform.position + new Vector3(0, 1.62f, 0.22f);
            cam.transform.rotation = playerObj.transform.rotation;

            PlayerController pc = playerObj.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.cameraTransform = cam.transform;
                pc.isFirstPerson = true;
            }

            EditorUtility.SetDirty(cam);
        }

        private static void EnsureAnimationLoops(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                var defaultClips = importer.defaultClipAnimations;
                if (defaultClips != null && defaultClips.Length > 0)
                {
                    clips = new ModelImporterClipAnimation[defaultClips.Length];
                    for (int i = 0; i < defaultClips.Length; i++)
                    {
                        clips[i] = new ModelImporterClipAnimation
                        {
                            takeName = defaultClips[i].takeName,
                            name = defaultClips[i].name,
                            firstFrame = defaultClips[i].firstFrame,
                            lastFrame = defaultClips[i].lastFrame,
                            loopTime = true,
                            wrapMode = WrapMode.Loop
                        };
                    }
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                }
            }
            else
            {
                bool modified = false;
                foreach (var c in clips)
                {
                    if (!c.loopTime)
                    {
                        c.loopTime = true;
                        modified = true;
                    }
                }
                if (modified)
                {
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                }
            }
        }

        private static bool HasTransition(AnimatorState from, AnimatorState to)
        {
            return from.transitions != null && from.transitions.Any(t => t.destinationState == to);
        }

        private static void EnsureParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType type)
        {
            if (controller.parameters.All(p => p.name != paramName))
            {
                controller.AddParameter(paramName, type);
            }
        }

        private static string FindFbxAsset(string keyword)
        {
            string[] guids = AssetDatabase.FindAssets("t:Model " + keyword);
            if (guids == null || guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets(keyword);
            }

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }

            return null;
        }
    }
}
