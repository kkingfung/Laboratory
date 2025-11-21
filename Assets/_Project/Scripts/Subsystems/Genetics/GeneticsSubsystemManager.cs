using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Configuration;

namespace Laboratory.Subsystems.Genetics
{
    /// <summary>
    /// Unified Genetics Subsystem Manager for Project Chimera.
    /// Manages genetic inheritance, breeding calculations, mutations, and trait expression.
    /// Integrates with ECS systems and provides ScriptableObject-based configuration.
    /// </summary>
    public class GeneticsSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        [Header("Configuration")]
        [SerializeField] private GeneticsSubsystemConfig config;

        [Header("Systems")]
        [SerializeField] private BreedingEngine breedingEngine;
        [SerializeField] private MutationProcessor mutationProcessor;
        [SerializeField] private TraitExpressionCalculator traitCalculator;
        [SerializeField] private GeneticDatabaseManager databaseManager;

        [Header("Services")]
        [SerializeField] private bool enableMutationTracking = true;
        [SerializeField] private bool enableGeneticValidation = true;

        // Events
        public static event System.Action<GeneticBreedingResult> OnBreedingComplete;
        public static event System.Action<MutationEvent> OnMutationOccurred;
        public static event System.Action<TraitDiscoveryEvent> OnTraitDiscovered;
        public static event System.Action<GeneticValidationEvent> OnValidationComplete;

        // Public Properties
        public bool IsInitialized { get; private set; }
        public string SubsystemName => "Genetics";
        public float InitializationProgress { get; private set; }

        // Services
        public IBreedingService BreedingService => breedingEngine;
        public IMutationService MutationService => mutationProcessor;
        public ITraitService TraitService => traitCalculator;
        public IGeneticDatabase GeneticDatabase => databaseManager;

        private readonly Dictionary<string, GeneticProfile> _activeProfiles = new();
        private readonly Dictionary<string, Laboratory.Chimera.Configuration.TraitDefinition> _traitDefinitions = new();

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
                Debug.LogError($"[{SubsystemName}] Configuration is missing! Please assign a GeneticsSubsystemConfig.");
                return;
            }

            if (config.DefaultTraitLibrary == null)
            {
                Debug.LogWarning($"[{SubsystemName}] Default trait library is missing. Some features may not work correctly.");
            }
        }

        private void InitializeComponents()
        {
            InitializationProgress = 0.1f;

            // Initialize core components
            if (breedingEngine == null)
                breedingEngine = gameObject.AddComponent<BreedingEngine>();

            if (mutationProcessor == null)
                mutationProcessor = gameObject.AddComponent<MutationProcessor>();

            if (traitCalculator == null)
                traitCalculator = gameObject.AddComponent<TraitExpressionCalculator>();

            if (databaseManager == null)
                databaseManager = gameObject.AddComponent<GeneticDatabaseManager>();

            InitializationProgress = 0.3f;
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            try
            {
                InitializationProgress = 0.4f;

                // Initialize breeding engine
                await breedingEngine.InitializeAsync(config);
                InitializationProgress = 0.5f;

                // Initialize mutation processor
                await mutationProcessor.InitializeAsync(config);
                InitializationProgress = 0.6f;

                // Initialize trait calculator
                await traitCalculator.InitializeAsync(config);
                InitializationProgress = 0.7f;

                // Initialize database manager
                await databaseManager.InitializeAsync(config);
                InitializationProgress = 0.8f;

                // Load trait definitions
                await LoadTraitDefinitions();
                InitializationProgress = 0.9f;

                // Subscribe to events
                SubscribeToEvents();

                // Register with service container
                RegisterServices();

                IsInitialized = true;
                InitializationProgress = 1.0f;

                Debug.Log($"[{SubsystemName}] Initialization complete. " +
                         $"Trait definitions: {_traitDefinitions.Count}, " +
                         $"Active profiles: {_activeProfiles.Count}");

                // Notify system initialization
                EventBus.Publish(new SubsystemInitializedEvent(SubsystemName));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Initialization failed: {ex.Message}");
                InitializationProgress = 0f;
            }
        }

        private async System.Threading.Tasks.Task LoadTraitDefinitions()
        {
            if (config.DefaultTraitLibrary != null)
            {
                var traits = config.DefaultTraitLibrary.GetAllTraits();
                foreach (var trait in traits)
                {
                    _traitDefinitions[trait.traitName] = trait;
                }

                Debug.Log($"[{SubsystemName}] Loaded {traits.Length} trait definitions");
            }

            await System.Threading.Tasks.Task.CompletedTask;
        }

        private void SubscribeToEvents()
        {
            if (breedingEngine != null)
            {
                breedingEngine.OnBreedingComplete += HandleBreedingComplete;
            }

            if (mutationProcessor != null)
            {
                mutationProcessor.OnMutationOccurred += HandleMutationOccurred;
            }

            if (traitCalculator != null)
            {
                traitCalculator.OnTraitDiscovered += HandleTraitDiscovered;
            }
        }

        private void RegisterServices()
        {
            // Register services with dependency injection container
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.RegisterService<IBreedingService>(breedingEngine);
                ServiceContainer.Instance.RegisterService<IMutationService>(mutationProcessor);
                ServiceContainer.Instance.RegisterService<ITraitService>(traitCalculator);
                ServiceContainer.Instance.RegisterService<IGeneticDatabase>(databaseManager);
                ServiceContainer.Instance.RegisterService<GeneticsSubsystemManager>(this);
            }
        }

        #endregion

        #region Core Genetics Operations

        /// <summary>
        /// Creates offspring from two parent genetic profiles
        /// </summary>
        public async System.Threading.Tasks.Task<GeneticBreedingResult> BreedCreaturesAsync(
            string parent1Id, string parent2Id,
            EnvironmentalFactors environment = null)
        {
            if (!IsInitialized)
            {
                Debug.LogError($"[{SubsystemName}] Cannot breed creatures - subsystem not initialized");
                return null;
            }

            if (!_activeProfiles.TryGetValue(parent1Id, out var parent1) ||
                !_activeProfiles.TryGetValue(parent2Id, out var parent2))
            {
                Debug.LogError($"[{SubsystemName}] Parent profiles not found for breeding");
                return null;
            }

            return await breedingEngine.BreedAsync(parent1, parent2, environment);
        }

        /// <summary>
        /// Registers a new genetic profile
        /// </summary>
        public void RegisterGeneticProfile(string creatureId, GeneticProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError($"[{SubsystemName}] Cannot register null genetic profile");
                return;
            }

            _activeProfiles[creatureId] = profile;

            // Validate the profile if enabled
            if (enableGeneticValidation)
            {
                ValidateGeneticProfile(profile, creatureId);
            }

            Debug.Log($"[{SubsystemName}] Registered genetic profile for creature {creatureId}");
        }

        /// <summary>
        /// Gets a genetic profile by creature ID
        /// </summary>
        public GeneticProfile GetGeneticProfile(string creatureId)
        {
            _activeProfiles.TryGetValue(creatureId, out var profile);
            return profile;
        }

        /// <summary>
        /// Applies a mutation to a creature's genetic profile
        /// </summary>
        public async System.Threading.Tasks.Task<bool> ApplyMutationAsync(
            string creatureId, MutationType mutationType,
            string targetTrait = null, float severity = 0.1f)
        {
            if (!_activeProfiles.TryGetValue(creatureId, out var profile))
            {
                Debug.LogWarning($"[{SubsystemName}] No genetic profile found for creature {creatureId}");
                return false;
            }

            return await mutationProcessor.ApplyMutationAsync(profile, mutationType, targetTrait, severity);
        }

        /// <summary>
        /// Calculates trait compatibility between two genetic profiles
        /// </summary>
        public float CalculateBreedingCompatibility(string parent1Id, string parent2Id)
        {
            if (!_activeProfiles.TryGetValue(parent1Id, out var parent1) ||
                !_activeProfiles.TryGetValue(parent2Id, out var parent2))
            {
                return 0f;
            }

            return breedingEngine.CalculateCompatibility(parent1, parent2);
        }

        /// <summary>
        /// Gets predicted breeding outcomes
        /// </summary>
        public BreedingPrediction PredictBreedingOutcome(
            string parent1Id, string parent2Id,
            EnvironmentalFactors environment = null)
        {
            if (!_activeProfiles.TryGetValue(parent1Id, out var parent1) ||
                !_activeProfiles.TryGetValue(parent2Id, out var parent2))
            {
                return null;
            }

            return breedingEngine.PredictOutcome(parent1, parent2, environment);
        }

        #endregion

        #region Trait Management

        /// <summary>
        /// Creates a random genetic profile using trait definitions
        /// </summary>
        public GeneticProfile CreateRandomProfile(string speciesId = null)
        {
            var relevantTraits = GetTraitsForSpecies(speciesId);
            var genes = new List<Gene>();

            foreach (var trait in relevantTraits)
            {
                var gene = CreateGeneFromTrait(trait);
                if (!string.IsNullOrEmpty(gene.traitName))
                {
                    genes.Add(gene);
                }
            }

            return new GeneticProfile(genes.ToArray());
        }

        /// <summary>
        /// Gets trait definitions for a specific species
        /// </summary>
        public List<Laboratory.Chimera.Configuration.TraitDefinition> GetTraitsForSpecies(string speciesId = null)
        {
            // If no species specified, return all traits
            if (string.IsNullOrEmpty(speciesId))
            {
                return new List<Laboratory.Chimera.Configuration.TraitDefinition>(_traitDefinitions.Values);
            }

            // Filter traits based on species compatibility
            var filtered = new List<Laboratory.Chimera.Configuration.TraitDefinition>();
            foreach (var trait in _traitDefinitions.Values)
            {
                // Check if trait is compatible with the species
                if (IsTraitCompatibleWithSpecies(trait, speciesId))
                {
                    filtered.Add(trait);
                }
            }

            return filtered;
        }

        /// <summary>
        /// Checks if a trait is compatible with a specific species
        /// </summary>
        public bool IsTraitCompatibleWithSpecies(Laboratory.Chimera.Configuration.TraitDefinition trait, string speciesId)
        {
            if (trait == null || string.IsNullOrEmpty(speciesId))
                return false;

            // Universal traits are available to all species
            if (trait.compatibleSpecies == null || trait.compatibleSpecies.Length == 0)
                return true;

            // Check if species is in compatible list
            foreach (var compatibleSpecies in trait.compatibleSpecies)
            {
                if (compatibleSpecies.Equals(speciesId, StringComparison.OrdinalIgnoreCase) ||
                    compatibleSpecies.Equals("Universal", StringComparison.OrdinalIgnoreCase) ||
                    compatibleSpecies.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Support wildcard matching (e.g., "Dragon*" matches "DragonFire", "DragonIce")
                if (compatibleSpecies.EndsWith("*"))
                {
                    var prefix = compatibleSpecies.Substring(0, compatibleSpecies.Length - 1);
                    if (speciesId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all species IDs that are compatible with a specific trait
        /// </summary>
        public List<string> GetSpeciesCompatibleWithTrait(string traitName)
        {
            if (!_traitDefinitions.TryGetValue(traitName, out var trait))
                return new List<string>();

            if (trait.compatibleSpecies == null || trait.compatibleSpecies.Length == 0)
            {
                // Universal trait - return all known species
                return GetAllKnownSpecies();
            }

            var compatibleSpecies = new List<string>();
            var allSpecies = GetAllKnownSpecies();

            foreach (var speciesId in allSpecies)
            {
                if (IsTraitCompatibleWithSpecies(trait, speciesId))
                {
                    compatibleSpecies.Add(speciesId);
                }
            }

            return compatibleSpecies;
        }

        /// <summary>
        /// Gets all known species IDs from active genetic profiles
        /// </summary>
        private List<string> GetAllKnownSpecies()
        {
            var species = new HashSet<string>();

            // Add species from active genetic profiles
            foreach (var profile in _activeProfiles.Values)
            {
                if (!string.IsNullOrEmpty(profile.SpeciesId))
                {
                    species.Add(profile.SpeciesId);
                }
            }

            // Add default species from configuration
            if (config?.DefaultSpecies != null)
            {
                foreach (var defaultSpecies in config.DefaultSpecies)
                {
                    if (!string.IsNullOrEmpty(defaultSpecies))
                    {
                        species.Add(defaultSpecies);
                    }
                }
            }

            return new List<string>(species);
        }

        /// <summary>
        /// Adds a new trait definition
        /// </summary>
        public void AddTraitDefinition(Laboratory.Chimera.Configuration.TraitDefinition trait)
        {
            if (trait == null || string.IsNullOrEmpty(trait.traitName))
            {
                Debug.LogError($"[{SubsystemName}] Invalid trait definition");
                return;
            }

            _traitDefinitions[trait.traitName] = trait;
            Debug.Log($"[{SubsystemName}] Added trait definition: {trait.traitName}");
        }

        /// <summary>
        /// Creates a Gene from a TraitDefinition
        /// </summary>
        private Gene CreateGeneFromTrait(Laboratory.Chimera.Configuration.TraitDefinition trait)
        {
            if (trait == null || string.IsNullOrEmpty(trait.traitName))
                return new Gene();

            return new Gene
            {
                traitName = trait.traitName,
                value = UnityEngine.Random.Range(trait.minValue, trait.maxValue),
                dominance = UnityEngine.Random.Range(0.3f, 0.7f),
                isActive = true,
                isMutation = false,
                mutationGeneration = 0
            };
        }

        #endregion

        #region Validation

        private void ValidateGeneticProfile(GeneticProfile profile, string creatureId)
        {
            var issues = new List<string>();

            // Check for null or empty genes
            if (profile.Genes == null || profile.Genes.Count == 0)
            {
                issues.Add("Profile has no genes");
            }

            // Check for duplicate traits
            var traitNames = new HashSet<string>();
            foreach (var gene in profile.Genes)
            {
                if (!traitNames.Add(gene.traitName))
                {
                    issues.Add($"Duplicate trait: {gene.traitName}");
                }
            }

            // Check trait compatibility
            foreach (var gene in profile.Genes)
            {
                if (_traitDefinitions.TryGetValue(gene.traitName, out var traitDef))
                {
                    foreach (var incompatible in traitDef.conflictingTraits)
                    {
                        if (traitNames.Contains(incompatible))
                        {
                            issues.Add($"Incompatible traits: {gene.traitName} and {incompatible}");
                        }
                    }
                }
            }

            // Publish validation results
            var validationEvent = new GeneticValidationEvent
            {
                creatureId = creatureId,
                profile = profile,
                issues = issues.ToArray(),
                isValid = issues.Count == 0
            };

            OnValidationComplete?.Invoke(validationEvent);

            if (issues.Count > 0)
            {
                Debug.LogWarning($"[{SubsystemName}] Validation issues for {creatureId}: {string.Join(", ", issues)}");
            }
        }

        #endregion

        #region Event Handlers

        private void HandleBreedingComplete(GeneticBreedingResult result)
        {
            if (result?.offspring != null)
            {
                // Register the new offspring profile
                RegisterGeneticProfile(result.offspringId, result.offspring);

                Debug.Log($"[{SubsystemName}] Breeding complete: {result.parent1Id} x {result.parent2Id} = {result.offspringId}");
            }

            OnBreedingComplete?.Invoke(result);
        }

        private void HandleMutationOccurred(MutationEvent mutationEvent)
        {
            if (enableMutationTracking)
            {
                Debug.Log($"[{SubsystemName}] Mutation occurred: {mutationEvent.mutation.mutationType} " +
                         $"affecting {mutationEvent.mutation.affectedTrait} (severity: {mutationEvent.mutation.severity:F2})");
            }

            OnMutationOccurred?.Invoke(mutationEvent);
        }

        private void HandleTraitDiscovered(TraitDiscoveryEvent discoveryEvent)
        {
            Debug.Log($"[{SubsystemName}] New trait discovered: {discoveryEvent.traitName} " +
                     $"in generation {discoveryEvent.generation}");

            OnTraitDiscovered?.Invoke(discoveryEvent);
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            // Unsubscribe from events
            if (breedingEngine != null)
                breedingEngine.OnBreedingComplete -= HandleBreedingComplete;

            if (mutationProcessor != null)
                mutationProcessor.OnMutationOccurred -= HandleMutationOccurred;

            if (traitCalculator != null)
                traitCalculator.OnTraitDiscovered -= HandleTraitDiscovered;

            // Clear collections
            _activeProfiles.Clear();
            _traitDefinitions.Clear();

            Debug.Log($"[{SubsystemName}] Cleanup complete");
        }

        #endregion

        #region Debug and Testing

        [ContextMenu("Test Random Breeding")]
        private void TestRandomBreeding()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Genetics subsystem not initialized");
                return;
            }

            var parent1 = CreateRandomProfile("TestSpecies");
            var parent2 = CreateRandomProfile("TestSpecies");

            RegisterGeneticProfile("TestParent1", parent1);
            RegisterGeneticProfile("TestParent2", parent2);

            _ = BreedCreaturesAsync("TestParent1", "TestParent2");
        }

        [ContextMenu("Test Mutation")]
        private void TestMutation()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Genetics subsystem not initialized");
                return;
            }

            var profile = CreateRandomProfile("TestSpecies");
            RegisterGeneticProfile("TestCreature", profile);

            _ = ApplyMutationAsync("TestCreature", MutationType.ValueShift, "Strength", 0.2f);
        }

        #endregion
    }
}