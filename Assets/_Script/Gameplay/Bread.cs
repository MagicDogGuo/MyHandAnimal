using UnityEngine;

/// <summary>
/// 掛在所有麵包 Prefab 上的標識組件。
/// 提供麵包種類、初始 Spawn 位置快照，以及強制解除吸附的 Detach 介面。
/// 被 Nest.cs / WaterTrigger.cs 用來識別「麵包物件」。
/// </summary>
[RequireComponent(typeof(BreadSnapToMouth))]
public class Bread : MonoBehaviour
{
    public enum BreadType { Toast, Baguette, Donut, Croissant }

    [Header("麵包屬性")]
    [Tooltip("麵包種類，供關卡管理與計分使用")]
    public BreadType breadType = BreadType.Toast;

    [Tooltip("進巢後的得分值（預設 1 分）")]
    public int scoreValue = 1;

    // ── 重置用：記錄初始 Pose ───────────────────────────────────────────
    private Vector3    _spawnPosition;
    private Quaternion _spawnRotation;
    private Vector3    _spawnScale;

    private BreadSnapToMouth _snapToMouth;
    private Rigidbody        _rb;

    /// <summary>麵包目前是否被手持住（轉傳 BreadSnapToMouth.IsHeld）。</summary>
    public bool IsHeld => _snapToMouth != null && _snapToMouth.IsHeld;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _snapToMouth = GetComponent<BreadSnapToMouth>();
        _rb          = GetComponent<Rigidbody>();

        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
        _spawnScale    = transform.localScale;
    }

    // ── 外部呼叫：解除嘴部吸附（供 Nest 入巢時使用）──────────────────────
    /// <summary>
    /// 從鵝嘴解除吸附並恢復物理，讓麵包可被巢接管。
    /// </summary>
    public void Detach()
    {
        _snapToMouth.Detach();
    }

    // ── 外部呼叫：重置回 Spawn 位置（關卡重置時使用）─────────────────────
    /// <summary>
    /// 將麵包重置至 Spawn 初始位置、旋轉與縮放，並恢復物理狀態。
    /// </summary>
    public void ResetToSpawn()
    {
        // 先嘗試透過 BreadSnapToMouth 正常解除（嘴部 Parent）
        Detach();

        // 無論麵包是在嘴裡、巢裡還是飛行中，都強制解除任何 Parent
        transform.SetParent(null);

        transform.position   = _spawnPosition;
        transform.rotation   = _spawnRotation;
        transform.localScale = _spawnScale;

        if (_rb != null)
        {
            _rb.isKinematic     = false;
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    // ── 外部呼叫：更新 Spawn Pose（Spawner 生成後呼叫）───────────────────
    /// <summary>
    /// 以目前 Transform 作為新的 Spawn 基準點（Spawner 設置好位置後呼叫）。
    /// </summary>
    public void SnapshotSpawnPose()
    {
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
        _spawnScale    = transform.localScale;
    }
}
