using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 將 <see cref="LevelConfig.levelHintText"/> 套用到 TMP。
/// 依賴場景載入後 <see cref="LevelManager"/> 已同步目前關卡索引（見 OnSceneLoaded／Start 順序）。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class LevelHintTMPFromConfig : MonoBehaviour
{
    TMP_Text targetText;

    [Tooltip("無對應 LevelConfig、或非主線場景時顯示的文字（留空則清空）")]
     string fallbackWhenNoConfig = "no content!";

    void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        ApplyFromCurrentLevel();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyFromCurrentLevel();
    }

    /// <summary>手動刷新（例如切換語系後）。</summary>
    public void ApplyFromCurrentLevel()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
        if (targetText == null) return;

        var lm = LevelManager.Instance;
        var cfg = lm != null ? lm.GetConfigForCurrentLevel() : null;
        targetText.text = cfg != null ? cfg.levelHintText : fallbackWhenNoConfig;
    }
}
