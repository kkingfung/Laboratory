using System;
using Unity.Entities;
using UnityEngine;
using UniRx;
using MessagePipe;
using Laboratory.Core;
using Laboratory.Infrastructure.AsyncUtils;
using Laboratory.UI;

namespace Laboratory.ECS.Systems
{
    /// <summary>
    /// System responsible for managing network chat functionality including sending and receiving
    /// chat messages between players. This system bridges UI chat events with network transport
    /// and handles message distribution through the messaging system.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class NetworkChatSystem : SystemBase
    {
        #region Fields
        
        /// <summary>
        /// Message broker for handling chat message events between systems and UI
        /// </summary>
        private IMessageBroker _messageBroker = null!;
        
        /// <summary>
        /// Network transport interface for sending and receiving chat messages over the network
        /// </summary>
        private INetworkChatTransport _networkTransport = null!;
        
        /// <summary>
        /// Subscription to outgoing chat message events from UI
        /// </summary>
        private IDisposable? _chatSubscription;
        
        /// <summary>
        /// Flag indicating whether the system is properly initialized
        /// </summary>
        private bool _isInitialized = false;
        
        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Called when the system is created. Initializes dependencies and sets up
        /// subscriptions for handling chat message events.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            InitializeDependencies();
            
            if (_isInitialized)
            {
                SubscribeToOutgoingMessages();
            }
        }

        /// <summary>
        /// Called every frame during simulation. Polls for incoming network messages
        /// and distributes them through the messaging system.
        /// </summary>
        protected override void OnUpdate()
        {
            if (!_isInitialized)
            {
                return;
            }

            ProcessIncomingMessages();
        }

        /// <summary>
        /// Called when the system is destroyed. Cleans up subscriptions and resources.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupSubscriptions();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes all required dependencies from the service locator
        /// </summary>
        private void InitializeDependencies()
        {
            try
            {
                _messageBroker = Infrastructure.ServiceLocator.Instance.Resolve<IMessageBroker>();
                _networkTransport = Infrastructure.ServiceLocator.Instance.Resolve<INetworkChatTransport>();
                
                ValidateDependencies();
                _isInitialized = true;
                Debug.Log("NetworkChatSystem initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize NetworkChatSystem dependencies: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Validates that all required dependencies are properly resolved
        /// </summary>
        private void ValidateDependencies()
        {
            if (_messageBroker == null)
            {
                throw new InvalidOperationException("IMessageBroker could not be resolved from ServiceLocator");
            }
            
            if (_networkTransport == null)
            {
                throw new InvalidOperationException("INetworkChatTransport could not be resolved from ServiceLocator");
            }
        }

        /// <summary>
        /// Subscribes to outgoing chat message events from the UI and other systems
        /// </summary>
        private void SubscribeToOutgoingMessages()
        {
            try
            {
                _chatSubscription = _messageBroker.Receive<ChatMessageEvent>()
                    .Subscribe(OnOutgoingChatMessage, OnChatSubscriptionError);
                
                Debug.Log("Successfully subscribed to outgoing chat messages");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to subscribe to chat messages: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles outgoing chat message events by sending them through the network transport
        /// </summary>
        /// <param name="evt">The chat message event containing sender and message information</param>
        private void OnOutgoingChatMessage(ChatMessageEvent evt)
        {
            try
            {
                if (evt == null)
                {
                    Debug.LogWarning("Received null chat message event");
                    return;
                }

                if (string.IsNullOrEmpty(evt.Sender) || string.IsNullOrEmpty(evt.Message))
                {
                    Debug.LogWarning($"Invalid chat message data - Sender: '{evt.Sender}', Message: '{evt.Message}'");
                    return;
                }

                // Send the message through network transport
                _networkTransport.SendChatMessage(evt.Sender, evt.Message);
                Debug.Log($"Sent chat message from {evt.Sender}: {evt.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending chat message: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles errors that occur during chat message subscription
        /// </summary>
        /// <param name="error">The exception that occurred</param>
        private void OnChatSubscriptionError(Exception error)
        {
            Debug.LogError($"Chat subscription error: {error.Message}");
        }

        /// <summary>
        /// Processes incoming chat messages from the network transport
        /// and publishes them to the messaging system
        /// </summary>
        private void ProcessIncomingMessages()
        {
            try
            {
                // Process all available incoming messages in a single frame
                int messageCount = 0;
                const int maxMessagesPerFrame = 10; // Prevent frame rate issues with message spam
                
                while (messageCount < maxMessagesPerFrame && 
                       _networkTransport.TryReceiveChatMessage(out string sender, out string message))
                {
                    ProcessIncomingMessage(sender, message);
                    messageCount++;
                }
                
                if (messageCount >= maxMessagesPerFrame)
                {
                    Debug.LogWarning($"Processed maximum messages per frame ({maxMessagesPerFrame}), remaining messages will be processed next frame");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing incoming chat messages: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a single incoming chat message and publishes it to the messaging system
        /// </summary>
        /// <param name="sender">The sender of the message</param>
        /// <param name="message">The message content</param>
        private void ProcessIncomingMessage(string sender, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(sender) || string.IsNullOrEmpty(message))
                {
                    Debug.LogWarning($"Received invalid chat message - Sender: '{sender}', Message: '{message}'");
                    return;
                }

                // Create and publish the incoming chat message event
                var chatEvent = new ChatMessageEvent(sender, message);
                _messageBroker.Publish(chatEvent);
                
                Debug.Log($"Received and published chat message from {sender}: {message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing incoming message from {sender}: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up all subscriptions and disposable resources
        /// </summary>
        private void CleanupSubscriptions()
        {
            try
            {
                _chatSubscription?.Dispose();
                _chatSubscription = null;
                
                _messageBroker = null!;
                _networkTransport = null!;
                _isInitialized = false;
                
                Debug.Log("NetworkChatSystem subscriptions and resources cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during NetworkChatSystem cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if the network chat system is properly initialized and ready for use
        /// </summary>
        /// <returns>True if the system is initialized and functional</returns>
        public bool IsInitialized()
        {
            return _isInitialized && _messageBroker != null && _networkTransport != null;
        }

        /// <summary>
        /// Gets the current network transport instance for external access
        /// </summary>
        /// <returns>The current network transport instance, or null if not initialized</returns>
        public INetworkChatTransport GetNetworkTransport()
        {
            return _isInitialized ? _networkTransport : null;
        }

        #endregion
    }

    /// <summary>
    /// Abstract interface for network transport implementations to support chat functionality.
    /// Implement this interface with your actual network API (Unity Netcode, Mirror, etc.)
    /// to provide chat message sending and receiving capabilities.
    /// </summary>
    public interface INetworkChatTransport
    {
        /// <summary>
        /// Sends a chat message to other connected players through the network.
        /// </summary>
        /// <param name="sender">The name or identifier of the message sender</param>
        /// <param name="message">The chat message content to send</param>
        /// <exception cref="System.ArgumentException">Thrown when sender or message parameters are invalid</exception>
        /// <exception cref="System.InvalidOperationException">Thrown when network is not connected or ready</exception>
        void SendChatMessage(string sender, string message);

        /// <summary>
        /// Attempts to receive an incoming chat message from the network.
        /// This method should be called frequently (typically each frame) to poll for new messages.
        /// </summary>
        /// <param name="sender">Output parameter containing the sender's name or identifier</param>
        /// <param name="message">Output parameter containing the received message content</param>
        /// <returns>True if a message was successfully received and the output parameters are valid</returns>
        bool TryReceiveChatMessage(out string sender, out string message);
    }
}
