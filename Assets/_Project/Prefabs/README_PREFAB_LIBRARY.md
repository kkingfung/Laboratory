# Project Chimera Prefab Library

**Created:** 2024-11-27
**Purpose:** Designer-friendly prefab templates for rapid scene assembly

---

## 📁 Library Structure

```
Assets/_Project/Prefabs/
├── Creatures/           # Creature prefab templates
│   ├── Templates/       # Base templates for creature archetypes
│   ├── Common/          # Common creature variants
│   └── Rare/            # Rare/unique creatures
├── UI/                  # UI panel prefabs
│   ├── HUD/             # In-game HUD elements
│   ├── Menus/           # Menu screens
│   └── Dialogs/         # Dialog/popup windows
├── Spawners/            # Spawn point prefabs
│   ├── Creature/        # Creature spawn points
│   ├── Environment/     # Environmental spawners
│   └── Events/          # Event-based spawners
├── VFX/                 # Visual effects (existing)
└── Environment/         # Environmental prefabs
```

---

## 🎮 Quick Start

### Using the Prefab Generator Tool

1. Open Unity Editor
2. Go to **Tools → Chimera → Prefab Generator**
3. Select prefab type (Creature, UI, Spawner)
4. Configure parameters
5. Click **Generate Prefab**
6. Prefab will be created in the appropriate folder

### Manual Prefab Creation

If you prefer to create prefabs manually, see the **Manual Creation Guide** below.

---

## 🧬 Creature Prefabs

### Creature Archetype Templates

| Template Name | Description | Use Case | Components |
|--------------|-------------|----------|------------|
| **BasicCreature** | Minimal creature setup | Testing, prototyping | EnhancedCreatureAuthoring, Transform |
| **AICreature** | Creature with full AI | Standard gameplay creatures | + BehaviorStateComponent, CreatureAI |
| **BattleCreature** | Combat-ready creature | Combat scenarios | + CombatComponent, AbilitySystem |
| **BreedingCreature** | Breeding-enabled | Breeding gameplay | + BreedingComponent, GeneticData |
| **PlayerCompanion** | Player-bonded creature | Player's primary chimeras | + BondingComponent, UI Integration |

### How to Use Creature Prefabs:

1. **Drag prefab** into scene
2. **Assign CreatureDefinition** (ScriptableObject) in inspector
3. **Optional:** Override genetic profile for specific traits
4. **Configure behaviors** using checkboxes (AI, Breeding, etc.)

### Creating Custom Creature Variants:

```csharp
// Create a new CreatureDefinition asset:
// Right-click in Project → Create → Chimera → Creature Definition

// Assign to prefab's EnhancedCreatureAuthoring component
```

---

## 🎨 UI Prefabs

### HUD Elements

| Prefab Name | Description | Use Case |
|------------|-------------|----------|
| **PlayerHUD** | Main player HUD | Gameplay scenes |
| **CreatureStatusBar** | Individual creature health/mood | Above creatures |
| **ActionBar** | Player action buttons | Combat/activities |
| **MinimapPanel** | Minimap display | Navigation |

### Menu Screens

| Prefab Name | Description | Use Case |
|------------|-------------|----------|
| **MainMenu** | Game main menu | Title screen |
| **PauseMenu** | In-game pause | Pause state |
| **SettingsMenu** | Settings configuration | Options |
| **BreedingUI** | Breeding interface | Breeding center |
| **InventoryUI** | Item management | Player inventory |

### Dialog Windows

| Prefab Name | Description | Use Case |
|------------|-------------|----------|
| **ConfirmDialog** | Yes/No confirmation | User confirmation |
| **InfoDialog** | Information display | Notifications |
| **ProgressDialog** | Progress tracking | Loading/processing |
| **RewardDialog** | Reward display | Achievements |

### How to Use UI Prefabs:

1. **Drag prefab** into Canvas
2. **Configure text/images** as needed
3. **Hook up events** in inspector or code
4. **Test in play mode**

---

## 🌍 Spawner Prefabs

### Creature Spawners

| Prefab Name | Description | Configuration |
|------------|-------------|--------------|
| **RandomSpawner** | Random creature spawning | Species list, spawn rate, radius |
| **WaveSpawner** | Wave-based spawning | Wave count, creatures per wave, interval |
| **AreaSpawner** | Area population | Species distribution, density, boundaries |
| **EventSpawner** | Event-triggered spawning | Trigger type, spawn conditions |

### How to Use Spawner Prefabs:

1. **Drag spawner** prefab into scene
2. **Position** at desired spawn location
3. **Assign creatures** to spawn (drag CreatureDefinitions into array)
4. **Configure spawn parameters** (count, interval, radius)
5. **Test spawn behavior** in play mode

---

## 🛠️ Prefab Components Reference

### Required Components for Each Prefab Type

#### Creature Prefabs:
```
GameObject
├── Transform (required)
├── EnhancedCreatureAuthoring (required)
├── MeshRenderer (optional, for visual)
├── Collider (optional, for physics)
├── Rigidbody (optional, for movement)
└── Additional Components (based on template)
```

#### UI Prefabs:
```
Canvas (if root)
├── RectTransform (required)
├── CanvasRenderer (required)
├── UI Component (Image, Text, Button, etc.)
└── Layout Components (optional)
```

#### Spawner Prefabs:
```
GameObject
├── Transform (required)
├── CreatureSpawnerAuthoring (required)
├── Gizmo Drawer (optional, for editor visualization)
└── Trigger Collider (optional, for area spawning)
```

---

## 📝 Best Practices

### Naming Conventions

- **Creature Prefabs:** `Creature_[SpeciesName]_[Variant].prefab`
  - Example: `Creature_Dragon_Fire.prefab`
- **UI Prefabs:** `UI_[PanelType]_[Name].prefab`
  - Example: `UI_HUD_PlayerStatus.prefab`
- **Spawner Prefabs:** `Spawner_[Type]_[Location].prefab`
  - Example: `Spawner_Wave_ForestEntrance.prefab`

### Organization Tips

1. **Use folders** to organize by type/category
2. **Create variants** by duplicating and modifying existing prefabs
3. **Document custom prefabs** with comments in inspector
4. **Test prefabs** in isolation before scene integration

### Performance Considerations

- **Creature Prefabs:**
  - Keep polygon count low (<5,000 tris for common creatures)
  - Use LOD groups for creatures visible at distance
  - Pool frequently spawned prefabs (handled by SpawningSubsystemManager)

- **UI Prefabs:**
  - Minimize Canvas rebuilds (separate Canvases for static/dynamic)
  - Use sprite atlases for UI images
  - Cache UI component references

- **Spawner Prefabs:**
  - Limit concurrent spawns (batch large spawns)
  - Use spatial hashing for area spawners
  - Despawn off-screen creatures

---

## 🔧 Prefab Generator Tool

### Tool Location
**Menu:** `Tools → Chimera → Prefab Generator`

### Features

1. **Creature Template Generator**
   - Select archetype (Basic, AI, Battle, Breeding, Companion)
   - Auto-adds required components
   - Generates with placeholder visuals
   - Saves to Prefabs/Creatures/Templates/

2. **UI Template Generator**
   - Select UI type (HUD, Menu, Dialog)
   - Creates Canvas hierarchy
   - Sets up anchors/sizing
   - Saves to Prefabs/UI/[Type]/

3. **Spawner Template Generator**
   - Select spawner type (Random, Wave, Area, Event)
   - Configures default spawn parameters
   - Adds editor gizmos for visualization
   - Saves to Prefabs/Spawners/[Type]/

### How to Use Generator:

```csharp
// Script is located at: Assets/_Project/Editor/PrefabGeneratorTool.cs

// Usage:
// 1. Open window: Tools → Chimera → Prefab Generator
// 2. Select template type
// 3. Enter prefab name
// 4. Configure settings
// 5. Click "Generate"
// 6. Prefab created in appropriate folder
```

---

## 📦 Prefab Variants

Unity's Prefab Variant system allows you to create specialized versions:

### Creating a Variant:

1. **Right-click** base prefab
2. Select **Create → Prefab Variant**
3. **Modify** variant properties
4. **Save** variant with descriptive name

### Common Variant Use Cases:

- **Creature Variants:** Same species, different colors/abilities
- **UI Variants:** Same layout, different themes/sizes
- **Spawner Variants:** Same pattern, different spawn lists

---

## 🎯 Example Workflows

### Workflow 1: Create Custom Creature

1. **Create CreatureDefinition:**
   - Right-click → Create → Chimera → Creature Definition
   - Name: "Dragon_Fire"
   - Configure stats, biomes, behaviors

2. **Generate Prefab:**
   - Tools → Chimera → Prefab Generator
   - Type: Creature → AI Creature
   - Name: "Creature_Dragon_Fire"
   - Generate

3. **Assign Definition:**
   - Open prefab
   - Assign "Dragon_Fire" CreatureDefinition
   - Configure components (enable AI, Breeding, etc.)

4. **Add Visual:**
   - Add child GameObject with MeshRenderer
   - Assign dragon model/materials

5. **Test:**
   - Drag into scene
   - Play mode → verify behavior

---

### Workflow 2: Create Breeding UI

1. **Generate UI Prefab:**
   - Tools → Chimera → Prefab Generator
   - Type: UI → Dialog
   - Name: "UI_Dialog_BreedingSelection"
   - Generate

2. **Customize Layout:**
   - Add parent slots (2x Image components)
   - Add offspring preview (Image component)
   - Add confirm/cancel buttons
   - Add compatibility meter (Slider)

3. **Hook Up Scripts:**
   - Add BreedingUIController component
   - Assign UI references in inspector

4. **Test:**
   - Create test scene with Canvas
   - Instantiate dialog prefab
   - Test interaction flow

---

### Workflow 3: Create Wave Spawner

1. **Generate Spawner:**
   - Tools → Chimera → Prefab Generator
   - Type: Spawner → Wave Spawner
   - Name: "Spawner_Wave_ForestBossFight"
   - Generate

2. **Configure Waves:**
   - Wave 1: 5x "Creature_Goblin"
   - Wave 2: 10x "Creature_Goblin", 2x "Creature_Orc"
   - Wave 3: 1x "Creature_Boss_ForestKing"

3. **Set Spawn Parameters:**
   - Spawn radius: 15m
   - Wave interval: 30 seconds
   - Spawn pattern: Circle

4. **Place in Scene:**
   - Position at arena center
   - Adjust spawn radius visualization
   - Test wave progression

---

## 🚨 Troubleshooting

### Prefab Won't Spawn

**Issue:** Creature prefab doesn't appear when spawned

**Solutions:**
- ✅ Verify prefab has collider/renderer
- ✅ Check layer/tag settings
- ✅ Ensure spawner has valid position
- ✅ Verify CreatureDefinition is assigned

---

### UI Prefab Not Visible

**Issue:** UI prefab instantiated but not visible

**Solutions:**
- ✅ Check Canvas render mode (Screen Space)
- ✅ Verify RectTransform anchors
- ✅ Ensure Canvas is in camera view
- ✅ Check sorting order/layer

---

### Performance Issues with Spawners

**Issue:** Too many creatures spawning, FPS drops

**Solutions:**
- ✅ Reduce spawn count/rate
- ✅ Enable object pooling
- ✅ Use LOD groups on creature prefabs
- ✅ Limit active creature count
- ✅ Implement spatial culling

---

## 📚 Related Documentation

- **Creature Authoring:** See `EnhancedCreatureAuthoring.cs` documentation
- **Spawning System:** See `SpawningSubsystemManager` documentation
- **UI System:** See `DemoHUD.cs` and UI documentation
- **ECS Integration:** See `ARCHITECTURE.md` for ECS patterns

---

## 🔗 Quick Links

| Resource | Location |
|----------|----------|
| **Prefab Generator Tool** | `Assets/_Project/Editor/PrefabGeneratorTool.cs` |
| **Creature Authoring** | `Assets/_Project/Scripts/Chimera/ECS/EnhancedCreatureAuthoring.cs` |
| **Spawner Authoring** | `Assets/_Project/Scripts/Chimera/Spawning/CreatureSpawnerAuthoring.cs` |
| **UI Templates** | `Assets/_Project/Scripts/Demo/UI/` |
| **Existing Prefabs** | `Assets/_Project/Prefabs/` |

---

**Last Updated:** 2024-11-27
**Maintained By:** Project Chimera Design Team
**Questions?** Check DEVELOPER_GUIDE.md or ask in #chimera-dev channel
