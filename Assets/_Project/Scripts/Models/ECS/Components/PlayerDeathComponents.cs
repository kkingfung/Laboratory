using Unity.Entities;
using UnityEngine;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Component indicating a player has died
    /// </summary>
    public struct PlayerDeathComponent : IComponentData
    {
        public float DeathTime;
        public float RespawnTime;
        public float FinalHP;
        public bool CanRespawn;
        public Vector3 DeathPosition;
        public Entity Killer;
        
        public static PlayerDeathComponent Create(float respawnDelay = 5f, float finalHP = 0f)
        {
            return new PlayerDeathComponent
            {
                DeathTime = Time.time,
                RespawnTime = Time.time + respawnDelay,
                FinalHP = finalHP,
                CanRespawn = true,
                DeathPosition = Vector3.zero,
                Killer = Entity.Null
            };
        }
    }
    
    /// <summary>
    /// Component for tracking death timing
    /// </summary>
    public struct DeathTime : IComponentData
    {
        public float Value;
        public float TimeOfDeath;
        
        public static DeathTime Create(float deathTime = 0f)
        {
            return new DeathTime
            {
                Value = deathTime > 0 ? deathTime : Time.time,
                TimeOfDeath = deathTime > 0 ? deathTime : Time.time
            };
        }
    }
    
    /// <summary>
    /// Player spawn preference component
    /// </summary>
    public struct PlayerSpawnPreference : IComponentData
    {
        public int PreferredSpawnPointId;
        public int TeamId;
        public bool UseLastDeathPosition;
        public Vector3 CustomSpawnPosition;
        public bool HasCustomPosition;
        
        public static PlayerSpawnPreference Create(int spawnPointId = -1, int teamId = 0)
        {
            return new PlayerSpawnPreference
            {
                PreferredSpawnPointId = spawnPointId,
                TeamId = teamId,
                UseLastDeathPosition = false,
                CustomSpawnPosition = Vector3.zero,
                HasCustomPosition = false
            };
        }
    }
}
