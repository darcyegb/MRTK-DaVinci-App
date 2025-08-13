using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DaVinciEye.Canvas
{
    /// <summary>
    /// Manages spatial anchoring for canvas persistence using Unity's XR Subsystem and AR Foundation
    /// Provides canvas position persistence and restoration functionality with MRTK integration
    /// </summary>
    public class CanvasAnchorManager : MonoBehaviour
    {
        [Header("AR Foundation Components")]
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private ARSessionOrigin sessionOrigin;
        
        [Header("Anchor Configuration")]
        [SerializeField] private float anchorValidationDistance = 0.1f; // 10cm tolerance
        [SerializeField] private int maxAnchorRetries = 3;
        [SerializeField] private float anchorTimeout = 10.0f; // 10 seconds
        
        [Header("Persistence Settings")]
        [SerializeField] private bool enableAutomaticPersistence = true;
        [SerializeField] private string canvasAnchorPrefix = "DaVinciCanvas_";
        
        // Anchor tracking
        private Dictionary<string, ARAnchor> activeAnchors = new Dictionary<string, ARAnchor>();
        private Dictionary<string, CanvasData> anchoredCanvases = new Dictionary<string, CanvasData>();
        
        // Events
        public event Action<string, CanvasData> OnCanvasAnchored;
        public event Action<string, CanvasData> OnCanvasAnchorRestored;
        public event Action<string> OnCanvasAnchorLost;
        public event Action<string, string> OnAnchorError;
        
        // State tracking
        private bool isInitialized = false;
        private Coroutine anchorValidationCoroutine;
        
        private void Awake()
        {
            InitializeAnchorManager();
        }
        
        private void Start()
        {
            StartCoroutine(InitializeARFoundation());
        }
        
        private void InitializeAnchorManager()
        {
            // Find AR Foundation components if not assigned
            if (anchorManager == null)
            {
                anchorManager = FindObjectOfType<ARAnchorManager>();
            }
            
            if (sessionOrigin == null)
            {
                sessionOrigin = FindObjectOfType<ARSessionOrigin>();
            }
            
            // Subscribe to anchor events
            if (anchorManager != null)
            {
                anchorManager.anchorsChanged += OnAnchorsChanged;
            }
            
            Debug.Log("CanvasAnchorManager: Anchor manager initialized");
        }
        
        private IEnumerator InitializeARFoundation()
        {
            // Wait for AR session to be ready
            while (ARSession.state != ARSessionState.SessionTracking)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            isInitialized = true;
            
            // Start anchor validation coroutine
            if (anchorValidationCoroutine == null)
            {
                anchorValidationCoroutine = StartCoroutine(ValidateAnchorsCoroutine());
            }
            
            Debug.Log("CanvasAnchorManager: AR Foundation initialized and ready");
        }
        
        /// <summary>
        /// Creates a spatial anchor for the given canvas data
        /// </summary>
        public void CreateCanvasAnchor(CanvasData canvasData, string canvasId = null)
        {
            if (!isInitialized)
            {
                Debug.LogError("CanvasAnchorManager: AR Foundation not initialized");
                OnAnchorError?.Invoke(canvasId, "AR Foundation not initialized");
                return;
            }
            
            if (canvasData == null || !canvasData.isValid)
            {
                Debug.LogError("CanvasAnchorManager: Invalid canvas data provided");
                OnAnchorError?.Invoke(canvasId, "Invalid canvas data");
                return;
            }
            
            if (string.IsNullOrEmpty(canvasId))
            {
                canvasId = GenerateCanvasId();
            }
            
            StartCoroutine(CreateAnchorCoroutine(canvasData, canvasId));
        }
        
        private IEnumerator CreateAnchorCoroutine(CanvasData canvasData, string canvasId)
        {
            Debug.Log($"CanvasAnchorManager: Creating anchor for canvas {canvasId}");
            
            // Create anchor at canvas center position
            var anchorPose = new Pose(canvasData.center, Quaternion.identity);
            
            // Request anchor creation
            var anchorRequest = anchorManager.RequestAnchor(anchorPose);
            
            float timeoutTimer = 0f;
            
            // Wait for anchor creation with timeout
            while (anchorRequest.status == TrackableRequestStatus.Pending && timeoutTimer < anchorTimeout)
            {
                timeoutTimer += Time.deltaTime;
                yield return null;
            }
            
            if (anchorRequest.status == TrackableRequestStatus.Complete && anchorRequest.result != null)
            {
                var anchor = anchorRequest.result;
                
                // Store anchor and canvas data
                activeAnchors[canvasId] = anchor;
                anchoredCanvases[canvasId] = canvasData;
                canvasData.anchorId = canvasId;
                
                // Validate anchor position
                if (ValidateAnchorPosition(anchor, canvasData.center))
                {
                    Debug.Log($"CanvasAnchorManager: Anchor created successfully for canvas {canvasId}");
                    OnCanvasAnchored?.Invoke(canvasId, canvasData);
                    
                    // Save to persistent storage if enabled
                    if (enableAutomaticPersistence)
                    {
                        SaveCanvasAnchorData(canvasId, canvasData);
                    }
                }
                else
                {
                    Debug.LogWarning($"CanvasAnchorManager: Anchor position validation failed for canvas {canvasId}");
                    OnAnchorError?.Invoke(canvasId, "Anchor position validation failed");
                }
            }
            else
            {
                Debug.LogError($"CanvasAnchorManager: Failed to create anchor for canvas {canvasId} - Status: {anchorRequest.status}");
                OnAnchorError?.Invoke(canvasId, $"Anchor creation failed: {anchorRequest.status}");
            }
        }
        
        /// <summary>
        /// Restores a canvas from its spatial anchor
        /// </summary>
        public void RestoreCanvasAnchor(string canvasId)
        {
            if (!isInitialized)
            {
                Debug.LogError("CanvasAnchorManager: AR Foundation not initialized");
                return;
            }
            
            StartCoroutine(RestoreAnchorCoroutine(canvasId));
        }
        
        private IEnumerator RestoreAnchorCoroutine(string canvasId)
        {
            Debug.Log($"CanvasAnchorManager: Restoring anchor for canvas {canvasId}");
            
            // Load canvas data from persistent storage
            var canvasData = LoadCanvasAnchorData(canvasId);
            if (canvasData == null)
            {
                Debug.LogError($"CanvasAnchorManager: No saved data found for canvas {canvasId}");
                OnAnchorError?.Invoke(canvasId, "No saved data found");
                yield break;
            }
            
            // Check if anchor already exists in active anchors
            if (activeAnchors.ContainsKey(canvasId))
            {
                var existingAnchor = activeAnchors[canvasId];
                if (existingAnchor != null && existingAnchor.trackingState == TrackingState.Tracking)
                {
                    Debug.Log($"CanvasAnchorManager: Anchor {canvasId} already active and tracking");
                    OnCanvasAnchorRestored?.Invoke(canvasId, canvasData);
                    yield break;
                }
            }
            
            // Wait for anchor to be tracked (AR Foundation will automatically restore persistent anchors)
            float timeoutTimer = 0f;
            bool anchorFound = false;
            
            while (timeoutTimer < anchorTimeout && !anchorFound)
            {
                // Check if any tracked anchors match our canvas position
                foreach (var anchor in anchorManager.trackables)
                {
                    if (ValidateAnchorPosition(anchor, canvasData.center))
                    {
                        activeAnchors[canvasId] = anchor;
                        anchoredCanvases[canvasId] = canvasData;
                        anchorFound = true;
                        
                        Debug.Log($"CanvasAnchorManager: Anchor restored for canvas {canvasId}");
                        OnCanvasAnchorRestored?.Invoke(canvasId, canvasData);
                        break;
                    }
                }
                
                timeoutTimer += Time.deltaTime;
                yield return new WaitForSeconds(0.1f);
            }
            
            if (!anchorFound)
            {
                Debug.LogWarning($"CanvasAnchorManager: Failed to restore anchor for canvas {canvasId} within timeout");
                OnAnchorError?.Invoke(canvasId, "Anchor restoration timeout");
            }
        }
        
        /// <summary>
        /// Removes a canvas anchor
        /// </summary>
        public void RemoveCanvasAnchor(string canvasId)
        {
            if (activeAnchors.ContainsKey(canvasId))
            {
                var anchor = activeAnchors[canvasId];
                if (anchor != null)
                {
                    anchorManager.RemoveAnchor(anchor);
                }
                
                activeAnchors.Remove(canvasId);
            }
            
            if (anchoredCanvases.ContainsKey(canvasId))
            {
                anchoredCanvases.Remove(canvasId);
            }
            
            // Remove from persistent storage
            RemoveCanvasAnchorData(canvasId);
            
            Debug.Log($"CanvasAnchorManager: Anchor removed for canvas {canvasId}");
        }
        
        /// <summary>
        /// Gets all anchored canvas IDs
        /// </summary>
        public List<string> GetAnchoredCanvasIds()
        {
            return new List<string>(anchoredCanvases.Keys);
        }
        
        /// <summary>
        /// Gets canvas data for a specific anchor
        /// </summary>
        public CanvasData GetAnchoredCanvasData(string canvasId)
        {
            return anchoredCanvases.ContainsKey(canvasId) ? anchoredCanvases[canvasId] : null;
        }
        
        /// <summary>
        /// Checks if a canvas is currently anchored and tracking
        /// </summary>
        public bool IsCanvasAnchored(string canvasId)
        {
            if (!activeAnchors.ContainsKey(canvasId)) return false;
            
            var anchor = activeAnchors[canvasId];
            return anchor != null && anchor.trackingState == TrackingState.Tracking;
        }
        
        private void OnAnchorsChanged(ARAnchorsChangedEventArgs eventArgs)
        {
            // Handle anchor tracking changes
            foreach (var anchor in eventArgs.updated)
            {
                HandleAnchorTrackingChange(anchor);
            }
            
            foreach (var anchor in eventArgs.removed)
            {
                HandleAnchorRemoved(anchor);
            }
        }
        
        private void HandleAnchorTrackingChange(ARAnchor anchor)
        {
            // Find canvas ID for this anchor
            string canvasId = null;
            foreach (var kvp in activeAnchors)
            {
                if (kvp.Value == anchor)
                {
                    canvasId = kvp.Key;
                    break;
                }
            }
            
            if (canvasId != null)
            {
                if (anchor.trackingState == TrackingState.None || anchor.trackingState == TrackingState.Limited)
                {
                    Debug.LogWarning($"CanvasAnchorManager: Anchor tracking lost for canvas {canvasId}");
                    OnCanvasAnchorLost?.Invoke(canvasId);
                }
                else if (anchor.trackingState == TrackingState.Tracking)
                {
                    Debug.Log($"CanvasAnchorManager: Anchor tracking restored for canvas {canvasId}");
                    
                    if (anchoredCanvases.ContainsKey(canvasId))
                    {
                        OnCanvasAnchorRestored?.Invoke(canvasId, anchoredCanvases[canvasId]);
                    }
                }
            }
        }
        
        private void HandleAnchorRemoved(ARAnchor anchor)
        {
            // Find and remove canvas ID for this anchor
            string canvasIdToRemove = null;
            foreach (var kvp in activeAnchors)
            {
                if (kvp.Value == anchor)
                {
                    canvasIdToRemove = kvp.Key;
                    break;
                }
            }
            
            if (canvasIdToRemove != null)
            {
                activeAnchors.Remove(canvasIdToRemove);
                Debug.Log($"CanvasAnchorManager: Anchor removed for canvas {canvasIdToRemove}");
                OnCanvasAnchorLost?.Invoke(canvasIdToRemove);
            }
        }
        
        private IEnumerator ValidateAnchorsCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f); // Check every second
                
                if (isInitialized)
                {
                    ValidateActiveAnchors();
                }
            }
        }
        
        private void ValidateActiveAnchors()
        {
            var anchorsToRemove = new List<string>();
            
            foreach (var kvp in activeAnchors)
            {
                var canvasId = kvp.Key;
                var anchor = kvp.Value;
                
                if (anchor == null || anchor.trackingState == TrackingState.None)
                {
                    anchorsToRemove.Add(canvasId);
                }
            }
            
            foreach (var canvasId in anchorsToRemove)
            {
                Debug.LogWarning($"CanvasAnchorManager: Removing invalid anchor for canvas {canvasId}");
                activeAnchors.Remove(canvasId);
                OnCanvasAnchorLost?.Invoke(canvasId);
            }
        }
        
        private bool ValidateAnchorPosition(ARAnchor anchor, Vector3 expectedPosition)
        {
            if (anchor == null) return false;
            
            var distance = Vector3.Distance(anchor.transform.position, expectedPosition);
            return distance <= anchorValidationDistance;
        }
        
        private string GenerateCanvasId()
        {
            return canvasAnchorPrefix + System.Guid.NewGuid().ToString("N")[..8];
        }
        
        // Persistent storage methods
        private void SaveCanvasAnchorData(string canvasId, CanvasData canvasData)
        {
            try
            {
                var json = JsonUtility.ToJson(canvasData);
                PlayerPrefs.SetString($"CanvasAnchor_{canvasId}", json);
                PlayerPrefs.Save();
                
                Debug.Log($"CanvasAnchorManager: Canvas data saved for {canvasId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"CanvasAnchorManager: Failed to save canvas data for {canvasId}: {ex.Message}");
            }
        }
        
        private CanvasData LoadCanvasAnchorData(string canvasId)
        {
            try
            {
                var key = $"CanvasAnchor_{canvasId}";
                if (PlayerPrefs.HasKey(key))
                {
                    var json = PlayerPrefs.GetString(key);
                    var canvasData = JsonUtility.FromJson<CanvasData>(json);
                    
                    Debug.Log($"CanvasAnchorManager: Canvas data loaded for {canvasId}");
                    return canvasData;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CanvasAnchorManager: Failed to load canvas data for {canvasId}: {ex.Message}");
            }
            
            return null;
        }
        
        private void RemoveCanvasAnchorData(string canvasId)
        {
            try
            {
                var key = $"CanvasAnchor_{canvasId}";
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                    PlayerPrefs.Save();
                    
                    Debug.Log($"CanvasAnchorManager: Canvas data removed for {canvasId}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CanvasAnchorManager: Failed to remove canvas data for {canvasId}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets all saved canvas IDs from persistent storage
        /// </summary>
        public List<string> GetSavedCanvasIds()
        {
            var canvasIds = new List<string>();
            
            // This is a simplified approach - in a real implementation, you might want to maintain an index
            for (int i = 0; i < 100; i++) // Check up to 100 possible canvas IDs
            {
                var testId = canvasAnchorPrefix + i.ToString("D2");
                if (PlayerPrefs.HasKey($"CanvasAnchor_{testId}"))
                {
                    canvasIds.Add(testId);
                }
            }
            
            return canvasIds;
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (anchorManager != null)
            {
                anchorManager.anchorsChanged -= OnAnchorsChanged;
            }
            
            // Stop validation coroutine
            if (anchorValidationCoroutine != null)
            {
                StopCoroutine(anchorValidationCoroutine);
            }
        }
        
        // Public utility methods
        public void SetAnchorValidationDistance(float distance)
        {
            anchorValidationDistance = Mathf.Max(0.01f, distance);
        }
        
        public void SetAutomaticPersistence(bool enabled)
        {
            enableAutomaticPersistence = enabled;
        }
        
        public int GetActiveAnchorCount()
        {
            return activeAnchors.Count;
        }
        
        public bool IsInitialized => isInitialized;
    }
}