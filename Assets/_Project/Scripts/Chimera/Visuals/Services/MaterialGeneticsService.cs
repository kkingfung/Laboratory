using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Core.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Chimera.Visuals
{
    /// <summary>
    /// Service for applying genetic properties to materials.
    /// Handles material caching, property application, and genetic-based material modifications.
    /// Extracted from ProceduralVisualSystem for single responsibility.
    /// </summary>
    public class MaterialGeneticsService
    {
        private readonly Renderer[] primaryRenderers;
        private readonly Dictionary<string, Material> materialCache;

        public MaterialGeneticsService(Renderer[] primaryRenderers)
        {
            this.primaryRenderers = primaryRenderers;
            this.materialCache = new Dictionary<string, Material>();
        }

        /// <summary>
        /// Applies material genetics to all renderers
        /// </summary>
        public void ApplyMaterialGenetics(GeneticProfile genetics, string visualHash)
        {
            foreach (var renderer in primaryRenderers)
            {
                if (renderer == null) continue;
                ApplyMaterialToRenderer(renderer, genetics, visualHash);
            }
        }

        private void ApplyMaterialToRenderer(Renderer renderer, GeneticProfile genetics, string visualHash)
        {
            var materials = renderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;

                // Create material instance if not already cached
                string materialKey = $"{renderer.name}_{i}_{visualHash}";

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

        /// <summary>
        /// Tints all materials with a specific color and strength
        /// </summary>
        public void TintAllMaterials(Color tintColor, float strength)
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

        /// <summary>
        /// Applies magical glow to all materials
        /// </summary>
        public void ApplyMagicalGlow(Color glowColor, float intensity)
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

        /// <summary>
        /// Applies generation refinements to materials
        /// </summary>
        public void ApplyGenerationRefinements(int generation)
        {
            if (generation <= 1) return;

            float refinementFactor = Mathf.Clamp01((generation - 1) / 10f);

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
        }

        private Color GetMagicalColor(GeneticProfile genetics)
        {
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

        private float GetGeneticValue(GeneticProfile genetics, string traitName, float defaultValue)
        {
            var gene = genetics.Genes.FirstOrDefault(g =>
                g.traitName.Equals(traitName, System.StringComparison.OrdinalIgnoreCase) ||
                g.traitName.ToLower().Contains(traitName.ToLower()));

            return (!string.IsNullOrEmpty(gene.traitName) && gene.value.HasValue) ? gene.value.Value : defaultValue;
        }

        public int GetMaterialCacheCount() => materialCache.Count;
    }
}
