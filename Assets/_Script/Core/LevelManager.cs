using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 關卡流程：主線 0～4 對應場景 "Level0"～"Level4"；在場中放置單例並勾 DontDestroyOnLoad（腳本內也會掛上）。
/// 若場景內沒有本元件，<see cref="GameManager"/> 過關時會以 <see cref="EnsureExists"/> 建立最小實例（陣列請在 first-scene 的 LevelManager 上指定）。
/// </summary>
[DefaultExecutionOrder(-200)]
public class LevelManager : MonoBehaviour
{
    public const int FirstLevelIndex  = 0;
    public const int LastLevelIndex   = 4;   // 主線 0～4，第五關本階段可不做
    public const int MainLevelCount   = 5;   // 0,1,2,3,4

    public static LevelManager Instance { get; private set; }

    [Header("關卡資料（索引須與關卡編號一致：0 = 第零關）")]
    [Tooltip("長度建議 5，對應 Level0～Level4；可留空則不覆寫巢與功能開關")]
    [SerializeField] LevelConfig[] levelConfigs;

    [Header("結局 / 可選")]
    [Tooltip("完成最後主線關卡後載入的場景名；留空則不載入結局，僅清除 UI")]
    [SerializeField] string endingSceneName = "Ending";

    [SerializeField, Tooltip("在 Console 列印關卡載入與巢套用設定")]
    bool logLevelFlow = true;

    /// <summary>目前主線關卡索引 0～4，由目前啟用場景名 "LevelN" 同步。</summary>
    public int CurrentLevelIndex { get; private set; }

    /// <summary>從 "Level0" 等形式解析的索引若無法辨識則為 false（例如 Ending 場景）。</summary>
    public bool IsInMainLevelScene { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SyncStateFromActiveScene();
        ApplyCurrentConfigToNest();
        if (logLevelFlow)
        {
            var cfg = GetConfigForCurrentLevel();
            Debug.Log(
                IsInMainLevelScene
                    ? $"[LevelManager] 已載入關卡 {CurrentLevelIndex}，config={(cfg != null ? cfg.name : "null")}"
                    : $"[LevelManager] 已載入非主線或無法辨識場景：{scene.name}");
        }
    }

    void Start()
    {
        // 執行順序上若 sceneLoaded 早於本物件 Start 未訂閱，補一次
        SyncStateFromActiveScene();
        ApplyCurrentConfigToNest();
    }

    static bool TryParseLevelIndexFromSceneName(string sceneName, out int index)
    {
        index = -1;
        if (string.IsNullOrEmpty(sceneName) || !sceneName.StartsWith("Level", StringComparison.Ordinal))
            return false;
        if (sceneName.Length <= 5)
            return false;
        return int.TryParse(sceneName.Substring(5), out index) && index >= 0;
    }

    public static void EnsureExists()
    {
        if (Instance != null) return;
        var go = new GameObject("LevelManager");
        go.AddComponent<LevelManager>();
    }

    /// <summary>取得目前關卡索引對應的 ScriptableObject；未設定陣列或越界則 null。</summary>
    public LevelConfig GetConfigForCurrentLevel()
    {
        if (levelConfigs == null || !IsInMainLevelScene) return null;
        if (CurrentLevelIndex < 0 || CurrentLevelIndex >= levelConfigs.Length) return null;
        return levelConfigs[CurrentLevelIndex];
    }

    public LevelConfig GetConfigForLevel(int levelIndex)
    {
        if (levelConfigs == null) return null;
        if (levelIndex < 0 || levelIndex >= levelConfigs.Length) return null;
        return levelConfigs[levelIndex];
    }

    void SyncStateFromActiveScene()
    {
        var name = SceneManager.GetActiveScene().name;
        if (TryParseLevelIndexFromSceneName(name, out int idx))
        {
            CurrentLevelIndex   = idx;
            IsInMainLevelScene  = true;
        }
        else
        {
            IsInMainLevelScene = false;
        }
    }

    void ApplyCurrentConfigToNest()
    {
        if (!IsInMainLevelScene) return;
        var config = GetConfigForCurrentLevel();
        if (config == null) return;

        var nest = FindFirstObjectByType<Nest>();
        if (nest == null) return;
        nest.requiredBread = config.breadToWin;
        nest.requiredGoose = config.gooseToWin;
    }

    /// <summary>從主選單或除錯用：以主線索引載入場景 "Level{index}"（須已加入 Build Settings）。</summary>
    public void LoadLevelByIndex(int levelIndex)
    {
        if (levelIndex < FirstLevelIndex || levelIndex > LastLevelIndex)
        {
            Debug.LogWarning($"[LevelManager] LoadLevelByIndex 越界：{levelIndex}");
            return;
        }
        SceneManager.LoadScene($"Level{levelIndex}");
    }

    /// <summary>過關後呼叫：第零關 → Level1，…，第四關過關 → 結局或僅關 UI。</summary>
    public void GoToNextLevelAfterWin()
    {
        if (!IsInMainLevelScene)
        {
            Debug.LogWarning("[LevelManager] GoToNextLevelAfterWin：目前不在主線 LevelN 場景，略過。");
            return;
        }

        if (CurrentLevelIndex < LastLevelIndex)
        {
            int next = CurrentLevelIndex + 1;
            if (logLevelFlow)
                Debug.Log($"[LevelManager] 過關 → 載入 Level{next}");
            SceneManager.LoadScene($"Level{next}");
            return;
        }

        if (!string.IsNullOrEmpty(endingSceneName))
        {
            if (logLevelFlow)
                Debug.Log($"[LevelManager] 主線完結 → {endingSceneName}");
            SceneManager.LoadScene(endingSceneName);
        }
        else if (logLevelFlow)
        {
            Debug.Log("[LevelManager] 主線完結，未設定 endingSceneName。");
        }
    }
}
