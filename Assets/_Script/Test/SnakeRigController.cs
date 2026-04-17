using System.Collections.Generic;
using UnityEngine;

public class SnakeRigController : MonoBehaviour
{
    [Header("骨骼順序")]
    public List<Transform> bones = new List<Transform>(); 

    [Header("移動設定")]
    public float moveSpeed = 0f;
    public float steerSpeed = 150f;
    
    [Header("間距設定")]
    public float minDistancePerPoint = 0.1f; // 每一點之間的物理距離
    public int pointsBetweenBones = 5;      // 骨頭與骨頭之間間隔幾個點

    private List<Vector3> posHistory = new List<Vector3>();
    private List<Quaternion> rotHistory = new List<Quaternion>();

    void Start()
    {
        // 初始記錄第一個點，避免 List 為空
        posHistory.Add(transform.position);
        rotHistory.Add(transform.rotation);

        foreach (var bone in bones)
        {
            bone.parent = null; 
        }
    }

    void Update()
    {
        // 1. 移動邏輯
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        float steer = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up * steer * steerSpeed * Time.deltaTime);

        // 2. 距離檢測：只有移動夠遠才記錄新點
        // 計算目前位置與最後一個記錄點的距離
        float distance = Vector3.Distance(transform.position, posHistory[0]);
        
        if (distance > minDistancePerPoint)
        {
            posHistory.Insert(0, transform.position);
            rotHistory.Insert(0, transform.rotation);
        }

        // 3. 更新骨骼位置
        for (int i = 0; i < bones.Count; i++)
        {
            // 根據間距取得正確的路徑索引
            int targetIndex = i * pointsBetweenBones;
            
            // 如果路徑紀錄還不夠長，就先停在最後一個點
            if (targetIndex < posHistory.Count)
            {
                // 使用 Lerp 讓移動更平滑，減少抖動感
                bones[i].position = Vector3.Lerp(bones[i].position, posHistory[targetIndex], Time.deltaTime * 15f);
                bones[i].rotation = Quaternion.Slerp(bones[i].rotation, rotHistory[targetIndex], Time.deltaTime * 15f);
            }
        }

        // 4. 清理過舊的路徑點
        int maxPoints = bones.Count * pointsBetweenBones + 10;
        if (posHistory.Count > maxPoints)
        {
            posHistory.RemoveRange(maxPoints, posHistory.Count - maxPoints);
            rotHistory.RemoveRange(maxPoints, rotHistory.Count - maxPoints);
        }
    }
}