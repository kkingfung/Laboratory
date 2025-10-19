using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.MonsterTown.Systems;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// ChimeraOS: Monster Breeding Town Builder with Genre Activities
    ///
    /// Core Game Loop:
    /// 1. Breed & Care for Monsters in your town
    /// 2. Send monsters to Activity Centers (mini-games)
    /// 3. Earn rewards to improve monsters & expand town
    /// 4. Unlock new genetics, equipment, and activities
    /// 5. Collaborate with other players in breeding & competitions
    ///
    /// Revolutionary Features:
    /// - Real genetics system affects monster performance in ALL activities
    /// - Town building creates better environments for breeding success
    /// - 10+ different activity types (FPS, Racing, Puzzle, Strategy, etc.)
    /// - Cross-activity progression (racing skills help in combat, etc.)
    /// - Educational genetics learning through engaging gameplay
    /// </summary>
    public class MonsterTownGameCore : MonoBehaviour
    {
        [Header("Core Game Configuration")]
        [SerializeField] private MonsterTownConfig gameConfig;
        [SerializeField] private bool enableEducationalMode = true;
        [SerializeField] private bool enableMultiplayer = true;
        [SerializeField] private bool enableCrossActivityProgression = true;

        [Header("Performance Settings")]
        [SerializeField] private int maxMonstersInTown = 50;
        [SerializeField] private int maxSimultaneousActivities = 10;
        [SerializeField] private float townUpdateFrequency = 1.0f;

        // Core game systems
        private MonsterBreedingSystem _breedingSystem;
        private ActivityCenterManager _activityManager;
        private MonsterCareSystem _careSystem;
        private RewardSystem _rewardSystem;
        private EducationalSystem _educationSystem;
        private MultiplayerManager _multiplayerManager;

        // Game state
        private PlayerTown _playerTown;
        private MonsterCollection _playerMonsters;
        private Dictionary<string, ActivityProgress> _activityProgression;
        private EquipmentInventory _equipment;

        #region Game Initialization

        private void Awake()
        {
            InitializeCoreSystems();
        }

        private async void Start()
        {
            await InitializeGameAsync();
        }

        private void InitializeCoreSystems()
        {
            _breedingSystem = GetComponent<MonsterBreedingSystem>() ?? gameObject.AddComponent<MonsterBreedingSystem>();
            _activityManager = GetComponent<ActivityCenterManager>() ?? gameObject.AddComponent<ActivityCenterManager>();
            _careSystem = GetComponent<MonsterCareSystem>() ?? gameObject.AddComponent<MonsterCareSystem>();
            _rewardSystem = GetComponent<RewardSystem>() ?? gameObject.AddComponent<RewardSystem>();

            if (enableEducationalMode)
                _educationSystem = GetComponent<EducationalSystem>() ?? gameObject.AddComponent<EducationalSystem>();

            if (enableMultiplayer)
                _multiplayerManager = GetComponent<MultiplayerManager>() ?? gameObject.AddComponent<MultiplayerManager>();
        }

        private async Task InitializeGameAsync()
        {
            Debug.Log("🏘️ Initializing ChimeraOS: Monster Breeding Town Builder");

            // Load or create player town
            await LoadPlayerTown();

            // Initialize monster collection
            await LoadPlayerMonsters();

            // Setup activity centers
            await InitializeActivityCenters();

            // Load progression data
            await LoadActivityProgression();

            // Start town simulation
            StartTownSimulation();

            Debug.Log("🎮 ChimeraOS Ready - Welcome to your Monster Town!");
        }

        #endregion

        #region Core Game Loop

        /// <summary>
        /// Main game update loop - manages town simulation and monster activities
        /// </summary>
        private void Update()
        {
            if (_playerTown == null) return;

            // Update town systems
            _townSystem.UpdateTown(_playerTown, Time.deltaTime);

            // Update monster care and happiness
            _careSystem.UpdateMonsterCare(_playerMonsters, Time.deltaTime);

            // Process ongoing activities
            _activityManager.UpdateActivities(Time.deltaTime);

            // Handle educational moments
            if (enableEducationalMode)
                _educationSystem.CheckForEducationalMoments();

            // Update multiplayer features
            if (enableMultiplayer)
                _multiplayerManager.UpdateMultiplayer();
        }

        /// <summary>
        /// Send a monster to participate in an activity mini-game
        /// </summary>
        public async Task<ActivityResult> SendMonsterToActivity(string monsterId, ActivityType activityType)
        {
            var monster = _playerMonsters.GetMonster(monsterId);
            if (monster == null)
            {
                Debug.LogWarning($"Monster {monsterId} not found");
                return null;
            }

            // Check if monster meets activity requirements
            if (!_activityManager.CanParticipateInActivity(monster, activityType))
            {
                Debug.LogWarning($"Monster {monster.Name} doesn't meet requirements for {activityType}");
                return null;
            }

            // Calculate monster performance based on genetics and equipment
            var performance = CalculateMonsterPerformance(monster, activityType);

            // Run the activity mini-game
            var result = await _activityManager.RunActivity(monster, activityType, performance);

            // Process rewards and monster improvement
            await ProcessActivityResult(monster, result);

            // Update activity progression
            UpdateActivityProgression(activityType, result);

            // Educational feedback
            if (enableEducationalMode)
                _educationSystem.ShowActivityEducation(activityType, result);

            return result;
        }

        /// <summary>
        /// Breed two monsters to create offspring with inherited genetics
        /// </summary>
        public async Task<Monster> BreedMonsters(string parent1Id, string parent2Id)
        {
            var parent1 = _playerMonsters.GetMonster(parent1Id);
            var parent2 = _playerMonsters.GetMonster(parent2Id);

            if (parent1 == null || parent2 == null)
            {
                Debug.LogWarning("One or both parent monsters not found");
                return null;
            }

            // Check breeding compatibility and requirements
            if (!_breedingSystem.CanBreed(parent1, parent2))
            {
                Debug.LogWarning($"Cannot breed {parent1.Name} and {parent2.Name}");
                return null;
            }

            // Create offspring with genetic inheritance
            var offspring = await _breedingSystem.CreateOffspring(parent1, parent2);

            // Add to player collection
            _playerMonsters.AddMonster(offspring);

            // Educational content about genetics
            if (enableEducationalMode)
                _educationSystem.ShowBreedingEducation(parent1, parent2, offspring);

            // Save progress
            await SavePlayerData();

            Debug.Log($"🧬 New monster born: {offspring.Name} with {offspring.Genetics.GetUniqueTraitCount()} unique traits!");
            return offspring;
        }

        #endregion

        #region Monster Performance & Activities

        /// <summary>
        /// Calculate how well a monster will perform in a specific activity
        /// Based on genetics, equipment, happiness, and experience
        /// </summary>
        private MonsterPerformance CalculateMonsterPerformance(Monster monster, ActivityType activityType)
        {
            var basePerformance = new MonsterPerformance();

            // Genetic influence (60% of performance)
            var geneticBonus = CalculateGeneticBonus(monster.Genetics, activityType);

            // Equipment influence (25% of performance)
            var equipmentBonus = CalculateEquipmentBonus(monster.Equipment, activityType);

            // Happiness & care influence (10% of performance)
            var happinessBonus = monster.Happiness * 0.1f;

            // Experience influence (5% of performance)
            var experienceBonus = monster.GetActivityExperience(activityType) * 0.05f;

            // Calculate final performance
            basePerformance.OverallRating = Mathf.Clamp01(
                geneticBonus * 0.6f +
                equipmentBonus * 0.25f +
                happinessBonus * 0.1f +
                experienceBonus * 0.05f
            );

            // Activity-specific performance calculations
            switch (activityType)
            {
                case ActivityType.Racing:
                    basePerformance.Speed = monster.Genetics.Agility * (1f + equipmentBonus);
                    basePerformance.Endurance = monster.Genetics.Vitality * (1f + equipmentBonus);
                    basePerformance.Handling = monster.Genetics.Intelligence * (1f + equipmentBonus);
                    break;

                case ActivityType.Combat:
                    basePerformance.AttackPower = monster.Genetics.Strength * (1f + equipmentBonus);
                    basePerformance.Defense = monster.Genetics.Vitality * (1f + equipmentBonus);
                    basePerformance.Agility = monster.Genetics.Agility * (1f + equipmentBonus);
                    break;

                case ActivityType.Puzzle:
                    basePerformance.Intelligence = monster.Genetics.Intelligence * (1f + equipmentBonus);
                    basePerformance.Patience = monster.Genetics.Social * (1f + equipmentBonus);
                    basePerformance.Memory = monster.Genetics.Adaptability * (1f + equipmentBonus);
                    break;

                case ActivityType.Strategy:
                    basePerformance.Leadership = monster.Genetics.Social * (1f + equipmentBonus);
                    basePerformance.Tactics = monster.Genetics.Intelligence * (1f + equipmentBonus);
                    basePerformance.Adaptability = monster.Genetics.Adaptability * (1f + equipmentBonus);
                    break;
            }

            return basePerformance;
        }

        /// <summary>
        /// Calculate genetic bonus for specific activity type
        /// Different genetics provide advantages in different activities
        /// </summary>
        private float CalculateGeneticBonus(MonsterGenetics genetics, ActivityType activityType)
        {
            return genetics.GetActivityBonus(activityType);
        }

        /// <summary>
        /// Calculate equipment bonus for activity performance
        /// </summary>
        private float CalculateEquipmentBonus(MonsterEquipment equipment, ActivityType activityType)
        {
            if (equipment.GetEquippedCount() == 0) return 0f;

            return equipment.GetActivityBonus(activityType);
        }

        #endregion

        #region Town Building & Management

        /// <summary>
        /// Build a new facility in the town
        /// </summary>
        public async Task<bool> BuildFacility(FacilityType facilityType, Vector2Int position)
        {
            var facility = new TownFacility
            {
                Type = facilityType,
                Position = position,
                Level = 1,
                ConstructionTime = GetConstructionTime(facilityType)
            };

            var cost = GetBuildingCost(facilityType);
            if (!_rewardSystem.CanAfford(cost))
            {
                Debug.LogWarning($"Cannot afford {facilityType} - costs {cost}");
                return false;
            }

            // Deduct cost and start construction
            _rewardSystem.SpendCurrency(cost);
            await _townSystem.StartConstruction(facility);

            Debug.Log($"🏗️ Building {facilityType} at {position}");
            return true;
        }

        /// <summary>
        /// Upgrade an existing facility
        /// </summary>
        public async Task<bool> UpgradeFacility(string facilityId)
        {
            var facility = _playerTown.GetFacility(facilityId);
            if (facility == null) return false;

            var upgradeCost = GetUpgradeCost(facility);
            if (!_rewardSystem.CanAfford(upgradeCost))
            {
                Debug.LogWarning($"Cannot afford upgrade for {facility.Type}");
                return false;
            }

            _rewardSystem.SpendCurrency(upgradeCost);
            await _townSystem.UpgradeFacility(facility);

            Debug.Log($"⬆️ Upgraded {facility.Type} to level {facility.Level}");
            return true;
        }

        #endregion

        #region Utility Methods

        private async Task LoadPlayerTown()
        {
            // Load from save or create new town
            _playerTown = new PlayerTown
            {
                Name = "Player's Monster Town",
                Level = 1,
                Population = 0,
                Facilities = new List<TownFacility>()
            };

            // Add starter facilities
            _playerTown.Facilities.Add(new TownFacility
            {
                Type = FacilityType.BreedingCenter,
                Position = Vector2Int.zero,
                Level = 1,
                IsConstructed = true
            });
        }

        private async Task LoadPlayerMonsters()
        {
            _playerMonsters = new MonsterCollection();

            // Add starter monsters
            var starterMonster = await _breedingSystem.CreateRandomMonster("Starter");
            _playerMonsters.AddMonster(starterMonster);
        }

        private async Task InitializeActivityCenters()
        {
            await _activityManager.InitializeActivities(new[]
            {
                ActivityType.Racing,
                ActivityType.Combat,
                ActivityType.Puzzle,
                ActivityType.Strategy,
                ActivityType.Adventure
            });
        }

        private async Task LoadActivityProgression()
        {
            _activityProgression = new Dictionary<string, ActivityProgress>();
        }

        private void StartTownSimulation()
        {
            InvokeRepeating(nameof(TownSimulationTick), townUpdateFrequency, townUpdateFrequency);
        }

        private void TownSimulationTick()
        {
            // Update town happiness based on facilities
            _townSystem.UpdateTownHappiness(_playerTown);

            // Update monster happiness based on care and facilities
            _careSystem.UpdateMonsterHappiness(_playerMonsters, _playerTown);

            // Process any automatic rewards
            _rewardSystem.ProcessPassiveRewards(_playerTown);
        }

        private async Task ProcessActivityResult(Monster monster, ActivityResult result)
        {
            // Improve monster stats based on activity
            monster.ImproveFromActivity(result);

            // Grant rewards to player
            _rewardSystem.GrantRewards(result.Rewards);

            // Update monster experience
            monster.AddActivityExperience(result.ActivityType, result.ExperienceGained);
        }

        private void UpdateActivityProgression(ActivityType activityType, ActivityResult result)
        {
            var key = activityType.ToString();
            if (!_activityProgression.ContainsKey(key))
            {
                _activityProgression[key] = new ActivityProgress();
            }

            _activityProgression[key].UpdateProgress(result);
        }

        private async Task SavePlayerData()
        {
            // Save town, monsters, and progression data
            // Implementation would depend on your save system
        }

        private TimeSpan GetConstructionTime(FacilityType facilityType)
        {
            return facilityType switch
            {
                FacilityType.BreedingCenter => TimeSpan.FromMinutes(30),
                FacilityType.TrainingGround => TimeSpan.FromMinutes(45),
                FacilityType.ActivityCenter => TimeSpan.FromHours(1),
                FacilityType.ResearchLab => TimeSpan.FromHours(2),
                _ => TimeSpan.FromMinutes(15)
            };
        }

        private CurrencyAmount GetBuildingCost(FacilityType facilityType)
        {
            return facilityType switch
            {
                FacilityType.BreedingCenter => new CurrencyAmount { Coins = 1000 },
                FacilityType.TrainingGround => new CurrencyAmount { Coins = 1500 },
                FacilityType.ActivityCenter => new CurrencyAmount { Coins = 2000 },
                FacilityType.ResearchLab => new CurrencyAmount { Coins = 3000 },
                _ => new CurrencyAmount { Coins = 500 }
            };
        }

        private CurrencyAmount GetUpgradeCost(TownFacility facility)
        {
            return new CurrencyAmount { Coins = facility.Level * 500 };
        }

        #endregion
    }

    #region Supporting Data Structures

    [Serializable]
    public class PlayerTown
    {
        public string Name;
        public int Level;
        public int Population;
        public float Happiness;
        public List<TownFacility> Facilities;

        public TownFacility GetFacility(string facilityId)
        {
            return Facilities.Find(f => f.Id == facilityId);
        }
    }

    [Serializable]
    public class TownFacility
    {
        public string Id = Guid.NewGuid().ToString();
        public FacilityType Type;
        public Vector2Int Position;
        public int Level;
        public bool IsConstructed;
        public TimeSpan ConstructionTime;
        public DateTime ConstructionStartTime;
    }

    public enum FacilityType
    {
        BreedingCenter,
        TrainingGround,
        ActivityCenter,
        ResearchLab,
        MonsterHabitat,
        EquipmentShop,
        SocialHub,
        EducationCenter
    }


    [Serializable]
    public class ActivityProgress
    {
        public int TotalParticipations;
        public int SuccessfulCompletions;
        public float BestScore;
        public float AverageScore;
        public DateTime LastParticipation;

        public void UpdateProgress(ActivityResult result)
        {
            TotalParticipations++;
            if (result.Success) SuccessfulCompletions++;
            if (result.PerformanceScore > BestScore) BestScore = result.PerformanceScore;

            AverageScore = ((AverageScore * (TotalParticipations - 1)) + result.PerformanceScore) / TotalParticipations;
            LastParticipation = DateTime.Now;
        }
    }

    [Serializable]
    public class CurrencyAmount
    {
        public int Coins;
        public int Gems;
        public int ActivityTokens;
    }

    #endregion
}