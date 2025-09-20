# Project Chimera CI/CD Documentation

This document describes the Continuous Integration and Continuous Deployment (CI/CD) setup for Project Chimera.

## 🚀 Overview

Our CI/CD pipeline ensures code quality, automated testing, and reliable builds across multiple platforms. The pipeline is designed to catch issues early and maintain high standards throughout development.

## 📋 Pipeline Components

### 1. Code Quality & Security (`code-quality.yml`)
- **Triggers**: Pull requests, weekly schedule
- **Purpose**: Static code analysis, security scanning, dependency checks
- **Tools**: GitHub Super Linter, custom analysis scripts
- **Duration**: ~5-10 minutes

### 2. Unity Build & Test (`unity-ci.yml`)
- **Triggers**: Push to main/develop, pull requests
- **Purpose**: Build testing, unit tests, multi-platform builds
- **Platforms**: Windows, Linux, macOS
- **Duration**: ~15-25 minutes

## 🔧 Configuration

### Environment Variables
- `UNITY_VERSION`: 6000.2.0b7
- `PROJECT_NAME`: ProjectChimera

### Build Matrix
The pipeline builds for three platforms:
- **Windows**: `StandaloneWindows64`
- **Linux**: `StandaloneLinux64` 
- **macOS**: `StandaloneOSX`

## 📊 Quality Gates

### Code Quality Checks
- ✅ C# syntax validation
- ✅ Security vulnerability scanning
- ✅ Dependency analysis
- ✅ Performance anti-pattern detection
- ✅ Documentation coverage analysis

### Build Requirements
- ✅ All tests must pass
- ✅ Build must succeed on all platforms
- ✅ No critical security vulnerabilities
- ✅ Code coverage above threshold

## 🧪 Testing Strategy

### Test Types
1. **Edit Mode Tests**: Unit tests that run in the Unity Editor
2. **Play Mode Tests**: Runtime tests that require the game to be running
3. **Integration Tests**: End-to-end functionality tests

### Test Files
- `Assets/Tests/EditMode/ProjectChimeraTests.cs`
- `Assets/Tests/PlayMode/ProjectChimeraPlayModeTests.cs`

## 🏗️ Build Process

### Build Script
The build process uses a custom `BuildScript.cs` located in `Assets/Editor/`:

```csharp
// Main build method
BuildScript.BuildProject(buildPath, buildName, targetPlatform);
```

### Build Optimization
- **Scripting Backend**: IL2CPP for better performance
- **API Compatibility**: .NET Standard 2.1
- **Code Stripping**: High level for smaller builds
- **Compression**: LZ4 for faster loading

## 📦 Artifacts

### Build Artifacts
- **Windows**: `ProjectChimera-Windows-{commit-sha}`
- **Linux**: `ProjectChimera-Linux-{commit-sha}`
- **macOS**: `ProjectChimera-macOS-{commit-sha}`

### Test Results
- **Edit Mode**: `TestResults-{platform}/EditMode`
- **Play Mode**: `TestResults-{platform}/PlayMode`

### Retention Policy
- **Build Artifacts**: 90 days
- **Test Results**: 30 days
- **Build Logs**: 30 days

## 🚀 Deployment

### Automatic Deployment
- **Trigger**: Push to `main` branch
- **Target**: Staging environment
- **Process**: Download artifacts → Deploy → Notify team

### Manual Deployment
For production deployments, use the GitHub Actions interface or CLI.

## 🔍 Monitoring & Debugging

### Build Logs
All build logs are automatically uploaded as artifacts and can be downloaded from the Actions tab.

### Common Issues
1. **Test Failures**: Check test results in artifacts
2. **Build Failures**: Review build logs for compilation errors
3. **Quality Gate Failures**: Address code quality issues

### Performance Monitoring
- Build times are tracked and reported
- Artifact sizes are monitored
- Test execution times are logged

## 🛠️ Local Development

### Running Tests Locally
```bash
# Edit Mode Tests
Unity -batchmode -quit -projectPath . -runTests -testPlatform editmode

# Play Mode Tests  
Unity -batchmode -quit -projectPath . -runTests -testPlatform playmode
```

### Building Locally
```bash
# Windows Build
Unity -batchmode -quit -projectPath . -executeMethod BuildScript.BuildProject -buildPath "Builds/Windows" -buildName "ProjectChimera.exe" -targetPlatform StandaloneWindows64
```

## 📈 Metrics & KPIs

### Key Performance Indicators
- **Build Success Rate**: Target >95%
- **Test Coverage**: Target >80%
- **Build Time**: Target <30 minutes
- **Deployment Frequency**: Daily
- **Mean Time to Recovery**: <2 hours

### Quality Metrics
- **Code Quality Score**: A+ rating
- **Security Vulnerabilities**: Zero critical
- **Technical Debt**: Low
- **Documentation Coverage**: >90%

## 🔧 Maintenance

### Regular Updates
- **Weekly**: Dependency updates
- **Monthly**: Unity version updates
- **Quarterly**: Pipeline optimization review

### Monitoring
- **Daily**: Build status checks
- **Weekly**: Quality report review
- **Monthly**: Performance analysis

## 📚 Resources

### Documentation
- [Unity CI/CD Best Practices](https://docs.unity3d.com/Manual/UnityCloudBuild.html)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Game CI Documentation](https://game.ci/)

### Support
- **Issues**: GitHub Issues tab
- **Discussions**: GitHub Discussions tab
- **Team**: Contact development team

## 🎯 Future Improvements

### Planned Enhancements
- [ ] Automated performance testing
- [ ] Visual regression testing
- [ ] Automated deployment to production
- [ ] Integration with external services (Steam, itch.io)
- [ ] Advanced code coverage reporting
- [ ] Automated dependency updates

### Optimization Goals
- Reduce build times by 20%
- Increase test coverage to 90%
- Implement automated rollback capabilities
- Add performance benchmarking

---

*Last updated: $(date)*
*Pipeline version: 2.0*
