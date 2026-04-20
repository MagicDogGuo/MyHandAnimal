using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

/// <summary>
/// 掛在每個麵包 Prefab 上。
/// 監聽 Meta SDK HandGrabInteractable 事件：
///   抓取時 → 吸附到 mouthAnchor（isKinematic = true，SetParent）
///   放開時 → 解除 Parent，恢復物理重力（isKinematic = false）
///
/// 場景設置：
///   1. 麵包 Prefab 需同時掛 Rigidbody、Collider、Grabbable、
///      HandGrabInteractable、BreadSnapToMouth。
///   2. mouthAnchor → 鵝嘴錨點空物件（在 Inspector 指定，
///      或透過 Tag "MouthAnchor" 自動搜尋）。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HandGrabInteractable))]
public class BreadSnapToMouth : MonoBehaviour
{
    [Header("Snap 目標")]
    [Tooltip("鵝嘴錨點 Transform（Inspector 直接拖入，或留空以 Tag 自動找）")]
    public Transform mouthAnchor;

    [Header("Snap 設定")]
    [Tooltip("吸附時的 Local 位置偏移（微調麵包在嘴裡的位置）")]
    public Vector3 snapLocalOffset = Vector3.zero;

    [Tooltip("吸附動畫時間（0 = 瞬間吸附）")]
    [Range(0f, 0.3f)]
    public float snapDuration = 0.08f;

    // ── 狀態 ──────────────────────────────────────────────────────────────
    private HandGrabInteractable _interactable;
    private Rigidbody _rb;
    private bool _isSnapped;

    // ── Snap 動畫 ─────────────────────────────────────────────────────────
    private float _snapTimer;
    private Vector3 _snapStartPos;
    private Quaternion _snapStartRot;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _interactable = GetComponent<HandGrabInteractable>();

        if (mouthAnchor == null)
        {
            GameObject anchor = GameObject.FindGameObjectWithTag("MouthAnchor");
            if (anchor != null)
                mouthAnchor = anchor.transform;
            else
                Debug.LogWarning("[BreadSnapToMouth] 找不到 mouthAnchor，請在 Inspector 指定或建立 Tag 為 MouthAnchor 的物件。", this);
        }
    }

    void OnEnable()
    {
        _interactable.WhenSelectingInteractorViewAdded   += OnGrabbed;
        _interactable.WhenSelectingInteractorViewRemoved += OnReleased;
    }

    void OnDisable()
    {
        _interactable.WhenSelectingInteractorViewAdded   -= OnGrabbed;
        _interactable.WhenSelectingInteractorViewRemoved -= OnReleased;
    }

    // ── 抓取事件 ──────────────────────────────────────────────────────────
    private void OnGrabbed(IInteractorView interactor)
    {
        if (mouthAnchor == null || _isSnapped) return;

        _isSnapped = true;
        _rb.isKinematic = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        transform.SetParent(mouthAnchor);

        if (snapDuration <= 0f)
        {
            // 瞬間吸附
            transform.localPosition = snapLocalOffset;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            // 緩動吸附動畫
            _snapStartPos = transform.localPosition;
            _snapStartRot = transform.localRotation;
            _snapTimer    = 0f;
        }
    }

    // ── 放開事件 ──────────────────────────────────────────────────────────
    private void OnReleased(IInteractorView interactor)
    {
        // 還有其他 Interactor 仍在抓取，保持吸附
        if (_interactable.SelectingInteractorViews.Any()) return;

        Detach();
    }

    // ── 供 Nest 等外部呼叫（強制解除吸附）─────────────────────────────────
    public void Detach()
    {
        if (!_isSnapped) return;

        _isSnapped = false;
        transform.SetParent(null);
        _rb.isKinematic = false;
    }

    // ── Snap 動畫 Update ──────────────────────────────────────────────────
    void Update()
    {
        if (!_isSnapped || snapDuration <= 0f) return;
        if (_snapTimer >= snapDuration) return;

        _snapTimer += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_snapTimer / snapDuration));

        transform.localPosition = Vector3.Lerp(_snapStartPos, snapLocalOffset, t);
        transform.localRotation = Quaternion.Slerp(_snapStartRot, Quaternion.identity, t);
    }

    // ── Spawn 時隨機外觀（由 Spawner 呼叫）────────────────────────────────
    /// <summary>
    /// 讓麵包在 Spawn 時帶有輕微縮放與旋轉變化，增加視覺多樣性。
    /// </summary>
    public void RandomizeAppearance()
    {
        float scale = Random.Range(0.9f, 1.1f);
        transform.localScale = Vector3.one * scale;
        transform.rotation   = Random.rotation;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (mouthAnchor == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(mouthAnchor.position, 0.03f);
        Gizmos.DrawLine(transform.position, mouthAnchor.position);
    }
#endif
}
