using UnityEngine;
using System.Collections.Generic;
using Laboratory.Core.Enums;
using Laboratory.Chimera.Progression;

namespace Laboratory.Subsystems.Gameplay
{
    /// <summary>
    /// Manages genre-specific gameplay rules and configurations
    /// Coordinates the 47 game genres in Project Chimera
    /// </summary>
    public class GenreManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameplayConfig config;

        // Genre-specific data
        private Dictionary<ActivityGenreCategory, GenreData> _genreData = new Dictionary<ActivityGenreCategory, GenreData>();

        // Current active genre
        private ActivityGenreCategory _currentGenre;
        private bool _isGenreActive = false;

        // Events
        public event System.Action<ActivityGenreCategory> OnGenreActivated;
        public event System.Action<ActivityGenreCategory> OnGenreDeactivated;
        public event System.Action<ActivityGenreCategory, ActivityGenreCategory> OnGenreChanged;

        private void Awake()
        {
            InitializeGenres();
        }

        /// <summary>
        /// Initialize all genre data
        /// Maps to the 7 core ActivityGenreCategory types
        /// </summary>
        private void InitializeGenres()
        {
            // Register the 7 core genre categories from ProgressionComponents
            RegisterGenre(ActivityGenreCategory.Action, "Action", requires3D: true, requiresPhysics: true);
            RegisterGenre(ActivityGenreCategory.Strategy, "Strategy", requires3D: false, requiresPhysics: false);
            RegisterGenre(ActivityGenreCategory.Puzzle, "Puzzle", requires3D: false, requiresPhysics: false);
            RegisterGenre(ActivityGenreCategory.Racing, "Racing", requires3D: true, requiresPhysics: true);
            RegisterGenre(ActivityGenreCategory.Rhythm, "Rhythm", requires3D: false, requiresPhysics: false);
            RegisterGenre(ActivityGenreCategory.Exploration, "Exploration", requires3D: true, requiresPhysics: false);
            RegisterGenre(ActivityGenreCategory.Economics, "Economics", requires3D: false, requiresPhysics: false);

            Debug.Log($"[GenreManager] Initialized {_genreData.Count} genres");
        }

        /// <summary>
        /// Register a genre with its configuration
        /// </summary>
        private void RegisterGenre(ActivityGenreCategory category, string displayName, bool requires3D, bool requiresPhysics)
        {
            _genreData[category] = new GenreData
            {
                Category = category,
                DisplayName = displayName,
                Requires3D = requires3D,
                RequiresPhysics = requiresPhysics,
                IsActive = false
            };
        }

        /// <summary>
        /// Activate a specific genre
        /// </summary>
        public void ActivateGenre(ActivityGenreCategory genre)
        {
            if (_isGenreActive && _currentGenre == genre)
            {
                Debug.LogWarning($"[GenreManager] Genre {genre} is already active");
                return;
            }

            ActivityGenreCategory previousGenre = _currentGenre;

            if (_isGenreActive)
            {
                DeactivateCurrentGenre();
            }

            _currentGenre = genre;
            _isGenreActive = true;

            if (_genreData.ContainsKey(genre))
            {
                _genreData[genre].IsActive = true;
            }

            OnGenreActivated?.Invoke(genre);

            if (_isGenreActive)
            {
                OnGenreChanged?.Invoke(previousGenre, genre);
            }

            Debug.Log($"[GenreManager] Activated genre: {genre}");
        }

        /// <summary>
        /// Deactivate current genre
        /// </summary>
        public void DeactivateCurrentGenre()
        {
            if (!_isGenreActive) return;

            if (_genreData.ContainsKey(_currentGenre))
            {
                _genreData[_currentGenre].IsActive = false;
            }

            OnGenreDeactivated?.Invoke(_currentGenre);

            _isGenreActive = false;
            Debug.Log($"[GenreManager] Deactivated genre: {_currentGenre}");
        }

        /// <summary>
        /// Get current active genre
        /// </summary>
        public ActivityGenreCategory GetCurrentGenre()
        {
            return _currentGenre;
        }

        /// <summary>
        /// Check if a genre is currently active
        /// </summary>
        public bool IsGenreActive()
        {
            return _isGenreActive;
        }

        /// <summary>
        /// Get genre data for a specific category
        /// </summary>
        public GenreData GetGenreData(ActivityGenreCategory category)
        {
            if (_genreData.TryGetValue(category, out GenreData data))
            {
                return data;
            }

            return null;
        }

        /// <summary>
        /// Get all registered genres
        /// </summary>
        public IEnumerable<ActivityGenreCategory> GetAllGenres()
        {
            return _genreData.Keys;
        }

        /// <summary>
        /// Data structure for genre information
        /// </summary>
        public class GenreData
        {
            public ActivityGenreCategory Category;
            public string DisplayName;
            public bool Requires3D;
            public bool RequiresPhysics;
            public bool IsActive;
        }
    }
}
