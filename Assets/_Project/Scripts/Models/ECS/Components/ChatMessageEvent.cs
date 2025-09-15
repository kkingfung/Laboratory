using Unity.Collections;
using Unity.Entities;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Component that contains chat message event data
    /// </summary>
    public struct ChatMessageEvent : IComponentData
    {
        public FixedString128Bytes Message;
        public FixedString64Bytes SenderName;
        public Entity Sender;
        public float Timestamp;
        public int MessageType; // 0 = Normal, 1 = System, 2 = Warning, etc.
        
        public static ChatMessageEvent Create(string message, string senderName, Entity sender)
        {
            return new ChatMessageEvent
            {
                Message = new FixedString128Bytes(message),
                SenderName = new FixedString64Bytes(senderName),
                Sender = sender,
                Timestamp = UnityEngine.Time.time,
                MessageType = 0
            };
        }
    }
}
