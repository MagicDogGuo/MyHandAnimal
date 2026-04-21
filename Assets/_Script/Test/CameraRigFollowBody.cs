using UnityEngine;

/// <summary>
/// 讓 [BuildingBlock] Camera Rig 跟隨 Goose Body 的世界位移。
///
/// 原理：每幀計算 Body 的位移 Delta，並將相同的 Delta 套用到 Camera Rig，
/// 使玩家的 Play Space 隨鵝身體一起平移，不影響 HMD 的本地視角旋轉。
///
/// 場景設定：
///   1. 將此腳本掛在 [BuildingBlock] Camera Rig GameObject 上。
///   2. gooseBody → 鵝身體的 Transform（有 Rigidbody 的那個物件）。
///
/// 常見配置：
///   - 只追水平移動：followY = false（預設）
///   - 超過一定距離才跟：deadZone > 0（避免微抖）
///   - 平滑追蹤：smoothSpeed > 0；直接跟隨：smoothSpeed = 0
/// </summary>
public class CameraRigFollowBody : MonoBehaviour
{
    [Header("目標物件")]
    [Tooltip("鵝身體的 Transform（NeckSplineController 中的 duckBody）")]
    public Transform gooseBody;

    [Header("跟隨軸向")]
    [Tooltip("跟隨 X / Z 軸水平移動（建議開啟）")]
    public bool followXZ = true;

    [Tooltip("跟隨 Y 軸垂直移動（若場景有高低落差時開啟）")]
    public bool followY = false;

    [Header("死區")]
    [Tooltip("Body 移動超過此距離才更新 Camera Rig（公尺）。0 = 每幀都跟。")]
    [Range(0f, 1f)]
    public float deadZone = 0f;

    [Header("平滑")]
    [Tooltip("Camera Rig 位置平滑速度。0 = 直接跟隨（無延遲）。")]
    [Range(0f, 30f)]
    public float smoothSpeed = 0f;

    // Camera Rig 目標位置（平滑模式使用）
    private Vector3 _targetPosition;

    // 上一幀 Body 在世界座標的位置（只記錄有效軸向）
    private Vector3 _lastBodyPosition;

    // 是否已完成初始化
    private bool _initialized = false;

    void OnEnable()
    {
        // 延遲初始化：等待 gooseBody 準備好
        _initialized = false;
    }

    void LateUpdate()
    {
        if (gooseBody == null) return;

        if (!_initialized)
        {
            Initialize();
            return;
        }

        Vector3 currentBodyPos = gooseBody.position;

        // 計算 Body 的 delta，只取有效軸
        Vector3 delta = currentBodyPos - _lastBodyPosition;
        if (!followXZ) { delta.x = 0f; delta.z = 0f; }
        if (!followY)  { delta.y = 0f; }

        // 死區判定：delta 太小就略過
        float horizontalMove = followXZ ? new Vector2(delta.x, delta.z).magnitude : 0f;
        float verticalMove   = followY  ? Mathf.Abs(delta.y) : 0f;
        float totalMove      = Mathf.Max(horizontalMove, verticalMove);

        if (totalMove > deadZone)
        {
            _targetPosition += delta;
            _lastBodyPosition = currentBodyPos;
        }

        // 套用到 Camera Rig
        if (smoothSpeed > 0f)
            transform.position = Vector3.Lerp(transform.position, _targetPosition, smoothSpeed * Time.deltaTime);
        else
            transform.position = _targetPosition;
    }

    void Initialize()
    {
        _lastBodyPosition = gooseBody.position;
        _targetPosition   = transform.position;
        _initialized      = true;
    }

    /// <summary>
    /// 手動重置跟隨基準點（例如：場景切換或 Body 被傳送後呼叫）。
    /// </summary>
    public void ResetFollowOrigin()
    {
        if (gooseBody == null) return;
        _lastBodyPosition = gooseBody.position;
        _targetPosition   = transform.position;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (gooseBody == null) return;

        // 黃線：Camera Rig → Body
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, gooseBody.position);

        // 白球：Body 目前位置
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(gooseBody.position, 0.05f);

        // 死區範圍圈（XZ 平面）
        if (deadZone > 0f)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(gooseBody.position, deadZone);
        }
    }
#endif
}
