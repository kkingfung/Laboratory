# 📁 Subsystem Reorganization Plan

## Current Status: ⚠️ READY FOR IMPLEMENTATION

This document outlines the proposed folder reorganization to better align with the 12 identified subsystems.

## 🗂️ New Folder Structure

```
Assets/_Project/Scripts/
├── Subsystems/
│   ├── Player/                    # Player System
│   │   ├── Character/             # From Core/Character/
│   │   ├── Camera/                # From Core/Camera/
│   │   ├── Input/                 # From Core/Input/
│   │   └── Tests/
│   │
│   ├── Combat/                    # Combat System
│   │   ├── Health/                # From Core/Health/
│   │   ├── Abilities/             # From Core/Abilities/
│   │   ├── Damage/                # From Gameplay/Combat/
│   │   ├── Ragdoll/               # From Core/Ragdoll/
│   │   └── Tests/
│   │
│   ├── EnemyAI/                   # Enemy/AI System
│   │   ├── NPC/                   # From Core/NPC/
│   │   ├── Behavior/              # AI behavior scripts
│   │   ├── Pathfinding/           # Navigation components
│   │   └── Tests/
│   │
│   ├── UI/                        # UI System (Keep existing structure)
│   │   ├── Helper/                # Existing UI components
│   │   ├── Utils/                 # UI utilities
│   │   ├── Input/                 # UI input handling
│   │   └── Tests/
│   │
│   ├── Networking/                # Networking System (Consolidate)
│   │   ├── Core/                  # From Networking/
│   │   ├── ECS/                   # From Models/ECS/
│   │   ├── Transports/            # Network transport layers
│   │   └── Tests/
│   │
│   ├── Inventory/                 # Inventory System
│   │   ├── Items/                 # From Gameplay/ (ItemData, etc.)
│   │   ├── UI/                    # Inventory UI components
│   │   ├── Storage/               # Inventory storage logic
│   │   └── Tests/
│   │
│   ├── Audio/                     # Audio System
│   │   ├── Core/                  # AudioManager and core systems
│   │   ├── Effects/               # Sound effects and audio processing
│   │   ├── Music/                 # Background music systems
│   │   └── Tests/
│   │
│   ├── Input/                     # Input System (Consolidate)
│   │   ├── Core/                  # From Core/Input/
│   │   ├── Configuration/         # From Infrastructure/Input/
│   │   ├── Rebinding/             # Input rebinding systems
│   │   └── Tests/
│   │
│   ├── Replay/                    # Replay System
│   │   ├── Recording/             # From Core/Replay/
│   │   ├── Playback/              # Replay playback components
│   │   ├── Data/                  # Replay data structures
│   │   └── Tests/
│   │
│   └── Physics/                   # Physics/Ragdoll System
│       ├── Ragdoll/               # From Core/Ragdoll/
│       ├── Collision/             # Collision detection
│       ├── Simulation/            # Physics simulation
│       └── Tests/
│
├── Core/                          # Core Infrastructure (Keep)
│   ├── Bootstrap/                 # Bootstrap system
│   ├── DI/                        # Dependency injection
│   ├── Events/                    # Event system
│   ├── Services/                  # Core services
│   ├── State/                     # Game state management
│   ├── Timing/                    # Timer systems
│   └── Systems/                   # System interfaces
│
├── Infrastructure/                # Infrastructure (Keep)
│   ├── Compatibility/             # Compatibility layers
│   ├── AsyncUtils/                # Async utilities
│   └── Networking/                # Network infrastructure
│
├── Models/                        # Data Models (Keep but clean)
│   ├── Core/                      # Core data models
│   └── Events/                    # Event data structures
│
├── Tools/                         # Development Tools (Keep)
│   ├── Debug/                     # Debug tools
│   ├── Performance/               # Performance tools
│   └── Editor/                    # Editor extensions
│
└── Tests/                         # All Tests (Reorganize)
    ├── Unit/                      # Unit tests by subsystem
    │   ├── Player/
    │   ├── Combat/
    │   ├── EnemyAI/
    │   ├── UI/
    │   ├── Networking/
    │   ├── Inventory/
    │   ├── Audio/
    │   ├── Input/
    │   ├── Replay/
    │   ├── Physics/
    │   └── Core/
    │
    ├── Integration/               # Integration tests
    │   ├── Subsystems/            # Cross-subsystem tests
    │   ├── Network/               # Network integration
    │   └── Performance/           # Performance tests
    │
    └── Utilities/                 # Test utilities and helpers
```

## 🔄 Migration Strategy

### Phase 1: Preparation (Current)
- [x] Create new folder structure
- [x] Identify all scripts and their target subsystems
- [x] Update assembly definitions
- [x] Create migration scripts

### Phase 2: Core Systems Migration
1. Move Core/Abilities/ → Subsystems/Combat/Abilities/
2. Move Core/Health/ → Subsystems/Combat/Health/
3. Move Core/Character/ → Subsystems/Player/Character/
4. Move Core/Camera/ → Subsystems/Player/Camera/
5. Update all references and dependencies

### Phase 3: Gameplay Systems Migration
1. Move Gameplay/Combat/ → Subsystems/Combat/Damage/
2. Move NPC scripts → Subsystems/EnemyAI/NPC/
3. Move UI scripts → Subsystems/UI/ (already in good place)
4. Update assembly references

### Phase 4: Infrastructure Migration
1. Consolidate Input systems → Subsystems/Input/
2. Consolidate Networking → Subsystems/Networking/
3. Move Ragdoll systems → Subsystems/Physics/Ragdoll/
4. Move Replay systems → Subsystems/Replay/

### Phase 5: Testing & Validation
1. Move and organize all tests by subsystem
2. Update test assembly definitions
3. Run full test suite
4. Fix any remaining dependency issues

## 🎯 Benefits of This Organization

### ✅ Clear Separation of Concerns
- Each subsystem has its own folder with related functionality
- Dependencies are more obvious and manageable
- Easier to understand system boundaries

### ✅ Improved Development Workflow
- Developers can focus on specific subsystems
- Easier to find related scripts and components
- Better code organization for team development

### ✅ Enhanced Testing Structure
- Tests are organized by the subsystem they test
- Integration tests clearly show cross-system dependencies
- Performance tests can target specific subsystems

### ✅ Better Assembly Organization
- Each subsystem can have its own assembly definition
- Compilation times can be improved with proper dependencies
- Plugin development becomes easier with clear interfaces

## 🔧 Implementation Commands

```bash
# Example migration commands (pseudo-code)
mv "Assets/_Project/Scripts/Core/Abilities" "Assets/_Project/Scripts/Subsystems/Combat/Abilities"
mv "Assets/_Project/Scripts/Core/Health" "Assets/_Project/Scripts/Subsystems/Combat/Health"
mv "Assets/_Project/Scripts/Core/Character" "Assets/_Project/Scripts/Subsystems/Player/Character"
mv "Assets/_Project/Scripts/Core/Camera" "Assets/_Project/Scripts/Subsystems/Player/Camera"
# ... continue for all subsystems
```

## 🚨 Important Notes

1. **Assembly Definitions**: Update .asmdef files after moving scripts
2. **References**: Update all script references and using statements
3. **Prefabs**: Check prefab references to moved scripts
4. **Tests**: Ensure test assemblies reference the correct production assemblies
5. **Documentation**: Update all documentation with new folder paths

## 📋 Migration Checklist

- [ ] Phase 1: Preparation complete
- [ ] Phase 2: Core Systems migrated
- [ ] Phase 3: Gameplay Systems migrated  
- [ ] Phase 4: Infrastructure migrated
- [ ] Phase 5: Testing & Validation complete
- [ ] All assembly definitions updated
- [ ] All references fixed
- [ ] Full test suite passing
- [ ] Documentation updated
- [ ] Team notified of new structure

---

**Status**: Ready for implementation
**Estimated Time**: 4-6 hours for full migration
**Risk Level**: Medium (requires careful reference updating)
**Recommended Approach**: Incremental migration with testing at each phase
