# GitHub Copilot Custom Instructions for 3D Action Game Development in Unity

## Project Context
You are assisting in the development of a 3D action game built in Unity (2021 LTS or newer).  

**Important:** Always refer to the `README.md` in this repository for:
- Game story, theme, and worldbuilding
- Core gameplay loop
- Player abilities and combat style
- Enemy types and behaviors
- Any special rules or mechanics unique to this project

The `README.md` is the single source of truth for game design details.  
Do not assume any gameplay or mechanic details that are not listed there.

## Technical Stack
- Unity 2021 LTS+
- C# (following Unity conventions)
- DOTS (ECS) for performance-critical systems
- Compute Shaders for advanced visual effects
- Cinemachine & Timeline for cutscenes and camera work
- Unity Input System for cross-platform controls
- NavMesh or custom pathfinding for enemy AI
- Netcode for multiplayer prototypes

## Coding Style
- PascalCase for public members; camelCase for private fields with `_` prefix.
- Keep methods under 50 lines; split logic into smaller functions if longer.
- Use `SerializeField` for private variables exposed in Inspector.
- XML documentation (`///`) for all public methods, classes, and properties.

## Best Practices
- Minimize use of `Update()`; prefer events, coroutines, or ECS systems.
- Stop all coroutines before objects are destroyed.
- Validate all network inputs in multiplayer.
- Use FixedUpdate() for physics-related movement; avoid modifying Transform directly on Rigidbody objects.
- Optimize shaders and materials for target platforms.

## AI & Gameplay
- Implement enemy AI with state machines or behavior trees.
- Scale difficulty by adjusting AI speed, damage, and reaction time.
- Keep gameplay logic decoupled from rendering and input for easier testing.

## Performance Guidelines
- Use object pooling for frequently spawned/despawned objects.
- Profile regularly with Unity Profiler and Frame Debugger.
- Avoid allocations in Update loops to minimize garbage collection spikes.

## Copilot Behavior
- Always check `README.md` for gameplay and thematic details before generating code.
- Follow coding style and best practices outlined above.
- Suggest performance improvements or Unity Editor tips when relevant.
- Include minimal examples for scripts and systems where appropriate.

## Example Interaction
**User:** "Generate a health system for enemies and players."
**Copilot Output:**  
- C# scripts for Health and Damage handling  
- Integration with UI (health bars)  
- Code comments following XML documentation style  
- Confirm mechanics are consistent with the README.md
