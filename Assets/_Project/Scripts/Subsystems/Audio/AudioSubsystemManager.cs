using System;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Audio;

namespace Laboratory.Subsystems.Audio
{
    /// <summary>
    /// Audio Subsystem Manager for Project Chimera.
    /// Integrates the existing comprehensive audio system with the unified subsystem architecture.
    /// Manages music, SFX, ambient audio, and dynamic audio mixing.
    /// </summary>
    public class AudioSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        [Header("Configuration")]
        [SerializeField] private AudioSubsystemConfig config;

        [Header("Core Audio System")]
        [SerializeField] private AudioSystemManager coreAudioSystem;

        [Header("Services")]
        [SerializeField] private bool enableDynamicMusic = true;
        [SerializeField] private bool enableEnvironmentalAudio = true;
        [SerializeField] private bool enableCreatureAudio = true;

        // Public Properties
        public bool IsInitialized { get; private set; }
        public string SubsystemName => "Audio";
        public float InitializationProgress { get; private set; }

        // Services
        public IAudioService AudioService => coreAudioSystem;
        public IDynamicMusicService DynamicMusicService { get; private set; }
        public IEnvironmentalAudioService EnvironmentalAudioService { get; private set; }
        public ICreatureAudioService CreatureAudioService { get; private set; }

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
                Debug.LogError($"[{SubsystemName}] Configuration is missing! Please assign an AudioSubsystemConfig.");
                return;
            }

            if (coreAudioSystem == null)
            {
                coreAudioSystem = FindFirstObjectByType<AudioSystemManager>();
                if (coreAudioSystem == null)
                {
                    Debug.LogError($"[{SubsystemName}] AudioSystemManager not found! Please ensure it exists in the scene.");
                }
            }
        }

        private void InitializeComponents()
        {
            InitializationProgress = 0.2f;

            // The core audio system initializes itself
            // We just need to set up our additional services

            InitializationProgress = 0.4f;
        }

        private async Task InitializeAsync()
        {
            try
            {
                InitializationProgress = 0.5f;

                // Wait for core audio system to initialize
                while (coreAudioSystem != null && !coreAudioSystem.IsInitialized)
                {
                    await Task.Delay(100);
                }

                if (coreAudioSystem == null || !coreAudioSystem.IsInitialized)
                {
                    throw new InvalidOperationException("Core audio system failed to initialize");
                }

                InitializationProgress = 0.6f;

                // Initialize additional services
                if (enableDynamicMusic)
                {
                    await InitializeDynamicMusicService();
                }

                InitializationProgress = 0.7f;

                if (enableEnvironmentalAudio)
                {
                    await InitializeEnvironmentalAudioService();
                }

                InitializationProgress = 0.8f;

                if (enableCreatureAudio)
                {
                    await InitializeCreatureAudioService();
                }

                InitializationProgress = 0.9f;

                // Register services
                RegisterServices();

                // Subscribe to ecosystem and genetics events for dynamic audio
                SubscribeToGameEvents();

                IsInitialized = true;
                InitializationProgress = 1.0f;

                Debug.Log($"[{SubsystemName}] Initialization complete. " +
                         $"Dynamic Music: {enableDynamicMusic}, " +
                         $"Environmental: {enableEnvironmentalAudio}, " +
                         $"Creature Audio: {enableCreatureAudio}");

                // Notify system initialization
                EventBus.Publish(new SubsystemInitializedEvent(SubsystemName));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Initialization failed: {ex.Message}");
                InitializationProgress = 0f;
            }
        }

        private async Task InitializeDynamicMusicService()
        {
            DynamicMusicService = new DynamicMusicService(coreAudioSystem, config);
            await Task.CompletedTask;
        }

        private async Task InitializeEnvironmentalAudioService()
        {
            EnvironmentalAudioService = new EnvironmentalAudioService(coreAudioSystem, config);
            await Task.CompletedTask;
        }

        private async Task InitializeCreatureAudioService()
        {
            CreatureAudioService = new CreatureAudioService(coreAudioSystem, config);
            await Task.CompletedTask;
        }

        private void RegisterServices()
        {
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.Register<IAudioService>(coreAudioSystem);
                ServiceContainer.Instance.Register<IDynamicMusicService>(DynamicMusicService);
                ServiceContainer.Instance.Register<IEnvironmentalAudioService>(EnvironmentalAudioService);
                ServiceContainer.Instance.Register<ICreatureAudioService>(CreatureAudioService);
                ServiceContainer.Instance.Register<AudioSubsystemManager>(this);
            }
        }

        private void SubscribeToGameEvents()
        {
            // Subscribe to ecosystem events for environmental audio
            if (EnvironmentalAudioService != null)
            {
                Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnWeatherChanged += HandleWeatherChanged;
                Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnBiomeChanged += HandleBiomeChanged;
                Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnEnvironmentalEvent += HandleEnvironmentalEvent;
            }

            // Subscribe to genetics events for creature audio
            if (CreatureAudioService != null)
            {
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnBreedingComplete += HandleBreedingComplete;
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnMutationOccurred += HandleMutationOccurred;
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnTraitDiscovered += HandleTraitDiscovered;
            }

            // Subscribe to general game events for dynamic music
            if (DynamicMusicService != null)
            {
                // These would be connected to combat, exploration, etc. when those systems exist
                Debug.Log($"[{SubsystemName}] Dynamic music service ready for game events");
            }
        }

        #endregion

        #region Core Audio Operations

        /// <summary>
        /// Plays music with automatic biome/context selection
        /// </summary>
        public void PlayContextualMusic(string context = null)
        {
            if (!IsInitialized)
                return;

            DynamicMusicService?.PlayContextualMusic(context);
        }

        /// <summary>
        /// Updates environmental audio based on current biome
        /// </summary>
        public void UpdateEnvironmentalAudio(string biomeId, Laboratory.Subsystems.Ecosystem.WeatherData weather = null)
        {
            if (!IsInitialized)
                return;

            EnvironmentalAudioService?.UpdateEnvironmentalAudio(biomeId, weather);
        }

        /// <summary>
        /// Plays creature-specific audio
        /// </summary>
        public void PlayCreatureAudio(string creatureId, CreatureAudioType audioType, Vector3? position = null)
        {
            if (!IsInitialized)
                return;

            CreatureAudioService?.PlayCreatureAudio(creatureId, audioType, position);
        }

        /// <summary>
        /// Sets the current biome for audio context
        /// </summary>
        public void SetCurrentBiome(string biomeId)
        {
            EnvironmentalAudioService?.SetCurrentBiome(biomeId);
            DynamicMusicService?.SetCurrentBiome(biomeId);
        }

        /// <summary>
        /// Triggers special audio for discovery events
        /// </summary>
        public void PlayDiscoveryAudio(string discoveryType, bool isWorldFirst = false)
        {
            if (!IsInitialized)
                return;

            // Play appropriate discovery sound based on type
            var clipName = isWorldFirst ? "Discovery_WorldFirst" : $"Discovery_{discoveryType}";
            coreAudioSystem.PlaySFX(clipName, volume: 0.8f);

            // Trigger dynamic music stinger if it's a major discovery
            if (isWorldFirst || discoveryType == "NewSpecies" || discoveryType == "RareMutation")
            {
                DynamicMusicService?.PlayDiscoveryStinger(discoveryType);
            }
        }

        #endregion

        #region Event Handlers

        private void HandleWeatherChanged(Laboratory.Subsystems.Ecosystem.WeatherEvent weatherEvent)
        {
            EnvironmentalAudioService?.HandleWeatherChange(weatherEvent);
            DynamicMusicService?.HandleWeatherChange(weatherEvent);
        }

        private void HandleBiomeChanged(Laboratory.Subsystems.Ecosystem.BiomeChangedEvent biomeEvent)
        {
            EnvironmentalAudioService?.HandleBiomeChange(biomeEvent);
        }

        private void HandleEnvironmentalEvent(Laboratory.Subsystems.Ecosystem.EnvironmentalEvent environmentalEvent)
        {
            EnvironmentalAudioService?.HandleEnvironmentalEvent(environmentalEvent);
            DynamicMusicService?.HandleEnvironmentalEvent(environmentalEvent);
        }

        private void HandleBreedingComplete(Laboratory.Subsystems.Genetics.GeneticBreedingResult result)
        {
            if (result.isSuccessful)
            {
                CreatureAudioService?.PlayBreedingSuccessAudio(result);

                // Check for special breeding achievements
                if (result.mutations.Count > 0)
                {
                    PlayDiscoveryAudio("Mutation");
                }
            }
        }

        private void HandleMutationOccurred(Laboratory.Subsystems.Genetics.MutationEvent mutationEvent)
        {
            CreatureAudioService?.PlayMutationAudio(mutationEvent);
        }

        private void HandleTraitDiscovered(Laboratory.Subsystems.Genetics.TraitDiscoveryEvent discoveryEvent)
        {
            PlayDiscoveryAudio("Trait", discoveryEvent.isWorldFirst);
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            // Unsubscribe from events
            Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnWeatherChanged -= HandleWeatherChanged;
            Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnBiomeChanged -= HandleBiomeChanged;
            Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnEnvironmentalEvent -= HandleEnvironmentalEvent;

            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnBreedingComplete -= HandleBreedingComplete;
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnMutationOccurred -= HandleMutationOccurred;
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnTraitDiscovered -= HandleTraitDiscovered;

            Debug.Log($"[{SubsystemName}] Cleanup complete");
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Test Environmental Audio")]
        private void TestEnvironmentalAudio()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Audio subsystem not initialized");
                return;
            }

            UpdateEnvironmentalAudio("Forest", new Laboratory.Subsystems.Ecosystem.WeatherData
            {
                weatherType = Laboratory.Subsystems.Ecosystem.WeatherType.Rainy,
                intensity = 0.7f
            });
        }

        [ContextMenu("Test Dynamic Music")]
        private void TestDynamicMusic()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Audio subsystem not initialized");
                return;
            }

            PlayContextualMusic("Exploration");
        }

        [ContextMenu("Test Discovery Audio")]
        private void TestDiscoveryAudio()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Audio subsystem not initialized");
                return;
            }

            PlayDiscoveryAudio("NewSpecies", true);
        }

        #endregion
    }

    #region Service Interfaces

    /// <summary>
    /// Dynamic music service interface
    /// </summary>
    public interface IDynamicMusicService
    {
        void PlayContextualMusic(string context);
        void SetCurrentBiome(string biomeId);
        void PlayDiscoveryStinger(string discoveryType);
        void HandleWeatherChange(Laboratory.Subsystems.Ecosystem.WeatherEvent weatherEvent);
        void HandleEnvironmentalEvent(Laboratory.Subsystems.Ecosystem.EnvironmentalEvent environmentalEvent);
    }

    /// <summary>
    /// Environmental audio service interface
    /// </summary>
    public interface IEnvironmentalAudioService
    {
        void UpdateEnvironmentalAudio(string biomeId, Laboratory.Subsystems.Ecosystem.WeatherData weather);
        void SetCurrentBiome(string biomeId);
        void HandleWeatherChange(Laboratory.Subsystems.Ecosystem.WeatherEvent weatherEvent);
        void HandleBiomeChange(Laboratory.Subsystems.Ecosystem.BiomeChangedEvent biomeEvent);
        void HandleEnvironmentalEvent(Laboratory.Subsystems.Ecosystem.EnvironmentalEvent environmentalEvent);
    }

    /// <summary>
    /// Creature audio service interface
    /// </summary>
    public interface ICreatureAudioService
    {
        void PlayCreatureAudio(string creatureId, CreatureAudioType audioType, Vector3? position);
        void PlayBreedingSuccessAudio(Laboratory.Subsystems.Genetics.GeneticBreedingResult result);
        void PlayMutationAudio(Laboratory.Subsystems.Genetics.MutationEvent mutationEvent);
    }

    /// <summary>
    /// Types of creature audio
    /// </summary>
    public enum CreatureAudioType
    {
        Birth,
        Call,
        Movement,
        Feeding,
        Breeding,
        Death,
        Mutation,
        Discovery
    }

    #endregion

    #region Placeholder Service Implementations

    /// <summary>
    /// Dynamic music service implementation
    /// </summary>
    public class DynamicMusicService : IDynamicMusicService
    {
        private readonly IAudioService _audioService;
        private readonly AudioSubsystemConfig _config;
        private string _currentBiome;
        private string _currentContext;

        public DynamicMusicService(IAudioService audioService, AudioSubsystemConfig config)
        {
            _audioService = audioService;
            _config = config;
        }

        public void PlayContextualMusic(string context)
        {
            _currentContext = context;
            // Implementation would select appropriate music based on context and biome
            Debug.Log($"[DynamicMusicService] Playing contextual music: {context} in {_currentBiome}");
        }

        public void SetCurrentBiome(string biomeId)
        {
            _currentBiome = biomeId;
            // Transition to biome-appropriate music
            Debug.Log($"[DynamicMusicService] Biome set to: {biomeId}");
        }

        public void PlayDiscoveryStinger(string discoveryType)
        {
            // Play a short musical stinger for discoveries
            Debug.Log($"[DynamicMusicService] Playing discovery stinger: {discoveryType}");
        }

        public void HandleWeatherChange(Laboratory.Subsystems.Ecosystem.WeatherEvent weatherEvent)
        {
            // Adjust music based on weather
            Debug.Log($"[DynamicMusicService] Weather changed: {weatherEvent.newWeatherData.weatherType}");
        }

        public void HandleEnvironmentalEvent(Laboratory.Subsystems.Ecosystem.EnvironmentalEvent environmentalEvent)
        {
            // Play dramatic music for major environmental events
            Debug.Log($"[DynamicMusicService] Environmental event: {environmentalEvent.eventType}");
        }
    }

    /// <summary>
    /// Environmental audio service implementation
    /// </summary>
    public class EnvironmentalAudioService : IEnvironmentalAudioService
    {
        private readonly IAudioService _audioService;
        private readonly AudioSubsystemConfig _config;
        private string _currentBiome;

        public EnvironmentalAudioService(IAudioService audioService, AudioSubsystemConfig config)
        {
            _audioService = audioService;
            _config = config;
        }

        public void UpdateEnvironmentalAudio(string biomeId, Laboratory.Subsystems.Ecosystem.WeatherData weather)
        {
            SetCurrentBiome(biomeId);
            // Update ambient sounds based on biome and weather
            Debug.Log($"[EnvironmentalAudioService] Updated environmental audio for {biomeId} with {weather?.weatherType}");
        }

        public void SetCurrentBiome(string biomeId)
        {
            _currentBiome = biomeId;
            // Load and play biome-specific ambient sounds
            Debug.Log($"[EnvironmentalAudioService] Biome set to: {biomeId}");
        }

        public void HandleWeatherChange(Laboratory.Subsystems.Ecosystem.WeatherEvent weatherEvent)
        {
            // Update ambient sounds for weather
            Debug.Log($"[EnvironmentalAudioService] Weather changed: {weatherEvent.newWeatherData.weatherType}");
        }

        public void HandleBiomeChange(Laboratory.Subsystems.Ecosystem.BiomeChangedEvent biomeEvent)
        {
            // Respond to biome health changes, species introductions, etc.
            Debug.Log($"[EnvironmentalAudioService] Biome changed: {biomeEvent.changeType}");
        }

        public void HandleEnvironmentalEvent(Laboratory.Subsystems.Ecosystem.EnvironmentalEvent environmentalEvent)
        {
            // Play sounds for environmental events
            Debug.Log($"[EnvironmentalAudioService] Environmental event: {environmentalEvent.eventType}");
        }
    }

    /// <summary>
    /// Creature audio service implementation
    /// </summary>
    public class CreatureAudioService : ICreatureAudioService
    {
        private readonly IAudioService _audioService;
        private readonly AudioSubsystemConfig _config;

        public CreatureAudioService(IAudioService audioService, AudioSubsystemConfig config)
        {
            _audioService = audioService;
            _config = config;
        }

        public void PlayCreatureAudio(string creatureId, CreatureAudioType audioType, Vector3? position)
        {
            var clipName = $"Creature_{audioType}";
            _audioService.PlaySFX(clipName, position);
            Debug.Log($"[CreatureAudioService] Playing {audioType} audio for {creatureId}");
        }

        public void PlayBreedingSuccessAudio(Laboratory.Subsystems.Genetics.GeneticBreedingResult result)
        {
            _audioService.PlaySFX("Breeding_Success", volume: 0.7f);
            Debug.Log($"[CreatureAudioService] Breeding success audio for {result.offspringId}");
        }

        public void PlayMutationAudio(Laboratory.Subsystems.Genetics.MutationEvent mutationEvent)
        {
            var clipName = mutationEvent.mutation.isHarmful ? "Mutation_Harmful" : "Mutation_Beneficial";
            _audioService.PlaySFX(clipName, volume: 0.6f);
            Debug.Log($"[CreatureAudioService] Mutation audio: {mutationEvent.mutation.mutationType}");
        }
    }

    #endregion
}