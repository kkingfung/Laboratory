using System;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Temporary stub for ChatMessageEvent.
    /// TODO: Move this to the appropriate assembly or implement properly.
    /// </summary>
    [Serializable]
    public struct ChatMessageEvent
    {
        public string Message { get; set; }
        public string Sender { get; set; }
        public ulong senderClientId;
        public float timestamp;
        
        public ChatMessageEvent(string message, string sender)
        {
            Message = message;
            Sender = sender;
            senderClientId = 0;
            timestamp = 0f;
        }
    }
}
