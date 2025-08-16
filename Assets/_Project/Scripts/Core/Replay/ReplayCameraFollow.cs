using UnityEngine;
using Unity.Cinemachine;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Manages camera following behavior during replay sequences.
    /// Integrates with Cinemachine virtual cameras to provide smooth camera transitions and target tracking.
    /// </summary>
    /// <remarks>
    /// This component allows dynamic switching between different targets during replay playback,
    /// enabling cinematic camera work and better replay visualization.
    /// </remarks>
    public class ReplayCameraFollow : MonoBehaviour
    {
        #region Fields

        [Header("Camera Configuration")]
        [Tooltip("Cinemachine virtual camera used for following targets")]
        [SerializeField] private CinemachineCamera  virtualCamera;
        
        [Tooltip("Current target transform to follow and look at")]
        [SerializeField] private Transform target;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initializes the camera following system on start.
        /// Sets up the initial target for the virtual camera if both camera and target are assigned.
        /// </summary>
        private void Start()
        {
            InitializeCameraTarget();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets a new target for the camera to follow and look at.
        /// Updates both the Follow and LookAt properties of the virtual camera.
        /// </summary>
        /// <param name="newTarget">The new transform to follow. Can be null to clear the target.</param>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            UpdateCameraTarget();
        }

        /// <summary>
        /// Gets the current target being followed by the camera.
        /// </summary>
        /// <returns>The current target transform, or null if no target is set</returns>
        public Transform GetCurrentTarget()
        {
            return target;
        }

        /// <summary>
        /// Checks if the camera system is properly configured.
        /// </summary>
        /// <returns>True if both virtual camera and target are assigned</returns>
        public bool IsConfigured()
        {
            return virtualCamera != null && target != null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the camera target on startup.
        /// </summary>
        private void InitializeCameraTarget()
        {
            if (virtualCamera != null && target != null)
            {
                UpdateCameraTarget();
            }
            else
            {
                LogConfigurationWarnings();
            }
        }

        /// <summary>
        /// Updates the virtual camera's follow and look-at targets.
        /// </summary>
        private void UpdateCameraTarget()
        {
            if (virtualCamera != null)
            {
                virtualCamera.Follow = target;
                virtualCamera.LookAt = target;
            }
        }

        /// <summary>
        /// Logs warnings if the camera system is not properly configured.
        /// </summary>
        private void LogConfigurationWarnings()
        {
            if (virtualCamera == null)
            {
                Debug.LogWarning($"[{nameof(ReplayCameraFollow)}] Virtual camera is not assigned on {gameObject.name}");
            }

            if (target == null)
            {
                Debug.LogWarning($"[{nameof(ReplayCameraFollow)}] Target transform is not assigned on {gameObject.name}");
            }
        }

        #endregion
    }
}
