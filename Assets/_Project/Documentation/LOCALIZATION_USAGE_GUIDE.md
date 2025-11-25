# Localization System - Usage Guide

**Version:** 1.0
**Last Updated:** 2024-11-25
**System:** Laboratory.Chimera.Localization

---

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [For Designers](#for-designers)
4. [For Programmers](#for-programmers)
5. [Translation Workflow](#translation-workflow)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)

---

## Overview

The Chimera Localization System provides:
- **5 Languages**: English, Spanish, French, German, Japanese
- **147 Translation Keys**: Covering UI, gameplay, species, biomes, equipment
- **Runtime Switching**: Change language without restarting
- **Designer-Friendly**: No code required for most tasks
- **Format Strings**: Dynamic text with variable substitution

### Architecture

```
LocalizationManager (ScriptableObject Singleton)
├── LocalizationDatabase[] - One per language
│   ├── English (147 keys)
│   ├── Spanish (147 keys)
│   ├── French (147 keys)
│   ├── German (147 keys)
│   └── Japanese (147 keys)
└── LocalizedText (MonoBehaviour Component)
    └── Auto-updates UI when language changes
```

---

## Quick Start

### 1. Add Localized Text to UI

**In Unity Editor:**

1. Select your UI Text/TextMeshProUGUI component
2. Add Component → `LocalizedText`
3. Set **Localization Key** (e.g., `game_title`)
4. Done! Text will auto-update when language changes

**Example Keys:**
```
game_title              → "Project Chimera"
ui_continue             → "Continue"
ui_new_game             → "New Game"
chimera_species_ignis   → "Ignis (Fire Dragon)"
tutorial_capture_01     → "Press E to interact with wild Chimeras"
```

### 2. Change Language at Runtime

**Via Code:**
```csharp
using Laboratory.Chimera.Localization;

LocalizationManager.Instance.SetLanguage(SystemLanguage.Spanish);
```

**Via Settings Menu:**
```csharp
// In your settings UI script
void OnLanguageDropdownChanged(int index)
{
    SystemLanguage[] languages = {
        SystemLanguage.English,
        SystemLanguage.Spanish,
        SystemLanguage.French,
        SystemLanguage.German,
        SystemLanguage.Japanese
    };

    LocalizationManager.Instance.SetLanguage(languages[index]);
}
```

### 3. Get Text in Code

**Simple Text:**
```csharp
string text = LocalizationManager.Instance.GetText("game_title");
// Returns: "Project Chimera" (or translated version)
```

**Format Strings:**
```csharp
string text = LocalizationManager.Instance.GetText("ui_level_value", "42");
// Returns: "Level: 42" (formatted with provided value)
```

---

## For Designers

### Adding New Translation Keys

**Method 1: Unity Inspector (Recommended)**

1. Navigate to `Resources/Localization/`
2. Open any `LocalizationDatabase_XX` asset
3. Expand **Translations** array
4. Click `+` to add new entry
5. Set:
   - **Key**: `your_new_key` (lowercase, underscores only)
   - **Value**: The translated text
6. Repeat for all 5 language databases
7. **IMPORTANT**: All databases must have the same keys

**Method 2: Import from JSON**

1. Navigate to `Resources/Localization/Translations/`
2. Edit `SampleTranslations_EN.json` (or any language)
3. Add new key-value pair:
   ```json
   {
       "your_new_key": "Your translated text here"
   }
   ```
4. Repeat for all language files
5. In Unity: Select database → Inspector → `Import from JSON`

### Organizing Translation Keys

**Naming Convention:**
```
category_subcategory_identifier

Examples:
ui_button_confirm           → UI buttons
ui_menu_settings           → UI menus
chimera_species_aquarius   → Chimera species
biome_volcanic_name        → Biome names
tutorial_combat_01         → Tutorial messages
equipment_weapon_sword     → Equipment names
```

**Current Categories:**
- `game_*` - Core game text (title, tagline)
- `ui_*` - User interface elements
- `chimera_*` - Chimera-related content
- `biome_*` - Biome and environment content
- `activity_*` - Gameplay activities
- `equipment_*` - Items and gear
- `tutorial_*` - Tutorial and help text
- `status_*` - Status messages
- `currency_*` - Economy and resources
- `reward_*` - Rewards and achievements
- `settings_*` - Settings menu
- `hud_*` - Heads-up display
- `social_*` - Social features
- `achievement_*` - Achievements

### Testing Translations

**In Unity Editor:**

1. Menu: `Chimera/Tests/Validate Localization System`
   - Runs 8 validation tests
   - Checks for missing keys, format errors
   - Verifies all languages have same key count

2. Menu: `Chimera/Tests/Export Localization Report`
   - Generates report of all languages
   - Shows key counts per language
   - Useful for tracking translation progress

### Common Designer Tasks

**1. Add New Species Translation**
```
Key: chimera_species_newdragon
EN:  "Draconis (Lightning Dragon)"
ES:  "Draconis (Dragón Relámpago)"
FR:  "Draconis (Dragon Foudre)"
DE:  "Draconis (Blitzdrache)"
JA:  "ドラコニス（雷竜）"
```

**2. Add Tutorial Message with Variables**
```
Key: tutorial_breeding_success
EN:  "Congratulations! You bred a new {0} with {1} genes!"
ES:  "¡Felicidades! ¡Criaste un nuevo {0} con {1} genes!"
FR:  "Félicitations ! Vous avez élevé un nouveau {0} avec {1} gènes !"
```

**3. Add UI Button Text**
```
Key: ui_button_breed
EN:  "Breed"
ES:  "Criar"
FR:  "Élever"
DE:  "Züchten"
JA:  "繁殖"
```

---

## For Programmers

### LocalizationManager API

```csharp
using Laboratory.Chimera.Localization;

// Singleton access
var manager = LocalizationManager.Instance;

// Get current language
SystemLanguage current = manager.CurrentLanguage;

// Change language
bool success = manager.SetLanguage(SystemLanguage.French);

// Get text
string text = manager.GetText("key");

// Get text with format args
string formatted = manager.GetText("key", "arg1", "arg2");

// Subscribe to language changes
manager.OnLanguageChanged += OnLanguageChanged;

void OnLanguageChanged(SystemLanguage newLanguage)
{
    Debug.Log($"Language changed to: {newLanguage}");
}
```

### LocalizedText Component

**Auto-Updating UI Text:**
```csharp
[ExecuteAlways]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;
    [SerializeField] private string[] formatArgs;

    // Automatically updates when:
    // - Component enabled
    // - Language changed
    // - Value changed in inspector (Edit Mode)
}
```

**Usage in Custom Scripts:**
```csharp
using Laboratory.Chimera.Localization;
using UnityEngine.UI;

public class CustomUI : MonoBehaviour
{
    [SerializeField] private Text titleText;

    void Start()
    {
        // Manual text update
        UpdateTitle();

        // Subscribe to language changes
        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    void UpdateTitle()
    {
        titleText.text = LocalizationManager.Instance.GetText("game_title");
    }

    void OnLanguageChanged(SystemLanguage newLang)
    {
        UpdateTitle();
    }

    void OnDestroy()
    {
        // Always unsubscribe!
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }
}
```

### LocalizationDatabase API

```csharp
using Laboratory.Chimera.Localization;

var database = Resources.Load<LocalizationDatabase>("Localization/LocalizationDatabase_EN");

// Get text by key
string text = database.GetText("key");

// Get text with format args
string formatted = database.GetText("key", "arg1", "arg2");

// Get all keys
string[] allKeys = database.GetAllKeys();

// Check if key exists
bool exists = database.ContainsKey("key");

// Context menu utilities (Editor only):
// - Sort Translations A-Z
// - Find Duplicate Keys
// - Find Empty Values
// - Import from JSON
```

### Adding New Languages

**1. Create New Database Asset:**
```csharp
// Right-click in Project: Create → Chimera → Localization → Localization Database
// Set language to new SystemLanguage enum value
```

**2. Add to LocalizationManager:**
```csharp
// Select LocalizationManager asset
// Inspector → Add database to languageDatabases array
```

**3. Create Translation JSON:**
```json
// Resources/Localization/Translations/SampleTranslations_XX.json
{
    "game_title": "Your Translation",
    "ui_continue": "Your Translation",
    // ... all 147 keys
}
```

**4. Import JSON:**
```
// Select database → Inspector → Import from JSON → Select file
```

### Format String Examples

**Single Parameter:**
```csharp
// Key: "ui_level_value"
// Value: "Level: {0}"
string text = manager.GetText("ui_level_value", "42");
// Result: "Level: 42"
```

**Multiple Parameters:**
```csharp
// Key: "ui_battle_result"
// Value: "{0} defeated {1} in {2} seconds!"
string text = manager.GetText("ui_battle_result", "Player", "Dragon", "12.5");
// Result: "Player defeated Dragon in 12.5 seconds!"
```

**Number Formatting:**
```csharp
// Key: "ui_currency_amount"
// Value: "Gold: {0:N0}"
string text = manager.GetText("ui_currency_amount", 1234567);
// Result: "Gold: 1,234,567"
```

---

## Translation Workflow

### For Translators

**1. Export Keys for Translation:**
```
Menu: Chimera/Tests/Export Localization Report
Copy English keys from Console log
Send to translator
```

**2. Receive Translations:**
```json
{
    "game_title": "Translated Title",
    "ui_continue": "Translated Continue",
    ...
}
```

**3. Import to Database:**
```
1. Save JSON to Resources/Localization/Translations/
2. Select LocalizationDatabase_XX asset
3. Inspector → Import from JSON
4. Select your JSON file
5. Translations imported!
```

**4. Validate:**
```
Menu: Chimera/Tests/Validate Localization System
Checks for:
- Missing keys
- Key count mismatches
- Empty values
- Format string errors
```

### Quality Assurance Checklist

- [ ] All 5 languages have exactly 147 keys
- [ ] No empty translations
- [ ] No duplicate keys
- [ ] Format strings match across languages (`{0}`, `{1}` positions)
- [ ] UI fits in all languages (some languages are longer)
- [ ] Special characters display correctly (Japanese, German umlauts)
- [ ] Context is clear (avoid ambiguous single words)

---

## Best Practices

### 1. Key Naming

**Good:**
```
ui_button_confirm
chimera_species_ignis_description
tutorial_breeding_step_01
```

**Bad:**
```
Confirm
ignisDescription
tut1
```

### 2. Context in Values

**Good:**
```
"ui_level_label": "Player Level"  (clear context)
"ui_health_label": "Health Points"
```

**Bad:**
```
"ui_level": "Level"  (which level? UI? Game? Audio?)
"ui_health": "Health"  (health what? Bar? Status? Potion?)
```

### 3. Consistent Formatting

**Good:**
```
"ui_button_save": "Save"
"ui_button_load": "Load"
"ui_button_quit": "Quit"
```

**Bad:**
```
"ui_button_save": "save"  (lowercase inconsistent)
"ui_load_button": "Load"  (key structure different)
"quit": "QUIT"  (ALL CAPS inconsistent)
```

### 4. Format String Safety

**Good:**
```
// Code: GetText("ui_score", score.ToString());
// Key: "ui_score": "Score: {0}"
```

**Bad:**
```
// Code: GetText("ui_score", score);  // Wrong! Pass string, not int
// Key: "ui_score": "Score: {0:N0}"   // Format in code, not here
```

### 5. Performance

**Good:**
```csharp
void Start()
{
    // Cache frequently used strings
    _cachedTitle = LocalizationManager.Instance.GetText("game_title");
}
```

**Bad:**
```csharp
void Update()
{
    // DON'T fetch text every frame!
    titleText.text = LocalizationManager.Instance.GetText("game_title");
}
```

**Use LocalizedText component instead** - it handles caching and updates automatically.

---

## Troubleshooting

### Problem: "Key not found" error

**Cause:** Key doesn't exist in current language database

**Solution:**
1. Check key spelling (case-sensitive)
2. Verify key exists in database: `Resources/Localization/LocalizationDatabase_XX`
3. Run validation: `Chimera/Tests/Validate Localization System`

### Problem: Text shows as `[KEY_NAME]`

**Cause:** Fallback behavior when key is missing

**Solution:**
1. Add key to current language database
2. Ensure all databases have the same keys
3. Reimport JSON if using JSON workflow

### Problem: Format string shows `{0}` literally

**Cause:** Not passing format arguments to `GetText()`

**Solution:**
```csharp
// Wrong:
string text = manager.GetText("ui_level_value");
// Shows: "Level: {0}"

// Correct:
string text = manager.GetText("ui_level_value", playerLevel.ToString());
// Shows: "Level: 42"
```

### Problem: Language doesn't change

**Cause 1:** Database not added to LocalizationManager
**Solution:** Add database to `languageDatabases[]` array

**Cause 2:** `SetLanguage()` returns false
**Solution:** Database for that language doesn't exist

**Cause 3:** UI not updating
**Solution:** Use `LocalizedText` component or subscribe to `OnLanguageChanged`

### Problem: Missing translations after adding new key

**Cause:** Key added to only one language database

**Solution:**
1. Add key to ALL 5 language databases
2. Use validation test to find missing keys
3. Consider using JSON import for consistency

### Problem: Japanese/German special characters broken

**Cause:** Font doesn't include Unicode characters

**Solution:**
1. Use font with full Unicode support (Noto Sans, Arial Unicode)
2. Import font with Character set: Unicode
3. Test all special characters

---

## Reference: All 147 Translation Keys

### Core Game (4 keys)
```
game_title, game_tagline, game_version, game_loading
```

### UI - Buttons (8 keys)
```
ui_continue, ui_new_game, ui_load_game, ui_settings,
ui_quit, ui_back, ui_confirm, ui_cancel
```

### UI - Menus (8 keys)
```
ui_menu_main, ui_menu_pause, ui_menu_inventory,
ui_menu_team, ui_menu_map, ui_menu_breeding,
ui_menu_research, ui_menu_social
```

### UI - Labels (10 keys)
```
ui_level_label, ui_health_label, ui_stamina_label,
ui_experience_label, ui_gold_label, ui_research_points_label,
ui_breeding_slots_label, ui_team_slots_label,
ui_inventory_weight_label, ui_time_label
```

### Chimera Species (8 keys)
```
chimera_species_ignis, chimera_species_aquarius,
chimera_species_terradon, chimera_species_zephyros,
chimera_species_luxara, chimera_species_umbraxis,
chimera_species_voltis, chimera_species_glacius
```

### Chimera Personalities (8 keys)
```
chimera_personality_brave, chimera_personality_cautious,
chimera_personality_curious, chimera_personality_loyal,
chimera_personality_aggressive, chimera_personality_playful,
chimera_personality_wise, chimera_personality_stubborn
```

### Biomes (8 keys)
```
biome_volcanic_name, biome_ocean_name, biome_forest_name,
biome_mountain_name, biome_desert_name, biome_tundra_name,
biome_swamp_name, biome_crystal_caverns_name
```

### Activities (14 keys)
```
activity_explore, activity_battle, activity_breed,
activity_train, activity_research, activity_trade,
activity_hunt, activity_gather, activity_fish,
activity_mine, activity_craft, activity_build,
activity_socialize, activity_rest
```

### Equipment (10 keys)
```
equipment_weapon_basic, equipment_armor_basic,
equipment_accessory_basic, equipment_consumable_potion,
equipment_consumable_food, equipment_tool_fishing_rod,
equipment_tool_pickaxe, equipment_tool_net,
equipment_saddle_basic, equipment_storage_bag
```

### Tutorial (8 keys)
```
tutorial_welcome, tutorial_movement, tutorial_interaction,
tutorial_combat, tutorial_breeding, tutorial_capture,
tutorial_inventory, tutorial_map
```

### Status Messages (4 keys)
```
status_victory, status_defeat, status_level_up,
status_achievement_unlocked
```

### Currency (3 keys)
```
currency_gold, currency_gems, currency_research_points
```

### Rewards (2 keys)
```
reward_common, reward_rare
```

### Settings (5 keys)
```
settings_audio, settings_graphics, settings_controls,
settings_gameplay, settings_language
```

### HUD (3 keys)
```
hud_minimap, hud_quest_tracker, hud_team_status
```

### Social (4 keys)
```
social_friends, social_guild, social_trade, social_chat
```

### Achievements (2 keys)
```
achievement_first_capture, achievement_master_breeder
```

### Format String Keys (30 keys)
```
ui_level_value, ui_health_value, ui_stamina_value, ...
(All keys ending in _value, _count, _amount, _progress)
```

---

## Support

**Documentation:** `/Assets/_Project/Documentation/LOCALIZATION_USAGE_GUIDE.md`
**Tests:** `Chimera/Tests/Validate Localization System`
**Scripts:** `/Assets/_Project/Scripts/Chimera/Localization/`

**Common Issues:** See [Troubleshooting](#troubleshooting) section above

**For Developers:** See `UI_ANIMATION_PRESETS_GUIDE.md` for UI integration patterns

---

**Happy Localizing! 🌍**
