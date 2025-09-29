using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Laboratory.Core.Events;

namespace Laboratory.UI
{
    /// <summary>
    /// UI controller for displaying temporary notification messages to the user.
    /// Manages notification queuing, timing, and visual presentation with smooth transitions.
    /// Supports automatic message queueing and configurable display duration.
    /// </summary>
    public class NotificationUI : IDisposable
    {
        #region Fields
        
        /// <summary>
        /// Canvas group controlling notification UI visibility and interaction
        /// </summary>
        private readonly CanvasGroup _canvasGroup;
        
        /// <summary>
        /// Text component for displaying notification messages
        /// </summary>
        private readonly Text _messageText;
        
        /// <summary>
        /// Queue for managing pending notification messages
        /// </summary>
        private readonly Queue<string> _messageQueue = new();
        
        /// <summary>
        /// Disposable container for reactive subscriptions
        /// </summary>
        private readonly CompositeDisposable _disposables = new();
        
        /// <summary>
        /// Message broker for receiving notification events
        /// </summary>
        private readonly IMessageBroker _messageBroker;
        
        /// <summary>
        /// Duration in seconds each notification is displayed
        /// </summary>
        private readonly float _displayDuration;
        
        /// <summary>
        /// Duration in seconds for fade transition effects
        /// </summary>
        private readonly float _fadeDuration;
        
        /// <summary>
        /// Flag indicating whether a notification is currently being displayed
        /// </summary>
        private bool _isShowing = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets a value indicating whether the notification UI is currently visible.
        /// </summary>
        public bool IsVisible => _canvasGroup.alpha > 0f;
        
        /// <summary>
        /// Gets the number of pending notifications in the queue.
        /// </summary>
        public int QueueCount => _messageQueue.Count;
        
        /// <summary>
        /// Gets a value indicating whether a notification is currently being processed.
        /// </summary>
        public bool IsProcessing => _isShowing;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the NotificationUI class.
        /// </summary>
        /// <param name="canvasGroup">Canvas group controlling notification UI visibility</param>
        /// <param name="messageText">Text component for displaying messages</param>
        /// <param name="messageBroker">Message broker for receiving notification events</param>
        /// <param name="displayDuration">Duration in seconds each notification is displayed (default: 3.0)</param>
        /// <param name="fadeDuration">Duration in seconds for fade transitions (default: 0.3)</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when duration values are negative</exception>
        public NotificationUI(
            CanvasGroup canvasGroup, 
            Text messageText, 
            IMessageBroker messageBroker, 
            float displayDuration = 3.0f,
            float fadeDuration = 0.3f)
        {
            _canvasGroup = canvasGroup ?? throw new ArgumentNullException(nameof(canvasGroup));
            _messageText = messageText ?? throw new ArgumentNullException(nameof(messageText));
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
            
            if (displayDuration < 0f)
                throw new ArgumentOutOfRangeException(nameof(displayDuration), "Display duration cannot be negative");
            if (fadeDuration < 0f)
                throw new ArgumentOutOfRangeException(nameof(fadeDuration), "Fade duration cannot be negative");
                
            _displayDuration = displayDuration;
            _fadeDuration = fadeDuration;

            Initialize();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Queues a notification message for display.
        /// </summary>
        /// <param name="message">The message content to display</param>
        public void ShowNotification(string message)
        {
            if (string.IsNullOrEmpty(message)) 
            {
                Debug.LogWarning("NotificationUI: Attempted to show empty or null message");
                return;
            }

            EnqueueMessage(message);
        }
        
        /// <summary>
        /// Clears all pending notifications from the queue.
        /// </summary>
        public void ClearQueue()
        {
            _messageQueue.Clear();
        }
        
        /// <summary>
        /// Forces the current notification to hide immediately and processes the next queued message.
        /// </summary>
        public void SkipCurrent()
        {
            if (_isShowing)
            {
                HideImmediate();
                ProcessNextNotification();
            }
        }
        
        /// <summary>
        /// Releases all resources used by the NotificationUI instance.
        /// </summary>
        public void Dispose()
        {
            ClearQueue();
            _disposables?.Dispose();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Initializes the notification UI system and sets up event subscriptions.
        /// </summary>
        private void Initialize()
        {
            HideImmediate();
            SubscribeToNotificationEvents();
        }
        
        /// <summary>
        /// Sets up subscription to notification events from the message broker.
        /// </summary>
        private void SubscribeToNotificationEvents()
        {
            _messageBroker.Receive<NotificationEvent>()
                .Subscribe(notificationEvent => ShowNotification(notificationEvent.Message))
                .AddTo(_disposables);
        }
        
        /// <summary>
        /// Adds a message to the notification queue and starts processing if not already active.
        /// </summary>
        /// <param name="message">The message to enqueue</param>
        private void EnqueueMessage(string message)
        {
            _messageQueue.Enqueue(message);

            if (!_isShowing)
            {
                ProcessNextNotification();
            }
        }
        
        /// <summary>
        /// Processes the next notification in the queue asynchronously.
        /// </summary>
        private async void ProcessNextNotification()
        {
            if (_messageQueue.Count == 0)
            {
                _isShowing = false;
                HideImmediate();
                return;
            }

            _isShowing = true;
            var message = _messageQueue.Dequeue();

            await DisplayNotificationAsync(message);
            
            // Process next notification after current one completes
            ProcessNextNotification();
        }
        
        /// <summary>
        /// Displays a single notification with proper timing and transitions.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <returns>Task representing the display operation</returns>
        private async Task DisplayNotificationAsync(string message)
        {
            SetMessageText(message);
            await FadeIn();
            await WaitForDisplayDuration();
            await FadeOut();
        }
        
        /// <summary>
        /// Sets the message text content.
        /// </summary>
        /// <param name="message">The message to set</param>
        private void SetMessageText(string message)
        {
            _messageText.text = message;
        }
        
        /// <summary>
        /// Performs a smooth fade-in transition.
        /// </summary>
        /// <returns>Task representing the fade-in operation</returns>
        private async Task FadeIn()
        {
            SetCanvasGroupState(true, false);
            
            float elapsedTime = 0f;
            while (elapsedTime < _fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsedTime / _fadeDuration);
                _canvasGroup.alpha = alpha;
                await Task.Yield();
            }
            
            _canvasGroup.alpha = 1f;
        }
        
        /// <summary>
        /// Waits for the configured display duration.
        /// </summary>
        /// <returns>Task representing the wait operation</returns>
        private async Task WaitForDisplayDuration()
        {
            await Task.Delay(TimeSpan.FromSeconds(_displayDuration));
        }
        
        /// <summary>
        /// Performs a smooth fade-out transition.
        /// </summary>
        /// <returns>Task representing the fade-out operation</returns>
        private async Task FadeOut()
        {
            float elapsedTime = 0f;
            while (elapsedTime < _fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - (elapsedTime / _fadeDuration));
                _canvasGroup.alpha = alpha;
                await Task.Yield();
            }
            
            HideImmediate();
        }
        
        /// <summary>
        /// Sets the canvas group to a specific visibility and interaction state.
        /// </summary>
        /// <param name="blocksRaycasts">Whether the canvas group should block raycasts</param>
        /// <param name="interactable">Whether the canvas group should be interactable</param>
        private void SetCanvasGroupState(bool blocksRaycasts, bool interactable)
        {
            _canvasGroup.blocksRaycasts = blocksRaycasts;
            _canvasGroup.interactable = interactable;
        }
        
        /// <summary>
        /// Immediately hides the notification UI without transitions.
        /// </summary>
        private void HideImmediate()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        
        #endregion
    }
}
