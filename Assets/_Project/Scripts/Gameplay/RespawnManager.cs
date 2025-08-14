// RespawnManager.cs
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Gameplay.Respawn
{
    /// <summary>
    /// Manages player respawn logic and spawn points.
    /// </summary>
    public class RespawnManager : MonoBehaviour
    {
        #region Fields

        [Header("Respawn Settings")]
        [SerializeField] private float respawnDelay = 3f;
        [SerializeField] private Transform[] spawnPoints;

        private readonly Dictionary<int, float> _respawnTimers = new();

        #endregion

        #region Unity Override Methods

        private void Update()
        {
            UpdateRespawnTimers();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initiates respawn for a player by ID.
        /// </summary>
        public void StartRespawn(int playerId)
        {
            if (!_respawnTimers.ContainsKey(playerId))
            {
                _respawnTimers[playerId] = respawnDelay;
            }
        }

        /// <summary>
        /// Gets a random spawn point transform.
        /// </summary>
        public Transform GetRandomSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return null;

            int index = Random.Range(0, spawnPoints.Length);
            return spawnPoints[index];
        }

        #endregion

        #region Private Methods

        private void UpdateRespawnTimers()
        {
            var finished = new List<int>();
            foreach (var kvp in _respawnTimers)
            {
                _respawnTimers[kvp.Key] -= Time.deltaTime;
                if (_respawnTimers[kvp.Key] <= 0f)
                {
                    finished.Add(kvp.Key);
                }
            }

            foreach (var playerId in finished)
            {
                RespawnPlayer(playerId);
                _respawnTimers.Remove(playerId);
            }
        }

        private void RespawnPlayer(int playerId)
        {
            // Implement respawn logic here (e.g., instantiate player, set position)
            Transform spawnPoint = GetRandomSpawnPoint();
            // Example: PlayerManager.Instance.RespawnPlayerAt(playerId, spawnPoint.position);
        }

        #endregion
    }
}
