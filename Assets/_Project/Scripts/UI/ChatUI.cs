using System;
using System.Collections.Generic;
using MessagePipe;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Infrastructure.UI
{
    /// <summary>
    /// Chat UI controller to display messages and send user input.
    /// Listens for incoming chat messages and sends outgoing messages via MessagingPipe.
    /// </summary>
    public class ChatUI : IDisposable
    {
        private readonly IMessageBroker _messageBroker;

        // UI references (assign via inspector or constructor)
        private readonly Text _chatDisplayText;
        private readonly InputField _chatInputField;
        private readonly Button _sendButton;

        // Keeps chat history (optional)
        private readonly Queue<string> _chatMessages = new();

        // Max number of messages to keep
        private readonly int _maxMessages = 50;

        public ChatUI(IMessageBroker messageBroker, Text chatDisplayText, InputField chatInputField, Button sendButton)
        {
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
            _chatDisplayText = chatDisplayText ?? throw new ArgumentNullException(nameof(chatDisplayText));
            _chatInputField = chatInputField ?? throw new ArgumentNullException(nameof(chatInputField));
            _sendButton = sendButton ?? throw new ArgumentNullException(nameof(sendButton));

            Bind();
        }

        private void Bind()
        {
            // Subscribe to incoming chat messages
            _messageBroker.Receive<ChatMessageEvent>()
                .Subscribe(OnChatMessageReceived)
                .AddTo(_disposables);

            // Send message on button click
            _sendButton.onClick.AddListener(SendChatMessage);

            // Optional: send message on Enter key in input field
            _chatInputField.onEndEdit.AddListener(text =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    SendChatMessage();
                }
            });
        }

        private void OnChatMessageReceived(ChatMessageEvent chatMessage)
        {
            AddMessage($"{chatMessage.Sender}: {chatMessage.Message}");
        }

        private void AddMessage(string message)
        {
            if (_chatMessages.Count >= _maxMessages)
            {
                _chatMessages.Dequeue();
            }

            _chatMessages.Enqueue(message);

            _chatDisplayText.text = string.Join("\n", _chatMessages);
        }

        private void SendChatMessage()
        {
            var message = _chatInputField.text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            var sender = "You"; // Or get from player info

            // Publish outgoing chat message event
            _messageBroker.Publish(new ChatMessageEvent(sender, message));

            _chatInputField.text = string.Empty;
            _chatInputField.ActivateInputField();
        }

        private readonly CompositeDisposable _disposables = new();

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }

    public readonly struct ChatMessageEvent
    {
        public string Sender { get; }
        public string Message { get; }

        public ChatMessageEvent(string sender, string message)
        {
            Sender = sender;
            Message = message;
        }
    }
}
