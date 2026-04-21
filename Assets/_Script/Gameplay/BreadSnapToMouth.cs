using System.Collections;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

/// <summary>
/// 掛在每個麵包 Prefab 上。
/// 監聽 HandGrabInteractable 事件：
///   抓取時 → SetParent 到 mouthAnchor，isKinematic = true
///   放開時 → SetParent(null)，isKinematic = false（恢復物理）
///
/// LateUpdate 每幀強制 localPosition = snapLocalOffset，
/// 防止 ISDK GrabFreeTransformer 每幀覆寫 world position 造成漂移。
///
/// 麵包 Prefab 需要：
///   Rigidbody、Collider、Grabbable、HandGrabInteractable、BreadSnapToMouth
///
/// mouthAnchor：
///   鵝頭 GameObject 下的子空物件，對齊嘴尖位置。
///   在 Inspector 直接拖入，或建立 Tag "MouthAnchor" 自動搜尋。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HandGrabInteractable))]
public class BreadSnapToMouth : MonoBehaviour
{
    [Header("抓取限制")]
    [Tooltip("限制只有指定的手才能抓取麵包")]
    public Handedness allowedHand = Handedness.Left;

    [Header("Snap 目標")]
    [Tooltip("鵝嘴尖端錨點（Inspector 直接拖入，或留空以 Tag 自動找）")]
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

    /// <summary>麵包目前是否被吸附在嘴上（供 Nest 判斷「放手才入巢」使用）。</summary>
    public bool IsHeld => _isSnapped;

    // ── Snap 動畫 ─────────────────────────────────────────────────────────
    private float _snapTimer;
    private Vector3 _snapStartLocalPos;
    private Quaternion _snapStartLocalRot;

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
        // 非允許的手 → 下一幀強制 release，不執行 snap
        if (interactor is HandGrabInteractor hgi &&
            hgi.Hand?.Handedness != allowedHand)
        {
            StartCoroutine(ForceReleaseNextFrame(hgi));
            return;
        }

        if (mouthAnchor == null || _isSnapped) return;

        _isSnapped = true;
        _rb.isKinematic    = true;
        _rb.linearVelocity  = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        transform.SetParent(mouthAnchor);
        _snapStartLocalPos = transform.localPosition;
        _snapStartLocalRot = transform.localRotation;
        _snapTimer         = 0f;
    }

    // ── 非允許手的強制釋放（延一幀避免事件重入）─────────────────────────────
    private IEnumerator ForceReleaseNextFrame(HandGrabInteractor interactor)
    {
        yield return null;
        interactor.ForceRelease();
    }

    // ── 放開事件 ──────────────────────────────────────────────────────────
    private void OnReleased(IInteractorView interactor)
    {
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

    // ── LateUpdate：動畫 + 強制鎖定位置（對抗 ISDK GrabTransformer）──────
    void LateUpdate()
    {
        if (!_isSnapped) return;

        if (snapDuration > 0f && _snapTimer < snapDuration)
        {
            _snapTimer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_snapTimer / snapDuration));
            transform.localPosition = Vector3.Lerp(_snapStartLocalPos, snapLocalOffset, t);
            transform.localRotation = Quaternion.Slerp(_snapStartLocalRot, Quaternion.identity, t);
        }
        else
        {
            // 動畫結束後每幀強制保持，防止 ISDK 拉走
            transform.localPosition = snapLocalOffset;
            transform.localRotation = Quaternion.identity;
        }
    }

    // ── Spawn 時隨機外觀（由 Spawner 呼叫）────────────────────────────────
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
