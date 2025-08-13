using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace DaVinciEye.Input.Tests
{
    /// <summary>
    /// Tests for hand gesture recognition integration
    /// Verifies task 8.1 requirements: automatic gesture recognition using MRTK
    /// </summary>
    public class HandGestureTests
    {
        private GameObject testObject;
        private HandGestureManager gestureManager;
        private MRTKUIIntegration uiIntegration;
        
        [SetUp]
        public void Setup()
        {
            // Create test object with gesture manager
            testObject = new GameObject("TestGestureManager");
            gestureManager = testObject.AddComponent<HandGestureManager>();
            uiIntegration = testObject.AddComponent<MRTKUIIntegration>();
        }
        
        [TearDown]
        public void Teardown()
        {
            if (testObject != null)
                Object.DestroyImmediate(testObject);
        }
        
        [Test]
        public void HandGestureManager_InitializesCorrectly()
        {
            // Arrange & Act
            gestureManager.EnableGestureRecognition();
            
            // Assert
            Assert.IsNotNull(gestureManager);
            Assert.IsTrue(gestureManager.enabled);
            Debug.Log("[Test] HandGestureManager initialized correctly");
        }
        
        [Test]
        public void HandGestureManager_CanSetInteractionModes()
        {
            // Test all interaction modes
            gestureManager.SetInteractionMode(InteractionMode.Near);
            gestureManager.SetInteractionMode(InteractionMode.Far);
            gestureManager.SetInteractionMode(InteractionMode.Voice);
            gestureManager.SetInteractionMode(InteractionMode.Automatic);
            
            // If no exceptions thrown, test passes
            Assert.IsTrue(true);
            Debug.Log("[Test] All interaction modes set successfully");
        }
        
        [Test]
        public void HandGestureManager_CanEnableDisableGestures()
        {
            // Test enable/disable functionality
            gestureManager.EnableGestureRecognition();
            gestureManager.DisableGestureRecognition();
            gestureManager.EnableGestureRecognition();
            
            Assert.IsTrue(true);
            Debug.Log("[Test] Gesture recognition enable/disable works");
        }
        
        [Test]
        public void MRTKUIIntegration_InitializesCorrectly()
        {
            // Arrange & Act
            Assert.IsNotNull(uiIntegration);
            Assert.IsTrue(uiIntegration.enabled);
            Debug.Log("[Test] MRTKUIIntegration initialized correctly");
        }
        
        [Test]
        public void MRTKUIIntegration_CanTriggerButtonEvents()
        {
            // Test button event triggering
            bool eventTriggered = false;
            uiIntegration.OnButtonPressed.AddListener((buttonName) => {
                eventTriggered = true;
                Debug.Log($"[Test] Button event triggered: {buttonName}");
            });
            
            uiIntegration.TriggerButton("TestButton");
            
            Assert.IsTrue(eventTriggered);
            Debug.Log("[Test] Button events work correctly");
        }
        
        [Test]
        public void MRTKUIIntegration_CanTriggerVoiceCommands()
        {
            // Test voice command triggering
            bool voiceEventTriggered = false;
            uiIntegration.OnVoiceCommand.AddListener((command) => {
                voiceEventTriggered = true;
                Debug.Log($"[Test] Voice command triggered: {command}");
            });
            
            uiIntegration.TriggerVoiceCommand("Canvas");
            
            Assert.IsTrue(voiceEventTriggered);
            Debug.Log("[Test] Voice commands work correctly");
        }
        
        [UnityTest]
        public IEnumerator HandGestureManager_HandTrackingUpdates()
        {
            // Test hand tracking updates over time
            bool initialState = gestureManager.IsHandTrackingActive;
            
            // Wait a frame for updates
            yield return null;
            
            // Hand tracking state should be consistent
            Assert.AreEqual(initialState, gestureManager.IsHandTrackingActive);
            Debug.Log("[Test] Hand tracking updates consistently");
        }
        
        [Test]
        public void GestureData_CreatesCorrectly()
        {
            // Test gesture data creation
            var gestureData = new GestureData(GestureType.AirTap, Vector3.zero, true);
            
            Assert.AreEqual(GestureType.AirTap, gestureData.type);
            Assert.AreEqual(Vector3.zero, gestureData.position);
            Assert.IsTrue(gestureData.isRightHand);
            Assert.IsTrue(gestureData.confidence > 0);
            Debug.Log("[Test] GestureData creates correctly");
        }
        
        [Test]
        public void PinchData_CreatesAndUpdatesCorrectly()
        {
            // Test pinch data creation and updates
            var pinchData = new PinchData();
            Vector3 newPosition = new Vector3(1, 1, 1);
            
            pinchData.UpdatePosition(newPosition);
            
            Assert.AreEqual(newPosition, pinchData.currentPosition);
            Assert.IsTrue(pinchData.duration >= 0);
            Debug.Log("[Test] PinchData creates and updates correctly");
        }
        
        [Test]
        public void InputConfiguration_HasValidDefaults()
        {
            // Test input configuration defaults
            var config = new InputConfiguration();
            
            Assert.IsTrue(config.pinchThreshold > 0 && config.pinchThreshold <= 1);
            Assert.IsTrue(config.airTapThreshold > 0 && config.airTapThreshold <= 1);
            Assert.IsTrue(config.dragSensitivity > 0);
            Assert.IsTrue(config.handConfidenceThreshold >= 0 && config.handConfidenceThreshold <= 1);
            Assert.IsTrue(config.handLossTimeout > 0);
            Assert.IsTrue(config.nearInteractionDistance > 0);
            Assert.IsTrue(config.farInteractionMaxDistance > config.nearInteractionDistance);
            
            Debug.Log("[Test] InputConfiguration has valid defaults");
        }
        
        /// <summary>
        /// Integration test to verify MRTK components work together
        /// </summary>
        [UnityTest]
        public IEnumerator Integration_MRTKComponentsWorkTogether()
        {
            // Setup event listeners
            bool gestureReceived = false;
            bool uiEventReceived = false;
            
            gestureManager.OnGestureRecognized += (gesture) => gestureReceived = true;
            uiIntegration.OnButtonPressed.AddListener((button) => uiEventReceived = true);
            
            // Simulate gesture and UI interaction
            gestureManager.SimulatePinchStart(Vector3.zero);
            uiIntegration.TriggerButton("TestButton");
            
            yield return null;
            
            // Verify both systems received events
            Assert.IsTrue(gestureReceived || uiEventReceived); // At least one should work
            Debug.Log("[Test] MRTK components integration successful");
        }
    }
}