using UnityEngine;

/// <summary>
/// 依 <see cref="LevelManager"/> 的 <see cref="LevelConfig"/> 啟用／停用場景內可選功能（鉤子、移動平台、教學、巡邏預留）。
/// 可掛在空物件上；陣列未填則不覆寫該選項。巢的過關條件由 <see cref="LevelManager"/> 寫入 <see cref="Nest"/>，不在此重複。
/// </summary>
[DefaultExecutionOrder(10)]
public class LevelFeatureSetup : MonoBehaviour
{
    [Header("場景參考（可選，不填則不控制）")]
    [SerializeField] GameObject hookRoot;
    [SerializeField] GameObject movingPlatformRoot;
    [SerializeField] GameObject tutorialArrowRoot;
    [SerializeField] GameObject patrolGuardRoot;

    [Header("可選：用於套用 LevelConfig.platformSpeed")]
    [SerializeField] MovingPlatform movingPlatform;

    void Start()
    {
        var lm = LevelManager.Instance;
        if (lm == null) return;
        var c = lm.GetConfigForCurrentLevel();
        if (c == null) return;

        SetActiveIfAssigned(hookRoot,            c.hasHook);
        SetActiveIfAssigned(movingPlatformRoot, c.hasMovingPlatform);
        SetActiveIfAssigned(tutorialArrowRoot,  c.hasTutorialArrow);
        SetActiveIfAssigned(patrolGuardRoot,   c.hasPatrolGuard);

        if (movingPlatform != null)
            movingPlatform.ApplyFromConfig(c);
    }

    static void SetActiveIfAssigned(GameObject go, bool active)
    {
        if (go == null) return;
        go.SetActive(active);
    }
}
