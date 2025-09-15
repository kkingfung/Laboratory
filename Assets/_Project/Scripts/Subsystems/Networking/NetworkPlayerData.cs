using Unity.Netcode;
using Unity.Collections;

namespace Laboratory.Subsystems.Networking
{
    /// <summary>
    /// Network data structure for player information
    /// </summary>
    public struct NetworkPlayerData : INetworkSerializable
    {
        public ulong ClientId;
        public FixedString64Bytes PlayerName;
        public int PlayerId;
        public int TeamId;
        public bool IsReady;
        public float Health;
        public float MaxHealth;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref TeamId);
            serializer.SerializeValue(ref IsReady);
            serializer.SerializeValue(ref Health);
            serializer.SerializeValue(ref MaxHealth);
        }
    }
}
