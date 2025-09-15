using UnityEngine;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Stub for NetworkObject to resolve compilation errors
    /// Replace with actual Netcode for GameObjects implementation when available
    /// </summary>
    public class NetworkObject : MonoBehaviour
    {
        public ulong NetworkObjectId { get; private set; }
        public bool IsOwner { get; private set; } = true;
        public bool IsServer { get; private set; } = true;
        public bool IsClient { get; private set; } = true;
        public bool IsHost { get; private set; } = true;
        public bool IsSpawned { get; private set; } = false;
        
        public void Spawn(bool destroyWithScene = false)
        {
            IsSpawned = true;
        }
        
        public void Despawn(bool destroy = true)
        {
            IsSpawned = false;
            if (destroy) Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Stub for NetworkEndpoint to resolve compilation errors
    /// </summary>
    public struct NetworkEndpoint
    {
        public string Address;
        public int Port;
        
        public NetworkEndpoint(string address, int port)
        {
            Address = address;
            Port = port;
        }
        
        public static NetworkEndpoint Parse(string endpointString)
        {
            var parts = endpointString.Split(':');
            return new NetworkEndpoint(parts[0], int.Parse(parts[1]));
        }
    }
    
    /// <summary>
    /// Network life state for entities
    /// </summary>
    public enum NetworkLifeState
    {
        Unspawned,
        Spawning,
        Spawned,
        Despawning,
        Despawned
    }
}
