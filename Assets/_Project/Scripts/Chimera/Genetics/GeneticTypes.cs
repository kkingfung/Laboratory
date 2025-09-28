using System;
using UnityEngine;

namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// Represents a single gene with dominant/recessive alleles in the Project Chimera genetic system.
    /// Each gene controls a specific trait and can express different phenotypes based on allele combinations.
    /// Supports Mendelian inheritance, X-linked traits, and mutation tracking.
    /// </summary>
    [System.Serializable]
    public struct Gene
    {
        /// <summary>Unique identifier for this gene (e.g., "STR001", "COL_EYES_01")</summary>
        public string geneId;

        /// <summary>Human-readable name of the trait this gene controls (e.g., "Eye Color", "Strength")</summary>
        public string traitName;

        /// <summary>Category of trait this gene represents (Physical, Mental, Magical, etc.)</summary>
        public TraitType traitType;

        /// <summary>Dominant allele - expressed when present with either dominant or recessive</summary>
        public Allele dominantAllele;

        /// <summary>Recessive allele - only expressed when paired with another recessive</summary>
        public Allele recessiveAllele;

        /// <summary>How strongly this gene is expressed (0.0 = hidden, 1.0 = full expression)</summary>
        public float expressionStrength;

        /// <summary>True if this gene is located on the X chromosome (sex-linked inheritance)</summary>
        public bool isXLinked;

        /// <summary>Position of this gene on its chromosome (for linkage calculations)</summary>
        public int chromosomePosition;
        
        // Compatibility properties for GeneticProfile
        public float dominance
        {
            get => dominantAllele.isDominant ? 1.0f : 0.5f;
            set => dominantAllele = new Allele(dominantAllele.value, dominantAllele.numericValue, true, value > 0.6f);
        }
        
        public float? value
        {
            get => GetExpressedAllele().numericValue;
            set 
            {
                if (value.HasValue)
                {
                    var expressed = GetExpressedAllele();
                    if (expressed.isDominant)
                        dominantAllele = new Allele(expressed.value, value.Value, true, true);
                    else
                        recessiveAllele = new Allele(expressed.value, value.Value, true, false);
                }
            }
        }
        
        public GeneExpression expression { get; set; }
        public bool isActive { get; set; }
        public bool isMutation { get; set; }
        public int mutationGeneration { get; set; }
        
        public Gene(string id, string name, TraitType type, Allele dominant, Allele recessive)
        {
            geneId = id;
            traitName = name;
            traitType = type;
            dominantAllele = dominant;
            recessiveAllele = recessive;
            expressionStrength = 1.0f;
            isXLinked = false;
            chromosomePosition = 0;
            expression = GeneExpression.Normal;
            isActive = true;
            isMutation = false;
            mutationGeneration = 0;
        }
        
        public Gene(Gene original)
        {
            geneId = original.geneId;
            traitName = original.traitName;
            traitType = original.traitType;
            dominantAllele = original.dominantAllele;
            recessiveAllele = original.recessiveAllele;
            expressionStrength = original.expressionStrength;
            isXLinked = original.isXLinked;
            chromosomePosition = original.chromosomePosition;
            expression = original.expression;
            isActive = original.isActive;
            isMutation = original.isMutation;
            mutationGeneration = original.mutationGeneration;
        }
        
        /// <summary>
        /// Gets the expressed allele based on dominance
        /// </summary>
        public Allele GetExpressedAllele()
        {
            // If dominant allele is present, it's expressed
            if (dominantAllele.isPresent)
                return dominantAllele;
            
            // Otherwise, recessive allele is expressed
            return recessiveAllele;
        }
        
        /// <summary>
        /// Checks if this gene expresses a specific trait value
        /// </summary>
        public bool ExpressesTrait(string traitValue)
        {
            var expressed = GetExpressedAllele();
            return expressed.value == traitValue;
        }
        
        /// <summary>
        /// Gets the numerical trait value for calculations
        /// </summary>
        public float GetTraitValue()
        {
            var expressed = GetExpressedAllele();
            return expressed.numericValue * expressionStrength;
        }
        
        /// <summary>
        /// Equality operators for Gene comparison
        /// </summary>
        public static bool operator ==(Gene left, Gene right)
        {
            return left.geneId == right.geneId && left.traitName == right.traitName;
        }
        
        public static bool operator !=(Gene left, Gene right)
        {
            return !(left == right);
        }
        
        public override bool Equals(object obj)
        {
            return obj is Gene other && this == other;
        }
        
        public override int GetHashCode()
        {
            return (geneId?.GetHashCode() ?? 0) ^ (traitName?.GetHashCode() ?? 0);
        }
    }
    
    /// <summary>
    /// Represents an allele (variant of a gene)
    /// </summary>
    [Serializable]
    public struct Allele
    {
        public string value;          // String representation (e.g., "Red", "Large")
        public float numericValue;    // Numeric value for calculations
        public bool isPresent;        // Whether this allele is present
        public bool isDominant;       // Whether this is the dominant allele
        public float rarityFactor;    // How rare this allele is (affects mutation)
        
        public Allele(string val, float numVal, bool present = true, bool dominant = false)
        {
            value = val;
            numericValue = numVal;
            isPresent = present;
            isDominant = dominant;
            rarityFactor = 1.0f;
        }
        
        public static Allele CreateDominant(string value, float numericValue)
        {
            return new Allele(value, numericValue, true, true);
        }
        
        public static Allele CreateRecessive(string value, float numericValue)
        {
            return new Allele(value, numericValue, true, false);
        }
    }
    
    /// <summary>
    /// Represents a genetic mutation
    /// </summary>
    [System.Serializable]
    public struct Mutation
    {
        public string mutationId;
        public string affectedGeneId;
        public MutationType type;
        public float effectStrength;
        public string description;
        public bool isBeneficial;
        public float stabilityFactor; // How likely the mutation is to persist
        public int generationOccurred;
        
        // Compatibility properties for GeneticProfile
        public MutationType mutationType
        {
            get => type;
            set => type = value;
        }
        
        public string affectedTrait
        {
            get => description ?? affectedGeneId;
            set => description = value;
        }
        
        public float severity
        {
            get => effectStrength;
            set => effectStrength = value;
        }
        
        public int generation
        {
            get => generationOccurred;
            set => generationOccurred = value;
        }
        
        public bool isHarmful
        {
            get => !isBeneficial;
            set => isBeneficial = !value;
        }
        
        public Mutation(string id, string geneId, MutationType mutationType, float strength)
        {
            mutationId = id;
            affectedGeneId = geneId;
            type = mutationType;
            effectStrength = strength;
            description = "";
            isBeneficial = strength > 0;
            stabilityFactor = 0.8f;
            generationOccurred = 1;
        }
    }
    
    /// <summary>
    /// Types of genetic mutations  
    /// </summary>
    public enum MutationType
    {
        Point,          // Single allele change
        Duplication,    // Gene duplication
        Deletion,       // Gene deletion
        Inversion,      // Gene sequence reversal
        Translocation,  // Gene moves to different chromosome
        Enhancement,    // Trait enhancement
        Suppression,    // Trait suppression
        Novel,          // Completely new trait
        Chimeric,       // Fusion of multiple traits
        
        // Additional types for compatibility with GeneticProfile
        ValueShift,     // Change in gene value
        DominanceChange, // Change in dominance level
        ExpressionChange, // Change in gene expression
        NewTrait        // Creation of entirely new trait
    }
    
    /// <summary>
    /// How a gene is expressed
    /// </summary>
    public enum GeneExpression
    {
        Suppressed, // Gene is present but not fully expressed
        Normal,     // Standard expression level
        Enhanced    // Gene is over-expressed
    }
    
    /// <summary>
    /// Defines a specific genetic trait that can be inherited
    /// </summary>
    [Serializable]
    public class TraitDefinition
    {
        [Header("Basic Information")]
        public string traitId;
        public string displayName;
        public TraitType category;
        [TextArea(2, 4)]
        public string description;
        
        [Header("Inheritance")]
        public InheritancePattern inheritance = InheritancePattern.Simple;
        public float dominanceStrength = 1.0f;
        public bool canMutate = true;
        public float mutationRate = 0.02f;
        
        [Header("Possible Values")]
        public AlleleOption[] possibleAlleles = Array.Empty<AlleleOption>();
        
        [Header("Expression")]
        public bool affectsStats = false;
        public StatModifier[] statModifiers = Array.Empty<StatModifier>();
        
        [Header("Compatibility")]
        public string[] incompatibleTraits = Array.Empty<string>();
        public string[] prerequisiteTraits = Array.Empty<string>();
        
        /// <summary>
        /// Creates a random gene for this trait
        /// </summary>
        public Gene CreateRandomGene()
        {
            if (possibleAlleles.Length < 2) 
            {
                UnityEngine.Debug.LogWarning($"Trait {traitId} needs at least 2 possible alleles");
                return default;
            }
            
            var dominant = possibleAlleles[UnityEngine.Random.Range(0, possibleAlleles.Length)];
            var recessive = possibleAlleles[UnityEngine.Random.Range(0, possibleAlleles.Length)];
            
            return new Gene(
                traitId,
                displayName,
                category,
                new Allele(dominant.name, dominant.value, true, true),
                new Allele(recessive.name, recessive.value, true, false)
            );
        }
    }
    
    /// <summary>
    /// Represents a possible allele value for a trait
    /// </summary>
    [Serializable]
    public struct AlleleOption
    {
        public string name;
        public float value;
        public float rarity;
        public Color displayColor;
        
        public AlleleOption(string n, float v, float r = 1.0f)
        {
            name = n;
            value = v;
            rarity = r;
            displayColor = Color.white;
        }
    }
    
    /// <summary>
    /// How a trait is inherited
    /// </summary>
    public enum InheritancePattern
    {
        Simple,         // Basic dominant/recessive
        Codominant,     // Both alleles express
        Incomplete,     // Blended expression
        Polygenic,      // Multiple genes affect trait
        SexLinked,      // Located on X chromosome
        Maternal,       // Only from mother
        Paternal,       // Only from father
        Environmental   // Influenced by environment
    }
    
    /// <summary>
    /// How a trait modifies creature stats
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        public string statName;
        public float multiplier;
        public float additive;
        public bool isPercentage;
        
        public StatModifier(string stat, float mult, float add = 0f)
        {
            statName = stat;
            multiplier = mult;
            additive = add;
            isPercentage = false;
        }
    }
}
