using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;

namespace Laboratory.Core.Camera
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
        private CinemachineBrain _cinemachineBrain;
        
        [SerializeField]
        [Tooltip("Primary player transform to follow")]
        private Transform _player;

        [Header("Virtual Cameras")]
        [SerializeField]
        [Tooltip("Camera for following alive player")]
        private CinemachineCamera _followPlayerVC;
        
        [SerializeField]
        [Tooltip("Camera for orbiting around dead player")]
        private CinemachineCamera _deathCamOrbitVC;
        
        [SerializeField]
        [Tooltip("Camera for following teammates when dead")]
        private CinemachineCamera _followTeammateVC;
        
        [SerializeField]
        [Tooltip("Camera for cinematic shots")]
        private CinemachineCamera _cinematicShotVC;
        
        [SerializeField]
        [Tooltip("Camera for free spectator mode")]
        private CinemachineCamera _freeSpectatorVC;

        [Header("Settings")]
        [SerializeField]
        [Tooltip("Delay before switching to death camera")]
        private float _blendDelay = 0.5f;
        
        #pragma warning disable 0414 // Field assigned but never used - planned for future teammate switching feature
        [SerializeField]
        [Tooltip("Delay when switching between teammates")]
        private float _teammateSwitchDelay = 1.0f;
        #pragma warning restore 0414

        // Runtime state
        private List<Transform> _teammates = new List<Transform>();
        private int _currentTeammateIndex = 0;
        private CameraMode _currentMode = CameraMode.FollowPlayer;

        #endregion

        #region Properties

        /// <summary>
        /// Current active camera mode
        /// </summary>
        public CameraMode CurrentMode => _currentMode;

        /// <summary>
        /// List of available teammates for spectating
        /// </summary>
        public IReadOnlyList<Transform> Teammates => _teammates.AsReadOnly();

        /// <summary>
        /// Currently spectated teammate index
        /// </summary>
        public int CurrentTeammateIndex => _currentTeammateIndex;

        /// <summary>
        /// Currently spectated teammate transform
        /// </summary>
        public Transform CurrentTeammate => _teammates.Count > 0 && _currentTeammateIndex < _teammates.Count 
            ? _teammates[_currentTeammateIndex] 
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
            _teammates.Clear();
            if (newTeammates != null)
            {
                _teammates.AddRange(newTeammates);
            }
            _currentTeammateIndex = 0;
        }

        /// <summary>
        /// Add a teammate to the spectate list
        /// </summary>
        /// <param name="teammate">Teammate transform to add</param>
        public void AddTeammate(Transform teammate)
        {
            if (teammate != null && !_teammates.Contains(teammate))
            {
                _teammates.Add(teammate);
            }
        }

        /// <summary>
        /// Remove a teammate from the spectate list
        /// </summary>
        /// <param name="teammate">Teammate transform to remove</param>
        public void RemoveTeammate(Transform teammate)
        {
            if (_teammates.Contains(teammate))
            {
                int removedIndex = _teammates.IndexOf(teammate);
                _teammates.Remove(teammate);
                
                // Adjust current index if necessary
                if (_currentTeammateIndex >= _teammates.Count && _teammates.Count > 0)
                {
                    _currentTeammateIndex = _teammates.Count - 1;
                }
                else if (_currentTeammateIndex > removedIndex)
                {
                    _currentTeammateIndex--;
                }
            }
        }

        /// <summary>
        /// Set the active camera mode
        /// </summary>
        /// <param name="mode">Camera mode to activate</param>
        public void SetCameraMode(CameraMode mode)
        {
            _currentMode = mode;
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
        /// Set the player transform target for camera following
        /// </summary>
        /// <param name="playerTransform">Transform of the player to follow</param>
        public void SetTarget(Transform playerTransform)
        {
            _player = playerTransform;
            
            // Update the current camera if it's following the player
            if (_currentMode == CameraMode.FollowPlayer)
            {
                ActivateFollowPlayerCamera();
            }
        }

        /// <summary>
        /// Cycle to the next available teammate for spectating
        /// </summary>
        public void CycleToNextTeammate()
        {
            if (_teammates.Count <= 1) 
                return;

            _currentTeammateIndex = (_currentTeammateIndex + 1) % _teammates.Count;
            UpdateTeammateCamera();
        }

        /// <summary>
        /// Cycle to the previous available teammate for spectating
        /// </summary>
        public void CycleToPreviousTeammate()
        {
            if (_teammates.Count <= 1) 
                return;

            _currentTeammateIndex = (_currentTeammateIndex - 1 + _teammates.Count) % _teammates.Count;
            UpdateTeammateCamera();
        }

        /// <summary>
        /// Set specific teammate to spectate by index
        /// </summary>
        /// <param name="index">Index of teammate to spectate</param>
        public void SetTeammateIndex(int index)
        {
            if (index >= 0 && index < _teammates.Count)
            {
                _currentTeammateIndex = index;
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
            if (_followPlayerVC != null) _followPlayerVC.gameObject.SetActive(false);
            if (_deathCamOrbitVC != null) _deathCamOrbitVC.gameObject.SetActive(false);
            if (_followTeammateVC != null) _followTeammateVC.gameObject.SetActive(false);
            if (_cinematicShotVC != null) _cinematicShotVC.gameObject.SetActive(false);
            if (_freeSpectatorVC != null) _freeSpectatorVC.gameObject.SetActive(false);
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
            if (_followPlayerVC != null && _player != null)
            {
                _followPlayerVC.Follow = _player;
                _followPlayerVC.LookAt = _player;
                _followPlayerVC.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Configure and activate death cam orbit camera
        /// </summary>
        private void ActivateDeathCamOrbitCamera()
        {
            if (_deathCamOrbitVC != null && _player != null)
            {
                _deathCamOrbitVC.Follow = _player;
                _deathCamOrbitVC.LookAt = _player;
                _deathCamOrbitVC.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Configure and activate follow teammate camera
        /// </summary>
        private void ActivateFollowTeammateCamera()
        {
            if (_followTeammateVC != null && _teammates.Count > 0)
            {
                _currentTeammateIndex = Mathf.Clamp(_currentTeammateIndex, 0, _teammates.Count - 1);
                UpdateTeammateCamera();
                _followTeammateVC.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Configure and activate cinematic shot camera
        /// </summary>
        private void ActivateCinematicShotCamera()
        {
            if (_cinematicShotVC != null)
            {
                _cinematicShotVC.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Configure and activate free spectator camera
        /// </summary>
        private void ActivateFreeSpectatorCamera()
        {
            if (_freeSpectatorVC != null)
            {
                _freeSpectatorVC.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Update teammate camera to follow current teammate
        /// </summary>
        private void UpdateTeammateCamera()
        {
            if (_followTeammateVC != null && CurrentTeammate != null)
            {
                _followTeammateVC.Follow = CurrentTeammate;
                _followTeammateVC.LookAt = CurrentTeammate;
            }
        }

        /// <summary>
        /// Coroutine to switch camera mode after a delay
        /// </summary>
        /// <param name="mode">Camera mode to switch to</param>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator SwitchAfterDelay(CameraMode mode)
        {
            yield return new WaitForSeconds(_blendDelay);
            SetCameraMode(mode);
        }

        #endregion
    }
}
