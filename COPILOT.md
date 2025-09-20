# GitHub Copilot Custom Instructions for Project Chimera - Unity Monster Breeding RPG

## Project Context
You are assisting in the development of **Project Chimera**, a sophisticated 3D Open-World Monster Breeding Online RPG built with Unity ECS & Netcode. This is a persistent online multiplayer game where every monster is unique, the ecosystem evolves dynamically, and players shape the world through exploration, combat, and breeding.

**Important:** Always refer to the `README.md` in this repository for:
- Game story, theme, and worldbuilding details
- Core gameplay loop and monster breeding mechanics
- Player abilities and combat systems
- Chimera monster types and behaviors
- Genetic breeding system specifications
- Any special rules or mechanics unique to this project

The `README.md` is the single source of truth for game design details.  
Do not assume any gameplay or mechanic details that are not listed there.

## Technical Stack & Architecture

### Core Framework
- **Unity 2022.3 LTS** - Stable foundation with latest features
- **Unity ECS (Entities, Physics, Collections)** - High-performance systems for creature simulation
- **Unity Netcode for GameObjects** - Seamless multiplayer synchronization
- **R3 Reactive Extensions** - Reactive programming for UI and game events
- **UniTask** - Modern async/await patterns for Unity
- **VContainer** - Dependency injection for modular architecture
- **MessagePipe** - Event-driven systems communication

### World & Visuals
- **URP + PostProcessing** - Optimized rendering across platforms
- **Cinemachine** - Dynamic camera work for exploration & combat
- **Visual Effect Graph** - Breeding rituals, elemental powers, battle FX
- **Splines + Tilemap** - Procedural biome layouts

### AI & Procedural Content
- **Unity Navigation** - Dynamic pathfinding for AI-driven ecosystems
- **Unity AI Generators + Inference** - Quest & dialogue generation
- **Animation Rigging + Character Controller** - Procedural monster rigs & traversal

### Online Services
- **Unity Services Authentication + Lobby** - Accounts, matchmaking, multiplayer sessions
- **Analytics + Memory/Performance Profilers** - LiveOps & optimization

## Project Architecture Overview

### 🏗️ Subsystem Architecture (12 Main Systems)
The project implements **modern Unity development patterns** designed for scalability, maintainability, and multiplayer synchronization:

1. **Player Systems** 🎮 (`Subsystems/Player/`)
   - Character control, movement, camera management, input handling
   - `UnifiedPlayerSubsystemManager`, `CharacterLookController`, `UnifiedAimController`
   - Advanced target selection, climbing mechanics, customization

2. **Chimera Monster AI** 🤖 (`Chimera/AI/`)
   - Advanced AI controller with state machines (`ChimeraMonsterAI`)
   - Genetic behavior integration, pathfinding, combat systems
   - ECS components for high-performance simulation (`CreatureECSComponents`)

3. **Combat Systems** ⚔️ (`Subsystems/Combat/`)
   - Unified health management (`CombatSubsystemManager`)
   - Damage processing, ability systems, combat events
   - Network-aware health components

4. **Networking Systems** 🌐 (`Subsystems/Networking/`)
   - `NetworkMessageHandler`, `AdvancedNetworkManager`
   - ECS synchronization, player data management
   - Client-server architecture with Netcode for GameObjects

5. **Inventory Systems** 📦 (`Subsystems/Inventory/`)
   - `UnifiedInventorySystem`, `EnhancedInventorySystem`
   - Item management, crafting, quest items
   - Event-driven inventory changes

6. **UI Systems** 🖥️ (`UI/`)
   - HUD controllers, scoreboard, minimap
   - Event-driven UI updates with R3 reactive extensions

7. **Core Infrastructure** 🔧 (`Core/`)
   - Dependency injection (`DI/`), event system (`Events/`)
   - Service container, bootstrap systems
   - State management and timing systems

8. **ECS Integration** (`Models/ECS/`)
   - High-performance creature simulation
   - Network synchronization components
   - Genetic breeding system integration

## Coding Style & Conventions

### Naming Conventions
- **PascalCase** for public members, classes, methods, properties
- **camelCase** for private fields with `_` prefix (e.g., `_isInitialized`)
- **Interfaces** start with `I` (e.g., `IHealthComponent`, `IInventorySystem`)
- **Events** use descriptive names ending with `EventArgs` (e.g., `HealthChangedEventArgs`)

### Code Organization
- **Namespace Structure**: `Laboratory.SubsystemName.ComponentName`
- **File Structure**: One main class per file, supporting classes in same file when related
- **XML Documentation**: All public methods, classes, and properties must have `///` documentation
- **Method Length**: Keep methods under 50 lines; split into smaller functions if longer

### Unity-Specific Patterns
- Use `SerializeField` for private variables exposed in Inspector
- Implement `IDisposable` for classes with subscriptions or resources
- Use `RequireComponent` attributes for component dependencies
- Prefer composition over inheritance for game objects

## Best Practices & Patterns

### Performance Guidelines
- **Minimize Update()**: Prefer events, coroutines, or ECS systems
- **Object Pooling**: Use for frequently spawned/despawned objects (monsters, projectiles)
- **Memory Management**: Proper disposal patterns and event cleanup
- **ECS Integration**: Use ECS for high-performance creature simulation
- **Profile Regularly**: Unity Profiler and Frame Debugger for optimization

### Event-Driven Architecture
- **Event Bus**: Use `IEventBus` for system communication
- **Reactive Programming**: R3 for UI updates and game events
- **Service Integration**: Dependency injection with `IServiceContainer`
- **State Management**: Centralized state with event notifications

### Network & Multiplayer
- **Server Authority**: Server-authoritative damage and state changes
- **Client Prediction**: For responsive gameplay where appropriate
- **Network Validation**: Validate all network inputs
- **ECS Synchronization**: Use ECS components for network state

### AI & Gameplay
- **State Machines**: Implement AI with clear state transitions
- **Genetic Integration**: Connect AI behavior to genetic traits
- **Scalable Difficulty**: Adjust AI speed, damage, and reaction time
- **Decoupled Logic**: Keep gameplay logic separate from rendering and input

## Monster DNA System Integration

### Genetic Components
```csharp
struct MonsterDNA : IComponentData {
    FixedString64Bytes SpeciesId;
    int Generation;
    uint Seed; // RNG for visuals
}

struct MonsterStats : IComponentData {
    float Strength, Agility, Vitality, Intelligence;
}

struct MonsterTraits : IBufferElementData {
    FixedString32Bytes TraitId;
    float TraitValue;
}
```

### AI Behavior Types
- **Companion**: Follows player, moderate aggression
- **Aggressive**: Seeks out enemies actively  
- **Defensive**: Only fights when player is threatened
- **Passive**: Never fights, only follows
- **Guard**: Patrols area, defends territory
- **Wild**: Natural wild creature behavior
- **Predator**: Active hunting behavior
- **Herbivore**: Peaceful grazing behavior

## Copilot Behavior Guidelines

### Code Generation Rules
- **Always check `README.md`** for gameplay and thematic details before generating code
- **Follow subsystem architecture** - place code in appropriate subsystem folders
- **Use existing interfaces** - implement `IHealthComponent`, `IInventorySystem`, etc.
- **Event-driven communication** - publish events rather than direct method calls
- **Dependency injection** - use `GlobalServiceProvider` for service resolution
- **Network awareness** - consider multiplayer implications for all systems

### Integration Patterns
- **Service Registration**: Register new services with `IServiceContainer`
- **Event Publishing**: Use `IEventBus.Publish()` for system communication
- **ECS Integration**: Create ECS components for high-performance systems
- **Configuration**: Use ScriptableObject configurations for system settings
- **Testing**: Include unit tests for new systems (85%+ test coverage target)

### Example Interactions

**User:** "Generate a new monster AI behavior type"
**Copilot Output:**
- Extend `AIBehaviorType` enum in `ChimeraECSComponents.cs`
- Update `ChimeraMonsterAI.cs` with new behavior logic
- Add genetic trait integration for the new behavior
- Include XML documentation and event integration
- Confirm mechanics align with README.md breeding system

**User:** "Create a new inventory item type"
**Copilot Output:**
- Extend `ItemData` class with new item properties
- Update `UnifiedInventorySystem.cs` for new item handling
- Add UI integration for the new item type
- Include network synchronization considerations
- Follow existing event-driven patterns for inventory changes

## Testing & Quality Assurance
- **Unit Tests**: 85%+ test coverage target
- **Integration Tests**: Test subsystem interactions
- **Performance Tests**: Profile ECS systems and networking
- **Network Tests**: Validate multiplayer synchronization
- **AI Tests**: Verify genetic behavior integration

Remember: This is a complex, multiplayer, ECS-based monster breeding RPG. Always consider the genetic system, networking implications, and subsystem architecture when generating code.
