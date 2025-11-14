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

        private static readonly ProfilerMarker s_ProcessExperienceMarker =
            new ProfilerMarker("Progression.ProcessExperience");
        private static readonly ProfilerMarker s_ProcessCurrencyMarker =
            new ProfilerMarker("Progression.ProcessCurrency");
        private static readonly ProfilerMarker s_UpdateLevelsMarker =
            new ProfilerMarker("Progression.UpdateLevels");

        protected override void OnCreate()
        {
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
            ProcessSkillUnlocks(currentTime);

            // Update daily challenges
            UpdateDailyChallenges(currentTime);
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
        /// </summary>
        private void ProcessExperienceAwards(float currentTime)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

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

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// Processes currency award requests
        /// </summary>
        private void ProcessCurrencyAwards(float currentTime)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

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

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// Checks for level ups and applies bonuses
        /// </summary>
        private void CheckForLevelUps(float currentTime)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

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

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// Processes skill unlock requests
        /// </summary>
        private void ProcessSkillUnlocks(float currentTime)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

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

                // TODO: Implement skill tree logic
                // For now, just consume the request

                Debug.Log($"Skill unlock request for skill {skillId}");

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// Updates daily challenge progress
        /// </summary>
        private void UpdateDailyChallenges(float currentTime)
        {
            // Daily challenges updated when activities complete
            // This is a placeholder for challenge expiration/refresh logic
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
