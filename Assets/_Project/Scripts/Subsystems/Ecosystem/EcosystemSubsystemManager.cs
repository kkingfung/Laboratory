using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Subsystems.Ecosystem
{
    /// <summary>
    /// Ecosystem Subsystem Manager for Project Chimera.
    /// Manages dynamic biomes, population dynamics, environmental events, and conservation systems.
    /// Integrates with genetics for environmental adaptation and breeding patterns.
    /// </summary>
    public class EcosystemSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        [Header("Configuration")]
        [SerializeField] private EcosystemSubsystemConfig config;

        [Header("Systems")]
        [SerializeField] private BiomeManager biomeManager;
        [SerializeField] private PopulationManager populationManager;
        [SerializeField] private WeatherSystem weatherSystem;
        [SerializeField] private ConservationManager conservationManager;
        [SerializeField] private EnvironmentalEventSystem eventSystem;

        [Header("Services")]
        [SerializeField] private bool enableDynamicWeather = true;
        [SerializeField] private bool enablePopulationSimulation = true;
        [SerializeField] private bool enableEnvironmentalEvents = true;
        [SerializeField] private bool enableConservationTracking = true;

        // Events
        public static event Action<BiomeChangedEvent> OnBiomeChanged;
        public static event Action<PopulationEvent> OnPopulationChanged;
        public static event Action<WeatherEvent> OnWeatherChanged;
        public static event Action<ConservationEvent> OnConservationStatusChanged;
        public static event Action<EnvironmentalEvent> OnEnvironmentalEvent;

        // Public Properties
        public bool IsInitialized { get; private set; }
        public string SubsystemName => "Ecosystem";
        public float InitializationProgress { get; private set; }

        // Services
        public IBiomeService BiomeService => biomeManager;
        public IPopulationService PopulationService => populationManager;
        public IWeatherService WeatherService => weatherSystem;
        public IConservationService ConservationService => conservationManager;
        public IEnvironmentalEventService EventService => eventSystem;

        // Current State
        private readonly Dictionary<string, BiomeData> _activeBiomes = new();
        private readonly Dictionary<string, PopulationData> _populations = new();
        private WeatherData _currentWeather;
        private float _globalTime = 0f;

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateConfiguration();
            InitializeComponents();
        }

        private void Start()
        {
            _ = InitializeAsync();
        }

        private void Update()
        {
            if (IsInitialized)
            {
                UpdateEcosystemSystems(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void ValidateConfiguration()
        {
            if (config == null)
            {
                Debug.LogError($"[{SubsystemName}] Configuration is missing! Please assign an EcosystemSubsystemConfig.");
                return;
            }

            if (config.DefaultBiomes == null || config.DefaultBiomes.Length == 0)
            {
                Debug.LogWarning($"[{SubsystemName}] No default biomes configured. Ecosystem will start empty.");
            }
        }

        private void InitializeComponents()
        {
            InitializationProgress = 0.1f;

            // Initialize core components
            if (biomeManager == null)
                biomeManager = gameObject.AddComponent<BiomeManager>();

            if (populationManager == null)
                populationManager = gameObject.AddComponent<PopulationManager>();

            if (weatherSystem == null)
                weatherSystem = gameObject.AddComponent<WeatherSystem>();

            if (conservationManager == null)
                conservationManager = gameObject.AddComponent<ConservationManager>();

            if (eventSystem == null)
                eventSystem = gameObject.AddComponent<EnvironmentalEventSystem>();

            InitializationProgress = 0.3f;
        }

        private async Task InitializeAsync()
        {
            try
            {
                InitializationProgress = 0.4f;

                // Initialize biome manager
                await biomeManager.InitializeAsync(config);
                InitializationProgress = 0.5f;

                // Initialize population manager
                await populationManager.InitializeAsync(config);
                InitializationProgress = 0.6f;

                // Initialize weather system
                await weatherSystem.InitializeAsync(config);
                InitializationProgress = 0.7f;

                // Initialize conservation manager
                await conservationManager.InitializeAsync(config);
                InitializationProgress = 0.8f;

                // Initialize environmental event system
                await eventSystem.InitializeAsync(config);
                InitializationProgress = 0.85f;

                // Load default biomes
                await LoadDefaultBiomes();
                InitializationProgress = 0.9f;

                // Initialize populations
                await InitializePopulations();
                InitializationProgress = 0.95f;

                // Subscribe to events
                SubscribeToEvents();

                // Register services
                RegisterServices();

                IsInitialized = true;
                InitializationProgress = 1.0f;

                Debug.Log($"[{SubsystemName}] Initialization complete. " +
                         $"Biomes: {_activeBiomes.Count}, " +
                         $"Populations: {_populations.Count}");

                // Notify system initialization
                EventBus.Publish(new SubsystemInitializedEvent(SubsystemName));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Initialization failed: {ex.Message}");
                InitializationProgress = 0f;
            }
        }

        private async Task LoadDefaultBiomes()
        {
            if (config.DefaultBiomes == null)
                return;

            foreach (var biomeConfig in config.DefaultBiomes)
            {
                var biomeData = await biomeManager.CreateBiomeAsync(biomeConfig);
                if (biomeData != null)
                {
                    _activeBiomes[biomeData.biomeId] = biomeData;
                    Debug.Log($"[{SubsystemName}] Loaded biome: {biomeData.biomeName}");
                }
            }
        }

        private async Task InitializePopulations()
        {
            foreach (var biome in _activeBiomes.Values)
            {
                var populations = await populationManager.InitializeBiomePopulationsAsync(biome);
                foreach (var population in populations)
                {
                    _populations[population.populationId] = population;
                }
            }

            Debug.Log($"[{SubsystemName}] Initialized {_populations.Count} populations across {_activeBiomes.Count} biomes");
        }

        private void SubscribeToEvents()
        {
            if (biomeManager != null)
            {
                biomeManager.OnBiomeChanged += HandleBiomeChanged;
            }

            if (populationManager != null)
            {
                populationManager.OnPopulationChanged += HandlePopulationChanged;
            }

            if (weatherSystem != null)
            {
                weatherSystem.OnWeatherChanged += HandleWeatherChanged;
            }

            if (conservationManager != null)
            {
                conservationManager.OnConservationStatusChanged += HandleConservationStatusChanged;
            }

            if (eventSystem != null)
            {
                eventSystem.OnEnvironmentalEvent += HandleEnvironmentalEvent;
            }
        }

        private void RegisterServices()
        {
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.Register<IBiomeService>(biomeManager);
                ServiceContainer.Instance.Register<IPopulationService>(populationManager);
                ServiceContainer.Instance.Register<IWeatherService>(weatherSystem);
                ServiceContainer.Instance.Register<IConservationService>(conservationManager);
                ServiceContainer.Instance.Register<IEnvironmentalEventService>(eventSystem);
                ServiceContainer.Instance.Register<EcosystemSubsystemManager>(this);
            }
        }

        #endregion

        #region Core Ecosystem Operations

        /// <summary>
        /// Updates all ecosystem systems
        /// </summary>
        private void UpdateEcosystemSystems(float deltaTime)
        {
            _globalTime += deltaTime;

            // Update weather system
            if (enableDynamicWeather && weatherSystem != null)
            {
                weatherSystem.UpdateWeather(deltaTime);
            }

            // Update population dynamics
            if (enablePopulationSimulation && populationManager != null)
            {
                populationManager.UpdatePopulations(deltaTime, _currentWeather);
            }

            // Update biome health and changes
            if (biomeManager != null)
            {
                biomeManager.UpdateBiomes(deltaTime, _currentWeather, _populations.Values.ToList());
            }

            // Check for environmental events
            if (enableEnvironmentalEvents && eventSystem != null)
            {
                eventSystem.UpdateEvents(deltaTime, _currentWeather, _populations.Values.ToList());
            }

            // Update conservation status
            if (enableConservationTracking && conservationManager != null)
            {
                conservationManager.UpdateConservationStatus(deltaTime, _populations.Values.ToList());
            }
        }

        /// <summary>
        /// Gets environmental factors for a specific biome
        /// </summary>
        public EnvironmentalFactors GetEnvironmentalFactors(string biomeId)
        {
            if (!_activeBiomes.TryGetValue(biomeId, out var biome))
                return EnvironmentalFactors.CreateDefault();

            return biomeManager.GetEnvironmentalFactors(biome, _currentWeather);
        }

        /// <summary>
        /// Gets population data for a species in a biome
        /// </summary>
        public PopulationData GetPopulationData(string speciesId, string biomeId = null)
        {
            if (string.IsNullOrEmpty(biomeId))
            {
                // Return combined population across all biomes
                var speciesPopulations = _populations.Values.Where(p => p.speciesId == speciesId).ToArray();
                if (speciesPopulations.Length == 0)
                    return null;

                return populationManager.CombinePopulations(speciesPopulations);
            }

            var populationId = $"{speciesId}_{biomeId}";
            return _populations.TryGetValue(populationId, out var population) ? population : null;
        }

        /// <summary>
        /// Triggers a migration event between biomes
        /// </summary>
        public async Task<bool> TriggerMigrationAsync(string speciesId, string fromBiomeId, string toBiomeId, float migrationPercent = 0.1f)
        {
            if (!IsInitialized)
                return false;

            return await populationManager.TriggerMigrationAsync(speciesId, fromBiomeId, toBiomeId, migrationPercent);
        }

        /// <summary>
        /// Introduces a new species to a biome
        /// </summary>
        public async Task<bool> IntroduceSpeciesAsync(string speciesId, string biomeId, int initialPopulation = 10)
        {
            if (!IsInitialized || !_activeBiomes.ContainsKey(biomeId))
                return false;

            var biome = _activeBiomes[biomeId];
            var population = await populationManager.IntroduceSpeciesAsync(speciesId, biome, initialPopulation);

            if (population != null)
            {
                _populations[population.populationId] = population;
                Debug.Log($"[{SubsystemName}] Introduced {speciesId} to {biomeId} (population: {initialPopulation})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets current weather data
        /// </summary>
        public WeatherData GetCurrentWeather()
        {
            return _currentWeather;
        }

        /// <summary>
        /// Forces a weather change
        /// </summary>
        public void SetWeather(WeatherType weatherType, float intensity = 1.0f, float duration = 3600f)
        {
            if (weatherSystem != null)
            {
                weatherSystem.SetWeather(weatherType, intensity, duration);
            }
        }

        /// <summary>
        /// Gets conservation status for all species
        /// </summary>
        public Dictionary<string, ConservationStatus> GetConservationStatus()
        {
            if (!IsInitialized || conservationManager == null)
                return new Dictionary<string, ConservationStatus>();

            return conservationManager.GetAllConservationStatus();
        }

        #endregion

        #region Event Handlers

        private void HandleBiomeChanged(BiomeChangedEvent biomeEvent)
        {
            if (_activeBiomes.ContainsKey(biomeEvent.biomeId))
            {
                _activeBiomes[biomeEvent.biomeId] = biomeEvent.newBiomeData;
            }

            OnBiomeChanged?.Invoke(biomeEvent);

            Debug.Log($"[{SubsystemName}] Biome changed: {biomeEvent.biomeId} ({biomeEvent.changeType})");
        }

        private void HandlePopulationChanged(PopulationEvent populationEvent)
        {
            if (_populations.ContainsKey(populationEvent.populationId))
            {
                _populations[populationEvent.populationId] = populationEvent.newPopulationData;
            }

            OnPopulationChanged?.Invoke(populationEvent);

            if (populationEvent.changeType == PopulationChangeType.Extinction)
            {
                Debug.LogWarning($"[{SubsystemName}] Species extinction: {populationEvent.speciesId} in {populationEvent.biomeId}");
            }
        }

        private void HandleWeatherChanged(WeatherEvent weatherEvent)
        {
            _currentWeather = weatherEvent.newWeatherData;
            OnWeatherChanged?.Invoke(weatherEvent);

            Debug.Log($"[{SubsystemName}] Weather changed: {weatherEvent.newWeatherData.weatherType} " +
                     $"(intensity: {weatherEvent.newWeatherData.intensity:F2})");
        }

        private void HandleConservationStatusChanged(ConservationEvent conservationEvent)
        {
            OnConservationStatusChanged?.Invoke(conservationEvent);

            if (conservationEvent.newStatus == ConservationStatus.CriticallyEndangered ||
                conservationEvent.newStatus == ConservationStatus.Extinct)
            {
                Debug.LogWarning($"[{SubsystemName}] Conservation alert: {conservationEvent.speciesId} " +
                               $"status changed to {conservationEvent.newStatus}");
            }
        }

        private void HandleEnvironmentalEvent(EnvironmentalEvent environmentalEvent)
        {
            OnEnvironmentalEvent?.Invoke(environmentalEvent);

            Debug.Log($"[{SubsystemName}] Environmental event: {environmentalEvent.eventType} " +
                     $"in {environmentalEvent.affectedBiomeId} (severity: {environmentalEvent.severity:F2})");

            // Apply event effects to populations
            ApplyEnvironmentalEventEffects(environmentalEvent);
        }

        private void ApplyEnvironmentalEventEffects(EnvironmentalEvent environmentalEvent)
        {
            var affectedPopulations = _populations.Values
                .Where(p => p.biomeId == environmentalEvent.affectedBiomeId)
                .ToArray();

            foreach (var population in affectedPopulations)
            {
                var effect = CalculateEventEffect(environmentalEvent, population);
                populationManager.ApplyEventEffect(population, effect);
            }
        }

        private PopulationEffect CalculateEventEffect(EnvironmentalEvent environmentalEvent, PopulationData population)
        {
            var effect = new PopulationEffect
            {
                populationMultiplier = 1.0f,
                survivalModifier = 0f,
                reproductionModifier = 0f,
                migrationPressure = 0f
            };

            switch (environmentalEvent.eventType)
            {
                case EnvironmentalEventType.Drought:
                    effect.populationMultiplier = 1.0f - (environmentalEvent.severity * 0.3f);
                    effect.migrationPressure = environmentalEvent.severity * 0.5f;
                    break;

                case EnvironmentalEventType.Flood:
                    effect.populationMultiplier = 1.0f - (environmentalEvent.severity * 0.2f);
                    effect.migrationPressure = environmentalEvent.severity * 0.3f;
                    break;

                case EnvironmentalEventType.Wildfire:
                    effect.populationMultiplier = 1.0f - (environmentalEvent.severity * 0.4f);
                    effect.migrationPressure = environmentalEvent.severity * 0.8f;
                    break;

                case EnvironmentalEventType.Disease:
                    effect.populationMultiplier = 1.0f - (environmentalEvent.severity * 0.5f);
                    effect.reproductionModifier = -environmentalEvent.severity * 0.3f;
                    break;

                case EnvironmentalEventType.FoodAbundance:
                    effect.populationMultiplier = 1.0f + (environmentalEvent.severity * 0.2f);
                    effect.reproductionModifier = environmentalEvent.severity * 0.4f;
                    break;
            }

            return effect;
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            // Unsubscribe from events
            if (biomeManager != null)
                biomeManager.OnBiomeChanged -= HandleBiomeChanged;

            if (populationManager != null)
                populationManager.OnPopulationChanged -= HandlePopulationChanged;

            if (weatherSystem != null)
                weatherSystem.OnWeatherChanged -= HandleWeatherChanged;

            if (conservationManager != null)
                conservationManager.OnConservationStatusChanged -= HandleConservationStatusChanged;

            if (eventSystem != null)
                eventSystem.OnEnvironmentalEvent -= HandleEnvironmentalEvent;

            // Clear collections
            _activeBiomes.Clear();
            _populations.Clear();

            Debug.Log($"[{SubsystemName}] Cleanup complete");
        }

        #endregion

        #region Debug and Testing

        [ContextMenu("Trigger Random Environmental Event")]
        private void TriggerRandomEnvironmentalEvent()
        {
            if (!IsInitialized || _activeBiomes.Count == 0)
            {
                Debug.LogWarning("Ecosystem not initialized or no biomes available");
                return;
            }

            var randomBiome = _activeBiomes.Keys.ElementAt(UnityEngine.Random.Range(0, _activeBiomes.Count));
            var randomEventType = (EnvironmentalEventType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(EnvironmentalEventType)).Length);
            var randomSeverity = UnityEngine.Random.Range(0.3f, 0.8f);

            eventSystem.TriggerEvent(randomEventType, randomBiome, randomSeverity);
        }

        [ContextMenu("Print Population Summary")]
        private void PrintPopulationSummary()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Ecosystem not initialized");
                return;
            }

            foreach (var population in _populations.Values)
            {
                Debug.Log($"Population: {population.speciesId} in {population.biomeId} - " +
                         $"Count: {population.currentPopulation}, Health: {population.healthIndex:F2}, " +
                         $"Status: {population.conservationStatus}");
            }
        }

        [ContextMenu("Force Weather Change")]
        private void ForceWeatherChange()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Ecosystem not initialized");
                return;
            }

            var randomWeather = (WeatherType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(WeatherType)).Length);
            SetWeather(randomWeather, UnityEngine.Random.Range(0.5f, 1.0f), 1800f); // 30 minutes
        }

        #endregion
    }
}