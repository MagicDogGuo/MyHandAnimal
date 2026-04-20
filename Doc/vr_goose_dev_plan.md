# 🪿 VR 鵝頭偷麵包 — 遊戲設計 + 開發規劃文件

> **版本：** v1.1　**最後更新：** 2026　**引擎：** Unity 6 (URP) + Meta all in one Plugin (Meta SDK)

---

## 核心概念

玩家用手模擬鵝頭，控制嘴部開合與頭部轉向，伸出脖子偷麵包帶回巢中。

---

## 關卡設計總覽

| 關卡 | 名稱 | 訓練核心 | 麵包 |
|------|------|---------|------|
| 第一關 | Straight to the Nest | 伸長 → 咬 → 縮回 | 🍞 白吐司 |
| 第二關 | Multitasking | 左右橫向移動 | 🥖 長棍麵包 |
| 第三關 | The Moving Meal | 預判移動目標 | 🍩 甜甜圈 |
| 第四關 | 視線躲避 | 躲避巡邏光束 | 🥐 可頌 |
| 第五關 | 時機抓取 | 綜合第三、四關 | 🍞🥖🍩 混合 |

---

## 詳細關卡設計

### 第一關：Straight to the Nest

| 項目 | 內容 |
|------|------|
| **場景配置** | 麵包在正前方碼頭邊緣，巢在視線正下方 |
| **過關條件** | 成功放入巢中 1 次 |
| **失敗條件** | 麵包掉入水中 → Fail UI → 重來 |
| **核心機制** | 基礎 Snap Grabbing；無障礙、無時限 |
| **⚠️ 待補強** | 需新增新手引導箭頭 UI |

### 第二關：Multitasking

| 項目 | 內容 |
|------|------|
| **場景配置** | 岸邊散落 3 塊麵包（左、中、右） |
| **過關條件** | `foodCount == 3` 顯示 Level Clear；順序不限 |
| **失敗條件** | 任一麵包落水 → 三塊全部重置 |
| **核心機制** | 巢內整數計數器 `int foodCount` |

### 第三關：The Moving Meal

| 項目 | 內容 |
|------|------|
| **場景配置** | 麵包放在左右緩慢往返的平台上 |
| **過關條件** | 成功放入巢中 1 次；無時限 |
| **失敗條件** | 麵包落水 → 重來 |
| **核心機制** | Sine 曲線往返平台；咬空無懲罰 |
| **⚠️ 待補強** | 平台移動速度待 playtesting 校準（建議初始 1.5 units/s） |

### 第四關：視線躲避

| 項目 | 內容 |
|------|------|
| **場景配置** | 巡邏員持手電筒 Cone 光束來回掃視 |
| **過關條件** | 成功放入 3 次且未被發現；無時限 |
| **失敗條件** | 手進入光束 → Fail UI；麵包落水亦同 |
| **核心機制** | Cone Trigger 判定；脖子穿模不計入 |
| **⚠️ 待補強** | 巡邏路徑與轉頭速度待校準（建議 3 秒一來回）；警戒過渡動畫 TBD |

### 第五關：時機抓取

| 項目 | 內容 |
|------|------|
| **場景配置** | 遮蔽物 + 移動平台 + 巡邏員 |
| **過關條件** | 放入 3 次且未被發現；無時限 |
| **失敗條件** | 進入光束 / 麵包落水 → 重來 |
| **核心機制** | 安全區系統 + 第三、四關機制組合 |
| **⚠️ 待補強** | 遮蔽物位置需實機 VR 站立測試確認 |

### 🏁 結局

進入白色空間，天上掉落所有偷來的麵包 🍞🥖🍩

---

## 7 天開發時程

| 天 | 程式 | 美術 |
|----|------|------|
| D1 | VR 基礎設置（XR Origin、手部追蹤、相機） | 鵝頭模型開始 + 場景 blockout |
| D2 | Snap Grabbing 系統 | 鵝頭完成 + 麵包 ×4 種類開始 |
| D3 | 巢判定 + foodCount 計數器 | 麵包完成 + 碼頭場景完成 |
| D4 | 移動平台（Sine）+ Cone 光束系統 | 推車 mesh + 巡邏員模型 + NPC 動畫 |
| D5 | 關卡管理 + Fail/Win UI + 新手引導 | 手電筒光效 + 遮蔽物 |
| D6 | 全 5 關串接 + Playtest 校準 | 結局白色空間 + 麵包粒子特效 |
| D7 | Bug 修正 + 版本凍結 | 最終美術 polish |

**里程碑：**
- D3 結束：關卡 1–2 可遊玩
- D5 結束：關卡 3–4 可遊玩
- D6 結束：全關串接完成
- D7 結束：Demo 版本凍結

---

## 概述

### 程式任務（Unity / C#）
- D1 — VR 基礎設置
建立 XR Origin、配置 Meta SDK、手部追蹤輸入、主相機。確認在裝置上能跑起來。
- D2 — Snap Grabbing 系統
使用 Meta SDK Building Blocks 加入 **Hand Grab Interaction** Block（自動建立 HandGrabInteractor）。麵包 Prefab 掛 `Grabbable` + `HandGrabInteractable`，再加薄層 `BreadSnapToMouth.cs` 監聽 SDK 事件，抓取時吸附到 mouthAnchor（isKinematic = true），放開後恢復物理重力（isKinematic = false）。無需自行輪詢 OVRInput。
- D3 — 巢判定 + foodCount 計數器
巢設一個大隱形 Sphere Collider，麵包進入自動解除 Parent。int foodCount 累計，達標時顯示 Level Clear UI；掉水判定觸發 Fail UI + 重置。
- D4 — 移動平台 + Cone 光束系統
移動平台用 Sine 曲線往返（Mathf.Sin）；巡邏員 Cone Trigger 以 Physics.OverlapSphere 或自訂 FOV 判定手部位置，命中即觸發「被發現」狀態。
- D5 — 關卡管理 + UI 系統
GameManager 統一控制關卡切換、重置邏輯、結局場景（白色空間 + 麵包雨粒子）。第一關補上新手引導箭頭 UI。

### 美術任務（Blender → Unity）
- D1–2 — 鵝頭模型
Low Poly 風格，保留嘴部骨架（兩根骨頭），貼圖做簡單白色 + 橘嘴。手部追蹤對應嘴部開合動畫。
- D2–3 — 四種麵包
🍞 白吐司 / 🥖 長棍 / 🍩 甜甜圈 / 🥐 可頌，各一個 mesh，Spawn 時加 ±10% 隨機縮放與旋轉。
- D1–3 — 碼頭場景（共用背景）
水面（Shader/Particle）、木板碼頭、巢（草葉細節）、環境光源。五關共用底圖，分層擺放。
- D4–5 — 移動道具 + 巡邏員
推車 mesh + 滑動動畫；NPC 模型配巡邏走路週期動畫；手電筒 Spot Light 做 Cone 光效，加輕微掃射音效。
- D6 — 結局場景
純白空間 + 所有麵包從天而降的粒子爆炸特效，搭配勝利音效/BGM。



---

## 程式設計建議（Unity / C#）

### 1. Snap Grabbing 系統

**問題：** VR 裡精準咬到小麵包困難，缺乏觸覺回饋。

**解決方案：** 直接使用 Meta SDK Interaction SDK 內建的 **Grab Building Block**，不需自行輪詢 OVRInput。Snap 吸附邏輯只需一個薄薄的監聽腳本掛在麵包上即可。

---

#### Scene 設置（Inspector 操作，無需手寫 Grab 邏輯）

1. **XR Origin（Camera Rig）**
   - 在 Meta Building Blocks 面板加入 **Hand Grab Interaction** Block
     （會自動建立 `HandGrabInteractor` 於左右手節點）

2. **麵包 Prefab** — 每個麵包加上以下 Component：
   - `Rigidbody`（非 Kinematic）
   - `Collider`（建議 Box / Sphere）
   - `Grabbable`（Meta Interaction SDK）
   - `HandGrabInteractable`（Meta Interaction SDK）
   - `BreadSnapToMouth`（自訂，見下方程式碼）

3. **鵝頭 GameObject**（mouthAnchor 空物件）
   - 掛上 `MouthAnchor` Tag 或直接在 `BreadSnapToMouth` 序列化指定

> ⚠️ **不需要** 再寫 `OVRInput.Get(...)` 或 `Physics.OverlapSphere` 來偵測抓取；Meta SDK 的 `HandGrabInteractable` 已處理所有輸入與物理。

---

#### 自訂腳本：吸附到嘴部（Snap to Mouth）

```csharp
// BreadSnapToMouth.cs
// 掛在每個麵包 Prefab 上
// 負責：抓取時吸附到 mouthAnchor；放開時恢復物理
using Oculus.Interaction;
using UnityEngine;

public class BreadSnapToMouth : MonoBehaviour
{
    [Header("Snap Target")]
    public Transform mouthAnchor;        // 鵝嘴錨點（Inspector 指定）

    private HandGrabInteractable _interactable;
    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _interactable = GetComponent<HandGrabInteractable>();
    }

    void OnEnable()
    {
        // Meta SDK 事件：當有 Interactor 開始選取（抓住）
        _interactable.WhenSelectingInteractorViewAdded += OnGrabbed;
        // Meta SDK 事件：當 Interactor 停止選取（放開）
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
        // 僅在沒有其他 Interactor 仍在抓時才真正放開
        if (_interactable.SelectingInteractorViews.Count > 0) return;

        transform.SetParent(null);
        _rb.isKinematic = false;         // 恢復重力物理
    }

    // 隨機縮放（Spawn 時呼叫）
    public void RandomizeAppearance()
    {
        float scale = Random.Range(0.9f, 1.1f);
        transform.localScale = Vector3.one * scale;
        transform.rotation = Random.rotation;
    }
}
```

> **為什麼不寫 Grab 主邏輯？**
> Meta SDK 的 `HandGrabInteractable` 已處理：
> - 手部追蹤 Pinch / Palm Grab 姿勢辨識
> - Controller Grip 按鍵偵測
> - 抓取時物理交互（velocity tracking）
> - 震動回饋（在 Building Block 設定 `HapticsOnGrab`）
>
> 我們只需監聽它暴露的事件，再做遊戲邏輯（吸附 / 解除）即可。

---

### 2. 巢判定系統

**問題：** 玩家縮手時麵包可能撞到自身或相機。

**解決方案：** 擴大巢的 Trigger 球體，麵包進入即自動入巢，不需精準放置。

```csharp
// Nest.cs
public class Nest : MonoBehaviour
{
    [Header("Win Condition")]
    public int requiredCount = 1;         // 各關設定不同目標
    public UnityEvent onLevelClear;

    private int foodCount = 0;

    void OnTriggerEnter(Collider other)
    {
        Bread bread = other.GetComponent<Bread>();
        if (bread == null) return;

        bread.Detach();                   // 解除手部 Parent
        bread.transform.SetParent(transform);

        // 播放入巢小位移動畫
        StartCoroutine(SnapIntoPile(bread.transform));

        foodCount++;
        if (foodCount >= requiredCount)
            onLevelClear.Invoke();
    }

    IEnumerator SnapIntoPile(Transform t)
    {
        Vector3 target = transform.position + Random.insideUnitSphere * 0.05f;
        float elapsed = 0f;
        Vector3 start = t.position;
        while (elapsed < 0.15f)
        {
            t.position = Vector3.Lerp(start, target, elapsed / 0.15f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.position = target;
        // 之後讓物理堆疊（可選）
        t.GetComponent<Rigidbody>().isKinematic = false;
    }

    public void ResetNest()
    {
        foodCount = 0;
        // 清除巢內所有麵包
        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }
}
```

---

### 3. 移動平台（第三關）

```csharp
// MovingPlatform.cs
public class MovingPlatform : MonoBehaviour
{
    public float speed = 1.5f;           // Playtesting 初始值
    public float range = 2.0f;          // 左右各偏移 2m

    private Vector3 origin;

    void Start() => origin = transform.position;

    void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * range;
        transform.position = origin + Vector3.right * offset;
    }
}
```

---

### 4. 巡邏員 Cone 光束系統（第四關）

**重點設計：** 以 `Physics.CheckSphere` 搭配視角判定，脖子（視覺用）不參與碰撞。

```csharp
// Guard.cs
public class Guard : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 1.5f;

    [Header("Detection")]
    public Transform eyeOrigin;
    public float detectionAngle = 40f;    // Cone 半角
    public float detectionRange = 5f;
    public LayerMask playerHandLayer;     // 只偵測手部 Layer，不偵測脖子

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

        // 看向巡邏目標
        Vector3 dir = (target.position - transform.position).normalized;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
            currentPoint = (currentPoint + 1) % patrolPoints.Length;
    }

    void CheckDetection()
    {
        // 在偵測範圍內取得手部 Collider
        Collider[] hits = Physics.OverlapSphere(
            eyeOrigin.position, detectionRange, playerHandLayer);

        foreach (Collider hit in hits)
        {
            Vector3 toTarget = hit.transform.position - eyeOrigin.position;
            float angle = Vector3.Angle(eyeOrigin.forward, toTarget);

            if (angle < detectionAngle)
            {
                // Raycast 確認無遮擋
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
        // Scene View 中顯示偵測 Cone（便於調試）
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eyeOrigin.position, detectionRange);
    }
}
```

---

### 5. 關卡管理系統

```csharp
// GameManager.cs
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Level Config")]
    public int currentLevel = 1;
    public int totalLevels = 5;

    [Header("UI")]
    public GameObject failUI;
    public GameObject clearUI;
    public GameObject tutorialArrow;    // 第一關新手引導

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SetupLevel(currentLevel);
    }

    void SetupLevel(int level)
    {
        // 第一關顯示引導
        if (tutorialArrow != null)
            tutorialArrow.SetActive(level == 1);
    }

    public void OnLevelClear()
    {
        clearUI.SetActive(true);
        Invoke(nameof(LoadNextLevel), 2f);
    }

    public void OnFail()
    {
        failUI.SetActive(true);
        Invoke(nameof(ResetCurrentLevel), 1.5f);
    }

    void LoadNextLevel()
    {
        clearUI.SetActive(false);
        currentLevel++;

        if (currentLevel > totalLevels)
        {
            // 觸發結局
            SceneManager.LoadScene("Ending");
            return;
        }

        SceneManager.LoadScene("Level" + currentLevel);
    }

    void ResetCurrentLevel()
    {
        failUI.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
```

---

### 6. 水面落水偵測

```csharp
// WaterTrigger.cs
// 掛在水面的 Trigger Collider 上
public class WaterTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Bread>() != null)
        {
            GameManager.Instance.OnFail();
        }
    }
}
```

---

## Unity 場景結構建議

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs
│   │   ├── LevelConfig.cs       ← ScriptableObject 設定各關參數
│   │   └── AudioManager.cs
│   ├── Gameplay/
│   │   ├── GooseHead.cs
│   │   ├── Bread.cs
│   │   ├── Nest.cs
│   │   ├── MovingPlatform.cs
│   │   ├── Guard.cs
│   │   ├── SafeZone.cs
│   │   └── WaterTrigger.cs
│   └── UI/
│       ├── FailUI.cs
│       ├── ClearUI.cs
│       └── TutorialArrow.cs
├── Prefabs/
│   ├── Bread_Toast.prefab
│   ├── Bread_Baguette.prefab
│   ├── Bread_Donut.prefab
│   ├── Bread_Croissant.prefab
│   ├── GooseHead.prefab
│   ├── Nest.prefab
│   └── Guard.prefab
└── Scenes/
    ├── Level1.unity ~ Level5.unity
    └── Ending.unity
```

---

## Layer 設定

| Layer | 用途 |
|-------|------|
| `Bread` | 所有麵包物件 |
| `PlayerHand` | 鵝頭（手部追蹤）—— 巡邏員偵測目標 |
| `GooseNeck` | 脖子視覺 mesh —— **不加入偵測 Layer** |
| `Water` | 水面 Trigger |
| `Obstacle` | 遮蔽物（第五關安全區）|

---

## ⚠️ 待校準數值（Playtesting）

| 項目 | 建議初始值 | 備註 |
|------|---------|------|
| Snap 半徑 | 0.10 m | 過大會感覺不真實 |
| 平台移動速度 | 1.5 units/s | 第三關 |
| 巡邏員往返週期 | 3 秒 | 第四關 |
| Cone 偵測角度 | 40° 半角 | 第四關 |
| 巢 Trigger 半徑 | 0.25 m | 覆蓋胸口下方 |
| 麵包縮放隨機範圍 | 0.9 ~ 1.1 | 視覺多樣性 |

---

*文件版本：v1.1　最後更新：2026*
