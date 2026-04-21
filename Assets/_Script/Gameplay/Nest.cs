using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 巢判定系統 + foodCount 計數器。
/// 掛在巢 GameObject 上，需搭配一個 isTrigger = true 的 SphereCollider（建議半徑 0.25 m）。
///
/// 流程：
///   1. 麵包進入 Trigger → 加入 _pendingBreads（可能還在手裡）
///   2. OnTriggerStay 每幀輪詢：bread.IsHeld 變為 false（放手）→ 觸發入巢
///   3. 麵包離開 Trigger 且未入巢 → 從 _pendingBreads 移除（帶走）
///   4. 入巢：Detach → SetParent → SnapIntoPile 動畫 → foodCount++
///   5. 達到 requiredCount → 觸發 onLevelClear
///
/// 外部呼叫 ResetNest() 重置計數器（巢內麵包由 GameManager.ResetToSpawn 處理）。
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class Nest : MonoBehaviour
{
    [Header("過關條件")]
    [Tooltip("需要放入幾塊麵包才算過關（各關在 Inspector 設定；第一關 = 1，第二關 = 3）")]
    public int requiredCount = 1;

    [Tooltip("達標時觸發（接上 GameManager.OnLevelClear 或直接驅動 ClearUI）")]
    public UnityEvent onLevelClear;

    [Header("入巢動畫")]
    [Tooltip("麵包落入巢中心的隨機散佈半徑（m）")]
    public float pileSpreadRadius = 0.05f;

    [Tooltip("入巢位移動畫時間（秒）")]
    [Range(0.05f, 0.5f)]
    public float snapDuration = 0.15f;

    // ── 狀態 ──────────────────────────────────────────────────────────────
    private int  _foodCount    = 0;
    private bool _levelCleared = false;

    /// <summary>在巢區域內、但尚未放手的麵包。</summary>
    private readonly HashSet<Bread> _pendingBreads = new HashSet<Bread>();

    /// <summary>已確認入巢計分的麵包，防止重複計數。</summary>
    private readonly HashSet<Bread> _counted = new HashSet<Bread>();

    // ── 公開屬性（供 UI / GameManager 讀取當前計數）──────────────────────
    public int FoodCount     => _foodCount;
    public int RequiredCount => requiredCount;

    // ── 初始化：確保 Collider 是 Trigger ─────────────────────────────────
    void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true;
    }

    // ── Step 1：麵包進入巢範圍 → 加入等待集合 ────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (_levelCleared) return;

        Bread bread = other.GetComponent<Bread>();
        if (bread == null || _counted.Contains(bread)) return;

        _pendingBreads.Add(bread);
    }

    // ── Step 2：每幀檢查等待中的麵包是否已放手 ───────────────────────────
    void OnTriggerStay(Collider other)
    {
        if (_levelCleared) return;

        Bread bread = other.GetComponent<Bread>();
        if (bread == null || !_pendingBreads.Contains(bread)) return;

        // 放手瞬間（IsHeld 從 true → false）才入巢
        if (!bread.IsHeld)
            AcceptBread(bread);
    }

    // ── Step 3：麵包離開範圍（被手帶走）→ 移出等待集合 ──────────────────
    void OnTriggerExit(Collider other)
    {
        Bread bread = other.GetComponent<Bread>();
        if (bread != null)
            _pendingBreads.Remove(bread);
    }

    // ── 確認入巢：Detach → Parent → 動畫 → 計分 ─────────────────────────
    private void AcceptBread(Bread bread)
    {
        _pendingBreads.Remove(bread);
        _counted.Add(bread);

        bread.Detach();
        bread.transform.SetParent(transform);

        Rigidbody rb = bread.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Vector3 target = transform.position + Random.insideUnitSphere * pileSpreadRadius;
        StartCoroutine(SnapIntoPile(bread.transform, rb, target));

        _foodCount++;
        Debug.Log($"[Nest] foodCount = {_foodCount} / {requiredCount}");

        if (_foodCount >= requiredCount)
        {
            _levelCleared = true;
            onLevelClear.Invoke();
        }
    }

    // ── 入巢位移小動畫 ────────────────────────────────────────────────────
    private IEnumerator SnapIntoPile(Transform breadTf, Rigidbody rb, Vector3 target)
    {
        Vector3 start   = breadTf.position;
        float   elapsed = 0f;

        while (elapsed < snapDuration)
        {
            if (breadTf == null) yield break;

            float t = Mathf.SmoothStep(0f, 1f, elapsed / snapDuration);
            breadTf.position = Vector3.Lerp(start, target, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (breadTf != null)
        {
            breadTf.position = target;
            // 動畫結束後恢復物理，讓麵包在巢中自然堆疊
            if (rb != null) rb.isKinematic = false;        
        }
    }

    // ── 重置（失敗 / 關卡重來時由 GameManager 呼叫）──────────────────────
    /// <summary>
    /// 只重置計數器與狀態旗標。
    /// 巢內麵包的 Parent 解除與位置還原由 GameManager 統一呼叫
    /// bread.ResetToSpawn() 處理，此處不再 Destroy。
    /// </summary>
    public void ResetNest()
    {
        StopAllCoroutines();
        _foodCount    = 0;
        _levelCleared = false;
        _pendingBreads.Clear();
        _counted.Clear();
    }

    // ── Editor Gizmos：在 Scene View 顯示巢 Trigger 範圍 ─────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var col = GetComponent<SphereCollider>();
        if (col == null) return;

        Vector3 center = transform.position + col.center;

        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.25f);
        Gizmos.DrawSphere(center, col.radius);

        Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
        Gizmos.DrawWireSphere(center, col.radius);
    }
#endif
}
