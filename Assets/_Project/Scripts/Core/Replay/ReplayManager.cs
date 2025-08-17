using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Manages recording and playback of actor replay sessions in the laboratory environment.
    /// Handles saving and loading replay data to/from JSON files.
    /// </summary>
    public class ReplayManager : MonoBehaviour
    {
        #region Fields
        
        [Header("Recording Configuration")]
        [SerializeField] private ActorRecorder[] _recorders;
        
        [Header("Playback Configuration")]
        [SerializeField] private ActorPlayback[] _players;
        
        [Header("File Settings")]
        [SerializeField] private string _saveFileName = "replay.json";
        
        /// <summary>
        /// Current replay session data containing all actor recordings
        /// </summary>
        private ReplaySession _session;
        
        #endregion
        
        #region Public Methods - Recording
        
        /// <summary>
        /// Starts recording a new replay session by clearing all recorders and initializing a new session
        /// </summary>
        public void StartRecording()
        {
            _session = new ReplaySession();
            
            foreach (var recorder in _recorders)
            {
                recorder.Clear();
            }
            
            Debug.Log("Replay recording started");
        }
        
        /// <summary>
        /// Stops the current recording session, collects data from all recorders, and saves to file
        /// </summary>
        public void StopRecording()
        {
            if (_session == null)
            {
                Debug.LogWarning("No active recording session to stop");
                return;
            }
            
            var actorDataList = new List<ActorReplayData>();
            
            foreach (var recorder in _recorders)
            {
                var replayData = recorder.GetReplayData();
                if (replayData != null)
                {
                    actorDataList.Add(replayData);
                }
            }
            
            _session.actors = actorDataList.ToArray();
            SaveToFile();
            
            Debug.Log($"Replay recording stopped. Recorded {actorDataList.Count} actors");
        }
        
        #endregion
        
        #region Public Methods - Playback
        
        /// <summary>
        /// Starts playback of the saved replay session by loading data and configuring all players
        /// </summary>
        public void StartPlayback()
        {
            LoadFromFile();
            
            if (_session?.actors == null)
            {
                Debug.LogError("No replay session data available for playback");
                return;
            }
            
            int playersConfigured = 0;
            
            foreach (var player in _players)
            {
                var actorData = System.Array.Find(_session.actors, actor => actor.actorName == player.name);
                
                if (actorData != null)
                {
                    player.SetFrames(actorData.frames);
                    player.Play();
                    playersConfigured++;
                }
                else
                {
                    Debug.LogWarning($"No replay data found for player: {player.name}");
                }
            }
            
            Debug.Log($"Replay playback started. Configured {playersConfigured} players");
        }
        
        /// <summary>
        /// Stops playback on all configured players
        /// </summary>
        public void StopPlayback()
        {
            foreach (var player in _players)
            {
                player.Stop();
            }
            
            Debug.Log("Replay playback stopped");
        }
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Gets whether a replay session is currently loaded and available for playback
        /// </summary>
        public bool HasReplaySession => _session?.actors != null && _session.actors.Length > 0;
        
        /// <summary>
        /// Gets the current replay session data
        /// </summary>
        public ReplaySession CurrentSession => _session;
        
        /// <summary>
        /// Gets the configured save file name
        /// </summary>
        public string SaveFileName => _saveFileName;
        
        #endregion
        
        #region Private Methods - File Operations
        
        /// <summary>
        /// Saves the current replay session to a JSON file in the persistent data path
        /// </summary>
        private void SaveToFile()
        {
            if (_session == null)
            {
                Debug.LogError("No session data to save");
                return;
            }
            
            try
            {
                string filePath = GetSaveFilePath();
                string jsonData = JsonUtility.ToJson(_session, true);
                
                // Ensure directory exists
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                
                File.WriteAllText(filePath, jsonData);
                Debug.Log($"Replay session saved successfully to: {filePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save replay session: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Loads a replay session from the JSON file in the persistent data path
        /// </summary>
        private void LoadFromFile()
        {
            try
            {
                string filePath = GetSaveFilePath();
                
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"Replay file not found at: {filePath}");
                    return;
                }
                
                string jsonData = File.ReadAllText(filePath);
                _session = JsonUtility.FromJson<ReplaySession>(jsonData);
                
                if (_session == null)
                {
                    Debug.LogError("Failed to deserialize replay session data");
                    return;
                }
                
                Debug.Log($"Replay session loaded successfully from: {filePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load replay session: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets the full file path for saving/loading replay data
        /// </summary>
        /// <returns>Complete file path for the replay save file</returns>
        private string GetSaveFilePath()
        {
            return Path.Combine(Application.persistentDataPath, "Replays", _saveFileName);
        }
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Validates component configuration on awake
        /// </summary>
        private void Awake()
        {
            ValidateConfiguration();
        }
        
        #endregion
        
        #region Private Methods - Validation
        
        /// <summary>
        /// Validates that all required components are properly configured
        /// </summary>
        private void ValidateConfiguration()
        {
            if (_recorders == null || _recorders.Length == 0)
            {
                Debug.LogWarning("No recorders configured for ReplayManager");
            }
            
            if (_players == null || _players.Length == 0)
            {
                Debug.LogWarning("No players configured for ReplayManager");
            }
            
            if (string.IsNullOrEmpty(_saveFileName))
            {
                Debug.LogWarning("Save file name is empty, using default");
                _saveFileName = "replay.json";
            }
        }
        
        #endregion
    }
}
