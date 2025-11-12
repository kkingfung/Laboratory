using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.Research
{
    /// <summary>
    /// Discovery journal service for logging and retrieving player discoveries.
    /// Extracted from ResearchSubsystemManager for single responsibility.
    /// </summary>
    public class DiscoveryJournalService : IDiscoveryJournalService
    {
        private readonly ResearchSubsystemConfig _config;
        private readonly Func<string, DiscoveryJournal> _getPlayerJournal;
        private readonly Action<DiscoveryJournalEvent> _onDiscoveryLogged;

        public DiscoveryJournalService(ResearchSubsystemConfig config, Func<string, DiscoveryJournal> getPlayerJournal, Action<DiscoveryJournalEvent> onDiscoveryLogged)
        {
            _config = config;
            _getPlayerJournal = getPlayerJournal;
            _onDiscoveryLogged = onDiscoveryLogged;
        }

        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
            Debug.Log("[DiscoveryJournalService] Initialized successfully");
        }

        public async Task<bool> LogDiscoveryAsync(string playerId, DiscoveryData discovery)
        {
            try
            {
                if (string.IsNullOrEmpty(playerId) || discovery == null)
                    return false;

                var journal = _getPlayerJournal(playerId);

                // Check if we're at the discovery limit
                if (journal.entries.Count >= _config.maxDiscoveriesPerPlayer)
                {
                    Debug.LogWarning($"Player {playerId} has reached maximum discoveries limit ({_config.maxDiscoveriesPerPlayer})");
                    return false;
                }

                var entry = new DiscoveryEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    PlayerId = playerId,
                    Data = discovery,
                    CreatedDate = DateTime.Now,
                    IsVerified = true,
                    Tags = new List<string> { discovery.discoveryType.ToString() }
                };

                journal.entries.Add(entry);
                journal.LastUpdated = DateTime.Now;

                // Fire discovery event
                var journalEvent = new DiscoveryJournalEvent
                {
                    PlayerId = playerId,
                    Discovery = discovery,
                    Timestamp = DateTime.Now
                };

                _onDiscoveryLogged?.Invoke(journalEvent);

                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DiscoveryJournalService] Failed to log discovery: {ex.Message}");
                return false;
            }
        }

        public async Task<List<DiscoveryData>> GetPlayerDiscoveriesAsync(string playerId)
        {
            try
            {
                if (string.IsNullOrEmpty(playerId))
                    return new List<DiscoveryData>();

                var journal = _getPlayerJournal(playerId);
                var discoveries = new List<DiscoveryData>();

                foreach (var entry in journal.entries)
                {
                    if (entry.Data != null)
                        discoveries.Add(entry.Data);
                }

                await Task.CompletedTask;
                return discoveries;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DiscoveryJournalService] Failed to get player discoveries: {ex.Message}");
                return new List<DiscoveryData>();
            }
        }
    }
}
