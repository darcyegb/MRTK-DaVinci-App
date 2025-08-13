using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DaVinciEye.Input
{
    /// <summary>
    /// Gesture Feedback Manager - Implements visual feedback for recognized gestures
    /// Task 8.3: Implement visual feedback for recognized gestures
    /// </summary>
    public class GestureFeedbackManager : MonoBehaviour
    {
        [Header("Visual Feedback Configuration")]
        [SerializeField] private bool enableVisualFeedback = true;
        [SerializeField] private bool enableAudioFeedback = true;
        [SerializeField] private bool enableHapticFeedback = true;
        [SerializeField] private float feedbackDuration = 0.5f;
        
        [Header("Gesture Feedback Prefabs")]
        [SerializeField] private GameObject airTapFeedbackPrefab;
        [SerializeField] private GameObject pinchFeedbackPrefab;
        [SerializeField] private GameObject dragFeedbackPrefab;
        [SerializeField] private GameObject errorFeedbackPrefab;
        
        [Header("Feedback Materials")]
        [SerializeField] private Material successFeedbackMaterial;
        [SerializeField] private Material errorFeedbackMaterial;
        [SerializeField] private Material warningFeedbackMaterial;
        
        [Header("Audio Feedback")]
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip errorSound;
        [SerializeField] private AudioClip warningSound;
        [SerializeField] private AudioSource audioSource;
        
        [Header("UI Feedback Elements")]
        [SerializeField] private Canvas feedbackCanvas;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Image feedbackIcon;
        [SerializeField] private Animator feedbackAnimator;
        
        [Header("Feedback Timing")]
        [SerializeField] private float successFeedbackDuration = 0.3f;
        [SerializeField] private float errorFeedbackDuration = 1.0f;
        [SerializeField] private float warningFeedbackDuration = 0.7f;
        
        // Private fields
        private HandGestureManager gestureManager;
        private UIInteractionManager uiInteractionManager;
        private Queue<FeedbackRequest> feedbackQueue;
        private Dictionary<GestureType, GameObject> gestureFeedbackPrefabs;
        private List<GameObject> activeFeedbackObjects;
        private Coroutine currentFeedbackCoroutine;
        
        // Events
        public event Action<GestureType, FeedbackType> FeedbackDisplayed;
        public event Action<string> ErrorFeedbackDisplayed;
        
        private void Awake()
        {
            InitializeFeedbackSystem();
        }
        
        private void Start()
        {
            SetupFeedbackComponents();
            ConnectToGestureSystem();
        }
        
        /// <summary>
        /// Initialize the feedback system
        /// </summary>
        private void InitializeFeedbackSystem()
        {
            feedbackQueue = new Queue<FeedbackRequest>();
            activeFeedbackObjects = new List<GameObject>();
            gestureFeedbackPrefabs = new Dictionary<GestureType, GameObject>();
            
            // Map gesture types to feedback prefabs
            if (airTapFeedbackPrefab != null)
                gestureFeedbackPrefabs[GestureType.AirTap] = airTapFeedbackPrefab;
            if (pinchFeedbackPrefab != null)
                gestureFeedbackPrefabs[GestureType.Pinch] = pinchFeedbackPrefab;
            if (dragFeedbackPrefab != null)
                gestureFeedbackPrefabs[GestureType.Drag] = dragFeedbackPrefab;
            
            Debug.Log("[GestureFeedbackManager] Feedback system initialized");
        }
        
        /// <summary>
        /// Setup feedback UI components
        /// </summary>
        private void SetupFeedbackComponents()
        {
            // Create feedback canvas if not assigned
            if (feedbackCanvas == null)
            {
                CreateFeedbackCanvas();
            }
            
            // Setup audio source if not assigned
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.volume = 0.5f;
            }
            
            // Create default materials if not assigned
            CreateDefaultMaterials();
        }
        
        /// <summary>
        /// Create feedback canvas for UI feedback
        /// </summary>
        private void CreateFeedbackCanvas()
        {
            var canvasObject = new GameObject("Gesture Feedback Canvas");
            feedbackCanvas = canvasObject.AddComponent<Canvas>();
            feedbackCanvas.renderMode = RenderMode.WorldSpace;
            feedbackCanvas.worldCamera = Camera.main;
            
            // Position canvas in front of user
            canvasObject.transform.position = new Vector3(0, 1.8f, 1.5f);
            canvasObject.transform.localScale = Vector3.one * 0.001f;
            
            // Add graphic raycaster
            canvasObject.AddComponent<GraphicRaycaster>();
            
            // Create feedback text
            CreateFeedbackText();
            CreateFeedbackIcon();
        }
        
        private void CreateFeedbackText()
        {
            var textObject = new GameObject("Feedback Text");
            textObject.transform.SetParent(feedbackCanvas.transform);
            
            feedbackText = textObject.AddComponent<Text>();
            feedbackText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            feedbackText.fontSize = 24;
            feedbackText.color = Color.white;
            feedbackText.alignment = TextAnchor.MiddleCenter;
            feedbackText.text = "";
            
            // Position text
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.7f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.7f);
            rectTransform.sizeDelta = new Vector2(400, 50);
        }
        
        private void CreateFeedbackIcon()
        {
            var iconObject = new GameObject("Feedback Icon");
            iconObject.transform.SetParent(feedbackCanvas.transform);
            
            feedbackIcon = iconObject.AddComponent<Image>();
            feedbackIcon.color = Color.white;
            
            // Position icon
            var rectTransform = iconObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(64, 64);
        }
        
        /// <summary>
        /// Create default materials for feedback
        /// </summary>
        private void CreateDefaultMaterials()
        {
            if (successFeedbackMaterial == null)
            {
                successFeedbackMaterial = new Material(Shader.Find("Standard"));
                successFeedbackMaterial.color = Color.green;
                successFeedbackMaterial.SetFloat("_Metallic", 0.5f);
                successFeedbackMaterial.SetFloat("_Smoothness", 0.8f);
            }
            
            if (errorFeedbackMaterial == null)
            {
                errorFeedbackMaterial = new Material(Shader.Find("Standard"));
                errorFeedbackMaterial.color = Color.red;
                errorFeedbackMaterial.SetFloat("_Metallic", 0.5f);
                errorFeedbackMaterial.SetFloat("_Smoothness", 0.8f);
            }
            
            if (warningFeedbackMaterial == null)
            {
                warningFeedbackMaterial = new Material(Shader.Find("Standard"));
                warningFeedbackMaterial.color = Color.yellow;
                warningFeedbackMaterial.SetFloat("_Metallic", 0.5f);
                warningFeedbackMaterial.SetFloat("_Smoothness", 0.8f);
            }
        }
        
        /// <summary>
        /// Connect to gesture system for feedback events
        /// </summary>
        private void ConnectToGestureSystem()
        {
            gestureManager = FindObjectOfType<HandGestureManager>();
            uiInteractionManager = FindObjectOfType<UIInteractionManager>();
            
            if (gestureManager != null)
            {
                gestureManager.OnGestureRecognized += OnGestureRecognized;
                gestureManager.OnAirTap += OnAirTap;
                gestureManager.OnPinchStart += OnPinchStart;
                gestureManager.OnPinchUpdate += OnPinchUpdate;
                gestureManager.OnPinchEnd += OnPinchEnd;
                gestureManager.OnHandTrackingLost += OnHandTrackingLost;
                gestureManager.OnHandTrackingRestored += OnHandTrackingRestored;
            }
            
            if (uiInteractionManager != null)
            {
                uiInteractionManager.OnGestureConflictDetected.AddListener(OnGestureConflictDetected);
                uiInteractionManager.InteractionModeChanged += OnInteractionModeChanged;
            }
            
            Debug.Log("[GestureFeedbackManager] Connected to gesture system");
        }
        
        /// <summary>
        /// Display visual feedback for recognized gesture
        /// </summary>
        public void ShowGestureFeedback(GestureType gestureType, Vector3 position, FeedbackType feedbackType = FeedbackType.Success)
        {
            if (!enableVisualFeedback) return;
            
            var request = new FeedbackRequest
            {
                gestureType = gestureType,
                position = position,
                feedbackType = feedbackType,
                timestamp = Time.time
            };
            
            feedbackQueue.Enqueue(request);
            ProcessFeedbackQueue();
            
            FeedbackDisplayed?.Invoke(gestureType, feedbackType);
        }
        
        /// <summary>
        /// Display error feedback with message
        /// </summary>
        public void ShowErrorFeedback(string errorMessage, Vector3 position)
        {
            ShowTextFeedback(errorMessage, FeedbackType.Error);
            ShowVisualFeedback(position, FeedbackType.Error);
            PlayAudioFeedback(FeedbackType.Error);
            
            ErrorFeedbackDisplayed?.Invoke(errorMessage);
            Debug.LogWarning($"[GestureFeedbackManager] Error feedback: {errorMessage}");
        }
        
        /// <summary>
        /// Display warning feedback
        /// </summary>
        public void ShowWarningFeedback(string warningMessage, Vector3 position)
        {
            ShowTextFeedback(warningMessage, FeedbackType.Warning);
            ShowVisualFeedback(position, FeedbackType.Warning);
            PlayAudioFeedback(FeedbackType.Warning);
            
            Debug.LogWarning($"[GestureFeedbackManager] Warning feedback: {warningMessage}");
        }
        
        /// <summary>
        /// Process feedback queue
        /// </summary>
        private void ProcessFeedbackQueue()
        {
            if (feedbackQueue.Count > 0 && currentFeedbackCoroutine == null)
            {
                currentFeedbackCoroutine = StartCoroutine(ProcessFeedbackCoroutine());
            }
        }
        
        private IEnumerator ProcessFeedbackCoroutine()
        {
            while (feedbackQueue.Count > 0)
            {
                var request = feedbackQueue.Dequeue();
                
                ShowVisualFeedback(request.position, request.feedbackType);
                ShowTextFeedback(GetGestureDisplayName(request.gestureType), request.feedbackType);
                PlayAudioFeedback(request.feedbackType);
                
                yield return new WaitForSeconds(GetFeedbackDuration(request.feedbackType));
            }
            
            currentFeedbackCoroutine = null;
        }
        
        /// <summary>
        /// Show visual feedback at position
        /// </summary>
        private void ShowVisualFeedback(Vector3 position, FeedbackType feedbackType)
        {
            GameObject feedbackPrefab = GetFeedbackPrefab(feedbackType);
            if (feedbackPrefab != null)
            {
                var feedbackObject = Instantiate(feedbackPrefab, position, Quaternion.identity);
                activeFeedbackObjects.Add(feedbackObject);
                
                // Apply appropriate material
                var renderer = feedbackObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = GetFeedbackMaterial(feedbackType);
                }
                
                // Auto-destroy after duration
                StartCoroutine(DestroyFeedbackObject(feedbackObject, GetFeedbackDuration(feedbackType)));
            }
        }
        
        /// <summary>
        /// Show text feedback on UI
        /// </summary>
        private void ShowTextFeedback(string message, FeedbackType feedbackType)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = GetFeedbackColor(feedbackType);
                
                // Animate text if animator is available
                if (feedbackAnimator != null)
                {
                    feedbackAnimator.SetTrigger("ShowFeedback");
                }
                
                StartCoroutine(ClearTextFeedback(GetFeedbackDuration(feedbackType)));
            }
        }
        
        /// <summary>
        /// Play audio feedback
        /// </summary>
        private void PlayAudioFeedback(FeedbackType feedbackType)
        {
            if (!enableAudioFeedback || audioSource == null) return;
            
            AudioClip clip = GetFeedbackAudioClip(feedbackType);
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        /// <summary>
        /// Get feedback prefab for type
        /// </summary>
        private GameObject GetFeedbackPrefab(FeedbackType feedbackType)
        {
            switch (feedbackType)
            {
                case FeedbackType.Error:
                    return errorFeedbackPrefab ?? CreateDefaultFeedbackPrefab();
                case FeedbackType.Success:
                case FeedbackType.Warning:
                default:
                    return airTapFeedbackPrefab ?? CreateDefaultFeedbackPrefab();
            }
        }
        
        private GameObject CreateDefaultFeedbackPrefab()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            prefab.transform.localScale = Vector3.one * 0.1f;
            return prefab;
        }
        
        /// <summary>
        /// Get feedback material for type
        /// </summary>
        private Material GetFeedbackMaterial(FeedbackType feedbackType)
        {
            switch (feedbackType)
            {
                case FeedbackType.Success:
                    return successFeedbackMaterial;
                case FeedbackType.Error:
                    return errorFeedbackMaterial;
                case FeedbackType.Warning:
                    return warningFeedbackMaterial;
                default:
                    return successFeedbackMaterial;
            }
        }
        
        /// <summary>
        /// Get feedback color for type
        /// </summary>
        private Color GetFeedbackColor(FeedbackType feedbackType)
        {
            switch (feedbackType)
            {
                case FeedbackType.Success:
                    return Color.green;
                case FeedbackType.Error:
                    return Color.red;
                case FeedbackType.Warning:
                    return Color.yellow;
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// Get feedback audio clip for type
        /// </summary>
        private AudioClip GetFeedbackAudioClip(FeedbackType feedbackType)
        {
            switch (feedbackType)
            {
                case FeedbackType.Success:
                    return successSound;
                case FeedbackType.Error:
                    return errorSound;
                case FeedbackType.Warning:
                    return warningSound;
                default:
                    return successSound;
            }
        }
        
        /// <summary>
        /// Get feedback duration for type
        /// </summary>
        private float GetFeedbackDuration(FeedbackType feedbackType)
        {
            switch (feedbackType)
            {
                case FeedbackType.Success:
                    return successFeedbackDuration;
                case FeedbackType.Error:
                    return errorFeedbackDuration;
                case FeedbackType.Warning:
                    return warningFeedbackDuration;
                default:
                    return feedbackDuration;
            }
        }
        
        /// <summary>
        /// Get display name for gesture type
        /// </summary>
        private string GetGestureDisplayName(GestureType gestureType)
        {
            switch (gestureType)
            {
                case GestureType.AirTap:
                    return "Air Tap";
                case GestureType.Pinch:
                    return "Pinch";
                case GestureType.Drag:
                    return "Drag";
                case GestureType.TwoHandPinch:
                    return "Two Hand Pinch";
                case GestureType.Palm:
                    return "Palm";
                case GestureType.Point:
                    return "Point";
                case GestureType.Grab:
                    return "Grab";
                default:
                    return "Gesture";
            }
        }
        
        private IEnumerator DestroyFeedbackObject(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (obj != null)
            {
                activeFeedbackObjects.Remove(obj);
                Destroy(obj);
            }
        }
        
        private IEnumerator ClearTextFeedback(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (feedbackText != null)
            {
                feedbackText.text = "";
            }
        }
        
        // Event handlers
        private void OnGestureRecognized(GestureData gestureData)
        {
            ShowGestureFeedback(gestureData.type, gestureData.position, FeedbackType.Success);
        }
        
        private void OnAirTap(Vector3 position)
        {
            ShowGestureFeedback(GestureType.AirTap, position, FeedbackType.Success);
        }
        
        private void OnPinchStart(PinchData pinchData)
        {
            ShowGestureFeedback(GestureType.Pinch, pinchData.startPosition, FeedbackType.Success);
        }
        
        private void OnPinchUpdate(PinchData pinchData)
        {
            // Optional: Show continuous feedback during pinch
        }
        
        private void OnPinchEnd(PinchData pinchData)
        {
            ShowGestureFeedback(GestureType.Pinch, pinchData.currentPosition, FeedbackType.Success);
        }
        
        private void OnHandTrackingLost()
        {
            ShowErrorFeedback("Hand tracking lost", Vector3.zero);
        }
        
        private void OnHandTrackingRestored()
        {
            ShowTextFeedback("Hand tracking restored", FeedbackType.Success);
        }
        
        private void OnGestureConflictDetected(string conflictMessage)
        {
            ShowWarningFeedback($"Gesture conflict: {conflictMessage}", Vector3.zero);
        }
        
        private void OnInteractionModeChanged(InteractionMode newMode)
        {
            ShowTextFeedback($"Mode: {newMode}", FeedbackType.Success);
        }
        
        /// <summary>
        /// Public API methods
        /// </summary>
        public void SetFeedbackEnabled(bool visual, bool audio, bool haptic)
        {
            enableVisualFeedback = visual;
            enableAudioFeedback = audio;
            enableHapticFeedback = haptic;
        }
        
        public void ClearAllFeedback()
        {
            // Clear active feedback objects
            foreach (var obj in activeFeedbackObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            activeFeedbackObjects.Clear();
            
            // Clear text feedback
            if (feedbackText != null)
                feedbackText.text = "";
            
            // Clear feedback queue
            feedbackQueue.Clear();
            
            // Stop current feedback coroutine
            if (currentFeedbackCoroutine != null)
            {
                StopCoroutine(currentFeedbackCoroutine);
                currentFeedbackCoroutine = null;
            }
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (gestureManager != null)
            {
                gestureManager.OnGestureRecognized -= OnGestureRecognized;
                gestureManager.OnAirTap -= OnAirTap;
                gestureManager.OnPinchStart -= OnPinchStart;
                gestureManager.OnPinchUpdate -= OnPinchUpdate;
                gestureManager.OnPinchEnd -= OnPinchEnd;
                gestureManager.OnHandTrackingLost -= OnHandTrackingLost;
                gestureManager.OnHandTrackingRestored -= OnHandTrackingRestored;
            }
            
            if (uiInteractionManager != null)
            {
                uiInteractionManager.OnGestureConflictDetected.RemoveListener(OnGestureConflictDetected);
                uiInteractionManager.InteractionModeChanged -= OnInteractionModeChanged;
            }
            
            ClearAllFeedback();
        }
    }
    
    /// <summary>
    /// Feedback request data structure
    /// </summary>
    [System.Serializable]
    public class FeedbackRequest
    {
        public GestureType gestureType;
        public Vector3 position;
        public FeedbackType feedbackType;
        public float timestamp;
    }
    
    /// <summary>
    /// Types of feedback
    /// </summary>
    public enum FeedbackType
    {
        Success,
        Error,
        Warning,
        Info
    }
}