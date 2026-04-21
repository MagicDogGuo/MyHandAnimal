using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 巢判定系統 + foodCount 計數器。
/// 掛在巢 GameObject 上，需搭配一個 isTrigger = true 的 SphereCollider（建議半徑 0.25 m）。
///
/// 流程：
///   1. 麵包進入 Trigger → 呼叫 bread.Detach() 解除嘴部吸附
///   2. 麵包 SetParent(transform) 成為巢的子物件
///   3. SnapIntoPile Coroutine 播放小位移落巢動畫
///   4. foodCount++；達到 requiredCount 時觸發 onLevelClear
///
/// 外部呼叫 ResetNest() 重置計數器並銷毀巢內所有子麵包物件。
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
    private int  _foodCount     = 0;
    private bool _levelCleared  = false;

    /// <summary>防止同一塊麵包在滾動中觸發多次 OnTriggerEnter 被重複計數。</summary>
    private readonly HashSet<Bread> _counted = new HashSet<Bread>();

    // ── 公開屬性（供 UI / GameManager 讀取當前計數）──────────────────────
    public int FoodCount     => _foodCount;
    public int RequiredCount => requiredCount;

    // ── 初始化：確保 Collider 是 Trigger ─────────────────────────────────
    void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true;
    }

    // ── 巢判定主邏輯 ──────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (_levelCleared) return;

        Bread bread = other.GetComponent<Bread>();
        if (bread == null)          return;
        if (_counted.Contains(bread)) return;   // 同一麵包只計一次

        _counted.Add(bread);

        // 先解除嘴部 Parent，再讓巢接管
        bread.Detach();
        bread.transform.SetParent(transform);

        // 停止物理碰撞，播放入巢動畫
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
