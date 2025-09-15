using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Laboratory.Models.ECS.Components
{
    // Note: SpawnPointTag, SpawnPointData, SpawnPointSafety are defined in SpawnPointComponents.cs
    // Note: PlayerTag is defined in PlayerTag.cs
    // These comments prevent duplicate definitions while maintaining code organization
    
    /// <summary>
    /// Component containing player data.
    /// </summary>
    public struct PlayerData : IComponentData
    {
        /// <summary>Unique player identifier.</summary>
        public int PlayerId;
        
        /// <summary>Player's team ID.</summary>
        public int TeamId;
        
        /// <summary>Player's display name.</summary>
        public FixedString128Bytes PlayerName;
        
        /// <summary>Whether player is currently alive.</summary>
        public bool IsAlive;
        
        /// <summary>Player's current health.</summary>
        public float Health;
        
        /// <summary>Player's maximum health.</summary>
        public float MaxHealth;
        
        /// <summary>Last time player was spawned.</summary>
        public float LastSpawnTime;
    }
    
    /// <summary>
    /// Component for entities that can respawn.
    /// </summary>
    public struct RespawnData : IComponentData
    {
        /// <summary>Time when respawn becomes available.</summary>
        public float RespawnTime;
        
        /// <summary>Duration of respawn timer.</summary>
        public float RespawnDuration;
        
        /// <summary>Whether respawn is currently pending.</summary>
        public bool IsPendingRespawn;
        
        /// <summary>Preferred spawn point ID for respawn.</summary>
        public int PreferredSpawnPointId;
    }
    
    /// <summary>
    /// Component for team-related data.
    /// </summary>
    public struct TeamData : IComponentData
    {
        /// <summary>Team identifier.</summary>
        public int TeamId;
        
        /// <summary>Team name.</summary>
        public FixedString64Bytes TeamName;
        
        /// <summary>Team color.</summary>
        public float4 TeamColor;
        
        /// <summary>Number of players on this team.</summary>
        public int PlayerCount;
        
        /// <summary>Maximum number of players allowed on this team.</summary>
        public int MaxPlayers;
    }
}
