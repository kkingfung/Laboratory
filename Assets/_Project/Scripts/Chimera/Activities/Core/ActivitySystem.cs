using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Profiling;
using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Creatures;
using System.Collections.Generic;

namespace Laboratory.Chimera.Activities
{
    /// <summary>
    /// Burst-compiled job for checking activity completion times
    /// Runs in parallel across all active activities
    /// </summary>
    [BurstCompile]
    public partial struct CheckActivityCompletionJob : IJobEntity
    {
        public float CurrentTime;

        public void Execute(ref ActiveActivityComponent activeActivity)
        {
            // Skip if already complete
            if (activeActivity.isComplete)
                return;

            float elapsedTime = CurrentTime - activeActivity.startTime;

            // Check if activity duration has elapsed
            if (elapsedTime >= activeActivity.duration)
            {
                // Mark as complete (performance will be calculated in main system)
                activeActivity.isComplete = true;
            }
        }
    }

    /// <summary>
    /// Main ECS system for managing activity participation and results
    /// Handles activity execution, performance calculation, and reward distribution
    /// Performance: Uses Burst-compiled jobs for parallel activity processing
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ActivitySystem : SystemBase
    {
        private Dictionary<ActivityType, IActivity> _activityImplementations;
        private Dictionary<ActivityType, ActivityConfig> _activityConfigs;

        // Cached entity queries for performance (SystemAPI.Query also auto-caches internally)
        private EntityQuery _systemDataQuery;
        private EntityQuery _activityRequestQuery;
        private EntityQuery _activeActivitiesQuery;
        private EntityQuery _activityResultsQuery;

        // Entity command buffer system for deferred structural changes
        private EndSimulationEntityCommandBufferSystem _endSimulationECBSystem;

        private static readonly ProfilerMarker s_ProcessActivityRequestsMarker =
            new ProfilerMarker("Activity.ProcessRequests");
        private static readonly ProfilerMarker s_UpdateActiveActivitiesMarker =
            new ProfilerMarker("Activity.UpdateActive");
        private static readonly ProfilerMarker s_ProcessActivityResultsMarker =
            new ProfilerMarker("Activity.ProcessResults");

        protected override void OnCreate()
        {
            _activityImplementations = new Dictionary<ActivityType, IActivity>();
            _activityConfigs = new Dictionary<ActivityType, ActivityConfig>();

            // Initialize cached entity queries
            _systemDataQuery = GetEntityQuery(ComponentType.ReadWrite<ActivitySystemData>());
            _activityRequestQuery = GetEntityQuery(ComponentType.ReadOnly<StartActivityRequest>());
            _activeActivitiesQuery = GetEntityQuery(
                ComponentType.ReadWrite<ActiveActivityComponent>(),
                ComponentType.ReadOnly<ActivityGeneticsData>());
            _activityResultsQuery = GetEntityQuery(
                ComponentType.ReadOnly<ActivityResultComponent>(),
                ComponentType.ReadWrite<CurrencyComponent>(),
                ComponentType.ReadWrite<ActivityProgressElement>());

            // Get entity command buffer system for optimized deferred operations
            _endSimulationECBSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

            // Load activity configurations from Resources
            LoadActivityConfigurations();

            // Create singleton entity for system data
            var singletonEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(singletonEntity, new ActivitySystemData
            {
                isInitialized = true,
                currentTime = 0f,
                totalActivitiesCompleted = 0,
                totalRewardsDistributed = 0
            });
        }

        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Update system data
            UpdateSystemData(currentTime);

            // Process new activity requests
            using (s_ProcessActivityRequestsMarker.Auto())
            {
                ProcessActivityRequests(currentTime);
            }

            // Update ongoing activities
            using (s_UpdateActiveActivitiesMarker.Auto())
            {
                UpdateActiveActivities(currentTime, deltaTime);
            }

            // Process completed activities and distribute rewards
            using (s_ProcessActivityResultsMarker.Auto())
            {
                ProcessActivityResults(currentTime);
            }
        }

        /// <summary>
        /// Loads activity configurations from Resources folder
        /// </summary>
        private void LoadActivityConfigurations()
        {
            var configs = Resources.LoadAll<ActivityConfig>("Configs/Activities");
            foreach (var config in configs)
            {
                _activityConfigs[config.activityType] = config;
                Debug.Log($"Loaded activity config: {config.activityName} ({config.activityType})");
            }

            if (_activityConfigs.Count == 0)
            {
                Debug.LogWarning("No activity configurations found in Resources/Configs/Activities/");
            }
        }

        /// <summary>
        /// Updates system singleton data
        /// </summary>
        private void UpdateSystemData(float currentTime)
        {
            foreach (var systemData in SystemAPI.Query<RefRW<ActivitySystemData>>())
            {
                systemData.ValueRW.currentTime = currentTime;
            }
        }

        /// <summary>
        /// Processes requests to start new activities
        /// Uses EntityCommandBufferSystem for optimized structural changes
        /// </summary>
        private void ProcessActivityRequests(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<StartActivityRequest>>().WithEntityAccess())
            {
                var monsterEntity = request.ValueRO.monsterEntity;

                // Check if monster exists and has genetics
                if (!EntityManager.Exists(monsterEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                if (!EntityManager.HasComponent<ActivityGeneticsData>(monsterEntity))
                {
                    Debug.LogWarning($"Monster entity missing genetics component");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Get activity config
                if (!_activityConfigs.TryGetValue(request.ValueRO.activityType, out var config))
                {
                    Debug.LogWarning($"No configuration found for activity: {request.ValueRO.activityType}");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Start the activity
                float duration = config.GetBaseDuration(request.ValueRO.difficulty);

                // Add or update active activity component
                ecb.AddComponent(monsterEntity, new ActiveActivityComponent
                {
                    currentActivity = request.ValueRO.activityType,
                    difficulty = request.ValueRO.difficulty,
                    startTime = currentTime,
                    duration = duration,
                    isComplete = false,
                    performanceScore = 0f
                });

                // Add activity participant tag if not present
                if (!EntityManager.HasComponent<ActivityParticipantTag>(monsterEntity))
                {
                    ecb.AddComponent<ActivityParticipantTag>(monsterEntity);
                }

                // Ensure progress buffer exists
                if (!EntityManager.HasBuffer<ActivityProgressElement>(monsterEntity))
                {
                    ecb.AddBuffer<ActivityProgressElement>(monsterEntity);
                }

                // Remove the request
                ecb.DestroyEntity(entity);
            }

            // ECB will be automatically played back by EndSimulationEntityCommandBufferSystem
        }

        /// <summary>
        /// Updates ongoing activities and calculates completion
        /// Performance: Step 1 runs in parallel Burst-compiled job, Step 2 handles results
        /// </summary>
        private void UpdateActiveActivities(float currentTime, float deltaTime)
        {
            // Step 1: Parallel job to check completion times (Burst-compiled)
            var completionJob = new CheckActivityCompletionJob
            {
                CurrentTime = currentTime
            };
            completionJob.ScheduleParallel();

            // Ensure job completes before processing results
            Dependency.Complete();

            // Step 2: Process newly completed activities (requires managed access for activity implementations)
            foreach (var (activeActivity, genetics, entity) in
                SystemAPI.Query<RefRW<ActiveActivityComponent>, RefRO<ActivityGeneticsData>>()
                .WithEntityAccess())
            {
                // Only process activities that just completed
                if (!activeActivity.ValueRO.isComplete)
                    continue;

                // Skip if we already calculated performance (has result component)
                if (EntityManager.HasComponent<ActivityResultComponent>(entity))
                    continue;

                float elapsedTime = currentTime - activeActivity.ValueRO.startTime;

                // Calculate performance
                float performanceScore = CalculateActivityPerformance(
                    in genetics.ValueRO,
                    activeActivity.ValueRO.currentActivity,
                    activeActivity.ValueRO.difficulty,
                    entity);

                // Store performance in component
                activeActivity.ValueRW.performanceScore = performanceScore;

                // Create result component for reward processing
                EntityManager.AddComponentData(entity, CreateActivityResult(
                    activeActivity.ValueRO.currentActivity,
                    activeActivity.ValueRO.difficulty,
                    performanceScore,
                    elapsedTime,
                    currentTime));
            }
        }

        /// <summary>
        /// Processes completed activities and distributes rewards
        /// Uses EntityCommandBufferSystem for optimized structural changes
        /// </summary>
        private void ProcessActivityResults(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            foreach (var (result, currency, progressBuffer, entity) in
                SystemAPI.Query<RefRO<ActivityResultComponent>, RefRW<CurrencyComponent>,
                DynamicBuffer<ActivityProgressElement>>().WithEntityAccess())
            {
                // Award currency
                currency.ValueRW.coins += result.ValueRO.coinsEarned;
                currency.ValueRW.activityTokens += result.ValueRO.tokensEarned;

                // Record skill improvement via PartnershipProgressionSystem
                if (result.ValueRO.experienceGained > 0)
                {
                    var skillRequest = EntityManager.CreateEntity();
                    ecb.AddComponent(skillRequest, new Laboratory.Chimera.Progression.RecordSkillImprovementRequest
                    {
                        partnershipEntity = entity,
                        genre = ConvertActivityTypeToGenre(result.ValueRO.activityType),
                        performanceQuality = result.ValueRO.performanceScore,
                        cooperatedWell = result.ValueRO.status >= ActivityResultStatus.Silver,
                        activityDuration = result.ValueRO.completionTime,
                        requestTime = (float)SystemAPI.Time.ElapsedTime
                    });
                }

                // Update activity progress
                UpdateActivityProgress(progressBuffer, result.ValueRO);

                // Update daily challenge progress
                UpdateDailyChallengeProgress(entity, result.ValueRO.activityType);

                // Remove active activity component
                if (EntityManager.HasComponent<ActiveActivityComponent>(entity))
                {
                    ecb.RemoveComponent<ActiveActivityComponent>(entity);
                }

                // Remove result component
                ecb.RemoveComponent<ActivityResultComponent>(entity);

                // Update system stats
                IncrementCompletedActivities();

                Debug.Log($"Activity completed: {result.ValueRO.activityType} " +
                         $"Rank: {result.ValueRO.status} " +
                         $"Performance: {result.ValueRO.performanceScore:F2} " +
                         $"Rewards: {result.ValueRO.coinsEarned} coins, {result.ValueRO.experienceGained} XP");
            }

            // ECB will be automatically played back by EndSimulationEntityCommandBufferSystem
        }

        /// <summary>
        /// Calculates activity performance based on genetics and equipment
        /// </summary>
        private float CalculateActivityPerformance(
            in ActivityGeneticsData genetics,
            ActivityType activityType,
            ActivityDifficulty difficulty,
            Entity monsterEntity)
        {
            // Get activity implementation
            if (!_activityImplementations.TryGetValue(activityType, out var activity))
            {
                // Use default calculation if no specific implementation
                return CalculateDefaultPerformance(in genetics, difficulty);
            }

            // Get equipment bonus
            float equipmentBonus = CalculateEquipmentBonus(monsterEntity, activityType);

            // Get mastery bonus
            float masteryBonus = GetMasteryBonus(monsterEntity, activityType);

            // Calculate performance using activity-specific logic
            return activity.CalculatePerformance(in genetics, difficulty, equipmentBonus, masteryBonus);
        }

        /// <summary>
        /// Default performance calculation when no specific implementation exists
        /// </summary>
        private float CalculateDefaultPerformance(in ActivityGeneticsData genetics, ActivityDifficulty difficulty)
        {
            ActivityPerformanceCalculator.ExtractGeneticStats(
                in genetics,
                out float strength,
                out float agility,
                out float intelligence,
                out float vitality,
                out float social,
                out float adaptability);

            // Use balanced weights for default calculation
            float basePerformance = ActivityPerformanceCalculator.CalculateWeightedPerformance(
                strength, agility, intelligence,
                0.33f, 0.33f, 0.34f);

            return ActivityPerformanceCalculator.ApplyDifficultyModifier(basePerformance, difficulty, 1.0f);
        }

        /// <summary>
        /// Calculates equipment bonus for activity using cached bonuses
        /// </summary>
        private float CalculateEquipmentBonus(Entity entity, ActivityType activityType)
        {
            // Use equipment bonus cache for performance (updated by EquipmentSystem)
            if (EntityManager.HasComponent<Laboratory.Chimera.Equipment.EquipmentBonusCache>(entity))
            {
                var bonusCache = EntityManager.GetComponentData<Laboratory.Chimera.Equipment.EquipmentBonusCache>(entity);

                return activityType switch
                {
                    ActivityType.Racing => bonusCache.racingBonus,
                    ActivityType.Combat => bonusCache.combatBonus,
                    ActivityType.Puzzle => bonusCache.puzzleBonus,
                    ActivityType.Strategy => bonusCache.strategyBonus,
                    ActivityType.Music => bonusCache.rhythmBonus,
                    ActivityType.Adventure => bonusCache.adventureBonus,
                    ActivityType.Platforming => bonusCache.platformingBonus,
                    ActivityType.Crafting => bonusCache.craftingBonus,
                    _ => 0f
                };
            }

            // Fallback to legacy buffer-based calculation
            if (!EntityManager.HasBuffer<EquippedItemElement>(entity))
                return 0f;

            var equipmentBuffer = EntityManager.GetBuffer<EquippedItemElement>(entity);
            float totalBonus = 0f;

            foreach (var item in equipmentBuffer)
            {
                // Check if equipment provides bonus for this activity
                if (item.activityBonus == activityType || item.activityBonus == ActivityType.None)
                {
                    totalBonus += item.bonusValue;
                }
            }

            return Mathf.Clamp01(totalBonus);
        }

        /// <summary>
        /// Gets mastery bonus multiplier for activity
        /// </summary>
        private float GetMasteryBonus(Entity entity, ActivityType activityType)
        {
            if (!EntityManager.HasBuffer<ActivityProgressElement>(entity))
                return 1.0f;

            var progressBuffer = EntityManager.GetBuffer<ActivityProgressElement>(entity);

            foreach (var progress in progressBuffer)
            {
                if (progress.activityType == activityType)
                {
                    return progress.masteryMultiplier > 0 ? progress.masteryMultiplier : 1.0f;
                }
            }

            return 1.0f;
        }

        /// <summary>
        /// Creates activity result with calculated rewards
        /// </summary>
        private ActivityResultComponent CreateActivityResult(
            ActivityType activityType,
            ActivityDifficulty difficulty,
            float performanceScore,
            float completionTime,
            float currentTime)
        {
            if (!_activityConfigs.TryGetValue(activityType, out var config))
            {
                // Return minimal result if config not found
                return new ActivityResultComponent
                {
                    activityType = activityType,
                    difficulty = difficulty,
                    status = ActivityResultStatus.Failed,
                    performanceScore = performanceScore,
                    completionTime = completionTime,
                    completedAt = currentTime
                };
            }

            // Get rank from performance
            var rank = ActivityPerformanceCalculator.GetRankFromScore(performanceScore, config.rankThresholds);

            // Calculate rewards
            int coins = ActivityPerformanceCalculator.CalculateCurrencyReward(
                config.GetCoinReward(rank), difficulty, performanceScore);

            int experience = ActivityPerformanceCalculator.CalculateExperienceGain(
                config.GetExperienceReward(rank), difficulty, performanceScore);

            int tokens = config.GetTokenReward(rank);

            return new ActivityResultComponent
            {
                activityType = activityType,
                difficulty = difficulty,
                status = rank,
                performanceScore = performanceScore,
                completionTime = completionTime,
                coinsEarned = coins,
                experienceGained = experience,
                tokensEarned = tokens,
                completedAt = currentTime
            };
        }

        /// <summary>
        /// Updates activity progress buffer with new result
        /// </summary>
        private void UpdateActivityProgress(DynamicBuffer<ActivityProgressElement> progressBuffer, ActivityResultComponent result)
        {
            bool found = false;

            // Find existing progress for this activity
            for (int i = 0; i < progressBuffer.Length; i++)
            {
                if (progressBuffer[i].activityType == result.activityType)
                {
                    var progress = progressBuffer[i];

                    // Update stats
                    progress.attemptsCount++;
                    if (result.status != ActivityResultStatus.Failed)
                        progress.successCount++;

                    progress.experiencePoints += result.experienceGained;

                    // Update best performance
                    if (result.performanceScore > progress.bestPerformanceScore)
                        progress.bestPerformanceScore = result.performanceScore;

                    if (result.status > progress.highestRank)
                        progress.highestRank = result.status;

                    if (result.completionTime < progress.bestCompletionTime || progress.bestCompletionTime == 0)
                        progress.bestCompletionTime = result.completionTime;

                    // Calculate level and mastery
                    if (_activityConfigs.TryGetValue(result.activityType, out var config))
                    {
                        progress.level = progress.experiencePoints / config.experiencePerLevel;
                        progress.masteryMultiplier = config.GetMasteryMultiplier(progress.level);
                    }

                    progress.lastAttemptTime = result.completedAt;

                    progressBuffer[i] = progress;
                    found = true;
                    break;
                }
            }

            // Create new progress entry if not found
            if (!found)
            {
                var newProgress = new ActivityProgressElement
                {
                    activityType = result.activityType,
                    experiencePoints = result.experienceGained,
                    level = 0,
                    attemptsCount = 1,
                    successCount = result.status != ActivityResultStatus.Failed ? 1 : 0,
                    bestPerformanceScore = result.performanceScore,
                    bestCompletionTime = result.completionTime,
                    highestRank = result.status,
                    masteryMultiplier = 1.0f,
                    lastAttemptTime = result.completedAt
                };

                progressBuffer.Add(newProgress);
            }
        }

        /// <summary>
        /// Increments total completed activities counter
        /// </summary>
        private void IncrementCompletedActivities()
        {
            foreach (var systemData in SystemAPI.Query<RefRW<ActivitySystemData>>())
            {
                systemData.ValueRW.totalActivitiesCompleted++;
            }
        }

        /// <summary>
        /// Registers a custom activity implementation
        /// </summary>
        public void RegisterActivity(ActivityType type, IActivity implementation)
        {
            _activityImplementations[type] = implementation;
            Debug.Log($"Registered activity implementation: {type}");
        }

        /// <summary>
        /// Updates daily challenge progress when activity completes
        /// </summary>
        private void UpdateDailyChallengeProgress(Entity entity, ActivityType activityType)
        {
            // Get PartnershipProgressionSystem and update challenges
            var progressionSystem = World.GetExistingSystemManaged<Laboratory.Chimera.Progression.PartnershipProgressionSystem>();
            if (progressionSystem != null)
            {
                // Partnership system handles challenges differently - via events
                // This integration point may need updating based on new challenge system
            }
        }

        /// <summary>
        /// Converts ActivityType to ActivityGenreCategory for progression system
        /// </summary>
        private Laboratory.Chimera.Progression.ActivityGenreCategory ConvertActivityTypeToGenre(ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Racing => Laboratory.Chimera.Progression.ActivityGenreCategory.Racing,
                ActivityType.Combat => Laboratory.Chimera.Progression.ActivityGenreCategory.Action,
                ActivityType.Puzzle => Laboratory.Chimera.Progression.ActivityGenreCategory.Puzzle,
                ActivityType.Strategy => Laboratory.Chimera.Progression.ActivityGenreCategory.Strategy,
                ActivityType.Music => Laboratory.Chimera.Progression.ActivityGenreCategory.Rhythm,
                ActivityType.Adventure => Laboratory.Chimera.Progression.ActivityGenreCategory.Exploration,
                ActivityType.Platforming => Laboratory.Chimera.Progression.ActivityGenreCategory.Action,
                ActivityType.Crafting => Laboratory.Chimera.Progression.ActivityGenreCategory.Economics,
                _ => Laboratory.Chimera.Progression.ActivityGenreCategory.Action
            };
        }
    }
}
