# Project Chimera - Systems Testing Guide

Comprehensive test harness for validating all ECS systems at scale (1000+ creatures).

## 🧪 Test Harness Overview

**ChimeraSystemsTestHarness.cs** is a MonoBehaviour that:
- Spawns 100-5000 creatures with varied genetics
- Automatically starts activities (Racing, Combat, Puzzle)
- Equips equipment items with stat bonuses
- Initializes progression (leveling, XP, daily challenges)
- Displays real-time FPS and statistics
- Demonstrates Burst compilation and Job system performance

## 🚀 Quick Start

### Method 1: Empty Scene Setup
1. Create new scene or open existing scene
2. Create empty GameObject (right-click in Hierarchy → Create Empty)
3. Rename to "SystemsTestHarness"
4. Add Component → Search "Chimera Systems Test Harness"
5. Configure settings in Inspector
6. Press Play

### Method 2: Existing Scene
1. Add "ChimeraSystemsTestHarness" component to any GameObject
2. Ensure configs exist in `Resources/Configs/`
3. Press Play

## ⚙️ Configuration Options

### Test Configuration
| Parameter | Range | Default | Description |
|-----------|-------|---------|-------------|
| Creature Count | 100-5000 | 1000 | Number of creatures to spawn |
| Active Creature % | 0-100 | 30 | Percentage doing activities |
| Auto Start Test | bool | true | Start automatically on scene load |

### System Tests
| Parameter | Description |
|-----------|-------------|
| Test Activities | Enable activity spawning (Racing, Combat, Puzzle) |
| Test Equipment | Enable equipment assignment |
| Test Progression | Enable progression components |

### Performance Monitoring
| Parameter | Description |
|-----------|-------------|
| Show Stats | Display FPS overlay (always recommended) |
| Enable Profiling | Log detailed performance metrics to console |

## 📊 What Gets Tested

### 1. Creature Spawning (ECS Entities)
- **Components**: CreatureGeneticsComponent, CurrencyComponent, 8 other components
- **Genetics**: Random stats (20-100) across 6 attributes
- **Starting State**: Level 1-10, 1000 coins, 10 gems
- **Validation**: All entities created successfully with proper archetype

### 2. Activity System
- **Activities**: Random selection from loaded configs
- **Difficulties**: Random (Easy → Master)
- **Active %**: Configurable (default 30% of creatures)
- **Expected**: Parallel job processing, completion tracking

### 3. Equipment System
- **Assignment**: Random items from loaded configs
- **Coverage**: 50% of creatures get 1 item
- **Types**: All rarities (Common → Legendary)
- **Expected**: Stat bonuses applied, bonus cache updated

### 4. Progression System
- **Levels**: Random starting levels (1-10)
- **XP Tracking**: Experience accumulation from activities
- **Skill Points**: Calculated based on level
- **Daily Challenges**: Initialized for all creatures

### 5. Performance Targets
| Metric | Target | Acceptable | Warning |
|--------|--------|-----------|---------|
| FPS (1000 creatures) | 60+ | 50-60 | <50 |
| Frame Time | <16.67ms | <20ms | >20ms |
| Entity Creation | <500ms | <1000ms | >1000ms |
| Activity Processing | <1ms/creature | <2ms | >2ms |

## 🎮 Using the Test Harness

### Runtime Controls (On-Screen GUI)
- **Performance Section**: Real-time FPS and frame time
- **Entities Section**: Creature count, active activities
- **Systems Section**: Loaded configs and active systems
- **Actions**:
  - `Restart Test`: Destroy all entities and respawn
  - `Start More Activities`: Launch additional activities

### Console Logs
```
[Test Harness] Starting test with 1000 creatures
[Test Harness] Active percentage: 30%
[Test Harness] Spawned 1000 creatures with varied genetics
[Test Harness] Started 300 random activities
[Test Harness] Equipped 500 items across creatures
[Test Harness] Test initialization complete!
```

### Profiling Output (if enabled)
```
[Test Harness] FPS: 62.3, Frame: 16.05ms, Active: 287
[Test Harness] FPS: 61.8, Frame: 16.18ms, Active: 291
```

## 🔍 Validation Checklist

### Pre-Test Validation
- ✅ Configs loaded: Activities (3), Equipment (5), Progression (1)
- ✅ ECS World initialized (World.DefaultGameObjectInjectionWorld exists)
- ✅ Systems registered: ActivitySystem, EquipmentSystem, ProgressionSystem

### During Test
- ✅ FPS maintains 60+ with 1000 creatures
- ✅ Activities complete successfully (check console for "Activity completed" logs)
- ✅ Equipment bonuses apply (check genetics + bonuses in Entity Inspector)
- ✅ XP accumulates (level up events in console)
- ✅ No errors or exceptions

### Post-Test
- ✅ All entities cleaned up on "Restart Test"
- ✅ Memory usage stable (no leaks)
- ✅ Profiler shows Burst-compiled jobs executing

## 🐛 Troubleshooting

### "No activity configurations found"
**Cause**: Configs not in Resources/Configs/Activities/
**Fix**: Check config files exist, verify directory structure

### "Loaded 0 activity configs"
**Cause**: ActivityConfig assets not properly created
**Fix**: Right-click → Create → Chimera → Activities → [Type] Config

### FPS <30 with 1000 creatures
**Cause**: Burst compilation not enabled OR Jobs not scheduling
**Fix**: Check Unity → Jobs → Burst → Enable Compilation

### "Entity doesn't exist" errors
**Cause**: Entity destroyed before async job completes
**Fix**: Check EntityManager.Exists() before operations

### Activities never complete
**Cause**: ActivitySystem not updating OR config duration = 0
**Fix**: Check system is in SimulationSystemGroup, verify config durations

## 📈 Performance Profiling

### Unity Profiler Setup
1. Open Unity Profiler (Window → Analysis → Profiler)
2. Enable these modules:
   - **CPU Usage**: Shows system update times
   - **Rendering**: Graphics overhead
   - **Memory**: Allocation tracking
3. Look for these markers:
   - `Activity.ProcessRequests` (should be <1ms)
   - `Activity.UpdateActive` (Burst-compiled job, <2ms)
   - `Equipment.UpdateBonusCache` (should be <1ms)
   - `Progression.ProcessExperience` (should be <0.5ms)

### ECS Profiler (Deep Dive)
1. Window → Analysis → ECS → Systems
2. Check system update times:
   - ActivitySystem: Target <3ms for 1000 creatures
   - EquipmentSystem: Target <2ms
   - ProgressionSystem: Target <1ms
3. Verify Burst icons (⚡) on jobs
4. Check parallel job worker threads (should use all CPU cores)

### Expected Profiler Results (1000 creatures, 30% active)
| System | Time/Frame | Notes |
|--------|------------|-------|
| ActivitySystem | 2-4ms | Includes CheckActivityCompletionJob (parallel) |
| EquipmentSystem | 1-2ms | Bonus cache updates |
| ProgressionSystem | 0.5-1ms | XP awards, level ups |
| **Total ECS** | **4-7ms** | Leaves 9-12ms for rendering/other |

## 🎯 Test Scenarios

### Scenario 1: Stress Test (5000 creatures)
```csharp
creatureCount = 5000
activeCreaturePercentage = 50
```
**Expected**: 40-50 FPS (acceptable), 20-25ms frame time
**Purpose**: Find performance ceiling

### Scenario 2: Activity Focus (1000 creatures, 100% active)
```csharp
creatureCount = 1000
activeCreaturePercentage = 100
```
**Expected**: 50-60 FPS, high parallel job utilization
**Purpose**: Test job system saturation

### Scenario 3: Equipment Heavy (1000 creatures)
Modify `EquipRandomItems()` to equip 5 items per creature
**Expected**: Bonus cache updates dominate performance
**Purpose**: Test equipment system caching

### Scenario 4: Progression Cascade
Manually trigger 1000 "activity completed" events
**Expected**: Mass XP awards, multiple level ups
**Purpose**: Test progression batching

## 📝 Adding Custom Tests

### Example: Test Specific Activity Type
```csharp
// In StartRandomActivities(), replace random selection:
var racingConfig = loadedActivityConfigs
    .FirstOrDefault(c => c.activityType == ActivityType.Racing);

if (racingConfig != null)
{
    Entity request = _entityManager.CreateEntity();
    _entityManager.AddComponentData(request, new StartActivityRequest
    {
        monsterEntity = entities[i],
        activityType = ActivityType.Racing,
        difficulty = ActivityDifficulty.Master, // Always master difficulty
        requestTime = Time.time
    });
}
```

### Example: Test Equipment Set Bonuses
```csharp
// Equip full "Racer's Kit" on specific creatures
var racerBoots = loadedEquipmentConfigs
    .FirstOrDefault(e => e.equipmentSetName == "Racer's Kit");
// Add logic to equip all set pieces
```

## 🚦 Success Criteria

### ✅ Test Passes If:
1. **Spawns** 1000 creatures successfully (<1 second)
2. **Maintains** 60 FPS with 30% active creatures
3. **Completes** activities with proper rewards
4. **Applies** equipment bonuses correctly
5. **Awards** XP and processes level ups
6. **No errors** in console during 5-minute run
7. **Memory stable** (no continuous growth)

### ⚠️ Test Warnings If:
1. FPS 50-60 (workable but not optimal)
2. Occasional frame spikes >20ms
3. Activities take longer than configured duration +10%

### ❌ Test Fails If:
1. FPS <30 consistently
2. Errors or exceptions thrown
3. Activities never complete
4. Memory leak detected
5. System deadlocks or hangs

## 🔧 Advanced Configuration

### Modify Creature Genetics Distribution
In `SpawnCreatures()`:
```csharp
// Create specialized creatures (high intelligence)
_entityManager.SetComponentData(creature, new CreatureGeneticsComponent
{
    strength = _random.NextFloat(20f, 40f),
    agility = _random.NextFloat(20f, 40f),
    intelligence = _random.NextFloat(80f, 100f), // High INT
    vitality = _random.NextFloat(40f, 60f),
    social = _random.NextFloat(20f, 40f),
    adaptability = _random.NextFloat(40f, 60f)
});
```

### Test Specific Level Range
```csharp
// All creatures start at level 50
int startLevel = 50; // Instead of random
```

### Test Daily Challenge System
```csharp
// In Update(), track challenge completion:
var challengeQuery = _entityManager.CreateEntityQuery(typeof(DailyChallengeElement));
var challengeEntities = challengeQuery.ToEntityArray(Allocator.Temp);
foreach (var entity in challengeEntities)
{
    var challenges = _entityManager.GetBuffer<DailyChallengeElement>(entity);
    int completed = challenges.Count(c => c.challenge.isCompleted);
    // Log or display completed count
}
challengeEntities.Dispose();
```

## 📚 Integration with Unity Test Framework

This harness complements Unity's test framework:
- **Play Mode Tests**: Use harness for integration tests
- **Performance Tests**: Capture FPS metrics programmatically
- **Unit Tests**: Test individual system logic

---

**Test Harness is ready for immediate use!** 🎉

Drop into any scene, press Play, and watch 1000+ creatures interact with all systems at 60 FPS.
