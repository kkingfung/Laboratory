using System;
using UnityEngine;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Configuration;
using Laboratory.Core.Events;
using Laboratory.Core.DI;

namespace Laboratory.Chimera
{
    /// <summary>
    /// Bridge component that connects GameObjects to the Chimera breeding/genetics system.
    /// Add this to any creature GameObject to make it participate in breeding and genetics.
    /// </summary>
    [DisallowMultipleComponent]
    public class CreatureInstanceComponent : MonoBehaviour
    {
        [Header("Creature Data")]
        [SerializeField] private CreatureSpeciesConfig speciesConfig;
        [SerializeField] private bool isWild = true;
        [SerializeField] private bool canBreed = true;
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool enableDetailedLogging = false;
        
        [Header("Visual Customization")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Material[] shinyMaterials;
        [SerializeField] private ParticleSystem[] specialEffects;
        
        // Runtime data
        private CreatureInstance creatureData;
        private IEventBus eventBus;
        private float lastUpdateTime;
        private bool isInitialized = false;
        private float accumulatedAge = 0f; // For fractional aging accumulation
        
        // Visual state
        private bool isShiny = false;
        private Color baseColor = Color.white;
        
        #region Properties
        
        /// <summary>
        /// The creature's data for breeding and genetics
        /// </summary>
        public CreatureInstance CreatureData => creatureData;
        
        /// <summary>
        /// The creature instance for breeding system compatibility
        /// </summary>
        public CreatureInstance Instance => creatureData;
        
        /// <summary>
        /// Whether this creature can currently breed
        /// </summary>
        public bool CanBreed => canBreed && creatureData != null && creatureData.CanBreed;
        
        /// <summary>
        /// Whether this creature has special shiny appearance
        /// </summary>
        public bool IsShiny => isShiny;
        
        /// <summary>
        /// The species configuration this creature is based on
        /// </summary>
        public CreatureSpeciesConfig SpeciesConfig => speciesConfig;

        /// <summary>
        /// Current health of the creature
        /// </summary>
        public int Health => creatureData?.CurrentHealth ?? 100;

        /// <summary>
        /// Whether this creature is currently in breeding cooldown
        /// </summary>
        public bool IsInBreedingCooldown => creatureData?.IsInBreedingCooldown ?? false;

        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            CacheComponents();
            InitializeEventBus();
        }
        
        private void Start()
        {
            if (creatureData == null && speciesConfig != null)
            {
                Initialize(speciesConfig.CreateInstance(isWild));
            }
            
            if (!isInitialized)
            {
                Debug.LogWarning($"{name}: CreatureInstanceComponent not initialized! Call Initialize() or assign speciesConfig.");
            }
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            // Update creature age and stats periodically
            if (Time.time - lastUpdateTime > 1f) // Every second
            {
                UpdateCreatureStats();
                lastUpdateTime = Time.time;
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize this creature with specific data
        /// </summary>
        public void Initialize(CreatureInstance data)
        {
            creatureData = data ?? throw new ArgumentNullException(nameof(data));
            
            // Apply genetic modifiers to stats
            ApplyGeneticModifiers();
            
            // Configure AI behavior based on genetics
            ConfigureAI();
            
            // Apply visual appearance based on genetics
            ApplyVisualGenetics();
            
            // Check for shiny variant
            CheckShinyVariant();
            
            isInitialized = true;
            
            Debug.Log($"✅ Initialized creature: {name} (ID: {creatureData.UniqueId})");
        }
        
        /// <summary>
        /// Initialize with a species config (creates new random creature)
        /// </summary>
        public void Initialize(CreatureSpeciesConfig config, bool wild = true)
        {
            speciesConfig = config;
            isWild = wild;
            Initialize(config.CreateInstance(wild));
        }
        
        /// <summary>
        /// Initialize the creature component from a CreatureInstance data object
        /// </summary>
        public void InitializeFromInstance(CreatureInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            // Use the existing Initialize method which expects CreatureInstance
            Initialize(instance);
            
            if (enableDetailedLogging)
                Debug.Log($"Initialized {name} from CreatureInstance data: Age {instance.AgeInDays} days, Health {instance.CurrentHealth}");
        }
        
        private void CacheComponents()
        {
            // Cache renderers if not manually assigned
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }
        }
        
        private void InitializeEventBus()
        {
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.TryResolve<IEventBus>(out eventBus);
            }
        }
        
        #endregion
        
        #region Genetic Application
        
        private void ApplyGeneticModifiers()
        {
            if (creatureData?.GeneticProfile == null || speciesConfig == null) return;
            
            // Apply genetic modifiers to base stats
            var modifiedStats = creatureData.GeneticProfile.ApplyModifiers(speciesConfig.baseStats);
            
            // Update creature definition with modified stats
            if (creatureData.Definition != null)
            {
                creatureData.Definition.baseStats = modifiedStats;
            }
            
            Debug.Log($"Applied genetic modifiers to {name}: Attack={modifiedStats.attack}, Health={modifiedStats.health}");
        }
        
        private void ConfigureAI()
        {
            // Use reflection to avoid hard dependency on AI assembly
            var aiComponent = GetComponent("ChimeraMonsterAI");
            if (aiComponent == null || creatureData?.GeneticProfile == null) return;
            
            var genetics = creatureData.GeneticProfile;
            
            // Find relevant genes and apply to AI using reflection
            var genes = genetics.Genes;
            foreach (var gene in genes)
            {
                if (!gene.isActive || !gene.value.HasValue) continue;
                
                switch (gene.traitName)
                {
                    case "Aggression":
                        InvokeAIMethod(aiComponent, "SetGeneticAggressionModifier", gene.value.Value * 2f);
                        break;
                    case "Intelligence":
                        float detectionModifier = 0.5f + gene.value.Value;
                        InvokeAIMethod(aiComponent, "SetGeneticDetectionRangeModifier", detectionModifier);
                        break;
                    case "Loyalty":
                        float followDistance = Mathf.Lerp(10f, 2f, gene.value.Value);
                        InvokeAIMethod(aiComponent, "SetGeneticFollowDistance", followDistance);
                        break;
                    case "Curiosity":
                        float patrolRadius = Mathf.Lerp(5f, 30f, gene.value.Value);
                        InvokeAIMethod(aiComponent, "SetGeneticPatrolRadius", patrolRadius);
                        break;
                }
            }
            
            Debug.Log($"Applied genetic AI modifiers to {name}");
        }
        
        private void InvokeAIMethod(Component component, string methodName, float parameter)
        {
            try
            {
                var method = component.GetType().GetMethod(methodName);
                method?.Invoke(component, new object[] { parameter });
            }
            catch (System.Exception e)
            {
                if (enableDetailedLogging)
                    Debug.LogWarning($"Could not invoke AI method {methodName}: {e.Message}");
            }
        }
        
        private void ApplyVisualGenetics()
        {
            if (creatureData?.GeneticProfile == null || renderers == null) return;
            
            var genetics = creatureData.GeneticProfile;
            
            // Find color and size genes
            foreach (var gene in genetics.Genes)
            {
                if (!gene.isActive || !gene.value.HasValue) continue;
                
                switch (gene.traitName)
                {
                    case "Color":
                        Color geneticColor = Color.HSVToRGB(gene.value.Value, 0.8f, 0.9f);
                        ApplyColorToRenderers(geneticColor);
                        baseColor = geneticColor;
                        break;
                    case "Size":
                        float sizeMultiplier = 0.7f + (gene.value.Value * 0.6f); // 0.7x to 1.3x size
                        transform.localScale = Vector3.one * sizeMultiplier;
                        break;
                }
            }
            
            Debug.Log($"Applied visual genetics to {name}");
        }
        
        private void CheckShinyVariant()
        {
            if (speciesConfig == null) return;
            
            // Check if this creature should be shiny
            float shinyRoll = UnityEngine.Random.value;
            if (shinyRoll < speciesConfig.shinyChance)
            {
                MakeShiny();
            }
        }
        
        private void MakeShiny()
        {
            isShiny = true;
            
            // Apply shiny materials if available
            if (shinyMaterials != null && shinyMaterials.Length > 0 && renderers != null)
            {
                for (int i = 0; i < renderers.Length && i < shinyMaterials.Length; i++)
                {
                    if (renderers[i] != null && shinyMaterials[i] != null)
                    {
                        renderers[i].material = shinyMaterials[i];
                    }
                }
            }
            else
            {
                // Apply metallic shiny effect to existing materials
                ApplyShinyEffect();
            }
            
            // Activate special effects
            if (specialEffects != null)
            {
                foreach (var effect in specialEffects)
                {
                    if (effect != null)
                        effect.Play();
                }
            }
            
            Debug.Log($"✨ {name} is SHINY!");
        }
        
        private void ApplyShinyEffect()
        {
            if (renderers == null) return;
            
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.material == null) continue;
                
                // Create shiny material instance
                var shinyMaterial = new Material(renderer.material);
                
                // Apply metallic/shiny properties
                if (shinyMaterial.HasProperty("_Metallic"))
                    shinyMaterial.SetFloat("_Metallic", 0.8f);
                    
                if (shinyMaterial.HasProperty("_Smoothness"))
                    shinyMaterial.SetFloat("_Smoothness", 0.9f);
                    
                // Add sparkle color
                Color shinyColor = Color.Lerp(baseColor, Color.white, 0.3f);
                shinyMaterial.color = shinyColor;
                
                renderer.material = shinyMaterial;
            }
        }
        
        private void ApplyColorToRenderers(Color color)
        {
            if (renderers == null) return;
            
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = color;
                }
            }
        }
        
        #endregion
        
        #region Runtime Updates
        
        private void UpdateCreatureStats()
        {
            if (creatureData == null) return;
            
            // Age the creature (using proper aging rate)
            float agingRatePerSecond = 1f / 86400f; // 1 day per 86400 seconds (24 hours)
            float ageIncrement = agingRatePerSecond * Time.deltaTime;
            
            // Since AgeInDays is int, accumulate fractional days and only increment when we have a full day
            accumulatedAge += ageIncrement;
            if (accumulatedAge >= 1f)
            {
                int daysToAdd = Mathf.FloorToInt(accumulatedAge);
                creatureData.AgeInDays += daysToAdd;
                accumulatedAge -= daysToAdd; // Keep the fractional part
            }
            
            // Update happiness based on environmental factors
            UpdateHappiness();
        }
        
        private void UpdateHappiness()
        {
            if (creatureData == null) return;
            
            float happinessChange = 0f;
            
            // Health factor
            var healthComponent = GetComponent<Laboratory.Core.Health.Components.LocalHealthComponent>();
            if (healthComponent != null)
            {
                float healthRatio = (float)healthComponent.CurrentHealth / (float)healthComponent.MaxHealth;
                if (healthRatio < 0.5f)
                    happinessChange -= 0.01f;
                else if (healthRatio > 0.9f)
                    happinessChange += 0.005f;
            }
            
            // Apply happiness change
            creatureData.Happiness = Mathf.Clamp01(creatureData.Happiness + happinessChange);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get creature info for UI display
        /// </summary>
        public string GetInfoText()
        {
            if (creatureData == null) return "Uninitialized Creature";
            
            var info = $"🐉 {speciesConfig?.speciesName ?? "Unknown"}\n";
            info += $"Age: {creatureData.AgeInDays:F1} days\n";
            info += $"Health: {creatureData.CurrentHealth}\n";
            info += $"Happiness: {creatureData.Happiness:P0}\n";
            info += $"Generation: {creatureData.GeneticProfile?.Generation ?? 1}\n";
            
            if (isShiny)
                info += "✨ SHINY!\n";
                
            if (creatureData.GeneticProfile != null)
            {
                var traits = creatureData.GeneticProfile.GetTraitSummary(3);
                if (!string.IsNullOrEmpty(traits))
                    info += $"Traits: {traits}\n";
            }
            
            return info;
        }
        
        /// <summary>
        /// Force this creature to be shiny (for testing)
        /// </summary>
        [ContextMenu("Make Shiny")]
        public void ForceShiny()
        {
            if (!isShiny)
                MakeShiny();
        }
        
        /// <summary>
        /// Set happiness directly (for testing)
        /// </summary>
        public void SetHappiness(float happiness)
        {
            if (creatureData != null)
                creatureData.Happiness = Mathf.Clamp01(happiness);
        }
        
        /// <summary>
        /// Apply damage to this creature through the health system
        /// </summary>
        public void TakeDamage(int damage)
        {
            var healthComponent = GetComponent<Laboratory.Core.Health.Components.LocalHealthComponent>();
            if (healthComponent != null)
            {
                // Create a damage request using default constructor to avoid Entity dependencies
                var damageRequest = new Laboratory.Core.Health.DamageRequest()
                {
                    Amount = damage,
                    Source = gameObject,
                    Type = Laboratory.Core.Health.DamageType.Physical,
                    Description = "Environmental stress"
                };
                
                healthComponent.TakeDamage(damageRequest);
                
                if (enableDetailedLogging)
                    Debug.Log($"{name} took {damage} damage from environmental stress");
            }
            else
            {
                // If no health component, affect happiness directly
                if (creatureData != null)
                {
                    float happinessLoss = damage * 0.01f; // 1 damage = 1% happiness loss
                    creatureData.Happiness = Mathf.Clamp01(creatureData.Happiness - happinessLoss);
                    
                    if (enableDetailedLogging)
                        Debug.Log($"{name} lost {happinessLoss:P1} happiness due to environmental stress (no health component)");
                }
            }
        }
        
        #endregion
        
        #region Debug & Gizmos
        
        private void OnDrawGizmosSelected()
        {
            if (!showDebugInfo || creatureData == null) return;
            
            // Draw creature info above the creature
            var infoPos = transform.position + Vector3.up * 3f;
            
#if UNITY_EDITOR
            UnityEditor.Handles.Label(infoPos, GetInfoText());
#endif
        }
        
        #endregion
    }
}
