using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Activities.Types;

namespace Laboratory.Core.Activities
{
    /// <summary>
    /// Master library of all 47 game genre configurations
    /// Provides centralized access to genre settings for the activity system
    /// Used by both ActivitySystem and PartnershipActivitySystem
    /// </summary>
    [CreateAssetMenu(fileName = "GenreLibrary", menuName = "Chimera/Genre Library")]
    public class GenreLibrary : ScriptableObject
    {
        [Header("Genre Configurations")]
        [Tooltip("All 47 genre configurations organized by category")]
        public GenreConfiguration[] allGenres = new GenreConfiguration[47];

        // Cached lookup for performance
        private Dictionary<ActivityType, GenreConfiguration> _genreLookup;

        /// <summary>
        /// Initialize genre lookup cache on first access
        /// </summary>
        private void OnEnable()
        {
            BuildGenreLookup();
        }

        /// <summary>
        /// Builds genre lookup dictionary for O(1) access
        /// </summary>
        private void BuildGenreLookup()
        {
            _genreLookup = new Dictionary<ActivityType, GenreConfiguration>();

            if (allGenres == null)
                return;

            foreach (var genre in allGenres)
            {
                if (genre != null)
                {
                    _genreLookup[genre.genreType] = genre;
                }
            }
        }

        /// <summary>
        /// Get genre configuration by activity type
        /// </summary>
        public GenreConfiguration GetGenreConfig(ActivityType activityType)
        {
            if (_genreLookup == null || _genreLookup.Count == 0)
                BuildGenreLookup();

            if (_genreLookup.TryGetValue(activityType, out var config))
                return config;

            Debug.LogWarning($"No genre configuration found for {activityType}");
            return null;
        }

        /// <summary>
        /// Get all genres in a specific category
        /// </summary>
        public GenreConfiguration[] GetGenresByCategory(string category)
        {
            if (allGenres == null)
                return System.Array.Empty<GenreConfiguration>();

            return allGenres.Where(g => g != null && g.displayName.Contains(category)).ToArray();
        }

        /// <summary>
        /// Validates that all 47 genres have configurations
        /// </summary>
        public bool ValidateCompleteness()
        {
            if (allGenres == null || allGenres.Length != 47)
            {
                Debug.LogError($"Genre library has {allGenres?.Length ?? 0} entries, expected 47");
                return false;
            }

            int nullCount = allGenres.Count(g => g == null);
            if (nullCount > 0)
            {
                Debug.LogWarning($"Genre library has {nullCount} null entries");
                return false;
            }

            // Check for duplicates
            var duplicates = allGenres
                .Where(g => g != null)
                .GroupBy(g => g.genreType)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                Debug.LogError($"Genre library has duplicate entries: {string.Join(", ", duplicates)}");
                return false;
            }

            Debug.Log("Genre library validation passed: All 47 genres configured");
            return true;
        }

        /// <summary>
        /// Get statistics about genre configurations
        /// </summary>
        public void PrintStatistics()
        {
            if (allGenres == null)
            {
                Debug.Log("Genre library is empty");
                return;
            }

            int configured = allGenres.Count(g => g != null);
            Debug.Log($"=== GENRE LIBRARY STATISTICS ===");
            Debug.Log($"Total Slots: {allGenres.Length}");
            Debug.Log($"Configured: {configured}");
            Debug.Log($"Missing: {allGenres.Length - configured}");

            // Count by primary player skill
            var skillDistribution = allGenres
                .Where(g => g != null)
                .GroupBy(g => g.primaryPlayerSkill)
                .OrderByDescending(group => group.Count());

            Debug.Log("\nPlayer Skill Distribution:");
            foreach (var group in skillDistribution)
            {
                Debug.Log($"  {group.Key}: {group.Count()} genres");
            }

            // Count by primary chimera trait
            var traitDistribution = allGenres
                .Where(g => g != null)
                .GroupBy(g => g.primaryChimeraTrait)
                .OrderByDescending(group => group.Count());

            Debug.Log("\nChimera Trait Distribution:");
            foreach (var group in traitDistribution)
            {
                Debug.Log($"  {group.Key}: {group.Count()} genres");
            }
        }
    }
}
