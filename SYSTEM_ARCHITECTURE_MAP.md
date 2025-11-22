# Project Chimera - System Architecture Map
**Purpose:** Document dual system implementations and clarify canonical paths
**Status:** Architecture consolidation in progress

---

## 🔴 CRITICAL: Dual System Implementations

### **BREEDING SYSTEMS** - ⚠️ THREE IMPLEMENTATIONS

#### 1. **BreedingSystem** (Service Layer)
- **Path:** `Assets/_Project/Scripts/Chimera/Breeding/BreedingSystem.cs`
- **Type:** MonoBehaviour/Service (implements IBreedingSystem)
- **Namespace:** `Laboratory.Chimera.Breeding`
- **Features:**
  - Environmental factor calculations
  - Async breeding with UniTask
  - Event bus integration
  - Food availability, stress, predator pressure
- **Status:** ✅ ACTIVE - Legacy service layer
- **Purpose:** High-level breeding logic with environmental simulation

#### 2. **ChimeraBreedingSystem** (Basic ECS)
- **Path:** `Assets/_Project/Scripts/Chimera/Breeding/ChimeraBreedingSystem.cs`
- **Type:** SystemBase (ECS)
- **Namespace:** `Laboratory.Chimera.Breeding`
- **Features:**
  - Dictionary-based breeding request tracking
  - Basic genetic combinations
  - Trait inheritance
- **Status:** ⚠️ UNCLEAR - May be deprecated or intermediate
- **Purpose:** ECS wrapper for breeding logic

#### 3. **ChimeraBreedingSystem** (Advanced ECS)
- **Path:** `Assets/_Project/Scripts/Chimera/ECS/Systems/ChimeraBreedingSystem.cs`
- **Type:** SystemBase (ECS) with Burst compilation
- **Namespace:** `Laboratory.Core.ECS.Systems`
- **Features:**
  - ✅ Burst-compiled for 10-100x performance
  - Spatial hash for mate finding
  - Territorial breeding requirements
  - Job parallelization
  - **Integrates with legacy BreedingSystem** via `_legacyBreedingSystem` field
- **Status:** ✅ CANONICAL - Production ECS implementation
- **Purpose:** High-performance breeding with legacy compatibility

**RECOMMENDATION:**
```
CANONICAL: Laboratory.Core.ECS.Systems.ChimeraBreedingSystem
LEGACY: Laboratory.Chimera.Breeding.BreedingSystem (bridged via canonical)
DEPRECATED: Laboratory.Chimera.Breeding.ChimeraBreedingSystem (remove or clarify)
```

---

### **PROGRESSION SYSTEMS** - ⚠️ TWO IMPLEMENTATIONS

#### 1. **ProgressionSystem** (Old Level-Based)
- **Path:** `Assets/_Project/Scripts/Chimera/Progression/Systems/ProgressionSystem.cs`
- **Type:** SystemBase (ECS)
- **Namespace:** `Laboratory.Chimera.Progression`
- **Features:**
  - XP and level tracking
  - Stat bonuses from levels
  - Skill points and unlocks
- **Status:** ⚠️ DEPRECATED - Marked with `[System.Obsolete]`
- **Purpose:** Backward compatibility during migration
- **Components:** `MonsterLevelComponent`, `LevelStatBonusComponent`, `AwardExperienceRequest`, `LevelUpEvent`

#### 2. **PartnershipProgressionSystem** (New Skill-Based)
- **Path:** `Assets/_Project/Scripts/Chimera/Progression/Systems/PartnershipProgressionSystem.cs`
- **Type:** SystemBase (ECS)
- **Namespace:** `Laboratory.Chimera.Progression`
- **Features:**
  - NO LEVELS - Skill-based progression
  - Practice-based improvement (no XP)
  - Partnership quality tracking
  - Cosmetic-only rewards
  - 7 genre categories (Action, Strategy, Puzzle, Racing, Rhythm, Exploration, Economics)
- **Status:** ✅ CANONICAL - New vision implementation
- **Components:** `PartnershipSkillComponent`, `RecordSkillImprovementRequest`, `SkillMilestoneReachedEvent`

**RECOMMENDATION:**
```
CANONICAL: PartnershipProgressionSystem
DEPRECATED: ProgressionSystem (keep for save migration only)

ACTION REQUIRED:
1. Add migration script to convert MonsterLevelComponent → PartnershipSkillComponent
2. Update all UI to show skill mastery instead of levels
3. Mark ProgressionSystem with [DisableAutoCreation] after migration complete
```

---

### **EQUIPMENT SYSTEMS** - ⚠️ TWO IMPLEMENTATIONS

#### 1. **Equipment (Chimera)**
- **Paths:**
  - `Assets/_Project/Scripts/Chimera/Equipment/Core/EquipmentComponents.cs`
  - `Assets/_Project/Scripts/Chimera/Equipment/Systems/EquipmentSystem.cs`
  - `Assets/_Project/Scripts/Chimera/Equipment/Systems/PersonalityEquipmentSystem.cs`
- **Namespace:** `Laboratory.Chimera.Equipment`
- **Features:**
  - Personality-based equipment affinity
  - Equipment fits chimera personality
  - Personality changes from equipment
  - `PersonalityEquipmentEffect`, `EquipmentPersonalityProfile`
- **Status:** ✅ ACTIVE - Personality integration

#### 2. **Equipment (Core)**
- **Paths:**
  - `Assets/_Project/Scripts/Core/Equipment/EquipmentECSSystem.cs`
  - `Assets/_Project/Scripts/Core/Equipment/Systems/EquipmentSystem.cs`
  - `Assets/_Project/Scripts/Core/Equipment/Systems/EquipmentCraftingSystem.cs`
  - `Assets/_Project/Scripts/Core/Equipment/Systems/ActivityEquipmentBonusSystem.cs`
- **Namespace:** `Laboratory.Core.Equipment`
- **Features:**
  - Equipment management
  - Crafting system
  - Activity bonuses
  - Durability tracking
- **Status:** ✅ ACTIVE - Core mechanics

**RECOMMENDATION:**
```
BOTH ARE ACTIVE - Different concerns
Chimera/Equipment: Personality integration (chimera-specific)
Core/Equipment: Generic equipment mechanics (reusable)

ACTION REQUIRED:
1. Document clear separation: Core = mechanics, Chimera = personality
2. Ensure PersonalityEquipmentSystem depends on Core/EquipmentSystem
3. Unify component definitions where possible
```

---

### **ACTIVITY SYSTEMS** - ⚠️ TWO IMPLEMENTATIONS

#### 1. **Activities (Chimera)**
- **Paths:**
  - `Assets/_Project/Scripts/Chimera/Activities/Core/ActivitySystem.cs`
  - `Assets/_Project/Scripts/Chimera/Activities/Core/PartnershipActivitySystem.cs`
- **Namespace:** `Laboratory.Chimera.Activities`
- **Features:**
  - Partnership-enhanced activities
  - Cooperation mechanics
  - Genetics influence on performance
  - `PartnershipActivityComponent`
- **Status:** ✅ ACTIVE - Partnership integration

#### 2. **Activities (Core)**
- **Paths:**
  - `Assets/_Project/Scripts/Core/Activities/ActivityCenterSystem.cs`
  - `Assets/_Project/Scripts/Core/Activities/*/` (9+ genre-specific systems)
- **Namespace:** `Laboratory.Core.Activities`
- **Features:**
  - Genre-specific implementations
  - Activity center management
  - Standalone activity mechanics
- **Status:** ✅ ACTIVE - Generic activities

**RECOMMENDATION:**
```
BOTH ARE ACTIVE - Different contexts
Chimera/Activities: Partnership mechanics with chimeras
Core/Activities: Generic activity mini-games

ACTION REQUIRED:
1. Clarify when to use each (solo player = Core, with chimera = Chimera)
2. Ensure ActivityType enum is unified (currently only in Chimera)
3. Document integration pattern
```

---

### **IDENTITY COMPONENTS** - ⚠️ TWO DEFINITIONS

#### 1. **CreatureIdentityComponent** (Chimera)
- **Path:** `Assets/_Project/Scripts/Chimera/Core/CreatureIdentityComponent.cs`
- **Namespace:** `Laboratory.Chimera.Core`
- **Fields:**
  - CreatureID, Species, SpeciesID, CreatureName
  - UniqueID, Generation, Age, AgePercentage
  - MaxLifespan, BirthTime, CurrentLifeStage
  - Rarity, OriginalParent1, OriginalParent2
- **Status:** ✅ CANONICAL - Chimera identity
- **Size:** 128 bytes

#### 2. **CreatureIdentityComponent** (Core)
- **Path:** `Assets/_Project/Scripts/Core/CoreCreatureComponents.cs`
- **Namespace:** `Laboratory.Core.ECS.Components`
- **Fields:**
  - SpeciesID, CreatureID, NameHash
  - Age, Generation, IsAlive
  - IsTamed, IsWild, OwnerPlayer
- **Status:** ⚠️ UNCLEAR - May be legacy or generic
- **Size:** ~40 bytes

**RESOLUTION APPLIED:**
```csharp
// PopulationManagementSystem uses type alias to avoid ambiguity
using ChimeraIdentity = Laboratory.Chimera.Core.CreatureIdentityComponent;
```

**RECOMMENDATION:**
```
CANONICAL: Laboratory.Chimera.Core.CreatureIdentityComponent
CONSIDER DEPRECATING: Laboratory.Core.ECS.Components.CreatureIdentityComponent

ACTION REQUIRED:
1. Audit all code using Core identity component
2. Migrate to Chimera identity or rename Core version to GenericCreatureIdentity
3. Add global using alias in project settings
```

---

## 📋 ARCHITECTURAL PRINCIPLES

### **Namespace Convention:**
```
Laboratory.Chimera.*   = Chimera-specific features (genetics, personality, bonding)
Laboratory.Core.*      = Generic/reusable systems (can work without chimeras)
```

### **When to Use Each:**

#### Use **Chimera** namespace when:
- Feature requires genetics/personality/bonding
- System needs chimera-specific data
- Integration with chimera lifecycle (breeding, aging)

#### Use **Core** namespace when:
- Feature is generic (equipment, activities)
- System could work in other projects
- No chimera-specific dependencies

### **Integration Pattern:**
```
Chimera systems CAN depend on Core systems
Core systems SHOULD NOT depend on Chimera systems

Example:
✅ PersonalityEquipmentSystem (Chimera) → EquipmentSystem (Core)
❌ EquipmentSystem (Core) → PersonalityEquipmentSystem (Chimera)
```

---

## 🎯 CONSOLIDATION ACTION PLAN

### **Phase 1: Documentation (Week 1)**
- [x] Create SYSTEM_ARCHITECTURE_MAP.md
- [ ] Add architecture diagram (mermaid)
- [ ] Document all dual implementations
- [ ] Clarify canonical vs deprecated systems

### **Phase 2: Deprecation Cleanup (Week 2)**
- [ ] Mark `ProgressionSystem` with `[DisableAutoCreation]`
- [ ] Create migration script: `MonsterLevelComponent` → `PartnershipSkillComponent`
- [ ] Clarify or remove `Chimera/Breeding/ChimeraBreedingSystem.cs`
- [ ] Audit `Core.ECS.Components.CreatureIdentityComponent` usage

### **Phase 3: Integration Clarification (Week 3)**
- [ ] Add XML docs to all systems explaining canonical path
- [ ] Create integration examples for developers
- [ ] Add unit tests for system interactions
- [ ] Document equipment personality vs equipment mechanics separation

### **Phase 4: Performance Validation (Week 4)**
- [ ] Profile breeding system performance (all 3 implementations)
- [ ] Ensure Burst compilation is active on canonical systems
- [ ] Validate 1000+ creature target with new partnership system
- [ ] Load testing with hybrid Chimera+Core systems

---

## 📊 SYSTEM STATUS SUMMARY

| System Area | Chimera Implementation | Core Implementation | Status |
|-------------|----------------------|---------------------|--------|
| **Breeding** | 2 systems (basic + service) | 1 system (Burst ECS) | ✅ Canonical defined |
| **Progression** | 2 systems (old + new) | None | ⚠️ Migration needed |
| **Equipment** | Personality integration | Core mechanics | ✅ Both active |
| **Activities** | Partnership system | Generic games | ✅ Both active |
| **Identity** | Full chimera data | Basic creature data | ⚠️ Ambiguity exists |

---

## 🔍 ADDITIONAL DUAL SYSTEMS FOUND

### **AI Systems**
- `AI/Personality/GeneticPersonalitySystem.cs` - Personality-driven AI
- `Chimera/AI/EnemyDetectionSystem.cs` - Chimera-specific detection
- `Chimera/ECS/Systems/ChimeraBehaviorSystem.cs` - Unified behavior
- **Status:** Needs investigation

### **Combat Systems**
- `Subsystems/Combat/*` - Generic combat framework
- `Chimera/Activities/Combat/*` - Chimera combat activities
- **Status:** Likely separate concerns (generic vs chimera-specific)

---

**Last Updated:** 2025-11-22
**Maintainer:** Architecture team
**Review Required:** Before production release
