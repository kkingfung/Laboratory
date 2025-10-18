using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Events;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Complete Activity Center Manager implementation that integrates with existing ActivityCenterManager.
    /// Manages all activity centers and coordinates with the existing activity systems.
    /// </summary>
    public class ActivityCenterManagerImpl : IActivityCenterManager
    {
        private readonly IEventBus _eventBus;
        private readonly Dictionary<ActivityType, ActivityCenterInfo> _activityCenters = new();
        private readonly Dictionary<ActivityType, IActivityMiniGame> _activitySystems = new();
        private readonly Queue<ActivityRequest> _activityQueue = new();
        private readonly List<ActiveActivity> _activeActivities = new();

        private bool _isInitialized = false;
        private float _lastUpdateTime;

        // Activity performance tracking
        private readonly Dictionary<string, MonsterActivityStats> _monsterStats = new();

        public ActivityCenterManagerImpl(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        #region IActivityCenterManager Implementation

        public async UniTask InitializeActivityCenter(ActivityType activityType)
        {
            if (_activityCenters.ContainsKey(activityType))
            {
                Debug.LogWarning($"Activity center {activityType} already initialized");
                return;
            }

            try
            {
                // Create activity center info
                var centerInfo = CreateActivityCenterInfo(activityType);
                _activityCenters[activityType] = centerInfo;

                // Initialize corresponding activity system
                var activitySystem = CreateActivitySystem(activityType);
                if (activitySystem != null)
                {
                    await activitySystem.InitializeAsync();
                    _activitySystems[activityType] = activitySystem;
                }

                // Fire initialization event
                _eventBus?.Publish(new ActivityCenterInitializedEvent(activityType, centerInfo));

                Debug.Log($"🎮 Activity Center {activityType} initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize activity center {activityType}: {ex.Message}");
                throw;
            }
        }

        public async UniTask<ActivityResult> RunActivity(MonsterInstance monster, ActivityType activityType, MonsterPerformance performance)
        {
            if (monster == null)
            {
                return ActivityResult.Failed("Monster is null");
            }

            if (!IsActivityAvailable(activityType))
            {
                return ActivityResult.Failed($"Activity {activityType} not available");
            }

            if (!_activitySystems.TryGetValue(activityType, out var activitySystem))
            {
                return ActivityResult.Failed($"Activity system {activityType} not found");
            }

            try
            {
                Debug.Log($"🎯 Starting {activityType} for {monster.Name}");

                // Create activity session
                var session = new ActivitySession
                {
                    Monster = ConvertToMonster(monster),
                    ActivityType = activityType,
                    Performance = ConvertToMonsterPerformance(performance),
                    StartTime = DateTime.Now
                };

                // Track active activity
                var activeActivity = new ActiveActivity
                {
                    Monster = monster,
                    ActivityType = activityType,
                    StartTime = Time.time,
                    ExpectedDuration = GetActivityDuration(activityType)
                };
                _activeActivities.Add(activeActivity);

                // Run the activity mini-game
                var result = await activitySystem.RunActivityAsync(session);

                // Remove from active activities
                _activeActivities.Remove(activeActivity);

                // Convert result and process
                var townResult = ConvertActivityResult(result, monster, activityType);

                // Update monster statistics
                UpdateMonsterStats(monster.UniqueId, activityType, townResult);

                // Fire completion event
                _eventBus?.Publish(new ActivityCompletedEvent(monster, activityType, townResult));

                Debug.Log($"🏆 {monster.Name} completed {activityType}: Success={townResult.IsSuccess}, Performance={townResult.PerformanceRating:F2}");

                return townResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error running activity {activityType} for {monster.Name}: {ex.Message}");
                return ActivityResult.Failed($"Activity error: {ex.Message}");
            }
        }

        public void Update(float deltaTime)
        {
            if (!_isInitialized) return;

            _lastUpdateTime += deltaTime;

            // Process activity queue
            ProcessActivityQueue();

            // Update active activities
            UpdateActiveActivities(deltaTime);

            // Clean up completed activities
            CleanupCompletedActivities();
        }

        public bool IsActivityAvailable(ActivityType activityType)
        {
            return _activityCenters.TryGetValue(activityType, out var centerInfo) &&
                   centerInfo.isUnlocked &&
                   _activitySystems.ContainsKey(activityType);
        }

        public ActivityCenterInfo GetActivityCenterInfo(ActivityType activityType)
        {
            return _activityCenters.TryGetValue(activityType, out var info) ? info : default;
        }

        public void Dispose()
        {
            // Dispose all activity systems
            foreach (var activitySystem in _activitySystems.Values)
            {
                try
                {
                    if (activitySystem is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error disposing activity system: {ex.Message}");
                }
            }

            _activityCenters.Clear();
            _activitySystems.Clear();
            _activityQueue.Clear();
            _activeActivities.Clear();
            _monsterStats.Clear();

            _isInitialized = false;
            Debug.Log("🎮 Activity Center Manager disposed");
        }

        #endregion

        #region Extended Activity Management

        /// <summary>
        /// Queue an activity for processing when capacity is available
        /// </summary>
        public void QueueActivity(MonsterInstance monster, ActivityType activityType, MonsterPerformance performance)
        {
            var request = new ActivityRequest
            {
                Monster = monster,
                ActivityType = activityType,
                Performance = performance,
                QueueTime = Time.time
            };

            _activityQueue.Enqueue(request);
            Debug.Log($"🎮 Queued {activityType} activity for {monster.Name}");
        }

        /// <summary>
        /// Get activity statistics for a monster
        /// </summary>
        public MonsterActivityStats GetMonsterActivityStats(string monsterId)
        {
            return _monsterStats.TryGetValue(monsterId, out var stats) ? stats : new MonsterActivityStats();
        }

        /// <summary>
        /// Get all available activity types
        /// </summary>
        public ActivityType[] GetAvailableActivities()
        {
            var availableActivities = new List<ActivityType>();

            foreach (var kvp in _activityCenters)
            {
                if (kvp.Value.isUnlocked)
                {
                    availableActivities.Add(kvp.Key);
                }
            }

            return availableActivities.ToArray();
        }

        /// <summary>
        /// Unlock a new activity center
        /// </summary>
        public bool UnlockActivityCenter(ActivityType activityType)
        {
            if (_activityCenters.TryGetValue(activityType, out var centerInfo))
            {
                if (!centerInfo.isUnlocked)
                {
                    centerInfo.isUnlocked = true;
                    _activityCenters[activityType] = centerInfo;

                    _eventBus?.Publish(new ActivityCenterUnlockedEvent(activityType));
                    Debug.Log($"🔓 Activity Center {activityType} unlocked!");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get current activity center capacity usage
        /// </summary>
        public float GetActivityCenterUsage(ActivityType activityType)
        {
            var activeCount = _activeActivities.FindAll(a => a.ActivityType == activityType).Count;
            var centerInfo = GetActivityCenterInfo(activityType);

            return centerInfo.Capacity > 0 ? (float)activeCount / centerInfo.Capacity : 0f;
        }

        #endregion

        #region Activity System Creation

        private IActivityMiniGame CreateActivitySystem(ActivityType activityType)
        {
            return activityType switch
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
                ActivityType.Sports => new SportsActivity(),
                ActivityType.Stealth => new StealthActivity(),
                ActivityType.Rhythm => new RhythmActivity(),
                ActivityType.CardGame => new CardGameActivity(),
                ActivityType.BoardGame => new BoardGameActivity(),
                ActivityType.Simulation => new SimulationActivity(),
                ActivityType.Detective => new DetectiveActivity(),
                _ => null
            };
        }

        private ActivityCenterInfo CreateActivityCenterInfo(ActivityType activityType)
        {
            return new ActivityCenterInfo
            {
                activityType = activityType,
                name = GetActivityName(activityType),
                description = GetActivityDescription(activityType),
                isUnlocked = IsInitiallyUnlocked(activityType),
                entryCost = GetActivityEntryCost(activityType),
                difficultyLevel = GetActivityDifficulty(activityType),
                baseRewards = GetActivityBaseRewards(activityType),
                Capacity = GetActivityCapacity(activityType)
            };
        }

        #endregion

        #region Activity Processing

        private void ProcessActivityQueue()
        {
            // Process queued activities if capacity allows
            while (_activityQueue.Count > 0)
            {
                var request = _activityQueue.Peek();

                // Check if activity center has capacity
                if (GetActivityCenterUsage(request.ActivityType) >= 1f)
                    break;

                _activityQueue.Dequeue();
                RunActivityAsync(request);
            }
        }

        private async void RunActivityAsync(ActivityRequest request)
        {
            try
            {
                var result = await RunActivity(request.Monster, request.ActivityType, request.Performance);
                // Result is automatically processed in RunActivity method
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing queued activity: {ex.Message}");
            }
        }

        private void UpdateActiveActivities(float deltaTime)
        {
            foreach (var activity in _activeActivities)
            {
                activity.ElapsedTime += deltaTime;

                // Check for timeouts
                if (activity.ElapsedTime > activity.ExpectedDuration * 2f) // Allow 2x expected time
                {
                    Debug.LogWarning($"Activity {activity.ActivityType} for {activity.Monster.Name} taking too long, may be stuck");
                }
            }
        }

        private void CleanupCompletedActivities()
        {
            // Remove activities that have been running way too long (likely abandoned)
            _activeActivities.RemoveAll(activity => activity.ElapsedTime > 300f); // 5 minutes max
        }

        #endregion

        #region Data Conversion

        private Monster ConvertToMonster(MonsterInstance monsterInstance)
        {
            return new Monster
            {
                UniqueId = monsterInstance.UniqueId,
                Name = monsterInstance.Name,
                Level = 1, // Default level
                Happiness = monsterInstance.Happiness,
                GeneticProfile = monsterInstance.GeneticProfile,
                Stats = ConvertToMonsterStats(monsterInstance.Stats),
                ActivityExperience = new Dictionary<ActivityType, float>(monsterInstance.ActivityExperience)
            };
        }

        private MonsterStats ConvertToMonsterStats(Laboratory.Core.MonsterTown.MonsterStats townStats)
        {
            return MonsterStats.CreateBalanced(50f); // Use existing creation method
        }

        private MonsterPerformance ConvertToMonsterPerformance(Laboratory.Core.MonsterTown.MonsterPerformance townPerformance)
        {
            return MonsterPerformance.FromMonsterStats(MonsterStats.CreateBalanced(50f));
        }

        private ActivityResult ConvertActivityResult(ActivityResult originalResult, MonsterInstance monster, ActivityType activityType)
        {
            var townResources = new TownResources
            {
                coins = originalResult.ExperienceGained * 2, // Convert experience to coins
                activityTokens = originalResult.Success ? 5 : 1
            };

            return ActivityResult.Success(activityType, originalResult.PerformanceRating, originalResult.ResourcesEarned, originalResult.ExperienceGained);
        }

        #endregion

        #region Activity Configuration

        private string GetActivityName(ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Racing => "Racing Circuit",
                ActivityType.Combat => "Combat Arena",
                ActivityType.Puzzle => "Puzzle Academy",
                ActivityType.Strategy => "Strategy Command",
                ActivityType.Adventure => "Adventure Guild",
                ActivityType.Platforming => "Obstacle Course",
                ActivityType.Music => "Rhythm Studio",
                ActivityType.Crafting => "Crafting Workshop",
                ActivityType.Exploration => "Exploration Expedition",
                ActivityType.Sports => "Sports Complex",
                ActivityType.Stealth => "Stealth Training",
                ActivityType.Rhythm => "Rhythm Academy",
                ActivityType.CardGame => "Card Game Lounge",
                ActivityType.BoardGame => "Board Game Café",
                ActivityType.Simulation => "Simulation Center",
                ActivityType.Detective => "Detective Bureau",
                _ => activityType.ToString()
            };
        }

        private string GetActivityDescription(ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Racing => "Test your monster's speed and agility on various racing tracks",
                ActivityType.Combat => "Battle against opponents to test your monster's fighting abilities",
                ActivityType.Puzzle => "Solve puzzles to develop your monster's intelligence",
                ActivityType.Strategy => "Lead armies and plan tactics in strategic battles",
                ActivityType.Adventure => "Embark on quests and explore dangerous territories",
                ActivityType.Platforming => "Navigate challenging platforming courses",
                ActivityType.Music => "Create music and test rhythm abilities",
                ActivityType.Crafting => "Create items and equipment through crafting",
                ActivityType.Exploration => "Discover new territories and hidden secrets",
                ActivityType.Sports => "Compete in various sporting events",
                ActivityType.Stealth => "Master the art of stealth and infiltration",
                ActivityType.Rhythm => "Perfect timing and musical coordination",
                ActivityType.CardGame => "Strategic card-based competitions",
                ActivityType.BoardGame => "Classic and modern board game challenges",
                ActivityType.Simulation => "Complex simulation scenarios",
                ActivityType.Detective => "Solve mysteries and investigate crimes",
                _ => $"Engage in {activityType.ToString().ToLower()} activities"
            };
        }

        private bool IsInitiallyUnlocked(ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Racing or ActivityType.Puzzle or ActivityType.Adventure => true, // Basic activities unlocked
                _ => false // Advanced activities require unlocking
            };
        }

        private TownResources GetActivityEntryCost(ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Racing => new TownResources { energy = 10 },
                ActivityType.Combat => new TownResources { energy = 15 },
                ActivityType.Strategy => new TownResources { energy = 20, activityTokens = 1 },
                _ => new TownResources { energy = 10 }
            };
        }

        private float GetActivityDifficulty(ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Racing or ActivityType.Puzzle => 1f,
                ActivityType.Combat or ActivityType.Adventure => 1.5f,
                ActivityType.Strategy or ActivityType.Detective => 2f,
                _ => 1f
            };
        }

        private TownResources GetActivityBaseRewards(ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Racing => new TownResources { coins = 50, activityTokens = 2 },
                ActivityType.Combat => new TownResources { coins = 75, activityTokens = 3 },
                ActivityType.Strategy => new TownResources { coins = 100, activityTokens = 5, gems = 1 },
                _ => new TownResources { coins = 50, activityTokens = 2 }
            };
        }

        private int GetActivityCapacity(ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Racing => 4, // Multiple racing lanes
                ActivityType.Combat => 2, // Combat pairs
                ActivityType.Puzzle => 6, // Multiple puzzle stations
                ActivityType.Strategy => 1, // Complex strategic scenarios
                _ => 3 // Default capacity
            };
        }

        private float GetActivityDuration(ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Racing => 30f,
                ActivityType.Combat => 45f,
                ActivityType.Puzzle => 60f,
                ActivityType.Strategy => 120f,
                ActivityType.Adventure => 90f,
                _ => 30f
            };
        }

        #endregion

        #region Statistics Management

        private void UpdateMonsterStats(string monsterId, ActivityType activityType, ActivityResult result)
        {
            if (!_monsterStats.TryGetValue(monsterId, out var stats))
            {
                stats = new MonsterActivityStats { MonsterId = monsterId };
                _monsterStats[monsterId] = stats;
            }

            // Update activity-specific stats
            if (!stats.ActivityStats.TryGetValue(activityType, out var activityStats))
            {
                activityStats = new ActivityStats();
                stats.ActivityStats[activityType] = activityStats;
            }

            activityStats.TotalAttempts++;
            if (result.IsSuccess)
            {
                activityStats.SuccessfulAttempts++;
            }
            activityStats.TotalExperience += result.ExperienceGained;
            activityStats.BestPerformance = Mathf.Max(activityStats.BestPerformance, result.PerformanceRating);
            activityStats.LastAttemptTime = DateTime.UtcNow;

            // Update overall stats
            stats.TotalActivitiesCompleted++;
            stats.TotalExperienceGained += result.ExperienceGained;
            stats.LastActivityTime = DateTime.UtcNow;
        }

        #endregion
    }

    #region Supporting Data Structures

    /// <summary>
    /// Activity request for queuing
    /// </summary>
    public struct ActivityRequest
    {
        public MonsterInstance Monster;
        public ActivityType ActivityType;
        public Laboratory.Core.MonsterTown.MonsterPerformance Performance;
        public float QueueTime;
    }

    /// <summary>
    /// Active activity tracking
    /// </summary>
    public class ActiveActivity
    {
        public MonsterInstance Monster;
        public ActivityType ActivityType;
        public float StartTime;
        public float ElapsedTime;
        public float ExpectedDuration;
    }

    /// <summary>
    /// Monster activity statistics
    /// </summary>
    [Serializable]
    public class MonsterActivityStats
    {
        public string MonsterId;
        public int TotalActivitiesCompleted;
        public float TotalExperienceGained;
        public DateTime LastActivityTime;
        public Dictionary<ActivityType, ActivityStats> ActivityStats = new();
    }

    /// <summary>
    /// Statistics for specific activity types
    /// </summary>
    [Serializable]
    public class ActivityStats
    {
        public int TotalAttempts;
        public int SuccessfulAttempts;
        public float TotalExperience;
        public float BestPerformance;
        public DateTime LastAttemptTime;

        public float SuccessRate => TotalAttempts > 0 ? (float)SuccessfulAttempts / TotalAttempts : 0f;
        public float AverageExperience => TotalAttempts > 0 ? TotalExperience / TotalAttempts : 0f;
    }


    #endregion

    #region Extended Activity Implementations

    /// <summary>
    /// Sports activity implementation
    /// </summary>
    public class SportsActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);

        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(2000, 4000));
            var performance = session.Performance.CalculateTotal();
            return ActivityResult.Success(ActivityType.Sports, performance, new TownResources { coins = Mathf.RoundToInt(performance * 60) }, Mathf.RoundToInt(performance * 60));
        }

        public string GetActivityName() => "Sports Complex";
        public string GetActivityDescription() => "Compete in various sporting events";
    }

    /// <summary>
    /// Stealth activity implementation
    /// </summary>
    public class StealthActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);

        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(3000, 6000));
            var performance = session.Performance.CalculateTotal();
            return ActivityResult.Success(ActivityType.Stealth, performance, new TownResources { coins = Mathf.RoundToInt(performance * 80) }, Mathf.RoundToInt(performance * 80));
        }

        public string GetActivityName() => "Stealth Training";
        public string GetActivityDescription() => "Master the art of stealth and infiltration";
    }

    /// <summary>
    /// Rhythm activity implementation
    /// </summary>
    public class RhythmActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);

        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(2000, 4000));
            var performance = session.Performance.CalculateTotal();
            return ActivityResult.Success(ActivityType.Rhythm, performance, new TownResources { coins = Mathf.RoundToInt(performance * 70) }, Mathf.RoundToInt(performance * 70));
        }

        public string GetActivityName() => "Rhythm Academy";
        public string GetActivityDescription() => "Perfect timing and musical coordination";
    }

    /// <summary>
    /// Card game activity implementation
    /// </summary>
    public class CardGameActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);

        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(4000, 8000));
            var performance = session.Performance.CalculateTotal();
            return ActivityResult.Success(ActivityType.CardGame, performance, new TownResources { coins = Mathf.RoundToInt(performance * 90) }, Mathf.RoundToInt(performance * 90));
        }

        public string GetActivityName() => "Card Game Lounge";
        public string GetActivityDescription() => "Strategic card-based competitions";
    }

    /// <summary>
    /// Board game activity implementation
    /// </summary>
    public class BoardGameActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);

        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(5000, 10000));
            var performance = session.Performance.CalculateTotal();
            return ActivityResult.Success(ActivityType.BoardGame, performance, new TownResources { coins = Mathf.RoundToInt(performance * 100) }, Mathf.RoundToInt(performance * 100));
        }

        public string GetActivityName() => "Board Game Café";
        public string GetActivityDescription() => "Classic and modern board game challenges";
    }

    /// <summary>
    /// Simulation activity implementation
    /// </summary>
    public class SimulationActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);

        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(6000, 12000));
            var performance = session.Performance.CalculateTotal();
            return ActivityResult.Success(ActivityType.Simulation, performance, new TownResources { coins = Mathf.RoundToInt(performance * 120) }, Mathf.RoundToInt(performance * 120));
        }

        public string GetActivityName() => "Simulation Center";
        public string GetActivityDescription() => "Complex simulation scenarios";
    }

    /// <summary>
    /// Detective activity implementation
    /// </summary>
    public class DetectiveActivity : IActivityMiniGame
    {
        public async Task InitializeAsync() => await Task.Delay(100);

        public async Task<ActivityResult> RunActivityAsync(ActivitySession session)
        {
            await Task.Delay(UnityEngine.Random.Range(8000, 15000));
            var performance = session.Performance.CalculateTotal();
            return ActivityResult.Success(ActivityType.Detective, performance, new TownResources { coins = Mathf.RoundToInt(performance * 150) }, Mathf.RoundToInt(performance * 150));
        }

        public string GetActivityName() => "Detective Bureau";
        public string GetActivityDescription() => "Solve mysteries and investigate crimes";
    }

    #endregion

    #region Events

    /// <summary>
    /// Activity center initialized event
    /// </summary>
    public class ActivityCenterInitializedEvent
    {
        public ActivityType ActivityType { get; private set; }
        public ActivityCenterInfo CenterInfo { get; private set; }

        public ActivityCenterInitializedEvent(ActivityType activityType, ActivityCenterInfo centerInfo)
        {
            ActivityType = activityType;
            CenterInfo = centerInfo;
        }
    }

    /// <summary>
    /// Activity center unlocked event
    /// </summary>
    public class ActivityCenterUnlockedEvent
    {
        public ActivityType ActivityType { get; private set; }

        public ActivityCenterUnlockedEvent(ActivityType activityType)
        {
            ActivityType = activityType;
        }
    }

    #endregion
}