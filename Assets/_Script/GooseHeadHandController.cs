using UnityEngine;
using Oculus.Interaction.Input;

/// <summary>
/// 讓鵝頭 GameObject 跟隨 VR 左手手腕位置與旋轉，
/// 並根據手部開闔（指尖到手腕平均距離）驅動嘴部下顎骨旋轉。
///
/// 使用 Meta SDK Interaction SDK 的 IHand 介面取得手部資料，
/// 不再依賴舊版 OVRHand / OVRSkeleton。
///
/// 場景設置步驟：
///   1. 將此腳本掛在鵝頭根 GameObject 上。
///   2. hand → OVRInteractionComprehensive / OVRHands / LeftHand 上的 Hand 元件。
///   3. lowerJawBone → 鵝嘴下顎骨 Transform（從 FBX Rig 層級中指定）。
///
/// 嘴部旋轉調整：
///   在 Play Mode 中調整 jawOpenRotation，找到自然的開嘴 Euler 角度後記錄。
/// </summary>
public class GooseHeadHandController : MonoBehaviour
{
    // ── 手部追蹤 ──────────────────────────────────────────────────────────
    [Header("手部追蹤來源（Interaction SDK）")]
    [Tooltip("OVRInteractionComprehensive / OVRHands / LeftHand 上的 Hand 元件")]
    [SerializeField]
    private Hand _hand;

    // ── 頭部跟隨 ──────────────────────────────────────────────────────────
    [Header("頭部跟隨設定")]
    [Tooltip("在手腕 Local Space 中的位置偏移（微調鵝頭對準手心位置）")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("在手腕旋轉基礎上疊加的旋轉偏移；Y=180 修正 Z 軸反向")]
    public Vector3 rotationOffset = new Vector3(0f, 180f, 0f);

    [Tooltip("位置平滑速度（0 = 直接對齊）")]
    [Range(0f, 50f)]
    public float positionSmoothing = 20f;

    [Tooltip("旋轉平滑速度")]
    [Range(0f, 50f)]
    public float rotationSmoothing = 20f;

    // ── 嘴部控制 ──────────────────────────────────────────────────────────
    [Header("嘴部骨骼控制")]
    [Tooltip("下顎骨 Transform（從 Goose FBX 骨架中指定）")]
    public Transform lowerJawBone;

    [Tooltip("下顎骨閉嘴時的 Local Euler 旋轉")]
    public Vector3 jawClosedRotation = Vector3.zero;

    [Tooltip("下顎骨開嘴時的 Local Euler 旋轉（建議 Play Mode 中微調 X 軸）")]
    public Vector3 jawOpenRotation = new Vector3(0f, 30f, 0f);

    [Tooltip("嘴部動作平滑速度")]
    [Range(0f, 30f)]
    public float jawSmoothing = 12f;

    // ── 開合偵測 ──────────────────────────────────────────────────────────
    [Header("手部開合偵測（指尖到手腕距離）")]
    [Tooltip("握拳時四指尖到手腕的平均距離（公尺）")]
    public float handClosedDist = 0.06f;

    [Tooltip("完全張開時四指尖到手腕的平均距離（公尺）")]
    public float handOpenDist = 0.13f;

    // ── 除錯 ──────────────────────────────────────────────────────────────
    [Header("除錯")]
    [Tooltip("啟用後在 Console 每幀顯示開合度數值")]
    public bool debugLogOpenness = false;

    [Tooltip("唯讀：目前手部開合度（0=閉合，1=張開）")]
    [Range(0f, 1f)]
    public float debugCurrentOpenness;

    // ── 私有狀態 ──────────────────────────────────────────────────────────
    private float _smoothedOpenness;

    private static readonly HandJointId[] TipJointIds =
    {
        HandJointId.HandIndexTip,
        HandJointId.HandMiddleTip,
        HandJointId.HandRingTip,
        HandJointId.HandPinkyTip,
    };

    // ─────────────────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (_hand == null || !_hand.IsConnected) return;

        UpdateHeadFollow();
        UpdateBeakControl();
    }

    // ── 頭部跟隨 ──────────────────────────────────────────────────────────
    void UpdateHeadFollow()
    {
        // 取得手腕世界座標（HandWristRoot）
        if (!_hand.GetRootPose(out Pose wristPose)) return;

        // 把 positionOffset 轉到手腕的朝向 Local Space
        Vector3    targetPos = wristPose.position + wristPose.rotation * positionOffset;
        Quaternion targetRot = wristPose.rotation * Quaternion.Euler(rotationOffset);

        transform.position = positionSmoothing > 0f
            ? Vector3.Lerp(transform.position, targetPos, positionSmoothing * Time.deltaTime)
            : targetPos;

        transform.rotation = rotationSmoothing > 0f
            ? Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothing * Time.deltaTime)
            : targetRot;
    }

    // ── 嘴部開闔 ──────────────────────────────────────────────────────────
    void UpdateBeakControl()
    {
        if (lowerJawBone == null) return;

        float rawOpenness = CalculateHandOpenness();
        _smoothedOpenness = Mathf.Lerp(_smoothedOpenness, rawOpenness, jawSmoothing * Time.deltaTime);

        debugCurrentOpenness = _smoothedOpenness;
        if (debugLogOpenness)
            Debug.Log($"[GooseHead] openness={_smoothedOpenness:F2}");

        Quaternion closedRot = Quaternion.Euler(jawClosedRotation);
        Quaternion openRot   = Quaternion.Euler(jawOpenRotation);
        lowerJawBone.localRotation = Quaternion.Slerp(closedRot, openRot, _smoothedOpenness);
    }

    // ── 開合度計算 ────────────────────────────────────────────────────────
    /// <summary>
    /// 回傳 0（握拳）到 1（完全張開）的手部開闔程度。
    /// 計算四根指尖到手腕根骨的平均歐氏距離，
    /// 再對 [handClosedDist, handOpenDist] 區間正規化。
    /// </summary>
    float CalculateHandOpenness()
    {
        if (!_hand.GetJointPose(HandJointId.HandWristRoot, out Pose wristPose)) return 0f;

        float totalDist = 0f;
        int   count     = 0;

        foreach (var tipId in TipJointIds)
        {
            if (_hand.GetJointPose(tipId, out Pose tipPose))
            {
                totalDist += Vector3.Distance(tipPose.position, wristPose.position);
                count++;
            }
        }

        if (count == 0) return 0f;

        float avgDist = totalDist / count;
        return Mathf.Clamp01((avgDist - handClosedDist) / (handOpenDist - handClosedDist));
    }

    // ── Gizmos ────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 青色射線：頭部目前朝向
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 0.2f);

        // 執行中才顯示手腕目標位置
        if (Application.isPlaying && _hand != null && _hand.IsConnected)
        {
            if (_hand.GetRootPose(out Pose wristPose))
            {
                Vector3 targetPos = wristPose.position + wristPose.rotation * positionOffset;
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(targetPos, 0.02f);
            }
        }

        if (lowerJawBone != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lowerJawBone.position, 0.015f);
        }
    }
#endif
}
