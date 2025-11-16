using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Laboratory.Core.Utilities;

namespace Laboratory.Tools
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
        /// UI InputField component for command input.
        /// </summary>
        private readonly InputField _commandInputField;

        /// <summary>
        /// Maximum number of log entries to retain.
        /// </summary>
        private readonly int _maxLogEntries;

        /// <summary>
        /// List of stored log messages.
        /// </summary>
        private readonly List<string> _logEntries;

        /// <summary>
        /// Subscription to Unity's log events.
        /// </summary>
        private IDisposable _logSubscription;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugConsole"/> class.
        /// </summary>
        /// <param name="logText">The Text component for displaying logs.</param>
        /// <param name="commandInputField">The InputField for command entry.</param>
        /// <param name="maxLogEntries">Maximum log entries to retain. Default is 100.</param>
        public DebugConsole(Text logText, InputField commandInputField, int maxLogEntries = 100)
        {
            _logText = logText;
            _commandInputField = commandInputField;
            _maxLogEntries = maxLogEntries;
            _logEntries = new List<string>(_maxLogEntries);

            InitializeCommandInput();
            SubscribeToUnityLogs();
        }

        /// <summary>
        /// Sets up the command input field event handlers.
        /// </summary>
        private void InitializeCommandInput()
        {
            if (_commandInputField == null) return;

            _commandInputField.onEndEdit.AddListener(OnCommandSubmit);
        }

        /// <summary>
        /// Subscribes to Unity's log output for automatic display.
        /// </summary>
        private void SubscribeToUnityLogs()
        {
            Application.logMessageReceived += HandleUnityLog;
        }

        #endregion

        #region Logging

        /// <summary>
        /// Adds a log entry to the console.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void AddLogEntry(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            _logEntries.Add(message);

            // Remove oldest entries if limit exceeded
            while (_logEntries.Count > _maxLogEntries)
            {
                _logEntries.RemoveAt(0);
            }

            RefreshLogDisplay();
        }

        /// <summary>
        /// Adds a log entry with a specified color.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The color to display the message in.</param>
        public void AddColoredLogEntry(string message, Color color)
        {
            string coloredMessage = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>";
            AddLogEntry(coloredMessage);
        }

        /// <summary>
        /// Logs a message with a specific log type color.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="logType">The log type (Info, Warning, Error).</param>
        public void Log(string message, LogType logType = LogType.Log)
        {
            Color color = logType switch
            {
                LogType.Error => Color.red,
                LogType.Warning => Color.yellow,
                LogType.Exception => Color.magenta,
                _ => Color.white
            };

            AddColoredLogEntry(message, color);
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

        #region Command Handling

        /// <summary>
        /// Handles command submission from the input field.
        /// </summary>
        /// <param name="command">The submitted command.</param>
        private void OnCommandSubmit(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            AddLogEntry($"> {command}");
            ProcessCommand(command);

            // Clear input field
            if (_commandInputField != null)
            {
                _commandInputField.text = "";
                _commandInputField.ActivateInputField();
            }
        }

        /// <summary>
        /// Processes a console command.
        /// </summary>
        /// <param name="command">The command to process.</param>
        private void ProcessCommand(string command)
        {
            string[] parts = command.Split(' ');
            string commandName = parts[0].ToLower();

            switch (commandName)
            {
                case "clear":
                    ClearLog();
                    break;

                case "help":
                    ShowHelp();
                    break;

                default:
                    AddLogEntry($"Unknown command: {commandName}");
                    break;
            }
        }

        /// <summary>
        /// Displays available commands.
        /// </summary>
        private void ShowHelp()
        {
            AddLogEntry("Available Commands:");
            AddLogEntry("  clear - Clear console log");
            AddLogEntry("  help  - Show this help message");
        }

        #endregion

        #region Unity Log Integration

        /// <summary>
        /// Handles Unity log messages and forwards them to the console.
        /// </summary>
        /// <param name="condition">The log message content.</param>
        /// <param name="stackTrace">The stack trace information.</param>
        /// <param name="type">The type of log message.</param>
        private void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            // Use optimized string formatting
            string logEntry = StringOptimizer.FormatOptimized("[{0}] {1}", type, condition);

            // Include stack trace for errors and exceptions
            if (type == LogType.Exception || type == LogType.Error)
            {
                logEntry = StringOptimizer.FormatOptimized("{0}\n{1}", logEntry, stackTrace);
            }

            AddLogEntry(logEntry);
        }

        /// <summary>
        /// Refreshes the log text display and scrolls to the bottom.
        /// </summary>
        private void RefreshLogDisplay()
        {
            if (_logText == null) return;

            _logText.text = string.Join("\n", _logEntries);

            // Force canvas update and scroll
            Canvas.ForceUpdateCanvases();
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Disposes of resources and unsubscribes from events.
        /// </summary>
        public void Dispose()
        {
            Application.logMessageReceived -= HandleUnityLog;
            _logSubscription?.Dispose();

            if (_commandInputField != null)
            {
                _commandInputField.onEndEdit.RemoveListener(OnCommandSubmit);
            }
        }

        #endregion
    }
}
