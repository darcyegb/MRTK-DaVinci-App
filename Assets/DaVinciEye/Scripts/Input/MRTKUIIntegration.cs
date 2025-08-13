using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DaVinciEye.Input
{
    /// <summary>
    /// Integration component for MRTK UI elements with automatic gesture recognition
    /// Implements task requirement: "Use UnityEvent callbacks on MRTK components (no custom gesture code)"
    /// </summary>
    public class MRTKUIIntegration : MonoBehaviour
    {
        [Header("MRTK UI Component References")]
        [SerializeField] private Button[] pressableButtons;
        [SerializeField] private Slider[] pinchSliders;
        [SerializeField] private Toggle[] toggles;
        
        [Header("Voice Command Integration")]
        [SerializeField] private bool enableVoiceCommands = true;
        [SerializeField] private string[] voiceKeywords = { "Canvas", "Image", "Filters", "Colors", "Reset" };
        
        [Header("UI Events - Connect to App Systems")]
        public UnityEvent<string> OnButtonPressed;
        public UnityEvent<float> OnSliderValueChanged;
        public UnityEvent<bool> OnToggleChanged;
        public UnityEvent<string> OnVoiceCommand;
        
        private HandGestureManager gestureManager;
        
        private void Start()
        {
            InitializeUIComponents();
            SetupVoiceCommands();
            FindGestureManager();
        }
        
        /// <summary>
        /// Initialize MRTK UI components with automatic event binding
        /// No custom gesture code needed - MRTK handles all interactions
        /// </summary>
        private void InitializeUIComponents()
        {
            // Auto-find UI components if not assigned
            if (pressableButtons == null || pressableButtons.Length == 0)
                pressableButtons = FindObjectsOfType<Button>();
            
            if (pinchSliders == null || pinchSliders.Length == 0)
                pinchSliders = FindObjectsOfType<Slider>();
            
            if (toggles == null || toggles.Length == 0)
                toggles = FindObjectsOfType<Toggle>();
            
            // Connect button events (automatic gesture recognition)
            foreach (var button in pressableButtons)
            {
                if (button != null)
                {
                    // Use Unity's built-in onClick event (works with MRTK automatically)
                    button.onClick.AddListener(() => HandleButtonClick(button.name));
                    
                    // Add SeeItSayItLabel component for voice commands if not present
                    AddVoiceCommandSupport(button.gameObject, button.name);
                }
            }
            
            // Connect slider events (automatic pinch gesture recognition)
            foreach (var slider in pinchSliders)
            {
                if (slider != null)
                {
                    // Use Unity's built-in onValueChanged event (works with MRTK automatically)
                    slider.onValueChanged.AddListener(HandleSliderValueChanged);
                    
                    // Add voice command support
                    AddVoiceCommandSupport(slider.gameObject, $"Adjust {slider.name}");
                }
            }
            
            // Connect toggle events
            foreach (var toggle in toggles)
            {
                if (toggle != null)
                {
                    toggle.onValueChanged.AddListener(HandleToggleChanged);
                    AddVoiceCommandSupport(toggle.gameObject, toggle.name);
                }
            }
            
            Debug.Log($"[MRTKUIIntegration] Initialized {pressableButtons.Length} buttons, " +
                     $"{pinchSliders.Length} sliders, {toggles.Length} toggles with automatic gesture recognition");
        }
        
        /// <summary>
        /// Add voice command support using SeeItSayItLabel (MRTK component)
        /// Implements task requirement: "Enable voice commands by adding SeeItSayItLabel to UI elements"
        /// </summary>
        private void AddVoiceCommandSupport(GameObject uiElement, string commandText)
        {
            if (!enableVoiceCommands) return;
            
            // Check if SeeItSayItLabel already exists
            var existingLabel = uiElement.GetComponent<SeeItSayItLabelComponent>();
            if (existingLabel != null) return;
            
            // Add SeeItSayItLabel component (this would be the actual MRTK component)
            // For now, we'll create a placeholder that simulates the functionality
            var voiceLabel = uiElement.AddComponent<SeeItSayItLabelComponent>();
            voiceLabel.Initialize(commandText, () => HandleVoiceCommand(commandText));
            
            Debug.Log($"[MRTKUIIntegration] Added voice command '{commandText}' to {uiElement.name}");
        }
        
        /// <summary>
        /// Setup voice command recognition using MRTK's built-in system
        /// </summary>
        private void SetupVoiceCommands()
        {
            if (!enableVoiceCommands) return;
            
            // In a real MRTK implementation, this would use MRTK's SpeechInputHandler
            // For now, we'll simulate the voice command setup
            Debug.Log("[MRTKUIIntegration] Voice commands enabled for keywords: " + 
                     string.Join(", ", voiceKeywords));
        }
        
        private void FindGestureManager()
        {
            gestureManager = FindObjectOfType<HandGestureManager>();
            if (gestureManager == null)
            {
                Debug.LogWarning("[MRTKUIIntegration] HandGestureManager not found in scene");
            }
        }
        
        // Event handlers for UI interactions
        private void HandleButtonClick(string buttonName)
        {
            Debug.Log($"[MRTKUIIntegration] Button clicked: {buttonName}");
            OnButtonPressed?.Invoke(buttonName);
            
            // Route to appropriate app system based on button name
            switch (buttonName.ToLower())
            {
                case "canvas":
                case "canvasbutton":
                    HandleCanvasMode();
                    break;
                case "image":
                case "imagebutton":
                    HandleImageMode();
                    break;
                case "filters":
                case "filtersbutton":
                    HandleFiltersMode();
                    break;
                case "colors":
                case "colorsbutton":
                    HandleColorsMode();
                    break;
                case "reset":
                case "resetbutton":
                    HandleReset();
                    break;
            }
        }
        
        private void HandleSliderValueChanged(float value)
        {
            Debug.Log($"[MRTKUIIntegration] Slider value changed: {value}");
            OnSliderValueChanged?.Invoke(value);
        }
        
        private void HandleToggleChanged(bool isOn)
        {
            Debug.Log($"[MRTKUIIntegration] Toggle changed: {isOn}");
            OnToggleChanged?.Invoke(isOn);
        }
        
        private void HandleVoiceCommand(string command)
        {
            Debug.Log($"[MRTKUIIntegration] Voice command: {command}");
            OnVoiceCommand?.Invoke(command);
            
            // Process voice commands
            switch (command.ToLower())
            {
                case "canvas":
                    HandleCanvasMode();
                    break;
                case "image":
                    HandleImageMode();
                    break;
                case "filters":
                    HandleFiltersMode();
                    break;
                case "colors":
                    HandleColorsMode();
                    break;
                case "reset":
                    HandleReset();
                    break;
            }
        }
        
        // Mode switching methods (connect to app systems)
        private void HandleCanvasMode()
        {
            Debug.Log("[MRTKUIIntegration] Switching to Canvas mode");
            // This would connect to the CanvasDefinitionManager
        }
        
        private void HandleImageMode()
        {
            Debug.Log("[MRTKUIIntegration] Switching to Image mode");
            // This would connect to the ImageOverlayManager
        }
        
        private void HandleFiltersMode()
        {
            Debug.Log("[MRTKUIIntegration] Switching to Filters mode");
            // This would connect to the FilterManager
        }
        
        private void HandleColorsMode()
        {
            Debug.Log("[MRTKUIIntegration] Switching to Colors mode");
            // This would connect to the ColorAnalyzer
        }
        
        private void HandleReset()
        {
            Debug.Log("[MRTKUIIntegration] Resetting application");
            // This would reset all systems to default state
        }
        
        /// <summary>
        /// Public method to programmatically trigger button clicks (for testing)
        /// </summary>
        public void TriggerButton(string buttonName)
        {
            HandleButtonClick(buttonName);
        }
        
        /// <summary>
        /// Public method to programmatically trigger voice commands (for testing)
        /// </summary>
        public void TriggerVoiceCommand(string command)
        {
            HandleVoiceCommand(command);
        }
    }
    
    /// <summary>
    /// Placeholder component that simulates MRTK's SeeItSayItLabel functionality
    /// In a real implementation, this would be replaced with the actual MRTK component
    /// </summary>
    public class SeeItSayItLabelComponent : MonoBehaviour
    {
        private string commandText;
        private System.Action onCommandCallback;
        
        public void Initialize(string text, System.Action callback)
        {
            commandText = text;
            onCommandCallback = callback;
        }
        
        // Simulate voice command recognition (in real MRTK, this would be automatic)
        private void Update()
        {
            // This is just a placeholder - real MRTK handles voice recognition automatically
            if (Input.GetKeyDown(KeyCode.V)) // Simulate voice command with V key for testing
            {
                onCommandCallback?.Invoke();
            }
        }
    }
}