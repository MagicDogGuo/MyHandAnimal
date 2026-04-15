using UnityEngine;

/// <summary>
/// 掛在 DuckHead 上，非 VR 模式下用鍵盤模擬頭部移動。
/// WASD 控制 XZ 平面，Q/E 控制高度。
/// 對應文件 4-5 節。
/// </summary>
public class HeadMouseController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float maxHeight = 3f;
    public float minHeight = 0.2f;

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float y = 0f;
        if (Input.GetKey(KeyCode.E)) y =  1f;
        if (Input.GetKey(KeyCode.Q)) y = -1f;

        Vector3 move = new Vector3(h, y, v) * moveSpeed * Time.deltaTime;
        Vector3 newPos = transform.position + move;

        newPos.y = Mathf.Clamp(newPos.y, minHeight, maxHeight);
        transform.position = newPos;
    }
}
