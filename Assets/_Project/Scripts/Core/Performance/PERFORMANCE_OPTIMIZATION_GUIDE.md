# ChimeraOS Performance Optimization Guide

## Quick Start: Adding [BurstCompile] to ECS Systems

### **CRITICAL: Add [BurstCompile] to All ECS Systems**

Burst compilation provides 10-100x performance improvement. Add it to **all** systems and jobs.

#### **Pattern 1: SystemBase Systems**

```csharp
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial class MySystem : SystemBase
{
    [BurstCompile]
    protected override void OnUpdate()
    {
        // System logic here
    }
}
```

#### **Pattern 2: ISystem Systems (Recommended)**

```csharp
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // System logic here
    }
}
```

#### **Pattern 3: Jobs**

```csharp
using Unity.Burst;
using Unity.Jobs;

[BurstCompile]
public struct MyJob : IJob
{
    public void Execute()
    {
        // Job logic here
    }
}
```

---

## Files Requiring [BurstCompile] (High Priority)

### **Activity Systems** (8 files)
1. `/Assets/_Project/Scripts/Core/Activities/Racing/RacingCircuitSystem.cs`
2. `/Assets/_Project/Scripts/Core/Activities/Combat/CombatArenaSystem.cs`
3. `/Assets/_Project/Scripts/Core/Activities/Puzzle/PuzzleAcademySystem.cs`
4. `/Assets/_Project/Scripts/Core/Activities/Strategy/StrategyCommandSystem.cs`
5. `/Assets/_Project/Scripts/Core/Activities/Music/RhythmStudioSystem.cs`
6. `/Assets/_Project/Scripts/Core/Activities/Adventure/AdventureQuestSystem.cs`
7. `/Assets/_Project/Scripts/Core/Activities/Platforming/PlatformingCourseSystem.cs`
8. `/Assets/_Project/Scripts/Core/Activities/Crafting/CraftingWorkshopSystem.cs`

### **Chimera ECS Systems** (15+ files)
- `/Assets/_Project/Scripts/Chimera/ECS/Systems/CreatureSimulationSystems.cs`
- `/Assets/_Project/Scripts/Chimera/ECS/Systems/ChimeraECSSystems.cs`
- `/Assets/_Project/Scripts/Chimera/ECS/Systems/EmergencyConservationSystem.cs`
- `/Assets/_Project/Scripts/Chimera/Breeding/BreedingSystem.cs`
- `/Assets/_Project/Scripts/Chimera/Genetics/GeneticPredictionSystem.cs`
- All other systems in `/Chimera/ECS/Systems/`

### **Core ECS Systems** (10+ files)
- `/Assets/_Project/Scripts/Core/ECS/LoadingScreenSystem.cs`
- `/Assets/_Project/Scripts/Core/Health/DamageSystem.cs`
- `/Assets/_Project/Scripts/Core/Equipment/EquipmentSystem.cs`
- `/Assets/_Project/Scripts/Core/Progression/AchievementSystem.cs`
- All other ECS systems in `/Core/`

### **Total: 46+ Systems Need [BurstCompile]**

---

## Performance Optimization Checklist

### **✅ Phase 1: Critical (Do First)**

#### 1. Replace FindObjectOfType Calls

**BEFORE:**
```csharp
void Update()
{
    var manager = FindFirstObjectByType<SaveDataManager>();
    manager.Save();
}
```

**AFTER:**
```csharp
private SaveDataManager _manager;

void Awake()
{
    _manager = FindFirstObjectByType<SaveDataManager>();
}

void Update()
{
    _manager?.Save();
}
```

**Files to Fix (33 files):**
- `PlayerAnalyticsTracker.cs`
- `ResearchSubsystemManager.cs`
- `PerformanceSubsystemManager.cs`
- All subsystem managers

---

#### 2. Use Object Pooling

**BEFORE:**
```csharp
GameObject monster = Instantiate(monsterPrefab, position, rotation);
// ...later...
Destroy(monster);
```

**AFTER:**
```csharp
// Initialization
MonsterPoolManager.Instance.CreatePool("DefaultMonster", monsterPrefab, 50);

// Spawning
GameObject monster = MonsterPoolManager.Instance.SpawnMonster("DefaultMonster", position, rotation);

// Despawning
MonsterPoolManager.Instance.DespawnMonster(monster, "DefaultMonster");
```

**Files to Fix:**
- `/Assets/Scripts/ChimeraNetworkManager.cs:260, 1013, 1075`
- Any other Instantiate/Destroy patterns

---

#### 3. Fix Memory Leaks - Event Cleanup

**BEFORE:**
```csharp
void Start()
{
    EventBus.Subscribe<GameEvent>(OnGameEvent);
}

// ❌ No OnDestroy - MEMORY LEAK!
```

**AFTER (Option A - Manual):**
```csharp
private IDisposable _subscription;

void Start()
{
    _subscription = EventBus.Subscribe<GameEvent>(OnGameEvent);
}

void OnDestroy()
{
    _subscription?.Dispose();
}
```

**AFTER (Option B - Automatic):**
```csharp
using Laboratory.Core.Performance;

public class MyClass : CleanupBehaviour  // Instead of MonoBehaviour
{
    void Start()
    {
        var subscription = EventBus.Subscribe<GameEvent>(OnGameEvent);
        RegisterSubscription(subscription); // Auto-cleanup!
    }

    // No OnDestroy needed - handled automatically!
}
```

**Files to Fix (137 files with subscriptions):**
- All UI managers
- All subsystem managers
- Networking systems

---

### **✅ Phase 2: High Priority**

#### 4. Convert File I/O to Async

**BEFORE:**
```csharp
void WriteLog(string message)
{
    File.AppendAllText(logPath, message); // ❌ Blocks main thread!
}
```

**AFTER:**
```csharp
using Laboratory.Core.Performance;

async void WriteLogAsync(string message)
{
    await AsyncFileIO.AppendLogLineAsync(logPath, message); // ✅ Non-blocking!
}
```

**Files to Fix:**
- `/Assets/Scripts/ChimeraErrorMonitor.cs:466`
- Any other File.Read/Write operations

---

#### 5. Replace JSON with Binary Serialization

**BEFORE:**
```csharp
// Slow JSON serialization
string json = JsonUtility.ToJson(gameState, true);
PlayerPrefs.SetString("GameState", json);
```

**AFTER:**
```csharp
using Laboratory.Core.Performance;

// Fast binary serialization (5-10x faster)
BinarySerialization.SaveToPlayerPrefs("GameState", gameState);

// Or save to file:
await BinarySerialization.SaveToFileAsync(savePath, gameState);
```

**Files to Fix:**
- `/Assets/Scripts/ChimeraManager.cs:532-534`
- CloudSyncManager.cs serialization

---

#### 6. Cache Transform/Camera Access

**BEFORE:**
```csharp
void Update()
{
    // ❌ Accesses transform.position 3x per frame
    if (Vector3.Distance(transform.position, target) > 0.1f)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed);
    }

    // ❌ Camera.main calls FindGameObjectWithTag every time
    Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
}
```

**AFTER:**
```csharp
using Laboratory.Core.Performance;

private Camera _camera;

void Awake()
{
    _camera = PerformanceCache.MainCamera; // Cached!
}

void Update()
{
    // ✅ Cache position
    Vector3 currentPos = transform.position;

    if (Vector3.Distance(currentPos, target) > 0.1f)
    {
        currentPos = Vector3.MoveTowards(currentPos, target, speed);
        transform.position = currentPos; // Only 1 write
    }

    // ✅ Use cached camera
    Vector3 screenPos = _camera.WorldToScreenPoint(currentPos);
}
```

**Files to Fix:**
- `/Assets/Scripts/ChimeraNetworkManager.cs:1400-1402`
- 17 files using Camera.main
- All Update methods with transform access

---

### **✅ Phase 3: Quick Wins**

#### 7. Cache WaitForSeconds

**BEFORE:**
```csharp
IEnumerator MyCoroutine()
{
    yield return new WaitForSeconds(0.1f); // ❌ Allocates every time!
}
```

**AFTER:**
```csharp
using Laboratory.Core.Performance;

IEnumerator MyCoroutine()
{
    yield return PerformanceCache.Wait01; // ✅ Cached!
}
```

**Files to Fix:**
- `/Assets/Scripts/ChimeraManager.cs:180, 183, 186`
- All coroutines with WaitForSeconds

---

#### 8. Use StringBuilder for String Building

**BEFORE:**
```csharp
string message = "";
foreach (var item in items)
{
    message += item.ToString(); // ❌ Creates garbage every iteration
}
```

**AFTER:**
```csharp
using Laboratory.Core.Performance;

// Option A: Manual
var sb = PerformanceCache.GetStringBuilder();
foreach (var item in items)
{
    sb.Append(item.ToString());
}
string message = sb.ToString();
PerformanceCache.ReturnStringBuilder(sb);

// Option B: Helper method
string message = PerformanceCache.BuildString(sb =>
{
    foreach (var item in items)
    {
        sb.Append(item.ToString());
    }
});
```

**Files to Fix:**
- All UI text building
- All log message concatenation

---

#### 9. Use MaterialPropertyBlock

**BEFORE:**
```csharp
renderer.material.color = color; // ❌ Creates material instance!
```

**AFTER:**
```csharp
using Laboratory.Core.Performance;

PerformanceCache.SetRendererColor(renderer, color); // ✅ No instance created
```

**Files to Fix:**
- `/Assets/Scripts/ChimeraNetworkManager.cs:1083`
- All renderer property changes

---

## Testing Performance Improvements

### **Load Test Your Changes**

```csharp
using Laboratory.Subsystems.Performance;

// Add to scene
var loadTest = gameObject.AddComponent<LoadTestingFramework>();
loadTest.targetCreatureCount = 1000;
loadTest.targetFPS = 60;
loadTest.testDurationSeconds = 60;

// Run test
await loadTest.RunAllTestsAsync();

// Check results - should pass with optimizations!
```

---

## Performance Targets

- **1000+ creatures @ 60 FPS** (primary goal)
- **<16.67ms frame time** (60 FPS budget)
- **<2GB memory usage**
- **No GC allocations in hot paths**

---

## Monitoring Performance

### **Unity Profiler**
1. Window → Analysis → Profiler
2. Check:
   - CPU Usage (should be <16.67ms)
   - Memory (should be stable, no growth)
   - GC.Alloc (should be 0 in gameplay)
   - Rendering (draw calls, batches)

### **ECS Profiler**
1. Window → Analysis → Profiler
2. Enable "Deep Profile"
3. Check system timings

### **In-Game Stats**
```csharp
// Print pool stats
MonsterPoolManager.Instance.PrintPoolStatistics();

// Print event subscriptions
EventSubscriptionTracker.PrintActiveSubscriptions();
```

---

## Common Mistakes to Avoid

❌ **DON'T:**
- Use FindObjectOfType in Update
- Access Camera.main repeatedly
- Create new WaitForSeconds in coroutines
- Use renderer.material.property
- Instantiate/Destroy in loops
- Use string concatenation in loops
- Forget OnDestroy for event cleanup
- Use synchronous File.Read/Write

✅ **DO:**
- Cache component references in Awake
- Use object pooling for frequent spawning
- Use MaterialPropertyBlock for renderer properties
- Use StringBuilder for string building
- Inherit from CleanupBehaviour
- Use AsyncFileIO for file operations
- Add [BurstCompile] to all ECS systems
- Use binary serialization for large data

---

## Need Help?

See the example files in `/Core/Performance/` for reference implementations:
- `ObjectPool.cs` - Generic pooling
- `MonsterPoolManager.cs` - Monster-specific pooling
- `PerformanceCache.cs` - Caching utilities
- `AsyncFileIO.cs` - Async file operations
- `BinarySerialization.cs` - Fast serialization
- `EventCleanupManager.cs` - Memory leak prevention

**Questions?** Check the Unity Profiler first, then review this guide.
