using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Events;

namespace Laboratory.Audio
{
    /// <summary>
    /// Audio event system for handling audio-related events
    /// </summary>
    public class AudioEventSystem
    {
        #region Events

        /// <summary>
        /// Triggered when audio playback starts
        /// </summary>
        public event Action<AudioPlaybackEventArgs> OnAudioPlaybackStarted;

        /// <summary>
        /// Triggered when audio playback stops
        /// </summary>
        public event Action<AudioPlaybackEventArgs> OnAudioPlaybackStopped;

        /// <summary>
        /// Triggered when audio volume changes
        /// </summary>
        public event Action<AudioVolumeChangedEventArgs> OnVolumeChanged;

        /// <summary>
        /// Triggered when audio settings change
        /// </summary>
        public event Action<AudioSettingsChangedEventArgs> OnAudioSettingsChanged;

        /// <summary>
        /// Triggered when an audio error occurs
        /// </summary>
        public event Action<AudioErrorEventArgs> OnAudioError;

        #endregion

        #region Private Fields

        private readonly Dictionary<string, List<IAudioEventListener>> _listeners = new();
        private readonly Queue<AudioEvent> _eventQueue = new();
        private bool _isProcessingEvents = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public AudioEventSystem()
        {
            // Default initialization
        }

        /// <summary>
        /// Constructor with service container (for dependency injection)
        /// </summary>
        public AudioEventSystem(object container)
        {
            // Initialize with container if needed
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Publishes an audio event
        /// </summary>
        public void PublishEvent<T>(T audioEvent) where T : AudioEvent
        {
            if (audioEvent == null) return;

            _eventQueue.Enqueue(audioEvent);
            
            if (!_isProcessingEvents)
            {
                ProcessEventQueue();
            }
        }

        /// <summary>
        /// Subscribes a listener to specific audio events
        /// </summary>
        public void Subscribe(string eventType, IAudioEventListener listener)
        {
            if (string.IsNullOrEmpty(eventType) || listener == null) return;

            if (!_listeners.ContainsKey(eventType))
            {
                _listeners[eventType] = new List<IAudioEventListener>();
            }

            if (!_listeners[eventType].Contains(listener))
            {
                _listeners[eventType].Add(listener);
            }
        }

        /// <summary>
        /// Unsubscribes a listener from specific audio events
        /// </summary>
        public void Unsubscribe(string eventType, IAudioEventListener listener)
        {
            if (string.IsNullOrEmpty(eventType) || listener == null) return;

            if (_listeners.ContainsKey(eventType))
            {
                _listeners[eventType].Remove(listener);
                
                if (_listeners[eventType].Count == 0)
                {
                    _listeners.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// Clears all event listeners
        /// </summary>
        public void ClearAllListeners()
        {
            _listeners.Clear();
        }

        /// <summary>
        /// Triggers audio playback started event
        /// </summary>
        public void TriggerPlaybackStarted(string audioId, AudioClip clip, AudioCategory category)
        {
            var eventArgs = new AudioPlaybackEventArgs(audioId, clip, category, AudioPlaybackState.Started);
            OnAudioPlaybackStarted?.Invoke(eventArgs);
        }

        /// <summary>
        /// Triggers audio playback stopped event
        /// </summary>
        public void TriggerPlaybackStopped(string audioId, AudioClip clip, AudioCategory category)
        {
            var eventArgs = new AudioPlaybackEventArgs(audioId, clip, category, AudioPlaybackState.Stopped);
            OnAudioPlaybackStopped?.Invoke(eventArgs);
        }

        /// <summary>
        /// Triggers volume changed event
        /// </summary>
        public void TriggerVolumeChanged(AudioCategory category, float oldVolume, float newVolume)
        {
            var eventArgs = new AudioVolumeChangedEventArgs(category, oldVolume, newVolume);
            OnVolumeChanged?.Invoke(eventArgs);
        }

        /// <summary>
        /// Triggers audio settings changed event
        /// </summary>
        public void TriggerAudioSettingsChanged(string settingName, object oldValue, object newValue)
        {
            var eventArgs = new AudioSettingsChangedEventArgs(settingName, oldValue, newValue);
            OnAudioSettingsChanged?.Invoke(eventArgs);
        }

        /// <summary>
        /// Triggers audio error event
        /// </summary>
        public void TriggerAudioError(string errorMessage, AudioClip failedClip = null)
        {
            var eventArgs = new AudioErrorEventArgs(errorMessage, failedClip);
            OnAudioError?.Invoke(eventArgs);
        }

        #endregion

        #region Private Methods

        private void ProcessEventQueue()
        {
            _isProcessingEvents = true;

            while (_eventQueue.Count > 0)
            {
                var audioEvent = _eventQueue.Dequeue();
                ProcessEvent(audioEvent);
            }

            _isProcessingEvents = false;
        }

        private void ProcessEvent(AudioEvent audioEvent)
        {
            var eventType = audioEvent.GetType().Name;
            
            if (_listeners.ContainsKey(eventType))
            {
                foreach (var listener in _listeners[eventType])
                {
                    try
                    {
                        listener.OnAudioEvent(audioEvent);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing audio event {eventType}: {ex.Message}");
                    }
                }
            }
        }

        #endregion
    }

    #region Event Args Classes

    public class AudioPlaybackEventArgs : EventArgs
    {
        public string AudioId { get; }
        public AudioClip Clip { get; }
        public AudioCategory Category { get; }
        public AudioPlaybackState State { get; }
        public float Timestamp { get; }

        public AudioPlaybackEventArgs(string audioId, AudioClip clip, AudioCategory category, AudioPlaybackState state)
        {
            AudioId = audioId;
            Clip = clip;
            Category = category;
            State = state;
            Timestamp = Time.time;
        }
    }

    public class AudioVolumeChangedEventArgs : EventArgs
    {
        public AudioCategory Category { get; }
        public float OldVolume { get; }
        public float NewVolume { get; }
        public float VolumeChange => NewVolume - OldVolume;
        public float Timestamp { get; }

        public AudioVolumeChangedEventArgs(AudioCategory category, float oldVolume, float newVolume)
        {
            Category = category;
            OldVolume = oldVolume;
            NewVolume = newVolume;
            Timestamp = Time.time;
        }
    }

    public class AudioSettingsChangedEventArgs : EventArgs
    {
        public string SettingName { get; }
        public object OldValue { get; }
        public object NewValue { get; }
        public float Timestamp { get; }

        public AudioSettingsChangedEventArgs(string settingName, object oldValue, object newValue)
        {
            SettingName = settingName;
            OldValue = oldValue;
            NewValue = newValue;
            Timestamp = Time.time;
        }
    }

    public class AudioErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }
        public AudioClip FailedClip { get; }
        public float Timestamp { get; }

        public AudioErrorEventArgs(string errorMessage, AudioClip failedClip = null)
        {
            ErrorMessage = errorMessage;
            FailedClip = failedClip;
            Timestamp = Time.time;
        }
    }

    #endregion

    #region Supporting Types

    public enum AudioPlaybackState
    {
        Started,
        Paused,
        Resumed,
        Stopped,
        Completed,
        Failed
    }

    // AudioCategory enum removed - using existing definition

    public abstract class AudioEvent
    {
        public float Timestamp { get; } = Time.time;
        public string EventId { get; } = Guid.NewGuid().ToString();
    }

    public interface IAudioEventListener
    {
        void OnAudioEvent(AudioEvent audioEvent);
    }

    #endregion
}
