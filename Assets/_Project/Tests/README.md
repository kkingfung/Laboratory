# Project Chimera - Test Suite

## Overview
Comprehensive test suite for Project Chimera's ECS-based monster breeding game.

## Test Structure

```
Tests/
├── EditMode/           # Editor-only tests (no play mode required)
│   ├── GeneticsSystemTests.cs       - Trait inheritance, breeding, fitness
│   ├── ActivitySystemTests.cs       - Activity performance calculations
│   └── *.Tests.EditMode.asmdef     - Assembly definition
│
├── PlayMode/          # Runtime tests (requires play mode)
│   └── *.Tests.PlayMode.asmdef     - Assembly definition
│
└── Performance/       # Performance regression tests
    ├── PerformanceRegressionTests.cs - Baseline metrics, regression detection
    └── *.Tests.Performance.asmdef    - Assembly definition
```

## Running Tests

### Unity Test Runner
1. Open Unity Test Runner: `Window > General > Test Runner`
2. Select `EditMode` or `PlayMode` tab
3. Click `Run All` or select specific tests

### Command Line
```bash
# Run all tests
unity -runTests -batchmode -projectPath . -testResults ./TestResults.xml

# Run specific category
unity -runTests -batchmode -testCategory EditMode -testResults ./TestResults.xml
```

## Test Categories

### Genetics System Tests
Tests the core breeding and evolution mechanics:
- ✅ Trait blending with various blend factors
- ✅ Fitness evaluation based on environmental factors
- ✅ Mating compatibility calculations
- ✅ Trait clamping to valid ranges
- ✅ Mutation effects

**Coverage:** `SIMDGeneticOptimizations` jobs

### Activity System Tests
Tests all 8 activity types:
- ✅ Racing (Speed, Stamina, Agility)
- ✅ Combat (Aggression, Size, Dominance)
- ✅ Puzzle (Intelligence, Curiosity)
- ✅ Strategy (Intelligence, Caution)
- ✅ Music (Sociability, Intelligence)
- ✅ Crafting (Intelligence, Adaptability)
- ✅ Adventure (Curiosity, Adaptability, Stamina)
- ✅ Platforming (Agility, Intelligence)

**Coverage:** `ActivityParticipationJob` performance calculations

### Performance Regression Tests
Establishes performance baselines and alerts on regressions:
- ✅ 1000 creature genetics processing (<5ms target)
- ✅ 1000 creature fitness evaluation (<10ms target)
- ✅ 1000 mating compatibility checks (<10ms target)
- ✅ Memory allocation monitoring (minimal GC)
- ✅ SIMD operation baselines

**Purpose:** Ensure Burst optimizations remain effective across updates

## Writing New Tests

### EditMode Test Template
```csharp
using NUnit.Framework;

namespace Laboratory.Tests.EditMode
{
    public class MySystemTests
    {
        [Test]
        public void MyTest_WhenCondition_ExpectedResult()
        {
            // Arrange
            var input = SetupTestData();

            // Act
            var result = SystemUnderTest(input);

            // Assert
            Assert.That(result, Is.EqualTo(expectedValue));
        }
    }
}
```

### Performance Test Template
```csharp
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace Laboratory.Tests.Performance
{
    [TestFixture]
    public class MyPerformanceTests
    {
        [Test, Performance]
        public void MySystem_1000Entities_CompletesWithinTarget()
        {
            Measure.Method(() =>
            {
                // Code to measure
            })
            .WarmupCount(5)
            .MeasurementCount(20)
            .SampleGroup("MySystem_Baseline")
            .Run();
        }
    }
}
```

## Best Practices

### DO:
✅ Test one thing per test method
✅ Use descriptive test names: `Method_WhenCondition_ExpectedResult`
✅ Dispose NativeArrays in tests
✅ Use `Allocator.Temp` for test data
✅ Measure performance with warmup iterations
✅ Test edge cases (null, zero, max values)

### DON'T:
❌ Test Unity framework itself
❌ Make tests depend on each other
❌ Use `Debug.Log` in tests (use Assert messages)
❌ Leave memory leaks in tests
❌ Write tests that take >1 second (except performance)

## Coverage Goals

### Current Coverage
- Genetics System: 80% (8/10 critical paths)
- Activity System: 100% (8/8 activity types)
- Performance Baselines: 5 established

### Target Coverage
- [ ] Breeding System: 0% → 80%
- [ ] Behavior AI: 0% → 70%
- [ ] Networking: 0% → 60%
- [ ] Save/Load: 0% → 90%
- [ ] Progression: 0% → 80%

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Run Tests
  run: |
    unity -runTests -batchmode -projectPath . -testResults ./TestResults.xml

- name: Publish Results
  uses: dorny/test-reporter@v1
  with:
    name: Unity Test Results
    path: TestResults.xml
    reporter: dotnet-nunit
```

## Performance Benchmarks

### Target Performance (with Burst)
| Test | Target | Current |
|------|--------|---------|
| 1000 Genetics | <5ms | TBD |
| 1000 Fitness | <10ms | TBD |
| 1000 Compatibility | <10ms | TBD |

Run performance tests to establish baselines!

## Troubleshooting

### "Assembly not found" errors
- Ensure test assembly references are correct in .asmdef files
- Regenerate project files: `Assets > Open C# Project`

### Tests not appearing in Test Runner
- Check `defineConstraints` includes `UNITY_INCLUDE_TESTS`
- Verify test files are in correct folders
- Reimport test assemblies

### Performance tests unstable
- Increase warmup count
- Run on dedicated hardware (not during development)
- Close other applications
- Use Release build configuration

## Contributing

When adding new systems:
1. Create corresponding test file
2. Aim for 70%+ coverage of critical paths
3. Add performance regression test if performance-critical
4. Update this README with coverage status

## Resources

- [Unity Test Framework Docs](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- [Unity Performance Testing](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@latest)
- [NUnit Documentation](https://docs.nunit.org/)
