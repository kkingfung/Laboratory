# Project Chimera - Developer Guide

**Welcome to Project Chimera!** This guide will help you get started with development, understand the codebase structure, and contribute effectively.

---

## 📚 Table of Contents

1. [Getting Started](#getting-started)
2. [Development Environment](#development-environment)
3. [Project Structure](#project-structure)
4. [Core Concepts](#core-concepts)
5. [Common Tasks](#common-tasks)
6. [Testing & Debugging](#testing--debugging)
7. [Contributing](#contributing)
8. [Best Practices](#best-practices)
9. [FAQ](#faq)

---

## Getting Started

### Prerequisites

**Required:**
- Unity 6 (6000.2.0b7 or later)
- Git with Git LFS enabled
- .NET 8.0 SDK
- 16GB RAM minimum (32GB recommended for large scenes)
- SSD storage (for faster iteration times)

**Optional but Recommended:**
- Visual Studio 2022 or Rider
- Unity Profiler knowledge
- Basic understanding of Unity ECS

### Initial Setup

```bash
# 1. Clone the repository
git clone <repository-url>
cd Laboratory

# 2. Install Git LFS (if not already installed)
git lfs install
git lfs pull

# 3. Open in Unity Hub
# - Add project from disk
# - Select Unity 6 (6000.2.0b7+)
# - Wait for package resolution and compilation

# 4. Verify setup
# - Open any scene in Assets/_Project/Scenes/
# - Press Play
# - Check console for errors (should be clean)
```

### First Run Checklist

- [ ] Unity opens without errors
- [ ] All packages resolved successfully
- [ ] No compilation errors
- [ ] Can enter Play mode
- [ ] Sample scene runs at 60 FPS

---

## Development Environment

### Recommended IDE Setup

#### Visual Studio 2022
```
Extensions:
- ReSharper (optional, for advanced refactoring)
- Unity Tools for Visual Studio
- EditorConfig Language Service

Settings:
- Enable "Attach to Unity"
- Set C# formatting to Unity conventions
- Enable "Break All Processes" for debugging
```

#### JetBrains Rider
```
Plugins:
- Unity Support
- Heap Allocations Viewer
- Burst Compiler Support

Settings:
- Enable Unity integration
- Set code style to Unity
- Configure Burst compilation checks
```

### Unity Configuration

**Editor Settings:**
```
Edit → Preferences:
├── External Tools
│   └── Set external script editor to VS/Rider
├── Jobs
│   └── Enable Burst Compilation
│   └── Set Safety Checks to Full (development)
└── Asset Pipeline
    └── Parallel Import (enable for speed)
```

**Project Settings:**
```
Edit → Project Settings:
├── Player
│   └── Scripting Backend: IL2CPP (production)
│   └── API Compatibility: .NET Standard 2.1
├── Quality
│   └── Set appropriate quality level for testing
└── Physics
    └── Configure collision layers as needed
```

---

## Project Structure

### High-Level Organization

```
Laboratory/
├── Assets/_Project/           ⭐ PRIMARY WORK AREA
│   ├── Scenes/                # Game scenes
│   ├── Scripts/               # All C# code (929 files)
│   ├── Prefabs/               # Reusable game objects
│   ├── Resources/             # Runtime-loaded assets
│   ├── Tests/                 # Unit & integration tests
│   └── Docs/                  # In-project documentation
│
├── Documentation Files (Root):
│   ├── README.md              # Project overview
│   ├── ARCHITECTURE.md        # Technical architecture
│   ├── DEVELOPER_GUIDE.md     # This file
│   ├── PROJECT_STATUS.md      # Current status
│   ├── TEAM_SYSTEM_REVIEW.md  # Integration review
│   ├── CLAUDE.md              # AI development guidelines
│   └── CI-CD-README.md        # Build pipeline docs
│
└── Configuration:
    ├── .github/workflows/     # CI/CD automation
    ├── ProjectSettings/       # Unity settings
    └── Packages/              # Package dependencies
```

### Scripts Organization

```
Assets/_Project/Scripts/
│
├── Core/                      # 42 fundamental systems
│   ├── Activities/            # Activity management
│   │   ├── ActivitySystem.cs (590 lines)
│   │   └── ActivityComponents.cs
│   │
│   ├── Combat/                # Combat mechanics
│   │   └── AdvancedCombatSystems.cs (1138 lines)
│   │
│   ├── Equipment/             # Gear & items
│   │   ├── EquipmentSystem.cs (642 lines)
│   │   └── EquipmentComponents.cs
│   │
│   ├── Progression/           # Leveling & XP
│   │   ├── ProgressionSystem.cs (619 lines)
│   │   └── ProgressionComponents.cs
│   │
│   └── [38 other core modules]
│
├── Chimera/                   # 28 genetic/breeding systems
│   ├── Genetics/              # 19 genetic files
│   │   ├── GeneticSystem.cs
│   │   ├── BreedingEngine.cs
│   │   └── [17 more files]
│   │
│   ├── ECS/                   # ECS integration
│   │   ├── Components/        # 40+ IComponentData structs
│   │   ├── Systems/           # 18+ SystemBase classes
│   │   └── README.md          # ECS integration guide
│   │
│   └── [26 other modules]
│
└── Subsystems/                # 35 feature subsystems
    ├── Team/                  # Team battles (3,710 lines)
    │   ├── UniversalTeamComponents.cs (540 lines)
    │   ├── MatchmakingSystem.cs (530 lines)
    │   ├── TutorialOnboardingSystem.cs (450 lines)
    │   ├── TeamCommunicationSystem.cs (540 lines)
    │   ├── GenreSpecificTeamSystems.cs (950 lines)
    │   ├── TeamSubsystemConfig.cs (440 lines)
    │   └── TeamSubsystemManager.cs (370 lines)
    │
    ├── Networking/            # 10 netcode modules
    ├── Combat/                # 6 combat modules
    └── [32 other subsystems]
```

---

## Core Concepts

### Unity ECS Architecture

Project Chimera uses **Entity Component System (ECS)** for high-performance simulation.

#### Key ECS Concepts

**1. Entities**
```csharp
// Entities are lightweight IDs (just an int)
Entity creature = entityManager.CreateEntity();
```

**2. Components (Data Only)**
```csharp
// IComponentData = pure data, no behavior
public struct CreatureIdentityComponent : IComponentData
{
    public FixedString64Bytes species;
    public FixedString64Bytes name;
    public int generation;
    public CreatureRarity rarity;
}
```

**3. Systems (Behavior)**
```csharp
// SystemBase = processes entities with specific components
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class CreatureAgingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Query for entities with age component
        Entities
            .ForEach((ref CreatureAgeComponent age) =>
            {
                age.currentAge += deltaTime * age.agingRate;
            })
            .ScheduleParallel(); // Run on all CPU cores
    }
}
```

### ScriptableObject Configuration

**All systems use ScriptableObjects for designer-friendly configuration:**

```csharp
// Create asset: Right-click → Create → Chimera → Species Config
[CreateAssetMenu(menuName = "Chimera/Species Config")]
public class ChimeraSpeciesConfig : ScriptableObject
{
    [Header("Identity")]
    public string speciesName;
    public Sprite icon;

    [Header("Genetics")]
    public GeneticTraitConfig[] geneticTraits;

    [Header("Behavior")]
    public BehaviorProfile behaviorProfile;

    // Designers can tweak without touching code!
}
```

### Authoring Component Pattern

**Bridge between MonoBehaviour (Editor) and ECS (Runtime):**

```csharp
public class CreatureAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public ChimeraSpeciesConfig speciesConfig;
    public int startingLevel = 1;

    public void Convert(Entity entity, EntityManager dstManager,
                        GameObjectConversionSystem conversionSystem)
    {
        // Convert editor data → ECS components
        dstManager.AddComponentData(entity, new CreatureIdentityComponent
        {
            species = speciesConfig.speciesName,
            generation = 0
        });

        dstManager.AddComponentData(entity, new ProgressionComponent
        {
            level = startingLevel,
            experience = 0
        });
    }
}
```

### 47-Genre Activity System

**Project Chimera features a comprehensive 47-genre activity system** where player skill and chimera traits combine for performance-based gameplay.

#### Architecture Overview

```
GenreLibrary (Master Index)
    ├── GenreConfiguration (47 configs)
    │   ├── PlayerSkill (15 types)
    │   ├── ChimeraTrait (15 types)
    │   ├── Performance calculations
    │   └── Reward scaling
    └── ActivityConfig (compatibility layer)
```

#### Quick Start for Designers

**1. Generate All Configurations** (One-time setup)
```
Tools → Chimera → Genre Configuration Generator
1. Click "Create Genre Library"
2. Click "Generate All 47 Genre Configurations"
3. Wait for completion (~1 minute)
```

**2. Customize a Genre**
```
Location: Assets/_Project/Resources/Configs/GenreConfigurations/
Example: Genre_Racing.asset

Fields to Customize:
- Base Duration: How long the activity takes
- Difficulty Scaling: Challenge multiplier
- Player Skill Weight: How much player performance matters
- Chimera Trait Weight: How much chimera stats matter
- Rewards: Currency, skill mastery, partnership gains
```

#### Performance Calculation

```csharp
// Genre-specific performance calculation
var result = GenrePerformanceCalculator.CalculateActivityResult(
    activityType: ActivityType.Racing,
    playerSkillValue: 0.8f,    // 80% player performance
    chimeraTraitValue: 0.9f,   // 90% chimera speed/agility
    bondStrength: 0.85f,       // 85% partnership bond
    chimeraAge: 120,           // 120 days old (adult)
    personalityTraits: new[] { "Competitive", "Brave" }
);

// Results
Debug.Log($"Performance: {result.performance}%");
Debug.Log($"Rank: {result.rank}"); // Failed/Bronze/Silver/Gold/Platinum
Debug.Log($"Reward: {result.currencyReward} coins");
Debug.Log($"Skill Gain: +{result.skillGain:F3}");
```

#### 47 Genre Categories

**Action (7):** FPS, TPS, Fighting, Beat Em Up, Hack and Slash, Stealth, Survival Horror
**Strategy (5):** RTS, Turn-Based, 4X, Grand Strategy, Auto Battler
**Puzzle (5):** Match-3, Tetris-Like, Physics Puzzle, Hidden Object, Word Game
**Adventure (4):** Point and Click, Visual Novel, Walking Sim, Metroidvania
**Platform (3):** 2D Platformer, 3D Platformer, Endless Runner
**Simulation (4):** Vehicle Sim, Flight Sim, Farming Sim, Construction Sim
**Arcade (4):** Roguelike, Roguelite, Bullet Hell, Classic Arcade
**Board & Card (3):** Board Game, Card Game, Chess-Like
**Core (10):** Exploration, Racing, Tower Defense, Battle Royale, City Builder, etc.
**Music (2):** Rhythm Game, Music Creation
**Legacy (12):** Combat, Puzzle, Strategy, Music, etc.

#### Adding a New Genre-Specific Minigame

```csharp
// 1. Choose genre from ActivityType enum
ActivityType myGenre = ActivityType.Racing;

// 2. Get genre configuration
var genreConfig = genreLibrary.GetGenreConfig(myGenre);

// 3. Use genre settings in your minigame
float duration = genreConfig.baseDuration; // 90 seconds
PlayerSkill requiredSkill = genreConfig.primaryPlayerSkill; // Reflexes
ChimeraTrait requiredTrait = genreConfig.primaryChimeraTrait; // Speed

// 4. Calculate performance when activity completes
float performance = genreConfig.CalculatePerformance(
    playerSkillValue,
    chimeraTraitValue,
    bondStrength,
    chimeraAge
);

// 5. Award rewards
int reward = genreConfig.CalculateReward(performance);
float skillGain = genreConfig.CalculateSkillGain(performance);
```

#### Integration with Existing Systems

**ActivitySystem (ECS):**
- Loads ActivityConfig assets from Resources
- Processes activity completion
- Awards currency and experience

**PartnershipActivitySystem:**
- Uses GenrePerformanceCalculator
- Calculates skill + cooperation dynamics
- Records skill improvements

**GenrePerformanceCalculator:**
- Bridge between configs and systems
- Handles all performance math
- Returns comprehensive results

**Implementation Details:** See code comments in `GenrePerformanceCalculator.cs` for usage examples

---

### Performance Principles

**1. Burst Compilation**
```csharp
// Compile to native code (10-50x speedup)
[BurstCompile]
public struct ProcessGeneticsJob : IJobEntity
{
    public void Execute(ref CreatureStatsComponent stats,
                        in ChimeraGeneticDataComponent genetics)
    {
        // This code runs as fast as C++
        stats.strength = genetics.strengthGenes * 10f;
    }
}
```

**2. Job System Parallelization**
```csharp
// Schedule work across all CPU cores
protected override void OnUpdate()
{
    var job = new ProcessGeneticsJob();
    this.Dependency = job.ScheduleParallel(this.Dependency);
    // Jobs run in parallel, dependency chain ensures order
}
```

**3. Zero Allocations**
```csharp
// Use stack-allocated collections
var creatures = creatureQuery.ToEntityArray(Allocator.Temp);
// Process creatures...
creatures.Dispose(); // No GC allocations!
```

---

## Common Tasks

### 1. Creating a New Creature Species

**Step 1: Create Species Config**
```
Right-click in Project → Create → Chimera → Species Config
Name: "FireDragonConfig"
```

**Step 2: Configure Genetics**
```csharp
// In Inspector:
Species Name: "Fire Dragon"
Base Stats:
  - Strength: 80
  - Agility: 60
  - Intelligence: 70

Genetic Traits:
  - Fire Affinity: Dominant
  - Flight Capability: Recessive
  - Size: Large (polygenic)
```

**Step 3: Add to Game Config**
```
Find: Resources/Configs/ChimeraGameConfig
Add to: Available Species array
```

**Step 4: Test**
```csharp
// In test scene, spawn creature:
var spawner = FindObjectOfType<CreatureSpawner>();
spawner.SpawnCreature("Fire Dragon");
```

### 2. Adding a New Activity Type

**Step 1: Create Activity Config**
```
Right-click → Create → Chimera → Activities → Activity Config
Name: "SkyRacingConfig"
```

**Step 2: Configure Stats**
```csharp
// In Inspector:
Activity Type: Racing
Primary Stat: Agility (50%)
Secondary Stat: Vitality (30%)
Tertiary Stat: Intelligence (20%)

Difficulty Levels:
  - Easy: 60s, 1.0x rewards
  - Normal: 90s, 1.5x rewards
  - Hard: 120s, 2.0x rewards
  - Expert: 150s, 2.5x rewards
  - Master: 180s, 3.0x rewards
```

**Step 3: Place in Resources**
```
Move to: Assets/_Project/Resources/Configs/Activities/
```

**Step 4: System Auto-Discovery**
```csharp
// ActivitySystem automatically loads all configs in Resources/Configs/Activities/
// No code changes needed!
```

### 3. Creating Equipment Items

**Step 1: Create Equipment Asset**
```
Right-click → Create → Chimera → Equipment → Equipment Item
Name: "DragonWings"
```

**Step 2: Configure Properties**
```csharp
// In Inspector:
Item ID: "dragon_wings_01" (must be unique!)
Item Name: "Dragon Wings"
Rarity: Epic
Slot: Accessory1

Stat Bonuses:
  - Agility: +35%
  - Adaptability: +20%

Activity Bonuses:
  - Racing: +25%
  - Platforming: +20%

Durability: 300 uses
Purchase Price: 8000 coins
```

**Step 3: Place in Resources**
```
Move to: Assets/_Project/Resources/Configs/Equipment/
```

### 4. Adding a New ECS System

**Step 1: Create System File**
```csharp
// Assets/_Project/Scripts/Core/YourFeature/YourSystem.cs
using Unity.Entities;
using Unity.Burst;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class YourSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Query entities and process
        Entities
            .ForEach((ref YourComponent component) =>
            {
                // Process logic here
                component.value += deltaTime;
            })
            .ScheduleParallel();
    }
}
```

**Step 2: Create Component**
```csharp
// Assets/_Project/Scripts/Core/YourFeature/YourComponents.cs
using Unity.Entities;

public struct YourComponent : IComponentData
{
    public float value;
    public int counter;
}
```

**Step 3: Create Authoring (if needed)**
```csharp
// Assets/_Project/Scripts/Core/YourFeature/YourAuthoring.cs
public class YourAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float initialValue = 0f;

    public void Convert(Entity entity, EntityManager dstManager,
                        GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new YourComponent
        {
            value = initialValue
        });
    }
}
```

**Step 4: Test**
```
1. Create GameObject in scene
2. Add YourAuthoring component
3. Set initialValue
4. Press Play
5. Check Entity Debugger (Window → Analysis → Entities)
```

### 5. Running Performance Tests

**Method 1: Test Harness (Quick)**
```
1. Open empty scene
2. Create empty GameObject
3. Add Component → "Chimera Systems Test Harness"
4. Configure:
   - Creature Count: 1000
   - Active %: 30
   - Enable all test systems
5. Press Play
6. Watch FPS counter (should be 60+)
```

**Method 2: Unit Tests (Automated)**
```
1. Open Test Runner (Window → General → Test Runner)
2. Select "PlayMode" tab
3. Run "PerformanceRegressionTests"
4. All tests should pass (green checkmarks)
```

**Method 3: Profiler (Deep Analysis)**
```
1. Window → Analysis → Profiler
2. Press Play
3. Look for:
   - CPU Usage: <16.67ms per frame
   - ECS systems: Check individual timings
   - Memory: No continuous growth
   - Rendering: GPU bottlenecks
```

---

## Testing & Debugging

### Unit Testing

**Location:** `Assets/_Project/Tests/EditMode/`

**Run Tests:**
```
Window → General → Test Runner
- EditMode: Unit tests (fast)
- PlayMode: Integration tests (slower)
```

**Example Test:**
```csharp
[Test]
public void Test_Genetics_TraitBlending()
{
    // Arrange
    var parent1 = new GeneticProfile { strength = 80 };
    var parent2 = new GeneticProfile { strength = 60 };

    // Act
    var offspring = GeneticsEngine.Breed(parent1, parent2);

    // Assert
    Assert.IsTrue(offspring.strength >= 60 && offspring.strength <= 80);
}
```

### Debugging ECS

**Entity Debugger:**
```
Window → Analysis → Entities → Hierarchy

Features:
- View all entities in world
- Inspect component data
- Filter by archetype
- Real-time updates
```

**System Debugger:**
```
Window → Analysis → ECS → Systems

Features:
- See system update order
- Check system timings
- Verify Burst compilation (⚡ icon)
- Debug system queries
```

**Common Debugging Techniques:**

**1. Check Entity Exists**
```csharp
if (!EntityManager.Exists(entity))
{
    Debug.LogWarning("Entity was destroyed!");
    return;
}
```

**2. Validate Component**
```csharp
if (!EntityManager.HasComponent<YourComponent>(entity))
{
    Debug.LogError("Missing YourComponent!");
    return;
}
```

**3. Log Component Data**
```csharp
var component = EntityManager.GetComponentData<YourComponent>(entity);
Debug.Log($"Value: {component.value}");
```

**4. Use Profiler Markers**
```csharp
using Unity.Profiling;

private static readonly ProfilerMarker s_ProcessMarker =
    new ProfilerMarker("YourSystem.Process");

protected override void OnUpdate()
{
    using (s_ProcessMarker.Auto())
    {
        // Your processing code
    }
}
```

### Common Issues & Solutions

**Issue: "Entity doesn't exist" errors**
```
Cause: Entity destroyed before async job completes
Solution: Check EntityManager.Exists() before operations
```

**Issue: FPS drops below 60**
```
Cause: Too many allocations or non-Burst code
Solution:
1. Check Profiler for hot paths
2. Enable Burst on jobs
3. Use native collections (NativeArray, etc.)
```

**Issue: "No activity configurations found"**
```
Cause: Configs not in Resources folder
Solution: Move configs to Assets/_Project/Resources/Configs/Activities/
```

**Issue: Activities never complete**
```
Cause: ActivitySystem not updating OR config duration = 0
Solution:
1. Verify system in SimulationSystemGroup
2. Check config durations are > 0
```

---

## Contributing

### Code Style

**Follow CLAUDE.md guidelines:**

```csharp
// Good: PascalCase for public
public class CreatureManager { }
public void ProcessCreatures() { }

// Good: camelCase with _ prefix for private
private int _creatureCount;
private void UpdateInternalState() { }

// Good: Methods under 50 lines
public void DoThing()
{
    // If method gets too long, split into:
    DoStep1();
    DoStep2();
    DoStep3();
}

// Good: XML documentation
/// <summary>
/// Processes creature genetics and updates behavior.
/// </summary>
/// <param name="entity">The creature entity to process</param>
/// <returns>True if processing succeeded</returns>
public bool ProcessGenetics(Entity entity) { }
```

### Git Workflow

**Branch Naming:**
```
claude/feature-description-sessionId
claude/fix-bug-description-sessionId

Examples:
claude/add-fire-element-01ABC123
claude/fix-breeding-crash-01XYZ789
```

**Commit Messages:**
```
feat: Add fire elemental affinity system
fix: Resolve breeding compatibility null reference
refactor: Optimize genetics processing with SIMD
docs: Update architecture documentation
test: Add unit tests for combat system
```

**Pull Request Process:**
```
1. Create feature branch
2. Make changes
3. Run tests (must pass)
4. Commit with clear messages
5. Push to remote
6. Create PR with description
7. Wait for CI/CD checks
8. Address review comments
9. Merge when approved
```

### Documentation

**Update docs when you:**
- Add new systems or features
- Change public APIs
- Modify configuration formats
- Fix significant bugs
- Improve performance

**Documentation locations:**
- Architecture changes → ARCHITECTURE.md
- Developer workflows → DEVELOPER_GUIDE.md
- Status updates → PROJECT_STATUS.md
- Inline code → XML comments

---

## Best Practices

### ECS Best Practices

**✅ DO:**
```csharp
// Use Burst compilation
[BurstCompile]
public struct MyJob : IJobEntity { }

// Use Jobs for parallelization
job.ScheduleParallel();

// Use native collections
var entities = query.ToEntityArray(Allocator.Temp);

// Cache queries
private EntityQuery _creatureQuery;

protected override void OnCreate()
{
    _creatureQuery = GetEntityQuery(typeof(CreatureComponent));
}
```

**❌ DON'T:**
```csharp
// Don't use managed references in components
public struct BadComponent : IComponentData
{
    public string name; // ❌ Not Burst-compatible
}

// Don't allocate in hot paths
Entities.ForEach(() => {
    var list = new List<int>(); // ❌ GC allocation
}).Run();

// Don't use LINQ in jobs
var filtered = entities.Where(e => condition); // ❌ Not Burst-compatible
```

### Performance Best Practices

**1. Profile Before Optimizing**
```
Use Profiler to find actual bottlenecks
Don't optimize blindly
```

**2. Minimize Allocations**
```csharp
// Good: Stack allocation
var temp = new NativeArray<int>(10, Allocator.Temp);
temp.Dispose();

// Bad: Heap allocation
var temp = new int[10]; // GC allocation
```

**3. Batch Operations**
```csharp
// Good: Batch entity creation
var archetype = EntityManager.CreateArchetype(
    typeof(Component1),
    typeof(Component2)
);
var entities = EntityManager.CreateEntity(archetype, 1000, Allocator.Temp);

// Bad: Individual creation
for (int i = 0; i < 1000; i++)
{
    var entity = EntityManager.CreateEntity();
    EntityManager.AddComponent<Component1>(entity); // Structural change each time
}
```

**4. Use ComponentLookup Wisely**
```csharp
// Cache lookups
[ReadOnly] public ComponentLookup<CreatureComponent> CreatureLookup;

// Reuse across job
CreatureLookup.Update(this);
```

### Configuration Best Practices

**1. ScriptableObjects for Everything**
```csharp
// ✅ Configuration in SO
[CreateAssetMenu]
public class GameConfig : ScriptableObject
{
    public int maxCreatures = 1000;
}

// ❌ Hardcoded values
public class GameManager
{
    private const int MAX_CREATURES = 1000; // Hard to change
}
```

**2. Validate Configurations**
```csharp
public class ActivityConfig : ScriptableObject
{
    public float statWeight1;
    public float statWeight2;

    private void OnValidate()
    {
        float total = statWeight1 + statWeight2;
        if (Mathf.Abs(total - 1.0f) > 0.01f)
        {
            Debug.LogWarning($"Stat weights should sum to 1.0, got {total}");
        }
    }
}
```

**3. Use Resources for Runtime Loading**
```
Place configs in: Assets/_Project/Resources/Configs/
Load at runtime: Resources.Load<ConfigType>("Configs/YourConfig")
```

---

## FAQ

### Q: How do I add a new creature to the game?

**A:** Create a `ChimeraSpeciesConfig` ScriptableObject, configure its genetics and behavior, then add it to the `ChimeraGameConfig.availableSpecies` array. No code changes needed!

### Q: Why is my FPS dropping below 60?

**A:** Common causes:
1. Burst compilation disabled (enable in Jobs → Burst)
2. Structural changes in hot path (use EntityCommandBuffer)
3. GC allocations (use native collections)
4. Too many creatures without LOD (implement distance culling)

Check Profiler to identify the exact bottleneck.

### Q: How do I test my changes?

**A:** Three methods:
1. **Manual Testing**: Press Play in Unity
2. **Test Harness**: Add `ChimeraSystemsTestHarness` to scene
3. **Unit Tests**: Run tests in Test Runner window

### Q: Where do I put new scripts?

**A:**
- Core gameplay: `Assets/_Project/Scripts/Core/[YourFeature]/`
- Genetics/breeding: `Assets/_Project/Scripts/Chimera/[YourFeature]/`
- Feature subsystem: `Assets/_Project/Scripts/Subsystems/[YourFeature]/`

### Q: How do I create a ScriptableObject config?

**A:**
```csharp
// 1. Define the class
[CreateAssetMenu(fileName = "NewConfig", menuName = "Chimera/YourConfig")]
public class YourConfig : ScriptableObject
{
    public int value;
}

// 2. Right-click in Project → Create → Chimera → YourConfig
// 3. Configure in Inspector
// 4. Reference in code:
public YourConfig config;
```

### Q: How do I debug ECS entities?

**A:**
1. `Window → Analysis → Entities → Hierarchy` - View all entities
2. Select entity → Inspect components in real-time
3. Use `EntityManager.GetComponentData<T>(entity)` to log values
4. Add profiler markers to track performance

### Q: What's the difference between Core/ and Chimera/?

**A:**
- **Core/**: Universal systems (activities, combat, equipment, progression)
  - Works for any game, not just creature breeding
- **Chimera/**: Genetics-specific systems (breeding, genetics, DNA)
  - Unique to Project Chimera's genetic simulation

### Q: How do I contribute?

**A:**
1. Read `CONTRIBUTING.md`
2. Create feature branch (`claude/feature-name-sessionId`)
3. Make changes following `CLAUDE.md` guidelines
4. Run tests
5. Commit with clear messages
6. Create pull request
7. Wait for CI/CD checks

### Q: Where can I find examples?

**A:**
- Test Harness: `Assets/_Project/Scripts/Chimera/Testing/ChimeraSystemsTestHarness.cs`
- Sample configs: `Assets/_Project/Resources/Configs/`
- Unit tests: `Assets/_Project/Tests/EditMode/`
- System examples: `Assets/_Project/Scripts/Chimera/ECS/Systems/`

---

## Additional Resources

### Documentation Files
- `README.md` - Project overview and features
- `ARCHITECTURE.md` - Technical architecture deep-dive
- `PROJECT_STATUS.md` - Current implementation status
- `TEAM_SYSTEM_REVIEW.md` - Integration opportunities
- `CLAUDE.md` - AI development guidelines
- `CONTRIBUTING.md` - Contribution guidelines
- `CI-CD-README.md` - Build pipeline documentation

### Unity Resources
- [Unity ECS Documentation](https://docs.unity3d.com/Packages/com.unity.entities@latest)
- [Burst Compiler](https://docs.unity3d.com/Packages/com.unity.burst@latest)
- [Job System](https://docs.unity3d.com/Manual/JobSystem.html)
- [Profiler](https://docs.unity3d.com/Manual/Profiler.html)

### Project-Specific Guides
- ECS Integration: `Assets/_Project/Scripts/Chimera/ECS/README.md`
- Configuration: `Assets/_Project/Resources/Configs/README_CONFIGS.md`
- Testing: `Assets/_Project/Scripts/Chimera/Testing/README_TESTING.md`

---

## Getting Help

**If you're stuck:**

1. **Check documentation** - This guide, README.md, ARCHITECTURE.md
2. **Search codebase** - Look for similar implementations
3. **Check console** - Unity console often has helpful errors
4. **Use debugger** - Entity Debugger, System Debugger, Profiler
5. **Review examples** - Test harness, unit tests, sample configs
6. **Ask for help** - Create GitHub issue with details

**When asking for help, include:**
- What you're trying to do
- What you've tried
- Error messages (full stack trace)
- Unity version
- Relevant code snippets

---

**Welcome to the team! Let's build amazing genetic simulations together!** 🧬🎮

*Last Updated: November 16, 2025*
