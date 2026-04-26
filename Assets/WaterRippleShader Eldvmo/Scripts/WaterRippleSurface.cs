using UnityEngine;

namespace Eldvmo.Ripples
{
    /// <summary>
    /// 掛在與水面 <see cref="MeshRenderer"/> 同一物件上，用單一環狀緩衝累積所有漣漪，
    /// 避免多個 <see cref="ObjectCollisionRipple"/> 各自對同一材質 <c>SetVectorArray</c> 互相覆寫而閃爍。
    /// </summary>
    [DisallowMultipleComponent]
    public class WaterRippleSurface : MonoBehaviour
    {
        static readonly int InputCentreId = Shader.PropertyToID("_InputCentre");

        [SerializeField] MeshRenderer targetRenderer;

        [Tooltip("須與著色器內 _InputCentre 陣列長度一致（例如 RingRipple_Lite = 10、RingRipple_Intensive = 100）。")]
        [Range(1, 100)]
        [SerializeField] int rippleSlotCount = 10;

        Vector4[] _points;
        int _write;
        MaterialPropertyBlock _mpb;

        void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<MeshRenderer>();

            _points = new Vector4[rippleSlotCount];
            _mpb = new MaterialPropertyBlock();
        }

        /// <summary>寫入一筆漣漪 UV；多個碰撞體／腳本可共用同一水面實例。</summary>
        public void PushRipple(Vector2 uv)
        {
            if (targetRenderer == null) return;

            _points[_write] = new Vector4(uv.x, uv.y, Time.time, 0f);
            _write = (_write + 1) % _points.Length;

            targetRenderer.GetPropertyBlock(_mpb);
            _mpb.SetVectorArray(InputCentreId, _points);
            targetRenderer.SetPropertyBlock(_mpb);
        }
    }
}
