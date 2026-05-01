using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全域關卡狀態管理單例（DontDestroyOnLoad）。結果 UI 由 Prefab 在當前場景的「UI_result」下生成。
///
/// 職責：
///   - 顯示 / 隱藏 Level Clear UI 與 Fail UI
///   - 延遲後重置所有麵包（ResetToSpawn）與巢（ResetNest）
///   - 防止「掉水事件在重置過渡期間重複觸發」
///
/// Scene 設置：
///   1. 建議僅在第一關（或 bootstrap）場景放一個 "GameManager" 物件並掛上此腳本；其餘關卡若重複放置會自動銷毀。
///   2. Inspector 指定 clearUIPrefab、failUIPrefab。
///   3. 每個主線場景需有同名物件 <see cref="UiResultObjectName"/>（子階層下會實例化兩塊 UI）。
///
/// 接線：
///   - <see cref="Nest"/> 達標時會自動呼叫 <see cref="OnLevelClear"/>；另可監聽 Nest.onLevelClear 掛額外邏輯
///   - WaterTrigger 呼叫 GameManager.Instance.OnFail()
///   - 過關後 <see cref="OnLevelClear"/> 透過 <see cref="LevelManager.GoToNextLevelAfterWin"/> 載入下一關
/// </summary>
public class GameManager : MonoBehaviour
{
    public const string UiResultObjectName = "UI_result";

    public static GameManager Instance { get; private set; }

    [Header("UI Prefab（在當前場景 UI_result 下 Instantiate）")]
    [SerializeField] GameObject clearUIPrefab;
    [SerializeField] GameObject failUIPrefab;

    [Header("時間設定")]
    [Tooltip("顯示 Fail UI 後幾秒開始重置")]
    [Range(0.5f, 5f)]
    public float failResetDelay = 1.5f;

    [Tooltip("顯示 Clear UI 後幾秒進入下一關（0 = 不自動跳關，只顯示 UI）")]
    [Range(0f, 10f)]
    public float clearNextDelay = 2f;

    GameObject _clearUI;
    GameObject _failUI;

    Nest nest;
    Bread[] breads;

    bool _isResetting;
    bool _levelCleared;

    // ─────────────────────────────────────────────────────────────────────
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

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        if (Instance != this)
            return;
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindCurrentScene();
    }

    void OnDisable()
    {
        if (Instance != this)
            return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BindCurrentScene();

    /// <summary>在當前場景尋找 UI_result 生成結果 UI，並重新綁定 Nest / 麵包。</summary>
    void BindCurrentScene()
    {
        if (Instance != this)
            return;

        StopAllCoroutines();
        _isResetting = false;
        _levelCleared = false;

        if (_clearUI != null)
        {
            Destroy(_clearUI);
            _clearUI = null;
        }

        if (_failUI != null)
        {
            Destroy(_failUI);
            _failUI = null;
        }

        GameObject uiRootGo = GameObject.Find(UiResultObjectName);
        if (uiRootGo == null)
        {
            Debug.LogWarning(
                $"[GameManager] 場景中找不到名為 \"{UiResultObjectName}\" 的物件，略過結果 UI 生成。");
        }
        else
        {
            Transform uiRoot = uiRootGo.transform;
            if (clearUIPrefab != null)
                _clearUI = Instantiate(clearUIPrefab, uiRoot);
            else
                Debug.LogWarning("[GameManager] clearUIPrefab 未指定。");

            if (failUIPrefab != null)
                _failUI = Instantiate(failUIPrefab, uiRoot);
            else
                Debug.LogWarning("[GameManager] failUIPrefab 未指定。");
        }

        nest   = FindFirstObjectByType<Nest>();
        breads = FindObjectsByType<Bread>(FindObjectsSortMode.None);

        SetUI(_clearUI, false);
        SetUI(_failUI, false);
    }

    // ── 過關（由 Nest 達標時呼叫；亦可在 Nest.onLevelClear 掛額外事件）──────────────────
    public void OnLevelClear()
    {
        if (_isResetting) return;

        _levelCleared = true;
        Debug.Log("[GameManager] Level Clear!");
        AudioManager.Instance?.PlayClear();
        SetUI(_clearUI, true);

        if (clearNextDelay > 0f)
            StartCoroutine(DelayedAction(clearNextDelay, LoadNextLevel));
    }

    // ── 失敗（由 WaterTrigger 呼叫）──────────────────────────────────────
    public void OnFail()
    {
        if (_isResetting || _levelCleared) return;
        _isResetting = true;

        Debug.Log("[GameManager] Fail — resetting level.");
        AudioManager.Instance?.PlayFail();
        SetUI(_failUI, true);

        StartCoroutine(DelayedAction(failResetDelay, ResetLevel));
    }

    // ── 重置關卡（所有麵包回 Spawn + 巢清空計數）─────────────────────────
    void ResetLevel()
    {
        SetUI(_failUI, false);

        if (nest != null)
            nest.ResetNest();

        foreach (Bread bread in breads)
        {
            if (bread != null)
                bread.ResetToSpawn();
        }

        _isResetting  = false;
        _levelCleared = false;
    }

    // ── 進入下一關（由 LevelManager 載入「Level{N+1}」或結局場景）──────────
    void LoadNextLevel()
    {
        SetUI(_clearUI, false);
        LevelManager.EnsureExists();
        if (LevelManager.Instance != null)
            LevelManager.Instance.GoToNextLevelAfterWin();
        else
            Debug.LogError("[GameManager] LoadNextLevel：無法建立 LevelManager。");
    }

    static void SetUI(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    IEnumerator DelayedAction(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}
