using System;
using Laboratory.Chimera.Creatures;

namespace Laboratory.Chimera.Breeding
{
    /// <summary>
    /// Event triggered when breeding is successful
    /// </summary>
    public class BreedingSuccessfulEvent
    {
        public BreedingResult Result { get; }
        public CreatureInstance Parent1 { get; }
        public CreatureInstance Parent2 { get; }
        public CreatureInstance Offspring { get; }
        public float Timestamp { get; }

        public BreedingSuccessfulEvent(BreedingResult result)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            Parent1 = result.Parent1;
            Parent2 = result.Parent2;
            Offspring = result.Offspring;
            Timestamp = UnityEngine.Time.time;
        }
    }

    /// <summary>
    /// Event triggered when breeding fails
    /// </summary>
    public class BreedingFailedEvent
    {
        public CreatureInstance Parent1 { get; }
        public CreatureInstance Parent2 { get; }
        public string Reason { get; }
        public float BreedingChance { get; }
        public float Timestamp { get; }

        public BreedingFailedEvent(CreatureInstance parent1, CreatureInstance parent2, string reason, float breedingChance = 0f)
        {
            Parent1 = parent1 ?? throw new ArgumentNullException(nameof(parent1));
            Parent2 = parent2 ?? throw new ArgumentNullException(nameof(parent2));
            Reason = reason ?? "Unknown failure";
            BreedingChance = breedingChance;
            Timestamp = UnityEngine.Time.time;
        }
    }

    /// <summary>
    /// Event triggered when a new mutation occurs
    /// </summary>
    public class MutationOccurredEvent
    {
        public CreatureInstance Creature { get; }
        public Laboratory.Chimera.Genetics.Mutation Mutation { get; }
        public float Timestamp { get; }

        public MutationOccurredEvent(CreatureInstance creature, Laboratory.Chimera.Genetics.Mutation mutation)
        {
            Creature = creature ?? throw new ArgumentNullException(nameof(creature));
            Mutation = mutation;
            Timestamp = UnityEngine.Time.time;
        }
    }

    /// <summary>
    /// Event triggered when a creature reaches maturation
    /// </summary>
    public class CreatureMaturedEvent
    {
        public CreatureInstance Creature { get; }
        public float Timestamp { get; }

        public CreatureMaturedEvent(CreatureInstance creature)
        {
            Creature = creature ?? throw new ArgumentNullException(nameof(creature));
            Timestamp = UnityEngine.Time.time;
        }
    }

    /// <summary>
    /// Event triggered when environmental conditions change and affect genetics
    /// </summary>
    public class EnvironmentalAdaptationEvent
    {
        public CreatureInstance Creature { get; }
        public Laboratory.Chimera.Genetics.EnvironmentalFactors OldEnvironment { get; }
        public Laboratory.Chimera.Genetics.EnvironmentalFactors NewEnvironment { get; }
        public string[] AffectedTraits { get; }
        public float Timestamp { get; }

        public EnvironmentalAdaptationEvent(CreatureInstance creature, 
            Laboratory.Chimera.Genetics.EnvironmentalFactors oldEnv,
            Laboratory.Chimera.Genetics.EnvironmentalFactors newEnv,
            string[] affectedTraits)
        {
            Creature = creature ?? throw new ArgumentNullException(nameof(creature));
            OldEnvironment = oldEnv;
            NewEnvironment = newEnv;
            AffectedTraits = affectedTraits ?? Array.Empty<string>();
            Timestamp = UnityEngine.Time.time;
        }
    }
}