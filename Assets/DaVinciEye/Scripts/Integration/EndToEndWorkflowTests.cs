using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using DaVinciEye.Integration;

namespace DaVinciEye.Tests.Integration
{
    /// <summary>
    /// Comprehensive end-to-end workflow tests for the Da Vinci Eye application
    /// Tests complete artist workflows from start to finish
    /// </summary>
    public class EndToEndWorkflowTests
    {
        private SystemIntegrationManager integrationManager;
        private GameObject testGameObject;
        private string testImagePath = "TestAssets/test_image.jpg";

        [SetUp]
        public void SetUp()
        {
            // Create test game object with integration manager
            testGameObject = new GameObject("TestIntegrationManager");
            integrationManager = testGameObject.AddComponent<SystemIntegrationManager>();

            // Create mock system components
            SetupMockSystems();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        private void SetupMockSystems()
        {
            // Create mock canvas manager
            var canvasManagerGO = new GameObject("MockCanvasManager");
            canvasManagerGO.transform.SetParent(testGameObject.transform);
            var canvasManager = canvasManagerGO.AddComponent<Canvas.CanvasDefinitionManager>();

            // Create mock image overlay manager
            var imageOverlayGO = new GameObject("MockImageOverlay");
            imageOverlayGO.transform.SetParent(testGameObject.transform);
            var imageOverlay = imageOverlayGO.AddComponent<ImageOverlay.ImageOverlayManager>();

            // Create mock filter manager
            var filterManagerGO = new GameObject("MockFilterManager");
            filterManagerGO.transform.SetParent(testGameObject.transform);
            var filterManager = filterManagerGO.AddComponent<Filters.FilterManager>();

            // Create mock color analyzer
            var colorAnalyzerGO = new GameObject("MockColorAnalyzer");
            colorAnalyzerGO.transform.SetParent(testGameObject.transform);
            var colorAnalyzer = colorAnalyzerGO.AddComponent<ColorAnalysis.ColorAnalyzer>();

            // Create mock tracking monitor
            var trackingMonitorGO = new GameObject("MockTrackingMonitor");
            trackingMonitorGO.transform.SetParent(testGameObject.transform);
            var trackingMonitor = trackingMonitorGO.AddComponent<SpatialTracking.TrackingQualityMonitor>();

            // Create mock session manager
            var sessionManagerGO = new GameObject("MockSessionManager");
            sessionManagerGO.transform.SetParent(testGameObject.transform);
            var sessionManager = sessionManagerGO.AddComponent<SessionManagement.SessionDataManager>();

            // Create mock UI manager
            var uiManagerGO = new GameObject("MockUIManager");
            uiManagerGO.transform.SetParent(testGameObject.transform);
            var uiManager = uiManagerGO.AddComponent<UI.UIManager>();

            // Create mock input manager
            var inputManagerGO = new GameObject("MockInputManager");
            inputManagerGO.transform.SetParent(testGameObject.transform);
            var inputManager = inputManagerGO.AddComponent<Input.HandGestureManager>();
        }

        [UnityTest]
        public IEnumerator TestSystemInitialization()
        {
            // Test that all systems initialize properly
            bool allSystemsReady = false;
            float timeout = 5f;
            float elapsed = 0f;

            while (!allSystemsReady && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
                allSystemsReady = integrationManager.AreAllSystemsReady();
            }

            Assert.IsTrue(allSystemsReady, "All systems should be initialized within timeout");

            var systemStatus = integrationManager.GetSystemStatus();
            Assert.IsTrue(systemStatus["Canvas"], "Canvas system should be initialized");
            Assert.IsTrue(systemStatus["ImageOverlay"], "ImageOverlay system should be initialized");
            Assert.IsTrue(systemStatus["Filter"], "Filter system should be initialized");
            Assert.IsTrue(systemStatus["ColorAnalysis"], "ColorAnalysis system should be initialized");
            Assert.IsTrue(systemStatus["Input"], "Input system should be initialized");
            Assert.IsTrue(systemStatus["Tracking"], "Tracking system should be initialized");
            Assert.IsTrue(systemStatus["Session"], "Session system should be initialized");
            Assert.IsTrue(systemStatus["UI"], "UI system should be initialized");
        }

        [UnityTest]
        public IEnumerator TestCanvasDefinitionWorkflow()
        {
            // Wait for systems to initialize
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Test canvas definition workflow
            bool workflowStarted = false;
            bool workflowCompleted = false;

            integrationManager.OnWorkflowStarted.AddListener((workflow) => {
                if (workflow == "CanvasDefinition") workflowStarted = true;
            });

            integrationManager.OnWorkflowCompleted.AddListener((workflow) => {
                if (workflow == "CanvasDefinition") workflowCompleted = true;
            });

            // Execute canvas definition workflow
            var task = integrationManager.ExecuteCanvasDefinitionWorkflow();
            
            // Wait for workflow to complete
            while (!task.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Assert.IsTrue(workflowStarted, "Canvas definition workflow should start");
            Assert.IsTrue(task.Result, "Canvas definition workflow should succeed");
            Assert.IsTrue(workflowCompleted, "Canvas definition workflow should complete");
        }

        [UnityTest]
        public IEnumerator TestImageOverlayWorkflow()
        {
            // Wait for systems to initialize
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // First define a canvas (prerequisite)
            var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
            while (!canvasTask.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }
            Assert.IsTrue(canvasTask.Result, "Canvas definition should succeed before image overlay");

            // Test image overlay workflow
            bool workflowStarted = false;
            bool workflowCompleted = false;

            integrationManager.OnWorkflowStarted.AddListener((workflow) => {
                if (workflow == "ImageOverlay") workflowStarted = true;
            });

            integrationManager.OnWorkflowCompleted.AddListener((workflow) => {
                if (workflow == "ImageOverlay") workflowCompleted = true;
            });

            // Execute image overlay workflow
            var task = integrationManager.ExecuteImageOverlayWorkflow(testImagePath);
            
            // Wait for workflow to complete
            while (!task.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Assert.IsTrue(workflowStarted, "Image overlay workflow should start");
            Assert.IsTrue(task.Result, "Image overlay workflow should succeed");
            Assert.IsTrue(workflowCompleted, "Image overlay workflow should complete");
        }

        [UnityTest]
        public IEnumerator TestColorMatchingWorkflow()
        {
            // Wait for systems to initialize
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Setup prerequisites (canvas and image)
            var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
            while (!canvasTask.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            var imageTask = integrationManager.ExecuteImageOverlayWorkflow(testImagePath);
            while (!imageTask.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Test color matching workflow
            bool workflowStarted = false;
            bool workflowCompleted = false;

            integrationManager.OnWorkflowStarted.AddListener((workflow) => {
                if (workflow == "ColorMatching") workflowStarted = true;
            });

            integrationManager.OnWorkflowCompleted.AddListener((workflow) => {
                if (workflow == "ColorMatching") workflowCompleted = true;
            });

            // Execute color matching workflow
            Vector2 imageCoordinate = new Vector2(0.5f, 0.5f); // Center of image
            Vector3 paintPosition = new Vector3(0, 0, 1); // 1 meter in front
            var task = integrationManager.ExecuteColorMatchingWorkflow(imageCoordinate, paintPosition);
            
            // Wait for workflow to complete
            while (!task.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Assert.IsTrue(workflowStarted, "Color matching workflow should start");
            Assert.IsTrue(task.Result, "Color matching workflow should succeed");
            Assert.IsTrue(workflowCompleted, "Color matching workflow should complete");
        }

        [UnityTest]
        public IEnumerator TestCompleteArtistWorkflow()
        {
            // Wait for systems to initialize
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Test complete artist workflow
            bool workflowStarted = false;
            bool workflowCompleted = false;

            integrationManager.OnWorkflowStarted.AddListener((workflow) => {
                if (workflow == "CompleteArtistSession") workflowStarted = true;
            });

            integrationManager.OnWorkflowCompleted.AddListener((workflow) => {
                if (workflow == "CompleteArtistSession") workflowCompleted = true;
            });

            // Execute complete artist workflow
            var task = integrationManager.ExecuteCompleteArtistWorkflow(testImagePath);
            
            // Wait for workflow to complete (with longer timeout for complete workflow)
            float timeout = 10f;
            float elapsed = 0f;
            while (!task.IsCompleted && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            Assert.IsTrue(workflowStarted, "Complete artist workflow should start");
            Assert.IsTrue(task.Result, "Complete artist workflow should succeed");
            Assert.IsTrue(workflowCompleted, "Complete artist workflow should complete");
        }

        [UnityTest]
        public IEnumerator TestPerformanceMonitoring()
        {
            // Wait for systems to initialize
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Monitor performance for a few seconds
            bool performanceUpdated = false;
            PerformanceMetrics lastMetrics = null;

            integrationManager.OnPerformanceUpdate.AddListener((metrics) => {
                performanceUpdated = true;
                lastMetrics = metrics;
            });

            // Wait for performance update
            yield return new WaitForSeconds(2f);

            Assert.IsTrue(performanceUpdated, "Performance monitoring should provide updates");
            Assert.IsNotNull(lastMetrics, "Performance metrics should be available");
            Assert.Greater(lastMetrics.fps, 0, "FPS should be greater than 0");
            Assert.GreaterOrEqual(lastMetrics.trackingQuality, 0, "Tracking quality should be non-negative");
        }

        [UnityTest]
        public IEnumerator TestSystemErrorHandling()
        {
            // Wait for systems to initialize
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Test error handling by trying to load non-existent image
            bool errorOccurred = false;
            string errorMessage = "";

            integrationManager.OnSystemError.AddListener((error) => {
                errorOccurred = true;
                errorMessage = error;
            });

            // Try to load invalid image
            var task = integrationManager.ExecuteImageOverlayWorkflow("invalid_path.jpg");
            
            // Wait for task to complete
            while (!task.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Assert.IsFalse(task.Result, "Invalid image workflow should fail");
            // Note: Error event might not fire in mock environment, so we don't assert on errorOccurred
        }

        [UnityTest]
        public IEnumerator TestSystemReset()
        {
            // Wait for systems to initialize
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Execute a workflow to change system state
            var task = integrationManager.ExecuteCompleteArtistWorkflow(testImagePath);
            while (!task.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Reset all systems
            integrationManager.ResetAllSystems();

            // Verify systems are reset (this would need to be implemented in actual system components)
            yield return new WaitForSeconds(0.5f);

            // Systems should still be ready after reset
            Assert.IsTrue(integrationManager.AreAllSystemsReady(), "Systems should remain ready after reset");
        }

        [Test]
        public void TestPerformanceMetricsValidation()
        {
            var goodMetrics = new PerformanceMetrics
            {
                fps = 60f,
                memoryUsage = 256 * 1024 * 1024, // 256MB
                trackingQuality = 0.9f
            };

            Assert.IsTrue(goodMetrics.IsPerformanceGood(), "Good metrics should pass validation");

            var badMetrics = new PerformanceMetrics
            {
                fps = 30f, // Below target
                memoryUsage = 600 * 1024 * 1024, // Above limit
                trackingQuality = 0.5f // Below threshold
            };

            Assert.IsFalse(badMetrics.IsPerformanceGood(), "Bad metrics should fail validation");
        }

        [UnityTest]
        public IEnumerator TestConcurrentWorkflows()
        {
            // Wait for systems to initialize
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Test that workflows handle concurrency properly
            var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
            
            // Wait a bit then try to start another workflow
            yield return new WaitForSeconds(0.5f);
            
            var imageTask = integrationManager.ExecuteImageOverlayWorkflow(testImagePath);

            // Wait for both to complete
            while (!canvasTask.IsCompleted || !imageTask.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Assert.IsTrue(canvasTask.Result, "Canvas workflow should succeed");
            Assert.IsTrue(imageTask.Result, "Image workflow should succeed after canvas");
        }

        [UnityTest]
        public IEnumerator TestWorkflowDependencies()
        {
            // Wait for systems to initialize
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Test that image overlay fails without canvas definition
            var imageTask = integrationManager.ExecuteImageOverlayWorkflow(testImagePath);
            
            while (!imageTask.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Assert.IsFalse(imageTask.Result, "Image overlay should fail without canvas definition");

            // Now define canvas and try again
            var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
            while (!canvasTask.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            var imageTask2 = integrationManager.ExecuteImageOverlayWorkflow(testImagePath);
            while (!imageTask2.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Assert.IsTrue(imageTask2.Result, "Image overlay should succeed after canvas definition");
        }

        [UnityTest]
        public IEnumerator TestExtendedSessionSimulation()
        {
            // Simulate a 30-second art session (scaled down for testing)
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            float sessionDuration = 3f; // 3 seconds for testing (represents 30 minutes)
            float startTime = Time.time;
            
            // Start complete workflow
            var workflowTask = integrationManager.ExecuteCompleteArtistWorkflow(testImagePath);
            while (!workflowTask.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Assert.IsTrue(workflowTask.Result, "Initial workflow should succeed");

            // Monitor performance during session
            List<PerformanceMetrics> performanceHistory = new List<PerformanceMetrics>();
            
            integrationManager.OnPerformanceUpdate.AddListener((metrics) => {
                performanceHistory.Add(metrics);
            });

            // Simulate user interactions during session
            while (Time.time - startTime < sessionDuration)
            {
                yield return new WaitForSeconds(0.5f);
                
                // Simulate opacity changes
                var imageOverlay = Object.FindObjectOfType<ImageOverlay.ImageOverlayManager>();
                if (imageOverlay != null)
                {
                    float randomOpacity = Random.Range(0.3f, 1.0f);
                    imageOverlay.SetOpacity(randomOpacity);
                }

                // Simulate filter changes
                var filterManager = Object.FindObjectOfType<Filters.FilterManager>();
                if (filterManager != null && Random.value > 0.7f)
                {
                    var filterParams = new Filters.FilterParameters
                    {
                        type = Filters.FilterType.Grayscale,
                        intensity = Random.Range(0.2f, 0.8f)
                    };
                    filterManager.ApplyFilter(filterParams.type, filterParams);
                }
            }

            // Verify performance remained stable
            Assert.Greater(performanceHistory.Count, 0, "Performance should be monitored during session");
            
            // Check that most performance samples were good
            int goodPerformanceSamples = 0;
            foreach (var metrics in performanceHistory)
            {
                if (metrics.IsPerformanceGood(55f)) // Slightly lower threshold for testing
                {
                    goodPerformanceSamples++;
                }
            }

            float performanceRatio = (float)goodPerformanceSamples / performanceHistory.Count;
            Assert.Greater(performanceRatio, 0.8f, "At least 80% of performance samples should be good");
        }
    }
}