using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Discovery.Data;
using Laboratory.Core.Discovery.Types;
using Laboratory.Core.Discovery.Systems;

namespace Laboratory.Core.Discovery.Services
{
    /// <summary>
    /// Service for managing journal entries
    /// </summary>
    public class JournalEntryService
    {
        private readonly DiscoveryJournalSystem discoverySystem;

        private Dictionary<string, PlayerJournal> playerJournals = new();
        private Dictionary<string, List<JournalEntry>> journalEntries = new();
        private Dictionary<string, DiscoveryStatistics> discoveryStats = new();

        public JournalEntryService(DiscoveryJournalSystem system)
        {
            discoverySystem = system;
        }

        public void Initialize(JournalConfig config)
        {
            InitializePlayerJournals(config);
        }

        private void InitializePlayerJournals(JournalConfig config)
        {
            var localPlayerJournal = new PlayerJournal
            {
                PlayerId = "LocalPlayer",
                JournalName = "My Monster Research Journal",
                CreatedDate = DateTime.UtcNow,
                TotalEntries = 0,
                Settings = new JournalSettings
                {
                    AutoDocumentBreeding = config.autoDocumentBreeding,
                    AutoDocumentDiscoveries = config.autoDocumentDiscoveries,
                    AllowPlayerNotes = config.enablePlayerNotes,
                    ShareWithFriends = false
                }
            };

            playerJournals["LocalPlayer"] = localPlayerJournal;
            journalEntries["LocalPlayer"] = new List<JournalEntry>();
            discoveryStats["LocalPlayer"] = new DiscoveryStatistics();
        }

        public JournalEntry AddJournalEntry(string playerId, JournalEntryType entryType, string title, string content, object associatedData = null)
        {
            if (!journalEntries.ContainsKey(playerId))
            {
                journalEntries[playerId] = new List<JournalEntry>();
            }

            var entry = new JournalEntry
            {
                EntryId = Guid.NewGuid().ToString(),
                PlayerId = playerId,
                EntryType = entryType,
                Title = title,
                Content = content,
                CreatedDate = DateTime.UtcNow,
                AssociatedData = associatedData,
                Tags = ExtractTags(content, entryType)
            };

            journalEntries[playerId].Add(entry);

            // Update journal statistics
            var journal = playerJournals[playerId];
            journal.TotalEntries++;
            journal.LastEntryDate = DateTime.UtcNow;

            Debug.Log($"📝 Journal entry added: {title}");
            return entry;
        }

        public void DocumentGeneticDiscovery(string playerId, GeneticDiscovery discovery)
        {
            var content = GenerateDiscoveryEntryContent(discovery);
            AddJournalEntry(playerId, JournalEntryType.GeneticDiscovery,
                $"Discovery: {discovery.DiscoveryName}", content, discovery);

            // Update discovery statistics
            var stats = discoveryStats[playerId];
            stats.TotalDiscoveries++;
            stats.LastDiscoveryDate = DateTime.UtcNow;

            Debug.Log($"🧬 Genetic discovery documented: {discovery.DiscoveryName}");
        }

        public JournalEntry AddPlayerObservation(string playerId, string observation, string hypothesis = "")
        {
            if (!discoverySystem.EnablePlayerNotes) return null;

            var content = $"Observation: {observation}";
            if (!string.IsNullOrEmpty(hypothesis))
            {
                content += $"\n\nHypothesis: {hypothesis}";
            }

            return AddJournalEntry(playerId, JournalEntryType.PlayerObservation,
                "Personal Observation", content);
        }

        public List<JournalEntry> GetJournalEntries(string playerId, JournalEntryType? entryType = null, int limit = 50)
        {
            if (!journalEntries.TryGetValue(playerId, out var entries))
                return new List<JournalEntry>();

            var filteredEntries = entryType.HasValue
                ? entries.Where(e => e.EntryType == entryType.Value)
                : entries;

            return filteredEntries.OrderByDescending(e => e.CreatedDate).Take(limit).ToList();
        }

        public DiscoveryStatistics GetDiscoveryStatistics(string playerId)
        {
            return discoveryStats.TryGetValue(playerId, out var stats) ? stats : new DiscoveryStatistics();
        }

        public List<JournalEntry> SearchJournal(string playerId, string searchTerm)
        {
            if (!journalEntries.TryGetValue(playerId, out var entries))
                return new List<JournalEntry>();

            var searchTermLower = searchTerm.ToLower();

            return entries.Where(e =>
                e.Title.ToLower().Contains(searchTermLower) ||
                e.Content.ToLower().Contains(searchTermLower) ||
                e.Tags.Any(tag => tag.ToLower().Contains(searchTermLower))
            ).OrderByDescending(e => e.CreatedDate).ToList();
        }

        private string GenerateDiscoveryEntryContent(GeneticDiscovery discovery)
        {
            var content = $"Genetic Discovery: {discovery.DiscoveryName}\n\n";
            content += $"Description: {discovery.Description}\n\n";
            content += $"Discovery Type: {discovery.DiscoveryType}\n";
            content += $"Significance Level: {discovery.Significance}\n\n";
            content += "This discovery contributes to our understanding of monster genetics and breeding patterns.";

            return content;
        }

        private List<string> ExtractTags(string content, JournalEntryType entryType)
        {
            var tags = new List<string>();

            // Add automatic tags based on entry type
            tags.Add(entryType.ToString());

            // Extract content-based tags
            if (content.ToLower().Contains("inherit"))
                tags.Add("Inheritance");

            if (content.ToLower().Contains("mutation"))
                tags.Add("Mutation");

            if (content.ToLower().Contains("discovery"))
                tags.Add("Discovery");

            if (content.ToLower().Contains("dominant"))
                tags.Add("Dominance");

            return tags;
        }
    }
}