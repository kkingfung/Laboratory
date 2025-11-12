using UnityEngine;
using Laboratory.Chimera.Genetics;
using System.Linq;

namespace Laboratory.Chimera.Visuals
{
    /// <summary>
    /// Service for applying genetic traits to creature physical structure and special features.
    /// Handles size, proportions, wings, horns, tails, eyes, and tentacles.
    /// Extracted from ProceduralVisualSystem for single responsibility.
    /// </summary>
    public class GeneticTraitApplicationService
    {
        private readonly Transform creatureTransform;
        private readonly Transform[] scalableBodyParts;
        private readonly float visualComplexityMultiplier;

        public GeneticTraitApplicationService(
            Transform creatureTransform,
            Transform[] scalableBodyParts,
            float visualComplexityMultiplier)
        {
            this.creatureTransform = creatureTransform;
            this.scalableBodyParts = scalableBodyParts;
            this.visualComplexityMultiplier = visualComplexityMultiplier;
        }

        /// <summary>
        /// Applies advanced genetic traits including physical structure and special features
        /// </summary>
        public void ApplyAdvancedGeneticTraits(GeneticProfile genetics)
        {
            ApplyPhysicalStructure(genetics);
            ApplySpecialFeatures(genetics);
        }

        private void ApplyPhysicalStructure(GeneticProfile genetics)
        {
            // Enhanced size system with multiple factors
            float baseSize = GetGeneticValue(genetics, "Size", 1.0f);
            float strengthModifier = GetGeneticValue(genetics, "Strength", 0.5f) * 0.2f;
            float constitutionModifier = GetGeneticValue(genetics, "Vitality", 0.5f) * 0.15f;

            float finalSize = (baseSize + strengthModifier + constitutionModifier) * visualComplexityMultiplier;

            // Apply overall scale
            creatureTransform.localScale = Vector3.one * finalSize;

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
            ApplyWingFeatures(genetics);
            ApplyHornFeatures(genetics);
            ApplyTailFeatures(genetics);
            ApplyEyeFeatures(genetics);
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

        // Feature management helpers
        private void EnableFeatureGroup(string featureName, bool enabled)
        {
            Transform featureGroup = creatureTransform.Find(featureName) ?? creatureTransform.Find($"{featureName}Group");
            if (featureGroup != null)
            {
                featureGroup.gameObject.SetActive(enabled);
            }
        }

        private void ScaleFeatureGroup(string featureName, float scale)
        {
            Transform featureGroup = creatureTransform.Find(featureName) ?? creatureTransform.Find($"{featureName}Group");
            if (featureGroup != null)
            {
                featureGroup.localScale = Vector3.one * scale;
            }
        }

        private void SetFeatureVariant(string featureName, string variant)
        {
            Transform variantParent = creatureTransform.Find($"{featureName}Variants");
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
            Transform featureGroup = creatureTransform.Find($"{featureName}Group");
            if (featureGroup != null)
            {
                for (int i = 0; i < featureGroup.childCount; i++)
                {
                    featureGroup.GetChild(i).gameObject.SetActive(i < count);
                }
            }
        }

        private float GetGeneticValue(GeneticProfile genetics, string traitName, float defaultValue)
        {
            var gene = genetics.Genes.FirstOrDefault(g =>
                g.traitName.Equals(traitName, System.StringComparison.OrdinalIgnoreCase) ||
                g.traitName.ToLower().Contains(traitName.ToLower()));

            return (!string.IsNullOrEmpty(gene.traitName) && gene.value.HasValue) ? gene.value.Value : defaultValue;
        }
    }
}
