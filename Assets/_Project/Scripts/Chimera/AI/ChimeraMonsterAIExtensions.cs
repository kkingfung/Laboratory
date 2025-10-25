using UnityEngine;
using System.Linq;
using Laboratory.Chimera;
using Laboratory.Chimera.Breeding;

namespace Laboratory.Chimera.AI
{
    /// <summary>
    /// Extension methods and additional functionality for ChimeraMonsterAI
    /// to support advanced genetic behavior systems.
    /// </summary>
    public static class ChimeraMonsterAIExtensions
    {
        /// <summary>
        /// Get the creature's dominant genetic trait based on highest value
        /// </summary>
        public static string GetDominantGeneticTrait(this ChimeraMonsterAI ai)
        {
            var creatureComponent = ai.GetComponent<CreatureInstanceComponent>();
            var creature = creatureComponent?.Instance;
            if (creature?.GeneticProfile?.Genes == null) return "Unknown";

            var dominantGene = creature.GeneticProfile.Genes
                .Where(g => g.isActive)
                .OrderByDescending(g => g.value)
                .FirstOrDefault();

            return string.IsNullOrEmpty(dominantGene.traitName) ? "Balanced" : dominantGene.traitName;
        }

        /// <summary>
        /// Get genetic trait value by name
        /// </summary>
        public static float GetGeneticTraitValue(this ChimeraMonsterAI ai, string traitName)
        {
            var creatureComponent = ai.GetComponent<CreatureInstanceComponent>();
            var creature = creatureComponent?.Instance;
            if (creature?.GeneticProfile?.Genes == null) return 0.5f;

            var gene = creature.GeneticProfile.Genes
                .FirstOrDefault(g => g.traitName == traitName && g.isActive);

            return gene.value ?? 0.5f;
        }

        /// <summary>
        /// Check if creature has high value for specific trait
        /// </summary>
        public static bool HasHighTrait(this ChimeraMonsterAI ai, string traitName, float threshold = 0.7f)
        {
            return ai.GetGeneticTraitValue(traitName) >= threshold;
        }

        /// <summary>
        /// Get creature's behavioral archetype based on genetic combination
        /// </summary>
        public static string GetBehavioralArchetype(this ChimeraMonsterAI ai)
        {
            var aggression = ai.GetGeneticTraitValue("Aggression");
            var loyalty = ai.GetGeneticTraitValue("Loyalty");
            var social = ai.GetGeneticTraitValue("Social");
            var intelligence = ai.GetGeneticTraitValue("Intelligence");
            var curiosity = ai.GetGeneticTraitValue("Curiosity");
            var territoriality = ai.GetGeneticTraitValue("Territoriality");

            // Complex archetype determination
            if (aggression > 0.8f && territoriality > 0.7f)
                return "🐅 Apex Predator";
            else if (loyalty > 0.8f && social > 0.6f)
                return "🐕 Loyal Companion";
            else if (intelligence > 0.8f && curiosity > 0.7f)
                return "🦊 Clever Trickster";
            else if (social > 0.8f && aggression < 0.3f)
                return "🐑 Peaceful Herd Animal";
            else if (territoriality > 0.8f && aggression > 0.5f)
                return "🐻 Territorial Guardian";
            else if (curiosity > 0.8f && intelligence > 0.6f)
                return "🐱 Curious Explorer";
            else if (aggression < 0.2f && loyalty > 0.6f)
                return "🐰 Gentle Follower";
            else if (intelligence > 0.7f && loyalty > 0.7f)
                return "🐺 Wise Pack Leader";
            else
                return "🦝 Adaptable Survivor";
        }

        /// <summary>
        /// Get comprehensive genetic behavior summary
        /// </summary>
        public static string GetGeneticBehaviorSummary(this ChimeraMonsterAI ai)
        {
            var archetype = ai.GetBehavioralArchetype();
            var dominantTrait = ai.GetDominantGeneticTrait();
            
            var aggression = ai.GetGeneticTraitValue("Aggression");
            var intelligence = ai.GetGeneticTraitValue("Intelligence");
            var social = ai.GetGeneticTraitValue("Social");

            return $"{archetype}\n" +
                   $"Dominant: {dominantTrait}\n" +
                   $"Aggression: {aggression:P0}\n" +
                   $"Intelligence: {intelligence:P0}\n" +
                   $"Social: {social:P0}";
        }
    }
}
