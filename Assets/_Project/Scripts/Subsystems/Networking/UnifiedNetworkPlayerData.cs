using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System;

namespace Laboratory.Subsystems.Networking
{
    /// <summary>
    /// Unified network player data structure that supports both Unity Netcode and simple networking
    /// </summary>
    public struct UnifiedNetworkPlayerData : INetworkSerializable
    {
        [Header("Identity")]
        public ulong ClientId;
        public FixedString64Bytes PlayerName;
        public int PlayerId;
        
        [Header("Team & Status")]
        public int TeamId;
        public bool IsReady;
        public bool IsHost;
        public bool IsAlive;
        
        [Header("Health")]
        public float Health;
        public float MaxHealth;
        
        [Header("Transform")]
        public Vector3 Position;
        public Quaternion Rotation;
        
        [Header("Gameplay")]
        public int Level;
        public int Score;
        public float LastUpdateTime;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Identity
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref PlayerId);
            
            // Team & Status
            serializer.SerializeValue(ref TeamId);
            serializer.SerializeValue(ref IsReady);
            serializer.SerializeValue(ref IsHost);
            serializer.SerializeValue(ref IsAlive);
            
            // Health
            serializer.SerializeValue(ref Health);
            serializer.SerializeValue(ref MaxHealth);
            
            // Transform
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
            
            // Gameplay
            serializer.SerializeValue(ref Level);
            serializer.SerializeValue(ref Score);
            serializer.SerializeValue(ref LastUpdateTime);
        }

        /// <summary>
        /// Create default player data
        /// </summary>
        public static UnifiedNetworkPlayerData CreateDefault(ulong clientId, string playerName = "Player")
        {
            return new UnifiedNetworkPlayerData
            {
                ClientId = clientId,
                PlayerName = playerName,
                PlayerId = (int)clientId,
                TeamId = 0,
                IsReady = false,
                IsHost = false,
                IsAlive = true,
                Health = 100f,
                MaxHealth = 100f,
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Level = 1,
                Score = 0,
                LastUpdateTime = Time.time
            };
        }

        /// <summary>
        /// Update player position and rotation
        /// </summary>
        public void UpdateTransform(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
            LastUpdateTime = Time.time;
        }

        /// <summary>
        /// Update player health
        /// </summary>
        public void UpdateHealth(float health, float maxHealth = -1f)
        {
            Health = Mathf.Clamp(health, 0f, MaxHealth);
            if (maxHealth > 0) MaxHealth = maxHealth;
            IsAlive = Health > 0f;
            LastUpdateTime = Time.time;
        }

        /// <summary>
        /// Update player score
        /// </summary>
        public void UpdateScore(int scoreChange)
        {
            Score = Mathf.Max(0, Score + scoreChange);
            LastUpdateTime = Time.time;
        }

        /// <summary>
        /// Convert to simple data structure for non-Netcode scenarios
        /// </summary>
        public SimpleNetworkPlayerData ToSimpleData()
        {
            return new SimpleNetworkPlayerData
            {
                PlayerId = PlayerId.ToString(),
                PlayerName = PlayerName.ToString(),
                Position = Position,
                Rotation = Rotation,
                Health = Health,
                Level = Level,
                IsAlive = IsAlive,
                LastUpdateTime = LastUpdateTime,
                Team = TeamId,
                Score = Score,
                IsHost = IsHost
            };
        }
    }

    /// <summary>
    /// Simple serializable player data for basic networking scenarios
    /// </summary>
    [Serializable]
    public class SimpleNetworkPlayerData
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

        public SimpleNetworkPlayerData()
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

        public SimpleNetworkPlayerData(string playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;
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

        /// <summary>
        /// Convert to Unity Netcode compatible structure
        /// </summary>
        public UnifiedNetworkPlayerData ToNetcodeData()
        {
            return new UnifiedNetworkPlayerData
            {
                ClientId = ulong.Parse(PlayerId),
                PlayerName = PlayerName,
                PlayerId = int.Parse(PlayerId),
                TeamId = Team,
                IsReady = false,
                IsHost = IsHost,
                IsAlive = IsAlive,
                Health = Health,
                MaxHealth = 100f,
                Position = Position,
                Rotation = Rotation,
                Level = Level,
                Score = Score,
                LastUpdateTime = LastUpdateTime
            };
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

        public void AddScore(int points)
        {
            Score = Mathf.Max(0, Score + points);
            LastUpdateTime = Time.time;
        }
    }
}
