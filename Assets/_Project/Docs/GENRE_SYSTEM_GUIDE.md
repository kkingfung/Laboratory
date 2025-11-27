# 🎮 Genre System Guide

## Overview

The **47-Genre Activity System** provides comprehensive configuration for all game activities in Project Chimera. This system combines player skill, chimera traits, partnership dynamics, and age factors to create nuanced, skill-based gameplay.

## Architecture

### Three-Layer System

1. **GenreConfiguration** (`ScriptableObject`)
   - Defines genre-specific mechanics
   - Player skill + chimera trait mappings
   - Performance calculations with partnership dynamics
   - Reward scaling and progression

2. **ActivityConfig** (`ScriptableObject`)
   - General activity settings (existing system)
   - Difficulty durations and multipliers
   - Rank thresholds and rewards
   - Cross-activity bonuses

3. **GenreLibrary** (`ScriptableObject`)
   - Master index of all 47 genres
   - O(1) genre lookup by `ActivityType`
   - Validation and statistics

## Quick Start

### For Designers

#### Generate All Configurations (One-Click Setup)

1. **Open Genre Generator**
   - Menu: `Tools > Chimera > Genre Configuration Generator`

2. **Create Genre Library**
   - Click "Create Genre Library" button
   - Saves to: `Assets/_Project/Resources/Configs/GenreLibrary.asset`

3. **Generate All 47 Genres**
   - Enable "Generate ActivityConfigs" (recommended)
   - Click "Generate All 47 Genre Configurations"
   - Wait for completion dialog

4. **Verify Setup**
   - Click "Validate Genre Library"
   - Check Console for statistics

#### Customize Individual Genres

1. **Locate Genre Asset**
   - Path: `Assets/_Project/Resources/Configs/GenreConfigurations/`
   - Example: `Genre_FPS.asset`

2. **Edit Configuration**
   - **Genre Identity**: Name, description, display name
   - **Core Mechanics**: Player skill, chimera trait, duration
   - **Scoring System**: Skill weights, difficulty scaling
   - **Partnership Dynamics**: Bond strength, age sensitivity
   - **Rewards**: Currency, skill mastery, partnership quality

3. **Personality Effects** (Advanced)
   - Add personality-based performance modifiers
   - Example: "Aggressive" chimeras get +20% in Combat activities

### For Programmers

#### Using GenrePerformanceCalculator

```csharp
using Laboratory.Chimera.Activities;
using Laboratory.Core.Activities;

// Initialize once at startup
public class ActivityBootstrap : MonoBehaviour
{
    [SerializeField] private GenreLibrary genreLibrary;

    void Start()
    {
        GenrePerformanceCalculator.Initialize(genreLibrary);
    }
}

// Calculate activity performance
public void ProcessActivity(ActivityType activityType, Entity chimeraEntity)
{
    // Get values from chimera
    float playerSkillValue = GetPlayerSkill(PlayerSkill.Aiming); // 0-1
    float chimeraTraitValue = GetChimeraTrait(chimeraEntity, ChimeraTrait.Focus); // 0-1
    float bondStrength = GetBondStrength(partnershipEntity); // 0-1
    int chimeraAge = GetChimeraAge(chimeraEntity); // Days

    // Calculate comprehensive result
    var result = GenrePerformanceCalculator.CalculateActivityResult(
        activityType,
        playerSkillValue,
        chimeraTraitValue,
        bondStrength,
        chimeraAge,
        personalityTraits: new[] { "Aggressive", "Brave" }
    );

    // Process results
    Debug.Log($"Performance: {result.performance:F1}%");
    Debug.Log($"Rank: {result.rank}");
    Debug.Log($"Reward: {result.currencyReward} coins");
    Debug.Log($"Skill Gain: +{result.skillGain:F3}");
    Debug.Log($"Partnership Change: {result.partnershipQualityChange:+0.000;-0.000}");

    // Award rewards
    AwardCurrency(result.currencyReward);
    ImproveSkill(result.primarySkillTested, result.skillGain);
    AdjustPartnership(result.partnershipQualityChange);
}
```

#### Integration with PartnershipActivitySystem

The `GenrePerformanceCalculator` is designed to integrate seamlessly:

```csharp
// In PartnershipActivitySystem.cs
private void CalculateFinalScore(ref PartnershipActivityComponent activity)
{
    // Use GenrePerformanceCalculator instead of manual calculation
    var result = GenrePerformanceCalculator.CalculateActivityResult(
        activity.currentActivity,
        activity.playerPerformance,
        GetChimeraTraitValue(activity.chimeraEntity),
        activity.chimeraCooperation,
        GetChimeraAge(activity.chimeraEntity)
    );

    activity.finalScore = result.performance / 100f; // Convert to 0-1 scale
    activity.resultStatus = result.rank;
    activity.skillImprovement = result.skillGain;
}
```

#### Custom Performance Calculations

For genre-specific minigames:

```csharp
// Get genre configuration directly
var genreConfig = genreLibrary.GetGenreConfig(ActivityType.FPS);

if (genreConfig != null)
{
    // Use genre-specific settings
    float duration = genreConfig.baseDuration;
    PlayerSkill requiredSkill = genreConfig.primaryPlayerSkill;
    ChimeraTrait requiredTrait = genreConfig.primaryChimeraTrait;

    // Calculate performance with custom values
    float performance = genreConfig.CalculatePerformance(
        playerSkillValue: 0.8f,   // 80% player skill
        chimeraTraitValue: 0.6f,  // 60% chimera trait
        bondStrength: 0.9f,       // 90% bond
        chimeraAge: 120           // 120 days old (adult)
    );

    // Calculate rewards
    int reward = genreConfig.CalculateReward(performance);
    float skillGain = genreConfig.CalculateSkillGain(performance);
    bool success = genreConfig.IsSuccess(performance);
}
```

## Genre Categories

### Action Genres (7)
- **FPS** - First Person Shooter (Aiming + Focus)
- **TPS** - Third Person Shooter (Aiming + Focus)
- **Fighting** - Fighting Games (Timing + Agility)
- **BeatEmUp** - Beat 'Em Up (Reaction + Strength)
- **HackAndSlash** - Hack and Slash (Reaction + Strength)
- **Stealth** - Stealth Games (Reflexes + Agility)
- **SurvivalHorror** - Survival Horror (Reflexes + Bravery)

### Strategy Genres (5)
- **RTS** - Real-Time Strategy (Strategy + Intelligence)
- **TurnBased** - Turn-Based Strategy (Strategy + Intelligence)
- **4X** - 4X Strategy (Planning + Intelligence)
- **GrandStrategy** - Grand Strategy (Planning + Leadership)
- **AutoBattler** - Auto Battler (Strategy + Adaptability)

### Puzzle Genres (5)
- **Match3** - Match-3 Puzzle (Problem Solving + Patience)
- **Tetris** - Tetris-Like (Reflexes + Adaptability)
- **Physics** - Physics Puzzle (Problem Solving + Intelligence)
- **HiddenObject** - Hidden Object (Observation + Focus)
- **WordGame** - Word Game (Memory + Intelligence)

### Adventure Genres (4)
- **PointAndClick** - Point and Click (Deduction + Curiosity)
- **VisualNovel** - Visual Novel (Observation + Curiosity)
- **WalkingSim** - Walking Simulator (Observation + Curiosity)
- **Metroidvania** - Metroidvania (Reflexes + Curiosity)

### Platform Genres (3)
- **Platformer2D** - 2D Platformer (Reflexes + Agility)
- **Platformer3D** - 3D Platformer (Reflexes + Agility)
- **EndlessRunner** - Endless Runner (Reflexes + Speed)

### Simulation Genres (4)
- **VehicleSim** - Vehicle Simulation (Precision + Precision)
- **FlightSim** - Flight Simulator (Precision + Precision)
- **FarmingSim** - Farming Simulator (Planning + Patience)
- **ConstructionSim** - Construction Simulator (Planning + Patience)

### Arcade Genres (4)
- **Roguelike** - Roguelike (Adaptation + Adaptability)
- **Roguelite** - Roguelite (Adaptation + Adaptability)
- **BulletHell** - Bullet Hell (Reflexes + Focus)
- **ClassicArcade** - Classic Arcade (Reaction + Speed)

### Board & Card Genres (3)
- **BoardGame** - Board Game (Strategy + Sociability)
- **CardGame** - Card Game (Memory + Intelligence)
- **ChessLike** - Chess-Like (Strategy + Intelligence)

### Core Activity Genres (10)
- **Exploration** - Exploration (Observation + Curiosity)
- **Racing** - Racing (Reflexes + Speed)
- **TowerDefense** - Tower Defense (Strategy + Intelligence)
- **BattleRoyale** - Battle Royale (Adaptation + Bravery)
- **CityBuilder** - City Builder (Planning + Intelligence)
- **Detective** - Detective (Deduction + Intelligence)
- **Economics** - Economics (Negotiation + Sociability)
- **Sports** - Sports (Coordination + Strength)
- **Combat** - Combat (Reaction + Strength)
- **Puzzle** - Puzzle (Problem Solving + Intelligence)

### Music Genres (2)
- **RhythmGame** - Rhythm Game (Timing + Rhythm)
- **MusicCreation** - Music Creation (Creativity + Creativity)

## Performance Calculation Details

### Base Formula

```
BasePerformance = (PlayerSkillValue × PlayerSkillWeight) + (ChimeraTraitValue × ChimeraTraitWeight)
BondMultiplier = Lerp(0.7, 1.3, BondStrength / OptimalBondStrength)
AgeFactor = CalculateAgeFactor(ChimeraAge)
FinalPerformance = BasePerformance × BondMultiplier × AgeFactor × DifficultyScaling × 100
```

### Age Factor Scaling

| Age Stage | Days | Performance Multiplier |
|-----------|------|----------------------|
| Baby      | 0-30 | 0.6 → 0.9 (improving) |
| Child     | 30-90 | 0.9 → 1.0 (learning) |
| Teen      | 90-180 | 1.0 (peak) |
| Adult     | 180-365 | 1.0 → 0.95 (stable) |
| Elderly   | 365+ | 0.95 → 0.8 (declining) |

### Reward Calculation

```
NormalizedPerformance = Performance / 100
RewardMultiplier = Lerp(0.5, 2.0, NormalizedPerformance)
FinalReward = BaseCurrencyReward × RewardMultiplier × ScoreMultiplier
```

### Skill Gain Calculation

```
NormalizedPerformance = Performance / 100
SkillGain = BaseSkillMasteryGain × Lerp(0.5, 1.5, NormalizedPerformance)
```

### Partnership Quality Change

```
Success: PartnershipGain = PartnershipQualityGain × NormalizedPerformance
Failure: PartnershipGain = -PartnershipQualityGain × 0.5
```

## Customization Examples

### Example 1: High-Difficulty Combat Genre

```
Genre Type: Combat
Display Name: "Elite Combat Arena"
Primary Player Skill: Reaction
Primary Chimera Trait: Strength
Base Duration: 180s (3 minutes)
Difficulty Scaling: 1.8 (very challenging)

Player Skill Weight: 0.7 (player skill matters more)
Chimera Trait Weight: 0.3
Minimum Passing Score: 60 (harder to succeed)

Base Currency Reward: 250 (higher risk, higher reward)
Base Skill Mastery Gain: 0.02 (learn faster)
Partnership Quality Gain: 0.008 (stronger bonding)
```

### Example 2: Relaxing Puzzle Genre

```
Genre Type: Match3
Display Name: "Zen Match Garden"
Primary Player Skill: ProblemSolving
Primary Chimera Trait: Patience
Base Duration: 300s (5 minutes)
Difficulty Scaling: 0.8 (easier)

Player Skill Weight: 0.5 (balanced)
Chimera Trait Weight: 0.5
Minimum Passing Score: 40 (easier to succeed)

Base Currency Reward: 80 (moderate reward)
Base Skill Mastery Gain: 0.008 (slower learning)
Partnership Quality Gain: 0.01 (strong bonding despite low difficulty)
```

### Example 3: Personality-Driven Racing

```
Genre Type: Racing
Personality Effects:
  - "Competitive": +0.25 (25% bonus)
  - "Cautious": -0.15 (15% penalty)
  - "Reckless": +0.1 cooperation, -0.2 consistency
  - "Timid": -0.2 (20% penalty)

Age Sensitivity: 0.8 (age matters a lot for racing)
Optimal Bond Strength: 0.8 (requires strong partnership)
```

## Validation & Testing

### Validate Genre Library

```csharp
// In Unity Editor
var genreLibrary = AssetDatabase.LoadAssetAtPath<GenreLibrary>(
    "Assets/_Project/Resources/Configs/GenreLibrary.asset");

bool valid = genreLibrary.ValidateCompleteness();
genreLibrary.PrintStatistics();
```

### Test Performance Calculation

```csharp
[Test]
public void TestFPSGenrePerformance()
{
    var genreConfig = Resources.Load<GenreConfiguration>(
        "Configs/GenreConfigurations/Genre_FPS");

    // Optimal conditions
    float performance = genreConfig.CalculatePerformance(
        playerSkillValue: 1.0f,  // Perfect aiming
        chimeraTraitValue: 1.0f, // Perfect focus
        bondStrength: 0.9f,      // Strong bond
        chimeraAge: 120          // Adult chimera
    );

    Assert.Greater(performance, 90f, "Optimal performance should exceed 90%");

    // Calculate rewards
    int reward = genreConfig.CalculateReward(performance);
    Assert.Greater(reward, 150, "Reward should scale with performance");

    bool success = genreConfig.IsSuccess(performance);
    Assert.IsTrue(success, "Should succeed with optimal stats");
}
```

## Performance Optimization

### Lookup Caching

The `GenreLibrary` caches genre lookups in a dictionary:

```csharp
// O(1) lookup after first access
var config = genreLibrary.GetGenreConfig(ActivityType.Racing);
```

### Batch Processing

For multiple activities:

```csharp
// Pre-load genre library once
GenrePerformanceCalculator.Initialize(genreLibrary);

// Process 1000+ activities efficiently
foreach (var activity in activeActivities)
{
    var result = GenrePerformanceCalculator.CalculateActivityResult(
        activity.activityType,
        GetPlayerSkill(),
        GetChimeraTrait(),
        GetBondStrength(),
        GetAge()
    );
    ProcessResult(result);
}
```

## Troubleshooting

### Genre Library Not Found

**Error**: `GenreLibrary not found in Resources/Configs/`

**Solution**:
1. Open `Tools > Chimera > Genre Configuration Generator`
2. Click "Create Genre Library"
3. Ensure asset is at: `Assets/_Project/Resources/Configs/GenreLibrary.asset`

### Missing Genre Configurations

**Error**: `No genre configuration found for [ActivityType]`

**Solution**:
1. Open Genre Configuration Generator
2. Click "Generate All 47 Genre Configurations"
3. Validate with "Validate Genre Library" button

### Performance Calculator Returns Default Values

**Symptom**: All activities use same calculation

**Solution**:
```csharp
// Initialize in bootstrap/startup scene
GenrePerformanceCalculator.Initialize(genreLibrary);
```

## Best Practices

1. **One Library Per Project**
   - Use single `GenreLibrary.asset` for consistency
   - Avoid duplicates

2. **Customize After Generation**
   - Generate defaults first
   - Customize specific genres as needed
   - Document custom changes

3. **Test Performance Curves**
   - Verify min/max performance values
   - Check age factor scaling
   - Validate reward scaling

4. **Version Control**
   - Commit `GenreLibrary.asset`
   - Commit all `Genre_*.asset` files
   - Track changes to configurations

5. **Designer Workflow**
   - Use Unity Inspector for tweaking
   - Test in Play Mode immediately
   - Balance across all 47 genres

## Integration Checklist

- [ ] Genre Library created
- [ ] All 47 genres generated
- [ ] Validation passed (no missing/duplicate genres)
- [ ] GenrePerformanceCalculator initialized in bootstrap
- [ ] ActivitySystem loads ActivityConfig assets
- [ ] PartnershipActivitySystem uses GenrePerformanceCalculator
- [ ] Personality effects configured for key genres
- [ ] Age factors tested across all life stages
- [ ] Reward scaling balanced across difficulties
- [ ] Player feedback reflects skill + chimera contribution

---

## Support

For questions or issues:
- Check DEVELOPER_GUIDE.md for architecture overview
- Review README.md for system integration patterns
- Open Genre Configuration Generator for visual workflow
- Test with provided unit tests in `Tests/Unit/Activities/`

🎮 **Genre system complete: 47 unique activities, balanced for skill + cooperation!**
