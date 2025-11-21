using System;
using System.Collections.Generic;
using System.Linq;
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
        private TownManagementSystem _townSystem;

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
            _townSystem = GetComponent<TownManagementSystem>() ?? gameObject.AddComponent<TownManagementSystem>();

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
            {
                // Update multiplayer connections and trades
                _multiplayerManager?.UpdateMultiplayer();
            }
        }

        /// <summary>
        /// Send a monster to participate in an activity mini-game
        /// </summary>
        public async Task<ActivityResult> SendMonsterToActivity(string monsterId, ActivityType activityType)
        {
            var monsterInstance = _playerMonsters.GetMonster(monsterId);
            if (monsterInstance == null)
            {
                Debug.LogWarning($"Monster {monsterId} not found");
                return ActivityResult.Failed("Monster not found");
            }

            // Convert to Monster type for activity systems
            var monster = ConvertToMonster(monsterInstance);

            // Check if monster meets activity requirements
            if (!_activityManager.CanParticipateInActivity(monster, activityType))
            {
                Debug.LogWarning($"Monster {monster.Name} doesn't meet requirements for {activityType}");
                return ActivityResult.Failed($"Monster {monster.Name} doesn't meet requirements for {activityType}");
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
            var parent1Instance = _playerMonsters.GetMonster(parent1Id);
            var parent2Instance = _playerMonsters.GetMonster(parent2Id);

            if (parent1Instance == null || parent2Instance == null)
            {
                Debug.LogWarning("One or both parent monsters not found");
                return null;
            }

            // Convert to Monster type for breeding system
            var parent1 = ConvertToMonster(parent1Instance);
            var parent2 = ConvertToMonster(parent2Instance);

            // Check breeding compatibility and requirements
            if (!_breedingSystem.CanBreed(parent1, parent2))
            {
                Debug.LogWarning($"Cannot breed {parent1.Name} and {parent2.Name}");
                return null;
            }

            // Create offspring with genetic inheritance
            var offspring = await _breedingSystem.CreateOffspring(parent1, parent2);

            // Convert offspring to MonsterInstance and add to player collection
            var offspringInstance = ConvertToMonsterInstance(offspring);

            // Check monster capacity limit
            if (_playerMonsters.Monsters.Count() >= maxMonstersInTown)
            {
                Debug.LogWarning($"Town is at maximum capacity ({maxMonstersInTown} monsters). Cannot add new monster.");
                return null;
            }

            _playerMonsters.AddMonster(offspringInstance);

            // Educational content about genetics
            if (enableEducationalMode)
                _educationSystem.ShowBreedingEducation(parent1Instance, parent2Instance, offspringInstance);

            // Save progress
            await SavePlayerData();

            Debug.Log($"🧬 New monster born: {offspring.Name} with {offspring.GeneticProfile.Traits.Count} unique traits!");
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
            var geneticBonus = CalculateGeneticBonus(monster.GeneticProfile, activityType);

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
                    basePerformance.Speed = monster.Stats.agility * (1f + equipmentBonus);
                    basePerformance.Endurance = monster.Stats.vitality * (1f + equipmentBonus);
                    basePerformance.Handling = monster.Stats.intelligence * (1f + equipmentBonus);
                    break;

                case ActivityType.Combat:
                    basePerformance.AttackPower = monster.Stats.strength * (1f + equipmentBonus);
                    basePerformance.Defense = monster.Stats.vitality * (1f + equipmentBonus);
                    basePerformance.Agility = monster.Stats.agility * (1f + equipmentBonus);
                    break;

                case ActivityType.Puzzle:
                    basePerformance.Intelligence = monster.Stats.intelligence * (1f + equipmentBonus);
                    basePerformance.Patience = monster.Stats.social * (1f + equipmentBonus);
                    basePerformance.Memory = monster.Stats.adaptability * (1f + equipmentBonus);
                    break;

                case ActivityType.Strategy:
                    basePerformance.Leadership = monster.Stats.social * (1f + equipmentBonus);
                    basePerformance.Tactics = monster.Stats.intelligence * (1f + equipmentBonus);
                    basePerformance.Adaptability = monster.Stats.adaptability * (1f + equipmentBonus);
                    break;
            }

            return basePerformance;
        }

        /// <summary>
        /// Calculate genetic bonus for specific activity type
        /// Different genetics provide advantages in different activities
        /// </summary>
        private float CalculateGeneticBonus(IGeneticProfile genetics, ActivityType activityType)
        {
            // Use genetic profile traits to calculate activity-specific bonuses
            return activityType switch
            {
                ActivityType.Racing => (genetics.GetTraitValue("Agility") + genetics.GetTraitValue("Vitality")) / 2f,
                ActivityType.Combat => (genetics.GetTraitValue("Strength") + genetics.GetTraitValue("Agility")) / 2f,
                ActivityType.Puzzle => (genetics.GetTraitValue("Intelligence") + genetics.GetTraitValue("Adaptability")) / 2f,
                ActivityType.Strategy => (genetics.GetTraitValue("Intelligence") + genetics.GetTraitValue("Social")) / 2f,
                ActivityType.Adventure => genetics.GetOverallFitness(),
                _ => genetics.GetOverallFitness()
            };
        }

        /// <summary>
        /// Calculate equipment bonus for activity performance
        /// </summary>
        private float CalculateEquipmentBonus(List<Equipment> equipment, ActivityType activityType)
        {
            if (equipment == null || equipment.Count == 0) return 0f;

            // Calculate bonus based on equipped items
            float totalBonus = 0f;
            foreach (var item in equipment)
            {
                // Basic implementation - each equipment piece adds a small bonus
                totalBonus += 0.1f;
            }

            return Mathf.Clamp01(totalBonus);
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

        #region Type Conversion Methods

        /// <summary>
        /// Convert MonsterInstance to Monster for breeding system compatibility
        /// </summary>
        private Monster ConvertToMonster(MonsterInstance monsterInstance)
        {
            return new Monster
            {
                UniqueId = monsterInstance.UniqueId,
                Name = monsterInstance.Name,
                Level = monsterInstance.Level,
                Happiness = monsterInstance.Happiness,
                GeneticProfile = monsterInstance.GeneticProfile,
                Stats = monsterInstance.Stats,
                ActivityExperience = new Dictionary<ActivityType, float>(monsterInstance.ActivityExperience),
                Equipment = ConvertEquipmentToList(monsterInstance.Equipment),
                CurrentLocation = monsterInstance.CurrentLocation,
                LastActivityTime = monsterInstance.LastActivityTime
            };
        }

        /// <summary>
        /// Convert Monster to MonsterInstance for collection compatibility
        /// </summary>
        private MonsterInstance ConvertToMonsterInstance(Monster monster)
        {
            return new MonsterInstance
            {
                UniqueId = monster.UniqueId,
                Name = monster.Name,
                Level = monster.Level,
                Happiness = monster.Happiness,
                GeneticProfile = monster.GeneticProfile,
                Stats = monster.Stats,
                ActivityExperience = new Dictionary<ActivityType, float>(monster.ActivityExperience),
                Equipment = ConvertEquipmentToStringList(monster.Equipment),
                CurrentLocation = monster.CurrentLocation,
                LastActivityTime = monster.LastActivityTime,
                BirthTime = DateTime.UtcNow,
                Generation = 1,
                Species = "Generated",
                Energy = 100f,
                Experience = 0f,
                IsInTown = true
            };
        }

        /// <summary>
        /// Convert Equipment list to string list
        /// </summary>
        private List<string> ConvertEquipmentToStringList(List<Equipment> equipment)
        {
            if (equipment == null) return new List<string>();

            var result = new List<string>();
            foreach (var item in equipment)
            {
                if (!string.IsNullOrEmpty(item.ItemId))
                    result.Add(item.ItemId);
                else if (!string.IsNullOrEmpty(item.Name))
                    result.Add(item.Name);
            }
            return result;
        }

        /// <summary>
        /// Convert string list to Equipment list (basic implementation)
        /// </summary>
        private List<Equipment> ConvertEquipmentToList(List<string> equipmentIds)
        {
            if (equipmentIds == null) return new List<Equipment>();

            var result = new List<Equipment>();
            foreach (var id in equipmentIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    result.Add(new Equipment
                    {
                        ItemId = id,
                        Name = id,
                        Type = EquipmentType.Accessory,
                        Rarity = EquipmentRarity.Common,
                        Level = 1,
                        IsEquipped = false
                    });
                }
            }
            return result;
        }

        #endregion

        #region Utility Methods

        private Task LoadPlayerTown()
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

            return Task.CompletedTask;
        }

        private async Task LoadPlayerMonsters()
        {
            _playerMonsters = new MonsterCollection();

            // Add starter monsters
            var starterMonster = await _breedingSystem.CreateRandomMonster("Starter");
            _playerMonsters.AddMonster(ConvertToMonsterInstance(starterMonster));
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

        private Task LoadActivityProgression()
        {
            _activityProgression = new Dictionary<string, ActivityProgress>();
            return Task.CompletedTask;
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

        private Task ProcessActivityResult(Monster monster, ActivityResult result)
        {
            // Improve monster stats based on activity
            monster.ImproveFromActivity(result);

            // Grant rewards to player
            _rewardSystem.GrantRewards(result.ResourcesEarned);

            // Update monster experience
            monster.AddActivityExperience(result.ActivityType, result.ExperienceGained);

            return Task.CompletedTask;
        }

        private void UpdateActivityProgression(ActivityType activityType, ActivityResult result)
        {
            if (!enableCrossActivityProgression) return;

            var key = activityType.ToString();
            if (!_activityProgression.ContainsKey(key))
            {
                _activityProgression[key] = new ActivityProgress();
            }

            _activityProgression[key].UpdateProgress(result);
        }

        private Task SavePlayerData()
        {
            // Save town, monsters, and progression data
            // Implementation would depend on your save system
            return Task.CompletedTask;
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

        /// <summary>
        /// Get count of currently active activities
        /// </summary>
        private int GetActiveActivitiesCount()
        {
            // Query activity manager for active activities count
            // This is a simple implementation - would be extended with actual tracking
            return 0; // Placeholder - actual implementation would track active activities
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
            if (result.IsSuccess) SuccessfulCompletions++;
            if (result.PerformanceRating > BestScore) BestScore = result.PerformanceRating;

            AverageScore = ((AverageScore * (TotalParticipations - 1)) + result.PerformanceRating) / TotalParticipations;
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