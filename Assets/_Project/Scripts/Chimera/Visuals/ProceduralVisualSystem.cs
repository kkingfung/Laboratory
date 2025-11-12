using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Core.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Laboratory.Chimera.Visuals
{
    /// <summary>
    /// Core procedural visual system for Project Chimera.
    /// Generates infinite visual diversity from genetic profiles with advanced effects.
    /// Refactored to use service-based composition for maintainability.
    /// </summary>
    [RequireComponent(typeof(CreatureInstanceComponent))]
    public class ProceduralVisualSystem : MonoBehaviour
    {
        [Header("Visual Generation Settings")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool enableRealTimeUpdates = false;
        [SerializeField] private float updateInterval = 1.0f;
        [SerializeField] private bool enableDebugMode = false;

        [Header("Feature Systems")]
        [SerializeField] private bool enableAdvancedPatterns = true;
        [SerializeField] private bool enableParticleEffects = true;
        [SerializeField] private bool enableMaterialGenetics = true;
        [SerializeField] private bool enableEnvironmentalAdaptation = true;

        [Header("Performance")]
        [SerializeField] private int maxSimultaneousEffects = 5;
        [SerializeField] private bool enableLODOptimization = true;
        [SerializeField] private float visualComplexityMultiplier = 1.0f;

        [Header("Visual Components")]
        [SerializeField] private Transform[] scalableBodyParts;
        [SerializeField] private Renderer[] primaryRenderers;
        [SerializeField] private ParticleSystem[] effectSystems;
        [SerializeField] private Light[] magicalLights;

        // System components
        private CreatureInstanceComponent creatureInstance;
        private AdvancedPatternSystem patternSystem;

        // Services (composition-based)
        private GeneticTraitApplicationService geneticTraitService;
        private MaterialGeneticsService materialGeneticsService;
        private VisualEffectsService visualEffectsService;

        // Visual state
        private string currentVisualHash = "";
        private bool isInitialized = false;
        private float lastUpdateTime = 0f;

        #region Initialization

        private void Awake()
        {
            creatureInstance = GetComponent<CreatureInstanceComponent>();
            patternSystem = GetComponent<AdvancedPatternSystem>();

            if (patternSystem == null)
            {
                patternSystem = gameObject.AddComponent<AdvancedPatternSystem>();
            }

            CacheVisualComponents();
            InitializeServices();
        }

        private void Start()
        {
            if (generateOnStart && creatureInstance?.CreatureData?.GeneticProfile != null)
            {
                StartCoroutine(DelayedInitialization());
            }
        }

        private void Update()
        {
            if (enableRealTimeUpdates && isInitialized && Time.time - lastUpdateTime > updateInterval)
            {
                CheckForGeneticUpdates();
                lastUpdateTime = Time.time;
            }
        }

        private IEnumerator DelayedInitialization()
        {
            yield return new WaitForSeconds(0.1f);
            GenerateProceduralAppearance();
        }

        private void CacheVisualComponents()
        {
            // Auto-find components if not manually assigned
            if (scalableBodyParts == null || scalableBodyParts.Length == 0)
            {
                scalableBodyParts = GetComponentsInChildren<Transform>()
                    .Where(t => t != transform && !t.name.ToLower().Contains("effect"))
                    .ToArray();
            }

            if (primaryRenderers == null || primaryRenderers.Length == 0)
            {
                primaryRenderers = GetComponentsInChildren<Renderer>();
            }

            if (effectSystems == null || effectSystems.Length == 0)
            {
                effectSystems = GetComponentsInChildren<ParticleSystem>();
            }

            if (magicalLights == null || magicalLights.Length == 0)
            {
                magicalLights = GetComponentsInChildren<Light>();
            }
        }

        private void InitializeServices()
        {
            // Initialize services with dependency injection
            geneticTraitService = new GeneticTraitApplicationService(
                transform,
                scalableBodyParts,
                visualComplexityMultiplier);

            materialGeneticsService = new MaterialGeneticsService(primaryRenderers);

            visualEffectsService = new VisualEffectsService(
                effectSystems,
                magicalLights,
                materialGeneticsService,
                enableLODOptimization,
                maxSimultaneousEffects);

            if (enableDebugMode)
                UnityEngine.Debug.Log("ProceduralVisualSystem services initialized");
        }

        #endregion

        #region Public API - Main Entry Points

        /// <summary>
        /// Main method to generate complete procedural appearance from genetics
        /// </summary>
        public void ApplyGeneticVisuals(GeneticProfile genetics)
        {
            if (genetics == null)
            {
                UnityEngine.Debug.LogWarning($"{name}: Cannot apply genetic visuals - no genetic profile provided");
                return;
            }

            // Check if we need to regenerate
            string newHash = GenerateGeneticHash(genetics);
            if (newHash == currentVisualHash)
            {
                if (enableDebugMode) UnityEngine.Debug.Log($"{name}: Genetic visuals already up to date");
                return;
            }

            UnityEngine.Debug.Log($"🎨 Generating procedural appearance for {name} (Gen {genetics.Generation})");

            try
            {
                // 1. Apply enhanced visual genetics (existing system)
                if (creatureInstance != null)
                {
                    creatureInstance.ApplyEnhancedVisualGenetics();
                }

                // 2. Apply advanced genetic traits (delegated to service)
                geneticTraitService.ApplyAdvancedGeneticTraits(genetics);

                // 3. Generate procedural patterns
                if (enableAdvancedPatterns && patternSystem != null)
                {
                    GenerateAdvancedPatterns(genetics);
                }

                // 4. Apply material genetics (delegated to service)
                if (enableMaterialGenetics)
                {
                    materialGeneticsService.ApplyMaterialGenetics(genetics, newHash);
                }

                // 5. Apply biome adaptations (delegated to service)
                if (enableEnvironmentalAdaptation)
                {
                    visualEffectsService.ApplyBiomeAdaptations(genetics);
                }

                // 6. Apply magical traits (delegated to service)
                visualEffectsService.ApplyMagicalTraits(genetics);

                // 7. Generate particle effects (delegated to service)
                if (enableParticleEffects)
                {
                    visualEffectsService.GenerateParticleEffects(genetics);
                }

                // 8. Apply generation-based refinements
                ApplyGenerationRefinements(genetics);

                currentVisualHash = newHash;
                isInitialized = true;

                UnityEngine.Debug.Log($"✅ Procedural appearance generated for {name}");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to generate procedural appearance for {name}: {e.Message}");
            }
        }

        /// <summary>
        /// Simplified method for backward compatibility
        /// </summary>
        public void GenerateProceduralAppearance()
        {
            if (creatureInstance?.CreatureData?.GeneticProfile != null)
            {
                ApplyGeneticVisuals(creatureInstance.CreatureData.GeneticProfile);
            }
        }

        /// <summary>
        /// Apply a specific genetic trait change in real-time
        /// </summary>
        public void ApplySpecificTrait(string traitName, float value)
        {
            var genetics = creatureInstance?.CreatureData?.GeneticProfile;
            if (genetics == null) return;

            // Find and update the gene
            var gene = genetics.Genes.FirstOrDefault(g => g.traitName == traitName);
            if (!string.IsNullOrEmpty(gene.traitName))
            {
                gene.value = value;

                // Apply specific trait changes without full regeneration
                ApplyTraitSpecificChange(traitName, value);

                if (enableDebugMode)
                    UnityEngine.Debug.Log($"Applied trait change: {traitName} = {value:F2}");
            }
        }

        #endregion

        #region Pattern Generation

        private void GenerateAdvancedPatterns(GeneticProfile genetics)
        {
            if (patternSystem == null) return;

            // Determine pattern type from genetics
            string patternType = DeterminePatternType(genetics);
            float patternComplexity = CalculatePatternComplexity(genetics);

            // Apply environmental influence to patterns
            patternType = ApplyEnvironmentalPatternInfluence(genetics, patternType);

            // Generate the pattern
            patternSystem.GeneratePattern(patternType, patternComplexity);

            if (enableDebugMode)
                UnityEngine.Debug.Log($"Generated pattern: {patternType} (complexity: {patternComplexity:F2})");
        }

        private string DeterminePatternType(GeneticProfile genetics)
        {
            // Check for specific pattern genes
            var patternGenes = genetics.Genes.Where(g =>
                g.traitType == TraitType.BodyMarkings ||
                g.traitType == TraitType.ColorPattern).ToArray();

            if (patternGenes.Length > 0)
            {
                var strongestPattern = patternGenes.OrderByDescending(g => g.value ?? 0).First();
                return ExtractPatternFromTrait(strongestPattern.traitName);
            }

            // Generate pattern based on other traits
            float camouflageStrength = GetGeneticValue(genetics, "Camouflage", 0f);
            float magicalPower = GetGeneticValue(genetics, "Magical", 0f);
            float aggressiveness = GetGeneticValue(genetics, "Combat", 0f);

            if (camouflageStrength > 0.6f)
                return "Camouflage";
            else if (magicalPower > 0.7f)
                return "Iridescent";
            else if (magicalPower > 0.4f)
                return "Bioluminescent";
            else if (aggressiveness > 0.7f)
                return "Stripes";
            else if (GetGeneticValue(genetics, "Agility", 0.5f) > 0.8f)
                return "Spots";
            else
                return "Solid";
        }

        private string ExtractPatternFromTrait(string traitName)
        {
            string lower = traitName.ToLower();
            if (lower.Contains("stripe")) return "Stripes";
            if (lower.Contains("spot")) return "Spots";
            if (lower.Contains("camouflage")) return "Camouflage";
            if (lower.Contains("iridescent")) return "Iridescent";
            if (lower.Contains("bioluminescent")) return "Bioluminescent";
            if (lower.Contains("geometric")) return "Geometric";
            if (lower.Contains("fractal")) return "Fractal";
            return "Solid";
        }

        private float CalculatePatternComplexity(GeneticProfile genetics)
        {
            float baseComplexity = GetGeneticValue(genetics, "Intelligence", 0.5f) * 0.3f;
            baseComplexity += GetGeneticValue(genetics, "Magical", 0f) * 0.4f;
            baseComplexity += genetics.Generation * 0.05f;
            baseComplexity += genetics.Mutations.Count * 0.1f;

            return Mathf.Clamp01(baseComplexity);
        }

        private string ApplyEnvironmentalPatternInfluence(GeneticProfile genetics, string basePattern)
        {
            var envGenes = genetics.Genes.Where(g => g.traitType == TraitType.Environmental).ToArray();

            foreach (var gene in envGenes)
            {
                if (gene.value < 0.5f) continue;

                string envType = gene.traitName.ToLower();
                if (envType.Contains("arctic") && basePattern == "Solid")
                    return "Camouflage";
                else if (envType.Contains("desert") && basePattern == "Solid")
                    return "Spots";
                else if (envType.Contains("forest") && basePattern == "Solid")
                    return "Stripes";
            }

            return basePattern;
        }

        #endregion

        #region Utility Methods

        private float GetGeneticValue(GeneticProfile genetics, string traitName, float defaultValue)
        {
            var gene = genetics.Genes.FirstOrDefault(g =>
                g.traitName.Equals(traitName, System.StringComparison.OrdinalIgnoreCase) ||
                g.traitName.ToLower().Contains(traitName.ToLower()));

            return (!string.IsNullOrEmpty(gene.traitName) && gene.value.HasValue) ? gene.value.Value : defaultValue;
        }

        private string GenerateGeneticHash(GeneticProfile genetics)
        {
            if (genetics == null) return "";

            // PERFORMANCE OPTIMIZED: Use hash code directly
            int hash = genetics.Generation.GetHashCode();
            hash = hash * 31 + genetics.Genes.Count.GetHashCode();
            hash = hash * 31 + genetics.Mutations.Count.GetHashCode();

            // Process first 10 genes without LINQ allocation
            int geneCount = 0;
            foreach (var gene in genetics.Genes)
            {
                if (geneCount >= 10) break;
                if (!string.IsNullOrEmpty(gene.traitName) && gene.value.HasValue)
                {
                    hash = hash * 31 + gene.traitName.GetHashCode();
                    hash = hash * 31 + ((int)(gene.value.Value * 100)).GetHashCode();
                }
                geneCount++;
            }

            return hash.ToString();
        }

        private void CheckForGeneticUpdates()
        {
            if (creatureInstance?.CreatureData?.GeneticProfile == null) return;

            string newHash = GenerateGeneticHash(creatureInstance.CreatureData.GeneticProfile);
            if (newHash != currentVisualHash)
            {
                ApplyGeneticVisuals(creatureInstance.CreatureData.GeneticProfile);
            }
        }

        private void ApplyTraitSpecificChange(string traitName, float value)
        {
            // Handle specific trait changes without full regeneration
            switch (traitName.ToLower())
            {
                case "size":
                    transform.localScale = Vector3.one * (0.7f + value * 0.6f);
                    break;
                case "magical":
                    UpdateMagicalEffects(value);
                    break;
            }
        }

        private void UpdateMagicalEffects(float magicalPower)
        {
            foreach (var light in magicalLights)
            {
                if (light != null)
                {
                    light.intensity = magicalPower * 2f;
                    light.enabled = magicalPower > 0.1f;
                }
            }
        }

        private void ApplyGenerationRefinements(GeneticProfile genetics)
        {
            if (genetics.Generation <= 1) return;

            float refinementFactor = Mathf.Clamp01((genetics.Generation - 1) / 10f);

            // Delegate material refinements to service
            materialGeneticsService.ApplyGenerationRefinements(genetics.Generation);

            // Subtle size increase for higher generations
            transform.localScale *= (1f + refinementFactor * 0.05f);
        }

        #endregion

        #region Debug & Public Interface

        [ContextMenu("Force Regenerate Visuals")]
        public void ForceRegenerateVisuals()
        {
            currentVisualHash = "";
            GenerateProceduralAppearance();
        }

        [ContextMenu("Toggle Debug Mode")]
        public void ToggleDebugMode()
        {
            enableDebugMode = !enableDebugMode;
            UnityEngine.Debug.Log($"ProceduralVisualSystem debug mode: {enableDebugMode}");
        }

        public string GetVisualDebugInfo()
        {
            if (!isInitialized) return "System not initialized";

            var genetics = creatureInstance?.CreatureData?.GeneticProfile;
            if (genetics == null) return "No genetic data";

            return $"Visual Hash: {currentVisualHash}\n" +
                   $"Generation: {genetics.Generation}\n" +
                   $"Active Features: {CountActiveFeatures()}\n" +
                   $"Particle Effects: {visualEffectsService?.CountActiveParticleEffects() ?? 0}\n" +
                   $"Material Instances: {materialGeneticsService?.GetMaterialCacheCount() ?? 0}";
        }

        private int CountActiveFeatures()
        {
            int count = 0;
            string[] features = { "Wing", "Horn", "Tentacle", "Eye" };

            foreach (string feature in features)
            {
                Transform featureGroup = transform.Find($"{feature}Group");
                if (featureGroup != null && featureGroup.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        #endregion
    }
}
