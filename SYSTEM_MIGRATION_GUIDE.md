# System Migration Guide

**Last Updated:** 2024-11-27

This guide clarifies which system implementations are canonical and which are deprecated. Use this as the authoritative reference when choosing which systems to use.

---

## 🎯 Quick Reference

| System Domain | ✅ USE THIS (Canonical) | ❌ DON'T USE (Deprecated) | Location |
|---------------|------------------------|---------------------------|----------|
| **Breeding** | `ChimeraBreedingSystem` (ECS) | `ChimeraBreedingSystem` (old) | `Assets/_Project/Scripts/Chimera/ECS/Systems/ChimeraBreedingSystem.cs` |
| **Breeding (Service Layer)** | `BreedingSystem` | - | `Assets/_Project/Scripts/Chimera/Breeding/BreedingSystem.cs` |
| **Progression** | `PartnershipProgressionSystem` | `ProgressionSystem` | `Assets/_Project/Scripts/Chimera/Progression/Systems/PartnershipProgressionSystem.cs` |
| **Equipment** | Two-layer design (both active) | - | See Equipment section below |

---

## 📋 Detailed Migration Paths

### 1. Breeding System

#### ✅ **CANONICAL: ChimeraBreedingSystem (ECS)**
**File:** `Assets/_Project/Scripts/Chimera/ECS/Systems/ChimeraBreedingSystem.cs`

**Why use this:**
- ✅ **Burst-compiled** - 10-100x performance improvement
- ✅ **Spatial hashing** - O(1) mate finding across 1000+ creatures
- ✅ **Job parallelization** - Multi-threaded processing
- ✅ **Territorial breeding** - Integrates with territory system
- ✅ **Genetics-driven** - Uses `ChimeraGeneticDataComponent` (16 traits)

**Features:**
- Mate selection based on genetic diversity
- Territory requirements for breeding
- Pregnancy and parental care simulation
- Offspring generation with inherited traits
- Burst-compatible configuration data

**When to use:**
- All ECS-based creature breeding
- High-performance scenarios (1000+ creatures)
- Runtime breeding simulation
- Multiplayer breeding sync

---

#### ✅ **ACTIVE: BreedingSystem (Service Layer)**
**File:** `Assets/_Project/Scripts/Chimera/Breeding/BreedingSystem.cs`

**Why this exists alongside ECS:**
- ✅ **Environmental factors** - Calculates food availability, stress, predator pressure
- ✅ **Async support** - UniTask-based for UI workflows
- ✅ **Complex calculations** - Species compatibility, genetic diversity, mutations
- ✅ **Service layer** - Integrates with non-ECS code

**Features:**
- Environmental breeding modifiers (food, stress, predators, population density)
- Genetic similarity calculations
- Mutation system with environmental influence
- Breeding success prediction
- Event bus integration

**When to use:**
- UI-driven breeding (player-initiated)
- Non-ECS code integration
- Complex environmental breeding calculations
- Editor tools and testing

**Relationship:**
- ECS system handles **runtime simulation** (AI-driven breeding)
- Service layer handles **player-initiated breeding** and complex calculations

---

#### ❌ **DEPRECATED: ChimeraBreedingSystem (Old)**
**File:** `Assets/_Project/Scripts/Chimera/Breeding/ChimeraBreedingSystem.cs`

**Status:** `[Obsolete("...", true)]` - **Compiler error if used**

**Why deprecated:**
- ❌ Not Burst-compiled (performance issue)
- ❌ No environmental factors
- ❌ No spatial hashing
- ❌ Dictionary-based (slow for large populations)
- ❌ Basic trait inheritance only

**Migration:**
```csharp
// OLD (deprecated) - DO NOT USE
var oldSystem = World.GetOrCreateSystemManaged<Laboratory.Chimera.Breeding.ChimeraBreedingSystem>();
oldSystem.StartBreeding(parent1, parent2);

// NEW (canonical) - USE THIS
// ECS system runs automatically, no manual calls needed
// Creatures with BreedingComponent will automatically seek mates
```

---

### 2. Progression System

#### ✅ **CANONICAL: PartnershipProgressionSystem**
**File:** `Assets/_Project/Scripts/Chimera/Progression/Systems/PartnershipProgressionSystem.cs`

**New Vision Philosophy:**
- ✅ **NO LEVELS** - No traditional RPG progression for players or chimeras
- ✅ **NO XP** - Skill improves through practice, not arbitrary points
- ✅ **SKILL-FIRST** - Victory through player ability + chimera cooperation
- ✅ **COSMETIC REWARDS** - Milestones unlock appearances, NOT stat boosts
- ✅ **PRACTICE-BASED** - Skills improve by DOING activities

**Features:**
- Tracks skill mastery across 47 game genres
- Records partnership quality (cooperation, trust, understanding)
- Detects skill milestones (cosmetic unlocks)
- Currency rewards for cosmetic purchases
- NO stat boosts or power creep

**When to use:**
- **ALL progression tracking** (this is the only active system)
- Skill improvement from activity completion
- Partnership quality updates
- Cosmetic unlock detection

---

#### ❌ **DEPRECATED: ProgressionSystem**
**File:** `Assets/_Project/Scripts/Chimera/Progression/Systems/ProgressionSystem.cs`

**Status:** `[System.Obsolete("...")]` - Marked deprecated

**Why deprecated:**
- ❌ Level-based progression (removed from game vision)
- ❌ XP system (not aligned with skill-first philosophy)
- ❌ Stat boost unlocks (creates pay-to-win risk)
- ❌ Arbitrary point accumulation (not practice-based)

**Migration:**
```csharp
// OLD (deprecated)
progressionComponent.Experience += 100;
if (progressionComponent.Experience >= nextLevelXP)
{
    progressionComponent.Level++;
}

// NEW (canonical)
// Skills improve through activity completion
partnershipComponent.RacingSkill += activityQuality * improvementRate;
if (partnershipComponent.RacingSkill >= milestoneThreshold)
{
    // Unlock cosmetic reward
    UnlockCosmetic(CosmeticType.RacingOutfit);
}
```

**Data Migration:**
Old save files with levels/XP should be converted:
1. Convert `Level` → Estimated skill mastery based on level
2. Convert `Experience` → Ignore (cosmetic-only currency awarded as compensation)
3. Stat unlocks → Convert to equivalent cosmetic unlocks

---

### 3. Equipment System

#### ✅ **TWO-LAYER DESIGN (Both Active)**

**This is NOT a duplicate - it's an intentional architectural pattern!**

#### Layer 1: Core Equipment System
**File:** `Assets/_Project/Scripts/Core/Equipment/`

**Purpose:** Generic equipment mechanics
- Base stat bonuses
- Durability system
- Crafting recipes
- Equipment slots

**When to use:**
- Implementing equipment base mechanics
- Non-creature equipment (player gear, town items)
- Generic equipment utilities

---

#### Layer 2: Chimera Equipment System
**File:** `Assets/_Project/Scripts/Chimera/Equipment/`

**Purpose:** Personality-driven equipment integration
- **Personality affinity** - Equipment effectiveness based on chimera personality match
- **Mood effects** - Equipment affects chimera mood and happiness
- **Preference system** - Chimeras have gear preferences
- **Bond quality** - Equipment choices affect partnership

**When to use:**
- Chimera-specific equipment
- Personality-based equipment effects
- Partnership progression through gear choices

**Relationship:**
- **Core Equipment** provides the foundation (stats, durability, slots)
- **Chimera Equipment** adds personality layer (affinity, preferences, mood)

```csharp
// Example: Equipping gear to a chimera
var coreEquipment = new Equipment(...); // Core layer
var chimeraEquipment = new ChimeraEquipment(coreEquipment); // Adds personality layer

float effectiveness = chimeraEquipment.CalculateEffectiveness(
    creature.Personality, // Personality match
    creature.Mood        // Current mood affects acceptance
);
```

---

## 🔧 Migration Checklist

### For New Development:

- [ ] **Breeding:** Use `ChimeraBreedingSystem` (ECS) for creature simulation
- [ ] **Breeding (Service):** Use `BreedingSystem` for player-initiated breeding
- [ ] **Progression:** Use `PartnershipProgressionSystem` exclusively
- [ ] **Equipment:** Use both Core (base) + Chimera (personality) as needed

### For Existing Code:

1. **Search for deprecated systems:**
   ```bash
   # Find usage of old breeding system
   grep -r "Laboratory.Chimera.Breeding.ChimeraBreedingSystem" Assets/

   # Find usage of old progression system
   grep -r "ProgressionSystem" Assets/ | grep -v "PartnershipProgressionSystem"
   ```

2. **Replace deprecated calls:**
   - Remove manual `StartBreeding()` calls (ECS system handles automatically)
   - Replace `Experience` accumulation with skill improvement
   - Replace level checks with skill mastery checks

3. **Test migration:**
   - Verify breeding works with ECS system
   - Confirm progression tracks skills, not XP
   - Ensure save/load handles old data gracefully

---

## 📊 Performance Comparison

| System | Old Implementation | Canonical Implementation | Performance Gain |
|--------|-------------------|-------------------------|------------------|
| **Breeding** | Dictionary lookup | Spatial hash + Burst | **10-100x faster** |
| **Progression** | XP calculation | Direct skill tracking | **5-10x faster** |
| **Equipment (Chimera)** | N/A | Two-layer design | **Better separation** |

---

## 🚨 Common Mistakes

### ❌ Mistake 1: Using old breeding system
```csharp
// WRONG
var breedingSystem = World.GetOrCreateSystemManaged<Laboratory.Chimera.Breeding.ChimeraBreedingSystem>();
// This will cause a compiler error due to [Obsolete("...", true)]
```

### ✅ Correct: Let ECS system handle breeding
```csharp
// CORRECT
// Just add BreedingComponent to creatures
EntityManager.AddComponent<BreedingComponent>(creatureEntity);
// ECS system handles breeding automatically
```

---

### ❌ Mistake 2: Giving XP/levels
```csharp
// WRONG - No more XP or levels!
playerProgression.Experience += 100;
playerProgression.Level++;
```

### ✅ Correct: Track skill improvement
```csharp
// CORRECT - Skills improve through practice
partnershipProgression.RacingSkill += CalculateImprovementFromPractice(activityQuality);
partnershipProgression.PartnershipQuality += CalculateCooperationBonus();
```

---

### ❌ Mistake 3: Using Core Equipment for chimeras without personality layer
```csharp
// INCOMPLETE - Missing personality integration
var helmet = new Equipment("Helmet", stats);
creature.Equip(helmet); // No personality check!
```

### ✅ Correct: Use Chimera Equipment with personality
```csharp
// CORRECT - Personality-aware equipment
var helmet = new Equipment("Helmet", stats);
var chimeraHelmet = new ChimeraEquipment(helmet);

if (chimeraHelmet.IsCompatibleWith(creature.Personality))
{
    creature.Equip(chimeraHelmet);
    float effectiveness = chimeraHelmet.CalculateEffectiveness(creature);
}
```

---

## 📝 Migration Support

If you encounter issues during migration:

1. **Check deprecation messages** - They contain migration instructions
2. **Review this guide** - Canonical implementations are documented
3. **Consult SYSTEM_ARCHITECTURE_MAP.md** - Shows dual system rationale
4. **Check commit history** - Recent commits show migration patterns

---

## 🎯 Summary

| System | Status | Migration Action |
|--------|--------|-----------------|
| `ChimeraBreedingSystem` (ECS) | ✅ Canonical | **Use for all ECS breeding** |
| `BreedingSystem` (Service) | ✅ Active | **Use for player-initiated breeding** |
| `ChimeraBreedingSystem` (old) | ❌ Deprecated | **Remove usage (compiler error)** |
| `PartnershipProgressionSystem` | ✅ Canonical | **Use for all progression** |
| `ProgressionSystem` | ❌ Deprecated | **Migrate to skill-based** |
| Core Equipment | ✅ Layer 1 | **Use for base mechanics** |
| Chimera Equipment | ✅ Layer 2 | **Use for personality integration** |

---

**Last Updated:** 2024-11-27
**Maintained By:** Project Chimera Architecture Team
**Related Docs:** SYSTEM_ARCHITECTURE_MAP.md, ARCHITECTURE.md, README.md
