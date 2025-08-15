using UnityEngine;
using Cinemachine;
using System.Collections.Generic;

namespace Game.CameraSystem
{
    public class PlayerCameraManager : MonoBehaviour
    {
        public enum CameraMode
        {
            FollowPlayer,   // Alive
            DeathCamOrbit,  // Dead
            FollowTeammate, // Dead
            CinematicShot,  // Dead
            FreeSpectator   // Dead
        }

        [Header("Common References")]
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private Transform player;

        [Header("Virtual Cameras")]
        [SerializeField] private CinemachineVirtualCamera followPlayerVC;
        [SerializeField] private CinemachineVirtualCamera deathCamOrbitVC;
        [SerializeField] private CinemachineVirtualCamera followTeammateVC;
        [SerializeField] private CinemachineVirtualCamera cinematicShotVC;
        [SerializeField] private CinemachineVirtualCamera freeSpectatorVC;

        [Header("Settings")]
        [SerializeField] private float blendDelay = 0.5f;
        [SerializeField] private float teammateSwitchDelay = 1.0f;

        private List<Transform> teammates = new List<Transform>();
        private int currentTeammateIndex = 0;

        private CameraMode currentMode = CameraMode.FollowPlayer;

        private void Awake()
        {
            SetCameraMode(CameraMode.FollowPlayer);
        }

        private void DisableAllCameras()
        {
            followPlayerVC.gameObject.SetActive(false);
            deathCamOrbitVC.gameObject.SetActive(false);
            followTeammateVC.gameObject.SetActive(false);
            cinematicShotVC.gameObject.SetActive(false);
            freeSpectatorVC.gameObject.SetActive(false);
        }

        public void SetTeammates(List<Transform> newTeammates) => teammates = newTeammates;
        public void AddTeammate(Transform teammate)
        {
            if (!teammates.Contains(teammate))
                teammates.Add(teammate);
        }
        public void RemoveTeammate(Transform teammate)
        {
            if (teammates.Contains(teammate))
                teammates.Remove(teammate);
        }

        public void SetCameraMode(CameraMode mode)
        {
            currentMode = mode;
            DisableAllCameras();

            switch (mode)
            {
                case CameraMode.FollowPlayer:
                    followPlayerVC.Follow = player;
                    followPlayerVC.LookAt = player;
                    followPlayerVC.gameObject.SetActive(true);
                    break;

                case CameraMode.DeathCamOrbit:
                    deathCamOrbitVC.Follow = player;
                    deathCamOrbitVC.LookAt = player;
                    deathCamOrbitVC.gameObject.SetActive(true);
                    break;

                case CameraMode.FollowTeammate:
                    if (teammates.Count > 0)
                    {
                        currentTeammateIndex = 0;
                        followTeammateVC.Follow = teammates[currentTeammateIndex];
                        followTeammateVC.LookAt = teammates[currentTeammateIndex];
                        followTeammateVC.gameObject.SetActive(true);
                    }
                    break;

                case CameraMode.CinematicShot:
                    cinematicShotVC.gameObject.SetActive(true);
                    break;

                case CameraMode.FreeSpectator:
                    freeSpectatorVC.gameObject.SetActive(true);
                    break;
            }
        }

        public void OnPlayerDied(CameraMode deathMode)
        {
            StartCoroutine(SwitchAfterDelay(deathMode));
        }

        public void OnPlayerRespawn()
        {
            SetCameraMode(CameraMode.FollowPlayer);
        }

        public void CycleTeammate()
        {
            if (teammates.Count <= 1) return;

            currentTeammateIndex = (currentTeammateIndex + 1) % teammates.Count;
            followTeammateVC.Follow = teammates[currentTeammateIndex];
            followTeammateVC.LookAt = teammates[currentTeammateIndex];
        }

        private System.Collections.IEnumerator SwitchAfterDelay(CameraMode mode)
        {
            yield return new WaitForSeconds(blendDelay);
            SetCameraMode(mode);
        }
    }
}
