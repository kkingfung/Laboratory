using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Discovery.Data;
using Laboratory.Core.Discovery.Types;
using Laboratory.Core.Discovery;

namespace Laboratory.Core.Discovery.Services
{
    /// <summary>
    /// Service for managing achievements and rewards
    /// </summary>
    public class AchievementService
    {
        private readonly DiscoveryJournalSystem discoverySystem;
        private AchievementDatabase achievementDatabase;
        private Dictionary<string, List<Achievement>> unlockedAchievements = new();

        public AchievementService(DiscoveryJournalSystem system, AchievementDatabase database)
        {
            discoverySystem = system;
            achievementDatabase = database;
        }

        public void Initialize(AchievementDatabase database)
        {
            achievementDatabase = database;

            if (achievementDatabase != null && achievementDatabase.achievements.Count == 0)
            {
                CreateDefaultAchievements();
            }

            unlockedAchievements["LocalPlayer"] = new List<Achievement>();
        }

        public void CheckJournalAchievements(string playerId, PlayerJournal journal, List<JournalEntry> entries)
        {
            if (!discoverySystem.EnableAchievements || achievementDatabase == null) return;

            var playerAchievements = unlockedAchievements[playerId];

            foreach (var achievement in achievementDatabase.achievements)
            {
                if (playerAchievements.Any(a => a.AchievementId == achievement.AchievementId))
                    continue; // Already unlocked

                if (CheckAchievementCondition(achievement, playerId, journal, entries))
                {
                    UnlockAchievement(playerId, achievement);
                }
            }
        }

        public void CheckDiscoveryAchievements(string playerId, DiscoveryStatistics stats)
        {
            // Special achievements for discovery milestones
            if (stats.TotalDiscoveries == 1)
            {
                var achievement = new Achievement
                {
                    AchievementId = "first_discovery",
                    Title = "First Discovery",
                    Description = "Made your first genetic discovery",
                    Type = AchievementType.GeneticDiscoveries,
                    UnlockedDate = DateTime.UtcNow
                };
                UnlockAchievement(playerId, achievement);
            }
        }

        private bool CheckAchievementCondition(Achievement achievement, string playerId, PlayerJournal journal, List<JournalEntry> entries)
        {
            switch (achievement.Type)
            {
                case AchievementType.JournalEntries:
                    return journal.TotalEntries >= achievement.RequiredValue;

                case AchievementType.BreedingDocumentation:
                    return entries.Count(e => e.EntryType == JournalEntryType.BreedingResult) >= achievement.RequiredValue;

                case AchievementType.GeneticDiscoveries:
                    // This would need access to genetic discoveries count
                    return false; // Simplified for now

                case AchievementType.ResearchProjects:
                    // This would need access to completed research projects count
                    return false; // Simplified for now

                case AchievementType.ScientificMethod:
                    return entries.Count(e => e.EntryType == JournalEntryType.PlayerObservation) >= achievement.RequiredValue;

                default:
                    return false;
            }
        }

        private void UnlockAchievement(string playerId, Achievement achievement)
        {
            var unlockedAchievement = new Achievement
            {
                AchievementId = achievement.AchievementId,
                Title = achievement.Title,
                Description = achievement.Description,
                Type = achievement.Type,
                RequiredValue = achievement.RequiredValue,
                UnlockedDate = DateTime.UtcNow,
                Rewards = achievement.Rewards
            };

            unlockedAchievements[playerId].Add(unlockedAchievement);
            discoverySystem.TriggerAchievementUnlocked(unlockedAchievement);

            Debug.Log($"🏆 Achievement unlocked: {achievement.Title}");
        }

        public List<Achievement> GetUnlockedAchievements(string playerId)
        {
            return unlockedAchievements.TryGetValue(playerId, out var achievements)
                ? new List<Achievement>(achievements)
                : new List<Achievement>();
        }

        private void CreateDefaultAchievements()
        {
            var defaultAchievements = new List<Achievement>
            {
                new Achievement
                {
                    AchievementId = "first_journal_entry",
                    Title = "Research Begins",
                    Description = "Create your first journal entry",
                    Type = AchievementType.JournalEntries,
                    RequiredValue = 1
                },
                new Achievement
                {
                    AchievementId = "dedicated_researcher",
                    Title = "Dedicated Researcher",
                    Description = "Create 10 journal entries",
                    Type = AchievementType.JournalEntries,
                    RequiredValue = 10
                },
                new Achievement
                {
                    AchievementId = "breeding_documenter",
                    Title = "Breeding Documenter",
                    Description = "Document 5 breeding results",
                    Type = AchievementType.BreedingDocumentation,
                    RequiredValue = 5
                },
                new Achievement
                {
                    AchievementId = "genetic_detective",
                    Title = "Genetic Detective",
                    Description = "Make 3 genetic discoveries",
                    Type = AchievementType.GeneticDiscoveries,
                    RequiredValue = 3
                },
                new Achievement
                {
                    AchievementId = "scientific_thinker",
                    Title = "Scientific Thinker",
                    Description = "Record 5 personal observations",
                    Type = AchievementType.ScientificMethod,
                    RequiredValue = 5
                }
            };

            achievementDatabase.achievements = defaultAchievements;
        }
    }
}