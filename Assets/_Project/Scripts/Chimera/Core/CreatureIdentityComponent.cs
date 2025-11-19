using Unity.Entities;
using Unity.Collections;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;

namespace Laboratory.Chimera.Core
{
    /// <summary>
    /// UNIFIED creature identity - consolidates scattered identity data
    /// Performance: 128 bytes, cache-friendly
    ///
    /// MOVED FROM Laboratory.Chimera.ECS to break circular dependencies
    /// This is a fundamental component needed by many systems including Consciousness
    /// </summary>
    public struct CreatureIdentityComponent : IComponentData
    {
        public FixedString64Bytes CreatureID;
        public FixedString64Bytes Species;
        public int SpeciesID;
        public FixedString32Bytes CreatureName;
        public uint UniqueID;
        public int Generation;
        public float Age;
        public float AgePercentage; // 0.0-1.0 of lifespan
        public float MaxLifespan;
        public float BirthTime;
        public LifeStage CurrentLifeStage; // Uses the 5-stage LifeStage from Core
        public RarityLevel Rarity;
        public Entity OriginalParent1; // For lineage tracking
        public Entity OriginalParent2;
    }
}
