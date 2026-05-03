# 🪿 VR 鵝頭偷麵包 — 遊戲設計 + 開發規劃文件

> **版本：** v2.1　**最後更新：** 2026-05-03　**引擎：** Unity 6 (URP) + Meta all in one Plugin (Meta SDK)

---

## 核心概念與背景

**Slogan（宣傳標語）：** 通過各種關卡 成為合格的鵝媽媽吧!

**背景故事：** 小鵝喜歡吃麵包；玩家扮演的鵝要去**偷麵包**，並在後續關卡中把**游來游去的小鵝**抓回巢裡。

**操作核心：** 玩家用手模擬鵝頭，控制嘴部開合與頭部轉向，伸長脖子取物（麵包 / 必要時含小鵝）帶回巢中。

**移動類型（舒適度 / 商店標示）：** 本作為 **Comfort / 有限制移動（Comfortable／limited locomotion）** — 玩家在場景中以 **右手「手槍手勢」** 驅動**水平方向**位移（程式：`GunGestureLocomotion`，經 Meta `FirstPersonLocomotor`，帶碰撞），**不是**自由房間級走動或大範圍瞬移作主軸。當脖子（頭／身距離）伸到約定門檻時會**禁止再往前推**，避免身體與頸伸過度組合加重暈眩風險（見下文 `NeckSplineController`）。

**新互動：** 除麵包外，關卡目標包含**抓小鵝回巢**（與巢的 Trigger 判定、計數邏輯需支援 `Bread` 與 `LittleGoose` 兩類可交付物）。

---

## 關卡設計總覽

| 關卡    | 名稱（工作稱呼）   | 過關要點摘要 |
| ----- | ----------- | ------- |
| **第零關** | 起始畫面 — 擺麵包開局 | 將麵包**擺入巢中**以開始遊戲；巢內可有小鵝（氛圍／教學） |
| **第一關** | 鉤子 + 高處／平台麵包 | **鉤子**取 **高處 1 塊麵包** + **平台 2 塊麵包**，共 **3 塊**回巢 |
| **第二關** | 移動平台麵包 + 雙鵝 | **2 塊麵包**置於**移動平台** + **2 隻小鵝**回巢 |
| **第三關** | 高腳桌 + 刷子推麵包 | **巢在場景中央被圍**；**2 張高腳桌**各 **1 塊麵包**，用**刷子**將麵包**推入巢** |
| **第四關** | 五隻小鵝       | **5 隻小鵝**皆回巢 |
| **第五關** | 三高腳桌 + 單鵝 | **3 張高腳桌**各 **1 塊麵包** + **1 隻小鵝**回巢 |
| **第六關** | 四移動平台四麵包 | **4 座移動平台**各 **1 塊麵包**，共 **4 塊**回巢 |
| **第七關** | 移動巢 + 三鵝   | **巢**沿軌跡**來回移動** + **3 隻小鵝**回巢 |

---

## 詳細關卡設計

### 第零關：起始畫面 — 擺麵包開局

| 項目 | 內容 |
| --- | --- |
| **場景配置** | 巢在視線可及處，巢內／旁可有**小鵝**（靜態或待機動畫）；旁邊提供 **1 塊可抓取麵包**（或明確提示物） |
| **開始條件** | 玩家將麵包**放入巢 Trigger**（與正式關卡相同判定）→ 載入第一關或進入主流程 |
| **核心機制** | 與 Nest 共用邏輯；可僅要求 `breadCount >= 1` 即切場景，不計為「排行榜關卡」 |
| **待補強** | 開場 UI、簡短文字／圖示引導「把麵包放進巢裡開始」 |

### 第一關：鉤子 + 高處 1 麵包 + 平台 2 麵包

| 項目 | 內容 |
| --- | --- |
| **場景配置** | **1 塊麵包**在**較高位置**（需鉤子勾取或拉近）；**平台上共 2 塊麵包** |
| **過關條件** | 巢中累計 **3 塊麵包**（順序不限） |
| **失敗條件** | 麵包掉入水中 → Fail UI → 重置（與現行設計一致） |
| **核心機制** | 長鉤子實體抓取 + Snap 咬麵包；`breadToWin = 3`、`gooseToWin = 0` |
| **待補強** | 鉤頭 Collider、麵包質量／阻力，避免彈飛 |

### 第二關：2 塊麵包在移動平台 + 2 隻小鵝

| 項目 | 內容 |
| --- | --- |
| **場景配置** | **2 塊麵包**分別（或共同）置於**移動平台**（可用 1～2 座平台承載，企劃以「平台上共 2 麵包」為準）；**2 隻小鵝**於**水面**或其它活動區（見「小鵝 AI」） |
| **過關條件** | **2 麵包 + 2 小鵝**皆曾進入巢並計入（順序不限） |
| **失敗條件** | 麵包落水依現行規則；小鵝落水建議不直接 Fail 或重生 |
| **核心機制** | `MovingPlatform`；Nest 分項計數 `breadDelivered` / `gooseDelivered` |

### 第三關：巢在中央 — 2 高腳桌 2 麵包 — 刷子推入巢

| 項目 | 內容 |
| --- | --- |
| **場景配置** | **巢**置於場景**中央**，周邊以障礙／低矮圍欄形成「須從外圍操作」的動線；**2 張高腳桌**，各放 **1 塊麵包**；提供可抓取的**刷子**（或類似長柄推物道具），用於將桌面麵包**推落／推滑**至巢的 Trigger 範圍 |
| **過關條件** | **2 塊麵包**皆進巢並計入（不要求一定要用刷子，但關卡以刷子解法為主軸） |
| **核心機制** | 刷子為獨立 `Grabbable`／物理推擠與麵包碰撞；高腳桌高度與巢周圍碰撞需調校，避免麵包卡死 |
| **待補強** | 刷子 Prefab、麵包—桌面摩擦與防穿透 |

### 第四關：5 隻小鵝

| 項目 | 內容 |
| --- | --- |
| **場景配置** | **5 隻小鵝**於水面（或其它安全活動區）游動 |
| **過關條件** | **5 隻小鵝**皆回巢（各只計一次）；`gooseToWin = 5`、`breadToWin = 0` |
| **核心機制** | 同「小鵝 AI」與 Nest `LittleGoose` 去重計數 |

### 第五關：3 高腳桌 3 麵包 + 1 隻小鵝

| 項目 | 內容 |
| --- | --- |
| **場景配置** | **3 張高腳桌**各 **1 塊麵包**；**1 隻小鵝**活動（水面或其它） |
| **過關條件** | **3 麵包 + 1 小鵝**回巢 |
| **核心機制** | 可與第三關共用高腳桌／抓取咬麵包流程；若桌面過高可選是否保留刷子或改為鉤子輔助（企劃定稿） |

### 第六關：4 移動平台 4 麵包

| 項目 | 內容 |
| --- | --- |
| **場景配置** | **4 座移動平台**，各承載 **1 塊麵包**（軌跡可平行或錯相為增加難度） |
| **過關條件** | **4 塊麵包**回巢；`breadToWin = 4` |
| **核心機制** | `MovingPlatform` 多實例；玩家預判平台位置咬取或鉤取 |

### 第七關：來回移動巢 + 3 隻小鵝

| 項目 | 內容 |
| --- | --- |
| **場景配置** | **巢**（含 Trigger）沿指定路徑**來回移動**（軌道／`MovingPlatform` 綁巢 Parent，或專用 `MovingNest`）；**3 隻小鵝**於場內活動 |
| **過關條件** | **3 隻小鵝**皆在巢移動過程中成功送入並計入 |
| **核心機制** | Nest 位移時需與交付判定一致（Trigger 隨巢移動）；小鵝 AI 與玩家走位需預留「對準移動巢」的時間窗 |
| **待補強** | 巢移動速度／範圍舒適度評估（避免與頸伸／槍手勢位移疊加眩暈） |

### 結局（建議保留）

全關完成後進入簡短結局演出（例如白色空間、麵包／小鵝慶祝），細節可沿用舊版或簡化。

---

## 巡邏功能（App 後續更新）

**決策：** 舊版主線中的**巡邏員、Cone 光束、視線躲避**不納入當前主線關卡；**保留為之後版本**的擴充內容。

文件中下列程式片段（`Guard.cs`、偵測 Layer）仍可作為將來實作參考，無需從專案刪除，但 **LevelConfig** 預設關閉 `hasPatrolGuard`。

---

## 小鵝：動畫與 AI（程式規格）

### 小鵝動畫

- 待機、游水（循環）、被叼起／被抓（可選）、入巢短動畫（可選）。
- 與 `Animator` 狀態機銜接：依是否在地面、是否在水中、是否被持有切換。

### 小鵝 AI 行為

| 行為 | 說明 |
| --- | --- |
| **游動** | 在水面範圍內隨機或簡單轉向巡航；避免卡邊；與 `Rigidbody`／浮力或設定高度對齊水面 |
| **自動扶正** | 若傾倒，以插值或短動畫將「向上」對齊世界 Up（或對齊地面法線），避免鵝腹朝上長時間停留 |
| **頭部 LookAt** | **小鵝的臉（頭部）**在**玩家鵝頭**進入某距離內時，對鵝頭方向做 **LookAt**（可限制水平角／俯仰角以免過度扭曲）；超出距離則漸回中立或繼續游動朝向 |

**實作提示：** 以 `Transform` 參考「玩家鵝頭／嘴錨點」與小鵝「頭骨節點」；距離用 `Vector3.Distance`；LookAt 用 `Quaternion.Slerp` 平滑。

### 與 Nest / 抓取整合

- 小鵝需可被「嘴部 Snap」或手抓規則與麵包一致（`HandGrabInteractable` + 類似 `SnapToMouth` 或專用 `GooseSnapToMouth`）。
- 進入巢 Trigger 時：`LittleGoose` 計入 `gooseDelivered`，並播放堆疊／安撫動畫（與麵包分開 `HashSet` 去重）。

---

## 開發時程（建議，可依人力調整）

| 階段 | 程式 | 美術 |
| --- | --- | --- |
| **W1** | VR 基礎、Nest 擴充（麵包 + 小鵝計數）、第零關開局流程 | 小鵝模型／綁定、基礎動畫 |
| **W2** | 小鵝 AI（游動、扶正、LookAt）、第一關鉤子 + 三麵包 | 場景 blockout、麵包與巢 |
| **W3** | 第二關移動平台麵包、第三關刷子／高腳桌原型 | 水面、小鵝材質 polish |
| **W4** | 第四～五關（大量小鵝／三高腳桌）、落水與重置 | 高腳桌與刷子美術 |
| **W5** | 第六～七關（多移動平台、移動巢）、全關串接、Bug 修 | 結局與 UI |

**里程碑建議：**

- Nest 支援雙類型交付物 + 第零關可玩 → 第一關可玩 → 第二～三關可玩 → 第四～五關 → 第六～七關 + 版本凍結。

---

## 概述：程式任務（Unity / C#）

- **VR 基礎** — XR Origin、Meta SDK、手部追蹤、`HandGrabInteractor`。
- **有限制移動** — `GunGestureLocomotion`（掛於 Camera Rig）：右手槍手勢驅動 `FirstPersonLocomotor` **水平位移**；`NeckSplineController.AllowsGunGestureLocomotion()` 在頭身距離 ≥ `maxNeckLength + locomotionBeyondMaxNeck` 時**鎖定槍手勢前移**。
- **Snap Grabbing** — 麵包與小鵝 Prefab 掛 `Grabbable` + `HandGrabInteractable`；嘴部錨點吸附。
- **Nest** — 擴充為：`requiredBread`、`requiredGoose`（或 ScriptableObject 關卡表）；`OnTriggerEnter` 分辨 `Bread` / `LittleGoose`；過關條件兩者皆達標。
- **第零關** — 單一麵包入巢 → `LoadScene("Level1")` 或 `GameManager` 狀態切換。
- **移動平台** — 沿用 `MovingPlatform`（Sine）；**第二、六關**等多場景啟用。
- **移動巢** — 第七關巢本體隨軌跡位移（Trigger 隨 Parent），可獨立 `MovingNest` 或共用平台腳本。
- **刷子推麵包** — 第三關（第五關可選）：可抓取道具對麵包施力／碰撞推出檯面落入巢。
- **小鵝** — `LittleGooseAI.cs`（游動、扶正、LookAt）、`Animator` 參數驅動。
- **關卡管理** — `totalLevels` 對應 **第零關～第七關**（索引 **0～7**）；結局場景可選。

---

## 程式設計建議（沿用與調整）

### 有限制移動：`GunGestureLocomotion` 與脖子伸長門檻（`NeckSplineController`）

**定位：** Meta Quest／App Lab 等平台可將本作標為 **Comfortable／limited locomotion**；並在說明文宣中註明「手勢按住移動」，供玩家評估是否在安全空間／座椅遊玩。

**觸發手勢（右手）：** **槍手勢** — 食指伸直（Pinch 低、`indexExtendedThreshold`）、食指指尖離手腕距離大於 `indexExtendedMinDist`（排除「比讚」誤判）、中指／無名指／小指「握拳」（指尖到手腕距離小於 `fingerCurledMaxDist`）。連續 `activationFrames` 幀達成才啟動，避免瞬間誤觸。**方向：** `HandIndex1` → `HandIndexTip` 世界空間向量。

**位移實作：** 取用子物件上 **Meta Interaction SDK `FirstPersonLocomotor`**：`HandleLocomotionEvent` 使用 `LocomotionEvent.TranslationType.Relative`，**帶 PhysX 碰撞**；若無則退化為直接累加 `transform.position`。預設 `horizontalOnly = true`：**只沿地面水平面**，忽略食指俯仰。**速度／手感：** `moveSpeed`、`accelerationTime`、`decelerationTime`；手鬆開或進入鎖定時會減速至停。**除錯：** `debugBlockedByNeck`、`debugForceGunGesture`（Editor：`P` 鍵）。

**脖子伸長與鎖定位移：** `GunGestureLocomotion` 可指到場景中的 **`NeckSplineController`**（未指派時於 `Awake` 自動 `FindFirstObjectByType`）。**禁止槍手勢移動的門檻**由 `NeckSplineController.AllowsGunGestureLocomotion()` 決定：目前頭／身距離 `CurrentNeckLength()` **小於** `maxNeckLength + locomotionBeyondMaxNeck` 才可移動；**達或大於門檻**時不推進並走減速邏輯（`locomotionBeyondMaxNeck = 0` 表示與 `maxNeckLength` 對齊即鎖）。**注意：** 超過 `maxNeckLength` 另有 `pullForce` 物理拉力拉身體；與「移動鎖定」門檻分開，`locomotionBeyondMaxNeck` 可在達 max 後再容忍一段長度才把位移關閉。

**場景掛載（摘要）：** 腳本掛 **`[BuildingBlock] Camera Rig`**；`hand` → `OVRInteractionComprehensive`／`OVRHands`／`RightHand` 上的 **`Hand`** 組件。

### 1. Snap Grabbing（麵包）

**問題：** VR 裡精準咬到小麵包困難，缺乏觸覺回饋。

**解決方案：** Meta SDK **Hand Grab Interaction** + 薄層監聽；小鵝可複用相同抓取鏈，另掛 `LittleGoose` 與 AI 腳本。

#### 自訂腳本：吸附到嘴部（Snap to Mouth）

```csharp
// BreadSnapToMouth.cs — 掛在每個麵包 Prefab 上
using Oculus.Interaction;
using UnityEngine;

public class BreadSnapToMouth : MonoBehaviour
{
    [Header("Snap Target")]
    public Transform mouthAnchor;

    private HandGrabInteractable _interactable;
    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _interactable = GetComponent<HandGrabInteractable>();
    }

    void OnEnable()
    {
        _interactable.WhenSelectingInteractorViewAdded += OnGrabbed;
        _interactable.WhenSelectingInteractorViewRemoved += OnReleased;
    }

    void OnDisable()
    {
        _interactable.WhenSelectingInteractorViewAdded -= OnGrabbed;
        _interactable.WhenSelectingInteractorViewRemoved -= OnReleased;
    }

    private void OnGrabbed(IInteractorView interactor)
    {
        if (mouthAnchor == null) return;
        _rb.isKinematic = true;
        transform.SetParent(mouthAnchor);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void OnReleased(IInteractorView interactor)
    {
        if (_interactable.SelectingInteractorViews.Count > 0) return;
        transform.SetParent(null);
        _rb.isKinematic = false;
    }

    public void RandomizeAppearance()
    {
        float scale = Random.Range(0.9f, 1.1f);
        transform.localScale = Vector3.one * scale;
        transform.rotation = Random.rotation;
    }
}
```

> 巢 `OnTriggerEnter` 需能解除 Parent（如 `bread.Detach()`），與現有 `Nest.cs` 設計對齊。

### 2. 巢判定系統（擴充）

**重點：** 在現有 `Nest.cs` 思維上增加：

- `int _breadCount` / `int _gooseCount`（或單一結構）。
- `HashSet<Bread>`、`HashSet<LittleGoose>` 避免重複計數。
- `requiredBread`、`requiredGoose` 由 `LevelConfig` 填入。
- **過關：** `_breadCount >= requiredBread && _gooseCount >= requiredGoose`。

第零關可設 `requiredBread = 1`、`requiredGoose = 0`。

### 3. 鉤子系統（第一關）

與舊版第二關相同：**實體長鉤子** + 物理帶動麵包，無需額外勾子腳本。

### 4. 移動平台（第二、六關等）

```csharp
// MovingPlatform.cs
public class MovingPlatform : MonoBehaviour
{
    public float speed = 1.5f;
    public float range = 2.0f;

    private Vector3 origin;

    void Start() => origin = transform.position;

    void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * range;
        transform.position = origin + Vector3.right * offset;
    }
}
```

### 5. 巡邏員 Cone 光束（後續更新／參考用）

以下邏輯**不列入當前主線**，保留供 App 更新時啟用：

```csharp
// Guard.cs（參考）
public class Guard : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 1.5f;

    [Header("Detection")]
    public Transform eyeOrigin;
    public float detectionAngle = 40f;
    public float detectionRange = 5f;
    public LayerMask playerHandLayer;

    public UnityEvent onPlayerDetected;

    private int currentPoint = 0;

    void Update()
    {
        Patrol();
        CheckDetection();
    }

    void Patrol()
    {
        Transform target = patrolPoints[currentPoint];
        transform.position = Vector3.MoveTowards(
            transform.position, target.position, patrolSpeed * Time.deltaTime);

        Vector3 dir = (target.position - transform.position).normalized;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
            currentPoint = (currentPoint + 1) % patrolPoints.Length;
    }

    void CheckDetection()
    {
        Collider[] hits = Physics.OverlapSphere(
            eyeOrigin.position, detectionRange, playerHandLayer);

        foreach (Collider hit in hits)
        {
            Vector3 toTarget = hit.transform.position - eyeOrigin.position;
            float angle = Vector3.Angle(eyeOrigin.forward, toTarget);

            if (angle < detectionAngle)
            {
                if (!Physics.Raycast(eyeOrigin.position, toTarget.normalized,
                    toTarget.magnitude, LayerMask.GetMask("Obstacle")))
                {
                    onPlayerDetected.Invoke();
                    return;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eyeOrigin.position, detectionRange);
    }
}
```

### 6. 關卡管理系統（調整）

- `currentLevel`：**0** = 第零關，**1～7** = 主線。
- `SetupLevel`：依 `LevelConfig` 開關鉤子、移動平台數量／巢是否移動、刷子、高腳桌布置、小鵝數量生成等。

### 7. 水面落水偵測

麵包觸發 `OnFail`；小鵝是否觸發依企劃（建議第三關起不因此直接 Fail）。

---

## Unity 場景結構建議（更新）

**路徑說明：** 鵝頭、脖子 Spline、槍手勢位移等腳本位於 `Assets/_Script/Goose/`（例如 `GunGestureLocomotion.cs`、`NeckSplineController.cs`）；下方樹狀圖為邏輯分組。

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs
│   │   ├── LevelConfig.cs
│   │   └── AudioManager.cs
│   ├── Gameplay/
│   │   ├── GooseHead.cs
│   │   ├── Bread.cs
│   │   ├── LittleGoose.cs
│   │   ├── LittleGooseAI.cs
│   │   ├── Nest.cs
│   │   ├── MovingPlatform.cs
│   │   ├── MovingNest.cs     ← 可選：第七關巢位移
│   │   ├── Guard.cs          ← 後續更新：巡邏
│   │   └── WaterTrigger.cs
│   └── UI/
│       ├── FailUI.cs
│       ├── ClearUI.cs
│       └── TutorialArrow.cs
├── Prefabs/
│   ├── Bread_*.prefab
│   ├── LittleGoose.prefab
│   ├── GooseHead.prefab
│   ├── Nest.prefab
│   ├── LongHook.prefab
│   ├── Brush.prefab          ← 第三／五關推麵包（可選）
│   └── Guard.prefab          ← 後續更新
└── Scenes/
    ├── Level0.unity          ← 起始／教學開局
    ├── Level1.unity ~ Level7.unity
    └── Ending.unity
```

---

## Layer 設定

| Layer | 用途 |
| --- | --- |
| `Bread` | 麵包 |
| `LittleGoose` | 小鵝（如需分開射線／物理） |
| `PlayerHand` | 鵝頭／手部追蹤 |
| `GooseNeck` | 脖子視覺 — 不參與巡邏偵測（將來用） |
| `Water` | 水面 Trigger |
| `Obstacle` | 遮蔽物（中央巢圍欄、巡邏視線遮蔽；後續 Guard 用） |

---

## 待校準數值（Playtesting）

| 項目 | 建議初始值 | 備註 |
| --- | --- | --- |
| Snap／嘴距 | 0.10 m | 麵包與小鵝分開測 |
| 平台速度 | 1.5 units/s | 第二、六關等 |
| 巢 Trigger 半徑 | 0.25 m | 與胸口下方對齊 |
| 小鵝 LookAt 距離 | TBD | 進入後 Slerp 速度需調 |
| 小鵝游速／轉向 | TBD | 避免眩暈與過難 |

---

## LevelConfig 可配置參數（v2）

| 參數 | 型別 | 說明 |
| --- | --- | --- |
| `breadToWin` | int | 該關所需麵包數 |
| `gooseToWin` | int | 該關所需小鵝數 |
| `hasHook` | bool | 是否啟用鉤子（第一關 true） |
| `hasMovingPlatform` | bool | 移動平台（第二、六關等；數量由場景／程式決定） |
| `nestMoves` | bool | 巢是否沿路徑來回移動（第七關 true） |
| `hasBrush` | bool | 是否配置刷子推麵包道具（第三關等） |
| `gooseSpawnCount` | int | 場景生成小鵝數（依關卡：如第二關 2、第四關 5、第七關 3） |
| `platformSpeed` | float | 移動平台 |
| `hasTutorialArrow` | bool | 第零關／第一關引導 |
| `hasPatrolGuard` | bool | **預設 false**；App 後續更新改 true |
| `patrolSpeed` / `patrolCycleTime` | float | 與 Guard 搭配，後續更新使用 |

### ScriptableObject 範例骨架

```csharp
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Header("Win")]
    public int breadToWin;
    public int gooseToWin;

    [Header("Features")]
    public bool hasTutorialArrow;
    public bool hasHook;
    public bool hasMovingPlatform;
    public bool nestMoves;
    public bool hasBrush;
    public bool hasPatrolGuard;

    [Header("Spawns")]
    public int gooseSpawnCount;

    [Header("Tuning")]
    public float platformSpeed = 1.5f;
    public float patrolSpeed = 1.5f;
    public float patrolCycleTime = 3f;
}
```

---

## 版本紀要

| 版本 | 日期 | 摘要 |
| --- | --- | --- |
| v1.1 | 2026 | 原五關：以麵包為主 + 巡邏關 |
| **v2.0** | **2026-04-24** | **小鵝愛麵包／偷麵包背景**；**第零關開局**；**關卡 1～4 重排（麵包 + 抓鵝回巢）**；**第五關暫緩**；**巡邏改後續更新**；**小鵝動畫與 AI（游動、扶正、臉部 LookAt）** |
| （補述） | 2026-04-30 | **Slogan**；**有限制移動**（`GunGestureLocomotion` + 脖子門檻 `NeckSplineController`） |
| **v2.1** | **2026-05-03** | **主線擴為第零～第七關**：第二關改為移動平台雙麵包 + 雙鵝；第三關中央巢 + 雙高腳桌 + 刷子推麵包；第四關五鵝；第五關三高腳桌三麵包 + 單鵝；第六關四移動平台四麵包；第七關來回移動巢 + 三鵝；**LevelConfig** 增列 `nestMoves`、`hasBrush` 建議欄位 |

---

*文件版本：v2.1　最後更新：2026-05-03*
