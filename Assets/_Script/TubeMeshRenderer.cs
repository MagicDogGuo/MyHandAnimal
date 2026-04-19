using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// 沿 SplineContainer 生成管狀 Mesh。
/// 每幀在 LateUpdate 重建，確保脖子曲線即時更新。
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(SplineContainer))]
public class TubeMeshRenderer : MonoBehaviour
{
    [Header("Tube 幾何設定")]
    [Tooltip("Spline 起點半徑")]
    public float startRadius = 0.06f;
    [Tooltip("Spline 終點半徑")]
    public float endRadius = 0.02f;
    public int lengthSegments = 12;
    public int radialSegments = 6;

    [Header("UV 設定")]
    [Tooltip("每公尺重複幾次貼圖，防止脖子拉伸時貼圖變形（對應文件 5-3 節）")]
    public float uvTilingPerMeter = 2f;

    private SplineContainer _splineContainer;
    private MeshFilter _meshFilter;
    private Mesh _mesh;

    void Awake()
    {
        _splineContainer = GetComponent<SplineContainer>();
        _meshFilter = GetComponent<MeshFilter>();

        _mesh = new Mesh();
        _mesh.name = "NeckTubeMesh";
        _meshFilter.mesh = _mesh;
    }

    public void RebuildMesh()
    {
        if (_splineContainer == null) return;

        var spline = _splineContainer.Spline;
        if (spline == null || spline.Count < 2) return;

        float splineLength = spline.GetLength();

        int ringCount = lengthSegments + 1;
        var vertices  = new Vector3[ringCount * (radialSegments + 1)];
        var normals   = new Vector3[ringCount * (radialSegments + 1)];
        var uvs       = new Vector2[ringCount * (radialSegments + 1)];
        var triangles = new int[lengthSegments * radialSegments * 6];

        // ── 頂點與 UV ──────────────────────────────────────────────
        for (int i = 0; i <= lengthSegments; i++)
        {
            float t = i / (float)lengthSegments;

            // Spline.Evaluate 回傳 SplineContainer local space 的座標
            spline.Evaluate(t, out float3 pos, out float3 tangent, out float3 up);

            Vector3 fwd = math.normalizesafe(tangent);
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;

            // 找出 t 兩側的 Knot，對 Rotation 做 Slerp 插值取得 up 向量
            float tScaled = t * (spline.Count - 1);
            int kA = Mathf.Clamp(Mathf.FloorToInt(tScaled), 0, spline.Count - 2);
            int kB = kA + 1;
            float knotBlend = tScaled - kA;
            Quaternion rotA = (Quaternion)spline[kA].Rotation;
            Quaternion rotB = (Quaternion)spline[kB].Rotation;
            Vector3 upV = Quaternion.Slerp(rotA, rotB, knotBlend) * Vector3.up;

            // 正交化：確保 right ⊥ fwd，upV ⊥ right
            Vector3 right = Vector3.Cross(fwd, upV).normalized;
            if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
            upV = Vector3.Cross(right, fwd).normalized;

            // 文件 5-3：依實際弧長計算 UV.y，避免拉伸
            float arcLength = splineLength * t;

            float currentRadius = Mathf.Lerp(startRadius, endRadius, t);

            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = j / (float)radialSegments * Mathf.PI * 2f;
                Vector3 radialDir = Mathf.Cos(angle) * right + Mathf.Sin(angle) * upV;
                int idx = i * (radialSegments + 1) + j;

                vertices[idx] = (Vector3)pos + radialDir * currentRadius;
                normals[idx]  = radialDir;

                // 文件 5-3 的 UV 公式
                uvs[idx] = new Vector2(j / (float)radialSegments, arcLength * uvTilingPerMeter);
            }
        }

        // ── 三角面 ─────────────────────────────────────────────────
        int triIdx = 0;
        for (int i = 0; i < lengthSegments; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int a = i       * (radialSegments + 1) + j;
                int b = (i + 1) * (radialSegments + 1) + j;
                int c = a + 1;
                int d = b + 1;

                triangles[triIdx++] = a;
                triangles[triIdx++] = b;
                triangles[triIdx++] = c;

                triangles[triIdx++] = c;
                triangles[triIdx++] = b;
                triangles[triIdx++] = d;
            }
        }

        _mesh.Clear();
        _mesh.SetVertices(vertices);
        _mesh.SetNormals(normals);
        _mesh.SetUVs(0, uvs);
        _mesh.SetTriangles(triangles, 0);
        _mesh.RecalculateBounds();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        radialSegments = Mathf.Max(3, radialSegments);
        lengthSegments = Mathf.Max(1, lengthSegments);
        startRadius    = Mathf.Max(0.001f, startRadius);
        endRadius      = Mathf.Max(0.001f, endRadius);
    }
#endif
}
