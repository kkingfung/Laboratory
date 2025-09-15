using System;
using UnityEngine;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Container for a complete replay session including metadata and actor data.
    /// Supports serialization for saving and loading replay files.
    /// </summary>
    [Serializable]
    public class ReplaySession
    {
        #region Fields

        [Header("Session Information")]
        [SerializeField] private string _sessionId;
        [SerializeField] private string _sessionName;
        [SerializeField] private DateTime _creationTime;
        [SerializeField] private DateTime _recordingTime;

        [Header("Recording Details")]
        [SerializeField] private float _duration;
        [SerializeField] private int _frameRate;
        [SerializeField] private int _totalFrames;
        [SerializeField] private string _mapName;
        [SerializeField] private string _gameMode;

        [Header("Actor Data")]
        [SerializeField] private ActorReplayData[] _actors;
        [SerializeField] private int _actorCount;

        [Header("Metadata")]
        [SerializeField] private string _version = "1.0.0";
        [SerializeField] private string _description;
        [SerializeField] private string[] _tags;
        [SerializeField] private bool _isCompressed;

        // Runtime state
        private bool _isLoaded = false;
        private float _currentPlaybackTime = 0f;
        private int _currentFrame = 0;

        #endregion

        #region Properties

        /// <summary>
        /// Unique identifier for the replay session
        /// </summary>
        public string SessionId => _sessionId;

        /// <summary>
        /// Human-readable name for the replay session
        /// </summary>
        public string SessionName => _sessionName;

        /// <summary>
        /// When the replay session was created
        /// </summary>
        public DateTime CreationTime => _creationTime;

        /// <summary>
        /// When the original recording was made
        /// </summary>
        public DateTime RecordingTime => _recordingTime;

        /// <summary>
        /// Total duration of the replay in seconds
        /// </summary>
        public float Duration => _duration;

        /// <summary>
        /// Frame rate of the original recording
        /// </summary>
        public int FrameRate => _frameRate;

        /// <summary>
        /// Total number of frames in the replay
        /// </summary>
        public int TotalFrames => _totalFrames;

        /// <summary>
        /// Name of the map/level where recording occurred
        /// </summary>
        public string MapName => _mapName;

        /// <summary>
        /// Game mode during recording
        /// </summary>
        public string GameMode => _gameMode;

        /// <summary>
        /// Array of all actor replay data
        /// </summary>
        public ActorReplayData[] Actors => _actors;

        /// <summary>
        /// Number of actors in the replay
        /// </summary>
        public int ActorCount => _actorCount;

        /// <summary>
        /// Version of the replay format
        /// </summary>
        public string Version => _version;

        /// <summary>
        /// Description of the replay content
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Tags for categorizing the replay
        /// </summary>
        public string[] Tags => _tags;

        /// <summary>
        /// Whether the replay data is compressed
        /// </summary>
        public bool IsCompressed => _isCompressed;

        /// <summary>
        /// Whether the replay session has been loaded
        /// </summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// Current playback time in seconds
        /// </summary>
        public float CurrentPlaybackTime => _currentPlaybackTime;

        /// <summary>
        /// Current frame index during playback
        /// </summary>
        public int CurrentFrame => _currentFrame;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new replay session
        /// </summary>
        /// <param name="sessionName">Name for the replay session</param>
        /// <param name="mapName">Name of the map/level</param>
        /// <param name="gameMode">Game mode during recording</param>
        /// <param name="frameRate">Recording frame rate</param>
        public ReplaySession(string sessionName, string mapName, string gameMode, int frameRate = 60)
        {
            _sessionId = Guid.NewGuid().ToString();
            _sessionName = sessionName;
            _mapName = mapName;
            _gameMode = gameMode;
            _frameRate = frameRate;
            _creationTime = DateTime.Now;
            _recordingTime = DateTime.Now;
            _actors = new ActorReplayData[0];
            _actorCount = 0;
            _tags = new string[0];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds an actor to the replay session
        /// </summary>
        /// <param name="actorData">Actor replay data to add</param>
        public void AddActor(ActorReplayData actorData)
        {
            if (actorData == null) return;

            Array.Resize(ref _actors, _actorCount + 1);
            _actors[_actorCount] = actorData;
            _actorCount++;

            // Update session duration if this actor has longer data
            if (actorData.TotalDuration > _duration)
            {
                _duration = actorData.TotalDuration;
                _totalFrames = Mathf.RoundToInt(_duration * _frameRate);
            }
        }

        /// <summary>
        /// Removes an actor from the replay session
        /// </summary>
        /// <param name="actorName">Name of the actor to remove</param>
        /// <returns>True if actor was removed successfully</returns>
        public bool RemoveActor(string actorName)
        {
            for (int i = 0; i < _actorCount; i++)
            {
                if (_actors[i].ActorName == actorName)
                {
                    // Shift remaining actors
                    for (int j = i; j < _actorCount - 1; j++)
                    {
                        _actors[j] = _actors[j + 1];
                    }
                    
                    _actorCount--;
                    Array.Resize(ref _actors, _actorCount);
                    
                    RecalculateSessionDuration();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets actor data by name
        /// </summary>
        /// <param name="actorName">Name of the actor to find</param>
        /// <returns>Actor replay data or null if not found</returns>
        public ActorReplayData GetActor(string actorName)
        {
            for (int i = 0; i < _actorCount; i++)
            {
                if (_actors[i].ActorName == actorName)
                {
                    return _actors[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Gets actor data by ID
        /// </summary>
        /// <param name="actorId">ID of the actor to find</param>
        /// <returns>Actor replay data or null if not found</returns>
        public ActorReplayData GetActorById(int actorId)
        {
            for (int i = 0; i < _actorCount; i++)
            {
                if (_actors[i].ActorId == actorId)
                {
                    return _actors[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Sets the replay description
        /// </summary>
        /// <param name="description">Description text</param>
        public void SetDescription(string description)
        {
            _description = description;
        }

        /// <summary>
        /// Adds tags to the replay
        /// </summary>
        /// <param name="newTags">Tags to add</param>
        public void AddTags(string[] newTags)
        {
            if (newTags == null || newTags.Length == 0) return;

            int oldLength = _tags.Length;
            Array.Resize(ref _tags, oldLength + newTags.Length);
            
            for (int i = 0; i < newTags.Length; i++)
            {
                _tags[oldLength + i] = newTags[i];
            }
        }

        /// <summary>
        /// Removes tags from the replay
        /// </summary>
        /// <param name="tagsToRemove">Tags to remove</param>
        public void RemoveTags(string[] tagsToRemove)
        {
            if (tagsToRemove == null || tagsToRemove.Length == 0) return;

            var newTags = new System.Collections.Generic.List<string>(_tags);
            
            foreach (var tag in tagsToRemove)
            {
                newTags.Remove(tag);
            }
            
            _tags = newTags.ToArray();
        }

        /// <summary>
        /// Checks if the replay has a specific tag
        /// </summary>
        /// <param name="tag">Tag to check for</param>
        /// <returns>True if the tag exists</returns>
        public bool HasTag(string tag)
        {
            return Array.Exists(_tags, t => t == tag);
        }

        /// <summary>
        /// Compresses all actor data in the session
        /// </summary>
        public void CompressSession()
        {
            if (_isCompressed) return;

            foreach (var actor in _actors)
            {
                if (actor != null)
                {
                    actor.Compress();
                }
            }
            
            _isCompressed = true;
        }

        /// <summary>
        /// Decompresses all actor data in the session
        /// </summary>
        public void DecompressSession()
        {
            if (!_isCompressed) return;

            foreach (var actor in _actors)
            {
                if (actor != null)
                {
                    actor.Decompress();
                }
            }
            
            _isCompressed = false;
        }

        /// <summary>
        /// Sets the current playback time
        /// </summary>
        /// <param name="time">Time in seconds</param>
        public void SetPlaybackTime(float time)
        {
            _currentPlaybackTime = Mathf.Clamp(time, 0f, _duration);
            _currentFrame = Mathf.RoundToInt(_currentPlaybackTime * _frameRate);
        }

        /// <summary>
        /// Sets the current frame index
        /// </summary>
        /// <param name="frame">Frame index</param>
        public void SetCurrentFrame(int frame)
        {
            _currentFrame = Mathf.Clamp(frame, 0, _totalFrames);
            _currentPlaybackTime = _currentFrame / (float)_frameRate;
        }

        /// <summary>
        /// Marks the session as loaded
        /// </summary>
        public void MarkAsLoaded()
        {
            _isLoaded = true;
        }

        /// <summary>
        /// Clears all session data
        /// </summary>
        public void Clear()
        {
            _actors = new ActorReplayData[0];
            _actorCount = 0;
            _duration = 0f;
            _totalFrames = 0;
            _currentPlaybackTime = 0f;
            _currentFrame = 0;
            _isLoaded = false;
            _isCompressed = false;
        }

        #endregion

        #region Private Methods

        private void RecalculateSessionDuration()
        {
            _duration = 0f;
            _totalFrames = 0;

            foreach (var actor in _actors)
            {
                if (actor != null && actor.TotalDuration > _duration)
                {
                    _duration = actor.TotalDuration;
                    _totalFrames = Mathf.RoundToInt(_duration * _frameRate);
                }
            }
        }

        #endregion
    }
}
