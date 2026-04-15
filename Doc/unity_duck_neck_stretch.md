# 🦆 Unity 鴨子脖子伸縮實作指南
> 基於 *Your Hand Is A Duck (VR)* 的技術分析，使用 Unity Splines + Procedural Tube Mesh

---

## 目錄
1. [技術架構總覽](#技術架構總覽)
2. [方法一：Unity Splines + Tube Mesh（推薦）](#方法一unity-splines--tube-mesh推薦)
3. [方法二：LineRenderer（快速原型）](#方法二linerenderer快速原型)
4. [方法三：多段 Cylinder + LookAt](#方法三多段-cylinder--lookat)
5. [身體 Follow 頭部的物理邏輯](#身體-follow-頭部的物理邏輯)
6. [方法比較表](#方法比較表)

---

## 技術架構總覽

```
[玩家手部 / XR 控制器]
        │
        ▼
  [頭部 Transform]  ◄─── 鴨頭位置（跟著手走）
        │
        │   Unity Spline（動態彎曲曲線）
        │   用 Tube Mesh 渲染成脖子
        │
        ▼
  [身體 Rigidbody]  ◄─── 物理模擬，超過最大距離才被拉著走
```

原作者在 itch.io 頁面明確提到使用了：
- `Unity XR Hands package` — 手部追蹤
- `Unity Splines package` — 脖子曲線

---

## 方法一：Unity Splines + Tube Mesh（推薦）

### 安裝套件
```
Package Manager → Add by name:
  com.unity.splines
```

### Step 1：建立場景結構

```
Duck (GameObject)
├── DuckHead         ← 跟著手/控制器走
├── DuckBody         ← 有 Rigidbody
└── Neck             ← 掛載 NeckRenderer 腳本
     └── SplineContainer (Component)
```

### Step 2：NeckSplineController.cs

```csharp
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

public class NeckSplineController : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform;
    public Transform bodyTransform;
    public SplineContainer splineContainer;

    [Header("Settings")]
    public int midKnotCount = 2;  // 中間控制點數量（越多越軟）

    private Spline _spline;

    void Start()
    {
        _spline = splineContainer.Spline;
        InitializeKnots();
    }

    void InitializeKnots()
    {
        _spline.Clear();

        // 身體端
        _spline.Add(new BezierKnot(bodyTransform.localPosition));

        // 中間控制點
        for (int i = 1; i <= midKnotCount; i++)
        {
            float t = i / (float)(midKnotCount + 1);
            float3 midPos = math.lerp(
                (float3)bodyTransform.position,
                (float3)headTransform.position, t);
            _spline.Add(new BezierKnot(midPos));
        }

        // 頭部端
        _spline.Add(new BezierKnot(headTransform.localPosition));
    }

    void LateUpdate()
    {
        // 更新首尾兩個 Knot 的位置
        int last = _spline.Count - 1;

        var bodyKnot = _spline[0];
        bodyKnot.Position = splineContainer.transform.InverseTransformPoint(bodyTransform.position);
        _spline.SetKnot(0, bodyKnot);

        var headKnot = _spline[last];
        headKnot.Position = splineContainer.transform.InverseTransformPoint(headTransform.position);
        _spline.SetKnot(last, headKnot);

        // 更新中間控制點（讓脖子自然下垂）
        for (int i = 1; i < last; i++)
        {
            float t = i / (float)last;
            float3 linearPos = math.lerp(
                (float3)bodyTransform.position,
                (float3)headTransform.position, t);

            // 加入重力下垂效果
            float sag = Mathf.Sin(t * Mathf.PI) * 0.1f;
            linearPos.y -= sag;

            var knot = _spline[i];
            knot.Position = splineContainer.transform.InverseTransformPoint(linearPos);
            _spline.SetKnot(i, knot);
        }
    }
}
```

### Step 3：TubeMeshRenderer.cs（沿 Spline 生成管狀 Mesh）

```csharp
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TubeMeshRenderer : MonoBehaviour
{
    [Header("References")]
    public SplineContainer splineContainer;

    [Header("Tube Settings")]
    public int lengthSegments = 20;   // 沿長度的分段數
    public int radialSegments = 8;    // 圓形截面的頂點數
    public float radius = 0.05f;      // 脖子半徑

    private Mesh _mesh;
    private MeshFilter _mf;

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mesh = new Mesh { name = "NeckTube" };
        _mf.mesh = _mesh;
    }

    void LateUpdate()
    {
        RebuildMesh();
    }

    void RebuildMesh()
    {
        int vertCount = (lengthSegments + 1) * (radialSegments + 1);
        int triCount  = lengthSegments * radialSegments * 6;

        var vertices  = new Vector3[vertCount];
        var normals   = new Vector3[vertCount];
        var uvs       = new Vector2[vertCount];
        var triangles = new int[triCount];

        var spline = splineContainer.Spline;

        for (int i = 0; i <= lengthSegments; i++)
        {
            float t = i / (float)lengthSegments;
            spline.Evaluate(t, out float3 pos, out float3 tangent, out float3 up);

            // 轉換到世界空間
            pos = splineContainer.transform.TransformPoint(pos);

            // 計算截面的局部座標系
            Vector3 forward = math.normalize(tangent);
            Vector3 right   = Vector3.Cross(forward, up).normalized;
            Vector3 upVec   = Vector3.Cross(right, forward).normalized;

            // 在截面上排列頂點
            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = j / (float)radialSegments * Mathf.PI * 2f;
                Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * upVec) * radius;

                int idx = i * (radialSegments + 1) + j;
                vertices[idx] = transform.InverseTransformPoint((Vector3)pos + offset);
                normals[idx]  = offset.normalized;
                uvs[idx]      = new Vector2(j / (float)radialSegments, t);
            }
        }

        // 生成三角形索引
        int ti = 0;
        for (int i = 0; i < lengthSegments; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int a = i       * (radialSegments + 1) + j;
                int b = (i + 1) * (radialSegments + 1) + j;
                int c = a + 1;
                int d = b + 1;

                triangles[ti++] = a;
                triangles[ti++] = b;
                triangles[ti++] = c;

                triangles[ti++] = c;
                triangles[ti++] = b;
                triangles[ti++] = d;
            }
        }

        _mesh.Clear();
        _mesh.vertices  = vertices;
        _mesh.normals   = normals;
        _mesh.uv        = uvs;
        _mesh.triangles = triangles;
    }
}
```

---

## 方法二：LineRenderer（快速原型）

最快的方式，適合 jam 初期驗證玩法，視覺效果比較像繩子。

```csharp
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class NeckLineRenderer : MonoBehaviour
{
    public Transform headTransform;
    public Transform bodyTransform;
    public int pointCount = 10;
    public float sagAmount = 0.15f;

    private LineRenderer _lr;

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.positionCount = pointCount;
        _lr.startWidth = 0.05f;
        _lr.endWidth   = 0.08f;
    }

    void LateUpdate()
    {
        for (int i = 0; i < pointCount; i++)
        {
            float t   = i / (float)(pointCount - 1);
            Vector3 p = Vector3.Lerp(bodyTransform.position, headTransform.position, t);

            // 拋物線下垂
            float sag = Mathf.Sin(t * Mathf.PI) * sagAmount;
            p.y -= sag;

            _lr.SetPosition(i, p);
        }
    }
}
```

---

## 方法三：多段 Cylinder + LookAt

用多個細長的 Capsule/Cylinder 物件串連，每段 LookAt 下一段，效能好但接縫明顯。

```csharp
using UnityEngine;

public class NeckSegmented : MonoBehaviour
{
    public Transform headTransform;
    public Transform bodyTransform;
    public int segmentCount = 6;
    public GameObject segmentPrefab;  // 細長 Cylinder prefab

    private Transform[] _segments;

    void Start()
    {
        _segments = new Transform[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            _segments[i] = Instantiate(segmentPrefab, transform).transform;
        }
    }

    void LateUpdate()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            float t0 = i / (float)segmentCount;
            float t1 = (i + 1) / (float)segmentCount;

            Vector3 from = Vector3.Lerp(bodyTransform.position, headTransform.position, t0);
            Vector3 to   = Vector3.Lerp(bodyTransform.position, headTransform.position, t1);

            _segments[i].position = (from + to) * 0.5f;

            // 讓段落朝向下一段
            if (to != from)
                _segments[i].LookAt(to, Vector3.up);

            // 根據距離調整長度
            float dist = Vector3.Distance(from, to);
            _segments[i].localScale = new Vector3(0.05f, dist * 0.5f, 0.05f);
        }
    }
}
```

---

## 身體 Follow 頭部的物理邏輯

脖子彈性感的關鍵在於「鬆弛時身體自由移動，拉緊時才被牽引」。

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DuckBodyFollow : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform;

    [Header("Neck Settings")]
    public float maxNeckLength  = 1.5f;  // 超過這個距離才拉身體
    public float pullForce      = 20f;   // 拉力大小
    public float walkSpeed      = 3f;    // 走路速度（短距離時）

    private Rigidbody _rb;

    void Awake() => _rb = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        Vector3 toHead = headTransform.position - transform.position;
        float   dist   = toHead.magnitude;

        if (dist > maxNeckLength)
        {
            // 脖子拉緊：強制施力朝頭部方向
            Vector3 direction = toHead.normalized;
            _rb.AddForce(direction * pullForce, ForceMode.Force);
        }
        else if (dist > 0.3f)
        {
            // 脖子稍微緊：緩慢走路跟上
            Vector3 flatDir = new Vector3(toHead.x, 0, toHead.z).normalized;
            _rb.MovePosition(transform.position +
                flatDir * walkSpeed * Time.fixedDeltaTime * (dist / maxNeckLength));
        }
        // dist 很小時：身體靜止，不需要跟上
    }
}
```

---

## 方法比較表

| 方法 | 視覺品質 | 實作難度 | 效能 | 適合場景 |
|------|----------|----------|------|----------|
| **Splines + Tube Mesh** | ⭐⭐⭐⭐⭐ | 🔧🔧🔧 高 | 中（每幀重建 Mesh） | 正式製作 |
| **LineRenderer** | ⭐⭐⭐ | 🔧 低 | 高 | 快速原型 / Jam |
| **多段 Cylinder** | ⭐⭐⭐ | 🔧🔧 中 | 高 | 簡單遊戲 |
| **Bone Chain + IK** | ⭐⭐⭐⭐⭐ | 🔧🔧🔧🔧 最高 | 中 | 有 Rig 的角色 |

---

## 效能優化建議

1. **降低 Segment 數量** — `lengthSegments = 10` 在大多數情況下已足夠
2. **使用 Job System** — 將 Mesh 生成移至 `IJob` 避免主執行緒阻塞
3. **只在距離變化時重建** — 加入閾值判斷，距離變化 < 0.001f 時跳過
4. **共用 NativeArray** — 避免每幀 GC allocation

```csharp
// 加入閾值跳過
private Vector3 _lastHeadPos;
private Vector3 _lastBodyPos;

void LateUpdate()
{
    if (Vector3.Distance(headTransform.position, _lastHeadPos) < 0.001f &&
        Vector3.Distance(bodyTransform.position, _lastBodyPos) < 0.001f)
        return;  // 沒有移動，跳過重建

    RebuildMesh();
    _lastHeadPos = headTransform.position;
    _lastBodyPos = bodyTransform.position;
}
```

---

> 💡 **建議流程：** 先用 **LineRenderer** 驗證玩法手感 → 確認好玩後換成 **Splines + Tube Mesh** 做最終視覺
