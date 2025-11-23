# Addressables Migration Guide

**Project Chimera - Reducing Build Size with Bundle Downloads**

This guide helps you migrate large assets from Resources folders to Unity Addressables, enabling on-demand downloads and reducing initial build size.

---

## 📊 Problem & Solution

### The Problem
- Large prefabs in Resources folders increase build size
- Players must download everything even if they don't use it
- Content updates require full app rebuild

### The Solution
- Unity Addressables allows on-demand asset downloads
- Smaller initial build (~50-100 MB instead of 500+ MB)
- Update content without rebuilding the app
- Platform-specific bundles (Windows/Mac/iOS/Android)

---

## 🎯 Quick Start (3 Steps)

### Step 1: Mark Assets as Addressable (5 minutes)

1. Open Unity Editor
2. Go to **Tools → Chimera → Addressables Migration Tool**
3. Select asset category (e.g., "Prefabs")
4. Click **"Scan Resources Folders"**
5. Review found assets
6. Click **"Mark as Addressable (Remote)"** for downloadable content
   - Or **"Mark as Addressable (Local)"** for bundled-but-not-downloaded content

**Result:** Assets are now managed by Addressables system

### Step 2: Migrate Code (10-30 minutes)

1. Go to **Tools → Chimera → Resources Code Migration Tool**
2. Click **"Scan C# Scripts for Resources.Load"**
3. Review found usages
4. Click **"Export Migration Report"** for detailed instructions
5. Update code following the suggestions

**Quick Pattern:**
```csharp
// Before
var prefab = Resources.Load<GameObject>("Prefabs/Creature");

// After
var prefab = await _assetService.LoadAssetAsync<GameObject>("Prefabs/Creature");
```

### Step 3: Build & Deploy (5 minutes)

1. Open **Window → Asset Management → Addressables → Groups**
2. Click **Build → New Build → Default Build Script**
3. Bundles are created in `ServerData/[Platform]/`
4. Test locally first, then upload to CDN when ready

---

## 🛠️ Detailed Migration Steps

### Phase 1: Asset Migration

#### 1.1 Scan Your Resources Folders

**Using the Tool:**
```
Tools → Chimera → Addressables Migration Tool
```

**What it does:**
- Finds all assets in Resources folders
- Calculates total size
- Shows what will be migrated

**Asset Categories:**
- **Prefabs** - Creature prefabs, environment props, UI prefabs
- **ScriptableObjects** - Configs, data assets
- **Materials** - Shaders and materials
- **Textures** - Large textures (2K, 4K)
- **AudioClips** - Music and sound effects

#### 1.2 Choose Local vs Remote

**Local (Bundled but not in Resources):**
- Assets packaged with app
- No separate download needed
- Still reduces Resources overhead
- **Use for:** Core UI, essential creatures, startup assets

**Remote (Downloadable):**
- Assets downloaded from server/CDN
- Not included in initial app download
- Can be updated without app rebuild
- **Use for:** Premium creatures, rare items, extra content

#### 1.3 Execute Migration

Click **"Mark as Addressable"** button in the tool.

**What happens:**
1. Creates Addressable groups (e.g., "Remote Prefabs")
2. Moves assets to those groups
3. Assigns addresses based on Resources path
4. Configures build/load paths

**Example:**
```
Resources/Prefabs/Creatures/Dragon.prefab
→ Addressable address: "Prefabs/Creatures/Dragon"
→ Group: "Remote Prefabs"
```

---

### Phase 2: Code Migration

#### 2.1 Scan Your Code

**Using the Tool:**
```
Tools → Chimera → Resources Code Migration Tool
```

**What it finds:**
- `Resources.Load<T>()` calls
- `Resources.LoadAll<T>()` calls
- `Resources.LoadAsync<T>()` calls

**Example Output:**
```
Found 45 Resources.Load usages in 23 files
- ChimeraManager.cs: 3 usages
- InventorySystem.cs: 5 usages
- CombatSystem.cs: 2 usages
...
```

#### 2.2 Migration Patterns

**Pattern 1: Simple Load**
```csharp
// BEFORE
var prefab = Resources.Load<GameObject>("Prefabs/Creature");
if (prefab != null)
{
    Instantiate(prefab);
}

// AFTER
var prefab = await _assetService.LoadAssetAsync<GameObject>("Prefabs/Creature");
if (prefab != null)
{
    Instantiate(prefab);
}
```

**Pattern 2: Async Load**
```csharp
// BEFORE
IEnumerator LoadAsset()
{
    var request = Resources.LoadAsync<AudioClip>("Audio/Music");
    yield return request;
    var clip = request.asset as AudioClip;
    audioSource.clip = clip;
}

// AFTER
async UniTask LoadAsset()
{
    var clip = await _assetService.LoadAssetAsync<AudioClip>("Audio/Music");
    audioSource.clip = clip;
}
```

**Pattern 3: LoadAll (Manual Migration Required)**
```csharp
// BEFORE
var items = Resources.LoadAll<ItemData>("Items");
foreach (var item in items)
{
    ProcessItem(item);
}

// AFTER - Option A: Use Addressables labels
// 1. In Addressables Groups window, add label "Items" to all item assets
var items = await Addressables.LoadAssetsAsync<ItemData>("Items", null);
foreach (var item in items)
{
    ProcessItem(item);
}

// AFTER - Option B: Load explicit list
var keys = new[] { "Items/Sword", "Items/Shield", "Items/Potion" };
await _assetService.LoadAssetsAsync(keys);
var sword = _assetService.GetCachedAsset<ItemData>("Items/Sword");
```

#### 2.3 Add Dependencies

**Add using statements:**
```csharp
using Laboratory.Core.Services;
using Cysharp.Threading.Tasks;
```

**Inject IAssetService:**
```csharp
public class YourComponent : MonoBehaviour
{
    [Inject] private IAssetService _assetService; // Using VContainer

    // Or get from service locator:
    private void Awake()
    {
        _assetService = ServiceLocator.Get<IAssetService>();
    }
}
```

#### 2.4 Convert to Async

**MonoBehaviour methods:**
```csharp
// Before (synchronous)
void Start()
{
    var asset = Resources.Load<GameObject>("MyAsset");
}

// After (async)
async void Start()
{
    var asset = await _assetService.LoadAssetAsync<GameObject>("MyAsset");
}

// Or with UniTask for better control
async UniTaskVoid Start()
{
    try
    {
        var asset = await _assetService.LoadAssetAsync<GameObject>("MyAsset");
    }
    catch (Exception ex)
    {
        Debug.LogError($"Failed to load asset: {ex}");
    }
}
```

**Custom methods:**
```csharp
// Return UniTask for awaitable methods
async UniTask<GameObject> LoadPrefab(string key)
{
    return await _assetService.LoadAssetAsync<GameObject>(key);
}

// Usage
var prefab = await LoadPrefab("Prefabs/Creature");
```

---

### Phase 3: Testing

#### 3.1 Local Testing

**Play Mode Testing:**
1. Window → Asset Management → Addressables → Groups
2. Set Play Mode Script: **Use Asset Database (fastest)**
3. Enter Play Mode
4. Verify assets load correctly

**Build Testing:**
1. Set Play Mode Script: **Use Existing Build**
2. Build Addressables: **Build → New Build → Default Build Script**
3. Enter Play Mode
4. Verify bundles load from local build

#### 3.2 Build Size Verification

**Before Migration:**
```bash
# Check current build size
Build/YourGame.exe (or .app): 500 MB
```

**After Migration:**
```bash
# Initial build
Build/YourGame.exe: 100 MB (80% reduction!)

# Bundles (separate)
ServerData/StandaloneWindows64/: 400 MB
```

---

### Phase 4: Deployment (Production)

#### 4.1 Configure Remote URLs

**In Addressables Groups window:**
1. Click on "Remote Prefabs" group
2. Inspector → Advanced → Path Pair
3. Build Path: `Remote.BuildPath` (keep as-is)
4. Load Path: Change to your CDN URL

**Example Load Paths:**
```
Development: http://localhost:8080/bundles/[BuildTarget]
Staging: https://staging-cdn.yourcompany.com/bundles/[BuildTarget]
Production: https://cdn.yourcompany.com/bundles/[BuildTarget]
```

#### 4.2 Build for Production

**Build Addressables:**
```
Window → Asset Management → Addressables → Groups
→ Build → New Build → Default Build Script
```

**Output:**
```
ServerData/
├── StandaloneWindows64/
│   ├── remote_prefabs_assets_all.bundle
│   ├── catalog.json
│   └── catalog.hash
├── Android/
│   ├── remote_prefabs_assets_all.bundle
│   ├── catalog.json
│   └── catalog.hash
└── iOS/
    ├── remote_prefabs_assets_all.bundle
    ├── catalog.json
    └── catalog.hash
```

#### 4.3 Deploy to CDN

**Upload bundles:**
```bash
# Example using AWS S3
aws s3 sync ServerData/ s3://your-bucket/bundles/ --acl public-read

# Example using Azure Blob Storage
az storage blob upload-batch -s ServerData/ -d bundles --account-name youraccount

# Or use FTP/SFTP to your server
```

**Verify URLs:**
```
https://cdn.yourcompany.com/bundles/StandaloneWindows64/remote_prefabs_assets_all.bundle
https://cdn.yourcompany.com/bundles/Android/remote_prefabs_assets_all.bundle
https://cdn.yourcompany.com/bundles/iOS/remote_prefabs_assets_all.bundle
```

#### 4.4 Update Load Path

**Before building your app:**
1. Addressables Groups window
2. Select "Remote Prefabs" group
3. Inspector → Load Path → Enter your CDN URL
4. Save

**Build your app with correct CDN URL configured**

---

## 📦 Recommended Asset Organization

### Asset Groups Structure

```
Addressables Groups:
├── Built-In Data (Local)
│   ├── Core UI prefabs
│   ├── Essential game systems
│   └── Startup scenes
│
├── Creatures - Starter (Local - 50 MB)
│   ├── 10-20 common creatures
│   └── Tutorial creatures
│
├── Creatures - Common (Remote - 200 MB)
│   ├── Regular creatures
│   └── Common variants
│
├── Creatures - Rare (Remote - 300 MB)
│   ├── Rare creatures
│   ├── Legendary creatures
│   └── Event-exclusive
│
├── Environments - Forest (Remote - 150 MB)
│   ├── Forest biome props
│   └── Forest-specific assets
│
├── Environments - Desert (Remote - 150 MB)
│   ├── Desert biome props
│   └── Desert-specific assets
│
└── Audio & VFX (Remote - 100 MB)
    ├── Music tracks
    ├── Ambient sounds
    └── Visual effects
```

### Download Strategy

**First Launch:**
1. Core UI + Starter Creatures: Immediate (50 MB)
2. Common Creatures: Download in background
3. Rare Creatures: Download on-demand when needed
4. Environments: Download when player enters biome

**Benefits:**
- Player starts playing in ~30 seconds (50 MB download)
- Total content downloads while playing
- Premium content downloads only if accessed

---

## 🚀 Advanced Features

### Content Updates Without Rebuilding

**Update workflow:**
```bash
# 1. Modify your assets in Unity
# 2. Build Content Update
Window → Addressables → Check for Content Update Restrictions
Window → Addressables → Build → Update a Previous Build

# 3. Deploy only changed bundles
# Upload new bundles to CDN
# Players automatically download updated content
```

**No app rebuild or resubmission needed!**

### Progress Tracking

```csharp
// Show download progress to user
var progress = new Progress<float>(p =>
{
    progressBar.value = p;
    progressText.text = $"Downloading: {p * 100:F0}%";
});

await _assetService.PreloadCoreAssetsAsync(progress);
```

### Caching & Memory Management

```csharp
// Check if asset is cached
if (_assetService.IsAssetCached("Prefabs/Dragon"))
{
    var dragon = _assetService.GetCachedAsset<GameObject>("Prefabs/Dragon");
    // Instant, no loading needed
}

// Unload when done to free memory
_assetService.UnloadAsset("Prefabs/Dragon");

// Clear all cache
_assetService.ClearCache();

// Get cache stats
var stats = _assetService.GetCacheStats();
Debug.Log($"Cached: {stats.TotalAssets} assets, {stats.TotalMemoryUsage / 1024 / 1024} MB");
```

---

## 🐛 Troubleshooting

### "Asset not found" after migration

**Problem:** AssetService can't find the asset
**Solution:**
1. Verify asset is marked as Addressable
2. Check the address matches what you're loading
3. Ensure Addressables are built (Window → Addressables → Groups → Build)

### Build size didn't decrease

**Problem:** Build is still large after migration
**Solution:**
1. Ensure assets are in "Remote" groups, not "Local" groups
2. Remote groups must have Load Path set to CDN URL (not Local.LoadPath)
3. Rebuild addressables AND rebuild your app

### Downloads are slow/failing

**Problem:** Asset downloads timeout or fail
**Solution:**
1. Check CDN URL is correct and accessible
2. Increase timeout in AddressableAssetSettings:
   ```
   Window → Addressables → Settings
   → Catalog Request Timeout: 30 (increase from default)
   ```
3. Verify bundles are uploaded correctly to CDN

### Invalid key exception

**Problem:** `InvalidKeyException` when loading asset
**Solution:**
1. Use correct address (e.g., "Prefabs/Creature", not "Prefabs/Creature.prefab")
2. Address must match exactly (case-sensitive)
3. Asset must be built into addressables bundles

---

## 📋 Migration Checklist

### Pre-Migration
- [ ] Backup your project
- [ ] Review current build size
- [ ] Identify large assets in Resources folders
- [ ] Plan asset organization (Local vs Remote)

### Asset Migration
- [ ] Run Addressables Migration Tool
- [ ] Scan Resources folders
- [ ] Mark assets as Addressable
- [ ] Organize into groups (Local/Remote)
- [ ] Configure CDN URL for remote groups

### Code Migration
- [ ] Run Resources Code Migration Tool
- [ ] Export migration report
- [ ] Update code to use AssetService
- [ ] Add async/await patterns
- [ ] Test each migrated component

### Testing
- [ ] Test in Play Mode (Use Asset Database)
- [ ] Build Addressables bundles
- [ ] Test in Play Mode (Use Existing Build)
- [ ] Create test build
- [ ] Verify build size reduction
- [ ] Test asset loading in build

### Deployment
- [ ] Configure production CDN URL
- [ ] Build final Addressables
- [ ] Upload bundles to CDN
- [ ] Verify CDN accessibility
- [ ] Build production app
- [ ] Test production download

---

## 📚 Additional Resources

**Unity Documentation:**
- [Addressables Overview](https://docs.unity3d.com/Packages/com.unity.addressables@latest)
- [Content Updates](https://docs.unity3d.com/Packages/com.unity.addressables@latest/manual/ContentUpdateWorkflow.html)

**Project Chimera Documentation:**
- AssetService: `Assets/_Project/Scripts/Core/Services/AssetService.cs`
- Migration Tools: `Assets/_Project/Editor/AddressablesMigrationTool.cs`

**Getting Help:**
- Check migration reports in `Assets/_Project/Docs/`
- Review code suggestions in Resources Code Migration Tool
- Contact team if you encounter issues

---

**Happy Migrating! Your build size will thank you. 🚀**
