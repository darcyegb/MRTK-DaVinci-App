using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace DaVinciEye.Input
{
    /// <summary>
    /// Fallback Interaction Manager - Creates fallback interaction methods for gesture recognition failures
    /// Task 8.3: Create fallback interaction methods for gesture recognition failures
    /// </summary>
    public class FallbackInteractionManager : MonoBehaviour
    {
        [Header("Fallback Configuration")]
        [SerializeField] private bool enableFallbackMethods = true;
        [SerializeField] private float gestureFailureTimeout = 2f;
        [SerializeField] private int maxGestureFailures = 3;
        [SerializeField] private float fallbackActivationDelay = 1f;
        
        [Header("Fallback Methods")]
        [SerializeField] private bool enableVoiceFallback = true;
        [SerializeField] private bool enableGazeFallback = true;
        [SerializeField] private bool enableControllerFallback = true;
        [SerializeField] private bool enableKeyboardFallback = true;
        
        [Header("Voice Fallback")]
        [SerializeField] private string[] voiceCommands = { "Select", "Back", "Menu", "Help", "Reset" };
        [SerializeField] private float voiceRecognitionTimeout = 3f;
        
        [Header("Gaze Fallback")]
        [SerializeField] private float gazeDwellTime = 2f;
        [SerializeField] private float gazeRayDistance = 10f;
        [SerializeField] private LayerMask gazeLayerMask = -1;
        
        [Header("Controller Fallback")]
        [SerializeField] private bool useGamepadInput = true;
        [SerializeField] private bool useMouseInput = true;
        [SerializeField] private bool useKeyboardInput = true;
        
        [Header("UI Fallback Elements")]
        [SerializeField] private Canvas fallbackCanvas;
        [SerializeField] private Button[] fallbackButtons;
        [SerializeField] private Text fallbackInstructionText;
        [SerializeField] private GameObject fallbackMenu;
        
        // Private fields
        private HandGestureManager gestureManager;
        private UIInteractionManager uiInteractionManager;
        private GestureFeedbackManager feedbackManager;
        
        private int consecutiveGestureFailures = 0;
        private float lastGestureFailureTime = 0f;
        private bool fallbackModeActive = false;
        private FallbackMethod currentFallbackMethod = FallbackMethod.None;
        
        private Camera mainCamera;
        private GameObject gazeTarget;
        private float gazeStartTime;
        private Coroutine gazeDwellCoroutine;
        
        // Events
        public event Action<FallbackMethod> FallbackMethodActivated;
        public event Action<FallbackMethod> FallbackMethodDeactivated;
        public event Action<string> FallbackActionExecuted;
        
        private void Awake()
        {
            InitializeFallbackSystem();
        }
        
        private void Start()
        {
            SetupFallbackComponents();
            ConnectToGestureSystem();
        }
        
        private void Update()
        {
            if (fallbackModeActive)
            {
                UpdateFallbackMethods();
            }
            
            MonitorGestureFailures();
        }
        
        /// <summary>
        /// Initialize the fallback system
        /// </summary>
        private void InitializeFallbackSystem()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();
            
            Debug.Log("[FallbackInteractionManager] Fallback system initialized");
        }
        
        /// <summary>
        /// Setup fallback UI components
        /// </summary>
        private void SetupFallbackComponents()
        {
            // Create fallback canvas if not assigned
            if (fallbackCanvas == null)
            {
                CreateFallbackCanvas();
            }
            
            // Setup fallback buttons
            SetupFallbackButtons();
            
            // Initially hide fallback UI
            SetFallbackUIVisible(false);
        }
        
        /// <summary>
        /// Create fallback canvas for alternative interactions
        /// </summary>
        private void CreateFallbackCanvas()
        {
            var canvasObject = new GameObject("Fallback Interaction Canvas");
            fallbackCanvas = canvasObject.AddComponent<Canvas>();
            fallbackCanvas.renderMode = RenderMode.WorldSpace;
            fallbackCanvas.worldCamera = mainCamera;
            
            // Position canvas in front of user
            canvasObject.transform.position = new Vector3(0, 1.5f, 2f);
            canvasObject.transform.localScale = Vector3.one * 0.001f;
            
            canvasObject.AddComponent<GraphicRaycaster>();
            
            // Create instruction text
            CreateFallbackInstructionText();
            CreateFallbackMenu();
        }
        
        private void CreateFallbackInstructionText()
        {
            var textObject = new GameObject("Fallback Instructions");
            textObject.transform.SetParent(fallbackCanvas.transform);
            
            fallbackInstructionText = textObject.AddComponent<Text>();
            fallbackInstructionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            fallbackInstructionText.fontSize = 18;
            fallbackInstructionText.color = Color.white;
            fallbackInstructionText.alignment = TextAnchor.MiddleCenter;
            fallbackInstructionText.text = "Gesture recognition failed. Using alternative input methods.";
            
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
            rectTransform.sizeDelta = new Vector2(600, 40);
        }
        
        private void CreateFallbackMenu()
        {
            fallbackMenu = new GameObject("Fallback Menu");
            fallbackMenu.transform.SetParent(fallbackCanvas.transform);
            
            // Create menu background
            var menuImage = fallbackMenu.AddComponent<Image>();
            menuImage.color = new Color(0, 0, 0, 0.7f);
            
            var menuRect = fallbackMenu.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuRect.sizeDelta = new Vector2(400, 300);
        }
        
        /// <summary>
        /// Setup fallback buttons for alternative interactions
        /// </summary>
        private void SetupFallbackButtons()
        {
            if (fallbackButtons == null || fallbackButtons.Length == 0)
            {
                CreateFallbackButtons();
            }
            
            // Configure button events
            for (int i = 0; i < fallbackButtons.Length; i++)
            {
                if (fallbackButtons[i] != null)
                {
                    int buttonIndex = i; // Capture for closure
                    fallbackButtons[i].onClick.AddListener(() => OnFallbackButtonClicked(buttonIndex));
                }
            }
        }
        
        private void CreateFallbackButtons()
        {
            string[] buttonLabels = { "Select", "Back", "Menu", "Help", "Reset" };
            fallbackButtons = new Button[buttonLabels.Length];
            
            for (int i = 0; i < buttonLabels.Length; i++)
            {
                var buttonObject = new GameObject($"Fallback Button {buttonLabels[i]}");
                buttonObject.transform.SetParent(fallbackMenu.transform);
                
                var button = buttonObject.AddComponent<Button>();
                var buttonImage = buttonObject.AddComponent<Image>();
                buttonImage.color = Color.gray;
                
                // Create button text
                var textObject = new GameObject("Text");
                textObject.transform.SetParent(buttonObject.transform);
                var text = textObject.AddComponent<Text>();
                text.text = buttonLabels[i];
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 16;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
                
                // Position button
                var buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.1f, 0.8f - i * 0.15f);
                buttonRect.anchorMax = new Vector2(0.9f, 0.9f - i * 0.15f);
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;
                
                // Position text
                var textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                
                fallbackButtons[i] = button;
            }
        }
        
        /// <summary>
        /// Connect to gesture system for failure monitoring
        /// </summary>
        private void ConnectToGestureSystem()
        {
            gestureManager = FindObjectOfType<HandGestureManager>();
            uiInteractionManager = FindObjectOfType<UIInteractionManager>();
            feedbackManager = FindObjectOfType<GestureFeedbackManager>();
            
            if (gestureManager != null)
            {
                gestureManager.OnHandTrackingLost += OnHandTrackingLost;
                gestureManager.OnHandTrackingRestored += OnHandTrackingRestored;
            }
            
            if (uiInteractionManager != null)
            {
                uiInteractionManager.OnGestureConflictDetected.AddListener(OnGestureConflictDetected);
            }
            
            Debug.Log("[FallbackInteractionManager] Connected to gesture system");
        }
        
        /// <summary>
        /// Monitor gesture failures and activate fallback methods
        /// </summary>
        private void MonitorGestureFailures()
        {
            // Check if we should activate fallback mode
            if (!fallbackModeActive && consecutiveGestureFailures >= maxGestureFailures)
            {
                ActivateFallbackMode();
            }
            
            // Check if we should deactivate fallback mode
            if (fallbackModeActive && Time.time - lastGestureFailureTime > gestureFailureTimeout * 2)
            {
                DeactivateFallbackMode();
            }
        }
        
        /// <summary>
        /// Activate fallback interaction mode
        /// </summary>
        public void ActivateFallbackMode()
        {
            if (!enableFallbackMethods || fallbackModeActive) return;
            
            fallbackModeActive = true;
            currentFallbackMethod = DetermineBestFallbackMethod();
            
            SetFallbackUIVisible(true);
            ShowFallbackInstructions();
            
            FallbackMethodActivated?.Invoke(currentFallbackMethod);
            
            if (feedbackManager != null)
            {
                feedbackManager.ShowWarningFeedback("Gesture recognition failed. Using alternative input.", Vector3.zero);
            }
            
            Debug.Log($"[FallbackInteractionManager] Fallback mode activated: {currentFallbackMethod}");
        }
        
        /// <summary>
        /// Deactivate fallback interaction mode
        /// </summary>
        public void DeactivateFallbackMode()
        {
            if (!fallbackModeActive) return;
            
            var previousMethod = currentFallbackMethod;
            fallbackModeActive = false;
            currentFallbackMethod = FallbackMethod.None;
            consecutiveGestureFailures = 0;
            
            SetFallbackUIVisible(false);
            
            FallbackMethodDeactivated?.Invoke(previousMethod);
            
            if (feedbackManager != null)
            {
                feedbackManager.ShowTextFeedback("Gesture recognition restored", FeedbackType.Success);
            }
            
            Debug.Log("[FallbackInteractionManager] Fallback mode deactivated");
        }
        
        /// <summary>
        /// Determine the best fallback method based on available options
        /// </summary>
        private FallbackMethod DetermineBestFallbackMethod()
        {
            // Priority order: Voice > Gaze > Controller > Keyboard
            if (enableVoiceFallback)
                return FallbackMethod.Voice;
            if (enableGazeFallback && mainCamera != null)
                return FallbackMethod.Gaze;
            if (enableControllerFallback)
                return FallbackMethod.Controller;
            if (enableKeyboardFallback)
                return FallbackMethod.Keyboard;
            
            return FallbackMethod.UI;
        }
        
        /// <summary>
        /// Update active fallback methods
        /// </summary>
        private void UpdateFallbackMethods()
        {
            switch (currentFallbackMethod)
            {
                case FallbackMethod.Voice:
                    UpdateVoiceFallback();
                    break;
                case FallbackMethod.Gaze:
                    UpdateGazeFallback();
                    break;
                case FallbackMethod.Controller:
                    UpdateControllerFallback();
                    break;
                case FallbackMethod.Keyboard:
                    UpdateKeyboardFallback();
                    break;
                case FallbackMethod.UI:
                    // UI fallback is always active when fallback mode is on
                    break;
            }
        }
        
        /// <summary>
        /// Update voice fallback method
        /// </summary>
        private void UpdateVoiceFallback()
        {
            // Simulate voice command recognition (in real implementation, use speech recognition)
            if (Input.GetKeyDown(KeyCode.V)) // Simulate voice command with V key
            {
                ExecuteFallbackAction("Voice command: Select");
            }
        }
        
        /// <summary>
        /// Update gaze fallback method
        /// </summary>
        private void UpdateGazeFallback()
        {
            if (mainCamera == null) return;
            
            // Cast ray from camera center
            Ray gazeRay = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            RaycastHit hit;
            
            if (Physics.Raycast(gazeRay, out hit, gazeRayDistance, gazeLayerMask))
            {
                // Check if we're looking at an interactable object
                var interactable = hit.collider.GetComponent<XRSimpleInteractable>();
                var button = hit.collider.GetComponent<Button>();
                
                if (interactable != null || button != null)
                {
                    if (gazeTarget != hit.collider.gameObject)
                    {
                        // New gaze target
                        gazeTarget = hit.collider.gameObject;
                        gazeStartTime = Time.time;
                        
                        if (gazeDwellCoroutine != null)
                            StopCoroutine(gazeDwellCoroutine);
                        
                        gazeDwellCoroutine = StartCoroutine(GazeDwellCoroutine());
                    }
                }
                else
                {
                    ResetGazeTarget();
                }
            }
            else
            {
                ResetGazeTarget();
            }
        }
        
        private IEnumerator GazeDwellCoroutine()
        {
            yield return new WaitForSeconds(gazeDwellTime);
            
            if (gazeTarget != null)
            {
                // Execute gaze selection
                var button = gazeTarget.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.Invoke();
                    ExecuteFallbackAction($"Gaze selection: {gazeTarget.name}");
                }
                
                ResetGazeTarget();
            }
        }
        
        private void ResetGazeTarget()
        {
            gazeTarget = null;
            gazeStartTime = 0f;
            
            if (gazeDwellCoroutine != null)
            {
                StopCoroutine(gazeDwellCoroutine);
                gazeDwellCoroutine = null;
            }
        }
        
        /// <summary>
        /// Update controller fallback method
        /// </summary>
        private void UpdateControllerFallback()
        {
            // Mouse input
            if (useMouseInput && Input.GetMouseButtonDown(0))
            {
                ExecuteFallbackAction("Mouse click");
            }
            
            // Gamepad input
            if (useGamepadInput)
            {
                if (Input.GetButtonDown("Fire1"))
                    ExecuteFallbackAction("Gamepad button A");
                if (Input.GetButtonDown("Fire2"))
                    ExecuteFallbackAction("Gamepad button B");
            }
        }
        
        /// <summary>
        /// Update keyboard fallback method
        /// </summary>
        private void UpdateKeyboardFallback()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                ExecuteFallbackAction("Keyboard: Select");
            if (Input.GetKeyDown(KeyCode.Escape))
                ExecuteFallbackAction("Keyboard: Back");
            if (Input.GetKeyDown(KeyCode.M))
                ExecuteFallbackAction("Keyboard: Menu");
            if (Input.GetKeyDown(KeyCode.H))
                ExecuteFallbackAction("Keyboard: Help");
            if (Input.GetKeyDown(KeyCode.R))
                ExecuteFallbackAction("Keyboard: Reset");
        }
        
        /// <summary>
        /// Execute fallback action
        /// </summary>
        private void ExecuteFallbackAction(string actionDescription)
        {
            FallbackActionExecuted?.Invoke(actionDescription);
            
            if (feedbackManager != null)
            {
                feedbackManager.ShowTextFeedback(actionDescription, FeedbackType.Success);
            }
            
            Debug.Log($"[FallbackInteractionManager] Executed: {actionDescription}");
        }
        
        /// <summary>
        /// Show fallback instructions to user
        /// </summary>
        private void ShowFallbackInstructions()
        {
            if (fallbackInstructionText == null) return;
            
            string instructions = GetFallbackInstructions(currentFallbackMethod);
            fallbackInstructionText.text = instructions;
        }
        
        private string GetFallbackInstructions(FallbackMethod method)
        {
            switch (method)
            {
                case FallbackMethod.Voice:
                    return "Say 'Select', 'Back', 'Menu', 'Help', or 'Reset'";
                case FallbackMethod.Gaze:
                    return "Look at an object for 2 seconds to select it";
                case FallbackMethod.Controller:
                    return "Use mouse click or gamepad buttons to interact";
                case FallbackMethod.Keyboard:
                    return "Use Space (Select), Esc (Back), M (Menu), H (Help), R (Reset)";
                case FallbackMethod.UI:
                    return "Use the buttons below to interact";
                default:
                    return "Alternative input methods available";
            }
        }
        
        /// <summary>
        /// Set fallback UI visibility
        /// </summary>
        private void SetFallbackUIVisible(bool visible)
        {
            if (fallbackCanvas != null)
                fallbackCanvas.gameObject.SetActive(visible);
        }
        
        // Event handlers
        private void OnHandTrackingLost()
        {
            RegisterGestureFailure();
        }
        
        private void OnHandTrackingRestored()
        {
            // Reset failure count when tracking is restored
            consecutiveGestureFailures = 0;
        }
        
        private void OnGestureConflictDetected(string conflictMessage)
        {
            RegisterGestureFailure();
        }
        
        private void OnFallbackButtonClicked(int buttonIndex)
        {
            string[] actions = { "Select", "Back", "Menu", "Help", "Reset" };
            if (buttonIndex >= 0 && buttonIndex < actions.Length)
            {
                ExecuteFallbackAction($"UI Button: {actions[buttonIndex]}");
            }
        }
        
        /// <summary>
        /// Register a gesture failure
        /// </summary>
        public void RegisterGestureFailure()
        {
            consecutiveGestureFailures++;
            lastGestureFailureTime = Time.time;
            
            Debug.Log($"[FallbackInteractionManager] Gesture failure registered: {consecutiveGestureFailures}/{maxGestureFailures}");
        }
        
        /// <summary>
        /// Reset gesture failure count
        /// </summary>
        public void ResetGestureFailures()
        {
            consecutiveGestureFailures = 0;
            lastGestureFailureTime = 0f;
        }
        
        /// <summary>
        /// Public API methods
        /// </summary>
        public bool IsFallbackModeActive()
        {
            return fallbackModeActive;
        }
        
        public FallbackMethod GetCurrentFallbackMethod()
        {
            return currentFallbackMethod;
        }
        
        public void SetFallbackMethod(FallbackMethod method)
        {
            if (fallbackModeActive)
            {
                currentFallbackMethod = method;
                ShowFallbackInstructions();
            }
        }
        
        public void ForceFallbackMode(bool activate)
        {
            if (activate)
                ActivateFallbackMode();
            else
                DeactivateFallbackMode();
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (gestureManager != null)
            {
                gestureManager.OnHandTrackingLost -= OnHandTrackingLost;
                gestureManager.OnHandTrackingRestored -= OnHandTrackingRestored;
            }
            
            if (uiInteractionManager != null)
            {
                uiInteractionManager.OnGestureConflictDetected.RemoveListener(OnGestureConflictDetected);
            }
            
            // Stop any running coroutines
            if (gazeDwellCoroutine != null)
            {
                StopCoroutine(gazeDwellCoroutine);
            }
        }
    }
    
    /// <summary>
    /// Available fallback methods
    /// </summary>
    public enum FallbackMethod
    {
        None,
        Voice,
        Gaze,
        Controller,
        Keyboard,
        UI
    }
}