using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Chimera.Visuals.Data;

namespace Laboratory.Chimera.Visuals.Systems
{
    /// <summary>
    /// Handles visual adaptation to different biomes and environments
    /// </summary>
    public class BiomeAdaptationSystem : MonoBehaviour
    {
        [Header("Adaptation Settings")]
        [SerializeField] private float adaptationSpeed = 1.0f;
        [SerializeField] private float maxAdaptationStrength = 0.8f;
        [SerializeField] private bool enableRealTimeAdaptation = true;
        [SerializeField] private bool enableSeasonalChanges = true;

        [Header("Biome Configurations")]
        [SerializeField] private BiomeAdaptationConfig[] biomeConfigs;

        private Dictionary<BiomeType, BiomeAdaptation> biomeAdaptations = new();
        private BiomeType currentBiome = BiomeType.Forest;
        private BiomeType targetBiome = BiomeType.Forest;
        private float adaptationProgress = 1.0f;
        private Coroutine adaptationCoroutine;

        // Cached components
        private Renderer[] renderers;
        private Dictionary<Renderer, Material> originalMaterials = new();
        private Dictionary<Renderer, Color> originalColors = new();

        private void Awake()
        {
            InitializeBiomeAdaptations();
            CacheRenderers();
        }

        private void Start()
        {
            DetectCurrentBiome();
        }

        public void AdaptToBiome(BiomeType biome, VisualGeneticTraits traits)
        {
            if (biome == currentBiome && adaptationProgress >= 1.0f) return;

            targetBiome = biome;

            if (adaptationCoroutine != null)
            {
                StopCoroutine(adaptationCoroutine);
            }

            adaptationCoroutine = StartCoroutine(PerformBiomeAdaptation(traits));
            Debug.Log($"🌍 Starting adaptation to {biome} biome");
        }

        private void InitializeBiomeAdaptations()
        {
            // Initialize default biome adaptations
            biomeAdaptations[BiomeType.Forest] = new BiomeAdaptation
            {
                TargetBiome = BiomeType.Forest,
                AdaptiveColor = new Color(0.4f, 0.8f, 0.3f, 1f),
                AdaptationStrength = 0.6f,
                BiomeModifiers = new Dictionary<string, float>
                {
                    {"Camouflage", 0.8f},
                    {"Moisture", 0.7f},
                    {"Vegetation", 1.0f}
                },
                HasCamouflage = true,
                CamouflageEffectiveness = 0.7f
            };

            biomeAdaptations[BiomeType.Desert] = new BiomeAdaptation
            {
                TargetBiome = BiomeType.Desert,
                AdaptiveColor = new Color(0.9f, 0.7f, 0.4f, 1f),
                AdaptationStrength = 0.8f,
                BiomeModifiers = new Dictionary<string, float>
                {
                    {"HeatResistance", 1.0f},
                    {"WaterConservation", 0.9f},
                    {"SandAdaptation", 0.8f}
                },
                HasCamouflage = true,
                CamouflageEffectiveness = 0.6f
            };

            biomeAdaptations[BiomeType.Tundra] = new BiomeAdaptation
            {
                TargetBiome = BiomeType.Tundra,
                AdaptiveColor = new Color(0.9f, 0.9f, 1.0f, 1f),
                AdaptationStrength = 0.7f,
                BiomeModifiers = new Dictionary<string, float>
                {
                    {"ColdResistance", 1.0f},
                    {"Insulation", 0.8f},
                    {"SnowCamouflage", 0.9f}
                },
                HasCamouflage = true,
                CamouflageEffectiveness = 0.8f
            };

            biomeAdaptations[BiomeType.Ocean] = new BiomeAdaptation
            {
                TargetBiome = BiomeType.Ocean,
                AdaptiveColor = new Color(0.2f, 0.5f, 0.8f, 1f),
                AdaptationStrength = 0.8f,
                BiomeModifiers = new Dictionary<string, float>
                {
                    {"Waterproofing", 1.0f},
                    {"Swimming", 0.9f},
                    {"Pressure", 0.7f}
                },
                HasCamouflage = true,
                CamouflageEffectiveness = 0.8f
            };

            biomeAdaptations[BiomeType.Volcanic] = new BiomeAdaptation
            {
                TargetBiome = BiomeType.Volcanic,
                AdaptiveColor = new Color(1.0f, 0.3f, 0.1f, 1f),
                AdaptationStrength = 0.9f,
                BiomeModifiers = new Dictionary<string, float>
                {
                    {"HeatResistance", 1.0f},
                    {"ToxicResistance", 0.8f},
                    {"LavaAdaptation", 0.9f}
                },
                HasCamouflage = false,
                CamouflageEffectiveness = 0.2f
            };

            biomeAdaptations[BiomeType.Cave] = new BiomeAdaptation
            {
                TargetBiome = BiomeType.Cave,
                AdaptiveColor = new Color(0.3f, 0.3f, 0.4f, 1f),
                AdaptationStrength = 0.6f,
                BiomeModifiers = new Dictionary<string, float>
                {
                    {"DarkVision", 1.0f},
                    {"EchoLocation", 0.7f},
                    {"RockAdaptation", 0.8f}
                },
                HasCamouflage = true,
                CamouflageEffectiveness = 0.9f
            };

            // Load custom configurations if available
            if (biomeConfigs != null)
            {
                foreach (var config in biomeConfigs)
                {
                    if (config != null)
                    {
                        biomeAdaptations[config.BiomeType] = config.ToAdaptationData();
                    }
                }
            }
        }

        private void CacheRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    originalMaterials[renderer] = renderer.material;
                    if (renderer.material.HasProperty("_Color"))
                    {
                        originalColors[renderer] = renderer.material.GetColor("_Color");
                    }
                }
            }
        }

        private IEnumerator PerformBiomeAdaptation(VisualGeneticTraits traits)
        {
            if (!biomeAdaptations.TryGetValue(targetBiome, out var adaptation))
            {
                Debug.LogWarning($"No adaptation data found for biome: {targetBiome}");
                yield break;
            }

            var startBiome = currentBiome;
            var startAdaptation = biomeAdaptations.GetValueOrDefault(startBiome, adaptation);

            adaptationProgress = 0f;
            float adaptationTime = 2.0f / adaptationSpeed;

            while (adaptationProgress < 1.0f)
            {
                adaptationProgress += Time.deltaTime / adaptationTime;
                adaptationProgress = Mathf.Clamp01(adaptationProgress);

                ApplyAdaptationVisuals(startAdaptation, adaptation, adaptationProgress, traits);

                yield return null;
            }

            currentBiome = targetBiome;
            adaptationProgress = 1.0f;

            Debug.Log($"🌍 Adaptation to {targetBiome} complete");
            adaptationCoroutine = null;
        }

        private void ApplyAdaptationVisuals(BiomeAdaptation fromAdaptation, BiomeAdaptation toAdaptation, float progress, VisualGeneticTraits traits)
        {
            var adaptiveColor = Color.Lerp(fromAdaptation.AdaptiveColor, toAdaptation.AdaptiveColor, progress);
            var adaptationStrength = Mathf.Lerp(fromAdaptation.AdaptationStrength, toAdaptation.AdaptationStrength, progress) * maxAdaptationStrength;

            foreach (var renderer in renderers)
            {
                if (renderer == null || !originalColors.ContainsKey(renderer)) continue;

                var material = renderer.material;
                if (material == null || !material.HasProperty("_Color")) continue;

                var originalColor = originalColors[renderer];
                var blendedColor = Color.Lerp(originalColor, adaptiveColor, adaptationStrength);

                // Blend with creature's genetic colors
                blendedColor = Color.Lerp(blendedColor, traits.PrimaryColor, 0.3f);

                material.SetColor("_Color", blendedColor);

                // Apply camouflage effect
                if (toAdaptation.HasCamouflage)
                {
                    ApplyCamouflageEffect(material, toAdaptation, progress);
                }

                // Apply biome-specific material modifications
                ApplyBiomeSpecificEffects(material, toAdaptation, progress, traits);
            }
        }

        private void ApplyCamouflageEffect(Material material, BiomeAdaptation adaptation, float progress)
        {
            if (!material.HasProperty("_Metallic")) return;

            var camouflageStrength = adaptation.CamouflageEffectiveness * progress;
            var reducedMetallic = material.GetFloat("_Metallic") * (1f - camouflageStrength * 0.5f);
            material.SetFloat("_Metallic", reducedMetallic);

            if (material.HasProperty("_Smoothness"))
            {
                var reducedSmoothness = material.GetFloat("_Smoothness") * (1f - camouflageStrength * 0.3f);
                material.SetFloat("_Smoothness", reducedSmoothness);
            }
        }

        private void ApplyBiomeSpecificEffects(Material material, BiomeAdaptation adaptation, float progress, VisualGeneticTraits traits)
        {
            switch (adaptation.TargetBiome)
            {
                case BiomeType.Desert:
                    ApplyDesertEffects(material, progress);
                    break;
                case BiomeType.Tundra:
                    ApplyTundraEffects(material, progress);
                    break;
                case BiomeType.Ocean:
                    ApplyOceanEffects(material, progress);
                    break;
                case BiomeType.Volcanic:
                    ApplyVolcanicEffects(material, progress, traits);
                    break;
                case BiomeType.Cave:
                    ApplyCaveEffects(material, progress);
                    break;
            }
        }

        private void ApplyDesertEffects(Material material, float progress)
        {
            if (material.HasProperty("_Smoothness"))
            {
                var sandTexture = material.GetFloat("_Smoothness") * (1f - progress * 0.4f);
                material.SetFloat("_Smoothness", sandTexture);
            }
        }

        private void ApplyTundraEffects(Material material, float progress)
        {
            if (material.HasProperty("_EmissionColor"))
            {
                var coldEmission = Color.blue * progress * 0.1f;
                material.SetColor("_EmissionColor", coldEmission);
                material.EnableKeyword("_EMISSION");
            }
        }

        private void ApplyOceanEffects(Material material, float progress)
        {
            if (material.HasProperty("_Metallic"))
            {
                var wetness = Mathf.Lerp(material.GetFloat("_Metallic"), 0.8f, progress * 0.6f);
                material.SetFloat("_Metallic", wetness);
            }
        }

        private void ApplyVolcanicEffects(Material material, float progress, VisualGeneticTraits traits)
        {
            if (material.HasProperty("_EmissionColor"))
            {
                var lavaGlow = Color.red * progress * 0.5f;
                var currentEmission = material.GetColor("_EmissionColor");
                material.SetColor("_EmissionColor", currentEmission + lavaGlow);
                material.EnableKeyword("_EMISSION");
            }
        }

        private void ApplyCaveEffects(Material material, float progress)
        {
            if (material.HasProperty("_Color"))
            {
                var darkening = 1f - progress * 0.3f;
                var currentColor = material.GetColor("_Color");
                material.SetColor("_Color", currentColor * darkening);
            }
        }

        private void DetectCurrentBiome()
        {
            // Simple biome detection based on environment
            // In a real implementation, this would query the world system
            currentBiome = BiomeType.Forest;
            adaptationProgress = 1.0f;
        }

        public BiomeType GetCurrentBiome() => currentBiome;
        public float GetAdaptationProgress() => adaptationProgress;
        public bool IsAdapting() => adaptationCoroutine != null;

        private void OnDestroy()
        {
            if (adaptationCoroutine != null)
            {
                StopCoroutine(adaptationCoroutine);
            }

            // Restore original materials
            foreach (var kvp in originalMaterials)
            {
                if (kvp.Key != null && kvp.Value != null)
                {
                    kvp.Key.material = kvp.Value;
                }
            }
        }
    }

    [System.Serializable]
    public class BiomeAdaptationConfig
    {
        public BiomeType BiomeType;
        public Color AdaptiveColor = Color.white;
        public float AdaptationStrength = 0.5f;
        public bool HasCamouflage = true;
        public float CamouflageEffectiveness = 0.5f;

        public BiomeAdaptation ToAdaptationData()
        {
            return new BiomeAdaptation
            {
                TargetBiome = BiomeType,
                AdaptiveColor = AdaptiveColor,
                AdaptationStrength = AdaptationStrength,
                BiomeModifiers = new Dictionary<string, float>(),
                HasCamouflage = HasCamouflage,
                CamouflageEffectiveness = CamouflageEffectiveness
            };
        }
    }
}