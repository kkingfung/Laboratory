using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;

namespace Laboratory.Subsystems.AIDirector
{
    /// <summary>
    /// Concrete implementation of content orchestration service
    /// Manages dynamic content spawning, adaptation, and collaborative opportunities
    /// </summary>
    public class ContentOrchestrationService : IContentOrchestrationService
    {
        #region Fields

        private readonly AIDirectorSubsystemConfig _config;
        private Dictionary<string, List<ContentAdaptation>> _playerAdaptations;
        private Dictionary<string, Queue<CollaborativeOpportunity>> _collaborativeOpportunities;
        private List<ContentTemplate> _contentTemplates;
        private Dictionary<string, int> _contentUsageCount;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public ContentOrchestrationService(AIDirectorSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IContentOrchestrationService Implementation

        public async Task<bool> InitializeAsync()
        {
            await Task.CompletedTask; // Synchronous initialization, but async for interface compatibility

            try
            {
                _playerAdaptations = new Dictionary<string, List<ContentAdaptation>>();
                _collaborativeOpportunities = new Dictionary<string, Queue<CollaborativeOpportunity>>();
                _contentTemplates = new List<ContentTemplate>();
                _contentUsageCount = new Dictionary<string, int>();

                // Initialize content templates
                InitializeContentTemplates();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[ContentOrchestrationService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ContentOrchestrationService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void SpawnContent(string playerId, string contentType, object spawnParameters)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return;

            var template = FindContentTemplate(contentType);
            if (template == null)
            {
                if (_config.enableDebugLogging)
                    Debug.LogWarning($"[ContentOrchestrationService] No template found for content type: {contentType}");
                return;
            }

            var spawnData = ProcessSpawnParameters(spawnParameters);
            ExecuteContentSpawn(playerId, template, spawnData);

            // Update usage count
            _contentUsageCount[contentType] = _contentUsageCount.GetValueOrDefault(contentType, 0) + 1;

            if (_config.enableDebugLogging)
                Debug.Log($"[ContentOrchestrationService] Spawned {contentType} content for {playerId}");
        }

        public void EncourageCollaboration(string playerId, string collaborationType, List<string> suggestedPartners)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return;

            var opportunity = new CollaborativeOpportunity
            {
                opportunityId = Guid.NewGuid().ToString(),
                potentialParticipants = new List<string> { playerId },
                collaborationType = collaborationType,
                suggestedActivity = GenerateCollaborativeActivity(collaborationType),
                compatibilityScore = CalculateCompatibilityScore(playerId, suggestedPartners),
                suggestedTime = DateTime.Now.AddMinutes(UnityEngine.Random.Range(5, 30)),
                opportunityDetails = new Dictionary<string, object>
                {
                    ["initiator"] = playerId,
                    ["suggestionReason"] = GenerateCollaborationReason(collaborationType),
                    ["expectedDuration"] = GenerateExpectedDuration(collaborationType),
                    ["difficultyLevel"] = DetermineCollaborationDifficulty(collaborationType)
                }
            };

            // Add suggested partners
            if (suggestedPartners != null)
            {
                opportunity.potentialParticipants.AddRange(suggestedPartners);
            }

            // Queue opportunity for all participants
            foreach (var participantId in opportunity.potentialParticipants)
            {
                if (!_collaborativeOpportunities.ContainsKey(participantId))
                    _collaborativeOpportunities[participantId] = new Queue<CollaborativeOpportunity>();

                _collaborativeOpportunities[participantId].Enqueue(opportunity);
            }

            if (_config.enableDebugLogging)
                Debug.Log($"[ContentOrchestrationService] Created {collaborationType} collaboration opportunity for {opportunity.potentialParticipants.Count} participants");
        }

        public List<ContentAdaptation> GenerateAdaptations(PlayerProfile profile, DirectorContext context)
        {
            if (!_isInitialized || profile == null || context == null)
                return new List<ContentAdaptation>();

            var adaptations = new List<ContentAdaptation>();

            // Generate difficulty adaptations
            var difficultyAdaptation = GenerateDifficultyAdaptation(profile, context);
            if (difficultyAdaptation != null)
                adaptations.Add(difficultyAdaptation);

            // Generate pacing adaptations
            var pacingAdaptation = GeneratePacingAdaptation(profile, context);
            if (pacingAdaptation != null)
                adaptations.Add(pacingAdaptation);

            // Generate style adaptations
            var styleAdaptation = GenerateStyleAdaptation(profile, context);
            if (styleAdaptation != null)
                adaptations.Add(styleAdaptation);

            // Generate personalization updates
            var personalizationAdaptation = GeneratePersonalizationAdaptation(profile, context);
            if (personalizationAdaptation != null)
                adaptations.Add(personalizationAdaptation);

            // Store adaptations for the player
            if (!_playerAdaptations.ContainsKey(profile.playerId))
                _playerAdaptations[profile.playerId] = new List<ContentAdaptation>();

            _playerAdaptations[profile.playerId].AddRange(adaptations);

            // Keep adaptation history manageable
            if (_playerAdaptations[profile.playerId].Count > 20)
            {
                _playerAdaptations[profile.playerId].RemoveRange(0, _playerAdaptations[profile.playerId].Count - 20);
            }

            // Trigger events for each adaptation
            foreach (var adaptation in adaptations)
            {
                AIDirectorSubsystemManager.TriggerContentAdaptedEvent(adaptation);
            }

            if (_config.enableDebugLogging && adaptations.Count > 0)
                Debug.Log($"[ContentOrchestrationService] Generated {adaptations.Count} adaptations for {profile.playerId}");

            return adaptations;
        }

        public List<CollaborativeOpportunity> FindCollaborativeOpportunities(string playerId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return new List<CollaborativeOpportunity>();

            var opportunities = new List<CollaborativeOpportunity>();

            if (_collaborativeOpportunities.TryGetValue(playerId, out var playerOpportunities))
            {
                // Return available opportunities
                opportunities.AddRange(playerOpportunities);
            }

            return opportunities;
        }

        #endregion

        #region Private Methods

        private void InitializeContentTemplates()
        {
            _contentTemplates.AddRange(new[]
            {
                // Creature spawning templates
                new ContentTemplate
                {
                    templateId = "basic_creature_spawn",
                    contentType = "creature",
                    spawnFunction = SpawnBasicCreature,
                    difficulty = DifficultyLevel.Easy,
                    weight = 1f,
                    requirements = new List<string> { "breeding_facility" }
                },

                new ContentTemplate
                {
                    templateId = "rare_creature_spawn",
                    contentType = "rare_creature",
                    spawnFunction = SpawnRareCreature,
                    difficulty = DifficultyLevel.Hard,
                    weight = 0.3f,
                    requirements = new List<string> { "advanced_facility", "high_skill" }
                },

                // Research content templates
                new ContentTemplate
                {
                    templateId = "research_opportunity",
                    contentType = "research",
                    spawnFunction = SpawnResearchOpportunity,
                    difficulty = DifficultyLevel.Medium,
                    weight = 0.8f,
                    requirements = new List<string> { "laboratory_access" }
                },

                // Challenge templates
                new ContentTemplate
                {
                    templateId = "breeding_challenge",
                    contentType = "challenge",
                    spawnFunction = SpawnBreedingChallenge,
                    difficulty = DifficultyLevel.Medium,
                    weight = 0.7f,
                    requirements = new List<string> { "breeding_experience" }
                },

                // Educational content templates
                new ContentTemplate
                {
                    templateId = "tutorial_content",
                    contentType = "tutorial",
                    spawnFunction = SpawnTutorialContent,
                    difficulty = DifficultyLevel.Easy,
                    weight = 1f,
                    requirements = new List<string>()
                },

                // Collaborative content templates
                new ContentTemplate
                {
                    templateId = "collaboration_project",
                    contentType = "collaboration",
                    spawnFunction = SpawnCollaborationProject,
                    difficulty = DifficultyLevel.Medium,
                    weight = 0.6f,
                    requirements = new List<string> { "social_features" }
                }
            });
        }

        private ContentTemplate FindContentTemplate(string contentType)
        {
            var candidateTemplates = _contentTemplates.FindAll(t => t.contentType == contentType);

            if (candidateTemplates.Count == 0)
                return null;

            // Prefer less-used templates
            candidateTemplates.Sort((a, b) =>
            {
                var usageA = _contentUsageCount.GetValueOrDefault(a.templateId, 0);
                var usageB = _contentUsageCount.GetValueOrDefault(b.templateId, 0);
                return usageA.CompareTo(usageB);
            });

            return candidateTemplates[0];
        }

        private Dictionary<string, object> ProcessSpawnParameters(object spawnParameters)
        {
            if (spawnParameters is Dictionary<string, object> dict)
                return dict;

            return new Dictionary<string, object>();
        }

        private void ExecuteContentSpawn(string playerId, ContentTemplate template, Dictionary<string, object> spawnData)
        {
            try
            {
                template.spawnFunction?.Invoke(playerId, spawnData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ContentOrchestrationService] Failed to spawn content: {ex.Message}");
            }
        }

        // Content spawning methods
        private void SpawnBasicCreature(string playerId, Dictionary<string, object> spawnData)
        {
            if (_config.enableDebugLogging)
                Debug.Log($"[ContentOrchestrationService] Spawning basic creature for {playerId}");

            // Implementation would integrate with creature spawning system
            // For now, just log the spawn event
        }

        private void SpawnRareCreature(string playerId, Dictionary<string, object> spawnData)
        {
            if (_config.enableDebugLogging)
                Debug.Log($"[ContentOrchestrationService] Spawning rare creature for {playerId}");

            // Implementation would integrate with creature spawning system
            // Rare creatures would have special traits or appearances
        }

        private void SpawnResearchOpportunity(string playerId, Dictionary<string, object> spawnData)
        {
            if (_config.enableDebugLogging)
                Debug.Log($"[ContentOrchestrationService] Creating research opportunity for {playerId}");

            // Implementation would integrate with research system
            // Create a research project or discovery opportunity
        }

        private void SpawnBreedingChallenge(string playerId, Dictionary<string, object> spawnData)
        {
            if (_config.enableDebugLogging)
                Debug.Log($"[ContentOrchestrationService] Creating breeding challenge for {playerId}");

            // Implementation would create a specific breeding goal or challenge
        }

        private void SpawnTutorialContent(string playerId, Dictionary<string, object> spawnData)
        {
            if (_config.enableDebugLogging)
                Debug.Log($"[ContentOrchestrationService] Creating tutorial content for {playerId}");

            // Implementation would create educational content or guided experiences
        }

        private void SpawnCollaborationProject(string playerId, Dictionary<string, object> spawnData)
        {
            if (_config.enableDebugLogging)
                Debug.Log($"[ContentOrchestrationService] Creating collaboration project for {playerId}");

            // Implementation would create a multi-player project or shared goal
        }

        // Collaboration methods
        private string GenerateCollaborativeActivity(string collaborationType)
        {
            return collaborationType switch
            {
                "research" => "Joint research project on genetic traits",
                "breeding" => "Collaborative breeding program",
                "exploration" => "Shared ecosystem exploration",
                "analysis" => "Data analysis collaboration",
                "publication" => "Co-authored research paper",
                _ => "General collaboration project"
            };
        }

        private float CalculateCompatibilityScore(string playerId, List<string> suggestedPartners)
        {
            if (suggestedPartners == null || suggestedPartners.Count == 0)
                return 0.5f;

            // Simple compatibility calculation
            // In a real implementation, this would consider player profiles, interests, and skills
            return math.min(1f, 0.5f + (suggestedPartners.Count * 0.1f));
        }

        private string GenerateCollaborationReason(string collaborationType)
        {
            return collaborationType switch
            {
                "research" => "Combining expertise could accelerate discovery",
                "breeding" => "Shared genetic material could improve outcomes",
                "exploration" => "Team exploration is safer and more effective",
                "analysis" => "Multiple perspectives improve data interpretation",
                _ => "Collaboration often leads to better results"
            };
        }

        private TimeSpan GenerateExpectedDuration(string collaborationType)
        {
            return collaborationType switch
            {
                "research" => TimeSpan.FromMinutes(30),
                "breeding" => TimeSpan.FromMinutes(20),
                "exploration" => TimeSpan.FromMinutes(15),
                "analysis" => TimeSpan.FromMinutes(25),
                "publication" => TimeSpan.FromMinutes(45),
                _ => TimeSpan.FromMinutes(20)
            };
        }

        private DifficultyLevel DetermineCollaborationDifficulty(string collaborationType)
        {
            return collaborationType switch
            {
                "research" => DifficultyLevel.Hard,
                "breeding" => DifficultyLevel.Medium,
                "exploration" => DifficultyLevel.Easy,
                "analysis" => DifficultyLevel.Medium,
                "publication" => DifficultyLevel.VeryHard,
                _ => DifficultyLevel.Medium
            };
        }

        // Adaptation generation methods
        private ContentAdaptation GenerateDifficultyAdaptation(PlayerProfile profile, DirectorContext context)
        {
            // Check if difficulty adaptation is needed
            if (context.engagement < 0.4f && context.frustrationLevel > 0.6f)
            {
                return new ContentAdaptation
                {
                    adaptationId = Guid.NewGuid().ToString(),
                    playerId = profile.playerId,
                    adaptationType = ContentAdaptationType.DifficultyAdjustment,
                    reason = "Player showing frustration - reducing difficulty",
                    adaptationTime = DateTime.Now,
                    expectedImpact = 0.3f,
                    adaptationParameters = new Dictionary<string, object>
                    {
                        ["adjustmentDirection"] = "decrease",
                        ["magnitude"] = 0.2f,
                        ["targetEngagement"] = 0.6f
                    }
                };
            }
            else if (context.engagement > 0.8f && context.flowState > 0.7f)
            {
                return new ContentAdaptation
                {
                    adaptationId = Guid.NewGuid().ToString(),
                    playerId = profile.playerId,
                    adaptationType = ContentAdaptationType.DifficultyAdjustment,
                    reason = "Player in flow state - can handle more challenge",
                    adaptationTime = DateTime.Now,
                    expectedImpact = 0.2f,
                    adaptationParameters = new Dictionary<string, object>
                    {
                        ["adjustmentDirection"] = "increase",
                        ["magnitude"] = 0.15f,
                        ["maintainFlow"] = true
                    }
                };
            }

            return null;
        }

        private ContentAdaptation GeneratePacingAdaptation(PlayerProfile profile, DirectorContext context)
        {
            // Adapt pacing based on player behavior
            if (context.timeInSession > 60 && context.engagement < 0.5f)
            {
                return new ContentAdaptation
                {
                    adaptationId = Guid.NewGuid().ToString(),
                    playerId = profile.playerId,
                    adaptationType = ContentAdaptationType.PacingChange,
                    reason = "Long session with declining engagement - suggesting break",
                    adaptationTime = DateTime.Now,
                    expectedImpact = 0.4f,
                    adaptationParameters = new Dictionary<string, object>
                    {
                        ["pacingAdjustment"] = "slower",
                        ["suggestBreak"] = true,
                        ["breakDuration"] = 10
                    }
                };
            }
            else if (context.progressRate > 0.8f && profile.learningVelocity > 1.2f)
            {
                return new ContentAdaptation
                {
                    adaptationId = Guid.NewGuid().ToString(),
                    playerId = profile.playerId,
                    adaptationType = ContentAdaptationType.PacingChange,
                    reason = "Fast learner - can handle accelerated pace",
                    adaptationTime = DateTime.Now,
                    expectedImpact = 0.2f,
                    adaptationParameters = new Dictionary<string, object>
                    {
                        ["pacingAdjustment"] = "faster",
                        ["advancedContent"] = true
                    }
                };
            }

            return null;
        }

        private ContentAdaptation GenerateStyleAdaptation(PlayerProfile profile, DirectorContext context)
        {
            // Adapt presentation style based on learning preferences
            if (profile.learningStyle == LearningStyle.Visual && context.engagement < 0.6f)
            {
                return new ContentAdaptation
                {
                    adaptationId = Guid.NewGuid().ToString(),
                    playerId = profile.playerId,
                    adaptationType = ContentAdaptationType.StyleAdaptation,
                    reason = "Visual learner needs more visual content",
                    adaptationTime = DateTime.Now,
                    expectedImpact = 0.3f,
                    adaptationParameters = new Dictionary<string, object>
                    {
                        ["emphasizeVisuals"] = true,
                        ["reduceText"] = true,
                        ["addDiagrams"] = true
                    }
                };
            }
            else if (profile.learningStyle == LearningStyle.Kinesthetic && context.socialActivity < 0.3f)
            {
                return new ContentAdaptation
                {
                    adaptationId = Guid.NewGuid().ToString(),
                    playerId = profile.playerId,
                    adaptationType = ContentAdaptationType.StyleAdaptation,
                    reason = "Kinesthetic learner needs more hands-on activities",
                    adaptationTime = DateTime.Now,
                    expectedImpact = 0.4f,
                    adaptationParameters = new Dictionary<string, object>
                    {
                        ["addInteractiveElements"] = true,
                        ["encourageExperimentation"] = true,
                        ["provideHandsOnTasks"] = true
                    }
                };
            }

            return null;
        }

        private ContentAdaptation GeneratePersonalizationAdaptation(PlayerProfile profile, DirectorContext context)
        {
            // Update personalization based on observed preferences
            if (profile.interests.Count > 0 && context.engagement > 0.7f)
            {
                return new ContentAdaptation
                {
                    adaptationId = Guid.NewGuid().ToString(),
                    playerId = profile.playerId,
                    adaptationType = ContentAdaptationType.PersonalizationUpdate,
                    reason = "Strong engagement - reinforcing successful content types",
                    adaptationTime = DateTime.Now,
                    expectedImpact = 0.2f,
                    adaptationParameters = new Dictionary<string, object>
                    {
                        ["reinforceInterests"] = true,
                        ["expandSimilarContent"] = true,
                        ["updatePreferences"] = true
                    }
                };
            }

            return null;
        }

        #endregion

        #region Helper Classes

        private class ContentTemplate
        {
            public string templateId;
            public string contentType;
            public Action<string, Dictionary<string, object>> spawnFunction;
            public DifficultyLevel difficulty;
            public float weight;
            public List<string> requirements;
        }

        #endregion
    }
}