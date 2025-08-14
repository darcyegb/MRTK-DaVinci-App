using System;
using System.Collections;
using UnityEngine;

namespace DaVinciEye.SessionManagement
{
    /// <summary>
    /// Verification script for Task 10: Color history and session management implementation
    /// Demonstrates the complete functionality of both ColorHistoryManager and SessionDataManager
    /// </summary>
    public class Task10Verification : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private ColorHistoryManager colorHistoryManager;
        [SerializeField] private SessionDataManager sessionDataManager;
        
        [Header("Verification Settings")]
        [SerializeField] private bool runVerificationOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;
        
        private void Start()
        {
            if (runVerificationOnStart)
            {
                StartCoroutine(RunCompleteVerification());
            }
        }
        
        /// <summary>
        /// Run complete verification of both color history and session management systems
        /// </summary>
        public IEnumerator RunCompleteVerification()
        {
            Debug.Log("=== Task 10 Verification: Color History and Session Management ===");
            
            // Verify component setup
            yield return StartCoroutine(VerifyComponentSetup());
            
            // Verify color history system (Task 10.1)
            yield return StartCoroutine(VerifyColorHistorySystem());
            
            // Verify session data management (Task 10.2)
            yield return StartCoroutine(VerifySessionDataManagement());
            
            // Verify integration between systems
            yield return StartCoroutine(VerifySystemIntegration());
            
            Debug.Log("=== Task 10 Verification Complete ===");
        }
        
        /// <summary>
        /// Verify that all required components are properly set up
        /// </summary>
        private IEnumerator VerifyComponentSetup()
        {
            Debug.Log("--- Verifying Component Setup ---");
            
            // Auto-find components if not assigned
            if (colorHistoryManager == null)
            {
                colorHistoryManager = FindObjectOfType<ColorHistoryManager>();
                if (colorHistoryManager == null)
                {
                    var go = new GameObject("ColorHistoryManager");
                    colorHistoryManager = go.AddComponent<ColorHistoryManager>();
                }
            }
            
            if (sessionDataManager == null)
            {
                sessionDataManager = FindObjectOfType<SessionDataManager>();
                if (sessionDataManager == null)
                {
                    var go = new GameObject("SessionDataManager");
                    sessionDataManager = go.AddComponent<SessionDataManager>();
                }
            }
            
            Assert(colorHistoryManager != null, "ColorHistoryManager component found/created");
            Assert(sessionDataManager != null, "SessionDataManager component found/created");
            
            yield return new WaitForSeconds(0.1f); // Allow components to initialize
            
            Debug.Log("✓ Component setup verification complete");
        }
        
        /// <summary>
        /// Verify Task 10.1: Color match history system
        /// </summary>
        private IEnumerator VerifyColorHistorySystem()
        {
            Debug.Log("--- Verifying Color History System (Task 10.1) ---");
            
            // Test session creation
            colorHistoryManager.StartNewSession();
            Assert(colorHistoryManager.CurrentSession != null, "Color history session created");
            Assert(!string.IsNullOrEmpty(colorHistoryManager.CurrentSession.sessionId), "Session has valid ID");
            
            // Test color match addition
            var testMatch1 = CreateTestColorMatch(Color.red, Color.magenta, 0.85f);
            var testMatch2 = CreateTestColorMatch(Color.blue, Color.cyan, 0.72f);
            var testMatch3 = CreateTestColorMatch(Color.green, Color.yellow, 0.45f);
            
            colorHistoryManager.AddColorMatch(testMatch1);
            colorHistoryManager.AddColorMatch(testMatch2);
            colorHistoryManager.AddColorMatch(testMatch3);
            
            Assert(colorHistoryManager.SessionMatches == 3, "Session matches count correct");
            Assert(colorHistoryManager.TotalMatches == 3, "Total matches count correct");
            
            // Test history retrieval
            var sessionMatches = colorHistoryManager.GetCurrentSessionMatches();
            var allMatches = colorHistoryManager.GetAllMatches();
            
            Assert(sessionMatches.Count == 3, "Session matches retrieved correctly");
            Assert(allMatches.Count == 3, "All matches retrieved correctly");
            
            // Test filtering
            var filter = new ColorMatchFilter
            {
                minAccuracy = 0.7f
            };
            var filteredMatches = colorHistoryManager.GetMatchesFiltered(filter);
            Assert(filteredMatches.Count == 2, "Filtered matches by accuracy");
            
            // Test statistics
            var stats = colorHistoryManager.GetStatistics();
            Assert(stats.totalMatches == 3, "Statistics total matches correct");
            Assert(stats.excellentMatches == 1, "Statistics excellent matches correct (>= 0.9)");
            Assert(stats.goodMatches == 1, "Statistics good matches correct (>= 0.7 && < 0.9)");
            Assert(stats.poorMatches == 1, "Statistics poor matches correct (< 0.5)");
            
            // Test persistence
            colorHistoryManager.SaveHistoryToDisk();
            yield return new WaitForSeconds(0.1f);
            
            colorHistoryManager.ClearHistory();
            Assert(colorHistoryManager.TotalMatches == 0, "History cleared");
            
            colorHistoryManager.LoadHistoryFromDisk();
            yield return new WaitForSeconds(0.1f);
            Assert(colorHistoryManager.TotalMatches == 3, "History loaded from disk");
            
            // Test export/import
            string exportedJson = colorHistoryManager.ExportHistoryToJson();
            Assert(!string.IsNullOrEmpty(exportedJson), "History exported to JSON");
            
            colorHistoryManager.ClearHistory();
            bool importSuccess = colorHistoryManager.ImportHistoryFromJson(exportedJson);
            Assert(importSuccess, "History imported from JSON");
            Assert(colorHistoryManager.TotalMatches == 3, "Imported history count correct");
            
            Debug.Log("✓ Color history system verification complete");
            yield return null;
        }
        
        /// <summary>
        /// Verify Task 10.2: Session data management
        /// </summary>
        private IEnumerator VerifySessionDataManagement()
        {
            Debug.Log("--- Verifying Session Data Management (Task 10.2) ---");
            
            // Test session creation
            sessionDataManager.CreateNewSession();
            Assert(sessionDataManager.HasActiveSession, "Session created successfully");
            Assert(sessionDataManager.CurrentSession != null, "Current session is not null");
            Assert(!string.IsNullOrEmpty(sessionDataManager.CurrentSession.sessionId), "Session has valid ID");
            
            // Test image adjustments persistence
            var imageAdjustments = new ImageAdjustments
            {
                contrast = 0.4f,
                exposure = 0.2f,
                hue = 0.1f,
                saturation = 0.3f,
                opacity = 0.8f,
                cropArea = new Rect(0.1f, 0.1f, 0.8f, 0.8f),
                isModified = true
            };
            
            sessionDataManager.UpdateImageAdjustments(imageAdjustments);
            Assert(sessionDataManager.IsSessionDirty, "Session marked as dirty after image adjustments");
            
            var retrievedAdjustments = sessionDataManager.GetImageAdjustments();
            Assert(Math.Abs(retrievedAdjustments.contrast - 0.4f) < 0.001f, "Image adjustments contrast preserved");
            Assert(Math.Abs(retrievedAdjustments.exposure - 0.2f) < 0.001f, "Image adjustments exposure preserved");
            Assert(retrievedAdjustments.isModified, "Image adjustments modified flag preserved");
            
            // Test filter settings persistence
            var filterSettings = new FilterSettings
            {
                filtersEnabled = true,
                globalFilterIntensity = 0.7f
            };
            filterSettings.grayscaleFilter.enabled = true;
            filterSettings.grayscaleFilter.intensity = 0.9f;
            filterSettings.edgeDetectionFilter.enabled = true;
            
            sessionDataManager.UpdateFilterSettings(filterSettings);
            
            var retrievedFilters = sessionDataManager.GetFilterSettings();
            Assert(retrievedFilters.filtersEnabled, "Filter settings enabled flag preserved");
            Assert(Math.Abs(retrievedFilters.globalFilterIntensity - 0.7f) < 0.001f, "Global filter intensity preserved");
            Assert(retrievedFilters.grayscaleFilter.enabled, "Grayscale filter enabled state preserved");
            Assert(retrievedFilters.edgeDetectionFilter.enabled, "Edge detection filter enabled state preserved");
            
            // Test canvas data persistence
            var canvasData = new CanvasData
            {
                corners = new Vector3[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(1, 1, 0),
                    new Vector3(0, 1, 0)
                },
                center = new Vector3(0.5f, 0.5f, 0),
                dimensions = new Vector2(1, 1),
                anchorId = "test-anchor-verification"
            };
            
            sessionDataManager.UpdateCanvasData(canvasData);
            
            var retrievedCanvas = sessionDataManager.GetCanvasData();
            Assert(retrievedCanvas != null, "Canvas data preserved");
            Assert(retrievedCanvas.corners.Length == 4, "Canvas corners count preserved");
            Assert(retrievedCanvas.anchorId == "test-anchor-verification", "Canvas anchor ID preserved");
            
            // Test app state persistence
            var appState = new AppState
            {
                currentMode = AppMode.ColorAnalysis,
                mainMenuVisible = false,
                adjustmentPanelVisible = true,
                filterPanelVisible = true,
                handGesturesEnabled = false,
                qualityLevel = QualityLevel.Medium
            };
            
            sessionDataManager.UpdateAppState(appState);
            
            var retrievedAppState = sessionDataManager.GetAppState();
            Assert(retrievedAppState.currentMode == AppMode.ColorAnalysis, "App mode preserved");
            Assert(!retrievedAppState.mainMenuVisible, "Main menu visibility preserved");
            Assert(retrievedAppState.adjustmentPanelVisible, "Adjustment panel visibility preserved");
            Assert(retrievedAppState.qualityLevel == QualityLevel.Medium, "Quality level preserved");
            
            // Test image path persistence
            string testImagePath = "/test/verification/image.jpg";
            sessionDataManager.UpdateCurrentImagePath(testImagePath);
            Assert(sessionDataManager.GetCurrentImagePath() == testImagePath, "Image path preserved");
            
            // Test session save and restore
            string originalSessionId = sessionDataManager.CurrentSession.sessionId;
            bool saveSuccess = sessionDataManager.SaveCurrentSession();
            Assert(saveSuccess, "Session saved successfully");
            Assert(!sessionDataManager.IsSessionDirty, "Session no longer dirty after save");
            
            yield return new WaitForSeconds(0.1f); // Allow file operations
            
            // Clear session and reload
            sessionDataManager.ClearCurrentSession();
            Assert(!sessionDataManager.HasActiveSession, "Session cleared");
            
            bool loadSuccess = sessionDataManager.LoadLastSession();
            Assert(loadSuccess, "Session loaded successfully");
            Assert(sessionDataManager.HasActiveSession, "Session restored");
            Assert(sessionDataManager.CurrentSession.sessionId == originalSessionId, "Session ID preserved");
            
            // Verify all data was restored correctly
            var restoredAdjustments = sessionDataManager.GetImageAdjustments();
            Assert(Math.Abs(restoredAdjustments.contrast - 0.4f) < 0.001f, "Restored image adjustments correct");
            
            var restoredFilters = sessionDataManager.GetFilterSettings();
            Assert(restoredFilters.grayscaleFilter.enabled, "Restored filter settings correct");
            
            var restoredCanvas = sessionDataManager.GetCanvasData();
            Assert(restoredCanvas.anchorId == "test-anchor-verification", "Restored canvas data correct");
            
            var restoredAppState = sessionDataManager.GetAppState();
            Assert(restoredAppState.currentMode == AppMode.ColorAnalysis, "Restored app state correct");
            
            Assert(sessionDataManager.GetCurrentImagePath() == testImagePath, "Restored image path correct");
            
            // Test session summary
            var summary = sessionDataManager.GetSessionSummary();
            Assert(summary.hasCanvasData, "Session summary shows canvas data");
            Assert(summary.hasImageLoaded, "Session summary shows image loaded");
            Assert(summary.hasImageAdjustments, "Session summary shows image adjustments");
            Assert(summary.activeFilterCount == 2, "Session summary shows correct filter count");
            Assert(summary.currentMode == AppMode.ColorAnalysis, "Session summary shows correct mode");
            
            Debug.Log("✓ Session data management verification complete");
            yield return null;
        }
        
        /// <summary>
        /// Verify integration between color history and session management systems
        /// </summary>
        private IEnumerator VerifySystemIntegration()
        {
            Debug.Log("--- Verifying System Integration ---");
            
            // Test that both systems can work together
            colorHistoryManager.StartNewSession();
            sessionDataManager.CreateNewSession();
            
            // Add some color matches
            var colorMatch = CreateTestColorMatch(Color.red, Color.blue, 0.8f);
            colorHistoryManager.AddColorMatch(colorMatch);
            
            // Update session with some data
            sessionDataManager.UpdateImageAdjustments(new ImageAdjustments { contrast = 0.5f, isModified = true });
            
            // Both systems should maintain their data independently
            Assert(colorHistoryManager.SessionMatches == 1, "Color history maintains data");
            Assert(sessionDataManager.GetImageAdjustments().isModified, "Session data maintains data");
            
            // Test concurrent save operations
            colorHistoryManager.SaveHistoryToDisk();
            sessionDataManager.SaveCurrentSession();
            
            yield return new WaitForSeconds(0.1f);
            
            // Clear both systems
            colorHistoryManager.ClearHistory();
            sessionDataManager.ClearCurrentSession();
            
            Assert(colorHistoryManager.TotalMatches == 0, "Color history cleared");
            Assert(!sessionDataManager.HasActiveSession, "Session cleared");
            
            // Load both systems
            colorHistoryManager.LoadHistoryFromDisk();
            sessionDataManager.LoadLastSession();
            
            yield return new WaitForSeconds(0.1f);
            
            Assert(colorHistoryManager.TotalMatches == 1, "Color history restored");
            Assert(sessionDataManager.HasActiveSession, "Session restored");
            
            Debug.Log("✓ System integration verification complete");
            yield return null;
        }
        
        /// <summary>
        /// Create a test color match for verification
        /// </summary>
        private ColorMatchData CreateTestColorMatch(Color reference, Color captured, float accuracy)
        {
            return new ColorMatchData
            {
                referenceColor = reference,
                capturedColor = captured,
                matchAccuracy = accuracy,
                capturePosition = UnityEngine.Random.insideUnitSphere * 2f,
                imageCoordinate = new Vector2(UnityEngine.Random.value, UnityEngine.Random.value),
                timestamp = DateTime.Now,
                deltaE = (1f - accuracy) * 20f, // Approximate delta E from accuracy
                matchQuality = accuracy >= 0.9f ? "Excellent" : accuracy >= 0.7f ? "Good" : accuracy >= 0.5f ? "Fair" : "Poor",
                adjustmentSuggestions = new[] { "Test suggestion" },
                matchingMethod = ColorMatchingMethod.DeltaE,
                notes = $"Test match with {accuracy:P1} accuracy"
            };
        }
        
        /// <summary>
        /// Simple assertion helper with logging
        /// </summary>
        private void Assert(bool condition, string message)
        {
            if (condition)
            {
                if (enableDetailedLogging)
                {
                    Debug.Log($"✓ {message}");
                }
            }
            else
            {
                Debug.LogError($"✗ FAILED: {message}");
            }
        }
        
        /// <summary>
        /// Public method to run verification manually
        /// </summary>
        [ContextMenu("Run Verification")]
        public void RunVerification()
        {
            StartCoroutine(RunCompleteVerification());
        }
        
        /// <summary>
        /// Public method to run color history verification only
        /// </summary>
        [ContextMenu("Verify Color History Only")]
        public void VerifyColorHistoryOnly()
        {
            StartCoroutine(VerifyColorHistorySystem());
        }
        
        /// <summary>
        /// Public method to run session management verification only
        /// </summary>
        [ContextMenu("Verify Session Management Only")]
        public void VerifySessionManagementOnly()
        {
            StartCoroutine(VerifySessionDataManagement());
        }
    }
}