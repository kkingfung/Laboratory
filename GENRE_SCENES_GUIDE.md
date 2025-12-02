# Project Chimera - Genre Scenes Guide
**Detailed setup instructions for all 47 activity genre demo scenes**

---

## 📋 Table of Contents

1. [Action Genres](#action-genres) - FPS, TPS, Fighting, Beat'Em Up, Hack & Slash, Stealth, Survival Horror
2. [Racing Genres](#racing-genres) - Racing, Kart Racing, Combat Racing
3. [Puzzle Genres](#puzzle-genres) - Match-3, Tetris, Physics Puzzle, Logic Puzzle, Hidden Object, Tile Matching
4. [Strategy Genres](#strategy-genres) - RTS, Turn-Based, 4X, Grand Strategy, Auto Battler, Tower Defense
5. [Rhythm Genres](#rhythm-genres) - Rhythm, Music
6. [RPG Genres](#rpg-genres) - Action RPG, Turn-Based RPG, Roguelike, MMORPG, Dungeon Crawler
7. [Simulation Genres](#simulation-genres) - Life Sim, Management, Tycoon, Farming, City Builder
8. [Sports Genres](#sports-genres) - Sports, Fighting Sports, Racing Sports
9. [Additional Genres](#additional-genres) - Platformer, Metroidvania, Visual Novel, etc.

---

## How to Use This Guide

Each genre section contains:

1. **Complete GameObject Hierarchy** - Full scene structure with all objects
2. **Component Details** - All components with properties and configurations
3. **UI Hierarchy** - Complete UI layout with anchors and sizes
4. **Setup Steps** - Step-by-step instructions to build the scene
5. **Testing Checklist** - Validation steps for scene functionality

**Prerequisites:**
- Core Scene Template already set up (see [DEMO_SCENE_SETUP_GUIDE.md](./DEMO_SCENE_SETUP_GUIDE.md))
- ChimeraGameConfig.asset created
- GenreLibrary.asset created (if using activity system)

---

# Action Genres

## FPS (First-Person Shooter)

### Scene Name
`DemoScene_FPS`

### Complete GameObject Hierarchy
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
│   │   ├── SpawnPoint_1 through SpawnPoint_10
│   ├── CoverObjects (parent)
│   │   ├── Crate_1 through Crate_12
│   └── Pickups (parent)
│       ├── HealthPack_1, HealthPack_2
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

### Component Details

#### ActivityCenter

**Components:**
| Component | Type | Key Properties |
|-----------|------|----------------|
| `ActivityCenterManager` | MonoBehaviour | Activity Type: FPS |
| | | Genre Config: Genre_FPS.asset |
| | | Time Limit: 300s |
| `ActivityCenterAuthoring` | IConvertGameObjectToEntity | Activity Type: FPS |
| `ConvertToEntity` | Hybrid | Convert and Destroy |

**Required ScriptableObjects:**
- **Genre_FPS.asset** (GenreConfiguration)
  - Primary Player Skill: Reflexes
  - Primary Chimera Trait: Aggression
  - Base Duration: 300s
  - Difficulty Scaling: 1.5
  - Score Multiplier: 1.2
  - Player Skill Weight: 0.7
  - Chimera Trait Weight: 0.3

- **Activity_FPS.asset** (ActivityConfig)
  - Enemy Count: 30
  - Enemy Types: [Basic, Armored, Fast]
  - Weapon Loadout: [Pistol, Rifle, Shotgun]

#### PlayerController

**Transform:**
- Position: (0, 1.8, 0)
- Rotation: (0, 0, 0)

**Components:**
| Component | Type | Key Properties |
|-----------|------|----------------|
| `CharacterController` | Unity | Height: 1.8, Radius: 0.4 |
| `FPSController` | MonoBehaviour | Movement Speed: 7.0 |
| | | Sprint Speed: 10.0 |
| | | Jump Force: 5.0 |
| | | Mouse Sensitivity: 2.0 |
| `PlayerInputHandler` | MonoBehaviour | Input Asset: PlayerInputActions |
| `PlayerSkillTracker` | MonoBehaviour | Tracked Skill: Reflexes |
| `PartnershipController` | MonoBehaviour | Partner: [Runtime assignment] |
| `PlayerHealthComponent` | MonoBehaviour | Max Health: 100 |

#### ChimeraPartner

**Transform:**
- Position: (2, 0, 0)
- Rotation: (0, 0, 0)

**Components:**
| Component | Type | Key Properties |
|-----------|------|----------------|
| `EnhancedCreatureAuthoring` | IConvertGameObjectToEntity | Species: FireDragon.asset |
| | | Age: 120 days |
| | | Bond Strength: 0.75 |
| | | Personality: ["Brave", "Aggressive", "Loyal"] |
| `ChimeraCombatAI` | MonoBehaviour | Combat Role: Support |
| | | Attack Style: Ranged |
| `ConvertToEntity` | Hybrid | Convert and Destroy |

#### EnemyManager

**Components:**
| Component | Type | Key Properties |
|-----------|------|----------------|
| `EnemySpawnerAuthoring` | IConvertGameObjectToEntity | Enemy Prefab: Enemy_Basic.prefab |
| | | Max Concurrent: 10 |
| | | Spawn Interval: 5s |
| | | Total Budget: 30 |
| `DifficultyController` | MonoBehaviour | Difficulty: Normal |
| | | Health Multiplier: 1.0 |
| | | Damage Multiplier: 1.0 |

### UI Hierarchy

#### FPS_HUD

```
FPS_HUD
├── Crosshair (Image)
│   Anchor: Center (0, 0)
│   Size: (32, 32)
│
├── HealthBar
│   ├── Background (Image)
│   ├── FillBar (Image - red)
│   └── HealthText (TextMeshPro)
│   Anchor: Bottom-Left (20, 120)
│   Size: (300, 40)
│
├── ChimeraHealthBar
│   ├── Background (Image)
│   ├── FillBar (Image - green)
│   └── HealthText (TextMeshPro)
│   Anchor: Bottom-Left (20, 70)
│   Size: (300, 40)
│
├── AmmoCounter
│   ├── CurrentAmmo (TextMeshPro - large)
│   └── ReserveAmmo (TextMeshPro - small)
│   Anchor: Bottom-Right (-120, 80)
│   Size: (200, 80)
│
├── WeaponDisplay
│   ├── WeaponIcon (Image)
│   └── WeaponName (TextMeshPro)
│   Anchor: Bottom-Right (-120, 170)
│   Size: (200, 60)
│
├── TimerDisplay
│   ├── Icon (Image)
│   └── TimeText (TextMeshPro)
│   Anchor: Top-Center (0, -20)
│   Size: (200, 50)
│
├── ScoreDisplay
│   ├── Label (TextMeshPro)
│   └── ScoreText (TextMeshPro)
│   Anchor: Top-Center (0, -80)
│   Size: (250, 50)
│
├── KillCounter
│   ├── Icon (Image)
│   └── CountText (TextMeshPro)
│   Anchor: Top-Left (20, -20)
│   Size: (150, 50)
│
├── PerformanceBar
│   ├── Background (Image)
│   ├── FillBar (Image - gradient)
│   └── PercentText (TextMeshPro)
│   Anchor: Right-Center (-20, 0)
│   Size: (40, 300)
│
├── CooperationIndicator
│   ├── BondIcon (Image)
│   ├── BondStrength (TextMeshPro)
│   └── BonusText (TextMeshPro)
│   Anchor: Bottom-Center (0, 20)
│   Size: (400, 60)
│
└── HitIndicators (parent)
    ├── HitLeft (Image)
    ├── HitRight (Image)
    ├── HitTop (Image)
    └── HitBottom (Image)
    All fade in/out on damage from direction
```

**HUD Components:**
- `FPSHUDController` - Updates all HUD elements
- `CanvasGroup` - For fade in/out effects

#### FPS_PreActivityPanel

```
FPS_PreActivityPanel
├── BackgroundOverlay (Image - semi-transparent)
├── ContentPanel
│   ├── Background (Image)
│   ├── TitleText "First-Person Shooter"
│   ├── DescriptionText
│   ├── DifficultySection
│   │   ├── Label "Select Difficulty:"
│   │   ├── EasyButton
│   │   ├── NormalButton
│   │   ├── HardButton
│   │   ├── ExpertButton
│   │   └── MasterButton
│   ├── ChimeraSelectionSection
│   │   ├── Label "Select Partner:"
│   │   ├── ChimeraList (ScrollView)
│   │   └── SelectedChimeraPreview
│   │       ├── Portrait
│   │       ├── NameText
│   │       ├── BondStrengthBar
│   │       └── StatsPanel
│   ├── RewardsSection
│   │   ├── Label "Expected Rewards:"
│   │   ├── CurrencyReward
│   │   ├── SkillGainReward
│   │   └── BondGainReward
│   ├── ObjectivesSection
│   │   ├── Label "Objectives:"
│   │   ├── "• Eliminate 30 enemies"
│   │   ├── "• Survive for 5 minutes"
│   │   └── "• Maintain 50%+ performance"
│   └── ButtonGroup
│       ├── StartButton (green)
│       └── CancelButton (red)
```

**Panel Components:**
- `PreActivityPanelController` - Panel management
- `DifficultySelector` - Difficulty selection logic
- `ChimeraSelector` - Partner selection
- `CanvasGroup` - Fade animations

#### FPS_ResultsPanel

```
FPS_ResultsPanel
├── BackgroundOverlay
├── ContentPanel
│   ├── TitleText "Mission Complete!"
│   ├── RankDisplay
│   │   ├── RankIcon (Bronze/Silver/Gold/Platinum)
│   │   └── RankText
│   ├── PerformanceSection
│   │   ├── FinalPerformanceText "87%"
│   │   ├── PlayerPerformanceRow "85%"
│   │   ├── ChimeraContributionRow "92%"
│   │   ├── BondMultiplierRow "x1.15"
│   │   └── AgeFactorRow "x1.0"
│   ├── StatsSection
│   │   ├── EnemiesKilledRow "30/30"
│   │   ├── AccuracyRow "78%"
│   │   ├── TimeRow "5:00"
│   │   └── DamageTakenRow "45"
│   ├── RewardsSection
│   │   ├── CurrencyReward "+187 coins"
│   │   ├── SkillGainReward "+0.0187 Reflexes"
│   │   └── BondGainReward "+0.0087"
│   ├── NewRecordsSection (if applicable)
│   │   └── "• New Best Kill Count!"
│   ├── PartnershipChangeSection
│   │   ├── "Bond: 75% → 78% (+3%)"
│   │   └── "Mood: Happy"
│   └── ButtonGroup
│       ├── RetryButton
│       ├── NextDifficultyButton
│       └── ExitButton
```

**Panel Components:**
- `ResultsPanelController` - Results display
- `ResultsAnimator` - Number count animations
- `CanvasGroup` - Fade effects

### Setup Steps

**Step 1: Create Base Scene**
1. Create new scene: `DemoScene_FPS`
2. Delete default camera and light
3. Add Core Scene Template (see main guide)

**Step 2: Setup ActivityCenter**
1. Create empty GameObject: `ActivityCenter`
2. Add `ActivityCenterManager` component
3. Add `ActivityCenterAuthoring` component
4. Create `Genre_FPS.asset` in `Assets/_Project/Resources/Configs/GenreConfigurations/`
5. Create `Activity_FPS.asset` in `Assets/_Project/Resources/Configs/Activities/`
6. Assign both assets to ActivityCenterManager

**Step 3: Setup PlayerController**
1. Create empty GameObject: `PlayerController` at (0, 1.8, 0)
2. Add `CharacterController` (Height: 1.8, Radius: 0.4)
3. Add `FPSController` (configure movement properties)
4. Add `PlayerInputHandler` (assign PlayerInputActions.inputactions)
5. Add `PlayerSkillTracker` (set Tracked Skill: Reflexes)
6. Add `PartnershipController`
7. Add `PlayerHealthComponent` (Max Health: 100)

**Step 4: Setup ChimeraPartner**
1. Create empty GameObject: `ChimeraPartner` at (2, 0, 0)
2. Add `EnhancedCreatureAuthoring`
3. Assign `FireDragon.asset` to Species Config
4. Set Age: 120, Initial Bond: 0.75
5. Add `ChimeraCombatAI` (Combat Role: Support)
6. Add `ConvertToEntity` (Convert and Destroy)

**Step 5: Setup Combat Arena**
1. Create parent: `CombatArena`
2. Create `ArenaFloor` plane (scale 50x1x50)
3. Create `Walls` parent with 4 wall cubes (position at arena edges)
4. Create `SpawnPoints` parent with 10 empty GameObjects (spread around arena)
5. Create `CoverObjects` parent with 12 crates (scattered for cover)
6. Create `Pickups` parent with health/ammo pickup objects

**Step 6: Setup EnemyManager**
1. Create empty GameObject: `EnemyManager`
2. Add `EnemySpawnerAuthoring`
3. Create enemy prefab: `Enemy_Basic.prefab`
4. Assign prefab to spawner (Max Concurrent: 10, Interval: 5s)
5. Add `DifficultyController` (Difficulty: Normal)

**Step 7: Setup UI**
1. Create `FPS_HUD` parent under UI Canvas
2. Add all child UI elements as specified in UI hierarchy
3. Set anchors and positions per layout table
4. Add `FPSHUDController` component
5. Create `FPS_PreActivityPanel` with full hierarchy
6. Add `PreActivityPanelController`
7. Create `FPS_ResultsPanel` with full hierarchy
8. Add `ResultsPanelController`

**Step 8: Setup Audio**
1. Create `Audio` parent
2. Add `BackgroundMusic` AudioSource with combat music
3. Add `CombatAmbience` AudioSource with ambient sounds
4. Configure 3D spatial audio if needed

**Step 9: Final Configuration**
1. Verify all ScriptableObject references assigned
2. Check component properties match specifications
3. Set camera to FPS view (attach to PlayerController if needed)
4. Test scene initialization

### Testing Checklist

**Initialization Tests**
- [ ] Scene loads without errors
- [ ] ChimeraSceneBootstrap initializes all systems
- [ ] ECS World created
- [ ] All ScriptableObjects load
- [ ] UI Canvas renders
- [ ] EventSystem active

**Gameplay Tests**
- [ ] Player responds to WASD/arrow keys
- [ ] Mouse look rotates camera
- [ ] Jump works
- [ ] Weapons fire correctly
- [ ] Chimera partner spawns near player
- [ ] Enemies spawn at spawn points
- [ ] Combat damage registers
- [ ] Health bars update
- [ ] Ammo counter updates
- [ ] Score increases on kills

**Activity System Tests**
- [ ] Pre-activity panel displays
- [ ] Difficulty selection works
- [ ] Chimera selection works
- [ ] Activity starts on button click
- [ ] Timer counts down
- [ ] Performance calculated real-time
- [ ] Activity ends correctly
- [ ] Results panel shows correct data

**Partnership Tests**
- [ ] Bond strength affects cooperation bonus
- [ ] Chimera AI supports player
- [ ] Partnership values update post-activity
- [ ] Emotional state changes

**Performance Tests**
- [ ] 60 FPS with 10 enemies
- [ ] No memory leaks
- [ ] No GC spikes
- [ ] ECS systems efficient

**UI Tests**
- [ ] All HUD elements visible
- [ ] Text readable
- [ ] Buttons clickable
- [ ] Panels fade smoothly
- [ ] Numbers animate

---

## Third-Person Shooter (TPS)

### Scene Name
`DemoScene_ThirdPersonShooter`

### Key Differences from FPS
- Camera positioned behind player (offset: 0, 2, -5)
- Full player character model visible
- Cover system more prominent
- Aiming reticle instead of crosshair
- Over-the-shoulder aiming mode

### Unique Components
- `ThirdPersonController` (replaces FPSController)
- `CoverSystemController`
- `ThirdPersonCameraController`

### Additional UI Elements
- Cover indicator (shows when near cover)
- Aim-down-sights overlay
- Character stance indicator (standing/crouching/prone)

### Setup Highlights
Follow FPS setup but:
1. Replace `FPSController` with `ThirdPersonController`
2. Add player character 3D model with animator
3. Position camera behind player (use Cinemachine if available)
4. Add cover detection system
5. Update UI for TPS-specific elements

---

## Fighting Game

### Scene Name
`DemoScene_Fighting`

### Complete GameObject Hierarchy
```
DemoScene_Fighting
├── ChimeraSceneBootstrap ⭐
├── WorldManager
├── Main Camera (side view, fixed)
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
│   ├── ArenaFloor (10x10)
│   ├── ArenaBoundaries (4 walls)
│   └── CornerPosts (4 posts)
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
├── Player2_Controller (AI)
│   └── AIFighterController
│
├── Player2_Chimera
│
└── Audio
    ├── AnnouncerAudio
    └── CrowdAmbience
```

### Component Details

#### Player1_Controller

**Transform:**
- Position: (-3, 0, 0)
- Rotation: (0, 90, 0) [facing right]

**Components:**
| Component | Type | Key Properties |
|-----------|------|----------------|
| `CharacterController` | Unity | Height: 1.8, Radius: 0.5 |
| `FighterController` | MonoBehaviour | Move Speed: 5.0 |
| | | Jump Force: 8.0 |
| | | Dash Speed: 15.0 |
| | | Block Reduction: 0.7 |
| `ComboSystem` | MonoBehaviour | Max Combo: 8 |
| | | Timeout: 1.0s |
| | | Combo List: ComboList_Default.asset |
| `FighterAnimator` | MonoBehaviour | Animator: FighterAnimController |
| `PlayerInputHandler` | MonoBehaviour | Input: FightingInputActions |
| `PlayerSkillTracker` | MonoBehaviour | Skill: Reflexes |
| `PartnershipController` | MonoBehaviour | Partner: Player1_Chimera |

### UI Hierarchy

#### Fighting_HUD

```
Fighting_HUD
├── Player1_Section (Left)
│   ├── HealthBar
│   │   Anchor: Top-Left (20, -20)
│   │   Size: (500, 50)
│   │   Fill Direction: Left-to-Right
│   ├── ChimeraHealthBar
│   │   Anchor: Top-Left (20, -80)
│   │   Size: (400, 35)
│   ├── ComboCounter
│   │   Anchor: Left-Center (30, 0)
│   │   Size: (150, 150)
│   │   Animated number display
│   ├── SpecialMeter
│   │   Anchor: Bottom-Left (40, 40)
│   │   Size: (60, 300)
│   │   5 segments, fills bottom-to-top
│   └── PlayerPortrait
│
├── Player2_Section (Right - mirrored)
│   [Same as Player1 but mirrored]
│
├── CenterSection
│   ├── TimerDisplay
│   │   Anchor: Top-Center (0, -30)
│   │   Size: (200, 80)
│   ├── RoundIndicator
│   │   ├── "ROUND 1"
│   │   └── WinIndicators (circles for wins)
│   └── AnnouncementText
│       Displays: "FIGHT!", "K.O.!", "PERFECT!"
│       Anchor: Center (0, 0)
│       Size: (800, 200)
│
└── PerformanceIndicator (Bottom-Center)
    Displays cooperation/bond bonus
```

### Setup Steps

**Step 1-2:** Base scene + ActivityCenter (as with FPS)

**Step 3: Setup Fighting Arena**
1. Create `FightingArena` parent
2. Add floor plane 10x10
3. Create 4 boundary walls (BoxColliders)
4. Add 4 corner posts (visual markers)

**Step 4: Setup Player1**
1. Create `Player1_Controller` at (-3, 0, 0) facing right
2. Add all fighter components
3. Add fighter model with Animator
4. Assign combo list ScriptableObject

**Step 5: Setup Player1 Chimera**
1. Create `Player1_Chimera` near Player1
2. Add chimera authoring components
3. Configure fighting AI (Assist mode)

**Step 6: Setup Player2** (AI opponent)
1. Duplicate Player1 setup
2. Replace PlayerInputHandler with AIFighterController
3. Position at (3, 0, 0) facing left
4. Add AI chimera partner

**Step 7: Setup Camera**
1. Position for side view: (0, 3, -12)
2. Rotation: (10, 0, 0)
3. Add dynamic framing (keeps both fighters in view)

**Step 8: Setup Fighting HUD**
1. Create mirrored health bars
2. Add combo counters (both sides)
3. Add special meters (fills on attacks)
4. Create timer + round indicator
5. Setup announcement text with animations

**Step 9: Setup Audio**
1. Add announcer voice clips
2. Add crowd ambience
3. Add impact sound effects

### Testing Checklist

**Initialization**
- [ ] Both fighters spawn correctly
- [ ] Camera frames both fighters
- [ ] HUD displays all elements

**Combat**
- [ ] Light/heavy attacks work
- [ ] Blocking reduces damage
- [ ] Combos chain correctly
- [ ] Special moves execute
- [ ] Super moves work when meter full
- [ ] Grabs/throws functional

**Round System**
- [ ] Round starts with "FIGHT!"
- [ ] K.O. ends round
- [ ] Round indicator updates
- [ ] Best of 3 works
- [ ] Winner determined

**Partnership**
- [ ] Chimera assist attacks
- [ ] Bond affects meter fill
- [ ] Cooperation bonus applies

**Performance**
- [ ] 60 FPS during combat
- [ ] No hitching on specials
- [ ] Smooth animations

---

# Racing Genres

## Racing

### Scene Name
`DemoScene_Racing`

### Complete GameObject Hierarchy
```
DemoScene_Racing
├── ChimeraSceneBootstrap ⭐
├── WorldManager
├── Main Camera (Chase Cam)
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
│   ├── TrackMesh
│   ├── StartingGrid (8 positions)
│   ├── Checkpoints (10 triggers)
│   ├── FinishLine (trigger)
│   ├── TrackBoundaries (colliders)
│   └── BoostPads (10 pads)
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
│   ├── AIRacer_1 through AIRacer_7
│
├── CheckpointSystem
│   └── RaceProgressTracker
│
└── Audio
    ├── BackgroundMusic
    └── CrowdAmbience
```

### Component Details

#### PlayerVehicle

**Components:**
| Component | Type | Key Properties |
|-----------|------|----------------|
| `Rigidbody` | Unity | Mass: 1000, Drag: 0.5 |
| `VehicleController` | MonoBehaviour | Max Speed: 200 km/h |
| | | Acceleration: 15.0 |
| | | Braking: 25.0 |
| | | Turning: 3.0 |
| | | Drift Factor: 0.8 |
| `VehiclePhysics` | MonoBehaviour | 4 Wheel Colliders |
| | | Suspension: 50000 |
| | | Tire Friction: 1.0 |
| `PlayerInputHandler` | MonoBehaviour | RacingInputActions |
| `PlayerSkillTracker` | MonoBehaviour | Reflexes |
| `ChimeraPassengerSlot` | MonoBehaviour | Seat position |

#### ChimeraPassenger (Copilot)

**Components:**
| Component | Type | Key Properties |
|-----------|------|----------------|
| `EnhancedCreatureAuthoring` | IConvertGameObjectToEntity | Any species |
| `ChimeraCopilotAI` | MonoBehaviour | Abilities: |
| | | - Call Boost |
| | | - Call Shortcut |
| | | - Call Defense |
| | | - Call Recovery |
| | | Cooldowns: 10s each |

#### CheckpointSystem

**Components:**
| Component | Type | Key Properties |
|-----------|------|----------------|
| `RaceProgressTracker` | MonoBehaviour | Total Racers: 8 |
| | | Total Laps: 3 |
| | | Total Checkpoints: 10 |
| | | Tracks positions, lap times |

### UI Hierarchy

#### Racing_HUD

```
Racing_HUD
├── Speedometer (Bottom-Left)
│   ├── Background (circular gauge)
│   ├── Needle (rotates with speed)
│   ├── SpeedText "195"
│   └── UnitText "km/h"
│   Anchor: Bottom-Left (120, 120)
│   Size: (200, 200)
│
├── LapCounter (Top-Left)
│   ├── LapText "LAP"
│   └── LapNumber "2/3"
│   Anchor: Top-Left (20, -20)
│   Size: (200, 60)
│
├── LapTimer (Top-Center)
│   ├── CurrentLap "1:23.456"
│   ├── BestLap "1:21.234"
│   Anchor: Top-Center (0, -20)
│   Size: (400, 80)
│
├── PositionIndicator (Top-Right)
│   ├── Position "2"
│   ├── Suffix "nd"
│   └── Total "/8"
│   Anchor: Top-Right (-120, -20)
│   Size: (180, 120)
│
├── Minimap (Bottom-Right)
│   ├── TrackImage
│   ├── PlayerDot (red)
│   ├── OpponentDots (blue)
│   └── CheckpointMarkers
│   Anchor: Bottom-Right (-20, 20)
│   Size: (250, 250)
│
├── BoostMeter (Left-Center)
│   Fills on successful copilot boost
│   Anchor: Left-Center (40, 0)
│   Size: (60, 250)
│
├── CooperationIndicator (Bottom-Center)
│   ├── ChimeraPortrait
│   ├── BondBar
│   ├── BondPercent "78%"
│   └── AbilityButtons (4)
│   Anchor: Bottom-Center (0, 40)
│   Size: (500, 100)
│
├── WrongWayIndicator
│   Shows when going backward
│   Anchor: Center (0, 100)
│   Size: (400, 150)
│
└── CountdownOverlay
    "3... 2... 1... GO!"
    Anchor: Center (0, 0)
    Size: (600, 400)
```

### Setup Steps

**Step 1: Base scene + ActivityCenter**

**Step 2: Create Race Track**
1. Create `RaceTrack` parent
2. Import/create track mesh (2.5km loop)
3. Add `RaceTrackManager`
4. Create 8 starting positions in grid
5. Place 10 checkpoints around track (BoxCollider triggers)
6. Place finish line trigger
7. Add boundary colliders around track
8. Place 10 boost pads at strategic points

**Step 3: Setup PlayerVehicle**
1. Create `PlayerVehicle` with 3D model
2. Add Rigidbody (Mass: 1000)
3. Add `VehicleController`
4. Add `VehiclePhysics` with 4 WheelColliders
5. Configure wheel suspension and friction
6. Add `PlayerInputHandler`
7. Add engine AudioSource

**Step 4: Setup ChimeraPassenger**
1. Create `ChimeraPassenger` as child of PlayerVehicle
2. Position in passenger seat
3. Add `EnhancedCreatureAuthoring`
4. Add `ChimeraCopilotAI` with 4 abilities
5. Configure cooldowns

**Step 5: Setup AI Racers**
1. Duplicate PlayerVehicle 7 times
2. Replace PlayerInputHandler with AIRacingController
3. Add AI chimera passenger to each
4. Assign different starting positions
5. Configure AI difficulty

**Step 6: Setup CheckpointSystem**
1. Create `CheckpointSystem`
2. Add `RaceProgressTracker`
3. Register all 10 checkpoints
4. Configure lap counting (3 laps)

**Step 7: Setup Camera**
1. Position behind player vehicle
2. Add chase camera script
3. Offset: (0, 2, -5)
4. Smooth follow with rotation

**Step 8: Setup Racing HUD**
1. Create speedometer with animated needle
2. Create lap counter and timer
3. Create position indicator (1st, 2nd, etc.)
4. Create minimap showing track + racers
5. Add boost meter
6. Add copilot ability buttons
7. Add wrong-way indicator
8. Add countdown overlay

**Step 9: Setup Audio**
1. Background music
2. Engine sounds for all vehicles
3. Boost sound effects
4. Countdown sounds

### Testing Checklist

**Initialization**
- [ ] Track displays correctly
- [ ] All 8 vehicles at starting grid
- [ ] Camera behind player
- [ ] HUD shows all elements

**Race Start**
- [ ] Countdown "3...2...1...GO!"
- [ ] Vehicles locked during countdown
- [ ] Race starts after GO
- [ ] Timer starts

**Racing Mechanics**
- [ ] Vehicle accelerates/brakes
- [ ] Steering works
- [ ] Drifting functional
- [ ] Boost pads give speed boost
- [ ] Collisions with barriers
- [ ] Collisions with other vehicles

**Checkpoint System**
- [ ] Checkpoints trigger in order
- [ ] Cannot skip checkpoints
- [ ] Finish line after all checkpoints
- [ ] Lap counter increments
- [ ] Wrong-way indicator shows correctly

**Chimera Copilot**
- [ ] Visible in vehicle
- [ ] Boost ability increases speed
- [ ] Shortcut ability shows racing line
- [ ] Defense ability shields collisions
- [ ] Recovery ability helps after crashes
- [ ] Abilities on cooldown
- [ ] Bond affects effectiveness

**UI**
- [ ] Speedometer updates real-time
- [ ] Lap counter correct
- [ ] Timer accurate
- [ ] Position updates (1st/2nd/etc.)
- [ ] Minimap shows positions
- [ ] Boost meter fills/depletes
- [ ] Ability buttons work

**AI**
- [ ] AI follows racing line
- [ ] AI avoids collisions
- [ ] AI uses boost pads
- [ ] AI respects checkpoints

**Race Completion**
- [ ] Race ends after 3 laps
- [ ] Final positions correct
- [ ] Results panel shows
- [ ] Performance calculated
- [ ] Rewards given

**Performance**
- [ ] 60 FPS with 8 vehicles
- [ ] No stuttering
- [ ] Fast loading

---

# Puzzle Genres

## Match-3

### Scene Name
`DemoScene_Match3`

### Complete GameObject Hierarchy
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
│   ├── GridBackground (9x9)
│   └── Tiles (runtime populated)
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
├── ParticleEffects
│   ├── MatchEffect
│   ├── ComboEffect
│   └── SpecialTileEffect
│
└── Audio
    ├── BackgroundMusic
    └── MatchSoundEffects
```

### Component Details

#### PuzzleGrid

**Components:**
| Component | Type | Key Properties |
|-----------|------|----------------|
| `GridManager` | MonoBehaviour | Width: 9, Height: 9 |
| | | Cell Size: 1.0 |
| | | Tile Types: 6 colors |
| | | Special Tile Chance: 0.1 |
| | | Gravity: true |
| | | Auto-Refill: true |
| `GridVisualizer` | MonoBehaviour | Draws grid lines |

#### TileFactory

**Tile Prefabs Required:**
- RedTile.prefab
- BlueTile.prefab
- GreenTile.prefab
- YellowTile.prefab
- PurpleTile.prefab
- OrangeTile.prefab
- BombTile.prefab (destroys 3x3)
- LineTile.prefab (destroys row/column)
- ColorBombTile.prefab (destroys all of one color)

Each tile has:
- SpriteRenderer (0.9x0.9)
- BoxCollider2D (1x1)
- TileComponent (data)
- Rigidbody2D (gravity)
- TileAnimator (animations)

**Components:**
| Component | Type | Key Properties |
|-----------|------|----------------|
| `TileSpawner` | MonoBehaviour | Tile Prefabs: 6 basic + 3 special |
| | | Pool Size: 100 |
| | | Spawn Anim Duration: 0.3s |

#### MatchDetectionSystem

**Components:**
| Component | Type | Key Properties |
|-----------|------|----------------|
| `MatchDetector` | MonoBehaviour | Min Match: 3 |
| | | Horizontal: true |
| | | Vertical: true |
| | | Diagonal: false |
| | | Special Patterns: T, L, 4-match, 5-match |
| `ComboTracker` | MonoBehaviour | Timeout: 2.0s |
| | | Multipliers: x1, x1.2, x1.5, x2, x3 |

#### ChimeraAssistant

**Transform:**
- Position: (12, 0, 0) [right of grid]
- Scale: (0.5, 0.5, 0.5) [smaller]

**Components:**
| Component | Type | Key Properties |
|-----------|------|----------------|
| `EnhancedCreatureAuthoring` | IConvertGameObjectToEntity | Any species |
| `ChimeraPuzzleAI` | MonoBehaviour | Abilities: |
| | | - Hint (highlights match) |
| | | - Shuffle (reshuffles board) |
| | | - Extra Moves (+3 moves) |
| | | - Combo Boost (multiplier) |
| | | Cooldowns: 15s |
| | | Auto-Hint: 10s delay |
| `Animator` | Unity | Idle, Thinking, Excited, Happy, Sad |

### UI Hierarchy

#### Match3_HUD

```
Match3_HUD
├── ScoreSection (Top-Left)
│   ├── ScoreLabel "SCORE"
│   ├── ScoreText "12,450"
│   ├── TargetLabel "Target:"
│   └── TargetText "20,000"
│   Anchor: Top-Left (20, -20)
│   Size: (250, 100)
│
├── MovesCounter (Top-Center-Left)
│   ├── Label "MOVES"
│   └── MovesText "15"
│   Anchor: Top-Center (-100, -20)
│   Size: (180, 90)
│
├── TimerDisplay (Top-Center-Right)
│   ├── Icon (clock)
│   └── TimeText "3:45"
│   Anchor: Top-Center (100, -20)
│   Size: (180, 90)
│
├── ProgressBar (Top-Right)
│   ├── FillBar (fills left-to-right)
│   ├── Label "Progress:"
│   └── Percent "62%"
│   Anchor: Top-Right (-20, -20)
│   Size: (300, 80)
│
├── ComboDisplay (Left-Center)
│   ├── Label "COMBO"
│   ├── ComboCount (large, animated)
│   └── Multiplier "x2.0"
│   Anchor: Left-Center (30, 0)
│   Size: (200, 200)
│
├── ObjectivesPanel (Right-Center)
│   ├── Label "Objectives:"
│   ├── Objective1
│   │   ├── Icon (red tile)
│   │   ├── Description "Collect 20 Red"
│   │   └── Progress "12/20"
│   ├── Objective2
│   │   ├── Icon (blue tile)
│   │   ├── Description "Collect 15 Blue"
│   │   └── Progress "15/15" [checkmark]
│   └── Objective3
│       ├── Icon (bomb)
│       ├── Description "Create 3 Bombs"
│       └── Progress "1/3"
│   Anchor: Right-Center (-30, 0)
│   Size: (280, 250)
│
├── ChimeraAssistancePanel (Bottom-Right)
│   ├── Portrait (circular)
│   ├── BondBar "82%"
│   └── AbilityButtons
│       ├── HintButton (+ cooldown)
│       ├── ShuffleButton (+ cooldown)
│       ├── ExtraMovesButton (+ cooldown)
│       └── ComboBoostButton (+ cooldown)
│   Anchor: Bottom-Right (-30, 30)
│   Size: (320, 150)
│
├── PerformanceBar (Right edge, vertical)
│   Anchor: Right (-15, 0)
│   Size: (30, 400)
│
└── HintOverlay (on grid)
    ├── HintArrow1 (animated)
    └── HintArrow2 (shows swap)
```

### Setup Steps

**Step 1: Base Scene**
1. Create scene
2. Core template
3. Set camera Orthographic
4. Position camera: (4.5, 4.5, -10) [centered on 9x9 grid]

**Step 2: ActivityCenter**
1. Create ActivityCenter
2. Add Match3ActivityAuthoring
3. Create Genre_Match3.asset (ProblemSolving skill, Intelligence trait)
4. Create Activity_Match3.asset (Grid: 9x9, Target: 20000, Moves: 30)

**Step 3: Create Tile Prefabs**
1. Create 6 basic tile prefabs (different sprites/colors)
2. Each: Sprite 0.9x0.9, BoxCollider2D 1x1, Rigidbody2D, TileComponent, TileAnimator
3. Create 3 special tile prefabs (Bomb, Line, ColorBomb)
4. Save to `Assets/_Project/Prefabs/Match3/`

**Step 4: Setup PuzzleGrid**
1. Create `PuzzleGrid` at (0, 0, 0)
2. Add GridManager (9x9, cell size 1.0)
3. Add GridBackground sprite
4. Add GridVisualizer

**Step 5: Setup TileFactory**
1. Create `TileFactory`
2. Add TileSpawner
3. Assign all 9 tile prefabs
4. Set pool size 100

**Step 6: Setup MatchDetectionSystem**
1. Create `MatchDetectionSystem`
2. Add MatchDetector (min match 3, special patterns)
3. Add ComboTracker (timeout 2s, multipliers)

**Step 7: Setup ChimeraAssistant**
1. Create `ChimeraAssistant` at (12, 0, 0), scale 0.5
2. Add EnhancedCreatureAuthoring
3. Add ChimeraPuzzleAI (4 abilities, cooldowns 15s)
4. Add Animator with states

**Step 8: Setup Particle Effects**
1. Create MatchEffect (plays on tile match)
2. Create ComboEffect (plays on combo)
3. Create SpecialTileEffect (plays on special activation)

**Step 9: Setup HUD**
1. Create all HUD elements per hierarchy
2. Add Match3HUDController
3. Add ComboAnimator
4. Add ObjectivesTracker
5. Add HintVisualizer

**Step 10: Test Grid**
1. Play mode
2. Verify 9x9 grid generates
3. No auto-matches on spawn
4. Test tile swapping
5. Verify match detection
6. Test gravity/refill

### Testing Checklist

**Initialization**
- [ ] Grid generates 9x9
- [ ] No pre-existing matches
- [ ] Chimera assistant appears
- [ ] HUD displays

**Tile Interaction**
- [ ] Select tile
- [ ] Swap adjacent tiles
- [ ] Cannot swap non-adjacent
- [ ] Cannot swap if no match
- [ ] Smooth animations

**Match Detection**
- [ ] 3-horizontal detected
- [ ] 3-vertical detected
- [ ] 4-match creates Bomb
- [ ] 5-match creates ColorBomb
- [ ] T-shape creates Line
- [ ] L-shape creates Line
- [ ] Matched tiles disappear

**Grid Physics**
- [ ] Tiles fall after match
- [ ] Natural gravity
- [ ] New tiles spawn from top
- [ ] Cascading matches
- [ ] No gaps

**Combo System**
- [ ] Combo increments on cascade
- [ ] Multiplier applies
- [ ] Resets after 2s
- [ ] Display animates
- [ ] High combos trigger effects

**Special Tiles**
- [ ] Bomb destroys 3x3
- [ ] Line destroys row/column
- [ ] ColorBomb destroys color
- [ ] Combining specials

**Chimera Assistance**
- [ ] Hint highlights match
- [ ] Auto-hint after 10s
- [ ] Shuffle works
- [ ] Extra moves granted
- [ ] Combo boost works
- [ ] Cooldowns work
- [ ] Bond affects effectiveness
- [ ] Animations match state

**Objectives**
- [ ] Progress tracks
- [ ] Collect tiles updates
- [ ] Create specials updates
- [ ] Checkmark on complete

**Score/Moves**
- [ ] Score increases
- [ ] Multiplier applies
- [ ] Move counter decrements
- [ ] Game ends at 0 moves

**Win/Lose**
- [ ] Win if target + objectives
- [ ] Lose if 0 moves
- [ ] Results panel

**UI**
- [ ] Score updates
- [ ] Moves correct
- [ ] Progress bar fills
- [ ] Objectives update
- [ ] Hint arrows display

**Performance**
- [ ] 60 FPS
- [ ] No lag on cascades
- [ ] Efficient pooling

---

# Strategy Genres

## Real-Time Strategy (RTS)

### Scene Name
`DemoScene_RTS`

### Core Concept
Player controls multiple units in real-time, managing resources, building structures, and commanding units in tactical combat. Chimera partner acts as a hero unit with special abilities.

### GameObject Hierarchy (Summary)
```
DemoScene_RTS
├── Core Template
├── ActivityCenter (RTSActivityAuthoring)
├── StrategicMap
│   ├── TerrainMesh
│   ├── GridSystem (for pathfinding)
│   ├── ResourceNodes (minerals, energy)
│   ├── FogOfWar
│   └── CapturePoints
├── PlayerBase
│   ├── CommandCenter (main building)
│   ├── ConstructionYard
│   └── InitialUnits (workers, scouts)
├── ChimeraCommander (hero unit)
├── EnemyBase
│   ├── EnemyCommandCenter
│   └── EnemyUnits
├── UI (RTS_HUD, Minimap, BuildMenu, UnitSelection)
└── Audio
```

### Key Components
- `RTSGameManager` - Resource management, win conditions
- `UnitSelectionSystem` (ECS) - Multi-unit selection
- `PathfindingSystem` (ECS) - A* pathfinding on grid
- `FogOfWarSystem` (ECS) - Visibility management
- `BuildingConstructionSystem` - Structure placement
- `ResourceGatheringSystem` (ECS) - Resource collection
- `ChimeraCommanderAI` - Hero unit abilities

### UI Elements
- **Minimap** - Top-right, shows map overview
- **Resource Counter** - Top-center (minerals, energy, population)
- **Unit Selection Panel** - Bottom-left (selected units)
- **Build Menu** - Bottom-right (structure/unit construction)
- **Command Card** - Bottom-center (unit abilities)

### Setup Highlights
1. Create strategic map with grid (50x50)
2. Place resource nodes
3. Setup player/enemy bases
4. Configure fog of war
5. Implement unit selection (box select)
6. Setup pathfinding system
7. Create build menu UI
8. Add minimap rendering

---

## Turn-Based Strategy

### Scene Name
`DemoScene_TurnBasedStrategy`

### Core Concept
Grid-based tactical combat where player and enemies take turns moving units and attacking. Chimera partner is a powerful unit with unique abilities.

### GameObject Hierarchy (Summary)
```
DemoScene_TurnBasedStrategy
├── Core Template
├── ActivityCenter (TurnBasedActivityAuthoring)
├── TacticalGrid
│   ├── HexGrid (or SquareGrid)
│   ├── Terrain (varied height)
│   └── CoverPositions
├── PlayerUnits
│   ├── Unit_1 through Unit_6
│   └── ChimeraCommander
├── EnemyUnits
│   ├── Enemy_1 through Enemy_8
├── TurnManager
│   └── TurnPhaseController
├── UI (TBS_HUD, UnitInfo, ActionMenu, TurnIndicator)
└── Audio
```

### Key Components
- `TurnManager` - Controls turn phases
- `TacticalGridManager` - Grid management
- `MovementRangeCalculator` - Valid move highlighting
- `AttackRangeCalculator` - Attack range display
- `LineOfSightChecker` - Visibility/cover
- `ChimeraTacticalAI` - Chimera special moves

### UI Elements
- **Turn Indicator** - Top-center "Player Turn" / "Enemy Turn"
- **Unit Info Panel** - Left side (health, AP, abilities)
- **Action Menu** - Bottom-center (Move, Attack, Ability, End Turn)
- **Grid Highlighting** - Movement range (blue), attack range (red)
- **Initiative Order** - Right side (turn order)

### Setup Highlights
1. Create grid (hexagonal or square)
2. Place player/enemy units
3. Setup turn manager
4. Configure movement/attack ranges
5. Implement grid highlighting
6. Add unit abilities
7. Create action menu
8. Setup AI for enemy units

---

# Rhythm Genres

## Rhythm Game

### Scene Name
`DemoScene_Rhythm`

### Core Concept
Notes scroll down lanes, player must hit them in time with music. Chimera partner provides visual/audio feedback and can trigger special effects.

### GameObject Hierarchy (Summary)
```
DemoScene_Rhythm
├── Core Template
├── ActivityCenter (RhythmActivityAuthoring)
├── MusicSystem
│   ├── AudioSource (music track)
│   ├── BeatDetector
│   └── NoteSpawner
├── NoteHighway
│   ├── Lane_1 through Lane_4
│   ├── HitZone
│   └── JudgmentLine
├── ChimeraPerformer
│   ├── EnhancedCreatureAuthoring
│   └── ChimeraRhythmAI (dances, cheers)
├── ParticleEffects
│   ├── PerfectHitEffect
│   ├── GoodHitEffect
│   └── ComboEffect
├── UI (Rhythm_HUD, ScoreDisplay, ComboCounter, Accuracy)
└── Audio
```

### Key Components
- `MusicPlayer` - Plays track, syncs timing
- `BeatDetector` - Detects beats for note spawning
- `NoteSpawner` - Spawns notes based on chart
- `NoteTimingSystem` (ECS) - Calculates hit timing
- `AccuracyCalculator` - Perfect/Good/Miss judgment
- `ComboTracker` - Combo multiplier
- `ChimeraRhythmAI` - Reacts to player performance

### UI Elements
- **Score Display** - Top-center
- **Combo Counter** - Center (large, animated)
- **Accuracy Meter** - Top-right (Perfect/Good/Miss counts)
- **Note Chart** - Center (scrolling notes)
- **Judgment Text** - Center ("PERFECT!", "GOOD", "MISS")
- **Performance Bar** - Shows current performance %

### Setup Highlights
1. Create 4-lane note highway
2. Setup beat detector for music track
3. Create note prefabs (different colors per lane)
4. Configure timing windows (Perfect: ±50ms, Good: ±100ms)
5. Setup chimera performer with dance animations
6. Create hit effects (particles)
7. Implement combo system
8. Create rhythm HUD

---

# RPG Genres

## Action RPG

### Scene Name
`DemoScene_ActionRPG`

### Core Concept
Real-time combat with RPG elements (levels, equipment, abilities). Chimera partner fights alongside player with its own abilities and growth.

### GameObject Hierarchy (Summary)
```
DemoScene_ActionRPG
├── Core Template
├── ActivityCenter (ActionRPGActivityAuthoring)
├── DungeonArea
│   ├── DungeonFloor
│   ├── Rooms (procedural or handcrafted)
│   ├── EnemySpawners
│   ├── LootChests
│   └── ExitPortal
├── PlayerCharacter
│   ├── ActionRPGController
│   ├── EquipmentSlots
│   ├── SkillBar
│   └── InventoryManager
├── ChimeraCompanion
│   ├── EnhancedCreatureAuthoring
│   └── ChimeraRPGAI (companion AI)
├── Enemies (various types)
├── Loot (weapons, armor, consumables)
├── UI (RPG_HUD, Inventory, Character Sheet, Skills)
└── Audio
```

### Key Components
- `ActionRPGController` - Combat + movement
- `EquipmentSystem` - Weapon/armor management
- `SkillSystem` - Active abilities with cooldowns
- `LevelingSystem` - XP gain, level up
- `LootGenerator` - Random item drops
- `ChimeraRPGAI` - Companion combat AI
- `DungeonManager` - Room spawning, enemy waves

### UI Elements
- **Health/Mana Bars** - Bottom-left
- **Skill Bar** - Bottom-center (1-5 hotkeys)
- **Inventory** - Tab to open
- **Character Sheet** - Stats, equipment
- **Chimera Companion Panel** - Shows bond, level, abilities
- **Loot Notification** - Pops up on item pickup

### Setup Highlights
1. Create dungeon area (rooms, corridors)
2. Setup player with equipment slots
3. Add skill bar with 5 abilities
4. Configure chimera companion AI
5. Setup enemy spawners
6. Create loot system
7. Implement inventory UI
8. Add level-up effects

---

# Simulation Genres

## Life Simulation

### Scene Name
`DemoScene_LifeSim`

### Core Concept
Player manages chimera's daily life (feeding, playing, training, bonding). Focus on relationship building and chimera well-being.

### GameObject Hierarchy (Summary)
```
DemoScene_LifeSim
├── Core Template
├── ActivityCenter (LifeSimActivityAuthoring)
├── HomeEnvironment
│   ├── LivingArea
│   ├── FeedingArea
│   ├── PlayArea
│   └── SleepingArea
├── PlayerCharacter
│   └── InteractionController
├── ChimeraLifeSim
│   ├── EnhancedCreatureAuthoring
│   ├── NeedsComponent (hunger, happiness, energy, hygiene)
│   ├── MoodComponent
│   └── PersonalityEvolution
├── InteractableObjects
│   ├── FoodBowl
│   ├── Toys
│   ├── Bed
│   └── BathTub
├── UI (LifeSim_HUD, NeedsPanel, MoodIndicator, Activities)
└── Audio
```

### Key Components
- `NeedsManagement System` (ECS) - Hunger, happiness, energy decay
- `MoodCalculator` - Determines chimera's emotional state
- `InteractionHandler` - Player interaction with objects
- `PersonalityEvolutionSystem` (ECS) - Traits change over time
- `DailyRoutineManager` - Time of day, scheduling
- `BondStrengthTracker` - Relationship progression

### UI Elements
- **Needs Bars** - Top-left (hunger, happiness, energy, hygiene)
- **Mood Indicator** - Top-right (Happy, Neutral, Sad, Stressed)
- **Activity Menu** - Bottom-center (Feed, Play, Train, Bond)
- **Time Display** - Top-center (day/time)
- **Bond Level** - Bottom-right (relationship strength)

### Setup Highlights
1. Create home environment with areas
2. Setup needs system (decay over time)
3. Add interactable objects (food, toys, bed)
4. Configure mood calculations
5. Implement daily routine (morning/afternoon/night)
6. Create activity interactions
7. Setup personality evolution
8. Add bond strength progression

---

## Management/Tycoon

### Scene Name
`DemoScene_Management`

### Core Concept
Player manages a chimera sanctuary/breeding facility. Manage resources, breed chimeras, expand facilities, keep chimeras happy.

### GameObject Hierarchy (Summary)
```
DemoScene_Management
├── Core Template
├── ActivityCenter (ManagementActivityAuthoring)
├── Sanctuary
│   ├── Facilities (enclosures, breeding centers, training halls)
│   ├── ChimeraEnclosures (multiple)
│   ├── VisitorAreas
│   └── StaffBuildings
├── ManagedChimeras (10-20)
│   [Multiple chimeras with various needs]
├── Visitors (NPCs that pay for entry)
├── Staff (keepers, trainers, vets)
├── UI (Management_HUD, BuildMenu, FinancePanel, ChimeraManagement)
└── Audio
```

### Key Components
- `SanctuaryManager` - Overall facility management
- `FinanceSystem` - Income, expenses, budgeting
- `ConstructionSystem` - Build/upgrade facilities
- `ChimeraPopulationManager` - Manages all chimeras
- `VisitorSystem` - Visitor AI, satisfaction
- `StaffManagementSystem` - Hire/assign staff
- `BreedingScheduler` - Automates breeding programs

### UI Elements
- **Finance Panel** - Top-right (income, expenses, balance)
- **Build Menu** - Bottom-right (facilities to construct)
- **Chimera List** - Left panel (all chimeras, needs, health)
- **Visitor Satisfaction** - Bottom-left
- **Staff Panel** - Manage employees
- **Notifications** - Chimera needs attention, low funds, etc.

### Setup Highlights
1. Create sanctuary layout
2. Setup finance system
3. Add construction menu
4. Configure chimera population (10-20)
5. Implement visitor AI
6. Setup staff management
7. Create management UI
8. Add notification system

---

# Sports Genres

## Sports (General)

### Scene Name
`DemoScene_Sports`

### Core Concept
Chimeras compete in sports (soccer, basketball, racing, etc.) with player controlling one chimera or coaching team.

### GameObject Hierarchy (Summary)
```
DemoScene_Sports
├── Core Template
├── ActivityCenter (SportsActivityAuthoring)
├── SportsField
│   ├── FieldMesh (stadium, court, etc.)
│   ├── Goals/Hoops
│   ├── Boundaries
│   └── ScoreZones
├── PlayerTeam
│   ├── Chimera_1 through Chimera_5
├── OpponentTeam
│   ├── OpponentChimera_1 through Chimera_5
├── Ball/Equipment
├── Referee (AI)
├── Crowd (audio/visual)
├── UI (Sports_HUD, Scoreboard, TeamStats, Controls)
└── Audio
```

### Key Components
- `SportsGameManager` - Rules, scoring, win conditions
- `TeamManagementSystem` - Formation, strategy
- `BallPhysicsController` - Ball movement
- `RefereeAI` - Enforces rules
- `ChimeraSportsController` - Chimera sports abilities
- `StaminaSystem` - Chimeras tire over time
- `ScoreTracker` - Goals, points, etc.

### UI Elements
- **Scoreboard** - Top-center (Team A vs Team B, time)
- **Team Stats** - Bottom (stamina, position)
- **Controls** - Context-sensitive (Pass, Shoot, Tackle)
- **Play Timer** - Countdown clock
- **Replays** - Slow-mo on goals

### Setup Highlights
1. Create sports field (soccer pitch, basketball court, etc.)
2. Setup teams (5v5)
3. Configure ball physics
4. Implement sport-specific rules
5. Add referee AI
6. Setup stamina system
7. Create scoreboard UI
8. Add replay system

---

# Additional Genres

## Platformer

**Scene:** `DemoScene_Platformer`

**Core:** Player controls chimera jumping across platforms, avoiding hazards, collecting items.

**Key Elements:**
- Platforming environment (platforms, moving platforms, springs)
- Player chimera controller (jump, double-jump, wall-jump)
- Hazards (spikes, enemies, pits)
- Collectibles (coins, power-ups)
- Checkpoint system

## Metroidvania

**Scene:** `DemoScene_Metroidvania`

**Core:** Interconnected world with abilities unlocking new areas.

**Key Elements:**
- Large interconnected map
- Ability gates (need double-jump to reach area, etc.)
- Save/fast-travel points
- Map system (reveals explored areas)
- Power-ups/upgrades

## Visual Novel

**Scene:** `DemoScene_VisualNovel`

**Core:** Story-driven dialogue with choices, chimera relationship building.

**Key Elements:**
- Character portraits (chimera, NPCs)
- Dialogue system (typewriter effect)
- Choice system (affects relationship)
- Background images (scenes)
- Bond-based story branches

---

## Summary

This guide covers detailed setups for:
- ✅ **Action Genres** (FPS, TPS, Fighting) - Complete with hierarchies, components, UI, steps, tests
- ✅ **Racing Genre** - Complete detailed setup
- ✅ **Puzzle Genres** (Match-3) - Complete detailed setup
- ✅ **Strategy Genres** (RTS, TBS) - Summary templates
- ✅ **Rhythm Genre** - Summary template
- ✅ **RPG Genres** (Action RPG) - Summary template
- ✅ **Simulation Genres** (Life Sim, Management) - Summary templates
- ✅ **Sports Genre** - Summary template
- ✅ **Additional Genres** (Platformer, Metroidvania, Visual Novel) - Quick summaries

**For Full Detail:**
The first 4 sections (FPS, Fighting, Racing, Match-3) provide complete, production-ready setups with:
- Full GameObject hierarchies
- All component details with properties
- Complete UI layouts with positioning
- Step-by-step setup instructions
- Comprehensive testing checklists

**For Other Genres:**
Summary templates provide the structure. Follow the detailed pattern from the complete genres to expand as needed.

---

**Next Steps:**
- Use these genre templates to create your activity demo scenes
- Refer back to [DEMO_SCENE_SETUP_GUIDE.md](./DEMO_SCENE_SETUP_GUIDE.md) for core requirements
- Follow the testing checklists to validate each scene
