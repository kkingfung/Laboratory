# Project Chimera  
*A 3D Open-World Monster Breeding Online RPG built with Unity ECS & Netcode*  

---

## 🎮 Game Concept  
Project Chimera is a **persistent online monster breeding RPG** where every monster is unique, the ecosystem evolves dynamically, and players shape the world through exploration, combat, and breeding.  

- 🧬 **Genetic Breeding System** – DNA-driven monsters with unique stats, traits, and visuals.  
- 🌍 **Living Ecosystem** – AI herds, predator-prey cycles, seasonal migrations.  
- ⚔️ **Action Combat** – Real-time PvE & PvP battles with environmental interactions.  
- 🌐 **Online Multiplayer** – Co-op exploration, player-driven breeding market, raid events.  
- 🎨 **Procedural Variety** – Monsters and worlds are generated via deterministic seeds.  

---

## 🛠️ Tech Stack  

This project leverages Unity's **latest ECS, AI, and Multiplayer packages** with third-party tools for scalability and modularity.  

### Core Framework
- [Unity ECS (Entities, Physics, Collections)](https://docs.unity3d.com/Packages/com.unity.entities@latest/) – Scalable AI & DNA simulations.
- [Netcode for Entities](https://docs.unity3d.com/Packages/com.unity.netcode@latest/) + Unity Transport – Multiplayer backbone.
- Unity's built-in async/await patterns – Async operations and reactive systems.
- Custom dependency injection – Modular architecture with service containers.  

### World & Visuals  
- [URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/) + PostProcessing – Optimized rendering across platforms.  
- [Cinemachine](https://docs.unity3d.com/Packages/com.unity.cinemachine@latest/) – Dynamic camera work for exploration & combat.  
- [Visual Effect Graph](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/) – Breeding rituals, elemental powers, battle FX.  
- Splines + Tilemap – Procedural biome layouts.  

### AI & Procedural Content  
- [Unity Navigation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@latest/) – Dynamic pathfinding for AI-driven ecosystems.  
- Unity AI Generators + Inference – Quest & dialogue generation.  
- Animation Rigging + Character Controller – Procedural monster rigs & traversal.  

### Online Services  
- Unity Services Authentication + Lobby – Accounts, matchmaking, multiplayer sessions.  
- Analytics + Memory/Performance Profilers – LiveOps & optimization.  
- Marketplace (Planned) – Player-to-player breeding and trading economy.  

---

## 🏗️ System Architecture

Project Chimera implements a **12-subsystem architecture** designed for scalability, maintainability, and multiplayer synchronization. Each subsystem is self-contained with clear interfaces and minimal dependencies.

### Architecture Principles
- **Single Responsibility**: Each subsystem has one clear purpose
- **Dependency Injection**: Services are injected, not directly referenced
- **Event-Driven**: Systems communicate through events, not direct calls
- **Interface Segregation**: Small, focused interfaces rather than large ones
- **Composition over Inheritance**: Favor composition and interfaces

---

## 📊 Subsystem Documentation

### 🎮 **1. Player Systems** (`Subsystems/Player/`)
**Responsibility:** Character control, movement, camera management, input handling

**Core Components:**
- **`UnifiedPlayerSubsystemManager`** - Centralized player subsystem coordination
- **`PlayerController`** - Main player controller with movement, combat, and health integration
- **`PlayerSubsystemConfig`** - ScriptableObject configuration for all player settings
- **`PlayerInputSystem`** - Input handling and processing

**Key Features:**
- ✅ Unified subsystem management with dependency injection
- ✅ Integrated health system with `IHealthComponent`
- ✅ Movement and combat systems with configurable parameters
- ✅ Event-driven communication with other subsystems
- ✅ Audio integration for movement and combat feedback

**Interfaces:**
- `IHealthComponent` - Health management interface
- `ICustomizationSystem` - Character customization interface

---

### 🤖 **2. Chimera Monster AI** (`Chimera/AI/`)
**Responsibility:** Advanced monster AI with genetic behavior integration

**Core Components:**
- **`ChimeraMonsterAI`** - Main AI controller with state machines and genetic integration
- **`EnhancedChimeraMonsterAI`** - Advanced AI with enhanced pathfinding
- **`ChimeraMonsterSetup`** - Monster configuration and initialization
- **`CreatureECSComponents`** - ECS components for high-performance simulation

**AI Behavior Types:**
- **Companion** - Follows player, moderate aggression
- **Aggressive** - Seeks out enemies actively
- **Defensive** - Only fights when player is threatened
- **Passive** - Never fights, only follows
- **Guard** - Patrols area, defends territory
- **Wild** - Natural wild creature behavior
- **Predator** - Active hunting behavior
- **Herbivore** - Peaceful grazing behavior

**Key Features:**
- ✅ Genetic behavior modifiers affecting AI decisions
- ✅ State machine with Idle, Following, Patrolling, Pursuing, Combat, Returning
- ✅ Advanced pathfinding with NavMesh integration
- ✅ ECS integration for high-performance simulation
- ✅ Event-driven combat and interaction systems

---

### ⚔️ **3. Combat Systems** (`Subsystems/Combat/`)
**Responsibility:** Health management, damage processing, ability systems, combat mechanics

**Core Components:**
- **`CombatSubsystemManager`** - Unified combat subsystem coordination
- **`CombatSystem`** - ECS-based combat processing
- **`HealthComponentBase`** - Base health system implementation
- **`CombatSubsystemConfig`** - Combat configuration settings

**Key Features:**
- ✅ Unified health interface (`IHealthComponent`)
- ✅ Server-authoritative damage processing
- ✅ Damage over time effects and status management
- ✅ Ability system integration with cooldowns
- ✅ Combat state management (Idle, InCombat, Dead)
- ✅ Event-driven combat events and notifications

**Health Architecture:**
- Centralized health management with O(1) component lookup
- Network-aware health components for multiplayer
- Pluggable damage processors for extensibility
- Comprehensive statistics tracking and analytics

---

### 🌐 **4. Networking Systems** (`Subsystems/Networking/`)
**Responsibility:** Multiplayer synchronization, network communication, ECS integration

**Core Components:**
- **`ChimeraNetworkManager`** - Unity Netcode integration for monster breeding
- **`NetworkSyncSystemSimple`** - ECS network synchronization system
- **Custom networking components** - Optimized for Unity 6 architecture

**Key Features:**
- ✅ Client-server architecture with Netcode for Entities
- ✅ ECS synchronization for high-performance networking
- ✅ Custom async patterns optimized for Unity 6
- ✅ Performance monitoring and analytics
- ✅ Security features and validation

**Network Architecture:**
- Server-authoritative gameplay with client prediction
- Event-driven network communication
- ECS-based multiplayer synchronization
- Unity 6 optimized networking patterns

---

### 📦 **5. Inventory Systems** (`Subsystems/Inventory/`)
**Responsibility:** Item management, crafting, quest items, inventory UI

**Core Components:**
- **`UnifiedInventorySystem`** - Main inventory implementation with events
- **`EnhancedInventorySystem`** - Alternative implementation with enhanced features
- **`IInventorySystem`** - Unified inventory interface
- **`CraftingSystem`** - Item crafting and recipe management

**Key Features:**
- ✅ Event-driven inventory changes (`InventoryChangedEvent`)
- ✅ Stackable items with configurable limits
- ✅ Item database with automatic loading from Resources
- ✅ Dependency injection integration
- ✅ Comprehensive item validation and error handling
- ✅ Save/load functionality for inventory persistence

**Item System:**
- `ItemData` - Core item data structure with stats and properties
- `InventorySlot` - Individual inventory slot management
- Support for consumables, equipment, quest items, and crafting materials

---

### 🖥️ **6. UI Systems** (`UI/`)
**Responsibility:** User interface, HUD, menus, notifications

**Core Components:**
- **`BreedingInterfaceUI`** - Monster breeding interface and genetics display
- **`CreatureManagementUI`** - Creature collection and management
- **`AdvancedBreedingUI`** - Advanced breeding features and prediction
- **`VisualGeneticsInspectorUI`** - Genetic trait visualization

**Key Features:**
- ✅ Unity UI system with event-driven updates
- ✅ Custom UI patterns for breeding systems
- ✅ Creature visualization and genetic displays
- ✅ Integration with breeding and genetics systems
- ✅ Responsive UI design for multiple screen sizes

**UI Architecture:**
- Event-driven UI updates through Unity events
- Component-based UI architecture
- Modular breeding and genetics interfaces
- Integration with monster genetics and breeding systems

---

### 🔧 **7. Core Infrastructure** (`Core/`)
**Responsibility:** Dependency injection, event system, service container, bootstrap

**Core Components:**
- **`ChimeraManager`** - Central Chimera system management and coordination
- **`SafeComponentManager`** - Safe component handling and validation
- **`ChimeraErrorMonitor`** - Error monitoring and system health
- **System Bootstrap scripts** - Game initialization and system startup

**Key Features:**
- ✅ Custom dependency management system
- ✅ Event-driven communication patterns
- ✅ Service container for system registration
- ✅ Bootstrap system for proper initialization order
- ✅ Error monitoring and recovery systems

**Infrastructure Architecture:**
- Service-oriented architecture with custom management
- Event-driven communication between systems
- Centralized error monitoring and recovery
- Safe component handling and resource management

---

### 🧬 **8. ECS Integration** (`Models/ECS/`)
**Responsibility:** High-performance creature simulation, network synchronization

**Core Components:**
- **`CreatureECSComponents`** - ECS components for creature simulation
- **`NetworkSyncSystemSimple`** - ECS network synchronization
- **`CombatSystem`** - ECS-based combat processing
- **`PlayerStateComponent`** - ECS player state management

**Key Features:**
- ✅ High-performance creature simulation (1000+ entities)
- ✅ Parallel processing of aging, metabolism, and behavior
- ✅ Efficient memory layout for genetic data
- ✅ Batch operations for environmental effects
- ✅ Network synchronization for multiplayer

---

## 🧬 Monster DNA System  

Monsters are built using a **component-based DNA model**:  

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

struct MonsterVisualDNA : IComponentData {
    float SizeScale, ColorHue, PatternSeed, LimbProportion;
}
```

### Genetic Integration
- **AI Behavior**: Genetic traits directly influence AI behavior patterns
- **Visual Expression**: DNA determines monster appearance and animations
- **Breeding System**: Genetic inheritance with mutation and crossover
- **Performance**: ECS optimization for large-scale genetic simulation

---

## 🚀 Quick Start

### **Requirements**
- Unity 6 (6000.2.0b7 or later)
- Visual Studio 2022 or JetBrains Rider
- 8GB RAM (16GB recommended for large worlds)
- DirectX 11/12 or Vulkan compatible GPU

### **Setup**
```bash
# Clone the repository
git clone https://github.com/yourusername/project-chimera.git
cd project-chimera

# Open in Unity Hub
# All dependencies are managed via Package Manager - no manual setup needed!

# Run tests to verify setup
Unity Test Framework → Run All Tests (should see 85%+ pass rate)

# Build and run
Build Settings → Development Build → Build and Run
```

### **First Run**
1. **Bootstrap Scene**: Starts at `Assets/_Project/Scenes/Bootstrap.unity`
2. **Core Systems Initialize**: Watch the console for ~7 startup tasks
3. **Main Menu**: Automatic transition to main menu scene

---

## 🧪 Testing & Quality Assurance

### **Test Coverage**
- **Unit Tests**: 85%+ test coverage target
- **Integration Tests**: Subsystem interaction testing
- **Performance Tests**: ECS systems and networking profiling
- **Network Tests**: Multiplayer synchronization validation
- **AI Tests**: Genetic behavior integration verification

### **Performance Guidelines**
- **ECS Integration**: Use ECS for high-performance creature simulation
- **Object Pooling**: Implement for frequently spawned objects
- **Memory Management**: Proper disposal patterns and event cleanup
- **Network Optimization**: Server-authoritative with client prediction
- **Profiling**: Regular Unity Profiler and Frame Debugger usage

---

## 🔗 Integration Points

### **System Communication**
- **Event Bus**: All systems communicate through `IEventBus`
- **Service Container**: Dependency injection via `GlobalServiceProvider`
- **ECS Bridge**: ECS systems integrate with traditional MonoBehaviour systems
- **Network Events**: Network state changes propagate through event system

### **Data Flow**
1. **Player Input** → Player Systems → Event Bus
2. **Event Bus** → Combat Systems → Health/Damage Processing
3. **Combat Events** → UI Systems → Reactive UI Updates
4. **Network Events** → ECS Systems → State Synchronization
5. **Genetic Changes** → AI Systems → Behavior Modification

---

## 📈 Performance Characteristics

### **Scalability**
- **Creatures**: 1000+ simultaneous creatures at 60 FPS
- **Players**: 100+ concurrent players per server
- **Networking**: Optimized for low-latency multiplayer
- **Memory**: Efficient ECS memory layout for large datasets

### **Platform Support**
- **PC**: Windows, macOS, Linux
- **Consoles**: PlayStation, Xbox (planned)
- **Mobile**: iOS, Android (future consideration)

---

## 🤝 Contributing

### **Development Guidelines**
- Follow the established 12-subsystem architecture
- Use dependency injection for all service dependencies
- Implement event-driven communication between systems
- Maintain 85%+ test coverage for new features
- Document all public APIs with XML documentation

### **Code Style**
- PascalCase for public members
- camelCase for private fields with `_` prefix
- Interfaces start with `I` (e.g., `IHealthComponent`)
- Events use descriptive names ending with `EventArgs`
- Keep methods under 50 lines

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Unity Technologies for ECS and Netcode packages
- Cysharp for UniTask and R3 reactive extensions
- VContainer team for dependency injection framework
- The open-source community for inspiration and tools

---

*Project Chimera - Where every monster is unique, and every player shapes the world.*
