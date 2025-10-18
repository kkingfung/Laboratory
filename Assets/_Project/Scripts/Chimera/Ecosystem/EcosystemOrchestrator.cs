using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Chimera.Ecosystem.Data;
using Laboratory.Chimera.Ecosystem.Systems;

namespace Laboratory.Chimera.Ecosystem
{
    /// <summary>
    /// Main orchestrator for the ecosystem evolution engine - coordinates all subsystems
    /// This replaces the monolithic EcosystemEvolutionEngine.cs file
    /// </summary>
    [CreateAssetMenu(fileName = "EcosystemOrchestrator", menuName = "Chimera/Ecosystem/Ecosystem Orchestrator")]
    public class EcosystemOrchestrator : ScriptableObject
    {
        [Header("System Configuration")]
        [SerializeField] private bool enableClimateEvolution = true;
        [SerializeField] private bool enableBiomeTransitions = true;
        [SerializeField] private bool enableResourceFlow = true;
        [SerializeField] private bool enableSpeciesInteractions = true;
        [SerializeField] private bool enableHealthMonitoring = true;
        [SerializeField] private bool enableCatastrophes = true;

        [Header("Performance Settings")]
        [SerializeField] private float masterTimeScale = 1.0f;
        [SerializeField] private float simulationSpeed = 1.0f;
        [SerializeField] private bool enableDebugLogging = false;

        [Header("Ecosystem Parameters")]
        [SerializeField] private EcosystemConfiguration ecosystemConfig;

        // System references (will be found/created at runtime)
        private ClimateEvolutionSystem climateSystem;
        private BiomeTransitionSystem biomeSystem;
        private ResourceFlowSystem resourceSystem;
        private SpeciesInteractionSystem speciesSystem;
        private EcosystemHealthMonitor healthMonitor;
        private CatastropheSystem catastropheSystem;

        // Orchestrator state
        private bool isInitialized = false;
        private float simulationTime = 0f;
        private EcosystemState globalState;

        // Events
        public System.Action<EcosystemState> OnEcosystemStateChanged;
        public System.Action<string> OnSystemStatusChanged;
        public System.Action<float> OnSimulationTimeUpdated;

        public void Initialize()
        {
            if (isInitialized) return;

            FindOrCreateEcosystemSystems();
            ConnectSystemEvents();
            ConfigureSubsystems();
            InitializeGlobalState();

            isInitialized = true;
            Debug.Log("🌍 Ecosystem Orchestrator initialized with modular architecture");
        }

        private void FindOrCreateEcosystemSystems()
        {
            var ecosystemParent = GameObject.Find("EcosystemSystems");
            if (ecosystemParent == null)
            {
                ecosystemParent = new GameObject("EcosystemSystems");
            }

            if (enableClimateEvolution)
            {
                climateSystem = ecosystemParent.GetComponent<ClimateEvolutionSystem>() ??
                              ecosystemParent.AddComponent<ClimateEvolutionSystem>();
            }

            if (enableBiomeTransitions)
            {
                biomeSystem = ecosystemParent.GetComponent<BiomeTransitionSystem>() ??
                            ecosystemParent.AddComponent<BiomeTransitionSystem>();
            }

            if (enableResourceFlow)
            {
                resourceSystem = ecosystemParent.GetComponent<ResourceFlowSystem>() ??
                               ecosystemParent.AddComponent<ResourceFlowSystem>();
            }

            if (enableSpeciesInteractions)
            {
                speciesSystem = ecosystemParent.GetComponent<SpeciesInteractionSystem>() ??
                              ecosystemParent.AddComponent<SpeciesInteractionSystem>();
            }

            if (enableHealthMonitoring)
            {
                healthMonitor = ecosystemParent.GetComponent<EcosystemHealthMonitor>() ??
                              ecosystemParent.AddComponent<EcosystemHealthMonitor>();
            }

            if (enableCatastrophes)
            {
                catastropheSystem = ecosystemParent.GetComponent<CatastropheSystem>() ??
                                  ecosystemParent.AddComponent<CatastropheSystem>();
            }
        }

        private void ConnectSystemEvents()
        {
            // Connect climate events
            if (climateSystem != null)
            {
                climateSystem.OnClimateChanged += OnClimateChanged;
                climateSystem.OnSeasonChanged += OnSeasonChanged;
                climateSystem.OnWeatherSystemFormed += OnWeatherSystemFormed;
            }

            // Connect biome events
            if (biomeSystem != null)
            {
                biomeSystem.OnBiomeTransitionStarted += OnBiomeTransitionStarted;
                biomeSystem.OnBiomeTransitionCompleted += OnBiomeTransitionCompleted;
            }

            // Connect resource events
            if (resourceSystem != null)
            {
                resourceSystem.OnResourceLevelChanged += OnResourceLevelChanged;
                resourceSystem.OnResourceDepleted += OnResourceDepleted;
                resourceSystem.OnGlobalResourceChanged += OnGlobalResourceChanged;
            }

            // Connect species events
            if (speciesSystem != null)
            {
                speciesSystem.OnInteractionOccurred += OnSpeciesInteraction;
                speciesSystem.OnPopulationChanged += OnPopulationChanged;
                speciesSystem.OnSpeciesExtinction += OnSpeciesExtinction;
                speciesSystem.OnMigrationTriggered += OnMigrationTriggered;
            }

            // Connect health events
            if (healthMonitor != null)
            {
                healthMonitor.OnHealthAssessmentComplete += OnHealthAssessmentComplete;
                healthMonitor.OnHealthWarning += OnHealthWarning;
                healthMonitor.OnHealthCritical += OnHealthCritical;
            }

            // Connect catastrophe events
            if (catastropheSystem != null)
            {
                catastropheSystem.OnCatastropheTriggered += OnCatastropheTriggered;
                catastropheSystem.OnCatastropheEnded += OnCatastropheEnded;
                catastropheSystem.OnRecoveryProgress += OnRecoveryProgress;
            }
        }

        private void ConfigureSubsystems()
        {
            if (ecosystemConfig.TimeScale > 0)
            {
                masterTimeScale = ecosystemConfig.TimeScale;
            }

            if (ecosystemConfig.SimulationSpeed > 0)
            {
                simulationSpeed = ecosystemConfig.SimulationSpeed;
            }

            // Apply configuration to subsystems
            ApplyTimeScaling();
            OnSystemStatusChanged?.Invoke("All ecosystem systems configured and connected");
        }

        private void InitializeGlobalState()
        {
            globalState = new EcosystemState
            {
                Temperature = 15.0f,
                Humidity = 0.6f,
                Rainfall = 0.5f,
                SoilQuality = 0.8f,
                Biodiversity = 0.7f,
                Stability = 0.8f,
                CurrentSeason = SeasonType.Spring,
                SeasonProgress = 0f,
                ClimateZone = ClimateType.Temperate,
                LastUpdate = System.DateTime.Now
            };
        }

        public void UpdateEcosystem()
        {
            if (!isInitialized) return;

            simulationTime += Time.deltaTime * simulationSpeed;
            UpdateGlobalState();

            OnSimulationTimeUpdated?.Invoke(simulationTime);
            OnEcosystemStateChanged?.Invoke(globalState);
        }

        private void UpdateGlobalState()
        {
            // Aggregate data from all subsystems to update global state
            if (climateSystem != null)
            {
                var climate = climateSystem.GetCurrentClimate();
                var season = climateSystem.GetCurrentSeason();
                var seasonProgress = climateSystem.GetSeasonProgress();

                globalState.Temperature = climate.GlobalTemperature;
                globalState.CurrentSeason = season;
                globalState.SeasonProgress = seasonProgress;
                globalState.Stability = climate.ClimateStability;
            }

            if (healthMonitor != null)
            {
                var health = healthMonitor.GetCurrentHealth();
                globalState.Biodiversity = health.BiodiversityIndex;
                globalState.Stability = Mathf.Min(globalState.Stability, health.PopulationStability);
            }

            // Calculate average resource availability as soil quality proxy
            if (resourceSystem != null)
            {
                var resources = resourceSystem.GetGlobalResourceLevels();
                if (resources.Count > 0)
                {
                    float totalQuality = 0f;
                    foreach (var resource in resources.Values)
                    {
                        totalQuality += Mathf.Clamp01(resource / 500f); // Normalize
                    }
                    globalState.SoilQuality = totalQuality / resources.Count;
                }
            }

            globalState.LastUpdate = System.DateTime.Now;
        }

        private void ApplyTimeScaling()
        {
            Time.timeScale = masterTimeScale;
        }

        #region System Event Handlers

        private void OnClimateChanged(ClimateData climate)
        {
            if (enableDebugLogging)
                Debug.Log($"🌡️ Climate changed: {climate.GlobalTemperature:F1}°C, Stability: {climate.ClimateStability:F2}");
        }

        private void OnSeasonChanged(SeasonType season)
        {
            if (enableDebugLogging)
                Debug.Log($"🍂 Season changed to {season}");
        }

        private void OnWeatherSystemFormed(WeatherPattern weather)
        {
            if (enableDebugLogging)
                Debug.Log($"🌦️ Weather system formed: {weather.Type} at {weather.Location}");
        }

        private void OnBiomeTransitionStarted(Vector2 location, BiomeType from, BiomeType to)
        {
            if (enableDebugLogging)
                Debug.Log($"🌿 Biome transition started at {location}: {from} → {to}");
        }

        private void OnBiomeTransitionCompleted(Vector2 location, BiomeType newBiome)
        {
            if (enableDebugLogging)
                Debug.Log($"🌿 Biome transition completed at {location}: now {newBiome}");
        }

        private void OnResourceLevelChanged(Vector2 location, ResourceType resource, float level)
        {
            if (enableDebugLogging && level < 50f)
                Debug.Log($"💧 Low resource at {location}: {resource} = {level:F1}");
        }

        private void OnResourceDepleted(Vector2 location, ResourceType resource)
        {
            Debug.LogWarning($"⚠️ Resource depleted at {location}: {resource}");
        }

        private void OnGlobalResourceChanged(ResourceType resource, float level)
        {
            if (enableDebugLogging)
                Debug.Log($"🌍 Global {resource}: {level:F1}");
        }

        private void OnSpeciesInteraction(uint species1, uint species2, InteractionType type, float strength)
        {
            if (enableDebugLogging)
                Debug.Log($"🦎 Species interaction: {species1} ↔ {species2} ({type}, {strength:F2})");
        }

        private void OnPopulationChanged(uint speciesId, float population)
        {
            if (enableDebugLogging && population < 10f)
                Debug.LogWarning($"⚠️ Low population for species {speciesId}: {population:F1}");
        }

        private void OnSpeciesExtinction(uint speciesId)
        {
            Debug.LogError($"💀 Species extinction: {speciesId}");
        }

        private void OnMigrationTriggered(uint speciesId, Vector2 from, Vector2 to)
        {
            if (enableDebugLogging)
                Debug.Log($"🦋 Migration: Species {speciesId} from {from} to {to}");
        }

        private void OnHealthAssessmentComplete(EcosystemHealth health)
        {
            if (enableDebugLogging)
                Debug.Log($"🌿 Ecosystem health: {health.OverallHealthScore:F2}");
        }

        private void OnHealthWarning(string warning)
        {
            Debug.LogWarning($"⚠️ Ecosystem warning: {warning}");
        }

        private void OnHealthCritical(string critical)
        {
            Debug.LogError($"🚨 Ecosystem critical: {critical}");
        }

        private void OnCatastropheTriggered(CatastrophicEvent catastrophe)
        {
            Debug.LogWarning($"💥 Catastrophe: {catastrophe.Type} at {catastrophe.EpicenterLocation}");
        }

        private void OnCatastropheEnded(CatastrophicEvent catastrophe)
        {
            if (enableDebugLogging)
                Debug.Log($"✅ Catastrophe ended: {catastrophe.Type}");
        }

        private void OnRecoveryProgress(Vector2 location, CatastropheType type, float progress)
        {
            if (enableDebugLogging && progress >= 1f)
                Debug.Log($"🌱 Recovery complete: {type} at {location}");
        }

        #endregion

        #region Public API Methods

        public void StartSimulation()
        {
            if (!isInitialized) Initialize();
            OnSystemStatusChanged?.Invoke("Ecosystem simulation started");
        }

        public void PauseSimulation()
        {
            simulationSpeed = 0f;
            OnSystemStatusChanged?.Invoke("Ecosystem simulation paused");
        }

        public void ResumeSimulation()
        {
            simulationSpeed = ecosystemConfig.SimulationSpeed > 0 ? ecosystemConfig.SimulationSpeed : 1f;
            OnSystemStatusChanged?.Invoke("Ecosystem simulation resumed");
        }

        public void SetSimulationSpeed(float speed)
        {
            simulationSpeed = Mathf.Max(0f, speed);
            OnSystemStatusChanged?.Invoke($"Simulation speed: {simulationSpeed:F1}x");
        }

        public void SetTimeScale(float scale)
        {
            masterTimeScale = Mathf.Clamp(scale, 0.1f, 10f);
            ApplyTimeScaling();
            OnSystemStatusChanged?.Invoke($"Time scale: {masterTimeScale:F1}x");
        }

        public void TriggerCatastrophe(CatastropheType type, Vector2 location, float intensity = 1.0f)
        {
            catastropheSystem?.TriggerCatastropheAt(type, location, intensity);
        }

        public void ForceClimateChange(float temperatureChange, float stabilityChange)
        {
            if (climateSystem != null)
            {
                climateSystem.SetClimateChangeRate(temperatureChange);
                // Would set stability if such method existed
            }
        }

        public void AddResourceSource(Vector2 location, ResourceType type, float amount)
        {
            resourceSystem?.AddResourceSource(location, type, amount);
        }

        #endregion

        #region Query Methods

        public EcosystemState GetGlobalState() => globalState;

        public bool IsInitialized => isInitialized;

        public float GetSimulationTime() => simulationTime;

        public Dictionary<string, object> GetSystemStatus()
        {
            var status = new Dictionary<string, object>
            {
                ["IsInitialized"] = isInitialized,
                ["SimulationTime"] = simulationTime,
                ["SimulationSpeed"] = simulationSpeed,
                ["MasterTimeScale"] = masterTimeScale,
                ["ClimateSystemActive"] = climateSystem != null,
                ["BiomeSystemActive"] = biomeSystem != null,
                ["ResourceSystemActive"] = resourceSystem != null,
                ["SpeciesSystemActive"] = speciesSystem != null,
                ["HealthMonitorActive"] = healthMonitor != null,
                ["CatastropheSystemActive"] = catastropheSystem != null
            };

            if (healthMonitor != null)
            {
                var health = healthMonitor.GetCurrentHealth();
                status["EcosystemHealth"] = health.OverallHealthScore;
                status["Biodiversity"] = health.BiodiversityIndex;
                status["Stability"] = health.PopulationStability;
            }

            if (climateSystem != null)
            {
                var climate = climateSystem.GetCurrentClimate();
                status["GlobalTemperature"] = climate.GlobalTemperature;
                status["ClimateStability"] = climate.ClimateStability;
                status["CurrentSeason"] = climateSystem.GetCurrentSeason().ToString();
            }

            return status;
        }

        public ClimateEvolutionSystem GetClimateSystem() => climateSystem;
        public BiomeTransitionSystem GetBiomeSystem() => biomeSystem;
        public ResourceFlowSystem GetResourceSystem() => resourceSystem;
        public SpeciesInteractionSystem GetSpeciesSystem() => speciesSystem;
        public EcosystemHealthMonitor GetHealthMonitor() => healthMonitor;
        public CatastropheSystem GetCatastropheSystem() => catastropheSystem;

        #endregion

        #region Configuration

        public void EnableSystem(string systemName, bool enable)
        {
            switch (systemName.ToLower())
            {
                case "climate":
                    enableClimateEvolution = enable;
                    break;
                case "biome":
                    enableBiomeTransitions = enable;
                    break;
                case "resource":
                    enableResourceFlow = enable;
                    break;
                case "species":
                    enableSpeciesInteractions = enable;
                    break;
                case "health":
                    enableHealthMonitoring = enable;
                    break;
                case "catastrophe":
                    enableCatastrophes = enable;
                    break;
            }

            OnSystemStatusChanged?.Invoke($"{systemName} system {(enable ? "enabled" : "disabled")}");
        }

        public void SetEcosystemConfiguration(EcosystemConfiguration config)
        {
            ecosystemConfig = config;
            ConfigureSubsystems();
        }

        #endregion
    }
}