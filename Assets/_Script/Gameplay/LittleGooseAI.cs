using System.Linq;
using Oculus.Interaction.HandGrab;
using UnityEngine;

/// <summary>
/// 小鵝在水面活動範圍內的巡航 AI：隨機路點、轉向、可選貼地或維持水面高度，並以 Animator 參數 <c>Swim</c> 切到游水動畫。
/// 游水時改為 Kinematic 並以 <see cref="Rigidbody.MovePosition"/>＋障礙物 Cast 移動，避免與場景其他 Collider 碰撞推擠造成水上亂飄。
/// 被手抓住時（依 <see cref="HandGrabInteractable"/>）暫停 AI 並關閉 <c>Swim</c>；<see cref="headBone"/>＋<see cref="lookAtTarget"/> 驅動頭部朝向（限 Yaw/Pitch，超出距離回中立）。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(LittleGoose))]
public class LittleGooseAI : MonoBehaviour
{
    [Header("水面／活動範圍")]
    [Tooltip("泳區：建議用 BoxCollider（可 isTrigger）包住水面體積。未指定時以「起點」＋半徑建立邊界。")]
    public Collider swimArea;

    [Tooltip("未掛 swimArea 時使用的水面高度（世界 Y）。")]
    public float waterSurfaceY = 0.1f;

    [Tooltip("未掛 swimArea 時，以起點為中心的 XZ 半徑。")]
    public Vector2 defaultSwimHalfExtents = new Vector2(3f, 3f);

    [Header("游動")]
    public float moveSpeed = 0.45f;
    [Tooltip("自轉朝向前進方向（度／秒）。")]
    public float turnDegreesPerSecond = 120f;

    [Min(0.1f)]
    public float minWaypointInterval = 2f;
    [Min(0.1f)]
    public float maxWaypointInterval = 5.5f;

    [Tooltip("邊界內保留距離，避免目標點貼在牆上。")]
    public float edgeMargin = 0.25f;

    [Tooltip("維持在水面的垂直回正（加加速度）。")]
    public float waterAlignmentStrength = 10f;

    [Tooltip("在水中時關閉 Rigidbody 重力，離開游區後還原。")]
    public bool disableGravityInWater = true;

    [Tooltip("接近邊界時往中心多推一點，減少卡邊。")]
    public float edgeNudgeStrength = 2.5f;

    [Header("碰撞與移動模式")]
    [Tooltip("游水時使用 Kinematic + MovePosition，碰到靜態障礙會被 Cast 擋下，不會被往旁邊彈飛。")]
    public bool kinematicSwim = true;

    [Tooltip("水平移動前 SphereCast 的半徑；0 則從子物件第一個 SphereCollider 推斷。")]
    public float swimCastRadius = 0f;

    [Tooltip("與其他 Collider 的 SphereCast 圖層；請排除僅觸發用的水面 Trigger 若會誤擋。")]
    public LayerMask swimObstacleMask = ~0;

    [Tooltip("貼地／貼牆面時的間隙（m），避免重疊導致持續撞擊。")]
    public float castSkin = 0.03f;

    [Tooltip("每幀將高度拉向目標（貼地或水面）的程度（Kinematic 游水用）。")]
    public float waterHeightLerp = 12f;

    [Header("貼地")]
    [Tooltip("啟用後在水平位置向下找地面；射不到則用 GetWaterY() 當回退。關閉則行為同以前僅鎖水面 Y。")]
    public bool snapToGround = true;

    [Tooltip("地板／地形所在圖層。請不要包含小鵝自體圖層，否則射線只會打到自己。")]
    public LayerMask groundMask = ~0;

    [Min(0.05f)]
    [Tooltip("自參考高度（目前位置或移動前）再往上幾公尺作射線起點，可略過自體 Collider。")]
    public float groundRayStartAbove = 1.5f;

    [Min(0.1f)]
    [Tooltip("自起點向下最多能搜尋的距離；若地形落差大請加大。")]
    public float groundRayMaxDistance = 8f;

    [Tooltip("貼在碰撞點法線向上偏移（公尺），避免重疊。")]
    public float groundOffset = 0.05f;

    [Header("頭部 LookAt")]
    [Tooltip("小鵝的頭骨／臉的 Transform，在此節點上套用轉向。")]
    public Transform headBone;

    [Tooltip("要看的目標：玩家鵝頭或嘴部錨點。留空則在 Start 以 Tag 'MouthAnchor' 尋找。")]
    public Transform lookAtTarget;

    [Min(0.1f)]
    [Tooltip("進入此距離（m）內才朝向目標，否則漸回中性的頭部擺位。")]
    public float lookAtRange = 3f;

    [Tooltip("頭部轉向的平滑速度（愈大轉得愈快）。")]
    public float lookAtSlerpSpeed = 6f;

    [Tooltip("身體座標中左右轉 Y（偏航，度），預設 5 度。")]
    [Range(-90f, 90f)]
    public float lookMaxYaw = 10f;

    [Tooltip("抬／低頭 X（俯仰，度），預設 5 度。")]
    [Range(-60f, 60f)]
    public float lookMaxPitch = 10f;

    static readonly int ParamSwim = Animator.StringToHash("Swim");

    Rigidbody  _rb;
    Animator   _anim;
    HandGrabInteractable _handGrab;
    Bounds     _bounds;
    bool       _inWater;
    bool       _gravityDefault;
    CollisionDetectionMode _defaultCollisionMode;
    float      _castRadius;
    Vector3    _waypoint;
    float      _nextReselectTime;
    Quaternion _headNeutralLocal;
    bool       _capturedHeadNeutral;

    RaycastHit[] _groundHits = new RaycastHit[8];

    void Awake()
    {
        _rb   = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();
        _handGrab = GetComponent<HandGrabInteractable>();
        _gravityDefault = _rb.useGravity;
        _defaultCollisionMode = _rb.collisionDetectionMode;

        var sc = GetComponentInChildren<SphereCollider>();
        _castRadius = swimCastRadius > 0.01f
            ? swimCastRadius
            : (sc != null ? sc.radius * Mathf.Max(sc.transform.lossyScale.x, sc.transform.lossyScale.y, sc.transform.lossyScale.z) * 0.95f
                : 0.25f);
    }

    void Start()
    {
        RebuildBounds();
        SetWaypoint(RandomInnerPoint());
        _nextReselectTime = Time.time + Random.Range(minWaypointInterval, maxWaypointInterval);

        if (lookAtTarget == null)
        {
            var mouth = GameObject.FindGameObjectWithTag("MouthAnchor");
            if (mouth != null) lookAtTarget = mouth.transform;
        }
    }

    void RebuildBounds()
    {
        if (swimArea != null)
            _bounds = swimArea.bounds;
        else
        {
            Vector3 c = transform.position;
            c.y = waterSurfaceY;
            _bounds = new Bounds(c, new Vector3(
                defaultSwimHalfExtents.x * 2f, Mathf.Max(0.5f, 1f), defaultSwimHalfExtents.y * 2f));
        }
    }

    public float GetWaterY()
    {
        if (swimArea != null)
            return swimArea.bounds.max.y;
        return waterSurfaceY;
    }

    /// <summary>在 <paramref name="x"/>、<paramref name="z"/> 自 <paramref name="refY"/> 參考高度向上起點向下尋地；排除本物件與子階層上的 Collider。</summary>
    bool TryGetGroundY(float x, float z, float refY, out float targetY)
    {
        targetY = refY;
        if (!snapToGround || groundMask.value == 0)
            return false;

        Vector3 origin = new Vector3(x, refY + groundRayStartAbove, z);
        float maxDist = groundRayStartAbove + groundRayMaxDistance;
        int n = Physics.RaycastNonAlloc(
            origin, Vector3.down, _groundHits, maxDist, groundMask, QueryTriggerInteraction.Ignore);
        if (n <= 0)
            return false;

        int   best  = -1;
        float bestD = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var h = _groundHits[i];
            if (h.collider == null) continue;
            var t = h.collider.transform;
            if (t == transform || t.IsChildOf(transform)) continue;
            if (h.distance < bestD)
            {
                bestD = h.distance;
                best = i;
            }
        }

        if (best < 0)
            return false;
        targetY = _groundHits[best].point.y + groundOffset;
        return true;
    }

    public bool IsInSwimAreaXZ(Vector3 p)
    {
        return p.x >= _bounds.min.x + edgeMargin && p.x <= _bounds.max.x - edgeMargin
            && p.z >= _bounds.min.z + edgeMargin && p.z <= _bounds.max.z - edgeMargin;
    }

    Vector3 RandomInnerPoint()
    {
        return new Vector3(
            Random.Range(_bounds.min.x + edgeMargin, _bounds.max.x - edgeMargin),
            GetWaterY(),
            Random.Range(_bounds.min.z + edgeMargin, _bounds.max.z - edgeMargin));
    }

    void SetWaypoint(Vector3 p)
    {
        p.y = GetWaterY();
        _waypoint = p;
    }

    bool IsHeldByHand => _handGrab != null && _handGrab.SelectingInteractorViews.Any();

    void FixedUpdate()
    {
        Vector3 pos = _rb.position;
        if (swimArea != null) _bounds = swimArea.bounds;

        _inWater = IsInSwimAreaXZ(pos) && pos.y >= _bounds.min.y - 0.3f && pos.y <= _bounds.max.y + 0.5f;

        if (IsHeldByHand)
        {
            if (_inWater) SetSwimAnimator(false);
            return;
        }

        if (!_inWater)
        {
            RestoreDynamicDefaults();
            if (disableGravityInWater) _rb.useGravity = _gravityDefault;
            SetSwimAnimator(false);
            return;
        }

        if (kinematicSwim)
            TickSwimKinematic(pos);
        else
            TickSwimDynamic(pos);
    }

    void LateUpdate()
    {
        if (headBone == null) return;

        if (!_capturedHeadNeutral)
        {
            _headNeutralLocal  = headBone.localRotation;
            _capturedHeadNeutral = true;
        }

        float t = 1f - Mathf.Exp(-lookAtSlerpSpeed * Time.deltaTime);

        if (IsHeldByHand || lookAtTarget == null)
        {
            headBone.localRotation = Quaternion.Slerp(headBone.localRotation, _headNeutralLocal, t);
            return;
        }

        float dist = Vector3.Distance(headBone.position, lookAtTarget.position);
        if (dist < 0.01f)
        {
            headBone.localRotation = Quaternion.Slerp(headBone.localRotation, _headNeutralLocal, t);
            return;
        }
        if (dist > lookAtRange)
        {
            headBone.localRotation = Quaternion.Slerp(headBone.localRotation, _headNeutralLocal, t);
            return;
        }

        Vector3 wTo = (lookAtTarget.position - headBone.position) / dist;
        Vector3 b   = transform.InverseTransformDirection(wTo);
        float  h2   = b.x * b.x + b.z * b.z;
        float  h    = h2 > 1e-8f ? Mathf.Sqrt(h2) : 0f;
        if (h < 1e-4f) h = 1e-4f;
        float yaw   = Mathf.Atan2(b.x, b.z) * Mathf.Rad2Deg;
        float pitch = Mathf.Atan2(b.y, h) * Mathf.Rad2Deg;
        yaw   = Mathf.Clamp(yaw,   -lookMaxYaw,   lookMaxYaw);
        pitch = Mathf.Clamp(pitch, -lookMaxPitch, lookMaxPitch);
        float yRad = yaw * Mathf.Deg2Rad;
        float pRad = pitch * Mathf.Deg2Rad;
        var bC = new Vector3(
            Mathf.Sin(yRad) * Mathf.Cos(pRad),
            Mathf.Sin(pRad),
            Mathf.Cos(yRad) * Mathf.Cos(pRad));
        bC.Normalize();
        Vector3 wC  = transform.TransformDirection(bC);
        var  lookRot = Quaternion.LookRotation(wC, transform.up);

        Transform parent = headBone.parent;
        if (parent != null)
        {
            var targetLocal = Quaternion.Inverse(parent.rotation) * lookRot;
            headBone.localRotation = Quaternion.Slerp(headBone.localRotation, targetLocal, t);
        }
        else
            headBone.rotation = Quaternion.Slerp(headBone.rotation, lookRot, t);
    }

    void RestoreDynamicDefaults()
    {
        if (_rb.isKinematic) _rb.isKinematic = false;
        _rb.collisionDetectionMode = _defaultCollisionMode;
    }

    void TickSwimKinematic(Vector3 pos)
    {
        if (disableGravityInWater) _rb.useGravity = false;
        if (!_rb.isKinematic)
        {
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        _rb.isKinematic = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        if (Time.time >= _nextReselectTime)
        {
            SetWaypoint(RandomInnerPoint());
            _nextReselectTime = Time.time + Random.Range(minWaypointInterval, maxWaypointInterval);
        }

        Vector3 to = _waypoint - pos;
        to.y = 0f;
        if (to.sqrMagnitude < 0.04f)
        {
            SetWaypoint(RandomInnerPoint());
            to = _waypoint - pos;
            to.y = 0f;
        }

        Vector3 dir = to.sqrMagnitude > 1e-6f ? to.normalized : transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward;

        NudgeFromEdges(pos, ref dir);

        float   dt  = Time.fixedDeltaTime;
        float   d   = moveSpeed * dt;
        Vector3 dXZ = new Vector3(dir.x, 0f, dir.z) * d;
        if (dXZ.sqrMagnitude > 1e-8f)
        {
            Vector3 n = dXZ.normalized;
            if (Physics.SphereCast(
                    pos, _castRadius, n, out RaycastHit hit, d, swimObstacleMask, QueryTriggerInteraction.Ignore))
            {
                float safe = Mathf.Max(0f, hit.distance - castSkin);
                dXZ = n * safe;
            }
        }

        float t = 1f - Mathf.Exp(-waterHeightLerp * dt);
        Vector3 next = pos + dXZ;
        next.x = Mathf.Clamp(next.x, _bounds.min.x + edgeMargin, _bounds.max.x - edgeMargin);
        next.z = Mathf.Clamp(next.z, _bounds.min.z + edgeMargin, _bounds.max.z - edgeMargin);

        float targetY = TryGetGroundY(next.x, next.z, pos.y, out float gY) ? gY : GetWaterY();
        next.y = Mathf.Lerp(pos.y, targetY, t);

        _rb.MovePosition(next);
        if (dir.sqrMagnitude > 1e-4f)
        {
            var targetRot = Quaternion.LookRotation(dir, Vector3.up);
            _rb.MoveRotation(Quaternion.RotateTowards(
                _rb.rotation, targetRot, turnDegreesPerSecond * dt));
        }

        SetSwimAnimator(true);
    }

    void TickSwimDynamic(Vector3 pos)
    {
        if (disableGravityInWater) _rb.useGravity = false;

        if (Time.time >= _nextReselectTime)
        {
            SetWaypoint(RandomInnerPoint());
            _nextReselectTime = Time.time + Random.Range(minWaypointInterval, maxWaypointInterval);
        }

        Vector3 to = _waypoint - pos;
        to.y = 0f;
        if (to.sqrMagnitude < 0.04f)
        {
            SetWaypoint(RandomInnerPoint());
            to = _waypoint - pos;
            to.y = 0f;
        }

        Vector3 dir = to.sqrMagnitude > 1e-6f ? to.normalized : transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward;

        NudgeFromEdges(pos, ref dir);

        Vector3 v = _rb.linearVelocity;
        v.x = dir.x * moveSpeed;
        v.z = dir.z * moveSpeed;
        _rb.linearVelocity = v;

        float targetY = TryGetGroundY(pos.x, pos.z, pos.y, out float gY) ? gY : GetWaterY();
        float yErr    = targetY - pos.y;
        _rb.AddForce(Vector3.up * (yErr * waterAlignmentStrength - v.y * 0.4f), ForceMode.Acceleration);
        // 減少側向碰撞在角速度上堆積
        _rb.angularVelocity = new Vector3(0f, _rb.angularVelocity.y, 0f);

        if (dir.sqrMagnitude > 1e-4f)
        {
            var targetRot = Quaternion.LookRotation(dir, Vector3.up);
            _rb.MoveRotation(Quaternion.RotateTowards(
                _rb.rotation, targetRot, turnDegreesPerSecond * Time.fixedDeltaTime));
        }

        SetSwimAnimator(true);
    }

    void NudgeFromEdges(Vector3 pos, ref Vector3 dir)
    {
        Vector3 c  = new Vector3(_bounds.center.x, pos.y, _bounds.center.z);
        Vector3 o  = pos - c;
        o.y = 0f;
        float ex = _bounds.extents.x - edgeMargin;
        float ez = _bounds.extents.z - edgeMargin;
        if (ex < 0.05f || ez < 0.05f) return;

        float px = o.x / ex;
        float pz = o.z / ez;
        if (Mathf.Abs(px) < 0.75f && Mathf.Abs(pz) < 0.75f) return;

        Vector3 inDir = (c - pos); inDir.y = 0f;
        if (inDir.sqrMagnitude < 1e-6f) return;
        inDir.Normalize();
        dir = (dir * 0.4f + inDir * 0.6f + inDir * edgeNudgeStrength * 0.05f).normalized;
    }

    void SetSwimAnimator(bool swim)
    {
        if (_anim == null) return;
        if (_anim.GetBool(ParamSwim) != swim) _anim.SetBool(ParamSwim, swim);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
        if (swimArea != null) Gizmos.DrawWireCube(swimArea.bounds.center, swimArea.bounds.size);
        else
        {
            Vector3 c = Application.isPlaying ? _bounds.center : transform.position;
            c.y = waterSurfaceY;
            Gizmos.DrawWireCube(c, new Vector3(defaultSwimHalfExtents.x * 2f, 0.05f, defaultSwimHalfExtents.y * 2f));
        }
    }
#endif
}
