using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;

namespace Laboratory.Subsystems.AIDirector
{
    /// <summary>
    /// Concrete implementation of emergent storytelling service
    /// Generates dynamic stories and manages collaborative storylines
    /// </summary>
    public class EmergentStorytellingService : IEmergentStorytellingService
    {
        #region Fields

        private readonly AIDirectorSubsystemConfig _config;
        private List<EmergentStory> _activeStories;
        private List<ActiveStoryline> _activeStorylines;
        private Dictionary<string, List<StoryElement>> _storyElements;
        private EmergentTrends _currentTrends;
        private Dictionary<string, float> _storyTypeWeights;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public EmergentStorytellingService(AIDirectorSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IEmergentStorytellingService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _activeStories = new List<EmergentStory>();
                _activeStorylines = new List<ActiveStoryline>();
                _storyElements = new Dictionary<string, List<StoryElement>>();
                _currentTrends = new EmergentTrends();
                _storyTypeWeights = new Dictionary<string, float>();

                // Initialize story elements and weights
                InitializeStoryElements();
                InitializeStoryTypeWeights();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[EmergentStorytellingService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EmergentStorytellingService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public List<EmergentStory> GenerateEmergentContent()
        {
            if (!_isInitialized)
                return new List<EmergentStory>();

            var newStories = new List<EmergentStory>();

            // Generate different types of emergent stories
            if (ShouldGenerateDiscoveryStory())
            {
                var discoveryStory = GenerateDiscoveryStory();
                if (discoveryStory != null)
                    newStories.Add(discoveryStory);
            }

            if (ShouldGenerateCollaborationStory())
            {
                var collaborationStory = GenerateCollaborationStory();
                if (collaborationStory != null)
                    newStories.Add(collaborationStory);
            }

            if (ShouldGenerateMysteryStory())
            {
                var mysteryStory = GenerateMysteryStory();
                if (mysteryStory != null)
                    newStories.Add(mysteryStory);
            }

            if (ShouldGenerateEvolutionStory())
            {
                var evolutionStory = GenerateEvolutionStory();
                if (evolutionStory != null)
                    newStories.Add(evolutionStory);
            }

            // Add to active stories
            _activeStories.AddRange(newStories);

            // Clean up completed stories
            CleanupCompletedStories();

            if (_config.enableDebugLogging && newStories.Count > 0)
                Debug.Log($"[EmergentStorytellingService] Generated {newStories.Count} emergent stories");

            return newStories;
        }

        public List<ActiveStoryline> GenerateCollaborativeStorylines(string playerId, string collaborationType)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return new List<ActiveStoryline>();

            var storylines = new List<ActiveStoryline>();

            switch (collaborationType)
            {
                case "research":
                    storylines.AddRange(GenerateResearchStorylines(playerId));
                    break;

                case "breeding":
                    storylines.AddRange(GenerateBreedingStorylines(playerId));
                    break;

                case "exploration":
                    storylines.AddRange(GenerateExplorationStorylines(playerId));
                    break;

                case "discovery":
                    storylines.AddRange(GenerateDiscoveryStorylines(playerId));
                    break;

                default:
                    storylines.AddRange(GenerateGenericCollaborativeStorylines(playerId));
                    break;
            }

            // Add to active storylines
            _activeStorylines.AddRange(storylines);

            if (_config.enableDebugLogging && storylines.Count > 0)
                Debug.Log($"[EmergentStorytellingService] Generated {storylines.Count} collaborative storylines for {playerId}");

            return storylines;
        }

        public void CompleteStoryline(ActiveStoryline storyline)
        {
            if (!_isInitialized || storyline == null)
                return;

            storyline.isCompleted = true;
            storyline.isActive = false;

            // Generate completion story elements
            GenerateCompletionElements(storyline);

            // Update trends based on completion
            UpdateTrendsFromCompletion(storyline);

            if (_config.enableDebugLogging)
                Debug.Log($"[EmergentStorytellingService] Completed storyline: {storyline.storylineId}");
        }

        public EmergentTrends AnalyzeEmergentTrends()
        {
            if (!_isInitialized)
                return new EmergentTrends();

            UpdateEmergentTrends();
            return _currentTrends;
        }

        #endregion

        #region Private Methods

        private void InitializeStoryElements()
        {
            _storyElements["discovery"] = new List<StoryElement>
            {
                new StoryElement { elementType = "discovery", name = "Rare Mutation", weight = 0.3f, description = "A rare genetic mutation appears" },
                new StoryElement { elementType = "discovery", name = "New Species", weight = 0.2f, description = "A previously unknown species is discovered" },
                new StoryElement { elementType = "discovery", name = "Behavioral Pattern", weight = 0.4f, description = "An unusual behavioral pattern emerges" },
                new StoryElement { elementType = "discovery", name = "Environmental Change", weight = 0.5f, description = "The environment shows unexpected changes" }
            };

            _storyElements["collaboration"] = new List<StoryElement>
            {
                new StoryElement { elementType = "collaboration", name = "Research Partnership", weight = 0.6f, description = "Researchers join forces" },
                new StoryElement { elementType = "collaboration", name = "Data Sharing", weight = 0.7f, description = "Scientists share crucial data" },
                new StoryElement { elementType = "collaboration", name = "Joint Expedition", weight = 0.4f, description = "A collaborative exploration begins" },
                new StoryElement { elementType = "collaboration", name = "Knowledge Exchange", weight = 0.8f, description = "Experts exchange insights" }
            };

            _storyElements["mystery"] = new List<StoryElement>
            {
                new StoryElement { elementType = "mystery", name = "Vanishing Traits", weight = 0.3f, description = "Certain traits mysteriously disappear" },
                new StoryElement { elementType = "mystery", name = "Unexpected Inheritance", weight = 0.4f, description = "Traits appear that shouldn't exist" },
                new StoryElement { elementType = "mystery", name = "Population Anomaly", weight = 0.5f, description = "Population numbers don't match expectations" },
                new StoryElement { elementType = "mystery", name = "Behavioral Shift", weight = 0.6f, description = "Creatures act differently than expected" }
            };

            _storyElements["evolution"] = new List<StoryElement>
            {
                new StoryElement { elementType = "evolution", name = "Rapid Adaptation", weight = 0.4f, description = "Species adapts quickly to change" },
                new StoryElement { elementType = "evolution", name = "Convergent Evolution", weight = 0.3f, description = "Different species develop similar traits" },
                new StoryElement { elementType = "evolution", name = "Evolutionary Pressure", weight = 0.5f, description = "Environmental pressure drives change" },
                new StoryElement { elementType = "evolution", name = "Genetic Drift", weight = 0.6f, description = "Random genetic changes accumulate" }
            };
        }

        private void InitializeStoryTypeWeights()
        {
            _storyTypeWeights["discovery"] = 0.4f;
            _storyTypeWeights["collaboration"] = 0.3f;
            _storyTypeWeights["mystery"] = 0.2f;
            _storyTypeWeights["evolution"] = 0.1f;
        }

        // Story generation condition methods
        private bool ShouldGenerateDiscoveryStory()
        {
            var threshold = _storyTypeWeights["discovery"] * _config.narrativeTriggerSensitivity;
            return UnityEngine.Random.value < threshold;
        }

        private bool ShouldGenerateCollaborationStory()
        {
            var threshold = _storyTypeWeights["collaboration"] * _config.narrativeTriggerSensitivity;
            return UnityEngine.Random.value < threshold;
        }

        private bool ShouldGenerateMysteryStory()
        {
            var threshold = _storyTypeWeights["mystery"] * _config.narrativeTriggerSensitivity;
            return UnityEngine.Random.value < threshold;
        }

        private bool ShouldGenerateEvolutionStory()
        {
            var threshold = _storyTypeWeights["evolution"] * _config.narrativeTriggerSensitivity;
            return UnityEngine.Random.value < threshold;
        }

        // Story generation methods
        private EmergentStory GenerateDiscoveryStory()
        {
            var elements = _storyElements["discovery"];
            var selectedElement = SelectWeightedElement(elements);

            return new EmergentStory
            {
                storyId = Guid.NewGuid().ToString(),
                storyType = "discovery",
                title = $"The {selectedElement.name}",
                synopsis = GenerateDiscoverySynopsis(selectedElement),
                startTime = DateTime.Now,
                estimatedDuration = TimeSpan.FromMinutes(UnityEngine.Random.Range(10, 30)),
                complexity = DetermineStoryComplexity(selectedElement),
                playerImpact = CalculatePlayerImpact(selectedElement),
                storyElements = new Dictionary<string, object>
                {
                    ["primaryElement"] = selectedElement.name,
                    ["elementType"] = selectedElement.elementType,
                    ["discoveryDifficulty"] = CalculateDiscoveryDifficulty(selectedElement)
                }
            };
        }

        private EmergentStory GenerateCollaborationStory()
        {
            var elements = _storyElements["collaboration"];
            var selectedElement = SelectWeightedElement(elements);

            return new EmergentStory
            {
                storyId = Guid.NewGuid().ToString(),
                storyType = "collaboration",
                title = $"The {selectedElement.name} Initiative",
                synopsis = GenerateCollaborationSynopsis(selectedElement),
                startTime = DateTime.Now,
                estimatedDuration = TimeSpan.FromMinutes(UnityEngine.Random.Range(15, 45)),
                complexity = StoryComplexity.Moderate,
                playerImpact = 0.7f,
                storyElements = new Dictionary<string, object>
                {
                    ["collaborationType"] = selectedElement.name,
                    ["participantCount"] = UnityEngine.Random.Range(2, 5),
                    ["expectedOutcome"] = "shared_knowledge"
                }
            };
        }

        private EmergentStory GenerateMysteryStory()
        {
            var elements = _storyElements["mystery"];
            var selectedElement = SelectWeightedElement(elements);

            return new EmergentStory
            {
                storyId = Guid.NewGuid().ToString(),
                storyType = "mystery",
                title = $"The Mystery of {selectedElement.name}",
                synopsis = GenerateMysterySynopsis(selectedElement),
                startTime = DateTime.Now,
                estimatedDuration = TimeSpan.FromMinutes(UnityEngine.Random.Range(20, 60)),
                complexity = StoryComplexity.Complex,
                playerImpact = 0.8f,
                storyElements = new Dictionary<string, object>
                {
                    ["mysteryType"] = selectedElement.name,
                    ["clueCount"] = UnityEngine.Random.Range(3, 7),
                    ["difficultyLevel"] = "high"
                }
            };
        }

        private EmergentStory GenerateEvolutionStory()
        {
            var elements = _storyElements["evolution"];
            var selectedElement = SelectWeightedElement(elements);

            return new EmergentStory
            {
                storyId = Guid.NewGuid().ToString(),
                storyType = "evolution",
                title = $"Evolution: {selectedElement.name}",
                synopsis = GenerateEvolutionSynopsis(selectedElement),
                startTime = DateTime.Now,
                estimatedDuration = TimeSpan.FromMinutes(UnityEngine.Random.Range(30, 90)),
                complexity = StoryComplexity.Epic,
                playerImpact = 0.9f,
                storyElements = new Dictionary<string, object>
                {
                    ["evolutionType"] = selectedElement.name,
                    ["timeScale"] = "generations",
                    ["impact"] = "ecosystem_wide"
                }
            };
        }

        // Collaborative storyline generation methods
        private List<ActiveStoryline> GenerateResearchStorylines(string playerId)
        {
            return new List<ActiveStoryline>
            {
                new ActiveStoryline
                {
                    storylineId = Guid.NewGuid().ToString(),
                    playerId = playerId,
                    narrativeType = "research_collaboration",
                    startTime = DateTime.Now,
                    estimatedDuration = TimeSpan.FromMinutes(30),
                    progress = 0f,
                    isActive = true,
                    currentEvent = "research_project_start"
                }
            };
        }

        private List<ActiveStoryline> GenerateBreedingStorylines(string playerId)
        {
            return new List<ActiveStoryline>
            {
                new ActiveStoryline
                {
                    storylineId = Guid.NewGuid().ToString(),
                    playerId = playerId,
                    narrativeType = "breeding_collaboration",
                    startTime = DateTime.Now,
                    estimatedDuration = TimeSpan.FromMinutes(20),
                    progress = 0f,
                    isActive = true,
                    currentEvent = "breeding_program_launch"
                }
            };
        }

        private List<ActiveStoryline> GenerateExplorationStorylines(string playerId)
        {
            return new List<ActiveStoryline>
            {
                new ActiveStoryline
                {
                    storylineId = Guid.NewGuid().ToString(),
                    playerId = playerId,
                    narrativeType = "exploration_collaboration",
                    startTime = DateTime.Now,
                    estimatedDuration = TimeSpan.FromMinutes(25),
                    progress = 0f,
                    isActive = true,
                    currentEvent = "expedition_planning"
                }
            };
        }

        private List<ActiveStoryline> GenerateDiscoveryStorylines(string playerId)
        {
            return new List<ActiveStoryline>
            {
                new ActiveStoryline
                {
                    storylineId = Guid.NewGuid().ToString(),
                    playerId = playerId,
                    narrativeType = "discovery_collaboration",
                    startTime = DateTime.Now,
                    estimatedDuration = TimeSpan.FromMinutes(15),
                    progress = 0f,
                    isActive = true,
                    currentEvent = "discovery_investigation"
                }
            };
        }

        private List<ActiveStoryline> GenerateGenericCollaborativeStorylines(string playerId)
        {
            return new List<ActiveStoryline>
            {
                new ActiveStoryline
                {
                    storylineId = Guid.NewGuid().ToString(),
                    playerId = playerId,
                    narrativeType = "general_collaboration",
                    startTime = DateTime.Now,
                    estimatedDuration = TimeSpan.FromMinutes(20),
                    progress = 0f,
                    isActive = true,
                    currentEvent = "collaboration_start"
                }
            };
        }

        // Helper methods
        private StoryElement SelectWeightedElement(List<StoryElement> elements)
        {
            var totalWeight = 0f;
            foreach (var element in elements)
                totalWeight += element.weight;

            var randomValue = UnityEngine.Random.value * totalWeight;
            var currentWeight = 0f;

            foreach (var element in elements)
            {
                currentWeight += element.weight;
                if (randomValue <= currentWeight)
                    return element;
            }

            return elements[0]; // Fallback
        }

        private string GenerateDiscoverySynopsis(StoryElement element)
        {
            return $"Scientists have observed {element.description.ToLower()}. This discovery could reshape our understanding of genetic inheritance and species development.";
        }

        private string GenerateCollaborationSynopsis(StoryElement element)
        {
            return $"{element.description} has begun. Multiple research teams are working together to unlock new insights and accelerate discovery.";
        }

        private string GenerateMysterySynopsis(StoryElement element)
        {
            return $"A puzzling phenomenon has emerged: {element.description.ToLower()}. Researchers must investigate to uncover the truth behind this mystery.";
        }

        private string GenerateEvolutionSynopsis(StoryElement element)
        {
            return $"Evolution is at work: {element.description.ToLower()}. This evolutionary process could have far-reaching implications for the entire ecosystem.";
        }

        private StoryComplexity DetermineStoryComplexity(StoryElement element)
        {
            return element.weight switch
            {
                < 0.3f => StoryComplexity.Complex,
                < 0.6f => StoryComplexity.Moderate,
                _ => StoryComplexity.Simple
            };
        }

        private float CalculatePlayerImpact(StoryElement element)
        {
            return math.clamp(0.5f + (1f - element.weight), 0.1f, 1f);
        }

        private float CalculateDiscoveryDifficulty(StoryElement element)
        {
            return 1f - element.weight; // Lower weight = higher difficulty
        }

        private void CleanupCompletedStories()
        {
            _activeStories.RemoveAll(story =>
            {
                var elapsed = DateTime.Now - story.startTime;
                return elapsed > story.estimatedDuration;
            });

            _activeStorylines.RemoveAll(storyline => storyline.isCompleted || storyline.hasExpired);
        }

        private void GenerateCompletionElements(ActiveStoryline storyline)
        {
            // Generate story elements based on completion
            storyline.completedEvents.Add($"completion_{DateTime.Now:HHmmss}");
        }

        private void UpdateTrendsFromCompletion(ActiveStoryline storyline)
        {
            // Update trends based on storyline completion
            if (!_currentTrends.popularActivities.Contains(storyline.narrativeType))
            {
                _currentTrends.popularActivities.Add(storyline.narrativeType);
            }

            _currentTrends.lastUpdate = DateTime.Now;
        }

        private void UpdateEmergentTrends()
        {
            _currentTrends.lastUpdate = DateTime.Now;

            // Analyze active stories to identify trends
            var storyTypes = new Dictionary<string, int>();
            foreach (var story in _activeStories)
            {
                storyTypes[story.storyType] = storyTypes.GetValueOrDefault(story.storyType, 0) + 1;
            }

            // Update emerging interests based on story types
            _currentTrends.emergingInterests.Clear();
            foreach (var kvp in storyTypes)
            {
                if (kvp.Value > 1) // Multiple stories of same type indicate trend
                {
                    _currentTrends.emergingInterests.Add(kvp.Key);
                }
            }

            // Update collaboration patterns
            var collaborationCount = _activeStorylines.Where(s => s.narrativeType.Contains("collaboration")).Count();
            _currentTrends.collaborationPatterns["active_collaborations"] = collaborationCount;

            // Calculate overall engagement trend
            _currentTrends.overallEngagementTrend = CalculateEngagementTrend();
        }

        private float CalculateEngagementTrend()
        {
            if (_activeStories.Count == 0)
                return 0.5f;

            var totalImpact = 0f;
            foreach (var story in _activeStories)
            {
                totalImpact += story.playerImpact;
            }

            return math.clamp(totalImpact / _activeStories.Count, 0f, 1f);
        }

        #endregion

        #region Helper Classes

        private class StoryElement
        {
            public string elementType;
            public string name;
            public float weight;
            public string description;
        }

        #endregion
    }
}