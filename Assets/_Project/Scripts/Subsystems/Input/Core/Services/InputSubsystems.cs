using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Input.Interfaces;

namespace Laboratory.Core.Input.Services
{
    /// <summary>
    /// Concrete implementation of IInputEventSystem.
    /// Manages event subscriptions and publishing for input actions.
    /// </summary>
    public class InputEventSystem : IInputEventSystem
    {
        private readonly Dictionary<string, List<Action<InputActionEventArgs>>> _subscriptions = new();

        public void Subscribe(string actionName, Action<InputActionEventArgs> callback)
        {
            if (string.IsNullOrEmpty(actionName) || callback == null) return;

            if (!_subscriptions.ContainsKey(actionName))
            {
                _subscriptions[actionName] = new List<Action<InputActionEventArgs>>();
            }

            if (!_subscriptions[actionName].Contains(callback))
            {
                _subscriptions[actionName].Add(callback);
            }
        }

        public void Unsubscribe(string actionName, Action<InputActionEventArgs> callback)
        {
            if (string.IsNullOrEmpty(actionName) || callback == null) return;

            if (_subscriptions.ContainsKey(actionName))
            {
                _subscriptions[actionName].Remove(callback);

                // Clean up empty lists
                if (_subscriptions[actionName].Count == 0)
                {
                    _subscriptions.Remove(actionName);
                }
            }
        }

        public void PublishEvent(InputActionEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.ActionName)) return;

            if (_subscriptions.TryGetValue(eventArgs.ActionName, out var callbacks))
            {
                // Create a copy to avoid issues with concurrent modification
                var callbacksCopy = callbacks.ToArray();

                foreach (var callback in callbacksCopy)
                {
                    try
                    {
                        callback.Invoke(eventArgs);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error invoking input event callback for '{eventArgs.ActionName}': {ex.Message}");
                    }
                }
            }
        }

        public void ClearSubscriptions()
        {
            _subscriptions.Clear();
        }
    }
}
