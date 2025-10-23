using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;

namespace Laboratory.Core.MonsterTown.Systems
{
    /// <summary>
    /// Monster Care System - handles monster happiness, health, and wellbeing
    /// Manages feeding, playing, resting, and other care activities
    /// </summary>
    public class MonsterCareSystem : MonoBehaviour
    {
        [Header("Care Configuration")]
        [SerializeField] private float hungerDecayRate = 1f; // Per hour
        [SerializeField] private float happinessDecayRate = 0.5f; // Per hour
        [SerializeField] private float energyDecayRate = 2f; // Per hour
        [SerializeField] private float healthDecayRate = 0.1f; // Per hour when sick

        [Header("Care Benefits")]
        [SerializeField] private float feedingHungerRestore = 50f;
        [SerializeField] private float playingHappinessBoost = 30f;
        [SerializeField] private float restingEnergyRestore = 40f;
        [SerializeField] private float medicineHealthRestore = 25f;

        // System dependencies
        private IEventBus eventBus;
        private Dictionary<string, MonsterCareStatus> monsterCareStatus = new();

        #region Unity Lifecycle

        private void Awake()
        {
            eventBus = ServiceContainer.Instance?.ResolveService<IEventBus>();
        }

        private void Start()
        {
            if (eventBus != null)
            {
                eventBus.Subscribe<MonsterAddedToTownEvent>(OnMonsterAddedToTown);
            }

            // Update care status every 30 seconds
            InvokeRepeating(nameof(UpdateAllMonsterCare), 30f, 30f);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Feed a monster to restore hunger
        /// </summary>
        public bool FeedMonster(MonsterInstance monster, FoodType foodType = FoodType.Regular)
        {
            if (!monsterCareStatus.TryGetValue(monster.UniqueId, out var careStatus))
            {
                Debug.LogWarning($"Monster {monster.Name} not found in care system");
                return false;
            }

            float hungerRestore = feedingHungerRestore * GetFoodMultiplier(foodType);
            careStatus.Hunger = Mathf.Min(100f, careStatus.Hunger + hungerRestore);
            careStatus.LastFed = DateTime.Now;

            // Feeding also slightly boosts happiness
            careStatus.Happiness = Mathf.Min(100f, careStatus.Happiness + 5f);

            monster.Happiness = careStatus.Happiness;
            monster.Energy = careStatus.Energy;

            eventBus?.Publish(new MonsterFedEvent(monster, foodType));

            Debug.Log($"🍎 Fed {monster.Name} with {foodType}. Hunger: {careStatus.Hunger:F1}");
            return true;
        }

        /// <summary>
        /// Play with a monster to boost happiness
        /// </summary>
        public bool PlayWithMonster(MonsterInstance monster, PlayActivity activity = PlayActivity.Fetch)
        {
            if (!monsterCareStatus.TryGetValue(monster.UniqueId, out var careStatus))
            {
                Debug.LogWarning($"Monster {monster.Name} not found in care system");
                return false;
            }

            // Can't play if monster is too tired
            if (careStatus.Energy < 20f)
            {
                Debug.LogWarning($"{monster.Name} is too tired to play");
                return false;
            }

            float happinessBoost = playingHappinessBoost * GetActivityMultiplier(activity);
            careStatus.Happiness = Mathf.Min(100f, careStatus.Happiness + happinessBoost);
            careStatus.Energy = Mathf.Max(0f, careStatus.Energy - 15f); // Playing uses energy
            careStatus.LastPlayed = DateTime.Now;

            monster.Happiness = careStatus.Happiness;
            monster.Energy = careStatus.Energy;

            eventBus?.Publish(new MonsterPlayedEvent(monster, activity));

            Debug.Log($"🎾 Played {activity} with {monster.Name}. Happiness: {careStatus.Happiness:F1}");
            return true;
        }

        /// <summary>
        /// Let monster rest to restore energy
        /// </summary>
        public bool LetMonsterRest(MonsterInstance monster, int restHours = 1)
        {
            if (!monsterCareStatus.TryGetValue(monster.UniqueId, out var careStatus))
            {
                Debug.LogWarning($"Monster {monster.Name} not found in care system");
                return false;
            }

            float energyRestore = restingEnergyRestore * restHours;
            careStatus.Energy = Mathf.Min(100f, careStatus.Energy + energyRestore);
            careStatus.LastRested = DateTime.Now;

            // Resting also slightly improves health
            careStatus.Health = Mathf.Min(100f, careStatus.Health + 2f * restHours);

            monster.Happiness = careStatus.Happiness;
            monster.Energy = careStatus.Energy;

            eventBus?.Publish(new MonsterRestedEvent(monster, restHours));

            Debug.Log($"😴 {monster.Name} rested for {restHours}h. Energy: {careStatus.Energy:F1}");
            return true;
        }

        /// <summary>
        /// Give medicine to sick monster
        /// </summary>
        public bool GiveMedicine(MonsterInstance monster, MedicineType medicine = MedicineType.Basic)
        {
            if (!monsterCareStatus.TryGetValue(monster.UniqueId, out var careStatus))
            {
                Debug.LogWarning($"Monster {monster.Name} not found in care system");
                return false;
            }

            if (careStatus.Health >= 90f)
            {
                Debug.LogWarning($"{monster.Name} is not sick enough for medicine");
                return false;
            }

            float healthRestore = medicineHealthRestore * GetMedicineMultiplier(medicine);
            careStatus.Health = Mathf.Min(100f, careStatus.Health + healthRestore);
            careStatus.LastMedicine = DateTime.Now;

            eventBus?.Publish(new MonsterMedicatedEvent(monster, medicine));

            Debug.Log($"💊 Gave {medicine} to {monster.Name}. Health: {careStatus.Health:F1}");
            return true;
        }

        /// <summary>
        /// Get care status for a monster
        /// </summary>
        public MonsterCareStatus GetCareStatus(MonsterInstance monster)
        {
            return monsterCareStatus.TryGetValue(monster.UniqueId, out var status)
                ? status
                : new MonsterCareStatus();
        }

        /// <summary>
        /// Get overall care level for a monster (0-1)
        /// </summary>
        public float GetCareLevel(MonsterInstance monster)
        {
            if (!monsterCareStatus.TryGetValue(monster.UniqueId, out var status))
                return 0f;

            return (status.Health + status.Happiness + status.Energy + status.Hunger) / 400f;
        }

        /// <summary>
        /// Update monster care status based on deltaTime
        /// </summary>
        public void UpdateMonsterCare(MonsterCollection monsters, float deltaTime)
        {
            if (monsters?.Monsters == null) return;

            foreach (var monster in monsters.Monsters)
            {
                UpdateSingleMonsterCare(monster, deltaTime);
            }
        }

        /// <summary>
        /// Update happiness for all monsters based on town facilities
        /// </summary>
        public void UpdateMonsterHappiness(MonsterCollection monsters, PlayerTown playerTown)
        {
            if (monsters?.Monsters == null || playerTown?.Facilities == null) return;

            float townHappinessBonus = CalculateTownHappinessBonus(playerTown);

            foreach (var monster in monsters.Monsters)
            {
                if (monsterCareStatus.TryGetValue(monster.UniqueId, out var careStatus))
                {
                    // Apply town happiness bonus
                    careStatus.Happiness = Mathf.Min(100f, careStatus.Happiness + townHappinessBonus * Time.deltaTime);
                    monster.Happiness = careStatus.Happiness;

                    // Check if monster is in suitable facilities
                    if (IsMonsterInGoodFacility(monster, playerTown))
                    {
                        careStatus.Happiness = Mathf.Min(100f, careStatus.Happiness + 1f * Time.deltaTime);
                        careStatus.Energy = Mathf.Min(100f, careStatus.Energy + 0.5f * Time.deltaTime);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void UpdateAllMonsterCare()
        {
            var now = DateTime.Now;

            foreach (var kvp in monsterCareStatus)
            {
                var monsterId = kvp.Key;
                var careStatus = kvp.Value;

                // Calculate time since last update
                float hoursSinceUpdate = (float)(now - careStatus.LastUpdate).TotalHours;
                if (hoursSinceUpdate < 0.1f) continue; // Skip if too recent

                // Apply decay rates
                careStatus.Hunger = Mathf.Max(0f, careStatus.Hunger - hungerDecayRate * hoursSinceUpdate);
                careStatus.Happiness = Mathf.Max(0f, careStatus.Happiness - happinessDecayRate * hoursSinceUpdate);
                careStatus.Energy = Mathf.Max(0f, careStatus.Energy - energyDecayRate * hoursSinceUpdate);

                // Health decay only when sick or very hungry
                if (careStatus.Health < 50f || careStatus.Hunger < 10f)
                {
                    careStatus.Health = Mathf.Max(0f, careStatus.Health - healthDecayRate * hoursSinceUpdate);
                }

                careStatus.LastUpdate = now;

                // Check for critical conditions
                CheckCriticalConditions(monsterId, careStatus);
            }
        }

        private void CheckCriticalConditions(string monsterId, MonsterCareStatus status)
        {
            // Very hungry
            if (status.Hunger < 10f)
            {
                eventBus?.Publish(new MonsterHungryEvent(monsterId, CareUrgency.Critical));
            }
            else if (status.Hunger < 25f)
            {
                eventBus?.Publish(new MonsterHungryEvent(monsterId, CareUrgency.Warning));
            }

            // Very unhappy
            if (status.Happiness < 10f)
            {
                eventBus?.Publish(new MonsterUnhappyEvent(monsterId, CareUrgency.Critical));
            }
            else if (status.Happiness < 25f)
            {
                eventBus?.Publish(new MonsterUnhappyEvent(monsterId, CareUrgency.Warning));
            }

            // Very tired
            if (status.Energy < 5f)
            {
                eventBus?.Publish(new MonsterTiredEvent(monsterId, CareUrgency.Critical));
            }

            // Sick
            if (status.Health < 25f)
            {
                eventBus?.Publish(new MonsterSickEvent(monsterId, CareUrgency.Critical));
            }
            else if (status.Health < 50f)
            {
                eventBus?.Publish(new MonsterSickEvent(monsterId, CareUrgency.Warning));
            }
        }

        private float GetFoodMultiplier(FoodType foodType)
        {
            return foodType switch
            {
                FoodType.Regular => 1.0f,
                FoodType.Premium => 1.5f,
                FoodType.Gourmet => 2.0f,
                FoodType.Medicine => 1.2f,
                _ => 1.0f
            };
        }

        private float GetActivityMultiplier(PlayActivity activity)
        {
            return activity switch
            {
                PlayActivity.Fetch => 1.0f,
                PlayActivity.Puzzle => 1.3f,
                PlayActivity.Adventure => 1.5f,
                PlayActivity.Social => 1.2f,
                _ => 1.0f
            };
        }

        private float GetMedicineMultiplier(MedicineType medicine)
        {
            return medicine switch
            {
                MedicineType.Basic => 1.0f,
                MedicineType.Advanced => 1.5f,
                MedicineType.Magical => 2.0f,
                _ => 1.0f
            };
        }

        private void OnMonsterAddedToTown(MonsterAddedToTownEvent evt)
        {
            // Initialize care status for new monster
            if (!monsterCareStatus.ContainsKey(evt.Monster.UniqueId))
            {
                monsterCareStatus[evt.Monster.UniqueId] = new MonsterCareStatus
                {
                    Health = 80f,
                    Happiness = evt.Monster.Happiness,
                    Energy = evt.Monster.Energy,
                    Hunger = 60f,
                    LastUpdate = DateTime.Now,
                    LastFed = DateTime.Now.AddHours(-2),
                    LastPlayed = DateTime.Now.AddHours(-4),
                    LastRested = DateTime.Now.AddHours(-6),
                    LastMedicine = DateTime.MinValue
                };

                Debug.Log($"🏠 Initialized care status for {evt.Monster.Name}");
            }
        }

        /// <summary>
        /// Update care status for a single monster
        /// </summary>
        private void UpdateSingleMonsterCare(MonsterInstance monster, float deltaTime)
        {
            if (!monsterCareStatus.TryGetValue(monster.UniqueId, out var careStatus))
            {
                // Initialize care status if not found
                monsterCareStatus[monster.UniqueId] = new MonsterCareStatus
                {
                    Health = 80f,
                    Happiness = monster.Happiness,
                    Energy = 100f,
                    Hunger = 60f,
                    LastUpdate = DateTime.Now
                };
                return;
            }

            // Convert deltaTime to hours
            float deltaHours = deltaTime / 3600f;

            // Apply gradual decay
            careStatus.Hunger = Mathf.Max(0f, careStatus.Hunger - hungerDecayRate * deltaHours);
            careStatus.Energy = Mathf.Max(0f, careStatus.Energy - energyDecayRate * deltaHours);
            careStatus.Happiness = Mathf.Max(0f, careStatus.Happiness - happinessDecayRate * deltaHours);

            // Health decay when hungry or sick
            if (careStatus.Hunger < 20f || careStatus.Health < 50f)
            {
                careStatus.Health = Mathf.Max(0f, careStatus.Health - healthDecayRate * deltaHours);
            }

            // Update monster values
            monster.Happiness = careStatus.Happiness;

            careStatus.LastUpdate = DateTime.Now;
        }

        /// <summary>
        /// Calculate happiness bonus from town facilities
        /// </summary>
        private float CalculateTownHappinessBonus(PlayerTown playerTown)
        {
            float bonus = 0f;

            foreach (var facility in playerTown.Facilities)
            {
                if (!facility.IsConstructed) continue;

                switch (facility.Type)
                {
                    case FacilityType.MonsterHabitat:
                        bonus += facility.Level * 0.5f;
                        break;
                    case FacilityType.SocialHub:
                        bonus += facility.Level * 0.3f;
                        break;
                    case FacilityType.EducationCenter:
                        bonus += facility.Level * 0.2f;
                        break;
                    case FacilityType.ActivityCenter:
                        bonus += facility.Level * 0.4f;
                        break;
                }
            }

            // Town level bonus
            bonus += playerTown.Level * 0.1f;

            return bonus;
        }

        /// <summary>
        /// Check if monster is in a good facility for their type
        /// </summary>
        private bool IsMonsterInGoodFacility(MonsterInstance monster, PlayerTown playerTown)
        {
            // Check if monster's current location matches available facilities
            foreach (var facility in playerTown.Facilities)
            {
                if (!facility.IsConstructed) continue;

                switch (monster.CurrentLocation)
                {
                    case TownLocation.Habitat when facility.Type == FacilityType.MonsterHabitat:
                    case TownLocation.ActivityCenter when facility.Type == FacilityType.ActivityCenter:
                    case TownLocation.TrainingGrounds when facility.Type == FacilityType.TrainingGround:
                    case TownLocation.Laboratory when facility.Type == FacilityType.ResearchLab:
                        return true;
                }
            }

            return false;
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class MonsterCareStatus
    {
        public float Health = 100f;
        public float Happiness = 50f;
        public float Energy = 100f;
        public float Hunger = 80f;

        public DateTime LastUpdate = DateTime.Now;
        public DateTime LastFed = DateTime.MinValue;
        public DateTime LastPlayed = DateTime.MinValue;
        public DateTime LastRested = DateTime.MinValue;
        public DateTime LastMedicine = DateTime.MinValue;
    }

    public enum FoodType
    {
        Regular,
        Premium,
        Gourmet,
        Medicine
    }

    public enum PlayActivity
    {
        Fetch,
        Puzzle,
        Adventure,
        Social
    }

    public enum MedicineType
    {
        Basic,
        Advanced,
        Magical
    }

    public enum CareUrgency
    {
        Normal,
        Warning,
        Critical
    }

    // Care Events
    public class MonsterFedEvent
    {
        public MonsterInstance Monster { get; }
        public FoodType Food { get; }
        public DateTime Timestamp { get; }

        public MonsterFedEvent(MonsterInstance monster, FoodType food)
        {
            Monster = monster;
            Food = food;
            Timestamp = DateTime.Now;
        }
    }

    public class MonsterPlayedEvent
    {
        public MonsterInstance Monster { get; }
        public PlayActivity Activity { get; }
        public DateTime Timestamp { get; }

        public MonsterPlayedEvent(MonsterInstance monster, PlayActivity activity)
        {
            Monster = monster;
            Activity = activity;
            Timestamp = DateTime.Now;
        }
    }

    public class MonsterRestedEvent
    {
        public MonsterInstance Monster { get; }
        public int RestHours { get; }
        public DateTime Timestamp { get; }

        public MonsterRestedEvent(MonsterInstance monster, int restHours)
        {
            Monster = monster;
            RestHours = restHours;
            Timestamp = DateTime.Now;
        }
    }

    public class MonsterMedicatedEvent
    {
        public MonsterInstance Monster { get; }
        public MedicineType Medicine { get; }
        public DateTime Timestamp { get; }

        public MonsterMedicatedEvent(MonsterInstance monster, MedicineType medicine)
        {
            Monster = monster;
            Medicine = medicine;
            Timestamp = DateTime.Now;
        }
    }

    public class MonsterHungryEvent
    {
        public string MonsterId { get; }
        public CareUrgency Urgency { get; }
        public DateTime Timestamp { get; }

        public MonsterHungryEvent(string monsterId, CareUrgency urgency)
        {
            MonsterId = monsterId;
            Urgency = urgency;
            Timestamp = DateTime.Now;
        }
    }

    public class MonsterUnhappyEvent
    {
        public string MonsterId { get; }
        public CareUrgency Urgency { get; }
        public DateTime Timestamp { get; }

        public MonsterUnhappyEvent(string monsterId, CareUrgency urgency)
        {
            MonsterId = monsterId;
            Urgency = urgency;
            Timestamp = DateTime.Now;
        }
    }

    public class MonsterTiredEvent
    {
        public string MonsterId { get; }
        public CareUrgency Urgency { get; }
        public DateTime Timestamp { get; }

        public MonsterTiredEvent(string monsterId, CareUrgency urgency)
        {
            MonsterId = monsterId;
            Urgency = urgency;
            Timestamp = DateTime.Now;
        }
    }

    public class MonsterSickEvent
    {
        public string MonsterId { get; }
        public CareUrgency Urgency { get; }
        public DateTime Timestamp { get; }

        public MonsterSickEvent(string monsterId, CareUrgency urgency)
        {
            MonsterId = monsterId;
            Urgency = urgency;
            Timestamp = DateTime.Now;
        }
    }

    #endregion
}