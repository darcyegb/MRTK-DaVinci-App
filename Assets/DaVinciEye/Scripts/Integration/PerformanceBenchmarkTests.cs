using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Profiling;
using NUnit.Framework;
using DaVinciEye.Integration;

namespace DaVinciEye.Tests.Performance
{
    /// <summary>
    /// Performance benchmark tests to ensure 60 FPS target and memory constraints
    /// Tests system performance under various load conditions
    /// </summary>
    public class PerformanceBenchmarkTests
    {
        private SystemIntegrationManager integrationManager;
        private GameObject testGameObject;
        private List<float> fpsHistory = new List<float>();
        private List<long> memoryHistory = new List<long>();

        // Performance targets
        private const float TARGET_FPS = 60f;
        private const float MIN_ACCEPTABLE_FPS = 55f; // 10% tolerance
        private const long MAX_MEMORY_USAGE = 512 * 1024 * 1024; // 512MB
        private const float MAX_UI_RESPONSE_TIME = 100f; // 100ms

        [SetUp]
        public void SetUp()
        {
            // Create test environment
            testGameObject = new GameObject("PerformanceTestManager");
            integrationManager = testGameObject.AddComponent<SystemIntegrationManager>();

            // Setup mock systems for performance testing
            SetupPerformanceTestSystems();

            // Clear performance history
            fpsHistory.Clear();
            memoryHistory.Clear();

            // Force garbage collection before tests
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }

            // Clean up after performance tests
            System.GC.Collect();
        }

        private void SetupPerformanceTestSystems()
        {
            // Create realistic system components for performance testing
            var canvasManager = testGameObject.AddComponent<Canvas.CanvasDefinitionManager>();
            var imageOverlay = testGameObject.AddComponent<ImageOverlay.ImageOverlayManager>();
            var filterManager = testGameObject.AddComponent<Filters.FilterManager>();
            var colorAnalyzer = testGameObject.AddComponent<ColorAnalysis.ColorAnalyzer>();
            var trackingMonitor = testGameObject.AddComponent<SpatialTracking.TrackingQualityMonitor>();
            var sessionManager = testGameObject.AddComponent<SessionManagement.SessionDataManager>();
            var uiManager = testGameObject.AddComponent<UI.UIManager>();
            var inputManager = testGameObject.AddComponent<Input.HandGestureManager>();

            // Create test textures for performance testing
            CreateTestTextures();
        }

        private void CreateTestTextures()
        {
            // Create various sized textures for performance testing
            var smallTexture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            var mediumTexture = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
            var largeTexture = new Texture2D(2048, 2048, TextureFormat.RGB24, false);

            // Fill with test data
            FillTextureWithTestData(smallTexture);
            FillTextureWithTestData(mediumTexture);
            FillTextureWithTestData(largeTexture);
        }

        private void FillTextureWithTestData(Texture2D texture)
        {
            var pixels = new Color[texture.width * texture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    1f
                );
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }

        [UnityTest]
        public IEnumerator TestBaselinePerformance()
        {
            // Test baseline performance with no systems active
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Monitor baseline performance for 3 seconds
            float testDuration = 3f;
            yield return StartCoroutine(MonitorPerformance(testDuration));

            // Analyze baseline results
            float averageFPS = CalculateAverageFPS();
            long averageMemory = CalculateAverageMemory();

            Assert.Greater(averageFPS, MIN_ACCEPTABLE_FPS, 
                $"Baseline FPS should be above {MIN_ACCEPTABLE_FPS}, got {averageFPS:F1}");
            Assert.Less(averageMemory, MAX_MEMORY_USAGE, 
                $"Baseline memory should be below {MAX_MEMORY_USAGE / (1024*1024)}MB, got {averageMemory / (1024*1024)}MB");

            Debug.Log($"Baseline Performance - FPS: {averageFPS:F1}, Memory: {averageMemory / (1024*1024)}MB");
        }

        [UnityTest]
        public IEnumerator TestImageOverlayPerformance()
        {
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Load and display image overlay
            var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
            while (!canvasTask.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            var imageTask = integrationManager.ExecuteImageOverlayWorkflow("TestAssets/test_image.jpg");
            while (!imageTask.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Monitor performance with image overlay active
            float testDuration = 3f;
            yield return StartCoroutine(MonitorPerformance(testDuration));

            float averageFPS = CalculateAverageFPS();
            long averageMemory = CalculateAverageMemory();

            Assert.Greater(averageFPS, MIN_ACCEPTABLE_FPS, 
                $"Image overlay FPS should be above {MIN_ACCEPTABLE_FPS}, got {averageFPS:F1}");
            Assert.Less(averageMemory, MAX_MEMORY_USAGE, 
                $"Image overlay memory should be below {MAX_MEMORY_USAGE / (1024*1024)}MB, got {averageMemory / (1024*1024)}MB");

            Debug.Log($"Image Overlay Performance - FPS: {averageFPS:F1}, Memory: {averageMemory / (1024*1024)}MB");
        }

        [UnityTest]
        public IEnumerator TestMultipleFiltersPerformance()
        {
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Setup image overlay
            var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
            while (!canvasTask.IsCompleted) yield return new WaitForSeconds(0.1f);

            var imageTask = integrationManager.ExecuteImageOverlayWorkflow("TestAssets/test_image.jpg");
            while (!imageTask.IsCompleted) yield return new WaitForSeconds(0.1f);

            // Apply multiple filters
            var filterManager = Object.FindObjectOfType<Filters.FilterManager>();
            if (filterManager != null)
            {
                // Apply 3 filters simultaneously (target requirement)
                var grayscaleFilter = new Filters.FilterParameters
                {
                    type = Filters.FilterType.Grayscale,
                    intensity = 0.5f
                };
                filterManager.ApplyFilter(grayscaleFilter.type, grayscaleFilter);

                var contrastFilter = new Filters.FilterParameters
                {
                    type = Filters.FilterType.ContrastEnhancement,
                    intensity = 0.7f
                };
                filterManager.ApplyFilter(contrastFilter.type, contrastFilter);

                var edgeFilter = new Filters.FilterParameters
                {
                    type = Filters.FilterType.EdgeDetection,
                    intensity = 0.3f
                };
                filterManager.ApplyFilter(edgeFilter.type, edgeFilter);
            }

            // Monitor performance with multiple filters active
            float testDuration = 3f;
            yield return StartCoroutine(MonitorPerformance(testDuration));

            float averageFPS = CalculateAverageFPS();
            long averageMemory = CalculateAverageMemory();

            Assert.Greater(averageFPS, MIN_ACCEPTABLE_FPS, 
                $"Multiple filters FPS should be above {MIN_ACCEPTABLE_FPS}, got {averageFPS:F1}");
            Assert.Less(averageMemory, MAX_MEMORY_USAGE, 
                $"Multiple filters memory should be below {MAX_MEMORY_USAGE / (1024*1024)}MB, got {averageMemory / (1024*1024)}MB");

            Debug.Log($"Multiple Filters Performance - FPS: {averageFPS:F1}, Memory: {averageMemory / (1024*1024)}MB");
        }

        [UnityTest]
        public IEnumerator TestUIResponseTimePerformance()
        {
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Test UI response times
            var uiManager = Object.FindObjectOfType<UI.UIManager>();
            var imageOverlay = Object.FindObjectOfType<ImageOverlay.ImageOverlayManager>();

            if (uiManager != null && imageOverlay != null)
            {
                List<float> responseTimes = new List<float>();

                // Test opacity slider response time
                for (int i = 0; i < 10; i++)
                {
                    float startTime = Time.realtimeSinceStartup;
                    
                    // Simulate opacity change
                    float newOpacity = Random.Range(0.1f, 1.0f);
                    imageOverlay.SetOpacity(newOpacity);
                    
                    yield return new WaitForEndOfFrame(); // Wait for UI update
                    
                    float responseTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to ms
                    responseTimes.Add(responseTime);
                    
                    yield return new WaitForSeconds(0.1f);
                }

                // Calculate average response time
                float averageResponseTime = 0f;
                foreach (float time in responseTimes)
                {
                    averageResponseTime += time;
                }
                averageResponseTime /= responseTimes.Count;

                Assert.Less(averageResponseTime, MAX_UI_RESPONSE_TIME, 
                    $"UI response time should be below {MAX_UI_RESPONSE_TIME}ms, got {averageResponseTime:F1}ms");

                Debug.Log($"UI Response Time Performance - Average: {averageResponseTime:F1}ms");
            }
        }

        [UnityTest]
        public IEnumerator TestMemoryLeakDetection()
        {
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Record initial memory usage
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            long initialMemory = System.GC.GetTotalMemory(false);

            // Perform memory-intensive operations repeatedly
            for (int cycle = 0; cycle < 5; cycle++)
            {
                // Load and unload images
                var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
                while (!canvasTask.IsCompleted) yield return new WaitForSeconds(0.1f);

                var imageTask = integrationManager.ExecuteImageOverlayWorkflow("TestAssets/test_image.jpg");
                while (!imageTask.IsCompleted) yield return new WaitForSeconds(0.1f);

                // Apply and remove filters
                var filterManager = Object.FindObjectOfType<Filters.FilterManager>();
                if (filterManager != null)
                {
                    var filter = new Filters.FilterParameters
                    {
                        type = Filters.FilterType.Grayscale,
                        intensity = 1.0f
                    };
                    filterManager.ApplyFilter(filter.type, filter);
                    yield return new WaitForSeconds(0.5f);
                    filterManager.RemoveFilter(filter.type);
                }

                // Reset systems
                integrationManager.ResetAllSystems();
                yield return new WaitForSeconds(0.5f);

                // Force garbage collection
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }

            // Check final memory usage
            long finalMemory = System.GC.GetTotalMemory(false);
            long memoryIncrease = finalMemory - initialMemory;
            long maxAcceptableIncrease = 50 * 1024 * 1024; // 50MB increase is acceptable

            Assert.Less(memoryIncrease, maxAcceptableIncrease, 
                $"Memory increase should be less than {maxAcceptableIncrease / (1024*1024)}MB, got {memoryIncrease / (1024*1024)}MB");

            Debug.Log($"Memory Leak Test - Initial: {initialMemory / (1024*1024)}MB, Final: {finalMemory / (1024*1024)}MB, Increase: {memoryIncrease / (1024*1024)}MB");
        }

        [UnityTest]
        public IEnumerator TestLargeImagePerformance()
        {
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Create large test image (2048x2048)
            var largeTexture = new Texture2D(2048, 2048, TextureFormat.RGB24, false);
            FillTextureWithTestData(largeTexture);

            // Setup canvas
            var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
            while (!canvasTask.IsCompleted) yield return new WaitForSeconds(0.1f);

            // Load large image
            var imageOverlay = Object.FindObjectOfType<ImageOverlay.ImageOverlayManager>();
            if (imageOverlay != null)
            {
                // Simulate loading large image
                imageOverlay.SetDisplayTexture(largeTexture);
                imageOverlay.SetVisibility(true);
            }

            // Monitor performance with large image
            float testDuration = 3f;
            yield return StartCoroutine(MonitorPerformance(testDuration));

            float averageFPS = CalculateAverageFPS();
            long averageMemory = CalculateAverageMemory();

            Assert.Greater(averageFPS, MIN_ACCEPTABLE_FPS, 
                $"Large image FPS should be above {MIN_ACCEPTABLE_FPS}, got {averageFPS:F1}");
            Assert.Less(averageMemory, MAX_MEMORY_USAGE, 
                $"Large image memory should be below {MAX_MEMORY_USAGE / (1024*1024)}MB, got {averageMemory / (1024*1024)}MB");

            Debug.Log($"Large Image Performance - FPS: {averageFPS:F1}, Memory: {averageMemory / (1024*1024)}MB");

            // Clean up
            Object.DestroyImmediate(largeTexture);
        }

        [UnityTest]
        public IEnumerator TestStressTestPerformance()
        {
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Stress test: All systems active with maximum load
            var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
            while (!canvasTask.IsCompleted) yield return new WaitForSeconds(0.1f);

            var imageTask = integrationManager.ExecuteImageOverlayWorkflow("TestAssets/test_image.jpg");
            while (!imageTask.IsCompleted) yield return new WaitForSeconds(0.1f);

            // Apply all available filters
            var filterManager = Object.FindObjectOfType<Filters.FilterManager>();
            if (filterManager != null)
            {
                var filters = new[]
                {
                    Filters.FilterType.Grayscale,
                    Filters.FilterType.ContrastEnhancement,
                    Filters.FilterType.EdgeDetection,
                    Filters.FilterType.ColorRange,
                    Filters.FilterType.ColorReduction
                };

                foreach (var filterType in filters)
                {
                    var filter = new Filters.FilterParameters
                    {
                        type = filterType,
                        intensity = 0.8f
                    };
                    filterManager.ApplyFilter(filter.type, filter);
                }
            }

            // Continuously change opacity and perform color analysis
            StartCoroutine(StressTestOperations());

            // Monitor performance under stress
            float testDuration = 5f;
            yield return StartCoroutine(MonitorPerformance(testDuration));

            float averageFPS = CalculateAverageFPS();
            long averageMemory = CalculateAverageMemory();

            // More lenient thresholds for stress test
            float stressTestMinFPS = MIN_ACCEPTABLE_FPS * 0.8f; // 20% reduction acceptable under stress
            
            Assert.Greater(averageFPS, stressTestMinFPS, 
                $"Stress test FPS should be above {stressTestMinFPS}, got {averageFPS:F1}");
            Assert.Less(averageMemory, MAX_MEMORY_USAGE, 
                $"Stress test memory should be below {MAX_MEMORY_USAGE / (1024*1024)}MB, got {averageMemory / (1024*1024)}MB");

            Debug.Log($"Stress Test Performance - FPS: {averageFPS:F1}, Memory: {averageMemory / (1024*1024)}MB");
        }

        private IEnumerator StressTestOperations()
        {
            var imageOverlay = Object.FindObjectOfType<ImageOverlay.ImageOverlayManager>();
            var colorAnalyzer = Object.FindObjectOfType<ColorAnalysis.ColorAnalyzer>();

            for (int i = 0; i < 50; i++) // 50 operations
            {
                // Change opacity
                if (imageOverlay != null)
                {
                    imageOverlay.SetOpacity(Random.Range(0.1f, 1.0f));
                }

                // Perform color analysis
                if (colorAnalyzer != null)
                {
                    Vector2 randomCoord = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
                    colorAnalyzer.PickColorFromImage(randomCoord);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator MonitorPerformance(float duration)
        {
            fpsHistory.Clear();
            memoryHistory.Clear();

            float startTime = Time.realtimeSinceStartup;
            
            while (Time.realtimeSinceStartup - startTime < duration)
            {
                // Record FPS
                float currentFPS = 1.0f / Time.unscaledDeltaTime;
                fpsHistory.Add(currentFPS);

                // Record memory usage
                long currentMemory = System.GC.GetTotalMemory(false);
                memoryHistory.Add(currentMemory);

                yield return new WaitForSeconds(0.1f); // Sample every 100ms
            }
        }

        private float CalculateAverageFPS()
        {
            if (fpsHistory.Count == 0) return 0f;

            float sum = 0f;
            foreach (float fps in fpsHistory)
            {
                sum += fps;
            }
            return sum / fpsHistory.Count;
        }

        private long CalculateAverageMemory()
        {
            if (memoryHistory.Count == 0) return 0L;

            long sum = 0L;
            foreach (long memory in memoryHistory)
            {
                sum += memory;
            }
            return sum / memoryHistory.Count;
        }

        [Test]
        public void TestPerformanceThresholds()
        {
            // Test that our performance thresholds are reasonable
            Assert.Greater(TARGET_FPS, 0, "Target FPS should be positive");
            Assert.Greater(MIN_ACCEPTABLE_FPS, 0, "Minimum acceptable FPS should be positive");
            Assert.Less(MIN_ACCEPTABLE_FPS, TARGET_FPS, "Minimum FPS should be less than target");
            Assert.Greater(MAX_MEMORY_USAGE, 0, "Maximum memory usage should be positive");
            Assert.Greater(MAX_UI_RESPONSE_TIME, 0, "Maximum UI response time should be positive");
        }

        [UnityTest]
        public IEnumerator TestPerformanceDegradationRecovery()
        {
            yield return new WaitUntil(() => integrationManager.AreAllSystemsReady());

            // Simulate performance degradation and recovery
            var performanceMetrics = new List<PerformanceMetrics>();
            
            integrationManager.OnPerformanceUpdate.AddListener((metrics) => {
                performanceMetrics.Add(metrics);
            });

            // Start with normal load
            var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
            while (!canvasTask.IsCompleted) yield return new WaitForSeconds(0.1f);

            yield return new WaitForSeconds(2f); // Collect baseline metrics

            // Simulate high load (multiple large operations)
            for (int i = 0; i < 3; i++)
            {
                var imageTask = integrationManager.ExecuteImageOverlayWorkflow($"TestAssets/test_image_{i}.jpg");
                // Don't wait for completion - create load
            }

            yield return new WaitForSeconds(3f); // Monitor under load

            // Reset to recover performance
            integrationManager.ResetAllSystems();
            System.GC.Collect();

            yield return new WaitForSeconds(2f); // Monitor recovery

            // Analyze performance recovery
            if (performanceMetrics.Count > 0)
            {
                var recentMetrics = performanceMetrics[performanceMetrics.Count - 1];
                Assert.Greater(recentMetrics.fps, MIN_ACCEPTABLE_FPS * 0.9f, 
                    "Performance should recover after system reset");
            }
        }
    }
}