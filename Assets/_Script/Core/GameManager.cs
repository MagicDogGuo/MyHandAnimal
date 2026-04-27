using System.Collections;
using UnityEngine;

/// <summary>
/// 關卡狀態管理單例。
///
/// 職責：
///   - 顯示 / 隱藏 Level Clear UI 與 Fail UI
///   - 延遲後重置所有麵包（ResetToSpawn）與巢（ResetNest）
///   - 防止「掉水事件在重置過渡期間重複觸發」
///
/// Scene 設置：
///   1. 建立空 GameObject 命名 "GameManager"，掛上此腳本
///   2. Inspector 指定 failUI、clearUI（Canvas 下的 Panel GameObject）
///   3. Inspector 指定 nest（場景中的 Nest GameObject）
///   4. breads 留空 → Awake 自動 FindObjectsByType 找全部麵包；
///      或手動拖入 Inspector 以指定特定麵包（不受 FindObjects 影響）
///
/// 接線：
///   - Nest.onLevelClear → GameManager.OnLevelClear()
///   - WaterTrigger 呼叫 GameManager.Instance.OnFail()
///   - 關卡流程：場景中建議放置 <see cref="LevelManager"/>（含 LevelConfig 陣列）；
///     過關後 <see cref="OnLevelClear"/> 會透過 <see cref="LevelManager.GoToNextLevelAfterWin"/>
///     載入「Level{下一關}」或結局場景（Build Settings 須已加入相關 .unity）
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI（拖入 Canvas 下的 Panel GameObject）")]
    [Tooltip("過關顯示的面板")]
    public GameObject clearUI;

    [Tooltip("失敗顯示的面板")]
    public GameObject failUI;

    [Header("場景物件")]
    [Tooltip("場景中的 Nest — 留空則 Awake 自動尋找")]
    public Nest nest;

    [Tooltip("所有麵包 — 留空則 Awake 自動收集全場景中的 Bread 組件")]
    public Bread[] breads;

    [Header("時間設定")]
    [Tooltip("顯示 Fail UI 後幾秒開始重置")]
    [Range(0.5f, 5f)]
    public float failResetDelay = 1.5f;

    [Tooltip("顯示 Clear UI 後幾秒進入下一關（0 = 不自動跳關，只顯示 UI）")]
    [Range(0f, 10f)]
    public float clearNextDelay = 2f;

    // ── 狀態 ──────────────────────────────────────────────────────────────
    private bool _isResetting   = false;   // 重置過渡期間封鎖重複 OnFail
    private bool _levelCleared  = false;   // 過關後封鎖 OnFail（巢內麵包物理恢復可能觸發落水）

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 自動收集場景物件（若 Inspector 未手動指定）
        if (nest == null)
            nest = FindFirstObjectByType<Nest>();

        if (breads == null || breads.Length == 0)
            breads = FindObjectsByType<Bread>(FindObjectsSortMode.None);
    }

    void Start()
    {
        // 確保 UI 在遊戲開始時都是隱藏的
        SetUI(clearUI, false);
        SetUI(failUI,  false);
    }

    // ── 過關（由 Nest.onLevelClear UnityEvent 呼叫）──────────────────────
    public void OnLevelClear()
    {
        if (_isResetting) return;

        _levelCleared = true;
        Debug.Log("[GameManager] Level Clear!");
        AudioManager.Instance?.PlayClear();
        SetUI(clearUI, true);

        if (clearNextDelay > 0f)
            StartCoroutine(DelayedAction(clearNextDelay, LoadNextLevel));
    }

    // ── 失敗（由 WaterTrigger 呼叫）──────────────────────────────────────
    public void OnFail()
    {
        if (_isResetting || _levelCleared) return;   // 已過關時忽略落水事件
        _isResetting = true;

        Debug.Log("[GameManager] Fail — resetting level.");
        AudioManager.Instance?.PlayFail();
        SetUI(failUI, true);

        StartCoroutine(DelayedAction(failResetDelay, ResetLevel));
    }

    // ── 重置關卡（所有麵包回 Spawn + 巢清空計數）─────────────────────────
    private void ResetLevel()
    {
        SetUI(failUI, false);

        // 重置巢計數器（不 Destroy，麵包 ResetToSpawn 會還原初始父與位置）
        if (nest != null)
            nest.ResetNest();

        // 所有麵包回到初始父物件下與初始 Local 姿態
        foreach (Bread bread in breads)
        {
            if (bread != null)
                bread.ResetToSpawn();
        }

        _isResetting  = false;
        _levelCleared = false;
    }

    // ── 進入下一關（由 <see cref="LevelManager"/> 載入「Level{N+1}」或結局場景）────
    private void LoadNextLevel()
    {
        SetUI(clearUI, false);
        LevelManager.EnsureExists();
        if (LevelManager.Instance != null)
            LevelManager.Instance.GoToNextLevelAfterWin();
        else
            Debug.LogError("[GameManager] LoadNextLevel：無法建立 LevelManager。");
    }

    // ── 工具：安全地切換 UI 顯示，避免 NullRef ──────────────────────────
    private static void SetUI(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    private IEnumerator DelayedAction(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}
