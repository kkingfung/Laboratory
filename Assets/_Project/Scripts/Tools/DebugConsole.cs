using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace Infrastructure.UI
{
    /// <summary>
    /// In-game debug console that displays logs and accepts input commands.
    /// </summary>
    public class DebugConsole : IDisposable
    {
        private readonly Text _logText;
        private readonly InputField _inputField;
        private readonly ScrollRect _scrollRect;

        private readonly List<string> _logEntries = new();
        private readonly int _maxEntries = 100;

        private readonly CompositeDisposable _disposables = new();

        /// <summary>
        /// Action invoked when a command is submitted via input field.
        /// </summary>
        public event Action<string>? OnCommandEntered;

        public DebugConsole(Text logText, InputField inputField, ScrollRect scrollRect)
        {
            _logText = logText ?? throw new ArgumentNullException(nameof(logText));
            _inputField = inputField ?? throw new ArgumentNullException(nameof(inputField));
            _scrollRect = scrollRect ?? throw new ArgumentNullException(nameof(scrollRect));

            BindInput();
            SubscribeToUnityLog();
        }

        private void BindInput()
        {
            _inputField.onEndEdit.AddListener(text =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    SubmitCommand(text);
                }
            });
        }

        private void SubmitCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            AddLogEntry($"> {command}");

            OnCommandEntered?.Invoke(command);

            _inputField.text = string.Empty;
            _inputField.ActivateInputField();
        }

        /// <summary>
        /// Adds a message to the console log.
        /// </summary>
        public void AddLogEntry(string message)
        {
            if (_logEntries.Count >= _maxEntries)
            {
                _logEntries.RemoveAt(0);
            }

            _logEntries.Add(message);
            RefreshLogText();
        }

        private void RefreshLogText()
        {
            _logText.text = string.Join("\n", _logEntries);
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>
        /// Subscribes to Unity's Application.logMessageReceived to display logs.
        /// </summary>
        private void SubscribeToUnityLog()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            string logEntry = $"[{type}] {condition}";

            if (type == LogType.Exception || type == LogType.Error)
            {
                logEntry += $"\n{stackTrace}";
            }

            AddLogEntry(logEntry);
        }

        public void Dispose()
        {
            Application.logMessageReceived -= HandleLog;
            _disposables.Dispose();
        }
    }
}
