# GitHub Actions Workflows

This directory contains automated workflows for the Project Chimera Unity 6 development pipeline.

## 🔧 Recent Fixes (December 2024)

### ❌ Deprecated Action Removed
**Issue**: `Warning: Unexpected input(s) 'unityVersion', valid inputs are ['']`
**Error**: `This action is no longer supported. Please use the updated activation instructions`

**Root Cause**: The `game-ci/unity-request-activation-file@v2` action has been deprecated and no longer works.

### ✅ Solution Applied
- **Removed**: Deprecated `game-ci/unity-request-activation-file@v2` from performance monitoring
- **Updated**: All Unity CI workflows to use latest GameCI actions:
  - `game-ci/unity-test-runner@v4` for testing
  - `game-ci/unity-builder@v4` for building
- **Added**: Unity authentication via environment variables
- **Improved**: Caching strategy for Unity installations

## 🛠️ Workflow Files

### Core Workflows
- **`unity-ci.yml`** - Basic Unity CI/CD pipeline
- **`unity-ci-enhanced.yml`** - Advanced CI with performance monitoring
- **`performance-monitoring.yml`** - Automated performance auditing (no Unity dependency)

### Automation Workflows
- **`pr-automation.yml`** - Pull request automation and analysis
- **`issue-automation.yml`** - Issue triage and management
- **`developer-experience.yml`** - Developer onboarding and tool validation

## 🔑 Required Secrets

To use Unity workflows, configure these repository secrets:

```
UNITY_LICENSE    # Unity Pro/Personal license (base64 encoded)
UNITY_EMAIL      # Unity account email
UNITY_PASSWORD   # Unity account password
```

## 📋 Unity Version

Current project uses: **Unity 6000.2.0b7**

## 🚨 Migration Notes

If you encounter similar activation errors:

1. ❌ **Don't use**: `game-ci/unity-request-activation-file@v2`
2. ✅ **Use instead**: Latest GameCI actions with proper authentication
3. 🔄 **Update**: All action versions to v4+ for Unity 6 compatibility

## 🔗 References

- [GameCI Documentation](https://game.ci/docs/github/getting-started)
- [Unity 6 CI/CD Best Practices](https://docs.unity3d.com/Manual/ContinuousIntegration.html)
- [GitHub Actions Unity Guide](https://game.ci/docs/github/activation)