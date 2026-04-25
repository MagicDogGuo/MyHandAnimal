# 🪿 VR 鵝頭偷麵包 — 遊戲設計 + 開發規劃文件

> **版本：** v2.0　**最後更新：** 2026-04-24　**引擎：** Unity 6 (URP) + Meta all in one Plugin (Meta SDK)

---

## 核心概念與背景

**背景故事：** 小鵝喜歡吃麵包；玩家扮演的鵝要去**偷麵包**，並在後續關卡中把**游來游去的小鵝**抓回巢裡。

**操作核心：** 玩家用手模擬鵝頭，控制嘴部開合與頭部轉向，伸長脖子取物（麵包 / 必要時含小鵝）帶回巢中。

**新互動：** 除麵包外，關卡目標包含**抓小鵝回巢**（與巢的 Trigger 判定、計數邏輯需支援 `Bread` 與 `LittleGoose` 兩類可交付物）。

---

## 關卡設計總覽

| 關卡    | 名稱（工作稱呼）   | 過關要點摘要 |
| ----- | ----------- | ------- |
| **第零關** | 起始畫面 / 開局 | 將麵包**擺入巢中**以開始遊戲；巢內已有小鵝（氛圍 / 教學） |
| **第一關** | 鉤子 + 平台麵包 | 用**鉤子**取 **1 個較高處麵包** + **平台上 2 個麵包**，共 3 塊回巢 |
| **第二關** | 麵包 + 單鵝     | **2 塊麵包**回巢 + **1 隻小鵝**回巢（小鵝可在水面游動） |
| **第三關** | 三鵝          | **3 隻小鵝**回巢（水面任意游動） |
| **第四關** | 移動平台 + 三鵝  | **移動平台**上 **1 塊麵包** + **3 隻小鵝**回巢（小鵝水面游動） |
| **第五關** | （預留）        | **可先不做**，後續再定義 |

---

## 詳細關卡設計

### 第零關：起始畫面 → 擺麵包開局

| 項目 | 內容 |
| --- | --- |
| **場景配置** | 巢在視線可及處，巢內／旁有**小鵝**（靜態或待機動畫）；旁邊提供 **1 塊可抓取麵包**（或明確提示物） |
| **開始條件** | 玩家將麵包**放入巢 Trigger**（與正式關卡相同判定）→ 載入第一關或進入主流程 |
| **核心機制** | 與 Nest 共用邏輯；可僅要求 `breadCount >= 1` 即切場景，不計為「排行榜關卡」 |
| **待補強** | 開場 UI、簡短文字／圖示引導「把麵包放進巢裡開始」 |

### 第一關：高處麵包 + 鉤子 + 雙平台麵包

| 項目 | 內容 |
| --- | --- |
| **場景配置** | **1 塊麵包**在**較高位置**（需鉤子勾取或拉近）；**同一平台（或兩處平台）上共 2 塊麵包** |
| **過關條件** | 巢中累計 **3 塊麵包**（順序不限） |
| **失敗條件** | 麵包掉入水中 → Fail UI → 重置（與現行設計一致） |
| **核心機制** | 長鉤子實體抓取 + Snap 咬麵包；`foodCount` / `breadToWin = 3` |
| **待補強** | 鉤頭 Collider、麵包質量／阻力，避免彈飛 |

### 第二關：兩麵包 + 一隻小鵝

| 項目 | 內容 |
| --- | --- |
| **場景配置** | **2 塊麵包**；**1 隻小鵝**在**水面**活動（四處游動，見「小鵝 AI」） |
| **過關條件** | **2 麵包 + 1 小鵝**皆曾進入巢並計入（達成順序可不限，依實作採「分項計數」或「總件數」） |
| **失敗條件** | 麵包落水；小鵝落水是否失敗由企劃決定（建議：小鵝僅在水面活動，不判失敗，或落水後重生） |
| **核心機制** | Nest 擴充：可辨識 `Bread` 與 `LittleGoose`，分別累加 `breadDelivered` / `gooseDelivered` |

### 第三關：三隻小鵝回巢

| 項目 | 內容 |
| --- | --- |
| **場景配置** | **3 隻小鵝**於水面游動 |
| **過關條件** | **3 隻小鵝**皆回巢（各只計一次） |
| **核心機制** | 與第二關相同 AI；計數 `gooseDelivered >= 3` |

### 第四關：移動平台麵包 + 三鵝

| 項目 | 內容 |
| --- | --- |
| **場景配置** | **移動平台**承載 **1 塊麵包**；**3 隻小鵝**水面游動 |
| **過關條件** | **1 麵包 + 3 小鵝**回巢 |
| **核心機制** | `MovingPlatform`（如 Sine 往返）；其餘同第三關 Nest 邏輯 |

### 第五關：預留

| 項目 | 內容 |
| --- | --- |
| **狀態** | **本階段可不做** |
| **備註** | 後續可與「巡邏／光束」等後期功能一併設計 |

### 結局（建議保留）

全關完成後進入簡短結局演出（例如白色空間、麵包／小鵝慶祝），細節可沿用舊版或簡化。

---

## 巡邏功能（App 後續更新）

**決策：** 第四、五關舊版中的**巡邏員、Cone 光束、視線躲避**不納入當前主線關卡；**保留為之後版本**的擴充內容。

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
| **W3** | 第二～三關串接、落水與重置 | 水面、小鵝材質 polish |
| **W4** | 第四關移動平台 + 全關串接、Bug 修 | 結局與 UI |

**里程碑建議：**

- Nest 支援雙類型交付物 + 第零關可玩 → 第一關可玩 → 第二～三關可玩 → 第四關 + 版本凍結。

---

## 概述：程式任務（Unity / C#）

- **VR 基礎** — XR Origin、Meta SDK、手部追蹤、`HandGrabInteractor`。
- **Snap Grabbing** — 麵包與小鵝 Prefab 掛 `Grabbable` + `HandGrabInteractable`；嘴部錨點吸附。
- **Nest** — 擴充為：`requiredBread`、`requiredGoose`（或 ScriptableObject 關卡表）；`OnTriggerEnter` 分辨 `Bread` / `LittleGoose`；過關條件兩者皆達標。
- **第零關** — 單一麵包入巢 → `LoadScene("Level1")` 或 `GameManager` 狀態切換。
- **移動平台** — 沿用 `MovingPlatform`（Sine）；僅第四關啟用。
- **小鵝** — `LittleGooseAI.cs`（游動、扶正、LookAt）、`Animator` 參數驅動。
- **關卡管理** — `totalLevels` 對應 0～4（第五關關閉或空場）；結局場景可選。

---

## 程式設計建議（沿用與調整）

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

### 4. 移動平台（第四關）

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

- `currentLevel`：0 = 第零關，1～4 = 主線；跳過 5 或 `totalLevels = 5` 但第五關場景空。
- `SetupLevel`：依 `LevelConfig` 開關鉤子、移動平台、小鵝數量生成等。

### 7. 水面落水偵測

麵包觸發 `OnFail`；小鵝是否觸發依企劃（建議第三關起不因此直接 Fail）。

---

## Unity 場景結構建議（更新）

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
│   └── Guard.prefab          ← 後續更新
└── Scenes/
    ├── Level0.unity          ← 起始／教學開局
    ├── Level1.unity ~ Level4.unity
    ├── Level5.unity          ← 可選／預留
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
| `Obstacle` | 遮蔽物（將來第五關／巡邏用） |

---

## 待校準數值（Playtesting）

| 項目 | 建議初始值 | 備註 |
| --- | --- | --- |
| Snap／嘴距 | 0.10 m | 麵包與小鵝分開測 |
| 平台速度 | 1.5 units/s | 第四關 |
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
| `hasMovingPlatform` | bool | 移動平台（第四關 true） |
| `gooseSpawnCount` | int | 場景生成小鵝數（2～3 關） |
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

---

*文件版本：v2.0　最後更新：2026-04-24*
