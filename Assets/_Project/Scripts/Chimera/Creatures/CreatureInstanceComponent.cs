using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.Creatures
{
    /// <summary>
    /// Component representing an individual creature instance
    /// Contains unique creature data including genetics, stats, and state
    /// </summary>
    public struct CreatureInstanceComponent : IComponentData
    {
        /// <summary>Unique identifier for this creature</summary>
        public uint CreatureId;

        /// <summary>Species identifier</summary>
        public int SpeciesId;

        /// <summary>Current health points</summary>
        public float Health;

        /// <summary>Maximum health points</summary>
        public float MaxHealth;

        /// <summary>Current energy/stamina</summary>
        public float Energy;

        /// <summary>Maximum energy/stamina</summary>
        public float MaxEnergy;

        /// <summary>Creature's current level</summary>
        public int Level;

        /// <summary>Experience points</summary>
        public float Experience;

        /// <summary>Current age in game time</summary>
        public float Age;

        /// <summary>Maximum lifespan</summary>
        public float MaxAge;

        /// <summary>Current mood/happiness</summary>
        public float Mood;

        /// <summary>Hunger level (0 = starving, 1 = full)</summary>
        public float Hunger;

        /// <summary>Is this creature currently alive</summary>
        public bool IsAlive;

        /// <summary>Is this creature owned by a player</summary>
        public bool IsOwned;

        /// <summary>Player ID who owns this creature (0 if wild)</summary>
        public uint OwnerId;

        /// <summary>Creation timestamp</summary>
        public double CreationTime;
    }

    /// <summary>
    /// Component linking creature instance to its genetic profile
    /// </summary>
    public struct CreatureGeneticsComponent : IComponentData
    {
        /// <summary>Reference to genetic profile</summary>
        public uint GeneticProfileId;

        /// <summary>Dominant trait expressions</summary>
        public int DominantTraits;

        /// <summary>Recessive trait expressions</summary>
        public int RecessiveTraits;

        /// <summary>Mutation count</summary>
        public int MutationCount;

        /// <summary>Generation number</summary>
        public int Generation;
    }

    /// <summary>
    /// Component for creature visual appearance
    /// </summary>
    public struct CreatureAppearanceComponent : IComponentData
    {
        /// <summary>Primary color</summary>
        public float4 PrimaryColor;

        /// <summary>Secondary color</summary>
        public float4 SecondaryColor;

        /// <summary>Scale modifier</summary>
        public float Scale;

        /// <summary>Visual variant ID</summary>
        public int VariantId;

        /// <summary>Pattern overlay ID</summary>
        public int PatternId;
    }
}