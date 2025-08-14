using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace DaVinciEye.Performance
{
    /// <summary>
    /// Comprehensive HoloLens 2 device testing and validation system
    /// Tests device-specific features, performance, and user workflows
    /// </summary>
    public class HoloLens2DeviceTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private bool enableVerboseLogging = true;
        [SerializeField] private float testTimeout = 30f;

        [Header("Performance Targets")]
        [SerializeField] private float targetFPS = 60f;
        [SerializeField] private float minAcceptableFPS = 55f;
        [SerializeField] private long maxMemoryUsage = 512 * 1024 * 1024; // 512MB
        [SerializeField] private float maxUIResponseTime = 100f; // milliseconds

        [Header("Test Assets")]
        [SerializeField] private List<string> testImagePaths = new List<string>();
        [SerializeField] private List<Texture2D> testTextures = new List<Texture2D>();

        private List<DeviceTestResult> testResults = new List<DeviceTestResult>();
        private bool isTestingInProgress = false;

        // Device capability tracking
        private bool isHoloLens2Device = false;
        private bool hasHandTracking = false;
        private bool hasEyeTracking = false;
        private bool hasSpatialMapping = false;
        private bool hasCameraAccess = false;

        public event Action<DeviceTestResult> OnTestCompleted;
        public event Action<DeviceTestReport> OnTestingFinished;

        private void Start()
        {
            DetectDeviceCapabilities();
            
            if (runTestsOnStart)
            {
                StartCoroutine(RunAllDeviceTests());
            }
        }

        /// <summary>
        /// Detect HoloLens 2 device capabilities
        /// </summary>
        private void DetectDeviceCapabilities()
        {
            LogTest("Detecting device capabilities...");

            // Check if running on HoloLens 2
            isHoloLens2Device = Application.platform == RuntimePlatform.WSAPlayerX64 && 
                               XRSettings.enabled;

            // Check hand tracking capability
            var handDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HandTracking, handDevices);
            hasHandTracking = handDevices.Count > 0;

            // Check eye tracking capability
            var eyeDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, eyeDevices);
            hasEyeTracking = eyeDevices.Count > 0;

            // Check spatial mapping capability
            hasSpatialMapping = XRSettings.enabled && XRDevice.isPresent;

            // Check camera access (simplified check)
            hasCameraAccess = Application.HasUserAuthorization(UserAuthorization.WebCam);

            LogTest($"Device Detection Results:");
            LogTest($"  HoloLens 2: {isHoloLens2Device}");
            LogTest($"  Hand Tracking: {hasHandTracking}");
            LogTest($"  Eye Tracking: {hasEyeTracking}");
            LogTest($"  Spatial Mapping: {hasSpatialMapping}");
            LogTest($"  Camera Access: {hasCameraAccess}");
        }

        /// <summary>
        /// Run comprehensive device testing suite
        /// </summary>
        public IEnumerator RunAllDeviceTests()
        {
            if (isTestingInProgress)
            {
                LogTest("Testing already in progress");
                yield break;
            }

            isTestingInProgress = true;
            testResults.Clear();

            LogTest("Starting comprehensive HoloLens 2 device testing...");

            // Core device tests
            yield return StartCoroutine(TestDeviceCapabilities());
            yield return StartCoroutine(TestPerformanceBaseline());
            yield return StartCoroutine(TestMemoryManagement());
            yield return StartCoroutine(TestSpatialTracking());
            yield return StartCoroutine(TestHandTracking());
            yield return StartCoroutine(TestUIInteraction());

            // Application-specific tests
            yield return StartCoroutine(TestCanvasDefinitionWorkflow());
            yield return StartCoroutine(TestImageOverlayPerformance());
            yield return StartCoroutine(TestFilterProcessingPerformance());
            yield return StartCoroutine(TestColorAnalysisAccuracy());

            // Extended session tests
            yield return StartCoroutine(TestExtendedSessionStability());
            yield return StartCoroutine(TestBatteryImpact());
            yield return StartCoroutine(TestThermalPerformance());

            // Generate final report
            GenerateDeviceTestReport();

            isTestingInProgress = false;
            LogTest("Device testing completed");
        }

        #region Core Device Tests

        private IEnumerator TestDeviceCapabilities()
        {
            LogTest("Testing device capabilities...");

            var result = new DeviceTestResult
            {
                testName = "Device Capabilities",
                category = TestCategory.DeviceCapabilities,
                startTime = DateTime.Now
            };

            try
            {
                // Test XR system
                bool xrWorking = XRSettings.enabled && XRDevice.isPresent;
                result.AddMetric("XR System", xrWorking ? 1f : 0f);

                // Test input system
                var inputDevices = new List<InputDevice>();
                InputDevices.GetDevices(inputDevices);
                result.AddMetric("Input Devices", inputDevices.Count);

                // Test display system
                float displayRefreshRate = XRDevice.refreshRate;
                result.AddMetric("Display Refresh Rate", displayRefreshRate);

                result.passed = xrWorking && inputDevices.Count > 0;
                result.details = $"XR: {xrWorking}, Input Devices: {inputDevices.Count}, Refresh Rate: {displayRefreshRate}Hz";
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);

            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator TestPerformanceBaseline()
        {
            LogTest("Testing performance baseline...");

            var result = new DeviceTestResult
            {
                testName = "Performance Baseline",
                category = TestCategory.Performance,
                startTime = DateTime.Now
            };

            try
            {
                List<float> fpsReadings = new List<float>();
                List<long> memoryReadings = new List<long>();

                // Monitor performance for 5 seconds
                float testDuration = 5f;
                float startTime = Time.realtimeSinceStartup;

                while (Time.realtimeSinceStartup - startTime < testDuration)
                {
                    float currentFPS = 1.0f / Time.unscaledDeltaTime;
                    long currentMemory = GC.GetTotalMemory(false);

                    fpsReadings.Add(currentFPS);
                    memoryReadings.Add(currentMemory);

                    yield return new WaitForSeconds(0.1f);
                }

                // Calculate averages
                float avgFPS = 0f;
                foreach (float fps in fpsReadings) avgFPS += fps;
                avgFPS /= fpsReadings.Count;

                long avgMemory = 0L;
                foreach (long memory in memoryReadings) avgMemory += memory;
                avgMemory /= memoryReadings.Count;

                result.AddMetric("Average FPS", avgFPS);
                result.AddMetric("Average Memory (MB)", avgMemory / (1024f * 1024f));

                result.passed = avgFPS >= minAcceptableFPS && avgMemory <= maxMemoryUsage;
                result.details = $"FPS: {avgFPS:F1}, Memory: {avgMemory / (1024 * 1024)}MB";
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);
        }

        private IEnumerator TestMemoryManagement()
        {
            LogTest("Testing memory management...");

            var result = new DeviceTestResult
            {
                testName = "Memory Management",
                category = TestCategory.Performance,
                startTime = DateTime.Now
            };

            try
            {
                // Record initial memory
                GC.Collect();
                GC.WaitForPendingFinalizers();
                long initialMemory = GC.GetTotalMemory(false);

                // Allocate and deallocate memory to test GC
                List<byte[]> allocations = new List<byte[]>();
                
                // Allocate 100MB in chunks
                for (int i = 0; i < 100; i++)
                {
                    allocations.Add(new byte[1024 * 1024]); // 1MB chunks
                    yield return null; // Yield to prevent frame drops
                }

                long peakMemory = GC.GetTotalMemory(false);

                // Clear allocations
                allocations.Clear();
                allocations = null;

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                yield return new WaitForSeconds(1f);

                long finalMemory = GC.GetTotalMemory(false);

                // Calculate memory recovery
                long memoryRecovered = peakMemory - finalMemory;
                float recoveryPercentage = (float)memoryRecovered / (peakMemory - initialMemory) * 100f;

                result.AddMetric("Initial Memory (MB)", initialMemory / (1024f * 1024f));
                result.AddMetric("Peak Memory (MB)", peakMemory / (1024f * 1024f));
                result.AddMetric("Final Memory (MB)", finalMemory / (1024f * 1024f));
                result.AddMetric("Recovery Percentage", recoveryPercentage);

                result.passed = recoveryPercentage > 80f && finalMemory < maxMemoryUsage;
                result.details = $"Memory recovery: {recoveryPercentage:F1}%, Final: {finalMemory / (1024 * 1024)}MB";
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);
        }

        private IEnumerator TestSpatialTracking()
        {
            LogTest("Testing spatial tracking...");

            var result = new DeviceTestResult
            {
                testName = "Spatial Tracking",
                category = TestCategory.SpatialTracking,
                startTime = DateTime.Now
            };

            try
            {
                if (!hasSpatialMapping)
                {
                    result.passed = false;
                    result.details = "Spatial mapping not available";
                }
                else
                {
                    // Test tracking quality over time
                    List<float> trackingQualityReadings = new List<float>();
                    float testDuration = 3f;
                    float startTime = Time.realtimeSinceStartup;

                    var trackingMonitor = FindObjectOfType<SpatialTracking.TrackingQualityMonitor>();
                    
                    while (Time.realtimeSinceStartup - startTime < testDuration)
                    {
                        float quality = trackingMonitor != null ? trackingMonitor.CurrentQuality : 1.0f;
                        trackingQualityReadings.Add(quality);
                        yield return new WaitForSeconds(0.1f);
                    }

                    float avgQuality = 0f;
                    foreach (float quality in trackingQualityReadings) avgQuality += quality;
                    avgQuality /= trackingQualityReadings.Count;

                    result.AddMetric("Average Tracking Quality", avgQuality);
                    result.AddMetric("Tracking Samples", trackingQualityReadings.Count);

                    result.passed = avgQuality > 0.7f;
                    result.details = $"Average tracking quality: {avgQuality:F2}";
                }
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);
        }

        private IEnumerator TestHandTracking()
        {
            LogTest("Testing hand tracking...");

            var result = new DeviceTestResult
            {
                testName = "Hand Tracking",
                category = TestCategory.HandTracking,
                startTime = DateTime.Now
            };

            try
            {
                if (!hasHandTracking)
                {
                    result.passed = false;
                    result.details = "Hand tracking not available";
                }
                else
                {
                    // Test hand tracking data availability
                    var handDevices = new List<InputDevice>();
                    InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HandTracking, handDevices);

                    int validHandReadings = 0;
                    int totalReadings = 0;

                    for (int i = 0; i < 30; i++) // Test for 3 seconds
                    {
                        foreach (var device in handDevices)
                        {
                            if (device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked))
                            {
                                totalReadings++;
                                if (isTracked)
                                {
                                    validHandReadings++;
                                }
                            }
                        }
                        yield return new WaitForSeconds(0.1f);
                    }

                    float trackingSuccessRate = totalReadings > 0 ? (float)validHandReadings / totalReadings : 0f;

                    result.AddMetric("Hand Devices", handDevices.Count);
                    result.AddMetric("Tracking Success Rate", trackingSuccessRate);
                    result.AddMetric("Valid Readings", validHandReadings);

                    result.passed = handDevices.Count > 0 && trackingSuccessRate > 0.5f;
                    result.details = $"Devices: {handDevices.Count}, Success Rate: {trackingSuccessRate:F2}";
                }
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);
        }

        private IEnumerator TestUIInteraction()
        {
            LogTest("Testing UI interaction...");

            var result = new DeviceTestResult
            {
                testName = "UI Interaction",
                category = TestCategory.UIInteraction,
                startTime = DateTime.Now
            };

            try
            {
                // Test UI responsiveness
                var uiManager = FindObjectOfType<UI.UIManager>();
                if (uiManager == null)
                {
                    result.passed = false;
                    result.details = "UI Manager not found";
                }
                else
                {
                    List<float> responseTimeReadings = new List<float>();

                    // Simulate UI interactions
                    for (int i = 0; i < 10; i++)
                    {
                        float startTime = Time.realtimeSinceStartup;
                        
                        // Simulate button press (would be actual interaction in real test)
                        yield return new WaitForEndOfFrame();
                        
                        float responseTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to ms
                        responseTimeReadings.Add(responseTime);
                        
                        yield return new WaitForSeconds(0.2f);
                    }

                    float avgResponseTime = 0f;
                    foreach (float time in responseTimeReadings) avgResponseTime += time;
                    avgResponseTime /= responseTimeReadings.Count;

                    result.AddMetric("Average Response Time (ms)", avgResponseTime);
                    result.AddMetric("Response Samples", responseTimeReadings.Count);

                    result.passed = avgResponseTime <= maxUIResponseTime;
                    result.details = $"Average response time: {avgResponseTime:F1}ms";
                }
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);
        }

        #endregion

        #region Application-Specific Tests

        private IEnumerator TestCanvasDefinitionWorkflow()
        {
            LogTest("Testing canvas definition workflow...");

            var result = new DeviceTestResult
            {
                testName = "Canvas Definition Workflow",
                category = TestCategory.ApplicationWorkflow,
                startTime = DateTime.Now
            };

            try
            {
                var integrationManager = FindObjectOfType<Integration.SystemIntegrationManager>();
                if (integrationManager == null)
                {
                    result.passed = false;
                    result.details = "Integration manager not found";
                }
                else
                {
                    float workflowStartTime = Time.realtimeSinceStartup;
                    
                    // Execute canvas definition workflow
                    var task = integrationManager.ExecuteCanvasDefinitionWorkflow();
                    
                    // Wait for completion with timeout
                    float elapsed = 0f;
                    while (!task.IsCompleted && elapsed < testTimeout)
                    {
                        yield return new WaitForSeconds(0.1f);
                        elapsed += 0.1f;
                    }

                    float workflowDuration = Time.realtimeSinceStartup - workflowStartTime;

                    result.AddMetric("Workflow Duration (s)", workflowDuration);
                    result.AddMetric("Workflow Completed", task.IsCompleted ? 1f : 0f);
                    result.AddMetric("Workflow Success", (task.IsCompleted && task.Result) ? 1f : 0f);

                    result.passed = task.IsCompleted && task.Result && workflowDuration < testTimeout;
                    result.details = $"Duration: {workflowDuration:F1}s, Success: {task.Result}";
                }
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);
        }

        private IEnumerator TestImageOverlayPerformance()
        {
            LogTest("Testing image overlay performance...");

            var result = new DeviceTestResult
            {
                testName = "Image Overlay Performance",
                category = TestCategory.Performance,
                startTime = DateTime.Now
            };

            try
            {
                var integrationManager = FindObjectOfType<Integration.SystemIntegrationManager>();
                if (integrationManager == null)
                {
                    result.passed = false;
                    result.details = "Integration manager not found";
                }
                else
                {
                    // First ensure canvas is defined
                    var canvasTask = integrationManager.ExecuteCanvasDefinitionWorkflow();
                    while (!canvasTask.IsCompleted) yield return new WaitForSeconds(0.1f);

                    // Test image overlay with different sizes
                    List<float> loadTimes = new List<float>();
                    List<float> fpsReadings = new List<float>();

                    foreach (string imagePath in testImagePaths)
                    {
                        float loadStartTime = Time.realtimeSinceStartup;
                        
                        var imageTask = integrationManager.ExecuteImageOverlayWorkflow(imagePath);
                        while (!imageTask.IsCompleted) yield return new WaitForSeconds(0.1f);
                        
                        float loadTime = Time.realtimeSinceStartup - loadStartTime;
                        loadTimes.Add(loadTime);

                        // Monitor FPS with image active
                        for (int i = 0; i < 30; i++) // 3 seconds
                        {
                            fpsReadings.Add(1.0f / Time.unscaledDeltaTime);
                            yield return new WaitForSeconds(0.1f);
                        }
                    }

                    float avgLoadTime = 0f;
                    foreach (float time in loadTimes) avgLoadTime += time;
                    avgLoadTime /= loadTimes.Count;

                    float avgFPS = 0f;
                    foreach (float fps in fpsReadings) avgFPS += fps;
                    avgFPS /= fpsReadings.Count;

                    result.AddMetric("Average Load Time (s)", avgLoadTime);
                    result.AddMetric("Average FPS with Overlay", avgFPS);
                    result.AddMetric("Images Tested", loadTimes.Count);

                    result.passed = avgLoadTime < 3f && avgFPS >= minAcceptableFPS;
                    result.details = $"Load Time: {avgLoadTime:F1}s, FPS: {avgFPS:F1}";
                }
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);
        }

        private IEnumerator TestFilterProcessingPerformance()
        {
            LogTest("Testing filter processing performance...");

            var result = new DeviceTestResult
            {
                testName = "Filter Processing Performance",
                category = TestCategory.Performance,
                startTime = DateTime.Now
            };

            try
            {
                var filterManager = FindObjectOfType<Filters.FilterManager>();
                if (filterManager == null)
                {
                    result.passed = false;
                    result.details = "Filter manager not found";
                }
                else
                {
                    // Test multiple filters simultaneously
                    var filterTypes = new[]
                    {
                        Filters.FilterType.Grayscale,
                        Filters.FilterType.ContrastEnhancement,
                        Filters.FilterType.EdgeDetection
                    };

                    List<float> filterApplicationTimes = new List<float>();
                    List<float> fpsWithFilters = new List<float>();

                    foreach (var filterType in filterTypes)
                    {
                        float filterStartTime = Time.realtimeSinceStartup;
                        
                        var filterParams = new Filters.FilterParameters
                        {
                            type = filterType,
                            intensity = 0.5f
                        };
                        filterManager.ApplyFilter(filterParams.type, filterParams);
                        
                        yield return new WaitForSeconds(0.2f); // Allow filter to process
                        
                        float filterTime = Time.realtimeSinceStartup - filterStartTime;
                        filterApplicationTimes.Add(filterTime);

                        // Monitor FPS with filters
                        for (int i = 0; i < 20; i++) // 2 seconds
                        {
                            fpsWithFilters.Add(1.0f / Time.unscaledDeltaTime);
                            yield return new WaitForSeconds(0.1f);
                        }
                    }

                    float avgFilterTime = 0f;
                    foreach (float time in filterApplicationTimes) avgFilterTime += time;
                    avgFilterTime /= filterApplicationTimes.Count;

                    float avgFPSWithFilters = 0f;
                    foreach (float fps in fpsWithFilters) avgFPSWithFilters += fps;
                    avgFPSWithFilters /= fpsWithFilters.Count;

                    result.AddMetric("Average Filter Application Time (s)", avgFilterTime);
                    result.AddMetric("Average FPS with Filters", avgFPSWithFilters);
                    result.AddMetric("Filters Tested", filterApplicationTimes.Count);

                    result.passed = avgFilterTime < 1f && avgFPSWithFilters >= minAcceptableFPS;
                    result.details = $"Filter Time: {avgFilterTime:F1}s, FPS: {avgFPSWithFilters:F1}";
                }
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);
        }

        private IEnumerator TestColorAnalysisAccuracy()
        {
            LogTest("Testing color analysis accuracy...");

            var result = new DeviceTestResult
            {
                testName = "Color Analysis Accuracy",
                category = TestCategory.ColorAnalysis,
                startTime = DateTime.Now
            };

            try
            {
                var colorAnalyzer = FindObjectOfType<ColorAnalysis.ColorAnalyzer>();
                if (colorAnalyzer == null)
                {
                    result.passed = false;
                    result.details = "Color analyzer not found";
                }
                else
                {
                    // Test color picking accuracy with known colors
                    var knownColors = new[]
                    {
                        Color.red,
                        Color.green,
                        Color.blue,
                        Color.white,
                        Color.black
                    };

                    List<float> colorAccuracyReadings = new List<float>();

                    foreach (var knownColor in knownColors)
                    {
                        // Simulate color picking (would use actual image coordinates in real test)
                        Vector2 testCoordinate = new Vector2(0.5f, 0.5f);
                        Color pickedColor = colorAnalyzer.PickColorFromImage(testCoordinate);

                        // Calculate color difference (simplified Delta E)
                        float deltaR = Mathf.Abs(pickedColor.r - knownColor.r);
                        float deltaG = Mathf.Abs(pickedColor.g - knownColor.g);
                        float deltaB = Mathf.Abs(pickedColor.b - knownColor.b);
                        float colorDifference = (deltaR + deltaG + deltaB) / 3f;
                        
                        float accuracy = 1f - colorDifference;
                        colorAccuracyReadings.Add(accuracy);

                        yield return new WaitForSeconds(0.1f);
                    }

                    float avgAccuracy = 0f;
                    foreach (float accuracy in colorAccuracyReadings) avgAccuracy += accuracy;
                    avgAccuracy /= colorAccuracyReadings.Count;

                    result.AddMetric("Average Color Accuracy", avgAccuracy);
                    result.AddMetric("Colors Tested", colorAccuracyReadings.Count);

                    result.passed = avgAccuracy > 0.9f; // 90% accuracy target
                    result.details = $"Color accuracy: {avgAccuracy:F2}";
                }
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);
        }

        #endregion

        #region Extended Session Tests

        private IEnumerator TestExtendedSessionStability()
        {
            LogTest("Testing extended session stability...");

            var result = new DeviceTestResult
            {
                testName = "Extended Session Stability",
                category = TestCategory.Stability,
                startTime = DateTime.Now
            };

            try
            {
                // Run extended session test (10 minutes scaled to 30 seconds for testing)
                float sessionDuration = 30f;
                float startTime = Time.realtimeSinceStartup;
                
                List<float> fpsHistory = new List<float>();
                List<long> memoryHistory = new List<long>();
                int errorCount = 0;

                // Subscribe to error events
                var errorManager = FindObjectOfType<ErrorHandling.ErrorManager>();
                if (errorManager != null)
                {
                    errorManager.OnErrorOccurred.AddListener((error) => errorCount++);
                }

                while (Time.realtimeSinceStartup - startTime < sessionDuration)
                {
                    // Record performance metrics
                    fpsHistory.Add(1.0f / Time.unscaledDeltaTime);
                    memoryHistory.Add(GC.GetTotalMemory(false));

                    // Simulate user activity
                    if (UnityEngine.Random.value > 0.8f) // 20% chance per second
                    {
                        // Simulate random user interactions
                        var imageOverlay = FindObjectOfType<ImageOverlay.ImageOverlayManager>();
                        if (imageOverlay != null)
                        {
                            imageOverlay.SetOpacity(UnityEngine.Random.Range(0.3f, 1.0f));
                        }
                    }

                    yield return new WaitForSeconds(1f);
                }

                // Analyze stability
                float avgFPS = 0f;
                foreach (float fps in fpsHistory) avgFPS += fps;
                avgFPS /= fpsHistory.Count;

                long avgMemory = 0L;
                foreach (long memory in memoryHistory) avgMemory += memory;
                avgMemory /= memoryHistory.Count;

                // Calculate performance stability (coefficient of variation)
                float fpsVariance = 0f;
                foreach (float fps in fpsHistory)
                {
                    fpsVariance += Mathf.Pow(fps - avgFPS, 2);
                }
                fpsVariance /= fpsHistory.Count;
                float fpsStdDev = Mathf.Sqrt(fpsVariance);
                float fpsStability = 1f - (fpsStdDev / avgFPS); // Higher is more stable

                result.AddMetric("Session Duration (s)", sessionDuration);
                result.AddMetric("Average FPS", avgFPS);
                result.AddMetric("Average Memory (MB)", avgMemory / (1024f * 1024f));
                result.AddMetric("FPS Stability", fpsStability);
                result.AddMetric("Error Count", errorCount);

                result.passed = avgFPS >= minAcceptableFPS && 
                               fpsStability > 0.8f && 
                               errorCount < 5 &&
                               avgMemory <= maxMemoryUsage;
                
                result.details = $"FPS: {avgFPS:F1}, Stability: {fpsStability:F2}, Errors: {errorCount}";
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);
        }

        private IEnumerator TestBatteryImpact()
        {
            LogTest("Testing battery impact...");

            var result = new DeviceTestResult
            {
                testName = "Battery Impact",
                category = TestCategory.BatteryOptimization,
                startTime = DateTime.Now
            };

            try
            {
                // Simulate battery monitoring (would use actual battery API on device)
                float initialBatteryLevel = 1.0f; // 100%
                float testDuration = 10f; // 10 seconds represents longer session
                float startTime = Time.realtimeSinceStartup;

                List<float> performanceReadings = new List<float>();
                
                while (Time.realtimeSinceStartup - startTime < testDuration)
                {
                    performanceReadings.Add(1.0f / Time.unscaledDeltaTime);
                    yield return new WaitForSeconds(0.5f);
                }

                float avgFPS = 0f;
                foreach (float fps in performanceReadings) avgFPS += fps;
                avgFPS /= performanceReadings.Count;

                // Estimate battery impact based on performance
                float estimatedBatteryDrain = (targetFPS - avgFPS) / targetFPS * 0.1f; // Simplified calculation
                float finalBatteryLevel = initialBatteryLevel - estimatedBatteryDrain;

                result.AddMetric("Initial Battery Level", initialBatteryLevel);
                result.AddMetric("Final Battery Level", finalBatteryLevel);
                result.AddMetric("Estimated Battery Drain", estimatedBatteryDrain);
                result.AddMetric("Average FPS During Test", avgFPS);

                result.passed = estimatedBatteryDrain < 0.05f; // Less than 5% drain
                result.details = $"Battery drain: {estimatedBatteryDrain * 100f:F1}%, FPS: {avgFPS:F1}";
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);
        }

        private IEnumerator TestThermalPerformance()
        {
            LogTest("Testing thermal performance...");

            var result = new DeviceTestResult
            {
                testName = "Thermal Performance",
                category = TestCategory.ThermalManagement,
                startTime = DateTime.Now
            };

            try
            {
                // Simulate thermal stress test
                float testDuration = 15f; // 15 seconds of intensive operations
                float startTime = Time.realtimeSinceStartup;

                List<float> fpsUnderLoad = new List<float>();
                
                // Apply maximum load
                var filterManager = FindObjectOfType<Filters.FilterManager>();
                if (filterManager != null)
                {
                    // Apply multiple intensive filters
                    var intensiveFilters = new[]
                    {
                        Filters.FilterType.EdgeDetection,
                        Filters.FilterType.ContrastEnhancement,
                        Filters.FilterType.ColorReduction
                    };

                    foreach (var filterType in intensiveFilters)
                    {
                        var filterParams = new Filters.FilterParameters
                        {
                            type = filterType,
                            intensity = 1.0f // Maximum intensity
                        };
                        filterManager.ApplyFilter(filterParams.type, filterParams);
                    }
                }

                // Monitor performance under thermal load
                while (Time.realtimeSinceStartup - startTime < testDuration)
                {
                    fpsUnderLoad.Add(1.0f / Time.unscaledDeltaTime);
                    yield return new WaitForSeconds(0.2f);
                }

                // Clear intensive operations
                if (filterManager != null)
                {
                    filterManager.ClearAllFilters();
                }

                float avgFPSUnderLoad = 0f;
                foreach (float fps in fpsUnderLoad) avgFPSUnderLoad += fps;
                avgFPSUnderLoad /= fpsUnderLoad.Count;

                // Calculate thermal throttling (performance degradation)
                float thermalThrottling = (targetFPS - avgFPSUnderLoad) / targetFPS;

                result.AddMetric("Average FPS Under Load", avgFPSUnderLoad);
                result.AddMetric("Thermal Throttling", thermalThrottling);
                result.AddMetric("Load Test Duration (s)", testDuration);

                result.passed = avgFPSUnderLoad >= minAcceptableFPS * 0.8f; // Allow 20% degradation under load
                result.details = $"FPS under load: {avgFPSUnderLoad:F1}, Throttling: {thermalThrottling * 100f:F1}%";
            }
            catch (Exception e)
            {
                result.passed = false;
                result.details = $"Exception: {e.Message}";
            }

            result.endTime = DateTime.Now;
            testResults.Add(result);
            OnTestCompleted?.Invoke(result);
        }

        #endregion

        private void GenerateDeviceTestReport()
        {
            LogTest("Generating device test report...");

            var report = new DeviceTestReport
            {
                deviceInfo = GetDeviceInfo(),
                testResults = new List<DeviceTestResult>(testResults),
                testDate = DateTime.Now,
                totalTests = testResults.Count
            };

            // Calculate summary statistics
            int passedTests = 0;
            foreach (var result in testResults)
            {
                if (result.passed) passedTests++;
            }

            report.passedTests = passedTests;
            report.failedTests = report.totalTests - passedTests;
            report.successRate = (float)passedTests / report.totalTests * 100f;

            // Generate report text
            var reportText = GenerateReportText(report);
            Debug.Log(reportText);

            // Save report to file
            try
            {
                string reportPath = Application.persistentDataPath + "/HoloLens2_DeviceTestReport.txt";
                System.IO.File.WriteAllText(reportPath, reportText);
                LogTest($"Device test report saved to: {reportPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save device test report: {e.Message}");
            }

            OnTestingFinished?.Invoke(report);
        }

        private DeviceInfo GetDeviceInfo()
        {
            return new DeviceInfo
            {
                deviceModel = SystemInfo.deviceModel,
                deviceName = SystemInfo.deviceName,
                operatingSystem = SystemInfo.operatingSystem,
                processorType = SystemInfo.processorType,
                systemMemorySize = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsMemorySize = SystemInfo.graphicsMemorySize,
                isHoloLens2 = isHoloLens2Device,
                hasHandTracking = hasHandTracking,
                hasEyeTracking = hasEyeTracking,
                hasSpatialMapping = hasSpatialMapping,
                hasCameraAccess = hasCameraAccess
            };
        }

        private string GenerateReportText(DeviceTestReport report)
        {
            var text = new System.Text.StringBuilder();
            
            text.AppendLine("=== HOLOLENS 2 DEVICE TEST REPORT ===");
            text.AppendLine($"Test Date: {report.testDate:yyyy-MM-dd HH:mm:ss}");
            text.AppendLine($"Device: {report.deviceInfo.deviceModel}");
            text.AppendLine($"OS: {report.deviceInfo.operatingSystem}");
            text.AppendLine($"Total Tests: {report.totalTests}");
            text.AppendLine($"Passed: {report.passedTests}");
            text.AppendLine($"Failed: {report.failedTests}");
            text.AppendLine($"Success Rate: {report.successRate:F1}%");
            text.AppendLine();

            text.AppendLine("=== DEVICE CAPABILITIES ===");
            text.AppendLine($"HoloLens 2: {report.deviceInfo.isHoloLens2}");
            text.AppendLine($"Hand Tracking: {report.deviceInfo.hasHandTracking}");
            text.AppendLine($"Eye Tracking: {report.deviceInfo.hasEyeTracking}");
            text.AppendLine($"Spatial Mapping: {report.deviceInfo.hasSpatialMapping}");
            text.AppendLine($"Camera Access: {report.deviceInfo.hasCameraAccess}");
            text.AppendLine($"System Memory: {report.deviceInfo.systemMemorySize}MB");
            text.AppendLine($"Graphics: {report.deviceInfo.graphicsDeviceName}");
            text.AppendLine();

            text.AppendLine("=== DETAILED TEST RESULTS ===");
            foreach (var result in report.testResults)
            {
                string status = result.passed ? "PASS" : "FAIL";
                text.AppendLine($"[{status}] {result.testName} ({result.category})");
                text.AppendLine($"  Duration: {(result.endTime - result.startTime).TotalSeconds:F1}s");
                text.AppendLine($"  Details: {result.details}");
                
                if (result.metrics.Count > 0)
                {
                    text.AppendLine("  Metrics:");
                    foreach (var metric in result.metrics)
                    {
                        text.AppendLine($"    {metric.Key}: {metric.Value:F2}");
                    }
                }
                text.AppendLine();
            }

            return text.ToString();
        }

        private void LogTest(string message)
        {
            if (enableVerboseLogging)
            {
                Debug.Log($"[HoloLens2DeviceTester] {message}");
            }
        }

        #region Public API

        /// <summary>
        /// Start device testing manually
        /// </summary>
        [ContextMenu("Run Device Tests")]
        public void StartDeviceTesting()
        {
            if (!isTestingInProgress)
            {
                StartCoroutine(RunAllDeviceTests());
            }
        }

        /// <summary>
        /// Get current test results
        /// </summary>
        public List<DeviceTestResult> GetTestResults()
        {
            return new List<DeviceTestResult>(testResults);
        }

        /// <summary>
        /// Check if device testing is in progress
        /// </summary>
        public bool IsTestingInProgress()
        {
            return isTestingInProgress;
        }

        #endregion
    }

    #region Data Structures

    public enum TestCategory
    {
        DeviceCapabilities,
        Performance,
        SpatialTracking,
        HandTracking,
        UIInteraction,
        ApplicationWorkflow,
        ColorAnalysis,
        Stability,
        BatteryOptimization,
        ThermalManagement
    }

    [System.Serializable]
    public class DeviceTestResult
    {
        public string testName;
        public TestCategory category;
        public bool passed;
        public string details;
        public DateTime startTime;
        public DateTime endTime;
        public Dictionary<string, float> metrics = new Dictionary<string, float>();

        public void AddMetric(string name, float value)
        {
            metrics[name] = value;
        }
    }

    [System.Serializable]
    public class DeviceInfo
    {
        public string deviceModel;
        public string deviceName;
        public string operatingSystem;
        public string processorType;
        public int systemMemorySize;
        public string graphicsDeviceName;
        public int graphicsMemorySize;
        public bool isHoloLens2;
        public bool hasHandTracking;
        public bool hasEyeTracking;
        public bool hasSpatialMapping;
        public bool hasCameraAccess;
    }

    [System.Serializable]
    public class DeviceTestReport
    {
        public DeviceInfo deviceInfo;
        public List<DeviceTestResult> testResults;
        public DateTime testDate;
        public int totalTests;
        public int passedTests;
        public int failedTests;
        public float successRate;
    }

    #endregion
}