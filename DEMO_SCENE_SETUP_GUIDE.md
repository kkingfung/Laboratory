# Project Chimera - Demo Scene Setup Guide
**Complete reference for creating demo scenes for all game modes and systems**

---

## 📋 Table of Contents

1. [Core Scene Template](#core-scene-template)
2. [Action Genre Scenes](#action-genre-scenes)
3. [Racing Genre Scene](#racing-genre-scene)
4. [Puzzle Genre Scenes](#puzzle-genre-scenes)
5. [Strategy Genre Scenes](#strategy-genre-scenes)
6. [Rhythm Genre Scene](#rhythm-genre-scene)
7. [RPG Genre Scenes](#rpg-genre-scenes)
8. [Simulation Genre Scenes](#simulation-genre-scenes)
9. [Sports Genre Scenes](#sports-genre-scenes)
10. [Breeding System Demo](#breeding-system-demo)
11. [Partnership System Demo](#partnership-system-demo)
12. [AI Behavior Demo](#ai-behavior-demo)
13. [Save/Load System Demo](#saveload-system-demo)
14. [Performance Test Scenes](#performance-test-scenes)

---

## Core Scene Template

### Overview
Every demo scene must have these foundational elements. Use this as the base for all scenes.

### GameObject Hierarchy
```
[SceneName]
├── ChimeraSceneBootstrap
├── WorldManager
├── Main Camera
├── Directional Light
├── UI Canvas
├── EventSystem
└── Environment (parent for scene-specific objects)
```

### 1. ChimeraSceneBootstrap

#### GameObject Setup
```
Name: "ChimeraSceneBootstrap"
Tag: Untagged
Layer: Default
```

#### Components List
| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `ChimeraSceneBootstrapper` | Initializes all ECS systems | See properties below |
| `ConvertToEntity` | Converts to ECS entity | Convert and Destroy |

#### ChimeraSceneBootstrapper Properties
```csharp
Game Config: ChimeraGameConfig.asset
Auto Spawn Test Creatures: true/false (scene-dependent)
Debug Mode: true (for demo scenes)
Initialize Networking: false (unless multiplayer scene)
```

#### Required ScriptableObject
- **Path**: `Assets/_Project/Resources/Configs/ChimeraGameConfig.asset`
- **Type**: `ChimeraGameConfig`
- **Must Reference**:
  - Available species configs
  - Biome configurations
  - Performance settings
  - Default genre library

---

### 2. WorldManager

#### GameObject Setup
```
Name: "WorldManager"
Tag: Untagged
Layer: Default
```

#### Components List
| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `World` | Auto-created by DOTS | Default World |
| `SubScene` (optional) | ECS entity container | Reference to SubScene asset |

#### SubScene Setup (Optional)
- **When to Use**: For large numbers of pre-placed entities
- **Location**: `Assets/_Project/Scenes/SubScenes/`
- **Naming**: `[SceneName]_SubScene`

---

### 3. Main Camera

#### GameObject Setup
```
Name: "Main Camera"
Tag: MainCamera
Layer: Default
Position: (0, 10, -10)
Rotation: (30, 0, 0)
```

#### Components List
| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `Camera` | Renders scene | See properties below |
| `AudioListener` | Receives audio | Default |
| `CameraController` (optional) | Camera movement | See properties below |

#### Camera Properties
```
Clear Flags: Skybox
Field of View: 60
Clipping Planes: Near 0.3, Far 1000
Allow HDR: true
Allow MSAA: true
```

#### CameraController Properties (if used)
```
Movement Speed: 10
Rotation Speed: 100
Zoom Speed: 5
Follow Target: [Set at runtime or reference player]
Camera Mode: Free / Follow / Fixed
```

---

### 4. Directional Light

#### GameObject Setup
```
Name: "Directional Light"
Tag: Untagged
Layer: Default
Position: (0, 10, 0)
Rotation: (50, -30, 0)
```

#### Components List
| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `Light` | Scene lighting | Type: Directional, Intensity: 1, Color: White |

---

### 5. UI Canvas

#### GameObject Setup
```
Name: "UI Canvas"
Tag: Untagged
Layer: UI
```

#### Components List
| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `Canvas` | UI container | Render Mode: Screen Space - Overlay |
| `CanvasScaler` | Resolution scaling | UI Scale Mode: Scale With Screen Size |
| `GraphicRaycaster` | UI input detection | Default |

#### CanvasScaler Properties
```
Reference Resolution: 1920x1080
Screen Match Mode: Match Width Or Height
Match: 0.5
```

#### Standard UI Children (All Scenes)
```
UI Canvas
├── DebugPanel (Top-Left)
├── PerformancePanel (Top-Right)
├── SceneControlsPanel (Bottom-Center)
└── [Scene-Specific UI]
```

---

### 6. EventSystem

#### GameObject Setup
```
Name: "EventSystem"
Tag: Untagged
Layer: Default
```

#### Components List
| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `EventSystem` | Manages UI events | Default |
| `StandaloneInputModule` | Input handling | Default |

---

### Standard UI Panels (All Scenes)

#### DebugPanel (Top-Left)

**Hierarchy**
```
DebugPanel
├── Background (Image)
├── FPSText (TextMeshPro)
├── EntityCountText (TextMeshPro)
├── SystemStatsText (TextMeshPro)
└── MemoryUsageText (TextMeshPro)
```

**Components**
- `DebugPanelController` - Updates all text fields
- `CanvasGroup` - For show/hide functionality

**Layout**
- Anchor: Top-Left
- Position: (10, -10, 0)
- Size: (300, 150)

---

#### PerformancePanel (Top-Right)

**Hierarchy**
```
PerformancePanel
├── Background (Image)
├── FrameTimeGraph (Image - custom graph)
├── JobTimeText (TextMeshPro)
├── AllocationText (TextMeshPro)
└── WarningIcon (Image - shows when performance issues)
```

**Components**
- `PerformanceMonitor` - Tracks frame times
- `GraphRenderer` - Draws performance graph

**Layout**
- Anchor: Top-Right
- Position: (-10, -10, 0)
- Size: (350, 200)

---

#### SceneControlsPanel (Bottom-Center)

**Hierarchy**
```
SceneControlsPanel
├── Background (Image)
├── PauseButton (Button + TextMeshPro)
├── RestartButton (Button + TextMeshPro)
├── TimeScaleSlider (Slider + Label)
└── SettingsButton (Button + TextMeshPro)
```

**Components**
- `SceneControlsController` - Handles button events

**Layout**
- Anchor: Bottom-Center
- Position: (0, 10, 0)
- Size: (600, 80)

---

## Action Genre Scenes

### Scene Types Covered
- FPS (First-Person Shooter)
- ThirdPersonShooter
- Fighting
- BeatEmUp
- HackAndSlash
- Stealth
- SurvivalHorror

---

### FPS Demo Scene

#### Scene Name
`DemoScene_FPS`

---

#### Complete GameObject Hierarchy
```
DemoScene_FPS
├── ChimeraSceneBootstrap ⭐
├── WorldManager
├── Main Camera (FPS)
│   └── Weapon Camera (child)
├── Directional Light
├── UI Canvas
│   ├── DebugPanel
│   ├── PerformancePanel
│   ├── SceneControlsPanel
│   ├── FPS_HUD
│   ├── FPS_PreActivityPanel
│   └── FPS_ResultsPanel
├── EventSystem
│
├── ActivityCenter
│   ├── ActivityCenterManager
│   └── FPSActivityAuthoring
│
├── PlayerController
│   ├── FPSController
│   ├── PlayerInputHandler
│   ├── PlayerSkillTracker
│   └── PartnershipController
│
├── ChimeraPartner
│   ├── EnhancedCreatureAuthoring
│   └── ChimeraCombatAI
│
├── CombatArena
│   ├── ArenaFloor
│   ├── Walls (parent)
│   │   ├── Wall_North
│   │   ├── Wall_South
│   │   ├── Wall_East
│   │   └── Wall_West
│   ├── SpawnPoints (parent)
│   │   ├── SpawnPoint_1
│   │   ├── SpawnPoint_2
│   │   └── [8 more spawn points]
│   ├── CoverObjects (parent)
│   │   ├── Crate_1
│   │   ├── Crate_2
│   │   └── [10 more cover objects]
│   └── Pickups (parent)
│       ├── HealthPack_1
│       ├── HealthPack_2
│       └── AmmoPack_1
│
├── EnemyManager
│   ├── EnemySpawnerAuthoring
│   └── DifficultyController
│
└── Audio
    ├── BackgroundMusic
    └── CombatAmbience
```

---

#### GameObject Details - ActivityCenter

**Name**: `ActivityCenter`

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `ActivityCenterManager` | MonoBehaviour | Activity Config (see below) |
| `ActivityCenterAuthoring` | IConvertGameObjectToEntity | Activity Type: FPS |
| `ConvertToEntity` | Hybrid Component | Convert and Destroy |

**ActivityCenterManager Properties**
```
Activity Type: FPS
Available Difficulties: [Easy, Normal, Hard, Expert, Master]
Genre Config: Genre_FPS.asset
Activity Config: Activity_FPS.asset
Time Limit: 300 seconds (5 minutes)
Success Threshold: 50%
```

**Required ScriptableObjects**
1. **Genre_FPS.asset** (`GenreConfiguration`)
   ```
   Genre Type: FPS
   Display Name: "First-Person Shooter"
   Primary Player Skill: Reflexes
   Primary Chimera Trait: Aggression
   Base Duration: 300s
   Difficulty Scaling: 1.5
   Score Multiplier: 1.2
   Player Skill Weight: 0.7
   Chimera Trait Weight: 0.3
   ```

2. **Activity_FPS.asset** (`ActivityConfig`)
   ```
   Activity Type: FPS
   Enemy Count: 30
   Enemy Types: [Basic, Armored, Fast]
   Weapon Loadout: [Pistol, Rifle, Shotgun]
   ```

---

#### GameObject Details - PlayerController

**Name**: `PlayerController`

**Transform**
```
Position: (0, 1.8, 0)
Rotation: (0, 0, 0)
```

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `CharacterController` | Unity Component | Height: 1.8, Radius: 0.4 |
| `FPSController` | MonoBehaviour | See properties below |
| `PlayerInputHandler` | MonoBehaviour | Input Asset: PlayerInputActions |
| `PlayerSkillTracker` | MonoBehaviour | Tracked Skill: Reflexes |
| `PartnershipController` | MonoBehaviour | Partner: [Set at runtime] |
| `PlayerHealthComponent` | MonoBehaviour | Max Health: 100, Current: 100 |

**FPSController Properties**
```
Movement Speed: 7.0
Sprint Speed: 10.0
Jump Force: 5.0
Mouse Sensitivity: 2.0
Camera Smoothing: 0.1
Weapon Slots: 3
```

**Required ScriptableObjects**
- **PlayerInputActions.inputactions** (Unity Input System asset)

---

#### GameObject Details - ChimeraPartner

**Name**: `ChimeraPartner`

**Transform**
```
Position: (2, 0, 0) [spawns near player]
Rotation: (0, 0, 0)
```

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `EnhancedCreatureAuthoring` | IConvertGameObjectToEntity | See properties below |
| `ChimeraCombatAI` | MonoBehaviour | Combat Role: Support |
| `CreatureHealthComponent` | ECS Component | Max Health: 150 |
| `ConvertToEntity` | Hybrid Component | Convert and Destroy |

**EnhancedCreatureAuthoring Properties**
```
Species Config: FireDragon.asset
Age: 120 days (adult)
Initial Bond Strength: 0.75
Personality Traits: ["Brave", "Aggressive", "Loyal"]
Combat Style: Ranged Support
```

**Required ScriptableObjects**
- **FireDragon.asset** (`ChimeraSpeciesConfig`)

---

#### GameObject Details - EnemyManager

**Name**: `EnemyManager`

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `EnemySpawnerAuthoring` | IConvertGameObjectToEntity | See properties below |
| `DifficultyController` | MonoBehaviour | Current Difficulty: Normal |
| `ConvertToEntity` | Hybrid Component | Convert and Destroy |

**EnemySpawnerAuthoring Properties**
```
Enemy Prefab: Enemy_Basic.prefab
Max Concurrent Enemies: 10
Spawn Interval: 5 seconds
Total Enemy Budget: 30
Spawn Mode: Wave-based
```

**DifficultyController Properties**
```
Difficulty: Normal
Enemy Health Multiplier: 1.0
Enemy Damage Multiplier: 1.0
Player Damage Multiplier: 1.0
```

---

#### UI Details - FPS_HUD

**Hierarchy**
```
FPS_HUD
├── Crosshair (Image)
├── HealthBar
│   ├── Background (Image)
│   ├── FillBar (Image - red)
│   └── HealthText (TextMeshPro)
├── ChimeraHealthBar
│   ├── Background (Image)
│   ├── FillBar (Image - green)
│   └── HealthText (TextMeshPro)
├── AmmoCounter
│   ├── CurrentAmmo (TextMeshPro - large)
│   └── ReserveAmmo (TextMeshPro - small)
├── WeaponDisplay
│   ├── WeaponIcon (Image)
│   └── WeaponName (TextMeshPro)
├── TimerDisplay
│   ├── Icon (Image)
│   └── TimeText (TextMeshPro)
├── ScoreDisplay
│   ├── Label (TextMeshPro)
│   └── ScoreText (TextMeshPro)
├── KillCounter
│   ├── Icon (Image)
│   └── CountText (TextMeshPro)
├── PerformanceBar
│   ├── Background (Image)
│   ├── FillBar (Image - gradient)
│   └── PercentText (TextMeshPro)
├── CooperationIndicator
│   ├── BondIcon (Image)
│   ├── BondStrength (TextMeshPro)
│   └── BonusText (TextMeshPro)
└── HitIndicators (parent)
    ├── HitLeft (Image)
    ├── HitRight (Image)
    ├── HitTop (Image)
    └── HitBottom (Image)
```

**Components**
- `FPSHUDController` - Updates all HUD elements
- `CanvasGroup` - For fade in/out

**Layout Specifications**

| Element | Anchor | Position | Size |
|---------|--------|----------|------|
| Crosshair | Center | (0, 0) | (32, 32) |
| HealthBar | Bottom-Left | (20, 120) | (300, 40) |
| ChimeraHealthBar | Bottom-Left | (20, 70) | (300, 40) |
| AmmoCounter | Bottom-Right | (-120, 80) | (200, 80) |
| WeaponDisplay | Bottom-Right | (-120, 170) | (200, 60) |
| TimerDisplay | Top-Center | (0, -20) | (200, 50) |
| ScoreDisplay | Top-Center | (0, -80) | (250, 50) |
| PerformanceBar | Right-Center | (-20, 0) | (40, 300) |
| CooperationIndicator | Bottom-Center | (0, 20) | (400, 60) |

---

#### UI Details - FPS_PreActivityPanel

**Hierarchy**
```
FPS_PreActivityPanel
├── BackgroundOverlay (Image - semi-transparent black)
├── ContentPanel (parent)
│   ├── Background (Image)
│   ├── TitleText (TextMeshPro) "First-Person Shooter"
│   ├── DescriptionText (TextMeshPro)
│   ├── DifficultySection
│   │   ├── Label (TextMeshPro) "Select Difficulty:"
│   │   ├── EasyButton (Button)
│   │   ├── NormalButton (Button)
│   │   ├── HardButton (Button)
│   │   ├── ExpertButton (Button)
│   │   └── MasterButton (Button)
│   ├── ChimeraSelectionSection
│   │   ├── Label (TextMeshPro) "Select Partner:"
│   │   ├── ChimeraList (ScrollView)
│   │   │   └── Content (parent)
│   │   │       ├── ChimeraSlot_1
│   │   │       ├── ChimeraSlot_2
│   │   │       └── ChimeraSlot_3
│   │   └── SelectedChimeraPreview
│   │       ├── Portrait (RawImage)
│   │       ├── NameText (TextMeshPro)
│   │       ├── LevelText (TextMeshPro)
│   │       ├── BondStrengthBar
│   │       └── StatsPanel
│   │           ├── AggressionStat
│   │           ├── HealthStat
│   │           └── CombatPowerStat
│   ├── RewardsSection
│   │   ├── Label (TextMeshPro) "Expected Rewards:"
│   │   ├── CurrencyReward (HorizontalGroup)
│   │   ├── SkillGainReward (HorizontalGroup)
│   │   └── BondGainReward (HorizontalGroup)
│   ├── ObjectivesSection
│   │   ├── Label (TextMeshPro) "Objectives:"
│   │   ├── Objective1 (TextMeshPro) "• Eliminate 30 enemies"
│   │   ├── Objective2 (TextMeshPro) "• Survive for 5 minutes"
│   │   └── Objective3 (TextMeshPro) "• Maintain 50%+ performance"
│   └── ButtonGroup
│       ├── StartButton (Button - green)
│       └── CancelButton (Button - red)
```

**Components**
- `PreActivityPanelController` - Manages panel logic
- `DifficultySelector` - Handles difficulty selection
- `ChimeraSelector` - Manages chimera selection
- `CanvasGroup` - For fade animations

---

#### UI Details - FPS_ResultsPanel

**Hierarchy**
```
FPS_ResultsPanel
├── BackgroundOverlay (Image)
├── ContentPanel
│   ├── Background (Image)
│   ├── TitleText (TextMeshPro) "Mission Complete!"
│   ├── RankDisplay
│   │   ├── RankIcon (Image - Bronze/Silver/Gold/Platinum)
│   │   └── RankText (TextMeshPro)
│   ├── PerformanceSection
│   │   ├── Label (TextMeshPro) "Performance Breakdown:"
│   │   ├── FinalPerformanceText (TextMeshPro) "87%"
│   │   ├── PlayerPerformanceRow
│   │   │   ├── Label (TextMeshPro) "Your Performance:"
│   │   │   ├── Bar (Image)
│   │   │   └── Value (TextMeshPro) "85%"
│   │   ├── ChimeraContributionRow
│   │   │   ├── Label (TextMeshPro) "Partner Contribution:"
│   │   │   ├── Bar (Image)
│   │   │   └── Value (TextMeshPro) "92%"
│   │   ├── BondMultiplierRow
│   │   │   ├── Label (TextMeshPro) "Bond Multiplier:"
│   │   │   └── Value (TextMeshPro) "x1.15"
│   │   └── AgeFactorRow
│   │       ├── Label (TextMeshPro) "Age Factor:"
│   │       └── Value (TextMeshPro) "x1.0"
│   ├── StatsSection
│   │   ├── Label (TextMeshPro) "Mission Stats:"
│   │   ├── EnemiesKilledRow
│   │   │   ├── Icon (Image)
│   │   │   ├── Label (TextMeshPro) "Enemies Eliminated:"
│   │   │   └── Value (TextMeshPro) "30/30"
│   │   ├── AccuracyRow
│   │   │   ├── Icon (Image)
│   │   │   ├── Label (TextMeshPro) "Accuracy:"
│   │   │   └── Value (TextMeshPro) "78%"
│   │   ├── TimeRow
│   │   │   ├── Icon (Image)
│   │   │   ├── Label (TextMeshPro) "Time Survived:"
│   │   │   └── Value (TextMeshPro) "5:00"
│   │   └── DamageTakenRow
│   │       ├── Icon (Image)
│   │       ├── Label (TextMeshPro) "Damage Taken:"
│   │       └── Value (TextMeshPro) "45"
│   ├── RewardsSection
│   │   ├── Label (TextMeshPro) "Rewards Earned:"
│   │   ├── CurrencyReward
│   │   │   ├── Icon (Image)
│   │   │   ├── Label (TextMeshPro) "Currency:"
│   │   │   └── Value (TextMeshPro) "+187 coins"
│   │   ├── SkillGainReward
│   │   │   ├── Icon (Image)
│   │   │   ├── Label (TextMeshPro) "Reflexes Skill:"
│   │   │   └── Value (TextMeshPro) "+0.0187"
│   │   └── BondGainReward
│   │       ├── Icon (Image)
│   │       ├── Label (TextMeshPro) "Bond Strength:"
│   │       └── Value (TextMeshPro) "+0.0087"
│   ├── NewRecordsSection (if applicable)
│   │   ├── Label (TextMeshPro) "New Records!"
│   │   └── RecordText (TextMeshPro) "• New Best Kill Count!"
│   ├── PartnershipChangeSection
│   │   ├── Label (TextMeshPro) "Partnership Changes:"
│   │   ├── BondChangeRow
│   │   │   ├── Icon (Image)
│   │   │   └── Text (TextMeshPro) "Bond: 75% → 78% (+3%)"
│   │   └── EmotionalImpactRow
│   │       ├── Icon (Image)
│   │       └── Text (TextMeshPro) "Mood: Happy"
│   └── ButtonGroup
│       ├── RetryButton (Button) "Retry"
│       ├── NextDifficultyButton (Button) "Next Difficulty"
│       └── ExitButton (Button) "Exit to Menu"
```

**Components**
- `ResultsPanelController` - Displays results
- `ResultsAnimator` - Animates number counting
- `CanvasGroup` - For fade in/out

---

#### Setup Steps (FPS Scene)

**Step 1: Create Base Scene**
1. Create new scene: `DemoScene_FPS`
2. Delete default camera and light
3. Add Core Scene Template objects (bootstrap, camera, UI, etc.)

**Step 2: Setup ActivityCenter**
1. Create empty GameObject: `ActivityCenter`
2. Add `ActivityCenterManager` component
3. Add `ActivityCenterAuthoring` component
4. Create `Genre_FPS.asset` in `Assets/_Project/Resources/Configs/GenreConfigurations/`
5. Create `Activity_FPS.asset` in `Assets/_Project/Resources/Configs/Activities/`
6. Assign both assets to ActivityCenterManager

**Step 3: Setup PlayerController**
1. Create empty GameObject: `PlayerController` at position (0, 1.8, 0)
2. Add `CharacterController` component
3. Add `FPSController` component
4. Add `PlayerInputHandler` component
5. Add `PlayerSkillTracker` component
6. Add `PartnershipController` component
7. Add `PlayerHealthComponent` component
8. Configure all properties as specified

**Step 4: Setup ChimeraPartner**
1. Create empty GameObject: `ChimeraPartner` at position (2, 0, 0)
2. Add `EnhancedCreatureAuthoring` component
3. Assign `FireDragon.asset` to Species Config
4. Add `ChimeraCombatAI` component
5. Add `ConvertToEntity` component

**Step 5: Setup Combat Arena**
1. Create parent GameObject: `CombatArena`
2. Create floor plane: `ArenaFloor` (scale 50x1x50)
3. Create `Walls` parent and 4 wall objects
4. Create `SpawnPoints` parent and 10 spawn point markers
5. Create `CoverObjects` parent and 12 cover crates
6. Create `Pickups` parent and health/ammo pickups

**Step 6: Setup EnemyManager**
1. Create empty GameObject: `EnemyManager`
2. Add `EnemySpawnerAuthoring` component
3. Add `DifficultyController` component
4. Create enemy prefab: `Enemy_Basic.prefab`
5. Assign prefab to EnemySpawnerAuthoring

**Step 7: Setup UI**
1. Create `FPS_HUD` hierarchy under UI Canvas
2. Create all child objects as specified
3. Add `FPSHUDController` component
4. Create `FPS_PreActivityPanel` hierarchy
5. Add `PreActivityPanelController` component
6. Create `FPS_ResultsPanel` hierarchy
7. Add `ResultsPanelController` component
8. Set all anchors, positions, and sizes

**Step 8: Setup Audio**
1. Create `Audio` parent GameObject
2. Add `BackgroundMusic` AudioSource
3. Add `CombatAmbience` AudioSource
4. Assign audio clips

**Step 9: Final Configuration**
1. Verify all ScriptableObject references are assigned
2. Check all component properties
3. Test scene initialization
4. Verify UI responds correctly

---

#### Testing Checklist (FPS Scene)

**Initialization Tests**
- [ ] Scene loads without errors
- [ ] ChimeraSceneBootstrap initializes all systems
- [ ] ECS World is created
- [ ] All ScriptableObjects load correctly
- [ ] UI Canvas renders properly
- [ ] Event System is active

**Gameplay Tests**
- [ ] Player controller responds to input
- [ ] Camera movement works correctly
- [ ] Chimera partner spawns at correct position
- [ ] Enemies spawn at designated spawn points
- [ ] Combat system registers hits
- [ ] Health bars update correctly
- [ ] Ammo counter updates on fire
- [ ] Score increases on kills

**Activity System Tests**
- [ ] Pre-activity panel displays correctly
- [ ] Difficulty selection works
- [ ] Chimera selection works
- [ ] Activity starts on "Start" button
- [ ] Timer counts down correctly
- [ ] Performance is calculated in real-time
- [ ] Activity ends when timer reaches 0 or all enemies defeated
- [ ] Results panel displays correctly

**Partnership Tests**
- [ ] Bond strength affects cooperation bonus
- [ ] Chimera AI supports player in combat
- [ ] Partnership values update after activity
- [ ] Emotional state changes based on performance

**Performance Tests**
- [ ] Maintains 60 FPS with 10 concurrent enemies
- [ ] No memory leaks
- [ ] No excessive GC allocations
- [ ] ECS systems process efficiently

**UI Tests**
- [ ] All HUD elements display correctly
- [ ] Text is readable and properly formatted
- [ ] Buttons respond to clicks
- [ ] Panels fade in/out smoothly
- [ ] Results panel animates numbers correctly

---

### Third-Person Shooter Demo Scene

#### Scene Name
`DemoScene_ThirdPersonShooter`

#### Key Differences from FPS
- Camera positioned behind player (offset: 0, 2, -5)
- Full player character model visible
- Cover system more prominent
- Aiming reticle instead of crosshair

#### Unique Components
- `ThirdPersonController` (instead of FPSController)
- `CoverSystemController`
- `ThirdPersonCameraController`

#### Unique UI Elements
- Cover indicator (shows when near cover)
- Aim-down-sights overlay
- Character stance indicator (standing/crouching/prone)

---

### Fighting Game Demo Scene

#### Scene Name
`DemoScene_Fighting`

#### Complete GameObject Hierarchy
```
DemoScene_Fighting
├── ChimeraSceneBootstrap ⭐
├── WorldManager
├── Main Camera (side view, fixed position)
├── Directional Light
├── UI Canvas
│   ├── DebugPanel
│   ├── PerformancePanel
│   ├── SceneControlsPanel
│   ├── Fighting_HUD
│   ├── Fighting_PreActivityPanel
│   └── Fighting_ResultsPanel
├── EventSystem
│
├── ActivityCenter
│   └── FightingActivityAuthoring
│
├── FightingArena
│   ├── Arena Floor
│   ├── Arena Boundaries
│   └── Corner Posts (4)
│
├── Player1_Controller
│   ├── FighterController
│   ├── ComboSystem
│   └── FighterAnimator
│
├── Player1_Chimera
│   ├── EnhancedCreatureAuthoring
│   └── ChimeraFightingAI
│
├── Player2_Controller (AI or second player)
│   └── AIFighterController
│
├── Player2_Chimera
│
└── Audio
    ├── AnnouncerAudio
    └── CrowdAmbience
```

#### GameObject Details - FightingArena

**Name**: `FightingArena`

**Children**
```
FightingArena
├── ArenaFloor (Plane, 10x10)
├── ArenaBoundaries (parent)
│   ├── Boundary_Left (BoxCollider, wall)
│   ├── Boundary_Right (BoxCollider, wall)
│   ├── Boundary_Front (BoxCollider, wall)
│   └── Boundary_Back (BoxCollider, wall)
└── CornerPosts (parent)
    ├── Post_TopLeft
    ├── Post_TopRight
    ├── Post_BottomLeft
    └── Post_BottomRight
```

#### GameObject Details - Player1_Controller

**Name**: `Player1_Controller`

**Transform**
```
Position: (-3, 0, 0)
Rotation: (0, 90, 0) [facing right]
```

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `CharacterController` | Unity Component | Height: 1.8, Radius: 0.5 |
| `FighterController` | MonoBehaviour | Move Speed: 5.0, Jump Force: 8.0 |
| `ComboSystem` | MonoBehaviour | Max Combo Length: 8 |
| `FighterAnimator` | MonoBehaviour | Animator: FighterAnimController |
| `PlayerInputHandler` | MonoBehaviour | Input Asset: FightingInputActions |
| `FighterHealthComponent` | MonoBehaviour | Max Health: 100, Current: 100 |
| `PlayerSkillTracker` | MonoBehaviour | Tracked Skill: Reflexes |
| `PartnershipController` | MonoBehaviour | Partner: Player1_Chimera |

**FighterController Properties**
```
Move Speed: 5.0
Jump Force: 8.0
Air Control: 0.3
Block Damage Reduction: 0.7
Dash Speed: 15.0
Dash Duration: 0.2
Dash Cooldown: 1.0
```

**ComboSystem Properties**
```
Max Combo Length: 8
Combo Timeout: 1.0 seconds
Special Move Count: 5
Super Move Count: 2
Combo List: ComboList_Default.asset
```

**Required ScriptableObjects**
- **ComboList_Default.asset** (`ComboListConfig`)
- **FightingInputActions.inputactions** (Unity Input System)

#### UI Details - Fighting_HUD

**Hierarchy**
```
Fighting_HUD
├── Player1_Section (Left side)
│   ├── HealthBar
│   │   ├── Background (Image)
│   │   ├── FillBar (Image - red, fills left-to-right)
│   │   └── HealthText (TextMeshPro)
│   ├── ChimeraHealthBar
│   │   ├── Background (Image)
│   │   ├── FillBar (Image - green)
│   │   └── HealthText (TextMeshPro)
│   ├── ComboCounter
│   │   ├── Label (TextMeshPro) "COMBO"
│   │   └── CountText (TextMeshPro - large, animated)
│   ├── SpecialMeter
│   │   ├── Background (Image)
│   │   ├── FillBar (Image - yellow, fills bottom-to-top)
│   │   └── Segments (parent - 5 segments)
│   └── PlayerPortrait
│       └── PortraitImage (RawImage)
│
├── Player2_Section (Right side - mirrored layout)
│   └── [Same structure as Player1, mirrored]
│
├── CenterSection
│   ├── TimerDisplay
│   │   ├── Background (Image)
│   │   └── TimeText (TextMeshPro - large, centered)
│   ├── RoundIndicator
│   │   ├── RoundText (TextMeshPro) "ROUND 1"
│   │   └── WinIndicators (parent)
│   │       ├── P1_Win1 (Image - circle, filled if P1 won round)
│   │       ├── P1_Win2 (Image)
│   │       ├── P2_Win1 (Image)
│   │       └── P2_Win2 (Image)
│   └── AnnouncementText (TextMeshPro - large, center)
│       [Shows "FIGHT!", "K.O.!", "PERFECT!", etc.]
│
└── PerformanceIndicator (Bottom-Center)
    ├── CooperationBar (horizontal bar)
    └── BondBonusText (TextMeshPro)
```

**Components**
- `FightingHUDController` - Updates all HUD elements
- `ComboAnimator` - Animates combo counter
- `AnnouncementAnimator` - Shows fight announcements
- `CanvasGroup` - For fade effects

**Layout Specifications**

| Element | Anchor | Position | Size |
|---------|--------|----------|------|
| Player1 HealthBar | Top-Left | (20, -20) | (500, 50) |
| Player1 ChimeraHealthBar | Top-Left | (20, -80) | (400, 35) |
| Player1 ComboCounter | Left-Center | (30, 0) | (150, 150) |
| Player1 SpecialMeter | Bottom-Left | (40, 40) | (60, 300) |
| Player2 Section | Mirrored on right | | |
| TimerDisplay | Top-Center | (0, -30) | (200, 80) |
| RoundIndicator | Top-Center | (0, -120) | (300, 50) |
| AnnouncementText | Center | (0, 0) | (800, 200) |
| PerformanceIndicator | Bottom-Center | (0, 30) | (600, 60) |

---

#### Setup Steps (Fighting Scene)

**Step 1-2**: Same as FPS scene (base scene + ActivityCenter)

**Step 3: Setup Fighting Arena**
1. Create parent: `FightingArena`
2. Add floor plane (10x10)
3. Create boundary colliders on all 4 sides
4. Add corner post visuals

**Step 4: Setup Player1**
1. Create `Player1_Controller` at (-3, 0, 0)
2. Add all fighter components
3. Create fighter model/placeholder
4. Setup animator controller

**Step 5: Setup Player1 Chimera**
1. Create `Player1_Chimera` near Player1
2. Add chimera authoring components
3. Configure fighting AI

**Step 6: Setup Player2** (AI opponent)
1. Duplicate Player1 setup
2. Replace PlayerInputHandler with AIFighterController
3. Position at (3, 0, 0) facing left

**Step 7: Setup Camera**
1. Position camera for side view: (0, 3, -12)
2. Rotation: (10, 0, 0)
3. Add camera tracking script (keeps both fighters in frame)

**Step 8: Setup Fighting HUD**
1. Create all HUD elements as specified
2. Mirror Player2 section
3. Add HUD controller scripts
4. Configure animations

**Step 9: Setup Audio**
1. Add announcer audio source
2. Add crowd ambience
3. Add impact sound effects

---

#### Testing Checklist (Fighting Scene)

**Initialization Tests**
- [ ] Scene loads without errors
- [ ] Both fighters spawn correctly
- [ ] Chimera partners spawn
- [ ] Camera frames both fighters
- [ ] HUD displays correctly

**Combat Tests**
- [ ] Light attacks work (punch/kick)
- [ ] Heavy attacks work
- [ ] Blocking reduces damage
- [ ] Combos chain correctly
- [ ] Special moves execute
- [ ] Super moves execute (when meter full)
- [ ] Grabs/throws work
- [ ] Jumping attacks work
- [ ] Air combos work

**Chimera Partnership Tests**
- [ ] Chimera provides assist attacks
- [ ] Cooperation affects special meter fill rate
- [ ] Bond strength shown in UI
- [ ] Partnership bonus applied to damage

**Round System Tests**
- [ ] Round starts with "FIGHT!" announcement
- [ ] Round ends when fighter reaches 0 health
- [ ] K.O. announcement displays
- [ ] Round indicator updates
- [ ] Best of 3 rounds works
- [ ] Winner determined correctly

**UI Tests**
- [ ] Health bars deplete correctly
- [ ] Combo counter displays and animates
- [ ] Special meter fills on attacks
- [ ] Timer counts down
- [ ] Round indicators update
- [ ] Announcements animate correctly

**Performance Tests**
- [ ] Maintains 60 FPS during intense combat
- [ ] No hitching on special moves
- [ ] Animations blend smoothly

---

## Racing Genre Scene

### Scene Name
`DemoScene_Racing`

### Complete GameObject Hierarchy
```
DemoScene_Racing
├── ChimeraSceneBootstrap ⭐
├── WorldManager
├── Main Camera (Chase Camera)
├── Directional Light
├── UI Canvas
│   ├── DebugPanel
│   ├── PerformancePanel
│   ├── SceneControlsPanel
│   ├── Racing_HUD
│   ├── Racing_PreActivityPanel
│   └── Racing_ResultsPanel
├── EventSystem
│
├── ActivityCenter
│   └── RacingActivityAuthoring
│
├── RaceTrack
│   ├── TrackMesh (3D model or procedural)
│   ├── StartingGrid (parent)
│   │   ├── StartPosition_1
│   │   ├── StartPosition_2
│   │   ├── StartPosition_3
│   │   └── [5 more positions]
│   ├── Checkpoints (parent)
│   │   ├── Checkpoint_1 (trigger)
│   │   ├── Checkpoint_2 (trigger)
│   │   └── [8 more checkpoints]
│   ├── FinishLine (trigger)
│   ├── TrackBoundaries (parent)
│   │   ├── Barrier_1 (collider)
│   │   └── [100 more barriers]
│   └── BoostPads (parent)
│       ├── BoostPad_1
│       └── [10 more boost pads]
│
├── PlayerVehicle
│   ├── VehicleController
│   ├── VehiclePhysics
│   ├── ChimeraPassengerSlot
│   └── EngineAudioSource
│
├── ChimeraPassenger
│   ├── EnhancedCreatureAuthoring
│   └── ChimeraCopilotAI
│
├── AIRacers (parent)
│   ├── AIRacer_1
│   │   ├── VehicleController
│   │   ├── AIRacingController
│   │   └── ChimeraPassenger
│   ├── AIRacer_2
│   └── [5 more AI racers]
│
├── CheckpointSystem
│   └── RaceProgressTracker
│
└── Audio
    ├── BackgroundMusic
    └── CrowdAmbience
```

---

### GameObject Details - RaceTrack

**Name**: `RaceTrack`

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `RaceTrackManager` | MonoBehaviour | Track Length: 2500m, Lap Count: 3 |
| `TrackMeshController` | MonoBehaviour | Track Material, Minimap Texture |

**Children Details**

**StartingGrid**
```
StartingGrid (parent at start line)
├── StartPosition_1 (Transform marker)
│   Position: (0, 0, 0)
│   Index: 1
├── StartPosition_2
│   Position: (4, 0, 0)
│   Index: 2
├── [Positions 3-8 in grid formation]
```

**Checkpoints**
```
Checkpoints (parent)
├── Checkpoint_1
│   ├── BoxCollider (Is Trigger: true, Size: 20x5x1)
│   └── CheckpointTrigger component
│       Checkpoint Index: 1
│       Next Checkpoint: Checkpoint_2
├── Checkpoint_2
│   [Same structure, Index: 2]
├── [Checkpoints 3-9]
└── Checkpoint_10 (before finish)
```

**FinishLine**
```
FinishLine
├── BoxCollider (Is Trigger: true)
└── FinishLineTrigger component
    Required Checkpoints: 10
    Lap Increment: true
```

**BoostPads**
```
BoostPads (parent)
├── BoostPad_1
│   ├── MeshRenderer (glowing pad visual)
│   ├── BoxCollider (Is Trigger: true)
│   ├── ParticleSystem (boost effect)
│   └── BoostPadTrigger component
│       Boost Multiplier: 1.5
│       Boost Duration: 2.0 seconds
├── [BoostPads 2-10]
```

---

### GameObject Details - PlayerVehicle

**Name**: `PlayerVehicle`

**Transform**
```
Position: [Set by StartPosition_1 at race start]
Rotation: (0, 0, 0)
```

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `Rigidbody` | Unity Component | Mass: 1000, Drag: 0.5, Angular Drag: 2 |
| `VehicleController` | MonoBehaviour | See properties below |
| `VehiclePhysics` | MonoBehaviour | See properties below |
| `PlayerInputHandler` | MonoBehaviour | Input Asset: RacingInputActions |
| `PlayerSkillTracker` | MonoBehaviour | Tracked Skill: Reflexes |
| `PartnershipController` | MonoBehaviour | Partner: ChimeraPassenger |
| `ChimeraPassengerSlot` | MonoBehaviour | Passenger Position |
| `AudioSource` | Unity Component | Engine sounds |

**VehicleController Properties**
```
Max Speed: 200 km/h
Acceleration: 15.0
Braking Force: 25.0
Turning Speed: 3.0
Drift Factor: 0.8
Handling: 0.7
```

**VehiclePhysics Properties**
```
Wheel Count: 4
Wheel Colliders: [FL, FR, RL, RR]
Suspension Stiffness: 50000
Suspension Damping: 4500
Tire Friction: 1.0
Anti-Roll Bar Force: 5000
```

**Required Assets**
- **VehicleModel.fbx** (3D model)
- **RacingInputActions.inputactions** (Input System)
- **EngineSound.wav** (audio clip)

---

### GameObject Details - ChimeraPassenger

**Name**: `ChimeraPassenger`

**Transform**
```
Position: [Child of PlayerVehicle, passenger seat]
Rotation: (0, 0, 0)
```

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `EnhancedCreatureAuthoring` | IConvertGameObjectToEntity | Species: Any |
| `ChimeraCopilotAI` | MonoBehaviour | See properties below |
| `ConvertToEntity` | Hybrid Component | Convert and Destroy |

**ChimeraCopilotAI Properties**
```
Cooperation Style: Active Copilot
Abilities:
  - Call Boost (increases speed temporarily)
  - Call Shortcut (reveals optimal racing line)
  - Call Defense (shields from collisions)
  - Call Recovery (faster recovery from crashes)

Ability Cooldowns: 10 seconds each
Cooperation Threshold: 0.5 (minimum bond to use abilities)
```

**Required ScriptableObjects**
- **Species Config**: Any chimera species

---

### GameObject Details - CheckpointSystem

**Name**: `CheckpointSystem`

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `RaceProgressTracker` | MonoBehaviour | See properties below |

**RaceProgressTracker Properties**
```
Total Racers: 8
Total Laps: 3
Total Checkpoints: 10
Racer Positions: Dictionary<Racer, RacePosition>
Lap Times: List<float>
Best Lap Time: float
Current Race Time: float
```

**Tracked Data Per Racer**
```csharp
public struct RacePosition
{
    public int CurrentLap;
    public int CurrentCheckpoint;
    public int OverallPosition; // 1st, 2nd, etc.
    public float DistanceToNextCheckpoint;
    public float TotalDistance;
    public float CurrentLapTime;
    public float BestLapTime;
}
```

---

### UI Details - Racing_HUD

**Hierarchy**
```
Racing_HUD
├── Speedometer (Bottom-Left)
│   ├── Background (Image - circular gauge)
│   ├── Needle (Image - rotates based on speed)
│   ├── SpeedText (TextMeshPro - large) "195"
│   └── UnitText (TextMeshPro - small) "km/h"
│
├── LapCounter (Top-Left)
│   ├── Background (Image)
│   ├── LapText (TextMeshPro) "LAP"
│   └── LapNumberText (TextMeshPro) "2/3"
│
├── LapTimer (Top-Center)
│   ├── Background (Image)
│   ├── CurrentLapLabel (TextMeshPro) "Current Lap:"
│   ├── CurrentLapTime (TextMeshPro - large) "1:23.456"
│   ├── BestLapLabel (TextMeshPro) "Best Lap:"
│   └── BestLapTime (TextMeshPro) "1:21.234"
│
├── PositionIndicator (Top-Right)
│   ├── Background (Image)
│   ├── PositionText (TextMeshPro - very large) "2"
│   ├── SuffixText (TextMeshPro) "nd"
│   └── TotalRacersText (TextMeshPro) "/8"
│
├── Minimap (Bottom-Right)
│   ├── Background (Image - circular or square)
│   ├── TrackImage (RawImage - shows track layout)
│   ├── PlayerDot (Image - red)
│   ├── OpponentDots (parent)
│   │   ├── Opponent1Dot (Image - blue)
│   │   └── [7 more opponent dots]
│   └── CheckpointMarkers (parent)
│       ├── NextCheckpoint (Image - green)
│       └── FutureCheckpoints (Images - gray)
│
├── BoostMeter (Left-Center)
│   ├── Background (Image)
│   ├── FillBar (Image - yellow, fills bottom-to-top)
│   └── BoostText (TextMeshPro) "BOOST"
│
├── CooperationIndicator (Bottom-Center)
│   ├── ChimeraPortrait (Image - circular)
│   ├── BondBar (horizontal bar)
│   ├── BondPercentText (TextMeshPro) "78%"
│   └── AbilityButtons (parent)
│       ├── BoostAbility (Button + Icon)
│       ├── ShortcutAbility (Button + Icon)
│       ├── DefenseAbility (Button + Icon)
│       └── RecoveryAbility (Button + Icon)
│
├── RacingLine (overlay on track)
│   └── LineRenderer (shows optimal path)
│
├── WrongWayIndicator (Center - shows when going backward)
│   ├── Background (Image - red warning)
│   ├── ArrowImage (Image - pointing correct direction)
│   └── WarningText (TextMeshPro) "WRONG WAY!"
│
└── CountdownOverlay (Center - shown at race start)
    ├── Background (Image - semi-transparent)
    └── CountdownText (TextMeshPro - very large) "3... 2... 1... GO!"
```

**Components**
- `RacingHUDController` - Updates all HUD elements
- `MinimapController` - Manages minimap display
- `SpeedometerAnimator` - Animates speedometer needle
- `CountdownController` - Handles race start countdown
- `CanvasGroup` - For fade effects

**Layout Specifications**

| Element | Anchor | Position | Size |
|---------|--------|----------|------|
| Speedometer | Bottom-Left | (120, 120) | (200, 200) |
| LapCounter | Top-Left | (20, -20) | (200, 60) |
| LapTimer | Top-Center | (0, -20) | (400, 80) |
| PositionIndicator | Top-Right | (-120, -20) | (180, 120) |
| Minimap | Bottom-Right | (-20, 20) | (250, 250) |
| BoostMeter | Left-Center | (40, 0) | (60, 250) |
| CooperationIndicator | Bottom-Center | (0, 40) | (500, 100) |
| WrongWayIndicator | Center | (0, 100) | (400, 150) |
| CountdownOverlay | Center | (0, 0) | (600, 400) |

---

### UI Details - Racing_PreActivityPanel

**Hierarchy**
```
Racing_PreActivityPanel
├── BackgroundOverlay (Image)
├── ContentPanel
│   ├── Background (Image)
│   ├── TitleText (TextMeshPro) "Racing Championship"
│   ├── DescriptionText (TextMeshPro)
│   ├── TrackInfoSection
│   │   ├── Label (TextMeshPro) "Track Info:"
│   │   ├── TrackImage (RawImage - track preview)
│   │   ├── TrackNameText (TextMeshPro) "Sunset Circuit"
│   │   ├── TrackLengthText (TextMeshPro) "Length: 2.5 km"
│   │   ├── LapCountText (TextMeshPro) "Laps: 3"
│   │   └── DifficultyStars (parent - 5 stars)
│   ├── DifficultySection
│   │   ├── Label (TextMeshPro) "Select Difficulty:"
│   │   ├── EasyButton (Button) "Casual"
│   │   ├── NormalButton (Button) "Amateur"
│   │   ├── HardButton (Button) "Professional"
│   │   ├── ExpertButton (Button) "Expert"
│   │   └── MasterButton (Button) "Legendary"
│   ├── VehicleSelectionSection
│   │   ├── Label (TextMeshPro) "Select Vehicle:"
│   │   ├── VehicleList (ScrollView)
│   │   │   └── Content
│   │   │       ├── VehicleSlot_1
│   │   │       ├── VehicleSlot_2
│   │   │       └── VehicleSlot_3
│   │   └── SelectedVehiclePreview
│   │       ├── VehicleModel (3D preview or image)
│   │       ├── VehicleNameText (TextMeshPro)
│   │       └── StatsPanel
│   │           ├── TopSpeedStat (slider + value)
│   │           ├── AccelerationStat (slider + value)
│   │           ├── HandlingStat (slider + value)
│   │           └── DriftStat (slider + value)
│   ├── ChimeraSelectionSection
│   │   ├── Label (TextMeshPro) "Select Copilot:"
│   │   ├── ChimeraList (ScrollView)
│   │   └── SelectedChimeraPreview
│   │       ├── Portrait (RawImage)
│   │       ├── NameText (TextMeshPro)
│   │       ├── BondStrengthBar
│   │       └── AbilitiesPanel
│   │           ├── Ability1 (Icon + Name)
│   │           ├── Ability2
│   │           ├── Ability3
│   │           └── Ability4
│   ├── RewardsSection
│   │   ├── Label (TextMeshPro) "Potential Rewards:"
│   │   ├── 1stPlaceReward (Icon + Text) "500 coins"
│   │   ├── 2ndPlaceReward (Icon + Text) "300 coins"
│   │   └── 3rdPlaceReward (Icon + Text) "150 coins"
│   ├── ObjectivesSection
│   │   ├── Label (TextMeshPro) "Objectives:"
│   │   ├── Objective1 (TextMeshPro) "• Finish in top 3"
│   │   ├── Objective2 (TextMeshPro) "• Complete all 3 laps"
│   │   └── Objective3 (TextMeshPro) "• Achieve 60%+ performance"
│   └── ButtonGroup
│       ├── StartButton (Button - green) "Start Race"
│       └── CancelButton (Button - red) "Cancel"
```

**Components**
- `RacingPreActivityController` - Manages panel
- `VehicleSelector` - Handles vehicle selection
- `ChimeraSelector` - Handles copilot selection
- `TrackPreview` - Shows track information

---

### UI Details - Racing_ResultsPanel

**Hierarchy**
```
Racing_ResultsPanel
├── BackgroundOverlay (Image)
├── ContentPanel
│   ├── Background (Image)
│   ├── TitleText (TextMeshPro) "Race Complete!"
│   ├── PositionDisplay
│   │   ├── PositionIcon (Image - trophy/medal based on placement)
│   │   ├── PositionText (TextMeshPro - huge) "2nd"
│   │   └── PlacementText (TextMeshPro) "Place"
│   ├── RaceStatsSection
│   │   ├── Label (TextMeshPro) "Race Statistics:"
│   │   ├── FinalTimeRow
│   │   │   ├── Label (TextMeshPro) "Final Time:"
│   │   │   └── Value (TextMeshPro) "4:15.678"
│   │   ├── BestLapRow
│   │   │   ├── Label (TextMeshPro) "Best Lap:"
│   │   │   └── Value (TextMeshPro) "1:21.234"
│   │   ├── AverageSpeedRow
│   │   │   ├── Label (TextMeshPro) "Avg Speed:"
│   │   │   └── Value (TextMeshPro) "175 km/h"
│   │   ├── TopSpeedRow
│   │   │   ├── Label (TextMeshPro) "Top Speed:"
│   │   │   └── Value (TextMeshPro) "212 km/h"
│   │   └── CheckpointsRow
│   │   │   ├── Label (TextMeshPro) "Checkpoints:"
│   │   │   └── Value (TextMeshPro) "30/30"
│   ├── PerformanceSection
│   │   ├── Label (TextMeshPro) "Performance Breakdown:"
│   │   ├── FinalPerformanceText (TextMeshPro) "82%"
│   │   ├── DrivingSkillRow
│   │   │   ├── Label (TextMeshPro) "Driving Skill:"
│   │   │   ├── Bar (Image)
│   │   │   └── Value (TextMeshPro) "85%"
│   │   ├── CopilotContributionRow
│   │   │   ├── Label (TextMeshPro) "Copilot Contribution:"
│   │   │   ├── Bar (Image)
│   │   │   └── Value (TextMeshPro) "78%"
│   │   ├── BondMultiplierRow
│   │   │   ├── Label (TextMeshPro) "Bond Multiplier:"
│   │   │   └── Value (TextMeshPro) "x1.12"
│   │   └── PositionBonusRow
│   │       ├── Label (TextMeshPro) "Position Bonus:"
│   │       └── Value (TextMeshPro) "+15%"
│   ├── StandingsTable
│   │   ├── Label (TextMeshPro) "Final Standings:"
│   │   ├── HeaderRow
│   │   │   ├── PositionHeader (TextMeshPro) "Pos"
│   │   │   ├── NameHeader (TextMeshPro) "Racer"
│   │   │   ├── TimeHeader (TextMeshPro) "Time"
│   │   │   └── BestLapHeader (TextMeshPro) "Best Lap"
│   │   ├── RacerRow_1 (player row highlighted)
│   │   │   ├── Position (TextMeshPro) "1"
│   │   │   ├── Name (TextMeshPro) "Player"
│   │   │   ├── Time (TextMeshPro) "4:10.123"
│   │   │   └── BestLap (TextMeshPro) "1:20.456"
│   │   ├── RacerRow_2
│   │   └── [Rows 3-8]
│   ├── RewardsSection
│   │   ├── Label (TextMeshPro) "Rewards Earned:"
│   │   ├── CurrencyReward
│   │   │   ├── Icon (Image)
│   │   │   ├── Label (TextMeshPro) "Prize Money:"
│   │   │   └── Value (TextMeshPro) "+300 coins"
│   │   ├── SkillGainReward
│   │   │   ├── Icon (Image)
│   │   │   ├── Label (TextMeshPro) "Reflexes Skill:"
│   │   │   └── Value (TextMeshPro) "+0.0155"
│   │   └── BondGainReward
│   │       ├── Icon (Image)
│   │       ├── Label (TextMeshPro) "Bond Strength:"
│   │       └── Value (TextMeshPro) "+0.0092"
│   ├── NewRecordsSection (if applicable)
│   │   ├── Label (TextMeshPro) "New Records!"
│   │   ├── RecordText_1 (TextMeshPro) "• New Best Lap Time!"
│   │   └── RecordText_2 (TextMeshPro) "• Track Record Broken!"
│   ├── PartnershipChangeSection
│   │   ├── Label (TextMeshPro) "Partnership Changes:"
│   │   ├── BondChangeRow
│   │   │   ├── Icon (Image)
│   │   │   └── Text (TextMeshPro) "Bond: 78% → 81% (+3%)"
│   │   └── EmotionalImpactRow
│   │       ├── Icon (Image)
│   │       └── Text (TextMeshPro) "Mood: Excited"
│   └── ButtonGroup
│       ├── RetryButton (Button) "Retry Race"
│       ├── NextTrackButton (Button) "Next Track"
│       └── ExitButton (Button) "Exit to Menu"
```

**Components**
- `RacingResultsController` - Displays results
- `StandingsTableController` - Populates racer standings
- `ResultsAnimator` - Animates numbers and reveals
- `CanvasGroup` - For fade animations

---

### Setup Steps (Racing Scene)

**Step 1: Create Base Scene**
1. Create new scene: `DemoScene_Racing`
2. Add Core Scene Template

**Step 2: Setup ActivityCenter**
1. Create `ActivityCenter` GameObject
2. Add `RacingActivityAuthoring` component
3. Create `Genre_Racing.asset`
4. Create `Activity_Racing.asset`
5. Assign assets

**Step 3: Create Race Track**
1. Create `RaceTrack` parent GameObject
2. Import or create track mesh
3. Add `RaceTrackManager` component
4. Create starting grid (8 positions)
5. Place 10 checkpoints around track
6. Place finish line trigger
7. Add track boundaries (colliders)
8. Add 10 boost pads at strategic locations

**Step 4: Setup PlayerVehicle**
1. Create `PlayerVehicle` GameObject
2. Import vehicle model
3. Add `Rigidbody` component
4. Add `VehicleController` component
5. Add `VehiclePhysics` component
6. Setup 4 wheel colliders
7. Add `PlayerInputHandler` component
8. Add engine audio source
9. Configure all properties

**Step 5: Setup ChimeraPassenger**
1. Create `ChimeraPassenger` as child of PlayerVehicle
2. Position in passenger seat
3. Add `EnhancedCreatureAuthoring` component
4. Add `ChimeraCopilotAI` component
5. Configure copilot abilities

**Step 6: Setup AI Racers**
1. Create `AIRacers` parent GameObject
2. Duplicate PlayerVehicle 7 times
3. For each AI racer:
   - Replace PlayerInputHandler with AIRacingController
   - Add AI chimera passenger
   - Assign to different starting grid position
4. Configure AI difficulty

**Step 7: Setup CheckpointSystem**
1. Create `CheckpointSystem` GameObject
2. Add `RaceProgressTracker` component
3. Register all checkpoints
4. Configure lap counting

**Step 8: Setup Camera**
1. Position Main Camera behind player vehicle
2. Add chase camera script
3. Configure camera offset: (0, 2, -5)
4. Add camera smoothing

**Step 9: Setup Racing HUD**
1. Create complete Racing_HUD hierarchy
2. Add all components
3. Configure speedometer
4. Setup minimap texture
5. Add ability buttons
6. Configure layouts

**Step 10: Setup Pre-Activity Panel**
1. Create Racing_PreActivityPanel
2. Add track preview image
3. Setup vehicle selection
4. Setup chimera selection
5. Configure rewards display

**Step 11: Setup Results Panel**
1. Create Racing_ResultsPanel
2. Setup standings table
3. Configure stats display
4. Add results animations

**Step 12: Setup Audio**
1. Add background music
2. Add engine sounds to all vehicles
3. Add boost sound effects
4. Add countdown sounds

---

### Testing Checklist (Racing Scene)

**Initialization Tests**
- [ ] Scene loads without errors
- [ ] Track mesh displays correctly
- [ ] All checkpoints registered
- [ ] All 8 vehicles spawn at starting grid
- [ ] Camera positioned correctly behind player
- [ ] HUD displays all elements

**Race Start Tests**
- [ ] Countdown displays "3... 2... 1... GO!"
- [ ] Vehicles cannot move during countdown
- [ ] Race starts after "GO!"
- [ ] Timer starts counting
- [ ] All AI racers begin racing

**Racing Mechanics Tests**
- [ ] Player vehicle responds to input (accelerate, brake, steer)
- [ ] Vehicle physics feel realistic
- [ ] Drifting works correctly
- [ ] Boost pads apply speed boost
- [ ] Collisions with barriers work
- [ ] Collisions with other vehicles work

**Checkpoint System Tests**
- [ ] First checkpoint triggers correctly
- [ ] All 10 checkpoints trigger in sequence
- [ ] Finish line triggers after all checkpoints
- [ ] Lap counter increments on finish line
- [ ] Cannot skip checkpoints
- [ ] "Wrong way" indicator shows when going backward

**Chimera Copilot Tests**
- [ ] Chimera passenger visible in vehicle
- [ ] Boost ability increases speed temporarily
- [ ] Shortcut ability shows racing line
- [ ] Defense ability shields from collisions
- [ ] Recovery ability speeds up recovery from crashes
- [ ] Abilities go on cooldown after use
- [ ] Bond strength affects ability effectiveness

**UI Tests**
- [ ] Speedometer updates in real-time
- [ ] Lap counter displays correctly (e.g., "2/3")
- [ ] Lap timer counts accurately
- [ ] Position indicator updates (1st, 2nd, etc.)
- [ ] Minimap shows player and opponent positions
- [ ] Boost meter fills and depletes correctly
- [ ] Cooperation indicator displays bond strength
- [ ] Ability buttons clickable (or hotkey)

**AI Racer Tests**
- [ ] AI racers follow racing line
- [ ] AI racers avoid collisions
- [ ] AI racers use boost pads
- [ ] AI racers respect checkpoints
- [ ] AI difficulty affects racing skill

**Race Completion Tests**
- [ ] Race ends after all laps completed
- [ ] Final positions calculated correctly
- [ ] Results panel displays
- [ ] Standings table shows all 8 racers
- [ ] Performance calculated based on position
- [ ] Rewards calculated correctly
- [ ] Partnership values update

**Performance Tests**
- [ ] Maintains 60 FPS with 8 vehicles
- [ ] No stuttering during gameplay
- [ ] Fast loading times
- [ ] No memory leaks

---

## Puzzle Genre Scenes

### Scene Types Covered
- Match3
- Tetris
- Physics Puzzle
- Logic Puzzle
- Hidden Object
- Tile Matching

---

### Match-3 Demo Scene

#### Scene Name
`DemoScene_Match3`

#### Complete GameObject Hierarchy
```
DemoScene_Match3
├── ChimeraSceneBootstrap ⭐
├── WorldManager
├── Main Camera (Orthographic, top-down)
├── Directional Light
├── UI Canvas
│   ├── DebugPanel
│   ├── PerformancePanel
│   ├── SceneControlsPanel
│   ├── Match3_HUD
│   ├── Match3_PreActivityPanel
│   └── Match3_ResultsPanel
├── EventSystem
│
├── ActivityCenter
│   └── Match3ActivityAuthoring
│
├── PuzzleGrid
│   ├── GridManager
│   ├── GridBackground (9x9 visual grid)
│   └── Tiles (parent - populated at runtime)
│
├── TileFactory
│   └── TileSpawner
│
├── MatchDetectionSystem
│   └── ComboTracker
│
├── ChimeraAssistant
│   ├── EnhancedCreatureAuthoring
│   └── ChimeraPuzzleAI
│
├── ParticleEffects (parent)
│   ├── MatchEffect
│   ├── ComboEffect
│   └── SpecialTileEffect
│
└── Audio
    ├── BackgroundMusic
    └── MatchSoundEffects
```

---

#### GameObject Details - PuzzleGrid

**Name**: `PuzzleGrid`

**Transform**
```
Position: (0, 0, 0)
Rotation: (0, 0, 0)
```

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `GridManager` | MonoBehaviour | See properties below |
| `GridVisualizer` | MonoBehaviour | Draws grid lines |

**GridManager Properties**
```
Grid Width: 9
Grid Height: 9
Cell Size: 1.0
Tile Types: 6 (different colors/symbols)
Special Tile Chance: 0.1 (10%)
Gravity Enabled: true
Auto-Refill: true
```

**GridBackground**
```
GridBackground (SpriteRenderer or Quad)
├── Size: 9x9 (to match grid)
├── Material: Grid material with lines
└── Sort Order: -1 (behind tiles)
```

---

#### GameObject Details - TileFactory

**Name**: `TileFactory`

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `TileSpawner` | MonoBehaviour | See properties below |

**TileSpawner Properties**
```
Tile Prefabs:
  - RedTile.prefab
  - BlueTile.prefab
  - GreenTile.prefab
  - YellowTile.prefab
  - PurpleTile.prefab
  - OrangeTile.prefab

Special Tile Prefabs:
  - BombTile.prefab (destroys 3x3 area)
  - LineTile.prefab (destroys entire row/column)
  - ColorBombTile.prefab (destroys all tiles of one color)

Object Pool Size: 100
Spawn Animation Duration: 0.3 seconds
```

**Required Prefabs**
Each tile prefab should have:
- `SpriteRenderer` (tile visual)
- `BoxCollider2D` (for mouse/touch input)
- `TileComponent` (MonoBehaviour with tile data)
- `Rigidbody2D` (for physics-based falling)
- `TileAnimator` (handles animations)

---

#### GameObject Details - MatchDetectionSystem

**Name**: `MatchDetectionSystem`

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `MatchDetector` | MonoBehaviour | See properties below |
| `ComboTracker` | MonoBehaviour | Tracks consecutive matches |

**MatchDetector Properties**
```
Minimum Match Length: 3
Check Horizontal: true
Check Vertical: true
Check Diagonal: false
Special Match Patterns:
  - T-Shape (creates Line Tile)
  - L-Shape (creates Line Tile)
  - 4-Match (creates Bomb Tile)
  - 5-Match (creates Color Bomb Tile)
```

**ComboTracker Properties**
```
Combo Timeout: 2.0 seconds
Combo Multipliers:
  - Combo 1: x1.0
  - Combo 2: x1.2
  - Combo 3: x1.5
  - Combo 4: x2.0
  - Combo 5+: x3.0
```

---

#### GameObject Details - ChimeraAssistant

**Name**: `ChimeraAssistant`

**Transform**
```
Position: (12, 0, 0) [to the right of grid]
Rotation: (0, 0, 0)
Scale: (0.5, 0.5, 0.5) [smaller than normal]
```

**Components List**
| Component | Type | Properties |
|-----------|------|------------|
| `EnhancedCreatureAuthoring` | IConvertGameObjectToEntity | Species config |
| `ChimeraPuzzleAI` | MonoBehaviour | See properties below |
| `Animator` | Unity Component | Chimera animations |
| `ConvertToEntity` | Hybrid Component | Convert and Destroy |

**ChimeraPuzzleAI Properties**
```
Assistance Type: Hint Provider
Abilities:
  - Hint (highlights potential match)
  - Shuffle (reshuffles board if no moves)
  - Extra Moves (grants 3 extra moves)
  - Combo Boost (increases combo multiplier)

Ability Cooldowns: 15 seconds each
Hint Auto-trigger: true (after 10 seconds of no input)
Cooperation Threshold: 0.5
```

**Animations**
- Idle (breathing, looking around)
- Thinking (when player stuck)
- Excited (when combo achieved)
- Happy (when match made)
- Sad (when player fails)

---

### UI Details - Match3_HUD

**Hierarchy**
```
Match3_HUD
├── ScoreSection (Top-Left)
│   ├── Background (Image)
│   ├── ScoreLabel (TextMeshPro) "SCORE"
│   ├── ScoreText (TextMeshPro - large) "12,450"
│   ├── TargetScoreLabel (TextMeshPro) "Target:"
│   └── TargetScoreText (TextMeshPro) "20,000"
│
├── MovesCounter (Top-Center)
│   ├── Background (Image)
│   ├── MovesLabel (TextMeshPro) "MOVES"
│   └── MovesText (TextMeshPro - large) "15"
│
├── ProgressBar (Top-Right)
│   ├── Background (Image)
│   ├── FillBar (Image - fills left-to-right)
│   ├── ProgressLabel (TextMeshPro) "Progress:"
│   └── ProgressPercent (TextMeshPro) "62%"
│
├── ComboDisplay (Left-Center)
│   ├── Background (Image)
│   ├── ComboLabel (TextMeshPro) "COMBO"
│   ├── ComboCountText (TextMeshPro - very large, animated)
│   └── MultiplierText (TextMeshPro) "x2.0"
│
├── ObjectivesPanel (Right-Center)
│   ├── Background (Image)
│   ├── ObjectivesLabel (TextMeshPro) "Objectives:"
│   ├── Objective1
│   │   ├── Icon (Image - red tile)
│   │   ├── Description (TextMeshPro) "Collect 20 Red"
│   │   └── Progress (TextMeshPro) "12/20"
│   ├── Objective2
│   │   ├── Icon (Image - blue tile)
│   │   ├── Description (TextMeshPro) "Collect 15 Blue"
│   │   └── Progress (TextMeshPro) "15/15" [checkmark]
│   └── Objective3
│       ├── Icon (Image - bomb)
│       ├── Description (TextMeshPro) "Create 3 Bombs"
│       └── Progress (TextMeshPro) "1/3"
│
├── ChimeraAssistancePanel (Bottom-Right)
│   ├── ChimeraPortrait (Image - circular)
│   ├── BondStrengthBar
│   │   ├── Background (Image)
│   │   ├── FillBar (Image)
│   │   └── BondText (TextMeshPro) "82%"
│   └── AbilityButtons (parent)
│       ├── HintButton (Button + Icon + Cooldown overlay)
│       ├── ShuffleButton (Button + Icon + Cooldown overlay)
│       ├── ExtraMovesButton (Button + Icon + Cooldown overlay)
│       └── ComboBoostButton (Button + Icon + Cooldown overlay)
│
├── TimerDisplay (Top-Center-Left)
│   ├── Background (Image)
│   ├── TimerIcon (Image - clock)
│   └── TimeText (TextMeshPro) "3:45"
│
├── PerformanceBar (Right edge, vertical)
│   ├── Background (Image)
│   ├── FillBar (Image - fills bottom-to-top)
│   └── PerformanceText (TextMeshPro) "75%"
│
└── HintOverlay (overlays on grid)
    ├── HintArrow1 (Image - animated arrow)
    └── HintArrow2 (Image - shows suggested swap)
```

**Components**
- `Match3HUDController` - Updates HUD elements
- `ComboAnimator` - Animates combo display
- `ObjectivesTracker` - Tracks objective progress
- `HintVisualizer` - Shows hint arrows
- `CanvasGroup` - For fade effects

**Layout Specifications**

| Element | Anchor | Position | Size |
|---------|--------|----------|------|
| ScoreSection | Top-Left | (20, -20) | (250, 100) |
| MovesCounter | Top-Center | (-100, -20) | (180, 90) |
| TimerDisplay | Top-Center | (100, -20) | (180, 90) |
| ProgressBar | Top-Right | (-20, -20) | (300, 80) |
| ComboDisplay | Left-Center | (30, 0) | (200, 200) |
| ObjectivesPanel | Right-Center | (-30, 0) | (280, 250) |
| ChimeraAssistancePanel | Bottom-Right | (-30, 30) | (320, 150) |
| PerformanceBar | Right edge | (-15, 0) | (30, 400) |

---

### Setup Steps (Match-3 Scene)

**Step 1: Create Base Scene**
1. Create scene: `DemoScene_Match3`
2. Add Core Scene Template
3. Set Main Camera to Orthographic
4. Position camera: (4.5, 4.5, -10) [centered on 9x9 grid]

**Step 2: Setup ActivityCenter**
1. Create `ActivityCenter`
2. Add `Match3ActivityAuthoring`
3. Create `Genre_Match3.asset`:
   ```
   Genre Type: Match3
   Primary Player Skill: Problem Solving
   Primary Chimera Trait: Intelligence
   Base Duration: 300s
   Difficulty Scaling: 1.2
   ```
4. Create `Activity_Match3.asset`:
   ```
   Grid Size: 9x9
   Tile Types: 6
   Target Score: 20000
   Max Moves: 30
   Objectives:
     - Collect 20 Red tiles
     - Collect 15 Blue tiles
     - Create 3 Bomb tiles
   ```

**Step 3: Create Tile Prefabs**
1. Create 6 basic tile prefabs (Red, Blue, Green, Yellow, Purple, Orange)
2. Each prefab:
   - Sprite of size 0.9x0.9 (leaves gap between tiles)
   - BoxCollider2D size 1x1
   - Rigidbody2D (Kinematic initially)
   - `TileComponent` script
   - `TileAnimator` script
3. Create 3 special tile prefabs (Bomb, Line, ColorBomb)
4. Save in `Assets/_Project/Prefabs/Match3/`

**Step 4: Setup PuzzleGrid**
1. Create `PuzzleGrid` GameObject at (0, 0, 0)
2. Add `GridManager` component
3. Configure grid size (9x9)
4. Create `GridBackground` sprite
5. Add `GridVisualizer` for visual grid lines

**Step 5: Setup TileFactory**
1. Create `TileFactory` GameObject
2. Add `TileSpawner` component
3. Assign all 6 basic tile prefabs
4. Assign all 3 special tile prefabs
5. Configure object pool size (100)

**Step 6: Setup MatchDetectionSystem**
1. Create `MatchDetectionSystem` GameObject
2. Add `MatchDetector` component
3. Configure minimum match length (3)
4. Define special match patterns
5. Add `ComboTracker` component
6. Configure combo multipliers

**Step 7: Setup ChimeraAssistant**
1. Create `ChimeraAssistant` at (12, 0, 0)
2. Scale to 0.5
3. Add `EnhancedCreatureAuthoring`
4. Assign species config
5. Add `ChimeraPuzzleAI`
6. Configure abilities and cooldowns
7. Add Animator with chimera animations

**Step 8: Setup Particle Effects**
1. Create `ParticleEffects` parent
2. Create `MatchEffect` particle system (plays on tile match)
3. Create `ComboEffect` particle system (plays on combo)
4. Create `SpecialTileEffect` particle system (plays on special activation)

**Step 9: Setup Match3 HUD**
1. Create complete Match3_HUD hierarchy
2. Add all UI elements
3. Add `Match3HUDController` component
4. Configure all layouts
5. Setup objective tracking
6. Add ability buttons with cooldown overlays

**Step 10: Setup Pre-Activity Panel**
1. Create Match3_PreActivityPanel
2. Show puzzle preview
3. Display objectives
4. Setup chimera selection
5. Add difficulty selection

**Step 11: Setup Results Panel**
1. Create Match3_ResultsPanel
2. Show final score
3. Display objectives completion
4. Show performance breakdown
5. Add retry/next puzzle buttons

**Step 12: Test Grid Generation**
1. Enter Play mode
2. Verify grid generates correctly
3. Ensure no automatic matches on spawn
4. Test tile swapping
5. Verify match detection
6. Test gravity and refill

---

### Testing Checklist (Match-3 Scene)

**Initialization Tests**
- [ ] Scene loads without errors
- [ ] Grid generates 9x9 correctly
- [ ] All tiles spawn without pre-existing matches
- [ ] Chimera assistant appears and animates
- [ ] HUD displays all elements correctly

**Tile Interaction Tests**
- [ ] Can select tile with mouse/touch
- [ ] Can swap adjacent tiles
- [ ] Cannot swap non-adjacent tiles
- [ ] Cannot swap if no match results
- [ ] Tiles animate smoothly during swap

**Match Detection Tests**
- [ ] 3-tile horizontal match detected
- [ ] 3-tile vertical match detected
- [ ] 4-tile match creates Bomb tile
- [ ] 5-tile match creates ColorBomb tile
- [ ] T-shape match creates Line tile
- [ ] L-shape match creates Line tile
- [ ] Matched tiles disappear with particle effect

**Grid Physics Tests**
- [ ] Tiles above fall down after match
- [ ] Gravity feels natural
- [ ] New tiles spawn from top to refill
- [ ] Multiple cascading matches work
- [ ] No gaps remain in grid

**Combo System Tests**
- [ ] Combo counter increases on cascading matches
- [ ] Combo multiplier applies to score
- [ ] Combo resets after timeout (2 seconds of no matches)
- [ ] Combo display animates correctly
- [ ] High combos trigger particle effects

**Special Tiles Tests**
- [ ] Bomb tile destroys 3x3 area
- [ ] Line tile (horizontal) destroys entire row
- [ ] Line tile (vertical) destroys entire column
- [ ] ColorBomb destroys all tiles of selected color
- [ ] Combining special tiles creates super effects

**Chimera Assistance Tests**
- [ ] Hint ability highlights potential match
- [ ] Hint auto-triggers after 10 seconds of inactivity
- [ ] Shuffle ability reshuffles board
- [ ] ExtraMoves ability grants 3 moves
- [ ] ComboBoost ability increases multiplier temporarily
- [ ] Abilities go on cooldown after use
- [ ] Bond strength affects ability effectiveness
- [ ] Chimera animations match game state

**Objectives Tests**
- [ ] Objective progress tracks correctly
- [ ] Collecting tiles updates "Collect X tiles" objectives
- [ ] Creating specials updates "Create X specials" objectives
- [ ] Completed objectives show checkmark
- [ ] All objectives must complete to win

**Score and Moves Tests**
- [ ] Score increases on matches
- [ ] Score reflects combo multiplier
- [ ] Move counter decrements on each swap
- [ ] Game ends when moves reach 0
- [ ] Target score displayed correctly

**Win/Lose Conditions Tests**
- [ ] Win if target score reached AND all objectives complete
- [ ] Lose if moves reach 0 without completing objectives
- [ ] Results panel displays on win
- [ ] Results panel displays on lose
- [ ] Performance calculated correctly

**UI Tests**
- [ ] Score updates in real-time
- [ ] Moves counter updates correctly
- [ ] Progress bar fills based on score
- [ ] Objectives panel updates
- [ ] Timer counts down (if timed mode)
- [ ] Hint arrows display correctly
- [ ] Ability buttons show cooldown overlay

**Performance Tests**
- [ ] Maintains 60 FPS with active matches
- [ ] No lag during cascading matches
- [ ] Particle effects don't cause slowdown
- [ ] Object pooling works efficiently

---

Due to length constraints, I'll continue with the remaining scenes in the next section. Would you like me to continue with:
- Strategy Genre Scenes
- Rhythm Genre Scene
- RPG Genre Scenes
- Simulation Genre Scenes
- Sports Genre Scenes
- All System Demo Scenes (Breeding, Partnership, AI, Save/Load)
- Performance Test Scenes

Should I proceed with all of these in a single expanded document?