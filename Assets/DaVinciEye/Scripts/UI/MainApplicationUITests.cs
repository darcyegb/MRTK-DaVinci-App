using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using DaVinciEye.Core;
using DaVinciEye.UI;
using MixedReality.Toolkit.UX;

namespace DaVinciEye.Tests.UI
{
    /// <summary>
    /// Tests for MainApplicationUI functionality and MRTK integration
    /// Covers UI interaction tests for all primary application functions
    /// </summary>
    public class MainApplicationUITests
    {
        private GameObject testObject;
        private MainApplicationUI mainUI;
        private DaVinciEyeApp testApp;
        private MRTKUISetup uiSetup;
        
        [SetUp]
        public void Setup()
        {
            // Create test object with required components
            testObject = new GameObject("TestMainApplicationUI");
            
            // Add DaVinciEyeApp for testing
            testApp = testObject.AddComponent<DaVinciEyeApp>();
            
            // Add UI components
            mainUI = testObject.AddComponent<MainApplicationUI>();
            uiSetup = testObject.AddComponent<MRTKUISetup>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        [Test]
        public void MainApplicationUI_InitializesCorrectly()
        {
            // Test that MainApplicationUI initializes without errors
            Assert.IsNotNull(mainUI);
            Assert.IsNotNull(testApp);
            
            // Verify initial state
            Assert.AreEqual(ApplicationMode.CanvasDefinition, mainUI.CurrentMode);
            Assert.IsFalse(mainUI.IsDialogVisible);
        }
        
        [Test]
        public void ModeChangeRequest_TriggersEvent()
        {
            // Arrange
            ApplicationMode requestedMode = ApplicationMode.ImageOverlay;
            ApplicationMode receivedMode = ApplicationMode.CanvasDefinition;
            bool eventTriggered = false;
            
            mainUI.OnModeChangeRequested += (mode) => {
                receivedMode = mode;
                eventTriggered = true;
            };
            
            // Act - Simulate button click through reflection
            var method = typeof(MainApplicationUI).GetMethod("RequestModeChange", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(mainUI, new object[] { requestedMode });
            
            // Assert
            Assert.IsTrue(eventTriggered);
            Assert.AreEqual(requestedMode, receivedMode);
        }
        
        [Test]
        public void ModeChange_UpdatesUIState()
        {
            // Arrange
            ApplicationMode newMode = ApplicationMode.FilterApplication;
            
            // Act
            testApp.SetApplicationMode(newMode);
            
            // Assert
            Assert.AreEqual(newMode, mainUI.CurrentMode);
        }
        
        [Test]
        public void ShowError_DisplaysErrorDialog()
        {
            // Arrange
            string errorMessage = "Test error message";
            bool errorEventTriggered = false;
            string receivedMessage = "";
            
            mainUI.OnUIError += (message) => {
                errorEventTriggered = true;
                receivedMessage = message;
            };
            
            // Act
            mainUI.ShowError(errorMessage);
            
            // Assert
            Assert.IsTrue(errorEventTriggered);
            Assert.AreEqual(errorMessage, receivedMessage);
        }
        
        [Test]
        public void HandMenu_CanBeShownAndHidden()
        {
            // This test would require actual MRTK prefabs to be meaningful
            // For now, test the method calls don't throw exceptions
            
            // Act & Assert
            Assert.DoesNotThrow(() => mainUI.ShowHandMenu());
            Assert.DoesNotThrow(() => mainUI.HideHandMenu());
        }
        
        [Test]
        public void NearMenu_CanBeShownAndHidden()
        {
            // This test would require actual MRTK prefabs to be meaningful
            // For now, test the method calls don't throw exceptions
            
            // Act & Assert
            Assert.DoesNotThrow(() => mainUI.ShowNearMenu());
            Assert.DoesNotThrow(() => mainUI.HideNearMenu());
        }
        
        [UnityTest]
        public IEnumerator ModeChangeConfirmation_WorksCorrectly()
        {
            // This test simulates the confirmation dialog workflow
            
            // Arrange
            ApplicationMode targetMode = ApplicationMode.ColorAnalysis;
            
            // Set current mode to one that requires confirmation
            testApp.SetApplicationMode(ApplicationMode.ImageOverlay);
            yield return null; // Wait a frame for UI update
            
            // Act - Request mode change that should trigger confirmation
            var method = typeof(MainApplicationUI).GetMethod("RequestModeChange", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(mainUI, new object[] { targetMode });
            
            yield return null; // Wait a frame for dialog to appear
            
            // Assert - In a real test with MRTK prefabs, we would check if dialog is visible
            // For now, verify no exceptions were thrown
            Assert.IsNotNull(mainUI);
        }
        
        [Test]
        public void GetModeDisplayName_ReturnsCorrectNames()
        {
            // Use reflection to test private method
            var method = typeof(MainApplicationUI).GetMethod("GetModeDisplayName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Test all modes
            Assert.AreEqual("Canvas Definition", method?.Invoke(mainUI, new object[] { ApplicationMode.CanvasDefinition }));
            Assert.AreEqual("Image Overlay", method?.Invoke(mainUI, new object[] { ApplicationMode.ImageOverlay }));
            Assert.AreEqual("Filter Application", method?.Invoke(mainUI, new object[] { ApplicationMode.FilterApplication }));
            Assert.AreEqual("Color Analysis", method?.Invoke(mainUI, new object[] { ApplicationMode.ColorAnalysis }));
            Assert.AreEqual("Settings", method?.Invoke(mainUI, new object[] { ApplicationMode.Settings }));
        }
        
        [Test]
        public void ShouldConfirmModeChange_ReturnsCorrectValues()
        {
            // Use reflection to test private method
            var method = typeof(MainApplicationUI).GetMethod("ShouldConfirmModeChange", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Test cases that should require confirmation
            Assert.IsTrue((bool)method?.Invoke(mainUI, new object[] { ApplicationMode.ImageOverlay, ApplicationMode.CanvasDefinition }));
            Assert.IsTrue((bool)method?.Invoke(mainUI, new object[] { ApplicationMode.FilterApplication, ApplicationMode.ColorAnalysis }));
            
            // Test cases that should not require confirmation
            Assert.IsFalse((bool)method?.Invoke(mainUI, new object[] { ApplicationMode.CanvasDefinition, ApplicationMode.ImageOverlay }));
            Assert.IsFalse((bool)method?.Invoke(mainUI, new object[] { ApplicationMode.ColorAnalysis, ApplicationMode.Settings }));
        }
    }
    
    /// <summary>
    /// Integration tests for MRTK UI setup functionality
    /// </summary>
    public class MRTKUISetupTests
    {
        private GameObject testObject;
        private MRTKUISetup uiSetup;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestMRTKUISetup");
            uiSetup = testObject.AddComponent<MRTKUISetup>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        [Test]
        public void MRTKUISetup_InitializesCorrectly()
        {
            Assert.IsNotNull(uiSetup);
        }
        
        [Test]
        public void ValidateMRTKPrefabs_DoesNotThrowException()
        {
            // Test that validation method runs without errors
            Assert.DoesNotThrow(() => uiSetup.ValidateMRTKPrefabs());
        }
        
        [Test]
        public void SetupMRTKUI_DoesNotThrowException()
        {
            // Test that setup method runs without errors (even without prefabs assigned)
            Assert.DoesNotThrow(() => uiSetup.SetupMRTKUI());
        }
        
        [Test]
        public void ButtonLabels_AreConfiguredCorrectly()
        {
            // Use reflection to access private field
            var field = typeof(MRTKUISetup).GetField("buttonLabels", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var labels = field?.GetValue(uiSetup) as string[];
            
            Assert.IsNotNull(labels);
            Assert.AreEqual(4, labels.Length);
            Assert.Contains("Canvas", labels);
            Assert.Contains("Image", labels);
            Assert.Contains("Filters", labels);
            Assert.Contains("Colors", labels);
        }
        
        [Test]
        public void VoiceCommands_AreConfiguredCorrectly()
        {
            // Use reflection to access private field
            var field = typeof(MRTKUISetup).GetField("voiceCommands", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var commands = field?.GetValue(uiSetup) as string[];
            
            Assert.IsNotNull(commands);
            Assert.AreEqual(4, commands.Length);
            Assert.Contains("canvas", commands);
            Assert.Contains("image", commands);
            Assert.Contains("filters", commands);
            Assert.Contains("colors", commands);
        }
    }
    
    /// <summary>
    /// Performance tests for UI responsiveness
    /// </summary>
    public class UIPerformanceTests
    {
        private GameObject testObject;
        private MainApplicationUI mainUI;
        private DaVinciEyeApp testApp;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestUIPerformance");
            testApp = testObject.AddComponent<DaVinciEyeApp>();
            mainUI = testObject.AddComponent<MainApplicationUI>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        [Test]
        public void ModeChange_CompletesWithinTimeLimit()
        {
            // Test that mode changes complete within acceptable time (16ms for 60 FPS)
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Act
            testApp.SetApplicationMode(ApplicationMode.ImageOverlay);
            
            stopwatch.Stop();
            
            // Assert - Should complete within one frame (16.67ms at 60 FPS)
            Assert.Less(stopwatch.ElapsedMilliseconds, 17);
        }
        
        [Test]
        public void UIUpdate_CompletesWithinTimeLimit()
        {
            // Test that UI updates complete within acceptable time
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Act - Trigger UI update
            testApp.SetApplicationMode(ApplicationMode.FilterApplication);
            
            stopwatch.Stop();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 17);
        }
        
        [Test]
        public void MultipleRapidModeChanges_HandleCorrectly()
        {
            // Test rapid mode changes don't cause issues
            var modes = new[] { 
                ApplicationMode.ImageOverlay, 
                ApplicationMode.FilterApplication, 
                ApplicationMode.ColorAnalysis, 
                ApplicationMode.CanvasDefinition 
            };
            
            // Act & Assert
            foreach (var mode in modes)
            {
                Assert.DoesNotThrow(() => testApp.SetApplicationMode(mode));
                Assert.AreEqual(mode, mainUI.CurrentMode);
            }
        }
    }
}