using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.Research
{
    /// <summary>
    /// Curriculum integration service for tracking educational progress through gameplay.
    /// Extracted from ResearchSubsystemManager for single responsibility.
    /// </summary>
    public class CurriculumIntegrationService : ICurriculumIntegrationService
    {
        private readonly ResearchSubsystemConfig _config;
        private readonly Action<CurriculumProgressEvent> _onCurriculumProgress;
        private readonly Dictionary<string, CurriculumProgress> _studentProgress = new();
        private readonly Dictionary<string, Dictionary<string, float>> _moduleProgress = new();

        public CurriculumIntegrationService(ResearchSubsystemConfig config, Action<CurriculumProgressEvent> onCurriculumProgress)
        {
            _config = config;
            _onCurriculumProgress = onCurriculumProgress;
        }

        public async Task InitializeAsync()
        {
            // Initialize curriculum mappings if configured
            if (_config.curriculumMappings != null)
            {
                foreach (var mapping in _config.curriculumMappings)
                {
                    if (!_moduleProgress.ContainsKey(mapping.moduleId))
                    {
                        _moduleProgress[mapping.moduleId] = new Dictionary<string, float>();
                    }
                }
            }

            await Task.CompletedTask;
            Debug.Log("[CurriculumIntegrationService] Initialized successfully");
        }

        public async Task<bool> UpdateProgressAsync(string playerId, string moduleId, float progress)
        {
            try
            {
                if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(moduleId))
                    return false;

                // Clamp progress to valid range
                progress = Mathf.Clamp01(progress);

                // Update module progress for the player
                if (!_moduleProgress.ContainsKey(moduleId))
                    _moduleProgress[moduleId] = new Dictionary<string, float>();

                var previousProgress = _moduleProgress[moduleId].GetValueOrDefault(playerId, 0f);
                _moduleProgress[moduleId][playerId] = progress;

                // Update overall curriculum progress
                var curriculumProgress = GetOrCreateStudentProgress(playerId);
                curriculumProgress.ModuleProgress[moduleId] = progress;
                curriculumProgress.LastUpdated = DateTime.Now;

                // Calculate completed modules
                curriculumProgress.CompletedModules = curriculumProgress.ModuleProgress.Values.Count(p => p >= 1f);

                // Fire progress event if there was meaningful change
                if (Math.Abs(progress - previousProgress) > 0.01f)
                {
                    var progressEvent = new CurriculumProgressEvent
                    {
                        PlayerId = playerId,
                        ModuleId = moduleId,
                        Progress = progress,
                        Timestamp = DateTime.Now
                    };

                    _onCurriculumProgress?.Invoke(progressEvent);
                }

                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CurriculumIntegrationService] Failed to update progress: {ex.Message}");
                return false;
            }
        }

        public async Task<CurriculumProgress> GetPlayerProgressAsync(string playerId)
        {
            try
            {
                if (string.IsNullOrEmpty(playerId))
                    return new CurriculumProgress { PlayerId = playerId };

                var progress = GetOrCreateStudentProgress(playerId);
                await Task.CompletedTask;
                return progress;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CurriculumIntegrationService] Failed to get player progress: {ex.Message}");
                return new CurriculumProgress { PlayerId = playerId };
            }
        }

        public void UpdateAllProgress()
        {
            try
            {
                // Update progress for all students based on recent discoveries
                foreach (var studentProgress in _studentProgress.Values)
                {
                    RecalculateProgressFromDiscoveries(studentProgress.PlayerId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CurriculumIntegrationService] Failed to update all progress: {ex.Message}");
            }
        }

        public void UpdateStudentProgress(CurriculumProgressEvent curriculumEvent)
        {
            try
            {
                if (curriculumEvent == null)
                    return;

                var progress = GetOrCreateStudentProgress(curriculumEvent.PlayerId);
                progress.ModuleProgress[curriculumEvent.ModuleId] = curriculumEvent.Progress;
                progress.LastUpdated = DateTime.Now;

                // Recalculate completed modules
                progress.CompletedModules = progress.ModuleProgress.Values.Count(p => p >= 1f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CurriculumIntegrationService] Failed to update student progress: {ex.Message}");
            }
        }

        public CurriculumProgress GetStudentProgress(string studentId, string curriculumId)
        {
            try
            {
                var progress = GetOrCreateStudentProgress(studentId);

                // Filter progress by curriculum if specified
                if (!string.IsNullOrEmpty(curriculumId))
                {
                    var filteredProgress = new CurriculumProgress
                    {
                        PlayerId = studentId,
                        LastUpdated = progress.LastUpdated
                    };

                    foreach (var moduleProgress in progress.ModuleProgress)
                    {
                        // In a real implementation, you would check if the module belongs to the curriculum
                        // For now, we'll include all modules
                        filteredProgress.ModuleProgress[moduleProgress.Key] = moduleProgress.Value;
                    }

                    filteredProgress.CompletedModules = filteredProgress.ModuleProgress.Values.Count(p => p >= 1f);
                    return filteredProgress;
                }

                return progress;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CurriculumIntegrationService] Failed to get student progress: {ex.Message}");
                return new CurriculumProgress { PlayerId = studentId };
            }
        }

        private CurriculumProgress GetOrCreateStudentProgress(string playerId)
        {
            if (!_studentProgress.TryGetValue(playerId, out var progress))
            {
                progress = new CurriculumProgress
                {
                    PlayerId = playerId,
                    ModuleProgress = new Dictionary<string, float>(),
                    CompletedModules = 0,
                    LastUpdated = DateTime.Now
                };
                _studentProgress[playerId] = progress;
            }
            return progress;
        }

        private void RecalculateProgressFromDiscoveries(string playerId)
        {
            // In a real implementation, this would analyze the player's discoveries
            // and update curriculum progress based on curriculum mappings
            var progress = GetOrCreateStudentProgress(playerId);

            if (_config.curriculumMappings != null)
            {
                foreach (var mapping in _config.curriculumMappings)
                {
                    // Calculate progress based on discovery types
                    var moduleProgress = CalculateModuleProgressFromDiscoveries(playerId, mapping);
                    progress.ModuleProgress[mapping.moduleId] = moduleProgress;
                }

                // Calculate completed modules based on each mapping's completion threshold
                progress.CompletedModules = 0;
                foreach (var mapping in _config.curriculumMappings)
                {
                    if (progress.ModuleProgress.TryGetValue(mapping.moduleId, out var moduleProgress) &&
                        moduleProgress >= mapping.completionThreshold)
                    {
                        progress.CompletedModules++;
                    }
                }
                progress.LastUpdated = DateTime.Now;
            }
        }

        private float CalculateModuleProgressFromDiscoveries(string playerId, CurriculumMapping mapping)
        {
            // This would analyze player discoveries against required discovery types
            // For now, return a simulated progress value
            return UnityEngine.Random.Range(0f, 1f);
        }
    }
}
