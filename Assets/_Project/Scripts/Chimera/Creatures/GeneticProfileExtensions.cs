using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Creatures;
using Laboratory.Core.Enums;

namespace Laboratory.Chimera
{
    /// <summary>
    /// Extension methods for GeneticProfile to avoid circular dependencies.
    /// This class bridges the Genetics and Creatures assemblies.
    /// </summary>
    public static class GeneticProfileExtensions
    {
        /// <summary>
        /// Applies genetic modifiers to creature stats
        /// </summary>
        public static CreatureStats ApplyModifiers(this GeneticProfile profile, CreatureStats baseStats)
        {
            if (profile == null || profile.Genes == null)
                return baseStats;

            var modifiedStats = baseStats;

            foreach (var gene in profile.Genes.Where(g => g.isActive))
            {
                if (!gene.value.HasValue) continue;

                float modifier = CalculateGeneModifier(gene);

                switch (gene.traitType)
                {
                    case TraitType.Strength:
                        modifiedStats.attack = Mathf.RoundToInt(modifiedStats.attack * modifier);
                        break;
                    case TraitType.Vitality:
                        modifiedStats.health = Mathf.RoundToInt(modifiedStats.health * modifier);
                        break;
                    case TraitType.Agility:
                        modifiedStats.speed = Mathf.RoundToInt(modifiedStats.speed * modifier);
                        break;
                    case TraitType.Stamina:
                        modifiedStats.defense = Mathf.RoundToInt(modifiedStats.defense * modifier);
                        break;
                    case TraitType.Intelligence:
                        modifiedStats.intelligence = Mathf.RoundToInt(modifiedStats.intelligence * modifier);
                        break;
                    case TraitType.Sociability:
                        modifiedStats.charisma = Mathf.RoundToInt(modifiedStats.charisma * modifier);
                        break;
                }
            }

            return modifiedStats;
        }

        private static float CalculateGeneModifier(Gene gene)
        {
            float baseModifier = 0.5f + (gene.value.Value * 0.5f); // 0.5 to 1.0 range

            // Expression affects the modifier
            switch (gene.expression)
            {
                case GeneExpression.Enhanced:
                    baseModifier *= 1.2f; // 20% boost for enhanced genes
                    break;
                case GeneExpression.Suppressed:
                    baseModifier *= 0.8f; // 20% reduction for suppressed genes
                    break;
                case GeneExpression.Normal:
                    baseModifier *= 1.0f; // No change for normal expression
                    break;
                default:
                    break;
            }

            return baseModifier;
        }
    }
}
