using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.AIDirector
{
    /// <summary>
    /// Concrete implementation of narrative generation service
    /// Creates dynamic narratives, celebrations, and story opportunities
    /// </summary>
    public class NarrativeGenerationService : INarrativeGenerationService
    {
        #region Fields

        private readonly AIDirectorSubsystemConfig _config;
        private Dictionary<string, List<NarrativeEvent>> _playerNarratives;
        private Dictionary<string, Queue<NarrativeOpportunity>> _pendingOpportunities;
        private List<NarrativeTemplate> _narrativeTemplates;
        private Dictionary<string, int> _narrativeUsageCount;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public NarrativeGenerationService(AIDirectorSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region INarrativeGenerationService Implementation

        public async Task<bool> InitializeAsync()
        {
            await Task.CompletedTask; // Synchronous initialization, but async for interface compatibility

            try
            {
                _playerNarratives = new Dictionary<string, List<NarrativeEvent>>();
                _pendingOpportunities = new Dictionary<string, Queue<NarrativeOpportunity>>();
                _narrativeTemplates = new List<NarrativeTemplate>();
                _narrativeUsageCount = new Dictionary<string, int>();

                // Initialize narrative templates
                InitializeNarrativeTemplates();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[NarrativeGenerationService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NarrativeGenerationService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public NarrativeEvent GenerateNarrative(string playerId, string narrativeType)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return null;

            var template = FindNarrativeTemplate(narrativeType);
            if (template == null)
                return null;

            var narrativeEvent = CreateNarrativeFromTemplate(playerId, template);

            // Track narrative
            if (!_playerNarratives.ContainsKey(playerId))
                _playerNarratives[playerId] = new List<NarrativeEvent>();

            _playerNarratives[playerId].Add(narrativeEvent);

            // Update usage count
            _narrativeUsageCount[narrativeType] = _narrativeUsageCount.GetValueOrDefault(narrativeType, 0) + 1;

            if (_config.enableDebugLogging)
                Debug.Log($"[NarrativeGenerationService] Generated {narrativeType} narrative for {playerId}: {narrativeEvent.title}");

            return narrativeEvent;
        }

        public void TriggerAchievementNarrative(string playerId, string achievementType)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return;

            var narrativeEvent = new NarrativeEvent
            {
                narrativeId = Guid.NewGuid().ToString(),
                playerId = playerId,
                narrativeType = NarrativeType.Achievement,
                title = GenerateAchievementTitle(achievementType),
                description = GenerateAchievementDescription(achievementType),
                triggerTime = DateTime.Now,
                duration = TimeSpan.FromMinutes(2),
                narrativeData = new Dictionary<string, object>
                {
                    ["achievementType"] = achievementType,
                    ["isSpecial"] = IsSpecialAchievement(achievementType)
                }
            };

            // Add choices for achievements
            AddAchievementChoices(narrativeEvent, achievementType);

            // Track narrative
            if (!_playerNarratives.ContainsKey(playerId))
                _playerNarratives[playerId] = new List<NarrativeEvent>();

            _playerNarratives[playerId].Add(narrativeEvent);

            if (_config.enableDebugLogging)
                Debug.Log($"[NarrativeGenerationService] Triggered achievement narrative for {playerId}: {achievementType}");
        }

        public void GenerateMilestoneCelebration(string playerId, string milestoneType)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return;

            var narrativeEvent = new NarrativeEvent
            {
                narrativeId = Guid.NewGuid().ToString(),
                playerId = playerId,
                narrativeType = NarrativeType.Celebration,
                title = GenerateMilestoneTitle(milestoneType),
                description = GenerateMilestoneDescription(milestoneType),
                triggerTime = DateTime.Now,
                duration = TimeSpan.FromMinutes(3),
                narrativeData = new Dictionary<string, object>
                {
                    ["milestoneType"] = milestoneType,
                    ["celebrationLevel"] = CalculateCelebrationLevel(milestoneType)
                }
            };

            // Add milestone-specific choices
            AddMilestoneChoices(narrativeEvent, milestoneType);

            // Track narrative
            if (!_playerNarratives.ContainsKey(playerId))
                _playerNarratives[playerId] = new List<NarrativeEvent>();

            _playerNarratives[playerId].Add(narrativeEvent);

            if (_config.enableDebugLogging)
                Debug.Log($"[NarrativeGenerationService] Generated milestone celebration for {playerId}: {milestoneType}");
        }

        public void CelebrateAchievement(string playerId, string achievementType, string celebrationType)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return;

            var narrativeEvent = new NarrativeEvent
            {
                narrativeId = Guid.NewGuid().ToString(),
                playerId = playerId,
                narrativeType = NarrativeType.Celebration,
                title = GenerateCelebrationTitle(achievementType, celebrationType),
                description = GenerateCelebrationDescription(achievementType, celebrationType),
                triggerTime = DateTime.Now,
                duration = TimeSpan.FromMinutes(1),
                narrativeData = new Dictionary<string, object>
                {
                    ["achievementType"] = achievementType,
                    ["celebrationType"] = celebrationType,
                    ["isPublic"] = celebrationType == "world_first"
                }
            };

            // Track narrative
            if (!_playerNarratives.ContainsKey(playerId))
                _playerNarratives[playerId] = new List<NarrativeEvent>();

            _playerNarratives[playerId].Add(narrativeEvent);

            if (_config.enableDebugLogging)
                Debug.Log($"[NarrativeGenerationService] Celebrated {achievementType} with {celebrationType} for {playerId}");
        }

        public List<NarrativeOpportunity> AnalyzeDiscoveryOpportunities(string playerId, string discoveryType)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return new List<NarrativeOpportunity>();

            var opportunities = new List<NarrativeOpportunity>();

            // Generate opportunities based on discovery type
            switch (discoveryType)
            {
                case "rare_mutation":
                    opportunities.Add(CreateResearchOpportunity(playerId, discoveryType));
                    opportunities.Add(CreateCollaborationOpportunity(playerId, discoveryType));
                    break;

                case "new_species":
                    opportunities.Add(CreateExplorationOpportunity(playerId, discoveryType));
                    opportunities.Add(CreateDocumentationOpportunity(playerId, discoveryType));
                    break;

                case "genetic_trait":
                    opportunities.Add(CreateBreedingOpportunity(playerId, discoveryType));
                    break;

                case "behavioral_pattern":
                    opportunities.Add(CreateObservationOpportunity(playerId, discoveryType));
                    break;

                default:
                    opportunities.Add(CreateGenericOpportunity(playerId, discoveryType));
                    break;
            }

            // Queue opportunities for the player
            if (!_pendingOpportunities.ContainsKey(playerId))
                _pendingOpportunities[playerId] = new Queue<NarrativeOpportunity>();

            foreach (var opportunity in opportunities)
            {
                _pendingOpportunities[playerId].Enqueue(opportunity);
            }

            if (_config.enableDebugLogging)
                Debug.Log($"[NarrativeGenerationService] Generated {opportunities.Count} narrative opportunities for {playerId} from {discoveryType}");

            return opportunities;
        }

        #endregion

        #region Private Methods

        private void InitializeNarrativeTemplates()
        {
            _narrativeTemplates.AddRange(new[]
            {
                // Discovery templates
                new NarrativeTemplate
                {
                    templateId = "discovery_basic",
                    narrativeType = NarrativeType.Discovery,
                    title = "An Intriguing Discovery",
                    descriptionTemplate = "You've discovered something fascinating about {subject}. This could lead to new understanding.",
                    estimatedDuration = TimeSpan.FromMinutes(2),
                    weight = 1f
                },

                // Achievement templates
                new NarrativeTemplate
                {
                    templateId = "achievement_milestone",
                    narrativeType = NarrativeType.Achievement,
                    title = "Milestone Reached",
                    descriptionTemplate = "Congratulations on reaching {milestone}! Your dedication has paid off.",
                    estimatedDuration = TimeSpan.FromMinutes(1),
                    weight = 1f
                },

                // Collaboration templates
                new NarrativeTemplate
                {
                    templateId = "collaboration_opportunity",
                    narrativeType = NarrativeType.Collaboration,
                    title = "Collaboration Opportunity",
                    descriptionTemplate = "There's an opportunity to collaborate on {topic}. Working together could yield better results.",
                    estimatedDuration = TimeSpan.FromMinutes(5),
                    weight = 0.8f
                },

                // Mystery templates
                new NarrativeTemplate
                {
                    templateId = "mystery_genetic",
                    narrativeType = NarrativeType.Mystery,
                    title = "Genetic Mystery",
                    descriptionTemplate = "Something unusual is happening with {creature}. Investigation may reveal new insights.",
                    estimatedDuration = TimeSpan.FromMinutes(3),
                    weight = 0.7f
                },

                // Tutorial templates
                new NarrativeTemplate
                {
                    templateId = "tutorial_guidance",
                    narrativeType = NarrativeType.Tutorial,
                    title = "Learning Opportunity",
                    descriptionTemplate = "Here's a chance to learn about {concept}. Understanding this will help your research.",
                    estimatedDuration = TimeSpan.FromMinutes(2),
                    weight = 1f
                }
            });
        }

        private NarrativeTemplate FindNarrativeTemplate(string narrativeType)
        {
            // Find suitable template based on type and usage frequency
            var candidateTemplates = _narrativeTemplates.FindAll(t =>
                t.narrativeType.ToString().ToLower() == narrativeType.ToLower());

            if (candidateTemplates.Count == 0)
                return _narrativeTemplates.Find(t => t.templateId == "discovery_basic");

            // Prefer less-used templates
            candidateTemplates.Sort((a, b) =>
            {
                var usageA = _narrativeUsageCount.GetValueOrDefault(a.templateId, 0);
                var usageB = _narrativeUsageCount.GetValueOrDefault(b.templateId, 0);
                return usageA.CompareTo(usageB);
            });

            return candidateTemplates[0];
        }

        private NarrativeEvent CreateNarrativeFromTemplate(string playerId, NarrativeTemplate template)
        {
            var narrativeEvent = new NarrativeEvent
            {
                narrativeId = Guid.NewGuid().ToString(),
                playerId = playerId,
                narrativeType = template.narrativeType,
                title = template.title,
                description = ProcessDescriptionTemplate(template.descriptionTemplate),
                triggerTime = DateTime.Now,
                duration = template.estimatedDuration,
                narrativeData = new Dictionary<string, object>
                {
                    ["templateId"] = template.templateId,
                    ["weight"] = template.weight
                }
            };

            // Add appropriate choices based on narrative type
            AddNarrativeChoices(narrativeEvent, template);

            return narrativeEvent;
        }

        private string ProcessDescriptionTemplate(string template)
        {
            // Simple template processing - replace placeholders with contextual content
            var processed = template;

            processed = processed.Replace("{subject}", GetRandomSubject());
            processed = processed.Replace("{milestone}", GetRandomMilestone());
            processed = processed.Replace("{topic}", GetRandomTopic());
            processed = processed.Replace("{creature}", GetRandomCreature());
            processed = processed.Replace("{concept}", GetRandomConcept());

            return processed;
        }

        private string GetRandomSubject() => new[] { "genetic patterns", "behavioral traits", "environmental factors", "mutation rates" }[UnityEngine.Random.Range(0, 4)];
        private string GetRandomMilestone() => new[] { "your research goal", "a breeding milestone", "discovery threshold", "collaboration target" }[UnityEngine.Random.Range(0, 4)];
        private string GetRandomTopic() => new[] { "genetic research", "behavioral studies", "ecosystem management", "trait analysis" }[UnityEngine.Random.Range(0, 4)];
        private string GetRandomCreature() => new[] { "this specimen", "the test subject", "your creature", "the breeding pair" }[UnityEngine.Random.Range(0, 4)];
        private string GetRandomConcept() => new[] { "genetic inheritance", "mutation mechanics", "trait expression", "breeding strategies" }[UnityEngine.Random.Range(0, 4)];

        private void AddNarrativeChoices(NarrativeEvent narrativeEvent, NarrativeTemplate template)
        {
            switch (template.narrativeType)
            {
                case NarrativeType.Discovery:
                    narrativeEvent.choices.Add(new NarrativeChoice
                    {
                        choiceId = "investigate_further",
                        choiceText = "Investigate Further",
                        description = "Dive deeper into this discovery",
                        consequences = new Dictionary<string, object> { ["engagement"] = 0.1f }
                    });
                    narrativeEvent.choices.Add(new NarrativeChoice
                    {
                        choiceId = "share_discovery",
                        choiceText = "Share with Others",
                        description = "Collaborate on this discovery",
                        consequences = new Dictionary<string, object> { ["collaboration"] = 0.15f }
                    });
                    break;

                case NarrativeType.Collaboration:
                    narrativeEvent.choices.Add(new NarrativeChoice
                    {
                        choiceId = "accept_collaboration",
                        choiceText = "Join the Collaboration",
                        description = "Work together on this project",
                        consequences = new Dictionary<string, object> { ["social"] = 0.2f }
                    });
                    narrativeEvent.choices.Add(new NarrativeChoice
                    {
                        choiceId = "decline_collaboration",
                        choiceText = "Work Solo",
                        description = "Continue working independently",
                        consequences = new Dictionary<string, object> { ["independence"] = 0.1f }
                    });
                    break;

                case NarrativeType.Mystery:
                    narrativeEvent.choices.Add(new NarrativeChoice
                    {
                        choiceId = "solve_mystery",
                        choiceText = "Investigate the Mystery",
                        description = "Try to solve this puzzle",
                        consequences = new Dictionary<string, object> { ["curiosity"] = 0.15f }
                    });
                    break;
            }
        }

        private string GenerateAchievementTitle(string achievementType)
        {
            return achievementType switch
            {
                "first_breeding" => "First Successful Breeding!",
                "rare_mutation" => "Rare Mutation Discovered!",
                "collaboration_success" => "Successful Collaboration!",
                "research_milestone" => "Research Milestone Achieved!",
                _ => "Achievement Unlocked!"
            };
        }

        private string GenerateAchievementDescription(string achievementType)
        {
            return achievementType switch
            {
                "first_breeding" => "You've successfully bred your first creature pair! This is the beginning of your genetic research journey.",
                "rare_mutation" => "You've discovered a rare genetic mutation! This could lead to breakthrough discoveries.",
                "collaboration_success" => "Your collaborative effort has paid off! Working together yields amazing results.",
                "research_milestone" => "You've reached an important milestone in your research. Keep up the excellent work!",
                _ => "Congratulations on your achievement! Your dedication to research is commendable."
            };
        }

        private bool IsSpecialAchievement(string achievementType)
        {
            return achievementType == "rare_mutation" || achievementType == "world_first_discovery";
        }

        private void AddAchievementChoices(NarrativeEvent narrativeEvent, string achievementType)
        {
            narrativeEvent.choices.Add(new NarrativeChoice
            {
                choiceId = "celebrate",
                choiceText = "Celebrate",
                description = "Take a moment to celebrate this achievement",
                consequences = new Dictionary<string, object> { ["confidence"] = 0.1f }
            });

            if (IsSpecialAchievement(achievementType))
            {
                narrativeEvent.choices.Add(new NarrativeChoice
                {
                    choiceId = "share_achievement",
                    choiceText = "Share with Community",
                    description = "Share this special achievement with other researchers",
                    consequences = new Dictionary<string, object> { ["social"] = 0.15f, ["recognition"] = 0.1f }
                });
            }

            narrativeEvent.choices.Add(new NarrativeChoice
            {
                choiceId = "continue_research",
                choiceText = "Continue Research",
                description = "Build on this achievement and continue your work",
                consequences = new Dictionary<string, object> { ["focus"] = 0.1f }
            });
        }

        private string GenerateMilestoneTitle(string milestoneType)
        {
            return milestoneType switch
            {
                "major_discovery" => "Major Discovery Milestone!",
                "collaboration_success" => "Collaboration Milestone!",
                "skill_mastery" => "Skill Mastery Achieved!",
                "research_completion" => "Research Project Completed!",
                _ => "Milestone Achieved!"
            };
        }

        private string GenerateMilestoneDescription(string milestoneType)
        {
            return milestoneType switch
            {
                "major_discovery" => "You've made a major discovery that advances the field of genetic research!",
                "collaboration_success" => "Your collaborative efforts have resulted in significant breakthroughs!",
                "skill_mastery" => "You've mastered key research skills and techniques!",
                "research_completion" => "You've successfully completed a major research project!",
                _ => "You've reached an important milestone in your research journey!"
            };
        }

        private int CalculateCelebrationLevel(string milestoneType)
        {
            return milestoneType switch
            {
                "major_discovery" => 5,
                "collaboration_success" => 4,
                "skill_mastery" => 3,
                "research_completion" => 4,
                _ => 2
            };
        }

        private void AddMilestoneChoices(NarrativeEvent narrativeEvent, string milestoneType)
        {
            narrativeEvent.choices.Add(new NarrativeChoice
            {
                choiceId = "reflect",
                choiceText = "Reflect on Journey",
                description = "Take time to reflect on your progress",
                consequences = new Dictionary<string, object> { ["wisdom"] = 0.1f }
            });

            narrativeEvent.choices.Add(new NarrativeChoice
            {
                choiceId = "set_new_goal",
                choiceText = "Set New Goals",
                description = "Plan your next research objectives",
                consequences = new Dictionary<string, object> { ["motivation"] = 0.15f }
            });

            if (milestoneType == "collaboration_success")
            {
                narrativeEvent.choices.Add(new NarrativeChoice
                {
                    choiceId = "thank_collaborators",
                    choiceText = "Thank Collaborators",
                    description = "Express gratitude to your research partners",
                    consequences = new Dictionary<string, object> { ["social"] = 0.2f }
                });
            }
        }

        private string GenerateCelebrationTitle(string achievementType, string celebrationType)
        {
            if (celebrationType == "world_first")
                return $"World First: {FormatAchievementName(achievementType)}!";
            else
                return $"Celebrating: {FormatAchievementName(achievementType)}!";
        }

        private string GenerateCelebrationDescription(string achievementType, string celebrationType)
        {
            var baseDescription = GenerateAchievementDescription(achievementType);

            if (celebrationType == "world_first")
                return $"{baseDescription} This is a world-first discovery that will be remembered by the research community!";
            else
                return $"{baseDescription} Take pride in this accomplishment!";
        }

        private string FormatAchievementName(string achievementType)
        {
            return achievementType.Replace("_", " ").ToTitleCase();
        }

        // Opportunity creation methods
        private NarrativeOpportunity CreateResearchOpportunity(string playerId, string discoveryType)
        {
            return new NarrativeOpportunity
            {
                opportunityId = Guid.NewGuid().ToString(),
                narrativeType = "research",
                trigger = discoveryType,
                relevanceScore = 0.8f,
                estimatedDuration = TimeSpan.FromMinutes(10),
                requiredElements = new List<string> { "laboratory_access", "research_skills" },
                opportunityData = new Dictionary<string, object>
                {
                    ["researchType"] = "mutation_analysis",
                    ["expectedOutcome"] = "scientific_publication"
                }
            };
        }

        private NarrativeOpportunity CreateCollaborationOpportunity(string playerId, string discoveryType)
        {
            return new NarrativeOpportunity
            {
                opportunityId = Guid.NewGuid().ToString(),
                narrativeType = "collaboration",
                trigger = discoveryType,
                relevanceScore = 0.7f,
                estimatedDuration = TimeSpan.FromMinutes(15),
                requiredElements = new List<string> { "other_researchers", "shared_interest" },
                opportunityData = new Dictionary<string, object>
                {
                    ["collaborationType"] = "joint_research",
                    ["expectedBenefit"] = "knowledge_sharing"
                }
            };
        }

        private NarrativeOpportunity CreateExplorationOpportunity(string playerId, string discoveryType)
        {
            return new NarrativeOpportunity
            {
                opportunityId = Guid.NewGuid().ToString(),
                narrativeType = "exploration",
                trigger = discoveryType,
                relevanceScore = 0.6f,
                estimatedDuration = TimeSpan.FromMinutes(8),
                requiredElements = new List<string> { "exploration_tools", "time" },
                opportunityData = new Dictionary<string, object>
                {
                    ["explorationType"] = "habitat_study",
                    ["expectedDiscovery"] = "behavioral_patterns"
                }
            };
        }

        private NarrativeOpportunity CreateDocumentationOpportunity(string playerId, string discoveryType)
        {
            return new NarrativeOpportunity
            {
                opportunityId = Guid.NewGuid().ToString(),
                narrativeType = "documentation",
                trigger = discoveryType,
                relevanceScore = 0.5f,
                estimatedDuration = TimeSpan.FromMinutes(5),
                requiredElements = new List<string> { "documentation_tools" },
                opportunityData = new Dictionary<string, object>
                {
                    ["documentationType"] = "species_catalog",
                    ["importance"] = "scientific_record"
                }
            };
        }

        private NarrativeOpportunity CreateBreedingOpportunity(string playerId, string discoveryType)
        {
            return new NarrativeOpportunity
            {
                opportunityId = Guid.NewGuid().ToString(),
                narrativeType = "breeding",
                trigger = discoveryType,
                relevanceScore = 0.9f,
                estimatedDuration = TimeSpan.FromMinutes(12),
                requiredElements = new List<string> { "compatible_creatures", "breeding_facility" },
                opportunityData = new Dictionary<string, object>
                {
                    ["breedingType"] = "trait_enhancement",
                    ["expectedOutcome"] = "improved_traits"
                }
            };
        }

        private NarrativeOpportunity CreateObservationOpportunity(string playerId, string discoveryType)
        {
            return new NarrativeOpportunity
            {
                opportunityId = Guid.NewGuid().ToString(),
                narrativeType = "observation",
                trigger = discoveryType,
                relevanceScore = 0.6f,
                estimatedDuration = TimeSpan.FromMinutes(6),
                requiredElements = new List<string> { "observation_time", "patience" },
                opportunityData = new Dictionary<string, object>
                {
                    ["observationType"] = "behavioral_study",
                    ["expectedInsight"] = "behavior_understanding"
                }
            };
        }

        private NarrativeOpportunity CreateGenericOpportunity(string playerId, string discoveryType)
        {
            return new NarrativeOpportunity
            {
                opportunityId = Guid.NewGuid().ToString(),
                narrativeType = "general",
                trigger = discoveryType,
                relevanceScore = 0.4f,
                estimatedDuration = TimeSpan.FromMinutes(5),
                requiredElements = new List<string> { "curiosity" },
                opportunityData = new Dictionary<string, object>
                {
                    ["opportunityType"] = "further_investigation",
                    ["benefit"] = "increased_knowledge"
                }
            };
        }

        #endregion

        #region Helper Classes

        private class NarrativeTemplate
        {
            public string templateId;
            public NarrativeType narrativeType;
            public string title;
            public string descriptionTemplate;
            public TimeSpan estimatedDuration;
            public float weight;
        }

        #endregion
    }

    #region Extension Methods

    public static class StringExtensions
    {
        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return string.Join(" ", words);
        }
    }

    #endregion
}