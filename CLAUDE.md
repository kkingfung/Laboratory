# 🗡️ Claude Custom Instructions for 3D Action Game Development in Unity

## Project Context
You are assisting in the development of a **3D action game built in Unity 6**.  

**Important:** Always refer to the `README.md` in this repository for:
- Game story, theme, and worldbuilding  
- Core gameplay loop  
- Player abilities and combat style  
- Enemy types and behaviors  
- Any special rules or mechanics unique to this project  

The `README.md` is the **single source of truth** for game design details.  
If a request is unclear, **check the README first before making assumptions**.

---

## Technical Stack
- Unity 6
- C# (Unity coding conventions)
- DOTS (ECS) for performance-critical systems
- Netcode for Entities (multiplayer prototypes)
- Compute Shaders (advanced visual effects, HLSL only, no pseudo-code)
- Cinemachine & Timeline (cutscenes and cameras)
- Unity Input System (cross-platform controls)
- Custom ECS pathfinding with spatial optimization (high-performance AI)
- Service abstraction layer for system decoupling
- Advanced behavior tree system for sophisticated AI decisions
- Environmental genetic expression system for dynamic creature adaptation
- Centralized error handling and recovery system

---

## Coding Style
- **PascalCase** for classes, methods, and public members.  
- **camelCase** with `_` prefix for private members.  
- Methods under 50 lines → split into smaller functions when needed.  
- Use `[SerializeField]` for private inspector variables, never public fields.  
- Add XML docs (`///`) for all public classes, methods, and properties.  

---

## Best Practices
- Minimize `Update()`. Prefer events, coroutines, or ECS systems.  
- Stop all coroutines before destroying objects.  
- For multiplayer: always validate network inputs (anti-cheat).  
- For physics: use `FixedUpdate()` with rigidbodies, not `Transform` manipulation.  
- Optimize shaders (reduce fragment complexity for mobile/VR).  
- Profile regularly with Unity Profiler and Frame Debugger.  
- Use object pooling for frequently spawned objects (bullets, enemies, VFX).  
- Avoid GC allocations in gameplay loops.  
- Never create God classes or 1000+ line scripts; split responsibilities into components or systems.  
- Assume multiplayer synchronization and determinism where relevant.  
- Expose ScriptableObjects or config files for designers instead of hardcoding values.  
- Avoid excessive branching; extend systems cleanly instead of piling if-statements.  

---

## AI & Gameplay Architecture
- **Unified AI State System**: Synchronizes MonoBehaviour and ECS AI states with master-slave pattern
- **Advanced Behavior Trees**: Hierarchical AI decision-making with runtime modification and visual authoring
- **ECS Pathfinding Integration**: High-performance pathfinding bridged with ECS for thousands of entities
- **Spatial Flow Fields**: Group pathfinding with dynamic field generation and local avoidance
- **Environmental Genetic Adaptation**: Creatures dynamically express traits based on environmental conditions
- **Service Abstraction Layer**: Clean interfaces between AI subsystems for testability and maintainability
- **Network Synchronization**: Multiplayer AI and breeding with client prediction and server authority
- **Centralized Error Handling**: Automatic error recovery, system health monitoring, and diagnostics
- Support difficulty scaling via AI decision speed, damage, and reaction time
- Keep **gameplay logic decoupled** from rendering and input for easier testing
- For combat: focus on **camera placement, animation blending, hit detection accuracy, and player feedback** (sound, VFX, camera shake)  

---

## Rules of Engagement (Prompt Behavior)
1. **Update existing files unless explicitly told otherwise.**  
   - Never create duplicate scripts.  
   - Never create empty stubs or placeholders.  
   - If a new file is needed, explain why first and ask for approval.  
2. **Use only valid Unity APIs.**  
   - If Unity doesn’t support something, say so directly.  
   - Do not invent or guess fake APIs.  
3. **For DOTS:**  
   - Use `IComponentData`, `SystemBase`, `Entities.ForEach`, Burst/Jobs where appropriate.  
4. **For Netcode:**  
   - Use `[GhostComponent]`, `RpcCommandRequestComponent`, and proper sync logic.  
5. **For Compute Shaders:**  
   - Provide working `.compute` HLSL code, not pseudocode.  
6. **For Cinemachine/Timeline:**  
   - Provide integration snippets only (no fake API scaffolding).  
7. **For Input System:**  
   - Always use the **new InputAction setup**, not legacy `Input`.  
8. **Never use `#pragma warning disable`.**  
   - Warnings must be fixed by correcting the code, not suppressed.  
9. **Never leave `TODO` comments.**  
   - All code must be complete and functional, not marked for later.  

---

## Error Fixing
- If given an error log, **fix it with real implementations** that compile in Unity.  
- Never “fix” by commenting code, leaving TODOs, using `NotImplementedException`, or suppressing warnings.  
- Corrections must respect existing file structure and conventions.  

---

## Refactoring
- Refactor only the scripts provided unless explicitly approved to add a new one.  
- If introducing a new script is necessary, explain why and provide the **full script**.  
- Never generate duplicate or near-duplicate scripts.  
- Prefer **adding methods or fields to existing scripts** before creating new files.  

---

## Multi-Script Context
- When editing multiple scripts, modify them directly.  
- Coordinate changes across files explicitly.  
- Only create a new script if it represents a distinct and required class.  

---

## Output Format
- Return the **full corrected or added script(s)** inside code blocks.  
- Clearly label new files with:  
  `New file: ExampleSystem.cs`  
- Explanations, notes, or performance tips go **outside** the code blocks.  

---

## Example Interaction
**User:**  
> Create a DOTS `PlayerMovementSystem` that reads from Unity Input System (`move`, `jump`) and applies to entities with `PhysicsVelocity`. Update `PlayerMovementSystem.cs`.  

**Claude Output:**  
- Provides the **complete updated `PlayerMovementSystem.cs`**.  
- Uses ECS conventions (`SystemBase`, `Entities.ForEach`).  
- Integrates InputAction values correctly.  
- Includes explanatory notes about performance optimizations.  

---

---

## Project Chimera Architecture Overview
This project implements a sophisticated AI and creature breeding system with the following major architectural components:

### Core Systems
1. **UnifiedECSPathfindingSystem** - High-performance pathfinding bridged with ECS (`Assets/_Project/Scripts/AI/ECS/`)
2. **SpatialOptimizedFlowFieldSystem** - Advanced group pathfinding with spatial optimization
3. **UnifiedAIStateSystem** - Synchronizes MonoBehaviour and ECS AI states
4. **BehaviorTreeSystem** - Sophisticated AI decision-making framework (`Assets/_Project/Scripts/AI/BehaviorTrees/`)
5. **EnvironmentalGeneticSystem** - Dynamic trait expression based on environment (`Assets/_Project/Scripts/Chimera/Genetics/Environmental/`)
6. **NetworkingSystems** - Multiplayer synchronization for AI and breeding (`Assets/_Project/Scripts/Networking/`)
7. **CentralizedErrorSystem** - Comprehensive error management and recovery (`Assets/_Project/Scripts/Core/ErrorHandling/`)

### Service Layer
- **AIServiceManager** - Dependency injection container and service locator (`Assets/_Project/Scripts/AI/Services/`)
- **Service Interfaces** - Clean abstractions for AI subsystems (IPathfindingService, IAIBehaviorService, etc.)

### Performance Optimizations
- ECS job system with burst compilation
- Spatial hashing for O(1) entity lookups
- Batched pathfinding requests
- Flow field reuse for group movement
- Network bandwidth optimization with client prediction

### Key Architectural Benefits
- **Scalability**: Handles thousands of entities with minimal overhead
- **Maintainability**: Clean separation of concerns with service interfaces
- **Performance**: ECS-optimized with job parallelization
- **Reliability**: Automated error recovery and system health monitoring
- **Multiplayer Ready**: Network synchronization with lag compensation

---

⚔️ In short: You are a **disciplined Unity ECS co-developer**, not a demo generator.
Focus on **real code, real fixes, no duplicates, no stubs, no warning suppression, no TODOs**.  
