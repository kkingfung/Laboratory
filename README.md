# 🔧 Tyrant: Dark Fantasy Strategy Card Game

> _"Even the dead kneel before power."_

## 🎮 Game Overview

| Property       | Description                                       |
|----------------|---------------------------------------------------|
| **Title**      | Tyrant                                            |
| **Genre**      | Dark Fantasy Strategy Card Game                   |
| **Modes**      | PvE, PvP, Friend Match, Quick Game                |
| **Platform**   | PC / Console / Mobile (TBD)                       |
| **Input**      | Gamepad, Keyboard & Mouse, Touchscreen            |
| **Loop**       | Strategic deck-building with RNG weapon draws     |

Tyrant combines race-specific deckbuilding, randomized weapon-draw combat, and a haunting world torn by the collapse of divine order. Master strategy and fate alike as one of 10 powerful factions battling for the right to rewrite the world.

---

## 🧱 Lore: The World of Tyrak

The gods are gone. Magic has broken the world, raising the dead and fueling empires of despair. The Tyrant's Throne sits vacant, contested by ancient races with forbidden weapons and terrifying powers. The battle is not just for territory — it’s for control of reality.

You are one of them. Or something worse.

---

## ⚔️ Core Gameplay

Players construct a custom deck of:

- **Servant Cards**: Unique units with race-based effects
- **Weapon Cards**: One-time random actions/effects

Winning requires synergy, adaptability, and mastery of timing.

### Match Flow:

1. **Status Resolve**: Apply/remove effects
2. **Card Decision**: Play 1 or more than 1 Servant (optional)
3. **Effect Resolution**: Activate Servant effects
4. **Weapon Draw**: Player attacks with random weapon
5. **Health Check**: Game ends if HP reaches 0
6. **Round End**: Reset for next round

> • Starting HP: 20 (or 10 for Quick Game)
> • Servants remain until enemy draws a Weapon (except Ghosts/Skeletons)
> • Weapon Deck reshuffles after full use

---

## 🧩 Deck-Building Rules

### Deck Cost Limit: **10 points**

#### Servants:
| Rarity | Cost | Note                |
|--------|------|---------------------|
| R1     | 1pt  | Common Servants     |
| R2     | 2pt  | Stronger Abilities  |
| R3     | 3pt  | Rare Power Cards    |

- Each race has **5 Servants**

#### Weapons:
- **5 × R1**, **3 × R2**, **2 × R3**
- Some are **race-exclusive**, others **shared**

---

## 🛡️ Weapon Mechanics

Weapon Cards apply more than just damage. They define the meta.

### Common Attributes:
- **Fear, Charm, Confuse**: Inflict status
- **Piercing, Burn, Bleed**: Bypass/block or DOT
- **Suppress**: Prevent next Servant play
- **Mirror Break**: Reflect status effects
- **Race Sync**: Bonus effect if matching race
- **Cursed Draw**: Both discard 1 card
- **Scavenge**: Draw a Servant if you kill

### Special Mechanic: **Destiny Draw**
Once per match, draw 2 Weapon Cards, pick 1. Adds high-risk decision making.

---

## 🧝 Races

| Race        | Theme                | Weapon Example (R1-R3)                    |
|-------------|----------------------|-------------------------------------------|
| Demon       | Fire, rage           | Infernal Blade, Soulbrand Spear, Hell Cleaver |
| Undead      | Sacrifice, decay     | Rotten Scythe, Grave Chain, Tombclaw         |
| Troll       | Brute force, healing | Stone Club, Gnarlhorn Maul, Bone Shatterer  |
| Goblin      | Chaos, madness       | Junk Knife, Boomstick, Spiked Toy           |
| Ghost       | Haunting, control    | Wailing Edge, Spectral Blade, Phantom Chain |
| Succubus    | Charm, control       | Whip of Thorns, Heartpiercer Needle         |
| Tree Man    | Nature, resilience   | Root Fist, Verdant Maul                     |
| Lizard Man  | Poison, cunning      | Poison Fang Blade, Chameleon Scythe         |
| Gargoyle    | Stone, vengeance     | Stone Talon, Obsidian Spike                 |
| Dark Elf    | Precision, corruption| Moonfang Blade, Nightfall Saber             |

> Each race has its own **leader**, **lore**, **5 unique servants**, and **exclusive weapons**

More: [See full race data](Docs/RaceLore.md)

---

## 🧠 UI & Systems

### Core Scenes
- Intro, Title Menu, Match Modes (PvE/PvP/Friend), Match Result
- Deck Edit, Replay History, Deck Check

### UI Components
- Card Prefabs (Weapon/Servant)
- Card Details, Status Visuals
- Player HUD, Input Buttons, Loading Screens

> Cleanly designed for cross-input platforms

---

## 🎯 Unique Selling Points

- 🔀 **RNG + Strategy Hybrid**: Randomized weapon draws with strategic timing
- 🧠 **Race Synergy**: Cards work best in combos, not isolation
- ⚔️ **Weapon-Focused Combat**: Every attack is a turning point
- 🧱 **Servant Lifecycle**: Units last until enemy draws a weapon
- 📱 **Cross-Platform Controls**: Built for mouse, touch, and gamepad
- 📜 **Strong Lore Backbone**: Each race has rich stories and themes


## 📍 Related Docs

- [Race Lore](Docs/RaceLore.md)
- [Servant Stories](Docs/ServantList.md)
- [Weapon Catalog](Docs/WeaponCatalog.md)
- [Weapon Grouping Table](Docs/WeaponGroupingTable.md)
- [UI Specification](Docs/UiSpec.md)

## ✨ Credits & License

**Design & Lore:** K  
**License:** MIT / Creative Commons / Proprietary (TBD)

> _"Build your empire not with armies, but with belief."_

other related links:
- https://huggingface.co/unity/inference-engine-jets-text-to-speech
- Noted assets are generated by Unity AI Assistant, ChatGPT 
