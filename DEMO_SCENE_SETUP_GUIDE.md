# Project Chimera - Demo Scene Setup Guide
**Quick reference index for creating demo scenes**

---

## 📋 Overview

This guide provides a comprehensive reference for creating demo scenes for all game modes and systems in Project Chimera. The documentation is split into specialized guides for easier navigation.

---

## 📚 Guide Index

### 1. **Core Scene Template**
Every demo scene starts with the same foundational elements. See [Core Scene Requirements](#core-scene-requirements) below.

### 2. **Genre/Activity Demo Scenes**
Detailed scene setups for all 47 activity genres.

📖 **See: [GENRE_SCENES_GUIDE.md](./GENRE_SCENES_GUIDE.md)**

Covers:
- **Action Genres** (FPS, Third-Person Shooter, Fighting, Beat'Em Up, Hack & Slash, Stealth, Survival Horror)
- **Racing Genres** (Racing, Kart Racing, Combat Racing)
- **Puzzle Genres** (Match-3, Tetris, Physics Puzzle, Logic Puzzle, Hidden Object, Tile Matching)
- **Strategy Genres** (RTS, Turn-Based Strategy, 4X Strategy, Grand Strategy, Auto Battler, Tower Defense)
- **Rhythm/Music Genres** (Rhythm, Music)
- **RPG Genres** (Action RPG, Turn-Based RPG, Roguelike, MMORPG, Dungeon Crawler)
- **Simulation Genres** (Life Sim, Management, Tycoon, Farming, City Builder)
- **Sports Genres** (Sports, Fighting Sports, Racing Sports)

Each genre includes:
- Complete GameObject hierarchies
- Detailed component specifications
- UI layouts with exact positioning
- Step-by-step setup instructions
- Comprehensive testing checklists

### 3. **System Demo Scenes**
Focused demos for testing individual systems.

📖 **See: [SYSTEM_DEMOS_GUIDE.md](./SYSTEM_DEMOS_GUIDE.md)**

Covers:
- **Breeding System Demo** - Genetics, compatibility, offspring generation
- **Partnership/Bonding Demo** - Bond strength, emotional states, personality evolution
- **AI Behavior Demo** - Creature needs, decision-making, behavior states
- **Save/Load System Demo** - Persistence, migration, data integrity
- **Performance Test Scenes** - Stress testing, profiling, optimization validation

---

## Core Scene Requirements

### Every Demo Scene Must Have:

```
[SceneName]
├── ChimeraSceneBootstrap ⭐ (Required)
├── WorldManager
├── Main Camera
├── Directional Light
├── UI Canvas
│   ├── DebugPanel
│   ├── PerformancePanel
│   ├── SceneControlsPanel
│   └── [Scene-Specific UI]
├── EventSystem
└── Environment (parent for scene-specific objects)
```

---

## 1. ChimeraSceneBootstrap (Required)

### GameObject Setup
```
Name: "ChimeraSceneBootstrap"
Tag: Untagged
Layer: Default
```

### Components
| Component | Configuration |
|-----------|---------------|
| `ChimeraSceneBootstrapper` | Game Config: ChimeraGameConfig.asset |
| | Auto Spawn Test Creatures: true/false |
| | Debug Mode: true (for demo scenes) |
| `ConvertToEntity` | Convert and Destroy |

### Required ScriptableObject
- **Path**: `Assets/_Project/Resources/Configs/ChimeraGameConfig.asset`
- **Type**: `ChimeraGameConfig`
- **Must Reference**:
  - Available species configs
  - Biome configurations
  - Performance settings
  - Default genre library

---

## 2. WorldManager

### GameObject Setup
```
Name: "WorldManager"
Tag: Untagged
Layer: Default
```

### Components
| Component | Purpose |
|-----------|---------|
| `World` | Auto-created by DOTS (Default World) |
| `SubScene` (optional) | ECS entity container for pre-placed entities |

**SubScene Usage:**
- Use for large numbers of pre-placed entities
- Location: `Assets/_Project/Scenes/SubScenes/`
- Naming: `[SceneName]_SubScene`

---

## 3. Main Camera

### GameObject Setup
```
Name: "Main Camera"
Tag: MainCamera
Layer: Default
Position: (0, 10, -10)
Rotation: (30, 0, 0)
```

### Components
| Component | Configuration |
|-----------|---------------|
| `Camera` | Clear Flags: Skybox, FOV: 60 |
| | Near: 0.3, Far: 1000 |
| | HDR: true, MSAA: true |
| `AudioListener` | Default |
| `CameraController` (optional) | Movement Speed: 10, Zoom Speed: 5 |

---

## 4. Directional Light

### GameObject Setup
```
Name: "Directional Light"
Tag: Untagged
Layer: Default
Position: (0, 10, 0)
Rotation: (50, -30, 0)
```

### Components
| Component | Configuration |
|-----------|---------------|
| `Light` | Type: Directional, Intensity: 1, Color: White |

---

## 5. UI Canvas

### GameObject Setup
```
Name: "UI Canvas"
Tag: Untagged
Layer: UI
```

### Components
| Component | Configuration |
|-----------|---------------|
| `Canvas` | Render Mode: Screen Space - Overlay |
| `CanvasScaler` | UI Scale Mode: Scale With Screen Size |
| | Reference Resolution: 1920x1080 |
| | Match: 0.5 |
| `GraphicRaycaster` | Default |

### Standard UI Children (All Scenes)

#### DebugPanel (Top-Left)
```
DebugPanel
├── Background (Image)
├── FPSText (TextMeshPro)
├── EntityCountText (TextMeshPro)
├── SystemStatsText (TextMeshPro)
└── MemoryUsageText (TextMeshPro)

Component: DebugPanelController
Anchor: Top-Left (10, -10)
Size: (300, 150)
```

#### PerformancePanel (Top-Right)
```
PerformancePanel
├── Background (Image)
├── FrameTimeGraph (Image - custom graph)
├── JobTimeText (TextMeshPro)
├── AllocationText (TextMeshPro)
└── WarningIcon (Image)

Components: PerformanceMonitor, GraphRenderer
Anchor: Top-Right (-10, -10)
Size: (350, 200)
```

#### SceneControlsPanel (Bottom-Center)
```
SceneControlsPanel
├── Background (Image)
├── PauseButton (Button)
├── RestartButton (Button)
├── TimeScaleSlider (Slider)
└── SettingsButton (Button)

Component: SceneControlsController
Anchor: Bottom-Center (0, 10)
Size: (600, 80)
```

---

## 6. EventSystem

### GameObject Setup
```
Name: "EventSystem"
Tag: Untagged
Layer: Default
```

### Components
| Component | Configuration |
|-----------|---------------|
| `EventSystem` | Default |
| `StandaloneInputModule` | Default |

---

## Quick Start Checklist

### For Any Demo Scene:

**Phase 1: Base Setup**
- [ ] Create new scene in Unity
- [ ] Delete default camera and light
- [ ] Add ChimeraSceneBootstrap with ChimeraGameConfig reference
- [ ] Add WorldManager
- [ ] Add Main Camera with appropriate position/rotation
- [ ] Add Directional Light
- [ ] Add UI Canvas with DebugPanel, PerformancePanel, SceneControlsPanel
- [ ] Add EventSystem

**Phase 2: Scene-Specific Setup**
- [ ] Add ActivityCenter (for genre scenes) OR system-specific managers
- [ ] Add required GameObjects for scene type
- [ ] Configure all components with properties
- [ ] Create and assign required ScriptableObjects
- [ ] Setup scene-specific UI

**Phase 3: Testing**
- [ ] Verify scene loads without errors
- [ ] Confirm ChimeraSceneBootstrap initializes
- [ ] Check ECS World creation
- [ ] Test all ScriptableObject references load
- [ ] Validate UI displays correctly
- [ ] Run scene-specific gameplay tests

---

## ScriptableObject Asset Requirements

### Every Scene Needs:

1. **ChimeraGameConfig.asset**
   - Path: `Assets/_Project/Resources/Configs/ChimeraGameConfig.asset`
   - Type: `ChimeraGameConfig`

2. **GenreLibrary.asset** (for activity scenes)
   - Path: `Assets/_Project/Resources/Configs/GenreLibrary.asset`
   - Type: `GenreLibrary`
   - Contains all 47 genre configs

3. **Genre-Specific Configs** (per activity scene)
   - Path: `Assets/_Project/Resources/Configs/GenreConfigurations/Genre_[Name].asset`
   - Type: `GenreConfiguration`

4. **Activity Configs** (per activity scene)
   - Path: `Assets/_Project/Resources/Configs/Activities/Activity_[Name].asset`
   - Type: `ActivityConfig`

5. **Species Configs** (for each chimera used)
   - Path: `Assets/_Project/Resources/Configs/Species/[SpeciesName].asset`
   - Type: `ChimeraSpeciesConfig`

---

## Common Patterns

### Pattern 1: Activity Scene Structure
```
DemoScene_[GenreName]
├── Core Template (Bootstrap, Camera, UI, etc.)
├── ActivityCenter (genre-specific)
├── PlayerController (input + skill tracking)
├── ChimeraPartner (bonded chimera)
├── Genre-Specific Environment
├── Genre-Specific UI
└── Audio
```

### Pattern 2: System Demo Structure
```
DemoScene_[SystemName]
├── Core Template
├── System Manager (specific to system)
├── Test Data/Entities
├── Debug UI (system-specific)
├── Control Panel (for testing)
└── Visual Debuggers
```

### Pattern 3: Performance Test Structure
```
DemoScene_StressTest
├── Core Template
├── Mass Spawner (stress generator)
├── Performance Monitor (detailed metrics)
└── Test Controls (spawn/clear/configure)
```

---

## Design Principles

### 1. **Designer-Friendly**
Every scene should be configurable without code changes. Use ScriptableObjects for all parameters.

### 2. **Drop-and-Play**
Scenes should work immediately after dropping required prefabs. Minimal manual setup.

### 3. **Complete Testing**
Each scene must validate:
- Initialization (systems start correctly)
- Gameplay (mechanics work as expected)
- Performance (60 FPS target met)
- UI (all elements display and respond)
- Partnership (chimera cooperation functional)

### 4. **Consistent Structure**
All scenes follow the same base template. Scene-specific elements are additions, not replacements.

### 5. **Performance-First**
Demo scenes must maintain performance targets:
- 60 FPS minimum
- No memory leaks
- No excessive GC allocations
- ECS systems optimized

---

## Troubleshooting

### Scene Won't Load
1. Check ChimeraGameConfig.asset exists and is assigned
2. Verify all required ScriptableObjects are created
3. Check for missing assembly references
4. Look for errors in Console during scene load

### ECS Systems Not Initializing
1. Verify ChimeraSceneBootstrapper component is present
2. Check ConvertToEntity is set to "Convert and Destroy"
3. Ensure WorldManager exists in scene
4. Check ECS systems are registered in bootstrap

### UI Not Displaying
1. Verify UI Canvas exists with CanvasScaler
2. Check EventSystem is present
3. Confirm UI elements have GraphicRaycaster
4. Verify anchors and positions are correct

### Performance Issues
1. Check entity count (use DebugPanel)
2. Profile with Unity Profiler + ECS Profiler
3. Verify Burst compilation is enabled
4. Check for excessive GameObject allocations
5. Review Job System batch sizes

### Chimera Not Spawning
1. Check species config is assigned
2. Verify EnhancedCreatureAuthoring component present
3. Confirm ConvertToEntity component attached
4. Check Auto Spawn is enabled (if using spawner)
5. Look for errors in creature authoring system

---

## Next Steps

1. **Choose your scene type**:
   - Building an activity demo? → See [GENRE_SCENES_GUIDE.md](./GENRE_SCENES_GUIDE.md)
   - Testing a system? → See [SYSTEM_DEMOS_GUIDE.md](./SYSTEM_DEMOS_GUIDE.md)

2. **Follow the detailed guide** for your scene type

3. **Use the checklists** to validate your scene

4. **Test thoroughly** before moving to next scene

---

## Reference Quick Links

- **Genre Scenes Guide**: [GENRE_SCENES_GUIDE.md](./GENRE_SCENES_GUIDE.md)
- **System Demos Guide**: [SYSTEM_DEMOS_GUIDE.md](./SYSTEM_DEMOS_GUIDE.md)
- **Main Project README**: [README.md](./README.md)
- **Developer Guide**: [DEVELOPER_GUIDE.md](./DEVELOPER_GUIDE.md)
- **Genre System Guide**: [GENRE_SYSTEM_GUIDE.md](./GENRE_SYSTEM_GUIDE.md)

---

**Use this index to quickly navigate to the specific scene type you need to build. Each specialized guide provides complete, step-by-step instructions with detailed component configurations and testing procedures.**
