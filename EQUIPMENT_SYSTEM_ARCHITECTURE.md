# Equipment System Architecture Documentation

## Overview

Project Chimera uses a **two-layer equipment system** that separates base mechanics from personality integration:

1. **Core Equipment System** (`Laboratory.Core.Equipment`) - Base mechanics, stat bonuses, durability
2. **Chimera Equipment System** (`Laboratory.Chimera.Equipment`) - Personality integration, cooperation effects, age-based resistance

This separation allows equipment to function as traditional RPG gear while also integrating with Project Chimera's unique personality-driven gameplay.

---

## Architecture Philosophy

### Core Equipment System: Mechanics Foundation
**Location:** `Assets/_Project/Scripts/Core/Equipment/`
**Purpose:** Provides base equipment functionality for any gameplay mode

The Core system handles equipment as **stat modifiers**:
- Equip/unequip items to slots (Head, Body, Hands, Feet, Accessories, Tool)
- Inventory management (add, remove, stack items)
- Stat bonuses (Strength, Agility, Intelligence, Vitality, Social, Adaptability)
- Activity bonuses (Racing, Combat, Puzzle, Strategy, Music, Adventure, Platforming, Crafting)
- Durability tracking (items break after use)
- ScriptableObject configuration (designer-friendly)

**Key Classes:**
- `EquipmentSystem` - Main ECS system for equipping/unequipping
- `EquipmentECSSystem` - Performance-optimized ECS processing
- `ActivityEquipmentBonusSystem` - Activity-specific bonus calculations
- `EquipmentCraftingSystem` - Item creation and upgrade

### Chimera Equipment System: Personality Integration
**Location:** `Assets/_Project/Scripts/Chimera/Equipment/`
**Purpose:** Extends Core equipment with personality-driven mechanics

The Chimera system treats equipment as **personality modifiers**:
- Equipment affects personality traits (Curiosity, Playfulness, Aggression, Affection, etc.)
- Chimeras have equipment preferences based on personality
- Good fit = bonus cooperation, poor fit = cooperation penalty
- Age affects resistance to personality changes (elderly chimeras resist equipment effects)
- Equipment happiness tracking

**Key Classes:**
- `PersonalityEquipmentSystem` - Personality integration with equipment
- `EquipmentFitCalculator` - Calculates personality-equipment compatibility

---

## Dependency Flow

```
┌─────────────────────────────────────────────────────────────┐
│                     Player Equips Item                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│          CORE EQUIPMENT SYSTEM (Base Mechanics)              │
│  - Adds item to EquippedItemsComponent                      │
│  - Calculates stat bonuses (STR, AGI, INT, VIT)             │
│  - Calculates activity bonuses (Racing, Combat, Puzzle)     │
│  - Updates EquipmentBonusCache                              │
│  - Tracks durability                                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│       CHIMERA EQUIPMENT SYSTEM (Personality Layer)           │
│  - Calculates personality fit (0.0 = bad, 1.0 = perfect)   │
│  - Applies personality modifiers (scaled by age)            │
│  - Adjusts cooperation level based on fit                   │
│  - Updates chimera mood/happiness/stress                    │
│  - Emits EquipmentPersonalityChangeEvent                    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              PARTNERSHIP PROGRESSION SYSTEM                  │
│  - Receives cooperation changes from equipment fit          │
│  - Updates PartnershipSkillComponent.cooperationLevel       │
│  - Affects activity success rates                           │
└─────────────────────────────────────────────────────────────┘
```

**Update Order (ECS System Groups):**
1. `EquipmentSystem` (Core) - Processes equip/unequip requests
2. `PersonalityStabilitySystem` - Calculates age-based malleability
3. `PersonalityEquipmentSystem` (Chimera) - Applies personality effects **[UpdateAfter(PersonalityStabilitySystem)]**
4. `PartnershipProgressionSystem` - Receives cooperation updates

---

## Data Flow Example

### Example: Player Equips "Scholarly Glasses" on a Curious Chimera

**Step 1: Core Equipment System**
```csharp
// Core system processes EquipItemRequest
EquipItemRequest request = new EquipItemRequest
{
    targetEntity = chimeraEntity,
    itemId = 42, // Scholarly Glasses
    targetSlot = EquipmentSlot.Head
};

// Core system equips item
EquippedItemsComponent.headSlotItemId = 42;
EquipmentBonusCache.intelligenceBonus += 10; // +10 Intelligence
EquipmentBonusCache.puzzleBonus += 5; // +5% Puzzle performance
```

**Step 2: Chimera Equipment System**
```csharp
// Chimera system calculates personality fit
CreaturePersonality personality = GetPersonality(chimeraEntity);
float personalityFit = CalculateEquipmentPersonalityFit(personality, itemId: 42);
// Result: 0.85 (high fit because chimera is Curious)

// Apply personality modifiers (scaled by age malleability)
PersonalityEquipmentEffect effect = new PersonalityEquipmentEffect
{
    equippedItemId = 42,
    curiosityModifier = +5, // Glasses boost curiosity
    personalityFit = 0.85,
    chimeraLikesEquipment = true // Fit > 0.6
};

// Apply cooperation bonus for good fit
float cooperationMod = EquipmentFitCalculator.GetCooperationModifier(0.85);
// Result: +0.15 cooperation bonus
```

**Step 3: Partnership Progression System**
```csharp
// Partnership system receives cooperation update
PartnershipSkillComponent.cooperationLevel += 0.15;
// Result: Chimera is happier and more cooperative!
```

**Net Result:**
- ✅ +10 Intelligence (stat bonus)
- ✅ +5% Puzzle performance (activity bonus)
- ✅ +5 Curiosity (personality boost, scaled by age)
- ✅ +15% Cooperation (personality fit bonus)
- ✅ Chimera happiness increased
- ✅ "Chimera loves the scholarly glasses!" notification

---

## Component Reference

### Core Equipment Components

| Component | Purpose | Location |
|-----------|---------|----------|
| `EquippedItemsComponent` | Tracks which items are equipped in each slot | IComponentData |
| `EquipmentInventoryElement` | Buffer storing unequipped items | IBufferElementData |
| `EquipmentBonusCache` | Cached totals for stat/activity bonuses | IComponentData |
| `EquipmentComponent` | Base equipment data (itemId, slot, durability) | IComponentData |
| `EquipmentStatsComponent` | Stat bonus values (STR, AGI, INT, etc.) | IComponentData |

### Chimera Equipment Components

| Component | Purpose | Location |
|-----------|---------|----------|
| `PersonalityEquipmentEffect` | Personality modifiers from equipment | IComponentData |
| `EquipmentPreferenceComponent` | Chimera's equipment preferences | IComponentData |
| `EquipmentPersonalityChangeEvent` | Event when equipment changes personality | IComponentData |

---

## Usage Examples

### For Gameplay Programmers: Equipping an Item

```csharp
using Laboratory.Core.Equipment;
using Laboratory.Chimera.Equipment;

// Get the equipment system
var equipmentSystem = World.DefaultGameObjectInjectionWorld
    .GetExistingSystemManaged<EquipmentSystem>();

// Create equip request (Core system handles it)
Entity request = equipmentSystem.CreateEquipRequest(
    targetEntity: chimeraEntity,
    itemId: 42, // Scholarly Glasses
    slot: EquipmentSlot.Head
);

// Core system will:
// 1. Validate item exists in inventory
// 2. Equip to head slot
// 3. Calculate stat bonuses
// 4. Update EquipmentBonusCache

// Chimera system will (automatically):
// 1. Calculate personality fit
// 2. Apply personality modifiers
// 3. Adjust cooperation based on fit
// 4. Emit EquipmentPersonalityChangeEvent
```

### For Designers: Creating New Equipment

**Step 1: Create EquipmentConfig ScriptableObject**

In Unity Editor: `Assets > Create > Equipment > Equipment Config`

```csharp
// Example: Scholarly Glasses
itemId: 42
itemName: "Scholarly Glasses"
slot: EquipmentSlot.Head

// Core Stats (handled by Core system)
statBonusType: StatBonusType.Intelligence
statBonusValue: 10
activityBonus: ActivityType.Puzzle
activityBonusValue: 5

// Personality Effects (handled by Chimera system)
curiosityModifier: +5
intelligenceModifier: +2
moodModifier: +0.1
```

**Step 2: Save to `Resources/Configs/Equipment/`**

Both systems will automatically load the config on initialization.

### For UI Programmers: Displaying Equipment Effects

```csharp
// Get equipment bonus cache (Core system)
var bonusCache = entityManager.GetComponentData<EquipmentBonusCache>(chimeraEntity);

Debug.Log($"Intelligence Bonus: +{bonusCache.intelligenceBonus}");
Debug.Log($"Puzzle Bonus: +{bonusCache.puzzleBonus}%");

// Get personality effects (Chimera system)
if (entityManager.HasComponent<PersonalityEquipmentEffect>(chimeraEntity))
{
    var personalityEffect = entityManager.GetComponentData<PersonalityEquipmentEffect>(chimeraEntity);

    Debug.Log($"Personality Fit: {personalityEffect.personalityFit:P0}");
    Debug.Log($"Chimera Likes Equipment: {personalityEffect.chimeraLikesEquipment}");
    Debug.Log($"Cooperation Modifier: +{personalityEffect.cooperationModifier:F2}");
}
```

### For Chimera AI: Equipment Preferences

```csharp
// Get chimera's equipment preferences (generated from personality)
var preferences = entityManager.GetComponentData<EquipmentPreferenceComponent>(chimeraEntity);

if ((preferences.preferredTypes & EquipmentPreferenceFlags.Scholarly) != 0)
{
    Debug.Log("Chimera prefers scholarly equipment!");
}

if ((preferences.dislikedTypes & EquipmentPreferenceFlags.Combat) != 0)
{
    Debug.Log("Chimera dislikes combat equipment.");
}

Debug.Log($"Happiness with current equipment: {preferences.happinessWithCurrentEquipment:P0}");
```

---

## Integration Points

### When to Use Core System Only
- Non-chimera entities (generic creatures, NPCs)
- Stat-based gameplay (traditional RPG mechanics)
- Activities that don't involve personality

### When to Use Both Systems
- Chimera entities with personality
- Partnership-based gameplay
- Activities where cooperation matters
- Age-based progression (personality malleability)

### When to Extend the Systems
- **New stat types:** Add to `StatBonusType` enum and `EquipmentBonusCache`
- **New activity types:** Add to `ActivityType` enum and bonus calculations
- **New personality traits:** Add modifiers to `PersonalityEquipmentEffect`
- **New equipment preferences:** Add flags to `EquipmentPreferenceFlags`

---

## Performance Considerations

### Core Equipment System
- **Burst-compiled jobs** for stat calculations
- **EntityCommandBufferSystem** for structural changes
- **Cached bonus totals** (only recalculate when equipment changes)
- **Parallel processing** with ECS queries

### Chimera Equipment System
- **Runs after PersonalityStabilitySystem** for age calculations
- **Profiler markers** for performance tracking:
  - `Equipment.ProcessEquipRequests`
  - `Equipment.UpdateBonusCache`
  - `Equipment.UpdateDurability`
- **Event-driven** personality changes (no polling)

**Target Performance:**
- 1000+ chimeras with equipped items at 60 FPS
- Equipment changes processed within 1 frame
- Personality effects updated every frame

---

## Testing Equipment Systems

### Unit Tests

```csharp
[Test]
public void EquipItem_AppliesStatBonuses()
{
    // Arrange
    var chimera = CreateChimera();
    var glasses = CreateEquipment(itemId: 42, intelligenceBonus: 10);

    // Act
    EquipItem(chimera, glasses, EquipmentSlot.Head);

    // Assert
    var bonusCache = GetComponent<EquipmentBonusCache>(chimera);
    Assert.AreEqual(10, bonusCache.intelligenceBonus);
}

[Test]
public void EquipItem_CalculatesPersonalityFit()
{
    // Arrange
    var curiousChimera = CreateChimera(personality: new CreaturePersonality { Curiosity = 80 });
    var glasses = CreateEquipment(itemId: 42, type: "Scholarly");

    // Act
    EquipItem(curiousChimera, glasses, EquipmentSlot.Head);

    // Assert
    var effect = GetComponent<PersonalityEquipmentEffect>(curiousChimera);
    Assert.IsTrue(effect.personalityFit > 0.6f, "Curious chimera should like scholarly equipment");
    Assert.IsTrue(effect.chimeraLikesEquipment);
}
```

### Integration Tests

```csharp
[Test]
public void EquipmentFit_AffectsCooperation()
{
    // Arrange
    var chimera = CreateChimera();
    var partnershipBefore = GetComponent<PartnershipSkillComponent>(chimera).cooperationLevel;

    // Act: Equip high-fit item
    EquipItem(chimera, itemId: 42, expectedFit: 0.85f);

    // Assert
    var partnershipAfter = GetComponent<PartnershipSkillComponent>(chimera).cooperationLevel;
    Assert.Greater(partnershipAfter, partnershipBefore, "Good equipment fit should increase cooperation");
}
```

---

## Common Issues & Solutions

### Issue: Equipment Not Applying Personality Effects
**Cause:** Chimera missing `CreaturePersonality` component
**Solution:** Ensure chimera has personality before equipping items

### Issue: Elderly Chimera Personality Not Changing
**Cause:** Age-based malleability scaling working as intended
**Solution:** This is expected! Elderly chimeras (5% malleability) resist personality changes

### Issue: Cooperation Not Updating
**Cause:** `PersonalityEquipmentSystem` running before `PersonalityStabilitySystem`
**Solution:** Verify `[UpdateAfter(typeof(PersonalityStabilitySystem))]` attribute

### Issue: Stat Bonuses Not Applying
**Cause:** `EquipmentBonusCache` not recalculated after equipping
**Solution:** Ensure `UpdateEquipmentBonuses()` runs in `EquipmentSystem.OnUpdate()`

---

## Future Enhancements

### Planned Features
- **Equipment Sets:** Bonus when wearing multiple items from same set
- **Legendary Items:** Unique personality transformations
- **Equipment Crafting:** Combine items with personality traits
- **Equipment Aging:** Items develop history with chimera over time
- **Favorit Equipment:** Chimeras bond with specific items
- **Equipment Trading:** Chimeras with preferences trade equipment
- **Seasonal Equipment:** Special items for events

### Extension Points
- Add new equipment slots via `EquipmentSlot` enum
- Create custom personality calculators via `IPersonalityFitCalculator` interface
- Add equipment-triggered activities via `EquipmentActivityTrigger` component
- Integrate with cosmetic system for visual customization

---

## Contact & Contribution

For questions or contributions to the equipment systems:
- **Architecture Questions:** Review this document and README.md
- **Bug Reports:** Check common issues first, then file detailed report
- **New Features:** Discuss with team before implementing
- **Performance Issues:** Profile with Unity Profiler + ECS Profiler

**Remember:** Core system = mechanics foundation, Chimera system = personality integration. Keep these concerns separated!

---

*Last Updated: 2025-11-24*
*Project Chimera - Equipment System Architecture*
