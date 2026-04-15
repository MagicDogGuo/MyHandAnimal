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

    [Header("脖子柔軟度")]
    [Tooltip("中間控制點數量（越多越軟，0 = 直線）")]
    public int midKnotCount = 2;

    [Tooltip("弧形偏移量（正值 = 朝 arcAxis 方向拱起，負值 = 反向）")]
    public float arcHeight = 0.15f;

    [Tooltip("弧形偏移的方向（預設向下，模擬重力垂墜）")]
    public Vector3 arcAxis = Vector3.down;

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

    void LateUpdate()
    {
        if (duckHead == null || duckBody == null) return;

        // midKnotCount 被調整時，重建 Knot 數量
        int requiredTotal = midKnotCount + 2;
        if (_lastKnotCount != requiredTotal)
            RebuildKnotCount();

        UpdateSplineKnots();
        HandleNeckPull();
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
            spline.Add(new BezierKnot(float3.zero));

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

        Vector3 bodyLocal = transform.InverseTransformPoint(duckBody.position);
        Vector3 headLocal = transform.InverseTransformPoint(duckHead.position);

        // 切線長度 = 段距的 40%，讓曲線自然彎曲
        Vector3 seg = (headLocal - bodyLocal) / (total - 1);
        Vector3 tangent = seg * 0.4f;

        // arcAxis 轉成 local space
        Vector3 arcAxisLocal = transform.InverseTransformDirection(arcAxis.normalized);

        for (int i = 0; i < total; i++)
        {
            float t = i / (float)(total - 1);
            Vector3 pos = Vector3.Lerp(bodyLocal, headLocal, t);

            // sin(t*π)：兩端為 0，中央為 1，形成自然弧形
            float sinWeight = Mathf.Sin(t * Mathf.PI);
            pos += arcAxisLocal * (arcHeight * sinWeight);

            spline.SetKnot(i, new BezierKnot(
                (float3)(Vector3)pos,
                (float3)(-tangent),
                (float3)(tangent)
            ));
        }
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
