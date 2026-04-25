using UnityEngine;

/// <summary>
/// 單一關卡的需求與功能開關（v2 規格見 Doc/vr_goose_dev_plan.md）。
/// 主線關卡索引 0～4 對應第零關～第四關；第五關可預留不納入 Build。
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/LevelConfig", order = 0)]
public class LevelConfig : ScriptableObject
{
    [Header("過關條件")]
    [Tooltip("該關所需麵包送達數")]
    public int breadToWin;

    [Tooltip("該關所需小鵝送達數")]
    public int gooseToWin;

    [Header("功能開關")]
    [Tooltip("第零／一關等可啟用引導箭頭")]
    public bool hasTutorialArrow;

    [Tooltip("第一關：鉤子")]
    public bool hasHook;

    [Tooltip("第四關：移動平台")]
    public bool hasMovingPlatform;

    [Tooltip("巡邏員 — 預設關閉，App 後續更新再開")]
    public bool hasPatrolGuard;

    [Header("生成 / 場景層（若由程式生小鵝可參考）")]
    [Tooltip("預期場景內小鵝隻數，供關卡設計參考（非必自動生成）")]
    public int gooseSpawnCount;

    [Header("微調參數")]
    [Tooltip("移動平台速度（與 LevelFeatureSetup 或 MovingPlatform 搭配）")]
    public float platformSpeed = 1.5f;

    [Tooltip("巡邏相關，後續與 Guard 搭配")]
    public float patrolSpeed = 1.5f;

    [Tooltip("巡邏週期時間，後續與 Guard 搭配")]
    public float patrolCycleTime = 3f;
}
