using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

namespace DaVinciEye.SpatialTracking
{
    /// <summary>
    /// Manages relocalization and recovery when tracking is lost
    /// Handles automatic anchor restoration and tracking recovery procedures
    /// </summary>
    public class RelocalizationManager : MonoBehaviour
    {
        [Header("Relocalization Settings")]
        [SerializeField] private float trackingLossThreshold = 2f;
        [SerializeField] private float relocalizationTimeout = 30f;
        [SerializeField] private float anchorRestoreDelay = 1f;
        [SerializeField] private int maxRelocalizationAttempts = 3;
        
        [Header("Recovery Behavior")]
        [SerializeField] private bool autoRelocalize = true;
        [SerializeField] private bool restoreAnchorsOnRecovery = true;
        [SerializeField] private bool showRecoveryUI = true;
        
        // State properties
        public bool IsTrackingLost { get; private set; }
        public bool IsRelocalizing { get; private set; }
        public float TrackingLostTime { get; private set; }
        public int RelocalizationAttempts { get; private set; }
        
        // Events
        public event Action OnTrackingLost;
        public event Action OnRelocalizationStarted;
        public event Action OnRelocalizationSucceeded;
        public event Action OnRelocalizationFailed;
        public event Action<List<ARAnchor>> OnAnchorsRestored;
        
        // Private fields
        private TrackingQualityMonitor trackingMonitor;
        private ARAnchorManager anchorManager;
        private ARSession arSession;
        private XRDisplaySubsystem displaySubsystem;
        
        private Coroutine relocalizationCoroutine;
        private List<StoredAnchorData> storedAnchors = new List<StoredAnchorData>();
        private Dictionary<string, ARAnchor> activeAnchors = new Dictionary<string, ARAnchor>();
        
        private bool wasTrackingStable = true;
        private float lastTrackingLossTime;
        
        private void Start()
        {
            InitializeRelocalizationManager();
            ConnectToTrackingMonitor();
            SetupXRComponents();
        }
        
        private void OnDestroy()
        {
            DisconnectFromTrackingMonitor();
            StopRelocalization();
        }
        
        /// <summary>
        /// Initialize the relocalization manager
        /// </summary>
        private void InitializeRelocalizationManager()
        {
            IsTrackingLost = false;
            IsRelocalizing = false;
            TrackingLostTime = 0f;
            RelocalizationAttempts = 0;
            
            Debug.Log("RelocalizationManager: Initialized successfully");
        }
        
        /// <summary>
        /// Connect to the tracking quality monitor
        /// </summary>
        private void ConnectToTrackingMonitor()
        {
            trackingMonitor = FindObjectOfType<TrackingQualityMonitor>();
            
            if (trackingMonitor != null)
            {
                trackingMonitor.OnTrackingStabilityChanged += OnTrackingStabilityChanged;
                trackingMonitor.OnTrackingQualityChanged += OnTrackingQualityChanged;
                
                Debug.Log("RelocalizationManager: Connected to TrackingQualityMonitor");
            }
            else
            {
                Debug.LogWarning("RelocalizationManager: TrackingQualityMonitor not found");
            }
        }
        
        /// <summary>
        /// Disconnect from tracking monitor
        /// </summary>
        private void DisconnectFromTrackingMonitor()
        {
            if (trackingMonitor != null)
            {
                trackingMonitor.OnTrackingStabilityChanged -= OnTrackingStabilityChanged;
                trackingMonitor.OnTrackingQualityChanged -= OnTrackingQualityChanged;
            }
        }
        
        /// <summary>
        /// Setup XR and AR Foundation components
        /// </summary>
        private void SetupXRComponents()
        {
            // Get AR Foundation components
            anchorManager = FindObjectOfType<ARAnchorManager>();
            arSession = FindObjectOfType<ARSession>();
            
            // Get XR display subsystem
            displaySubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRDisplaySubsystem>();
            
            if (anchorManager == null)
            {
                Debug.LogWarning("RelocalizationManager: ARAnchorManager not found. Anchor restoration will be limited.");
            }
            
            if (arSession == null)
            {
                Debug.LogWarning("RelocalizationManager: ARSession not found. Session management will be limited.");
            }
            
            if (displaySubsystem == null)
            {
                Debug.LogWarning("RelocalizationManager: XR Display Subsystem not found. Tracking detection may be limited.");
            }
        }
        
        /// <summary>
        /// Handle tracking stability changes
        /// </summary>
        private void OnTrackingStabilityChanged(bool isStable)
        {
            if (wasTrackingStable && !isStable)
            {
                // Tracking just became unstable
                OnTrackingBecameUnstable();
            }
            else if (!wasTrackingStable && isStable)
            {
                // Tracking recovered
                OnTrackingRecovered();
            }
            
            wasTrackingStable = isStable;
        }
        
        /// <summary>
        /// Handle tracking quality changes
        /// </summary>
        private void OnTrackingQualityChanged(TrackingQuality quality)
        {
            if (quality == TrackingQuality.Poor && !IsTrackingLost)
            {
                // Poor quality might indicate impending tracking loss
                StartTrackingLossTimer();
            }
            else if (quality == TrackingQuality.Good && IsTrackingLost)
            {
                // Good quality indicates tracking recovery
                OnTrackingRecovered();
            }
        }
        
        /// <summary>
        /// Start tracking loss timer
        /// </summary>
        private void StartTrackingLossTimer()
        {
            if (Time.time - lastTrackingLossTime > trackingLossThreshold)
            {
                lastTrackingLossTime = Time.time;
                StartCoroutine(CheckForTrackingLoss());
            }
        }
        
        /// <summary>
        /// Check if tracking is actually lost
        /// </summary>
        private IEnumerator CheckForTrackingLoss()
        {
            yield return new WaitForSeconds(trackingLossThreshold);
            
            // Check if tracking is still poor after threshold time
            if (trackingMonitor != null && 
                (trackingMonitor.CurrentTrackingQuality == TrackingQuality.Poor || 
                 !trackingMonitor.IsTrackingStable))
            {
                OnTrackingBecameUnstable();
            }
        }
        
        /// <summary>
        /// Handle when tracking becomes unstable
        /// </summary>
        private void OnTrackingBecameUnstable()
        {
            if (!IsTrackingLost)
            {
                IsTrackingLost = true;
                TrackingLostTime = Time.time;
                RelocalizationAttempts = 0;
                
                Debug.LogWarning("RelocalizationManager: Tracking lost detected");
                
                // Store current anchors before they're lost
                StoreCurrentAnchors();
                
                // Fire tracking lost event
                OnTrackingLost?.Invoke();
                
                // Start relocalization if enabled
                if (autoRelocalize)
                {
                    StartRelocalization();
                }
            }
        }
        
        /// <summary>
        /// Handle tracking recovery
        /// </summary>
        private void OnTrackingRecovered()
        {
            if (IsTrackingLost)
            {
                Debug.Log("RelocalizationManager: Tracking recovered");
                
                IsTrackingLost = false;
                IsRelocalizing = false;
                
                // Stop relocalization coroutine if running
                StopRelocalization();
                
                // Restore anchors if enabled
                if (restoreAnchorsOnRecovery)
                {
                    StartCoroutine(RestoreAnchorsAfterDelay());
                }
                
                // Fire success event
                OnRelocalizationSucceeded?.Invoke();
            }
        }
        
        /// <summary>
        /// Store current anchors before tracking loss
        /// </summary>
        private void StoreCurrentAnchors()
        {
            storedAnchors.Clear();
            
            if (anchorManager != null)
            {
                foreach (var anchor in anchorManager.trackables)
                {
                    var anchorData = new StoredAnchorData
                    {
                        anchorId = anchor.trackableId.ToString(),
                        pose = anchor.transform.GetPose(),
                        timestamp = Time.time
                    };
                    
                    storedAnchors.Add(anchorData);
                }
                
                Debug.Log($"RelocalizationManager: Stored {storedAnchors.Count} anchors");
            }
        }
        
        /// <summary>
        /// Start relocalization process
        /// </summary>
        public void StartRelocalization()
        {
            if (!IsRelocalizing)
            {
                IsRelocalizing = true;
                RelocalizationAttempts++;
                
                Debug.Log($"RelocalizationManager: Starting relocalization attempt {RelocalizationAttempts}");
                
                OnRelocalizationStarted?.Invoke();
                
                if (relocalizationCoroutine != null)
                {
                    StopCoroutine(relocalizationCoroutine);
                }
                
                relocalizationCoroutine = StartCoroutine(RelocalizationProcess());
            }
        }
        
        /// <summary>
        /// Stop relocalization process
        /// </summary>
        public void StopRelocalization()
        {
            if (relocalizationCoroutine != null)
            {
                StopCoroutine(relocalizationCoroutine);
                relocalizationCoroutine = null;
            }
            
            IsRelocalizing = false;
        }
        
        /// <summary>
        /// Main relocalization process coroutine
        /// </summary>
        private IEnumerator RelocalizationProcess()
        {
            float startTime = Time.time;
            
            while (IsRelocalizing && Time.time - startTime < relocalizationTimeout)
            {
                // Attempt to reset AR session
                if (arSession != null)
                {
                    Debug.Log("RelocalizationManager: Resetting AR session");
                    arSession.Reset();
                    yield return new WaitForSeconds(1f);
                }
                
                // Check if tracking has recovered
                if (trackingMonitor != null && 
                    trackingMonitor.CurrentTrackingQuality != TrackingQuality.Poor &&
                    trackingMonitor.IsTrackingStable)
                {
                    Debug.Log("RelocalizationManager: Relocalization successful");
                    OnTrackingRecovered();
                    yield break;
                }
                
                yield return new WaitForSeconds(1f);
            }
            
            // Relocalization timed out
            if (IsRelocalizing)
            {
                Debug.LogWarning("RelocalizationManager: Relocalization timed out");
                
                if (RelocalizationAttempts < maxRelocalizationAttempts)
                {
                    // Try again
                    yield return new WaitForSeconds(2f);
                    StartRelocalization();
                }
                else
                {
                    // Give up
                    IsRelocalizing = false;
                    OnRelocalizationFailed?.Invoke();
                    Debug.LogError("RelocalizationManager: Relocalization failed after maximum attempts");
                }
            }
        }
        
        /// <summary>
        /// Restore anchors after tracking recovery
        /// </summary>
        private IEnumerator RestoreAnchorsAfterDelay()
        {
            yield return new WaitForSeconds(anchorRestoreDelay);
            
            List<ARAnchor> restoredAnchors = new List<ARAnchor>();
            
            if (anchorManager != null && storedAnchors.Count > 0)
            {
                Debug.Log($"RelocalizationManager: Attempting to restore {storedAnchors.Count} anchors");
                
                foreach (var anchorData in storedAnchors)
                {
                    try
                    {
                        // Try to create anchor at stored position
                        var anchorRequest = anchorManager.AddAnchor(anchorData.pose);
                        
                        if (anchorRequest != null)
                        {
                            restoredAnchors.Add(anchorRequest);
                            activeAnchors[anchorData.anchorId] = anchorRequest;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"RelocalizationManager: Failed to restore anchor {anchorData.anchorId}: {ex.Message}");
                    }
                }
                
                Debug.Log($"RelocalizationManager: Successfully restored {restoredAnchors.Count} anchors");
            }
            
            // Fire anchors restored event
            OnAnchorsRestored?.Invoke(restoredAnchors);
        }
        
        /// <summary>
        /// Manually trigger relocalization
        /// </summary>
        public void ManualRelocalization()
        {
            Debug.Log("RelocalizationManager: Manual relocalization triggered");
            
            if (!IsRelocalizing)
            {
                StartRelocalization();
            }
        }
        
        /// <summary>
        /// Get relocalization status information
        /// </summary>
        public RelocalizationStatus GetRelocalizationStatus()
        {
            return new RelocalizationStatus
            {
                isTrackingLost = IsTrackingLost,
                isRelocalizing = IsRelocalizing,
                trackingLostDuration = IsTrackingLost ? Time.time - TrackingLostTime : 0f,
                relocalizationAttempts = RelocalizationAttempts,
                storedAnchorCount = storedAnchors.Count,
                activeAnchorCount = activeAnchors.Count
            };
        }
        
        /// <summary>
        /// Clear stored anchor data
        /// </summary>
        public void ClearStoredAnchors()
        {
            storedAnchors.Clear();
            activeAnchors.Clear();
            Debug.Log("RelocalizationManager: Cleared stored anchor data");
        }
    }
    
    /// <summary>
    /// Data structure for storing anchor information
    /// </summary>
    [System.Serializable]
    public class StoredAnchorData
    {
        public string anchorId;
        public Pose pose;
        public float timestamp;
    }
    
    /// <summary>
    /// Status information for relocalization process
    /// </summary>
    [System.Serializable]
    public class RelocalizationStatus
    {
        public bool isTrackingLost;
        public bool isRelocalizing;
        public float trackingLostDuration;
        public int relocalizationAttempts;
        public int storedAnchorCount;
        public int activeAnchorCount;
    }
}