using Unity.Entities;
using Infrastructure.UI;
using MessagePipe;
using UnityEngine;
using UniRx;
// FIXME: tidyup after 8/29
namespace Models.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class NetworkChatSystem : SystemBase
    {
        private IMessageBroker _messageBroker = null!;
        private INetworkChatTransport _networkTransport = null!;

        protected override void OnCreate()
        {
            base.OnCreate();

            _messageBroker = Infrastructure.ServiceLocator.Instance.Resolve<IMessageBroker>();
            _networkTransport = Infrastructure.ServiceLocator.Instance.Resolve<INetworkChatTransport>();

            // Subscribe to outgoing chat messages from UI
            _messageBroker.Receive<ChatMessageEvent>()
                .Subscribe(OnOutgoingChatMessage)
                .AddTo(Dependency); // Dispose with system
        }

        private void OnOutgoingChatMessage(ChatMessageEvent evt)
        {
            // Send chat message over the network
            _networkTransport.SendChatMessage(evt.Sender, evt.Message);
        }

        protected override void OnUpdate()
        {
            // Poll or receive incoming messages from network transport
            while (_networkTransport.TryReceiveChatMessage(out var sender, out var message))
            {
                // Publish incoming chat messages to MessagingPipe so UI can display
                _messageBroker.Publish(new ChatMessageEvent(sender, message));
            }
        }
    }

    /// <summary>
    /// Abstract interface your network transport should implement to support chat.
    /// Implement this with your actual network API.
    /// </summary>
    public interface INetworkChatTransport
    {
        /// <summary>
        /// Send a chat message to other players.
        /// </summary>
        void SendChatMessage(string sender, string message);

        /// <summary>
        /// Try to receive incoming chat messages.
        /// Returns true if a message was received.
        /// </summary>
        bool TryReceiveChatMessage(out string sender, out string message);
    }
}
