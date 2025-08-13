using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace DaVinciEye.Input
{
    /// <summary>
    /// Simplified hand gesture manager using MRTK's pre-configured Input Actions
    /// Follows the task requirement: "Use MRTK's pre-configured Input Actions (no custom gesture code)"
    /// </summary>
    public class HandGestureManager : MonoBehaviour, IInputManager
    {
        [Header("MRTK Integration")]
        [SerializeField] private XRRayInteractor leftRayInteractor;
        [SerializeField] private XRRayInteractor rightRayInteractor;
        [SerializeField] private XRDirectInteractor leftDirectInteractor;
        [SerializeField] private XRDirectInteractor rightDirectInteractor;
        
        [Header("Input Configuration")]
        [SerializeField] private InputConfiguration inputConfig = new InputConfiguration();
        
        [Header("Hand Tracking Status")]
        [SerializeField] private bool isHandTrackingActive = false;
        [SerializeField] private Vector3 dominantHandPosition = Vector3.zero;
        [SerializeField] private Vector3 dominantHandForward = Vector3.forward;
        
        [Header("Unity Events - Connect to MRTK UI Components")]
        public UnityEvent<GestureData> OnGestureRecognizedEvent;
        public UnityEvent<Vector3> OnAirTapEvent;
        public UnityEvent<PinchData> OnPinchStartEvent;
        public UnityEvent<PinchData> OnPinchUpdateEvent;
        public UnityEvent<PinchData> OnPinchEndEvent;
        public UnityEvent OnHandTrackingLostEvent;
        public UnityEvent OnHandTrackingRestoredEvent;
        
        // Interface properties
        public bool IsHandTrackingActive => isHandTrackingActive;
        public Vector3 DominantHandPosition => dominantHandPosition;
        public Vector3 DominantHandForward => dominantHandForward;
        
        // Interface events
        public event Action<GestureData> OnGestureRecognized;
        public event Action<Vector3> OnAirTap;
        public event Action<PinchData> OnPinchStart;
        public event Action<PinchData> OnPinchUpdate;
        public event Action<PinchData> OnPinchEnd;
        public event Action OnHandTrackingLost;
        public event Action OnHandTrackingRestored;
        
        private InteractionMode currentMode = InteractionMode.Automatic;
        private bool gestureRecognitionEnabled = true;
        
        private void Start()
        {
            InitializeMRTKIntegration();
            EnableGestureRecognition();
        }
        
        /// <summary>
        /// Initialize MRTK integration using existing XR Interaction Toolkit components
        /// No custom gesture code needed - MRTK handles everything automatically
        /// </summary>
        private void InitializeMRTKIntegration()
        {
            // Find XR interactors if not assigned
            if (leftRayInteractor == null)
                leftRayInteractor = FindObjectOfType<XRRayInteractor>();
            
            if (rightRayInteractor == null)
            {
                var rayInteractors = FindObjectsOfType<XRRayInteractor>();
                if (rayInteractors.Length > 1)
                    rightRayInteractor = rayInteractors[1];
            }
            
            // Subscribe to XR Interaction Toolkit events (automatic gesture recognition)
            if (leftRayInteractor != null)
            {
                leftRayInteractor.selectEntered.AddListener(OnSelectEntered);
                leftRayInteractor.selectExited.AddListener(OnSelectExited);
                leftRayInteractor.hoverEntered.AddListener(OnHoverEntered);
                leftRayInteractor.hoverExited.AddListener(OnHoverExited);
            }
            
            if (rightRayInteractor != null)
            {
                rightRayInteractor.selectEntered.AddListener(OnSelectEntered);
                rightRayInteractor.selectExited.AddListener(OnSelectExited);
                rightRayInteractor.hoverEntered.AddListener(OnHoverEntered);
                rightRayInteractor.hoverExited.AddListener(OnHoverExited);
            }
            
            Debug.Log("[HandGestureManager] MRTK integration initialized - automatic gesture recognition active");
        }
        
        /// <summary>
        /// Handle XR select events (equivalent to air tap)
        /// </summary>
        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (!gestureRecognitionEnabled) return;
            
            Vector3 position = args.interactorObject.transform.position;
            
            // Create gesture data
            var gestureData = new GestureData(GestureType.AirTap, position, 
                args.interactorObject == rightRayInteractor);
            
            // Fire events
            OnGestureRecognized?.Invoke(gestureData);
            OnGestureRecognizedEvent?.Invoke(gestureData);
            
            OnAirTap?.Invoke(position);
            OnAirTapEvent?.Invoke(position);
            
            Debug.Log($"[HandGestureManager] Air tap detected at {position}");
        }
        
        private void OnSelectExited(SelectExitEventArgs args)
        {
            // Handle select exit if needed
        }
        
        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            // Handle hover enter for UI feedback
        }
        
        private void OnHoverExited(HoverExitEventArgs args)
        {
            // Handle hover exit for UI feedback
        }
        
        private void Update()
        {
            UpdateHandTracking();
        }
        
        /// <summary>
        /// Update hand tracking status using XR interactors
        /// </summary>
        private void UpdateHandTracking()
        {
            bool wasActive = isHandTrackingActive;
            
            // Check if any interactor is active (indicates hand tracking)
            isHandTrackingActive = (leftRayInteractor != null && leftRayInteractor.isActiveAndEnabled) ||
                                  (rightRayInteractor != null && rightRayInteractor.isActiveAndEnabled) ||
                                  (leftDirectInteractor != null && leftDirectInteractor.isActiveAndEnabled) ||
                                  (rightDirectInteractor != null && rightDirectInteractor.isActiveAndEnabled);
            
            // Update dominant hand position (prefer right hand)
            if (rightRayInteractor != null && rightRayInteractor.isActiveAndEnabled)
            {
                dominantHandPosition = rightRayInteractor.transform.position;
                dominantHandForward = rightRayInteractor.transform.forward;
            }
            else if (leftRayInteractor != null && leftRayInteractor.isActiveAndEnabled)
            {
                dominantHandPosition = leftRayInteractor.transform.position;
                dominantHandForward = leftRayInteractor.transform.forward;
            }
            
            // Fire tracking events
            if (wasActive != isHandTrackingActive)
            {
                if (isHandTrackingActive)
                {
                    OnHandTrackingRestored?.Invoke();
                    OnHandTrackingRestoredEvent?.Invoke();
                    Debug.Log("[HandGestureManager] Hand tracking restored");
                }
                else
                {
                    OnHandTrackingLost?.Invoke();
                    OnHandTrackingLostEvent?.Invoke();
                    Debug.Log("[HandGestureManager] Hand tracking lost");
                }
            }
        }
        
        // Interface implementation
        public void EnableGestureRecognition()
        {
            gestureRecognitionEnabled = true;
            Debug.Log("[HandGestureManager] Gesture recognition enabled");
        }
        
        public void DisableGestureRecognition()
        {
            gestureRecognitionEnabled = false;
            Debug.Log("[HandGestureManager] Gesture recognition disabled");
        }
        
        public void SetInteractionMode(InteractionMode mode)
        {
            currentMode = mode;
            Debug.Log($"[HandGestureManager] Interaction mode set to {mode}");
            
            // Enable/disable appropriate interactors based on mode
            switch (mode)
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
            }
        }
        
        private void EnableDirectInteractors(bool enable)
        {
            if (leftDirectInteractor != null)
                leftDirectInteractor.enabled = enable;
            if (rightDirectInteractor != null)
                rightDirectInteractor.enabled = enable;
        }
        
        private void EnableRayInteractors(bool enable)
        {
            if (leftRayInteractor != null)
                leftRayInteractor.enabled = enable;
            if (rightRayInteractor != null)
                rightRayInteractor.enabled = enable;
        }
        
        /// <summary>
        /// Simulate pinch gesture for testing (called by MRTK UI components)
        /// </summary>
        public void SimulatePinchStart(Vector3 position)
        {
            var pinchData = new PinchData
            {
                startPosition = position,
                currentPosition = position,
                isActive = true,
                pinchStrength = 1f,
                isRightHand = true
            };
            
            OnPinchStart?.Invoke(pinchData);
            OnPinchStartEvent?.Invoke(pinchData);
        }
        
        public void SimulatePinchUpdate(Vector3 position)
        {
            var pinchData = new PinchData
            {
                currentPosition = position,
                isActive = true,
                pinchStrength = 1f,
                isRightHand = true
            };
            pinchData.UpdatePosition(position);
            
            OnPinchUpdate?.Invoke(pinchData);
            OnPinchUpdateEvent?.Invoke(pinchData);
        }
        
        public void SimulatePinchEnd(Vector3 position)
        {
            var pinchData = new PinchData
            {
                currentPosition = position,
                isActive = false,
                pinchStrength = 0f,
                isRightHand = true
            };
            
            OnPinchEnd?.Invoke(pinchData);
            OnPinchEndEvent?.Invoke(pinchData);
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (leftRayInteractor != null)
            {
                leftRayInteractor.selectEntered.RemoveListener(OnSelectEntered);
                leftRayInteractor.selectExited.RemoveListener(OnSelectExited);
                leftRayInteractor.hoverEntered.RemoveListener(OnHoverEntered);
                leftRayInteractor.hoverExited.RemoveListener(OnHoverExited);
            }
            
            if (rightRayInteractor != null)
            {
                rightRayInteractor.selectEntered.RemoveListener(OnSelectEntered);
                rightRayInteractor.selectExited.RemoveListener(OnSelectExited);
                rightRayInteractor.hoverEntered.RemoveListener(OnHoverEntered);
                rightRayInteractor.hoverExited.RemoveListener(OnHoverExited);
            }
        }
    }
}