using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System;

namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// Genetic profile containing all genetic information for a creature
    /// </summary>
    [Serializable]
    public partial class GeneticProfile
    {
        /// <summary>Unique identifier for this genetic profile</summary>
        public uint ProfileId { get; set; }

        /// <summary>Species this profile belongs to</summary>
        public int SpeciesId { get; set; }

        /// <summary>Collection of genes</summary>
        public Gene[] Genes { get; set; } = new Gene[0];

        /// <summary>Trait expressions derived from genes</summary>
        public TraitExpression[] TraitExpressions { get; set; } = new TraitExpression[0];

        /// <summary>Mutations present in this profile</summary>
        public Mutation[] Mutations { get; set; } = new Mutation[0];

        /// <summary>Generation number</summary>
        public int Generation { get; set; }

        /// <summary>Fitness score (0-1)</summary>
        public float Fitness { get; set; }

        /// <summary>Parent profiles (up to 2)</summary>
        public uint[] ParentProfiles { get; set; } = new uint[0];
    }

    /// <summary>
    /// Individual gene with alleles
    /// </summary>
    [Serializable]
    public class Gene
    {
        /// <summary>Gene identifier</summary>
        public int GeneId { get; set; }

        /// <summary>Gene name</summary>
        public string Name { get; set; } = "";

        /// <summary>Dominant allele</summary>
        public Allele DominantAllele { get; set; } = new Allele();

        /// <summary>Recessive allele</summary>
        public Allele RecessiveAllele { get; set; } = new Allele();

        /// <summary>Expression strength (0-1)</summary>
        public float ExpressionStrength { get; set; } = 1f;

        /// <summary>Whether this gene is active</summary>
        public bool IsActive { get; set; } = true;
    }

    // Note: Allele struct is defined in GeneticTypes.cs to avoid duplicate definitions

    /// <summary>
    /// Expressed trait from genetic information
    /// </summary>
    [Serializable]
    public class TraitExpression
    {
        /// <summary>Trait type identifier</summary>
        public int TraitTypeId { get; set; }

        /// <summary>Trait name</summary>
        public string TraitName { get; set; } = "";

        /// <summary>Expressed value</summary>
        public float Value { get; set; }

        /// <summary>Visual representation (color, size modifier, etc.)</summary>
        public float4 VisualData { get; set; }

        /// <summary>Behavioral modifier</summary>
        public float BehaviorModifier { get; set; }

        /// <summary>Statistical impact</summary>
        public float StatModifier { get; set; }
    }

    /// <summary>
    /// Genetic mutation
    /// </summary>
    [Serializable]
    public class Mutation
    {
        /// <summary>Mutation identifier</summary>
        public int MutationId { get; set; }

        /// <summary>Mutation name</summary>
        public string Name { get; set; } = "";

        /// <summary>Target gene ID</summary>
        public int TargetGeneId { get; set; }

        /// <summary>Effect strength</summary>
        public float Strength { get; set; }

        /// <summary>Whether mutation is beneficial</summary>
        public bool IsBeneficial { get; set; }

        /// <summary>Rarity (0 = common, 1 = extremely rare)</summary>
        public float Rarity { get; set; }

        /// <summary>Generation when mutation first appeared</summary>
        public int OriginGeneration { get; set; }
    }

    /// <summary>
    /// ECS component for genetic data
    /// </summary>
    public struct GeneticComponent : IComponentData
    {
        /// <summary>Reference to genetic profile</summary>
        public uint GeneticProfileId;

        /// <summary>Species identifier</summary>
        public int SpeciesId;

        /// <summary>Generation number</summary>
        public int Generation;

        /// <summary>Fitness score</summary>
        public float Fitness;

        /// <summary>Number of beneficial mutations</summary>
        public int BeneficialMutations;

        /// <summary>Number of detrimental mutations</summary>
        public int DetrimentalMutations;
    }
}