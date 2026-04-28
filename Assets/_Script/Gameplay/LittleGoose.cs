using System.Collections;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

/// <summary>
/// 小鵝實體：供 Nest 辨識、計分去重、與釋放嘴／手攜帶狀態。
/// 可選與麵包相同掛 <see cref="BreadSnapToMouth"/> 做嘴部吸附。
/// </summary>
public class LittleGoose : MonoBehaviour
{
    [Header("關卡計分")]
    [Tooltip("關閉時不計入巢的「小鵝送達」條件（例：第零關巢內靜態裝飾鵝）。")]
    public bool countsTowardsGoal = true;

    HandGrabInteractable _handGrab;
    BreadSnapToMouth     _mouthSnap;

    public bool IsHeld
    {
        get
        {
            if (_mouthSnap != null) return _mouthSnap.IsHeld;
            if (_handGrab != null) return _handGrab.SelectingInteractorViews.Any();
            return false;
        }
    }

    void Awake()
    {
        _handGrab  = GetComponent<HandGrabInteractable>();
        _mouthSnap = GetComponent<BreadSnapToMouth>();
    }

    void OnEnable()
    {
        if (_mouthSnap == null && _handGrab != null)
            _handGrab.WhenSelectingInteractorViewAdded += OnHandGrabbedForPickupSfx;
    }

    void OnDisable()
    {
        if (_handGrab != null)
            _handGrab.WhenSelectingInteractorViewAdded -= OnHandGrabbedForPickupSfx;
    }

    /// <summary>
    /// 無嘴部吸附時，抓取成功與否需等下一幀確認（與 HandGrabRestrictor 放開錯手對齊）。
    /// </summary>
    void OnHandGrabbedForPickupSfx(IInteractorView interactor)
    {
        StartCoroutine(PlayPickupSfxIfStillHeldNextFrame());
    }

    IEnumerator PlayPickupSfxIfStillHeldNextFrame()
    {
        yield return null;
        if (_handGrab == null || !_handGrab.SelectingInteractorViews.Any()) yield break;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayLittleGoosePickup();
    }

    /// <summary>入巢前呼叫：解除嘴部吸附、嘗試放開手抓、解除 Parent。</summary>
    public void DetachFromCarry()
    {
        if (_mouthSnap != null) _mouthSnap.Detach();

        if (_handGrab != null && _handGrab.SelectingInteractorViews.Any())
            StartCoroutine(ReleaseHandsNextFrame());

        if (transform.parent != null) transform.SetParent(null);
    }

    IEnumerator ReleaseHandsNextFrame()
    {
        yield return null;
        if (_handGrab == null) yield break;
        var views = _handGrab.SelectingInteractorViews.ToList();
        foreach (IInteractorView v in views)
        {
            if (v is HandGrabInteractor hgi) hgi.ForceRelease();
        }
    }
}
