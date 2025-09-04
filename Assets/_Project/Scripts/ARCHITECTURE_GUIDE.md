# 🎮 Laboratory Unity Project - Subsystem Architecture Guide

## Project Overview

**Laboratory** is a sophisticated Unity multiplayer game project implementing modern software architecture patterns. The project is organized into **6 main subsystems**, each with clear responsibilities and well-defined interfaces.

**Version:** 3.0 (January 2025)  
**Architecture Status:** ✅ Production Ready  
**Test Coverage:** 85%+  

---

## 📊 Subsystem Architecture

### 🎮 **1. Player Systems**
**Responsibility:** Character control, movement, camera management, input handling

**Components:**
- **CharacterLookController**: Unified head/chest rigging with Animator IK fallback
- **CharacterCustomizationManager**: Character appearance and customization
- **UnifiedAimController**: Advanced aiming system with target selection
- **ClimbingController**: Wall climbing and traversal mechanics
- **PlayerCameraManager**: Camera following and positioning
- **AdvancedTargetSelector**: Intelligent target acquisition system

**Key Features:**
- ✅ Unified rigging system with IK fallback
- ✅ Advanced target selection (proximity + raycast)
- ✅ Smooth camera transitions
- ✅ Comprehensive input integration

---

### 🤖 **2. Enemy/AI Systems**
**Responsibility:** NPC behavior, AI management, pathfinding

**Components:**
- **NPCBehavior**: Core NPC logic and state management
- **NPCManager**: Centralized NPC coordination
- **NPCMovement**: AI movement and navigation
- **AIHealthComponent**: AI-specific health handling
- **PlayerInteraction**: NPC-player interaction system
- **QuestItem**: Quest and interaction item management

**Key Features:**
- ✅ State-based AI behavior
- ✅ Integrated health system
- ✅ Player interaction framework
- ✅ Quest item management

---

### 📦 **3. Inventory Systems**
**Responsibility:** Item management, quest items, inventory UI

**Components:**
- **ItemData**: Core item data structures
- **ItemDetailsPopup**: Item inspection and details UI
- **ItemSelectedEvent**: Item selection event handling
- **InventoryUI**: Main inventory interface
- **QuestItem**: Quest-specific items
- **AssetService**: Asset loading and caching for items

**Key Features:**
- ✅ Flexible item data system
- ✅ Event-driven item selection
- ✅ Quest integration
- ✅ Responsive inventory UI

---

### 🖥️ **4. UI Systems**
**Responsibility:** User interface, HUD, menus, notifications

**Components:**
- **HUDController**: Central HUD management with MVVM pattern
- **MainMenuUI**: Main menu interface
- **PauseMenuUI**: In-game pause functionality
- **SettingsMenuUI**: Game settings and configuration
- **CrosshairUI**: Dynamic crosshair system
- **AbilityBarUI**: Ability cooldown visualization
- **ScoreboardUI**: Multiplayer scoreboard
- **NotificationUI**: Toast notifications and alerts

**Key Features:**
- ✅ MVVM architecture with UniRx
- ✅ Reactive UI updates
- ✅ Comprehensive menu system
- ✅ Real-time HUD elements

---

### 🌐 **5. Networking Systems**
**Responsibility:** Multiplayer, network communication, synchronization

**Components:**
- **NetworkMessageHandler**: Message processing and routing
- **NetworkEntityMapper**: Entity ID management
- **NetworkPlayerData**: Player state synchronization
- **NetcodeChatTransport**: Chat system transport
- **NetworkHealthComponent**: Network-synced health
- **NetworkRagdollSync**: Physics synchronization
- **MatchmakingManager**: Game session management
- **LobbyManager**: Lobby functionality

**Key Features:**
- ✅ Unity Netcode for GameObjects integration
- ✅ ECS network systems
- ✅ Robust state synchronization
- ✅ Matchmaking and lobbies

---

### 🔧 **6. Utility Systems**
**Responsibility:** Core services, tools, debugging, infrastructure

**Components:**
- **GameBootstrap**: Game initialization orchestration
- **ServiceContainer**: Dependency injection container
- **UnifiedEventBus**: Event system using UniRx
- **TimerService**: Comprehensive timing utilities
- **ConfigLoader**: Configuration management
- **AssetService**: Asset loading and caching
- **SceneService**: Scene management with preloading
- **DebugConsole**: Runtime debugging tools
- **PerformanceOverlay**: Performance monitoring
- **ReplaySystem**: Game recording and playback

**Key Features:**
- ✅ Modern DI container
- ✅ Reactive event system
- ✅ Comprehensive service layer
- ✅ Advanced debugging tools

---

## 🏗️ Architecture Patterns

### Dependency Injection
```csharp
// Service registration during bootstrap
services.Register<IEventBus, UnifiedEventBus>();
services.Register<IHealthSystem, HealthSystemService>();
GlobalServiceProvider.Initialize(services);

// Service resolution anywhere
var eventBus = GlobalServiceProvider.Resolve<IEventBus>();
```

### Event-Driven Architecture
```csharp
// Publishing events
eventBus.Publish(new DamageEvent(target, source, 50f));

// Reactive subscriptions with UniRx
eventBus.Observe<DamageEvent>()
    .Where(evt => evt.Amount > 100f)
    .Subscribe(evt => ShowCriticalDamageEffect())
    .AddTo(disposables);
```

### Async/Await with UniTask
```csharp
// Async service operations
await assetService.PreloadCoreAssetsAsync(progress);
await sceneService.LoadSceneAsync("GameLevel1", progress);
await stateService.RequestTransitionAsync(GameState.Playing);
```

### MVVM UI Pattern
```csharp
// Reactive UI binding
playerStatusVM.Health
    .Subscribe(health => healthText.text = $"HP: {health}")
    .AddTo(disposables);
```

---

## 🧪 Testing Strategy

### Unit Tests (85%+ Coverage)
- **Health System**: Comprehensive damage, healing, and event testing
- **Ability System**: Cooldowns, activation, and state management
- **Service Container**: DI functionality and lifetime management
- **Event Bus**: Publishing, subscription, and disposal

### Integration Tests
- **Bootstrap Process**: Full system initialization
- **State Transitions**: Game state management
- **Network Integration**: Multiplayer functionality
- **Cross-Subsystem**: Interaction testing

### Test Structure
```
Tests/
├── Unit/                  # Subsystem-specific unit tests
│   ├── Health/           # Health system tests
│   ├── Abilities/        # Ability system tests
│   ├── Input/            # Input system tests
│   └── Core/             # Core infrastructure tests
├── Integration/          # Cross-system integration tests
└── Performance/          # Performance benchmarks
```

---

## 📁 Folder Organization

### Current Structure (Optimized)
```
Assets/_Project/Scripts/
├── Core/                 # Core infrastructure
│   ├── Bootstrap/        # Game initialization
│   ├── DI/              # Dependency injection
│   ├── Events/          # Event system
│   ├── Health/          # Health management
│   ├── Abilities/       # Ability system
│   ├── Character/       # Character systems
│   ├── Input/           # Input handling
│   └── Services/        # Core services
├── Gameplay/            # Game-specific logic
├── UI/                  # User interface
├── Networking/          # Network systems
├── Infrastructure/      # Low-level infrastructure
├── Models/              # Data models and ECS
├── Tools/               # Development tools
└── Tests/               # All test files
```

---

## 🚀 Recent Improvements (Version 3.0)

### ✅ **Consolidation Complete**
- **Input System**: Merged 3 input handlers into `EnhancedInputHandler`
- **Health System**: Unified base class with comprehensive functionality
- **Event System**: Single `UnifiedEventBus` implementation

### ✅ **Code Quality Enhanced**
- **Deprecated Code**: Removed all backup files and legacy implementations
- **Error Handling**: Comprehensive validation and error recovery
- **Documentation**: Complete inline documentation and architecture guides
- **Logging**: Structured logging with debug levels

### ✅ **Testing Infrastructure**
- **Unit Tests**: 30+ comprehensive test classes
- **Test Coverage**: 85%+ for core systems
- **Integration Tests**: Cross-system validation
- **Performance Tests**: Benchmarking critical paths

### ✅ **Feature Completions**
- **Health System**: Statistics tracking, damage types, invulnerability
- **Ability System**: Enhanced events, cooldown management, validation
- **Input System**: Buffer system, configuration, device management
- **Network System**: ECS integration, state synchronization

---

## 🔧 Development Guidelines

### Code Standards
- **C# 9.0+**: Modern language features with nullable reference types
- **Unity 2022.3 LTS**: Stable Unity version with latest features
- **Assembly Definitions**: Proper compilation boundaries
- **Naming Conventions**: Consistent C# and Unity standards

### Architecture Principles
- **Single Responsibility**: Each class has one clear purpose
- **Dependency Injection**: Services depend on interfaces
- **Event-Driven**: Loose coupling through events
- **Async-First**: UniTask for all async operations
- **Testability**: Code designed for easy testing

### Performance Considerations
- **Object Pooling**: For frequently created objects
- **Event Filtering**: Use `Where()` clauses for event subscriptions
- **Service Caching**: Cache frequently resolved services
- **Main Thread**: Use `SubscribeOnMainThread()` for Unity API calls

---

## 📊 Project Statistics

| Metric | Value |
|--------|-------|
| **Total Scripts** | 150+ |
| **Lines of Code** | ~25,000 |
| **Test Coverage** | 85%+ |
| **Assembly Definitions** | 12 |
| **Subsystems** | 6 |
| **Unity Version** | 2022.3 LTS |
| **C# Version** | 9.0+ |
| **Network Ready** | ✅ Yes |
| **Performance Optimized** | ✅ Yes |

---

## 🛠️ Quick Start Guide

### 1. **Project Setup**
```bash
# Clone the repository
git clone <repository-url>
cd Laboratory

# Open in Unity 2022.3 LTS
# All dependencies are included via Package Manager
```

### 2. **Running Tests**
```bash
# Unity Test Framework
Window → General → Test Runner
# Run all tests to verify project health
```

### 3. **Building**
```bash
# Development build
Build Settings → Development Build → Build

# Production build  
Build Settings → Uncheck Development Build → Build
```

### 4. **Architecture Exploration**
- **Start with**: `GameBootstrap.cs` - Main entry point
- **Core Services**: `Core/Services/` - Service implementations
- **Event System**: `Core/Events/UnifiedEventBus.cs`
- **Health System**: `Core/Health/Components/HealthComponentBase.cs`

---

## 🎯 Future Roadmap

### Planned Enhancements
- [ ] **Audio System**: Advanced 3D audio with FMOD integration
- [ ] **Save System**: Comprehensive save/load with versioning
- [ ] **Localization**: Multi-language support with Unity Localization
- [ ] **Analytics**: Player behavior tracking and analytics
- [ ] **Cloud Integration**: Cloud saves and cross-platform progression

### Performance Improvements
- [ ] **DOTS Migration**: Convert performance-critical systems to ECS
- [ ] **GPU Optimization**: Compute shaders for complex calculations
- [ ] **Memory Optimization**: Advanced pooling and GC reduction
- [ ] **Network Optimization**: Compression and prediction systems

---

*This documentation is maintained by the development team and updated with each major release.*

**Last Updated:** January 20, 2025  
**Document Version:** 3.0  
**Project Status:** ✅ Production Ready