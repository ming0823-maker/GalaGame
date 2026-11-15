using UnityEngine;

namespace Game
{
    /// <summary>
    /// 旋轉跳動態平台：使平台平滑自轉 (例如 20°/秒)
    /// 支援 CharacterController 穩定踩踏與跳躍
    /// </summary>
    public class RotatingPlatform : MonoBehaviour
    {
        [Tooltip("自轉角速度 (度/秒，正值順時針，負值逆時針)")]
        public float rotationSpeed = 20f;

        [Tooltip("自轉軸向 (預設繞垂直 Y 軸旋轉)")]
        public Vector3 rotationAxis = Vector3.up;

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void FixedUpdate()
        {
            if (Mathf.Abs(rotationSpeed) > 0.001f)
            {
                Quaternion deltaRotation = Quaternion.Euler(rotationAxis.normalized * (rotationSpeed * Time.fixedDeltaTime));
                rb.MoveRotation(rb.rotation * deltaRotation);
            }
        }
    }
}
