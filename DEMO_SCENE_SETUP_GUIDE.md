# Project Chimera - Demo Scene Setup Guide
**Complete reference for creating demo scenes for all game modes and systems**

---

## 📋 Table of Contents

1. [Core Scene Template](#core-scene-template)
2. [Activity/Genre Demo Scenes](#activitygenre-demo-scenes)
3. [System Demo Scenes](#system-demo-scenes)
4. [UI Requirements](#ui-requirements)
5. [Testing Scenes](#testing-scenes)

---

## Core Scene Template

### Every Demo Scene Should Have:

#### 1. **Scene Bootstrap** (Essential)
```
GameObject: "ChimeraSceneBootstrap"
├── Component: ChimeraSceneBootstrapper (MonoBehaviour)
│   └── Reference: ChimeraGameConfig (ScriptableObject)
└── Component: ConvertToEntity
```

**Why:** Initializes all ECS systems and loads configurations automatically.

#### 2. **World Management**
```
GameObject: "WorldManager"
├── Component: World (auto-created by DOTS)
└── SubScene: "MainSubScene" (optional, for ECS entities)
```

#### 3. **Camera Setup**
```
GameObject: "Main Camera"
├── Component: Camera
├── Component: AudioListener
└── Component: CameraController (optional)
```

#### 4. **UI Canvas**
```
GameObject: "UI Canvas"
├── Component: Canvas (Screen Space - Overlay)
├── Component: CanvasScaler
├── Component: GraphicRaycaster
└── Children: [Scene-specific UI]
```

#### 5. **Event System**
```
GameObject: "EventSystem"
├── Component: EventSystem
└── Component: StandaloneInputModule
```

---

## Activity/Genre Demo Scenes

### Template: Activity Demo Scene
**Use for any of the 47 genres**

#### Scene Hierarchy
```
DemoScene_[GenreName]
├── ChimeraSceneBootstrap ⭐ (Required)
│   └── ChimeraSceneBootstrapper
│       ├── Game Config: ChimeraGameConfig.asset
│       └── Auto-spawn Test Creatures: ✓
│
├── ActivityCenter ⭐ (Required for activities)
│   ├── ActivityCenterManager
│   └── ActivityCenterAuthoring
│       ├── Activity Type: [GenreName]
│       ├── Available Difficulties: [Easy, Normal, Hard, Expert, Master]
│       └── Genre Config: Genre_[GenreName].asset
│
├── PlayerController
│   ├── PlayerInputHandler
│   ├── PlayerSkillTracker (tracks player performance)
│   └── PartnershipController (links to active chimera)
│
├── ChimeraSpawner (For testing)
│   ├── CreatureSpawnerAuthoring
│   │   ├── Species Weight: FireDragon (1.0)
│   │   ├── Initial Population: 1
│   │   └── Auto-spawn on Start: ✓
│   └── ConvertToEntity
│
├── Main Camera
│   ├── Camera
│   ├── CameraController
│   └── CameraFollowTarget (follows player/chimera)
│
├── UI Canvas ⭐ (Required)
│   ├── ActivityHUD (Activity-specific UI)
│   ├── PerformanceDisplay (shows real-time performance)
│   ├── PartnershipIndicator (bond strength, cooperation)
│   ├── TimerDisplay (activity timer)
│   ├── ScoreDisplay (current score/rank)
│   └── ResultsPanel (shown on completion)
│
├── Environment
│   ├── ActivityPlayArea (genre-specific)
│   ├── Lighting (DirectionalLight)
│   └── AudioSource (background music)
│
└── EventSystem
```

### Genre-Specific Requirements

#### 🎯 **Action Genres** (FPS, TPS, Fighting, etc.)

**Additional Objects:**
```
├── Combat Arena
│   ├── Spawn Points (for enemies)
│   ├── Cover Objects (for tactical gameplay)
│   ├── Health Pickups
│   └── Boundaries (colliders)
│
├── Enemy Manager
│   ├── EnemySpawner
│   └── DifficultyController
│
└── Combat UI
    ├── Crosshair/Reticle
    ├── Health Bar (player)
    ├── Health Bar (chimera)
    ├── Ammo Counter (if applicable)
    └── Hit Indicators
```

**Required Components:**
- `CombatActivitySystem` (ECS)
- `PlayerCombatController` (MonoBehaviour)
- `ChimeraCombatAI` (attached to chimera entity)
- `DamageSystem` (ECS)
- `ProjectileSystem` (ECS)

#### 🏎️ **Racing Genre**

**Additional Objects:**
```
├── Race Track
│   ├── Starting Grid
│   ├── Checkpoints (triggers)
│   ├── Finish Line
│   └── Track Boundaries
│
├── Vehicle System
│   ├── Player Vehicle
│   │   ├── VehicleController
│   │   └── ChimeraPassenger (chimera as copilot)
│   └── AI Racers (3-7 opponents)
│
└── Racing UI
    ├── Speedometer
    ├── Lap Timer
    ├── Position Indicator (1st, 2nd, etc.)
    ├── Mini-map
    └── Boost Meter (cooperation bonus)
```

**Required Components:**
- `RacingActivitySystem` (ECS)
- `VehiclePhysicsController`
- `CheckpointSystem` (ECS)
- `RaceProgressTracker`

#### 🧩 **Puzzle Genres** (Match-3, Tetris, Physics, etc.)

**Additional Objects:**
```
├── Puzzle Grid/Board
│   ├── GridManager
│   ├── Tile Spawner
│   └── Match Detector
│
├── Input Handler
│   └── PuzzleInputController
│
└── Puzzle UI
    ├── Move Counter
    ├── Score Display
    ├── Combo Meter
    ├── Hint Button
    └── Next Piece Preview (Tetris-like)
```

**Required Components:**
- `PuzzleActivitySystem` (ECS)
- `GridMatchingSystem` (for Match-3)
- `PhysicsSimulationSystem` (for physics puzzles)
- `PuzzleSolutionValidator`

#### 🎮 **Strategy Genres** (RTS, Turn-Based, etc.)

**Additional Objects:**
```
├── Strategic Map
│   ├── Grid System
│   ├── Fog of War
│   └── Resource Nodes
│
├── Unit Management
│   ├── Player Units
│   ├── Enemy Units
│   └── Neutral Units
│
├── Building System (RTS)
│   └── Buildable Structures
│
└── Strategy UI
    ├── Unit Selection Panel
    ├── Resource Counter
    ├── Minimap
    ├── Build Menu
    ├── Tech Tree (if applicable)
    └── Turn Indicator (turn-based)
```

**Required Components:**
- `StrategyActivitySystem` (ECS)
- `UnitSelectionSystem` (ECS)
- `PathfindingSystem` (ECS)
- `FogOfWarSystem` (ECS)
- `ResourceManagementSystem`

#### 🎵 **Rhythm/Music Genres**

**Additional Objects:**
```
├── Music System
│   ├── AudioSource (music track)
│   ├── Beat Detector
│   └── Note Spawner
│
├── Note Highway/Track
│   ├── Note Lanes (4-6 lanes)
│   └── Hit Zone
│
└── Rhythm UI
    ├── Score Display
    ├── Combo Counter
    ├── Accuracy Meter
    ├── Note Chart
    └── Perfect/Good/Miss Indicators
```

**Required Components:**
- `RhythmActivitySystem` (ECS)
- `BeatSyncSystem` (ECS)
- `NoteTimingSystem`
- `AccuracyCalculator`

---

## System Demo Scenes

### 1. Breeding System Demo
**Scene:** `DemoScene_Breeding`

```
BreedingDemo
├── ChimeraSceneBootstrap ⭐
│   └── ChimeraSceneBootstrapper
│
├── BreedingCenter
│   ├── BreedingSystemManager
│   ├── BreedingEnvironment (MonoBehaviour)
│   │   ├── Biome Type: Temperate
│   │   ├── Temperature: 22°C
│   │   ├── Food Quality: 0.8
│   │   └── Stress Level: 0.2
│   └── BreedingPairValidator
│
├── Parent Chimeras (2)
│   ├── Parent 1
│   │   ├── EnhancedCreatureAuthoring
│   │   │   ├── Species: FireDragon
│   │   │   ├── Age: 180 days (adult)
│   │   │   └── Genetic Profile: Configured
│   │   └── ConvertToEntity
│   │
│   └── Parent 2
│       ├── EnhancedCreatureAuthoring
│       └── ConvertToEntity
│
├── Breeding UI
│   ├── Canvas
│   │   ├── Parent Selection Panel
│   │   │   ├── Parent 1 Info (stats, genetics)
│   │   │   └── Parent 2 Info
│   │   ├── Compatibility Display
│   │   │   ├── Genetic Compatibility: 85%
│   │   │   ├── Success Chance: 72%
│   │   │   └── Expected Traits
│   │   ├── Breeding Controls
│   │   │   ├── "Attempt Breeding" Button
│   │   │   └── "View Genetics" Button
│   │   └── Offspring Preview
│   │       ├── Predicted Appearance
│   │       ├── Genetic Breakdown
│   │       └── Trait Inheritance
│   └── EventSystem
│
├── Genetics Visualizer
│   ├── DNA Strand Display
│   ├── Gene Comparison View
│   └── Mutation Indicator
│
└── Environment
    ├── Breeding Habitat (visual)
    └── Lighting
```

**Required Components:**
- `BreedingSystem` (service layer)
- `GeneticCalculator`
- `OffspringGenerator`
- `BreedingEnvironment`
- `CompatibilityChecker`

**ScriptableObject References:**
- `ChimeraSpeciesConfig.asset` (for each parent)
- `BreedingRulesConfig.asset`
- `MutationConfig.asset`

---

### 2. Partnership/Bonding Demo
**Scene:** `DemoScene_Partnership`

```
PartnershipDemo
├── ChimeraSceneBootstrap ⭐
│
├── Partnership Manager
│   ├── PartnershipProgressionSystem (ECS reference)
│   ├── BondStrengthTracker
│   └── EmotionalStateManager
│
├── Player Character
│   ├── PlayerController
│   ├── InteractionHandler
│   └── InventoryManager (for gifts/food)
│
├── Chimera Partner
│   ├── EnhancedCreatureAuthoring
│   │   ├── Species: Configured
│   │   ├── Personality: Random/Configured
│   │   ├── Age: 50 days (child)
│   │   └── Initial Bond: 0.5
│   ├── EmotionalIndicatorComponent
│   └── ConvertToEntity
│
├── Interaction Objects
│   ├── Food Items (3-5 varieties)
│   │   └── FoodItemAuthoring (affects happiness)
│   ├── Toys/Entertainment
│   │   └── PlayItemAuthoring
│   └── Equipment Items
│       └── EquipmentItemAuthoring
│
├── Partnership UI
│   ├── Canvas
│   │   ├── Bond Strength Meter
│   │   │   ├── Visual Bar (0-100%)
│   │   │   ├── Current Level: "Friend" / "Partner" / "Soulmate"
│   │   │   └── Progress to Next Level
│   │   ├── Emotional Indicator
│   │   │   ├── Current Mood Icon
│   │   │   ├── Happiness: 75%
│   │   │   ├── Trust: 60%
│   │   │   └── Stress: 20%
│   │   ├── Personality Display
│   │   │   ├── Current Traits (5 shown)
│   │   │   └── Trait Change Indicators
│   │   ├── Interaction Panel
│   │   │   ├── Feed Button
│   │   │   ├── Play Button
│   │   │   ├── Pet Button
│   │   │   └── Train Button
│   │   └── History Log
│   │       └── Recent Interactions (last 10)
│   └── EventSystem
│
├── Activity Test Area
│   ├── Simple Mini-game (for bonding)
│   └── Success/Failure Triggers
│
└── Environment
    ├── Home/Rest Area
    └── Interactive Objects
```

**Required Components:**
- `PartnershipProgressionSystem` (ECS)
- `EmotionalResponseSystem` (ECS)
- `BondingEventHandler`
- `PersonalityEvolutionSystem` (ECS)
- `InteractionValidator`

**ScriptableObject References:**
- `PersonalityConfig.asset`
- `BondingRulesConfig.asset`
- `EmotionalResponseConfig.asset`

---

### 3. AI Behavior Demo
**Scene:** `DemoScene_AI`

```
AIDemo
├── ChimeraSceneBootstrap ⭐
│
├── AI Test Environment
│   ├── Large Open Area (50x50)
│   ├── Food Sources (scattered)
│   ├── Water Sources
│   ├── Shelter Areas
│   └── Danger Zones (for fleeing behavior)
│
├── Test Chimeras (5-10)
│   ├── Chimera 1
│   │   ├── EnhancedCreatureAuthoring
│   │   ├── BehaviorStateComponent
│   │   ├── CreatureNeedsComponent
│   │   │   ├── Hunger: 0.5
│   │   │   ├── Thirst: 0.7
│   │   │   └── Energy: 0.8
│   │   ├── CreaturePersonalityComponent
│   │   └── ConvertToEntity
│   │
│   └── [Chimera 2-10 similar setup]
│
├── AI Debug UI
│   ├── Canvas
│   │   ├── Selected Creature Panel
│   │   │   ├── Current Behavior State
│   │   │   ├── Needs Display (all 6 needs)
│   │   │   ├── Personality Traits
│   │   │   └── Decision Weights
│   │   ├── Behavior Override Panel
│   │   │   ├── Force Behavior Dropdown
│   │   │   └── "Override" Button
│   │   ├── Needs Control
│   │   │   ├── Hunger Slider
│   │   │   ├── Thirst Slider
│   │   │   └── [Other needs sliders]
│   │   └── AI Stats
│   │       ├── Total Decisions Made
│   │       ├── Behavior Switches
│   │       └── Average Decision Time
│   └── EventSystem
│
├── Visual Debuggers
│   ├── Path Visualizer (draws current path)
│   ├── Need Indicators (above each chimera)
│   └── Behavior State Labels
│
└── Camera
    ├── Free-look Camera
    └── Target Follow (optional)
```

**Required Components:**
- `CreatureAISystem` (ECS)
- `BehaviorStateSystem` (ECS)
- `NeedsPrioritySystem` (ECS)
- `PathfindingSystem` (ECS)
- `DecisionMakingSystem` (ECS)

**Debug Tools:**
- `AIDebugVisualizer` (draws gizmos)
- `BehaviorLogger`

---

### 4. Save/Load System Demo
**Scene:** `DemoScene_SaveLoad`

```
SaveLoadDemo
├── ChimeraSceneBootstrap ⭐
│
├── Save System Manager
│   ├── SaveLoadSystem
│   ├── SaveFileManager
│   └── MigrationHandler
│
├── Test Environment
│   ├── Multiple Chimeras (3-5)
│   ├── Player Progress Data
│   └── World State
│
├── Save/Load UI
│   ├── Canvas
│   │   ├── Save Panel
│   │   │   ├── Save Slot List (3 slots)
│   │   │   │   ├── Slot 1 Info (date, time, creatures)
│   │   │   │   ├── Slot 2 Info
│   │   │   │   └── Slot 3 Info
│   │   │   ├── "Quick Save" Button
│   │   │   └── "Save As..." Button
│   │   ├── Load Panel
│   │   │   ├── Available Saves List
│   │   │   ├── Save Preview
│   │   │   │   ├── Screenshot
│   │   │   │   ├── Save Version
│   │   │   │   ├── Creature Count
│   │   │   │   └── Play Time
│   │   │   ├── "Load" Button
│   │   │   └── "Delete" Button
│   │   ├── Settings Panel
│   │   │   ├── Auto-save Toggle
│   │   │   ├── Auto-save Interval
│   │   │   └── Backup Saves Toggle
│   │   └── Debug Tools
│   │       ├── "Corrupt Save" (testing)
│   │       ├── "Test Migration" (v1.0 → v2.0)
│   │       └── "Validate Integrity"
│   └── EventSystem
│
└── Progress Indicators
    └── Saving/Loading Overlay
```

**Required Components:**
- `SaveLoadSystem`
- `SaveDataSerializer`
- `FileIOManager`
- `DataIntegrityValidator`
- `MigrationSystem`

**Test Data:**
- Pre-made save files (v1.0, v2.0)
- Corrupted save (for recovery testing)

---

## UI Requirements

### Universal UI Components (Every Scene)

#### 1. **Debug HUD** (Top-Left)
```
Debug Panel
├── FPS Counter
├── Entity Count
├── System Stats
└── Memory Usage
```

#### 2. **Performance Monitor** (Top-Right)
```
Performance Panel
├── Frame Time Graph
├── ECS Job Time
├── Memory Allocations
└── Warning Indicators
```

#### 3. **Scene Controls** (Bottom)
```
Control Panel
├── Pause Button
├── Restart Scene Button
├── Time Scale Slider
└── Settings Button
```

### Activity-Specific UI

#### **Pre-Activity Screen**
```
Pre-Activity Panel
├── Activity Title
├── Genre Description
├── Difficulty Selection
│   └── [Easy] [Normal] [Hard] [Expert] [Master]
├── Chimera Selection
│   ├── Available Partners List
│   └── Stats Preview
├── Expected Rewards
│   ├── Base Currency: 100 coins
│   ├── Skill Gain: +0.015
│   └── Partnership: +0.008
└── "Start Activity" Button
```

#### **During Activity**
```
Activity HUD
├── Timer (countdown/elapsed)
├── Performance Meter (0-100%)
├── Cooperation Indicator
│   ├── Bond Strength: 75%
│   └── Cooperation Bonus: +15%
├── Player Skill Display
│   └── Current Skill: 82%
├── Chimera Contribution
│   └── Trait Match: 90%
├── Current Score/Rank
│   └── Projected: Gold
└── Objectives (genre-specific)
```

#### **Post-Activity Results**
```
Results Panel
├── Final Performance: 87%
├── Rank Achieved: [Gold Medal Icon]
├── Breakdown
│   ├── Player Performance: 85%
│   ├── Chimera Contribution: 92%
│   ├── Bond Multiplier: 1.15x
│   └── Age Factor: 1.0x
├── Rewards Earned
│   ├── Currency: +187 coins
│   ├── Skill Mastery: +0.0187 (Racing)
│   └── Partnership Quality: +0.0087
├── New Records
│   └── "New Best Time!" (if applicable)
├── Partnership Changes
│   ├── Bond Strength: 75% → 78% (+3%)
│   └── Emotional Impact: "Happy"
└── Buttons
    ├── "Retry" (same difficulty)
    ├── "Next Difficulty"
    └── "Exit to Menu"
```

---

## Testing Scenes

### Stress Test Scene
**Scene:** `DemoScene_StressTest`

```
StressTest
├── ChimeraSceneBootstrap ⭐
│
├── Creature Spawner
│   ├── CreatureSpawnerAuthoring
│   │   ├── Spawn Count: 1000
│   │   ├── Spawn Rate: 100/second
│   │   └── Random Species: ✓
│   └── ConvertToEntity
│
├── Performance Monitor
│   ├── FPS Tracker
│   ├── Entity Count Display
│   ├── Memory Usage
│   └── System Profiler
│
└── Test Controls UI
    ├── Spawn Controls
    │   ├── Count Slider (0-2000)
    │   ├── "Spawn Batch" Button
    │   └── "Clear All" Button
    ├── Simulation Controls
    │   ├── Time Scale Slider
    │   ├── Enable/Disable Systems Toggles
    │   └── Pause/Resume
    └── Performance Stats
        ├── Current FPS
        ├── Entity Count
        ├── Active Systems Count
        └── Frame Budget Graph
```

---

## Quick Reference Checklists

### ✅ Activity Demo Scene Checklist
- [ ] ChimeraSceneBootstrap with ChimeraGameConfig
- [ ] ActivityCenter with genre-specific config
- [ ] Player controller with input handling
- [ ] At least 1 test chimera (spawner or pre-placed)
- [ ] Genre-specific environment/playarea
- [ ] Activity HUD (timer, score, performance)
- [ ] Results panel with breakdown
- [ ] Camera with appropriate controls
- [ ] Event system for UI
- [ ] Background music/SFX (optional)

### ✅ System Demo Scene Checklist
- [ ] ChimeraSceneBootstrap
- [ ] System-specific manager component
- [ ] Test data/entities
- [ ] Debug UI with system stats
- [ ] Control panel for testing
- [ ] Visual debuggers (gizmos, labels)
- [ ] Performance monitoring
- [ ] Documentation panel (what to test)

### ✅ Multiplayer Test Scene
- [ ] Network Manager
- [ ] Server/Client Toggle
- [ ] Lobby UI
- [ ] 2-4 Player spawn points
- [ ] Synchronized activity area
- [ ] Network stats display
- [ ] Connection controls
- [ ] Lag simulation controls

---

## ScriptableObject Asset Requirements

### Every Scene Needs:
1. **ChimeraGameConfig.asset** - Master configuration
2. **GenreLibrary.asset** - All 47 genre configs (if using activities)
3. **Genre_[Name].asset** - Specific genre config (per activity scene)
4. **Activity_[Name].asset** - ActivityConfig (per activity scene)
5. **Species configs** - For each chimera type used

### Location:
```
Assets/_Project/Resources/Configs/
├── ChimeraGameConfig.asset
├── GenreLibrary.asset
├── GenreConfigurations/
│   ├── Genre_Racing.asset
│   ├── Genre_Combat.asset
│   └── [45 more...]
├── Activities/
│   ├── Activity_Racing.asset
│   ├── Activity_Combat.asset
│   └── [45 more...]
└── Species/
    ├── FireDragon.asset
    ├── IceWolf.asset
    └── [More species...]
```

---

## Next Steps After Scene Creation

1. **Test Scene Validation**
   - Does it compile without errors?
   - Do all systems initialize?
   - Can you spawn chimeras?
   - Does UI respond correctly?

2. **Performance Check**
   - Maintain 60 FPS with target entity count
   - No memory leaks
   - No excessive GC allocations

3. **Gameplay Test**
   - Can you complete the activity?
   - Are results calculated correctly?
   - Does partnership system respond?

4. **Polish**
   - Add visual feedback
   - Improve UI clarity
   - Add sound effects
   - Smooth transitions

---

**Use this guide as a reference when creating any demo scene. Each scene type has specific requirements, but they all share the core bootstrap and UI structure.**

Need help with a specific scene type? Refer to the detailed section for that system!
