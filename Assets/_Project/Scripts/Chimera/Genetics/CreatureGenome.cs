using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// Complete genetic information for a creature
    /// Contains all chromosomes and genetic data
    /// </summary>
    [Serializable]
    public class CreatureGenome
    {
        /// <summary>Unique genome identifier</summary>
        public string GenomeId { get; set; } = System.Guid.NewGuid().ToString();

        /// <summary>Species this genome belongs to</summary>
        public int SpeciesId { get; set; }

        /// <summary>Genetic profile containing processed traits</summary>
        public GeneticProfile GeneticProfile { get; set; }

        /// <summary>Raw chromosomal data</summary>
        public Chromosome[] Chromosomes { get; set; } = new Chromosome[0];

        /// <summary>Active mutations in this genome</summary>
        public List<Mutation> Mutations { get; set; } = new List<Mutation>();

        /// <summary>Generation number</summary>
        public int Generation { get; set; } = 1;

        /// <summary>Fitness score (0-1)</summary>
        public float Fitness { get; set; } = 0.5f;

        /// <summary>Parent genome IDs</summary>
        public string[] ParentGenomes { get; set; } = new string[0];

        /// <summary>Genetic diversity index</summary>
        public float DiversityIndex { get; set; } = 0.5f;

        /// <summary>Whether this genome is viable</summary>
        public bool IsViable { get; set; } = true;

        /// <summary>
        /// Calculate genetic compatibility with another genome
        /// </summary>
        public float CalculateCompatibility(CreatureGenome other)
        {
            if (other == null || SpeciesId != other.SpeciesId)
                return 0f;

            // Simple compatibility based on diversity and fitness
            float diversityScore = Mathf.Abs(DiversityIndex - other.DiversityIndex);
            float fitnessScore = (Fitness + other.Fitness) / 2f;

            return Mathf.Clamp01(fitnessScore - diversityScore * 0.5f);
        }

        /// <summary>
        /// Get all expressed traits from this genome
        /// </summary>
        public TraitExpression[] GetExpressedTraits()
        {
            if (GeneticProfile == null)
                return new TraitExpression[0];

            var traitDict = GeneticProfile.TraitExpressions;
            if (traitDict == null)
                return new TraitExpression[0];

            var traits = new TraitExpression[traitDict.Count];
            int index = 0;
            foreach (var kvp in traitDict)
            {
                traits[index++] = kvp.Value;
            }
            return traits;
        }

        /// <summary>
        /// Check if genome has a specific mutation
        /// </summary>
        public bool HasMutation(string mutationId)
        {
            return Mutations.Exists(m => m.mutationId == mutationId);
        }
    }

    /// <summary>
    /// Individual chromosome containing genetic data
    /// </summary>
    [Serializable]
    public class Chromosome
    {
        /// <summary>Chromosome identifier</summary>
        public int ChromosomeId { get; set; }

        /// <summary>Genes on this chromosome</summary>
        public Gene[] Genes { get; set; } = new Gene[0];

        /// <summary>Length of chromosome in base pairs</summary>
        public int Length { get; set; }

        /// <summary>Whether this is a sex chromosome</summary>
        public bool IsSexChromosome { get; set; } = false;
    }

    /// <summary>
    /// Types of social interactions for personality system
    /// </summary>
    public enum SocialInteractionType : byte
    {
        /// <summary>Neutral interaction</summary>
        Neutral = 0,
        /// <summary>Friendly greeting</summary>
        Friendly = 1,
        /// <summary>Aggressive confrontation</summary>
        Aggressive = 2,
        /// <summary>Playful interaction</summary>
        Playful = 3,
        /// <summary>Territorial display</summary>
        Territorial = 4,
        /// <summary>Mating behavior</summary>
        Mating = 5,
        /// <summary>Parental behavior</summary>
        Parental = 6,
        /// <summary>Submissive behavior</summary>
        Submissive = 7,
        /// <summary>Dominant behavior</summary>
        Dominant = 8,
        /// <summary>Curious investigation</summary>
        Curious = 9,
        /// <summary>Fearful avoidance</summary>
        Fearful = 10,
        /// <summary>Cooperative behavior</summary>
        Cooperative = 11,
        /// <summary>Competitive behavior</summary>
        Competitive = 12
    }
}