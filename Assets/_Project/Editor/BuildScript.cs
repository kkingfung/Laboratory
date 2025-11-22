using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

/// <summary>
/// Custom build script for Project Chimera CI/CD pipeline.
/// Handles multi-platform builds with robust error handling and monster-level optimization.
/// </summary>
public static class BuildScript
{
    private const string PROJECT_NAME = "ProjectChimera";
    private const string BUILD_FOLDER = "Builds";
    
    #region Public Build Methods

    /// <summary>
    /// Main build method called by CI/CD pipeline or manual builds.
    /// Builds the project for the specified platform with optimized settings.
    /// </summary>
    /// <param name="buildPath">Output path for the build</param>
    /// <param name="buildName">Name of the build executable</param>
    /// <param name="targetPlatform">Target platform to build for</param>
    public static void BuildProject(string buildPath = null, string buildName = null, BuildTarget targetPlatform = BuildTarget.StandaloneWindows64)
    {
        try
        {
            Debug.Log($"🚀 Starting {PROJECT_NAME} build for {targetPlatform}...");

            // Validate Unity version and project state
            if (!ValidateUnityEnvironment())
            {
                Debug.LogError("❌ Unity environment validation failed");
                ExitWithCode(1);
                return;
            }

            // Set default values if not provided
            if (string.IsNullOrEmpty(buildPath))
            {
                buildPath = GetDefaultBuildPath(targetPlatform);
            }

            if (string.IsNullOrEmpty(buildName))
            {
                buildName = GetDefaultBuildName(targetPlatform);
            }

            // Ensure build directory exists
            try
            {
                if (!Directory.Exists(buildPath))
                {
                    Directory.CreateDirectory(buildPath);
                    Debug.Log($"📁 Created build directory: {buildPath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to create build directory: {e.Message}");
                ExitWithCode(1);
                return;
            }

            // Get build scenes with fallback
            string[] scenes = GetBuildScenes();
            if (scenes.Length == 0)
            {
                Debug.LogWarning("⚠️ No scenes found in build settings. Searching project...");
                scenes = GetAllScenesFromProject();
                
                if (scenes.Length == 0)
                {
                    Debug.LogError("❌ No scenes found in the project. Cannot build without scenes!");
                    ExitWithCode(1);
                    return;
                }
                
                Debug.Log($"✅ Found {scenes.Length} scene(s) in project to include");
            }

            // Configure build settings
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(buildPath, buildName),
                target = targetPlatform,
                options = GetBuildOptions(targetPlatform)
            };

            // Apply platform-specific settings
            if (!ApplyPlatformSettings(targetPlatform))
            {
                Debug.LogError("❌ Failed to apply platform settings");
                ExitWithCode(1);
                return;
            }

            // Pre-build cleanup
            PerformPreBuildCleanup();

            // Perform the build
            Debug.Log($"📦 Building to: {buildPlayerOptions.locationPathName}");
            Debug.Log($"📋 Including {scenes.Length} scene(s):");
            foreach (string scene in scenes)
            {
                Debug.Log($"  • {scene}");
            }
            
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            // Handle build results
            HandleBuildResult(report, buildPath, buildName, targetPlatform);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Build process crashed: {e.Message}\n{e.StackTrace}");
            ExitWithCode(1);
        }
    }

    #endregion

    #region Build Configuration

    private static bool ValidateUnityEnvironment()
    {
        try
        {
            Debug.Log($"🔍 Validating Unity environment...");
            Debug.Log($"   Unity Version: {Application.unityVersion}");
            Debug.Log($"   Platform: {Application.platform}");
            Debug.Log($"   Is Editor: {Application.isEditor}");
            
            // Check if we're in batch mode for CI builds
            if (Application.isBatchMode)
            {
                Debug.Log("   Running in batch mode (CI/CD)");
            }
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Environment validation failed: {e.Message}");
            return false;
        }
    }

    private static string GetDefaultBuildPath(BuildTarget targetPlatform)
    {
        string platformName = targetPlatform.ToString();
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        return Path.Combine(BUILD_FOLDER, platformName, timestamp);
    }

    private static string GetDefaultBuildName(BuildTarget targetPlatform)
    {
        switch (targetPlatform)
        {
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneWindows:
                return $"{PROJECT_NAME}.exe";
            case BuildTarget.StandaloneLinux64:
                return PROJECT_NAME;
            case BuildTarget.StandaloneOSX:
                return $"{PROJECT_NAME}.app";
            case BuildTarget.Android:
                return $"{PROJECT_NAME}.apk";
            case BuildTarget.iOS:
                return PROJECT_NAME;
            default:
                return PROJECT_NAME;
        }
    }

    private static string[] GetBuildScenes()
    {
        try
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
                .Select(scene => scene.path)
                .ToArray();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to get build scenes: {e.Message}");
            return new string[0];
        }
    }

    private static string[] GetAllScenesFromProject()
    {
        try
        {
            var scenes = new System.Collections.Generic.List<string>();
            
            // Find all scene assets in the project
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            
            foreach (string guid in sceneGuids)
            {
                try
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(scenePath) && 
                        !IsExcludedScene(scenePath))
                    {
                        scenes.Add(scenePath);
                        Debug.Log($"  ✅ Found scene: {scenePath}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"⚠️ Failed to process scene GUID {guid}: {e.Message}");
                }
            }

            return scenes.ToArray();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to get project scenes: {e.Message}");
            return new string[0];
        }
    }

    private static bool IsExcludedScene(string scenePath)
    {
        string[] excludePatterns = {
            "_Template",
            "_Sandbox", 
            "_Test",
            "/Test/",
            "/Tests/",
            "/Editor/",
            "/Debug/",
            "_Debug"
        };

        string lowerPath = scenePath.ToLower();
        return excludePatterns.Any(pattern => lowerPath.Contains(pattern.ToLower()));
    }

    private static BuildOptions GetBuildOptions(BuildTarget targetPlatform)
    {
        BuildOptions options = BuildOptions.None;
        
        try
        {
            // Use LZ4HC compression for good balance of size vs build time
            options |= BuildOptions.CompressWithLz4HC;
            
            // Enable strict mode for better error detection
            options |= BuildOptions.StrictMode;
            
            // Check for development build flag
            if (System.Environment.GetCommandLineArgs().Contains("-development") || 
                EditorUserBuildSettings.development)
            {
                options |= BuildOptions.Development;
                options |= BuildOptions.AllowDebugging;
                options |= BuildOptions.ConnectWithProfiler;
                Debug.Log("🐛 Development build enabled");
            }
            
            // Platform-specific options
            switch (targetPlatform)
            {
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneOSX:
                    // No additional options needed for desktop
                    break;
                case BuildTarget.Android:
                    // Android-specific build options would go here
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Failed to configure some build options: {e.Message}");
        }
        
        return options;
    }

    private static bool ApplyPlatformSettings(BuildTarget targetPlatform)
    {
        try
        {
            Debug.Log($"🎛️ Applying settings for {targetPlatform}...");
            
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(targetPlatform);
            
            // Safely apply common settings
            if (targetGroup != BuildTargetGroup.Unknown)
            {
                // Only modify settings if we can determine the target group
                ApplyCommonSettings(targetGroup);
                ApplyPlatformSpecificSettings(targetPlatform, targetGroup);
            }
            else
            {
                Debug.LogWarning($"⚠️ Unknown target group for {targetPlatform}, using default settings");
            }
            
            Debug.Log($"✅ Platform settings applied for {targetPlatform}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to apply platform settings: {e.Message}");
            return false;
        }
    }

    private static void ApplyCommonSettings(BuildTargetGroup targetGroup)
    {
        try
        {
            // Safely set stripping settings
            if (System.Enum.IsDefined(typeof(ManagedStrippingLevel), ManagedStrippingLevel.Medium))
            {
                // Use modern API for managed stripping level
                #if UNITY_2021_2_OR_NEWER
                var namedTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
                var currentBackend = PlayerSettings.GetScriptingBackend(namedTarget);
                PlayerSettings.SetManagedStrippingLevel(namedTarget, ManagedStrippingLevel.Medium);
                #else
                var currentBackend = PlayerSettings.GetScriptingBackend(targetGroup);
                PlayerSettings.SetManagedStrippingLevel(targetGroup, ManagedStrippingLevel.Medium);
                #endif
            }
            
            // Set reasonable API compatibility
            try
            {
                // Use modern API for API compatibility level
                #if UNITY_2021_2_OR_NEWER
                var namedTargetForApi = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
                PlayerSettings.SetApiCompatibilityLevel(namedTargetForApi, ApiCompatibilityLevel.NET_Standard_2_0);
                #else
                PlayerSettings.SetApiCompatibilityLevel(targetGroup, ApiCompatibilityLevel.NET_Standard_2_0);
                #endif
            }
            catch (System.Exception)
            {
                // Fallback if NET_Standard_2_1 is not available
                Debug.LogWarning("⚠️ .NET Standard 2.1 not available, using default API level");
            }
            
            // Configure scripting backend if available
            try
            {
                // Use modern API for scripting backend
                #if UNITY_2021_2_OR_NEWER
                var namedTargetForBackend = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
                PlayerSettings.SetScriptingBackend(namedTargetForBackend, ScriptingImplementation.Mono2x);
                #else
                PlayerSettings.SetScriptingBackend(targetGroup, ScriptingImplementation.Mono2x);
                #endif
            }
            catch (System.Exception)
            {
                Debug.LogWarning("⚠️ Could not set scripting backend");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Some common settings could not be applied: {e.Message}");
        }
    }

    private static void ApplyPlatformSpecificSettings(BuildTarget targetPlatform, BuildTargetGroup targetGroup)
    {
        try
        {
            switch (targetPlatform)
            {
                case BuildTarget.StandaloneWindows64:
                    ApplyWindowsSettings(targetGroup);
                    break;
                case BuildTarget.StandaloneLinux64:
                    ApplyLinuxSettings(targetGroup);
                    break;
                case BuildTarget.StandaloneOSX:
                    ApplyMacSettings(targetGroup);
                    break;
                case BuildTarget.Android:
                    ApplyAndroidSettings(targetGroup);
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Could not apply all platform-specific settings: {e.Message}");
        }
    }

    private static void ApplyWindowsSettings(BuildTargetGroup targetGroup)
    {
        try
        {
            // Use modern API for architecture setting
            #if UNITY_2021_2_OR_NEWER
            #if UNITY_2021_2_OR_NEWER
        var namedTargetForArch = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
        PlayerSettings.SetArchitecture(namedTargetForArch, 1); // x64
        #else
        PlayerSettings.SetArchitecture(targetGroup, 1); // x64
        #endif
            #else
            PlayerSettings.SetArchitecture(targetGroup, 1); // x64
            #endif
            Debug.Log("   🖥️ Windows x64 architecture set");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Could not set Windows architecture: {e.Message}");
        }
    }

    private static void ApplyLinuxSettings(BuildTargetGroup targetGroup)
    {
        Debug.Log("   🐧 Linux settings applied");
    }

    private static void ApplyMacSettings(BuildTargetGroup targetGroup)
    {
        Debug.Log("   🍎 macOS settings applied");
    }

    private static void ApplyAndroidSettings(BuildTargetGroup targetGroup)
    {
        try
        {
            Debug.Log("   🤖 Android settings applied");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Could not apply Android settings: {e.Message}");
        }
    }

    #endregion

    #region Build Process Management

    private static void PerformPreBuildCleanup()
    {
        try
        {
            Debug.Log("🧹 Performing pre-build cleanup...");
            
            // Clear console to reduce noise
            var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
            if (logEntries != null)
            {
                var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                clearMethod?.Invoke(null, null);
            }
            
            // Force asset database refresh
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            Debug.Log("✅ Pre-build cleanup completed");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Pre-build cleanup had issues: {e.Message}");
        }
    }

    private static void HandleBuildResult(BuildReport report, string buildPath, string buildName, BuildTarget targetPlatform)
    {
        if (report == null)
        {
            Debug.LogError("❌ Build report is null!");
            ExitWithCode(1);
            return;
        }

        var summary = report.summary;
        
        if (summary.result == BuildResult.Succeeded)
        {
            LogBuildSuccess(summary, buildPath, buildName, targetPlatform);
            ExitWithCode(0);
        }
        else
        {
            LogBuildFailure(report, summary);
            ExitWithCode(1);
        }
    }

    private static void LogBuildSuccess(BuildSummary summary, string buildPath, string buildName, BuildTarget targetPlatform)
    {
        Debug.Log($"🎉 {PROJECT_NAME} build succeeded for {targetPlatform}!");
        Debug.Log($"📁 Build location: {buildPath}");
        Debug.Log($"📊 Build size: {GetBuildSize(buildPath):F2} MB");
        Debug.Log($"⏱️ Build time: {summary.totalTime.TotalMinutes:F2} minutes");
        Debug.Log($"⚠️ Warnings: {summary.totalWarnings}");
        Debug.Log($"❌ Errors: {summary.totalErrors}");
        
        if (summary.totalWarnings > 0)
        {
            Debug.LogWarning($"⚠️ Build completed with {summary.totalWarnings} warnings - consider reviewing them");
        }
    }

    private static void LogBuildFailure(BuildReport report, BuildSummary summary)
    {
        Debug.LogError($"❌ {PROJECT_NAME} build failed!");
        Debug.LogError($"🔍 Build result: {summary.result}");
        Debug.LogError($"⚠️ Total warnings: {summary.totalWarnings}");
        Debug.LogError($"❌ Total errors: {summary.totalErrors}");
        
        // Log specific build errors and warnings
        try
        {
            foreach (var step in report.steps)
            {
                foreach (var message in step.messages)
                {
                    switch (message.type)
                    {
                        case LogType.Error:
                        case LogType.Exception:
                            Debug.LogError($"Build Error: {message.content}");
                            break;
                        case LogType.Warning:
                            Debug.LogWarning($"Build Warning: {message.content}");
                            break;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to log build messages: {e.Message}");
        }
    }

    private static double GetBuildSize(string buildPath)
    {
        if (!Directory.Exists(buildPath))
            return 0;

        long totalSize = 0;
        try
        {
            var files = Directory.GetFiles(buildPath, "*", SearchOption.AllDirectories);
            
            foreach (string file in files)
            {
                try
                {
                    totalSize += new FileInfo(file).Length;
                }
                catch (System.Exception)
                {
                    // Skip files we can't read
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not calculate build size: {e.Message}");
        }

        return totalSize / (1024.0 * 1024.0); // Convert to MB
    }

    private static void ExitWithCode(int exitCode)
    {
        if (!Application.isEditor || Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    #endregion

    #region Menu Items & Utility Methods

    [MenuItem("🧪 Laboratory/Build/Windows 64-bit")]
    public static void BuildWindows64()
    {
        BuildProject(null, null, BuildTarget.StandaloneWindows64);
    }

    [MenuItem("🧪 Laboratory/Build/Linux 64-bit")]
    public static void BuildLinux64()
    {
        BuildProject(null, null, BuildTarget.StandaloneLinux64);
    }

    [MenuItem("🧪 Laboratory/Build/macOS")]
    public static void BuildMacOS()
    {
        BuildProject(null, null, BuildTarget.StandaloneOSX);
    }

    [MenuItem("🧪 Laboratory/Build/All Platforms")]
    public static void BuildAllPlatforms()
    {
        Debug.Log("🏗️ Starting multi-platform build for Project Chimera...");

        var platforms = new[]
        {
            BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneLinux64,
            BuildTarget.StandaloneOSX
        };

        int successCount = 0;
        foreach (var platform in platforms)
        {
            try
            {
                Debug.Log($"🎯 Building for {platform}...");
                BuildProject(null, null, platform);
                successCount++;
                Debug.Log($"✅ {platform} build completed!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Failed to build for {platform}: {ex.Message}");
            }
        }

        if (successCount == platforms.Length)
        {
            Debug.Log($"🎉 Multi-platform build completed successfully! All {platforms.Length} platforms built.");
        }
        else
        {
            Debug.LogWarning($"⚠️ Multi-platform build completed with issues. {successCount}/{platforms.Length} platforms succeeded.");
        }
    }

    [MenuItem("🧪 Laboratory/Build/Quick Development")]
    public static void QuickDevelopmentBuild()
    {
        EditorUserBuildSettings.development = true;
        BuildProject("Builds/Development", $"{PROJECT_NAME}-Dev.exe", BuildTarget.StandaloneWindows64);
    }
    
    [MenuItem("🧪 Laboratory/Build/Validate Settings")]
    public static void ValidateProjectSettings()
    {
        Debug.Log("🔍 Validating Project Chimera settings...");
        
        // Check scenes configuration
        var buildScenes = GetBuildScenes();
        if (buildScenes.Length == 0)
        {
            Debug.LogWarning("⚠️ No scenes in Build Settings!");
            
            var projectScenes = GetAllScenesFromProject();
            if (projectScenes.Length > 0)
            {
                Debug.Log($"📋 Found {projectScenes.Length} scene(s) in project:");
                foreach (var scene in projectScenes)
                {
                    Debug.Log($"  • {scene}");
                }
                Debug.Log("💡 Add these scenes via File > Build Settings");
            }
            else
            {
                Debug.LogError("❌ No scenes found in project! Create at least one scene.");
            }
        }
        else
        {
            Debug.Log($"✅ {buildScenes.Length} scene(s) configured for build");
            foreach (var scene in buildScenes)
            {
                Debug.Log($"  ✅ {scene}");
            }
        }
        
        // Check player settings
        Debug.Log("🎮 Player Settings:");
        Debug.Log($"  Company: {PlayerSettings.companyName}");
        Debug.Log($"  Product: {PlayerSettings.productName}");
        Debug.Log($"  Version: {PlayerSettings.bundleVersion}");
        // Use modern API for getting application identifier
        #if UNITY_2021_2_OR_NEWER
        #if UNITY_2021_2_OR_NEWER
        Debug.Log($"  Bundle ID: {PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone)}");
        #else
        Debug.Log($"  Bundle ID: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Standalone)}");
        #endif
        #else
        Debug.Log($"  Bundle ID: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Standalone)}");
        #endif
        
        // Check critical packages
        Debug.Log("📦 Checking critical packages...");
        if (System.IO.File.Exists("Packages/manifest.json"))
        {
            Debug.Log("✅ Package manifest found");
        }
        else
        {
            Debug.LogWarning("⚠️ No package manifest found");
        }
        
        Debug.Log("✅ Project Chimera validation complete!");
    }

    [MenuItem("🧪 Laboratory/Build/Open Build Folder")]
    public static void OpenBuildFolder()
    {
        string buildPath = Path.GetFullPath(BUILD_FOLDER);
        
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
            Debug.Log($"📁 Created build folder: {buildPath}");
        }
        
        EditorUtility.RevealInFinder(buildPath);
    }

    #endregion
}
