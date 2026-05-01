using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// 巢判定：分別累計 <see cref="Bread"/> 與 <see cref="LittleGoose"/> 送達數；
/// 過關條件為 <c>breadDelivered &gt;= requiredBread</c> 且 <c>gooseDelivered &gt;= requiredGoose</c>。
/// 掛在巢 GameObject 上，需搭配一個 isTrigger = true 的 SphereCollider（建議半徑 0.25 m）。
///
/// 麵包流程：
///   1. 進入 Trigger → <c>_pendingBreads</c>；2. 放手（<c>!IsHeld</c>）→ 入巢計分
/// 小鵝流程：同上，且 <c>LittleGoose.countsTowardsGoal == false</c> 者（如第零關裝飾鵝）不進等待／計分。
/// 兩者皆以 <c>HashSet</c> 去重。達標時會自動呼叫 <see cref="GameManager.OnLevelClear"/>；
/// <see cref="onLevelClear"/> 僅供額外監聽。外部呼叫 <see cref="ResetNest"/> 清空狀態。
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class Nest : MonoBehaviour
{
    [Header("過關條件")]
    [Tooltip("需要幾塊麵包送達巢中（第零關 = 1、第一關 = 3）。")]
    [FormerlySerializedAs("requiredCount")]
    public int requiredBread = 1;

    [Tooltip("需要幾隻小鵝送達（第零關 = 0、第三關 = 3；僅「計分用」小鵝會累加）。")]
    public int requiredGoose = 0;

    [Tooltip("達標時額外觸發（已自動呼叫 GameManager.OnLevelClear；此欄位可加其他監聽）")]
    UnityEvent onLevelClear;

    [Header("入巢動畫")]
    [Tooltip("麵包落入巢中心的隨機散佈半徑（m）")]
    public float pileSpreadRadius = 0.05f;

    [Tooltip("小鵝入巢在麵包半徑基礎上額外放大的散佈（m），避免重疊。")]
    public float goosePileExtraSpread = 0.04f;

    [Tooltip("入巢位移動畫時間（秒）")]
    [Range(0.05f, 0.5f)]
    public float snapDuration = 0.15f;

    // ── 狀態 ──────────────────────────────────────────────────────────────
    private int  _breadDelivered  = 0;
    private int  _gooseDelivered  = 0;
    private bool _levelCleared  = false;

    private readonly HashSet<Bread>        _pendingBreads = new HashSet<Bread>();
    private readonly HashSet<LittleGoose>  _pendingGeese  = new HashSet<LittleGoose>();
    private readonly HashSet<Bread>        _countedBreads = new HashSet<Bread>();
    private readonly HashSet<LittleGoose>  _countedGeese  = new HashSet<LittleGoose>();

    public int BreadDelivered  => _breadDelivered;
    public int GooseDelivered  => _gooseDelivered;
    public int RequiredBread   => requiredBread;
    public int RequiredGoose   => requiredGoose;
    /// <summary>與 <see cref="BreadDelivered"/> 相同（舊專案相容名稱）。</summary>
    public int FoodCount     => _breadDelivered;
    public int RequiredCount => requiredBread;

    void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_levelCleared) return;

        var bread = other.GetComponentInParent<Bread>();
        if (bread != null && !_countedBreads.Contains(bread))
            _pendingBreads.Add(bread);

        var goose = other.GetComponentInParent<LittleGoose>();
        if (goose != null && goose.countsTowardsGoal && !_countedGeese.Contains(goose))
            _pendingGeese.Add(goose);
    }

    void OnTriggerStay(Collider other)
    {
        if (_levelCleared) return;

        var bread = other.GetComponentInParent<Bread>();
        if (bread != null && _pendingBreads.Contains(bread) && !bread.IsHeld)
            AcceptBread(bread);

        var goose = other.GetComponentInParent<LittleGoose>();
        if (goose != null && _pendingGeese.Contains(goose) && !goose.IsHeld)
            AcceptGoose(goose);
    }

    void OnTriggerExit(Collider other)
    {
        var bread = other.GetComponentInParent<Bread>();
        if (bread != null) _pendingBreads.Remove(bread);
        var goose = other.GetComponentInParent<LittleGoose>();
        if (goose != null) _pendingGeese.Remove(goose);
    }

    void TryFireLevelClear()
    {
        if (_breadDelivered < requiredBread || _gooseDelivered < requiredGoose) return;
        _levelCleared = true;
        GameManager.Instance?.OnLevelClear();
        onLevelClear.Invoke();
    }

    private void AcceptBread(Bread bread)
    {
        _pendingBreads.Remove(bread);
        _countedBreads.Add(bread);

        bread.Detach();
        bread.transform.SetParent(transform);

        Rigidbody rb = bread.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Vector3 target = transform.position + Random.insideUnitSphere * pileSpreadRadius;
        StartCoroutine(SnapIntoPile(bread.transform, rb, target));

        _breadDelivered++;
        Debug.Log($"[Nest] 麵包 = {_breadDelivered}/{requiredBread}，小鵝 = {_gooseDelivered}/{requiredGoose}");

        TryFireLevelClear();
    }

    private void AcceptGoose(LittleGoose goose)
    {
        _pendingGeese.Remove(goose);
        _countedGeese.Add(goose);

        goose.DetachFromCarry();
        goose.transform.SetParent(transform);

        Rigidbody rb = goose.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        var gtf = goose.transform;
        float r  = pileSpreadRadius + goosePileExtraSpread;
        Vector3 target = transform.position + Random.insideUnitSphere * r;
        var ai = goose.GetComponent<LittleGooseAI>();
        if (ai != null) ai.enabled = false;
        StartCoroutine(SnapIntoPile(gtf, rb, target));

        _gooseDelivered++;
        Debug.Log($"[Nest] 麵包 = {_breadDelivered}/{requiredBread}，小鵝 = {_gooseDelivered}/{requiredGoose}");

        TryFireLevelClear();
    }

    private IEnumerator SnapIntoPile(Transform t, Rigidbody rb, Vector3 target)
    {
        Vector3 start   = t.position;
        float   elapsed = 0f;

        while (elapsed < snapDuration)
        {
            if (t == null) yield break;

            float tStep = Mathf.SmoothStep(0f, 1f, elapsed / snapDuration);
            t.position = Vector3.Lerp(start, target, tStep);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (t != null)
        {
            t.position = target;
            // 維持 isKinematic（Accept 時已設 true）：若此處改回 dynamic，巢內多個
            // Rigidbody 重疊時會因解穿透互相把對方噴飛。
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public void ResetNest()
    {
        StopAllCoroutines();
        _breadDelivered  = 0;
        _gooseDelivered  = 0;
        _levelCleared  = false;
        _pendingBreads.Clear();
        _pendingGeese.Clear();
        _countedBreads.Clear();
        _countedGeese.Clear();
    }

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
