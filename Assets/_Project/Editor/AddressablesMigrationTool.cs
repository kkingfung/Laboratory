using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Laboratory.Editor
{
    /// <summary>
    /// Tool to migrate Resources folder assets to Addressables system.
    /// Helps reduce build size by converting large prefabs to downloadable bundles.
    /// </summary>
    public class AddressablesMigrationTool : EditorWindow
    {
        private enum AssetCategory
        {
            All,
            Prefabs,
            ScriptableObjects,
            Materials,
            Textures,
            AudioClips,
            Configs
        }

        private AssetCategory _selectedCategory = AssetCategory.Prefabs;
        private bool _createRemoteGroup = true;
        private string _remoteCDNUrl = "https://your-cdn.com/bundles";
        private Vector2 _scrollPosition;
        private List<string> _foundAssets = new List<string>();
        private bool _scanComplete = false;
        private long _totalSize = 0;

        [MenuItem("Tools/Chimera/Addressables Migration Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<AddressablesMigrationTool>("Addressables Migration");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Addressables Migration Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool helps migrate assets from Resources folders to Addressables system.\n" +
                "Benefits: Smaller builds, on-demand downloads, easier content updates.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // Step 1: Scan for assets
            EditorGUILayout.LabelField("Step 1: Scan for Assets", EditorStyles.boldLabel);
            _selectedCategory = (AssetCategory)EditorGUILayout.EnumPopup("Asset Category:", _selectedCategory);

            if (GUILayout.Button("Scan Resources Folders", GUILayout.Height(30)))
            {
                ScanResourcesFolders();
            }

            EditorGUILayout.Space(10);

            // Step 2: Review findings
            if (_scanComplete && _foundAssets.Count > 0)
            {
                EditorGUILayout.LabelField("Step 2: Review Assets", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Found: {_foundAssets.Count} assets ({FormatBytes(_totalSize)})");

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
                foreach (var assetPath in _foundAssets.Take(50))
                {
                    EditorGUILayout.LabelField($"• {assetPath}");
                }
                if (_foundAssets.Count > 50)
                {
                    EditorGUILayout.LabelField($"... and {_foundAssets.Count - 50} more");
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(10);

                // Step 3: Configure migration
                EditorGUILayout.LabelField("Step 3: Configure Migration", EditorStyles.boldLabel);
                _createRemoteGroup = EditorGUILayout.Toggle("Create Remote Group", _createRemoteGroup);

                if (_createRemoteGroup)
                {
                    EditorGUI.indentLevel++;
                    _remoteCDNUrl = EditorGUILayout.TextField("CDN Base URL:", _remoteCDNUrl);
                    EditorGUILayout.HelpBox(
                        "Remote groups allow downloading assets from a server/CDN.\n" +
                        "Leave as default for local bundles, update later when deploying.",
                        MessageType.Info
                    );
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(10);

                // Step 4: Execute migration
                EditorGUILayout.LabelField("Step 4: Execute Migration", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Mark as Addressable (Local)", GUILayout.Height(40)))
                {
                    MarkAssetsAsAddressable(false);
                }
                if (GUILayout.Button("Mark as Addressable (Remote)", GUILayout.Height(40)))
                {
                    MarkAssetsAsAddressable(true);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                if (GUILayout.Button("Generate Code Migration Report", GUILayout.Height(30)))
                {
                    GenerateCodeMigrationReport();
                }
            }
            else if (_scanComplete)
            {
                EditorGUILayout.HelpBox("No assets found in Resources folders.", MessageType.Warning);
            }
        }

        private void ScanResourcesFolders()
        {
            _foundAssets.Clear();
            _totalSize = 0;

            // Find all Resources folders
            var resourceFolders = AssetDatabase.FindAssets("", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.Contains("/Resources/"))
                .ToList();

            // Search for assets based on category
            string filter = _selectedCategory switch
            {
                AssetCategory.Prefabs => "t:Prefab",
                AssetCategory.ScriptableObjects => "t:ScriptableObject",
                AssetCategory.Materials => "t:Material",
                AssetCategory.Textures => "t:Texture2D",
                AssetCategory.AudioClips => "t:AudioClip",
                AssetCategory.Configs => "t:ScriptableObject",
                _ => ""
            };

            var guids = AssetDatabase.FindAssets(filter, new[] { "Assets" });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                // Only include assets in Resources folders
                if (path.Contains("/Resources/"))
                {
                    _foundAssets.Add(path);

                    // Calculate size
                    var fileInfo = new FileInfo(path);
                    if (fileInfo.Exists)
                    {
                        _totalSize += fileInfo.Length;
                    }
                }
            }

            _foundAssets.Sort();
            _scanComplete = true;

            Debug.Log($"[Addressables Migration] Found {_foundAssets.Count} {_selectedCategory} assets ({FormatBytes(_totalSize)})");
        }

        private void MarkAssetsAsAddressable(bool isRemote)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Error", "Addressables settings not found. Please initialize Addressables first.", "OK");
                return;
            }

            // Create or get the target group
            AddressableAssetGroup targetGroup;
            if (isRemote && _createRemoteGroup)
            {
                targetGroup = GetOrCreateGroup(settings, $"Remote {_selectedCategory}", true);
            }
            else
            {
                targetGroup = GetOrCreateGroup(settings, $"Local {_selectedCategory}", false);
            }

            int successCount = 0;
            int alreadyAddressable = 0;

            EditorUtility.DisplayProgressBar("Marking Assets as Addressable", "Processing...", 0f);

            try
            {
                for (int i = 0; i < _foundAssets.Count; i++)
                {
                    var assetPath = _foundAssets[i];
                    EditorUtility.DisplayProgressBar(
                        "Marking Assets as Addressable",
                        $"Processing {i + 1}/{_foundAssets.Count}: {Path.GetFileName(assetPath)}",
                        (float)i / _foundAssets.Count
                    );

                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    var entry = settings.FindAssetEntry(guid);

                    if (entry != null)
                    {
                        alreadyAddressable++;
                        continue;
                    }

                    // Create addressable entry
                    entry = settings.CreateOrMoveEntry(guid, targetGroup, false, false);

                    if (entry != null)
                    {
                        // Set address based on Resources path
                        var address = GetAddressFromResourcesPath(assetPath);
                        entry.SetAddress(address);
                        successCount++;
                    }
                }

                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();

            var message = $"Migration Complete!\n\n" +
                         $"Marked as Addressable: {successCount}\n" +
                         $"Already Addressable: {alreadyAddressable}\n" +
                         $"Total: {_foundAssets.Count}\n\n" +
                         $"Group: {targetGroup.Name}\n" +
                         $"Type: {(isRemote ? "Remote (Downloadable)" : "Local (Bundled)")}\n\n" +
                         $"Next Steps:\n" +
                         $"1. Update code to use AssetService instead of Resources.Load\n" +
                         $"2. Click 'Generate Code Migration Report' for guidance\n" +
                         $"3. Build Addressables: Window → Asset Management → Addressables → Groups → Build";

            EditorUtility.DisplayDialog("Success", message, "OK");

            Debug.Log($"[Addressables Migration] Marked {successCount} assets as Addressable in group '{targetGroup.Name}'");
        }

        private AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName, bool isRemote)
        {
            var group = settings.FindGroup(groupName);
            if (group != null)
                return group;

            // Create new group
            group = settings.CreateGroup(groupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            // Configure schemas
            var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
            if (bundledSchema != null)
            {
                if (isRemote)
                {
                    // Remote group settings
                    bundledSchema.BuildPath.SetVariableByName(settings, "Remote.BuildPath");
                    bundledSchema.LoadPath.SetVariableByName(settings, "Remote.LoadPath");
                    bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                }
                else
                {
                    // Local group settings
                    bundledSchema.BuildPath.SetVariableByName(settings, "Local.BuildPath");
                    bundledSchema.LoadPath.SetVariableByName(settings, "Local.LoadPath");
                    bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                }
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupAdded, group, true, true);
            return group;
        }

        private string GetAddressFromResourcesPath(string assetPath)
        {
            // Extract the path after "Resources/"
            var resourcesIndex = assetPath.IndexOf("/Resources/", StringComparison.Ordinal);
            if (resourcesIndex >= 0)
            {
                var relativePath = assetPath.Substring(resourcesIndex + "/Resources/".Length);
                // Remove file extension
                var extensionIndex = relativePath.LastIndexOf('.');
                if (extensionIndex > 0)
                {
                    relativePath = relativePath.Substring(0, extensionIndex);
                }
                return relativePath;
            }

            return Path.GetFileNameWithoutExtension(assetPath);
        }

        private void GenerateCodeMigrationReport()
        {
            var reportPath = "Assets/_Project/Docs/ADDRESSABLES_MIGRATION_REPORT.md";
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            var report = GenerateMigrationReport();
            File.WriteAllText(reportPath, report);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Report Generated", $"Code migration report saved to:\n{reportPath}", "OK");

            // Open the file
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(reportPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }

            Debug.Log($"[Addressables Migration] Report generated: {reportPath}");
        }

        private string GenerateMigrationReport()
        {
            var report = "# Addressables Migration Report\n\n";
            report += $"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";
            report += "## Summary\n\n";
            report += $"- **Assets Scanned:** {_foundAssets.Count}\n";
            report += $"- **Category:** {_selectedCategory}\n";
            report += $"- **Total Size:** {FormatBytes(_totalSize)}\n\n";

            report += "## Code Migration Guide\n\n";
            report += "### Replace Resources.Load Patterns\n\n";

            report += "#### Pattern 1: Single Asset Load\n";
            report += "```csharp\n";
            report += "// OLD (Resources)\n";
            report += "var prefab = Resources.Load<GameObject>(\"Prefabs/Creature\");\n\n";
            report += "// NEW (Addressables via AssetService)\n";
            report += "var prefab = await _assetService.LoadAssetAsync<GameObject>(\"Prefabs/Creature\");\n";
            report += "```\n\n";

            report += "#### Pattern 2: LoadAll Pattern\n";
            report += "```csharp\n";
            report += "// OLD (Resources)\n";
            report += "var items = Resources.LoadAll<ItemData>(\"Items\");\n\n";
            report += "// NEW (Addressables - requires manual label/group setup)\n";
            report += "// Option A: Load by label\n";
            report += "await Addressables.LoadAssetsAsync<ItemData>(\"Items\", null);\n\n";
            report += "// Option B: Load explicit list\n";
            report += "var keys = new[] { \"Items/Sword\", \"Items/Shield\", \"Items/Potion\" };\n";
            report += "await _assetService.LoadAssetsAsync(keys);\n";
            report += "```\n\n";

            report += "#### Pattern 3: Async Load\n";
            report += "```csharp\n";
            report += "// OLD (Resources.LoadAsync)\n";
            report += "var request = Resources.LoadAsync<AudioClip>(\"Audio/Music\");\n";
            report += "yield return request;\n";
            report += "var clip = request.asset as AudioClip;\n\n";
            report += "// NEW (AssetService with UniTask)\n";
            report += "var clip = await _assetService.LoadAssetAsync<AudioClip>(\"Audio/Music\");\n";
            report += "```\n\n";

            report += "### Dependency Injection Setup\n\n";
            report += "```csharp\n";
            report += "// In your VContainer scope configuration\n";
            report += "builder.Register<IAssetService, AssetService>(Lifetime.Singleton);\n\n";
            report += "// In your MonoBehaviour\n";
            report += "public class MyComponent : MonoBehaviour\n";
            report += "{\n";
            report += "    [Inject] private IAssetService _assetService;\n\n";
            report += "    private async UniTask LoadAssets()\n";
            report += "    {\n";
            report += "        var asset = await _assetService.LoadAssetAsync<GameObject>(\"MyAsset\");\n";
            report += "    }\n";
            report += "}\n";
            report += "```\n\n";

            report += "## Migrated Assets\n\n";
            report += "| Asset Path | Addressable Address |\n";
            report += "|------------|--------------------|\n";

            foreach (var assetPath in _foundAssets.Take(100))
            {
                var address = GetAddressFromResourcesPath(assetPath);
                report += $"| `{assetPath}` | `{address}` |\n";
            }

            if (_foundAssets.Count > 100)
            {
                report += $"\n... and {_foundAssets.Count - 100} more assets\n";
            }

            report += "\n## Next Steps\n\n";
            report += "1. **Update Code:** Replace `Resources.Load` calls with `AssetService.LoadAssetAsync`\n";
            report += "2. **Inject Dependencies:** Ensure `IAssetService` is injected where needed\n";
            report += "3. **Test Locally:** Use Play Mode script to verify assets load correctly\n";
            report += "4. **Build Addressables:** Window → Asset Management → Addressables → Groups → Build\n";
            report += "5. **Test Build:** Create a build and verify bundle loading\n";
            report += "6. **Deploy Remote:** Upload bundles to CDN and update Remote.LoadPath\n\n";

            report += "## Build Size Estimate\n\n";
            report += $"**Potential Build Size Reduction:** {FormatBytes(_totalSize)}\n\n";
            report += "By moving these assets to Addressables:\n";
            report += "- Initial app download will be smaller\n";
            report += "- Assets download on-demand or at first launch\n";
            report += "- Content updates without full app rebuild\n\n";

            return report;
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
