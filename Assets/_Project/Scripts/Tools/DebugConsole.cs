using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.Infrastructure.Tools
{
    /// <summary>
    /// In-game debug console that displays logs and accepts input commands.
    /// Provides functionality for logging messages, handling Unity log output,
    /// and processing user commands through an input interface.
    /// </summary>
    public class DebugConsole : IDisposable
    {
        #region Fields

        /// <summary>
        /// UI Text component for displaying log messages.
        /// </summary>
        private readonly Text _logText;

        /// <summary>
        /// Input field for user command entry.
        /// </summary>
        private readonly InputField _inputField;

        /// <summary>
        /// Scroll rect component for automatic scrolling to latest logs.
        /// </summary>
        private readonly ScrollRect _scrollRect;

        /// <summary>
        /// Collection of logged messages with automatic capacity management.
        /// </summary>
        private readonly List<string> _logEntries = new();

        /// <summary>
        /// Maximum number of log entries to retain in memory.
        /// </summary>
        private readonly int _maxEntries = 100;

        /// <summary>
        /// Disposable container for managing subscriptions and resources.
        /// </summary>
        private readonly CompositeDisposable _disposables = new();

        #endregion

        #region Events

        /// <summary>
        /// Event raised when a command is submitted via the input field.
        /// Subscribers can handle command processing and execution.
        /// </summary>
        public event Action<string> OnCommandEntered;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the DebugConsole class.
        /// </summary>
        /// <param name="logText">The text component for displaying log messages.</param>
        /// <param name="inputField">The input field for command entry.</param>
        /// <param name="scrollRect">The scroll rect for automatic scrolling.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public DebugConsole(Text logText, InputField inputField, ScrollRect scrollRect)
        {
            _logText = logText ?? throw new ArgumentNullException(nameof(logText));
            _inputField = inputField ?? throw new ArgumentNullException(nameof(inputField));
            _scrollRect = scrollRect ?? throw new ArgumentNullException(nameof(scrollRect));

            Initialize();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a message to the console log with automatic scrolling and capacity management.
        /// </summary>
        /// <param name="message">The message to add to the log.</param>
        public void AddLogEntry(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            if (_logEntries.Count >= _maxEntries)
            {
                _logEntries.RemoveAt(0);
            }

            _logEntries.Add(message);
            RefreshLogDisplay();
        }

        /// <summary>
        /// Clears all log entries from the console.
        /// </summary>
        public void ClearLog()
        {
            _logEntries.Clear();
            RefreshLogDisplay();
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases all resources used by the DebugConsole.
        /// Unsubscribes from Unity log events and disposes of managed resources.
        /// </summary>
        public void Dispose()
        {
            Application.logMessageReceived -= HandleUnityLog;
            _disposables.Dispose();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the debug console by setting up input handling and Unity log subscription.
        /// </summary>
        private void Initialize()
        {
            SetupInputHandling();
            SubscribeToUnityLog();
        }

        /// <summary>
        /// Configures input field event handling for command submission.
        /// </summary>
        private void SetupInputHandling()
        {
            _inputField.onEndEdit.AddListener(HandleInputSubmission);
        }

        /// <summary>
        /// Handles input field submission and processes commands.
        /// </summary>
        /// <param name="inputText">The text entered in the input field.</param>
        private void HandleInputSubmission(string inputText)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ProcessCommand(inputText);
            }
        }

        /// <summary>
        /// Processes and executes a submitted command.
        /// </summary>
        /// <param name="command">The command string to process.</param>
        private void ProcessCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            // Echo the command in the log
            AddLogEntry($"> {command}");

            // Notify subscribers of the command
            OnCommandEntered?.Invoke(command);

            // Clear and refocus input field
            _inputField.text = string.Empty;
            _inputField.ActivateInputField();
        }

        /// <summary>
        /// Subscribes to Unity's Application.logMessageReceived to display Unity logs.
        /// </summary>
        private void SubscribeToUnityLog()
        {
            Application.logMessageReceived += HandleUnityLog;
        }

        /// <summary>
        /// Handles Unity log messages and formats them for console display.
        /// </summary>
        /// <param name="condition">The log message content.</param>
        /// <param name="stackTrace">The stack trace information.</param>
        /// <param name="type">The type of log message.</param>
        private void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            string logEntry = $"[{type}] {condition}";

            // Include stack trace for errors and exceptions
            if (type == LogType.Exception || type == LogType.Error)
            {
                logEntry += $"\n{stackTrace}";
            }

            AddLogEntry(logEntry);
        }

        /// <summary>
        /// Refreshes the log text display and scrolls to the bottom.
        /// </summary>
        private void RefreshLogDisplay()
        {
            _logText.text = string.Join("\n", _logEntries);
            
            // Force canvas update and scroll to bottom
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        #endregion
    }
}
