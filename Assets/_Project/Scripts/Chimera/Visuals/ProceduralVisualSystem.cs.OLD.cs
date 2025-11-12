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
    /// Integrates perfectly with your existing CreatureInstanceComponent system.
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
        private Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
        private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        
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
        }
        
        private void Start()
        {
            if (generateOnStart && creatureInstance?.CreatureData?.GeneticProfile != null)
            {
                // Small delay to ensure creature is fully initialized
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
        
        #endregion
        
        #region Public API - Main Entry Points
        
        /// <summary>
        /// Main method to generate complete procedural appearance from genetics
        /// Called by GeneticVisualIntegration component
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
                // 1. Apply enhanced visual genetics (your existing system)
                if (creatureInstance != null)
                {
                    creatureInstance.ApplyEnhancedVisualGenetics();
                }
                
                // 2. Apply advanced genetic traits
                ApplyAdvancedGeneticTraits(genetics);
                
                // 3. Generate procedural patterns
                if (enableAdvancedPatterns)
                {
                    GenerateAdvancedPatterns(genetics);
                }
                
                // 4. Apply material genetics
                if (enableMaterialGenetics)
                {
                    ApplyMaterialGenetics(genetics);
                }
                
                // 5. Generate particle effects
                if (enableParticleEffects)
                {
                    GenerateParticleEffects(genetics);
                }
                
                // 6. Environmental adaptations
                if (enableEnvironmentalAdaptation)
                {
                    ApplyEnvironmentalAdaptations(genetics);
                }
                
                // 7. Apply generation-based refinements
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
        
        #region Advanced Genetic Traits
        
        private void ApplyAdvancedGeneticTraits(GeneticProfile genetics)
        {
            // Physical structure modifications
            ApplyPhysicalStructure(genetics);
            
            // Special appendages and features
            ApplySpecialFeatures(genetics);
            
            // Biome-specific adaptations
            ApplyBiomeAdaptations(genetics);
            
            // Magical trait manifestations
            ApplyMagicalTraits(genetics);
        }
        
        private void ApplyBiomeAdaptations(GeneticProfile genetics)
        {
            // Apply visual adaptations based on biome-specific genes
            var biomeGenes = genetics.Genes.Where(g =>
                g.traitType == TraitType.Environmental ||
                g.traitType == TraitType.BiomeAdaptation)
                .ToArray();
            
            foreach (var gene in biomeGenes)
            {
                if ((gene.value ?? 0f) < 0.3f) continue;
                
                string geneNameLower = gene.traitName.ToLower();
                float strength = gene.value ?? 0.5f;
                
                if (geneNameLower.Contains("arctic") || geneNameLower.Contains("ice"))
                {
                    ApplyArcticBiomeVisuals(strength);
                }
                else if (geneNameLower.Contains("desert") || geneNameLower.Contains("sand"))
                {
                    ApplyDesertBiomeVisuals(strength);
                }
                else if (geneNameLower.Contains("ocean") || geneNameLower.Contains("aquatic"))
                {
                    ApplyOceanBiomeVisuals(strength);
                }
                else if (geneNameLower.Contains("forest") || geneNameLower.Contains("woodland"))
                {
                    ApplyForestBiomeVisuals(strength);
                }
                else if (geneNameLower.Contains("volcanic") || geneNameLower.Contains("lava"))
                {
                    ApplyVolcanicBiomeVisuals(strength);
                }
                else if (geneNameLower.Contains("mountain") || geneNameLower.Contains("alpine"))
                {
                    ApplyMountainBiomeVisuals(strength);
                }
            }
        }
        
        private void ApplyMagicalTraits(GeneticProfile genetics)
        {
            // Apply visual manifestations of magical abilities
            var magicalGenes = genetics.Genes.Where(g =>
                g.traitType == TraitType.Elemental ||
                g.traitType == TraitType.Magical ||
                g.traitType == TraitType.Arcane)
                .ToArray();
            
            float totalMagicalPower = 0f;
            
            foreach (var gene in magicalGenes)
            {
                if ((gene.value ?? 0f) < 0.2f) continue;
                
                float magicStrength = gene.value ?? 0.5f;
                totalMagicalPower += magicStrength;
                
                string geneNameLower = gene.traitName.ToLower();
                
                if (geneNameLower.Contains("fire") || geneNameLower.Contains("flame"))
                {
                    ApplyFireMagicVisuals(magicStrength);
                }
                else if (geneNameLower.Contains("ice") || geneNameLower.Contains("frost"))
                {
                    ApplyIceMagicVisuals(magicStrength);
                }
                else if (geneNameLower.Contains("lightning") || geneNameLower.Contains("electric"))
                {
                    ApplyLightningMagicVisuals(magicStrength);
                }
                else if (geneNameLower.Contains("nature") || geneNameLower.Contains("earth"))
                {
                    ApplyNatureMagicVisuals(magicStrength);
                }
                else if (geneNameLower.Contains("shadow") || geneNameLower.Contains("dark"))
                {
                    ApplyShadowMagicVisuals(magicStrength);
                }
                else if (geneNameLower.Contains("light") || geneNameLower.Contains("holy"))
                {
                    ApplyLightMagicVisuals(magicStrength);
                }
                else if (geneNameLower.Contains("wind") || geneNameLower.Contains("air"))
                {
                    ApplyWindMagicVisuals(magicStrength);
                }
                else
                {
                    ApplyGenericMagicVisuals(magicStrength);
                }
            }
            
            // Apply overall magical aura based on total magical power
            if (totalMagicalPower > 0.5f)
            {
                ApplyMagicalAura(Mathf.Min(totalMagicalPower, 2.0f));
            }
        }
        
        private void ApplyPhysicalStructure(GeneticProfile genetics)
        {
            // Enhanced size system with multiple factors
            float baseSize = GetGeneticValue(genetics, "Size", 1.0f);
            float strengthModifier = GetGeneticValue(genetics, "Strength", 0.5f) * 0.2f;
            float constitutionModifier = GetGeneticValue(genetics, "Vitality", 0.5f) * 0.15f;
            
            float finalSize = (baseSize + strengthModifier + constitutionModifier) * visualComplexityMultiplier;
            
            // Apply overall scale
            transform.localScale = Vector3.one * finalSize;
            
            // Proportional body part scaling
            ApplyProportionalScaling(genetics, finalSize);
        }
        
        private void ApplyProportionalScaling(GeneticProfile genetics, float overallSize)
        {
            foreach (var bodyPart in scalableBodyParts)
            {
                if (bodyPart == null) continue;
                
                Vector3 partScale = Vector3.one * overallSize;
                string partName = bodyPart.name.ToLower();
                
                // Intelligence affects head size
                if (partName.Contains("head"))
                {
                    float intelligence = GetGeneticValue(genetics, "Intellect", 0.5f);
                    partScale *= (0.8f + intelligence * 0.4f);
                }
                // Agility affects limb proportions
                else if (partName.Contains("leg") || partName.Contains("arm") || partName.Contains("limb"))
                {
                    float agility = GetGeneticValue(genetics, "Agility", 0.5f);
                    partScale.y *= (0.9f + agility * 0.3f); // Longer limbs
                    partScale.x *= (0.95f + agility * 0.1f); // Slightly thicker
                }
                // Strength affects torso/muscle mass
                else if (partName.Contains("torso") || partName.Contains("chest") || partName.Contains("muscle"))
                {
                    float strength = GetGeneticValue(genetics, "Strength", 0.5f);
                    partScale *= (0.85f + strength * 0.3f);
                }
                
                bodyPart.localScale = partScale;
            }
        }
        
        private void ApplySpecialFeatures(GeneticProfile genetics)
        {
            // Wings
            ApplyWingFeatures(genetics);
            
            // Horns and spikes
            ApplyHornFeatures(genetics);
            
            // Tails
            ApplyTailFeatures(genetics);
            
            // Multiple eyes
            ApplyEyeFeatures(genetics);
            
            // Tentacles
            ApplyTentacleFeatures(genetics);
        }
        
        private void ApplyWingFeatures(GeneticProfile genetics)
        {
            float wingStrength = GetGeneticValue(genetics, "Flight", 0f);
            
            if (wingStrength > 0.1f)
            {
                EnableFeatureGroup("Wing", true);
                ScaleFeatureGroup("Wing", 0.5f + wingStrength * 1.5f);
                
                // Wing type based on other traits
                float agilityFactor = GetGeneticValue(genetics, "Agility", 0.5f);
                if (agilityFactor > 0.7f)
                {
                    SetFeatureVariant("Wing", "Feathered");
                }
                else if (GetGeneticValue(genetics, "Magical", 0f) > 0.5f)
                {
                    SetFeatureVariant("Wing", "Ethereal");
                }
                else
                {
                    SetFeatureVariant("Wing", "Membranous");
                }
            }
            else
            {
                EnableFeatureGroup("Wing", false);
            }
        }
        
        private void ApplyHornFeatures(GeneticProfile genetics)
        {
            float hornStrength = GetGeneticValue(genetics, "Horns", 0f);
            
            if (hornStrength > 0.2f)
            {
                EnableFeatureGroup("Horn", true);
                
                // Horn count based on strength
                int hornCount = hornStrength > 0.8f ? 4 : (hornStrength > 0.5f ? 2 : 1);
                SetFeatureCount("Horn", hornCount);
                
                // Horn size
                ScaleFeatureGroup("Horn", 0.3f + hornStrength * 0.7f);
            }
            else
            {
                EnableFeatureGroup("Horn", false);
            }
        }
        
        private void ApplyTailFeatures(GeneticProfile genetics)
        {
            float agilityFactor = GetGeneticValue(genetics, "Agility", 0.5f);
            
            // Most creatures have tails, but size varies
            float tailScale = 0.7f + agilityFactor * 0.6f;
            ScaleFeatureGroup("Tail", tailScale);
            
            // Tail type based on traits
            if (GetGeneticValue(genetics, "Combat", 0f) > 0.6f)
            {
                SetFeatureVariant("Tail", "Spiked");
            }
            else if (agilityFactor > 0.8f)
            {
                SetFeatureVariant("Tail", "Long");
            }
            else
            {
                SetFeatureVariant("Tail", "Standard");
            }
        }
        
        private void ApplyEyeFeatures(GeneticProfile genetics)
        {
            float intelligence = GetGeneticValue(genetics, "Intellect", 0.5f);
            
            // Eye count based on intelligence and magical traits
            int eyeCount = 2; // Default
            if (intelligence > 0.8f && GetGeneticValue(genetics, "Magical", 0f) > 0.5f)
            {
                eyeCount = 4; // Four eyes for highly intelligent magical creatures
            }
            else if (intelligence > 0.9f)
            {
                eyeCount = 3; // Third eye for very intelligent creatures
            }
            
            SetFeatureCount("Eye", eyeCount);
            
            // Eye glow for magical creatures
            float magicalPower = GetGeneticValue(genetics, "Magical", 0f);
            if (magicalPower > 0.3f)
            {
                ApplyEyeGlow(magicalPower);
            }
        }
        
        private void ApplyTentacleFeatures(GeneticProfile genetics)
        {
            float tentacleStrength = GetGeneticValue(genetics, "Tentacles", 0f);
            
            if (tentacleStrength > 0.2f)
            {
                EnableFeatureGroup("Tentacle", true);
                
                // Tentacle count
                int tentacleCount = Mathf.RoundToInt(2 + tentacleStrength * 6); // 2-8 tentacles
                SetFeatureCount("Tentacle", tentacleCount);
                
                ScaleFeatureGroup("Tentacle", tentacleStrength);
            }
            else
            {
                EnableFeatureGroup("Tentacle", false);
            }
        }
        
        #endregion
        
        #region Advanced Pattern Generation
        
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
            var patternGenes = genetics.Genes.Where(g => g.traitType == TraitType.BodyMarkings || g.traitType == TraitType.ColorPattern).ToArray();
            
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
            baseComplexity += genetics.Generation * 0.05f; // Higher generations = more complex
            
            // Mutations can increase complexity
            baseComplexity += genetics.Mutations.Count * 0.1f;
            
            return Mathf.Clamp01(baseComplexity);
        }
        
        private string ApplyEnvironmentalPatternInfluence(GeneticProfile genetics, string basePattern)
        {
            // Check for environmental adaptation genes
            var envGenes = genetics.Genes.Where(g => g.traitType == TraitType.Environmental).ToArray();
            
            foreach (var gene in envGenes)
            {
                if (gene.value < 0.5f) continue;
                
                string envType = gene.traitName.ToLower();
                if (envType.Contains("arctic") && basePattern == "Solid")
                    return "Camouflage"; // Arctic camouflage
                else if (envType.Contains("desert") && basePattern == "Solid")
                    return "Spots"; // Desert spots
                else if (envType.Contains("forest") && basePattern == "Solid")
                    return "Stripes"; // Forest stripes
            }
            
            return basePattern;
        }
        
        #endregion
        
        #region Material Genetics
        
        private void ApplyMaterialGenetics(GeneticProfile genetics)
        {
            foreach (var renderer in primaryRenderers)
            {
                if (renderer == null) continue;
                
                ApplyMaterialToRenderer(renderer, genetics);
            }
        }
        
        private void ApplyMaterialToRenderer(Renderer renderer, GeneticProfile genetics)
        {
            var materials = renderer.materials;
            
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;
                
                // Create material instance if not already cached
                string materialKey = $"{renderer.name}_{i}_{currentVisualHash}";
                
                if (!materialCache.ContainsKey(materialKey))
                {
                    materials[i] = new Material(materials[i]);
                    materialCache[materialKey] = materials[i];
                    
                    // Apply genetic material properties
                    ApplyGeneticMaterialProperties(materials[i], genetics);
                }
                else
                {
                    materials[i] = materialCache[materialKey];
                }
            }
            
            renderer.materials = materials;
        }
        
        private void ApplyGeneticMaterialProperties(Material material, GeneticProfile genetics)
        {
            // Metallic properties
            float metallicStrength = GetGeneticValue(genetics, "Metallic", 0f);
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallicStrength);
            }
            
            // Surface roughness
            float roughness = GetGeneticValue(genetics, "Roughness", 0.5f);
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 1f - roughness);
            }
            
            // Magical glow
            float magicalPower = GetGeneticValue(genetics, "Magical", 0f);
            if (magicalPower > 0.2f && material.HasProperty("_EmissionColor"))
            {
                Color magicColor = GetMagicalColor(genetics);
                material.SetColor("_EmissionColor", magicColor * magicalPower * 0.5f);
                material.EnableKeyword("_EMISSION");
            }
            
            // Scale hardness (for armor-like appearance)
            float hardness = GetGeneticValue(genetics, "Armor", 0f);
            if (hardness > 0.3f)
            {
                if (material.HasProperty("_Metallic"))
                    material.SetFloat("_Metallic", Mathf.Max(material.GetFloat("_Metallic"), hardness * 0.7f));
                if (material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness", Mathf.Max(material.GetFloat("_Smoothness"), hardness * 0.8f));
            }
        }
        
        private Color GetMagicalColor(GeneticProfile genetics)
        {
            // Determine magical color based on magical type genes
            var magicalGenes = genetics.Genes.Where(g => g.traitType == TraitType.Elemental).ToArray();
            
            if (magicalGenes.Length > 0)
            {
                var strongestMagic = magicalGenes.OrderByDescending(g => g.value ?? 0).First();
                
                switch (strongestMagic.traitName.ToLower())
                {
                    case var name when name.Contains("fire"):
                        return Color.red;
                    case var name when name.Contains("ice"):
                        return Color.cyan;
                    case var name when name.Contains("lightning"):
                        return Color.yellow;
                    case var name when name.Contains("nature"):
                        return Color.green;
                    case var name when name.Contains("shadow"):
                        return new Color(0.3f, 0.1f, 0.7f);
                    default:
                        return Color.magenta;
                }
            }
            
            return Color.white;
        }
        
        #endregion
        
        #region Particle Effects
        
        private void GenerateParticleEffects(GeneticProfile genetics)
        {
            // Clear existing effects
            StopAllParticleEffects();
            
            // Apply magical effects
            ApplyMagicalParticleEffects(genetics);
            
            // Apply environmental effects
            ApplyEnvironmentalParticleEffects(genetics);
            
            // Apply generation effects
            ApplyGenerationParticleEffects(genetics);
        }
        
        private void ApplyMagicalParticleEffects(GeneticProfile genetics)
        {
            float magicalPower = GetGeneticValue(genetics, "Magical", 0f);
            if (magicalPower < 0.3f) return;
            
            var magicalGenes = genetics.Genes.Where(g => g.traitType == TraitType.Elemental && g.value > 0.4f).ToArray();
            
            // Apply LOD optimization - reduce effects if enabled
            int effectLimit = enableLODOptimization ? Mathf.Min(maxSimultaneousEffects, 3) : maxSimultaneousEffects;
            
            foreach (var gene in magicalGenes.Take(effectLimit))
            {
                CreateMagicalParticleEffect(gene.traitName, gene.value ?? 0.5f);
            }
        }
        
        private void CreateMagicalParticleEffect(string magicType, float intensity)
        {
            var availableSystem = GetAvailableParticleSystem();
            if (availableSystem == null) return;
            
            ConfigureMagicalParticleSystem(availableSystem, magicType, intensity);
            availableSystem.Play();
            
            if (enableDebugMode)
                UnityEngine.Debug.Log($"Created magical effect: {magicType} (intensity: {intensity:F2})");
        }
        
        private void ConfigureMagicalParticleSystem(ParticleSystem ps, string magicType, float intensity)
        {
            var main = ps.main;
            var emission = ps.emission;
            var colorOverLifetime = ps.colorOverLifetime;
            
            main.startLifetime = 2f + intensity;
            emission.rateOverTime = 10f * intensity;
            
            switch (magicType.ToLower())
            {
                case var name when name.Contains("fire"):
                    main.startColor = Color.Lerp(Color.red, Color.yellow, UnityEngine.Random.value);
                    main.startSpeed = 3f;
                    break;
                case var name when name.Contains("ice"):
                    main.startColor = Color.Lerp(Color.cyan, Color.white, UnityEngine.Random.value);
                    main.startSpeed = 1f;
                    break;
                case var name when name.Contains("lightning"):
                    main.startColor = Color.yellow;
                    main.startSpeed = 8f;
                    emission.rateOverTime = 5f * intensity; // Burst style
                    break;
                default:
                    main.startColor = Color.magenta;
                    main.startSpeed = 2f;
                    break;
            }
        }
        
        private void ApplyEnvironmentalParticleEffects(GeneticProfile genetics)
        {
            // Apply particle effects based on environmental adaptations
            var envGenes = genetics.Genes.Where(g => g.traitType == TraitType.Environmental).ToArray();
            
            foreach (var gene in envGenes)
            {
                if ((gene.value ?? 0f) < 0.4f) continue;
                
                CreateEnvironmentalParticleEffect(gene.traitName, gene.value ?? 0.5f);
            }
        }
        
        private void ApplyGenerationParticleEffects(GeneticProfile genetics)
        {
            // Higher generation creatures get subtle particle refinements
            if (genetics.Generation <= 2) return;
            
            float generationFactor = Mathf.Clamp01((genetics.Generation - 2) / 8f);
            
            // Subtle sparkle effect for high-generation creatures
            var sparkleSystem = GetAvailableParticleSystem();
            if (sparkleSystem != null)
            {
                var main = sparkleSystem.main;
                main.startColor = new Color(1f, 1f, 1f, 0.3f + generationFactor * 0.4f);
                main.startLifetime = 1f + generationFactor;
                main.startSpeed = 0.5f;
                
                var emission = sparkleSystem.emission;
                emission.rateOverTime = generationFactor * 3f;
                
                sparkleSystem.Play();
            }
        }
        
        private void CreateEnvironmentalParticleEffect(string adaptationType, float intensity)
        {
            var availableSystem = GetAvailableParticleSystem();
            if (availableSystem == null) return;
            
            var main = availableSystem.main;
            string envType = adaptationType.ToLower();
            
            if (envType.Contains("arctic"))
            {
                main.startColor = Color.white;
                main.startLifetime = 2f;
                main.startSpeed = 0.3f;
            }
            else if (envType.Contains("desert"))
            {
                main.startColor = new Color(1f, 0.9f, 0.6f, 0.5f);
                main.startLifetime = 1f;
                main.startSpeed = 1f;
            }
            else if (envType.Contains("ocean"))
            {
                main.startColor = new Color(0.3f, 0.7f, 1f, 0.6f);
                main.startLifetime = 3f;
                main.startSpeed = 0.8f;
            }
            else if (envType.Contains("forest"))
            {
                main.startColor = Color.green;
                main.startLifetime = 4f;
                main.startSpeed = 0.2f;
            }
            
            var emission = availableSystem.emission;
            emission.rateOverTime = intensity * 8f;
            
            availableSystem.Play();
        }
        
        #endregion
        
        #region Environmental Adaptations
        
        private void ApplyEnvironmentalAdaptations(GeneticProfile genetics)
        {
            var envGenes = genetics.Genes.Where(g => g.traitType == TraitType.Environmental).ToArray();
            
            foreach (var gene in envGenes)
            {
                if (gene.value < 0.4f) continue;
                
                ApplyEnvironmentalVisualAdaptation(gene.traitName, gene.value ?? 0.5f);
            }
        }
        
        private void ApplyEnvironmentalVisualAdaptation(string adaptationType, float strength)
        {
            string envType = adaptationType.ToLower();
            
            if (envType.Contains("arctic"))
            {
                ApplyArcticAdaptation(strength);
            }
            else if (envType.Contains("desert"))
            {
                ApplyDesertAdaptation(strength);
            }
            else if (envType.Contains("ocean") || envType.Contains("aquatic"))
            {
                ApplyAquaticAdaptation(strength);
            }
            else if (envType.Contains("forest"))
            {
                ApplyForestAdaptation(strength);
            }
            else if (envType.Contains("volcanic"))
            {
                ApplyVolcanicAdaptation(strength);
            }
        }
        
        private void ApplyArcticAdaptation(float strength)
        {
            // Breath vapor effect
            var breathEffect = GetAvailableParticleSystem();
            if (breathEffect != null)
            {
                var main = breathEffect.main;
                main.startColor = Color.white;
                main.startLifetime = 1f;
                main.startSpeed = 0.5f;
                breathEffect.transform.localPosition = Vector3.up * 1.5f; // Near head
                breathEffect.Play();
            }
            
            // Slight blue tint to materials
            TintAllMaterials(new Color(0.9f, 0.95f, 1f), strength * 0.2f);
        }
        
        private void ApplyDesertAdaptation(float strength)
        {
            // Heat shimmer effect
            var shimmerEffect = GetAvailableParticleSystem();
            if (shimmerEffect != null)
            {
                var main = shimmerEffect.main;
                main.startColor = new Color(1f, 1f, 1f, 0.3f);
                main.startLifetime = 0.5f;
                main.startSpeed = 1f;
                shimmerEffect.Play();
            }
            
            // Sandy coloration
            TintAllMaterials(new Color(1f, 0.9f, 0.7f), strength * 0.3f);
        }
        
        private void ApplyAquaticAdaptation(float strength)
        {
            // Bubble trail effect
            var bubbleEffect = GetAvailableParticleSystem();
            if (bubbleEffect != null)
            {
                var main = bubbleEffect.main;
                main.startColor = new Color(0.8f, 0.9f, 1f, 0.6f);
                main.startLifetime = 2f;
                main.startSpeed = 2f;
                
                var shape = bubbleEffect.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.5f;
                
                bubbleEffect.Play();
            }
            
            // Iridescent blue-green tint
            TintAllMaterials(new Color(0.7f, 0.9f, 0.8f), strength * 0.25f);
        }
        
        private void ApplyForestAdaptation(float strength)
        {
            // Leaf particle effect occasionally
            if (UnityEngine.Random.value < strength * 0.3f)
            {
                var leafEffect = GetAvailableParticleSystem();
                if (leafEffect != null)
                {
                    var main = leafEffect.main;
                    main.startColor = Color.green;
                    main.startLifetime = 3f;
                    main.startSpeed = 0.5f;
                    leafEffect.Play();
                }
            }
            
            // Green-brown forest tint
            TintAllMaterials(new Color(0.8f, 0.9f, 0.7f), strength * 0.2f);
        }
        
        private void ApplyVolcanicAdaptation(float strength)
        {
            // Ember effect
            var emberEffect = GetAvailableParticleSystem();
            if (emberEffect != null)
            {
                var main = emberEffect.main;
                main.startColor = Color.Lerp(Color.red, Color.orange, UnityEngine.Random.value);
                main.startLifetime = 1.5f;
                main.startSpeed = 1f;
                emberEffect.Play();
            }
            
            // Heat distortion and reddish tint
            TintAllMaterials(new Color(1f, 0.8f, 0.7f), strength * 0.3f);
            
            // Add glow to simulate heat
            foreach (var renderer in primaryRenderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor("_EmissionColor", Color.red * strength * 0.2f);
                        material.EnableKeyword("_EMISSION");
                    }
                }
            }
        }
        
        #endregion
        
        #region Biome Visual Methods
        
        private void ApplyArcticBiomeVisuals(float strength)
        {
            // Apply cold-adapted visual features
            TintAllMaterials(new Color(0.9f, 0.95f, 1f), strength * 0.3f);
            
            // Thicker fur/scales effect (increase material roughness)
            foreach (var renderer in primaryRenderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_Smoothness"))
                    {
                        float currentSmoothness = material.GetFloat("_Smoothness");
                        material.SetFloat("_Smoothness", currentSmoothness * (1f - strength * 0.3f));
                    }
                }
            }
        }
        
        private void ApplyDesertBiomeVisuals(float strength)
        {
            // Sandy, heat-adapted coloration
            TintAllMaterials(new Color(1f, 0.92f, 0.8f), strength * 0.4f);
            
            // Heat shimmer effect
            var shimmerSystem = GetAvailableParticleSystem();
            if (shimmerSystem != null)
            {
                var main = shimmerSystem.main;
                main.startColor = new Color(1f, 1f, 0.8f, 0.2f * strength);
                main.startLifetime = 0.8f;
                main.startSpeed = 0.3f;
                main.maxParticles = Mathf.RoundToInt(5 * strength);
                shimmerSystem.Play();
            }
        }
        
        private void ApplyOceanBiomeVisuals(float strength)
        {
            // Aquatic blue-green tinting
            TintAllMaterials(new Color(0.8f, 0.9f, 0.95f), strength * 0.35f);
            
            // Scales become more reflective
            foreach (var renderer in primaryRenderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_Metallic"))
                    {
                        float currentMetallic = material.GetFloat("_Metallic");
                        material.SetFloat("_Metallic", Mathf.Min(1f, currentMetallic + strength * 0.3f));
                    }
                    if (material.HasProperty("_Smoothness"))
                    {
                        float currentSmoothness = material.GetFloat("_Smoothness");
                        material.SetFloat("_Smoothness", Mathf.Min(1f, currentSmoothness + strength * 0.4f));
                    }
                }
            }
        }
        
        private void ApplyForestBiomeVisuals(float strength)
        {
            // Forest camouflage coloring
            TintAllMaterials(new Color(0.85f, 0.9f, 0.8f), strength * 0.25f);
            
            // Occasional leaf particles
            if (UnityEngine.Random.value < strength * 0.4f)
            {
                var leafSystem = GetAvailableParticleSystem();
                if (leafSystem != null)
                {
                    var main = leafSystem.main;
                    main.startColor = new Color(0.3f, 0.6f, 0.2f, 0.8f);
                    main.startLifetime = 2f + strength;
                    main.startSpeed = 0.2f;
                    main.maxParticles = Mathf.RoundToInt(3 * strength);
                    leafSystem.Play();
                }
            }
        }
        
        private void ApplyVolcanicBiomeVisuals(float strength)
        {
            // Heat-adapted reddish coloring
            TintAllMaterials(new Color(1f, 0.85f, 0.7f), strength * 0.4f);
            
            // Ember particles
            var emberSystem = GetAvailableParticleSystem();
            if (emberSystem != null)
            {
                var main = emberSystem.main;
                main.startColor = Color.Lerp(Color.red, Color.orange, UnityEngine.Random.value);
                main.startLifetime = 1f + strength;
                main.startSpeed = 0.5f + strength;
                main.maxParticles = Mathf.RoundToInt(8 * strength);
                emberSystem.Play();
            }
            
            // Heat glow
            foreach (var renderer in primaryRenderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor("_EmissionColor", Color.red * strength * 0.2f);
                        material.EnableKeyword("_EMISSION");
                    }
                }
            }
        }
        
        private void ApplyMountainBiomeVisuals(float strength)
        {
            // Rocky, hardy coloration
            TintAllMaterials(new Color(0.9f, 0.88f, 0.85f), strength * 0.3f);
            
            // Increased material hardness appearance
            foreach (var renderer in primaryRenderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_Metallic"))
                    {
                        float currentMetallic = material.GetFloat("_Metallic");
                        material.SetFloat("_Metallic", Mathf.Min(1f, currentMetallic + strength * 0.2f));
                    }
                }
            }
        }
        
        #endregion
        
        #region Magical Visual Methods
        
        private void ApplyFireMagicVisuals(float strength)
        {
            var fireSystem = GetAvailableParticleSystem();
            if (fireSystem != null)
            {
                var main = fireSystem.main;
                main.startColor = Color.Lerp(Color.red, Color.yellow, UnityEngine.Random.value);
                main.startLifetime = 1.5f + strength;
                main.startSpeed = 2f + strength * 2f;
                main.maxParticles = Mathf.RoundToInt(15 * strength);
                
                var emission = fireSystem.emission;
                emission.rateOverTime = 8f * strength;
                
                fireSystem.Play();
            }
            
            // Fire glow on materials
            ApplyMagicalGlow(new Color(1f, 0.5f, 0.2f), strength);
        }
        
        private void ApplyIceMagicVisuals(float strength)
        {
            var iceSystem = GetAvailableParticleSystem();
            if (iceSystem != null)
            {
                var main = iceSystem.main;
                main.startColor = Color.Lerp(Color.cyan, Color.white, UnityEngine.Random.value);
                main.startLifetime = 2f + strength;
                main.startSpeed = 0.5f + strength;
                main.maxParticles = Mathf.RoundToInt(12 * strength);
                
                iceSystem.Play();
            }
            
            // Ice blue glow
            ApplyMagicalGlow(Color.cyan, strength);
            
            // Frosted material effect
            TintAllMaterials(new Color(0.9f, 0.95f, 1f), strength * 0.2f);
        }
        
        private void ApplyLightningMagicVisuals(float strength)
        {
            var lightningSystem = GetAvailableParticleSystem();
            if (lightningSystem != null)
            {
                var main = lightningSystem.main;
                main.startColor = Color.yellow;
                main.startLifetime = 0.3f + strength * 0.2f;
                main.startSpeed = 8f + strength * 4f;
                main.maxParticles = Mathf.RoundToInt(6 * strength);
                
                var emission = lightningSystem.emission;
                emission.rateOverTime = 3f * strength;
                
                lightningSystem.Play();
            }
            
            // Electric yellow glow
            ApplyMagicalGlow(Color.yellow, strength);
        }
        
        private void ApplyNatureMagicVisuals(float strength)
        {
            var natureSystem = GetAvailableParticleSystem();
            if (natureSystem != null)
            {
                var main = natureSystem.main;
                main.startColor = Color.green;
                main.startLifetime = 3f + strength;
                main.startSpeed = 0.3f;
                main.maxParticles = Mathf.RoundToInt(10 * strength);
                
                natureSystem.Play();
            }
            
            // Natural green glow
            ApplyMagicalGlow(Color.green, strength * 0.7f);
        }
        
        private void ApplyShadowMagicVisuals(float strength)
        {
            var shadowSystem = GetAvailableParticleSystem();
            if (shadowSystem != null)
            {
                var main = shadowSystem.main;
                main.startColor = new Color(0.2f, 0.1f, 0.3f, 0.8f);
                main.startLifetime = 2f + strength;
                main.startSpeed = 1f;
                main.maxParticles = Mathf.RoundToInt(8 * strength);
                
                shadowSystem.Play();
            }
            
            // Dark purple glow
            ApplyMagicalGlow(new Color(0.4f, 0.2f, 0.6f), strength);
            
            // Darken materials slightly
            TintAllMaterials(new Color(0.9f, 0.9f, 0.9f), strength * 0.15f);
        }
        
        private void ApplyLightMagicVisuals(float strength)
        {
            var lightSystem = GetAvailableParticleSystem();
            if (lightSystem != null)
            {
                var main = lightSystem.main;
                main.startColor = Color.white;
                main.startLifetime = 2f + strength;
                main.startSpeed = 1.5f;
                main.maxParticles = Mathf.RoundToInt(12 * strength);
                
                lightSystem.Play();
            }
            
            // Bright white glow
            ApplyMagicalGlow(Color.white, strength);
            
            // Brighten materials
            TintAllMaterials(new Color(1.1f, 1.1f, 1.1f), strength * 0.2f);
        }
        
        private void ApplyWindMagicVisuals(float strength)
        {
            var windSystem = GetAvailableParticleSystem();
            if (windSystem != null)
            {
                var main = windSystem.main;
                main.startColor = new Color(0.8f, 0.9f, 1f, 0.6f);
                main.startLifetime = 1f + strength;
                main.startSpeed = 3f + strength * 2f;
                main.maxParticles = Mathf.RoundToInt(15 * strength);
                
                var velocityOverLifetime = windSystem.velocityOverLifetime;
                velocityOverLifetime.enabled = true;
                velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
                
                windSystem.Play();
            }
            
            // Light blue glow
            ApplyMagicalGlow(new Color(0.7f, 0.8f, 1f), strength * 0.6f);
        }
        
        private void ApplyGenericMagicVisuals(float strength)
        {
            var magicSystem = GetAvailableParticleSystem();
            if (magicSystem != null)
            {
                var main = magicSystem.main;
                main.startColor = Color.magenta;
                main.startLifetime = 2f + strength;
                main.startSpeed = 1f + strength;
                main.maxParticles = Mathf.RoundToInt(10 * strength);
                
                magicSystem.Play();
            }
            
            // Generic magical glow
            ApplyMagicalGlow(Color.magenta, strength);
        }
        
        private void ApplyMagicalAura(float power)
        {
            // Create a subtle magical aura around the creature
            foreach (var light in magicalLights)
            {
                if (light != null)
                {
                    light.enabled = true;
                    light.intensity = power * 0.5f;
                    light.range = 2f + power;
                    light.color = GetMagicalColor(creatureInstance.CreatureData.GeneticProfile);
                }
            }
            
            // Enhance all magical glows
            foreach (var renderer in primaryRenderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_EmissionColor"))
                    {
                        Color currentEmission = material.GetColor("_EmissionColor");
                        if (currentEmission.maxColorComponent > 0.01f)
                        {
                            material.SetColor("_EmissionColor", currentEmission * (1f + power * 0.3f));
                        }
                    }
                }
            }
        }
        
        private void ApplyMagicalGlow(Color glowColor, float intensity)
        {
            foreach (var renderer in primaryRenderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor("_EmissionColor", glowColor * intensity * 0.3f);
                        material.EnableKeyword("_EMISSION");
                    }
                }
            }
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

            // PERFORMANCE OPTIMIZED: Use hash code directly instead of string concatenation
            int hash = genetics.Generation.GetHashCode();
            hash = hash * 31 + genetics.Genes.Count.GetHashCode();
            hash = hash * 31 + genetics.Mutations.Count.GetHashCode();

            // Process first 10 genes without LINQ.Take() allocation
            int geneCount = 0;
            foreach (var gene in genetics.Genes)
            {
                if (geneCount >= 10) break;
                if (!string.IsNullOrEmpty(gene.traitName) && gene.value.HasValue)
                {
                    hash = hash * 31 + gene.traitName.GetHashCode();
                    hash = hash * 31 + ((int)(gene.value.Value * 100)).GetHashCode(); // Avoid float precision issues
                }
                geneCount++;
            }

            return hash.ToString(); // Only one ToString() call
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
                // Add more specific trait handlers as needed
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
        
        // Feature management helpers
        private void EnableFeatureGroup(string featureName, bool enabled)
        {
            Transform featureGroup = transform.Find(featureName) ?? transform.Find($"{featureName}Group");
            if (featureGroup != null)
            {
                featureGroup.gameObject.SetActive(enabled);
            }
        }
        
        private void ScaleFeatureGroup(string featureName, float scale)
        {
            Transform featureGroup = transform.Find(featureName) ?? transform.Find($"{featureName}Group");
            if (featureGroup != null)
            {
                featureGroup.localScale = Vector3.one * scale;
            }
        }
        
        private void SetFeatureVariant(string featureName, string variant)
        {
            Transform variantParent = transform.Find($"{featureName}Variants");
            if (variantParent != null)
            {
                // Disable all variants
                for (int i = 0; i < variantParent.childCount; i++)
                {
                    variantParent.GetChild(i).gameObject.SetActive(false);
                }
                
                // Enable specific variant
                Transform specificVariant = variantParent.Find(variant);
                if (specificVariant != null)
                {
                    specificVariant.gameObject.SetActive(true);
                }
            }
        }
        
        private void SetFeatureCount(string featureName, int count)
        {
            Transform featureGroup = transform.Find($"{featureName}Group");
            if (featureGroup != null)
            {
                for (int i = 0; i < featureGroup.childCount; i++)
                {
                    featureGroup.GetChild(i).gameObject.SetActive(i < count);
                }
            }
        }
        
        private void ApplyEyeGlow(float intensity)
        {
            Transform eyeGroup = transform.Find("EyeGroup") ?? transform.Find("Eyes");
            if (eyeGroup != null)
            {
                var eyeRenderers = eyeGroup.GetComponentsInChildren<Renderer>();
                foreach (var renderer in eyeRenderers)
                {
                    foreach (var material in renderer.materials)
                    {
                        if (material.HasProperty("_EmissionColor"))
                        {
                            Color eyeColor = material.color;
                            material.SetColor("_EmissionColor", eyeColor * intensity * 0.5f);
                            material.EnableKeyword("_EMISSION");
                        }
                    }
                }
            }
        }
        
        private ParticleSystem GetAvailableParticleSystem()
        {
            foreach (var ps in effectSystems)
            {
                if (ps != null && !ps.isPlaying)
                {
                    return ps;
                }
            }
            return null;
        }
        
        private void StopAllParticleEffects()
        {
            foreach (var ps in effectSystems)
            {
                if (ps != null && ps.isPlaying)
                {
                    ps.Stop();
                }
            }
        }
        
        private void TintAllMaterials(Color tintColor, float strength)
        {
            foreach (var renderer in primaryRenderers)
            {
                if (renderer == null) continue;
                
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        Color currentColor = materials[i].color;
                        materials[i].color = Color.Lerp(currentColor, currentColor * tintColor, strength);
                    }
                }
                renderer.materials = materials;
            }
        }
        
        private void ApplyGenerationRefinements(GeneticProfile genetics)
        {
            if (genetics.Generation <= 1) return;
            
            // Higher generation creatures get subtle refinements
            float refinementFactor = Mathf.Clamp01((genetics.Generation - 1) / 10f);
            
            // Slightly improved material quality
            foreach (var renderer in primaryRenderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_Smoothness"))
                    {
                        float currentSmoothness = material.GetFloat("_Smoothness");
                        material.SetFloat("_Smoothness", Mathf.Min(1f, currentSmoothness + refinementFactor * 0.1f));
                    }
                }
            }
            
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
                   $"Particle Effects: {CountActiveParticleEffects()}\n" +
                   $"Material Instances: {materialCache.Count}";
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
        
        private int CountActiveParticleEffects()
        {
            return effectSystems.Count(ps => ps != null && ps.isPlaying);
        }
        
        #endregion
    }
}