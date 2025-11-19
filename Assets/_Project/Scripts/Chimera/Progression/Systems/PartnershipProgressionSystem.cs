using Unity.Entities;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using Laboratory.Chimera.Activities;

namespace Laboratory.Chimera.Progression
{
    /// <summary>
    /// PARTNERSHIP PROGRESSION SYSTEM - Replaces level-based progression
    ///
    /// NEW VISION:
    /// - NO LEVELS - Neither players nor chimeras have traditional RPG progression
    /// - SKILL-FIRST - Victory through player ability + chimera cooperation
    /// - PRACTICE-BASED - Skill improves through DOING activities, not earning XP
    /// - COSMETIC REWARDS - Milestones unlock appearances, not stat boosts
    /// - PARTNERSHIP QUALITY - Success depends on understanding your chimera's personality
    ///
    /// Handles:
    /// - Tracking skill mastery across 47 game genres
    /// - Recording partnership quality (cooperation, trust, understanding)
    /// - Detecting skill milestones and awarding cosmetic unlocks
    /// - Managing currency rewards (cosmetic purchases)
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivitySystem))]
    public partial class PartnershipProgressionSystem : SystemBase
    {
        private ProgressionConfig _config;
        private EndSimulationEntityCommandBufferSystem _endSimulationECBSystem;

        private static readonly ProfilerMarker s_ProcessSkillImprovementMarker =
            new ProfilerMarker("Partnership.ProcessSkillImprovement");
        private static readonly ProfilerMarker s_ProcessCurrencyMarker =
            new ProfilerMarker("Partnership.ProcessCurrency");
        private static readonly ProfilerMarker s_DetectMilestonesMarker =
            new ProfilerMarker("Partnership.DetectMilestones");
        private static readonly ProfilerMarker s_UpdatePartnershipQualityMarker =
            new ProfilerMarker("Partnership.UpdatePartnershipQuality");

        protected override void OnCreate()
        {
            _endSimulationECBSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            _config = Resources.Load<ProgressionConfig>("Configs/ProgressionConfig");

            if (_config == null)
            {
                Debug.LogWarning("ProgressionConfig not found - using defaults");
            }

            // Create singleton for system tracking
            var singletonEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(singletonEntity, new ProgressionSystemData
            {
                isInitialized = true,
                currentTime = 0f,
                totalLevelsGained = 0, // Legacy
                totalExperienceDistributed = 0, // Legacy
                totalCurrencyDistributed = 0,
                totalSkillImprovementsRecorded = 0,
                totalMilestonesReached = 0,
                totalPartnershipsFormed = 0
            });

            Debug.Log("Partnership Progression System initialized - NO LEVELS, SKILL-FIRST!");
        }

        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            UpdateSystemData(currentTime);

            // Process skill improvements (replaces XP)
            using (s_ProcessSkillImprovementMarker.Auto())
            {
                ProcessSkillImprovements(currentTime);
            }

            // Process currency awards (for cosmetic purchases)
            using (s_ProcessCurrencyMarker.Auto())
            {
                ProcessCurrencyAwards(currentTime);
            }

            // Detect skill milestones (replaces level-ups)
            using (s_DetectMilestonesMarker.Auto())
            {
                DetectSkillMilestones(currentTime);
            }

            // Update partnership quality
            using (s_UpdatePartnershipQualityMarker.Auto())
            {
                UpdatePartnershipQuality(currentTime);
            }

            // Legacy XP system support (for migration)
            ProcessLegacyExperienceRequests(currentTime);
        }

        /// <summary>
        /// Updates system singleton data
        /// </summary>
        private void UpdateSystemData(float currentTime)
        {
            foreach (var systemData in SystemAPI.Query<RefRW<ProgressionSystemData>>())
            {
                systemData.ValueRW.currentTime = currentTime;
            }
        }

        /// <summary>
        /// Processes skill improvement requests from activity completion
        /// REPLACES XP SYSTEM - Skill improves through practice, not points!
        /// </summary>
        private void ProcessSkillImprovements(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<RecordSkillImprovementRequest>>().WithEntityAccess())
            {
                var partnershipEntity = request.ValueRO.partnershipEntity;

                if (!EntityManager.Exists(partnershipEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Ensure partnership has skill component
                if (!EntityManager.HasComponent<PartnershipSkillComponent>(partnershipEntity))
                {
                    // Initialize new partnership
                    ecb.AddComponent(partnershipEntity, new PartnershipSkillComponent
                    {
                        actionMastery = 0f,
                        strategyMastery = 0f,
                        puzzleMastery = 0f,
                        racingMastery = 0f,
                        rhythmMastery = 0f,
                        explorationMastery = 0f,
                        economicsMastery = 0f,
                        cooperationLevel = 0.5f, // Start neutral
                        trustLevel = 0.3f, // Start low, must be earned
                        understandingLevel = 0.1f, // Must learn chimera's personality
                        totalActivitiesCompleted = 0,
                        genresExplored = 0,
                        recentSuccessRate = 0.5f,
                        improvementTrend = 0f
                    });
                }

                // Get current skill data
                var skillData = EntityManager.GetComponentData<PartnershipSkillComponent>(partnershipEntity);

                // Calculate skill improvement amount based on:
                // - Performance quality (how well they did)
                // - Activity duration (more practice = more improvement)
                // - Cooperation (chimera working with player boosts learning)
                float baseImprovement = request.ValueRO.performanceQuality * 0.01f; // Max 1% per activity
                float durationMultiplier = Mathf.Clamp(request.ValueRO.activityDuration / 60f, 0.5f, 2.0f); // 1x at 60s
                float cooperationBonus = request.ValueRO.cooperatedWell ? 1.2f : 0.8f;

                float totalImprovement = baseImprovement * durationMultiplier * cooperationBonus;

                // Apply improvement to appropriate genre mastery
                switch (request.ValueRO.genre)
                {
                    case ActivityGenreCategory.Action:
                        skillData.actionMastery = Mathf.Clamp01(skillData.actionMastery + totalImprovement);
                        break;
                    case ActivityGenreCategory.Strategy:
                        skillData.strategyMastery = Mathf.Clamp01(skillData.strategyMastery + totalImprovement);
                        break;
                    case ActivityGenreCategory.Puzzle:
                        skillData.puzzleMastery = Mathf.Clamp01(skillData.puzzleMastery + totalImprovement);
                        break;
                    case ActivityGenreCategory.Racing:
                        skillData.racingMastery = Mathf.Clamp01(skillData.racingMastery + totalImprovement);
                        break;
                    case ActivityGenreCategory.Rhythm:
                        skillData.rhythmMastery = Mathf.Clamp01(skillData.rhythmMastery + totalImprovement);
                        break;
                    case ActivityGenreCategory.Exploration:
                        skillData.explorationMastery = Mathf.Clamp01(skillData.explorationMastery + totalImprovement);
                        break;
                    case ActivityGenreCategory.Economics:
                        skillData.economicsMastery = Mathf.Clamp01(skillData.economicsMastery + totalImprovement);
                        break;
                }

                // Update cooperation level based on this activity
                if (request.ValueRO.cooperatedWell)
                {
                    skillData.cooperationLevel = Mathf.Clamp01(skillData.cooperationLevel + 0.005f);
                }
                else
                {
                    skillData.cooperationLevel = Mathf.Clamp01(skillData.cooperationLevel - 0.002f);
                }

                // Track activity completion
                skillData.totalActivitiesCompleted++;

                // Update recent success rate (exponential moving average)
                float successValue = request.ValueRO.performanceQuality;
                skillData.recentSuccessRate = 0.9f * skillData.recentSuccessRate + 0.1f * successValue;

                // Update improvement trend
                float previousAvg = skillData.recentSuccessRate;
                skillData.improvementTrend = successValue - previousAvg;

                EntityManager.SetComponentData(partnershipEntity, skillData);

                // Update system stats
                IncrementSkillImprovements();

                Debug.Log($"Skill improved! Genre: {request.ValueRO.genre}, " +
                         $"Quality: {request.ValueRO.performanceQuality:F2}, " +
                         $"Improvement: +{totalImprovement:F3}, " +
                         $"Cooperation: {request.ValueRO.cooperatedWell}");

                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// Detects skill milestones and awards cosmetic rewards
        /// REPLACES LEVEL-UPS with meaningful skill achievements
        /// </summary>
        private void DetectSkillMilestones(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            foreach (var (skillData, entity) in
                SystemAPI.Query<RefRW<PartnershipSkillComponent>>().WithEntityAccess())
            {
                // Check each genre for milestone crossing
                CheckGenreMilestone(entity, ActivityGenreCategory.Action, skillData.ValueRO.actionMastery, currentTime, ecb);
                CheckGenreMilestone(entity, ActivityGenreCategory.Strategy, skillData.ValueRO.strategyMastery, currentTime, ecb);
                CheckGenreMilestone(entity, ActivityGenreCategory.Puzzle, skillData.ValueRO.puzzleMastery, currentTime, ecb);
                CheckGenreMilestone(entity, ActivityGenreCategory.Racing, skillData.ValueRO.racingMastery, currentTime, ecb);
                CheckGenreMilestone(entity, ActivityGenreCategory.Rhythm, skillData.ValueRO.rhythmMastery, currentTime, ecb);
                CheckGenreMilestone(entity, ActivityGenreCategory.Exploration, skillData.ValueRO.explorationMastery, currentTime, ecb);
                CheckGenreMilestone(entity, ActivityGenreCategory.Economics, skillData.ValueRO.economicsMastery, currentTime, ecb);
            }
        }

        /// <summary>
        /// Checks if a mastery level has crossed a milestone threshold
        /// </summary>
        private void CheckGenreMilestone(Entity partnership, ActivityGenreCategory genre, float mastery,
            float currentTime, EntityCommandBuffer ecb)
        {
            SkillMilestoneType? milestone = null;
            string cosmeticReward = "";

            // Check milestone thresholds
            if (mastery >= 0.90f)
            {
                milestone = SkillMilestoneType.Master;
                cosmeticReward = $"Master {genre} outfit + animation set";
            }
            else if (mastery >= 0.75f)
            {
                milestone = SkillMilestoneType.Expert;
                cosmeticReward = $"Expert {genre} accessory";
            }
            else if (mastery >= 0.50f)
            {
                milestone = SkillMilestoneType.Proficient;
                cosmeticReward = $"Proficient {genre} visual effect";
            }
            else if (mastery >= 0.25f)
            {
                milestone = SkillMilestoneType.Competent;
                cosmeticReward = $"Competent {genre} emblem";
            }
            else if (mastery >= 0.10f)
            {
                milestone = SkillMilestoneType.Beginner;
                cosmeticReward = $"Beginner {genre} badge";
            }

            if (milestone.HasValue)
            {
                // Create milestone event
                var milestoneEvent = EntityManager.CreateEntity();
                ecb.AddComponent(milestoneEvent, new SkillMilestoneReachedEvent
                {
                    partnershipEntity = partnership,
                    genre = genre,
                    milestoneType = milestone.Value,
                    masteryLevel = mastery,
                    eventTime = currentTime,
                    cosmeticRewardDescription = cosmeticReward
                });

                IncrementMilestones();

                Debug.Log($"MILESTONE REACHED! {genre} - {milestone} ({mastery:P0}) - Reward: {cosmeticReward}");
            }
        }

        /// <summary>
        /// Updates partnership quality metrics
        /// </summary>
        private void UpdatePartnershipQuality(float currentTime)
        {
            foreach (var (skillData, entity) in
                SystemAPI.Query<RefRW<PartnershipSkillComponent>>().WithEntityAccess())
            {
                // Trust increases with consistent positive cooperation
                if (skillData.ValueRO.cooperationLevel > 0.7f && skillData.ValueRO.recentSuccessRate > 0.6f)
                {
                    skillData.ValueRW.trustLevel = Mathf.Clamp01(skillData.ValueRW.trustLevel + 0.001f);
                }

                // Understanding increases with activity completion
                if (skillData.ValueRO.totalActivitiesCompleted > 0)
                {
                    float understandingGain = 1f / (100f + skillData.ValueRO.totalActivitiesCompleted);
                    skillData.ValueRW.understandingLevel = Mathf.Clamp01(
                        skillData.ValueRW.understandingLevel + understandingGain
                    );
                }
            }
        }

        /// <summary>
        /// Processes currency awards (for cosmetic purchases)
        /// Currency system REMAINS but used for cosmetics, not power
        /// </summary>
        private void ProcessCurrencyAwards(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<AwardCurrencyRequest>>().WithEntityAccess())
            {
                var targetEntity = request.ValueRO.targetEntity;

                if (!EntityManager.Exists(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Ensure currency component exists
                if (!EntityManager.HasComponent<CurrencyComponent>(targetEntity))
                {
                    ecb.AddComponent(targetEntity, new CurrencyComponent
                    {
                        coins = 0,
                        gems = 0,
                        activityTokens = 0
                    });
                }

                var currency = EntityManager.GetComponentData<CurrencyComponent>(targetEntity);

                // Award currency (for cosmetic purchases!)
                switch (request.ValueRO.currencyType)
                {
                    case CurrencyType.Coins:
                        currency.coins += Mathf.RoundToInt(request.ValueRO.amount);
                        break;
                    case CurrencyType.Gems:
                        currency.gems += request.ValueRO.amount;
                        break;
                    case CurrencyType.ActivityTokens:
                        currency.activityTokens += request.ValueRO.amount;
                        break;
                }

                EntityManager.SetComponentData(targetEntity, currency);
                IncrementTotalCurrency(request.ValueRO.amount);

                Debug.Log($"Awarded {request.ValueRO.amount} {request.ValueRO.currencyType} (for cosmetics!)");

                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// LEGACY SUPPORT - Converts old XP requests to skill improvements
        /// Helps during migration period
        /// </summary>
        private void ProcessLegacyExperienceRequests(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<AwardExperienceRequest>>().WithEntityAccess())
            {
                Debug.LogWarning($"DEPRECATED: XP request detected. Converting to skill improvement. " +
                                $"Please use RecordSkillImprovementRequest instead!");

                // Convert to skill improvement with default values
                var skillRequest = EntityManager.CreateEntity();
                ecb.AddComponent(skillRequest, new RecordSkillImprovementRequest
                {
                    partnershipEntity = request.ValueRO.targetEntity,
                    genre = ActivityGenreCategory.Action, // Default
                    performanceQuality = 0.5f, // Assume average
                    cooperatedWell = true,
                    activityDuration = 60f,
                    requestTime = currentTime
                });

                ecb.DestroyEntity(entity);
            }
        }

        // Stat tracking helpers
        private void IncrementSkillImprovements()
        {
            foreach (var systemData in SystemAPI.Query<RefRW<ProgressionSystemData>>())
            {
                systemData.ValueRW.totalSkillImprovementsRecorded++;
            }
        }

        private void IncrementMilestones()
        {
            foreach (var systemData in SystemAPI.Query<RefRW<ProgressionSystemData>>())
            {
                systemData.ValueRW.totalMilestonesReached++;
            }
        }

        private void IncrementTotalCurrency(int amount)
        {
            foreach (var systemData in SystemAPI.Query<RefRW<ProgressionSystemData>>())
            {
                systemData.ValueRW.totalCurrencyDistributed += amount;
            }
        }

        /// <summary>
        /// Public API: Record skill improvement
        /// </summary>
        public Entity CreateSkillImprovementRecord(Entity partnership, ActivityGenreCategory genre,
            float quality, bool cooperated, float duration)
        {
            var requestEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(requestEntity, new RecordSkillImprovementRequest
            {
                partnershipEntity = partnership,
                genre = genre,
                performanceQuality = quality,
                cooperatedWell = cooperated,
                activityDuration = duration,
                requestTime = (float)SystemAPI.Time.ElapsedTime
            });

            return requestEntity;
        }

        /// <summary>
        /// Public API: Award currency (for cosmetics)
        /// </summary>
        public Entity CreateCurrencyAward(Entity target, CurrencyType type, int amount)
        {
            var requestEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(requestEntity, new AwardCurrencyRequest
            {
                targetEntity = target,
                currencyType = type,
                amount = amount,
                requestTime = (float)SystemAPI.Time.ElapsedTime
            });

            return requestEntity;
        }
    }
}
