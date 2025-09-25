# 🧬 Contributing to Project Chimera

Welcome to Project Chimera! This guide will help you contribute effectively to our Unity 6 3D action game featuring advanced monster breeding systems.

## 🚀 Quick Start for Developers

### Prerequisites
- **Unity 6** (6000.2.0b7 or later)
- **Git LFS** (for large assets)
- **.NET 8.0** (for Unity scripting)
- **Visual Studio** or **VS Code** with Unity extensions

### Getting Started
1. **Fork and Clone**
   ```bash
   git clone https://github.com/yourusername/Laboratory.git
   cd Laboratory
   git lfs pull  # Important: Pull large assets
   ```

2. **Open in Unity**
   - Launch Unity Hub
   - Open the project (Unity will auto-configure)
   - Wait for initial compilation

3. **Verify Setup**
   - Open Unity Editor
   - Check Menu → Laboratory → Tools are available
   - Run QA Automation Tool for project health check

## 🛠️ Development Tools

We provide custom Unity Editor tools to enhance your development experience:

### 🚀 Performance Auditor Tool
**Access**: Unity Editor → Menu → `🚀 Laboratory/Performance Auditor`

**When to use**: Before committing code, especially performance-critical changes
- Automated detection of performance anti-patterns
- Real-time code scanning for bottlenecks
- One-click navigation to problematic code

### 🛠️ QA Automation Tool
**Access**: Unity Editor → Menu → `🛠️ Laboratory/QA Automation`

**When to use**: Daily development, before submitting PRs
- Comprehensive project validation (Scene, Asset, Performance, Build)
- Automated testing across multiple categories
- Quality gate enforcement

### ⚡ GameDev Workflow Tool
**Access**: Unity Editor → Menu → `⚡ Laboratory/GameDev Workflow`

**When to use**: Daily development tasks
- Quick development actions (save all, clear console)
- Scene management utilities
- Asset organization tools

## 📋 Coding Standards

### Namespace Convention
All new code must use the `Laboratory.*` namespace:
```csharp
namespace Laboratory.Subsystems.AI
{
    /// <summary>
    /// Description of what this class does
    /// </summary>
    public class MonsterBehavior : MonoBehaviour
    {
        // Implementation
    }
}
```

### Documentation Requirements
- **All public classes and methods** must have XML documentation
- Use `<summary>`, `<param>`, and `<returns>` tags
- Explain the "why", not just the "what"

```csharp
/// <summary>
/// Manages the breeding process between two Chimera monsters,
/// calculating genetic traits and offspring characteristics.
/// </summary>
/// <param name="parent1">First parent monster</param>
/// <param name="parent2">Second parent monster</param>
/// <returns>New offspring with inherited traits</returns>
public ChimeraOffspring BreedMonsters(ChimeraInstance parent1, ChimeraInstance parent2)
{
    // Implementation
}
```

### Performance Best Practices
✅ **DO:**
- Cache component references in `Awake()` or `Start()`
- Use object pooling for frequently spawned objects
- Implement proper disposal patterns for `IDisposable`
- Use ECS for performance-critical systems
- Use `StringBuilder` for string concatenation

❌ **DON'T:**
- Call `FindObjectOfType()` or `GameObject.Find()` in `Update()`
- Use `GetComponent()` repeatedly - cache the reference
- Allocate memory in `Update()` loops
- Use expensive LINQ operations in hot paths
- Leave empty `Update()` methods

### Error Handling
Always implement proper error handling:
```csharp
try
{
    var result = PerformBreeding(parent1, parent2);
    return result;
}
catch (BreedingException e)
{
    Debug.LogError($"Breeding failed: {e.Message}", this);
    // Handle breeding-specific errors
    return null;
}
catch (System.Exception e)
{
    Debug.LogError($"Unexpected error during breeding: {e.Message}", this);
    // Handle unexpected errors
    throw; // Re-throw if critical
}
```

## 🔄 Development Workflow

### 1. Before Starting Work
- Pull latest changes from `develop`
- Run **QA Automation Tool** to ensure project health
- Create feature branch: `feature/your-feature-name`

### 2. During Development
- Use **GameDev Workflow Tool** for common tasks
- Run **Performance Auditor Tool** regularly
- Write tests for new functionality
- Follow coding standards and documentation requirements

### 3. Before Committing
- Run **Performance Auditor Tool** - fix any critical issues
- Run **QA Automation Tool** - ensure all checks pass
- Verify build compiles without warnings
- Test your changes in both Editor and builds

### 4. Submitting Pull Requests
- Use our PR template (auto-populated)
- Include performance analysis from our tools
- Add screenshots/videos for visual changes
- Link related issues

## 🧪 Testing Guidelines

### Unit Tests
- Place tests in `Assets/Tests/EditMode/`
- Use Unity Test Runner conventions
- Test public APIs and critical paths
- Mock external dependencies

### Integration Tests
- Place tests in `Assets/Tests/PlayMode/`
- Test complete workflows
- Include performance benchmarks for critical systems

### Manual Testing Checklist
- [ ] Editor functionality works
- [ ] Build compiles and runs
- [ ] Performance acceptable (use Profiler)
- [ ] Multiplayer features sync correctly
- [ ] No console errors or warnings

## 🎮 Chimera-Specific Guidelines

### Monster Breeding System
- All breeding logic must be deterministic
- Support genetic trait inheritance
- Include proper validation for breeding combinations
- Performance-test with large numbers of monsters

### AI Behavior
- Use state machines or behavior trees
- Support difficulty scaling
- Ensure AI decisions are networked properly
- Profile AI performance regularly

### Visual Genetics
- Procedural generation must be reproducible
- Support genetic-based appearance changes
- Optimize shader performance for mobile targets

## 🚀 Performance Optimization

### Before Optimizing
1. **Profile First**: Use Unity Profiler to identify actual bottlenecks
2. **Use Our Tools**: Run Performance Auditor Tool for pattern detection
3. **Measure Impact**: Compare before/after metrics

### Common Optimizations
- **Component Caching**: Store references, don't repeatedly call `GetComponent()`
- **Object Pooling**: Reuse GameObjects instead of Instantiate/Destroy
- **ECS Systems**: Use DOTS for performance-critical monster AI
- **LOD Groups**: Implement for complex 3D models
- **Texture Compression**: Use appropriate formats for target platforms

### Performance Monitoring
- Run **Performance Auditor Tool** before each commit
- Include performance impact in PR descriptions
- Monitor CI/CD performance quality gates
- Profile regularly on target hardware

## 🔧 Debugging & Troubleshooting

### Common Issues
- **Build Failures**: Use QA Automation Tool to identify issues
- **Performance Problems**: Use Performance Auditor Tool for analysis
- **Missing Components**: GameDev Workflow Tool has health checks
- **Test Failures**: Check QA tool outputs for detailed reports

### Debug Tools
- **Unity Profiler**: CPU, Memory, Rendering analysis
- **Frame Debugger**: Draw call inspection
- **Console**: Monitor for errors and warnings
- **Our Custom Tools**: Comprehensive project analysis

### Getting Help
- Check tool outputs and recommendations first
- Search existing GitHub issues
- Use issue templates for bug reports
- Include tool outputs in issue descriptions

## 📊 Quality Gates

### Automatic Checks (CI/CD)
- Code compilation without warnings
- All tests pass
- Performance risk assessment
- Code quality analysis
- Security scanning

### Manual Verification
- Visual/gameplay testing
- Performance profiling
- Cross-platform testing (where applicable)
- Multiplayer functionality

### Quality Standards
- **Test Coverage**: Maintain reasonable coverage for critical paths
- **Performance**: No significant performance regressions
- **Documentation**: All public APIs documented
- **Code Quality**: Pass static analysis checks

## 🎯 Issue Reporting

### Before Creating Issues
1. **Search Existing**: Check if issue already exists
2. **Use Our Tools**: Run QA/Performance tools for analysis
3. **Gather Data**: Include profiler screenshots, tool outputs
4. **Test Reproduction**: Verify issue is reproducible

### Issue Templates
- **🐛 Bug Report**: For unexpected behavior
- **⚡ Performance Issue**: For optimization opportunities
- **✨ Feature Request**: For new functionality
- **📚 Documentation**: For documentation improvements

### Performance Issues
Always include:
- Performance Auditor Tool output
- Unity Profiler screenshots
- Specific metrics (FPS, memory usage)
- Hardware specifications
- Reproduction steps

## 🌟 Best Practices Summary

### Daily Development
1. **Start**: Pull latest, run QA tool
2. **Code**: Follow standards, document everything
3. **Test**: Run tools frequently, write tests
4. **Commit**: Performance audit, quality checks

### Code Quality
- Use meaningful names and clear structure
- Keep methods small and focused
- Handle errors gracefully
- Optimize for readability first, performance second (unless critical)

### Performance Culture
- Profile before optimizing
- Use our automated tools regularly
- Consider ECS for performance-critical systems
- Monitor CI/CD quality gates

---

## 🤝 Community

- **Respectful**: Be kind and constructive in all interactions
- **Collaborative**: Share knowledge and help others learn
- **Quality-Focused**: Maintain high standards for code and testing
- **Performance-Aware**: Consider impact on game performance

Thank you for contributing to Project Chimera! Together, we're building an amazing monster breeding experience. 🧬✨

---

*For questions about these guidelines, please open a discussion or check the existing documentation.*