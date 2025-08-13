using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

namespace DaVinciEye.Input
{
    /// <summary>
    /// UI Interaction Manager using XR Interaction Toolkit
    /// Implements near and far interaction modes for different UI elements
    /// Task 8.2: Create UIInteractionManager using XR Interaction Toolkit
    /// </summary>
    public class UIInteractionManager : MonoBehaviour
    {
        [Header("Interaction Mode Configuration")]
        [SerializeField] private InteractionMode currentMode = InteractionMode.Automatic;
        [SerializeField] private float nearInteractionDistance = 0.1f;
        [SerializeField] private float farInteractionMaxDistance = 10f;
        [SerializeField] private float modeTransitionDelay = 0.5f;
        
        [Header("MRTK Component References")]
        [SerializeField] private Transform interactionModeManager;
        [SerializeField] private Transform proximityDetector;
        [SerializeField] private Transform nearInteractionModeDetector;
        [SerializeField] private Transform flatScreenModeDetector;
        
        [Header("XR Interactors")]
        [SerializeField] private List<XRRayInteractor> rayInteractors = new List<XRRayInteractor>();
        [SerializeField] private List<XRDirectInteractor> directInteractors = new List<XRDirectInteractor>();
        [SerializeField] private List<XRSocketInteractor> socketInteractors = new List<XRSocketInteractor>();
        
        [Header("UI Element Management")]
        [SerializeField] private List<UIInteractionElement> managedUIElements = new List<UIInteractionElement>();
        [SerializeField] private Canvas nearUICanvas;
        [SerializeField] private Canvas farUICanvas;
        
        [Header("Interaction Events")]
        public UnityEngine.Events.UnityEvent<InteractionMode> OnInteractionModeChanged;
        public UnityEngine.Events.UnityEvent<UIInteractionElement> OnUIElementHovered;
        public UnityEngine.Events.UnityEvent<UIInteractionElement> OnUIElementSelected;
        public UnityEngine.Events.UnityEvent<string> OnGestureConflictDetected;
        
        // Private fields
        private InteractionMode previousMode;
        private float lastModeChangeTime;
        private Dictionary<XRBaseInteractor, InteractionMode> interactorModes;
        private List<IXRSelectInteractable> currentlyHoveredElements;
        private HandGestureManager gestureManager;
        
        // Events
        public event Action<InteractionMode> InteractionModeChanged;
        public event Action<UIInteractionElement, InteractionMode> UIElementInteractionChanged;
        public event Action<string> GestureConflictDetected;
        
        private void Awake()
        {
            InitializeInteractionManager();
        }
        
        private void Start()
        {
            SetupInteractors();
            SetupUIElements();
            FindGestureManager();
            SetInteractionMode(currentMode);
        }
        
        private void Update()
        {
            UpdateInteractionMode();
            DetectGestureConflicts();
            UpdateUIResponsiveness();
        }
        
        /// <summary>
        /// Initialize the interaction manager with default settings
        /// </summary>
        private void InitializeInteractionManager()
        {
            interactorModes = new Dictionary<XRBaseInteractor, InteractionMode>();
            currentlyHoveredElements = new List<IXRSelectInteractable>();
            previousMode = currentMode;
            lastModeChangeTime = Time.time;
            
            Debug.Log("[UIInteractionManager] Initialized with mode: " + currentMode);
        }
        
        /// <summary>
        /// Setup XR interactors for near and far interaction
        /// Implements MRTK Components: InteractionModeManager, ProximityDetector
        /// </summary>
        private void SetupInteractors()
        {
            // Find all XR interactors in the scene if not assigned
            if (rayInteractors.Count == 0)
            {
                rayInteractors.AddRange(FindObjectsOfType<XRRayInteractor>());
            }
            
            if (directInteractors.Count == 0)
            {
                directInteractors.AddRange(FindObjectsOfType<XRDirectInteractor>());
            }
            
            if (socketInteractors.Count == 0)
            {
                socketInteractors.AddRange(FindObjectsOfType<XRSocketInteractor>());
            }
            
            // Subscribe to interactor events
            foreach (var rayInteractor in rayInteractors)
            {
                if (rayInteractor != null)
                {
                    rayInteractor.hoverEntered.AddListener(OnRayHoverEntered);
                    rayInteractor.hoverExited.AddListener(OnRayHoverExited);
                    rayInteractor.selectEntered.AddListener(OnRaySelectEntered);
                    rayInteractor.selectExited.AddListener(OnRaySelectExited);
                    interactorModes[rayInteractor] = InteractionMode.Far;
                }
            }
            
            foreach (var directInteractor in directInteractors)
            {
                if (directInteractor != null)
                {
                    directInteractor.hoverEntered.AddListener(OnDirectHoverEntered);
                    directInteractor.hoverExited.AddListener(OnDirectHoverExited);
                    directInteractor.selectEntered.AddListener(OnDirectSelectEntered);
                    directInteractor.selectExited.AddListener(OnDirectSelectExited);
                    interactorModes[directInteractor] = InteractionMode.Near;
                }
            }
            
            Debug.Log($"[UIInteractionManager] Setup complete - Ray: {rayInteractors.Count}, Direct: {directInteractors.Count}, Socket: {socketInteractors.Count}");
        }
        
        /// <summary>
        /// Setup UI elements for interaction management
        /// Implements MRTK UI Elements: CanvasSlider.prefab, CanvasButtonBar.prefab, NearMenuBase.prefab
        /// </summary>
        private void SetupUIElements()
        {
            // Find UI canvases if not assigned
            if (nearUICanvas == null)
            {
                var canvases = FindObjectsOfType<Canvas>();
                foreach (var canvas in canvases)
                {
                    if (canvas.name.ToLower().Contains("near"))
                    {
                        nearUICanvas = canvas;
                        break;
                    }
                }
            }
            
            if (farUICanvas == null)
            {
                var canvases = FindObjectsOfType<Canvas>();
                foreach (var canvas in canvases)
                {
                    if (canvas.name.ToLower().Contains("far") || canvas.renderMode == RenderMode.WorldSpace)
                    {
                        farUICanvas = canvas;
                        break;
                    }
                }
            }
            
            // Auto-discover UI elements if not manually assigned
            if (managedUIElements.Count == 0)
            {
                DiscoverUIElements();
            }
            
            // Configure UI elements for appropriate interaction modes
            foreach (var uiElement in managedUIElements)
            {
                ConfigureUIElementInteraction(uiElement);
            }
            
            Debug.Log($"[UIInteractionManager] Managing {managedUIElements.Count} UI elements");
        }
        
        /// <summary>
        /// Automatically discover UI elements in the scene
        /// </summary>
        private void DiscoverUIElements()
        {
            // Find buttons
            var buttons = FindObjectsOfType<Button>();
            foreach (var button in buttons)
            {
                var uiElement = new UIInteractionElement
                {
                    gameObject = button.gameObject,
                    elementType = UIElementType.Button,
                    preferredMode = DeterminePreferredInteractionMode(button.transform),
                    component = button
                };
                managedUIElements.Add(uiElement);
            }
            
            // Find sliders
            var sliders = FindObjectsOfType<Slider>();
            foreach (var slider in sliders)
            {
                var uiElement = new UIInteractionElement
                {
                    gameObject = slider.gameObject,
                    elementType = UIElementType.Slider,
                    preferredMode = InteractionMode.Near, // Sliders work better with direct interaction
                    component = slider
                };
                managedUIElements.Add(uiElement);
            }
            
            // Find toggles
            var toggles = FindObjectsOfType<Toggle>();
            foreach (var toggle in toggles)
            {
                var uiElement = new UIInteractionElement
                {
                    gameObject = toggle.gameObject,
                    elementType = UIElementType.Toggle,
                    preferredMode = DeterminePreferredInteractionMode(toggle.transform),
                    component = toggle
                };
                managedUIElements.Add(uiElement);
            }
        }
        
        /// <summary>
        /// Determine preferred interaction mode based on UI element position and context
        /// </summary>
        private InteractionMode DeterminePreferredInteractionMode(Transform elementTransform)
        {
            // Check distance from user (camera)
            var camera = Camera.main;
            if (camera != null)
            {
                float distance = Vector3.Distance(camera.transform.position, elementTransform.position);
                if (distance <= nearInteractionDistance)
                {
                    return InteractionMode.Near;
                }
                else if (distance <= farInteractionMaxDistance)
                {
                    return InteractionMode.Far;
                }
            }
            
            // Check if element is part of a near or far UI canvas
            var canvas = elementTransform.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                if (canvas == nearUICanvas)
                    return InteractionMode.Near;
                if (canvas == farUICanvas)
                    return InteractionMode.Far;
            }
            
            return InteractionMode.Automatic;
        }
        
        /// <summary>
        /// Configure UI element for appropriate interaction mode
        /// </summary>
        private void ConfigureUIElementInteraction(UIInteractionElement uiElement)
        {
            if (uiElement.gameObject == null) return;
            
            // Add XR interaction components if not present
            var interactable = uiElement.gameObject.GetComponent<XRSimpleInteractable>();
            if (interactable == null)
            {
                interactable = uiElement.gameObject.AddComponent<XRSimpleInteractable>();
            }
            
            // Configure interaction layers based on preferred mode
            switch (uiElement.preferredMode)
            {
                case InteractionMode.Near:
                    // Configure for direct interaction
                    interactable.interactionLayers = InteractionLayerMask.GetMask("Direct");
                    break;
                case InteractionMode.Far:
                    // Configure for ray interaction
                    interactable.interactionLayers = InteractionLayerMask.GetMask("Ray");
                    break;
                case InteractionMode.Automatic:
                    // Configure for both
                    interactable.interactionLayers = InteractionLayerMask.GetMask("Direct", "Ray");
                    break;
            }
            
            // Subscribe to interaction events
            interactable.hoverEntered.AddListener((args) => OnUIElementHovered?.Invoke(uiElement));
            interactable.selectEntered.AddListener((args) => OnUIElementSelected?.Invoke(uiElement));
        }
        
        private void FindGestureManager()
        {
            gestureManager = FindObjectOfType<HandGestureManager>();
            if (gestureManager != null)
            {
                gestureManager.OnGestureRecognized += OnGestureRecognized;
            }
        }
        
        /// <summary>
        /// Update interaction mode based on context
        /// Implements NearInteractionModeDetector, FlatScreenModeDetector for context-aware UI
        /// </summary>
        private void UpdateInteractionMode()
        {
            if (currentMode != InteractionMode.Automatic) return;
            
            InteractionMode detectedMode = DetectContextualInteractionMode();
            
            if (detectedMode != previousMode && Time.time - lastModeChangeTime > modeTransitionDelay)
            {
                SetInteractionMode(detectedMode);
            }
        }
        
        /// <summary>
        /// Detect contextual interaction mode based on hand position and UI proximity
        /// </summary>
        private InteractionMode DetectContextualInteractionMode()
        {
            if (gestureManager == null) return currentMode;
            
            Vector3 handPosition = gestureManager.DominantHandPosition;
            
            // Check proximity to near UI elements
            foreach (var uiElement in managedUIElements)
            {
                if (uiElement.gameObject != null)
                {
                    float distance = Vector3.Distance(handPosition, uiElement.gameObject.transform.position);
                    if (distance <= nearInteractionDistance)
                    {
                        return InteractionMode.Near;
                    }
                }
            }
            
            // Default to far interaction if no near elements detected
            return InteractionMode.Far;
        }
        
        /// <summary>
        /// Set interaction mode and update all interactors
        /// </summary>
        public void SetInteractionMode(InteractionMode mode)
        {
            if (currentMode == mode) return;
            
            previousMode = currentMode;
            currentMode = mode;
            lastModeChangeTime = Time.time;
            
            UpdateInteractorStates();
            
            OnInteractionModeChanged?.Invoke(mode);
            InteractionModeChanged?.Invoke(mode);
            
            Debug.Log($"[UIInteractionManager] Interaction mode changed to: {mode}");
        }
        
        /// <summary>
        /// Update interactor states based on current mode
        /// </summary>
        private void UpdateInteractorStates()
        {
            switch (currentMode)
            {
                case InteractionMode.Near:
                    EnableDirectInteractors(true);
                    EnableRayInteractors(false);
                    break;
                case InteractionMode.Far:
                    EnableDirectInteractors(false);
                    EnableRayInteractors(true);
                    break;
                case InteractionMode.Automatic:
                    EnableDirectInteractors(true);
                    EnableRayInteractors(true);
                    break;
                case InteractionMode.Voice:
                    EnableDirectInteractors(false);
                    EnableRayInteractors(false);
                    break;
            }
        }
        
        private void EnableDirectInteractors(bool enable)
        {
            foreach (var interactor in directInteractors)
            {
                if (interactor != null)
                    interactor.enabled = enable;
            }
        }
        
        private void EnableRayInteractors(bool enable)
        {
            foreach (var interactor in rayInteractors)
            {
                if (interactor != null)
                    interactor.enabled = enable;
            }
        }
        
        /// <summary>
        /// Detect and handle gesture conflicts
        /// </summary>
        private void DetectGestureConflicts()
        {
            // Check for simultaneous interactions that might conflict
            int activeDirectInteractions = 0;
            int activeRayInteractions = 0;
            
            foreach (var interactor in directInteractors)
            {
                if (interactor != null && interactor.hasSelection)
                    activeDirectInteractions++;
            }
            
            foreach (var interactor in rayInteractors)
            {
                if (interactor != null && interactor.hasSelection)
                    activeRayInteractions++;
            }
            
            // Detect conflicts
            if (activeDirectInteractions > 0 && activeRayInteractions > 0)
            {
                string conflictMessage = $"Gesture conflict: {activeDirectInteractions} direct + {activeRayInteractions} ray interactions";
                OnGestureConflictDetected?.Invoke(conflictMessage);
                GestureConflictDetected?.Invoke(conflictMessage);
                
                // Resolve conflict by prioritizing direct interaction
                if (currentMode == InteractionMode.Automatic)
                {
                    SetInteractionMode(InteractionMode.Near);
                }
            }
        }
        
        /// <summary>
        /// Update UI responsiveness based on interaction performance
        /// </summary>
        private void UpdateUIResponsiveness()
        {
            // Monitor interaction performance and adjust responsiveness
            foreach (var uiElement in managedUIElements)
            {
                if (uiElement.gameObject != null)
                {
                    UpdateUIElementResponsiveness(uiElement);
                }
            }
        }
        
        private void UpdateUIElementResponsiveness(UIInteractionElement uiElement)
        {
            // Adjust UI element responsiveness based on interaction mode and performance
            var interactable = uiElement.gameObject.GetComponent<XRSimpleInteractable>();
            if (interactable != null)
            {
                // Adjust hover and selection thresholds based on current mode
                switch (currentMode)
                {
                    case InteractionMode.Near:
                        // Tighter thresholds for direct interaction
                        break;
                    case InteractionMode.Far:
                        // Looser thresholds for ray interaction
                        break;
                }
            }
        }
        
        // Event handlers for ray interactions
        private void OnRayHoverEntered(HoverEnterEventArgs args)
        {
            var uiElement = FindUIElementByGameObject(args.interactableObject.transform.gameObject);
            if (uiElement != null)
            {
                UIElementInteractionChanged?.Invoke(uiElement, InteractionMode.Far);
            }
        }
        
        private void OnRayHoverExited(HoverExitEventArgs args) { }
        
        private void OnRaySelectEntered(SelectEnterEventArgs args)
        {
            var uiElement = FindUIElementByGameObject(args.interactableObject.transform.gameObject);
            if (uiElement != null)
            {
                Debug.Log($"[UIInteractionManager] Ray selection: {uiElement.elementType}");
            }
        }
        
        private void OnRaySelectExited(SelectExitEventArgs args) { }
        
        // Event handlers for direct interactions
        private void OnDirectHoverEntered(HoverEnterEventArgs args)
        {
            var uiElement = FindUIElementByGameObject(args.interactableObject.transform.gameObject);
            if (uiElement != null)
            {
                UIElementInteractionChanged?.Invoke(uiElement, InteractionMode.Near);
            }
        }
        
        private void OnDirectHoverExited(HoverExitEventArgs args) { }
        
        private void OnDirectSelectEntered(SelectEnterEventArgs args)
        {
            var uiElement = FindUIElementByGameObject(args.interactableObject.transform.gameObject);
            if (uiElement != null)
            {
                Debug.Log($"[UIInteractionManager] Direct selection: {uiElement.elementType}");
            }
        }
        
        private void OnDirectSelectExited(SelectExitEventArgs args) { }
        
        private void OnGestureRecognized(GestureData gestureData)
        {
            // Handle gesture recognition in context of UI interactions
            Debug.Log($"[UIInteractionManager] Gesture recognized: {gestureData.type} in {currentMode} mode");
        }
        
        private UIInteractionElement FindUIElementByGameObject(GameObject go)
        {
            return managedUIElements.Find(element => element.gameObject == go);
        }
        
        /// <summary>
        /// Public API methods
        /// </summary>
        public void ForceInteractionMode(InteractionMode mode)
        {
            SetInteractionMode(mode);
        }
        
        public InteractionMode GetCurrentInteractionMode()
        {
            return currentMode;
        }
        
        public List<UIInteractionElement> GetManagedUIElements()
        {
            return new List<UIInteractionElement>(managedUIElements);
        }
        
        public void AddUIElement(UIInteractionElement element)
        {
            if (!managedUIElements.Contains(element))
            {
                managedUIElements.Add(element);
                ConfigureUIElementInteraction(element);
            }
        }
        
        public void RemoveUIElement(UIInteractionElement element)
        {
            managedUIElements.Remove(element);
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            foreach (var rayInteractor in rayInteractors)
            {
                if (rayInteractor != null)
                {
                    rayInteractor.hoverEntered.RemoveListener(OnRayHoverEntered);
                    rayInteractor.hoverExited.RemoveListener(OnRayHoverExited);
                    rayInteractor.selectEntered.RemoveListener(OnRaySelectEntered);
                    rayInteractor.selectExited.RemoveListener(OnRaySelectExited);
                }
            }
            
            foreach (var directInteractor in directInteractors)
            {
                if (directInteractor != null)
                {
                    directInteractor.hoverEntered.RemoveListener(OnDirectHoverEntered);
                    directInteractor.hoverExited.RemoveListener(OnDirectHoverExited);
                    directInteractor.selectEntered.RemoveListener(OnDirectSelectEntered);
                    directInteractor.selectExited.RemoveListener(OnDirectSelectExited);
                }
            }
            
            if (gestureManager != null)
            {
                gestureManager.OnGestureRecognized -= OnGestureRecognized;
            }
        }
    }
    
    /// <summary>
    /// Data structure for UI interaction elements
    /// </summary>
    [System.Serializable]
    public class UIInteractionElement
    {
        public GameObject gameObject;
        public UIElementType elementType;
        public InteractionMode preferredMode;
        public Component component;
        public bool isInteractable = true;
        public float responsiveness = 1f;
    }
    
    /// <summary>
    /// Types of UI elements that can be managed
    /// </summary>
    public enum UIElementType
    {
        Button,
        Slider,
        Toggle,
        Menu,
        Panel,
        Custom
    }
}