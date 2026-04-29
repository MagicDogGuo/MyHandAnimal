using UnityEngine;

/// <summary>
/// 脖子伸長達 <see cref="NeckSplineController.AllowsGunGestureLocomotion"/> 門檻（false）時，
/// 顯示同一物件上的 World Space Canvas，並將 Transform 置於鵝頭附近、每幀朝向攝影機。
/// 請與 World Space Canvas 同層或父層；勿 SetActive(false) 整顆提示物件（否則無法再開）。
/// </summary>
public class NeckLimitWorldUi : MonoBehaviour
{
    [SerializeField]
    private NeckSplineController neckSpline;

    [Tooltip("未指派時於 Awake 內 FindFirstObjectByType")]
    [SerializeField]
    private Transform billboardTransform;

    [Tooltip("相對 duckHead 的本地空間偏移（沿鵝頭軸）")]
    [SerializeField]
    private Vector3 offsetLocal = new Vector3(0f, 0.12f, 0.15f);

    [Tooltip("VR 請指定 Center Eye 或主視野 Camera；未指派時使用 Camera.main")]
    [SerializeField]
    private Camera targetCamera;

    [Tooltip("在 LookAt 之後再套用本地旋轉（例如 Canvas 翻面時調 Z）")]
    [SerializeField]
    private Vector3 rotationEulerOffset = Vector3.zero;

    private Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponent<Canvas>() ?? GetComponentInChildren<Canvas>(true);
        if (_canvas == null)
            Debug.LogWarning("[NeckLimitWorldUi] 請在同一物件或子物件上擁有 World Space Canvas。", this);

        if (neckSpline == null)
            neckSpline = FindFirstObjectByType<NeckSplineController>(FindObjectsInactive.Exclude);
        if (billboardTransform == null)
            billboardTransform = transform;
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (neckSpline == null || neckSpline.duckHead == null)
        {
            if (_canvas != null) _canvas.enabled = false;
            return;
        }

        bool show = !neckSpline.AllowsGunGestureLocomotion();
        if (_canvas != null) _canvas.enabled = show;
        if (!show) return;

        Transform head = neckSpline.duckHead;
        billboardTransform.position = head.position + head.TransformDirection(offsetLocal);

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam != null)
        {
            Vector3 toCam = cam.transform.position - billboardTransform.position;
            if (toCam.sqrMagnitude > 1e-8f)
            {
                Quaternion face = Quaternion.LookRotation(toCam.normalized);
                if (rotationEulerOffset != Vector3.zero)
                    face *= Quaternion.Euler(rotationEulerOffset);
                billboardTransform.rotation = face;
            }
        }
    }
}
