using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;
using Laboratory.Chimera.Genetics.Advanced;
using Laboratory.Systems.Analytics;
using Laboratory.Systems.Ecosystem;
using Laboratory.AI.Personality;

namespace Laboratory.Systems.Storytelling
{
    /// <summary>
    /// AI-driven storytelling system that creates dynamic narratives based on
    /// creature genetics, ecosystem events, player behavior, and emergent gameplay.
    /// Generates contextual stories, character development, and narrative arcs
    /// that respond to the living world simulation.
    /// </summary>
    public class AIStorytellerSystem : MonoBehaviour
    {
        [Header("Storytelling Configuration")]
        [SerializeField] private bool enableStoryGeneration = true;
        [SerializeField] private float storyUpdateInterval = 15f;
        [SerializeField] private int maxActiveStories = 5;

        [Header("Narrative Style")]
        [SerializeField, Range(0f, 1f)] private float dramaticIntensity = 0.6f;
        [SerializeField, Range(0f, 1f)] private float humorLevel = 0.3f;
        [SerializeField, Range(0f, 1f)] private float technicalDetail = 0.7f;

        [Header("Story Generation")]
        [SerializeField] private StoryTemplate[] storyTemplates;
        [SerializeField] private CharacterArchetype[] characterArchetypes;
        [SerializeField] private PlotDevice[] plotDevices;
        [SerializeField] private NarrativeTheme[] narrativeThemes;

        [Header("Content Sources")]
        [SerializeField] private string[] scientificTerms;
        [SerializeField] private string[] emotionalDescriptors;
        [SerializeField] private string[] environmentalTerms;
        [SerializeField] private string[] characterTraits;

        [Header("Debug Settings")]
        [SerializeField] private bool logPersonalityEvents = false;

        // Core storytelling components
        private List<DynamicStory> activeStories = new List<DynamicStory>();
        private List<StoryFragment> storyHistory = new List<StoryFragment>();
        private Dictionary<uint, CharacterProfile> creatureCharacters = new Dictionary<uint, CharacterProfile>();

        // Story generation systems
        private NarrativeGenerator narrativeGenerator;
        private CharacterDevelopmentSystem characterSystem;
        private PlotStructureAnalyzer plotAnalyzer;
        private ContextAnalyzer contextAnalyzer;

        // Tracking and analysis
        private float lastStoryUpdate;
        private StoryAnalytics analytics = new StoryAnalytics();
        private Dictionary<Laboratory.Core.Enums.NarrativeTheme, float> themePopularity = new Dictionary<Laboratory.Core.Enums.NarrativeTheme, float>();

        // Connected systems
        private PlayerAnalyticsTracker analyticsTracker;
        private DynamicEcosystemSimulator ecosystemSimulator;
        private GeneticEvolutionManager evolutionManager;
        private CreaturePersonalityManager personalityManager;

        // Events
        public System.Action<DynamicStory> OnStoryGenerated;
        public System.Action<DynamicStory> OnStoryCompleted;
        public System.Action<StoryFragment> OnNarrativeUpdate;
        public System.Action<CharacterProfile> OnCharacterDevelopment;

        // Singleton access
        private static AIStorytellerSystem instance;
        public static AIStorytellerSystem Instance => instance;

        public IReadOnlyList<DynamicStory> ActiveStories => activeStories.AsReadOnly();
        public IReadOnlyList<StoryFragment> StoryHistory => storyHistory.AsReadOnly();
        public StoryAnalytics Analytics => analytics;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeStorytellerSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            ConnectToGameSystems();
            InitializeThemePopularity();
        }

        private void Update()
        {
            if (!enableStoryGeneration) return;

            // Update active stories
            UpdateActiveStories();

            // Generate new stories periodically
            if (Time.time - lastStoryUpdate >= storyUpdateInterval)
            {
                CheckForNewStoryOpportunities();
                lastStoryUpdate = Time.time;
            }
        }

        private void InitializeStorytellerSystem()
        {
            Debug.Log("Initializing AI Storyteller System");

            // Initialize story generation systems
            narrativeGenerator = new NarrativeGenerator(this);
            characterSystem = new CharacterDevelopmentSystem(this);
            plotAnalyzer = new PlotStructureAnalyzer();
            contextAnalyzer = new ContextAnalyzer();

            // Initialize default story templates if none provided
            if (storyTemplates == null || storyTemplates.Length == 0)
            {
                CreateDefaultStoryTemplates();
            }

            Debug.Log("AI Storyteller System initialized");
        }

        private void ConnectToGameSystems()
        {
            // Connect to analytics system
            analyticsTracker = FindObjectOfType<PlayerAnalyticsTracker>();
            if (analyticsTracker != null)
            {
                analyticsTracker.OnPlayerArchetypeIdentified += HandlePlayerArchetypeChange;
                analyticsTracker.OnBehaviorInsightGenerated += HandleBehaviorInsight;
            }

            // Connect to ecosystem system
            ecosystemSimulator = DynamicEcosystemSimulator.Instance;
            if (ecosystemSimulator != null)
            {
                ecosystemSimulator.OnEnvironmentalEventStarted += HandleEnvironmentalEvent;
                ecosystemSimulator.OnBiomeHealthChanged += HandleBiomeHealthChange;
            }

            // Connect to personality system
            personalityManager = CreaturePersonalityManager.Instance;
            if (personalityManager != null)
            {
                personalityManager.OnSocialInteraction += HandleSocialInteraction;
                personalityManager.OnMoodChanged += HandleMoodChange;
            }

            // Connect to evolution system
            evolutionManager = GeneticEvolutionManager.Instance;
            if (evolutionManager != null)
            {
                evolutionManager.OnEliteCreatureEmerged += HandleEliteCreatureEmergence;
                evolutionManager.OnEvolutionaryMilestone += HandleEvolutionaryMilestone;
            }
        }

        /// <summary>
        /// Generates a new story based on current game state
        /// </summary>
        public DynamicStory GenerateStory(StoryGenerationContext context = null)
        {
            if (activeStories.Count >= maxActiveStories)
            {
                return null; // Too many active stories
            }

            context = context ?? CreateStoryContextFromGameState();

            // Select story template based on context
            var template = SelectAppropriateTemplate(context);
            if (template == null) return null;

            // Generate story content
            var story = new DynamicStory
            {
                id = System.Guid.NewGuid().ToString(),
                template = template,
                title = narrativeGenerator.GenerateTitle(template, context),
                mainCharacters = SelectMainCharacters(context),
                setting = DetermineStorySetting(context),
                plotStructure = GeneratePlotStructure(template),
                generationTime = Time.time,
                status = StoryStatus.Active,
                context = context
            };

            // Generate opening narrative
            story.currentNarrative = narrativeGenerator.GenerateOpeningNarrative(story);

            activeStories.Add(story);
            analytics.totalStoriesGenerated++;

            OnStoryGenerated?.Invoke(story);

            Debug.Log($"Generated story: '{story.title}' using template '{template.templateName}'");

            return story;
        }

        /// <summary>
        /// Updates story narrative based on new events
        /// </summary>
        public void UpdateStoryNarrative(string storyId, GameEvent gameEvent)
        {
            var story = activeStories.FirstOrDefault(s => s.id == storyId);
            if (story == null) return;

            // Analyze event impact on story
            var impact = AnalyzeEventImpact(story, gameEvent);

            if (impact.significanceLevel > 0.3f)
            {
                // Generate narrative update
                var narrativeUpdate = narrativeGenerator.GenerateEventNarrative(story, gameEvent, impact);

                var fragment = new StoryFragment
                {
                    id = System.Guid.NewGuid().ToString(),
                    storyId = storyId,
                    content = narrativeUpdate,
                    eventType = gameEvent.eventType,
                    timestamp = Time.time,
                    significanceLevel = impact.significanceLevel,
                    characterInvolvement = impact.affectedCharacters
                };

                story.fragments.Add(fragment);
                storyHistory.Add(fragment);

                // Update character development
                UpdateCharacterDevelopment(story, gameEvent, impact);

                // Check for story completion
                CheckStoryCompletion(story);

                OnNarrativeUpdate?.Invoke(fragment);

                Debug.Log($"Updated story '{story.title}' with event: {gameEvent.eventType}");
            }
        }

        /// <summary>
        /// Gets story recommendations for current game state
        /// </summary>
        public StoryRecommendation[] GetStoryRecommendations(int count = 3)
        {
            var context = CreateStoryContextFromGameState();
            var recommendations = new List<StoryRecommendation>();

            foreach (var template in storyTemplates)
            {
                if (recommendations.Count >= count) break;

                var suitability = CalculateTemplateSuitability(template, context);
                if (suitability > 0.6f)
                {
                    recommendations.Add(new StoryRecommendation
                    {
                        template = template,
                        suitabilityScore = suitability,
                        potentialCharacters = SelectMainCharacters(context),
                        estimatedNarrativeLength = EstimateNarrativeLength(template, context),
                        thematicRelevance = CalculateThematicRelevance(template, context)
                    });
                }
            }

            return recommendations.OrderByDescending(r => r.suitabilityScore).Take(count).ToArray();
        }

        /// <summary>
        /// Generates character profile for a creature
        /// </summary>
        public CharacterProfile GenerateCharacterProfile(uint creatureId)
        {
            if (creatureCharacters.ContainsKey(creatureId))
                return creatureCharacters[creatureId];

            var characterProfile = characterSystem.CreateCharacterProfile(creatureId);
            creatureCharacters[creatureId] = characterProfile;

            Debug.Log($"Generated character profile for creature {creatureId}: {characterProfile.characterName}");

            return characterProfile;
        }

        /// <summary>
        /// Gets comprehensive story analysis
        /// </summary>
        public StoryAnalysis AnalyzeCurrentStories()
        {
            var analysis = new StoryAnalysis
            {
                analysisTime = Time.time,
                activeStoryCount = activeStories.Count,
                totalNarrativeFragments = storyHistory.Count,
                dominantThemes = GetDominantThemes(),
                characterDevelopmentArcs = GetActiveCharacterArcs(),
                narrativeComplexity = CalculateNarrativeComplexity(),
                playerEngagementPrediction = PredictPlayerEngagement(),
                storyQualityMetrics = CalculateStoryQuality(),
                recommendations = GenerateStorytellingRecommendations()
            };

            return analysis;
        }

        private void UpdateActiveStories()
        {
            foreach (var story in activeStories.ToList())
            {
                // Update story progression
                UpdateStoryProgression(story);

                // Check for natural story events
                CheckForNaturalStoryEvents(story);

                // Update character arcs
                UpdateCharacterArcs(story);
            }
        }

        private void CheckForNewStoryOpportunities()
        {
            if (activeStories.Count >= maxActiveStories) return;

            var context = CreateStoryContextFromGameState();

            // Check for significant events that warrant new stories
            if (HasSignificantRecentEvents(context))
            {
                GenerateStory(context);
            }
        }

        private StoryGenerationContext CreateStoryContextFromGameState()
        {
            var context = new StoryGenerationContext
            {
                timestamp = Time.time,
                playerBehavior = analyticsTracker != null ? analyticsTracker.GeneratePlayerBehaviorAnalysis() : new Laboratory.Systems.Analytics.PlayerBehaviorAnalysis(),
                ecosystemState = ecosystemSimulator?.OverallHealth ?? EcosystemHealth.Good,
                activeCreatures = GetActiveCreatureData(),
                recentEvents = GetRecentGameEvents(),
                environmentalConditions = GetCurrentEnvironmentalConditions(),
                playerPreferences = GetPlayerNarrativePreferences()
            };

            return context;
        }

        private List<CreatureData> GetActiveCreatureData()
        {
            var creatures = new List<CreatureData>();

            // Get creatures from various systems
            if (personalityManager != null)
            {
                for (uint i = 1; i <= personalityManager.ActivePersonalityCount && i <= 10; i++)
                {
                    var characterProfile = GetOrCreateCharacterProfile(i);
                    creatures.Add(new CreatureData
                    {
                        creatureId = i,
                        characterProfile = characterProfile,
                        personalityTraits = characterProfile.personalityTraits,
                        currentMood = characterProfile.currentMood
                    });
                }
            }

            return creatures;
        }

        private CharacterProfile GetOrCreateCharacterProfile(uint creatureId)
        {
            if (!creatureCharacters.ContainsKey(creatureId))
            {
                creatureCharacters[creatureId] = characterSystem.CreateCharacterProfile(creatureId);
            }
            return creatureCharacters[creatureId];
        }

        private void HandleEnvironmentalEvent(EnvironmentalEvent envEvent)
        {
            var gameEvent = new GameEvent
            {
                eventType = "EnvironmentalEvent",
                description = $"Environmental event occurred: {envEvent.eventType}",
                significance = 0.7f,
                timestamp = Time.time,
                eventData = new Dictionary<ParamKey, object>
                {
                    [ParamKey.EventType] = envEvent.eventType,
                    [ParamKey.EventDescription] = envEvent.description
                }
            };

            // Update all active stories with this event
            foreach (var story in activeStories)
            {
                UpdateStoryNarrative(story.id, gameEvent);
            }

            // Consider generating new story if significant enough
            if (gameEvent.significance > 0.8f)
            {
                var context = CreateStoryContextFromGameState();
                context.triggeringEvent = gameEvent;
                GenerateStory(context);
            }
        }

        private void HandleEliteCreatureEmergence(CreatureGenome eliteCreature)
        {
            if (eliteCreature == null) return;

            var gameEvent = new GameEvent
            {
                eventType = "EliteEmergence",
                description = $"An exceptional creature has emerged with unprecedented genetic traits",
                significance = 0.9f,
                timestamp = Time.time,
                primaryCreatureId = eliteCreature.id,
                eventData = new Dictionary<ParamKey, object>
                {
                    [ParamKey.OffspringFitness] = eliteCreature.fitness,
                    [ParamKey.Generation] = eliteCreature.generation,
                    [ParamKey.Content] = eliteCreature.traits.Count
                }
            };

            // This is always significant enough for a new story
            var context = CreateStoryContextFromGameState();
            context.triggeringEvent = gameEvent;
            context.focusCreature = eliteCreature.id;
            GenerateStory(context);
        }

        private void HandleSocialInteraction(uint creatureA, uint creatureB, SocialInteractionType interactionType)
        {
            var gameEvent = new GameEvent
            {
                eventType = "SocialInteraction",
                description = $"Social interaction between creatures: {interactionType}",
                significance = CalculateInteractionSignificance(interactionType),
                timestamp = Time.time,
                primaryCreatureId = creatureA,
                secondaryCreatureId = creatureB,
                eventData = new Dictionary<ParamKey, object>
                {
                    [ParamKey.InteractionType] = interactionType
                }
            };

            // Update stories involving these creatures
            var relevantStories = activeStories.Where(s =>
                s.mainCharacters.Any(c => c.creatureId == creatureA || c.creatureId == creatureB));

            foreach (var story in relevantStories)
            {
                UpdateStoryNarrative(story.id, gameEvent);
            }
        }

        private float CalculateInteractionSignificance(SocialInteractionType interactionType)
        {
            return interactionType switch
            {
                SocialInteractionType.Conflict => 0.8f,
                SocialInteractionType.Mating => 0.9f,
                SocialInteractionType.Cooperation => 0.6f,
                SocialInteractionType.Play => 0.4f,
                SocialInteractionType.Territorial => 0.7f,
                SocialInteractionType.Protection => 0.8f,
                SocialInteractionType.Competition => 0.7f,
                SocialInteractionType.Feeding => 0.5f,
                SocialInteractionType.Grooming => 0.3f,
                _ => 0.2f
            };
        }

        private void HandleMoodChange(uint creatureId, Laboratory.AI.Personality.MoodState newMood)
        {
            // Generate mood-related story events for significant mood changes
            if (newMood.stress > 0.8f || newMood.happiness < 0.2f)
            {
                var gameEvent = new GameEvent
                {
                    eventType = "MoodChange",
                    description = $"Creature {creatureId} experienced significant mood change: stress={newMood.stress:F2}, happiness={newMood.happiness:F2}",
                    significance = 0.5f,
                    timestamp = Time.time,
                    primaryCreatureId = creatureId,
                    eventData = new Dictionary<ParamKey, object>
                    {
                        [ParamKey.EmotionalState] = newMood,
                        [ParamKey.Content] = newMood.stress,
                        [ParamKey.Intensity] = newMood.happiness
                    }
                };

                // Update stories involving this creature
                var relevantStories = activeStories.Where(s =>
                    s.mainCharacters.Any(c => c.creatureId == creatureId));

                foreach (var story in relevantStories)
                {
                    UpdateStoryNarrative(story.id, gameEvent);
                }
            }
        }

        private void CreateDefaultStoryTemplates()
        {
            storyTemplates = new StoryTemplate[]
            {
                new StoryTemplate
                {
                    templateName = "Scientific Discovery",
                    storyType = StoryType.Discovery,
                    requiredElements = new[] { "Laboratory", "Creature", "Genetics" },
                    plotStructure = PlotStructureType.Scientific,
                    estimatedLength = StoryLength.Medium,
                    themeWeight = 0.8f
                },
                new StoryTemplate
                {
                    templateName = "Evolutionary Breakthrough",
                    storyType = StoryType.Evolution,
                    requiredElements = new[] { "Genetics", "Adaptation", "Environment" },
                    plotStructure = PlotStructureType.Progressive,
                    estimatedLength = StoryLength.Long,
                    themeWeight = 0.9f
                },
                new StoryTemplate
                {
                    templateName = "Creature Relationships",
                    storyType = StoryType.Social,
                    requiredElements = new[] { "Creatures", "Interaction", "Personality" },
                    plotStructure = PlotStructureType.CharacterDriven,
                    estimatedLength = StoryLength.Short,
                    themeWeight = 0.6f
                },
                new StoryTemplate
                {
                    templateName = "Environmental Challenge",
                    storyType = StoryType.Survival,
                    requiredElements = new[] { "Environment", "Challenge", "Adaptation" },
                    plotStructure = PlotStructureType.ConflictResolution,
                    estimatedLength = StoryLength.Medium,
                    themeWeight = 0.7f
                }
            };
        }

        private Dictionary<EnvironmentalCondition, float> GetCurrentEnvironmentalConditions()
        {
            var conditions = new Dictionary<EnvironmentalCondition, float>();

            if (ecosystemSimulator != null)
            {
                conditions[EnvironmentalCondition.Temperature] = Random.Range(0f, 1f);
                conditions[EnvironmentalCondition.Humidity] = Random.Range(0f, 1f);
                conditions[EnvironmentalCondition.ResourceAvailability] = Random.Range(0f, 1f);
                conditions[EnvironmentalCondition.PopulationDensity] = Random.Range(0f, 1f);
            }
            else
            {
                // Default values when ecosystem simulator is not available
                conditions[EnvironmentalCondition.Temperature] = 0.5f;
                conditions[EnvironmentalCondition.Humidity] = 0.5f;
                conditions[EnvironmentalCondition.ResourceAvailability] = 0.5f;
                conditions[EnvironmentalCondition.PopulationDensity] = 0.3f;
            }

            return conditions;
        }

        private Dictionary<NarrativePreference, float> GetPlayerNarrativePreferences()
        {
            var preferences = new Dictionary<NarrativePreference, float>();

            if (analyticsTracker != null)
            {
                var behaviorAnalysis = analyticsTracker.GetBehaviorAnalysis();
                preferences[NarrativePreference.DramaticIntensity] = behaviorAnalysis.competitiveFocus;
                preferences[NarrativePreference.HumorLevel] = behaviorAnalysis.socialFocus * 0.7f;
                preferences[NarrativePreference.TechnicalDetail] = behaviorAnalysis.explorationFocus;
                preferences[NarrativePreference.CharacterFocus] = behaviorAnalysis.socialFocus;
            }
            else
            {
                // Default preferences
                preferences[NarrativePreference.DramaticIntensity] = dramaticIntensity;
                preferences[NarrativePreference.HumorLevel] = humorLevel;
                preferences[NarrativePreference.TechnicalDetail] = technicalDetail;
                preferences[NarrativePreference.CharacterFocus] = 0.6f;
            }

            return preferences;
        }

        private List<GameEvent> GetRecentGameEvents()
        {
            var events = new List<GameEvent>();

            // Get recent events from the last 60 seconds
            float cutoffTime = Time.time - 60f;

            foreach (var fragment in storyHistory)
            {
                if (fragment.timestamp > cutoffTime)
                {
                    events.Add(new GameEvent
                    {
                        eventType = fragment.eventType,
                        description = fragment.content,
                        significance = fragment.significanceLevel,
                        timestamp = fragment.timestamp
                    });
                }
            }

            return events;
        }

        private StoryTemplate SelectAppropriateTemplate(StoryGenerationContext context)
        {
            if (storyTemplates == null || storyTemplates.Length == 0)
                return null;

            float bestScore = 0f;
            StoryTemplate bestTemplate = null;

            foreach (var template in storyTemplates)
            {
                float score = CalculateTemplateSuitability(template, context);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTemplate = template;
                }
            }

            return bestTemplate;
        }

        private float CalculateTemplateSuitability(StoryTemplate template, StoryGenerationContext context)
        {
            float suitability = 0.5f; // Base suitability

            // Factor in ecosystem state
            switch (template.storyType)
            {
                case StoryType.Survival:
                    if (context.ecosystemState == EcosystemHealth.Poor)
                        suitability += 0.3f;
                    break;
                case StoryType.Evolution:
                    if (context.activeCreatures.Count > 5)
                        suitability += 0.2f;
                    break;
                case StoryType.Social:
                    if (context.activeCreatures.Count > 2)
                        suitability += 0.25f;
                    break;
            }

            // Factor in recent events
            if (context.triggeringEvent != null && context.triggeringEvent.significance > 0.7f)
                suitability += 0.2f;

            return Mathf.Clamp01(suitability);
        }

        private List<CharacterProfile> SelectMainCharacters(StoryGenerationContext context)
        {
            var characters = new List<CharacterProfile>();

            // Select up to 3 main characters from active creatures
            int maxCharacters = Mathf.Min(3, context.activeCreatures.Count);

            for (int i = 0; i < maxCharacters; i++)
            {
                var creatureData = context.activeCreatures[i];
                characters.Add(creatureData.characterProfile ?? GetOrCreateCharacterProfile(creatureData.creatureId));
            }

            return characters;
        }

        private StorySetting DetermineStorySetting(StoryGenerationContext context)
        {
            // Create a basic story setting based on context
            return new StorySetting();
        }

        private PlotStructure GeneratePlotStructure(StoryTemplate template)
        {
            // Generate plot structure based on template
            return new PlotStructure();
        }

        private EventImpact AnalyzeEventImpact(DynamicStory story, GameEvent gameEvent)
        {
            var impact = new EventImpact
            {
                significanceLevel = gameEvent.significance
            };

            // Find affected characters
            if (gameEvent.primaryCreatureId != 0)
            {
                impact.affectedCharacters.Add(gameEvent.primaryCreatureId);
            }
            if (gameEvent.secondaryCreatureId != 0)
            {
                impact.affectedCharacters.Add(gameEvent.secondaryCreatureId);
            }

            return impact;
        }

        private void UpdateCharacterDevelopment(DynamicStory story, GameEvent gameEvent, EventImpact impact)
        {
            foreach (var characterId in impact.affectedCharacters)
            {
                var character = story.mainCharacters.FirstOrDefault(c => c.creatureId == characterId);
                if (character != null)
                {
                    character.developmentLevel += impact.significanceLevel * 0.1f;
                    character.characterArc.Add($"Experienced {gameEvent.eventType} at {gameEvent.timestamp:F1}s");

                    OnCharacterDevelopment?.Invoke(character);
                }
            }
        }

        private void CheckStoryCompletion(DynamicStory story)
        {
            // Check if story has reached natural completion
            if (story.fragments.Count >= 10 ||
                (Time.time - story.generationTime) > 300f || // 5 minutes
                story.plotStructure == null)
            {
                story.status = StoryStatus.Completed;
                story.completionTime = Time.time;
                activeStories.Remove(story);
                analytics.totalStoriesCompleted++;

                OnStoryCompleted?.Invoke(story);
            }
        }

        private void UpdateStoryProgression(DynamicStory story)
        {
            // Update story progression logic
            float progressTime = Time.time - story.generationTime;

            // Stories naturally progress over time
            if (progressTime > 30f && story.fragments.Count == 0)
            {
                // Generate a progression event if story has been stagnant
                var progressEvent = new GameEvent
                {
                    eventType = "StoryProgression",
                    description = "The story naturally develops",
                    significance = 0.4f,
                    timestamp = Time.time
                };

                UpdateStoryNarrative(story.id, progressEvent);
            }
        }

        private void CheckForNaturalStoryEvents(DynamicStory story)
        {
            // Check for natural story events based on characters and setting
            foreach (var character in story.mainCharacters)
            {
                if (Random.Range(0f, 1f) < 0.05f) // 5% chance per update
                {
                    var naturalEvent = new GameEvent
                    {
                        eventType = "CharacterMoment",
                        description = $"Character {character.characterName} has a significant moment",
                        significance = 0.3f,
                        timestamp = Time.time,
                        primaryCreatureId = character.creatureId
                    };

                    UpdateStoryNarrative(story.id, naturalEvent);
                }
            }
        }

        private void UpdateCharacterArcs(DynamicStory story)
        {
            // Update character development arcs
            foreach (var character in story.mainCharacters)
            {
                if (personalityManager != null)
                {
                    var personalityProfile = personalityManager.AnalyzeCreaturePersonality(character.creatureId);
                    if (personalityProfile != null)
                    {
                        // Update character based on personality analysis
                        character.currentMood = personalityProfile.currentMoodState.ToString();
                    }
                }
            }
        }

        private bool HasSignificantRecentEvents(StoryGenerationContext context)
        {
            return context.recentEvents.Any(e => e.significance > 0.6f) ||
                   context.triggeringEvent != null;
        }

        private void HandlePlayerArchetypeChange(PlayerArchetype newArchetype)
        {
            // Adjust storytelling style based on player archetype
            if (logPersonalityEvents)
            {
                Debug.Log($"Player archetype changed to: {newArchetype}. Adjusting narrative style.");
            }
        }

        private void HandleBehaviorInsight(BehaviorInsight insight)
        {
            // Use behavior insights to inform story generation
            if (logPersonalityEvents)
            {
                Debug.Log($"Behavior insight: {insight}");
            }
        }

        private void HandleBiomeHealthChange(BiomeType biome, float newHealth)
        {
            var gameEvent = new GameEvent
            {
                eventType = "BiomeHealthChange",
                description = $"Biome {biome} health changed to {newHealth}",
                significance = 0.6f,
                timestamp = Time.time,
                eventData = new Dictionary<ParamKey, object>
                {
                    [ParamKey.Biome] = biome,
                    [ParamKey.Content] = newHealth
                }
            };

            // Update relevant stories
            foreach (var story in activeStories)
            {
                UpdateStoryNarrative(story.id, gameEvent);
            }
        }

        private void HandleEvolutionaryMilestone(string milestone)
        {
            var gameEvent = new GameEvent
            {
                eventType = "EvolutionaryMilestone",
                description = $"Evolutionary milestone achieved: {milestone}",
                significance = 0.8f,
                timestamp = Time.time,
                eventData = new Dictionary<ParamKey, object>
                {
                    [ParamKey.Content] = milestone
                }
            };

            // This is significant enough for a new story
            var context = CreateStoryContextFromGameState();
            context.triggeringEvent = gameEvent;
            GenerateStory(context);
        }

        private void InitializeThemePopularity()
        {
            themePopularity[Laboratory.Core.Enums.NarrativeTheme.Discovery] = 0.5f;
            themePopularity[Laboratory.Core.Enums.NarrativeTheme.Evolution] = 0.6f;
            themePopularity[Laboratory.Core.Enums.NarrativeTheme.Social] = 0.4f;
            themePopularity[Laboratory.Core.Enums.NarrativeTheme.Survival] = 0.5f;
            themePopularity[Laboratory.Core.Enums.NarrativeTheme.Mystery] = 0.3f;
            themePopularity[Laboratory.Core.Enums.NarrativeTheme.Adventure] = 0.4f;
        }

        private Dictionary<Laboratory.Core.Enums.NarrativeTheme, float> GetDominantThemes()
        {
            return themePopularity.OrderByDescending(kvp => kvp.Value)
                                 .Take(3)
                                 .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private List<CharacterArc> GetActiveCharacterArcs()
        {
            var arcs = new List<CharacterArc>();

            foreach (var story in activeStories)
            {
                foreach (var character in story.mainCharacters)
                {
                    // Create character arc representation
                    arcs.Add(new CharacterArc());
                }
            }

            return arcs;
        }

        private float CalculateNarrativeComplexity()
        {
            if (activeStories.Count == 0) return 0f;

            float totalComplexity = 0f;

            foreach (var story in activeStories)
            {
                float storyComplexity = story.fragments.Count * 0.1f +
                                      story.mainCharacters.Count * 0.2f;
                totalComplexity += storyComplexity;
            }

            return totalComplexity / activeStories.Count;
        }

        private float PredictPlayerEngagement()
        {
            if (analyticsTracker != null)
            {
                var behavior = analyticsTracker.GetBehaviorAnalysis();
                return (behavior.explorationFocus + behavior.socialFocus + behavior.creativeFocus) / 3f;
            }
            return 0.5f; // Default moderate engagement
        }

        private StoryQualityMetrics CalculateStoryQuality()
        {
            return new StoryQualityMetrics();
        }

        private List<string> GenerateStorytellingRecommendations()
        {
            var recommendations = new List<string>();

            if (activeStories.Count < maxActiveStories / 2)
            {
                recommendations.Add("Consider generating more concurrent stories");
            }

            if (analytics.averagePlayerEngagement < 0.4f)
            {
                recommendations.Add("Increase dramatic intensity to boost engagement");
            }

            var dominantThemes = GetDominantThemes();
            if (dominantThemes.Values.Max() > 0.8f)
            {
                recommendations.Add("Diversify story themes for variety");
            }

            return recommendations;
        }

        private int EstimateNarrativeLength(StoryTemplate template, StoryGenerationContext context)
        {
            return template.estimatedLength switch
            {
                StoryLength.Short => Random.Range(3, 6),
                StoryLength.Medium => Random.Range(6, 12),
                StoryLength.Long => Random.Range(12, 20),
                StoryLength.Epic => Random.Range(20, 35),
                _ => 8
            };
        }

        private float CalculateThematicRelevance(StoryTemplate template, StoryGenerationContext context)
        {
            float relevance = 0.5f;

            // Check if template themes match current context
            if (template.storyType == StoryType.Social && context.activeCreatures.Count > 2)
                relevance += 0.2f;

            if (template.storyType == StoryType.Survival && context.ecosystemState == EcosystemHealth.Poor)
                relevance += 0.3f;

            if (template.storyType == StoryType.Evolution && context.recentEvents.Any(e => e.eventType.Contains("Evolution")))
                relevance += 0.25f;

            return Mathf.Clamp01(relevance);
        }

        private void SaveStoryHistory()
        {
            // Save story history to persistent storage
            Debug.Log($"Saving story history: {storyHistory.Count} fragments, {analytics.totalStoriesGenerated} total stories");
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                // Save story history before destroying
                SaveStoryHistory();
                instance = null;
            }
        }

        // Editor menu items
#if UNITY_EDITOR
        [UnityEditor.MenuItem("🧪 Laboratory/Storytelling/Generate New Story", false, 600)]
        private static void MenuGenerateStory()
        {
            if (Application.isPlaying && Instance != null)
            {
                var story = Instance.GenerateStory();
                if (story != null)
                {
                    Debug.Log($"Generated story: '{story.title}' - {story.currentNarrative}");
                }
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Storytelling/Show Story Analysis", false, 601)]
        private static void MenuShowStoryAnalysis()
        {
            if (Application.isPlaying && Instance != null)
            {
                var analysis = Instance.AnalyzeCurrentStories();
                Debug.Log($"Story Analysis:\n" +
                         $"Active Stories: {analysis.activeStoryCount}\n" +
                         $"Narrative Fragments: {analysis.totalNarrativeFragments}\n" +
                         $"Narrative Complexity: {analysis.narrativeComplexity:F2}\n" +
                         $"Character Arcs: {analysis.characterDevelopmentArcs.Count}");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Storytelling/Get Story Recommendations", false, 602)]
        private static void MenuGetStoryRecommendations()
        {
            if (Application.isPlaying && Instance != null)
            {
                var recommendations = Instance.GetStoryRecommendations();
                Debug.Log($"Story Recommendations ({recommendations.Length}):");

                foreach (var rec in recommendations)
                {
                    Debug.Log($"- {rec.template.templateName} (Suitability: {rec.suitabilityScore:F2})");
                }
            }
        }
#endif
    }

    // Supporting data structures for AI storytelling system
    [System.Serializable]
    public class DynamicStory
    {
        public string id;
        public StoryTemplate template;
        public string title;
        public string currentNarrative;
        public List<CharacterProfile> mainCharacters = new List<CharacterProfile>();
        public StorySetting setting;
        public PlotStructure plotStructure;
        public List<StoryFragment> fragments = new List<StoryFragment>();
        public StoryStatus status;
        public float generationTime;
        public float completionTime;
        public StoryGenerationContext context;
    }

    [System.Serializable]
    public class StoryFragment
    {
        public string id;
        public string storyId;
        public string content;
        public string eventType;
        public float timestamp;
        public float significanceLevel;
        public List<uint> characterInvolvement = new List<uint>();
    }

    [System.Serializable]
    public class CharacterProfile
    {
        public uint creatureId;
        public string characterName;
        public CharacterArchetype archetype;
        public PersonalityTrait personalityTraits;
        public string currentMood;
        public List<string> characterArc = new List<string>();
        public Dictionary<uint, string> relationships = new Dictionary<uint, string>();
        public float developmentLevel;
    }

    [System.Serializable]
    public class StoryGenerationContext
    {
        public float timestamp;
        public Laboratory.Systems.Analytics.PlayerBehaviorAnalysis playerBehavior;
        public EcosystemHealth ecosystemState;
        public List<CreatureData> activeCreatures = new List<CreatureData>();
        public List<GameEvent> recentEvents = new List<GameEvent>();
        public Dictionary<EnvironmentalCondition, float> environmentalConditions = new Dictionary<EnvironmentalCondition, float>();
        public Dictionary<NarrativePreference, float> playerPreferences = new Dictionary<NarrativePreference, float>();
        public GameEvent triggeringEvent;
        public uint focusCreature;
    }

    [System.Serializable]
    public class GameEvent
    {
        public string eventType;
        public string description;
        public float significance;
        public float timestamp;
        public uint primaryCreatureId;
        public uint secondaryCreatureId;
        public Dictionary<ParamKey, object> eventData = new Dictionary<ParamKey, object>();
    }

    [System.Serializable]
    public class StoryAnalytics
    {
        public int totalStoriesGenerated;
        public int totalStoriesCompleted;
        public int totalNarrativeFragments;
        public Dictionary<StoryType, int> storiesByType = new Dictionary<StoryType, int>();
        public float averageStoryLength;
        public float averagePlayerEngagement;
    }

    // Enums and supporting types for storytelling
    public enum NarrativeStyle { Scientific, Poetic, Dramatic, Humorous, Technical }
    public enum StoryType { Discovery, Evolution, Social, Survival, Mystery, Adventure }
    public enum StoryStatus { Active, Completed, Suspended, Archived }
    public enum StoryLength { Short, Medium, Long, Epic }
    public enum PlotStructureType { Scientific, Progressive, CharacterDriven, ConflictResolution, Mystery }

    [System.Serializable]
    public class StoryTemplate
    {
        public string templateName;
        public StoryType storyType;
        public string[] requiredElements;
        public PlotStructureType plotStructure;
        public StoryLength estimatedLength;
        public float themeWeight;
        public string[] narrativeHooks;
    }

    [System.Serializable]
    public class CharacterArchetype
    {
        public string archetypeName;
        public string[] traits;
        public string[] motivations;
        public string[] conflictTypes;
    }

    [System.Serializable]
    public class PlotDevice
    {
        public string deviceName;
        public string description;
        public StoryType[] applicableStoryTypes;
    }

    [System.Serializable]
    public class NarrativeTheme
    {
        public string themeName;
        public float currentPopularity;
        public string[] keywords;
    }

    [System.Serializable]
    public class CreatureData
    {
        public uint creatureId;
        public CharacterProfile characterProfile;
        public PersonalityTrait personalityTraits;
        public string currentMood;
    }

    [System.Serializable]
    public class StoryRecommendation
    {
        public StoryTemplate template;
        public float suitabilityScore;
        public List<CharacterProfile> potentialCharacters;
        public int estimatedNarrativeLength;
        public float thematicRelevance;
    }

    [System.Serializable]
    public class StoryAnalysis
    {
        public float analysisTime;
        public int activeStoryCount;
        public int totalNarrativeFragments;
        public Dictionary<Laboratory.Core.Enums.NarrativeTheme, float> dominantThemes;
        public List<CharacterArc> characterDevelopmentArcs;
        public float narrativeComplexity;
        public float playerEngagementPrediction;
        public StoryQualityMetrics storyQualityMetrics;
        public List<string> recommendations;
    }

    // Supporting classes (simplified implementations)
    public class NarrativeGenerator
    {
        private AIStorytellerSystem system;

        public NarrativeGenerator(AIStorytellerSystem system)
        {
            this.system = system;
        }

        public string GenerateTitle(StoryTemplate template, StoryGenerationContext context)
        {
            // Advanced title generation based on template and context
            return $"The {template.storyType} of Generation {Random.Range(1, 20)}";
        }

        public string GenerateOpeningNarrative(DynamicStory story)
        {
            // Generate opening narrative based on story elements
            return $"In the laboratory's depths, a remarkable {story.template.storyType.ToString().ToLower()} began to unfold...";
        }

        public string GenerateEventNarrative(DynamicStory story, GameEvent gameEvent, EventImpact impact)
        {
            // Generate narrative based on the event and its impact on the story
            return $"As {gameEvent.description.ToLower()}, the story took an unexpected turn...";
        }
    }

    public class CharacterDevelopmentSystem
    {
        private AIStorytellerSystem system;

        public CharacterDevelopmentSystem(AIStorytellerSystem system)
        {
            this.system = system;
        }

        public CharacterProfile CreateCharacterProfile(uint creatureId)
        {
            return new CharacterProfile
            {
                creatureId = creatureId,
                characterName = GenerateCharacterName(),
                archetype = SelectRandomArchetype(),
                personalityTraits = GeneratePersonalityTraits(),
                currentMood = "Curious",
                developmentLevel = 0f
            };
        }

        private string GenerateCharacterName()
        {
            var names = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta" };
            return names[Random.Range(0, names.Length)] + "-" + Random.Range(100, 999);
        }

        private CharacterArchetype SelectRandomArchetype()
        {
            return new CharacterArchetype
            {
                archetypeName = "Explorer",
                traits = new[] { "Curious", "Brave", "Intelligent" }
            };
        }

        private PersonalityTrait GeneratePersonalityTraits()
        {
            return new PersonalityTrait
            {
                openness = Random.Range(0f, 1f),
                extroversion = Random.Range(0f, 1f),
                agreeableness = Random.Range(0f, 1f),
                conscientiousness = Random.Range(0f, 1f),
                neuroticism = Random.Range(0f, 1f),
                aggressiveness = Random.Range(0f, 1f)
            };
        }
    }

    public class PlotStructureAnalyzer
    {
        // Plot structure analysis implementation
    }

    public class ContextAnalyzer
    {
        // Context analysis implementation
    }

    // Additional supporting classes and data structures...
    public class StorySetting { }
    public class PlotStructure { }
    public class EventImpact
    {
        public float significanceLevel;
        public List<uint> affectedCharacters = new List<uint>();
    }
    public class CharacterArc { }
    public class StoryQualityMetrics { }
}