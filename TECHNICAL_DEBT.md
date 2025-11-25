# Technical Debt Tracking - Project Chimera

This document tracks known technical debt items identified during code reviews and development. Items are prioritized and tracked for future cleanup.

---

## 🔴 HIGH PRIORITY

### Deprecated FindObjectOfType API Usage (27 occurrences) ✅

**Status:** ✅ Completed (Phases 1-3)
**Priority:** High (Unity 2023+ deprecation)
**Effort:** Medium (~4-6 hours)
**Created:** 2024 Code Review
**Completed:** 2024-11-25

**Description:**
27 files across the codebase use deprecated `FindObjectOfType<T>()` and `FindObjectsOfType<T>()` methods. Unity 2023+ recommends `FindFirstObjectByType<T>()` and `FindObjectsByType<T>()` instead for better performance and clarity.

**Impact:**
- Compilation warnings in Unity 2023+
- Potential performance issues (deprecated methods are slower)
- Future Unity versions may remove these APIs entirely

**Affected Files:**
```
Assets/_Project/Scripts/Editor/DebugConsoleEditorWindow.cs (4 occurrences)
Assets/_Project/Scripts/Subsystems/Performance/PerformanceSubsystemManager.cs (2 occurrences)
Assets/_Project/Scripts/Editor/Tools/AssetManagementWindow.cs (1 occurrence)
Assets/_Project/Scripts/Tools/RuntimePerformanceDashboard.cs (1 occurrence)
Assets/_Project/Scripts/ChimeraSceneValidator.cs (1 occurrence)
Assets/_Project/Scripts/Tools/Development/HotReloadSystem.cs (2 occurrences)
Assets/_Project/Scripts/Editor/Tools/MemoryProfilerWindow.cs (1 occurrence)
Assets/_Project/Scripts/Tools/HotReloadSystem.cs (1 occurrence)
Assets/_Project/Scripts/Tools/MemoryProfiler.cs (2 occurrences)
Assets/_Project/Scripts/Compatibility/UnityCompatibility.cs (2 occurrences)
Assets/_Project/Scripts/Core/Network/NetworkSynchronizationDebugger.cs (3 occurrences)
Assets/_Project/Scripts/Chimera/Social/Core/SocialServiceLocator.cs (2 comments)
Assets/_Project/Scripts/Advanced/AdvancedShaderManager.cs (1 occurrence)
Assets/_Project/Scripts/Advanced/DynamicWeatherSystem.cs (1 occurrence)
Assets/_Project/Scripts/Chimera/ECS/Systems/ChimeraECSSystems.cs (2 occurrences)
Assets/_Project/Scripts/Chimera/Ecosystem/Core/EcosystemServiceLocator.cs (2 comments)
```

**Recommended Approach:**
1. **Phase 1: Core Systems** (Priority)
   - Replace in performance-critical systems (PerformanceSubsystemManager, ECS systems)
   - Test thoroughly as these run frequently

2. **Phase 2: Editor Tools**
   - Update editor-only scripts (lower priority, editor-only impact)
   - Can be done incrementally without affecting runtime

3. **Phase 3: Compatibility Layer** ✅ Already Correct
   - UnityCompatibility.cs already uses conditional compilation correctly
   - Unity 2023+: Uses FindFirstObjectByType (no warnings)
   - Unity < 2023: Uses FindObjectOfType in #else blocks (backward compatibility)
   - No changes needed - deprecated calls won't compile on Unity 2023+

**Migration Pattern:**
```csharp
// OLD (deprecated)
var obj = FindObjectOfType<MyComponent>();
var objs = FindObjectsOfType<MyComponent>();

// NEW (Unity 2023+)
var obj = FindFirstObjectByType<MyComponent>();
var objs = FindObjectsByType<MyComponent>(FindObjectsSortMode.None);
```

**Testing Requirements:**
- Verify all 27 call sites after migration
- Test in both Play Mode and Editor
- Confirm no performance regressions

**Acceptance Criteria:**
- [x] All 27 occurrences addressed (23 replaced, 2 in compatibility layer, 2 in comments)
- [x] No compilation warnings related to FindObjectOfType
- [ ] All functionality tested and working (Phase 4: Testing)
- [ ] Performance benchmarks show no regression (Phase 4: Testing)

---

## 🟢 COMPLETED

### Deprecated FindObjectOfType Migration (Phases 1-3)
**Status:** ✅ Completed (Commits: 63eab0f6, f5bd6860)
**Completed:** 2024-11-25
**Summary:**
- Phase 1: Core Systems - 17 occurrences in 11 files (63eab0f6)
- Phase 2: Editor Tools - 6 occurrences in 3 files (f5bd6860)
- Phase 3: Compatibility Layer - Already correct with conditional compilation
**Total:** 23 deprecated calls migrated to Unity 2023+ APIs

### #pragma warning disable in MatchmakingSystem.cs
**Status:** ✅ Completed (Commit: 0de02734)
**Completed:** 2024-11-24
**Fix:** Implemented unused fields `_beginnerProtectionThreshold` and `_idealTeamSize` instead of suppressing warnings

### UILoadingScreen Singleton Cleanup
**Status:** ✅ Completed (Commit: aec31999)
**Completed:** 2024-11-24
**Fix:** Added OnDestroy() to clean up singleton reference

---

## 📋 BACKLOG

*(No items currently)*

---

## 📝 Notes

- All new code should avoid introducing deprecated API usage
- Code review should flag #pragma warning disable immediately
- Technical debt items should be addressed before major releases
