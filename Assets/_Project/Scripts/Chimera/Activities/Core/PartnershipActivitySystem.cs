using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Progression;
using Laboratory.Chimera.Consciousness.Core;

namespace Laboratory.Chimera.Activities
{
    /// <summary>
    /// PARTNERSHIP ACTIVITY SYSTEM
    ///
    /// Processes activities using skill + cooperation instead of stats
    ///
    /// NEW VISION: Victory comes from player skill + chimera cooperation
    /// - Player performance is primary factor (player skill matters!)
    /// - Chimera cooperation acts as multiplier (0.5x to 1.5x)
    /// - Personality fit provides bonus (+/- cooperation)
    /// - Equipment affects cooperation, not stats
    /// - Success improves partnership and skill
    ///
    /// Success Formula:
    /// FinalScore = PlayerPerformance × (BaseCooperation + PersonalityFit + Equipment + Mood)
    ///
    /// Responsibilities:
    /// - Start partnership activities
    /// - Calculate cooperation multipliers
    /// - Apply personality fit bonuses
    /// - Process activity results
    /// - Record skill improvements
    /// - Trigger emotional responses
    /// - Update partnership cooperation
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PartnershipProgressionSystem))]
    public partial class PartnershipActivitySystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            Debug.Log("Partnership Activity System initialized - skill + cooperation over stats!");
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Initialize mastery trackers
            InitializeMasteryTrackers();

            // Process activity start requests
            ProcessActivityStartRequests(currentTime);

            // Update active activities
            UpdateActiveActivities(deltaTime, currentTime);

            // Process activity completion
            ProcessActivityResults(currentTime);

            // Update mastery trackers
            UpdateMasteryTrackers();
        }

        /// <summary>
        /// Initializes mastery trackers for partnerships
        /// </summary>
        private void InitializeMasteryTrackers()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (partnershipSkill, entity) in
                SystemAPI.Query<RefRO<PartnershipSkillComponent>>()
                .WithEntityAccess()
                .WithNone<ActivityMasteryTracker>())
            {
                // Add buffer for tracking mastery across all genres
                ecb.AddBuffer<ActivityMasteryTracker>(entity);
            }
        }

        /// <summary>
        /// Processes requests to start partnership activities
        /// </summary>
        private void ProcessActivityStartRequests(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (startRequest, entity) in
                SystemAPI.Query<RefRO<StartPartnershipActivityRequest>>().WithEntityAccess())
            {
                var partnershipEntity = startRequest.ValueRO.partnershipEntity;
                var chimeraEntity = startRequest.ValueRO.chimeraEntity;

                if (!EntityManager.Exists(partnershipEntity) || !EntityManager.Exists(chimeraEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Get partnership skill
                if (!EntityManager.HasComponent<PartnershipSkillComponent>(partnershipEntity))
                {
                    Debug.LogWarning("Partnership entity missing PartnershipSkillComponent!");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var partnershipSkill = EntityManager.GetComponentData<PartnershipSkillComponent>(partnershipEntity);

                // Calculate activity genre
                var genre = GetGenreForActivity(startRequest.ValueRO.activityType);

                // Create active activity component
                var activeActivity = new PartnershipActivityComponent
                {
                    partnershipEntity = partnershipEntity,
                    currentActivity = startRequest.ValueRO.activityType,
                    genre = genre,
                    difficulty = startRequest.ValueRO.difficulty,
                    playerPerformance = 0f,
                    playerInputQuality = 0f,
                    chimeraCooperation = startRequest.ValueRO.currentCooperation,
                    personalityFitBonus = ActivityPersonalityFitCalculator.GetCooperationBonus(startRequest.ValueRO.personalityFit),
                    moodBonus = 0f, // TODO: Get from emotional state
                    equipmentCooperationBonus = startRequest.ValueRO.equipmentBonus,
                    startTime = currentTime,
                    duration = GetActivityDuration(startRequest.ValueRO.activityType, startRequest.ValueRO.difficulty),
                    elapsedTime = 0f,
                    isComplete = false,
                    finalScore = 0f,
                    resultStatus = ActivityResultStatus.InProgress,
                    skillImprovement = 0f
                };

                ecb.AddComponent(partnershipEntity, activeActivity);

                Debug.Log($"Started {startRequest.ValueRO.activityType} activity - " +
                         $"Cooperation: {startRequest.ValueRO.currentCooperation:F2}, " +
                         $"Personality Fit: {startRequest.ValueRO.personalityFit:F2}");

                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// Updates active activities
        /// </summary>
        private void UpdateActiveActivities(float deltaTime, float currentTime)
        {
            foreach (var (activity, entity) in
                SystemAPI.Query<RefRW<PartnershipActivityComponent>>().WithEntityAccess())
            {
                if (activity.ValueRO.isComplete)
                    continue;

                activity.ValueRW.elapsedTime += deltaTime;

                // Check if time's up
                if (activity.ValueRO.elapsedTime >= activity.ValueRO.duration)
                {
                    activity.ValueRW.isComplete = true;

                    // Calculate final score
                    CalculateFinalScore(ref activity.ValueRW);

                    Debug.Log($"Activity completed - Player: {activity.ValueRO.playerPerformance:F2}, " +
                             $"Cooperation: {activity.ValueRO.chimeraCooperation:F2}, " +
                             $"Final Score: {activity.ValueRO.finalScore:F2}");
                }
            }
        }

        /// <summary>
        /// Processes completed activity results
        /// </summary>
        private void ProcessActivityResults(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (activity, partnershipSkill, entity) in
                SystemAPI.Query<RefRW<PartnershipActivityComponent>, RefRW<PartnershipSkillComponent>>().WithEntityAccess())
            {
                if (!activity.ValueRO.isComplete)
                    continue;

                // Record skill improvement
                RecordSkillImprovement(ref partnershipSkill.ValueRW, activity.ValueRO);

                // Create result entity
                var resultEntity = EntityManager.CreateEntity();
                ecb.AddComponent(resultEntity, new PartnershipActivityResult
                {
                    partnershipEntity = activity.ValueRO.partnershipEntity,
                    chimeraEntity = Entity.Null, // TODO: Get chimera entity
                    activityType = activity.ValueRO.currentActivity,
                    genre = activity.ValueRO.genre,
                    playerPerformance = activity.ValueRO.playerPerformance,
                    cooperationMultiplier = CalculateCooperationMultiplier(activity.ValueRO),
                    personalityFitBonus = activity.ValueRO.personalityFitBonus,
                    finalScore = activity.ValueRO.finalScore,
                    status = activity.ValueRO.resultStatus,
                    completionTime = activity.ValueRO.elapsedTime,
                    timestamp = currentTime,
                    skillGained = activity.ValueRO.skillImprovement,
                    cooperationImproved = activity.ValueRO.skillImprovement > 0.01f,
                    bondStrengthChange = activity.ValueRO.finalScore > 0.6f ? 0.05f : -0.02f,
                    cosmeticRewardsUnlocked = 0,
                    achievementUnlocked = "",
                    emotionalImpact = activity.ValueRO.finalScore > 0.7f ? EmotionalTrigger.WonActivity : EmotionalTrigger.LostActivity
                });

                // Remove active activity component
                ecb.RemoveComponent<PartnershipActivityComponent>(entity);

                Debug.Log($"Activity result processed - Skill gained: {activity.ValueRO.skillImprovement:F3}");
            }
        }

        /// <summary>
        /// Updates mastery trackers based on activity results
        /// </summary>
        private void UpdateMasteryTrackers()
        {
            foreach (var (result, entity) in
                SystemAPI.Query<RefRO<PartnershipActivityResult>>().WithEntityAccess())
            {
                var partnershipEntity = result.ValueRO.partnershipEntity;

                if (!EntityManager.Exists(partnershipEntity))
                    continue;

                if (!EntityManager.HasBuffer<ActivityMasteryTracker>(partnershipEntity))
                    continue;

                var masteryBuffer = EntityManager.GetBuffer<ActivityMasteryTracker>(partnershipEntity);

                // Find or create mastery tracker for this genre
                int trackerIndex = -1;
                for (int i = 0; i < masteryBuffer.Length; i++)
                {
                    if (masteryBuffer[i].genre == result.ValueRO.genre)
                    {
                        trackerIndex = i;
                        break;
                    }
                }

                if (trackerIndex == -1)
                {
                    // Create new tracker
                    masteryBuffer.Add(new ActivityMasteryTracker
                    {
                        genre = result.ValueRO.genre,
                        masteryLevel = 0f,
                        totalAttempts = 0,
                        successfulCompletions = 0,
                        successRate = 0f,
                        averagePerformance = 0f,
                        bestPerformance = 0f,
                        recentTrend = 0f,
                        averageCooperation = 0f,
                        lastActivityTime = result.ValueRO.timestamp
                    });
                    trackerIndex = masteryBuffer.Length - 1;
                }

                // Update tracker
                var tracker = masteryBuffer[trackerIndex];
                tracker.totalAttempts++;
                if (result.ValueRO.status == ActivityResultStatus.Success ||
                    result.ValueRO.status == ActivityResultStatus.Perfect)
                {
                    tracker.successfulCompletions++;
                }

                tracker.successRate = (float)tracker.successfulCompletions / tracker.totalAttempts;
                tracker.masteryLevel = math.min(1f, tracker.masteryLevel + result.ValueRO.skillGained);
                tracker.averagePerformance = (tracker.averagePerformance * 0.8f) + (result.ValueRO.playerPerformance * 0.2f);
                tracker.bestPerformance = math.max(tracker.bestPerformance, result.ValueRO.playerPerformance);
                tracker.averageCooperation = (tracker.averageCooperation * 0.8f) + (result.ValueRO.cooperationMultiplier * 0.2f);
                tracker.lastActivityTime = result.ValueRO.timestamp;

                masteryBuffer[trackerIndex] = tracker;
            }
        }

        // Helper methods

        private void CalculateFinalScore(ref PartnershipActivityComponent activity)
        {
            // TODO: Get actual player performance from gameplay
            // For now, use random performance as placeholder
            var random = new Unity.Mathematics.Random((uint)(activity.startTime * 1000));
            activity.playerPerformance = random.NextFloat(0.3f, 1.0f);

            // Calculate cooperation multiplier
            float cooperationMultiplier = CalculateCooperationMultiplier(activity);

            // Calculate final score
            activity.finalScore = activity.playerPerformance * cooperationMultiplier;
            activity.finalScore = math.clamp(activity.finalScore, 0f, 1.5f);

            // Determine result status
            if (activity.finalScore >= 0.9f)
                activity.resultStatus = ActivityResultStatus.Perfect;
            else if (activity.finalScore >= 0.7f)
                activity.resultStatus = ActivityResultStatus.Success;
            else if (activity.finalScore >= 0.5f)
                activity.resultStatus = ActivityResultStatus.Partial;
            else
                activity.resultStatus = ActivityResultStatus.Failed;

            // Calculate skill improvement
            float baseImprovement = 0.01f; // 1% base improvement per activity
            float qualityMultiplier = activity.playerPerformance; // Better performance = more learning
            float cooperationBonus = activity.chimeraCooperation > 0.8f ? 1.2f : 1.0f; // Cooperation boosts learning

            activity.skillImprovement = baseImprovement * qualityMultiplier * cooperationBonus;
        }

        private float CalculateCooperationMultiplier(PartnershipActivityComponent activity)
        {
            float multiplier = activity.chimeraCooperation; // Base cooperation (0.0-1.2)
            multiplier += activity.personalityFitBonus;      // Personality fit bonus (-0.3 to +0.3)
            multiplier += activity.equipmentCooperationBonus; // Equipment bonus (-0.3 to +0.3)
            multiplier += activity.moodBonus;                 // Mood bonus (-0.2 to +0.2)

            // Clamp to reasonable range
            return math.clamp(multiplier, 0.5f, 1.5f); // Min 50%, max 150%
        }

        private void RecordSkillImprovement(ref PartnershipSkillComponent partnershipSkill, PartnershipActivityComponent activity)
        {
            // Improve genre mastery
            switch (activity.genre)
            {
                case ActivityGenreCategory.Action:
                    partnershipSkill.actionMastery = math.min(1f, partnershipSkill.actionMastery + activity.skillImprovement);
                    break;
                case ActivityGenreCategory.Strategy:
                    partnershipSkill.strategyMastery = math.min(1f, partnershipSkill.strategyMastery + activity.skillImprovement);
                    break;
                case ActivityGenreCategory.Puzzle:
                    partnershipSkill.puzzleMastery = math.min(1f, partnershipSkill.puzzleMastery + activity.skillImprovement);
                    break;
                case ActivityGenreCategory.Racing:
                    partnershipSkill.racingMastery = math.min(1f, partnershipSkill.racingMastery + activity.skillImprovement);
                    break;
                case ActivityGenreCategory.Rhythm:
                    partnershipSkill.rhythmMastery = math.min(1f, partnershipSkill.rhythmMastery + activity.skillImprovement);
                    break;
                case ActivityGenreCategory.Exploration:
                    partnershipSkill.explorationMastery = math.min(1f, partnershipSkill.explorationMastery + activity.skillImprovement);
                    break;
                case ActivityGenreCategory.Economics:
                    partnershipSkill.economicsMastery = math.min(1f, partnershipSkill.economicsMastery + activity.skillImprovement);
                    break;
            }

            // Update partnership quality
            if (activity.finalScore > 0.7f)
            {
                // Good performance improves cooperation
                partnershipSkill.cooperationLevel = math.min(1.2f, partnershipSkill.cooperationLevel + 0.01f);
                partnershipSkill.trustLevel = math.min(1f, partnershipSkill.trustLevel + 0.005f);
            }

            // Track activity completion
            partnershipSkill.totalActivitiesCompleted++;
        }

        private ActivityGenreCategory GetGenreForActivity(ActivityType activityType)
        {
            // TODO: Implement proper mapping from ActivityType to Genre
            // For now, return Action as placeholder
            return ActivityGenreCategory.Action;
        }

        private float GetActivityDuration(ActivityType activityType, ActivityDifficulty difficulty)
        {
            // Base durations in seconds
            float baseDuration = difficulty switch
            {
                ActivityDifficulty.Easy => 30f,
                ActivityDifficulty.Medium => 60f,
                ActivityDifficulty.Hard => 120f,
                ActivityDifficulty.Expert => 180f,
                _ => 60f
            };

            return baseDuration;
        }
    }
}
