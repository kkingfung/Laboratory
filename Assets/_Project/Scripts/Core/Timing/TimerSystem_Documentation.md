# Timer System Architecture Documentation

## Overview

The Laboratory Timer System provides a comprehensive, event-driven timing solution for Unity projects. It consists of specialized timer implementations managed by a centralized service, ensuring consistent behavior and optimal performance across the entire project.

## Architecture Components

### Core Interface: `ITimer`

All timer implementations inherit from the `ITimer` interface, providing:

- **Standardized API**: Consistent methods across all timer types
- **Event System**: Completion and tick events for reactive programming
- **Lifecycle Management**: Start, stop, reset, and disposal patterns
- **Progress Tracking**: Normalized progress values (0-1) for UI binding

```csharp
public interface ITimer : IDisposable
{
    float Duration { get; }
    float Remaining { get; }
    float Elapsed { get; }
    bool IsActive { get; }
    float Progress { get; }
    
    event Action OnCompleted;
    event Action<float> OnTick;
    
    void Start();
    bool TryStart();
    void Stop();
    void Tick(float deltaTime);
    void Reset();
}
```

### Timer Implementations

#### 1. **CooldownTimer**
- **Purpose**: Weapon cooldowns, ability recharge, debuff timers
- **Behavior**: Starts at duration, counts down to zero
- **Use Cases**: Combat systems, skill rotations, status effects

#### 2. **CountdownTimer**  
- **Purpose**: Match timers, loading timeouts, timed events
- **Behavior**: Counts down from duration, can be started/stopped
- **Use Cases**: Game sessions, UI countdowns, time-limited operations

#### 3. **ProgressTimer**
- **Purpose**: Loading screens, progress bars, completion tracking
- **Behavior**: Can auto-progress or be manually controlled
- **Use Cases**: Asset loading, level transitions, download progress

### Centralized Management: `TimerService`

The `TimerService` provides:

- **Centralized Updates**: Single Update() loop for all timers
- **Automatic Registration**: Timers auto-register when created
- **Memory Management**: Automatic cleanup of completed timers
- **Service Integration**: Registers with the global service container
- **Debug Support**: Real-time timer monitoring

## Integration Points

### Event Bus Integration

Timers integrate with the unified event system:

```csharp
// Loading screen events
_eventBus.Subscribe<LoadingStartedEvent>(OnGlobalLoadingStarted);
_eventBus.Subscribe<LoadingProgressEvent>(OnGlobalLoadingProgress);
_eventBus.Subscribe<LoadingCompletedEvent>(OnGlobalLoadingCompleted);
```

### Service Container Integration

The timer service automatically registers with the global service provider:

```csharp
var services = GlobalServiceProvider.Instance;
var timerService = services.Resolve<TimerService>();
```

### AsyncUtils Integration

The `LoadingScreen` class demonstrates advanced timer integration:

- **Progress Tracking**: Uses `ProgressTimer` for smooth progress bars
- **Event Coordination**: Coordinates with global loading events
- **Service Dependency**: Integrates with scene and asset services
- **Cancellation Support**: Full async/await pattern support

## Usage Patterns

### Basic Timer Creation

```csharp
// Auto-registered cooldown timer
var weaponCooldown = new CooldownTimer(3f);
weaponCooldown.OnCompleted += () => Debug.Log("Weapon ready!");
weaponCooldown.Start();

// Manual progress timer
var loadingProgress = new ProgressTimer(autoRegister: false);
loadingProgress.OnProgressChanged += UpdateProgressBar;
loadingProgress.SetProgress(0.5f);
```

### Advanced Integration

```csharp
// Service-based timer management
public class WeaponSystem : MonoBehaviour
{
    private TimerService _timerService;
    private CooldownTimer _primaryWeaponCooldown;
    
    private void Awake()
    {
        _timerService = GlobalServiceProvider.Instance.Resolve<TimerService>();
        _primaryWeaponCooldown = new CooldownTimer(2.5f);
        _primaryWeaponCooldown.OnCompleted += OnWeaponReady;
    }
    
    public bool TryFireWeapon()
    {
        if (_primaryWeaponCooldown.IsActive) return false;
        
        FireProjectile();
        _primaryWeaponCooldown.Start();
        return true;
    }
}
```

## Performance Characteristics

### Memory Efficiency
- **Object Pooling**: Timers are lightweight value types
- **Event Cleanup**: Automatic event unsubscription on disposal  
- **List Management**: Efficient removal of completed timers

### Update Performance
- **Single Update Loop**: All timers updated in one pass
- **Null Safety**: Robust handling of disposed timers
- **Batch Operations**: Minimal allocations per frame

### Service Registration
- **Lazy Loading**: Services resolve only when needed
- **Graceful Degradation**: Works without service container
- **Singleton Pattern**: Ensures single timer service instance

## Best Practices

### Timer Creation
1. **Use appropriate timer type** for your use case
2. **Set autoRegister=true** unless manually managing updates
3. **Always dispose** timers to prevent memory leaks
4. **Use TryStart()** to prevent double-activation

### Event Handling
1. **Unsubscribe events** in OnDestroy or Dispose
2. **Use lambda captures carefully** to avoid memory leaks
3. **Consider using UniRx** for complex event chains

### Service Integration
1. **Check service availability** before resolving
2. **Use TryResolve()** for optional dependencies
3. **Cache service references** in Awake/Start

### Performance Optimization
1. **Minimize timer creation** in hot paths
2. **Batch timer operations** when possible
3. **Use context menus** for testing and debugging

## Testing Strategy

### Unit Tests
- **Timer Behavior**: Start, stop, tick, reset operations
- **Event Firing**: Completion and progress events
- **Edge Cases**: Zero duration, negative values, double starts

### Integration Tests
- **Service Registration**: TimerService integration
- **Event Bus Integration**: Global event coordination
- **Memory Management**: Disposal and cleanup

### Performance Tests
- **Scalability**: Performance with many active timers
- **Memory Usage**: Allocation patterns and garbage collection
- **Frame Rate Impact**: Update loop efficiency

## Future Enhancements

### Planned Features
1. **Timer Pooling**: Reusable timer instances
2. **Pause/Resume**: Global timer pause functionality
3. **Save/Load**: Persistent timer state
4. **Timeline Integration**: Visual timer editing

### API Extensions
1. **Timer Chains**: Sequential timer execution
2. **Timer Groups**: Batch operations on timer sets  
3. **Dynamic Duration**: Runtime duration modification
4. **Custom Easing**: Non-linear progress curves

---

## Quick Reference

| Timer Type | Use Case | Key Features |
|------------|----------|-------------|
| `CooldownTimer` | Abilities, Weapons | Auto-starts inactive, events on completion |
| `CountdownTimer` | Matches, Timeouts | Start/stop control, time remaining focus |
| `ProgressTimer` | Loading, Progress | Manual or auto progress, completion tracking |

| Service | Purpose | Integration |
|---------|---------|-------------|
| `TimerService` | Centralized management | Auto-registration, efficient updates |
| `LoadingScreen` | Progress UI | Event coordination, service integration |
| `GlobalServiceProvider` | Dependency injection | Service resolution, lifecycle management |