# Project Chimera - ScriptableObject Configuration Guide

This directory contains example ScriptableObject configurations for immediate designer use. All systems are configured and ready to test!

## 📁 Directory Structure

```
Configs/
├── Activities/
│   ├── RacingConfig.asset       # Racing Circuit activity
│   ├── CombatConfig.asset       # Combat Arena activity
│   └── PuzzleConfig.asset       # Puzzle Academy activity
├── Equipment/
│   ├── SpeedBoots.asset         # Common (Racing, Agility)
│   ├── BattleArmor.asset        # Uncommon (Combat, Vitality)
│   ├── FocusHeadband.asset      # Rare (Puzzle, Intelligence)
│   ├── PowerGauntlets.asset     # Epic (Combat, Strength)
│   └── OmniAdapter.asset        # Legendary (All activities, Adaptability)
└── ProgressionConfig.asset      # Monster leveling and rewards
```

## 🎮 Activity Configurations

### RacingConfig
- **Activity Type**: Racing Circuit
- **Primary Stat**: Agility (50%)
- **Secondary Stat**: Vitality (30%)
- **Tertiary Stat**: Adaptability (20%)
- **Performance Variance**: 5% (moderate randomness)
- **Track Types**: Land, Sky, Water
- **Rewards**: Moderate coins, standard experience

**Designer Notes:**
- Sky tracks heavily favor agility (70%)
- Water tracks favor vitality + adaptability
- Shorter durations than puzzles (45-150s)

### CombatConfig
- **Activity Type**: Combat Arena
- **Primary Stat**: Strength (50%)
- **Secondary Stat**: Vitality (30%)
- **Tertiary Stat**: Intelligence (20%)
- **Combat Variance**: 15% (tactical choices)
- **Combat Styles**: Aggressive, Defensive, Tactical, Balanced
- **Rewards**: 20% higher than racing (risk premium)

**Designer Notes:**
- Tournament mode enabled (3 rounds, 2x rewards)
- 4 combat styles for different monster builds
- Cross-activity bonus: Strategy → Combat (+10%)

### PuzzleConfig
- **Activity Type**: Puzzle Academy
- **Primary Stat**: Intelligence (60%)
- **Secondary Stat**: Adaptability (25%)
- **Tertiary Stat**: Social (15%)
- **Puzzle Variance**: 2% (very deterministic)
- **Puzzle Types**: Logic, Pattern, Memory, Spatial
- **Rewards**: High experience, moderate coins

**Designer Notes:**
- Mastery bonus amplified (1.75x vs 1.5x)
- Speed bonuses enabled (up to 1.5x rewards)
- Collaborative puzzles supported
- Cross-activity bonuses: Puzzle → Strategy (+20%), Puzzle → Crafting (+15%)

## ⚔️ Equipment Items

### Rarity Tiers
| Rarity | Color | Example Item | Stat Bonus | Activity Bonus | Price Range |
|--------|-------|--------------|------------|----------------|-------------|
| Common | Gray | Speed Boots | 15% | 10% | 250 coins |
| Uncommon | Green | Battle Armor | 20% | 15% | 800 coins |
| Rare | Blue | Focus Headband | 25% | 20% | 2,500 coins |
| Epic | Purple | Power Gauntlets | 35% | 25% | 8,000 coins |
| Legendary | Orange | Omni-Adapter | 40% | 20% all | 25,000 coins |

### Equipment Slots
- **Head**: Headbands, helmets, visors
- **Body**: Armor, clothing, suits
- **Hands**: Gauntlets, gloves, bracelets
- **Feet**: Boots, shoes, skates
- **Accessory1/2**: Rings, amulets, trinkets
- **Tool**: Special items, devices

### Stat Bonus Types (Flags)
```csharp
None = 0
Strength = 1      // Physical power
Agility = 2       // Speed and reflexes
Intelligence = 4  // Problem-solving
Vitality = 8      // Health and endurance
Social = 16       // Collaboration
Adaptability = 32 // Environmental adaptation
```

**Multi-stat bonuses**: Combine flags (e.g., `Strength | Agility = 3`)

### Durability System
- **0 durability** = infinite uses
- **> 0 durability** = loses 1 per activity
- **0 remaining** = item breaks (stays in inventory, no bonuses)

## 📈 Progression System

### Leveling Curve
- **Formula**: Exponential scaling (1.15x per level)
- **Max Level**: 100
- **Level 2**: 100 XP
- **Level 10**: ~2,500 XP
- **Level 50**: ~1,000,000 XP
- **Level 100**: ~20,000,000 XP

### Skill Points
- **Per Level**: 1 skill point
- **Milestone Bonus**: +3 every 10 levels
- **Level 10**: 13 total points (10 base + 3 bonus)
- **Level 100**: 127 total points (99 base + 28 bonus)

### Stat Bonuses (from leveling)
- **Per Level**: +2% to all stats
- **Max Bonus**: +100% (at level 100)
- **Level 50**: +98% stat boost

### Daily Challenges
- **Count**: 3 per day
- **Expiration**: 24 hours
- **Rewards**: 500 coins, 5 tokens, 200 XP per challenge
- **Auto-refresh**: When all expire

### Currency Types
- **Coins**: Activity rewards, purchases
- **Gems**: Premium currency (10 gems per 100 coins conversion)
- **Activity Tokens**: Special rewards for Gold/Platinum ranks

## 🎯 Creating New Configurations

### New Activity Config
1. Right-click in Project → `Create > Chimera > Activities > [Type] Config`
2. Configure stat weights (must sum to 1.0)
3. Set difficulty durations and multipliers
4. Define rank thresholds (Bronze: 0.4, Silver: 0.6, Gold: 0.8, Platinum: 0.95)
5. Set rewards (experience, coins, tokens)
6. Place in `Resources/Configs/Activities/`

### New Equipment Item
1. Right-click in Project → `Create > Chimera > Equipment > Equipment Item`
2. Assign unique `itemId` (avoid duplicates!)
3. Configure slot, rarity, and bonuses
4. Set requirements (level, activity level)
5. Define durability and economy values
6. Place in `Resources/Configs/Equipment/`

### Editing Progression Config
1. Select `ProgressionConfig.asset`
2. Adjust leveling curve (`experienceScaling`)
3. Modify skill point rewards
4. Configure daily challenge rewards
5. Enable/disable prestige system

## ⚙️ System Integration

### Activity System
- Loads configs from `Resources/Configs/Activities/`
- Automatically discovers all ActivityConfig assets
- Registers activity implementations on startup

### Equipment System
- Loads configs from `Resources/Configs/Equipment/`
- Uses `itemId` as unique identifier
- Caches bonuses for performance

### Progression System
- Loads `Resources/Configs/ProgressionConfig`
- Calculates XP curves at runtime
- Manages daily challenge refresh

## 🧪 Testing Recommendations

### Test Scenarios
1. **Activity Performance**:
   - Create monsters with varied genetic stats
   - Run each activity type at different difficulties
   - Verify rank thresholds and rewards

2. **Equipment Bonuses**:
   - Equip items and check stat boosts
   - Test activity-specific bonuses
   - Verify durability loss

3. **Progression Curve**:
   - Level monsters from 1 to 100
   - Check skill point accumulation
   - Test daily challenge rotation

### Performance Targets
- **1000+ creatures** at 60 FPS
- **Activity completion** < 1ms per creature
- **Equipment bonus calc** < 0.1ms per creature

## 🔍 Debugging Tips

### Common Issues

**"No activity configurations found"**
→ Ensure configs are in `Resources/Configs/Activities/`

**"Duplicate equipment ID"**
→ Check all equipment configs for unique `itemId` values

**"Stat weights don't sum to 1.0"**
→ Use OnValidate warnings in config inspector

**"Activity not registered"**
→ Check ActivitySystem initialization logs

### Validation
- Use Unity Inspector's `OnValidate()` warnings
- Check console for configuration loading logs
- Verify Resources folder structure matches expectations

## 📊 Balancing Guidelines

### Activity Difficulty Scaling
- **Easy**: 0.5x performance requirement (training)
- **Normal**: 1.0x baseline
- **Hard**: 1.5x rewards, 0.85x performance modifier
- **Expert**: 2.0x rewards, 0.7x performance modifier
- **Master**: 3.0x rewards, 0.5x performance modifier

### Equipment Rarity Balance
| Rarity | Stat Bonus | Activity Bonus | Durability | Cost Multiplier |
|--------|------------|----------------|------------|-----------------|
| Common | 10-15% | 5-10% | 100 | 1x |
| Uncommon | 15-20% | 10-15% | 150 | 3x |
| Rare | 20-30% | 15-20% | 200 | 10x |
| Epic | 30-40% | 20-30% | 300 | 30x |
| Legendary | 40-50% | 20-30% all | 500 | 100x |

### Reward Curves
- **Racing**: Base rewards (speed-focused)
- **Combat**: +20% rewards (risk premium)
- **Puzzle**: +40% experience (learning emphasis)

## 🚀 Quick Start Workflow

1. **Load Unity Project**
2. **Check Configs** in `Resources/Configs/`
3. **Adjust Values** in Inspector as needed
4. **Play Scene** to test systems
5. **Monitor Performance** with Profiler
6. **Iterate Balance** based on gameplay feel

---

**All configurations are production-ready and Burst-optimized!** 🎉

For system architecture details, see `README.md` in project root.
For coding guidelines, see `CLAUDE.md` in project root.
