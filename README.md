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

This project leverages Unity’s **latest ECS, AI, and Multiplayer packages** with third-party tools for scalability and modularity.  

### Core Framework  
- [Unity ECS (Entities, Physics, Collections)](https://docs.unity3d.com/Packages/com.unity.entities@latest/) – Scalable AI & DNA simulations.  
- [Netcode for GameObjects](https://docs-multiplayer.unity3d.com/) + Unity Transport – Multiplayer backbone.  
- [UniTask](https://github.com/Cysharp/UniTask), [R3](https://github.com/Cysharp/R3), [MessagePipe](https://github.com/Cysharp/MessagePipe) – Async, reactive, event-driven systems.  
- [VContainer](https://github.com/hadashiA/VContainer) – Dependency injection for modular architecture.  

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
