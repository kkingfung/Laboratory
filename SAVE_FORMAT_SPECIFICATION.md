# Project Chimera Save Format Specification & Migration Guide

**Version:** 1.0.0
**Created:** 2024-11-27
**Purpose:** Complete specification of save data format and migration strategies

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Save File Format](#save-file-format)
3. [Data Structures](#data-structures)
4. [System-Specific Data](#system-specific-data)
5. [Versioning System](#versioning-system)
6. [Migration Strategy](#migration-strategy)
7. [Compression & Optimization](#compression--optimization)
8. [Best Practices](#best-practices)
9. [API Reference](#api-reference)
10. [Examples](#examples)

---

## Overview

### Save System Architecture

```
┌─────────────────────────────────────┐
│  GameStatePersistenceSystem         │
│  (Singleton Manager)                │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│  GameStateData (Root)               │
│  ├── Metadata (version, timestamps) │
│  ├── SystemStates Dictionary        │
│  └── Flags (autoSave, incremental)  │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│  Individual System Data             │
│  ├── Evolution                      │
│  ├── Personality                    │
│  ├── Ecosystem                      │
│  ├── Analytics                      │
│  ├── Quest                          │
│  ├── Breeding                       │
│  ├── Storytelling                   │
│  └── Integration                    │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│  Serialization Layer                │
│  ├── JSON (UnityEngine.JsonUtility) │
│  ├── Optional Compression           │
│  └── File I/O                       │
└─────────────────────────────────────┘
```

### Key Features

- **Versioned Save Data** - Automatic migration between versions
- **Modular System Data** - Each system saves independently
- **Incremental Saves** - Support for save history
- **Auto-Save** - Configurable automatic saving
- **Compression** - Optional data compression (placeholder for now)
- **Export Formats** - JSON, CSV, XML export support
- **Emergency Saves** - Automatic save on application quit

---

## Save File Format

### File Location

```
[PersistentDataPath]/LaboratorySaves/[SaveSlotName].json
```

**Platform-Specific Paths:**
- **Windows:** `C:/Users/[Username]/AppData/LocalLow/[CompanyName]/[ProjectName]/LaboratorySaves/`
- **macOS:** `~/Library/Application Support/[CompanyName]/[ProjectName]/LaboratorySaves/`
- **Linux:** `~/.config/unity3d/[CompanyName]/[ProjectName]/LaboratorySaves/`

### File Naming Convention

| Save Type | Format | Example |
|-----------|--------|---------|
| Manual Save | `[UserChosenName].json` | `MyGame.json` |
| Auto Save | `AutoSave_[DateTime].json` | `AutoSave_20241127_143022.json` |
| Quick Save | `QuickSave.json` | `QuickSave.json` |
| Emergency Save | `EmergencySave.json` | `EmergencySave.json` |

---

## Data Structures

### Root: GameStateData

```csharp
[System.Serializable]
public class GameStateData
{
    public string dataVersion;           // e.g., "1.0.0"
    public string saveSlotName;          // e.g., "MyGame"
    public float creationTime;           // Unity Time.time when save was created
    public float lastSaveTime;           // Unity Time.time of last save
    public float sessionTime;            // Total session time in seconds
    public bool isAutoSave;              // true if auto-saved
    public bool isIncremental;           // true if incremental save
    public bool requiresBackup;          // true if backup required before migration
    public Dictionary<string, SystemSaveData> systemStates;
}
```

**JSON Example:**
```json
{
  "dataVersion": "1.0.0",
  "saveSlotName": "Adventure_001",
  "creationTime": 1234.5,
  "lastSaveTime": 5678.9,
  "sessionTime": 4444.4,
  "isAutoSave": false,
  "isIncremental": false,
  "requiresBackup": false,
  "systemStates": {
    "Evolution": { ... },
    "Personality": { ... },
    "Ecosystem": { ... }
  }
}
```

---

### System Data: SystemSaveData

```csharp
[System.Serializable]
public class SystemSaveData
{
    public string systemName;            // e.g., "Breeding"
    public string dataVersion;           // System-specific version
    public float saveTime;               // When system data was saved
    public Dictionary<string, object> systemData;
}
```

**JSON Example:**
```json
{
  "systemName": "Breeding",
  "dataVersion": "1.0.0",
  "saveTime": 5678.9,
  "systemData": {
    "activeBreedings": 3,
    "completedBreedings": 15,
    "breedingPairs": [
      {
        "parent1Id": "creature_001",
        "parent2Id": "creature_002",
        "offspringId": "creature_003"
      }
    ]
  }
}
```

---

### Save Slot: SaveSlot

```csharp
[System.Serializable]
public class SaveSlot
{
    public string slotName;              // Internal identifier
    public string displayName;           // User-facing name
    public float saveTime;               // Unix timestamp
    public string dataVersion;           // Save data version
    public long fileSizeBytes;           // File size in bytes
    public bool isAutoSave;              // Auto-save flag
    public string screenshotPath;        // Path to save thumbnail
}
```

---

## System-Specific Data

### Creature/Breeding System

**Structure:**
```json
{
  "systemName": "Breeding",
  "dataVersion": "1.0.0",
  "saveTime": 1234.5,
  "systemData": {
    "ownedCreatures": [
      {
        "uniqueId": 1,
        "speciesName": "Dragon",
        "level": 5,
        "experience": 1250,
        "health": 95.5,
        "maxHealth": 100,
        "age": 45,
        "happiness": 0.85,
        "genes": [
          {
            "traitName": "Strength",
            "value": 0.8,
            "dominance": 0.7,
            "isActive": true
          },
          {
            "traitName": "Speed",
            "value": 0.6,
            "dominance": 0.5,
            "isActive": true
          }
        ],
        "parentIds": [0, 0],
        "generation": 1
      }
    ],
    "breedingHistory": [
      {
        "parent1Id": 0,
        "parent2Id": 0,
        "offspringId": 1,
        "timestamp": 1000.0,
        "breedingSuccess": true
      }
    ]
  }
}
```

### Player Progression System

**Structure:**
```json
{
  "systemName": "Progression",
  "dataVersion": "2.0.0",
  "saveTime": 1234.5,
  "systemData": {
    "playerName": "ChimeraMaster",
    "playTime": 15000.0,
    "currency": 5000,
    "skillMastery": {
      "Racing": 0.75,
      "Combat": 0.60,
      "Breeding": 0.85,
      "Puzzle": 0.45
    },
    "partnershipQuality": {
      "creature_001": 0.90,
      "creature_002": 0.75
    },
    "unlockedAchievements": [
      "FIRST_CAPTURE",
      "FIRST_BREEDING",
      "MASTER_BREEDER"
    ],
    "cosmeticUnlocks": [
      "outfit_dragon_01",
      "outfit_phoenix_01"
    ]
  }
}
```

### Ecosystem System

**Structure:**
```json
{
  "systemName": "Ecosystem",
  "dataVersion": "1.0.0",
  "saveTime": 1234.5,
  "systemData": {
    "activeBiomes": [
      {
        "biomeType": "Forest",
        "populationDensity": 0.65,
        "resourceAvailability": 0.80,
        "predatorCount": 12,
        "preyCount": 45
      },
      {
        "biomeType": "Desert",
        "populationDensity": 0.30,
        "resourceAvailability": 0.40,
        "predatorCount": 5,
        "preyCount": 20
      }
    ],
    "weatherState": {
      "currentWeather": "Clear",
      "temperature": 22.5,
      "humidity": 0.60
    }
  }
}
```

---

## Versioning System

### Version Format

**Semantic Versioning:** `MAJOR.MINOR.PATCH`

- **MAJOR:** Breaking changes (incompatible format)
- **MINOR:** New features (backward compatible)
- **PATCH:** Bug fixes, minor changes

### Version History

| Version | Date | Changes | Migration Required |
|---------|------|---------|-------------------|
| **1.0.0** | 2024-11-01 | Initial save format | N/A |
| **2.0.0** | 2024-11-15 | Progression system overhaul (XP → Skills) | ✅ Yes |
| **2.1.0** | 2024-11-20 | Added cosmetic unlocks | ❌ No |
| **2.1.1** | 2024-11-25 | Bug fixes | ❌ No |

### Checking Save Version

```csharp
public bool IsCurrentVersion(string saveDataVersion)
{
    return saveDataVersion == GameStatePersistenceSystem.Instance.CurrentDataVersion;
}

public bool RequiresMigration(string saveDataVersion)
{
    // Simple version comparison
    Version saveVersion = new Version(saveDataVersion);
    Version currentVersion = new Version(currentDataVersion);

    return saveVersion < currentVersion;
}
```

---

## Migration Strategy

### Migration Flow

```
Load Save File
      ↓
Check Version
      ↓
Version Match?
   ↙     ↘
YES      NO
  ↓       ↓
Load   Create Backup (if enabled)
       ↓
   Apply Migrations (chain)
       ↓
   Validate Migrated Data
       ↓
   Update Version Number
       ↓
   Load Migrated Data
```

### Migration Implementation

**Base Migration:**
```csharp
private GameStateData MigrateGameStateData(GameStateData oldData)
{
    Version oldVersion = new Version(oldData.dataVersion);
    Version currentVersion = new Version(currentDataVersion);

    if (oldVersion < currentVersion)
    {
        Debug.Log($"Migrating save from {oldData.dataVersion} to {currentDataVersion}");

        // Create backup if enabled
        if (backupBeforeMigration)
        {
            CreateBackup(oldData);
        }

        // Apply migration chain
        GameStateData migratedData = oldData;

        if (oldVersion < new Version("2.0.0"))
        {
            migratedData = MigrateTo_2_0_0(migratedData);
        }

        if (oldVersion < new Version("2.1.0"))
        {
            migratedData = MigrateTo_2_1_0(migratedData);
        }

        // Update version
        migratedData.dataVersion = currentDataVersion;

        return migratedData;
    }

    return oldData;
}
```

### Example Migration: 1.0.0 → 2.0.0 (XP to Skills)

**Scenario:** Progression system changed from XP/Levels to skill-based mastery.

**Before (1.0.0):**
```json
{
  "systemName": "Progression",
  "dataVersion": "1.0.0",
  "systemData": {
    "playerLevel": 25,
    "experience": 125000,
    "skillPoints": 50
  }
}
```

**After (2.0.0):**
```json
{
  "systemName": "Progression",
  "dataVersion": "2.0.0",
  "systemData": {
    "skillMastery": {
      "Racing": 0.60,
      "Combat": 0.55,
      "Breeding": 0.70,
      "Puzzle": 0.40
    },
    "partnershipQuality": {
      "creature_001": 0.75
    }
  }
}
```

**Migration Code:**
```csharp
private GameStateData MigrateTo_2_0_0(GameStateData oldData)
{
    Debug.Log("Applying migration 1.0.0 → 2.0.0 (XP to Skills)");

    if (oldData.systemStates.ContainsKey("Progression"))
    {
        var progressionData = oldData.systemStates["Progression"];
        var oldSystemData = progressionData.systemData;

        // Convert XP/Level to skill mastery
        int playerLevel = Convert.ToInt32(oldSystemData["playerLevel"]);
        float experience = Convert.ToSingle(oldSystemData["experience"]);

        // Calculate estimated skill mastery based on level
        // Formula: Each level = ~0.04 mastery across 4 main skills
        float totalMastery = playerLevel * 0.04f;

        var newSystemData = new Dictionary<string, object>
        {
            ["skillMastery"] = new Dictionary<string, float>
            {
                ["Racing"] = Mathf.Min(totalMastery * 0.25f, 1.0f),
                ["Combat"] = Mathf.Min(totalMastery * 0.25f, 1.0f),
                ["Breeding"] = Mathf.Min(totalMastery * 0.25f, 1.0f),
                ["Puzzle"] = Mathf.Min(totalMastery * 0.25f, 1.0f)
            },
            ["partnershipQuality"] = new Dictionary<string, float>(),
            ["playerName"] = oldSystemData.ContainsKey("playerName") ? oldSystemData["playerName"] : "Player",
            ["playTime"] = oldSystemData.ContainsKey("playTime") ? oldSystemData["playTime"] : 0f,
            ["currency"] = oldSystemData.ContainsKey("currency") ? oldSystemData["currency"] : 0
        };

        // Preserve achievements if they exist
        if (oldSystemData.ContainsKey("unlockedAchievements"))
        {
            newSystemData["unlockedAchievements"] = oldSystemData["unlockedAchievements"];
        }

        // Award compensation currency for lost XP/levels
        int compensationCurrency = playerLevel * 100;
        newSystemData["currency"] = Convert.ToInt32(newSystemData["currency"]) + compensationCurrency;

        progressionData.systemData = newSystemData;
        progressionData.dataVersion = "2.0.0";

        Debug.Log($"Migrated player level {playerLevel} → skill mastery (avg: {totalMastery/4:P1})");
        Debug.Log($"Awarded {compensationCurrency} compensation currency");
    }

    return oldData;
}
```

---

### Example Migration: 2.0.0 → 2.1.0 (Add Cosmetics)

**Scenario:** New cosmetic unlock system added.

**Migration Code:**
```csharp
private GameStateData MigrateTo_2_1_0(GameStateData oldData)
{
    Debug.Log("Applying migration 2.0.0 → 2.1.0 (Add Cosmetics)");

    if (oldData.systemStates.ContainsKey("Progression"))
    {
        var progressionData = oldData.systemStates["Progression"];

        // Add cosmetic unlocks if not present
        if (!progressionData.systemData.ContainsKey("cosmeticUnlocks"))
        {
            progressionData.systemData["cosmeticUnlocks"] = new List<string>();
            Debug.Log("Added cosmetic unlocks system");
        }

        progressionData.dataVersion = "2.1.0";
    }

    return oldData;
}
```

---

### Backup System

**Creating Backups:**
```csharp
private void CreateBackup(GameStateData data)
{
    string backupPath = GetBackupFilePath(data.saveSlotName);
    string serialized = JsonUtility.ToJson(data, true);

    try
    {
        File.WriteAllText(backupPath, serialized);
        Debug.Log($"Backup created: {backupPath}");
    }
    catch (Exception ex)
    {
        Debug.LogError($"Failed to create backup: {ex.Message}");
    }
}

private string GetBackupFilePath(string saveSlotName)
{
    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    string backupName = $"{saveSlotName}_backup_{timestamp}.json";
    string backupPath = Path.Combine(Application.persistentDataPath, "LaboratorySaves", "Backups");

    if (!Directory.Exists(backupPath))
    {
        Directory.CreateDirectory(backupPath);
    }

    return Path.Combine(backupPath, backupName);
}
```

---

## Compression & Optimization

### Current Implementation

**Note:** Compression is currently a placeholder. The following describes the intended implementation.

### Planned Compression Strategy

**Algorithm:** GZip compression for JSON data

**Implementation:**
```csharp
using System.IO;
using System.IO.Compression;
using System.Text;

private string CompressString(string data)
{
    byte[] bytes = Encoding.UTF8.GetBytes(data);

    using (var memoryStream = new MemoryStream())
    {
        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
        {
            gzipStream.Write(bytes, 0, bytes.Length);
        }

        byte[] compressed = memoryStream.ToArray();
        return Convert.ToBase64String(compressed);
    }
}

private string DecompressString(string compressedData)
{
    byte[] compressed = Convert.FromBase64String(compressedData);

    using (var memoryStream = new MemoryStream(compressed))
    {
        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
        {
            using (var reader = new StreamReader(gzipStream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
```

### Compression Benefits

| Save Size | Uncompressed | Compressed | Savings |
|-----------|-------------|------------|---------|
| Small (5KB) | 5KB | 2KB | ~60% |
| Medium (50KB) | 50KB | 15KB | ~70% |
| Large (500KB) | 500KB | 100KB | ~80% |

---

## Best Practices

### 1. Always Use Versioning

```csharp
// ✅ Good: Include version in save data
var saveData = new GameStateData
{
    dataVersion = "2.1.0",
    // ...
};

// ❌ Bad: No version tracking
var saveData = new GameStateData
{
    // Missing dataVersion
};
```

### 2. Validate Before Saving

```csharp
// ✅ Good: Validate data before serialization
public void SaveGameState(string slotName)
{
    var data = CollectAllGameStateData();

    if (!ValidateSaveData(data))
    {
        Debug.LogError("Save data validation failed!");
        return;
    }

    SerializeAndSave(data, slotName);
}

private bool ValidateSaveData(GameStateData data)
{
    if (string.IsNullOrEmpty(data.dataVersion))
        return false;

    if (data.systemStates == null || data.systemStates.Count == 0)
        return false;

    return true;
}
```

### 3. Handle Migration Failures Gracefully

```csharp
// ✅ Good: Try migration with fallback
try
{
    var migratedData = MigrateGameStateData(oldData);
    return migratedData;
}
catch (Exception ex)
{
    Debug.LogError($"Migration failed: {ex.Message}");

    // Restore from backup
    if (backupExists)
    {
        return LoadBackup();
    }

    // Last resort: Fresh start
    return CreateNewGameState();
}
```

### 4. Use Async Saving

```csharp
// ✅ Good: Async saves don't block gameplay
StartCoroutine(SaveGameStateCoroutine(slotName));

// ❌ Bad: Synchronous save freezes game
SaveGameStateImmediate(slotName); // Blocks main thread
```

### 5. Implement Auto-Save Wisely

```csharp
// ✅ Good: Auto-save at safe points
public void OnSafePoint()
{
    if (Time.time - lastAutoSave >= autoSaveInterval)
    {
        AutoSave();
    }
}

// ❌ Bad: Auto-save during gameplay
void Update()
{
    AutoSave(); // Terrible performance!
}
```

---

## API Reference

### GameStatePersistenceSystem

```csharp
public class GameStatePersistenceSystem
{
    // Properties
    public static GameStatePersistenceSystem Instance { get; }
    public bool IsSaveInProgress { get; }
    public bool IsLoadInProgress { get; }
    public List<SaveSlot> AvailableSaves { get; }

    // Save/Load Methods
    public void SaveGameState(string saveSlotName, bool isAutoSave = false);
    public void LoadGameState(string saveSlotName);
    public void AutoSave();
    public void QuickSave();
    public void QuickLoad();

    // Export Methods
    public void ExportGameStateData(string exportPath, ExportFormat format = ExportFormat.JSON);

    // Statistics
    public SaveDataStatistics GetSaveDataStatistics();

    // Events
    public event Action<string> OnSaveStarted;
    public event Action<string, bool> OnSaveCompleted;
    public event Action<string> OnLoadStarted;
    public event Action<string, bool> OnLoadCompleted;
    public event Action<float> OnSaveProgress;
    public event Action<float> OnLoadProgress;
}
```

---

## Examples

### Example 1: Manual Save with UI

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Laboratory.Core.Persistence;

public class SaveLoadUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField saveNameInput;
    [SerializeField] private Button saveButton;
    [SerializeField] private Slider saveProgressBar;
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {
        saveButton.onClick.AddListener(OnSaveClicked);

        // Subscribe to save events
        var persistence = GameStatePersistenceSystem.Instance;
        persistence.OnSaveStarted += OnSaveStarted;
        persistence.OnSaveProgress += OnSaveProgress;
        persistence.OnSaveCompleted += OnSaveCompleted;
    }

    private void OnSaveClicked()
    {
        string saveName = saveNameInput.text;

        if (string.IsNullOrWhiteSpace(saveName))
        {
            statusText.text = "Please enter a save name";
            return;
        }

        GameStatePersistenceSystem.Instance.SaveGameState(saveName);
    }

    private void OnSaveStarted(string slotName)
    {
        statusText.text = $"Saving: {slotName}...";
        saveProgressBar.value = 0f;
        saveButton.interactable = false;
    }

    private void OnSaveProgress(float progress)
    {
        saveProgressBar.value = progress;
    }

    private void OnSaveCompleted(string slotName, bool success)
    {
        if (success)
        {
            statusText.text = $"Saved successfully: {slotName}";
        }
        else
        {
            statusText.text = $"Save failed: {slotName}";
        }

        saveButton.interactable = true;
    }
}
```

### Example 2: Load Save Slot Picker

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Laboratory.Core.Persistence;

public class LoadGameUI : MonoBehaviour
{
    [SerializeField] private Transform saveSlotContainer;
    [SerializeField] private GameObject saveSlotPrefab;

    private void Start()
    {
        RefreshSaveSlots();
    }

    private void RefreshSaveSlots()
    {
        // Clear existing slots
        foreach (Transform child in saveSlotContainer)
        {
            Destroy(child.gameObject);
        }

        // Get available saves
        var saves = GameStatePersistenceSystem.Instance.AvailableSaves;

        // Create UI element for each save
        foreach (var save in saves)
        {
            GameObject slotObj = Instantiate(saveSlotPrefab, saveSlotContainer);
            var slotUI = slotObj.GetComponent<SaveSlotUI>();

            slotUI.SetSaveData(save);
            slotUI.OnLoadClicked += () => LoadSave(save.slotName);
        }
    }

    private void LoadSave(string slotName)
    {
        GameStatePersistenceSystem.Instance.LoadGameState(slotName);
    }
}
```

### Example 3: Custom System Data Serialization

```csharp
using System.Collections.Generic;

public class MyCustomSystem : MonoBehaviour
{
    private List<CustomData> customDataList = new List<CustomData>();

    // Called by GameStatePersistenceSystem when saving
    public SystemSaveData CollectSaveData()
    {
        var saveData = new SystemSaveData
        {
            systemName = "MyCustomSystem",
            dataVersion = "1.0.0",
            saveTime = Time.time,
            systemData = new Dictionary<string, object>()
        };

        // Serialize custom data
        saveData.systemData["customDataCount"] = customDataList.Count;
        saveData.systemData["customDataItems"] = SerializeCustomData();

        return saveData;
    }

    // Called by GameStatePersistenceSystem when loading
    public void RestoreSaveData(SystemSaveData saveData)
    {
        if (saveData.systemData.ContainsKey("customDataItems"))
        {
            DeserializeCustomData(saveData.systemData["customDataItems"]);
        }

        Debug.Log($"Restored {customDataList.Count} custom data items");
    }

    private object SerializeCustomData()
    {
        // Convert to serializable format
        var serializable = new List<Dictionary<string, object>>();

        foreach (var data in customDataList)
        {
            serializable.Add(new Dictionary<string, object>
            {
                ["id"] = data.Id,
                ["value"] = data.Value
            });
        }

        return serializable;
    }

    private void DeserializeCustomData(object data)
    {
        // Restore from serialized format
        customDataList.Clear();
        // Implementation...
    }
}
```

---

## Troubleshooting

### "Save file corrupted"

**Cause:** JSON deserialization failed
**Solution:**
1. Check backup files in `LaboratorySaves/Backups/`
2. Restore from most recent backup
3. Enable `backupBeforeMigration` for future saves

### "Version migration failed"

**Cause:** Migration code threw exception
**Solution:**
1. Check migration logic for errors
2. Validate old save data format
3. Ensure all required fields exist

### "Save too large"

**Cause:** Save file exceeds reasonable size
**Solution:**
1. Enable compression: `compressSaveData = true`
2. Implement data pruning (remove old auto-saves)
3. Consider binary serialization for very large saves

---

**Last Updated:** 2024-11-27
**Maintained By:** Project Chimera Persistence Team
**Related Docs:** ARCHITECTURE.md, DEVELOPER_GUIDE.md, MULTIPLAYER_WORKFLOW_GUIDE.md
