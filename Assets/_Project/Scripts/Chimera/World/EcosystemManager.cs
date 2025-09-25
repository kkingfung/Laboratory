using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;
using Laboratory.Core.Events;
using CoreBiomeType = Laboratory.Chimera.Core.BiomeType;

namespace Laboratory.Chimera.World
{
    /// <summary>
    /// Advanced ecosystem manager for Project Chimera.
    /// Manages population dynamics, environmental pressures, seasonal changes,
    /// and player impact on the ecosystem balance.
    /// </summary>
    public class EcosystemManager : MonoBehaviour
    {
        // Singleton instance for performance-optimized creature registration
        public static EcosystemManager Instance { get; private set; }

        [Header("Ecosystem Configuration")]
        [SerializeField] private float updateInterval = 30f; // Real-time seconds per ecosystem update
        [SerializeField] private float seasonLength = 300f; // Real-time seconds per season
        [SerializeField] private bool enableSeasonalChanges = true;
        [SerializeField] private bool enablePopulationDynamics = true;
        [SerializeField] private bool enablePlayerImpactTracking = true;

        [Header("Global Parameters")]
        [Range(0.5f, 2f)]
        [SerializeField] private float globalGrowthRate = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float extinctionThreshold = 0.1f;
        [Range(0f, 2f)]
        [SerializeField] private float playerImpactSensitivity = 1f;

        [Header("Debug")]
        [SerializeField] private bool enableDetailedLogging = false;
        [SerializeField] private bool showEcosystemUI = true;

        // Core systems
        private Dictionary<Laboratory.Chimera.Core.BiomeType, BiomeEcosystem> biomes = new();
        private Dictionary<string, SpeciesPopulation> globalSpeciesData = new();
        private PlayerImpactData playerImpact = new();
        private IEventBus eventBus;

        // Seasonal system
        private float currentSeasonTimer = 0f;
        private int currentSeasonIndex = 0;
        private readonly string[] seasons = { "Spring", "Summer", "Autumn", "Winter" };
        private float seasonalMultiplier = 1f;

        // Population tracking
        private float lastEcosystemUpdate = 0f;
        private List<CreatureInstanceComponent> trackedCreatures = new();

        #region Unity Lifecycle

        private void Awake()
        {
            // Set singleton instance for performance-optimized creature registration
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeEventBus();
            InitializeBiomes();
        }

        private void Start()
        {
            SubscribeToEvents();
            StartEcosystemTracking();
            
            Log("🌍 Ecosystem Manager initialized - tracking begins!");
        }

        private void Update()
        {
            UpdateSeasonal();
            UpdateEcosystemDynamics();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        private void InitializeEventBus()
        {
            if (Laboratory.Core.DI.GlobalServiceProvider.TryResolve<IEventBus>(out eventBus))
            {
                Log("Connected to global event bus");
            }
            else
            {
                Log("Warning: No event bus found, some features may not work");
            }
        }

        private void InitializeBiomes()
        {
            var biomeTypes = Enum.GetValues(typeof(Laboratory.Chimera.Core.BiomeType)).Cast<Laboratory.Chimera.Core.BiomeType>()
                .Where(b => b != Laboratory.Chimera.Core.BiomeType.Void).ToArray();

            foreach (var biomeType in biomeTypes)
            {
                biomes[biomeType] = new BiomeEcosystem
                {
                    BiomeType = biomeType,
                    CarryingCapacity = GetBiomeCarryingCapacity(biomeType),
                    ResourceAvailability = 1f,
                    PollutionLevel = 0f,
                    EcosystemHealth = 1f
                };
            }

            Log($"Initialized {biomes.Count} biome ecosystems");
        }

        private float GetBiomeCarryingCapacity(Laboratory.Chimera.Core.BiomeType biome)
        {
            return biome switch
            {
                Laboratory.Chimera.Core.BiomeType.Forest => 50f,
                Laboratory.Chimera.Core.BiomeType.Ocean => 75f,
                Laboratory.Chimera.Core.BiomeType.Grassland => 60f,
                Laboratory.Chimera.Core.BiomeType.Mountain => 25f,
                Laboratory.Chimera.Core.BiomeType.Desert => 15f,
                Laboratory.Chimera.Core.BiomeType.Arctic => 20f,
                Laboratory.Chimera.Core.BiomeType.Swamp => 40f,
                Laboratory.Chimera.Core.BiomeType.Temperate => 35f,
                _ => 30f
            };
        }

        #endregion

        #region Event Management

        private void SubscribeToEvents()
        {
            if (eventBus == null) return;

            breedingSubscription = eventBus.Subscribe<BreedingSuccessfulEvent>(OnBreedingSuccess);
            maturationSubscription = eventBus.Subscribe<CreatureMaturedEvent>(OnCreatureMaturation);
            adaptationSubscription = eventBus.Subscribe<EnvironmentalAdaptationEvent>(OnEnvironmentalAdaptation);
        }

        // Event subscriptions
        private System.IDisposable breedingSubscription;
        private System.IDisposable maturationSubscription;
        private System.IDisposable adaptationSubscription;
        
        private void UnsubscribeFromEvents()
        {
            breedingSubscription?.Dispose();
            maturationSubscription?.Dispose();
            adaptationSubscription?.Dispose();
        }

        #endregion

        #region Ecosystem Tracking

        private void StartEcosystemTracking()
        {
            RefreshCreatureTracking();
            InvokeRepeating(nameof(PerformEcosystemUpdate), updateInterval, updateInterval);
        }

        /// <summary>
        /// Register a creature with the ecosystem manager - call this when creatures spawn
        /// </summary>
        public static void RegisterCreature(CreatureInstanceComponent creature)
        {
            if (Instance != null && creature != null)
            {
                Instance.trackedCreatures.Add(creature);
                Instance.Log($"Registered creature - now tracking {Instance.trackedCreatures.Count} creatures");
            }
        }

        /// <summary>
        /// Unregister a creature from the ecosystem manager - call this when creatures despawn
        /// </summary>
        public static void UnregisterCreature(CreatureInstanceComponent creature)
        {
            if (Instance != null && creature != null)
            {
                Instance.trackedCreatures.Remove(creature);
                Instance.Log($"Unregistered creature - now tracking {Instance.trackedCreatures.Count} creatures");
            }
        }

        private void RefreshCreatureTracking()
        {
            // Clean up any null references (creatures that were destroyed without proper unregistration)
            trackedCreatures.RemoveAll(creature => creature == null);
            Log($"Cleaned up creature tracking - now tracking {trackedCreatures.Count} valid creatures");
        }

        private void PerformEcosystemUpdate()
        {
            if (!enablePopulationDynamics) return;

            RefreshCreatureTracking();
            UpdateSpeciesPopulations();
            UpdateBiomeHealth();
            ApplyPopulationPressures();
            CheckForEcosystemCrises();

            lastEcosystemUpdate = Time.time;
            Log($"🔄 Ecosystem update complete - {trackedCreatures.Count} creatures tracked");
        }

        private void UpdateSpeciesPopulations()
        {
            globalSpeciesData.Clear();

            // Group creatures by species
            var speciesGroups = trackedCreatures.GroupBy(c => c.CreatureData.Definition?.speciesName ?? "Unknown");

            foreach (var group in speciesGroups)
            {
                var creatures = group.ToList();
                var adults = creatures.Where(c => c.CreatureData.IsAdult).ToList();
                var juveniles = creatures.Where(c => !c.CreatureData.IsAdult).ToList();

                var speciesData = new SpeciesPopulation
                {
                    SpeciesName = group.Key,
                    AdultCount = adults.Count,
                    JuvenileCount = juveniles.Count,
                    AverageHealth = creatures.Average(c => c.CreatureData.Happiness) * globalGrowthRate, // Apply global growth modifier
                    PreferredBiome = creatures.FirstOrDefault()?.CreatureData.Definition?.preferredBiomes?.FirstOrDefault() ?? CoreBiomeType.Forest
                };

                // Calculate genetic diversity
                speciesData.GeneticDiversity = CalculateGeneticDiversity(creatures);

                globalSpeciesData[group.Key] = speciesData;

                // Update biome population data
                var biome = speciesData.PreferredBiome;
                if (biomes.TryGetValue(biome, out var ecosystem))
                {
                    ecosystem.SpeciesPopulations[group.Key] = creatures.Count;
                    ecosystem.CurrentPopulation = ecosystem.SpeciesPopulations.Values.Sum();
                }
            }
        }

        private float CalculateGeneticDiversity(List<CreatureInstanceComponent> creatures)
        {
            if (creatures.Count < 2) return 0f;

            var allGenes = new List<Gene>();
            foreach (var creature in creatures)
            {
                if (creature.CreatureData.GeneticProfile?.Genes != null)
                {
                    allGenes.AddRange(creature.CreatureData.GeneticProfile.Genes);
                }
            }

            if (allGenes.Count == 0) return 0f;

            // Calculate diversity based on unique gene combinations
            var uniqueTraitCombinations = allGenes
                .GroupBy(g => new { g.traitName, ValueRange = Mathf.FloorToInt((g.value ?? 0.5f) * 10) })
                .Count();

            var totalPossibleCombinations = allGenes
                .GroupBy(g => g.traitName)
                .Count() * 10; // 10 value ranges per trait

            return Mathf.Clamp01((float)uniqueTraitCombinations / totalPossibleCombinations);
        }

        private void UpdateBiomeHealth()
        {
            foreach (var kvp in biomes)
            {
                var biome = kvp.Value;
                
                // Health factors
                float populationPressure = biome.CurrentPopulation / biome.CarryingCapacity;
                float diversityBonus = CalculateBiomeDiversity(biome);
                float pollutionPenalty = biome.PollutionLevel;
                float seasonalEffect = GetSeasonalEffect(biome.BiomeType);

                // Calculate health (0-1 range)
                float baseHealth = 1f;
                baseHealth -= Mathf.Max(0f, populationPressure - 1f) * 0.5f; // Overpopulation penalty
                baseHealth += diversityBonus * 0.2f; // Diversity bonus
                baseHealth -= pollutionPenalty * 0.3f; // Pollution penalty
                baseHealth *= seasonalEffect; // Seasonal effects

                biome.EcosystemHealth = Mathf.Clamp01(baseHealth);

                // Update resource availability based on health
                biome.ResourceAvailability = Mathf.Lerp(0.3f, 1.2f, biome.EcosystemHealth);

                // Log critical health states
                if (biome.EcosystemHealth < 0.3f && enableDetailedLogging)
                {
                    Log($"⚠️ {biome.BiomeType} ecosystem health critically low: {biome.EcosystemHealth:P1}");
                }
            }
        }

        private float CalculateBiomeDiversity(BiomeEcosystem biome)
        {
            int speciesCount = biome.SpeciesPopulations.Count;
            if (speciesCount <= 1) return 0f;

            // Shannon diversity index approximation
            float totalPop = biome.SpeciesPopulations.Values.Sum();
            if (totalPop == 0) return 0f;

            float diversity = 0f;
            foreach (var pop in biome.SpeciesPopulations.Values)
            {
                if (pop > 0)
                {
                    float proportion = pop / totalPop;
                    diversity -= proportion * Mathf.Log(proportion, 2);
                }
            }

            return diversity / Mathf.Log(speciesCount, 2); // Normalized to 0-1
        }

        private void ApplyPopulationPressures()
        {
            foreach (var kvp in globalSpeciesData)
            {
                var species = kvp.Value;
                var biome = biomes.GetValueOrDefault(species.PreferredBiome);
                
                if (biome == null) continue;

                // Apply ecosystem pressures to creatures
                float healthModifier = biome.EcosystemHealth;
                float resourceModifier = biome.ResourceAvailability;
                
                ApplyPressureToSpecies(species, healthModifier, resourceModifier);
            }
        }

        private void ApplyPressureToSpecies(SpeciesPopulation species, float healthMod, float resourceMod)
        {
            var speciesCreatures = trackedCreatures
                .Where(c => c.CreatureData.Definition?.speciesName == species.SpeciesName)
                .ToList();

            foreach (var creature in speciesCreatures)
            {
                // Environmental stress affects happiness and aging
                float environmentalStress = 1f - (healthMod * resourceMod);
                
                if (environmentalStress > 0.3f)
                {
                    // High stress accelerates aging and reduces happiness
                    if (UnityEngine.Random.value < environmentalStress * 0.1f)
                    {
                        // Accelerated aging under stress - would need AddAge method
                        // creature.AddAge(1f);
                    }
                }

                // Resource availability affects health
                if (resourceMod < 0.5f && UnityEngine.Random.value < 0.05f)
                {
                    creature.TakeDamage(Mathf.RoundToInt(5f * (1f - resourceMod)));
                }
            }
        }

        private void CheckForEcosystemCrises()
        {
            foreach (var kvp in biomes)
            {
                var biome = kvp.Value;
                
                // Check if population has fallen below extinction threshold
                if (biome.CurrentPopulation > 0 && (biome.CurrentPopulation / biome.CarryingCapacity) < extinctionThreshold)
                {
                    TriggerEcosystemCrisis(biome, EcosystemCrisisType.LossOfDiversity);
                }
                else if (biome.EcosystemHealth < 0.2f)
                {
                    TriggerEcosystemCrisis(biome, EcosystemCrisisType.General);
                }
                else if (biome.CurrentPopulation > biome.CarryingCapacity * 1.5f)
                {
                    TriggerEcosystemCrisis(biome, EcosystemCrisisType.Overpopulation);
                }
                else if (biome.ResourceAvailability < 0.3f)
                {
                    TriggerEcosystemCrisis(biome, EcosystemCrisisType.ResourceDepletion);
                }
                else if (CalculateBiomeDiversity(biome) < extinctionThreshold && biome.CurrentPopulation > 5)
                {
                    TriggerEcosystemCrisis(biome, EcosystemCrisisType.LossOfDiversity);
                }
            }
        }

        private void TriggerEcosystemCrisis(BiomeEcosystem biome, EcosystemCrisisType crisisType)
        {
            Log($"🚨 ECOSYSTEM CRISIS in {biome.BiomeType}: {crisisType}");
            
            var crisisEvent = new EcosystemCrisisEvent
            {
                BiomeType = biome.BiomeType,
                HealthLevel = biome.EcosystemHealth,
                CrisisType = crisisType
            };

            eventBus?.Publish(crisisEvent);

            // Apply crisis effects
            ApplyCrisisEffects(biome, crisisType);
        }

        private void ApplyCrisisEffects(BiomeEcosystem biome, EcosystemCrisisType crisisType)
        {
            switch (crisisType)
            {
                case EcosystemCrisisType.Overpopulation:
                    // Reduce carrying capacity temporarily
                    biome.CarryingCapacity *= 0.8f;
                    biome.ResourceAvailability *= 0.6f;
                    break;

                case EcosystemCrisisType.ResourceDepletion:
                    // Severe resource shortage
                    biome.ResourceAvailability = Mathf.Min(biome.ResourceAvailability, 0.2f);
                    break;

                case EcosystemCrisisType.LossOfDiversity:
                    // Ecosystem becomes unstable
                    biome.EcosystemHealth *= 0.7f;
                    break;

                case EcosystemCrisisType.General:
                    // Overall ecosystem decline
                    biome.EcosystemHealth *= 0.9f;
                    biome.ResourceAvailability *= 0.8f;
                    break;
            }
        }

        #endregion

        #region Seasonal System

        private void UpdateSeasonal()
        {
            if (!enableSeasonalChanges) return;

            currentSeasonTimer += Time.deltaTime;
            
            if (currentSeasonTimer >= seasonLength)
            {
                AdvanceSeason();
                currentSeasonTimer = 0f;
            }
        }

        private void UpdateEcosystemDynamics()
        {
            if (Time.time - lastEcosystemUpdate < updateInterval) return;
            // Ecosystem updates are handled by PerformEcosystemUpdate via InvokeRepeating
        }

        private void AdvanceSeason()
        {
            currentSeasonIndex = (currentSeasonIndex + 1) % seasons.Length;
            string newSeason = seasons[currentSeasonIndex];
            
            seasonalMultiplier = GetSeasonalMultiplier(newSeason);
            
            Log($"🍂 Season changed to {newSeason} (multiplier: {seasonalMultiplier:F2})");

            var seasonEvent = new SeasonChangedEvent
            {
                Season = newSeason,
                SeasonalMultiplier = seasonalMultiplier
            };

            eventBus?.Publish(seasonEvent);

            // Apply seasonal effects to all biomes
            ApplySeasonalEffects(newSeason);
        }

        private float GetSeasonalMultiplier(string season)
        {
            return season switch
            {
                "Spring" => 1.2f, // Growth season
                "Summer" => 1.1f, // Abundance
                "Autumn" => 0.9f, // Preparation
                "Winter" => 0.7f, // Harsh conditions
                _ => 1f
            };
        }

        private float GetSeasonalEffect(Laboratory.Chimera.Core.BiomeType biome)
        {
            string currentSeason = seasons[currentSeasonIndex];

            // Different biomes react differently to seasons
            return (biome, currentSeason) switch
            {
                (Laboratory.Chimera.Core.BiomeType.Arctic, "Winter") => 1.2f, // Arctic creatures thrive in winter
                (Laboratory.Chimera.Core.BiomeType.Arctic, "Summer") => 0.8f,
                (Laboratory.Chimera.Core.BiomeType.Desert, "Summer") => 1.1f, // Desert adapted to heat
                (Laboratory.Chimera.Core.BiomeType.Desert, "Winter") => 0.7f,
                (Laboratory.Chimera.Core.BiomeType.Forest, "Spring") => 1.3f, // Forest blooms in spring
                (Laboratory.Chimera.Core.BiomeType.Forest, "Winter") => 0.8f,
                (Laboratory.Chimera.Core.BiomeType.Ocean, _) => 1f, // Ocean less affected by seasons
                _ => seasonalMultiplier
            };
        }

        private void ApplySeasonalEffects(string season)
        {
            foreach (var biome in biomes.Values)
            {
                float seasonalEffect = GetSeasonalEffect(biome.BiomeType);
                
                // Temporary seasonal adjustments
                biome.ResourceAvailability = Mathf.Clamp(biome.ResourceAvailability * seasonalEffect, 0.1f, 2f);
                
                // Some biomes get pollution reduction in certain seasons
                if (season == "Spring" && biome.PollutionLevel > 0)
                {
                    biome.PollutionLevel = Mathf.Max(0, biome.PollutionLevel - 0.1f);
                }
            }
        }

        #endregion

        #region Player Impact Tracking

        public void RecordPlayerBreeding(CreatureInstance parent1, CreatureInstance parent2)
        {
            if (!enablePlayerImpactTracking) return;

            playerImpact.BreedingImpact += 0.1f * playerImpactSensitivity;
            playerImpact.LastActivity = DateTime.UtcNow;

            // Positive impact on ecosystem through controlled breeding
            var biome = parent1.Definition?.preferredBiomes?.FirstOrDefault() ?? Laboratory.Chimera.Core.BiomeType.Forest;
            if (biomes.TryGetValue(biome, out var ecosystem))
            {
                ecosystem.EcosystemHealth += 0.01f; // Small positive impact
                ecosystem.EcosystemHealth = Mathf.Clamp01(ecosystem.EcosystemHealth);
            }

            Log($"Player breeding impact recorded: {playerImpact.BreedingImpact:F2}");
        }

        public void RecordPlayerHunting(CreatureInstance creature)
        {
            if (!enablePlayerImpactTracking) return;

            playerImpact.HuntingImpact += 0.2f * playerImpactSensitivity;
            playerImpact.LastActivity = DateTime.UtcNow;

            // Negative impact on ecosystem through hunting
            var biome = creature.Definition?.preferredBiomes?.FirstOrDefault() ?? Laboratory.Chimera.Core.BiomeType.Forest;
            if (biomes.TryGetValue(biome, out var ecosystem))
            {
                ecosystem.EcosystemHealth -= 0.05f; // Hunting impact
                ecosystem.CurrentPopulation = Mathf.Max(0, ecosystem.CurrentPopulation - 1);
                ecosystem.EcosystemHealth = Mathf.Clamp01(ecosystem.EcosystemHealth);
            }

            Log($"Player hunting impact recorded: {playerImpact.HuntingImpact:F2}");
        }

        public void RecordPlayerConservation(float effort)
        {
            if (!enablePlayerImpactTracking) return;

            playerImpact.ConservationEffort += effort * playerImpactSensitivity;
            playerImpact.LastActivity = DateTime.UtcNow;

            // Positive impact through conservation efforts
            foreach (var biome in biomes.Values)
            {
                biome.EcosystemHealth += effort * 0.02f;
                biome.PollutionLevel = Mathf.Max(0, biome.PollutionLevel - effort * 0.1f);
                biome.EcosystemHealth = Mathf.Clamp01(biome.EcosystemHealth);
            }

            Log($"Player conservation effort recorded: +{effort:F2}");
        }

        #endregion

        #region Event Handlers

        private void OnBreedingSuccess(BreedingSuccessfulEvent evt)
        {
            RecordPlayerBreeding(evt.Parent1, evt.Parent2);
        }

        private void OnCreatureMaturation(CreatureMaturedEvent evt)
        {
            Log($"🎂 {evt.Creature.Definition?.speciesName} reached maturity");
            
            // Maturation contributes slightly to ecosystem health
            var biome = evt.Creature.Definition?.preferredBiomes?.FirstOrDefault() ?? Laboratory.Chimera.Core.BiomeType.Forest;
            if (biomes.TryGetValue(biome, out var ecosystem))
            {
                ecosystem.EcosystemHealth += 0.005f;
                ecosystem.EcosystemHealth = Mathf.Clamp01(ecosystem.EcosystemHealth);
            }
        }

        private void OnEnvironmentalAdaptation(EnvironmentalAdaptationEvent evt)
        {
            Log($"🌱 {evt.Creature.Definition?.speciesName} adapted to {evt.AdaptationType}");
            
            // Environmental adaptation improves biome health
            if (biomes.TryGetValue(evt.NewBiome, out var ecosystem))
            {
                ecosystem.EcosystemHealth += evt.AdaptationStrength * 0.01f;
                ecosystem.EcosystemHealth = Mathf.Clamp01(ecosystem.EcosystemHealth);
            }
            
            // Environmental adaptation increases genetic diversity
            if (evt.Creature.Definition != null)
            {
                var speciesName = evt.Creature.Definition.speciesName;
                if (globalSpeciesData.TryGetValue(speciesName, out var species))
                {
                    species.GeneticDiversity = Mathf.Min(1f, species.GeneticDiversity + evt.AdaptationStrength * 0.05f);
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets current ecosystem status for UI display
        /// </summary>
        public EcosystemStatus GetEcosystemStatus()
        {
            int healthyBiomes = biomes.Values.Count(b => b.EcosystemHealth > 0.6f);
            float overallHealth = biomes.Values.Average(b => b.EcosystemHealth);
            int totalCreatures = trackedCreatures.Count;
            string currentSeason = seasons[currentSeasonIndex];
            
            return new EcosystemStatus
            {
                TotalCreatures = totalCreatures,
                HealthyBiomes = healthyBiomes,
                TotalBiomes = biomes.Count,
                CurrentSeason = currentSeason,
                OverallHealth = overallHealth
            };
        }

        /// <summary>
        /// Gets detailed biome information
        /// </summary>
        public BiomeEcosystem GetBiomeEcosystem(Laboratory.Chimera.Core.BiomeType biome)
        {
            return biomes.GetValueOrDefault(biome);
        }

        /// <summary>
        /// Gets species population data
        /// </summary>
        public SpeciesPopulation GetSpeciesPopulation(string speciesName)
        {
            return globalSpeciesData.GetValueOrDefault(speciesName);
        }

        /// <summary>
        /// Gets player's ecological impact
        /// </summary>
        public PlayerImpactData GetPlayerImpact()
        {
            return playerImpact;
        }

        /// <summary>
        /// Forces immediate ecosystem update
        /// </summary>
        [ContextMenu("Force Ecosystem Update")]
        public void ForceEcosystemUpdate()
        {
            PerformEcosystemUpdate();
            Log("🔄 Forced ecosystem update completed");
        }

        /// <summary>
        /// Advances to next season immediately
        /// </summary>
        [ContextMenu("Advance Season")]
        public void ForceSeasonAdvance()
        {
            AdvanceSeason();
        }

        /// <summary>
        /// Triggers ecosystem recovery in a specific biome
        /// </summary>
        public void TriggerEcosystemRecovery(Laboratory.Chimera.Core.BiomeType biome, float recoveryAmount = 0.2f)
        {
            if (biomes.TryGetValue(biome, out var ecosystem))
            {
                ecosystem.EcosystemHealth += recoveryAmount;
                ecosystem.PollutionLevel = Mathf.Max(0, ecosystem.PollutionLevel - recoveryAmount * 0.5f);
                ecosystem.ResourceAvailability += recoveryAmount * 0.3f;
                
                ecosystem.EcosystemHealth = Mathf.Clamp01(ecosystem.EcosystemHealth);
                ecosystem.ResourceAvailability = Mathf.Clamp(ecosystem.ResourceAvailability, 0.1f, 2f);

                Log($"🌱 Triggered ecosystem recovery in {biome}: +{recoveryAmount:P1}");
            }
        }

        #endregion

        #region Debug & UI

        private void Log(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[EcosystemManager] {message}");
            }
        }

        private void OnGUI()
        {
            if (!showEcosystemUI) return;

            var status = GetEcosystemStatus();
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Box("🌍 ECOSYSTEM STATUS");
            
            GUILayout.Label($"Season: {status.CurrentSeason}");
            GUILayout.Label($"Total Creatures: {status.TotalCreatures}");
            GUILayout.Label($"Healthy Biomes: {status.HealthyBiomes}/{status.TotalBiomes}");
            GUILayout.Label($"Overall Health: {status.OverallHealth:P1}");
            
            GUILayout.Space(10);
            GUILayout.Label("PLAYER IMPACT:");
            GUILayout.Label($"Breeding: +{playerImpact.BreedingImpact:F1}");
            GUILayout.Label($"Hunting: -{playerImpact.HuntingImpact:F1}");
            GUILayout.Label($"Conservation: +{playerImpact.ConservationEffort:F1}");
            
            GUILayout.EndArea();
        }

        #endregion
    }

    #region Supporting Classes

    [System.Serializable]
    public class BiomeEcosystem
    {
        public Laboratory.Chimera.Core.BiomeType BiomeType { get; set; }
        public float CarryingCapacity { get; set; }
        public float CurrentPopulation { get; set; }
        public float ResourceAvailability { get; set; }
        public float PollutionLevel { get; set; }
        public float EcosystemHealth { get; set; }
        public Dictionary<string, float> SpeciesPopulations { get; set; } = new();
    }

    [System.Serializable]
    public class SpeciesPopulation
    {
        public string SpeciesName { get; set; }
        public int AdultCount { get; set; }
        public int JuvenileCount { get; set; }
        public float GeneticDiversity { get; set; }
        public float AverageHealth { get; set; }
        public Laboratory.Chimera.Core.BiomeType PreferredBiome { get; set; }
    }

    [System.Serializable]
    public class PlayerImpactData
    {
        public float HuntingImpact { get; set; }
        public float BreedingImpact { get; set; }
        public float ConservationEffort { get; set; }
        public DateTime LastActivity { get; set; }
    }

    [System.Serializable]
    public class EcosystemStatus
    {
        public int TotalCreatures { get; set; }
        public int HealthyBiomes { get; set; }
        public int TotalBiomes { get; set; }
        public string CurrentSeason { get; set; }
        public float OverallHealth { get; set; }
    }

    // Event classes
    public class EcosystemCrisisEvent
    {
        public Laboratory.Chimera.Core.BiomeType BiomeType { get; set; }
        public float HealthLevel { get; set; }
        public EcosystemCrisisType CrisisType { get; set; }
        public float Timestamp { get; set; } = Time.time;
    }

    public class SeasonChangedEvent
    {
        public string Season { get; set; }
        public float SeasonalMultiplier { get; set; }
        public float Timestamp { get; set; } = Time.time;
    }

    public enum EcosystemCrisisType
    {
        General,
        Overpopulation,
        ResourceDepletion,
        Pollution,
        LossOfDiversity,
        ClimateChange
    }

    // Missing Event Classes - Added to fix compilation errors
    public class BreedingSuccessfulEvent
    {
        public CreatureInstance Parent1 { get; set; }
        public CreatureInstance Parent2 { get; set; }
        public CreatureInstance Offspring { get; set; }
        public float Timestamp { get; set; } = Time.time;
    }

    public class CreatureMaturedEvent
    {
        public CreatureInstance Creature { get; set; }
        public float Timestamp { get; set; } = Time.time;
    }

    public class EnvironmentalAdaptationEvent
    {
        public CreatureInstance Creature { get; set; }
        public string AdaptationType { get; set; }
        public float AdaptationStrength { get; set; }
        public Laboratory.Chimera.Core.BiomeType NewBiome { get; set; }
        public float Timestamp { get; set; } = Time.time;
    }

    #endregion
}
