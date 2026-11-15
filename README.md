# GalaGame 🎮

A 3D First-Person & Third-Person Character Controller Game built with **Unity 6 (6000.3.19f1)** and **High Definition Render Pipeline (HDRP)**.

---

## 🌟 特色 (Features)

- **第一人稱 / 第三人稱雙視角控制器 (First & Third-Person Controller)**：
  - 操作角色：**`Meshy_AI_T_Pose_Boy_Running .fbx`**（完整骨骼綁定與跑步循環動畫）
  - **預設第一人稱視角**：相機精準錨定頭部眼位，支援自然跑步動態擺動與左右橫移（Strafe）。
  - **即時視角切換**：隨時按 `V` 鍵或透過滑鼠滾輪，在第一人稱與第三人稱之間無縫切換。
  - 跳躍 (Jump) 與重力加速度運算
  - 衝刺加速 (Sprint)
  - 串聯 Meshy AI 3D 角色模型與跑步循環動畫
- **3D 跑酷闖關挑戰系統 (Modular Parkour Course)**：
  - **獨立模組化架構**：整套跑酷關卡收納於 `ParkourCourse` 根節點，完美相容未來隨時載入的實拍 3D 地形！
  - **精心設計的 6 大挑戰階段**：
    1. 🟢 **起點與基礎進階階梯** (Warmup Steps)
    2. 🔵 **懸空浮石跳躍群** (Floating Pillars & Stepping Stones - 考驗第一人稱視角落點掌控)
    3. ⚪ **高空折角平衡木** (Narrow Catwalks - 4 米高空窄梁極限行走)
    4. 🟠 **螺旋旋轉跳躍群** (Spiral & Rotating Jump Path - 刪除舊版深淵大跳台，改為弧形升騰旋轉跳與自轉動態跳台，完全無頂頭遮蔽)
    5. 🟣 **螺旋垂直攀升瞭望塔** (Spiral Ascending Tower - 垂直旋轉登頂至 9 米高空，俯瞰壯闊全景)
    6. 🔴 **信仰之躍與終點神殿** (Leap of Faith to Victory Island - 從高塔俯衝飛躍至金色榮耀終點浮島)
  - **無縫跌落重生 (Checkpoint Respawn)**：不慎墜落自動傳送回最近檢查點，並有畫面上方橫幅提示。
  - **競速計時與一鍵重試**：HUD 即時顯示關卡計時、最佳紀錄與檢查點進度，隨時按 `R` 鍵重新挑戰。
- **高自由度相機控制器 (Perspective Camera)**：
  - 第一人稱：-85° ~ +85° 上下全方位仰望俯瞰、滑鼠水平同步轉向
  - 第三人稱：360° 滑鼠環繞、球體射線防穿牆防穿地（SphereCast Collision Detection）
  - 滑鼠滾輪即時縮放鏡頭距離
  - ESC / 滑鼠點擊游標鎖定與釋放
- **即時 HUD 狀態與操作指南 (On-Screen HUD)**：
  - 顯示當前視角模式（第一人稱 / 第三人稱）、速度 (m/s) 與角色狀態（靜止/移動/衝刺/跳躍中）
  - 顯示跑酷計時、檢查點進度與通關橫幅
  - 隨時按 `H` 鍵切換顯示/隱藏
- **一鍵場景與角色配置精靈 (Editor Tool)**：
  - Unity 頂部選單 `Tools > 🎮 遊戲配置 > 一鍵配置人物與跑酷關卡`，自動建立地面、6 大跑酷階段、設定相機與動畫控制器，並指定操作角色為 `Meshy_AI_T_Pose_Boy_Running .fbx`（預設第一人稱）。

---

## 🕹️ 操作方式 (Controls)

| 操作項目 | 鍵盤 / 滑鼠 | 遊戲手把 (Gamepad) |
| :--- | :--- | :--- |
| **前後移動 / 左右橫移** | `W` / `A` / `S` / `D` | 左類比搖桿 (Left Stick) |
| **轉向與俯仰視角** | `滑鼠移動` | 右類比搖桿 (Right Stick) |
| **切換第一 / 第三人稱** | `V` 鍵 | - |
| **返回上一個存檔點** | `B` 鍵 / `Backspace` / `K` 鍵 | Select / Back 鍵 |
| **衝刺加速 (助跑大跳)** | `Left Shift` | 左搖桿下壓 (L3) / 右板機 (RT) |
| **跳躍** | `空白鍵 (Space)` | A 鍵 / ✕ 鍵 (Button South) |
| **重新開始跑酷** | `R` 鍵 | - |
| **鏡頭縮放** | `滑鼠滾輪` | - |
| **解鎖/鎖定游標**| `ESC` / `滑鼠左鍵` | - |
| **開關說明 HUD** | `H` 鍵 | - |

---

## 🗺️ 實拍 3D 地形整合說明 (For Photogrammetry Terrain)

本跑酷關卡採用**完全模組化設計**，專為後續實拍生成 3D 地形打造：
1. **獨立父節點**：所有跑酷平台、檢查點與終點皆組織在場景中的 `ParkourCourse` 物件下。
2. **輕鬆貼齊與調整**：未來將實拍生成的 3D 地形模型（.obj / .fbx / .glb 等）拖入場景後，只需為地形加上 `MeshCollider`，並移動/旋轉 `ParkourCourse` 根節點，即可將整套跑酷路徑無縫架設在實拍山脈、峽谷或城市廢墟地形之上！

---

## 🛠️ 專案技術規格 (Tech Stack)

* **Engine**: Unity 6 (6000.3.19f1)
* **Render Pipeline**: High Definition Render Pipeline (HDRP) 17.3.0
* **Input**: Unity New Input System Package (1.19.0)
* **Target Platforms**: PC / Standalone Windows
