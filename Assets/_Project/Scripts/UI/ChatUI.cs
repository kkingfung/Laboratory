using System;
using System.Collections.Generic;
using MessagePipe;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Laboratory.Core.DI;

namespace Laboratory.UI
{
    /// <summary>
    /// Chat UI controller that manages chat message display and user input.
    /// Handles bidirectional chat communication through MessagePipe event system.
    /// Maintains chat history with configurable message limits and provides keyboard shortcuts.
    /// </summary>
    public class ChatUI : IDisposable
    {
        #region Fields
        
        /// <summary>
        /// Message broker for handling chat events
        /// </summary>
        private readonly IMessageBroker _messageBroker;
        
        /// <summary>
        /// UI text component for displaying chat messages
        /// </summary>
        private readonly Text _chatDisplayText;
        
        /// <summary>
        /// Input field for user message input
        /// </summary>
        private readonly InputField _chatInputField;
        
        /// <summary>
        /// Button for sending chat messages
        /// </summary>
        private readonly Button _sendButton;
        
        /// <summary>
        /// Queue to maintain chat message history
        /// </summary>
        private readonly Queue<string> _chatMessages = new();
        
        /// <summary>
        /// Maximum number of messages to keep in history
        /// </summary>
        private readonly int _maxMessages = 50;
        
        /// <summary>
        /// Disposable container for reactive subscriptions
        /// </summary>
        private readonly CompositeDisposable _disposables = new();
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the ChatUI class.
        /// </summary>
        /// <param name="messageBroker">Message broker for event handling</param>
        /// <param name="chatDisplayText">Text component for displaying messages</param>
        /// <param name="chatInputField">Input field for user input</param>
        /// <param name="sendButton">Button for sending messages</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public ChatUI(IMessageBroker messageBroker, Text chatDisplayText, InputField chatInputField, Button sendButton)
        {
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
            _chatDisplayText = chatDisplayText ?? throw new ArgumentNullException(nameof(chatDisplayText));
            _chatInputField = chatInputField ?? throw new ArgumentNullException(nameof(chatInputField));
            _sendButton = sendButton ?? throw new ArgumentNullException(nameof(sendButton));

            Initialize();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Releases all resources used by the ChatUI instance.
        /// </summary>
        public void Dispose()
        {
            _disposables?.Dispose();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Initializes UI bindings and event subscriptions.
        /// </summary>
        private void Initialize()
        {
            BindMessageEvents();
            BindUIEvents();
        }
        
        /// <summary>
        /// Sets up message broker event subscriptions.
        /// </summary>
        private void BindMessageEvents()
        {
            _messageBroker.Receive<ChatMessageEvent>()
                .Subscribe(OnChatMessageReceived)
                .AddTo(_disposables);
        }
        
        /// <summary>
        /// Sets up UI element event handlers.
        /// </summary>
        private void BindUIEvents()
        {
            // Send message on button click
            _sendButton.onClick.AddListener(SendChatMessage);

            // Send message on Enter key press
            _chatInputField.onEndEdit.AddListener(text =>
            {
                if (Laboratory.UI.Input.InputSystem.GetKeyDown(KeyCode.Return) || Laboratory.UI.Input.InputSystem.GetKeyDown(KeyCode.KeypadEnter))
                {
                    SendChatMessage();
                }
            });
        }
        
        /// <summary>
        /// Handles incoming chat message events.
        /// </summary>
        /// <param name="chatMessage">The received chat message event</param>
        private void OnChatMessageReceived(ChatMessageEvent chatMessage)
        {
            AddMessage($"{chatMessage.Sender}: {chatMessage.Message}");
        }
        
        /// <summary>
        /// Adds a message to the chat history and updates the display.
        /// </summary>
        /// <param name="message">The message to add</param>
        private void AddMessage(string message)
        {
            // Remove oldest message if at capacity
            if (_chatMessages.Count >= _maxMessages)
            {
                _chatMessages.Dequeue();
            }

            _chatMessages.Enqueue(message);
            UpdateChatDisplay();
        }
        
        /// <summary>
        /// Updates the chat display with current message history.
        /// </summary>
        private void UpdateChatDisplay()
        {
            _chatDisplayText.text = string.Join("\n", _chatMessages);
        }
        
        /// <summary>
        /// Sends the current input as a chat message.
        /// </summary>
        private void SendChatMessage()
        {
            var message = _chatInputField.text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            var sender = GetPlayerName();

            // Publish outgoing chat message event
            _messageBroker.Publish(new ChatMessageEvent(sender, message));

            ClearInput();
        }
        
        /// <summary>
        /// Gets the current player's name from available player information systems.
        /// Attempts to retrieve from NetworkPlayerData, fallback to Unity username, then default.
        /// </summary>
        /// <returns>The player's display name</returns>
        private string GetPlayerName()
        {
            try
            {
                // Method 1: Try to get from NetworkPlayerData if available
                var networkPlayerData = UnityEngine.Object.FindFirstObjectByType<Laboratory.Infrastructure.Networking.NetworkPlayerData>();
                if (networkPlayerData != null && !networkPlayerData.PlayerName.Value.IsEmpty)
                {
                    return networkPlayerData.PlayerName.Value.ToString();
                }
                
                // Method 2: Try to get from GlobalServiceProvider if a player service exists
                if (GlobalServiceProvider.IsInitialized)
                {
                    // Check if there's a config service that might have player info
                    if (GlobalServiceProvider.TryResolve<Laboratory.Core.Services.IConfigService>(out var configService))
                    {
                        // Try to get player name from config
                        // Note: GetConfig method may not exist in current IConfigService implementation
                        // var playerName = configService.GetConfig<string>("Player.Name");
                        // if (!string.IsNullOrEmpty(playerName))
                        // {
                        //     return playerName;
                        // }
                    }
                }
                
                // Method 3: Try to get system username
                var systemUsername = System.Environment.UserName;
                if (!string.IsNullOrEmpty(systemUsername))
                {
                    return systemUsername;
                }
                
                // Method 4: Try Unity's Cloud Build username (if available)
                var unityUsername = SystemInfo.deviceName;
                if (!string.IsNullOrEmpty(unityUsername) && unityUsername != "Unknown")
                {
                    return unityUsername;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ChatUI] Failed to get player name: {ex.Message}");
            }
            
            // Fallback: Default name with random ID to avoid conflicts
            return $"Player{UnityEngine.Random.Range(1000, 9999)}";
        }
        
        /// <summary>
        /// Clears the input field and refocuses it.
        /// </summary>
        private void ClearInput()
        {
            _chatInputField.text = string.Empty;
            _chatInputField.ActivateInputField();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Event structure for chat messages containing sender and message content.
    /// </summary>
    public readonly struct ChatMessageEvent
    {
        #region Properties
        
        /// <summary>
        /// Gets the name of the message sender.
        /// </summary>
        public string Sender { get; }
        
        /// <summary>
        /// Gets the message content.
        /// </summary>
        public string Message { get; }
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new ChatMessageEvent.
        /// </summary>
        /// <param name="sender">The name of the message sender</param>
        /// <param name="message">The message content</param>
        public ChatMessageEvent(string sender, string message)
        {
            Sender = sender;
            Message = message;
        }
        
        #endregion
    }
}
