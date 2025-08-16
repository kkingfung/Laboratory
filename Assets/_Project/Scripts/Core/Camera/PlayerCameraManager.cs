using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;

namespace Laboratory.Core
{
    /// <summary>
    /// Manages player camera states and transitions between different camera modes.
    /// Handles alive/dead states, team spectating, and cinematic shots using Cinemachine virtual cameras.
    /// </summary>
    public class PlayerCameraManager : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// Available camera modes for different gameplay states
        /// </summary>
        public enum CameraMode
        {
            /// <summary>Player is alive and being followed</summary>
            FollowPlayer,
            /// <summary>Player is dead, orbiting around death location</summary>
            DeathCamOrbit,
            /// <summary>Player is dead, following a teammate</summary>
            FollowTeammate,
            /// <summary>Cinematic shot mode for dramatic moments</summary>
            CinematicShot,
            /// <summary>Free spectator mode for exploring</summary>
            FreeSpectator
        }

        #endregion

        #region Fields

        [Header("Common References")]
        [SerializeField]
        [Tooltip("Main Cinemachine brain controlling camera transitions")]
        private CinemachineBrain cinemachineBrain;
        
        [SerializeField]
        [Tooltip("Primary player transform to follow")]
        private Transform player;

        [Header("Virtual Cameras")]
        [SerializeField]
        [Tooltip("Camera for following alive player")]
        private CinemachineCamera  followPlayerVC;
        
        [SerializeField]
        [Tooltip("Camera for orbiting around dead player")]
        private CinemachineCamera  deathCamOrbitVC;
        
        [SerializeField]
        [Tooltip("Camera for following teammates when dead")]
        private CinemachineCamera  followTeammateVC;
        
        [SerializeField]
        [Tooltip("Camera for cinematic shots")]
        private CinemachineCamera  cinematicShotVC;
        
        [SerializeField]
        [Tooltip("Camera for free spectator mode")]
        private CinemachineCamera  freeSpectatorVC;

        [Header("Settings")]
        [SerializeField]
        [Tooltip("Delay before switching to death camera")]
        private float blendDelay = 0.5f;
        
        [SerializeField]
        [Tooltip("Delay when switching between teammates")]
        private float teammateSwitchDelay = 1.0f;

        // Runtime state
        private List<Transform> teammates = new List<Transform>();
        private int currentTeammateIndex = 0;
        private CameraMode currentMode = CameraMode.FollowPlayer;

        #endregion

        #region Properties

        /// <summary>
        /// Current active camera mode
        /// </summary>
        public CameraMode CurrentMode => currentMode;

        /// <summary>
        /// List of available teammates for spectating
        /// </summary>
        public IReadOnlyList<Transform> Teammates => teammates.AsReadOnly();

        /// <summary>
        /// Currently spectated teammate index
        /// </summary>
        public int CurrentTeammateIndex => currentTeammateIndex;

        /// <summary>
        /// Currently spectated teammate transform
        /// </summary>
        public Transform CurrentTeammate => teammates.Count > 0 && currentTeammateIndex < teammates.Count 
            ? teammates[currentTeammateIndex] 
            : null;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize camera system with default follow player mode
        /// </summary>
        private void Awake()
        {
            SetCameraMode(CameraMode.FollowPlayer);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the complete list of available teammates
        /// </summary>
        /// <param name="newTeammates">List of teammate transforms</param>
        public void SetTeammates(List<Transform> newTeammates)
        {
            teammates.Clear();
            if (newTeammates != null)
            {
                teammates.AddRange(newTeammates);
            }
            currentTeammateIndex = 0;
        }

        /// <summary>
        /// Add a teammate to the spectate list
        /// </summary>
        /// <param name="teammate">Teammate transform to add</param>
        public void AddTeammate(Transform teammate)
        {
            if (teammate != null && !teammates.Contains(teammate))
            {
                teammates.Add(teammate);
            }
        }

        /// <summary>
        /// Remove a teammate from the spectate list
        /// </summary>
        /// <param name="teammate">Teammate transform to remove</param>
        public void RemoveTeammate(Transform teammate)
        {
            if (teammates.Contains(teammate))
            {
                int removedIndex = teammates.IndexOf(teammate);
                teammates.Remove(teammate);
                
                // Adjust current index if necessary
                if (currentTeammateIndex >= teammates.Count && teammates.Count > 0)
                {
                    currentTeammateIndex = teammates.Count - 1;
                }
                else if (currentTeammateIndex > removedIndex)
                {
                    currentTeammateIndex--;
                }
            }
        }

        /// <summary>
        /// Set the active camera mode
        /// </summary>
        /// <param name="mode">Camera mode to activate</param>
        public void SetCameraMode(CameraMode mode)
        {
            currentMode = mode;
            DisableAllCameras();
            ActivateModeCamera(mode);
        }

        /// <summary>
        /// Handle player death and switch to appropriate death camera
        /// </summary>
        /// <param name="deathMode">Death camera mode to use</param>
        public void OnPlayerDied(CameraMode deathMode = CameraMode.DeathCamOrbit)
        {
            StartCoroutine(SwitchAfterDelay(deathMode));
        }

        /// <summary>
        /// Handle player respawn and return to follow mode
        /// </summary>
        public void OnPlayerRespawn()
        {
            SetCameraMode(CameraMode.FollowPlayer);
        }

        /// <summary>
        /// Cycle to the next available teammate for spectating
        /// </summary>
        public void CycleToNextTeammate()
        {
            if (teammates.Count <= 1) 
                return;

            currentTeammateIndex = (currentTeammateIndex + 1) % teammates.Count;
            UpdateTeammateCamera();
        }

        /// <summary>
        /// Cycle to the previous available teammate for spectating
        /// </summary>
        public void CycleToPreviousTeammate()
        {
            if (teammates.Count <= 1) 
                return;

            currentTeammateIndex = (currentTeammateIndex - 1 + teammates.Count) % teammates.Count;
            UpdateTeammateCamera();
        }

        /// <summary>
        /// Set specific teammate to spectate by index
        /// </summary>
        /// <param name="index">Index of teammate to spectate</param>
        public void SetTeammateIndex(int index)
        {
            if (index >= 0 && index < teammates.Count)
            {
                currentTeammateIndex = index;
                UpdateTeammateCamera();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Disable all virtual cameras to ensure clean transitions
        /// </summary>
        private void DisableAllCameras()
        {
            if (followPlayerVC != null) followPlayerVC.gameObject.SetActive(false);
            if (deathCamOrbitVC != null) deathCamOrbitVC.gameObject.SetActive(false);
            if (followTeammateVC != null) followTeammateVC.gameObject.SetActive(false);
            if (cinematicShotVC != null) cinematicShotVC.gameObject.SetActive(false);
            if (freeSpectatorVC != null) freeSpectatorVC.gameObject.SetActive(false);
        }

        /// <summary>
        /// Activate the appropriate virtual camera for the given mode
        /// </summary>
        /// <param name="mode">Camera mode to activate</param>
        private void ActivateModeCamera(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.FollowPlayer:
                    ActivateFollowPlayerCamera();
                    break;

                case CameraMode.DeathCamOrbit:
                    ActivateDeathCamOrbitCamera();
                    break;

                case CameraMode.FollowTeammate:
                    ActivateFollowTeammateCamera();
                    break;

                case CameraMode.CinematicShot:
                    ActivateCinematicShotCamera();
                    break;

                case CameraMode.FreeSpectator:
                    ActivateFreeSpectatorCamera();
                    break;
            }
        }

        /// <summary>
        /// Configure and activate follow player camera
        /// </summary>
        private void ActivateFollowPlayerCamera()
        {
            if (followPlayerVC != null && player != null)
            {
                followPlayerVC.Follow = player;
                followPlayerVC.LookAt = player;
                followPlayerVC.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Configure and activate death cam orbit camera
        /// </summary>
        private void ActivateDeathCamOrbitCamera()
        {
            if (deathCamOrbitVC != null && player != null)
            {
                deathCamOrbitVC.Follow = player;
                deathCamOrbitVC.LookAt = player;
                deathCamOrbitVC.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Configure and activate follow teammate camera
        /// </summary>
        private void ActivateFollowTeammateCamera()
        {
            if (followTeammateVC != null && teammates.Count > 0)
            {
                currentTeammateIndex = Mathf.Clamp(currentTeammateIndex, 0, teammates.Count - 1);
                UpdateTeammateCamera();
                followTeammateVC.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Configure and activate cinematic shot camera
        /// </summary>
        private void ActivateCinematicShotCamera()
        {
            if (cinematicShotVC != null)
            {
                cinematicShotVC.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Configure and activate free spectator camera
        /// </summary>
        private void ActivateFreeSpectatorCamera()
        {
            if (freeSpectatorVC != null)
            {
                freeSpectatorVC.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Update teammate camera to follow current teammate
        /// </summary>
        private void UpdateTeammateCamera()
        {
            if (followTeammateVC != null && CurrentTeammate != null)
            {
                followTeammateVC.Follow = CurrentTeammate;
                followTeammateVC.LookAt = CurrentTeammate;
            }
        }

        /// <summary>
        /// Coroutine to switch camera mode after a delay
        /// </summary>
        /// <param name="mode">Camera mode to switch to</param>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator SwitchAfterDelay(CameraMode mode)
        {
            yield return new WaitForSeconds(blendDelay);
            SetCameraMode(mode);
        }

        #endregion
    }
}
