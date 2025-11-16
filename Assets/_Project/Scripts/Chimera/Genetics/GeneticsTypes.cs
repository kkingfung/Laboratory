using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System;

namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// Genetic profile containing all genetic information for a creature
    /// Partial class extension for ECS integration properties
    /// Main definition in GeneticProfile.cs
    /// Note: Gene, Mutation, and Allele structs are defined in GeneticTypes.cs
    /// Note: TraitExpression class is defined in TraitExpression.cs
    /// </summary>
    public partial class GeneticProfile
    {
        /// <summary>Unique identifier for this genetic profile</summary>
        public uint ProfileId { get; set; }

        /// <summary>Species this profile belongs to</summary>
        public int SpeciesId { get; set; }

        /// <summary>Collection of genes (uses Gene struct from GeneticTypes.cs)</summary>
        public Gene[] Genes { get; set; } = new Gene[0];

        /// <summary>Trait expressions derived from genes (uses TraitExpression from TraitExpression.cs)</summary>
        public TraitExpression[] TraitExpressions { get; set; } = new TraitExpression[0];

        /// <summary>Mutations present in this profile (uses Mutation struct from GeneticTypes.cs)</summary>
        public Mutation[] Mutations { get; set; } = new Mutation[0];

        /// <summary>Generation number</summary>
        public int Generation { get; set; }

        /// <summary>Fitness score (0-1)</summary>
        public float Fitness { get; set; }

        /// <summary>Parent profiles (up to 2)</summary>
        public uint[] ParentProfiles { get; set; } = new uint[0];
    }

    /// <summary>
    /// ECS component for genetic data
    /// References the main GeneticProfile class defined in GeneticProfile.cs
    /// Uses Gene, Mutation, and other types from GeneticTypes.cs
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
