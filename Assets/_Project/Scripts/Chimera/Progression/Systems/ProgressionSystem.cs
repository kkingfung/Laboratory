using Unity.Entities;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using Laboratory.Chimera.Activities;

namespace Laboratory.Chimera.Progression
{
    /// <summary>
    /// Main ECS system for monster progression and currency management
    /// Handles leveling, experience gain, skill unlocks, and rewards
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivitySystem))]
    public partial class ProgressionSystem : SystemBase
    {
        private ProgressionConfig _config;

        // Entity command buffer system for optimized deferred operations
        private EndSimulationEntityCommandBufferSystem _endSimulationECBSystem;

        private static readonly ProfilerMarker s_ProcessExperienceMarker =
            new ProfilerMarker("Progression.ProcessExperience");
        private static readonly ProfilerMarker s_ProcessCurrencyMarker =
            new ProfilerMarker("Progression.ProcessCurrency");
        private static readonly ProfilerMarker s_UpdateLevelsMarker =
            new ProfilerMarker("Progression.UpdateLevels");
        private static readonly ProfilerMarker s_ProcessSkillUnlocksMarker =
            new ProfilerMarker("Progression.ProcessSkillUnlocks");
        private static readonly ProfilerMarker s_UpdateDailyChallengesMarker =
            new ProfilerMarker("Progression.UpdateDailyChallenges");

        protected override void OnCreate()
        {
            // Get entity command buffer system for optimized deferred operations
            _endSimulationECBSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

            // Load progression configuration
            _config = Resources.Load<ProgressionConfig>("Configs/ProgressionConfig");

            if (_config == null)
            {
                Debug.LogWarning("ProgressionConfig not found at Resources/Configs/ProgressionConfig");
            }

            // Create singleton entity for system data
            var singletonEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(singletonEntity, new ProgressionSystemData
            {
                isInitialized = true,
                currentTime = 0f,
                totalLevelsGained = 0,
                totalExperienceDistributed = 0,
                totalCurrencyDistributed = 0
            });

            Debug.Log("Progression System initialized");
        }

        protected override void OnUpdate()
        {
            if (_config == null) return;

            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Update system data
            UpdateSystemData(currentTime);

            // Process experience awards
            using (s_ProcessExperienceMarker.Auto())
            {
                ProcessExperienceAwards(currentTime);
            }

            // Process currency awards
            using (s_ProcessCurrencyMarker.Auto())
            {
                ProcessCurrencyAwards(currentTime);
            }

            // Check for level ups
            using (s_UpdateLevelsMarker.Auto())
            {
                CheckForLevelUps(currentTime);
            }

            // Process skill unlocks
            using (s_ProcessSkillUnlocksMarker.Auto())
            {
                ProcessSkillUnlocks(currentTime);
            }

            // Update daily challenges
            using (s_UpdateDailyChallengesMarker.Auto())
            {
                UpdateDailyChallenges(currentTime);
            }
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
        /// Processes experience award requests
        /// Uses EntityCommandBufferSystem for optimized structural changes
        /// </summary>
        private void ProcessExperienceAwards(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<AwardExperienceRequest>>().WithEntityAccess())
            {
                var targetEntity = request.ValueRO.targetEntity;
                var experienceAmount = request.ValueRO.experienceAmount;

                // Validate target entity
                if (!EntityManager.Exists(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Ensure monster has level component
                if (!EntityManager.HasComponent<MonsterLevelComponent>(targetEntity))
                {
                    // Initialize with level 1
                    ecb.AddComponent(targetEntity, new MonsterLevelComponent
                    {
                        level = 1,
                        experiencePoints = 0,
                        experienceToNextLevel = _config.GetExperienceToNextLevel(1),
                        totalExperienceGained = 0,
                        skillPointsAvailable = 0,
                        skillPointsSpent = 0,
                        prestigeLevel = 0,
                        prestigeMultiplier = 1.0f
                    });

                    ecb.AddComponent<LevelStatBonusComponent>(targetEntity);
                }

                // Award experience
                var levelData = EntityManager.GetComponentData<MonsterLevelComponent>(targetEntity);
                levelData.experiencePoints += experienceAmount;
                levelData.totalExperienceGained += experienceAmount;

                EntityManager.SetComponentData(targetEntity, levelData);

                // Update system stats
                IncrementTotalExperience(experienceAmount);

                Debug.Log($"Awarded {experienceAmount} XP to monster {targetEntity.Index}");

                // Remove request
                ecb.DestroyEntity(entity);
            }

            // ECB will be automatically played back by EndSimulationEntityCommandBufferSystem
        }

        /// <summary>
        /// Processes currency award requests
        /// Uses EntityCommandBufferSystem for optimized structural changes
        /// </summary>
        private void ProcessCurrencyAwards(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<AwardCurrencyRequest>>().WithEntityAccess())
            {
                var targetEntity = request.ValueRO.targetEntity;
                var currencyType = request.ValueRO.currencyType;
                var amount = request.ValueRO.amount;

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

                // Award currency
                var currency = EntityManager.GetComponentData<CurrencyComponent>(targetEntity);

                switch (currencyType)
                {
                    case CurrencyType.Coins:
                        currency.coins += Mathf.RoundToInt(amount * _config.coinRewardMultiplier);
                        break;
                    case CurrencyType.Gems:
                        currency.gems += amount;
                        break;
                    case CurrencyType.ActivityTokens:
                        currency.activityTokens += amount;
                        break;
                }

                EntityManager.SetComponentData(targetEntity, currency);

                // Update system stats
                IncrementTotalCurrency(amount);

                Debug.Log($"Awarded {amount} {currencyType} to monster {targetEntity.Index}");

                ecb.DestroyEntity(entity);
            }

            // ECB will be automatically played back by EndSimulationEntityCommandBufferSystem
        }

        /// <summary>
        /// Checks for level ups and applies bonuses
        /// Uses EntityCommandBufferSystem for optimized structural changes
        /// </summary>
        private void CheckForLevelUps(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            foreach (var (levelData, statBonus, entity) in
                SystemAPI.Query<RefRW<MonsterLevelComponent>, RefRW<LevelStatBonusComponent>>()
                .WithEntityAccess())
            {
                // Check if enough experience for level up
                while (levelData.ValueRO.experiencePoints >= levelData.ValueRO.experienceToNextLevel &&
                       levelData.ValueRO.level < _config.maxLevel)
                {
                    int oldLevel = levelData.ValueRO.level;
                    int newLevel = oldLevel + 1;

                    // Deduct experience
                    levelData.ValueRW.experiencePoints -= levelData.ValueRO.experienceToNextLevel;

                    // Increase level
                    levelData.ValueRW.level = newLevel;

                    // Update experience to next level
                    levelData.ValueRW.experienceToNextLevel = _config.GetExperienceToNextLevel(newLevel);

                    // Award skill points
                    int skillPointsAwarded = _config.skillPointsPerLevel;

                    // Bonus skill points at milestones
                    if (newLevel % _config.milestoneInterval == 0)
                    {
                        skillPointsAwarded += _config.bonusSkillPointsAtMilestone;

                        // Create milestone event
                        var milestoneEvent = EntityManager.CreateEntity();
                        ecb.AddComponent(milestoneEvent, new MilestoneReachedEvent
                        {
                            monsterEntity = entity,
                            milestoneLevel = newLevel,
                            rewardType = MilestoneRewardType.SkillPoints,
                            rewardValue = _config.bonusSkillPointsAtMilestone,
                            eventTime = currentTime
                        });
                    }

                    levelData.ValueRW.skillPointsAvailable += skillPointsAwarded;

                    // Update stat bonuses
                    float statBonus_Value = _config.GetStatBonusAtLevel(newLevel);
                    statBonus.ValueRW.strengthBonus = statBonus_Value;
                    statBonus.ValueRW.agilityBonus = statBonus_Value;
                    statBonus.ValueRW.intelligenceBonus = statBonus_Value;
                    statBonus.ValueRW.vitalityBonus = statBonus_Value;
                    statBonus.ValueRW.socialBonus = statBonus_Value;
                    statBonus.ValueRW.adaptabilityBonus = statBonus_Value;

                    // Create level up event
                    var levelUpEvent = EntityManager.CreateEntity();
                    ecb.AddComponent(levelUpEvent, new LevelUpEvent
                    {
                        monsterEntity = entity,
                        oldLevel = oldLevel,
                        newLevel = newLevel,
                        skillPointsAwarded = skillPointsAwarded,
                        eventTime = currentTime
                    });

                    // Update system stats
                    IncrementTotalLevels();

                    Debug.Log($"Monster {entity.Index} leveled up! {oldLevel} -> {newLevel} " +
                             $"(+{skillPointsAwarded} skill points)");
                }
            }

            // ECB will be automatically played back by EndSimulationEntityCommandBufferSystem
        }

        /// <summary>
        /// Processes skill unlock requests
        /// Uses EntityCommandBufferSystem for optimized structural changes
        /// </summary>
        private void ProcessSkillUnlocks(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<UnlockSkillRequest>>().WithEntityAccess())
            {
                var targetEntity = request.ValueRO.targetEntity;
                var skillId = request.ValueRO.skillId;

                if (!EntityManager.Exists(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Check if monster has level component
                if (!EntityManager.HasComponent<MonsterLevelComponent>(targetEntity))
                {
                    Debug.LogWarning($"Cannot unlock skill - monster has no level component");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var levelData = EntityManager.GetComponentData<MonsterLevelComponent>(targetEntity);

                // Skills cost 1 point per rank
                int skillCost = 1;

                if (levelData.skillPointsAvailable < skillCost)
                {
                    Debug.LogWarning($"Not enough skill points to unlock skill {skillId}");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Ensure skill buffer exists
                if (!EntityManager.HasBuffer<UnlockedSkillElement>(targetEntity))
                {
                    ecb.AddBuffer<UnlockedSkillElement>(targetEntity);
                }

                var skillBuffer = EntityManager.GetBuffer<UnlockedSkillElement>(targetEntity);

                // Check if skill already unlocked (upgrade rank)
                bool upgraded = false;
                for (int i = 0; i < skillBuffer.Length; i++)
                {
                    if (skillBuffer[i].skill.skillId == skillId)
                    {
                        var skill = skillBuffer[i].skill;
                        if (skill.currentRank < skill.maxRank)
                        {
                            skill.currentRank++;
                            skillBuffer[i] = new UnlockedSkillElement { skill = skill };
                            upgraded = true;
                            Debug.Log($"Upgraded skill {skillId} to rank {skill.currentRank}");
                        }
                        break;
                    }
                }

                // Unlock new skill if not upgraded
                if (!upgraded)
                {
                    var newSkill = new SkillNode
                    {
                        skillId = skillId,
                        skillName = $"Skill_{skillId}",
                        currentRank = 1,
                        maxRank = 5,
                        isUnlocked = true,
                        bonusPerRank = 0.02f
                    };
                    skillBuffer.Add(new UnlockedSkillElement { skill = newSkill });
                    Debug.Log($"Unlocked new skill {skillId}");
                }

                // Deduct skill points
                levelData.skillPointsAvailable -= skillCost;
                levelData.skillPointsSpent += skillCost;
                EntityManager.SetComponentData(targetEntity, levelData);

                ecb.DestroyEntity(entity);
            }

            // ECB will be automatically played back by EndSimulationEntityCommandBufferSystem
        }

        /// <summary>
        /// Updates daily challenge progress and handles expiration/refresh
        /// Uses EntityCommandBufferSystem for optimized structural changes
        /// </summary>
        private void UpdateDailyChallenges(float currentTime)
        {
            var ecb = _endSimulationECBSystem.CreateCommandBuffer();

            // Update all entities with daily challenges
            foreach (var (challengeBuffer, entity) in
                SystemAPI.Query<DynamicBuffer<DailyChallengeElement>>().WithEntityAccess())
            {
                bool hasExpiredChallenges = false;

                // Check each challenge for expiration
                for (int i = challengeBuffer.Length - 1; i >= 0; i--)
                {
                    var challenge = challengeBuffer[i].challenge;

                    // Check if challenge has expired
                    if (challenge.expirationTime > 0 && currentTime >= challenge.expirationTime)
                    {
                        if (!challenge.isCompleted)
                        {
                            Debug.Log($"Daily challenge {challenge.challengeId} expired without completion");
                        }

                        // Remove expired challenge
                        challengeBuffer.RemoveAt(i);
                        hasExpiredChallenges = true;
                    }
                }

                // Refresh challenges if expired or none exist
                if (hasExpiredChallenges || challengeBuffer.Length == 0)
                {
                    RefreshDailyChallenges(entity, challengeBuffer, currentTime, ecb);
                }
            }

            // ECB will be automatically played back by EndSimulationEntityCommandBufferSystem
        }

        /// <summary>
        /// Refreshes daily challenges for an entity
        /// </summary>
        private void RefreshDailyChallenges(Entity entity, DynamicBuffer<DailyChallengeElement> challengeBuffer, float currentTime, EntityCommandBuffer ecb)
        {
            // Generate new daily challenges (24 hour expiration)
            float expirationTime = currentTime + (24f * 3600f); // 24 hours

            // Clear existing challenges
            challengeBuffer.Clear();

            // Create new challenges (simplified - would pull from challenge pool in full implementation)
            for (int i = 0; i < _config.dailyChallengesCount; i++)
            {
                var newChallenge = new DailyChallenge
                {
                    challengeId = UnityEngine.Random.Range(1000, 9999),
                    description = $"Complete {3 + i} activities",
                    targetActivity = (ActivityType)(i % 3 + 1), // Rotate through Racing, Combat, Puzzle
                    targetCount = 3 + i,
                    currentProgress = 0,
                    isCompleted = false,
                    rewardCoins = _config.dailyChallengeCoins,
                    rewardTokens = _config.dailyChallengeTokens,
                    rewardExperience = _config.dailyChallengeExperience,
                    expirationTime = expirationTime
                };

                challengeBuffer.Add(new DailyChallengeElement { challenge = newChallenge });
            }

            Debug.Log($"Refreshed {_config.dailyChallengesCount} daily challenges for entity {entity.Index}");
        }

        /// <summary>
        /// Updates daily challenge progress when activity completes
        /// </summary>
        public void UpdateChallengeProgress(Entity entity, ActivityType activityType)
        {
            if (!EntityManager.HasBuffer<DailyChallengeElement>(entity))
                return;

            var challengeBuffer = EntityManager.GetBuffer<DailyChallengeElement>(entity);
            bool challengeCompleted = false;

            for (int i = 0; i < challengeBuffer.Length; i++)
            {
                var challenge = challengeBuffer[i].challenge;

                // Check if this challenge matches the activity
                if (!challenge.isCompleted &&
                    (challenge.targetActivity == activityType || challenge.targetActivity == ActivityType.None))
                {
                    challenge.currentProgress++;

                    // Check if challenge is now complete
                    if (challenge.currentProgress >= challenge.targetCount)
                    {
                        challenge.isCompleted = true;
                        challengeCompleted = true;

                        // Award challenge rewards
                        var coinRequest = EntityManager.CreateEntity();
                        EntityManager.AddComponentData(coinRequest, new AwardCurrencyRequest
                        {
                            targetEntity = entity,
                            currencyType = CurrencyType.Coins,
                            amount = challenge.rewardCoins,
                            requestTime = (float)SystemAPI.Time.ElapsedTime
                        });

                        var tokenRequest = EntityManager.CreateEntity();
                        EntityManager.AddComponentData(tokenRequest, new AwardCurrencyRequest
                        {
                            targetEntity = entity,
                            currencyType = CurrencyType.ActivityTokens,
                            amount = challenge.rewardTokens,
                            requestTime = (float)SystemAPI.Time.ElapsedTime
                        });

                        var expRequest = EntityManager.CreateEntity();
                        EntityManager.AddComponentData(expRequest, new AwardExperienceRequest
                        {
                            targetEntity = entity,
                            experienceAmount = challenge.rewardExperience,
                            requestTime = (float)SystemAPI.Time.ElapsedTime
                        });

                        Debug.Log($"Daily challenge completed! Rewards: {challenge.rewardCoins} coins, {challenge.rewardTokens} tokens, {challenge.rewardExperience} XP");
                    }

                    challengeBuffer[i] = new DailyChallengeElement { challenge = challenge };
                }
            }
        }

        /// <summary>
        /// Increments total experience counter
        /// </summary>
        private void IncrementTotalExperience(int amount)
        {
            foreach (var systemData in SystemAPI.Query<RefRW<ProgressionSystemData>>())
            {
                systemData.ValueRW.totalExperienceDistributed += amount;
            }
        }

        /// <summary>
        /// Increments total currency counter
        /// </summary>
        private void IncrementTotalCurrency(int amount)
        {
            foreach (var systemData in SystemAPI.Query<RefRW<ProgressionSystemData>>())
            {
                systemData.ValueRW.totalCurrencyDistributed += amount;
            }
        }

        /// <summary>
        /// Increments total levels counter
        /// </summary>
        private void IncrementTotalLevels()
        {
            foreach (var systemData in SystemAPI.Query<RefRW<ProgressionSystemData>>())
            {
                systemData.ValueRW.totalLevelsGained++;
            }
        }

        /// <summary>
        /// Creates an experience award request
        /// </summary>
        public Entity CreateExperienceAward(Entity targetEntity, int experienceAmount)
        {
            var requestEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(requestEntity, new AwardExperienceRequest
            {
                targetEntity = targetEntity,
                experienceAmount = experienceAmount,
                requestTime = (float)SystemAPI.Time.ElapsedTime
            });

            return requestEntity;
        }

        /// <summary>
        /// Creates a currency award request
        /// </summary>
        public Entity CreateCurrencyAward(Entity targetEntity, CurrencyType currencyType, int amount)
        {
            var requestEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(requestEntity, new AwardCurrencyRequest
            {
                targetEntity = targetEntity,
                currencyType = currencyType,
                amount = amount,
                requestTime = (float)SystemAPI.Time.ElapsedTime
            });

            return requestEntity;
        }

        /// <summary>
        /// Gets progression configuration
        /// </summary>
        public ProgressionConfig GetConfig()
        {
            return _config;
        }
    }
}
