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

To use Unity workflows, configure these repository secrets in GitHub Settings → Secrets and variables → Actions:

### For Personal (Free) License:
```
UNITY_LICENSE    # Contents of your .ulf license file (NOT base64 encoded)
UNITY_EMAIL      # Your Unity account email
UNITY_PASSWORD   # Your Unity account password
```

### For Professional License:
```
UNITY_SERIAL     # Your Unity serial number/key
UNITY_EMAIL      # Your Unity account email
UNITY_PASSWORD   # Your Unity account password
```

### 📋 Setup Instructions:

**Personal License Setup:**
1. Install Unity Hub and log in to your Unity account
2. Activate a Personal license in Unity Hub
3. Find your license file (.ulf) in:
   - **Windows**: `%PROGRAMDATA%\Unity\Unity_lic.ulf`
   - **macOS**: `/Library/Application Support/Unity/Unity_lic.ulf`
   - **Linux**: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
4. Copy the **entire contents** of the .ulf file (not base64 encoded)
5. Add as `UNITY_LICENSE` secret in GitHub

**Professional License Setup:**
1. Get your serial key from Unity Dashboard → Organizations → Subscriptions
2. Add the serial key as `UNITY_SERIAL` secret in GitHub
3. Add your Unity email and password as secrets

### 🚨 Troubleshooting Common Issues:

**❌ "Missing Unity License File and no Serial was found"**
- **Fix**: Set up one of the secret configurations above
- **Check**: Go to GitHub repo → Settings → Secrets and variables → Actions
- **Verify**: Ensure you have either `UNITY_LICENSE` OR `UNITY_SERIAL` (not both)

**⚠️ "Library folder does not exist"**
- **Fix**: This is normal for first builds - caching will improve subsequent builds
- **Note**: The enhanced caching in our workflows will resolve this after the first successful build

**❌ "Could not parse [git-sha] to semver"**
- **Fix**: Updated workflows now use semantic versioning (`1.0.{run_number}`)
- **Note**: This error should be resolved in the latest workflow versions

**🔍 Testing Your Setup:**
1. Trigger the new `unity-diagnostic.yml` workflow manually
2. Check the diagnostic results to identify specific issues
3. The diagnostic will show exactly which secrets are missing

## 📋 Unity Version

Current project uses: **Unity 6000.2.0f1** (Updated from beta to stable release)

**Note**: Beta versions (like 6000.2.0b7) don't have Docker images available for CI/CD. Always use final release versions (f1, f2, etc.) for reliable CI/CD pipelines.

## 🚨 Migration Notes

If you encounter similar activation errors:

1. ❌ **Don't use**: `game-ci/unity-request-activation-file@v2`
2. ✅ **Use instead**: Latest GameCI actions with proper authentication
3. 🔄 **Update**: All action versions to v4+ for Unity 6 compatibility

## 🔗 References

- [GameCI Documentation](https://game.ci/docs/github/getting-started)
- [Unity 6 CI/CD Best Practices](https://docs.unity3d.com/Manual/ContinuousIntegration.html)
- [GitHub Actions Unity Guide](https://game.ci/docs/github/activation)