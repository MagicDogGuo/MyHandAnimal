using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// 每幀把 Spline 兩端 Knot 對齊 DuckHead / DuckBody，
/// 中間插入 midKnotCount 個控制點讓脖子更柔軟，
/// 並在脖子超過 maxNeckLength 時對 Rigidbody 施拉力。
/// 對應文件驗證 Checklist：
///   ✓ Spline 兩端 Knot 跟著 Head/Body Transform 更新
///   ✓ 脖子長度超過 maxNeckLength 時身體正確被拉動
///   ✓ Rigidbody Drag 設定讓身體停止時不抖動（Drag 設在 DuckBody 的 Rigidbody 上）
/// </summary>
[RequireComponent(typeof(SplineContainer))]
public class NeckSplineController : MonoBehaviour
{
    [Header("連結物件")]
    [Tooltip("鴨頭 Transform（Spline 起點）")]
    public Transform duckHead;

    [Tooltip("鴨身體 Transform（Spline 終點）")]
    public Transform duckBody;

    [Header("Knot 端點位置")]
    [Tooltip("第一個 Knot 的位置來源（未設定則使用 duckBody）")]
    public Transform knotStart;

    [Tooltip("最後一個 Knot 的位置來源（未設定則使用 duckHead）")]
    public Transform knotEnd;

    [Header("脖子柔軟度")]
    [Tooltip("中間控制點數量（越多越軟，0 = 直線）")]
    public int midKnotCount = 2;

    [Tooltip("弧形偏移量（正值 = 朝 arcAxis 方向拱起，負值 = 反向）")]
    public float arcHeight = 0.15f;

    [Tooltip("弧形偏移的方向（預設向下，模擬重力垂墜）")]
    public Vector3 arcAxis = Vector3.down;

    [Header("身體旋轉")]
    [Tooltip("Body Y 軸朝向 Head 的旋轉速度（度/秒），0 = 關閉")]
    public float bodyRotateSpeed = 90f;

    [Header("脖子限制")]
    [Tooltip("脖子最大長度（公尺）；超過時對身體施拉力")]
    public float maxNeckLength = 1.2f;

    [Tooltip("拉力係數，乘上超出距離後加到 Rigidbody")]
    public float pullForce = 8f;

    private SplineContainer _splineContainer;
    private Rigidbody _bodyRb;
    private int _lastKnotCount = -1;

    void Awake()
    {
        _splineContainer = GetComponent<SplineContainer>();

        if (duckBody != null)
            _bodyRb = duckBody.GetComponent<Rigidbody>();

        RebuildKnotCount();
    }

    void FixedUpdate()
    {
        if (duckHead == null || duckBody == null) return;

        UpdateBodyRotation();
        HandleNeckPull();
    }

    void LateUpdate()
    {
        if (duckHead == null || duckBody == null) return;

        // midKnotCount 被調整時，重建 Knot 數量
        int requiredTotal = midKnotCount + 2;
        if (_lastKnotCount != requiredTotal)
            RebuildKnotCount();

        UpdateSplineKnots();
    }

    /// <summary>
    /// 依 midKnotCount 重設 Spline 的 Knot 總數：
    /// 總共 = 2（兩端）+ midKnotCount（中間）
    /// </summary>
    void RebuildKnotCount()
    {
        var spline = _splineContainer.Spline;
        int required = midKnotCount + 2;

        spline.Clear();
        for (int i = 0; i < required; i++)
            spline.Add(new BezierKnot(float3.zero), TangentMode.AutoSmooth);

        _lastKnotCount = required;
    }

    /// <summary>
    /// 更新所有 Knot 的位置。
    /// 兩端對齊 Body / Head，中間點沿直線均勻插值，
    /// 切線使用 AutoSmooth 讓曲線自然（不需要手動設定）。
    /// </summary>
    void UpdateSplineKnots()
    {
        var spline = _splineContainer.Spline;
        int total = spline.Count;

        Transform startTrans = knotStart != null ? knotStart : duckBody;
        Transform endTrans   = knotEnd   != null ? knotEnd   : duckHead;

        Vector3 bodyLocal = transform.InverseTransformPoint(startTrans.position);
        Vector3 headLocal = transform.InverseTransformPoint(endTrans.position);

        // arcAxis 轉成 local space
        Vector3 arcAxisLocal = transform.InverseTransformDirection(arcAxis.normalized);

        for (int i = 0; i < total; i++)
        {
            float t = i / (float)(total - 1);
            Vector3 pos = Vector3.Lerp(bodyLocal, headLocal, t);

            // sin(t*π)：兩端為 0，中央為 1，形成自然弧形
            float sinWeight = Mathf.Sin(t * Mathf.PI);
            pos += arcAxisLocal * (arcHeight * sinWeight);

            spline.SetKnot(i, new BezierKnot((float3)(Vector3)pos));
            spline.SetTangentMode(i, TangentMode.AutoSmooth);
        }
    }

    /// <summary>
    /// 依 Head 的水平位置，讓 Body 在 Y 軸上朝向 Head。
    /// 使用 angularVelocity 驅動，Freeze Rotation X/Z 才能正確生效；
    /// MoveRotation 會繞過 Freeze Constraints，所以不適用。
    /// </summary>
    void UpdateBodyRotation()
    {
        if (_bodyRb == null || bodyRotateSpeed <= 0f) return;

        Vector3 dir = duckHead.position - duckBody.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            Vector3 av = _bodyRb.angularVelocity;
            av.y = 0f;
            _bodyRb.angularVelocity = av;
            return;
        }

        float targetY   = Quaternion.LookRotation(-dir).eulerAngles.y;
        float currentY  = _bodyRb.rotation.eulerAngles.y;
        float angleDiff = Mathf.DeltaAngle(currentY, targetY);

        // 比例控制：角度差越大轉越快，上限為 bodyRotateSpeed（度/秒）
        float maxRadSec   = bodyRotateSpeed * Mathf.Deg2Rad;
        float yAngularVel = Mathf.Clamp(angleDiff * Mathf.Deg2Rad * 10f, -maxRadSec, maxRadSec);

        // 只寫 Y 軸角速度；X/Z 由 Freeze Constraints 維持在 0
        _bodyRb.angularVelocity = new Vector3(0f, yAngularVel, 0f);
    }

    /// <summary>
    /// 超過 maxNeckLength 時，依超出量對 Rigidbody 施加拉力。
    /// 停止時靠 Rigidbody.Drag 自然減速（不抖動）。
    /// </summary>
    void HandleNeckPull()
    {
        if (_bodyRb == null) return;

        float dist = Vector3.Distance(duckHead.position, duckBody.position);
        if (dist > maxNeckLength)
        {
            Vector3 pullDir = (duckHead.position - duckBody.position).normalized;
            float overStretch = dist - maxNeckLength;
            _bodyRb.AddForce(pullDir * pullForce * overStretch, ForceMode.Force);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        midKnotCount = Mathf.Max(0, midKnotCount);
        if (arcAxis == Vector3.zero) arcAxis = Vector3.down;
    }

    void OnDrawGizmosSelected()
    {
        if (duckHead == null || duckBody == null) return;

        float dist = Vector3.Distance(duckHead.position, duckBody.position);

        // 綠色 = 正常；紅色 = 超過 maxNeckLength
        Gizmos.color = dist > maxNeckLength ? Color.red : Color.green;
        Gizmos.DrawLine(duckHead.position, duckBody.position);

        // 以 Body 為中心畫出最大長度範圍球
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(duckBody.position, maxNeckLength);
    }
#endif
}
