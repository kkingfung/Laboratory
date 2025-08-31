# Core Architecture Testing Documentation

## Overview

This document describes the comprehensive testing framework for the Core Architecture subsystem of the Laboratory Unity project. The testing framework ensures reliability, performance, and maintainability of the core systems.

## Test Structure

```
Core/Tests/
├── Unit/                           # Unit Tests
│   ├── ServiceContainerTests.cs   # Dependency Injection Tests
│   ├── UnifiedEventBusTests.cs    # Event System Tests
│   └── GameStateServiceTests.cs   # State Management Tests
├── Integration/                    # Integration Tests
│   └── BootstrapIntegrationTests.cs
└── Laboratory.Core.Tests.asmdef   # Assembly Definition
```

## Test Categories

### 🔧 Unit Tests

#### ServiceContainerTests.cs
- **Coverage**: Dependency injection container functionality
- **Key Tests**:
  - Service registration (Interface, Concrete, Instance, Factory)
  - Service resolution (Success, Failure, Try methods)
  - Lifetime management (Singleton, Transient, Scoped)
  - Constructor injection with dependencies
  - Circular dependency detection
  - Proper disposal of services

#### UnifiedEventBusTests.cs
- **Coverage**: Event system functionality
- **Key Tests**:
  - Event publishing and subscription
  - Advanced subscription patterns (filtering, first-only, main thread)
  - Observable patterns integration
  - Subscriber count tracking
  - Event type management
  - Performance under load
  - Proper disposal and cleanup

#### GameStateServiceTests.cs
- **Coverage**: Game state management
- **Key Tests**:
  - State registration and transitions
  - Event publishing on state changes
  - Remote state synchronization
  - Transition validation rules
  - State implementation lifecycle
  - Error handling and recovery

### 🔗 Integration Tests

#### BootstrapIntegrationTests.cs
- **Coverage**: Complete system initialization
- **Key Tests**:
  - Full bootstrap process
  - Service container integration
  - Inter-service communication
  - Performance benchmarks
  - Resource cleanup
  - Error handling and recovery

## Running Tests

### Unity Test Runner

1. Open **Window > General > Test Runner**
2. Select **PlayMode** or **EditMode** tab
3. Click **Run All** or select specific test categories

### Command Line

```bash
# Run all tests
Unity.exe -runTests -batchmode -projectPath /path/to/project

# Run specific test assembly
Unity.exe -runTests -testPlatform editmode -testResults results.xml -testFilter Laboratory.Core.Tests
```

### Continuous Integration

Tests are designed to run in CI/CD pipelines with proper exit codes and XML results output.

## Test Configuration

### Assembly Definition

The test assembly (`Laboratory.Core.Tests.asmdef`) includes:
- **Runtime Dependencies**: UniRx, UniTask, Netcode
- **Test Framework**: NUnit, Unity Test Framework
- **Editor Only**: Tests run in Unity Editor environment
- **Test Runner Integration**: Automatic discovery and execution

### Mock Services

Tests use mock implementations for external dependencies:
- `MockMainMenuState` - State implementation for testing
- `MockPlayingState` - Alternative state for transition testing  
- `MockRestrictiveState` - State with limited transition rules
- `MockFailingState` - State that throws exceptions for error testing

## Test Data and Scenarios

### Performance Benchmarks

| Test | Target Performance | Measured Metric |
|------|-------------------|-----------------|
| Bootstrap Initialization | < 5 seconds | Total startup time |
| 1000 Service Resolutions | < 1 second | Resolution speed |
| 100 Event Processing | < 1 second | Event throughput |
| Memory Usage | < 10MB increase | Memory efficiency |

### Error Scenarios

Tests cover various error conditions:
- **Null Dependencies**: Services with null parameters
- **Circular Dependencies**: Invalid dependency graphs  
- **Missing Services**: Unregistered service resolution
- **State Transition Errors**: Invalid state changes
- **Event Handler Exceptions**: Faulty event subscribers
- **Cancellation Handling**: Proper async cancellation

## Coverage Goals

### Target Coverage Metrics

- **Line Coverage**: > 85%
- **Branch Coverage**: > 80%  
- **Method Coverage**: > 90%
- **Class Coverage**: > 95%

### Coverage Analysis

Use Unity's Code Coverage package:

1. Install **Code Coverage** package
2. Enable coverage recording
3. Run tests with coverage
4. Generate HTML reports

```csharp
// Example coverage annotation
[Test]
[Category("Critical")]
public void ServiceContainer_ResolveDependency_ShouldInjectCorrectly()
{
    // Test ensures critical dependency injection path is covered
}
```

## Test Best Practices

### 🎯 Test Design

1. **AAA Pattern**: Arrange, Act, Assert
2. **Single Responsibility**: One concept per test
3. **Descriptive Names**: Test names explain what's being tested
4. **Independent Tests**: No dependencies between tests
5. **Deterministic**: Tests produce consistent results

### ⚡ Performance Testing

1. **Baseline Measurements**: Establish performance baselines
2. **Resource Cleanup**: Proper disposal in teardown
3. **Memory Monitoring**: Track memory usage patterns
4. **Timeout Handling**: Prevent hanging tests

### 🔄 Integration Testing

1. **Real Dependencies**: Use actual implementations where possible
2. **Async Patterns**: Proper async/await testing with UniTask
3. **Unity Lifecycle**: Account for Unity's execution model
4. **Cross-System Validation**: Test system interactions

## Debugging Tests

### Common Issues

1. **Timing Issues**: Use `WaitForSeconds` or `WaitUntil` for async operations
2. **Unity Objects**: Properly create/destroy GameObjects in tests
3. **Static State**: Clean up static state between tests
4. **Service Dependencies**: Ensure proper service registration order

### Debug Techniques

```csharp
[Test]
public void DebugExample()
{
    // Use Debug.Log for test debugging
    Debug.Log($"Service count: {serviceContainer.GetRegisteredServiceCount()}");
    
    // Use Assert messages for better failure information
    Assert.That(result, Is.True, "Service should initialize successfully");
    
    // Use TestContext for additional information
    TestContext.WriteLine($"Test completed in {stopwatch.ElapsedMilliseconds}ms");
}
```

## Continuous Testing

### Pre-Commit Hooks

Run critical tests before code commits:

```bash
#!/bin/bash
# pre-commit hook
echo "Running core architecture tests..."
Unity.exe -runTests -testFilter "Laboratory.Core.Tests.Unit" -batchmode
if [ $? -ne 0 ]; then
    echo "Tests failed! Commit aborted."
    exit 1
fi
```

### CI/CD Pipeline

```yaml
# Example CI configuration
test-core-architecture:
  runs-on: unity-runner
  steps:
    - name: Run Unit Tests
      run: Unity.exe -runTests -testPlatform editmode -testResults unit-results.xml
    - name: Run Integration Tests  
      run: Unity.exe -runTests -testPlatform playmode -testResults integration-results.xml
    - name: Generate Coverage Report
      run: ReportGenerator -reports:coverage.xml -targetdir:coverage-report
```

## Test Maintenance

### Regular Updates

- **Test Reviews**: Monthly review of test coverage and effectiveness
- **Performance Updates**: Update benchmarks as system improves
- **New Feature Tests**: Add tests for new functionality
- **Legacy Cleanup**: Remove obsolete tests

### Test Metrics

Track test suite health:
- **Execution Time**: Total test suite runtime
- **Flaky Tests**: Tests with inconsistent results  
- **Coverage Trends**: Coverage percentage over time
- **Performance Trends**: Benchmark results over time

---

*This testing framework ensures the Core Architecture subsystem maintains high quality and reliability as the Laboratory project evolves.*
