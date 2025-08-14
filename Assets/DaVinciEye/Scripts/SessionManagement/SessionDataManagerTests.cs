using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DaVinciEye.SessionManagement.Tests
{
    /// <summary>
    /// Integration tests for SessionDataManager
    /// Tests session data consistency, restoration accuracy, and persistence
    /// </summary>
    public class SessionDataManagerTests
    {
        private GameObject testGameObject;
        private SessionDataManager sessionManager;
        private string testSessionPath;
        
        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with SessionDataManager
            testGameObject = new GameObject("TestSessionDataManager");
            sessionManager = testGameObject.AddComponent<SessionDataManager>();
            
            // Set up test file path
            testSessionPath = Path.Combine(Application.temporaryCachePath, "test_session_data.json");
            
            // Use reflection to set the private sessionFilePath field for testing
            var field = typeof(SessionDataManager).GetField("sessionFilePath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(sessionManager, testSessionPath);
            
            // Clean up any existing test files
            CleanupTestFiles();
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up test files
            CleanupTestFiles();
            
            // Destroy test GameObject
            if (testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(testGameObject);
            }
        }
        
        private void CleanupTestFiles()
        {
            if (File.Exists(testSessionPath))
            {
                File.Delete(testSessionPath);
            }
            
            if (File.Exists(testSessionPath + ".backup"))
            {
                File.Delete(testSessionPath + ".backup");
            }
        }
        
        [Test]
        public void SessionDataManager_Initialization_SetsUpCorrectly()
        {
            // Act - Awake is called automatically
            
            // Assert
            Assert.IsNotNull(sessionManager);
            Assert.IsFalse(sessionManager.HasActiveSession); // Session not created yet in test
        }
        
        [Test]
        public void CreateNewSession_CreatesValidSession()
        {
            // Act
            sessionManager.CreateNewSession();
            
            // Assert
            Assert.IsTrue(sessionManager.HasActiveSession);
            Assert.IsNotNull(sessionManager.CurrentSession);
            Assert.IsNotEmpty(sessionManager.CurrentSession.sessionId);
            Assert.IsTrue(sessionManager.IsSessionDirty);
            
            var session = sessionManager.CurrentSession;
            Assert.IsNotNull(session.imageAdjustments);
            Assert.IsNotNull(session.filterSettings);
            Assert.IsNotNull(session.appState);
            Assert.AreEqual("", session.currentImagePath);
            Assert.IsNull(session.canvasData);
        }
        
        [Test]
        public void UpdateImageAdjustments_UpdatesSessionCorrectly()
        {
            // Arrange
            sessionManager.CreateNewSession();
            var adjustments = new ImageAdjustments
            {
                contrast = 0.5f,
                exposure = 0.3f,
                hue = 0.1f,
                saturation = 0.2f,
                opacity = 0.8f,
                isModified = true
            };
            
            // Act
            sessionManager.UpdateImageAdjustments(adjustments);
            
            // Assert
            Assert.IsTrue(sessionManager.IsSessionDirty);
            var sessionAdjustments = sessionManager.GetImageAdjustments();
            Assert.AreEqual(0.5f, sessionAdjustments.contrast);
            Assert.AreEqual(0.3f, sessionAdjustments.exposure);
            Assert.AreEqual(0.1f, sessionAdjustments.hue);
            Assert.AreEqual(0.2f, sessionAdjustments.saturation);
            Assert.AreEqual(0.8f, sessionAdjustments.opacity);
            Assert.IsTrue(sessionAdjustments.isModified);
        }
        
        [Test]
        public void UpdateFilterSettings_UpdatesSessionCorrectly()
        {
            // Arrange
            sessionManager.CreateNewSession();
            var filterSettings = new FilterSettings
            {
                filtersEnabled = true,
                globalFilterIntensity = 0.7f
            };
            filterSettings.grayscaleFilter.enabled = true;
            filterSettings.grayscaleFilter.intensity = 0.8f;
            
            // Act
            sessionManager.UpdateFilterSettings(filterSettings);
            
            // Assert
            Assert.IsTrue(sessionManager.IsSessionDirty);
            var sessionFilters = sessionManager.GetFilterSettings();
            Assert.IsTrue(sessionFilters.filtersEnabled);
            Assert.AreEqual(0.7f, sessionFilters.globalFilterIntensity);
            Assert.IsTrue(sessionFilters.grayscaleFilter.enabled);
            Assert.AreEqual(0.8f, sessionFilters.grayscaleFilter.intensity);
        }
        
        [Test]
        public void UpdateCanvasData_UpdatesSessionCorrectly()
        {
            // Arrange
            sessionManager.CreateNewSession();
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
                anchorId = "test-anchor-123"
            };
            
            // Act
            sessionManager.UpdateCanvasData(canvasData);
            
            // Assert
            Assert.IsTrue(sessionManager.IsSessionDirty);
            var sessionCanvas = sessionManager.GetCanvasData();
            Assert.IsNotNull(sessionCanvas);
            Assert.AreEqual(4, sessionCanvas.corners.Length);
            Assert.AreEqual(new Vector3(0.5f, 0.5f, 0), sessionCanvas.center);
            Assert.AreEqual(new Vector2(1, 1), sessionCanvas.dimensions);
            Assert.AreEqual("test-anchor-123", sessionCanvas.anchorId);
        }
        
        [Test]
        public void UpdateCurrentImagePath_UpdatesSessionCorrectly()
        {
            // Arrange
            sessionManager.CreateNewSession();
            string testImagePath = "/test/path/image.jpg";
            
            // Act
            sessionManager.UpdateCurrentImagePath(testImagePath);
            
            // Assert
            Assert.IsTrue(sessionManager.IsSessionDirty);
            Assert.AreEqual(testImagePath, sessionManager.GetCurrentImagePath());
        }
        
        [Test]
        public void UpdateAppState_UpdatesSessionCorrectly()
        {
            // Arrange
            sessionManager.CreateNewSession();
            var appState = new AppState
            {
                currentMode = AppMode.ImageAdjustment,
                mainMenuVisible = false,
                adjustmentPanelVisible = true,
                handGesturesEnabled = false,
                qualityLevel = QualityLevel.Medium
            };
            
            // Act
            sessionManager.UpdateAppState(appState);
            
            // Assert
            Assert.IsTrue(sessionManager.IsSessionDirty);
            var sessionAppState = sessionManager.GetAppState();
            Assert.AreEqual(AppMode.ImageAdjustment, sessionAppState.currentMode);
            Assert.IsFalse(sessionAppState.mainMenuVisible);
            Assert.IsTrue(sessionAppState.adjustmentPanelVisible);
            Assert.IsFalse(sessionAppState.handGesturesEnabled);
            Assert.AreEqual(QualityLevel.Medium, sessionAppState.qualityLevel);
        }
        
        [UnityTest]
        public IEnumerator SaveAndLoadSession_PreservesAllData()
        {
            // Arrange - Create session with comprehensive data
            sessionManager.CreateNewSession();
            
            // Set up image adjustments
            var adjustments = new ImageAdjustments
            {
                contrast = 0.4f,
                exposure = 0.2f,
                hue = 0.1f,
                saturation = 0.3f,
                opacity = 0.9f,
                cropArea = new Rect(0.1f, 0.1f, 0.8f, 0.8f),
                isModified = true
            };
            sessionManager.UpdateImageAdjustments(adjustments);
            
            // Set up filter settings
            var filterSettings = new FilterSettings
            {
                filtersEnabled = true,
                globalFilterIntensity = 0.8f
            };
            filterSettings.grayscaleFilter.enabled = true;
            filterSettings.edgeDetectionFilter.enabled = true;
            filterSettings.edgeDetectionFilter.intensity = 0.6f;
            sessionManager.UpdateFilterSettings(filterSettings);
            
            // Set up canvas data
            var canvasData = new CanvasData
            {
                corners = new Vector3[] { Vector3.zero, Vector3.right, Vector3.up, Vector3.one },
                center = Vector3.one * 0.5f,
                dimensions = Vector2.one,
                anchorId = "test-anchor"
            };
            sessionManager.UpdateCanvasData(canvasData);
            
            // Set up app state
            var appState = new AppState
            {
                currentMode = AppMode.ColorAnalysis,
                filterPanelVisible = true,
                qualityLevel = QualityLevel.High
            };
            sessionManager.UpdateAppState(appState);
            
            sessionManager.UpdateCurrentImagePath("/test/image.png");
            
            string originalSessionId = sessionManager.CurrentSession.sessionId;
            
            // Act - Save session
            bool saveSuccess = sessionManager.SaveCurrentSession();
            Assert.IsTrue(saveSuccess);
            Assert.IsFalse(sessionManager.IsSessionDirty);
            
            yield return null; // Wait a frame for file operations
            
            // Clear current session and load from disk
            sessionManager.ClearCurrentSession();
            Assert.IsFalse(sessionManager.HasActiveSession);
            
            bool loadSuccess = sessionManager.LoadLastSession();
            Assert.IsTrue(loadSuccess);
            
            yield return null; // Wait a frame for file operations
            
            // Assert - Verify all data was preserved
            Assert.IsTrue(sessionManager.HasActiveSession);
            Assert.AreEqual(originalSessionId, sessionManager.CurrentSession.sessionId);
            
            // Verify image adjustments
            var loadedAdjustments = sessionManager.GetImageAdjustments();
            Assert.AreEqual(0.4f, loadedAdjustments.contrast);
            Assert.AreEqual(0.2f, loadedAdjustments.exposure);
            Assert.AreEqual(0.1f, loadedAdjustments.hue);
            Assert.AreEqual(0.3f, loadedAdjustments.saturation);
            Assert.AreEqual(0.9f, loadedAdjustments.opacity);
            Assert.AreEqual(new Rect(0.1f, 0.1f, 0.8f, 0.8f), loadedAdjustments.cropArea);
            Assert.IsTrue(loadedAdjustments.isModified);
            
            // Verify filter settings
            var loadedFilters = sessionManager.GetFilterSettings();
            Assert.IsTrue(loadedFilters.filtersEnabled);
            Assert.AreEqual(0.8f, loadedFilters.globalFilterIntensity);
            Assert.IsTrue(loadedFilters.grayscaleFilter.enabled);
            Assert.IsTrue(loadedFilters.edgeDetectionFilter.enabled);
            Assert.AreEqual(0.6f, loadedFilters.edgeDetectionFilter.intensity);
            
            // Verify canvas data
            var loadedCanvas = sessionManager.GetCanvasData();
            Assert.IsNotNull(loadedCanvas);
            Assert.AreEqual(Vector3.one * 0.5f, loadedCanvas.center);
            Assert.AreEqual(Vector2.one, loadedCanvas.dimensions);
            Assert.AreEqual("test-anchor", loadedCanvas.anchorId);
            
            // Verify app state
            var loadedAppState = sessionManager.GetAppState();
            Assert.AreEqual(AppMode.ColorAnalysis, loadedAppState.currentMode);
            Assert.IsTrue(loadedAppState.filterPanelVisible);
            Assert.AreEqual(QualityLevel.High, loadedAppState.qualityLevel);
            
            // Verify image path
            Assert.AreEqual("/test/image.png", sessionManager.GetCurrentImagePath());
        }
        
        [Test]
        public void LoadLastSession_WithNoFile_CreatesNewSession()
        {
            // Ensure no session file exists
            Assert.IsFalse(File.Exists(testSessionPath));
            
            // Act
            bool loadSuccess = sessionManager.LoadLastSession();
            
            // Assert
            Assert.IsFalse(loadSuccess); // Should return false when creating new session
            Assert.IsTrue(sessionManager.HasActiveSession); // But should have created new session
            Assert.IsNotNull(sessionManager.CurrentSession);
        }
        
        [Test]
        public void ResetSessionToDefaults_ResetsAllSettings()
        {
            // Arrange
            sessionManager.CreateNewSession();
            
            // Modify session data
            sessionManager.UpdateImageAdjustments(new ImageAdjustments { contrast = 0.5f, isModified = true });
            var filterSettings = new FilterSettings();
            filterSettings.grayscaleFilter.enabled = true;
            sessionManager.UpdateFilterSettings(filterSettings);
            sessionManager.UpdateCurrentImagePath("/test/image.jpg");
            
            // Act
            sessionManager.ResetSessionToDefaults();
            
            // Assert
            Assert.IsTrue(sessionManager.IsSessionDirty);
            
            var adjustments = sessionManager.GetImageAdjustments();
            Assert.AreEqual(0f, adjustments.contrast);
            Assert.IsFalse(adjustments.isModified);
            
            var filters = sessionManager.GetFilterSettings();
            Assert.IsFalse(filters.grayscaleFilter.enabled);
            
            Assert.AreEqual("", sessionManager.GetCurrentImagePath());
        }
        
        [Test]
        public void GetSessionSummary_ReturnsCorrectInformation()
        {
            // Arrange
            sessionManager.CreateNewSession();
            
            // Set up some data
            sessionManager.UpdateCanvasData(new CanvasData { anchorId = "test" });
            sessionManager.UpdateCurrentImagePath("/test/image.jpg");
            sessionManager.UpdateImageAdjustments(new ImageAdjustments { isModified = true });
            
            var filterSettings = new FilterSettings();
            filterSettings.grayscaleFilter.enabled = true;
            filterSettings.edgeDetectionFilter.enabled = true;
            sessionManager.UpdateFilterSettings(filterSettings);
            
            sessionManager.UpdateAppState(new AppState { currentMode = AppMode.FilterApplication });
            
            // Act
            var summary = sessionManager.GetSessionSummary();
            
            // Assert
            Assert.IsNotEmpty(summary.sessionId);
            Assert.IsTrue(summary.hasCanvasData);
            Assert.IsTrue(summary.hasImageLoaded);
            Assert.IsTrue(summary.hasImageAdjustments);
            Assert.AreEqual(2, summary.activeFilterCount);
            Assert.AreEqual(AppMode.FilterApplication, summary.currentMode);
            
            string summaryText = summary.GetSummaryText();
            Assert.IsTrue(summaryText.Contains("Canvas Defined"));
            Assert.IsTrue(summaryText.Contains("Image Loaded"));
            Assert.IsTrue(summaryText.Contains("Adjustments Applied"));
            Assert.IsTrue(summaryText.Contains("2 Filters"));
        }
        
        [Test]
        public void ExportAndImportSession_PreservesData()
        {
            // Arrange
            sessionManager.CreateNewSession();
            sessionManager.UpdateImageAdjustments(new ImageAdjustments { contrast = 0.3f, isModified = true });
            sessionManager.UpdateCurrentImagePath("/test/export.jpg");
            
            string originalSessionId = sessionManager.CurrentSession.sessionId;
            
            // Act - Export
            string exportedJson = sessionManager.ExportSessionToJson();
            Assert.IsNotNull(exportedJson);
            Assert.IsNotEmpty(exportedJson);
            
            // Clear and import
            sessionManager.ClearCurrentSession();
            Assert.IsFalse(sessionManager.HasActiveSession);
            
            bool importSuccess = sessionManager.ImportSessionFromJson(exportedJson);
            
            // Assert
            Assert.IsTrue(importSuccess);
            Assert.IsTrue(sessionManager.HasActiveSession);
            Assert.AreEqual(originalSessionId, sessionManager.CurrentSession.sessionId);
            Assert.AreEqual(0.3f, sessionManager.GetImageAdjustments().contrast);
            Assert.AreEqual("/test/export.jpg", sessionManager.GetCurrentImagePath());
        }
        
        [Test]
        public void ImportSession_WithInvalidJson_ReturnsFalse()
        {
            // Arrange
            string invalidJson = "{ invalid json structure }";
            
            // Act
            bool importSuccess = sessionManager.ImportSessionFromJson(invalidJson);
            
            // Assert
            Assert.IsFalse(importSuccess);
        }
        
        [Test]
        public void ClearCurrentSession_RemovesSessionAndFile()
        {
            // Arrange
            sessionManager.CreateNewSession();
            sessionManager.SaveCurrentSession();
            Assert.IsTrue(sessionManager.HasActiveSession);
            
            // Act
            sessionManager.ClearCurrentSession();
            
            // Assert
            Assert.IsFalse(sessionManager.HasActiveSession);
            Assert.IsNull(sessionManager.CurrentSession);
            Assert.IsFalse(sessionManager.IsSessionDirty);
        }
        
        [Test]
        public void UpdateMethods_WithNoActiveSession_LogWarnings()
        {
            // Arrange - No session created
            Assert.IsFalse(sessionManager.HasActiveSession);
            
            // Act & Assert - These should not throw exceptions
            sessionManager.UpdateImageAdjustments(new ImageAdjustments());
            sessionManager.UpdateFilterSettings(new FilterSettings());
            sessionManager.UpdateCanvasData(new CanvasData());
            sessionManager.UpdateCurrentImagePath("/test/path");
            sessionManager.UpdateAppState(new AppState());
            
            // Should still have no active session
            Assert.IsFalse(sessionManager.HasActiveSession);
        }
        
        [Test]
        public void ImageAdjustments_HasAdjustments_DetectsChangesCorrectly()
        {
            // Arrange
            var defaultAdjustments = new ImageAdjustments();
            var modifiedAdjustments = new ImageAdjustments { contrast = 0.5f };
            var croppedAdjustments = new ImageAdjustments { cropArea = new Rect(0.1f, 0.1f, 0.8f, 0.8f) };
            var flaggedAdjustments = new ImageAdjustments { isModified = true };
            
            // Act & Assert
            Assert.IsFalse(defaultAdjustments.HasAdjustments());
            Assert.IsTrue(modifiedAdjustments.HasAdjustments());
            Assert.IsTrue(croppedAdjustments.HasAdjustments());
            Assert.IsTrue(flaggedAdjustments.HasAdjustments());
        }
        
        [Test]
        public void FilterSettings_HasActiveFilters_DetectsFiltersCorrectly()
        {
            // Arrange
            var noFilters = new FilterSettings();
            var disabledFilters = new FilterSettings { filtersEnabled = false };
            disabledFilters.grayscaleFilter.enabled = true; // Enabled but filters globally disabled
            
            var activeFilters = new FilterSettings();
            activeFilters.grayscaleFilter.enabled = true;
            
            // Act & Assert
            Assert.IsFalse(noFilters.HasActiveFilters());
            Assert.IsFalse(disabledFilters.HasActiveFilters());
            Assert.IsTrue(activeFilters.HasActiveFilters());
            
            Assert.AreEqual(0, noFilters.GetActiveFilterCount());
            Assert.AreEqual(0, disabledFilters.GetActiveFilterCount());
            Assert.AreEqual(1, activeFilters.GetActiveFilterCount());
        }
        
        [Test]
        public void SessionData_HasModifications_DetectsChangesCorrectly()
        {
            // Arrange
            var emptySession = new SessionData();
            var modifiedSession = new SessionData();
            modifiedSession.imageAdjustments.isModified = true;
            
            var sessionWithCanvas = new SessionData();
            sessionWithCanvas.canvasData = new CanvasData();
            
            var sessionWithImage = new SessionData();
            sessionWithImage.currentImagePath = "/test/image.jpg";
            
            // Act & Assert
            Assert.IsFalse(emptySession.HasModifications());
            Assert.IsTrue(modifiedSession.HasModifications());
            Assert.IsTrue(sessionWithCanvas.HasModifications());
            Assert.IsTrue(sessionWithImage.HasModifications());
        }
    }
}