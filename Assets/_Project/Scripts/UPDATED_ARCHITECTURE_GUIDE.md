# 🏗️ Laboratory Unity Project - Updated Architecture Guide

## 📋 Table of Contents

1. [Overview](#overview)
2. [Core Subsystems](#core-subsystems)
3. [Unified Systems](#unified-systems)
4. [Dependency Injection](#dependency-injection)
5. [Event System](#event-system)
6. [Best Practices](#best-practices)
7. [Testing Strategy](#testing-strategy)
8. [Performance Considerations](#performance-considerations)

## Overview

The Laboratory Unity Project has been restructured with a clean, modular architecture featuring 8 core subsystems. Each subsystem is self-contained with clear interfaces and minimal dependencies.

### Architecture Principles

- **Single Responsibility**: Each subsystem has one clear purpose
- **Dependency Injection**: Services are injected, not directly referenced
- **Event-Driven**: Systems communicate through events, not direct calls
- **Interface Segregation**: Small, focused interfaces rather than large ones
- **Composition over Inheritance**: Favor composition and interfaces

### Project Structure

```
Assets/_Project/Scripts/
├── Core/                           # Core infrastructure
│   ├── Bootstrap/                  # Game initialization
│   ├── DI/                        # Dependency injection
│   ├── Events/                    # Global event system
│   ├── Services/                  # Core services
│   └── Systems/                   # System interfaces
├── Subsystems/                    # Main game subsystems
│   ├── Player/                    # Player-related functionality
│   ├── Combat/                    # Combat and abilities
│   ├── EnemyAI/                   # AI and NPC systems
│   ├── UI/                        # User interface
│   ├── Networking/                # Network functionality
│   ├── Inventory/                 # Item management
│   ├── Audio/                     # Sound and music
│   └── Utility/                   # Utilities and tools
└── Tests/                         # All test code
```

## Core Subsystems

### 1. Player Subsystem 🎮

**Purpose**: Manages all player-related functionality including character control, camera, and input.

**Key Components**:
- `UnifiedTargetSelector`: Advanced target detection and selection
- `PlayerCameraManager`: Camera control and management
- `CharacterLookController`: Character orientation and look-at behavior
- `CharacterCustomizationManager`: Player appearance customization

**Interfaces**:
- `ITargetSelector`: Target selection functionality
- `ICharacterController`: Character control interface
- `IAimController`: Aiming system interface

**Events**:
- `TargetDetectedEvent`, `TargetSelectedEvent`, `TargetLostEvent`
- `CharacterStateChangedEvent`, `CharacterMovementEvent`

### 2. Combat Subsystem ⚔️

**Purpose**: Handles all combat-related functionality including health, damage, abilities, and effects.

**Key Components**:
- `UnifiedAbilitySystem`: Central ability management
- `HealthComponentBase`: Base health system
- `DamageRequest`: Structured damage handling
- `AbilityEventBus`: Ability-related events

**Interfaces**:
- `IHealthComponent`: Health management interface
- `IAbilitySystem`: Ability system interface
- `IAbilityManagerCore`: Core ability manager functionality

**Events**:
- `AbilityActivatedEvent`, `AbilityStateChangedEvent`
- `HealthChangedEventArgs`, `DeathEventArgs`
- `DamageRequest` for structured damage

### 3. Enemy/AI Subsystem 🤖

**Purpose**: Manages all AI behavior, NPC interactions, and enemy systems.

**Key Components**:
- `UnifiedNPCBehavior`: Comprehensive NPC AI system
- `NPCManager`: Global NPC coordination
- `AIStateMachine`: State management for AI
- `BehaviorTree`: AI decision making

**Features**:
- Multiple NPC types (Civilian, Guard, Hunter, etc.)
- Personality system (Coward, Fighter, Brave, etc.)
- Difficulty scaling system
- Quest and reputation system
- Patrol and combat behaviors

### 4. UI Subsystem 🖥️

**Purpose**: Handles all user interface elements and interactions.

**Key Components**:
- `HUDController`: Main game UI controller
- Specialized UI components (Minimap, Scoreboard, Inventory, etc.)
- UI utilities and effects

**Architecture**:
- MVVM pattern with reactive programming (UniRx)
- Event-driven UI updates
- Modular UI component system

### 5. Networking Subsystem 🌐

**Purpose**: Manages all network functionality for multiplayer support.

**Key Components**:
- `NetworkPlayerData`: Player data synchronization
- `NetworkMessageHandler`: Message processing
- Network transport abstraction

**Features**:
- Unity Netcode integration
- Player data synchronization
- Network event handling

### 6. Inventory Subsystem 📦

**Purpose**: Manages item storage, crafting, and trading systems.

**Key Components**:
- `EnhancedInventorySystem`: Full-featured inventory
- `CraftingSystem`: Item crafting logic
- `InventorySaveSystem`: Persistence
- `ItemData`: Item definitions

**Interfaces**:
- `IInventorySystem`: Core inventory operations

### 7. Audio Subsystem 🔊

**Purpose**: Handles all audio including music, sound effects, and spatial audio.

**Key Components**:
- `AudioSystemManager`: Central audio coordination
- `MusicManager`: Background music
- `SFXManager`: Sound effects
- `Audio3DManager`: Spatial audio

### 8. Utility Subsystem 🛠️

**Purpose**: Provides shared utilities, tools, and infrastructure support.

**Key Components**:
- `TimerService`: Game timing utilities
- `DebugConsole`: Development tools
- `PerformanceOverlay`: Performance monitoring
- Configuration and validation tools

## Unified Systems

### Target Selection System

The `UnifiedTargetSelector` combines the best features of previous target selectors:

```csharp
// Configure detection
var detectionSettings = new DetectionSettings
{
    detectionRadius = 10f,
    maxDetectionDistance = 15f,
    targetLayers = enemyLayers,
    validateLineOfSight = true
};

targetSelector.UpdateDetectionSettings(detectionSettings);

// Get targets
Transform closestTarget = targetSelector.GetClosestTarget();
Transform bestTarget = targetSelector.GetHighestPriorityTarget();
var nearbyTargets = targetSelector.GetTargetsWithinDistance(5f);
```

### NPC Behavior System

The `UnifiedNPCBehavior` provides comprehensive AI functionality:

```csharp
// Configure NPC
npc.SetDifficulty(NPCDifficulty.Hard);
npc.AddPatrolPoint(waypoint1);
npc.AddPatrolPoint(waypoint2);

// React to events
npc.OnAttacked(attacker, 25f);
npc.OnHelped(helper);
npc.StartConversation(player);
npc.CompleteQuest();
```

### Ability System

The `UnifiedAbilitySystem` manages all abilities centrally:

```csharp
// Register with system
abilitySystem.RegisterAbilityManager(abilityManager);

// Use abilities
bool activated = abilitySystem.TryActivateAbility(manager, 0);
float cooldown = abilitySystem.GetAbilityCooldown(manager, 0);
bool onCooldown = abilitySystem.IsAbilityOnCooldown(manager, 0);
```

## Dependency Injection

The project uses a custom dependency injection system via `ServiceContainer`:

### Service Registration

```csharp
// In GameBootstrap
_services.Register<IEventBus, UnifiedEventBus>();
_services.Register<IGameStateService, GameStateService>();
_services.Register<IAssetService, AssetService>();
```

### Service Resolution

```csharp
// In component initialization
public void Initialize(IServiceContainer services)
{
    _eventBus = services.Resolve<IEventBus>();
    _assetService = services.Resolve<IAssetService>();
}
```

### Global Access

```csharp
// For systems that can't use DI
var services = GlobalServiceProvider.Instance;
var eventBus = services?.Resolve<IEventBus>();
```

## Event System

The project uses a unified event system for decoupled communication:

### Publishing Events

```csharp
// Publish via event bus
_eventBus.Publish(new TargetDetectedEvent(detector, target, distance, score));

// Publish via specific event bus
AbilityEventBus.PublishAbilityActivated(gameObject, abilityIndex, "Fireball");
```

### Subscribing to Events

```csharp
// Subscribe to specific events
_eventBus.Subscribe<TargetDetectedEvent>(OnTargetDetected);

// Subscribe via Unity Events
AbilityEventBus.OnAbilityActivated.AddListener(OnAbilityActivated);
```

## Best Practices

### Code Organization

1. **Use Interfaces**: Always define interfaces for systems
2. **Event-Driven**: Communicate through events, not direct references
3. **Single Responsibility**: Each class should have one clear purpose
4. **Composition**: Favor composition over inheritance
5. **Validation**: Always validate inputs and handle edge cases

### Performance

1. **Object Pooling**: Pool frequently created objects
2. **Update Intervals**: Don't update every frame unless necessary
3. **LOD Systems**: Use level-of-detail for expensive operations
4. **Profiling**: Regular performance profiling and optimization

### Testing

1. **Unit Tests**: Test individual components in isolation
2. **Integration Tests**: Test system interactions
3. **Performance Tests**: Monitor performance regressions
4. **Mock Services**: Use mocks for external dependencies

## Testing Strategy

### Unit Testing Structure

```
Tests/
├── Unit/
│   ├── Player/
│   │   ├── UnifiedTargetSelectorTests.cs
│   │   └── CharacterControllerTests.cs
│   ├── Combat/
│   │   ├── AbilitySystemTests.cs
│   │   └── HealthComponentTests.cs
│   └── [Other Subsystems]/
├── Integration/
│   ├── PlayerCombatIntegrationTests.cs
│   └── NetworkingIntegrationTests.cs
└── Performance/
    ├── TargetSelectionPerformanceTests.cs
    └── AIPerformanceTests.cs
```

### Test Examples

```csharp
[Test]
public void UnifiedTargetSelector_DetectsTargetsInRange()
{
    // Arrange
    var selector = CreateTargetSelector();
    var target = CreateTarget(Vector3.forward * 5f);
    
    // Act
    selector.ForceTargetUpdate();
    
    // Assert
    Assert.IsTrue(selector.HasTargets);
    Assert.AreEqual(target, selector.GetClosestTarget());
}
```

## Performance Considerations

### Target Selection

- Update intervals to reduce per-frame overhead
- Distance culling for irrelevant targets
- LOD system for reduced accuracy at distance
- Object pooling for temporary calculations

### AI System

- Behavior tree caching
- State transition optimization
- Perception update intervals
- Group AI coordination

### Audio System

- 3D audio distance culling
- Audio source pooling
- Compression for network audio
- LOD for audio quality

## Migration Notes

### From Old Systems

1. **TargetSelector** → `UnifiedTargetSelector`
2. **NPCBehavior** → `UnifiedNPCBehavior`
3. **Separate ability managers** → `UnifiedAbilitySystem`

### Breaking Changes

- Old target selector events no longer available
- NPC behavior API changes for unified system
- Event system structure changes

### Compatibility

- `IsExternalInit.cs` maintained for C# 9.0+ compatibility
- Deprecated DOTS systems removed (can be re-implemented if needed)
- Stub systems replaced with proper implementations

---

## 📚 Additional Resources

- [Unity Best Practices](https://docs.unity3d.com/Manual/BestPractice.html)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Event-Driven Architecture](https://martinfowler.com/articles/201701-event-driven.html)
- [Dependency Injection Patterns](https://martinfowler.com/articles/injection.html)

---

**Last Updated**: December 2024  
**Version**: 2.0  
**Status**: ✅ Complete and Ready for Use
