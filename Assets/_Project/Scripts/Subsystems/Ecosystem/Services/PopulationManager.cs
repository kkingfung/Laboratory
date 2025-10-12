using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.Ecosystem
{
    /// <summary>
    /// Manages population dynamics, growth, migration, and conservation for Project Chimera.
    /// Handles realistic population simulation with carrying capacity, environmental effects, and genetic factors.
    /// </summary>
    public class PopulationManager : MonoBehaviour, IPopulationService
    {
        [Header("Configuration")]
        [SerializeField] private bool enablePopulationGrowth = true;
        [SerializeField] private bool enableMigration = true;
        [SerializeField] private bool enableExtinction = true;

        private EcosystemSubsystemConfig _config;
        private readonly Dictionary<string, PopulationData> _populations = new();
        private readonly Dictionary<string, PopulationTrends> _populationTrends = new();
        private readonly Queue<MigrationOperation> _migrationQueue = new();

        private float _lastUpdateTime = 0f;
        private int _populationUpdateCounter = 0;

        // Events
        public event Action<PopulationEvent> OnPopulationChanged;

        // Properties
        public bool IsInitialized { get; private set; }
        public int TotalPopulations => _populations.Count;
        public int ActiveMigrations => _migrationQueue.Count;

        #region Initialization

        public async Task InitializeAsync(EcosystemSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            enablePopulationGrowth = true;
            enableMigration = _config.PopulationConfig.EnableMigration;
            enableExtinction = true; // Always enable for realistic simulation

            // Start background migration processing
            if (enableMigration)
            {
                StartCoroutine(ProcessMigrationQueue());
            }

            IsInitialized = true;
            await Task.CompletedTask;

            Debug.Log("[PopulationManager] Initialized successfully");
        }

        #endregion

        #region Core Population Operations

        /// <summary>
        /// Initializes populations for a biome
        /// </summary>
        public async Task<List<PopulationData>> InitializeBiomePopulationsAsync(BiomeData biome)
        {
            if (!IsInitialized || biome == null)
                return new List<PopulationData>();

            var populations = new List<PopulationData>();

            // Create populations for native species
            foreach (var speciesId in biome.presentSpecies)
            {
                var population = await CreatePopulationAsync(speciesId, biome);
                if (population != null)
                {
                    populations.Add(population);
                    _populations[population.populationId] = population;

                    // Initialize trends tracking
                    _populationTrends[population.populationId] = new PopulationTrends
                    {
                        populationHistory = new float[30], // 30 data points
                        averageGrowthRate = _config.PopulationConfig.BaseGrowthRate,
                        volatility = 0f,
                        seasonalVariation = 0f,
                        isIncreasing = true,
                        isStable = true
                    };
                }
            }

            Debug.Log($"[PopulationManager] Initialized {populations.Count} populations for biome {biome.biomeName}");
            await Task.CompletedTask;
            return populations;
        }

        /// <summary>
        /// Updates population dynamics
        /// </summary>
        public void UpdatePopulations(float deltaTime, WeatherData weather)
        {
            if (!IsInitialized || !enablePopulationGrowth)
                return;

            _lastUpdateTime += deltaTime;

            // Update at configured intervals
            if (_lastUpdateTime >= _config.PerformanceConfig.PopulationUpdateInterval)
            {
                var populationsToUpdate = GetPopulationsForUpdate();

                foreach (var population in populationsToUpdate)
                {
                    UpdatePopulation(population, _lastUpdateTime, weather);
                }

                _lastUpdateTime = 0f;
            }
        }

        /// <summary>
        /// Triggers migration between biomes
        /// </summary>
        public async Task<bool> TriggerMigrationAsync(string speciesId, string fromBiome, string toBiome, float percentage)
        {
            if (!IsInitialized || !enableMigration)
                return false;

            var fromPopulationId = $"{speciesId}_{fromBiome}";
            var toPopulationId = $"{speciesId}_{toBiome}";

            if (!_populations.TryGetValue(fromPopulationId, out var fromPopulation))
                return false;

            var migrationCount = Mathf.RoundToInt(fromPopulation.currentPopulation * percentage);
            if (migrationCount <= 0)
                return false;

            // Queue migration operation
            var migration = new MigrationOperation
            {
                speciesId = speciesId,
                fromBiomeId = fromBiome,
                toBiomeId = toBiome,
                migrationCount = migrationCount,
                startTime = DateTime.UtcNow
            };

            _migrationQueue.Enqueue(migration);

            Debug.Log($"[PopulationManager] Queued migration: {migrationCount} {speciesId} from {fromBiome} to {toBiome}");
            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// Introduces a new species to a biome
        /// </summary>
        public async Task<PopulationData> IntroduceSpeciesAsync(string speciesId, BiomeData biome, int initialCount)
        {
            if (!IsInitialized || biome == null || initialCount <= 0)
                return null;

            var populationId = $"{speciesId}_{biome.biomeId}";

            // Check if population already exists
            if (_populations.ContainsKey(populationId))
            {
                Debug.LogWarning($"[PopulationManager] Population {populationId} already exists");
                return _populations[populationId];
            }

            // Create new population
            var population = new PopulationData
            {
                populationId = populationId,
                speciesId = speciesId,
                biomeId = biome.biomeId,
                currentPopulation = initialCount,
                carryingCapacity = CalculateSpeciesCarryingCapacity(speciesId, biome),
                healthIndex = 1.0f,
                growthRate = _config.PopulationConfig.BaseGrowthRate,
                conservationStatus = DetermineConservationStatus(initialCount),
                trends = new PopulationTrends
                {
                    populationHistory = new float[30],
                    averageGrowthRate = _config.PopulationConfig.BaseGrowthRate,
                    isStable = true
                },
                recentEvents = new List<PopulationEvent>(),
                lastUpdated = DateTime.UtcNow
            };

            // Initialize population history
            for (int i = 0; i < population.trends.populationHistory.Length; i++)
            {
                population.trends.populationHistory[i] = initialCount;
            }

            _populations[populationId] = population;
            _populationTrends[populationId] = population.trends;

            // Fire introduction event
            FirePopulationEvent(population, PopulationChangeType.Introduction, 0, initialCount,
                $"Species {speciesId} introduced to {biome.biomeName}");

            await Task.CompletedTask;
            return population;
        }

        /// <summary>
        /// Applies an environmental event effect to a population
        /// </summary>
        public void ApplyEventEffect(PopulationData population, PopulationEffect effect)
        {
            if (population == null || effect == null)
                return;

            var previousPopulation = population.currentPopulation;

            // Apply population multiplier
            var newPopulation = Mathf.RoundToInt(population.currentPopulation * effect.populationMultiplier);
            population.currentPopulation = Mathf.Max(0, newPopulation);

            // Apply health impact
            population.healthIndex = Mathf.Clamp01(population.healthIndex + effect.healthImpact);

            // Apply growth rate modifier
            var baseGrowthRate = _config.PopulationConfig.BaseGrowthRate;
            population.growthRate = Mathf.Max(0.5f, baseGrowthRate + effect.reproductionModifier);

            // Update conservation status
            var previousStatus = population.conservationStatus;
            population.conservationStatus = DetermineConservationStatus(population.currentPopulation);

            // Check for extinction
            if (population.currentPopulation <= _config.PopulationConfig.ExtinctionThreshold)
            {
                HandleExtinction(population);
            }
            else if (population.currentPopulation != previousPopulation)
            {
                // Fire population change event
                var changeType = population.currentPopulation > previousPopulation
                    ? PopulationChangeType.Growth
                    : PopulationChangeType.Decline;

                FirePopulationEvent(population, changeType, previousPopulation, population.currentPopulation,
                    $"Environmental event affected {population.speciesId} population");
            }

            // Handle migration pressure
            if (effect.migrationPressure > 0.3f && enableMigration)
            {
                // Trigger migration to a random suitable biome (simplified)
                _ = TriggerStressMigration(population, effect.migrationPressure);
            }

            population.lastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Combines multiple population data sets
        /// </summary>
        public PopulationData CombinePopulations(PopulationData[] populations)
        {
            if (populations == null || populations.Length == 0)
                return null;

            if (populations.Length == 1)
                return populations[0];

            var combined = new PopulationData
            {
                populationId = $"combined_{populations[0].speciesId}",
                speciesId = populations[0].speciesId,
                biomeId = "multiple",
                currentPopulation = populations.Sum(p => p.currentPopulation),
                carryingCapacity = populations.Sum(p => p.carryingCapacity),
                healthIndex = populations.Average(p => p.healthIndex),
                growthRate = populations.Average(p => p.growthRate),
                conservationStatus = DetermineCombinedConservationStatus(populations),
                trends = CombinePopulationTrends(populations.Select(p => p.trends).ToArray()),
                recentEvents = populations.SelectMany(p => p.recentEvents ?? new List<PopulationEvent>()).ToList(),
                lastUpdated = DateTime.UtcNow
            };

            return combined;
        }

        #endregion

        #region Population Updates

        private void UpdatePopulation(PopulationData population, float deltaTime, WeatherData weather)
        {
            var previousPopulation = population.currentPopulation;
            var previousStatus = population.conservationStatus;

            // Calculate growth factors
            var environmentalFactor = CalculateEnvironmentalGrowthFactor(population, weather);
            var capacityFactor = CalculateCarryingCapacityFactor(population);
            var healthFactor = CalculateHealthGrowthFactor(population);
            var seasonalFactor = CalculateSeasonalGrowthFactor(weather);

            // Calculate new growth rate
            var effectiveGrowthRate = population.growthRate * environmentalFactor * capacityFactor * healthFactor * seasonalFactor;

            // Apply mortality
            var mortalityRate = _config.PopulationConfig.NaturalMortalityRate;
            var survivalRate = 1.0f - mortalityRate;

            // Calculate population change
            var growthDelta = (effectiveGrowthRate - 1.0f) * deltaTime;
            var mortalityDelta = mortalityRate * deltaTime;

            var netGrowthRate = 1.0f + growthDelta - mortalityDelta;
            var newPopulation = Mathf.RoundToInt(population.currentPopulation * netGrowthRate);

            // Apply minimum viable population rules
            if (newPopulation < _config.PopulationConfig.MinViablePopulation && newPopulation > 0)
            {
                // Small populations have random fluctuations
                var randomFactor = UnityEngine.Random.Range(0.8f, 1.2f);
                newPopulation = Mathf.RoundToInt(newPopulation * randomFactor);
            }

            // Enforce minimum of 0
            population.currentPopulation = Mathf.Max(0, newPopulation);

            // Update health based on population stress
            UpdatePopulationHealth(population, effectiveGrowthRate);

            // Update trends
            UpdatePopulationTrends(population, deltaTime);

            // Update conservation status
            population.conservationStatus = DetermineConservationStatus(population.currentPopulation);

            // Check for significant events
            CheckForPopulationEvents(population, previousPopulation, previousStatus);

            population.lastUpdated = DateTime.UtcNow;
        }

        private float CalculateEnvironmentalGrowthFactor(PopulationData population, WeatherData weather)
        {
            var factor = 1.0f;

            if (weather != null)
            {
                // Temperature effects (species-specific, simplified here)
                var tempStress = CalculateTemperatureStress(weather.temperature);
                factor *= (1.0f - tempStress * _config.PopulationConfig.EnvironmentalSensitivity * 0.2f);

                // Weather-specific effects
                switch (weather.weatherType)
                {
                    case WeatherType.Sunny:
                        factor *= 1.0f + (weather.intensity * 0.1f);
                        break;
                    case WeatherType.Rainy:
                        factor *= 1.0f + (weather.intensity * 0.05f); // Mild boost from rain
                        break;
                    case WeatherType.Stormy:
                        factor *= 1.0f - (weather.intensity * 0.15f); // Storm stress
                        break;
                    case WeatherType.Snowy:
                        factor *= 1.0f - (weather.intensity * 0.2f); // Cold stress
                        break;
                }
            }

            return Mathf.Clamp(factor, 0.3f, 2.0f);
        }

        private float CalculateCarryingCapacityFactor(PopulationData population)
        {
            if (population.carryingCapacity <= 0)
                return 1.0f;

            var capacityRatio = (float)population.currentPopulation / population.carryingCapacity;

            if (capacityRatio <= 0.5f)
            {
                return 1.0f; // Plenty of room for growth
            }
            else if (capacityRatio <= 1.0f)
            {
                // Growth slows as carrying capacity approaches
                return 1.0f - (capacityRatio - 0.5f) * _config.PopulationConfig.CarryingCapacityEffect;
            }
            else
            {
                // Over capacity - negative growth
                return 0.3f - ((capacityRatio - 1.0f) * 0.5f);
            }
        }

        private float CalculateHealthGrowthFactor(PopulationData population)
        {
            // Healthy populations grow better
            return 0.5f + (population.healthIndex * 0.5f);
        }

        private float CalculateSeasonalGrowthFactor(WeatherData weather)
        {
            if (weather?.currentSeason == null || !_config.PopulationConfig.EnableSeasonalChanges)
                return 1.0f;

            return weather.currentSeason switch
            {
                Season.Spring => 1.2f, // Growth season
                Season.Summer => 1.1f, // Mild growth
                Season.Autumn => 0.9f, // Preparation for winter
                Season.Winter => 0.7f, // Survival mode
                _ => 1.0f
            };
        }

        private float CalculateTemperatureStress(float temperature)
        {
            // Simplified temperature stress calculation
            // Optimal range is 15-25°C
            if (temperature >= 15f && temperature <= 25f)
                return 0f;

            if (temperature < 15f)
                return (15f - temperature) / 30f; // Normalize cold stress

            return (temperature - 25f) / 40f; // Normalize heat stress
        }

        private void UpdatePopulationHealth(PopulationData population, float effectiveGrowthRate)
        {
            var healthChange = 0f;

            // Growth rate affects health
            if (effectiveGrowthRate > 1.1f)
            {
                healthChange += 0.01f; // Thriving population
            }
            else if (effectiveGrowthRate < 0.9f)
            {
                healthChange -= 0.02f; // Struggling population
            }

            // Carrying capacity effects
            var capacityRatio = (float)population.currentPopulation / population.carryingCapacity;
            if (capacityRatio > 1.2f)
            {
                healthChange -= 0.03f; // Overcrowding stress
            }
            else if (capacityRatio < 0.1f)
            {
                healthChange -= 0.01f; // Low population stress
            }

            population.healthIndex = Mathf.Clamp01(population.healthIndex + healthChange);
        }

        private void UpdatePopulationTrends(PopulationData population, float deltaTime)
        {
            if (!_populationTrends.TryGetValue(population.populationId, out var trends))
                return;

            // Shift history array
            for (int i = 0; i < trends.populationHistory.Length - 1; i++)
            {
                trends.populationHistory[i] = trends.populationHistory[i + 1];
            }
            trends.populationHistory[trends.populationHistory.Length - 1] = population.currentPopulation;

            // Calculate trends
            if (trends.populationHistory.Length >= 2)
            {
                var growthRates = new List<float>();
                for (int i = 1; i < trends.populationHistory.Length; i++)
                {
                    if (trends.populationHistory[i - 1] > 0)
                    {
                        var growthRate = trends.populationHistory[i] / trends.populationHistory[i - 1];
                        growthRates.Add(growthRate);
                    }
                }

                if (growthRates.Count > 0)
                {
                    trends.averageGrowthRate = growthRates.Average();
                    trends.volatility = CalculateVolatility(growthRates.ToArray());
                    trends.isIncreasing = trends.averageGrowthRate > 1.01f;
                    trends.isStable = trends.volatility < 0.1f && Math.Abs(trends.averageGrowthRate - 1.0f) < 0.05f;
                }
            }

            population.trends = trends;
        }

        #endregion

        #region Migration Processing

        private System.Collections.IEnumerator ProcessMigrationQueue()
        {
            while (true)
            {
                if (_migrationQueue.Count > 0)
                {
                    var migration = _migrationQueue.Dequeue();
                    yield return StartCoroutine(ProcessMigration(migration));
                }

                yield return new WaitForSeconds(1f); // Process every second
            }
        }

        private System.Collections.IEnumerator ProcessMigration(MigrationOperation migration)
        {
            var fromPopulationId = $"{migration.speciesId}_{migration.fromBiomeId}";
            var toPopulationId = $"{migration.speciesId}_{migration.toBiomeId}";

            // Get source population
            if (!_populations.TryGetValue(fromPopulationId, out var fromPopulation))
            {
                Debug.LogWarning($"[PopulationManager] Source population not found: {fromPopulationId}");
                yield break;
            }

            // Ensure we don't migrate more than available
            var actualMigrationCount = Mathf.Min(migration.migrationCount, fromPopulation.currentPopulation);
            if (actualMigrationCount <= 0)
            {
                yield break;
            }

            // Remove from source population
            var previousFromPopulation = fromPopulation.currentPopulation;
            fromPopulation.currentPopulation -= actualMigrationCount;

            // Get or create destination population
            PopulationData toPopulation = null;
            if (_populations.TryGetValue(toPopulationId, out toPopulation))
            {
                // Add to existing population
                var previousToPopulation = toPopulation.currentPopulation;
                toPopulation.currentPopulation += actualMigrationCount;

                FirePopulationEvent(toPopulation, PopulationChangeType.Migration,
                    previousToPopulation, toPopulation.currentPopulation,
                    $"{actualMigrationCount} {migration.speciesId} migrated from {migration.fromBiomeId}");
            }
            else
            {
                Debug.LogWarning($"[PopulationManager] Cannot migrate to non-existent biome: {migration.toBiomeId}");
                // Restore source population
                fromPopulation.currentPopulation += actualMigrationCount;
                yield break;
            }

            // Fire migration events
            FirePopulationEvent(fromPopulation, PopulationChangeType.Migration,
                previousFromPopulation, fromPopulation.currentPopulation,
                $"{actualMigrationCount} {migration.speciesId} migrated to {migration.toBiomeId}");

            Debug.Log($"[PopulationManager] Migration completed: {actualMigrationCount} {migration.speciesId} " +
                     $"from {migration.fromBiomeId} to {migration.toBiomeId}");

            yield return null;
        }

        private async Task TriggerStressMigration(PopulationData population, float migrationPressure)
        {
            // Simplified stress migration - would need biome connectivity data in real implementation
            var migrationPercent = Mathf.Min(migrationPressure * 0.1f, _config.PopulationConfig.MaxMigrationPercent);

            // For now, just reduce population as "migration" without specifying destination
            var migrationCount = Mathf.RoundToInt(population.currentPopulation * migrationPercent);
            if (migrationCount > 0)
            {
                var previousPopulation = population.currentPopulation;
                population.currentPopulation -= migrationCount;

                FirePopulationEvent(population, PopulationChangeType.Migration,
                    previousPopulation, population.currentPopulation,
                    $"Stress migration: {migrationCount} {population.speciesId} left {population.biomeId}");
            }

            await Task.CompletedTask;
        }

        #endregion

        #region Utility Methods

        private List<PopulationData> GetPopulationsForUpdate()
        {
            var maxUpdatesPerFrame = _config.PerformanceConfig.MaxPopulationsPerFrame;
            var allPopulations = _populations.Values.ToList();

            // Round-robin update to ensure fairness
            var startIndex = _populationUpdateCounter % allPopulations.Count;
            var populationsToUpdate = new List<PopulationData>();

            for (int i = 0; i < Math.Min(maxUpdatesPerFrame, allPopulations.Count); i++)
            {
                var index = (startIndex + i) % allPopulations.Count;
                populationsToUpdate.Add(allPopulations[index]);
            }

            _populationUpdateCounter += populationsToUpdate.Count;
            return populationsToUpdate;
        }

        private async Task<PopulationData> CreatePopulationAsync(string speciesId, BiomeData biome)
        {
            var populationId = $"{speciesId}_{biome.biomeId}";

            if (_populations.ContainsKey(populationId))
            {
                return _populations[populationId];
            }

            // Calculate initial population and carrying capacity
            var carryingCapacity = CalculateSpeciesCarryingCapacity(speciesId, biome);
            var initialPopulation = CalculateInitialPopulation(speciesId, biome, carryingCapacity);

            var population = new PopulationData
            {
                populationId = populationId,
                speciesId = speciesId,
                biomeId = biome.biomeId,
                currentPopulation = initialPopulation,
                carryingCapacity = carryingCapacity,
                healthIndex = 1.0f,
                growthRate = _config.PopulationConfig.BaseGrowthRate,
                conservationStatus = DetermineConservationStatus(initialPopulation),
                trends = new PopulationTrends
                {
                    populationHistory = new float[30],
                    averageGrowthRate = _config.PopulationConfig.BaseGrowthRate,
                    isStable = true
                },
                recentEvents = new List<PopulationEvent>(),
                lastUpdated = DateTime.UtcNow
            };

            // Initialize population history
            for (int i = 0; i < population.trends.populationHistory.Length; i++)
            {
                population.trends.populationHistory[i] = initialPopulation;
            }

            await Task.CompletedTask;
            return population;
        }

        private int CalculateSpeciesCarryingCapacity(string speciesId, BiomeData biome)
        {
            // Base carrying capacity per species (would be species-specific in real implementation)
            var baseCapacity = biome.carryingCapacity / Mathf.Max(1, biome.presentSpecies.Count);

            // Species size factor (simplified - larger species need more resources)
            var sizeFactor = GetSpeciesSizeFactor(speciesId);

            return Mathf.RoundToInt(baseCapacity / sizeFactor);
        }

        private int CalculateInitialPopulation(string speciesId, BiomeData biome, int carryingCapacity)
        {
            // Start with 30-70% of carrying capacity for native species
            var factor = UnityEngine.Random.Range(0.3f, 0.7f);
            var initialPop = Mathf.RoundToInt(carryingCapacity * factor);

            // Ensure minimum viable population
            return Mathf.Max(_config.PopulationConfig.MinViablePopulation, initialPop);
        }

        private float GetSpeciesSizeFactor(string speciesId)
        {
            // Simplified species size categorization
            return speciesId.ToLower() switch
            {
                var s when s.Contains("micro") => 0.1f,
                var s when s.Contains("small") => 0.5f,
                var s when s.Contains("large") => 2.0f,
                var s when s.Contains("giant") => 5.0f,
                _ => 1.0f // Medium size
            };
        }

        private ConservationStatus DetermineConservationStatus(int population)
        {
            var config = _config.ConservationConfig;

            if (population >= config.AbundantThreshold)
                return ConservationStatus.Abundant;
            if (population >= config.StableThreshold)
                return ConservationStatus.Stable;
            if (population >= config.ThreatenedThreshold)
                return ConservationStatus.Threatened;
            if (population >= config.EndangeredThreshold)
                return ConservationStatus.Endangered;
            if (population >= config.CriticallyEndangeredThreshold)
                return ConservationStatus.CriticallyEndangered;

            return ConservationStatus.Extinct;
        }

        private ConservationStatus DetermineCombinedConservationStatus(PopulationData[] populations)
        {
            var totalPopulation = populations.Sum(p => p.currentPopulation);
            return DetermineConservationStatus(totalPopulation);
        }

        private PopulationTrends CombinePopulationTrends(PopulationTrends[] trends)
        {
            if (trends.Length == 0)
                return new PopulationTrends();

            var combined = new PopulationTrends
            {
                populationHistory = new float[30],
                averageGrowthRate = trends.Average(t => t.averageGrowthRate),
                volatility = trends.Average(t => t.volatility),
                seasonalVariation = trends.Average(t => t.seasonalVariation),
                isIncreasing = trends.Any(t => t.isIncreasing),
                isStable = trends.All(t => t.isStable)
            };

            // Combine population histories
            for (int i = 0; i < 30; i++)
            {
                combined.populationHistory[i] = trends.Sum(t => t.populationHistory[i]);
            }

            return combined;
        }

        private float CalculateVolatility(float[] growthRates)
        {
            if (growthRates.Length < 2)
                return 0f;

            var mean = growthRates.Average();
            var variance = growthRates.Select(r => Mathf.Pow(r - mean, 2)).Average();
            return Mathf.Sqrt(variance);
        }

        private void CheckForPopulationEvents(PopulationData population, int previousPopulation, ConservationStatus previousStatus)
        {
            // Check for extinction
            if (population.currentPopulation <= _config.PopulationConfig.ExtinctionThreshold && previousPopulation > _config.PopulationConfig.ExtinctionThreshold)
            {
                HandleExtinction(population);
                return;
            }

            // Check for significant population changes
            var changePercent = Mathf.Abs(population.currentPopulation - previousPopulation) / (float)Mathf.Max(1, previousPopulation);

            if (changePercent > 0.2f) // 20% change threshold
            {
                var changeType = population.currentPopulation > previousPopulation
                    ? PopulationChangeType.Growth
                    : PopulationChangeType.Decline;

                FirePopulationEvent(population, changeType, previousPopulation, population.currentPopulation,
                    $"Significant population change: {changePercent:P1}");
            }

            // Check for conservation status changes
            if (population.conservationStatus != previousStatus)
            {
                var statusChangeType = population.conservationStatus < previousStatus
                    ? PopulationChangeType.Decline
                    : PopulationChangeType.Recovery;

                FirePopulationEvent(population, statusChangeType, previousPopulation, population.currentPopulation,
                    $"Conservation status changed from {previousStatus} to {population.conservationStatus}");
            }

            // Check for stabilization
            if (population.trends.isStable && !_populationTrends[population.populationId].isStable)
            {
                FirePopulationEvent(population, PopulationChangeType.Stabilization, previousPopulation, population.currentPopulation,
                    $"Population {population.speciesId} has stabilized in {population.biomeId}");
            }
        }

        private void HandleExtinction(PopulationData population)
        {
            if (!enableExtinction)
                return;

            var previousPopulation = population.currentPopulation;
            population.currentPopulation = 0;
            population.conservationStatus = ConservationStatus.Extinct;
            population.healthIndex = 0f;

            FirePopulationEvent(population, PopulationChangeType.Extinction, previousPopulation, 0,
                $"Species {population.speciesId} extinct in {population.biomeId}");

            Debug.LogWarning($"[PopulationManager] EXTINCTION: {population.speciesId} in {population.biomeId}");
        }

        private void FirePopulationEvent(PopulationData population, PopulationChangeType changeType,
            int previousPopulation, int newPopulation, string description)
        {
            var populationEvent = new PopulationEvent
            {
                populationId = population.populationId,
                speciesId = population.speciesId,
                biomeId = population.biomeId,
                newPopulationData = population,
                changeType = changeType,
                previousPopulation = previousPopulation,
                newPopulation = newPopulation,
                description = description,
                timestamp = DateTime.UtcNow
            };

            // Add to recent events
            population.recentEvents.Add(populationEvent);

            // Limit recent events to last 10
            if (population.recentEvents.Count > 10)
            {
                population.recentEvents.RemoveAt(0);
            }

            OnPopulationChanged?.Invoke(populationEvent);
        }

        #endregion

        private class MigrationOperation
        {
            public string speciesId;
            public string fromBiomeId;
            public string toBiomeId;
            public int migrationCount;
            public DateTime startTime;
        }
    }
}