using UnityEngine;

namespace Laboratory.Genres.Racing
{
    /// <summary>
    /// Racing checkpoint for lap tracking
    /// Triggers when vehicles pass through
    /// </summary>
    public class RacingCheckpoint : MonoBehaviour
    {
        [Header("Checkpoint Settings")]
        [SerializeField] private int checkpointIndex = 0;
        [SerializeField] private bool isFinishLine = false;
        [SerializeField] private float triggerRadius = 5f;

        [Header("Visual")]
        [SerializeField] private GameObject visualIndicator;

        // Events
        public event System.Action<RacingVehicleController> OnVehiclePassed;

        // Properties
        public int CheckpointIndex => checkpointIndex;
        public bool IsFinishLine => isFinishLine;

        private void OnTriggerEnter(Collider other)
        {
            RacingVehicleController vehicle = other.GetComponent<RacingVehicleController>();
            if (vehicle != null)
            {
                OnVehiclePassed?.Invoke(vehicle);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isFinishLine ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);

            // Draw direction arrow
            Gizmos.color = Color.blue;
            Vector3 forward = transform.forward * triggerRadius;
            Gizmos.DrawRay(transform.position, forward);
        }
    }
}
