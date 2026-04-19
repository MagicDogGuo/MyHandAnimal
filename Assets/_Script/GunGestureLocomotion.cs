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
    [Tooltip("食指 PinchStrength 低於此值 = 食指伸直（GetFingerPinchStrength：指尖靠近拇指程度）")]
    [Range(0f, 1f)]
    public float indexExtendedThreshold = 0.2f;

    [Tooltip("中/無名/小指尖到手腕距離短於此值（公尺）= 握拳。握拳約 0.07~0.09，伸直約 0.14~0.18。")]
    [Range(0.03f, 0.20f)]
    public float fingerCurledMaxDist = 0.09f;

    [Tooltip("食指指尖到手腕距離長於此值（公尺）= 食指伸直。比讚時食指捲入拳頭，距離約 0.07~0.09，伸直約 0.14~0.18。")]
    [Range(0.05f, 0.20f)]
    public float indexExtendedMinDist = 0.12f;

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
    private float _currentSpeedRatio = 0f;
    private int   _gestureFrameCount = 0;
    private bool  _isMoving = false;

#if UNITY_EDITOR
    [Header("Editor 除錯（僅在 Editor 中可用）")]
    [Tooltip("唯讀：P 鍵切換強制手槍手勢 ON/OFF")]
    public bool debugForceGunGesture = false;
#endif

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
        {
            debugForceGunGesture = !debugForceGunGesture;
            Debug.Log($"[GunGesture] Editor 強制手槍手勢：{(debugForceGunGesture ? "ON" : "OFF")}");
        }

        bool handReady = debugForceGunGesture || (_hand != null && _hand.IsConnected);
#else
        bool handReady = _hand != null && _hand.IsConnected;
#endif
        if (!handReady)
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
    /// 手槍手勢 = 食指伸直（低 PinchStrength）+ 中/無名/小指握拳（指尖到手腕距離短）。
    /// GetFingerPinchStrength 只適合偵測「捏拇指」，不適合偵測「往手心捲」，
    /// 因此中/無/小指改用指尖到手腕距離判斷。
    /// </summary>
    bool DetectGunGesture()
    {
#if UNITY_EDITOR
        if (debugForceGunGesture) return true;
#endif
        float indexPinch = _hand.GetFingerPinchStrength(HandFinger.Index);
        bool  indexLowPinch = indexPinch < indexExtendedThreshold;

        // 食指指尖到手腕距離夠遠 = 食指真正伸直（排除比讚：食指捲入拳頭，距離短）
        bool indexFarFromWrist = IsFingerExtendedByDist(HandJointId.HandIndexTip);
        bool indexExtended = indexLowPinch && indexFarFromWrist;

        bool middleCurled = IsFingerCurledByDist(HandJointId.HandMiddleTip);
        bool ringCurled   = IsFingerCurledByDist(HandJointId.HandRingTip);
        bool pinkyCurled  = IsFingerCurledByDist(HandJointId.HandPinkyTip);
        bool othersCurled = middleCurled && ringCurled && pinkyCurled;

        if (debugLog)
        {
            _hand.GetJointPose(HandJointId.HandWristRoot, out Pose wrist);
            _hand.GetJointPose(HandJointId.HandIndexTip,  out Pose idxTip);
            _hand.GetJointPose(HandJointId.HandMiddleTip, out Pose midTip);
            _hand.GetJointPose(HandJointId.HandRingTip,   out Pose ringTip);
            _hand.GetJointPose(HandJointId.HandPinkyTip,  out Pose pinkyTip);
            float idxD   = Vector3.Distance(idxTip.position,   wrist.position);
            float midD   = Vector3.Distance(midTip.position,   wrist.position);
            float ringD  = Vector3.Distance(ringTip.position,  wrist.position);
            float pinkyD = Vector3.Distance(pinkyTip.position, wrist.position);
            Debug.Log($"[GunGesture] indexPinch={indexPinch:F2}(lowPinch={indexLowPinch}) " +
                      $"idxDist={idxD:F3}(far={indexFarFromWrist}) | " +
                      $"midDist={midD:F3} ringDist={ringD:F3} pinkyDist={pinkyD:F3} | curled={othersCurled}");
        }

        return indexExtended && othersCurled;
    }

    /// <summary>
    /// 指尖到手腕距離小於 fingerCurledMaxDist 則視為該指握拳。
    /// </summary>
    bool IsFingerCurledByDist(HandJointId tipJointId)
    {
        if (!_hand.GetJointPose(HandJointId.HandWristRoot, out Pose wrist)) return false;
        if (!_hand.GetJointPose(tipJointId, out Pose tip)) return false;
        return Vector3.Distance(tip.position, wrist.position) < fingerCurledMaxDist;
    }

    /// <summary>
    /// 指尖到手腕距離大於 indexExtendedMinDist 則視為該指伸直。
    /// 用於排除「比讚」：食指捲進拳頭時距離短，不應判定為伸直。
    /// </summary>
    bool IsFingerExtendedByDist(HandJointId tipJointId)
    {
        if (!_hand.GetJointPose(HandJointId.HandWristRoot, out Pose wrist)) return false;
        if (!_hand.GetJointPose(tipJointId, out Pose tip)) return false;
        return Vector3.Distance(tip.position, wrist.position) > indexExtendedMinDist;
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
