using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Chimera.Visuals.Data;

namespace Laboratory.Chimera.Visuals.Generators
{
    /// <summary>
    /// Material generation system based on genetic traits
    /// </summary>
    public class MaterialGenerator : MonoBehaviour
    {
        [Header("Material Configuration")]
        [SerializeField] private Material baseMaterial;
        [SerializeField] private bool createUniqueMaterials = true;
        [SerializeField] private bool enableShaderVariations = true;
        [SerializeField] private string[] availableShaders = { "Standard", "Standard (Specular setup)" };

        private Dictionary<string, Material> materialCache = new();

        public IEnumerator GenerateMaterials(VisualGeneticTraits traits, Renderer[] renderers)
        {
            Debug.Log("🎨 Generating genetic materials");

            var materialGenetics = ExtractMaterialGenetics(traits);

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                var newMaterial = GenerateMaterialForRenderer(renderer, materialGenetics, traits);
                renderer.material = newMaterial;

                yield return null;
            }

            Debug.Log("✨ Material generation complete");
        }

        private MaterialGenetics ExtractMaterialGenetics(VisualGeneticTraits traits)
        {
            return new MaterialGenetics
            {
                BaseMetallic = traits.Metallic,
                BaseSmoothness = 1f - traits.Roughness,
                BaseEmission = traits.Emission,
                EmissionColor = traits.HasGlow ? traits.AccentColor : Color.black,
                NormalStrength = Mathf.Lerp(0.5f, 2.0f, traits.PatternIntensity),
                OcclusionStrength = 1.0f,
                DetailScale = traits.PatternScale,
                UseCustomShader = traits.HasMagicalAura,
                ShaderName = traits.HasMagicalAura ? "Custom/MagicalCreature" : "Standard"
            };
        }

        private Material GenerateMaterialForRenderer(Renderer renderer, MaterialGenetics genetics, VisualGeneticTraits traits)
        {
            var cacheKey = CalculateMaterialHash(genetics, traits);

            if (materialCache.TryGetValue(cacheKey, out var cachedMaterial))
            {
                return cachedMaterial;
            }

            Material sourceMaterial = renderer.material;
            if (baseMaterial != null)
            {
                sourceMaterial = baseMaterial;
            }

            var newMaterial = createUniqueMaterials ?
                new Material(sourceMaterial) :
                sourceMaterial;

            ApplyGeneticProperties(newMaterial, genetics, traits);

            if (createUniqueMaterials)
            {
                materialCache[cacheKey] = newMaterial;
            }

            return newMaterial;
        }

        private void ApplyGeneticProperties(Material material, MaterialGenetics genetics, VisualGeneticTraits traits)
        {
            // Basic material properties
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", traits.PrimaryColor);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", genetics.BaseMetallic);

            if (material.HasProperty("_Glossiness") || material.HasProperty("_Smoothness"))
            {
                var smoothnessProperty = material.HasProperty("_Smoothness") ? "_Smoothness" : "_Glossiness";
                material.SetFloat(smoothnessProperty, genetics.BaseSmoothness);
            }

            // Emission properties
            if (traits.HasGlow || genetics.BaseEmission > 0)
            {
                if (material.HasProperty("_EmissionColor"))
                {
                    var emissionColor = genetics.EmissionColor * genetics.BaseEmission;
                    material.SetColor("_EmissionColor", emissionColor);
                    material.EnableKeyword("_EMISSION");
                }
            }

            // Iridescence effect
            if (traits.Iridescence > 0.1f)
            {
                ApplyIridescenceEffect(material, traits);
            }

            // Transparency
            if (traits.Transparency > 0.1f)
            {
                ApplyTransparencyEffect(material, traits);
            }

            // Magical properties
            if (traits.HasMagicalAura)
            {
                ApplyMagicalProperties(material, traits);
            }

            // Pattern-related properties
            if (material.HasProperty("_DetailNormalMapScale"))
                material.SetFloat("_DetailNormalMapScale", genetics.NormalStrength);

            if (material.HasProperty("_DetailAlbedoMapScale"))
                material.SetFloat("_DetailAlbedoMapScale", genetics.DetailScale);

            // Color variation
            ApplyColorVariation(material, traits);
        }

        private void ApplyIridescenceEffect(Material material, VisualGeneticTraits traits)
        {
            // Simulate iridescence using reflection and emission
            if (material.HasProperty("_Metallic"))
            {
                var currentMetallic = material.GetFloat("_Metallic");
                material.SetFloat("_Metallic", Mathf.Max(currentMetallic, traits.Iridescence * 0.8f));
            }

            if (material.HasProperty("_EmissionColor"))
            {
                var iridescenceColor = Color.HSVToRGB(
                    (Time.time * 0.1f + traits.Iridescence) % 1f,
                    0.8f,
                    traits.Iridescence * 0.5f
                );

                var currentEmission = material.GetColor("_EmissionColor");
                material.SetColor("_EmissionColor", currentEmission + iridescenceColor);
            }
        }

        private void ApplyTransparencyEffect(Material material, VisualGeneticTraits traits)
        {
            // Switch to transparent rendering mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;

            var color = material.GetColor("_Color");
            color.a = 1f - traits.Transparency;
            material.SetColor("_Color", color);
        }

        private void ApplyMagicalProperties(Material material, VisualGeneticTraits traits)
        {
            // Enhanced emission for magical creatures
            if (material.HasProperty("_EmissionColor"))
            {
                var magicalColor = traits.AccentColor * traits.MagicalIntensity * 2f;
                var currentEmission = material.GetColor("_EmissionColor");
                material.SetColor("_EmissionColor", currentEmission + magicalColor);
                material.EnableKeyword("_EMISSION");
            }

            // Magical rim lighting effect
            if (material.HasProperty("_RimColor"))
            {
                material.SetColor("_RimColor", traits.AccentColor * traits.MagicalIntensity);
                material.SetFloat("_RimPower", 2.0f + traits.MagicalIntensity * 3f);
            }

            // Animated magical properties
            StartCoroutine(AnimateMagicalProperties(material, traits));
        }

        private void ApplyColorVariation(Material material, VisualGeneticTraits traits)
        {
            if (traits.ColorVariation > 0.1f)
            {
                // Apply subtle color variations
                var baseColor = traits.PrimaryColor;
                var variation = traits.ColorVariation * 0.2f;

                var hue = 0f;
                var saturation = 0f;
                var value = 0f;
                Color.RGBToHSV(baseColor, out hue, out saturation, out value);

                // Add variation
                hue += Random.Range(-variation, variation);
                saturation += Random.Range(-variation * 0.5f, variation * 0.5f);
                value += Random.Range(-variation * 0.3f, variation * 0.3f);

                // Clamp values
                hue = hue % 1f;
                saturation = Mathf.Clamp01(saturation);
                value = Mathf.Clamp01(value);

                var variedColor = Color.HSVToRGB(hue, saturation, value);
                variedColor.a = baseColor.a;

                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", variedColor);
                }
            }
        }

        private IEnumerator AnimateMagicalProperties(Material material, VisualGeneticTraits traits)
        {
            var originalEmission = material.HasProperty("_EmissionColor") ?
                material.GetColor("_EmissionColor") : Color.black;

            while (traits.HasMagicalAura && material != null)
            {
                if (material.HasProperty("_EmissionColor"))
                {
                    var pulse = Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f;
                    var animatedEmission = originalEmission * (1f + pulse * traits.MagicalIntensity);
                    material.SetColor("_EmissionColor", animatedEmission);
                }

                yield return new WaitForSeconds(0.05f);
            }
        }

        private string CalculateMaterialHash(MaterialGenetics genetics, VisualGeneticTraits traits)
        {
            return $"{genetics.BaseMetallic}_{genetics.BaseSmoothness}_{traits.PrimaryColor}_{traits.HasMagicalAura}".GetHashCode().ToString();
        }

        private void OnDestroy()
        {
            // Clean up created materials
            foreach (var material in materialCache.Values)
            {
                if (material != null && createUniqueMaterials)
                {
                    DestroyImmediate(material);
                }
            }
            materialCache.Clear();
        }
    }
}