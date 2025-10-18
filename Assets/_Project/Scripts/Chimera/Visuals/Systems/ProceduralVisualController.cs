using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Visuals.Data;
using Laboratory.Chimera.Visuals.Generators;

namespace Laboratory.Chimera.Visuals.Systems
{
    /// <summary>
    /// Main controller for procedural visual generation
    /// Coordinates all visual subsystems and manages the generation pipeline
    /// </summary>
    [RequireComponent(typeof(CreatureInstanceComponent))]
    public class ProceduralVisualController : MonoBehaviour
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
        private PatternGenerator patternGenerator;
        private MaterialGenerator materialGenerator;
        private ParticleEffectGenerator particleGenerator;
        private BiomeAdaptationSystem biomeAdaptation;
        private MagicalEffectGenerator magicalGenerator;

        // Visual state
        private VisualCache visualCache = new();
        private VisualGeneticTraits currentTraits;
        private bool isInitialized = false;
        private float lastUpdateTime = 0f;

        // Events
        public System.Action<VisualGeneticTraits> OnVisualGenerated;
        public System.Action<BiomeType> OnBiomeAdaptation;

        #region Initialization

        private void Awake()
        {
            creatureInstance = GetComponent<CreatureInstanceComponent>();
            InitializeGenerators();
            CacheVisualComponents();
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

        private void InitializeGenerators()
        {
            patternGenerator = GetComponent<PatternGenerator>() ?? gameObject.AddComponent<PatternGenerator>();
            materialGenerator = GetComponent<MaterialGenerator>() ?? gameObject.AddComponent<MaterialGenerator>();
            particleGenerator = GetComponent<ParticleEffectGenerator>() ?? gameObject.AddComponent<ParticleEffectGenerator>();
            biomeAdaptation = GetComponent<BiomeAdaptationSystem>() ?? gameObject.AddComponent<BiomeAdaptationSystem>();
            magicalGenerator = GetComponent<MagicalEffectGenerator>() ?? gameObject.AddComponent<MagicalEffectGenerator>();
        }

        private IEnumerator DelayedInitialization()
        {
            yield return new WaitForSeconds(0.1f);
            GenerateProceduralAppearance();
        }

        private void CacheVisualComponents()
        {
            if (scalableBodyParts == null || scalableBodyParts.Length == 0)
            {
                scalableBodyParts = GetComponentsInChildren<Transform>()
                    .Where(t => t != transform && !t.name.ToLower().Contains("effect"))
                    .ToArray();
            }

            if (primaryRenderers == null || primaryRenderers.Length == 0)
            {
                primaryRenderers = GetComponentsInChildren<Renderer>()
                    .Where(r => r.enabled && r.gameObject.activeInHierarchy)
                    .ToArray();
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

        #endregion

        #region Public API

        public void GenerateProceduralAppearance()
        {
            if (creatureInstance?.CreatureData?.GeneticProfile == null)
            {
                Debug.LogWarning("Cannot generate appearance: Missing genetic profile");
                return;
            }

            var geneticProfile = creatureInstance.CreatureData.GeneticProfile;
            currentTraits = ExtractVisualTraits(geneticProfile);

            StartCoroutine(GenerateVisualsPipeline(currentTraits));
        }

        public void UpdateAppearanceFromGenetics(GeneticProfile newProfile)
        {
            if (newProfile == null) return;

            var newTraits = ExtractVisualTraits(newProfile);
            var newHash = CalculateVisualHash(newTraits);

            if (newHash != visualCache.CurrentVisualHash)
            {
                currentTraits = newTraits;
                StartCoroutine(GenerateVisualsPipeline(currentTraits));
            }
        }

        public void AdaptToBiome(BiomeType biome)
        {
            if (biomeAdaptation != null && enableEnvironmentalAdaptation)
            {
                biomeAdaptation.AdaptToBiome(biome, currentTraits);
                OnBiomeAdaptation?.Invoke(biome);
            }
        }

        public void SetVisualComplexity(float complexity)
        {
            visualComplexityMultiplier = Mathf.Clamp01(complexity);

            // Update all generators
            patternGenerator?.SetComplexityLevel(complexity);
            particleGenerator?.SetComplexityLevel(complexity);
            magicalGenerator?.SetComplexityLevel(complexity);
        }

        #endregion

        #region Visual Generation Pipeline

        private IEnumerator GenerateVisualsPipeline(VisualGeneticTraits traits)
        {
            Debug.Log("🎨 Starting procedural visual generation pipeline");

            // Phase 1: Basic scaling and structure
            ApplyBasicScaling(traits);
            yield return null;

            // Phase 2: Material generation
            if (enableMaterialGenetics)
            {
                yield return StartCoroutine(materialGenerator.GenerateMaterials(traits, primaryRenderers));
            }

            // Phase 3: Pattern generation
            if (enableAdvancedPatterns)
            {
                yield return StartCoroutine(patternGenerator.GeneratePatterns(traits, primaryRenderers));
            }

            // Phase 4: Particle effects
            if (enableParticleEffects)
            {
                yield return StartCoroutine(particleGenerator.GenerateEffects(traits, effectSystems));
            }

            // Phase 5: Magical effects
            if (traits.HasMagicalAura)
            {
                yield return StartCoroutine(magicalGenerator.GenerateMagicalEffects(traits, magicalLights));
            }

            // Finalize
            visualCache.CurrentVisualHash = CalculateVisualHash(traits);
            visualCache.LastCacheUpdate = System.DateTime.Now;
            isInitialized = true;

            OnVisualGenerated?.Invoke(traits);
            Debug.Log("✨ Procedural visual generation complete");
        }

        private VisualGeneticTraits ExtractVisualTraits(GeneticProfile geneticProfile)
        {
            var traits = new VisualGeneticTraits();

            // Extract size traits
            traits.OverallSize = Mathf.Lerp(0.5f, 2.0f, geneticProfile.GetTraitValue("Size", 0.5f));
            traits.HeadScale = Mathf.Lerp(0.8f, 1.4f, geneticProfile.GetTraitValue("HeadSize", 0.5f));
            traits.BodyScale = Mathf.Lerp(0.7f, 1.5f, geneticProfile.GetTraitValue("BodySize", 0.5f));
            traits.LimbScale = Mathf.Lerp(0.8f, 1.3f, geneticProfile.GetTraitValue("LimbSize", 0.5f));

            // Extract color traits
            var colorSeed = geneticProfile.GetTraitValue("ColorSeed", 0.5f);
            var hue = geneticProfile.GetTraitValue("ColorHue", colorSeed);
            var saturation = geneticProfile.GetTraitValue("ColorSaturation", 0.7f);
            var brightness = geneticProfile.GetTraitValue("ColorBrightness", 0.6f);

            traits.PrimaryColor = Color.HSVToRGB(hue, saturation, brightness);
            traits.SecondaryColor = Color.HSVToRGB((hue + 0.3f) % 1f, saturation * 0.8f, brightness * 0.9f);
            traits.AccentColor = Color.HSVToRGB((hue + 0.6f) % 1f, saturation * 1.2f, brightness * 1.1f);

            traits.ColorIntensity = geneticProfile.GetTraitValue("ColorIntensity", 0.8f);
            traits.ColorVariation = geneticProfile.GetTraitValue("ColorVariation", 0.3f);

            // Extract pattern traits
            var patternValue = geneticProfile.GetTraitValue("PatternType", 0.5f);
            traits.PrimaryPattern = (PatternType)Mathf.FloorToInt(patternValue * System.Enum.GetValues(typeof(PatternType)).Length);
            traits.PatternIntensity = geneticProfile.GetTraitValue("PatternIntensity", 0.6f);
            traits.PatternScale = geneticProfile.GetTraitValue("PatternScale", 1.0f);
            traits.PatternComplexity = geneticProfile.GetTraitValue("PatternComplexity", 0.5f);

            // Extract material traits
            traits.Metallic = geneticProfile.GetTraitValue("Metallic", 0.1f);
            traits.Roughness = geneticProfile.GetTraitValue("Roughness", 0.5f);
            traits.Emission = geneticProfile.GetTraitValue("Emission", 0.0f);
            traits.Iridescence = geneticProfile.GetTraitValue("Iridescence", 0.2f);

            // Extract magical traits
            traits.HasMagicalAura = geneticProfile.GetTraitValue("MagicalAffinity", 0.0f) > 0.3f;
            traits.MagicalIntensity = geneticProfile.GetTraitValue("MagicalPower", 0.0f);
            traits.HasGlow = geneticProfile.GetTraitValue("Luminescence", 0.0f) > 0.4f;

            return traits;
        }

        private void ApplyBasicScaling(VisualGeneticTraits traits)
        {
            foreach (var bodyPart in scalableBodyParts)
            {
                if (bodyPart == null) continue;

                var scale = Vector3.one * traits.OverallSize;

                // Apply specific scaling based on body part name
                if (bodyPart.name.ToLower().Contains("head"))
                    scale *= traits.HeadScale;
                else if (bodyPart.name.ToLower().Contains("body"))
                    scale *= traits.BodyScale;
                else if (bodyPart.name.ToLower().Contains("limb") || bodyPart.name.ToLower().Contains("leg") || bodyPart.name.ToLower().Contains("arm"))
                    scale *= traits.LimbScale;

                bodyPart.localScale = scale;
            }
        }

        #endregion

        #region Utility Methods

        private void CheckForGeneticUpdates()
        {
            if (creatureInstance?.CreatureData?.GeneticProfile != null)
            {
                var newTraits = ExtractVisualTraits(creatureInstance.CreatureData.GeneticProfile);
                var newHash = CalculateVisualHash(newTraits);

                if (newHash != visualCache.CurrentVisualHash)
                {
                    currentTraits = newTraits;
                    StartCoroutine(GenerateVisualsPipeline(currentTraits));
                }
            }
        }

        private string CalculateVisualHash(VisualGeneticTraits traits)
        {
            return $"{traits.PrimaryColor}_{traits.PatternIntensity}_{traits.OverallSize}_{traits.HasMagicalAura}".GetHashCode().ToString();
        }

        #endregion

        #region Public Properties

        public VisualGeneticTraits CurrentTraits => currentTraits;
        public bool IsInitialized => isInitialized;
        public bool EnableAdvancedPatterns => enableAdvancedPatterns;
        public bool EnableParticleEffects => enableParticleEffects;
        public bool EnableMaterialGenetics => enableMaterialGenetics;
        public float VisualComplexityMultiplier => visualComplexityMultiplier;

        #endregion
    }
}