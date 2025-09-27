using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Events;
using Laboratory.Chimera.Genetics.Advanced;
using Laboratory.AI.Personality;

namespace Laboratory.Systems.Quests
{
    /// <summary>
    /// Procedural quest generator that creates dynamic objectives based on game state,
    /// creature populations, environmental conditions, and player behavior patterns.
    /// </summary>
    public class ProceduralQuestGenerator : MonoBehaviour
    {
        [Header("Quest Generation Settings")]
        [SerializeField] private bool enableQuestGeneration = true;
        [SerializeField] private int maxActiveQuests = 5;
        [SerializeField] private int maxCompletedQuests = 20;
        [SerializeField] private float questGenerationInterval = 30f;
        [SerializeField] private float questExpirationTime = 300f; // 5 minutes

        [Header("Quest Difficulty")]
        [SerializeField] private AnimationCurve difficultyProgression;
        [SerializeField, Range(0f, 1f)] private float baseDifficulty = 0.3f;
        [SerializeField, Range(0f, 2f)] private float difficultyVariance = 0.3f;

        [Header("Quest Types Weights")]
        [SerializeField, Range(0f, 1f)] private float explorationQuestWeight = 0.3f;
        [SerializeField, Range(0f, 1f)] private float collectionQuestWeight = 0.25f;
        [SerializeField, Range(0f, 1f)] private float survivalQuestWeight = 0.2f;
        [SerializeField, Range(0f, 1f)] private float discoveryQuestWeight = 0.15f;
        [SerializeField, Range(0f, 1f)] private float socialQuestWeight = 0.1f;

        [Header("Reward Configuration")]
        [SerializeField] private QuestRewardConfig defaultRewardConfig;
        [SerializeField] private QuestRewardConfig[] specialRewardConfigs;

        [Header("Environmental Context")]
        [SerializeField] private Transform[] questLocationPoints;
        [SerializeField] private string[] availableBiomes;
        [SerializeField] private CreatureSpeciesConfig[] targetSpecies;

        // Core systems
        private List<ProceduralQuest> activeQuests = new List<ProceduralQuest>();
        private List<ProceduralQuest> completedQuests = new List<ProceduralQuest>();
        private Queue<ProceduralQuest> questQueue = new Queue<ProceduralQuest>();

        // Generation tracking
        private float lastQuestGenerationTime;
        private uint nextQuestId = 1;
        private QuestAnalytics analytics = new QuestAnalytics();

        // Context data
        private GameStateContext currentGameState = new GameStateContext();
        private Dictionary<QuestType, float> typePopularity = new Dictionary<QuestType, float>();

        // Events
        public System.Action<ProceduralQuest> OnQuestGenerated;
        public System.Action<ProceduralQuest> OnQuestCompleted;
        public System.Action<ProceduralQuest> OnQuestExpired;
        public System.Action<ProceduralQuest> OnQuestUpdated;

        // Singleton access
        private static ProceduralQuestGenerator instance;
        public static ProceduralQuestGenerator Instance => instance;

        public IReadOnlyList<ProceduralQuest> ActiveQuests => activeQuests.AsReadOnly();
        public IReadOnlyList<ProceduralQuest> CompletedQuests => completedQuests.AsReadOnly();
        public QuestAnalytics Analytics => analytics;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeQuestSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeQuestSystem()
        {
            DebugManager.LogInfo("Initializing Procedural Quest Generator");

            // Initialize type popularity tracking
            foreach (QuestType type in System.Enum.GetValues(typeof(QuestType)))
            {
                typePopularity[type] = 0.5f; // Start with neutral popularity
            }

            // Initialize difficulty progression if not set
            if (difficultyProgression == null || difficultyProgression.keys.Length == 0)
            {
                CreateDefaultDifficultyProgression();
            }

            // Subscribe to relevant game events
            SubscribeToGameEvents();

            DebugManager.LogInfo("Procedural Quest Generator initialized");
        }

        private void Update()
        {
            if (!enableQuestGeneration) return;

            // Generate new quests periodically
            if (Time.time - lastQuestGenerationTime >= questGenerationInterval)
            {
                UpdateGameStateContext();
                GenerateNewQuests();
                lastQuestGenerationTime = Time.time;
            }

            // Update active quests
            UpdateActiveQuests();

            // Process quest queue
            ProcessQuestQueue();
        }

        /// <summary>
        /// Generates a new procedural quest based on current game state
        /// </summary>
        public ProceduralQuest GenerateQuest(QuestGenerationContext context = null)
        {
            if (activeQuests.Count >= maxActiveQuests)
            {
                return null; // Too many active quests
            }

            // Use provided context or create from current game state
            context = context ?? CreateContextFromGameState();

            // Select quest type based on weights and popularity
            QuestType questType = SelectQuestType(context);

            // Generate quest based on type
            ProceduralQuest newQuest = GenerateQuestOfType(questType, context);

            if (newQuest != null)
            {
                newQuest.id = nextQuestId++;
                newQuest.generatedTime = Time.time;
                newQuest.expirationTime = Time.time + questExpirationTime;
                newQuest.difficulty = CalculateQuestDifficulty(context);

                // Add to active quests
                activeQuests.Add(newQuest);
                analytics.totalQuestsGenerated++;

                OnQuestGenerated?.Invoke(newQuest);

                DebugManager.LogInfo($"Generated {questType} quest: {newQuest.title}");
            }

            return newQuest;
        }

        /// <summary>
        /// Updates quest progress based on game events
        /// </summary>
        public void UpdateQuestProgress(string eventType, Dictionary<string, object> eventData)
        {
            foreach (var quest in activeQuests.Where(q => q.status == QuestStatus.Active))
            {
                bool progressMade = false;

                foreach (var objective in quest.objectives.Where(o => !o.isCompleted))
                {
                    if (CheckObjectiveProgress(objective, eventType, eventData))
                    {
                        progressMade = true;
                        objective.currentProgress++;

                        if (objective.currentProgress >= objective.targetProgress)
                        {
                            objective.isCompleted = true;
                            DebugManager.LogInfo($"Objective completed: {objective.description}");
                        }
                    }
                }

                if (progressMade)
                {
                    UpdateQuestStatus(quest);
                    OnQuestUpdated?.Invoke(quest);
                }
            }
        }

        /// <summary>
        /// Manually completes a quest
        /// </summary>
        public void CompleteQuest(uint questId)
        {
            var quest = activeQuests.FirstOrDefault(q => q.id == questId);
            if (quest != null)
            {
                quest.status = QuestStatus.Completed;
                quest.completedTime = Time.time;

                // Move to completed quests
                activeQuests.Remove(quest);
                completedQuests.Add(quest);

                // Manage completed quest limit
                if (completedQuests.Count > maxCompletedQuests)
                {
                    completedQuests.RemoveAt(0);
                }

                analytics.totalQuestsCompleted++;
                UpdateTypePopularity(quest.type, true);

                OnQuestCompleted?.Invoke(quest);
                DebugManager.LogInfo($"Quest completed: {quest.title}");
            }
        }

        /// <summary>
        /// Gets quest recommendations based on player behavior
        /// </summary>
        public ProceduralQuest[] GetQuestRecommendations(int count = 3)
        {
            var context = CreateContextFromGameState();
            var recommendations = new List<ProceduralQuest>();

            for (int i = 0; i < count && recommendations.Count < count; i++)
            {
                var quest = GenerateQuestOfType(GetRecommendedQuestType(context), context);
                if (quest != null)
                {
                    quest.id = uint.MaxValue - (uint)i; // Temporary ID for recommendations
                    quest.isRecommendation = true;
                    recommendations.Add(quest);
                }
            }

            return recommendations.ToArray();
        }

        private void GenerateNewQuests()
        {
            if (activeQuests.Count < maxActiveQuests)
            {
                int questsToGenerate = Mathf.Min(2, maxActiveQuests - activeQuests.Count);

                for (int i = 0; i < questsToGenerate; i++)
                {
                    var quest = GenerateQuest();
                    if (quest == null) break; // Failed to generate
                }
            }
        }

        private void UpdateActiveQuests()
        {
            var expiredQuests = activeQuests.Where(q => Time.time >= q.expirationTime).ToList();

            foreach (var expiredQuest in expiredQuests)
            {
                expiredQuest.status = QuestStatus.Expired;
                activeQuests.Remove(expiredQuest);

                analytics.totalQuestsExpired++;
                UpdateTypePopularity(expiredQuest.type, false);

                OnQuestExpired?.Invoke(expiredQuest);
                DebugManager.LogInfo($"Quest expired: {expiredQuest.title}");
            }
        }

        private void UpdateQuestStatus(ProceduralQuest quest)
        {
            if (quest.objectives.All(o => o.isCompleted))
            {
                CompleteQuest(quest.id);
            }
        }

        private void ProcessQuestQueue()
        {
            if (questQueue.Count > 0 && activeQuests.Count < maxActiveQuests)
            {
                var quest = questQueue.Dequeue();
                activeQuests.Add(quest);
                OnQuestGenerated?.Invoke(quest);
            }
        }

        private QuestType SelectQuestType(QuestGenerationContext context)
        {
            // Calculate weighted probabilities
            var weights = new Dictionary<QuestType, float>
            {
                [QuestType.Exploration] = explorationQuestWeight * typePopularity[QuestType.Exploration],
                [QuestType.Collection] = collectionQuestWeight * typePopularity[QuestType.Collection],
                [QuestType.Survival] = survivalQuestWeight * typePopularity[QuestType.Survival],
                [QuestType.Discovery] = discoveryQuestWeight * typePopularity[QuestType.Discovery],
                [QuestType.Social] = socialQuestWeight * typePopularity[QuestType.Social]
            };

            // Adjust based on context
            AdjustWeightsForContext(weights, context);

            // Select randomly based on weights
            return SelectWeightedRandom(weights);
        }

        private void AdjustWeightsForContext(Dictionary<QuestType, float> weights, QuestGenerationContext context)
        {
            // Boost exploration if many unexplored areas
            if (context.unexploredAreaRatio > 0.5f)
            {
                weights[QuestType.Exploration] *= 1.5f;
            }

            // Boost collection if resources are abundant
            if (context.resourceAbundance > 0.6f)
            {
                weights[QuestType.Collection] *= 1.3f;
            }

            // Boost survival if population is struggling
            if (context.averageCreatureFitness < 0.4f)
            {
                weights[QuestType.Survival] *= 1.4f;
            }

            // Boost social if many creatures are available
            if (context.totalCreaturePopulation > 20)
            {
                weights[QuestType.Social] *= 1.2f;
            }
        }

        private T SelectWeightedRandom<T>(Dictionary<T, float> weights)
        {
            float totalWeight = weights.Values.Sum();
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var kvp in weights)
            {
                currentWeight += kvp.Value;
                if (randomValue <= currentWeight)
                {
                    return kvp.Key;
                }
            }

            return weights.Keys.First(); // Fallback
        }

        private ProceduralQuest GenerateQuestOfType(QuestType type, QuestGenerationContext context)
        {
            switch (type)
            {
                case QuestType.Exploration:
                    return GenerateExplorationQuest(context);
                case QuestType.Collection:
                    return GenerateCollectionQuest(context);
                case QuestType.Survival:
                    return GenerateSurvivalQuest(context);
                case QuestType.Discovery:
                    return GenerateDiscoveryQuest(context);
                case QuestType.Social:
                    return GenerateSocialQuest(context);
                default:
                    return GenerateExplorationQuest(context);
            }
        }

        private ProceduralQuest GenerateExplorationQuest(QuestGenerationContext context)
        {
            var quest = new ProceduralQuest
            {
                type = QuestType.Exploration,
                title = "Explore the Unknown",
                description = "Venture into uncharted territories and discover new areas.",
                status = QuestStatus.Active
            };

            // Generate exploration objectives
            if (questLocationPoints != null && questLocationPoints.Length > 0)
            {
                var targetLocations = questLocationPoints
                    .OrderBy(x => Random.value)
                    .Take(Random.Range(2, 5))
                    .ToArray();

                foreach (var location in targetLocations)
                {
                    quest.objectives.Add(new QuestObjective
                    {
                        id = (uint)quest.objectives.Count,
                        description = $"Explore area near {location.name}",
                        targetProgress = 1,
                        currentProgress = 0,
                        objectiveData = new Dictionary<string, object>
                        {
                            ["targetPosition"] = location.position,
                            ["requiredDistance"] = 5f
                        }
                    });
                }
            }
            else
            {
                // Generic exploration objective
                quest.objectives.Add(new QuestObjective
                {
                    id = 0,
                    description = "Cover new ground in your travels",
                    targetProgress = Random.Range(100, 500),
                    currentProgress = 0
                });
            }

            return quest;
        }

        private ProceduralQuest GenerateCollectionQuest(QuestGenerationContext context)
        {
            var quest = new ProceduralQuest
            {
                type = QuestType.Collection,
                title = "Gather Resources",
                description = "Collect valuable resources from the environment.",
                status = QuestStatus.Active
            };

            // Generate collection objectives
            var resourceTypes = new[] { "Energy Crystals", "Genetic Samples", "Rare Materials", "Food Sources" };
            var selectedResource = resourceTypes[Random.Range(0, resourceTypes.Length)];

            quest.objectives.Add(new QuestObjective
            {
                id = 0,
                description = $"Collect {selectedResource}",
                targetProgress = Random.Range(5, 20),
                currentProgress = 0,
                objectiveData = new Dictionary<string, object>
                {
                    ["resourceType"] = selectedResource
                }
            });

            return quest;
        }

        private ProceduralQuest GenerateSurvivalQuest(QuestGenerationContext context)
        {
            var quest = new ProceduralQuest
            {
                type = QuestType.Survival,
                title = "Survive the Challenge",
                description = "Overcome environmental hazards and maintain your creatures' well-being.",
                status = QuestStatus.Active
            };

            quest.objectives.Add(new QuestObjective
            {
                id = 0,
                description = "Maintain population above threshold",
                targetProgress = Mathf.Max(5, context.totalCreaturePopulation / 2),
                currentProgress = context.totalCreaturePopulation
            });

            return quest;
        }

        private ProceduralQuest GenerateDiscoveryQuest(QuestGenerationContext context)
        {
            var quest = new ProceduralQuest
            {
                type = QuestType.Discovery,
                title = "Scientific Discovery",
                description = "Make breakthrough discoveries in genetic research.",
                status = QuestStatus.Active
            };

            quest.objectives.Add(new QuestObjective
            {
                id = 0,
                description = "Achieve new evolutionary milestone",
                targetProgress = 1,
                currentProgress = 0
            });

            return quest;
        }

        private ProceduralQuest GenerateSocialQuest(QuestGenerationContext context)
        {
            var quest = new ProceduralQuest
            {
                type = QuestType.Social,
                title = "Foster Community",
                description = "Encourage positive social interactions among creatures.",
                status = QuestStatus.Active
            };

            quest.objectives.Add(new QuestObjective
            {
                id = 0,
                description = "Facilitate successful creature interactions",
                targetProgress = Random.Range(10, 25),
                currentProgress = 0
            });

            return quest;
        }

        private bool CheckObjectiveProgress(QuestObjective objective, string eventType, Dictionary<string, object> eventData)
        {
            // Basic event matching logic
            switch (eventType)
            {
                case "LocationExplored":
                    return objective.description.Contains("Explore");

                case "ResourceCollected":
                    if (objective.objectiveData != null &&
                        objective.objectiveData.TryGetValue("resourceType", out var resourceType) &&
                        eventData.TryGetValue("resourceType", out var collectedType))
                    {
                        return resourceType.ToString() == collectedType.ToString();
                    }
                    break;

                case "CreatureBred":
                    return objective.description.Contains("interaction") || objective.description.Contains("breed");

                case "EvolutionaryMilestone":
                    return objective.description.Contains("milestone") || objective.description.Contains("discovery");
            }

            return false;
        }

        private void UpdateGameStateContext()
        {
            currentGameState = CreateContextFromGameState();
        }

        private QuestGenerationContext CreateContextFromGameState()
        {
            var context = new QuestGenerationContext();

            // Get data from genetic evolution manager
            if (GeneticEvolutionManager.Instance != null)
            {
                context.totalCreaturePopulation = GeneticEvolutionManager.Instance.TotalPopulationSize;
                context.averageCreatureFitness = GeneticEvolutionManager.Instance.AveragePopulationFitness;
            }

            // Get data from personality manager
            if (CreaturePersonalityManager.Instance != null)
            {
                context.activePersonalities = CreaturePersonalityManager.Instance.ActivePersonalityCount;
            }

            // Calculate other context values
            context.unexploredAreaRatio = CalculateUnexploredRatio();
            context.resourceAbundance = CalculateResourceAbundance();
            context.sessionTime = Time.time;

            return context;
        }

        private float CalculateQuestDifficulty(QuestGenerationContext context)
        {
            float sessionProgress = Mathf.Clamp01(context.sessionTime / 3600f); // Based on hour of play
            float baseDiff = difficultyProgression.Evaluate(sessionProgress) * baseDifficulty;
            float variance = Random.Range(-difficultyVariance, difficultyVariance);

            return Mathf.Clamp01(baseDiff + variance);
        }

        private QuestType GetRecommendedQuestType(QuestGenerationContext context)
        {
            // Simple recommendation logic based on context
            if (context.averageCreatureFitness < 0.3f)
                return QuestType.Survival;
            if (context.unexploredAreaRatio > 0.6f)
                return QuestType.Exploration;
            if (context.totalCreaturePopulation > 15)
                return QuestType.Social;

            return QuestType.Discovery;
        }

        private void UpdateTypePopularity(QuestType type, bool wasCompleted)
        {
            float adjustment = wasCompleted ? 0.05f : -0.02f;
            typePopularity[type] = Mathf.Clamp01(typePopularity[type] + adjustment);
        }

        private float CalculateUnexploredRatio()
        {
            // Simplified calculation - in real game would track actual exploration
            return Random.Range(0.3f, 0.8f);
        }

        private float CalculateResourceAbundance()
        {
            // Simplified calculation - in real game would track actual resources
            return Random.Range(0.4f, 0.9f);
        }

        private void CreateDefaultDifficultyProgression()
        {
            difficultyProgression = new AnimationCurve(
                new Keyframe(0f, 0.1f),
                new Keyframe(0.3f, 0.3f),
                new Keyframe(0.7f, 0.6f),
                new Keyframe(1f, 0.9f)
            );
        }

        private void SubscribeToGameEvents()
        {
            // Subscribe to genetic evolution events
            if (GeneticEvolutionManager.Instance != null)
            {
                GeneticEvolutionManager.Instance.OnEvolutionaryMilestone += HandleEvolutionaryEvent;
            }

            // Subscribe to personality system events
            if (CreaturePersonalityManager.Instance != null)
            {
                CreaturePersonalityManager.Instance.OnSocialInteraction += HandleSocialInteraction;
            }
        }

        private void HandleEvolutionaryEvent(string eventMessage)
        {
            UpdateQuestProgress("EvolutionaryMilestone", new Dictionary<string, object>
            {
                ["message"] = eventMessage
            });
        }

        private void HandleSocialInteraction(uint creatureA, uint creatureB, SocialInteractionType interactionType)
        {
            UpdateQuestProgress("CreatureBred", new Dictionary<string, object>
            {
                ["creatureA"] = creatureA,
                ["creatureB"] = creatureB,
                ["interactionType"] = interactionType
            });
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        // Editor menu items
        [UnityEditor.MenuItem("Laboratory/Quests/Generate Random Quest", false, 200)]
        private static void MenuGenerateQuest()
        {
            if (Application.isPlaying && Instance != null)
            {
                var quest = Instance.GenerateQuest();
                if (quest != null)
                {
                    Debug.Log($"Generated quest: {quest.title} - {quest.description}");
                }
            }
        }

        [UnityEditor.MenuItem("Laboratory/Quests/Show Quest Analytics", false, 201)]
        private static void MenuShowAnalytics()
        {
            if (Application.isPlaying && Instance != null)
            {
                var analytics = Instance.Analytics;
                Debug.Log($"Quest Analytics:\n" +
                         $"Generated: {analytics.totalQuestsGenerated}\n" +
                         $"Completed: {analytics.totalQuestsCompleted}\n" +
                         $"Expired: {analytics.totalQuestsExpired}\n" +
                         $"Active: {Instance.ActiveQuests.Count}");
            }
        }
    }

    // Supporting data structures
    [System.Serializable]
    public class ProceduralQuest
    {
        public uint id;
        public QuestType type;
        public string title;
        public string description;
        public QuestStatus status;
        public float difficulty;
        public float generatedTime;
        public float expirationTime;
        public float completedTime;
        public bool isRecommendation;

        public List<QuestObjective> objectives = new List<QuestObjective>();
        public QuestReward reward;
        public Dictionary<string, object> metadata = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class QuestObjective
    {
        public uint id;
        public string description;
        public int targetProgress;
        public int currentProgress;
        public bool isCompleted;
        public Dictionary<string, object> objectiveData;
    }

    [System.Serializable]
    public class QuestReward
    {
        public string rewardType;
        public int amount;
        public Dictionary<string, object> rewardData;
    }

    [System.Serializable]
    public class QuestGenerationContext
    {
        public int totalCreaturePopulation;
        public float averageCreatureFitness;
        public int activePersonalities;
        public float unexploredAreaRatio;
        public float resourceAbundance;
        public float sessionTime;
        public Dictionary<string, float> environmentalFactors = new Dictionary<string, float>();
    }

    [System.Serializable]
    public class GameStateContext
    {
        public float timestamp;
        public int totalPopulation;
        public float averageFitness;
        public Dictionary<string, object> gameData = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class QuestAnalytics
    {
        public int totalQuestsGenerated;
        public int totalQuestsCompleted;
        public int totalQuestsExpired;
        public Dictionary<QuestType, int> completionsByType = new Dictionary<QuestType, int>();
    }

    [System.Serializable]
    public class QuestRewardConfig
    {
        public string configName;
        public QuestReward[] possibleRewards;
    }

    public enum QuestType
    {
        Exploration,
        Collection,
        Survival,
        Discovery,
        Social,
        Challenge,
        Story
    }

    public enum QuestStatus
    {
        Generated,
        Active,
        Completed,
        Failed,
        Expired
    }
}