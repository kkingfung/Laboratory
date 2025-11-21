using System;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Discovery.Core;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// Shared types and enums for the social and bonding systems
    ///
    /// NOTE: All bonding types have been MOVED to Laboratory.Chimera.Core to break circular dependencies.
    ///
    /// Import Laboratory.Chimera.Core to use:
    /// - BondingEmotionalState, BondingInteractionType, BondingMomentType (enums)
    /// - EmotionalMemoryType, LegacyConnectionType, GenerationalPatternType (enums)
    /// - TriggerConditionType, MemoryTriggerCondition (enum + struct)
    /// - CreatureBondData, PlayerBondingHistory, BondingMoment (components)
    /// - LegacyConnection, EmotionalMemory, PastBondData, GenerationalPattern (components)
    ///
    /// This file is kept for backward compatibility and documentation.
    /// All types are now in Laboratory.Chimera.Core.BondingTypes and BondingComponents.
    /// </summary>
}