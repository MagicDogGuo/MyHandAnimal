using UnityEngine;
using Oculus.Interaction.Input;

/// <summary>
/// 偵測右手「手槍手勢」（食指伸直 + 中/無名/小指握拳 + 大拇指抬起），
/// 並讓 [BuildingBlock] Camera Rig 往食指指向的方向移動。
///
/// 手槍手勢判斷條件：
///   - 食指未捏合（IndexPinchStrength 低）= 食指伸直
///   - 中指、無名指、小指捏合值高           = 三指握拳
///   - 大拇指不捏 Index                   = 拇指抬起（自然成立於食指伸直時）
///
/// 方向來源：
///   HandIndex1（近端指骨）→ HandIndexTip（指尖）的世界向量，
///   即實際食指指向的方向。
///
/// 場景設定：
///   1. 將此腳本掛在 [BuildingBlock] Camera Rig GameObject 上。
///   2. hand → OVRInteractionComprehensive / OVRHands / RightHand 上的 Hand 元件。
/// </summary>
public class GunGestureLocomotion : MonoBehaviour
{
    // ── 手部追蹤 ──────────────────────────────────────────────────────────
    [Header("手部追蹤來源（Interaction SDK）")]
    [Tooltip("右手的 Hand 元件（OVRInteractionComprehensive / OVRHands / RightHand）")]
    [SerializeField]
    private Hand _hand;

    // ── 手勢偵測門檻 ──────────────────────────────────────────────────────
    [Header("手勢偵測門檻")]
    [Tooltip("食指 PinchStrength 低於此值 = 食指伸直。建議 0.15~0.25。")]
    [Range(0f, 1f)]
    public float indexExtendedThreshold = 0.2f;

    [Tooltip("其他三指（中/無名/小指）PinchStrength 均高於此值 = 握拳。建議 0.5~0.7。")]
    [Range(0f, 1f)]
    public float fingersCurledThreshold = 0.55f;

    [Tooltip("需要同時滿足幾幀的手勢才啟動移動（防止誤觸）")]
    [Range(0, 10)]
    public int activationFrames = 3;

    // ── 移動設定 ──────────────────────────────────────────────────────────
    [Header("移動設定")]
    [Tooltip("最大移動速度（公尺/秒）")]
    public float moveSpeed = 2.5f;

    [Tooltip("加速時間（秒）：值越小啟動越快")]
    [Range(0f, 2f)]
    public float accelerationTime = 0.3f;

    [Tooltip("減速時間（秒）：值越小停止越快")]
    [Range(0f, 2f)]
    public float decelerationTime = 0.2f;

    [Tooltip("啟用後只在水平面（XZ）移動，忽略食指的上下傾斜")]
    public bool horizontalOnly = true;

    // ── 除錯 ──────────────────────────────────────────────────────────────
    [Header("除錯")]
    [Tooltip("啟用後在 Console 輸出手勢偵測資訊")]
    public bool debugLog = false;

    [Tooltip("唯讀：目前是否偵測到手槍手勢")]
    public bool debugIsGunGesture;

    [Tooltip("唯讀：目前速度比例（0~1）")]
    [Range(0f, 1f)]
    public float debugSpeedRatio;

    // ── 私有狀態 ──────────────────────────────────────────────────────────
    private float _currentSpeedRatio = 0f;   // 0~1 的速度比例
    private int   _gestureFrameCount = 0;    // 連續偵測到手勢的幀數
    private bool  _isMoving = false;

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_hand == null || !_hand.IsConnected)
        {
            DecelerateAndStop();
            return;
        }

        bool gestureDetected = DetectGunGesture();

        // 需連續 activationFrames 幀才正式啟動，避免瞬間誤觸
        if (gestureDetected)
            _gestureFrameCount = Mathf.Min(_gestureFrameCount + 1, activationFrames + 1);
        else
            _gestureFrameCount = 0;

        _isMoving = _gestureFrameCount >= activationFrames;
        debugIsGunGesture = _isMoving;

        if (_isMoving)
        {
            // 加速至 1
            float accelRate = accelerationTime > 0f ? Time.deltaTime / accelerationTime : 1f;
            _currentSpeedRatio = Mathf.MoveTowards(_currentSpeedRatio, 1f, accelRate);

            Vector3 direction = GetIndexFingerDirection();
            if (debugLog)
                Debug.Log($"[GunGesture] Moving direction={direction}, speed={_currentSpeedRatio * moveSpeed:F2}");

            transform.position += direction * (_currentSpeedRatio * moveSpeed * Time.deltaTime);
        }
        else
        {
            DecelerateAndStop();
        }

        debugSpeedRatio = _currentSpeedRatio;
    }

    // ── 手勢偵測 ──────────────────────────────────────────────────────────
    /// <summary>
    /// 手槍手勢 = 食指伸直（低 Pinch）+ 中/無名/小指握拳（高 Pinch）。
    /// </summary>
    bool DetectGunGesture()
    {
        float indexPinch  = _hand.GetFingerPinchStrength(HandFinger.Index);
        float middlePinch = _hand.GetFingerPinchStrength(HandFinger.Middle);
        float ringPinch   = _hand.GetFingerPinchStrength(HandFinger.Ring);
        float pinkyPinch  = _hand.GetFingerPinchStrength(HandFinger.Pinky);

        bool indexExtended = indexPinch  < indexExtendedThreshold;
        bool othersCurled  = middlePinch > fingersCurledThreshold &&
                             ringPinch   > fingersCurledThreshold &&
                             pinkyPinch  > fingersCurledThreshold;

        if (debugLog)
            Debug.Log($"[GunGesture] index={indexPinch:F2} mid={middlePinch:F2} ring={ringPinch:F2} pinky={pinkyPinch:F2} | ext={indexExtended} curled={othersCurled}");

        return indexExtended && othersCurled;
    }

    // ── 食指方向計算 ──────────────────────────────────────────────────────
    /// <summary>
    /// 取得食指近端（Index1）→ 指尖（IndexTip）的世界向量作為瞄準方向。
    /// </summary>
    Vector3 GetIndexFingerDirection()
    {
        bool hasProximal = _hand.GetJointPose(HandJointId.HandIndex1, out Pose proximalPose);
        bool hasTip      = _hand.GetJointPose(HandJointId.HandIndexTip, out Pose tipPose);

        if (hasProximal && hasTip)
        {
            Vector3 rawDir = (tipPose.position - proximalPose.position);
            if (rawDir.sqrMagnitude > 0.0001f)
            {
                rawDir.Normalize();

                if (horizontalOnly)
                {
                    rawDir.y = 0f;
                    if (rawDir.sqrMagnitude < 0.0001f)
                        rawDir = transform.forward; // fallback：正前方
                    else
                        rawDir.Normalize();
                }

                return rawDir;
            }
        }

        // Fallback：使用 PointerPose 方向
        if (_hand.IsPointerPoseValid && _hand.GetPointerPose(out Pose pointerPose))
        {
            Vector3 fallback = pointerPose.forward;
            if (horizontalOnly) { fallback.y = 0f; fallback.Normalize(); }
            return fallback;
        }

        // 最終 fallback：Camera Rig 的正前方
        return transform.forward;
    }

    // ── 減速 ──────────────────────────────────────────────────────────────
    void DecelerateAndStop()
    {
        if (_currentSpeedRatio <= 0f) return;
        float decelRate = decelerationTime > 0f ? Time.deltaTime / decelerationTime : 1f;
        _currentSpeedRatio = Mathf.MoveTowards(_currentSpeedRatio, 0f, decelRate);
        debugSpeedRatio = _currentSpeedRatio;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || _hand == null || !_hand.IsConnected) return;

        if (_hand.GetJointPose(HandJointId.HandIndex1, out Pose proximalPose) &&
            _hand.GetJointPose(HandJointId.HandIndexTip, out Pose tipPose))
        {
            // 綠色（啟動）或灰色（未啟動）的食指方向射線
            Gizmos.color = _isMoving ? Color.green : Color.gray;
            Vector3 dir = (tipPose.position - proximalPose.position).normalized;
            Gizmos.DrawRay(proximalPose.position, dir * 0.5f);
            Gizmos.DrawWireSphere(tipPose.position, 0.01f);
        }

        // Camera Rig 朝向
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 0.3f);
    }
#endif
}
