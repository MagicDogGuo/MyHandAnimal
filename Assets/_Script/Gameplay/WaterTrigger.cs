using UnityEngine;

/// <summary>
/// 水面落水偵測。
/// 掛在水面 GameObject 上，Collider 必須設為 isTrigger = true。
///
/// 任何帶有 Bread 組件的物件進入 Trigger → 通知 GameManager.OnFail()。
///
/// Scene 設置：
///   1. 水面 GameObject 加上 Box / Mesh Collider，勾選 Is Trigger
///   2. 將此腳本掛上同一個 GameObject
///   3. 確保場景中有 GameManager 單例
/// </summary>
[RequireComponent(typeof(Collider))]
public class WaterTrigger : MonoBehaviour
{
    void Awake()
    {
        // 強制確認 isTrigger，防止忘記在 Inspector 勾選
        GetComponent<Collider>().isTrigger = true;
    }

    // OnTriggerEnter：未持拿直接落水
    void OnTriggerEnter(Collider other)
    {
        TryFail(other);
    }

    // OnTriggerStay：手持進入水中後放手
    void OnTriggerStay(Collider other)
    {
        TryFail(other);
    }

    private void TryFail(Collider other)
    {
        Bread bread = other.GetComponent<Bread>();
        if (bread == null) return;

        // 手持中的麵包可能碰到水面，不算失敗；放手後才判定
        if (bread.IsHeld) return;

        if (GameManager.Instance != null)
            GameManager.Instance.OnFail();
        else
            Debug.LogWarning("[WaterTrigger] 找不到 GameManager，無法觸發 Fail。", this);
    }
}
