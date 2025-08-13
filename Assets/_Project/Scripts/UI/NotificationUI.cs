using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
// FIXME: tidyup after 8/29
namespace Infrastructure.UI
{
    /// <summary>
    /// Manages display and queueing of on-screen notifications.
    /// Supports timed auto-hide and stacking.
    /// </summary>
    public class NotificationUI : IDisposable
    {
        private readonly CanvasGroup _canvasGroup;
        private readonly Text _messageText;
        private readonly Queue<string> _messageQueue = new();

        private readonly CompositeDisposable _disposables = new();

        private bool _isShowing = false;
        private float _displayDuration = 3f; // seconds per notification

        private readonly IMessageBroker _messageBroker;

        public NotificationUI(CanvasGroup canvasGroup, Text messageText, IMessageBroker messageBroker, float displayDuration = 3f)
        {
            _canvasGroup = canvasGroup ?? throw new ArgumentNullException(nameof(canvasGroup));
            _messageText = messageText ?? throw new ArgumentNullException(nameof(messageText));
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
            _displayDuration = displayDuration;

            HideImmediate();

            // Subscribe to NotificationEvents
            _messageBroker.Receive<NotificationEvent>()
                .Subscribe(evt => ShowNotification(evt.Message))
                .AddTo(_disposables);
        }

        /// <summary>
        /// Enqueue and show a notification message.
        /// </summary>
        public void ShowNotification(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            _messageQueue.Enqueue(message);

            if (!_isShowing)
            {
                ShowNext();
            }
        }

        private async void ShowNext()
        {
            if (_messageQueue.Count == 0)
            {
                HideImmediate();
                _isShowing = false;
                return;
            }

            _isShowing = true;
            var msg = _messageQueue.Dequeue();

            _messageText.text = msg;
            Show();

            await Task.Delay(TimeSpan.FromSeconds(_displayDuration));

            Hide();

            // Wait for fade out (optional)
            await Task.Delay(300);

            ShowNext();
        }

        private void Show()
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        private void Hide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void HideImmediate()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
