using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using MixedReality.Toolkit.Input;
using MixedReality.Toolkit.UX;

namespace DaVinciEye.SpatialTracking
{
    /// <summary>
    /// Monitors HoloLens tracking quality and provides visual feedback for tracking issues
    /// Uses MRTK tracking subsystems and XR display helpers for comprehensive tracking monitoring
    /// </summary>
    public class TrackingQualityMonitor : MonoBehaviour
    {
        [Header("Tracking Quality Settings")]
        [SerializeField] private float trackingCheckInterval = 0.5f;
        [SerializeField] private float poorTrackingThreshold = 0.3f;
        [SerializeField] private float warningDisplayDuration = 3f;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject trackingWarningDialog;
        [SerializeField] private Color goodTrackingColor = Color.green;
        [SerializeField] private Color poorTrackingColor = Color.red;
        [SerializeField] private Color warningTrackingColor = Color.yellow;
        
        // Tracking state properties
        public TrackingQuality CurrentTrackingQuality { get; private set; } = TrackingQuality.Good;
        public bool IsTrackingStable { get; private set; } = true;
        public float TrackingConfidence { get; private set; } = 1.0f;
        
        // Events
        public event Action<TrackingQuality> OnTrackingQualityChanged;
        public event Action<bool> OnTrackingStabilityChanged;
        public event Action<string> OnTrackingWarning;
        
        // Private fields
        private XRDisplaySubsystem displaySubsystem;
        private Coroutine trackingMonitorCoroutine;
        private Dialog currentWarningDialog;
        private TrackingQuality previousTrackingQuality;
        private bool previousTrackingStability;
        private float lastWarningTime;
        
        private void Start()
        {
            InitializeTrackingMonitor();
            StartTrackingMonitoring();
        }
        
        private void OnDestroy()
        {
            StopTrackingMonitoring();
        }
        
        /// <summary>
        /// Initialize the tracking monitoring system
        /// </summary>
        private void InitializeTrackingMonitor()
        {
            // Get XR display subsystem for tracking state information
            displaySubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRDisplaySubsystem>();
            
            if (displaySubsystem == null)
            {
                Debug.LogWarning("TrackingQualityMonitor: XR Display Subsystem not found. Tracking monitoring may be limited.");
            }
            
            // Initialize tracking state
            previousTrackingQuality = CurrentTrackingQuality;
            previousTrackingStability = IsTrackingStable;
            
            Debug.Log("TrackingQualityMonitor: Initialized successfully");
        }
        
        /// <summary>
        /// Start continuous tracking quality monitoring
        /// </summary>
        public void StartTrackingMonitoring()
        {
            if (trackingMonitorCoroutine == null)
            {
                trackingMonitorCoroutine = StartCoroutine(MonitorTrackingQuality());
                Debug.Log("TrackingQualityMonitor: Started tracking monitoring");
            }
        }
        
        /// <summary>
        /// Stop tracking quality monitoring
        /// </summary>
        public void StopTrackingMonitoring()
        {
            if (trackingMonitorCoroutine != null)
            {
                StopCoroutine(trackingMonitorCoroutine);
                trackingMonitorCoroutine = null;
                Debug.Log("TrackingQualityMonitor: Stopped tracking monitoring");
            }
        }
        
        /// <summary>
        /// Coroutine that continuously monitors tracking quality
        /// </summary>
        private IEnumerator MonitorTrackingQuality()
        {
            while (true)
            {
                UpdateTrackingQuality();
                yield return new WaitForSeconds(trackingCheckInterval);
            }
        }
        
        /// <summary>
        /// Update current tracking quality based on XR subsystem data
        /// </summary>
        private void UpdateTrackingQuality()
        {
            // Get tracking state from XR subsystem
            var trackingState = GetCurrentTrackingState();
            var headPose = GetHeadPose();
            
            // Calculate tracking confidence based on multiple factors
            float confidence = CalculateTrackingConfidence(trackingState, headPose);
            TrackingConfidence = confidence;
            
            // Determine tracking quality
            TrackingQuality newQuality = DetermineTrackingQuality(confidence, trackingState);
            bool newStability = confidence > poorTrackingThreshold;
            
            // Check for changes and fire events
            if (newQuality != previousTrackingQuality)
            {
                CurrentTrackingQuality = newQuality;
                OnTrackingQualityChanged?.Invoke(newQuality);
                previousTrackingQuality = newQuality;
                
                // Show warning if quality degraded
                if (newQuality == TrackingQuality.Poor)
                {
                    ShowTrackingWarning("Tracking quality is poor. Please ensure good lighting and visible surfaces.");
                }
            }
            
            if (newStability != previousTrackingStability)
            {
                IsTrackingStable = newStability;
                OnTrackingStabilityChanged?.Invoke(newStability);
                previousTrackingStability = newStability;
            }
        }
        
        /// <summary>
        /// Get current XR tracking state
        /// </summary>
        private InputTrackingState GetCurrentTrackingState()
        {
            if (displaySubsystem != null && displaySubsystem.running)
            {
                // Try to get head tracking state
                var inputDevices = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, inputDevices);
                
                if (inputDevices.Count > 0)
                {
                    var headDevice = inputDevices[0];
                    if (headDevice.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState trackingState))
                    {
                        return trackingState;
                    }
                }
            }
            
            return InputTrackingState.None;
        }
        
        /// <summary>
        /// Get current head pose information
        /// </summary>
        private Pose GetHeadPose()
        {
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, inputDevices);
            
            if (inputDevices.Count > 0)
            {
                var headDevice = inputDevices[0];
                if (headDevice.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 position) &&
                    headDevice.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion rotation))
                {
                    return new Pose(position, rotation);
                }
            }
            
            return Pose.identity;
        }
        
        /// <summary>
        /// Calculate tracking confidence based on multiple factors
        /// </summary>
        private float CalculateTrackingConfidence(InputTrackingState trackingState, Pose headPose)
        {
            float confidence = 0f;
            
            // Base confidence on tracking state flags
            if ((trackingState & InputTrackingState.Position) != 0)
                confidence += 0.4f;
            if ((trackingState & InputTrackingState.Rotation) != 0)
                confidence += 0.4f;
            if ((trackingState & InputTrackingState.Velocity) != 0)
                confidence += 0.1f;
            if ((trackingState & InputTrackingState.AngularVelocity) != 0)
                confidence += 0.1f;
            
            // Additional factors could include:
            // - Spatial mesh quality
            // - Hand tracking availability
            // - Environmental lighting
            
            return Mathf.Clamp01(confidence);
        }
        
        /// <summary>
        /// Determine tracking quality based on confidence and state
        /// </summary>
        private TrackingQuality DetermineTrackingQuality(float confidence, InputTrackingState trackingState)
        {
            if (confidence >= 0.8f && (trackingState & InputTrackingState.Position) != 0 && (trackingState & InputTrackingState.Rotation) != 0)
                return TrackingQuality.Good;
            else if (confidence >= 0.5f && (trackingState & InputTrackingState.Position) != 0)
                return TrackingQuality.Fair;
            else
                return TrackingQuality.Poor;
        }
        
        /// <summary>
        /// Show tracking warning dialog to user
        /// </summary>
        private void ShowTrackingWarning(string message)
        {
            // Prevent spam warnings
            if (Time.time - lastWarningTime < warningDisplayDuration)
                return;
                
            lastWarningTime = Time.time;
            
            // Fire warning event
            OnTrackingWarning?.Invoke(message);
            
            // Show MRTK dialog if available
            if (trackingWarningDialog != null)
            {
                ShowMRTKDialog(message);
            }
            else
            {
                Debug.LogWarning($"TrackingQualityMonitor: {message}");
            }
        }
        
        /// <summary>
        /// Show MRTK dialog for tracking warnings
        /// </summary>
        private void ShowMRTKDialog(string message)
        {
            // Close existing dialog if open
            if (currentWarningDialog != null)
            {
                currentWarningDialog.Dismiss();
            }
            
            // Create new dialog
            var dialogPrefab = trackingWarningDialog.GetComponent<Dialog>();
            if (dialogPrefab != null)
            {
                currentWarningDialog = Dialog.InstantiateFromPrefab(dialogPrefab, new DialogProperty
                {
                    Title = "Tracking Warning",
                    Message = message,
                    Type = DialogButtonType.OK
                });
                
                if (currentWarningDialog != null)
                {
                    currentWarningDialog.OnClosed.AddListener(() => currentWarningDialog = null);
                    currentWarningDialog.Show();
                }
            }
        }
        
        /// <summary>
        /// Get current tracking quality as color for visual feedback
        /// </summary>
        public Color GetTrackingQualityColor()
        {
            return CurrentTrackingQuality switch
            {
                TrackingQuality.Good => goodTrackingColor,
                TrackingQuality.Fair => warningTrackingColor,
                TrackingQuality.Poor => poorTrackingColor,
                _ => Color.white
            };
        }
        
        /// <summary>
        /// Force a tracking quality check (useful for testing)
        /// </summary>
        public void ForceTrackingCheck()
        {
            UpdateTrackingQuality();
        }
    }
    
    /// <summary>
    /// Enumeration for tracking quality levels
    /// </summary>
    public enum TrackingQuality
    {
        Poor,
        Fair,
        Good
    }
}