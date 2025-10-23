using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.MonsterTown;
using Laboratory.Core.Education;

namespace Laboratory.Core.Discovery
{
    /// <summary>
    /// Discovery Journal System - Documents genetic findings, breeding successes, and scientific discoveries
    ///
    /// Key Features:
    /// - Automatic documentation of genetic discoveries and breeding results
    /// - Scientific journal entries with player observations and hypotheses
    /// - Achievement system rewarding scientific thinking and experimentation
    /// - Research projects and long-term investigations
    /// - Discovery sharing with friends and the community
    /// - Integration with educational content for deeper learning
    /// </summary>
    public class DiscoveryJournalSystem : MonoBehaviour
    {
        [Header("📔 Journal Configuration")]
        [SerializeField] private JournalConfig journalConfig;
        [SerializeField] private bool enableAutoDocumentation = true;
        [SerializeField] private bool enablePlayerNotes = true;
        [SerializeField] private bool enableResearchProjects = true;

        [Header("🏆 Achievement Settings")]
        [SerializeField] private AchievementDatabase achievementDatabase;
        [SerializeField] private bool enableAchievements = true;
        [SerializeField] private bool enableProgressiveAchievements = true;

        [Header("🔬 Research Features")]
        [SerializeField] private int maxActiveResearchProjects = 5;
        [SerializeField] private float researchProjectDuration = 604800f; // 7 days
        [SerializeField] private bool enableCollaborativeResearch = true;

        // Journal state
        private Dictionary<string, PlayerJournal> _playerJournals = new();
        private Dictionary<string, List<JournalEntry>> _journalEntries = new();
        private Dictionary<string, List<Achievement>> _unlockedAchievements = new();
        private Dictionary<string, List<ResearchProject>> _activeResearchProjects = new();
        private Dictionary<string, DiscoveryStatistics> _discoveryStats = new();

        // Discovery tracking
        private Dictionary<string, List<GeneticDiscovery>> _geneticDiscoveries = new();
        private Dictionary<string, List<BreedingSuccess>> _breedingSuccesses = new();
        private List<CommunityDiscovery> _communityDiscoveries = new();

        // Events
        public event Action<JournalEntry> OnJournalEntryAdded;
        public event Action<Achievement> OnAchievementUnlocked;
        public event Action<GeneticDiscovery> OnGeneticDiscoveryMade;
        public event Action<ResearchProject> OnResearchProjectCompleted;

        // Public properties for services
        public bool EnableAutoDocumentation => enableAutoDocumentation;
        public bool EnableAchievements => enableAchievements;
        public bool EnableResearchProjects => enableResearchProjects;
        public bool EnablePlayerNotes => enablePlayerNotes;
        public int MaxActiveResearchProjects => maxActiveResearchProjects;
        public float ResearchProjectDuration => researchProjectDuration;

        #region Initialization

        public void InitializeDiscoverySystem(JournalConfig config, AchievementDatabase achievements)
        {
            journalConfig = config;
            achievementDatabase = achievements;

            InitializePlayerJournals();
            InitializeAchievementSystem();
            InitializeResearchProjects();

            Debug.Log("📔 Discovery Journal System initialized");
        }

        private void InitializePlayerJournals()
        {
            // Create default journal for local player
            var localPlayerJournal = new PlayerJournal
            {
                PlayerId = "LocalPlayer",
                JournalName = "My Monster Research Journal",
                CreatedDate = DateTime.UtcNow,
                TotalEntries = 0,
                Settings = new JournalSettings
                {
                    AutoDocumentBreeding = enableAutoDocumentation,
                    AutoDocumentDiscoveries = enableAutoDocumentation,
                    AllowPlayerNotes = enablePlayerNotes,
                    ShareWithFriends = false
                }
            };

            _playerJournals["LocalPlayer"] = localPlayerJournal;
            _journalEntries["LocalPlayer"] = new List<JournalEntry>();
            _discoveryStats["LocalPlayer"] = new DiscoveryStatistics();
        }

        private void InitializeAchievementSystem()
        {
            if (!enableAchievements || achievementDatabase == null) return;

            // Create default achievements if database is empty
            if (achievementDatabase.achievements.Count == 0)
            {
                CreateDefaultAchievements();
            }

            _unlockedAchievements["LocalPlayer"] = new List<Achievement>();
        }

        private void InitializeResearchProjects()
        {
            if (!enableResearchProjects) return;

            _activeResearchProjects["LocalPlayer"] = new List<ResearchProject>();

            // Create starter research projects
            var starterProjects = new[]
            {
                new ResearchProject
                {
                    ProjectId = "basic_inheritance_study",
                    Title = "Basic Inheritance Patterns",
                    Description = "Study how traits are passed from parents to offspring",
                    ObjectiveType = ResearchObjectiveType.BreedingAnalysis,
                    RequiredBreedings = 5,
                    Status = ResearchStatus.Available
                },
                new ResearchProject
                {
                    ProjectId = "speed_genetics_analysis",
                    Title = "Speed Genetics Analysis",
                    Description = "Investigate which genetic factors contribute to racing performance",
                    ObjectiveType = ResearchObjectiveType.TraitAnalysis,
                    RequiredSamples = 10,
                    Status = ResearchStatus.Available
                }
            };

            foreach (var project in starterProjects)
            {
                _activeResearchProjects["LocalPlayer"].Add(project);
            }
        }

        #endregion

        #region Journal Entry Management

        /// <summary>
        /// Add a journal entry documenting a discovery or observation
        /// </summary>
        public JournalEntry AddJournalEntry(string playerId, JournalEntryType entryType, string title, string content, object associatedData = null)
        {
            if (!_journalEntries.ContainsKey(playerId))
            {
                _journalEntries[playerId] = new List<JournalEntry>();
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

            _journalEntries[playerId].Add(entry);

            // Update journal statistics
            var journal = _playerJournals[playerId];
            journal.TotalEntries++;
            journal.LastEntryDate = DateTime.UtcNow;

            OnJournalEntryAdded?.Invoke(entry);

            // Check for achievements
            CheckJournalAchievements(playerId);

            Debug.Log($"📝 Journal entry added: {title}");
            return entry;
        }

        /// <summary>
        /// Document a breeding result automatically
        /// </summary>
        public void DocumentBreedingResult(string playerId, Monster parent1, Monster parent2, Monster offspring)
        {
            if (!enableAutoDocumentation) return;

            var playerJournal = _playerJournals[playerId];
            if (!playerJournal.Settings.AutoDocumentBreeding) return;

            var analysis = AnalyzeBreedingResult(parent1, parent2, offspring);
            var content = GenerateBreedingEntryContent(parent1, parent2, offspring, analysis);

            var entry = AddJournalEntry(playerId, JournalEntryType.BreedingResult,
                $"Breeding: {parent1.Name} × {parent2.Name}", content,
                new BreedingData { Parent1 = parent1, Parent2 = parent2, Offspring = offspring });

            // Check for genetic discoveries
            CheckForGeneticDiscoveries(playerId, analysis, entry);

            // Update breeding success tracking
            TrackBreedingSuccess(playerId, parent1, parent2, offspring, analysis);
        }

        /// <summary>
        /// Document a genetic discovery
        /// </summary>
        public void DocumentGeneticDiscovery(string playerId, GeneticDiscovery discovery)
        {
            if (!_geneticDiscoveries.ContainsKey(playerId))
            {
                _geneticDiscoveries[playerId] = new List<GeneticDiscovery>();
            }

            _geneticDiscoveries[playerId].Add(discovery);

            var content = GenerateDiscoveryEntryContent(discovery);
            AddJournalEntry(playerId, JournalEntryType.GeneticDiscovery,
                $"Discovery: {discovery.DiscoveryName}", content, discovery);

            OnGeneticDiscoveryMade?.Invoke(discovery);

            // Update discovery statistics
            var stats = _discoveryStats[playerId];
            stats.TotalDiscoveries++;
            stats.LastDiscoveryDate = DateTime.UtcNow;

            CheckDiscoveryAchievements(playerId);

            Debug.Log($"🧬 Genetic discovery documented: {discovery.DiscoveryName}");
        }

        /// <summary>
        /// Add a player observation or note
        /// </summary>
        public JournalEntry AddPlayerObservation(string playerId, string observation, string hypothesis = "")
        {
            if (!enablePlayerNotes) return null;

            var content = $"Observation: {observation}";
            if (!string.IsNullOrEmpty(hypothesis))
            {
                content += $"\n\nHypothesis: {hypothesis}";
            }

            return AddJournalEntry(playerId, JournalEntryType.PlayerObservation,
                "Personal Observation", content);
        }

        #endregion

        #region Achievement System

        /// <summary>
        /// Check and unlock achievements
        /// </summary>
        private void CheckJournalAchievements(string playerId)
        {
            if (!enableAchievements) return;

            var journal = _playerJournals[playerId];
            var entries = _journalEntries[playerId];
            var unlockedAchievements = _unlockedAchievements[playerId];

            foreach (var achievement in achievementDatabase.achievements)
            {
                if (unlockedAchievements.Any(a => a.AchievementId == achievement.AchievementId))
                    continue; // Already unlocked

                if (CheckAchievementCondition(achievement, playerId, journal, entries))
                {
                    UnlockAchievement(playerId, achievement);
                }
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
                    var discoveries = _geneticDiscoveries.TryGetValue(playerId, out var disc) ? disc.Count : 0;
                    return discoveries >= achievement.RequiredValue;

                case AchievementType.ResearchProjects:
                    var completedProjects = _activeResearchProjects.TryGetValue(playerId, out var projects)
                        ? projects.Count(p => p.Status == ResearchStatus.Completed) : 0;
                    return completedProjects >= achievement.RequiredValue;

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

            _unlockedAchievements[playerId].Add(unlockedAchievement);
            OnAchievementUnlocked?.Invoke(unlockedAchievement);

            Debug.Log($"🏆 Achievement unlocked: {achievement.Title}");
        }

        private void CheckDiscoveryAchievements(string playerId)
        {
            var stats = _discoveryStats[playerId];

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

        #endregion

        #region Research Projects

        /// <summary>
        /// Start a research project
        /// </summary>
        public bool StartResearchProject(string playerId, string projectId)
        {
            if (!enableResearchProjects) return false;

            var projects = _activeResearchProjects[playerId];
            if (projects.Count >= maxActiveResearchProjects) return false;

            var project = projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null || project.Status != ResearchStatus.Available) return false;

            project.Status = ResearchStatus.InProgress;
            project.StartDate = DateTime.UtcNow;
            project.EndDate = DateTime.UtcNow.AddSeconds(researchProjectDuration);

            Debug.Log($"🔬 Research project started: {project.Title}");
            return true;
        }

        /// <summary>
        /// Update research project progress
        /// </summary>
        public void UpdateResearchProgress(string playerId, ResearchObjectiveType objectiveType, object data = null)
        {
            var projects = _activeResearchProjects[playerId];
            var activeProjects = projects.Where(p => p.Status == ResearchStatus.InProgress && p.ObjectiveType == objectiveType);

            foreach (var project in activeProjects)
            {
                switch (objectiveType)
                {
                    case ResearchObjectiveType.BreedingAnalysis:
                        project.CurrentProgress++;
                        break;

                    case ResearchObjectiveType.TraitAnalysis:
                        project.CurrentProgress++;
                        break;

                    case ResearchObjectiveType.PerformanceStudy:
                        project.CurrentProgress++;
                        break;
                }

                CheckProjectCompletion(playerId, project);
            }
        }

        private void CheckProjectCompletion(string playerId, ResearchProject project)
        {
            bool isComplete = false;

            switch (project.ObjectiveType)
            {
                case ResearchObjectiveType.BreedingAnalysis:
                    isComplete = project.CurrentProgress >= project.RequiredBreedings;
                    break;

                case ResearchObjectiveType.TraitAnalysis:
                    isComplete = project.CurrentProgress >= project.RequiredSamples;
                    break;

                case ResearchObjectiveType.PerformanceStudy:
                    isComplete = project.CurrentProgress >= project.RequiredTests;
                    break;
            }

            if (isComplete)
            {
                CompleteResearchProject(playerId, project);
            }
        }

        private void CompleteResearchProject(string playerId, ResearchProject project)
        {
            project.Status = ResearchStatus.Completed;
            project.CompletionDate = DateTime.UtcNow;

            // Generate research findings
            var findings = GenerateResearchFindings(project);
            project.Findings = findings;

            // Add journal entry
            var content = GenerateResearchEntryContent(project);
            AddJournalEntry(playerId, JournalEntryType.ResearchCompletion,
                $"Research Complete: {project.Title}", content, project);

            OnResearchProjectCompleted?.Invoke(project);

            Debug.Log($"🔬 Research project completed: {project.Title}");
        }

        /// <summary>
        /// Manually trigger research project completion (for services)
        /// </summary>
        public void TriggerResearchProjectCompleted(string playerId, string projectId)
        {
            if (!_activeResearchProjects.TryGetValue(playerId, out var projects)) return;

            var project = projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project != null && project.Status == ResearchStatus.InProgress)
            {
                CompleteResearchProject(playerId, project);
            }
        }

        #endregion

        #region Data Analysis and Generation

        private BreedingAnalysis AnalyzeBreedingResult(Monster parent1, Monster parent2, Monster offspring)
        {
            var analysis = new BreedingAnalysis
            {
                InheritancePatterns = new Dictionary<string, InheritanceType>(),
                TraitComparisons = new Dictionary<string, TraitComparison>(),
                NotableObservations = new List<string>(),
                GeneticNovelty = CalculateGeneticNovelty(parent1, parent2, offspring)
            };

            // Analyze stat inheritance
            var statNames = new[] { "Strength", "Agility", "Vitality", "Intelligence", "Speed", "Social" };
            var parentStats = new[] { parent1.Stats.strength, parent1.Stats.agility, parent1.Stats.vitality,
                                    parent1.Stats.intelligence, parent1.Stats.speed, parent1.Stats.social };
            var parent2Stats = new[] { parent2.Stats.strength, parent2.Stats.agility, parent2.Stats.vitality,
                                     parent2.Stats.intelligence, parent2.Stats.speed, parent2.Stats.social };
            var offspringStats = new[] { offspring.Stats.strength, offspring.Stats.agility, offspring.Stats.vitality,
                                       offspring.Stats.intelligence, offspring.Stats.speed, offspring.Stats.social };

            for (int i = 0; i < statNames.Length; i++)
            {
                var comparison = new TraitComparison
                {
                    Parent1Value = parentStats[i],
                    Parent2Value = parent2Stats[i],
                    OffspringValue = offspringStats[i],
                    InheritanceType = DetermineInheritanceType(parentStats[i], parent2Stats[i], offspringStats[i])
                };

                analysis.TraitComparisons[statNames[i]] = comparison;
                analysis.InheritancePatterns[statNames[i]] = comparison.InheritanceType;
            }

            // Generate observations
            analysis.NotableObservations = GenerateBreedingObservations(analysis);

            return analysis;
        }

        private InheritanceType DetermineInheritanceType(float parent1Stat, float parent2Stat, float offspringStat)
        {
            var midpoint = (parent1Stat + parent2Stat) / 2f;
            var tolerance = Math.Abs(parent1Stat - parent2Stat) * 0.1f;

            if (Math.Abs(offspringStat - midpoint) <= tolerance)
                return InheritanceType.Blended;
            else if (Math.Abs(offspringStat - parent1Stat) < Math.Abs(offspringStat - parent2Stat))
                return InheritanceType.DominantFromParent1;
            else
                return InheritanceType.DominantFromParent2;
        }

        private float CalculateGeneticNovelty(Monster parent1, Monster parent2, Monster offspring)
        {
            // Calculate how different the offspring is from both parents
            var noveltyScore = 0f;

            // Compare stats
            var statDifferences = new[]
            {
                Math.Abs(offspring.Stats.strength - (parent1.Stats.strength + parent2.Stats.strength) / 2f),
                Math.Abs(offspring.Stats.agility - (parent1.Stats.agility + parent2.Stats.agility) / 2f),
                Math.Abs(offspring.Stats.vitality - (parent1.Stats.vitality + parent2.Stats.vitality) / 2f),
                Math.Abs(offspring.Stats.intelligence - (parent1.Stats.intelligence + parent2.Stats.intelligence) / 2f),
                Math.Abs(offspring.Stats.speed - (parent1.Stats.speed + parent2.Stats.speed) / 2f),
                Math.Abs(offspring.Stats.social - (parent1.Stats.social + parent2.Stats.social) / 2f)
            };

            noveltyScore = statDifferences.Average() / 100f; // Normalize to 0-1 range
            return Mathf.Clamp01(noveltyScore);
        }

        private List<string> GenerateBreedingObservations(BreedingAnalysis analysis)
        {
            var observations = new List<string>();

            // Check for interesting inheritance patterns
            var dominantTraits = analysis.InheritancePatterns.Where(kvp =>
                kvp.Value == InheritanceType.DominantFromParent1 ||
                kvp.Value == InheritanceType.DominantFromParent2).ToList();

            if (dominantTraits.Count >= 3)
            {
                observations.Add("Strong dominant inheritance patterns observed in multiple traits");
            }

            var blendedTraits = analysis.InheritancePatterns.Where(kvp =>
                kvp.Value == InheritanceType.Blended).Count();

            if (blendedTraits >= 4)
            {
                observations.Add("Most traits show blended inheritance from both parents");
            }

            // Check for high genetic novelty
            if (analysis.GeneticNovelty > 0.3f)
            {
                observations.Add("Offspring shows significant genetic variation from expected patterns");
            }

            return observations;
        }

        private void CheckForGeneticDiscoveries(string playerId, BreedingAnalysis analysis, JournalEntry entry)
        {
            // Check for discoveries based on breeding analysis
            if (analysis.GeneticNovelty > 0.4f)
            {
                var discovery = new GeneticDiscovery
                {
                    DiscoveryId = Guid.NewGuid().ToString(),
                    DiscoveryName = "Unexpected Genetic Variation",
                    Description = "Observed significant genetic variation beyond normal inheritance patterns",
                    DiscoveryType = DiscoveryType.InheritancePattern,
                    Significance = DiscoverySignificance.Notable,
                    RelatedJournalEntry = entry.EntryId,
                    DiscoveryDate = DateTime.UtcNow
                };

                DocumentGeneticDiscovery(playerId, discovery);
            }

            // Check for rare trait combinations
            var exceptionalTraits = analysis.TraitComparisons.Where(kvp =>
                kvp.Value.OffspringValue > Math.Max(kvp.Value.Parent1Value, kvp.Value.Parent2Value) + 10f);

            if (exceptionalTraits.Count() >= 2)
            {
                var discovery = new GeneticDiscovery
                {
                    DiscoveryId = Guid.NewGuid().ToString(),
                    DiscoveryName = "Hybrid Vigor Expression",
                    Description = "Offspring exceeded both parents in multiple traits, showing hybrid vigor",
                    DiscoveryType = DiscoveryType.TraitExpression,
                    Significance = DiscoverySignificance.Significant,
                    RelatedJournalEntry = entry.EntryId,
                    DiscoveryDate = DateTime.UtcNow
                };

                DocumentGeneticDiscovery(playerId, discovery);
            }
        }

        private void TrackBreedingSuccess(string playerId, Monster parent1, Monster parent2, Monster offspring, BreedingAnalysis analysis)
        {
            var success = new BreedingSuccess
            {
                SuccessId = Guid.NewGuid().ToString(),
                Parent1Species = parent1.Name,
                Parent2Species = parent2.Name,
                OffspringQualities = CalculateOffspringQualities(offspring),
                GeneticNovelty = analysis.GeneticNovelty,
                SuccessDate = DateTime.UtcNow
            };

            if (!_breedingSuccesses.ContainsKey(playerId))
            {
                _breedingSuccesses[playerId] = new List<BreedingSuccess>();
            }

            _breedingSuccesses[playerId].Add(success);
        }

        private float CalculateOffspringQualities(Monster offspring)
        {
            // Calculate overall quality score based on stats
            var avgStats = (offspring.Stats.strength + offspring.Stats.agility + offspring.Stats.vitality +
                          offspring.Stats.intelligence + offspring.Stats.speed + offspring.Stats.social) / 6f;
            return avgStats / 100f; // Normalize to 0-1
        }

        private string GenerateBreedingEntryContent(Monster parent1, Monster parent2, Monster offspring, BreedingAnalysis analysis)
        {
            var content = $"Breeding Experiment: {parent1.Name} × {parent2.Name}\n\n";
            content += $"Result: {offspring.Name}\n\n";
            content += "Trait Analysis:\n";

            foreach (var trait in analysis.TraitComparisons)
            {
                content += $"• {trait.Key}: {trait.Value.Parent1Value:F1} + {trait.Value.Parent2Value:F1} → {trait.Value.OffspringValue:F1} ({trait.Value.InheritanceType})\n";
            }

            if (analysis.NotableObservations.Any())
            {
                content += "\nNotable Observations:\n";
                foreach (var observation in analysis.NotableObservations)
                {
                    content += $"• {observation}\n";
                }
            }

            content += $"\nGenetic Novelty Score: {analysis.GeneticNovelty:P1}";

            return content;
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

        private string GenerateResearchEntryContent(ResearchProject project)
        {
            var content = $"Research Project: {project.Title}\n\n";
            content += $"Objective: {project.Description}\n\n";
            content += $"Duration: {(project.CompletionDate - project.StartDate).TotalDays:F1} days\n\n";

            if (project.Findings?.Any() == true)
            {
                content += "Key Findings:\n";
                foreach (var finding in project.Findings)
                {
                    content += $"• {finding}\n";
                }
            }

            return content;
        }

        private List<string> GenerateResearchFindings(ResearchProject project)
        {
            var findings = new List<string>();

            switch (project.ObjectiveType)
            {
                case ResearchObjectiveType.BreedingAnalysis:
                    findings.Add("Confirmed Mendelian inheritance patterns in monster breeding");
                    findings.Add("Identified key factors influencing trait expression");
                    break;

                case ResearchObjectiveType.TraitAnalysis:
                    findings.Add("Mapped correlation between genetic markers and performance traits");
                    findings.Add("Discovered optimal trait combinations for specific activities");
                    break;

                case ResearchObjectiveType.PerformanceStudy:
                    findings.Add("Quantified relationship between genetics and activity performance");
                    findings.Add("Identified training methods that enhance genetic potential");
                    break;
            }

            return findings;
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

        #endregion

        #region Query Methods

        /// <summary>
        /// Get journal entries for a player
        /// </summary>
        public List<JournalEntry> GetJournalEntries(string playerId, JournalEntryType? entryType = null, int limit = 50)
        {
            if (!_journalEntries.TryGetValue(playerId, out var entries))
                return new List<JournalEntry>();

            var filteredEntries = entryType.HasValue
                ? entries.Where(e => e.EntryType == entryType.Value)
                : entries;

            return filteredEntries.OrderByDescending(e => e.CreatedDate).Take(limit).ToList();
        }

        /// <summary>
        /// Get unlocked achievements for a player
        /// </summary>
        public List<Achievement> GetUnlockedAchievements(string playerId)
        {
            return _unlockedAchievements.TryGetValue(playerId, out var achievements)
                ? new List<Achievement>(achievements)
                : new List<Achievement>();
        }

        /// <summary>
        /// Trigger achievement unlocked event
        /// </summary>
        public void TriggerAchievementUnlocked(Achievement achievement)
        {
            OnAchievementUnlocked?.Invoke(achievement);
        }

        /// <summary>
        /// Get active research projects for a player
        /// </summary>
        public List<ResearchProject> GetActiveResearchProjects(string playerId)
        {
            return _activeResearchProjects.TryGetValue(playerId, out var projects)
                ? projects.Where(p => p.Status == ResearchStatus.InProgress).ToList()
                : new List<ResearchProject>();
        }

        /// <summary>
        /// Get discovery statistics for a player
        /// </summary>
        public DiscoveryStatistics GetDiscoveryStatistics(string playerId)
        {
            return _discoveryStats.TryGetValue(playerId, out var stats) ? stats : new DiscoveryStatistics();
        }

        /// <summary>
        /// Search journal entries by content or tags
        /// </summary>
        public List<JournalEntry> SearchJournal(string playerId, string searchTerm)
        {
            if (!_journalEntries.TryGetValue(playerId, out var entries))
                return new List<JournalEntry>();

            var searchTermLower = searchTerm.ToLower();

            return entries.Where(e =>
                e.Title.ToLower().Contains(searchTermLower) ||
                e.Content.ToLower().Contains(searchTermLower) ||
                e.Tags.Any(tag => tag.ToLower().Contains(searchTermLower))
            ).OrderByDescending(e => e.CreatedDate).ToList();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Journal configuration
    /// </summary>
    [Serializable]
    public class JournalConfig
    {
        [Header("Documentation Settings")]
        public bool autoDocumentBreeding = true;
        public bool autoDocumentDiscoveries = true;
        public bool enablePlayerNotes = true;

        [Header("Sharing Settings")]
        public bool enableCommunitySharing = true;
        public bool enableFriendSharing = true;
    }

    /// <summary>
    /// Player journal data
    /// </summary>
    [Serializable]
    public class PlayerJournal
    {
        public string PlayerId;
        public string JournalName;
        public DateTime CreatedDate;
        public DateTime LastEntryDate;
        public int TotalEntries;
        public JournalSettings Settings;
    }

    /// <summary>
    /// Journal settings
    /// </summary>
    [Serializable]
    public class JournalSettings
    {
        public bool AutoDocumentBreeding = true;
        public bool AutoDocumentDiscoveries = true;
        public bool AllowPlayerNotes = true;
        public bool ShareWithFriends = false;
        public bool ShareWithCommunity = false;
    }

    /// <summary>
    /// Journal entry
    /// </summary>
    [Serializable]
    public class JournalEntry
    {
        public string EntryId;
        public string PlayerId;
        public JournalEntryType EntryType;
        public string Title;
        [TextArea(5, 10)]
        public string Content;
        public DateTime CreatedDate;
        public List<string> Tags = new();
        public object AssociatedData;
    }

    /// <summary>
    /// Achievement data
    /// </summary>
    [Serializable]
    public class Achievement
    {
        public string AchievementId;
        public string Title;
        public string Description;
        public AchievementType Type;
        public int RequiredValue;
        public DateTime UnlockedDate;
        public TownResources Rewards;
    }

    /// <summary>
    /// Achievement database
    /// </summary>
    [CreateAssetMenu(fileName = "Achievement Database", menuName = "Chimera/Achievement Database")]
    public class AchievementDatabase : ScriptableObject
    {
        public List<Achievement> achievements = new();
    }

    /// <summary>
    /// Research project
    /// </summary>
    [Serializable]
    public class ResearchProject
    {
        public string ProjectId;
        public string Title;
        public string Description;
        public ResearchObjectiveType ObjectiveType;
        public ResearchStatus Status;
        public DateTime StartDate;
        public DateTime EndDate;
        public DateTime CompletionDate;
        public int CurrentProgress;
        public int RequiredBreedings;
        public int RequiredSamples;
        public int RequiredTests;
        public List<string> Findings = new();
    }

    /// <summary>
    /// Genetic discovery
    /// </summary>
    [Serializable]
    public class GeneticDiscovery
    {
        public string DiscoveryId;
        public string DiscoveryName;
        public string Description;
        public DiscoveryType DiscoveryType;
        public DiscoverySignificance Significance;
        public DateTime DiscoveryDate;
        public string RelatedJournalEntry;
    }

    /// <summary>
    /// Breeding analysis results
    /// </summary>
    [Serializable]
    public class BreedingAnalysis
    {
        public Dictionary<string, InheritanceType> InheritancePatterns = new();
        public Dictionary<string, TraitComparison> TraitComparisons = new();
        public List<string> NotableObservations = new();
        public float GeneticNovelty;
    }

    /// <summary>
    /// Trait comparison data
    /// </summary>
    [Serializable]
    public struct TraitComparison
    {
        public float Parent1Value;
        public float Parent2Value;
        public float OffspringValue;
        public InheritanceType InheritanceType;
    }

    /// <summary>
    /// Breeding success tracking
    /// </summary>
    [Serializable]
    public class BreedingSuccess
    {
        public string SuccessId;
        public string Parent1Species;
        public string Parent2Species;
        public float OffspringQualities;
        public float GeneticNovelty;
        public DateTime SuccessDate;
    }

    /// <summary>
    /// Discovery statistics
    /// </summary>
    [Serializable]
    public class DiscoveryStatistics
    {
        public int TotalDiscoveries;
        public int TotalJournalEntries;
        public int CompletedResearchProjects;
        public DateTime LastDiscoveryDate;
        public DateTime LastJournalEntry;
    }

    /// <summary>
    /// Community discovery for sharing
    /// </summary>
    [Serializable]
    public class CommunityDiscovery
    {
        public string DiscoveryId;
        public string DiscovererId;
        public GeneticDiscovery Discovery;
        public DateTime SharedDate;
        public int CommunityRating;
        public List<string> Comments = new();
    }

    /// <summary>
    /// Breeding data for journal entries
    /// </summary>
    [Serializable]
    public class BreedingData
    {
        public Monster Parent1;
        public Monster Parent2;
        public Monster Offspring;
    }

    /// <summary>
    /// Enums for discovery system
    /// </summary>
    public enum JournalEntryType { BreedingResult, GeneticDiscovery, PlayerObservation, ResearchCompletion, Achievement }
    public enum AchievementType { JournalEntries, BreedingDocumentation, GeneticDiscoveries, ResearchProjects, ScientificMethod }
    public enum DiscoveryType { InheritancePattern, TraitExpression, MutationEvent, PerformanceCorrelation }
    public enum DiscoverySignificance { Minor, Notable, Significant, Major, Groundbreaking }
    public enum ResearchObjectiveType { BreedingAnalysis, TraitAnalysis, PerformanceStudy, PopulationStudy }
    public enum ResearchStatus { Available, InProgress, Completed, Cancelled }
    public enum InheritanceType { Blended, DominantFromParent1, DominantFromParent2, Novel }

    #endregion
}