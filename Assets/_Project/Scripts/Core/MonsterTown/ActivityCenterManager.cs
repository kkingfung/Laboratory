using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Activity Center Manager - Handles all mini-game activities where monsters participate
    ///
    /// Key Features:
    /// - 10+ different activity types (FPS, Racing, Puzzle, Strategy, etc.)
    /// - Monster genetics directly affect performance in each activity
    /// - Real-time mini-games with genetic-based outcomes
    /// - Progressive difficulty and reward systems
    /// - Cross-activity skill development (racing skills help in combat, etc.)
    /// - Educational content explaining how genetics affect performance
    /// </summary>
    public class ActivityCenterManager : MonoBehaviour
    {
        [Header("Activity Configuration")]
        [SerializeField] private ActivityConfig[] availableActivities;
        [SerializeField] private bool enableCrossActivityProgression = true;
        [SerializeField] private bool showEducationalContent = true;

        [Header("Performance Settings")]
        [SerializeField] private int maxSimultaneousActivities = 5;
        [SerializeField] private float activityUpdateFrequency = 0.1f;

        // Activity systems
        private Dictionary<ActivityType, IActivityMiniGame> _activitySystems = new();
        private Dictionary<string, ActiveParticipation> _activeParticipations = new();
        private Queue<PendingActivity> _activityQueue = new();

        #region Activity System Initialization

        public async Task InitializeActivities(ActivityType[] activityTypes)
        {
            Debug.Log("🎮 Initializing Activity Centers...");

            foreach (var activityType in activityTypes)
            {
                await InitializeActivityType(activityType);
            }

            // Start activity processing
            InvokeRepeating(nameof(ProcessActivityQueue), 1f, activityUpdateFrequency);

            Debug.Log($"✅ {_activitySystems.Count} Activity Centers ready!");
        }

        private async Task InitializeActivityType(ActivityType activityType)
        {
            IActivityMiniGame activitySystem = activityType switch
            {
                ActivityType.Racing => new RacingActivity(),
                ActivityType.Combat => new CombatActivity(),
                ActivityType.Puzzle => new PuzzleActivity(),
                ActivityType.Strategy => new StrategyActivity(),
                ActivityType.Adventure => new AdventureActivity(),
                ActivityType.Platforming => new PlatformingActivity(),
                ActivityType.Music => new MusicActivity(),
                ActivityType.Crafting => new CraftingActivity(),
                ActivityType.Exploration => new ExplorationActivity(),
                ActivityType.Social => new SocialActivity(),
                _ => throw new NotImplementedException($"Activity type {activityType} not implemented")
            };

            await activitySystem.InitializeAsync();
            _activitySystems[activityType] = activitySystem;

            Debug.Log($"📍 {activityType} Activity Center initialized");
        }

        #endregion

        #region Activity Participation

        /// <summary>
        /// Check if a monster can participate in a specific activity
        /// </summary>
        public bool CanParticipateInActivity(Monster monster, ActivityType activityType)
        {
            if (!_activitySystems.ContainsKey(activityType))
                return false;

            // Check activity requirements
            var requirements = GetActivityRequirements(activityType);
            return monster.MeetsRequirements(requirements);
        }

        /// <summary>
        /// Run an activity mini-game with a monster
        /// </summary>
        public async Task<ActivityResult> RunActivity(Monster monster, ActivityType activityType, MonsterPerformance performance)
        {
            if (!_activitySystems.TryGetValue(activityType, out var activitySystem))
            {
                Debug.LogError($"Activity system for {activityType} not found");
                return new ActivityResult
                {
                    IsSuccess = false,
                    ActivityType = activityType,
                    ResultMessage = $"Activity system for {activityType} not found",
                    FailureReason = "System not available"
                };
            }

            Debug.Log($"🎯 {monster.Name} starting {activityType} activity...");

            // Create activity session
            var session = new ActivitySession
            {
                Monster = monster,
                ActivityType = activityType,
                Performance = performance,
                StartTime = DateTime.Now
            };

            // Run the mini-game
            var result = await activitySystem.RunActivityAsync(session);

            // Process cross-activity progression
            if (enableCrossActivityProgression)
            {
                ApplyCrossActivityBenefits(monster, result);
            }

            // Generate educational content
            if (showEducationalContent)
            {
                result.EducationalContent = GenerateEducationalContent(activityType, result);
            }

            Debug.Log($"🏆 {monster.Name} completed {activityType}: {result.PerformanceRating:F2} score, {result.ExperienceGained} XP");

            return result;
        }

        /// <summary>
        /// Add activity to processing queue (for non-blocking execution)
        /// </summary>
        public void QueueActivity(Monster monster, ActivityType activityType, MonsterPerformance performance)
        {
            _activityQueue.Enqueue(new PendingActivity
            {
                Monster = monster,
                ActivityType = activityType,
                Performance = performance,
                QueueTime = DateTime.Now
            });
        }

        #endregion

        #region Activity Processing

        public void UpdateActivities(float deltaTime)
        {
            // Update active participations
            var completedActivities = new List<string>();

            foreach (var kvp in _activeParticipations)
            {
                var participation = kvp.Value;
                participation.ElapsedTime += deltaTime;

                // Check if activity is complete
                if (participation.ElapsedTime >= participation.Duration)
                {
                    completedActivities.Add(kvp.Key);
                }
            }

            // Process completed activities
            foreach (var activityId in completedActivities)
            {
                CompleteActivity(activityId);
            }
        }

        private void ProcessActivityQueue()
        {
            // Process pending activities if we have capacity
            while (_activityQueue.Count > 0 && _activeParticipations.Count < maxSimultaneousActivities)
            {
                var pendingActivity = _activityQueue.Dequeue();
                StartActivityAsync(pendingActivity);
            }
        }

        private async void StartActivityAsync(PendingActivity pendingActivity)
        {
            var result = await RunActivity(pendingActivity.Monster, pendingActivity.ActivityType, pendingActivity.Performance);
            // Process result (this would typically notify the main game system)
            ProcessActivityCompletion(pendingActivity.Monster, result);
        }

        private void CompleteActivity(string activityId)
        {
            if (_activeParticipations.TryGetValue(activityId, out var participation))
            {
                _activeParticipations.Remove(activityId);
                Debug.Log($"⏰ Activity {participation.ActivityType} completed for {participation.Monster.Name}");
            }
        }

        #endregion

        #region Cross-Activity Progression

        /// <summary>
        /// Apply benefits from one activity to other activities
        /// Example: Racing improves agility which helps in combat
        /// </summary>
        private void ApplyCrossActivityBenefits(Monster monster, ActivityResult result)
        {
            var crossBenefits = CalculateCrossActivityBenefits(result.ActivityType, result.PerformanceRating);

            foreach (var benefit in crossBenefits)
            {
                monster.ApplyCrossActivityBonus(benefit.TargetActivity, benefit.BonusAmount);
            }

            if (crossBenefits.Count > 0)
            {
                Debug.Log($"🔄 {monster.Name} gained cross-activity benefits from {result.ActivityType}");
            }
        }

        /// <summary>
        /// Calculate how performance in one activity benefits others
        /// </summary>
        private List<CrossActivityBenefit> CalculateCrossActivityBenefits(ActivityType sourceActivity, float performanceScore)
        {
            var benefits = new List<CrossActivityBenefit>();
            var bonusStrength = performanceScore * 0.1f; // 10% of performance score

            switch (sourceActivity)
            {
                case ActivityType.Racing:
                    // Racing improves agility-based activities
                    benefits.Add(new CrossActivityBenefit { TargetActivity = ActivityType.Combat, BonusAmount = bonusStrength * 0.5f });
                    benefits.Add(new CrossActivityBenefit { TargetActivity = ActivityType.Platforming, BonusAmount = bonusStrength * 0.7f });
                    break;

                case ActivityType.Combat:
                    // Combat improves strength and reaction-based activities
                    benefits.Add(new CrossActivityBenefit { TargetActivity = ActivityType.Racing, BonusAmount = bonusStrength * 0.3f });
                    benefits.Add(new CrossActivityBenefit { TargetActivity = ActivityType.Adventure, BonusAmount = bonusStrength * 0.6f });
                    break;

                case ActivityType.Puzzle:
                    // Puzzle solving improves intelligence-based activities
                    benefits.Add(new CrossActivityBenefit { TargetActivity = ActivityType.Strategy, BonusAmount = bonusStrength * 0.8f });
                    benefits.Add(new CrossActivityBenefit { TargetActivity = ActivityType.Crafting, BonusAmount = bonusStrength * 0.4f });
                    break;

                case ActivityType.Strategy:
                    // Strategy improves planning and tactical activities
                    benefits.Add(new CrossActivityBenefit { TargetActivity = ActivityType.Combat, BonusAmount = bonusStrength * 0.5f });
                    benefits.Add(new CrossActivityBenefit { TargetActivity = ActivityType.Adventure, BonusAmount = bonusStrength * 0.4f });
                    break;

                case ActivityType.Music:
                    // Music improves rhythm and coordination
                    benefits.Add(new CrossActivityBenefit { TargetActivity = ActivityType.Racing, BonusAmount = bonusStrength * 0.3f });
                    benefits.Add(new CrossActivityBenefit { TargetActivity = ActivityType.Platforming, BonusAmount = bonusStrength * 0.5f });
                    break;
            }

            return benefits;
        }

        #endregion

        #region Educational Content Generation

        /// <summary>
        /// Generate educational content explaining how genetics affected performance
        /// </summary>
        private string GenerateEducationalContent(ActivityType activityType, ActivityResult result)
        {
            return activityType switch
            {
                ActivityType.Racing => GenerateRacingEducation(result),
                ActivityType.Combat => GenerateCombatEducation(result),
                ActivityType.Puzzle => GeneratePuzzleEducation(result),
                ActivityType.Strategy => GenerateStrategyEducation(result),
                ActivityType.Music => GenerateMusicEducation(result),
                _ => GenerateGenericEducation(activityType, result)
            };
        }

        private string GenerateRacingEducation(ActivityResult result)
        {
            if (result.PerformanceRating > 0.8f)
            {
                return "🏃‍♂️ Excellent speed! Your monster's high Agility genetics gave it superior acceleration and cornering ability. " +
                       "In real life, athletes with fast-twitch muscle fibers (genetic trait) excel at sprinting and quick movements.";
            }
            else if (result.PerformanceRating > 0.5f)
            {
                return "🏃 Good endurance! Your monster's Vitality genetics helped it maintain speed throughout the race. " +
                       "This mirrors how some people have genetic advantages for endurance activities like marathon running.";
            }
            else
            {
                return "🐌 Room for improvement! Try breeding for higher Agility and Vitality stats. " +
                       "Genetic training can improve performance, just like how athletes can enhance their natural abilities through practice.";
            }
        }

        private string GenerateCombatEducation(ActivityResult result)
        {
            return result.PerformanceRating > 0.7f
                ? "⚔️ Strong performance! Your monster's Strength genetics provided powerful attacks, while good Vitality gave defensive resilience. " +
                  "This reflects how genetic factors influence muscle mass, bone density, and recovery rates in real combat sports."
                : "🛡️ Practice needed! Combat success depends on balanced Strength, Vitality, and Agility genetics. " +
                  "Professional fighters often have genetic advantages in reaction time, muscle composition, and injury recovery.";
        }

        private string GeneratePuzzleEducation(ActivityResult result)
        {
            return result.PerformanceRating > 0.75f
                ? "🧠 Brilliant thinking! Your monster's high Intelligence genetics enabled fast problem-solving and pattern recognition. " +
                  "Intelligence has both genetic and environmental components - nature provides the foundation, nurture develops the skills."
                : "🤔 Keep training that brain! Puzzle-solving improves with practice, building on genetic intelligence potential. " +
                  "Like humans, your monster's cognitive abilities can be enhanced through mental exercise and learning.";
        }

        private string GenerateStrategyEducation(ActivityResult result)
        {
            return result.PerformanceRating > 0.7f
                ? "🎯 Strategic mastery! Your monster's combination of Intelligence and Social genetics enabled excellent leadership and tactical planning. " +
                  "Great leaders often have genetic predispositions for analytical thinking and social awareness."
                : "📚 Strategy takes time to develop! Good strategic thinking builds on Intelligence genetics but requires experience. " +
                  "Military and business leaders develop their genetic potential through training and practice.";
        }

        private string GenerateMusicEducation(ActivityResult result)
        {
            return result.PerformanceRating > 0.7f
                ? "🎵 Perfect rhythm! Your monster's Agility and Intelligence genetics created excellent timing and coordination. " +
                  "Musical ability often runs in families, suggesting genetic components for rhythm, pitch recognition, and motor coordination."
                : "🎼 Music is learnable! While some genetic advantages exist for musical ability, practice and training can develop these skills. " +
                  "Your monster can improve its musical genetics through continued participation in rhythm activities.";
        }

        private string GenerateGenericEducation(ActivityType activityType, ActivityResult result)
        {
            return $"🧬 Activity completed! Your monster's genetic traits influenced its performance in {activityType}. " +
                   "Different genetics provide advantages in different activities - breed strategically to optimize for your favorite activities!";
        }

        #endregion

        #region Utility Methods

        private ActivityRequirements GetActivityRequirements(ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Racing => new ActivityRequirements { MinLevel = 1, RequiredStats = new[] { "Agility" } },
                ActivityType.Combat => new ActivityRequirements { MinLevel = 2, RequiredStats = new[] { "Strength", "Vitality" } },
                ActivityType.Puzzle => new ActivityRequirements { MinLevel = 1, RequiredStats = new[] { "Intelligence" } },
                ActivityType.Strategy => new ActivityRequirements { MinLevel = 3, RequiredStats = new[] { "Intelligence", "Social" } },
                _ => new ActivityRequirements { MinLevel = 1 }
            };
        }

        private void ProcessActivityCompletion(Monster monster, ActivityResult result)
        {
            // This would notify the main game system about activity completion
            // Implementation depends on your event system
        }

        #endregion
    }

    #region Activity Mini-Game Interfaces and Implementations

    /// <summary>
    /// Interface for all activity mini-games
    /// </summary>
    public interface IActivityMiniGame
    {
        Task InitializeAsync();
        Task<ActivityResult> RunActivityAsync(ActivitySession session);
        string GetActivityName();
        string GetActivityDescription();
    }

    /// <summary>
    /// Racing Activity - Speed-based competition
    /// </summary>
    public class RacingActivity : IActivityMiniGame
    {
        public async Task InitializeAsync()
        {
            // Initialize racing tracks, difficulty levels, etc.
            await Task.Delay(100); // Simulated initialization
        }

        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            var monster = session.Monster;
            var performance = session.Performance;

            // Simulate racing mini-game
            var trackDifficulty = UnityEngine.Random.Range(0.5f, 1.5f);
            var raceTime = CalculateRaceTime(performance, trackDifficulty);
            var success = raceTime < 60f; // Under 60 seconds = success

            var result = new ActivityResult
            {
                ActivityType = ActivityType.Racing,
                IsSuccess = success,
                PerformanceRating = Mathf.Clamp01(60f / raceTime), // Better time = higher score
                ExperienceGained = success ? 50 : 25,
                ResourcesEarned = ConvertRewardsToTownResources(GenerateRacingRewards(success, raceTime))
            };

            // Simulate activity duration
            await Task.Delay(UnityEngine.Random.Range(2000, 5000));

            return result;
        }

        private float CalculateRaceTime(MonsterPerformance performance, float trackDifficulty)
        {
            var baseTime = 80f;
            var speedFactor = performance.Speed;
            var enduranceFactor = performance.Endurance;
            var handlingFactor = performance.Handling;

            var adjustedTime = baseTime * trackDifficulty / (speedFactor * 0.5f + enduranceFactor * 0.3f + handlingFactor * 0.2f);
            return Mathf.Max(20f, adjustedTime); // Minimum 20 seconds
        }

        private List<Reward> GenerateRacingRewards(bool success, float raceTime)
        {
            var rewards = new List<Reward>();

            if (success)
            {
                rewards.Add(new Reward { Type = RewardType.Coins, Amount = 100 });
                rewards.Add(new Reward { Type = RewardType.ActivityTokens, Amount = 5 });

                if (raceTime < 40f) // Exceptional performance
                {
                    rewards.Add(new Reward { Type = RewardType.Equipment, ItemId = "SpeedBoots" });
                }
            }
            else
            {
                rewards.Add(new Reward { Type = RewardType.Coins, Amount = 25 }); // Participation reward
            }

            return rewards;
        }

        /// <summary>
        /// Convert List<Reward> to TownResources
        /// </summary>
        private TownResources ConvertRewardsToTownResources(List<Reward> rewards)
        {
            var townResources = new TownResources();

            foreach (var reward in rewards)
            {
                switch (reward.Type)
                {
                    case RewardType.Coins:
                        townResources.coins += reward.Amount;
                        break;
                    case RewardType.Gems:
                        townResources.gems += reward.Amount;
                        break;
                    case RewardType.ActivityTokens:
                        townResources.activityTokens += reward.Amount;
                        break;
                    case RewardType.GeneticSamples:
                        townResources.geneticSamples += reward.Amount;
                        break;
                    case RewardType.Materials:
                        townResources.materials += reward.Amount;
                        break;
                    case RewardType.Energy:
                        townResources.energy += reward.Amount;
                        break;
                }
            }

            return townResources;
        }

        public string GetActivityName() => "Racing Circuit";
        public string GetActivityDescription() => "Test your monster's speed and agility on various racing tracks";
    }

    /// <summary>
    /// Combat Activity - Fighting and battle skills
    /// </summary>
    public class CombatActivity : IActivityMiniGame
    {
        public async Task InitializeAsync()
        {
            await Task.Delay(100);
        }

        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            var performance = session.Performance;

            // Simulate combat encounter
            var opponentStrength = UnityEngine.Random.Range(0.3f, 1.2f);
            var combatSuccess = (performance.AttackPower + performance.Defense + performance.Agility) / 3f > opponentStrength;

            var result = new ActivityResult
            {
                ActivityType = ActivityType.Combat,
                IsSuccess = combatSuccess,
                PerformanceRating = Mathf.Clamp01((performance.AttackPower + performance.Defense) / 2f),
                ExperienceGained = combatSuccess ? 75 : 30,
                ResourcesEarned = new TownResources { coins = combatSuccess ? 100 : 25 },
                HappinessChange = combatSuccess ? 0.08f : -0.02f,
                ResultMessage = combatSuccess ? "Victory in combat!" : "Defeat in combat, but learned from the experience."
            };

            await Task.Delay(UnityEngine.Random.Range(3000, 6000));
            return result;
        }

        private List<Reward> GenerateCombatRewards(bool success, float attackPower)
        {
            var rewards = new List<Reward>();

            if (success)
            {
                rewards.Add(new Reward { Type = RewardType.Coins, Amount = 150 });
                rewards.Add(new Reward { Type = RewardType.ActivityTokens, Amount = 7 });

                if (attackPower > 0.8f)
                {
                    rewards.Add(new Reward { Type = RewardType.Equipment, ItemId = "CombatArmor" });
                }
            }
            else
            {
                rewards.Add(new Reward { Type = RewardType.Coins, Amount = 40 });
            }

            return rewards;
        }

        public string GetActivityName() => "Combat Arena";
        public string GetActivityDescription() => "Battle against opponents to test your monster's fighting abilities";
    }

    // Additional activity implementations would follow similar patterns...
    public class PuzzleActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);
        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            // Implement puzzle-solving mini-game
            await Task.Delay(UnityEngine.Random.Range(1000, 4000));
            return new ActivityResult { ActivityType = ActivityType.Puzzle, IsSuccess = true, PerformanceRating = 0.7f, ResourcesEarned = new TownResources { coins = 50 }, ExperienceGained = 40, HappinessChange = 0.07f, ResultMessage = "Great puzzle solving!" };
        }
        public string GetActivityName() => "Puzzle Academy";
        public string GetActivityDescription() => "Solve puzzles to develop your monster's intelligence";
    }

    public class StrategyActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);
        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(4000, 8000));
            return new ActivityResult { ActivityType = ActivityType.Strategy, IsSuccess = true, PerformanceRating = 0.6f, ResourcesEarned = new TownResources { coins = 60 }, ExperienceGained = 45, HappinessChange = 0.06f, ResultMessage = "Excellent strategic thinking!" };
        }
        public string GetActivityName() => "Strategy Command";
        public string GetActivityDescription() => "Lead armies and plan tactics in strategic battles";
    }

    public class AdventureActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);
        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(5000, 10000));
            return new ActivityResult { ActivityType = ActivityType.Adventure, IsSuccess = true, PerformanceRating = 0.8f, ResourcesEarned = new TownResources { coins = 80 }, ExperienceGained = 60, HappinessChange = 0.08f, ResultMessage = "Amazing adventure completed!" };
        }
        public string GetActivityName() => "Adventure Guild";
        public string GetActivityDescription() => "Embark on quests and explore dangerous territories";
    }

    public class PlatformingActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);
        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(2000, 5000));
            return new ActivityResult { ActivityType = ActivityType.Platforming, IsSuccess = true, PerformanceRating = 0.65f, ResourcesEarned = new TownResources { coins = 55 }, ExperienceGained = 42, HappinessChange = 0.065f, ResultMessage = "Great platforming skills!" };
        }
        public string GetActivityName() => "Obstacle Course";
        public string GetActivityDescription() => "Navigate challenging platforming courses";
    }

    public class MusicActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);
        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(2000, 4000));
            return new ActivityResult { ActivityType = ActivityType.Music, IsSuccess = true, PerformanceRating = 0.7f, ResourcesEarned = new TownResources { coins = 65 }, ExperienceGained = 50, HappinessChange = 0.07f, ResultMessage = "Beautiful musical performance!" };
        }
        public string GetActivityName() => "Rhythm Studio";
        public string GetActivityDescription() => "Create music and test rhythm abilities";
    }

    public class CraftingActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);
        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(3000, 6000));
            return new ActivityResult { ActivityType = ActivityType.Crafting, IsSuccess = true, PerformanceRating = 0.6f, ResourcesEarned = new TownResources { coins = 45 }, ExperienceGained = 35, HappinessChange = 0.06f, ResultMessage = "Nice crafting work!" };
        }
        public string GetActivityName() => "Crafting Workshop";
        public string GetActivityDescription() => "Create items and equipment through crafting";
    }

    public class ExplorationActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);
        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(4000, 8000));
            return new ActivityResult { ActivityType = ActivityType.Exploration, IsSuccess = true, PerformanceRating = 0.75f, ResourcesEarned = new TownResources { coins = 70 }, ExperienceGained = 55, HappinessChange = 0.075f, ResultMessage = "Fantastic exploration!" };
        }
        public string GetActivityName() => "Exploration Expedition";
        public string GetActivityDescription() => "Discover new territories and hidden secrets";
    }

    public class SocialActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);
        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(1000, 3000));
            return new ActivityResult { ActivityType = ActivityType.Social, IsSuccess = true, PerformanceRating = 0.8f, ResourcesEarned = new TownResources { coins = 75 }, ExperienceGained = 58, HappinessChange = 0.08f, ResultMessage = "Wonderful social interaction!" };
        }
        public string GetActivityName() => "Social Hub";
        public string GetActivityDescription() => "Interact with other monsters and players";
    }

    #endregion

    #region Supporting Data Structures

    [Serializable]
    public class ActivitySession
    {
        public Monster Monster;
        public ActivityType ActivityType;
        public MonsterPerformance Performance;
        public DateTime StartTime;
        public Dictionary<string, object> SessionData;
    }

    [Serializable]
    public class ActiveParticipation
    {
        public Monster Monster;
        public ActivityType ActivityType;
        public float Duration;
        public float ElapsedTime;
        public DateTime StartTime;
    }

    [Serializable]
    public class PendingActivity
    {
        public Monster Monster;
        public ActivityType ActivityType;
        public MonsterPerformance Performance;
        public DateTime QueueTime;
    }

    [Serializable]
    public class CrossActivityBenefit
    {
        public ActivityType TargetActivity;
        public float BonusAmount;
    }


    #endregion
}