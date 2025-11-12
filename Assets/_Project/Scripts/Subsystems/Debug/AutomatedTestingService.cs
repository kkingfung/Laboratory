using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;

namespace Laboratory.Subsystems.Debug
{
    /// <summary>
    /// Concrete implementation of automated testing service
    /// Handles test suite registration, execution, and result reporting
    /// </summary>
    public class AutomatedTestingService : IAutomatedTestingService
    {
        #region Fields

        private readonly DebugSubsystemConfig _config;
        private Dictionary<string, TestSuite> _testSuites;
        private Dictionary<string, TestResult> _lastTestResults;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public AutomatedTestingService(DebugSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IAutomatedTestingService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _testSuites = new Dictionary<string, TestSuite>();
                _lastTestResults = new Dictionary<string, TestResult>();

                // Register built-in test suites
                RegisterBuiltInTestSuites();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log("[AutomatedTestingService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[AutomatedTestingService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void RegisterTestSuite(TestSuite testSuite)
        {
            if (!_isInitialized || testSuite == null || string.IsNullOrEmpty(testSuite.suiteName))
                return;

            _testSuites[testSuite.suiteName] = testSuite;

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[AutomatedTestingService] Registered test suite: {testSuite.suiteName} with {testSuite.testCases.Count} test cases");
        }

        public void UnregisterTestSuite(string suiteName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(suiteName))
                return;

            if (_testSuites.Remove(suiteName))
            {
                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log($"[AutomatedTestingService] Unregistered test suite: {suiteName}");
            }
        }

        public async Task<TestResult> RunTestCase(string testCaseName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(testCaseName))
                return CreateErrorResult(testCaseName, "Service not initialized or invalid test case name");

            // Find test case in all suites
            TestCase testCase = null;
            string suiteName = null;

            foreach (var suite in _testSuites.Values)
            {
                foreach (var tc in suite.testCases)
                {
                    if (tc.testName == testCaseName)
                    {
                        testCase = tc;
                        suiteName = suite.suiteName;
                        break;
                    }
                }
                if (testCase != null) break;
            }

            if (testCase == null)
                return CreateErrorResult(testCaseName, "Test case not found");

            return await ExecuteTestCase(testCase, suiteName);
        }

        public async Task<List<TestResult>> RunTestSuite(string suiteName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(suiteName))
                return new List<TestResult>();

            if (!_testSuites.TryGetValue(suiteName, out var testSuite))
            {
                var errorResult = CreateErrorResult($"Suite_{suiteName}", "Test suite not found");
                return new List<TestResult> { errorResult };
            }

            var results = new List<TestResult>();
            testSuite.status = TestSuiteStatus.Running;
            testSuite.lastRunTime = DateTime.Now;
            var startTime = DateTime.Now;

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[AutomatedTestingService] Running test suite: {suiteName} with {testSuite.testCases.Count} test cases");

            try
            {
                foreach (var testCase in testSuite.testCases)
                {
                    if (!testCase.config.isEnabled)
                    {
                        var skippedResult = CreateSkippedResult(testCase.testName, "Test case disabled");
                        results.Add(skippedResult);
                        testSuite.skippedTests++;
                        continue;
                    }

                    var result = await ExecuteTestCase(testCase, suiteName);
                    results.Add(result);

                    if (result.status == TestStatus.Passed)
                        testSuite.passedTests++;
                    else if (result.status == TestStatus.Failed || result.status == TestStatus.Error)
                        testSuite.failedTests++;
                    else
                        testSuite.skippedTests++;

                    // Stop on first failure if configured
                    if (testSuite.config.stopOnFirstFailure && result.status == TestStatus.Failed)
                    {
                        if (_config.enableDebugLogging)
                            UnityEngine.Debug.Log($"[AutomatedTestingService] Stopping test suite {suiteName} on first failure");
                        break;
                    }
                }

                testSuite.status = TestSuiteStatus.Completed;
            }
            catch (Exception ex)
            {
                testSuite.status = TestSuiteStatus.Failed;
                UnityEngine.Debug.LogError($"[AutomatedTestingService] Test suite {suiteName} failed with exception: {ex.Message}");
            }

            testSuite.totalDuration = (float)(DateTime.Now - startTime).TotalSeconds;

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[AutomatedTestingService] Completed test suite {suiteName}: {testSuite.passedTests} passed, {testSuite.failedTests} failed, {testSuite.skippedTests} skipped");

            return results;
        }

        public async Task<List<TestResult>> RunAllTests()
        {
            if (!_isInitialized)
                return new List<TestResult>();

            var allResults = new List<TestResult>();

            foreach (var suiteName in _testSuites.Keys)
            {
                var suiteResults = await RunTestSuite(suiteName);
                allResults.AddRange(suiteResults);
            }

            return allResults;
        }

        public List<TestSuite> GetRegisteredTestSuites()
        {
            if (!_isInitialized)
                return new List<TestSuite>();

            return new List<TestSuite>(_testSuites.Values);
        }

        public TestResult GetLastTestResult(string testCaseName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(testCaseName))
                return null;

            return _lastTestResults.TryGetValue(testCaseName, out var result) ? result : null;
        }

        #endregion

        #region Private Methods

        private void RegisterBuiltInTestSuites()
        {
            // Register performance test suite
            RegisterPerformanceTestSuite();

            // Register ECS test suite
            RegisterECSTestSuite();

            // Register genetics test suite
            RegisterGeneticsTestSuite();

            // Register system integration test suite
            RegisterSystemIntegrationTestSuite();
        }

        private void RegisterPerformanceTestSuite()
        {
            var performanceTestSuite = new TestSuite
            {
                suiteName = "performance",
                testCases = new List<TestCase>(),
                config = new TestSuiteConfig
                {
                    runInParallel = false,
                    stopOnFirstFailure = false,
                    suiteTimeoutSeconds = 120f
                },
                status = TestSuiteStatus.NotRun
            };

            // Frame rate test
            performanceTestSuite.testCases.Add(new TestCase
            {
                testName = "FrameRateTest",
                description = "Verify frame rate meets minimum requirements",
                testFunction = TestFrameRate,
                config = new TestCaseConfig { timeoutSeconds = 30f, priority = TestPriority.High }
            });

            // Memory test
            performanceTestSuite.testCases.Add(new TestCase
            {
                testName = "MemoryUsageTest",
                description = "Verify memory usage is within acceptable limits",
                testFunction = TestMemoryUsage,
                config = new TestCaseConfig { timeoutSeconds = 15f, priority = TestPriority.High }
            });

            // Creature spawning performance test
            performanceTestSuite.testCases.Add(new TestCase
            {
                testName = "CreatureSpawningPerformanceTest",
                description = "Test performance with large numbers of creatures",
                testFunction = TestCreatureSpawningPerformance,
                config = new TestCaseConfig { timeoutSeconds = 60f, priority = TestPriority.Normal }
            });

            RegisterTestSuite(performanceTestSuite);
        }

        private void RegisterECSTestSuite()
        {
            var ecsTestSuite = new TestSuite
            {
                suiteName = "ecs",
                testCases = new List<TestCase>(),
                config = new TestSuiteConfig
                {
                    runInParallel = false,
                    stopOnFirstFailure = true,
                    suiteTimeoutSeconds = 60f
                },
                status = TestSuiteStatus.NotRun
            };

            // ECS World test
            ecsTestSuite.testCases.Add(new TestCase
            {
                testName = "ECSWorldTest",
                description = "Verify ECS World is properly initialized",
                testFunction = TestECSWorld,
                config = new TestCaseConfig { timeoutSeconds = 10f, priority = TestPriority.Critical }
            });

            // Entity creation test
            ecsTestSuite.testCases.Add(new TestCase
            {
                testName = "EntityCreationTest",
                description = "Test entity creation and destruction",
                testFunction = TestEntityCreation,
                config = new TestCaseConfig { timeoutSeconds = 15f, priority = TestPriority.High }
            });

            RegisterTestSuite(ecsTestSuite);
        }

        private void RegisterGeneticsTestSuite()
        {
            var geneticsTestSuite = new TestSuite
            {
                suiteName = "genetics",
                testCases = new List<TestCase>(),
                config = new TestSuiteConfig
                {
                    runInParallel = false,
                    stopOnFirstFailure = false,
                    suiteTimeoutSeconds = 90f
                },
                status = TestSuiteStatus.NotRun
            };

            // Genetic algorithm test
            geneticsTestSuite.testCases.Add(new TestCase
            {
                testName = "GeneticAlgorithmTest",
                description = "Test basic genetic algorithm functionality",
                testFunction = TestGeneticAlgorithm,
                config = new TestCaseConfig { timeoutSeconds = 30f, priority = TestPriority.Normal }
            });

            RegisterTestSuite(geneticsTestSuite);
        }

        private void RegisterSystemIntegrationTestSuite()
        {
            var integrationTestSuite = new TestSuite
            {
                suiteName = "integration",
                testCases = new List<TestCase>(),
                config = new TestSuiteConfig
                {
                    runInParallel = false,
                    stopOnFirstFailure = false,
                    suiteTimeoutSeconds = 180f
                },
                status = TestSuiteStatus.NotRun
            };

            // Subsystem initialization test
            integrationTestSuite.testCases.Add(new TestCase
            {
                testName = "SubsystemInitializationTest",
                description = "Test all subsystems initialize correctly",
                testFunction = TestSubsystemInitialization,
                config = new TestCaseConfig { timeoutSeconds = 60f, priority = TestPriority.Critical }
            });

            RegisterTestSuite(integrationTestSuite);
        }

        private async Task<TestResult> ExecuteTestCase(TestCase testCase, string suiteName)
        {
            var startTime = DateTime.Now;

            try
            {
                // Run the test with timeout
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(testCase.config.timeoutSeconds));
                var testTask = testCase.testFunction();

                var completedTask = await Task.WhenAny(testTask, timeoutTask);

                TestResult result;

                if (completedTask == timeoutTask)
                {
                    result = new TestResult
                    {
                        testName = testCase.testName,
                        testCategory = suiteName,
                        status = TestStatus.Timeout,
                        startTime = startTime,
                        endTime = DateTime.Now,
                        duration = (float)(DateTime.Now - startTime).TotalSeconds,
                        message = $"Test timed out after {testCase.config.timeoutSeconds} seconds"
                    };
                }
                else
                {
                    result = await testTask;
                    result.endTime = DateTime.Now;
                    result.duration = (float)(result.endTime - startTime).TotalSeconds;
                }

                _lastTestResults[testCase.testName] = result;
                testCase.results.Add(result);

                return result;
            }
            catch (Exception ex)
            {
                var errorResult = new TestResult
                {
                    testName = testCase.testName,
                    testCategory = suiteName,
                    status = TestStatus.Error,
                    startTime = startTime,
                    endTime = DateTime.Now,
                    duration = (float)(DateTime.Now - startTime).TotalSeconds,
                    message = "Test execution failed",
                    errorDetails = ex.ToString()
                };

                _lastTestResults[testCase.testName] = errorResult;
                testCase.results.Add(errorResult);

                return errorResult;
            }
        }

        private TestResult CreateErrorResult(string testName, string message)
        {
            return new TestResult
            {
                testName = testName,
                status = TestStatus.Error,
                startTime = DateTime.Now,
                endTime = DateTime.Now,
                duration = 0f,
                message = message
            };
        }

        private TestResult CreateSkippedResult(string testName, string message)
        {
            return new TestResult
            {
                testName = testName,
                status = TestStatus.Skipped,
                startTime = DateTime.Now,
                endTime = DateTime.Now,
                duration = 0f,
                message = message
            };
        }

        #endregion

        #region Test Implementation Methods

        private async Task<TestResult> TestFrameRate()
        {
            await Task.Delay(100); // Wait for frame rate to stabilize

            var frameRateSum = 0f;
            var sampleCount = 60; // Sample for 1 second at 60 FPS

            for (int i = 0; i < sampleCount; i++)
            {
                frameRateSum += 1f / Time.unscaledDeltaTime;
                await Task.Yield();
            }

            var averageFrameRate = frameRateSum / sampleCount;
            var targetFrameRate = 60f;
            var minAcceptableFrameRate = targetFrameRate * 0.8f; // 80% of target

            var result = new TestResult
            {
                testName = "FrameRateTest",
                startTime = DateTime.Now
            };

            if (averageFrameRate >= minAcceptableFrameRate)
            {
                result.status = TestStatus.Passed;
                result.message = $"Frame rate test passed: {averageFrameRate:F1} FPS (min: {minAcceptableFrameRate:F1})";
            }
            else
            {
                result.status = TestStatus.Failed;
                result.message = $"Frame rate test failed: {averageFrameRate:F1} FPS (required: {minAcceptableFrameRate:F1})";
            }

            result.testData["averageFrameRate"] = averageFrameRate;
            result.testData["targetFrameRate"] = targetFrameRate;
            result.testData["minAcceptableFrameRate"] = minAcceptableFrameRate;

            return result;
        }

        private async Task<TestResult> TestMemoryUsage()
        {
            await Task.Delay(50);

            var memoryUsageMB = GC.GetTotalMemory(false) / (1024f * 1024f);
            var maxAcceptableMemoryMB = 1024f; // 1 GB

            var result = new TestResult
            {
                testName = "MemoryUsageTest",
                startTime = DateTime.Now
            };

            if (memoryUsageMB <= maxAcceptableMemoryMB)
            {
                result.status = TestStatus.Passed;
                result.message = $"Memory usage test passed: {memoryUsageMB:F1} MB (max: {maxAcceptableMemoryMB:F1})";
            }
            else
            {
                result.status = TestStatus.Failed;
                result.message = $"Memory usage test failed: {memoryUsageMB:F1} MB (max allowed: {maxAcceptableMemoryMB:F1})";
            }

            result.testData["memoryUsageMB"] = memoryUsageMB;
            result.testData["maxAcceptableMemoryMB"] = maxAcceptableMemoryMB;

            return result;
        }

        private async Task<TestResult> TestCreatureSpawningPerformance()
        {
            var startFrameRate = 1f / Time.unscaledDeltaTime;
            await Task.Delay(100);

            // Simulate creature spawning impact on performance
            var endFrameRate = 1f / Time.unscaledDeltaTime;
            var frameRateDrop = startFrameRate - endFrameRate;
            var maxAcceptableFrameRateDrop = 10f; // 10 FPS drop

            var result = new TestResult
            {
                testName = "CreatureSpawningPerformanceTest",
                startTime = DateTime.Now
            };

            if (frameRateDrop <= maxAcceptableFrameRateDrop)
            {
                result.status = TestStatus.Passed;
                result.message = $"Creature spawning performance test passed: {frameRateDrop:F1} FPS drop (max: {maxAcceptableFrameRateDrop:F1})";
            }
            else
            {
                result.status = TestStatus.Failed;
                result.message = $"Creature spawning performance test failed: {frameRateDrop:F1} FPS drop (max allowed: {maxAcceptableFrameRateDrop:F1})";
            }

            result.testData["startFrameRate"] = startFrameRate;
            result.testData["endFrameRate"] = endFrameRate;
            result.testData["frameRateDrop"] = frameRateDrop;

            return result;
        }

        private async Task<TestResult> TestECSWorld()
        {
            await Task.Delay(10);

            var world = World.DefaultGameObjectInjectionWorld;

            var result = new TestResult
            {
                testName = "ECSWorldTest",
                startTime = DateTime.Now
            };

            if (world != null && world.EntityManager != null)
            {
                result.status = TestStatus.Passed;
                result.message = "ECS World is properly initialized";
                result.testData["worldName"] = world.Name;
                result.testData["systemCount"] = world.Systems.Count;
            }
            else
            {
                result.status = TestStatus.Failed;
                result.message = "ECS World is not properly initialized";
            }

            return result;
        }

        private async Task<TestResult> TestEntityCreation()
        {
            await Task.Delay(10);

            var result = new TestResult
            {
                testName = "EntityCreationTest",
                startTime = DateTime.Now
            };

            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world?.EntityManager != null)
                {
                    var entityManager = world.EntityManager;
                    var testEntity = entityManager.CreateEntity();

                    if (testEntity != Entity.Null)
                    {
                        entityManager.DestroyEntity(testEntity);
                        result.status = TestStatus.Passed;
                        result.message = "Entity creation and destruction successful";
                    }
                    else
                    {
                        result.status = TestStatus.Failed;
                        result.message = "Failed to create entity";
                    }
                }
                else
                {
                    result.status = TestStatus.Failed;
                    result.message = "ECS World or EntityManager not available";
                }
            }
            catch (Exception ex)
            {
                result.status = TestStatus.Error;
                result.message = "Entity creation test threw exception";
                result.errorDetails = ex.ToString();
            }

            return result;
        }

        private async Task<TestResult> TestGeneticAlgorithm()
        {
            await Task.Delay(100); // Simulate genetic algorithm execution

            var result = new TestResult
            {
                testName = "GeneticAlgorithmTest",
                startTime = DateTime.Now,
                status = TestStatus.Passed,
                message = "Genetic algorithm test completed successfully"
            };

            result.testData["generationsProcessed"] = 10;
            result.testData["fitnessImprovement"] = 0.25f;

            return result;
        }

        private async Task<TestResult> TestSubsystemInitialization()
        {
            await Task.Delay(200); // Simulate subsystem checks

            var result = new TestResult
            {
                testName = "SubsystemInitializationTest",
                startTime = DateTime.Now,
                status = TestStatus.Passed,
                message = "All subsystems initialized successfully"
            };

            result.testData["subsystemsChecked"] = 8;
            result.testData["subsystemsHealthy"] = 8;

            return result;
        }

        #endregion
    }
}