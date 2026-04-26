using System.Collections;
using UnityEngine;

namespace Eldvmo.Ripples
{
    public class ObjectCollisionRipple : MonoBehaviour
    {
        private bool isInWater = false;
        [SerializeField] private MeshRenderer ripplePlane;
        private Collider ripplePlaneCollider;
        private Vector4[] ripplePoints = new Vector4[10];
        private int rippleIndex = 0;
        private Vector2 _oldInputCentre;
        private bool _hasLastInputCentre;
        private int waterLayerMask;
        [SerializeField] private Collider waterTrigger;
        [Tooltip("關閉時只更新漣漪材質，不碰 Rigidbody（角色／鵝請維持關閉）。")]
        [SerializeField] bool isFloatingWithWater = false;
        [SerializeField] float moveUpHeight = 2f;
        [Tooltip("在 UV 上與上一筆接觸點至少相隔這段距離才再產生漣漪")]
        [SerializeField] float minUvDistanceForNewRipple = 0.05f;
        private Rigidbody rb;
        private Coroutine _gravityRestoreCoroutine;

        void Start()
        {
            if (ripplePlane != null)
                ripplePlaneCollider = ripplePlane.GetComponent<Collider>();
            waterLayerMask = LayerMask.GetMask("Water");
            rb = GetComponent<Rigidbody>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (ripplePlaneCollider != null && other == waterTrigger)
            {
                Debug.Log("OnTriggerEnter");
                isInWater = true;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (ripplePlaneCollider != null && other == waterTrigger)
            {
                isInWater = false;
                StopGravityRestoreRoutine();
                if (rb != null)
                    rb.useGravity = true;
            }
        }

        void FixedUpdate()
        {
            if (!isInWater) return;
            //Raycast from the object toward the plane
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 direction = Vector3.down;

            Ray ray = new Ray(origin, direction);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 2f, waterLayerMask))
            {
                Vector2 uv = hit.textureCoord;

                if (_hasLastInputCentre && Vector2.Distance(_oldInputCentre, uv) < minUvDistanceForNewRipple)
                    return;

                ripplePoints[rippleIndex] = new Vector4(uv.x, uv.y, Time.time, 0);
                rippleIndex = (rippleIndex + 1) % ripplePoints.Length;
                _oldInputCentre = uv;
                _hasLastInputCentre = true;

                if (ripplePlane != null)
                    ripplePlane.material.SetVectorArray("_InputCentre", ripplePoints);

                if (!isFloatingWithWater) return;
                if (rb == null) return;

                SetObjectHeight(hit.point.y + moveUpHeight);
                rb.useGravity = false;
                RestartGravityRestoreRoutine();
            }
        }

        /// <summary>
        /// 必須改 <see cref="Rigidbody.position"/>，勿直接改 <see cref="Transform.position"/>，
        /// 否則會與物理步進不同步，容易出現非預期旋轉／抖動。
        /// </summary>
        private void SetObjectHeight(float targetHeight)
        {
            Vector3 p = rb.position;
            p.y = Mathf.Lerp(p.y, targetHeight, Time.fixedDeltaTime * 0.5f);
            rb.position = p;
        }

        void RestartGravityRestoreRoutine()
        {
            StopGravityRestoreRoutine();
            _gravityRestoreCoroutine = StartCoroutine(EnableGravity());
        }

        void StopGravityRestoreRoutine()
        {
            if (_gravityRestoreCoroutine != null)
            {
                StopCoroutine(_gravityRestoreCoroutine);
                _gravityRestoreCoroutine = null;
            }
        }

        private IEnumerator EnableGravity()
        {
            yield return new WaitForSeconds(0.5f);
            if (rb != null)
                rb.useGravity = true;
            _gravityRestoreCoroutine = null;
        }
    }
}
