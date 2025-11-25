# Progression System - Designer Guide

**Version:** 2.0 (Skill-Based System)
**Last Updated:** 2024-11-25
**System:** Laboratory.Chimera.Progression

---

## Table of Contents

1. [Overview](#overview)
2. [Philosophy: NO LEVELS](#philosophy-no-levels)
3. [Skill-Based Progression](#skill-based-progression)
4. [Partnership Quality System](#partnership-quality-system)
5. [UI Configuration](#ui-configuration)
6. [Mastery Tiers](#mastery-tiers)
7. [Design Patterns](#design-patterns)
8. [Balancing Guide](#balancing-guide)
9. [Testing & Validation](#testing--validation)

---

## Overview

Project Chimera uses a **skill-based progression system** instead of traditional levels and XP. Players develop mastery across multiple gameplay genres while building partnership quality with their Chimeras.

### Key Principles

✅ **NO LEVELS** - Skills grow through gameplay, not grinding
✅ **Genre Mastery** - 7 distinct skill categories
✅ **Partnership Quality** - Emotional bond with Chimeras
✅ **Mastery Tiers** - Visible progression milestones
✅ **ECS Integration** - High-performance entity queries

### System Components

```
PlayerProgressionManager (MonoBehaviour)
├── Events: Skill milestones, improvements, quality changes
├── Data: PlayerProgressionData (ScriptableObject)
└── Integration: ECS PartnershipSkillComponent

PlayerProgressionUI (MonoBehaviour)
├── Genre Mastery: 7 skill sliders
├── Partnership Quality: 3 metric sliders
├── ECS Queries: LocalPlayerTag + PartnershipSkillComponent
└── Visual Feedback: Mastery tier labels, fill colors
```

---

## Philosophy: NO LEVELS

### Why No Levels?

**Traditional Level System:**
```
Player: Level 42 (12,450 XP / 15,000 XP to next level)
Problem: Generic number doesn't reflect actual skills
```

**Skill-Based System:**
```
Player Mastery:
- Action:       Expert (0.72)
- Strategy:     Master (0.85)
- Puzzle:       Adept (0.45)
- Economics:    Grandmaster (0.92)

Problem: Shows actual player strengths and weaknesses
```

### Design Benefits

1. **Meaningful Progression**: Skills reflect actual gameplay
2. **No Grinding**: Play the content you enjoy
3. **Multiple Paths**: Excel in your preferred genres
4. **Partner Synergy**: Chimera personality × Genre mastery
5. **Late-Game Depth**: Grandmaster tier takes significant effort

---

## Skill-Based Progression

### The 7 Genre Mastery Skills

#### 1. **Action Mastery** (Combat & Reflexes)
**Improved By:**
- Winning battles (higher difficulty = more gain)
- Perfect dodges/blocks (timing-based)
- Combat combo chains
- Boss encounters

**Gameplay Impact:**
- Unlocks advanced combat moves
- Increases critical hit chance
- Faster stamina regeneration
- Better dodge timing windows

**Example Progression:**
```
Novice:      Basic attacks only
Apprentice:  Dodge unlocked
Adept:       Combo system unlocked
Expert:      Critical hits enabled
Master:      Special moves unlocked
Grandmaster: Ultimate techniques
```

#### 2. **Strategy Mastery** (Planning & Tactics)
**Improved By:**
- Team composition optimization
- Elemental advantage usage
- Trap/ambush setups
- Resource management efficiency

**Gameplay Impact:**
- Reveals enemy weaknesses
- Larger team capacity
- Better AI for Chimera companions
- Territory control bonuses

**Example Progression:**
```
Novice:      See basic enemy stats
Apprentice:  See elemental weaknesses
Adept:       Team size +1
Expert:      See enemy AI patterns
Master:      Team size +2
Grandmaster: Full enemy analysis
```

#### 3. **Puzzle Mastery** (Logic & Problem-Solving)
**Improved By:**
- Completing environmental puzzles
- Genetic breeding combinations
- Research tree optimization
- Secret discovery rate

**Gameplay Impact:**
- Faster research speed
- Breeding preview accuracy
- Unlocks genetic manipulation
- Access to secret areas

**Example Progression:**
```
Novice:      Basic breeding (random traits)
Apprentice:  See 1 guaranteed trait
Adept:       See 2 guaranteed traits
Expert:      Genetic trait selection
Master:      Cross-species breeding
Grandmaster: Custom trait engineering
```

#### 4. **Racing Mastery** (Speed & Mobility)
**Improved By:**
- Mount speed challenges
- Parkour/traversal efficiency
- Escape encounters successfully
- Time trial completions

**Gameplay Impact:**
- Faster mount speed
- Reduced stamina cost for sprinting
- Unlocks air/water mounts
- Better escape success rate

**Example Progression:**
```
Novice:      Ground mounts only
Apprentice:  +10% mount speed
Adept:       +20% mount speed, reduced stamina
Expert:      Flying mounts unlocked
Master:      +30% speed, water mounts
Grandmaster: +50% speed, all mount types
```

#### 5. **Rhythm Mastery** (Timing & Synchronization)
**Improved By:**
- Taming wild Chimeras (timing mini-game)
- Synchronized combo attacks
- Musical/ritual interactions
- Breeding synchronization

**Gameplay Impact:**
- Higher taming success rate
- Team attack synchronization bonuses
- Unlocks ritual/summon abilities
- Better breeding success rate

**Example Progression:**
```
Novice:      Narrow timing windows
Apprentice:  +10% window tolerance
Adept:       Combo sync attacks unlocked
Expert:      +25% taming success
Master:      Ritual abilities unlocked
Grandmaster: Perfect timing = guaranteed success
```

#### 6. **Exploration Mastery** (Discovery & Navigation)
**Improved By:**
- Discovering new areas/biomes
- Finding hidden collectibles
- Mapping uncharted territories
- Rare creature encounters

**Gameplay Impact:**
- Reveals map fog of war
- Detects nearby secrets
- Increases rare spawn rates
- Unlocks fast travel points

**Example Progression:**
```
Novice:      Basic minimap
Apprentice:  Compass directions
Adept:       Secret detection radius
Expert:      +50% rare spawn rate
Master:      Full regional map
Grandmaster: All secrets revealed
```

#### 7. **Economics Mastery** (Trading & Resources)
**Improved By:**
- Profitable trades
- Resource gathering efficiency
- Market speculation
- Territory economic value

**Gameplay Impact:**
- Better trade prices (±20% at Grandmaster)
- Increased gathering yields
- Unlocks auction house features
- Territory passive income

**Example Progression:**
```
Novice:      Standard prices
Apprentice:  ±5% trade modifier
Adept:       +25% gathering yield
Expert:      ±10% trade modifier
Master:      Auction house access
Grandmaster: ±20% trade, passive income
```

### Skill Growth Rates

**Base Formula:**
```
Skill Gain = (Activity Success * Difficulty Modifier * Partnership Bonus) / Skill Level

Where:
- Activity Success: 0.0-1.0 (failure to perfect)
- Difficulty Modifier: 0.5 (easy) to 2.0 (master)
- Partnership Bonus: 1.0 + (Cooperation * 0.5)
- Skill Level: Current mastery (0.0-1.0)
```

**Diminishing Returns:**
- 0.0 → 0.2 (Novice): Fast growth
- 0.2 → 0.6 (Apprentice/Adept/Expert): Moderate growth
- 0.6 → 1.0 (Master/Grandmaster): Slow growth

**Example:**
```
Action Combat Win (Medium Difficulty):
- Success: 0.8 (won with 80% health remaining)
- Difficulty: 1.0 (medium)
- Partnership: 1.3 (Cooperation: 0.6)
- Current Skill: 0.4 (Adept)

Gain = (0.8 * 1.0 * 1.3) / 0.4 = 2.6 points
Normalized: 0.026 mastery gain (2.6%)

Result: 0.4 → 0.426 (still Adept, approaching Expert)
```

---

## Partnership Quality System

### The 3 Partnership Metrics

#### 1. **Cooperation** (Teamwork)
**Increased By:**
- Synchronized team attacks
- Using Chimera abilities strategically
- Completing activities together
- Following Chimera personality preferences

**Decreased By:**
- Ignoring Chimera in battle
- Forcing actions against personality
- Leaving Chimera in storage too long
- Not healing/feeding when needed

**Gameplay Impact:**
- Increases skill gain rate
- Unlocks team combo attacks
- Better AI decision-making
- Higher breeding success with this partner

#### 2. **Trust** (Reliability)
**Increased By:**
- Consistent care and feeding
- Winning battles together
- Keeping Chimera healthy
- Not fleeing from battles unnecessarily

**Decreased By:**
- Letting Chimera faint frequently
- Abandoning battles
- Neglecting health/hunger
- Swapping Chimeras too frequently

**Gameplay Impact:**
- Reduces stamina cost for abilities
- Increases critical hit chance
- Better AI targeting priority
- Unlocks loyalty-based skills

#### 3. **Understanding** (Communication)
**Increased By:**
- Correctly reading Chimera moods
- Using preferred tactics/abilities
- Discovering favorite foods/activities
- Successful breeding outcomes

**Decreased By:**
- Misreading mood signals
- Using disliked abilities
- Forcing incompatible activities
- Failed breeding attempts

**Gameplay Impact:**
- Reveals hidden Chimera stats
- Unlocks genetic trait insights
- Better breeding preview accuracy
- Telepathic communication (flavor)

### Partnership Quality Tiers

```
0.0 - 0.2: Stranger      (just met)
0.2 - 0.4: Acquaintance  (getting to know each other)
0.4 - 0.6: Friend        (comfortable together)
0.6 - 0.8: Companion     (strong bond)
0.8 - 1.0: Soulmate      (perfect partnership)
```

### Quality Growth Formula

```
Quality Change = (Interaction Success * Personality Match * Time Together) / Current Quality

Where:
- Interaction Success: -1.0 to 1.0 (negative = decreased)
- Personality Match: 0.5 (poor fit) to 2.0 (perfect match)
- Time Together: Hours active in team / 10
- Current Quality: Current value (0.0-1.0)
```

**Example:**
```
Feeding Chimera (Favorite Food):
- Success: 1.0 (perfect)
- Personality: 1.5 (Playful Chimera loves this food)
- Time: 1.2 (12 hours together)
- Current: 0.5 (Friend)

Change = (1.0 * 1.5 * 1.2) / 0.5 = 3.6 points
Normalized: 0.036 quality gain (3.6%)

Result: 0.5 → 0.536 (still Friend, approaching Companion)
```

---

## UI Configuration

### PlayerProgressionUI Setup

**Inspector Fields:**

**Genre Mastery Sliders (7):**
```
Action Mastery Slider:
├── Min: 0, Max: 1
├── Whole Numbers: OFF
├── Fill: Gradient (red → orange → yellow → green)
└── Handle: None (read-only visualization)

[Repeat for Strategy, Puzzle, Racing, Rhythm, Exploration, Economics]
```

**Partnership Quality Sliders (3):**
```
Cooperation Slider:
├── Min: 0, Max: 1
├── Whole Numbers: OFF
├── Fill: Gradient (gray → blue)
└── Handle: None (read-only visualization)

[Repeat for Trust, Understanding]
```

**Text Labels:**
```
Action Mastery Label:
├── Text: "Action: {tier}"
├── Font: Bold, 14pt
├── Color: Tier-based (see below)
└── Anchor: Left-aligned with slider

[Repeat for all skills and quality metrics]
```

### Mastery Tier Colors

```csharp
Color GetTierColor(float mastery)
{
    if (mastery >= 1.0f) return Color.magenta;    // Grandmaster
    if (mastery >= 0.8f) return Color.yellow;     // Master
    if (mastery >= 0.6f) return Color.green;      // Expert
    if (mastery >= 0.4f) return Color.cyan;       // Adept
    if (mastery >= 0.2f) return new Color(1f, 0.5f, 0f); // Apprentice (orange)
    return Color.gray;                            // Novice
}
```

### Fill Image Gradients

**Skill Mastery Gradient:**
```
0.0 (Novice):      #808080 (gray)
0.2 (Apprentice):  #FF8000 (orange)
0.4 (Adept):       #00FFFF (cyan)
0.6 (Expert):      #00FF00 (green)
0.8 (Master):      #FFFF00 (yellow)
1.0 (Grandmaster): #FF00FF (magenta)
```

**Partnership Quality Gradient:**
```
0.0 (Stranger):     #808080 (gray)
0.5 (Friend):       #4080FF (blue)
1.0 (Soulmate):     #FF1493 (deep pink)
```

---

## Mastery Tiers

### The 6 Mastery Levels

| Tier | Range | Name | Description | Color | Unlocks |
|------|-------|------|-------------|-------|---------|
| 0 | 0.00 - 0.19 | Novice | Beginner | Gray | Basic abilities |
| 1 | 0.20 - 0.39 | Apprentice | Learning | Orange | Intermediate skills |
| 2 | 0.40 - 0.59 | Adept | Competent | Cyan | Advanced techniques |
| 3 | 0.60 - 0.79 | Expert | Proficient | Green | Expert-only content |
| 4 | 0.80 - 0.99 | Master | Elite | Yellow | Master challenges |
| 5 | 1.00 | Grandmaster | Perfection | Magenta | Ultimate abilities |

### Tier Transition Thresholds

**Design Principle:** Transitions should feel meaningful

```
Novice → Apprentice (0.20):
- Tutorial complete
- Basic mechanics understood
- ~2-3 hours of focused gameplay

Apprentice → Adept (0.40):
- Intermediate mastery
- Can teach others basics
- ~10-15 hours

Adept → Expert (0.60):
- Advanced understanding
- Comfortable with complexity
- ~30-50 hours

Expert → Master (0.80):
- Near-perfect execution
- Deep system knowledge
- ~100-150 hours

Master → Grandmaster (1.00):
- Absolute mastery
- Perfect execution consistently
- ~250-500 hours (intentionally very rare)
```

### Visual Feedback per Tier

**Novice (0.0-0.2):**
- Gray slider fill
- "Novice" label
- No special effects

**Apprentice (0.2-0.4):**
- Orange slider fill
- "Apprentice" label
- Subtle glow on tier transition

**Adept (0.4-0.6):**
- Cyan slider fill
- "Adept" label
- Particle effect on tier transition

**Expert (0.6-0.8):**
- Green slider fill
- "Expert" label
- Larger particle burst on transition

**Master (0.8-1.0):**
- Yellow slider fill
- "Master" label
- Golden particle effect
- UI frame highlights

**Grandmaster (1.0):**
- Magenta slider fill
- "Grandmaster" label
- Rainbow particle cascade
- UI frame iridescent effect
- Achievement unlock

---

## Design Patterns

### Pattern 1: Balanced Generalist

**Player Profile:**
```
Action:       0.50 (Adept)
Strategy:     0.50 (Adept)
Puzzle:       0.50 (Adept)
Racing:       0.50 (Adept)
Rhythm:       0.50 (Adept)
Exploration:  0.50 (Adept)
Economics:    0.50 (Adept)
```

**Design Intent:** Can tackle all content moderately well

**Gameplay Access:**
- All basic + intermediate content unlocked
- No master-tier challenges accessible yet
- Good for casual players exploring all systems

### Pattern 2: Combat Specialist

**Player Profile:**
```
Action:       0.85 (Master)
Strategy:     0.65 (Expert)
Puzzle:       0.25 (Apprentice)
Racing:       0.30 (Apprentice)
Rhythm:       0.40 (Adept)
Exploration:  0.35 (Apprentice)
Economics:    0.20 (Novice)
```

**Design Intent:** Excels at combat, weak elsewhere

**Gameplay Access:**
- Master-tier combat challenges
- Expert-tier strategic content
- Limited breeding/trading/exploration options
- Must cooperate with other players for some content

### Pattern 3: Researcher/Breeder

**Player Profile:**
```
Action:       0.30 (Apprentice)
Strategy:     0.55 (Adept)
Puzzle:       0.90 (Master)
Racing:       0.25 (Apprentice)
Rhythm:       0.60 (Expert)
Exploration:  0.50 (Adept)
Economics:    0.70 (Expert)
```

**Design Intent:** Genetic mastery focus

**Gameplay Access:**
- Master-tier breeding challenges
- Cross-species genetic engineering
- Expert-tier market manipulation
- Weak in direct combat (needs strong Chimeras)

### Pattern 4: Explorer/Trader

**Player Profile:**
```
Action:       0.40 (Adept)
Strategy:     0.45 (Adept)
Puzzle:       0.50 (Adept)
Racing:       0.75 (Expert)
Rhythm:       0.35 (Apprentice)
Exploration:  0.95 (Master)
Economics:    0.85 (Master)
```

**Design Intent:** Open-world mastery

**Gameplay Access:**
- All map areas revealed
- Rare creature spawns maximized
- Master-tier trading profits
- Moderate combat capability

---

## Balancing Guide

### Skill Gain Tuning

**Target Time to Tier (Focused Gameplay):**

```
Novice → Apprentice:     2-4 hours
Apprentice → Adept:      10-15 hours
Adept → Expert:          30-50 hours
Expert → Master:         100-150 hours
Master → Grandmaster:    250-500 hours
```

**Adjustment Levers:**

1. **Activity Rewards:**
```csharp
// Low reward (repetitive grinding):
float gain = 0.001f; // 0.1% per action

// Medium reward (normal gameplay):
float gain = 0.005f; // 0.5% per action

// High reward (challenging content):
float gain = 0.020f; // 2.0% per action

// Boss/milestone reward:
float gain = 0.050f; // 5.0% per milestone
```

2. **Difficulty Scaling:**
```csharp
// Easy content (below player skill):
float modifier = 0.5f; // Half normal gain

// Appropriate challenge:
float modifier = 1.0f; // Normal gain

// Hard content (above player skill):
float modifier = 1.5f; // 1.5x gain

// Master-tier content:
float modifier = 2.0f; // Double gain
```

3. **Diminishing Returns:**
```csharp
// Early tiers (fast progress):
float divider = Mathf.Max(currentSkill, 0.2f);

// Late tiers (slow progress):
float divider = Mathf.Max(currentSkill, 0.8f);
```

### Partnership Quality Tuning

**Target Time to Tier (With One Chimera):**

```
Stranger → Acquaintance:  1-2 hours
Acquaintance → Friend:    5-10 hours
Friend → Companion:       20-30 hours
Companion → Soulmate:     50-100 hours
```

**Interaction Values:**

```csharp
// Positive interactions:
Feed (normal food):       +0.005 (0.5%)
Feed (favorite food):     +0.015 (1.5%)
Win battle together:      +0.010 (1.0%)
Perfect combo attack:     +0.020 (2.0%)
Successful breeding:      +0.030 (3.0%)

// Negative interactions:
Chimera faints:           -0.010 (1.0%)
Flee from battle:         -0.015 (1.5%)
Neglect (1 day):          -0.020 (2.0%)
Force wrong personality:  -0.025 (2.5%)
```

### Content Gating

**What to Lock Behind Mastery Tiers:**

**Novice (No Gates):**
- All tutorial content
- Basic breeding
- Simple combat
- Starting biomes

**Apprentice (0.2+):**
- Intermediate combat moves
- Basic team tactics
- Simple genetic traits
- Second biome access

**Adept (0.4+):**
- Advanced abilities
- Team size +1
- Trait selection in breeding
- Third biome access
- Rare creature encounters

**Expert (0.6+):**
- Expert-only challenges
- Team size +2
- Cross-species breeding
- Fourth biome access
- Epic creature spawns

**Master (0.8+):**
- Master challenges
- Ultimate abilities
- Custom trait engineering
- Fifth biome access
- Legendary creatures

**Grandmaster (1.0):**
- Grandmaster-only cosmetics
- Title/badge
- All secrets revealed
- Passive bonuses in this skill
- Mentor system (teach others)

---

## Testing & Validation

### Automated Tests

**Menu: `Chimera/Tests/Validate Progression UI Integration`**

Runs 6 integration tests:
1. Component Structure (all sliders present)
2. Genre Mastery Sliders (7 configured)
3. Partnership Quality Sliders (3 configured)
4. ECS Integration (component queries work)
5. Event Subscriptions (skill-based events)
6. Mastery Tier Conversion (correct tier names)

### Manual Testing Checklist

**Skill Progression:**
- [ ] Skills increase when performing related activities
- [ ] Diminishing returns work (slower at high mastery)
- [ ] UI updates immediately when skills change
- [ ] Tier transitions trigger visual feedback
- [ ] Tier labels show correct names

**Partnership Quality:**
- [ ] Quality increases with positive interactions
- [ ] Quality decreases with negative interactions
- [ ] Cooperation affects skill gain rate
- [ ] Trust affects stamina costs
- [ ] Understanding reveals hidden stats

**UI Responsiveness:**
- [ ] Sliders fill smoothly (no stuttering)
- [ ] Colors match tier correctly
- [ ] Text labels update with tier changes
- [ ] Layout works at all resolutions
- [ ] Gradients render correctly

**ECS Integration:**
- [ ] Queries find LocalPlayerTag entity
- [ ] PartnershipSkillComponent data loads
- [ ] Fallback works when ECS not initialized
- [ ] No memory leaks from queries
- [ ] NativeArrays disposed properly

### Balance Testing

**Skill Gain Rate Test:**
```
1. Start new game
2. Perform same activity 100 times
3. Record skill before/after
4. Calculate: gain per action = (after - before) / 100
5. Compare to target rates above
6. Adjust ActivityReward values if needed
```

**Time-to-Tier Test:**
```
1. Fresh character
2. Focus on ONE skill exclusively
3. Record time to each tier transition
4. Compare to target hours above
5. Adjust difficulty modifiers if needed
```

**Partnership Quality Test:**
```
1. New Chimera partner
2. Record quality value
3. Perform 50 positive interactions
4. Record new quality value
5. Calculate gain rate
6. Adjust interaction values if needed
```

---

## ECS Architecture Reference

### PartnershipSkillComponent (IComponentData)

```csharp
public struct PartnershipSkillComponent : IComponentData
{
    // Genre Mastery (0.0-1.0)
    public float ActionMastery;
    public float StrategyMastery;
    public float PuzzleMastery;
    public float RacingMastery;
    public float RhythmMastery;
    public float ExplorationMastery;
    public float EconomicsMastery;

    // Partnership Quality (0.0-1.0)
    public float Cooperation;
    public float Trust;
    public float Understanding;
}
```

### LocalPlayerTag (IComponentData)

```csharp
public struct LocalPlayerTag : IComponentData
{
    // Marker component for local player entity
}
```

### ECS Query Pattern

```csharp
var world = World.DefaultGameObjectInjectionWorld;
if (world == null || !world.IsCreated) return null;

var entityManager = world.EntityManager;

// Query for local player with partnership skills
var query = entityManager.CreateEntityQuery(
    ComponentType.ReadOnly<LocalPlayerTag>(),
    ComponentType.ReadOnly<PartnershipSkillComponent>()
);

if (query.CalculateEntityCount() > 0)
{
    var entity = query.GetSingletonEntity();
    var skills = entityManager.GetComponentData<PartnershipSkillComponent>(entity);

    // Use skills data to update UI
    UpdateMasteryUI(skills);
}

// IMPORTANT: Dispose queries when done
query.Dispose();
```

---

## Quick Reference

### Tier Thresholds

| Value | Tier | Name |
|-------|------|------|
| 0.00 | 0 | Novice |
| 0.20 | 1 | Apprentice |
| 0.40 | 2 | Adept |
| 0.60 | 3 | Expert |
| 0.80 | 4 | Master |
| 1.00 | 5 | Grandmaster |

### Genre Skill Categories

1. Action (Combat)
2. Strategy (Tactics)
3. Puzzle (Logic)
4. Racing (Speed)
5. Rhythm (Timing)
6. Exploration (Discovery)
7. Economics (Trading)

### Partnership Quality Metrics

1. Cooperation (Teamwork)
2. Trust (Reliability)
3. Understanding (Communication)

### Key Events

```csharp
PlayerProgressionManager.OnSkillMilestoneReached
├── Genre: string (e.g., "Action")
├── Milestone: string (e.g., "Expert")
└── Value: float (e.g., 0.6f)

PlayerProgressionManager.OnSkillImproved
├── SkillName: string
├── OldValue: float
└── NewValue: float

PlayerProgressionManager.OnPartnershipQualityChanged
├── Cooperation: float
├── Trust: float
└── Understanding: float
```

---

## Support

**Documentation:** `/Assets/_Project/Documentation/PROGRESSION_DESIGNER_GUIDE.md`
**Tests:** `Chimera/Tests/Validate Progression UI Integration`
**Scripts:** `/Assets/_Project/Scripts/Chimera/Progression/`
**UI Prefabs:** `/Assets/_Project/Prefabs/UI/PlayerProgressionUI.prefab`

**Related Guides:**
- `LOCALIZATION_USAGE_GUIDE.md` - For localizing progression text
- `UI_ANIMATION_PRESETS_GUIDE.md` - For animating mastery tier transitions

---

**Build partnerships, master genres, become legendary! 🎮**
