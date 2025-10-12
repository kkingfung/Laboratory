using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.Companion
{
    /// <summary>
    /// Implementation of offline progress calculation and application service
    /// </summary>
    public class OfflineProgressService : IOfflineProgressService
    {
        private readonly CompanionSubsystemConfig _config;
        private readonly Dictionary<string, OfflineProgressSettings> _userSettings = new();

        public OfflineProgressService(CompanionSubsystemConfig config)
        {
            _config = config;
        }

        public async Task<bool> InitializeAsync()
        {
            Debug.Log("[OfflineProgressService] Initializing offline progress service...");
            await Task.Delay(50);
            return true;
        }

        public OfflineProgress CalculateOfflineProgress(string userId, DateTime lastOnlineTime)
        {
            var timeOffline = DateTime.Now - lastOnlineTime;
            Debug.Log($"[OfflineProgressService] Calculating progress for {userId}, offline for: {timeOffline}");

            return new OfflineProgress
            {
                userId = userId,
                lastOnlineTime = lastOnlineTime,
                calculationTime = DateTime.Now,
                timeOffline = timeOffline,
                settings = GetOfflineSettings(userId),
                isProcessed = false
            };
        }

        public OfflineRewards CalculateOfflineRewards(OfflineProgress progress)
        {
            var multiplier = progress.settings.progressRateMultiplier;
            var offlineHours = (float)progress.timeOffline.TotalHours;

            Debug.Log($"[OfflineProgressService] Calculating rewards for {offlineHours} hours offline");

            return new OfflineRewards
            {
                userId = progress.userId,
                timeOffline = progress.timeOffline,
                calculationTime = DateTime.Now,
                researchProgress = offlineHours * multiplier * 0.1f,
                experienceGained = Mathf.RoundToInt(offlineHours * multiplier * 5f)
            };
        }

        public async Task<bool> ApplyOfflineRewardsAsync(string userId, OfflineRewards rewards)
        {
            Debug.Log($"[OfflineProgressService] Applying offline rewards for user: {userId}");
            await Task.Delay(100);
            return true;
        }

        public void UpdateOfflineSettings(string userId, OfflineProgressSettings settings)
        {
            Debug.Log($"[OfflineProgressService] Updating settings for user: {userId}");
            _userSettings[userId] = settings;
        }

        public OfflineProgressSettings GetOfflineSettings(string userId)
        {
            if (_userSettings.TryGetValue(userId, out var settings))
            {
                return settings;
            }

            return new OfflineProgressSettings();
        }
    }
}