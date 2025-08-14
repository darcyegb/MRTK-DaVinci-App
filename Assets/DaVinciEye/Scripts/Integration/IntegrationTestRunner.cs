using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using DaVinciEye.Integration;

namespace DaVinciEye.Tests.Integration
{
    /// <summary>
    /// Comprehensive integration test runner that orchestrates all system integration tests
    /// Provides automated testing of complete Da Vinci Eye workflows
    /// </summary>
    public class IntegrationTestRunner : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runPerformanceTests = true;
        [SerializeField] private bool runWorkflowTests = true;
        [SerializeField] private bool runStressTests = false; // Disabled by default
        [SerializeField] private float testTimeout = 30f;

        [Header("Test Assets")]
        [SerializeField] private List<string> testImagePaths = new List<string>
        {
            "TestAssets/test_image_small.jpg",
            "TestAssets/test_image_medium.jpg",
            "TestAssets/test_image_large.jpg"
        };

        private SystemIntegrationManager integrationManager;
        private TestResults testResults = new TestResults();

        public static IntegrationTestRunner Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Run all integration tests programmatically
        /// </summary>
        public IEnumerator RunAllIntegrationTests()
        {
            Debug.Log("[IntegrationTestRunner] Starting comprehensive integration tests...");
            testResults.Reset();

            // Initialize integration manager
            yield return StartCoroutine(InitializeTestEnvironment());

            // Run workflow tests
            if (runWorkflowTests)
            {
                yield return StartCoroutine(RunWorkflowTests());
            }

            // Run performance tests
            if (runPerformanceTests)
            {
                yield return StartCoroutine(RunPerformanceTests());
            }

            // Run stress tests (if enabled)
            if (runStressTests)
            {
                yield return StartCoroutine(RunStressTests());
            }

            // Generate test report
            GenerateTestReport();

            Debug.Log("[IntegrationTestRunner] All integration tests completed");
        }

        private IEnumerator InitializeTestEnvironment()
        {
            Debug.Log("[IntegrationTestRunner] Initializing test environment...");

            // Create integration manager if not exists
            if (integrationManager == null)
            {
                var managerGO = new GameObject("TestIntegrationManager");
                integrationManager = managerGO.AddComponent<SystemIntegrationManager>();
            }

            // Wait for all systems to initialize
            float timeout = testTimeout;
            float elapsed = 0f;

            while (!integrationManager.AreAllSystemsReady() && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (!integrationManager.AreAllSystemsReady())
            {
                testResults.AddFailure("System Initialization", "Systems failed to initialize within timeout");
                yield break;
            }

            testResults.AddSuccess("System Initialization", "All systems initialized successfully");
            Debug.Log("[IntegrationTestRunner] Test environment initialized");
        }

        private IEnumerator RunWorkflowTests()
        {
            Debug.Log("[IntegrationTestRunner] Running workflow tests...");

            // Test 1: Canvas Definition Workflow
            yield return StartCoroutine(TestCanvasDefinitionWorkflow());

            // Test 2: Image Overlay Workflow
            yield return StartCoroutine(TestImageOverlayWorkflow());

            // Test 3: Filter Application Workflow
            yield return StartCoroutine(TestFilterWorkflow());

            // Test 4: Color Analysis Workflow
            yield return StartCoroutine(TestColorAnalysisWorkflow());

            // Test 5: Complete Artist Workflow
            yield return StartCoroutine(TestCompleteArtistWorkflow());

            // Test 6: Session Management Workflow
            yield return StartCoroutine(TestSessionManagementWorkflow());
        }

        private IEnumerator TestCanvasDefinitionWorkflow()
        {
            Debug.Log("[IntegrationTestRunner] Testing canvas definition workflow...");

            try
            {
                var task = integrationManager.ExecuteCanvasDefinitionWorkflow();
                float elapsed = 0f;

                while (!task.IsCompleted && elapsed < testTimeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }

                if (task.IsCompleted && task.Result)
                {
                    testResults.AddSuccess("Canvas Definition Workflow", "Canvas definition completed successfully");
                }
                else
                {
                    testResults.AddFailure("Canvas Definition Workflow", "Canvas definition failed or timed out");
                }
            }
            catch (Exception e)
            {
                testResults.AddFailure("Canvas Definition Workflow", $"Exception: {e.Message}");
            }
        }

        private IEnumerator TestImageOverlayWorkflow()
        {
            Debug.Log("[IntegrationTestRunner] Testing image overlay workflow...");

            foreach (string imagePath in testImagePaths)
            {
                try
                {
                    var task = integrationManager.ExecuteImageOverlayWorkflow(imagePath);
                    float elapsed = 0f;

                    while (!task.IsCompleted && elapsed < testTimeout)
                    {
                        yield return new WaitForSeconds(0.1f);
                        elapsed += 0.1f;
                    }

                    if (task.IsCompleted && task.Result)
                    {
                        testResults.AddSuccess($"Image Overlay - {imagePath}", "Image overlay completed successfully");
                    }
                    else
                    {
                        testResults.AddFailure($"Image Overlay - {imagePath}", "Image overlay failed or timed out");
                    }
                }
                catch (Exception e)
                {
                    testResults.AddFailure($"Image Overlay - {imagePath}", $"Exception: {e.Message}");
                }

                yield return new WaitForSeconds(0.5f); // Brief pause between tests
            }
        }

        private IEnumerator TestFilterWorkflow()
        {
            Debug.Log("[IntegrationTestRunner] Testing filter workflow...");

            var filterManager = FindObjectOfType<Filters.FilterManager>();
            if (filterManager == null)
            {
                testResults.AddFailure("Filter Workflow", "Filter manager not found");
                yield break;
            }

            try
            {
                // Test each filter type
                var filterTypes = new[]
                {
                    Filters.FilterType.Grayscale,
                    Filters.FilterType.ContrastEnhancement,
                    Filters.FilterType.EdgeDetection,
                    Filters.FilterType.ColorRange,
                    Filters.FilterType.ColorReduction
                };

                foreach (var filterType in filterTypes)
                {
                    var filterParams = new Filters.FilterParameters
                    {
                        type = filterType,
                        intensity = 0.5f
                    };

                    filterManager.ApplyFilter(filterParams.type, filterParams);
                    yield return new WaitForSeconds(0.2f); // Allow filter to process

                    // Verify filter was applied
                    var activeFilters = filterManager.GetActiveFilters();
                    bool filterApplied = false;
                    foreach (var filter in activeFilters)
                    {
                        if (filter.type == filterType)
                        {
                            filterApplied = true;
                            break;
                        }
                    }

                    if (filterApplied)
                    {
                        testResults.AddSuccess($"Filter - {filterType}", "Filter applied successfully");
                    }
                    else
                    {
                        testResults.AddFailure($"Filter - {filterType}", "Filter was not applied");
                    }

                    // Remove filter
                    filterManager.RemoveFilter(filterType);
                    yield return new WaitForSeconds(0.1f);
                }
            }
            catch (Exception e)
            {
                testResults.AddFailure("Filter Workflow", $"Exception: {e.Message}");
            }
        }

        private IEnumerator TestColorAnalysisWorkflow()
        {
            Debug.Log("[IntegrationTestRunner] Testing color analysis workflow...");

            try
            {
                Vector2 imageCoordinate = new Vector2(0.5f, 0.5f);
                Vector3 paintPosition = new Vector3(0, 0, 1);

                var task = integrationManager.ExecuteColorMatchingWorkflow(imageCoordinate, paintPosition);
                float elapsed = 0f;

                while (!task.IsCompleted && elapsed < testTimeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }

                if (task.IsCompleted && task.Result)
                {
                    testResults.AddSuccess("Color Analysis Workflow", "Color matching completed successfully");
                }
                else
                {
                    testResults.AddFailure("Color Analysis Workflow", "Color matching failed or timed out");
                }
            }
            catch (Exception e)
            {
                testResults.AddFailure("Color Analysis Workflow", $"Exception: {e.Message}");
            }
        }

        private IEnumerator TestCompleteArtistWorkflow()
        {
            Debug.Log("[IntegrationTestRunner] Testing complete artist workflow...");

            try
            {
                string testImage = testImagePaths.Count > 0 ? testImagePaths[0] : "TestAssets/default.jpg";
                var task = integrationManager.ExecuteCompleteArtistWorkflow(testImage);
                float elapsed = 0f;

                while (!task.IsCompleted && elapsed < testTimeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }

                if (task.IsCompleted && task.Result)
                {
                    testResults.AddSuccess("Complete Artist Workflow", "Complete workflow executed successfully");
                }
                else
                {
                    testResults.AddFailure("Complete Artist Workflow", "Complete workflow failed or timed out");
                }
            }
            catch (Exception e)
            {
                testResults.AddFailure("Complete Artist Workflow", $"Exception: {e.Message}");
            }
        }

        private IEnumerator TestSessionManagementWorkflow()
        {
            Debug.Log("[IntegrationTestRunner] Testing session management workflow...");

            var sessionManager = FindObjectOfType<SessionManagement.SessionDataManager>();
            if (sessionManager == null)
            {
                testResults.AddFailure("Session Management", "Session manager not found");
                yield break;
            }

            try
            {
                // Test session save
                sessionManager.SaveSession();
                yield return new WaitForSeconds(0.5f);

                // Test session load
                sessionManager.LoadSession();
                yield return new WaitForSeconds(0.5f);

                testResults.AddSuccess("Session Management Workflow", "Session save/load completed successfully");
            }
            catch (Exception e)
            {
                testResults.AddFailure("Session Management Workflow", $"Exception: {e.Message}");
            }
        }

        private IEnumerator RunPerformanceTests()
        {
            Debug.Log("[IntegrationTestRunner] Running performance tests...");

            // Test 1: Baseline Performance
            yield return StartCoroutine(TestBaselinePerformance());

            // Test 2: Image Overlay Performance
            yield return StartCoroutine(TestImageOverlayPerformance());

            // Test 3: Filter Performance
            yield return StartCoroutine(TestFilterPerformance());

            // Test 4: Memory Usage
            yield return StartCoroutine(TestMemoryUsage());

            // Test 5: UI Response Time
            yield return StartCoroutine(TestUIResponseTime());
        }

        private IEnumerator TestBaselinePerformance()
        {
            Debug.Log("[IntegrationTestRunner] Testing baseline performance...");

            try
            {
                List<float> fpsReadings = new List<float>();
                float testDuration = 3f;
                float startTime = Time.realtimeSinceStartup;

                while (Time.realtimeSinceStartup - startTime < testDuration)
                {
                    float currentFPS = 1.0f / Time.unscaledDeltaTime;
                    fpsReadings.Add(currentFPS);
                    yield return new WaitForSeconds(0.1f);
                }

                float averageFPS = 0f;
                foreach (float fps in fpsReadings)
                {
                    averageFPS += fps;
                }
                averageFPS /= fpsReadings.Count;

                if (averageFPS >= 55f) // 10% tolerance below 60 FPS target
                {
                    testResults.AddSuccess("Baseline Performance", $"Average FPS: {averageFPS:F1}");
                }
                else
                {
                    testResults.AddFailure("Baseline Performance", $"FPS below target: {averageFPS:F1}");
                }
            }
            catch (Exception e)
            {
                testResults.AddFailure("Baseline Performance", $"Exception: {e.Message}");
            }
        }

        private IEnumerator TestImageOverlayPerformance()
        {
            Debug.Log("[IntegrationTestRunner] Testing image overlay performance...");

            try
            {
                // Setup image overlay
                var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
                while (!canvasTask.IsCompleted) yield return new WaitForSeconds(0.1f);

                var imageTask = integrationManager.ExecuteImageOverlayWorkflow(testImagePaths[0]);
                while (!imageTask.IsCompleted) yield return new WaitForSeconds(0.1f);

                // Monitor performance
                List<float> fpsReadings = new List<float>();
                float testDuration = 3f;
                float startTime = Time.realtimeSinceStartup;

                while (Time.realtimeSinceStartup - startTime < testDuration)
                {
                    float currentFPS = 1.0f / Time.unscaledDeltaTime;
                    fpsReadings.Add(currentFPS);
                    yield return new WaitForSeconds(0.1f);
                }

                float averageFPS = 0f;
                foreach (float fps in fpsReadings)
                {
                    averageFPS += fps;
                }
                averageFPS /= fpsReadings.Count;

                if (averageFPS >= 55f)
                {
                    testResults.AddSuccess("Image Overlay Performance", $"Average FPS: {averageFPS:F1}");
                }
                else
                {
                    testResults.AddFailure("Image Overlay Performance", $"FPS below target: {averageFPS:F1}");
                }
            }
            catch (Exception e)
            {
                testResults.AddFailure("Image Overlay Performance", $"Exception: {e.Message}");
            }
        }

        private IEnumerator TestFilterPerformance()
        {
            Debug.Log("[IntegrationTestRunner] Testing filter performance...");

            try
            {
                var filterManager = FindObjectOfType<Filters.FilterManager>();
                if (filterManager == null)
                {
                    testResults.AddFailure("Filter Performance", "Filter manager not found");
                    yield break;
                }

                // Apply multiple filters
                var filters = new[]
                {
                    Filters.FilterType.Grayscale,
                    Filters.FilterType.ContrastEnhancement,
                    Filters.FilterType.EdgeDetection
                };

                foreach (var filterType in filters)
                {
                    var filterParams = new Filters.FilterParameters
                    {
                        type = filterType,
                        intensity = 0.5f
                    };
                    filterManager.ApplyFilter(filterParams.type, filterParams);
                }

                // Monitor performance with filters active
                List<float> fpsReadings = new List<float>();
                float testDuration = 3f;
                float startTime = Time.realtimeSinceStartup;

                while (Time.realtimeSinceStartup - startTime < testDuration)
                {
                    float currentFPS = 1.0f / Time.unscaledDeltaTime;
                    fpsReadings.Add(currentFPS);
                    yield return new WaitForSeconds(0.1f);
                }

                float averageFPS = 0f;
                foreach (float fps in fpsReadings)
                {
                    averageFPS += fps;
                }
                averageFPS /= fpsReadings.Count;

                if (averageFPS >= 55f)
                {
                    testResults.AddSuccess("Filter Performance", $"Average FPS with 3 filters: {averageFPS:F1}");
                }
                else
                {
                    testResults.AddFailure("Filter Performance", $"FPS below target with filters: {averageFPS:F1}");
                }
            }
            catch (Exception e)
            {
                testResults.AddFailure("Filter Performance", $"Exception: {e.Message}");
            }
        }

        private IEnumerator TestMemoryUsage()
        {
            Debug.Log("[IntegrationTestRunner] Testing memory usage...");

            try
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                long initialMemory = System.GC.GetTotalMemory(false);

                // Perform memory-intensive operations
                var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
                while (!canvasTask.IsCompleted) yield return new WaitForSeconds(0.1f);

                var imageTask = integrationManager.ExecuteCompleteArtistWorkflow(testImagePaths[0]);
                while (!imageTask.IsCompleted) yield return new WaitForSeconds(0.1f);

                yield return new WaitForSeconds(2f); // Allow operations to complete

                long currentMemory = System.GC.GetTotalMemory(false);
                long memoryUsage = currentMemory - initialMemory;
                long maxMemoryLimit = 512 * 1024 * 1024; // 512MB

                if (currentMemory <= maxMemoryLimit)
                {
                    testResults.AddSuccess("Memory Usage", $"Memory usage: {currentMemory / (1024 * 1024)}MB");
                }
                else
                {
                    testResults.AddFailure("Memory Usage", $"Memory usage exceeds limit: {currentMemory / (1024 * 1024)}MB");
                }
            }
            catch (Exception e)
            {
                testResults.AddFailure("Memory Usage", $"Exception: {e.Message}");
            }
        }

        private IEnumerator TestUIResponseTime()
        {
            Debug.Log("[IntegrationTestRunner] Testing UI response time...");

            try
            {
                var imageOverlay = FindObjectOfType<ImageOverlay.ImageOverlayManager>();
                if (imageOverlay == null)
                {
                    testResults.AddFailure("UI Response Time", "Image overlay manager not found");
                    yield break;
                }

                List<float> responseTimes = new List<float>();

                for (int i = 0; i < 10; i++)
                {
                    float startTime = Time.realtimeSinceStartup;
                    
                    // Simulate UI interaction
                    imageOverlay.SetOpacity(UnityEngine.Random.Range(0.1f, 1.0f));
                    
                    yield return new WaitForEndOfFrame();
                    
                    float responseTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to ms
                    responseTimes.Add(responseTime);
                    
                    yield return new WaitForSeconds(0.1f);
                }

                float averageResponseTime = 0f;
                foreach (float time in responseTimes)
                {
                    averageResponseTime += time;
                }
                averageResponseTime /= responseTimes.Count;

                if (averageResponseTime <= 100f) // 100ms target
                {
                    testResults.AddSuccess("UI Response Time", $"Average response time: {averageResponseTime:F1}ms");
                }
                else
                {
                    testResults.AddFailure("UI Response Time", $"Response time exceeds target: {averageResponseTime:F1}ms");
                }
            }
            catch (Exception e)
            {
                testResults.AddFailure("UI Response Time", $"Exception: {e.Message}");
            }
        }

        private IEnumerator RunStressTests()
        {
            Debug.Log("[IntegrationTestRunner] Running stress tests...");

            // Extended session simulation
            yield return StartCoroutine(TestExtendedSession());

            // Rapid workflow switching
            yield return StartCoroutine(TestRapidWorkflowSwitching());

            // Maximum concurrent operations
            yield return StartCoroutine(TestMaximumLoad());
        }

        private IEnumerator TestExtendedSession()
        {
            Debug.Log("[IntegrationTestRunner] Testing extended session...");

            try
            {
                float sessionDuration = 10f; // 10 seconds (represents extended session)
                float startTime = Time.realtimeSinceStartup;
                
                var workflowTask = integrationManager.ExecuteCompleteArtistWorkflow(testImagePaths[0]);
                while (!workflowTask.IsCompleted) yield return new WaitForSeconds(0.1f);

                List<float> performanceHistory = new List<float>();

                while (Time.realtimeSinceStartup - startTime < sessionDuration)
                {
                    float currentFPS = 1.0f / Time.unscaledDeltaTime;
                    performanceHistory.Add(currentFPS);
                    
                    // Simulate user interactions
                    var imageOverlay = FindObjectOfType<ImageOverlay.ImageOverlayManager>();
                    if (imageOverlay != null)
                    {
                        imageOverlay.SetOpacity(UnityEngine.Random.Range(0.3f, 1.0f));
                    }
                    
                    yield return new WaitForSeconds(0.5f);
                }

                // Check performance stability
                float averageFPS = 0f;
                foreach (float fps in performanceHistory)
                {
                    averageFPS += fps;
                }
                averageFPS /= performanceHistory.Count;

                if (averageFPS >= 50f) // Slightly lower threshold for extended session
                {
                    testResults.AddSuccess("Extended Session", $"Stable performance over {sessionDuration}s: {averageFPS:F1} FPS");
                }
                else
                {
                    testResults.AddFailure("Extended Session", $"Performance degraded during extended session: {averageFPS:F1} FPS");
                }
            }
            catch (Exception e)
            {
                testResults.AddFailure("Extended Session", $"Exception: {e.Message}");
            }
        }

        private IEnumerator TestRapidWorkflowSwitching()
        {
            Debug.Log("[IntegrationTestRunner] Testing rapid workflow switching...");

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    // Rapid switching between workflows
                    var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
                    yield return new WaitForSeconds(0.5f);

                    var imageTask = integrationManager.ExecuteImageOverlayWorkflow(testImagePaths[i % testImagePaths.Count]);
                    yield return new WaitForSeconds(0.5f);

                    integrationManager.ResetAllSystems();
                    yield return new WaitForSeconds(0.2f);
                }

                testResults.AddSuccess("Rapid Workflow Switching", "Successfully handled rapid workflow changes");
            }
            catch (Exception e)
            {
                testResults.AddFailure("Rapid Workflow Switching", $"Exception: {e.Message}");
            }
        }

        private IEnumerator TestMaximumLoad()
        {
            Debug.Log("[IntegrationTestRunner] Testing maximum load...");

            try
            {
                // Apply maximum load: all systems active with heavy operations
                var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
                while (!canvasTask.IsCompleted) yield return new WaitForSeconds(0.1f);

                var imageTask = integrationManager.ExecuteImageOverlayWorkflow(testImagePaths[0]);
                while (!imageTask.IsCompleted) yield return new WaitForSeconds(0.1f);

                // Apply all filters
                var filterManager = FindObjectOfType<Filters.FilterManager>();
                if (filterManager != null)
                {
                    var allFilters = new[]
                    {
                        Filters.FilterType.Grayscale,
                        Filters.FilterType.ContrastEnhancement,
                        Filters.FilterType.EdgeDetection,
                        Filters.FilterType.ColorRange,
                        Filters.FilterType.ColorReduction
                    };

                    foreach (var filterType in allFilters)
                    {
                        var filterParams = new Filters.FilterParameters
                        {
                            type = filterType,
                            intensity = 1.0f
                        };
                        filterManager.ApplyFilter(filterParams.type, filterParams);
                    }
                }

                // Monitor performance under maximum load
                float testDuration = 3f;
                float startTime = Time.realtimeSinceStartup;
                List<float> fpsReadings = new List<float>();

                while (Time.realtimeSinceStartup - startTime < testDuration)
                {
                    float currentFPS = 1.0f / Time.unscaledDeltaTime;
                    fpsReadings.Add(currentFPS);
                    yield return new WaitForSeconds(0.1f);
                }

                float averageFPS = 0f;
                foreach (float fps in fpsReadings)
                {
                    averageFPS += fps;
                }
                averageFPS /= fpsReadings.Count;

                // More lenient threshold for maximum load test
                if (averageFPS >= 40f)
                {
                    testResults.AddSuccess("Maximum Load", $"Handled maximum load: {averageFPS:F1} FPS");
                }
                else
                {
                    testResults.AddFailure("Maximum Load", $"Performance too low under maximum load: {averageFPS:F1} FPS");
                }
            }
            catch (Exception e)
            {
                testResults.AddFailure("Maximum Load", $"Exception: {e.Message}");
            }
        }

        private void GenerateTestReport()
        {
            Debug.Log("[IntegrationTestRunner] Generating test report...");

            string report = testResults.GenerateReport();
            Debug.Log(report);

            // Save report to file
            string reportPath = Application.persistentDataPath + "/DaVinciEye_IntegrationTestReport.txt";
            try
            {
                System.IO.File.WriteAllText(reportPath, report);
                Debug.Log($"[IntegrationTestRunner] Test report saved to: {reportPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[IntegrationTestRunner] Failed to save test report: {e.Message}");
            }
        }

        /// <summary>
        /// Public method to run tests from external scripts or UI
        /// </summary>
        public void StartIntegrationTests()
        {
            StartCoroutine(RunAllIntegrationTests());
        }
    }

    /// <summary>
    /// Test results tracking and reporting
    /// </summary>
    [System.Serializable]
    public class TestResults
    {
        public List<TestResult> results = new List<TestResult>();
        public DateTime testStartTime;
        public DateTime testEndTime;

        public void Reset()
        {
            results.Clear();
            testStartTime = DateTime.Now;
        }

        public void AddSuccess(string testName, string details)
        {
            results.Add(new TestResult
            {
                testName = testName,
                passed = true,
                details = details,
                timestamp = DateTime.Now
            });
        }

        public void AddFailure(string testName, string details)
        {
            results.Add(new TestResult
            {
                testName = testName,
                passed = false,
                details = details,
                timestamp = DateTime.Now
            });
        }

        public string GenerateReport()
        {
            testEndTime = DateTime.Now;
            var duration = testEndTime - testStartTime;

            int passedTests = 0;
            int failedTests = 0;

            foreach (var result in results)
            {
                if (result.passed) passedTests++;
                else failedTests++;
            }

            var report = new System.Text.StringBuilder();
            report.AppendLine("=== DA VINCI EYE INTEGRATION TEST REPORT ===");
            report.AppendLine($"Test Date: {testStartTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Duration: {duration.TotalSeconds:F1} seconds");
            report.AppendLine($"Total Tests: {results.Count}");
            report.AppendLine($"Passed: {passedTests}");
            report.AppendLine($"Failed: {failedTests}");
            report.AppendLine($"Success Rate: {(passedTests * 100.0 / results.Count):F1}%");
            report.AppendLine();

            report.AppendLine("=== TEST RESULTS ===");
            foreach (var result in results)
            {
                string status = result.passed ? "PASS" : "FAIL";
                report.AppendLine($"[{status}] {result.testName}");
                report.AppendLine($"  Details: {result.details}");
                report.AppendLine($"  Time: {result.timestamp:HH:mm:ss}");
                report.AppendLine();
            }

            return report.ToString();
        }
    }

    [System.Serializable]
    public class TestResult
    {
        public string testName;
        public bool passed;
        public string details;
        public DateTime timestamp;
    }
}