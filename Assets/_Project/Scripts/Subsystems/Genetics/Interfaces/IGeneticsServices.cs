using System.Collections.Generic;
using System.Threading.Tasks;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Subsystems.Genetics
{
    /// <summary>
    /// Core breeding service interface for Project Chimera genetics
    /// </summary>
    public interface IBreedingService
    {
        /// <summary>
        /// Breeds two genetic profiles to create offspring
        /// </summary>
        Task<GeneticBreedingResult> BreedAsync(
            GeneticProfile parent1,
            GeneticProfile parent2,
            EnvironmentalFactors environment = null);

        /// <summary>
        /// Calculates compatibility between two genetic profiles
        /// </summary>
        float CalculateCompatibility(GeneticProfile parent1, GeneticProfile parent2);

        /// <summary>
        /// Predicts breeding outcomes without actually breeding
        /// </summary>
        BreedingPrediction PredictOutcome(
            GeneticProfile parent1,
            GeneticProfile parent2,
            EnvironmentalFactors environment = null);

        /// <summary>
        /// Event fired when breeding completes
        /// </summary>
        event System.Action<GeneticBreedingResult> OnBreedingComplete;
    }

    /// <summary>
    /// Mutation processing service interface
    /// </summary>
    public interface IMutationService
    {
        /// <summary>
        /// Applies a mutation to a genetic profile
        /// </summary>
        Task<bool> ApplyMutationAsync(
            GeneticProfile profile,
            MutationType mutationType,
            string targetTrait = null,
            float severity = 0.1f);

        /// <summary>
        /// Calculates mutation probability for a profile
        /// </summary>
        float CalculateMutationProbability(
            GeneticProfile profile,
            EnvironmentalFactors environment = null);

        /// <summary>
        /// Gets possible mutations for a genetic profile
        /// </summary>
        List<PossibleMutation> GetPossibleMutations(GeneticProfile profile);

        /// <summary>
        /// Event fired when a mutation occurs
        /// </summary>
        event System.Action<MutationEvent> OnMutationOccurred;
    }

    /// <summary>
    /// Trait calculation and expression service interface
    /// </summary>
    public interface ITraitService
    {
        /// <summary>
        /// Calculates final trait values from genetic profile
        /// </summary>
        TraitExpressionResult CalculateTraitExpression(
            GeneticProfile profile,
            EnvironmentalFactors environment = null);

        /// <summary>
        /// Gets trait modifiers for creature stats
        /// </summary>
        StatModifier[] GetStatModifiers(GeneticProfile profile);

        /// <summary>
        /// Discovers new trait combinations
        /// </summary>
        TraitDiscovery AnalyzeTraitCombinations(GeneticProfile profile);

        /// <summary>
        /// Event fired when a new trait is discovered
        /// </summary>
        event System.Action<TraitDiscoveryEvent> OnTraitDiscovered;
    }

    /// <summary>
    /// Genetic database service interface
    /// </summary>
    public interface IGeneticDatabase
    {
        /// <summary>
        /// Stores a genetic profile
        /// </summary>
        Task<bool> StoreProfileAsync(string id, GeneticProfile profile);

        /// <summary>
        /// Retrieves a genetic profile
        /// </summary>
        Task<GeneticProfile> GetProfileAsync(string id);

        /// <summary>
        /// Searches profiles by traits
        /// </summary>
        Task<List<string>> SearchProfilesByTraitsAsync(List<string> traitNames);

        /// <summary>
        /// Gets breeding history for a profile
        /// </summary>
        Task<BreedingHistory> GetBreedingHistoryAsync(string profileId);

        /// <summary>
        /// Records a breeding event
        /// </summary>
        Task RecordBreedingAsync(GeneticBreedingResult result);
    }

    #region Data Transfer Objects

    /// <summary>
    /// Result of a breeding operation
    /// </summary>
    public class GeneticBreedingResult
    {
        public string parent1Id;
        public string parent2Id;
        public string offspringId;
        public GeneticProfile offspring;
        public float compatibility;
        public List<string> inheritedTraits;
        public List<Mutation> mutations;
        public bool isSuccessful;
        public string errorMessage;
        public EnvironmentalFactors environment;
        public System.DateTime breedingTime;
    }

    /// <summary>
    /// Prediction of breeding outcomes
    /// </summary>
    public class BreedingPrediction
    {
        public float successProbability;
        public float compatibility;
        public List<TraitPrediction> predictedTraits;
        public List<MutationPrediction> possibleMutations;
        public float[] statRanges; // Min/max for each stat
        public string[] warnings;
    }

    /// <summary>
    /// Prediction for a specific trait
    /// </summary>
    public class TraitPrediction
    {
        public string traitName;
        public float probability;
        public float minValue;
        public float maxValue;
        public float averageValue;
        public bool isNovelTrait;
    }

    /// <summary>
    /// Prediction for possible mutations
    /// </summary>
    public class MutationPrediction
    {
        public MutationType mutationType;
        public string affectedTrait;
        public float probability;
        public float averageSeverity;
        public bool isBeneficial;
    }

    /// <summary>
    /// Possible mutation that could occur
    /// </summary>
    public class PossibleMutation
    {
        public MutationType mutationType;
        public string targetTrait;
        public float probability;
        public float expectedSeverity;
        public bool isBeneficial;
        public string description;
    }

    /// <summary>
    /// Event data for mutation occurrences
    /// </summary>
    public class MutationEvent
    {
        public string creatureId;
        public GeneticProfile profile;
        public Mutation mutation;
        public EnvironmentalFactors environment;
        public System.DateTime timestamp;
    }

    /// <summary>
    /// Result of trait expression calculations
    /// </summary>
    public class TraitExpressionResult
    {
        public Dictionary<string, float> expressedTraits;
        public Dictionary<string, TraitExpression> traitExpressions;
        public float overallFitness;
        public List<string> dominantTraits;
        public List<string> recessiveTraits;
        public List<string> novelTraits;
    }

    /// <summary>
    /// Discovery of new trait combinations
    /// </summary>
    public class TraitDiscovery
    {
        public List<string> newCombinations;
        public List<string> rareTraits;
        public float discoveryScore;
        public bool isWorldFirst;
    }

    /// <summary>
    /// Event data for trait discoveries
    /// </summary>
    public class TraitDiscoveryEvent
    {
        public string creatureId;
        public string traitName;
        public TraitType traitType;
        public float expressionValue;
        public int generation;
        public bool isWorldFirst;
        public string discoverer;
        public System.DateTime timestamp;
    }

    /// <summary>
    /// Breeding history for a genetic profile
    /// </summary>
    public class BreedingHistory
    {
        public string profileId;
        public List<string> parentIds;
        public List<string> offspringIds;
        public List<BreedingRecord> breedingRecords;
        public int generation;
        public float averageCompatibility;
    }

    /// <summary>
    /// Record of a single breeding event
    /// </summary>
    public class BreedingRecord
    {
        public string partnerId;
        public string offspringId;
        public float compatibility;
        public System.DateTime timestamp;
        public List<string> inheritedTraits;
        public bool wasSuccessful;
    }

    /// <summary>
    /// Validation event data
    /// </summary>
    public class GeneticValidationEvent
    {
        public string creatureId;
        public GeneticProfile profile;
        public string[] issues;
        public bool isValid;
        public System.DateTime timestamp;
    }

    /// <summary>
    /// Expression of a specific trait
    /// </summary>
    public class TraitExpression
    {
        public string traitName;
        public float value;
        public TraitType type;
        public float dominanceStrength;
        public GeneExpression expression;
        public bool isActive;

        public TraitExpression(string name, float val, TraitType traitType)
        {
            traitName = name;
            value = val;
            type = traitType;
            dominanceStrength = 1.0f;
            expression = GeneExpression.Normal;
            isActive = true;
        }
    }

    #endregion
}