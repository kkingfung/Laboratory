# Claude Custom Instructions for 3D Action Game Development in Unity

## Project Context
You are assisting in the development of a 3D action game built in Unity (2021 LTS or newer).  

**Important:** Always refer to the `README.md` in this repository for:
- Game story, theme, and worldbuilding
- Core gameplay loop
- Player abilities and combat style
- Enemy types and behaviors
- Any special rules or mechanics unique to this project

The `README.md` is the single source of truth for game design details.  
If a request is unclear, check the README first before making assumptions.

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
- Use **PascalCase** for public members, **camelCase** for private members with `_` prefix.
- Keep methods under 50 lines. Split into smaller functions when possible.
- Use `SerializeField` for private inspector-exposed variables instead of public fields.
- Add XML documentation (`///`) for all public methods, classes, and properties.

## Best Practices
- Minimize use of `Update()`. Prefer events, coroutines, or ECS systems.
- Ensure all coroutines are stopped before objects are destroyed.
- For multiplayer: Validate all network inputs to prevent cheating.
- For physics: Use FixedUpdate() for rigidbody movement, avoid using Transform directly for physics objects.
- Optimize shaders for target hardware (reduce fragment complexity for mobile/VR).

## AI & Gameplay
- Implement enemy AI with state machines or behavior trees.
- Support difficulty scaling by adjusting AI decision speed, damage, and reaction time.
- Keep gameplay logic decoupled from rendering and input for easier testing.

## Performance Guidelines
- Use object pooling for frequently spawned/despawned objects (bullets, enemies, effects).
- Profile regularly with Unity Profiler and Frame Debugger.
- Avoid allocations in Update loops to reduce garbage collection spikes.

## Claude Prompt Behavior
When generating code:
- Follow the coding style and best practices above.
- Always check `README.md` for gameplay and thematic details before coding.
- Provide explanations alongside complex code.
- Suggest performance optimizations when relevant.
- Offer Unity Editor tips (shortcuts, tools, package recommendations) when useful.

When designing gameplay:
- Ensure ideas are consistent with the lore, mechanics, and tone in `README.md`.
- Suggest mechanics that fit 3D action combat.
- Consider camera placement, animation blending, and hit detection accuracy.
- Include ideas for combat feedback (sound, VFX, camera shake).

## Example Interaction
**User:** "Create a basic melee attack system with animation events."
**Claude Output:**  
- C# script for attack handling with `Animator` integration  
- Example animation event usage to trigger hitboxes  
- Notes on hit detection and combo chaining  
- Confirm that attack styles match the descriptions in `README.md`
