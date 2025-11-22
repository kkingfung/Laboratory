using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System;

namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// DEPRECATED - Use ChimeraGeneticDataComponent instead!
    ///
    /// Legacy ECS component for genetic data.
    /// Replaced by Laboratory.Chimera.ECS.ChimeraGeneticDataComponent which provides:
    /// - 16 behavioral traits directly usable by behavior systems
    /// - Physical traits (Size, Speed, Stamina)
    /// - Environmental adaptation (Heat/Cold/Water tolerance)
    /// - Performance optimizations (cached fitness, genetic hash)
    ///
    /// Migration Path:
    /// 1. Replace GeneticComponent with ChimeraGeneticDataComponent
    /// 2. Map GeneticProfileId to behavioral traits using genetic profile lookup
    /// 3. Update all systems querying GeneticComponent to use ChimeraGeneticDataComponent
    ///
    /// See: Laboratory.Chimera.ECS.ChimeraGeneticDataComponent
    /// </summary>
    [System.Obsolete("Use ChimeraGeneticDataComponent from Laboratory.Chimera.ECS instead - provides comprehensive genetic traits")]
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
