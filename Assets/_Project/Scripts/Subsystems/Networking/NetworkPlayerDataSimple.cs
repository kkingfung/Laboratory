using System;
using UnityEngine;

namespace Laboratory.Networking
{
    /// <summary>
    /// Network player data for multiplayer functionality
    /// </summary>
    [Serializable]
    public class NetworkPlayerDataSimple
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public float Health { get; set; }
        public int Level { get; set; }
        public bool IsAlive { get; set; }
        public float LastUpdateTime { get; set; }

        public NetworkPlayerDataSimple()
        {
            PlayerId = Guid.NewGuid().ToString();
            PlayerName = "Player";
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            Health = 100f;
            Level = 1;
            IsAlive = true;
            LastUpdateTime = Time.time;
        }

        public NetworkPlayerDataSimple(string playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            Health = 100f;
            Level = 1;
            IsAlive = true;
            LastUpdateTime = Time.time;
        }

        public void UpdatePosition(Vector3 newPosition)
        {
            Position = newPosition;
            LastUpdateTime = Time.time;
        }

        public void UpdateRotation(Quaternion newRotation)
        {
            Rotation = newRotation;
            LastUpdateTime = Time.time;
        }

        public void UpdateHealth(float newHealth)
        {
            Health = Mathf.Clamp(newHealth, 0f, 100f);
            IsAlive = Health > 0f;
            LastUpdateTime = Time.time;
        }

        public void SetLevel(int newLevel)
        {
            Level = Mathf.Max(1, newLevel);
            LastUpdateTime = Time.time;
        }

        public NetworkPlayerData ToNetworkPlayerData()
        {
            return new NetworkPlayerData
            {
                PlayerId = this.PlayerId,
                PlayerName = this.PlayerName,
                Position = this.Position,
                Rotation = this.Rotation,
                Health = this.Health,
                Level = this.Level,
                IsAlive = this.IsAlive,
                LastUpdateTime = this.LastUpdateTime
            };
        }
    }

    /// <summary>
    /// Full network player data with additional fields
    /// </summary>
    [Serializable]
    public class NetworkPlayerData
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public float Health { get; set; }
        public int Level { get; set; }
        public bool IsAlive { get; set; }
        public float LastUpdateTime { get; set; }
        public int Team { get; set; }
        public int Score { get; set; }
        public bool IsHost { get; set; }
        
        public NetworkPlayerData()
        {
            PlayerId = Guid.NewGuid().ToString();
            PlayerName = "Player";
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            Health = 100f;
            Level = 1;
            IsAlive = true;
            LastUpdateTime = Time.time;
            Team = 0;
            Score = 0;
            IsHost = false;
        }
    }
}
