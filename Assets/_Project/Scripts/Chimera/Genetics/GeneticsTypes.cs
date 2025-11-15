using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System;

namespace Laboratory.Chimera.Genetics
{
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