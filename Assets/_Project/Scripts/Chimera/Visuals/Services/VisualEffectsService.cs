using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Core.Enums;
using System.Linq;

namespace Laboratory.Chimera.Visuals
{
    /// <summary>
    /// Service for managing all visual effects including particles, biome adaptations, and magical effects.
    /// Consolidates particle generation, biome-specific visuals, and magical manifestations.
    /// Extracted from ProceduralVisualSystem for single responsibility.
    /// </summary>
    public class VisualEffectsService
    {
        private readonly ParticleSystem[] effectSystems;
        private readonly Light[] magicalLights;
        private readonly MaterialGeneticsService materialService;
        private readonly bool enableLODOptimization;
        private readonly int maxSimultaneousEffects;

        public VisualEffectsService(
            ParticleSystem[] effectSystems,
            Light[] magicalLights,
            MaterialGeneticsService materialService,
            bool enableLODOptimization,
            int maxSimultaneousEffects)
        {
            this.effectSystems = effectSystems;
            this.magicalLights = magicalLights;
            this.materialService = materialService;
            this.enableLODOptimization = enableLODOptimization;
            this.maxSimultaneousEffects = maxSimultaneousEffects;
        }

        /// <summary>
        /// Generates all particle effects based on genetics
        /// </summary>
        public void GenerateParticleEffects(GeneticProfile genetics)
        {
            StopAllParticleEffects();
            ApplyMagicalParticleEffects(genetics);
            ApplyEnvironmentalParticleEffects(genetics);
            ApplyGenerationParticleEffects(genetics);
        }

        /// <summary>
        /// Applies biome-specific visual adaptations
        /// </summary>
        public void ApplyBiomeAdaptations(GeneticProfile genetics)
        {
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
                    ApplyArcticBiomeVisuals(strength);
                else if (geneNameLower.Contains("desert") || geneNameLower.Contains("sand"))
                    ApplyDesertBiomeVisuals(strength);
                else if (geneNameLower.Contains("ocean") || geneNameLower.Contains("aquatic"))
                    ApplyOceanBiomeVisuals(strength);
                else if (geneNameLower.Contains("forest") || geneNameLower.Contains("woodland"))
                    ApplyForestBiomeVisuals(strength);
                else if (geneNameLower.Contains("volcanic") || geneNameLower.Contains("lava"))
                    ApplyVolcanicBiomeVisuals(strength);
                else if (geneNameLower.Contains("mountain") || geneNameLower.Contains("alpine"))
                    ApplyMountainBiomeVisuals(strength);
            }
        }

        /// <summary>
        /// Applies magical trait visual manifestations
        /// </summary>
        public void ApplyMagicalTraits(GeneticProfile genetics)
        {
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
                    ApplyFireMagicVisuals(magicStrength);
                else if (geneNameLower.Contains("ice") || geneNameLower.Contains("frost"))
                    ApplyIceMagicVisuals(magicStrength);
                else if (geneNameLower.Contains("lightning") || geneNameLower.Contains("electric"))
                    ApplyLightningMagicVisuals(magicStrength);
                else if (geneNameLower.Contains("nature") || geneNameLower.Contains("earth"))
                    ApplyNatureMagicVisuals(magicStrength);
                else if (geneNameLower.Contains("shadow") || geneNameLower.Contains("dark"))
                    ApplyShadowMagicVisuals(magicStrength);
                else if (geneNameLower.Contains("light") || geneNameLower.Contains("holy"))
                    ApplyLightMagicVisuals(magicStrength);
                else if (geneNameLower.Contains("wind") || geneNameLower.Contains("air"))
                    ApplyWindMagicVisuals(magicStrength);
            }

            // Apply overall magical aura based on total magical power
            if (totalMagicalPower > 0.5f)
            {
                ApplyMagicalAura(Mathf.Min(totalMagicalPower, 2.0f), genetics);
            }
        }

        #region Particle Effect Generation

        private void ApplyMagicalParticleEffects(GeneticProfile genetics)
        {
            float magicalPower = GetGeneticValue(genetics, "Magical", 0f);
            if (magicalPower < 0.3f) return;

            var magicalGenes = genetics.Genes.Where(g => g.traitType == TraitType.Elemental && g.value > 0.4f).ToArray();

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

            var main = availableSystem.main;
            var emission = availableSystem.emission;

            main.startLifetime = 2f + intensity;
            emission.rateOverTime = 10f * intensity;

            switch (magicType.ToLower())
            {
                case var name when name.Contains("fire"):
                    main.startColor = Color.Lerp(Color.red, Color.yellow, Random.value);
                    main.startSpeed = 3f;
                    break;
                case var name when name.Contains("ice"):
                    main.startColor = Color.Lerp(Color.cyan, Color.white, Random.value);
                    main.startSpeed = 1f;
                    break;
                case var name when name.Contains("lightning"):
                    main.startColor = Color.yellow;
                    main.startSpeed = 8f;
                    emission.rateOverTime = 5f * intensity;
                    break;
                default:
                    main.startColor = Color.magenta;
                    main.startSpeed = 2f;
                    break;
            }

            availableSystem.Play();
        }

        private void ApplyEnvironmentalParticleEffects(GeneticProfile genetics)
        {
            var envGenes = genetics.Genes.Where(g => g.traitType == TraitType.Environmental).ToArray();

            foreach (var gene in envGenes)
            {
                if ((gene.value ?? 0f) < 0.4f) continue;
                CreateEnvironmentalParticleEffect(gene.traitName, gene.value ?? 0.5f);
            }
        }

        private void CreateEnvironmentalParticleEffect(string adaptationType, float intensity)
        {
            var availableSystem = GetAvailableParticleSystem();
            if (availableSystem == null) return;

            var main = availableSystem.main;
            var emission = availableSystem.emission;
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

            emission.rateOverTime = intensity * 8f;
            availableSystem.Play();
        }

        private void ApplyGenerationParticleEffects(GeneticProfile genetics)
        {
            if (genetics.Generation <= 2) return;

            float generationFactor = Mathf.Clamp01((genetics.Generation - 2) / 8f);

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

        #endregion

        #region Biome Adaptations

        private void ApplyArcticBiomeVisuals(float strength)
        {
            materialService.TintAllMaterials(new Color(0.9f, 0.95f, 1f), strength * 0.3f);

            var breathEffect = GetAvailableParticleSystem();
            if (breathEffect != null)
            {
                var main = breathEffect.main;
                main.startColor = Color.white;
                main.startLifetime = 1f;
                main.startSpeed = 0.5f;
                breathEffect.Play();
            }
        }

        private void ApplyDesertBiomeVisuals(float strength)
        {
            materialService.TintAllMaterials(new Color(1f, 0.92f, 0.8f), strength * 0.4f);

            var shimmerSystem = GetAvailableParticleSystem();
            if (shimmerSystem != null)
            {
                var main = shimmerSystem.main;
                main.startColor = new Color(1f, 1f, 0.8f, 0.2f * strength);
                main.startLifetime = 0.8f;
                main.startSpeed = 0.3f;
                shimmerSystem.Play();
            }
        }

        private void ApplyOceanBiomeVisuals(float strength)
        {
            materialService.TintAllMaterials(new Color(0.8f, 0.9f, 0.95f), strength * 0.35f);
        }

        private void ApplyForestBiomeVisuals(float strength)
        {
            materialService.TintAllMaterials(new Color(0.85f, 0.9f, 0.8f), strength * 0.25f);
        }

        private void ApplyVolcanicBiomeVisuals(float strength)
        {
            materialService.TintAllMaterials(new Color(1f, 0.85f, 0.7f), strength * 0.4f);

            var emberSystem = GetAvailableParticleSystem();
            if (emberSystem != null)
            {
                var main = emberSystem.main;
                main.startColor = Color.Lerp(Color.red, Color.orange, Random.value);
                main.startLifetime = 1f + strength;
                main.startSpeed = 0.5f + strength;
                emberSystem.Play();
            }

            materialService.ApplyMagicalGlow(Color.red, strength * 0.2f);
        }

        private void ApplyMountainBiomeVisuals(float strength)
        {
            materialService.TintAllMaterials(new Color(0.9f, 0.88f, 0.85f), strength * 0.3f);
        }

        #endregion

        #region Magical Visuals

        private void ApplyFireMagicVisuals(float strength)
        {
            var fireSystem = GetAvailableParticleSystem();
            if (fireSystem != null)
            {
                var main = fireSystem.main;
                main.startColor = Color.Lerp(Color.red, Color.yellow, Random.value);
                main.startLifetime = 1.5f + strength;
                main.startSpeed = 2f + strength * 2f;
                fireSystem.Play();
            }

            materialService.ApplyMagicalGlow(new Color(1f, 0.5f, 0.2f), strength);
        }

        private void ApplyIceMagicVisuals(float strength)
        {
            var iceSystem = GetAvailableParticleSystem();
            if (iceSystem != null)
            {
                var main = iceSystem.main;
                main.startColor = Color.Lerp(Color.cyan, Color.white, Random.value);
                main.startLifetime = 2f + strength;
                main.startSpeed = 0.5f + strength;
                iceSystem.Play();
            }

            materialService.ApplyMagicalGlow(Color.cyan, strength);
            materialService.TintAllMaterials(new Color(0.9f, 0.95f, 1f), strength * 0.2f);
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
                lightningSystem.Play();
            }

            materialService.ApplyMagicalGlow(Color.yellow, strength);
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
                natureSystem.Play();
            }

            materialService.ApplyMagicalGlow(Color.green, strength * 0.7f);
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
                shadowSystem.Play();
            }

            materialService.ApplyMagicalGlow(new Color(0.4f, 0.2f, 0.6f), strength);
            materialService.TintAllMaterials(new Color(0.9f, 0.9f, 0.9f), strength * 0.15f);
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
                lightSystem.Play();
            }

            materialService.ApplyMagicalGlow(Color.white, strength);
            materialService.TintAllMaterials(new Color(1.1f, 1.1f, 1.1f), strength * 0.2f);
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
                windSystem.Play();
            }

            materialService.ApplyMagicalGlow(new Color(0.7f, 0.8f, 1f), strength * 0.6f);
        }

        private void ApplyMagicalAura(float power, GeneticProfile genetics)
        {
            foreach (var light in magicalLights)
            {
                if (light != null)
                {
                    light.enabled = true;
                    light.intensity = power * 0.5f;
                    light.range = 2f + power;
                }
            }
        }

        #endregion

        #region Utility Methods

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

        public int CountActiveParticleEffects()
        {
            return effectSystems.Count(ps => ps != null && ps.isPlaying);
        }

        private float GetGeneticValue(GeneticProfile genetics, string traitName, float defaultValue)
        {
            var gene = genetics.Genes.FirstOrDefault(g =>
                g.traitName.Equals(traitName, System.StringComparison.OrdinalIgnoreCase) ||
                g.traitName.ToLower().Contains(traitName.ToLower()));

            return (!string.IsNullOrEmpty(gene.traitName) && gene.value.HasValue) ? gene.value.Value : defaultValue;
        }

        #endregion
    }
}
