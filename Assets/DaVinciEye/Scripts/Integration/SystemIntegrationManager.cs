using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace DaVinciEye.Integration
{
    /// <summary>
    /// Central integration manager that coordinates all Da Vinci Eye systems
    /// and provides end-to-end workflow orchestration
    /// </summary>
    public class SystemIntegrationManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private Canvas.CanvasDefinitionManager canvasManager;
        [SerializeField] private ImageOverlay.ImageOverlayManager imageOverlayManager;
        [SerializeField] private Filters.FilterManager filterManager;
        [SerializeField] private ColorAnalysis.ColorAnalyzer colorAnalyzer;
        [SerializeField] private Input.HandGestureManager inputManager;
        [SerializeField] private SpatialTracking.TrackingQualityMonitor trackingMonitor;
        [SerializeField] private SessionManagement.SessionDataManager sessionManager;
        [SerializeField] private UI.UIManager uiManager;

        [Header("Performance Monitoring")]
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] private float maxUIResponseTime = 100f; // milliseconds
        [SerializeField] private long maxMemoryUsage = 512 * 1024 * 1024; // 512MB

        // System state tracking
        private Dictionary<string, bool> systemStates = new Dictionary<string, bool>();
        private List<string> activeWorkflows = new List<string>();
        
        // Performance metrics
        private float currentFPS;
        private float averageUIResponseTime;
        private long currentMemoryUsage;
        private int frameCount;
        private float deltaTimeSum;

        // Events
        public UnityEvent<string> OnSystemInitialized = new UnityEvent<string>();
        public UnityEvent<string> OnWorkflowStarted = new UnityEvent<string>();
        public UnityEvent<string> OnWorkflowCompleted = new UnityEvent<string>();
        public UnityEvent<string> OnSystemError = new UnityEvent<string>();
        public UnityEvent<PerformanceMetrics> OnPerformanceUpdate = new UnityEvent<PerformanceMetrics>();

        private void Awake()
        {
            InitializeSystemReferences();
        }

        private void Start()
        {
            StartCoroutine(InitializeAllSystems());
            
            if (enablePerformanceMonitoring)
            {
                StartCoroutine(MonitorPerformance());
            }
        }

        private void Update()
        {
            UpdatePerformanceMetrics();
        }

        /// <summary>
        /// Initialize references to all system components
        /// </summary>
        private void InitializeSystemReferences()
        {
            // Auto-find components if not assigned
            if (canvasManager == null)
                canvasManager = FindObjectOfType<Canvas.CanvasDefinitionManager>();
            if (imageOverlayManager == null)
                imageOverlayManager = FindObjectOfType<ImageOverlay.ImageOverlayManager>();
            if (filterManager == null)
                filterManager = FindObjectOfType<Filters.FilterManager>();
            if (colorAnalyzer == null)
                colorAnalyzer = FindObjectOfType<ColorAnalysis.ColorAnalyzer>();
            if (inputManager == null)
                inputManager = FindObjectOfType<Input.HandGestureManager>();
            if (trackingMonitor == null)
                trackingMonitor = FindObjectOfType<SpatialTracking.TrackingQualityMonitor>();
            if (sessionManager == null)
                sessionManager = FindObjectOfType<SessionManagement.SessionDataManager>();
            if (uiManager == null)
                uiManager = FindObjectOfType<UI.UIManager>();

            // Initialize system state tracking
            systemStates["Canvas"] = false;
            systemStates["ImageOverlay"] = false;
            systemStates["Filter"] = false;
            systemStates["ColorAnalysis"] = false;
            systemStates["Input"] = false;
            systemStates["Tracking"] = false;
            systemStates["Session"] = false;
            systemStates["UI"] = false;
        }

        /// <summary>
        /// Initialize all systems in proper dependency order
        /// </summary>
        private IEnumerator InitializeAllSystems()
        {
            Debug.Log("[SystemIntegration] Starting system initialization...");

            // Phase 1: Core systems
            yield return StartCoroutine(InitializeSystem("Tracking", InitializeTrackingSystem));
            yield return StartCoroutine(InitializeSystem("Input", InitializeInputSystem));
            yield return StartCoroutine(InitializeSystem("Session", InitializeSessionSystem));

            // Phase 2: Content systems
            yield return StartCoroutine(InitializeSystem("Canvas", InitializeCanvasSystem));
            yield return StartCoroutine(InitializeSystem("ImageOverlay", InitializeImageOverlaySystem));
            yield return StartCoroutine(InitializeSystem("Filter", InitializeFilterSystem));
            yield return StartCoroutine(InitializeSystem("ColorAnalysis", InitializeColorAnalysisSystem));

            // Phase 3: UI system (depends on all others)
            yield return StartCoroutine(InitializeSystem("UI", InitializeUISystem));

            // Wire up system interactions
            WireSystemInteractions();

            Debug.Log("[SystemIntegration] All systems initialized successfully");
        }

        private IEnumerator InitializeSystem(string systemName, Func<IEnumerator> initializeFunc)
        {
            try
            {
                Debug.Log($"[SystemIntegration] Initializing {systemName} system...");
                yield return StartCoroutine(initializeFunc());
                systemStates[systemName] = true;
                OnSystemInitialized.Invoke(systemName);
                Debug.Log($"[SystemIntegration] {systemName} system initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SystemIntegration] Failed to initialize {systemName} system: {e.Message}");
                OnSystemError.Invoke($"{systemName} initialization failed: {e.Message}");
            }
        }

        #region System Initialization Methods

        private IEnumerator InitializeTrackingSystem()
        {
            if (trackingMonitor != null)
            {
                trackingMonitor.enabled = true;
                yield return new WaitForSeconds(0.1f); // Allow tracking to stabilize
            }
            yield return null;
        }

        private IEnumerator InitializeInputSystem()
        {
            if (inputManager != null)
            {
                inputManager.enabled = true;
                yield return new WaitForSeconds(0.1f); // Allow input system to initialize
            }
            yield return null;
        }

        private IEnumerator InitializeSessionSystem()
        {
            if (sessionManager != null)
            {
                sessionManager.enabled = true;
                sessionManager.LoadSession(); // Load previous session if available
                yield return new WaitForSeconds(0.1f);
            }
            yield return null;
        }

        private IEnumerator InitializeCanvasSystem()
        {
            if (canvasManager != null)
            {
                canvasManager.enabled = true;
                yield return new WaitForSeconds(0.1f);
            }
            yield return null;
        }

        private IEnumerator InitializeImageOverlaySystem()
        {
            if (imageOverlayManager != null)
            {
                imageOverlayManager.enabled = true;
                yield return new WaitForSeconds(0.1f);
            }
            yield return null;
        }

        private IEnumerator InitializeFilterSystem()
        {
            if (filterManager != null)
            {
                filterManager.enabled = true;
                yield return new WaitForSeconds(0.1f);
            }
            yield return null;
        }

        private IEnumerator InitializeColorAnalysisSystem()
        {
            if (colorAnalyzer != null)
            {
                colorAnalyzer.enabled = true;
                yield return new WaitForSeconds(0.1f);
            }
            yield return null;
        }

        private IEnumerator InitializeUISystem()
        {
            if (uiManager != null)
            {
                uiManager.enabled = true;
                uiManager.InitializeUI();
                yield return new WaitForSeconds(0.2f); // Allow UI to fully initialize
            }
            yield return null;
        }

        #endregion

        /// <summary>
        /// Wire up interactions between all systems
        /// </summary>
        private void WireSystemInteractions()
        {
            Debug.Log("[SystemIntegration] Wiring system interactions...");

            // Canvas system events
            if (canvasManager != null)
            {
                canvasManager.OnCanvasDefined.AddListener(OnCanvasDefined);
                canvasManager.OnCanvasCleared.AddListener(OnCanvasCleared);
            }

            // Image overlay events
            if (imageOverlayManager != null)
            {
                imageOverlayManager.OnImageLoaded.AddListener(OnImageLoaded);
                imageOverlayManager.OnOpacityChanged.AddListener(OnOpacityChanged);
            }

            // Filter system events
            if (filterManager != null)
            {
                filterManager.OnFilterApplied.AddListener(OnFilterApplied);
                filterManager.OnFilterRemoved.AddListener(OnFilterRemoved);
            }

            // Color analysis events
            if (colorAnalyzer != null)
            {
                colorAnalyzer.OnColorAnalyzed.AddListener(OnColorAnalyzed);
                colorAnalyzer.OnColorMatchSaved.AddListener(OnColorMatchSaved);
            }

            // Tracking quality events
            if (trackingMonitor != null)
            {
                trackingMonitor.OnTrackingQualityChanged.AddListener(OnTrackingQualityChanged);
            }

            Debug.Log("[SystemIntegration] System interactions wired successfully");
        }

        #region System Event Handlers

        private void OnCanvasDefined(Canvas.CanvasData canvasData)
        {
            Debug.Log("[SystemIntegration] Canvas defined, updating dependent systems");
            
            // Update image overlay to fit new canvas
            if (imageOverlayManager != null && imageOverlayManager.CurrentImage != null)
            {
                imageOverlayManager.FitImageToCanvas(canvasData.dimensions);
            }

            // Save canvas data to session
            if (sessionManager != null)
            {
                sessionManager.SaveCanvasData(canvasData);
            }
        }

        private void OnCanvasCleared()
        {
            Debug.Log("[SystemIntegration] Canvas cleared, hiding image overlay");
            
            // Hide image overlay when canvas is cleared
            if (imageOverlayManager != null)
            {
                imageOverlayManager.SetVisibility(false);
            }
        }

        private void OnImageLoaded(Texture2D image)
        {
            Debug.Log("[SystemIntegration] Image loaded, updating filter system");
            
            // Update filter system with new image
            if (filterManager != null)
            {
                filterManager.SetSourceImage(image);
            }

            // Update color analysis system
            if (colorAnalyzer != null)
            {
                colorAnalyzer.SetReferenceImage(image);
            }

            // Save image path to session
            if (sessionManager != null)
            {
                sessionManager.SaveCurrentImagePath(imageOverlayManager.CurrentImagePath);
            }
        }

        private void OnOpacityChanged(float opacity)
        {
            // Save opacity to session
            if (sessionManager != null)
            {
                sessionManager.SaveOpacitySetting(opacity);
            }
        }

        private void OnFilterApplied(Texture2D filteredImage)
        {
            Debug.Log("[SystemIntegration] Filter applied, updating image overlay");
            
            // Update image overlay with filtered image
            if (imageOverlayManager != null)
            {
                imageOverlayManager.UpdateDisplayTexture(filteredImage);
            }

            // Save filter settings to session
            if (sessionManager != null && filterManager != null)
            {
                sessionManager.SaveFilterSettings(filterManager.GetActiveFilters());
            }
        }

        private void OnFilterRemoved(Filters.FilterType filterType)
        {
            Debug.Log($"[SystemIntegration] Filter {filterType} removed");
            
            // Update session data
            if (sessionManager != null && filterManager != null)
            {
                sessionManager.SaveFilterSettings(filterManager.GetActiveFilters());
            }
        }

        private void OnColorAnalyzed(ColorAnalysis.ColorMatchResult result)
        {
            Debug.Log("[SystemIntegration] Color analysis completed");
            
            // Update UI with color analysis results
            if (uiManager != null)
            {
                uiManager.DisplayColorMatchResult(result);
            }
        }

        private void OnColorMatchSaved(SessionManagement.ColorMatchData matchData)
        {
            Debug.Log("[SystemIntegration] Color match saved to history");
            
            // Save to session manager
            if (sessionManager != null)
            {
                sessionManager.SaveColorMatch(matchData);
            }
        }

        private void OnTrackingQualityChanged(float quality)
        {
            // Update UI with tracking quality indicator
            if (uiManager != null)
            {
                uiManager.UpdateTrackingQualityIndicator(quality);
            }

            // If tracking quality is poor, show warning
            if (quality < 0.5f)
            {
                Debug.LogWarning("[SystemIntegration] Poor tracking quality detected");
                if (uiManager != null)
                {
                    uiManager.ShowTrackingWarning("Poor tracking quality. Move to a well-lit area with good spatial features.");
                }
            }
        }

        #endregion

        #region End-to-End Workflow Methods

        /// <summary>
        /// Execute complete canvas definition workflow
        /// </summary>
        public async Task<bool> ExecuteCanvasDefinitionWorkflow()
        {
            string workflowName = "CanvasDefinition";
            activeWorkflows.Add(workflowName);
            OnWorkflowStarted.Invoke(workflowName);

            try
            {
                Debug.Log("[SystemIntegration] Starting canvas definition workflow");

                // Step 1: Ensure tracking quality is good
                if (trackingMonitor != null && trackingMonitor.CurrentQuality < 0.7f)
                {
                    Debug.LogWarning("Tracking quality too low for canvas definition");
                    return false;
                }

                // Step 2: Start canvas definition
                if (canvasManager != null)
                {
                    canvasManager.StartCanvasDefinition();
                    
                    // Wait for canvas to be defined (with timeout)
                    float timeout = 30f; // 30 seconds
                    float elapsed = 0f;
                    
                    while (!canvasManager.IsCanvasDefined && elapsed < timeout)
                    {
                        await Task.Delay(100);
                        elapsed += 0.1f;
                    }

                    if (!canvasManager.IsCanvasDefined)
                    {
                        Debug.LogError("Canvas definition timed out");
                        return false;
                    }
                }

                Debug.Log("[SystemIntegration] Canvas definition workflow completed successfully");
                OnWorkflowCompleted.Invoke(workflowName);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SystemIntegration] Canvas definition workflow failed: {e.Message}");
                OnSystemError.Invoke($"Canvas definition workflow failed: {e.Message}");
                return false;
            }
            finally
            {
                activeWorkflows.Remove(workflowName);
            }
        }

        /// <summary>
        /// Execute complete image overlay workflow
        /// </summary>
        public async Task<bool> ExecuteImageOverlayWorkflow(string imagePath)
        {
            string workflowName = "ImageOverlay";
            activeWorkflows.Add(workflowName);
            OnWorkflowStarted.Invoke(workflowName);

            try
            {
                Debug.Log($"[SystemIntegration] Starting image overlay workflow for: {imagePath}");

                // Step 1: Ensure canvas is defined
                if (canvasManager == null || !canvasManager.IsCanvasDefined)
                {
                    Debug.LogError("Canvas must be defined before loading image");
                    return false;
                }

                // Step 2: Load image
                if (imageOverlayManager != null)
                {
                    bool loaded = await imageOverlayManager.LoadImageAsync(imagePath);
                    if (!loaded)
                    {
                        Debug.LogError($"Failed to load image: {imagePath}");
                        return false;
                    }

                    // Step 3: Fit image to canvas
                    imageOverlayManager.FitImageToCanvas(canvasManager.CanvasBounds.size);
                    
                    // Step 4: Make image visible
                    imageOverlayManager.SetVisibility(true);
                }

                Debug.Log("[SystemIntegration] Image overlay workflow completed successfully");
                OnWorkflowCompleted.Invoke(workflowName);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SystemIntegration] Image overlay workflow failed: {e.Message}");
                OnSystemError.Invoke($"Image overlay workflow failed: {e.Message}");
                return false;
            }
            finally
            {
                activeWorkflows.Remove(workflowName);
            }
        }

        /// <summary>
        /// Execute complete color matching workflow
        /// </summary>
        public async Task<bool> ExecuteColorMatchingWorkflow(Vector2 imageCoordinate, Vector3 paintPosition)
        {
            string workflowName = "ColorMatching";
            activeWorkflows.Add(workflowName);
            OnWorkflowStarted.Invoke(workflowName);

            try
            {
                Debug.Log("[SystemIntegration] Starting color matching workflow");

                // Step 1: Ensure image is loaded
                if (imageOverlayManager == null || imageOverlayManager.CurrentImage == null)
                {
                    Debug.LogError("Image must be loaded before color matching");
                    return false;
                }

                // Step 2: Pick color from image
                Color referenceColor = Color.white;
                if (colorAnalyzer != null)
                {
                    referenceColor = colorAnalyzer.PickColorFromImage(imageCoordinate);
                }

                // Step 3: Analyze paint color
                Color paintColor = await colorAnalyzer.AnalyzePaintColorAsync(paintPosition);

                // Step 4: Compare colors
                var matchResult = colorAnalyzer.CompareColors(referenceColor, paintColor);

                // Step 5: Save color match
                var matchData = new SessionManagement.ColorMatchData
                {
                    referenceColor = referenceColor,
                    capturedColor = paintColor,
                    matchAccuracy = matchResult.deltaE,
                    capturePosition = paintPosition,
                    timestamp = DateTime.Now,
                    notes = $"Match accuracy: {matchResult.deltaE:F2} Delta E"
                };

                colorAnalyzer.SaveColorMatch(matchData);

                Debug.Log("[SystemIntegration] Color matching workflow completed successfully");
                OnWorkflowCompleted.Invoke(workflowName);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SystemIntegration] Color matching workflow failed: {e.Message}");
                OnSystemError.Invoke($"Color matching workflow failed: {e.Message}");
                return false;
            }
            finally
            {
                activeWorkflows.Remove(workflowName);
            }
        }

        /// <summary>
        /// Execute complete artist session workflow
        /// </summary>
        public async Task<bool> ExecuteCompleteArtistWorkflow(string imagePath)
        {
            string workflowName = "CompleteArtistSession";
            activeWorkflows.Add(workflowName);
            OnWorkflowStarted.Invoke(workflowName);

            try
            {
                Debug.Log("[SystemIntegration] Starting complete artist workflow");

                // Step 1: Canvas definition
                bool canvasSuccess = await ExecuteCanvasDefinitionWorkflow();
                if (!canvasSuccess)
                {
                    Debug.LogError("Canvas definition failed in complete workflow");
                    return false;
                }

                // Step 2: Image overlay
                bool imageSuccess = await ExecuteImageOverlayWorkflow(imagePath);
                if (!imageSuccess)
                {
                    Debug.LogError("Image overlay failed in complete workflow");
                    return false;
                }

                // Step 3: Apply default filters (optional)
                if (filterManager != null)
                {
                    // Apply a subtle contrast enhancement as default
                    var contrastFilter = new Filters.FilterParameters
                    {
                        type = Filters.FilterType.ContrastEnhancement,
                        intensity = 0.3f
                    };
                    filterManager.ApplyFilter(contrastFilter.type, contrastFilter);
                }

                // Step 4: Set default opacity
                if (imageOverlayManager != null)
                {
                    imageOverlayManager.SetOpacity(0.7f); // 70% opacity as starting point
                }

                Debug.Log("[SystemIntegration] Complete artist workflow completed successfully");
                OnWorkflowCompleted.Invoke(workflowName);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SystemIntegration] Complete artist workflow failed: {e.Message}");
                OnSystemError.Invoke($"Complete artist workflow failed: {e.Message}");
                return false;
            }
            finally
            {
                activeWorkflows.Remove(workflowName);
            }
        }

        #endregion

        #region Performance Monitoring

        private void UpdatePerformanceMetrics()
        {
            if (!enablePerformanceMonitoring) return;

            frameCount++;
            deltaTimeSum += Time.unscaledDeltaTime;

            // Update FPS every second
            if (deltaTimeSum >= 1.0f)
            {
                currentFPS = frameCount / deltaTimeSum;
                frameCount = 0;
                deltaTimeSum = 0f;
            }

            // Update memory usage (approximate)
            currentMemoryUsage = GC.GetTotalMemory(false);
        }

        private IEnumerator MonitorPerformance()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);

                var metrics = new PerformanceMetrics
                {
                    fps = currentFPS,
                    memoryUsage = currentMemoryUsage,
                    uiResponseTime = averageUIResponseTime,
                    activeFilterCount = filterManager != null ? filterManager.GetActiveFilters().Count : 0,
                    trackingQuality = trackingMonitor != null ? trackingMonitor.CurrentQuality : 1.0f
                };

                OnPerformanceUpdate.Invoke(metrics);

                // Check performance thresholds
                if (currentFPS < targetFrameRate * 0.9f) // 10% tolerance
                {
                    Debug.LogWarning($"[SystemIntegration] FPS below target: {currentFPS:F1} < {targetFrameRate}");
                }

                if (currentMemoryUsage > maxMemoryUsage)
                {
                    Debug.LogWarning($"[SystemIntegration] Memory usage above limit: {currentMemoryUsage / (1024 * 1024)}MB");
                    // Trigger garbage collection
                    GC.Collect();
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Check if all systems are initialized and ready
        /// </summary>
        public bool AreAllSystemsReady()
        {
            foreach (var state in systemStates.Values)
            {
                if (!state) return false;
            }
            return true;
        }

        /// <summary>
        /// Get current system status
        /// </summary>
        public Dictionary<string, bool> GetSystemStatus()
        {
            return new Dictionary<string, bool>(systemStates);
        }

        /// <summary>
        /// Get current performance metrics
        /// </summary>
        public PerformanceMetrics GetCurrentPerformanceMetrics()
        {
            return new PerformanceMetrics
            {
                fps = currentFPS,
                memoryUsage = currentMemoryUsage,
                uiResponseTime = averageUIResponseTime,
                activeFilterCount = filterManager != null ? filterManager.GetActiveFilters().Count : 0,
                trackingQuality = trackingMonitor != null ? trackingMonitor.CurrentQuality : 1.0f
            };
        }

        /// <summary>
        /// Reset all systems to initial state
        /// </summary>
        public void ResetAllSystems()
        {
            Debug.Log("[SystemIntegration] Resetting all systems");

            // Reset each system
            canvasManager?.ResetCanvas();
            imageOverlayManager?.ClearImage();
            filterManager?.ClearAllFilters();
            colorAnalyzer?.ClearHistory();
            sessionManager?.ClearSession();

            // Clear active workflows
            activeWorkflows.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Performance metrics data structure
    /// </summary>
    [System.Serializable]
    public class PerformanceMetrics
    {
        public float fps;
        public long memoryUsage;
        public float uiResponseTime;
        public int activeFilterCount;
        public float trackingQuality;
        public DateTime timestamp = DateTime.Now;

        public bool IsPerformanceGood(float targetFPS = 60f, long maxMemory = 512 * 1024 * 1024)
        {
            return fps >= targetFPS * 0.9f && 
                   memoryUsage <= maxMemory && 
                   trackingQuality >= 0.7f;
        }
    }
}