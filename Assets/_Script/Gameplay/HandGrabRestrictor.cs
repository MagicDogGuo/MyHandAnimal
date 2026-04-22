using System.Collections;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

/// <summary>
/// 掛在任何可抓取物件上，單純限制只有指定的手才能抓取。
/// 需搭配 HandGrabInteractable 使用。
/// </summary>
[RequireComponent(typeof(HandGrabInteractable))]
public class HandGrabRestrictor : MonoBehaviour
{
    public enum AllowedHand { Left, Right, Both }

    [Header("抓取手限制")]
    [Tooltip("Left = 只允許左手；Right = 只允許右手；Both = 兩手都可以")]
    public AllowedHand allowedHand = AllowedHand.Left;

    private HandGrabInteractable _interactable;

    void Awake()
    {
        _interactable = GetComponent<HandGrabInteractable>();
    }

    void OnEnable()
    {
        _interactable.WhenSelectingInteractorViewAdded += OnGrabbed;
    }

    void OnDisable()
    {
        _interactable.WhenSelectingInteractorViewAdded -= OnGrabbed;
    }

    private void OnGrabbed(IInteractorView interactor)
    {
        if (allowedHand == AllowedHand.Both) return;

        if (interactor is HandGrabInteractor hgi)
        {
            Handedness grabHand = hgi.Hand?.Handedness ?? Handedness.Left;
            bool isAllowed = allowedHand == AllowedHand.Left
                ? grabHand == Handedness.Left
                : grabHand == Handedness.Right;

            if (!isAllowed)
                StartCoroutine(ForceReleaseNextFrame(hgi));
        }
    }

    private IEnumerator ForceReleaseNextFrame(HandGrabInteractor interactor)
    {
        yield return null;
        interactor.ForceRelease();
    }
}
