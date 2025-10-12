using UnityEngine;
using Laboratory.Chimera.Genetics;
using System.Linq;

namespace Laboratory.Chimera.Visuals
{
    /// <summary>
    /// Enhanced visual genetics system that extends the existing CreatureInstanceComponent
    /// ApplyVisualGenetics method with many more genetic traits and visual effects.
    /// 
    /// This directly replaces/enhances the basic Color + Size system currently in place.
    /// </summary>
    public static class EnhancedVisualGenetics
    {
        /// <summary>
        /// Enhanced version of ApplyVisualGenetics that uses many more genetic traits
        /// Call this instead of the basic ApplyVisualGenetics method
        /// </summary>
        public static void ApplyEnhancedVisualGenetics(this CreatureInstanceComponent creature)
        {
            if (creature?.CreatureData?.GeneticProfile == null) return;
            
            var genetics = creature.CreatureData.GeneticProfile;
            var renderers = creature.GetComponentsInChildren<Renderer>();
            
            UnityEngine.Debug.Log($"🎨 Applying enhanced visual genetics to {creature.name}");
            
            // 1. Enhanced Size System (replaces basic size)
            ApplyAdvancedSizing(creature, genetics);
            
            // 2. Advanced Color System (replaces basic color)
            ApplyAdvancedColoring(creature, genetics, renderers);
            
            // 3. NEW: Pattern System
            ApplyPatternGenetics(creature, genetics, renderers);
            
            // 4. NEW: Material Properties
            ApplyMaterialGenetics(creature, genetics, renderers);
            
            // 5. NEW: Special Effects
            ApplySpecialEffects(creature, genetics);
            
            // 6. NEW: Mutation Visuals
            ApplyMutationVisuals(creature, genetics, renderers);
            
            UnityEngine.Debug.Log($"✅ Enhanced visual genetics applied to {creature.name}");
        }
        
        #region Advanced Sizing
        
        private static void ApplyAdvancedSizing(CreatureInstanceComponent creature, GeneticProfile genetics)
        {
            // Base size from genetics (improved version of existing system)
            var sizeGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Size");
            var strengthGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Strength");
            var agilityGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Agility");
            
            float baseSize = 1.0f;
            if (!string.IsNullOrEmpty(sizeGene.traitName) && sizeGene.value.HasValue)
            {
                baseSize = 0.7f + (sizeGene.value.Value * 0.6f); // 0.7x to 1.3x size
            }
            
            // Apply additional size modifiers from other traits
            if (!string.IsNullOrEmpty(strengthGene.traitName) && strengthGene.value.HasValue)
            {
                baseSize *= (1f + strengthGene.value.Value * 0.1f); // Strength adds bulk
            }
            
            creature.transform.localScale = Vector3.one * baseSize;
            
            // Apply proportional scaling to child objects
            ApplyProportionalScaling(creature, genetics);
        }
        
        private static void ApplyProportionalScaling(CreatureInstanceComponent creature, GeneticProfile genetics)
        {
            var childTransforms = creature.GetComponentsInChildren<Transform>();
            
            foreach (var child in childTransforms)
            {
                if (child == creature.transform) continue; // Skip self
                
                Vector3 proportionalScale = Vector3.one;
                
                // Different body parts scale differently based on genetics
                if (child.name.ToLower().Contains("head"))
                {
                    var intelligenceGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Intelligence");
                    if (!string.IsNullOrEmpty(intelligenceGene.traitName) && intelligenceGene.value.HasValue)
                    {
                        proportionalScale *= (1f + intelligenceGene.value.Value * 0.15f);
                    }
                }
                else if (child.name.ToLower().Contains("muscle") || child.name.ToLower().Contains("chest"))
                {
                    var strengthGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Strength");
                    if (!string.IsNullOrEmpty(strengthGene.traitName) && strengthGene.value.HasValue)
                    {
                        proportionalScale *= (1f + strengthGene.value.Value * 0.2f);
                    }
                }
                else if (child.name.ToLower().Contains("leg") || child.name.ToLower().Contains("limb"))
                {
                    var agilityGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Agility");
                    if (!string.IsNullOrEmpty(agilityGene.traitName) && agilityGene.value.HasValue)
                    {
                        proportionalScale.y *= (1f + agilityGene.value.Value * 0.1f); // Longer legs
                    }
                }
                
                child.localScale = proportionalScale;
            }
        }
        
        #endregion
        
        #region Advanced Coloring
        
        private static void ApplyAdvancedColoring(CreatureInstanceComponent creature, GeneticProfile genetics, Renderer[] renderers)
        {
            // Extract multiple color genes
            var primaryColorGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Primary Color" || g.traitName == "Color");
            var secondaryColorGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Secondary Color");
            var eyeColorGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Eye Color");
            
            // Generate primary color
            Color primaryColor = Color.white;
            if (!string.IsNullOrEmpty(primaryColorGene.traitName) && primaryColorGene.value.HasValue)
            {
                float hue = primaryColorGene.value.Value;
                float saturation = 0.6f + (genetics.GetGeneticPurity() * 0.4f);
                float brightness = 0.7f + (Random.value * 0.3f);
                primaryColor = Color.HSVToRGB(hue, saturation, brightness);
            }
            
            // Generate secondary color
            Color secondaryColor = primaryColor;
            if (!string.IsNullOrEmpty(secondaryColorGene.traitName) && secondaryColorGene.value.HasValue)
            {
                float hue = secondaryColorGene.value.Value;
                secondaryColor = Color.HSVToRGB(hue, 0.5f, 0.8f);
            }
            else
            {
                // Generate complementary color
                Color.RGBToHSV(primaryColor, out float h, out float s, out float v);
                secondaryColor = Color.HSVToRGB((h + 0.3f) % 1f, s * 0.7f, v * 0.9f);
            }
            
            // Apply biome environmental influence
            ApplyBiomeInfluence(genetics, ref primaryColor, ref secondaryColor);
            
            // Apply colors to renderers
            ApplyColorsToRenderers(renderers, primaryColor, secondaryColor);
            
            // Special eye coloring
            ApplyEyeColoring(creature, genetics, eyeColorGene);
        }
        
        private static void ApplyBiomeInfluence(GeneticProfile genetics, ref Color primary, ref Color secondary)
        {
            var envGenes = genetics.Genes.Where(g => g.traitName.Contains("Adaptation")).ToArray();
            
            foreach (var gene in envGenes)
            {
                if (!gene.value.HasValue) continue;
                
                float influence = gene.value.Value * 0.2f; // Max 20% influence
                
                switch (gene.traitName.ToLower())
                {
                    case var name when name.Contains("desert"):
                        primary = Color.Lerp(primary, new Color(0.9f, 0.7f, 0.4f), influence);
                        break;
                    case var name when name.Contains("arctic"):
                        primary = Color.Lerp(primary, new Color(0.95f, 0.95f, 1f), influence);
                        break;
                    case var name when name.Contains("forest"):
                        primary = Color.Lerp(primary, new Color(0.4f, 0.6f, 0.3f), influence);
                        break;
                    case var name when name.Contains("ocean"):
                        primary = Color.Lerp(primary, new Color(0.2f, 0.5f, 0.8f), influence);
                        break;
                }
            }
        }
        
        private static void ApplyColorsToRenderers(Renderer[] renderers, Color primary, Color secondary)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                
                // Alternate between primary and secondary colors
                Color colorToUse = (i % 2 == 0) ? primary : secondary;
                
                // Create new material instance to avoid affecting other creatures
                var materials = renderers[i].materials;
                for (int m = 0; m < materials.Length; m++)
                {
                    materials[m] = new Material(materials[m]);
                    materials[m].color = colorToUse;
                }
                renderers[i].materials = materials;
            }
        }
        
        private static void ApplyEyeColoring(CreatureInstanceComponent creature, GeneticProfile genetics, Gene eyeColorGene)
        {
            // Find eye renderers
            var eyeRenderers = creature.GetComponentsInChildren<Renderer>()
                .Where(r => r.name.ToLower().Contains("eye")).ToArray();
            
            if (eyeRenderers.Length == 0) return;
            
            Color eyeColor = Color.blue; // Default
            if (!string.IsNullOrEmpty(eyeColorGene.traitName) && eyeColorGene.value.HasValue)
            {
                eyeColor = Color.HSVToRGB(eyeColorGene.value.Value, 0.8f, 0.9f);
            }
            
            foreach (var eyeRenderer in eyeRenderers)
            {
                var material = new Material(eyeRenderer.material);
                material.color = eyeColor;
                
                // Add glow for high intelligence
                var intelligenceGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Intelligence");
                if (!string.IsNullOrEmpty(intelligenceGene.traitName) && intelligenceGene.value.HasValue && intelligenceGene.value.Value > 0.7f)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", eyeColor * 0.3f);
                }
                
                eyeRenderer.material = material;
            }
        }
        
        #endregion
        
        #region Pattern Genetics
        
        private static void ApplyPatternGenetics(CreatureInstanceComponent creature, GeneticProfile genetics, Renderer[] renderers)
        {
            var patternGenes = genetics.Genes.Where(g => 
                g.traitName.Contains("Pattern") || 
                g.traitName.Contains("Stripe") || 
                g.traitName.Contains("Spot") ||
                g.traitName.Contains("Camouflage")).ToArray();
            
            if (patternGenes.Length == 0) return;
            
            foreach (var gene in patternGenes)
            {
                if (!gene.value.HasValue || gene.value.Value < 0.3f) continue;
                
                ApplyPatternEffect(renderers, gene.traitName, gene.value.Value);
            }
        }
        
        private static void ApplyPatternEffect(Renderer[] renderers, string patternType, float intensity)
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    switch (patternType.ToLower())
                    {
                        case var name when name.Contains("stripe"):
                            ApplyStripePattern(materials[i], intensity);
                            break;
                        case var name when name.Contains("spot"):
                            ApplySpotPattern(materials[i], intensity);
                            break;
                        case var name when name.Contains("camouflage"):
                            ApplyCamouflagePattern(materials[i], intensity);
                            break;
                    }
                }
                renderer.materials = materials;
            }
        }
        
        private static void ApplyStripePattern(Material material, float intensity)
        {
            // Create striped appearance by modulating the existing color
            Color baseColor = material.color;
            Color stripeColor = baseColor * 0.7f; // Darker stripes
            
            // This is a simple approximation - in reality you'd use a shader with texture UV manipulation
            material.color = Color.Lerp(baseColor, stripeColor, intensity * 0.3f);
        }
        
        private static void ApplySpotPattern(Material material, float intensity)
        {
            // Spotted pattern effect
            Color baseColor = material.color;
            Color spotColor = baseColor * 1.2f; // Lighter spots
            
            material.color = Color.Lerp(baseColor, spotColor, intensity * 0.2f);
        }
        
        private static void ApplyCamouflagePattern(Material material, float intensity)
        {
            // Mottled camouflage effect
            Color baseColor = material.color;
            Color camoColor = new Color(baseColor.r * 0.8f, baseColor.g * 1.1f, baseColor.b * 0.9f);
            
            material.color = Color.Lerp(baseColor, camoColor, intensity * 0.25f);
        }
        
        #endregion
        
        #region Material Genetics
        
        private static void ApplyMaterialGenetics(CreatureInstanceComponent creature, GeneticProfile genetics, Renderer[] renderers)
        {
            var materialGenes = genetics.Genes.Where(g => 
                g.traitName.Contains("Metallic") || 
                g.traitName.Contains("Hardness") ||
                g.traitName.Contains("Shine") ||
                g.traitName.Contains("Armor")).ToArray();
            
            foreach (var gene in materialGenes)
            {
                if (!gene.value.HasValue) continue;
                
                ApplyMaterialProperty(renderers, gene.traitName, gene.value.Value);
            }
        }
        
        private static void ApplyMaterialProperty(Renderer[] renderers, string propertyType, float value)
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    switch (propertyType.ToLower())
                    {
                        case var name when name.Contains("metallic"):
                            if (materials[i].HasProperty("_Metallic"))
                                materials[i].SetFloat("_Metallic", value);
                            break;
                        case var name when name.Contains("shine"):
                            if (materials[i].HasProperty("_Smoothness"))
                                materials[i].SetFloat("_Smoothness", value);
                            break;
                        case var name when name.Contains("hardness") || name.Contains("armor"):
                            if (materials[i].HasProperty("_Metallic"))
                                materials[i].SetFloat("_Metallic", value * 0.6f);
                            if (materials[i].HasProperty("_Smoothness"))
                                materials[i].SetFloat("_Smoothness", value * 0.4f);
                            break;
                    }
                }
                renderer.materials = materials;
            }
        }
        
        #endregion
        
        #region Special Effects
        
        private static void ApplySpecialEffects(CreatureInstanceComponent creature, GeneticProfile genetics)
        {
            // Find particle systems and lights
            var particleSystems = creature.GetComponentsInChildren<ParticleSystem>();
            var lights = creature.GetComponentsInChildren<Light>();
            
            // Apply magical trait effects
            var magicalGenes = genetics.Genes.Where(g => g.traitType == TraitType.Magical).ToArray();
            
            foreach (var gene in magicalGenes)
            {
                if (!gene.value.HasValue || gene.value.Value < 0.5f) continue;
                
                ApplyMagicalEffect(particleSystems, lights, gene.traitName, gene.value.Value);
            }
            
            // Apply generation effects (higher generation = more refined effects)
            if (genetics.Generation > 3)
            {
                ApplyGenerationEffects(creature, genetics.Generation);
            }
        }
        
        private static void ApplyMagicalEffect(ParticleSystem[] particles, Light[] lights, string effectType, float intensity)
        {
            switch (effectType.ToLower())
            {
                case var name when name.Contains("fire"):
                    ApplyFireEffect(particles, lights, intensity);
                    break;
                case var name when name.Contains("ice"):
                    ApplyIceEffect(particles, lights, intensity);
                    break;
                case var name when name.Contains("lightning"):
                    ApplyLightningEffect(particles, lights, intensity);
                    break;
            }
        }
        
        private static void ApplyFireEffect(ParticleSystem[] particles, Light[] lights, float intensity)
        {
            // Fire particle effects
            if (particles.Length > 0)
            {
                var ps = particles[0];
                var main = ps.main;
                main.startColor = Color.Lerp(Color.red, Color.yellow, Random.value);
                main.startLifetime = intensity * 2f;
                ps.Play();
            }
            
            // Fire light effects
            if (lights.Length > 0)
            {
                var light = lights[0];
                light.color = Color.Lerp(Color.red, Color.orange, Random.value);
                light.intensity = intensity * 2f;
                light.enabled = true;
            }
        }
        
        private static void ApplyIceEffect(ParticleSystem[] particles, Light[] lights, float intensity)
        {
            // Ice effects - blue/white particles and cool lighting
            if (particles.Length > 0)
            {
                var ps = particles[0];
                var main = ps.main;
                main.startColor = Color.Lerp(Color.cyan, Color.white, Random.value);
                main.startLifetime = intensity * 1.5f;
                ps.Play();
            }
            
            if (lights.Length > 0)
            {
                var light = lights[0];
                light.color = Color.cyan;
                light.intensity = intensity * 1.5f;
                light.enabled = true;
            }
        }
        
        private static void ApplyLightningEffect(ParticleSystem[] particles, Light[] lights, float intensity)
        {
            // Electric effects
            if (particles.Length > 0)
            {
                var ps = particles[0];
                var main = ps.main;
                main.startColor = Color.cyan;
                main.startSpeed = intensity * 5f;
                ps.Play();
            }
            
            if (lights.Length > 0)
            {
                var light = lights[0];
                light.color = Color.white;
                light.intensity = intensity * 3f;
                light.enabled = true;
            }
        }
        
        private static void ApplyGenerationEffects(CreatureInstanceComponent creature, int generation)
        {
            // Higher generation creatures get subtle refinement effects
            var renderers = creature.GetComponentsInChildren<Renderer>();
            float refinement = Mathf.Clamp01((generation - 3) / 10f);
            
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i].HasProperty("_Smoothness"))
                    {
                        float currentSmoothness = materials[i].GetFloat("_Smoothness");
                        materials[i].SetFloat("_Smoothness", currentSmoothness + refinement * 0.2f);
                    }
                }
                renderer.materials = materials;
            }
        }
        
        #endregion
        
        #region Mutation Visuals
        
        private static void ApplyMutationVisuals(CreatureInstanceComponent creature, GeneticProfile genetics, Renderer[] renderers)
        {
            if (genetics.Mutations.Count == 0) return;
            
            foreach (var mutation in genetics.Mutations)
            {
                ApplyMutationVisualEffect(renderers, mutation);
            }
            
            UnityEngine.Debug.Log($"🧬 Applied {genetics.Mutations.Count} mutation visual effects to {creature.name}");
        }
        
        private static void ApplyMutationVisualEffect(Renderer[] renderers, Mutation mutation)
        {
            if (renderers.Length == 0) return;
            
            // Pick a random renderer to show the mutation
            var targetRenderer = renderers[Random.Range(0, renderers.Length)];
            
            switch (mutation.mutationType)
            {
                case MutationType.ValueShift:
                    // Color shift mutation
                    ApplyColorShiftMutation(targetRenderer, mutation);
                    break;
                case MutationType.ExpressionChange:
                    // Pattern/texture mutation
                    ApplyExpressionMutation(targetRenderer, mutation);
                    break;
                case MutationType.NewTrait:
                    // Extraordinary mutation - very visible
                    ApplyExtraordinaryMutation(targetRenderer, mutation);
                    break;
            }
        }
        
        private static void ApplyColorShiftMutation(Renderer renderer, Mutation mutation)
        {
            var materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Color currentColor = materials[i].color;
                Color mutationColor = Color.HSVToRGB(Random.value, 0.4f, 0.9f);
                
                float mutationStrength = mutation.severity * (mutation.isHarmful ? 0.3f : 0.1f);
                materials[i].color = Color.Lerp(currentColor, mutationColor, mutationStrength);
            }
            renderer.materials = materials;
        }
        
        private static void ApplyExpressionMutation(Renderer renderer, Mutation mutation)
        {
            // Expression mutations create unique visual anomalies
            var materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (mutation.severity > 0.5f)
                {
                    // High severity mutations get emission glow
                    materials[i].EnableKeyword("_EMISSION");
                    Color emissionColor = Color.HSVToRGB(Random.value, 0.8f, 0.5f);
                    materials[i].SetColor("_EmissionColor", emissionColor);
                }
            }
            renderer.materials = materials;
        }
        
        private static void ApplyExtraordinaryMutation(Renderer renderer, Mutation mutation)
        {
            // Extremely rare mutations get very noticeable effects
            var materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                // Bright, unusual colors
                Color extraordinaryColor = Color.HSVToRGB(Random.value, 1f, 1f);
                materials[i].color = extraordinaryColor;
                
                // Add strong emission
                materials[i].EnableKeyword("_EMISSION");
                materials[i].SetColor("_EmissionColor", extraordinaryColor * 0.5f);
                
                // Max metallic/smoothness for alien appearance
                if (materials[i].HasProperty("_Metallic"))
                    materials[i].SetFloat("_Metallic", 1f);
                if (materials[i].HasProperty("_Smoothness"))
                    materials[i].SetFloat("_Smoothness", 1f);
            }
            renderer.materials = materials;
        }
        
        #endregion
    }
}