using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;

namespace Laboratory.Core.MonsterTown.Systems
{
    /// <summary>
    /// Reward System - manages achievements, rewards, and progression
    /// Handles daily rewards, achievement tracking, and milestone rewards
    /// </summary>
    public class RewardSystem : MonoBehaviour
    {
        [Header("Daily Rewards")]
        [SerializeField] private bool enableDailyRewards = true;
        [SerializeField] private int dailyRewardCoins = 100;
        [SerializeField] private int dailyRewardGems = 2;

        [Header("Achievement Rewards")]
        [SerializeField] private bool enableAchievements = true;
        [SerializeField] private AchievementConfig[] achievements;

        [Header("Activity Rewards")]
        [SerializeField] private float baseActivityReward = 10f;
        [SerializeField] private float breedingReward = 50f;
        [SerializeField] private float careReward = 5f;

        // System dependencies
        private IEventBus eventBus;
        private IResourceManager resourceManager;

        // Reward tracking
        private Dictionary<string, AchievementProgress> achievementProgress = new();
        private Dictionary<string, DateTime> lastDailyReward = new();
        private Dictionary<string, int> activityCounts = new();

        #region Unity Lifecycle

        private void Awake()
        {
            eventBus = ServiceContainer.Instance?.ResolveService<IEventBus>();
            resourceManager = ServiceContainer.Instance?.ResolveService<IResourceManager>();
        }

        private void Start()
        {
            InitializeAchievements();
            SubscribeToEvents();

            if (enableDailyRewards)
            {
                InvokeRepeating(nameof(CheckDailyRewards), 60f, 300f); // Check every 5 minutes
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Claim daily reward for player
        /// </summary>
        public bool ClaimDailyReward(string playerId)
        {
            if (!enableDailyRewards) return false;

            var today = DateTime.Now.Date;
            if (lastDailyReward.TryGetValue(playerId, out var lastClaim))
            {
                if (lastClaim.Date >= today)
                {
                    Debug.LogWarning($"Daily reward already claimed today for player {playerId}");
                    return false;
                }
            }

            // Award daily rewards
            var reward = new TownResources
            {
                coins = dailyRewardCoins,
                gems = dailyRewardGems
            };

            if (resourceManager != null)
            {
                resourceManager.AddResources(reward);
            }

            lastDailyReward[playerId] = today;

            eventBus?.Publish(new DailyRewardClaimedEvent(playerId, reward));

            Debug.Log($"🎁 Daily reward claimed: {dailyRewardCoins} coins, {dailyRewardGems} gems");
            return true;
        }

        /// <summary>
        /// Award reward for activity completion
        /// </summary>
        public void AwardActivityReward(string activityType, float performanceMultiplier = 1f)
        {
            float rewardAmount = baseActivityReward * performanceMultiplier;

            var reward = new TownResources
            {
                coins = Mathf.RoundToInt(rewardAmount),
                gems = UnityEngine.Random.Range(0, 3) == 0 ? 1 : 0 // 33% chance for 1 gem
            };

            if (resourceManager != null)
            {
                resourceManager.AddResources(reward);
            }

            // Track activity for achievements
            if (!activityCounts.ContainsKey(activityType))
                activityCounts[activityType] = 0;
            activityCounts[activityType]++;

            CheckAchievements();

            eventBus?.Publish(new ActivityRewardEvent(activityType, reward, performanceMultiplier));

            Debug.Log($"🏆 Activity reward: {reward.coins} coins for {activityType}");
        }

        /// <summary>
        /// Award reward for breeding success
        /// </summary>
        public void AwardBreedingReward(MonsterInstance parent1, MonsterInstance parent2, MonsterInstance offspring)
        {
            var reward = new TownResources
            {
                coins = Mathf.RoundToInt(breedingReward),
                gems = UnityEngine.Random.Range(1, 4) // 1-3 gems for breeding
            };

            // Bonus for rare offspring
            if (offspring.Genetics.GetOverallFitness() > 0.8f)
            {
                reward.gems += 2;
                reward.coins += 50;
            }

            if (resourceManager != null)
            {
                resourceManager.AddResources(reward);
            }

            // Track for achievements
            if (!activityCounts.ContainsKey("breeding"))
                activityCounts["breeding"] = 0;
            activityCounts["breeding"]++;

            CheckAchievements();

            eventBus?.Publish(new BreedingRewardEvent(offspring, reward));

            Debug.Log($"🧬 Breeding reward: {reward.coins} coins, {reward.gems} gems");
        }

        /// <summary>
        /// Award reward for monster care
        /// </summary>
        public void AwardCareReward(MonsterInstance monster, string careAction)
        {
            var reward = new TownResources
            {
                coins = Mathf.RoundToInt(careReward),
                gems = 0
            };

            if (resourceManager != null)
            {
                resourceManager.AddResources(reward);
            }

            // Track for achievements
            string activityKey = $"care_{careAction}";
            if (!activityCounts.ContainsKey(activityKey))
                activityCounts[activityKey] = 0;
            activityCounts[activityKey]++;

            CheckAchievements();

            eventBus?.Publish(new CareRewardEvent(monster, careAction, reward));

            Debug.Log($"💖 Care reward: {reward.coins} coins for {careAction}");
        }

        /// <summary>
        /// Get achievement progress
        /// </summary>
        public List<AchievementProgress> GetAchievementProgress()
        {
            return new List<AchievementProgress>(achievementProgress.Values);
        }

        /// <summary>
        /// Check if daily reward is available
        /// </summary>
        public bool IsDailyRewardAvailable(string playerId)
        {
            if (!enableDailyRewards) return false;

            var today = DateTime.Now.Date;
            if (lastDailyReward.TryGetValue(playerId, out var lastClaim))
            {
                return lastClaim.Date < today;
            }

            return true;
        }

        /// <summary>
        /// Grant rewards from activity result to the player
        /// </summary>
        public void GrantRewards(TownResources resources)
        {
            if (resourceManager != null)
            {
                resourceManager.AddResources(resources);
            }

            eventBus?.Publish(new RewardsGrantedEvent(resources));
            Debug.Log($"💰 Rewards granted: {resources.coins} coins, {resources.gems} gems, {resources.activityTokens} tokens");
        }

        /// <summary>
        /// Check if player can afford a cost
        /// </summary>
        public bool CanAfford(CurrencyAmount cost)
        {
            if (resourceManager == null) return false;

            var currentResources = resourceManager.GetCurrentResources();
            return currentResources.coins >= cost.Coins &&
                   currentResources.gems >= cost.Gems &&
                   currentResources.activityTokens >= cost.ActivityTokens;
        }

        /// <summary>
        /// Spend currency on something
        /// </summary>
        public bool SpendCurrency(CurrencyAmount cost)
        {
            if (!CanAfford(cost)) return false;

            var spendResources = new TownResources
            {
                coins = -cost.Coins,
                gems = -cost.Gems,
                activityTokens = -cost.ActivityTokens
            };

            if (resourceManager != null)
            {
                resourceManager.AddResources(spendResources);
            }

            eventBus?.Publish(new CurrencySpentEvent(cost));
            Debug.Log($"💸 Currency spent: {cost.Coins} coins, {cost.Gems} gems, {cost.ActivityTokens} tokens");
            return true;
        }

        /// <summary>
        /// Process passive rewards based on town facilities and time
        /// </summary>
        public void ProcessPassiveRewards(PlayerTown playerTown)
        {
            if (playerTown?.Facilities == null || playerTown.Facilities.Count == 0) return;

            var passiveRewards = new TownResources();

            // Calculate passive income from facilities
            foreach (var facility in playerTown.Facilities)
            {
                if (!facility.IsConstructed) continue;

                var facilityReward = GetFacilityPassiveReward(facility);
                passiveRewards.coins += facilityReward.coins;
                passiveRewards.gems += facilityReward.gems;
                passiveRewards.activityTokens += facilityReward.activityTokens;
            }

            // Town level bonus
            var townLevelBonus = playerTown.Level * 2;
            passiveRewards.coins += townLevelBonus;

            if (passiveRewards.coins > 0 || passiveRewards.gems > 0 || passiveRewards.activityTokens > 0)
            {
                GrantRewards(passiveRewards);
                Debug.Log($"🏘️ Passive town rewards: {passiveRewards.coins} coins, {passiveRewards.gems} gems");
            }
        }

        #endregion

        #region Private Methods

        private void InitializeAchievements()
        {
            if (!enableAchievements) return;

            if (achievements == null || achievements.Length == 0)
            {
                // Create default achievements
                achievements = new AchievementConfig[]
                {
                    new AchievementConfig
                    {
                        id = "first_breeding",
                        name = "First Steps",
                        description = "Successfully breed your first monster",
                        category = AchievementCategory.Breeding,
                        targetValue = 1,
                        activityType = "breeding",
                        reward = new TownResources { coins = 200, gems = 5 }
                    },
                    new AchievementConfig
                    {
                        id = "master_breeder",
                        name = "Master Breeder",
                        description = "Breed 50 monsters",
                        category = AchievementCategory.Breeding,
                        targetValue = 50,
                        activityType = "breeding",
                        reward = new TownResources { coins = 2000, gems = 50 }
                    },
                    new AchievementConfig
                    {
                        id = "caring_owner",
                        name = "Caring Owner",
                        description = "Feed monsters 100 times",
                        category = AchievementCategory.Care,
                        targetValue = 100,
                        activityType = "care_feeding",
                        reward = new TownResources { coins = 500, gems = 10 }
                    },
                    new AchievementConfig
                    {
                        id = "activity_champion",
                        name = "Activity Champion",
                        description = "Complete 100 activities",
                        category = AchievementCategory.Activities,
                        targetValue = 100,
                        activityType = "racing", // This will check total across all activity types
                        reward = new TownResources { coins = 1000, gems = 25 }
                    }
                };
            }

            // Initialize progress tracking
            foreach (var achievement in achievements)
            {
                if (!achievementProgress.ContainsKey(achievement.id))
                {
                    achievementProgress[achievement.id] = new AchievementProgress
                    {
                        achievementId = achievement.id,
                        currentProgress = 0,
                        isCompleted = false,
                        completedDate = DateTime.MinValue
                    };
                }
            }
        }

        private void SubscribeToEvents()
        {
            if (eventBus == null) return;

            eventBus.Subscribe<BreedingSuccessfulEvent>(OnBreedingSuccess);
            eventBus.Subscribe<MonsterFedEvent>(OnMonsterFed);
            eventBus.Subscribe<MonsterPlayedEvent>(OnMonsterPlayed);
        }

        private void CheckAchievements()
        {
            if (!enableAchievements || achievements == null) return;

            foreach (var achievement in achievements)
            {
                if (!achievementProgress.TryGetValue(achievement.id, out var progress))
                    continue;

                if (progress.isCompleted) continue;

                // Check progress based on activity type
                int currentCount = 0;
                if (achievement.activityType == "racing") // Special case for total activities
                {
                    foreach (var activityCount in activityCounts.Values)
                    {
                        currentCount += activityCount;
                    }
                }
                else if (activityCounts.TryGetValue(achievement.activityType, out var count))
                {
                    currentCount = count;
                }

                progress.currentProgress = currentCount;

                // Check if achievement is completed
                if (currentCount >= achievement.targetValue && !progress.isCompleted)
                {
                    CompleteAchievement(achievement, progress);
                }
            }
        }

        private void CompleteAchievement(AchievementConfig achievement, AchievementProgress progress)
        {
            progress.isCompleted = true;
            progress.completedDate = DateTime.Now;

            // Award achievement reward
            if (resourceManager != null)
            {
                resourceManager.AddResources(achievement.reward);
            }

            eventBus?.Publish(new AchievementCompletedEvent(achievement, achievement.reward));

            Debug.Log($"🏅 Achievement completed: {achievement.name} - Reward: {achievement.reward.coins} coins, {achievement.reward.gems} gems");
        }

        private void CheckDailyRewards()
        {
            // This could be expanded to automatically notify players about available daily rewards
            var playerIds = new List<string> { "player1" }; // This would come from player management system

            foreach (var playerId in playerIds)
            {
                if (IsDailyRewardAvailable(playerId))
                {
                    eventBus?.Publish(new DailyRewardAvailableEvent(playerId));
                }
            }
        }

        /// <summary>
        /// Calculate passive rewards for a facility
        /// </summary>
        private TownResources GetFacilityPassiveReward(TownFacility facility)
        {
            var reward = new TownResources();

            switch (facility.Type)
            {
                case FacilityType.BreedingCenter:
                    reward.coins = facility.Level * 10;
                    break;

                case FacilityType.TrainingGround:
                    reward.coins = facility.Level * 8;
                    reward.activityTokens = facility.Level;
                    break;

                case FacilityType.ActivityCenter:
                    reward.coins = facility.Level * 15;
                    reward.activityTokens = facility.Level * 2;
                    break;

                case FacilityType.ResearchLab:
                    reward.coins = facility.Level * 5;
                    reward.gems = facility.Level > 2 ? 1 : 0;
                    break;

                case FacilityType.MonsterHabitat:
                    reward.coins = facility.Level * 12;
                    break;

                case FacilityType.EquipmentShop:
                    reward.coins = facility.Level * 20;
                    break;

                case FacilityType.SocialHub:
                    reward.coins = facility.Level * 6;
                    reward.activityTokens = facility.Level;
                    break;

                case FacilityType.EducationCenter:
                    reward.coins = facility.Level * 7;
                    reward.gems = facility.Level > 3 ? 1 : 0;
                    break;
            }

            return reward;
        }

        // Event Handlers
        private void OnBreedingSuccess(BreedingSuccessfulEvent evt)
        {
            AwardBreedingReward(evt.Parent1, evt.Parent2, evt.Offspring);
        }

        private void OnMonsterFed(MonsterFedEvent evt)
        {
            AwardCareReward(evt.Monster, "feeding");
        }

        private void OnMonsterPlayed(MonsterPlayedEvent evt)
        {
            AwardCareReward(evt.Monster, "playing");
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class AchievementConfig
    {
        public string id;
        public string name;
        public string description;
        public AchievementCategory category;
        public int targetValue;
        public string activityType;
        public TownResources reward;
    }

    [System.Serializable]
    public class AchievementProgress
    {
        public string achievementId;
        public int currentProgress;
        public bool isCompleted;
        public DateTime completedDate;

        public float GetProgressPercentage(int targetValue)
        {
            return Mathf.Clamp01((float)currentProgress / targetValue);
        }
    }

    public enum AchievementCategory
    {
        Breeding,
        Care,
        Activities,
        Collection,
        Social,
        Progression
    }

    // Reward Events
    public class DailyRewardClaimedEvent
    {
        public string PlayerId { get; }
        public TownResources Reward { get; }
        public DateTime Timestamp { get; }

        public DailyRewardClaimedEvent(string playerId, TownResources reward)
        {
            PlayerId = playerId;
            Reward = reward;
            Timestamp = DateTime.Now;
        }
    }

    public class ActivityRewardEvent
    {
        public string ActivityType { get; }
        public TownResources Reward { get; }
        public float PerformanceMultiplier { get; }
        public DateTime Timestamp { get; }

        public ActivityRewardEvent(string activityType, TownResources reward, float performanceMultiplier)
        {
            ActivityType = activityType;
            Reward = reward;
            PerformanceMultiplier = performanceMultiplier;
            Timestamp = DateTime.Now;
        }
    }

    public class BreedingRewardEvent
    {
        public MonsterInstance Offspring { get; }
        public TownResources Reward { get; }
        public DateTime Timestamp { get; }

        public BreedingRewardEvent(MonsterInstance offspring, TownResources reward)
        {
            Offspring = offspring;
            Reward = reward;
            Timestamp = DateTime.Now;
        }
    }

    public class CareRewardEvent
    {
        public MonsterInstance Monster { get; }
        public string CareAction { get; }
        public TownResources Reward { get; }
        public DateTime Timestamp { get; }

        public CareRewardEvent(MonsterInstance monster, string careAction, TownResources reward)
        {
            Monster = monster;
            CareAction = careAction;
            Reward = reward;
            Timestamp = DateTime.Now;
        }
    }

    public class AchievementCompletedEvent
    {
        public AchievementConfig Achievement { get; }
        public TownResources Reward { get; }
        public DateTime Timestamp { get; }

        public AchievementCompletedEvent(AchievementConfig achievement, TownResources reward)
        {
            Achievement = achievement;
            Reward = reward;
            Timestamp = DateTime.Now;
        }
    }

    public class DailyRewardAvailableEvent
    {
        public string PlayerId { get; }
        public DateTime Timestamp { get; }

        public DailyRewardAvailableEvent(string playerId)
        {
            PlayerId = playerId;
            Timestamp = DateTime.Now;
        }
    }

    public class RewardsGrantedEvent
    {
        public TownResources Resources { get; }
        public DateTime Timestamp { get; }

        public RewardsGrantedEvent(TownResources resources)
        {
            Resources = resources;
            Timestamp = DateTime.Now;
        }
    }

    public class CurrencySpentEvent
    {
        public CurrencyAmount Cost { get; }
        public DateTime Timestamp { get; }

        public CurrencySpentEvent(CurrencyAmount cost)
        {
            Cost = cost;
            Timestamp = DateTime.Now;
        }
    }

    #endregion
}