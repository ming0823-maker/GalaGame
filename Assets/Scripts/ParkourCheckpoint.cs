using UnityEngine;

namespace Game
{
    /// <summary>
    /// 跑酷檢查點 / 終點觸發器
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ParkourCheckpoint : MonoBehaviour
    {
        [Header("檢查點設定")]
        [Tooltip("檢查點序號 (0 為起點，依此類推)")]
        public int checkpointIndex = 0;

        [Tooltip("檢查點名稱")]
        public string checkpointName = "檢查點";

        [Tooltip("是否為最終終點 (Finish Line)")]
        public bool isGoal = false;

        [Tooltip("重生點位置 (若留空則使用此物件的位置)")]
        public Vector3 respawnOffset = new Vector3(0, 0.5f, 0);

        private BoxCollider triggerCollider;

        private void Awake()
        {
            triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        public Vector3 RespawnPosition => transform.position + respawnOffset;
        public Quaternion RespawnRotation => transform.rotation;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                var manager = ParkourCourseManager.Instance;
                if (manager != null)
                {
                    manager.OnPlayerReachCheckpoint(this);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isGoal ? new Color(1f, 0.85f, 0.1f, 0.5f) : new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawCube(transform.position, transform.localScale);
        }
    }
}
