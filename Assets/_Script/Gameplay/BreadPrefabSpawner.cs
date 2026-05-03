using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 依固定時間間隔，從多個麵包 Prefab 中隨機 Instantiate 一台，直到達指定總數為止。
/// 生成後會呼叫 <see cref="Bread.SnapshotSpawnPose"/> 以利關卡重置；可選套用 <see cref="BreadSnapToMouth.RandomizeAppearance"/>。
/// </summary>
public class BreadPrefabSpawner : MonoBehaviour
{
    [Header("麵包 Prefab（建議指派 4 種）")]
    [SerializeField] GameObject[] breadPrefabs;

    [Header("生成節奏與數量")]
    [Tooltip("每生成一個之後等待幾秒再生成下一個；數字越小越快。")]
    [Min(0.01f)]
    public float spawnIntervalSeconds = 2f;

    [Tooltip("總共生幾個。-1 = 無限循環生成（需呼叫 StopSpawning 或停用物件才會停）")]
    public int totalSpawnCount = 10;

    [Tooltip("若為 true，第一次生成不需等待間隔立刻出現一枚。")]
    public bool spawnFirstImmediately = true;

    [Header("位置")]
    [Tooltip("未指定時使用本元件的 Transform")]
    public Transform spawnPoint;

    [Tooltip("在錨點周圍的隨機水平偏移（公尺）")]
    public float horizontalSpread = 0.15f;

    [Tooltip("在錨點周圍的隨機垂直偏移（公尺）")]
    public float verticalSpread = 0.05f;

    [Header("可選")]
    public bool randomizeAppearance = true;

    [Tooltip("生成物父物件；未指定則掛在場景根下")]
    public Transform spawnParent;

    [Tooltip("啟用元件時自動開始生成；若關閉請自行呼叫 StartSpawning()")]
    public bool spawnOnEnable = true;

    Coroutine _spawnRoutine;

    void OnEnable()
    {
        if (!spawnOnEnable)
            return;

        if (breadPrefabs != null && breadPrefabs.Length > 0)
            StartSpawning();
        else
            Debug.LogWarning("[BreadPrefabSpawner] breadPrefabs 未設定或為空。", this);
    }

    void OnDisable() => StopSpawning();

    /// <summary>重新開始協程（若已在跑會先停止再開始）。</summary>
    public void StartSpawning()
    {
        StopSpawning();
        _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    IEnumerator SpawnLoop()
    {
        List<GameObject> pool = ValidPrefabs();
        if (pool.Count == 0)
        {
            Debug.LogWarning("[BreadPrefabSpawner] 無有效 Prefab。", this);
            yield break;
        }

        Transform anchor = spawnPoint != null ? spawnPoint : transform;
        int spawned = 0;
        bool first = true;

        while (totalSpawnCount < 0 || spawned < totalSpawnCount)
        {
            if (!(first && spawnFirstImmediately))
                yield return new WaitForSeconds(spawnIntervalSeconds);
            first = false;

            GameObject prefab = pool[Random.Range(0, pool.Count)];
            Vector3 pos = anchor.position +
                          new Vector3(
                              Random.Range(-horizontalSpread, horizontalSpread),
                              Random.Range(-verticalSpread, verticalSpread),
                              Random.Range(-horizontalSpread, horizontalSpread));

            Quaternion rot = anchor.rotation;
            GameObject go = Instantiate(prefab, pos, rot, spawnParent);

            ApplyPostSpawn(go);
            spawned++;
        }

        _spawnRoutine = null;
    }

    List<GameObject> ValidPrefabs()
    {
        var list = new List<GameObject>();
        if (breadPrefabs == null) return list;

        foreach (GameObject p in breadPrefabs)
        {
            if (p != null)
                list.Add(p);
        }

        return list;
    }

    void ApplyPostSpawn(GameObject go)
    {
        if (randomizeAppearance)
        {
            var snap = go.GetComponent<BreadSnapToMouth>();
            if (snap != null)
                snap.RandomizeAppearance();
        }

        Bread bread = go.GetComponent<Bread>();
        if (bread != null)
            bread.SnapshotSpawnPose();
    }
}
