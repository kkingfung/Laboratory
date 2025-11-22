# Project Chimera - System Initialization Order
**Purpose:** Document the correct initialization order for all ECS systems
**Critical:** Systems must initialize in this order to prevent race conditions and null references

---

## 🎯 INITIALIZATION PHASES

Project Chimera uses a phased initialization approach to ensure all dependencies are met:

### **Phase 1: Bootstrap & Configuration** (Earliest)
### **Phase 2: Core Infrastructure**
### **Phase 3: Creature Systems**
### **Phase 4: Gameplay Systems**
### **Phase 5: UI & Presentation** (Latest)

---

## 📋 DETAILED INITIALIZATION ORDER

### **PHASE 1: BOOTSTRAP & CONFIGURATION**

#### **1.1 Game Bootstrap** (`GameBootstrap.cs`)
- **Order:** FIRST
- **UpdateGroup:** N/A (MonoBehaviour, runs in Awake/Start)
- **Purpose:** Asynchronous startup orchestration
- **Initializes:**
  - CoreServices
  - Configuration loading
  - AssetPreload
  - GameState
  - Network
  - UI
- **Dependencies:** None
- **Status:** ✅ Active

#### **1.2 Configuration System** (`ChimeraGameConfig.cs`)
- **Order:** Before any systems that need configuration
- **UpdateGroup:** N/A (ScriptableObject)
- **Purpose:** Master game configuration
- **Provides:**
  - Species definitions
  - Biome configurations
  - AI settings
  - Performance parameters
  - Networking flags
- **Dependencies:** None
- **Status:** ✅ Active

#### **1.3 Network Bootstrap** (`ChimeraNetworkBootstrapper.cs`)
- **Order:** After configuration, before gameplay systems
- **UpdateGroup:** N/A (MonoBehaviour)
- **Purpose:** Netcode for Entities setup
- **Condition:** Only if `ChimeraGameConfig.enableMultiplayer == true`
- **Initializes:**
  - Server/client connections
  - Network synchronization systems
  - Player entity spawning
- **Dependencies:** ChimeraGameConfig
- **Status:** ✅ Gated (off by default)

---

### **PHASE 2: CORE INFRASTRUCTURE**

#### **2.1 Social Systems Integration Hub** (`SocialSystemsIntegrationHub.cs`)
- **Order:** OrderFirst = true in SimulationSystemGroup
- **UpdateGroup:** `SimulationSystemGroup` (FIRST)
- **Purpose:** Coordinate all social systems
- **Initializes:**
  - Bond strength caches
  - Strong bond count tracking
  - System health monitoring
- **Dependencies:** None (runs first to prepare data for other systems)
- **Systems Coordinated:**
  1. EnhancedBondingSystem
  2. AgeSensitivitySystem
  3. PopulationManagementSystem
  4. SocialEngagementSystem
  5. EmotionalContagionSystem
  6. GroupDynamicsSystem
  7. CommunicationSystem
  8. SocialNetworkSystem
  9. CulturalEvolutionSystem
- **Status:** ✅ Active (Week 2)

#### **2.2 Pathfinding Systems**
- **Order:** Early in SimulationSystemGroup
- **UpdateGroup:** `SimulationSystemGroup`
- **Purpose:** Spatial navigation and movement
- **Systems:**
  - `FlowFieldSystem` - Flow field pathfinding
  - `UnifiedECSPathfindingSystem` - ECS integration
  - `EnhancedPathfindingSystem` - A* with optimizations
- **Dependencies:** None (provides movement data)
- **Status:** ✅ Active (multiple implementations - see SYSTEM_ARCHITECTURE_MAP.md)

---

### **PHASE 3: CREATURE SYSTEMS**

#### **3.1 Genetics & Identity**

##### **3.1.1 Creature Identity Initialization**
- **Component:** `CreatureIdentityComponent` (Laboratory.Chimera.Core)
- **Order:** Before any creature-dependent systems
- **Purpose:** Establish creature ID, species, age, life stage
- **Dependencies:** None
- **Status:** ✅ Canonical

##### **3.1.2 Genetic Data Initialization**
- **Component:** `ChimeraGeneticDataComponent` (Laboratory.Chimera.ECS)
- **Order:** After identity, before behavior systems
- **Purpose:** Genetics drive behavior decisions
- **Dependencies:** CreatureIdentityComponent
- **Legacy:** `GeneticComponent` (DEPRECATED - Week 2)
- **Status:** ✅ Canonical

#### **3.2 Behavior & AI**

##### **3.2.1 Behavior State System** (`ChimeraBehaviorSystem.cs`)
- **Order:** After genetics, before movement
- **UpdateGroup:** `SimulationSystemGroup`
- **Purpose:** Genetics-driven behavior decision-making
- **Components Used:**
  - `ChimeraGeneticDataComponent`
  - `BehaviorStateComponent`
  - `CreatureNeedsComponent`
- **Dependencies:** ChimeraGeneticDataComponent, CreatureIdentityComponent
- **Status:** ✅ Active

##### **3.2.2 AI State System** (`UnifiedAIStateSystem.cs`)
- **Order:** After behavior decisions
- **UpdateGroup:** `SimulationSystemGroup`
- **Purpose:** Unified AI state management
- **Dependencies:** BehaviorStateComponent
- **Status:** ✅ Active

#### **3.3 Bonding & Social Systems**

**IMPORTANT:** Social systems coordinate through SocialSystemsIntegrationHub (runs FIRST)

##### **3.3.1 Enhanced Bonding System** (`EnhancedBondingSystem.cs`)
- **Order:** After SocialSystemsIntegrationHub
- **UpdateGroup:** `SimulationSystemGroup`
- **Purpose:** Core bonding with generational memory
- **Components:**
  - `BondingComponent`
  - `ActiveBond`
  - `GenerationalMemoryComponent`
- **Dependencies:**
  - SocialSystemsIntegrationHub (coordination)
  - CreatureIdentityComponent
- **Integrates With:** AgeSensitivitySystem (reads AgeSensitivityComponent)
- **Status:** ✅ Active

##### **3.3.2 Age Sensitivity System** (`AgeSensitivitySystem.cs`)
- **Order:** Before or parallel with EnhancedBondingSystem
- **UpdateGroup:** `SimulationSystemGroup`
- **Purpose:** Age-based forgiveness/memory modifiers
- **Components:**
  - `AgeSensitivityComponent`
  - `EmotionalScarComponent`
- **Dependencies:**
  - SocialSystemsIntegrationHub
  - CreatureIdentityComponent (for age/life stage)
- **Status:** ✅ Active

##### **3.3.3 Population Management System** (`PopulationManagementSystem.cs`)
- **Order:** After EnhancedBondingSystem
- **UpdateGroup:** `SimulationSystemGroup`
- **UpdateAfter:** `EnhancedBondingSystem`
- **Purpose:** 1-5 chimera slot capacity management
- **Components:**
  - `ChimeraPopulationCapacity`
  - `ChimeraBondTracker` (buffer)
- **Dependencies:**
  - SocialSystemsIntegrationHub (bond calculations)
  - EnhancedBondingSystem (bond strength data)
  - CreatureBondData
- **Status:** ✅ Active (type alias for CreatureIdentityComponent - Week 1)

#### **3.4 Consciousness & Emotions**

##### **3.4.1 Emotional Indicator System** (`EmotionalIndicatorSystem.cs`)
- **Order:** After personality and bonding
- **UpdateGroup:** `SimulationSystemGroup`
- **Purpose:** Display mood/feelings via emoji indicators
- **Components:**
  - `EmotionalIndicatorComponent`
  - `EmotionalContext` (buffer)
- **Dependencies:**
  - `CreaturePersonality`
  - `CreatureBondData` (bond strength)
  - `CreatureIdentityComponent` (life stage)
- **Status:** ✅ Active

##### **3.4.2 Personality Stability System** (`PersonalityStabilitySystem.cs`)
- **Order:** After emotional indicators
- **UpdateGroup:** `SimulationSystemGroup`
- **Purpose:** Maintain personality coherence, lock baselines
- **Components:**
  - `PersonalityStabilityComponent`
  - `PersonalityBaseline`
- **Dependencies:** CreaturePersonality
- **Status:** ✅ Active

---

### **PHASE 4: GAMEPLAY SYSTEMS**

#### **4.1 Breeding Systems**

##### **4.1.1 Breeding Service** (`BreedingSystem.cs`) - LEGACY
- **Order:** N/A (called from other systems)
- **Type:** MonoBehaviour service
- **Purpose:** Environmental breeding calculations
- **Status:** ✅ Active (bridged by ECS system)

##### **4.1.2 Chimera Breeding System** (`ChimeraBreedingSystem.cs` - ECS)
- **Order:** After ChimeraBehaviorSystem
- **UpdateGroup:** `SimulationSystemGroup`
- **UpdateAfter:** `ChimeraBehaviorSystem`
- **Purpose:** Burst-compiled breeding with genetics
- **Features:**
  - Spatial hash for mate finding
  - Territorial breeding requirements
  - Job parallelization
- **Dependencies:**
  - ChimeraGeneticDataComponent
  - CreatureIdentityComponent
  - BehaviorStateComponent
  - Legacy BreedingSystem (bridged)
- **Status:** ✅ Canonical (SYSTEM_ARCHITECTURE_MAP.md)

#### **4.2 Progression Systems**

##### **4.2.1 Partnership Progression System** (`PartnershipProgressionSystem.cs`) - CANONICAL
- **Order:** After ActivitySystem
- **UpdateGroup:** `SimulationSystemGroup`
- **UpdateAfter:** `ActivitySystem`
- **Purpose:** NO LEVELS - Skill-based progression
- **Components:**
  - `PartnershipSkillComponent`
  - `RecordSkillImprovementRequest`
  - `SkillMilestoneReachedEvent`
- **Dependencies:** ActivitySystem (activity completion events)
- **Status:** ✅ Canonical (Week 1)

##### **4.2.2 Progression System** (`ProgressionSystem.cs`) - DEPRECATED
- **Order:** After ActivitySystem
- **UpdateGroup:** `SimulationSystemGroup`
- **UpdateAfter:** `ActivitySystem`
- **Purpose:** Legacy level-based progression
- **Components:**
  - `MonsterLevelComponent` (OBSOLETE)
  - `LevelStatBonusComponent` (OBSOLETE)
- **Status:** ⚠️ DEPRECATED - Marked [System.Obsolete] (Week 1)
- **Migration:** Use PartnershipProgressionSystem instead

#### **4.3 Activity Systems**

##### **4.3.1 Activity System** (`ActivitySystem.cs`)
- **Order:** After behavior/AI, before progression
- **UpdateGroup:** `SimulationSystemGroup`
- **Purpose:** Core activity mechanics
- **Components:** `ActivityComponents`
- **Dependencies:** BehaviorStateComponent
- **Status:** ✅ Active

##### **4.3.2 Partnership Activity System** (`PartnershipActivitySystem.cs`)
- **Order:** After ActivitySystem
- **UpdateGroup:** `SimulationSystemGroup`
- **Purpose:** Partnership-enhanced activities
- **Components:** `PartnershipActivityComponent`
- **Dependencies:**
  - ActivitySystem
  - PartnershipSkillComponent
  - EmotionalIndicatorComponent (mood bonus)
- **Status:** ✅ Active

#### **4.4 Equipment Systems**

##### **4.4.1 Equipment System** (Core)
- **Order:** Before personality equipment
- **UpdateGroup:** `SimulationSystemGroup`
- **Purpose:** Core equipment mechanics
- **Namespace:** `Laboratory.Core.Equipment`
- **Status:** ✅ Active

##### **4.4.2 Personality Equipment System** (`PersonalityEquipmentSystem.cs`)
- **Order:** After Core Equipment System
- **UpdateGroup:** `SimulationSystemGroup`
- **Purpose:** Personality-based equipment affinity
- **Namespace:** `Laboratory.Chimera.Equipment`
- **Dependencies:**
  - Core EquipmentSystem
  - CreaturePersonality
- **Status:** ✅ Active

#### **4.5 Combat Systems**

##### **4.5.1 Combat System** (`CombatSystem.cs`)
- **Order:** After behavior, before damage
- **UpdateGroup:** `SimulationSystemGroup`
- **Purpose:** Core combat mechanics
- **Status:** ✅ Active

##### **4.5.2 Damage System** (`DamageSystem.cs`)
- **Order:** After CombatSystem
- **UpdateGroup:** `SimulationSystemGroup`
- **UpdateAfter:** `CombatSystem`
- **Purpose:** Damage calculations
- **Status:** ✅ Active

##### **4.5.3 Death System** (`DeathSystem.cs`)
- **Order:** After DamageSystem
- **UpdateGroup:** `SimulationSystemGroup`
- **UpdateAfter:** `DamageSystem`
- **Purpose:** Death/respawn mechanics
- **Status:** ✅ Active

---

### **PHASE 5: UI & PRESENTATION**

#### **5.1 Visual Systems**
- **Order:** After all gameplay logic
- **UpdateGroup:** `PresentationSystemGroup`
- **Systems:**
  - ProceduralVisualSystem
  - BiomeAdaptationSystem
  - GeneticPhotographySystem
- **Dependencies:** ChimeraGeneticDataComponent (visual traits)
- **Status:** ✅ Active

#### **5.2 UI Systems**
- **Order:** Last (reads final game state)
- **UpdateGroup:** `PresentationSystemGroup`
- **Purpose:** Display game state to player
- **Dependencies:** All gameplay systems
- **Status:** ✅ Active

---

## ⚙️ UPDATE GROUP HIERARCHY

Unity ECS systems execute in this group order:

```
1. InitializationSystemGroup (earliest)
   ↓
2. SimulationSystemGroup (main gameplay)
   ├─ SocialSystemsIntegrationHub (OrderFirst = true)
   ├─ Pathfinding Systems
   ├─ ChimeraBehaviorSystem
   ├─ EnhancedBondingSystem
   ├─ AgeSensitivitySystem
   ├─ PopulationManagementSystem (UpdateAfter: EnhancedBondingSystem)
   ├─ EmotionalIndicatorSystem
   ├─ ChimeraBreedingSystem (UpdateAfter: ChimeraBehaviorSystem)
   ├─ ActivitySystem
   ├─ PartnershipActivitySystem
   ├─ PartnershipProgressionSystem (UpdateAfter: ActivitySystem)
   ├─ CombatSystem
   ├─ DamageSystem (UpdateAfter: CombatSystem)
   └─ DeathSystem (UpdateAfter: DamageSystem)
   ↓
3. PresentationSystemGroup (UI/visuals)
   ├─ ProceduralVisualSystem
   └─ UI Systems (latest)
   ↓
4. EndSimulationEntityCommandBufferSystem (cleanup)
```

---

## 🔗 DEPENDENCY CHAIN

Critical dependency chains that must be respected:

### **Bonding → Population Unlock Chain:**
```
1. SocialSystemsIntegrationHub (calculates effective bond strength)
   ↓
2. EnhancedBondingSystem (updates base bond strength)
   ↓  (reads AgeSensitivityComponent)
3. AgeSensitivitySystem (provides age modifiers)
   ↓
4. PopulationManagementSystem (checks unlock requirements)
   ↓
5. UI displays unlock notification
```

### **Genetics → Behavior → Activity Chain:**
```
1. ChimeraGeneticDataComponent (initialized on spawn)
   ↓
2. ChimeraBehaviorSystem (genetics drive decisions)
   ↓
3. ActivitySystem (executes activities)
   ↓
4. PartnershipActivitySystem (cooperation bonus)
   ↓
5. PartnershipProgressionSystem (skill improvement)
```

### **Age → Emotions → Bonding Chain:**
```
1. CreatureIdentityComponent (age percentage, life stage)
   ↓
2. AgeSensitivitySystem (age-based modifiers)
   ↓
3. EmotionalIndicatorSystem (age-appropriate emotions)
   ↓
4. EnhancedBondingSystem (reads age sensitivity)
   ↓
5. SocialSystemsIntegrationHub (effective bond strength)
```

---

## 🚨 COMMON INITIALIZATION ERRORS

### **Error 1: NullReferenceException in PopulationManagementSystem**
**Cause:** Trying to access CreatureBondData before EnhancedBondingSystem initialized
**Fix:** Ensure `[UpdateAfter(typeof(EnhancedBondingSystem))]`

### **Error 2: Bond strength always 0**
**Cause:** SocialSystemsIntegrationHub not running first
**Fix:** Ensure `[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]`

### **Error 3: Personality changes not affecting equipment**
**Cause:** PersonalityEquipmentSystem running before Core EquipmentSystem
**Fix:** Ensure PersonalityEquipmentSystem depends on Core EquipmentSystem

### **Error 4: Activities not improving skills**
**Cause:** PartnershipProgressionSystem running before ActivitySystem
**Fix:** Ensure `[UpdateAfter(typeof(ActivitySystem))]`

### **Error 5: Creatures always show Neutral emotion**
**Cause:** EmotionalIndicatorSystem running before bond/personality data ready
**Fix:** Ensure it runs after EnhancedBondingSystem and CreaturePersonality init

---

## 📋 VALIDATION CHECKLIST

Use this checklist to verify correct initialization order:

- [ ] SocialSystemsIntegrationHub runs FIRST in SimulationSystemGroup
- [ ] Genetics initialized before behavior systems
- [ ] Behavior systems run before activity systems
- [ ] Activity systems run before progression systems
- [ ] Bonding systems run before population management
- [ ] Combat systems run before damage systems
- [ ] Damage systems run before death systems
- [ ] Gameplay systems run before UI/presentation
- [ ] Network systems only initialize if `enableMultiplayer == true`
- [ ] Legacy systems (ProgressionSystem, GeneticComponent) not actively used

---

## 🔧 DEBUGGING TOOLS

### **System Health Check**
```csharp
// Get hub status
var status = SocialSystemsIntegrationHub.GetDebugInfo();
Debug.Log(status);

// Expected output:
// Social Systems Status:
// - EnhancedBonding: ✅
// - AgeSensitivity: ✅
// - Population: ✅
// - Tracked bonds: 23
// - Players tracked: 1
```

### **Validate Initialization Order**
```csharp
// In Editor, check system order:
// Window → Analysis → Entity Debugger → Systems
// Verify SimulationSystemGroup hierarchy matches this document
```

---

## 📚 REFERENCES

- [SYSTEM_REVIEW.md](SYSTEM_REVIEW.md) - Complete system audit
- [SYSTEM_ARCHITECTURE_MAP.md](SYSTEM_ARCHITECTURE_MAP.md) - Dual implementation mapping
- [WEEK1_CRITICAL_FIXES_COMPLETE.md](WEEK1_CRITICAL_FIXES_COMPLETE.md) - Week 1 fixes
- Unity ECS Docs: [System Update Order](https://docs.unity3d.com/Packages/com.unity.entities@latest)

---

**Last Updated:** 2025-11-22 (Week 2)
**Maintainer:** Architecture team
**Review Required:** Before production release
**Status:** Living document - update when adding new systems
