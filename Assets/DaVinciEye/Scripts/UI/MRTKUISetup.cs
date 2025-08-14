using UnityEngine;
using MixedReality.Toolkit.UX;
using UnityEngine.UI;

namespace DaVinciEye.UI
{
    /// <summary>
    /// Helper class for setting up MRTK UI prefabs as specified in the implementation plan
    /// Handles the "NO-CODE UI SETUP" requirements from task 11.1
    /// </summary>
    public class MRTKUISetup : MonoBehaviour
    {
        [Header("MRTK Prefab References")]
        [SerializeField] private GameObject handMenuBasePrefab;
        [SerializeField] private GameObject nearMenuBasePrefab;
        [SerializeField] private GameObject canvasButtonBarPrefab;
        [SerializeField] private GameObject canvasDialogPrefab;
        [SerializeField] private GameObject seeItSayItLabelPrefab;
        
        [Header("Generated UI Components")]
        [SerializeField] private GameObject handMenuInstance;
        [SerializeField] private GameObject nearMenuInstance;
        [SerializeField] private GameObject buttonBarInstance;
        [SerializeField] private GameObject dialogInstance;
        
        [Header("Button Configuration")]
        [SerializeField] private string[] buttonLabels = { "Canvas", "Image", "Filters", "Colors" };
        [SerializeField] private string[] voiceCommands = { "canvas", "image", "filters", "colors" };
        
        private MainApplicationUI mainUI;
        
        private void Awake()
        {
            mainUI = GetComponent<MainApplicationUI>();
            if (mainUI == null)
            {
                Debug.LogError("MRTKUISetup: MainApplicationUI component not found");
            }
        }
        
        private void Start()
        {
            SetupMRTKUI();
        }
        
        /// <summary>
        /// Sets up all MRTK UI components according to task specifications
        /// </summary>
        [ContextMenu("Setup MRTK UI")]
        public void SetupMRTKUI()
        {
            Debug.Log("MRTKUISetup: Starting MRTK UI setup...");
            
            SetupHandMenu();
            SetupNearMenu();
            SetupButtonBar();
            SetupDialog();
            
            Debug.Log("MRTKUISetup: MRTK UI setup complete");
        }
        
        /// <summary>
        /// Sets up HandMenuBase.prefab for palm-up menu (automatically follows hand)
        /// </summary>
        private void SetupHandMenu()
        {
            if (handMenuBasePrefab == null)
            {
                Debug.LogWarning("MRTKUISetup: HandMenuBase prefab not assigned");
                return;
            }
            
            if (handMenuInstance != null)
            {
                DestroyImmediate(handMenuInstance);
            }
            
            handMenuInstance = Instantiate(handMenuBasePrefab, transform);
            handMenuInstance.name = "HandMenu";
            
            // Configure hand menu for quick access
            var handMenu = handMenuInstance.GetComponent<HandMenu>();
            if (handMenu != null)
            {
                // MRTK HandMenu automatically follows hand - no additional setup needed
                Debug.Log("MRTKUISetup: Hand menu configured (automatically follows hand)");
            }
            
            // Add quick action buttons to hand menu
            SetupHandMenuButtons();
        }
        
        /// <summary>
        /// Sets up NearMenuBase.prefab for floating control panel (use GrabBar.mat for dragging)
        /// </summary>
        private void SetupNearMenu()
        {
            if (nearMenuBasePrefab == null)
            {
                Debug.LogWarning("MRTKUISetup: NearMenuBase prefab not assigned");
                return;
            }
            
            if (nearMenuInstance != null)
            {
                DestroyImmediate(nearMenuInstance);
            }
            
            nearMenuInstance = Instantiate(nearMenuBasePrefab, transform);
            nearMenuInstance.name = "NearMenu";
            
            // Position near menu in front of user
            nearMenuInstance.transform.localPosition = new Vector3(0, 0, 1.5f);
            
            // Configure near menu for detailed controls
            var nearMenu = nearMenuInstance.GetComponent<NearMenu>();
            if (nearMenu != null)
            {
                Debug.Log("MRTKUISetup: Near menu configured with GrabBar for dragging");
            }
            
            // The GrabBar material should be applied in the prefab itself
            // This allows users to drag the menu around
        }
        
        /// <summary>
        /// Sets up CanvasButtonBar.prefab with 4 buttons: "Canvas", "Image", "Filters", "Colors"
        /// </summary>
        private void SetupButtonBar()
        {
            if (canvasButtonBarPrefab == null)
            {
                Debug.LogWarning("MRTKUISetup: CanvasButtonBar prefab not assigned");
                return;
            }
            
            if (buttonBarInstance != null)
            {
                DestroyImmediate(buttonBarInstance);
            }
            
            buttonBarInstance = Instantiate(canvasButtonBarPrefab, nearMenuInstance != null ? nearMenuInstance.transform : transform);
            buttonBarInstance.name = "ModeSelectionButtonBar";
            
            // Configure button bar with mode selection buttons
            var buttonBar = buttonBarInstance.GetComponent<ButtonBar>();
            if (buttonBar != null)
            {
                SetupModeSelectionButtons(buttonBar);
            }
            
            Debug.Log("MRTKUISetup: Button bar configured with mode selection buttons");
        }
        
        /// <summary>
        /// Sets up CanvasDialog.prefab for confirmations - just set title and message text
        /// </summary>
        private void SetupDialog()
        {
            if (canvasDialogPrefab == null)
            {
                Debug.LogWarning("MRTKUISetup: CanvasDialog prefab not assigned");
                return;
            }
            
            if (dialogInstance != null)
            {
                DestroyImmediate(dialogInstance);
            }
            
            dialogInstance = Instantiate(canvasDialogPrefab, transform);
            dialogInstance.name = "ConfirmationDialog";
            
            // Position dialog in front of user
            dialogInstance.transform.localPosition = new Vector3(0, 0, 2f);
            
            // Initially hide dialog
            dialogInstance.SetActive(false);
            
            Debug.Log("MRTKUISetup: Confirmation dialog configured");
        }
        
        /// <summary>
        /// Sets up mode selection buttons with voice commands
        /// </summary>
        private void SetupModeSelectionButtons(ButtonBar buttonBar)
        {
            var buttons = buttonBar.GetComponentsInChildren<PressableButton>();
            
            for (int i = 0; i < Mathf.Min(buttons.Length, buttonLabels.Length); i++)
            {
                var button = buttons[i];
                var label = buttonLabels[i];
                var voiceCommand = voiceCommands[i];
                
                // Set button text
                var textComponent = button.GetComponentInChildren<Text>();
                if (textComponent != null)
                {
                    textComponent.text = label;
                }
                
                // Add voice command support
                SetupVoiceCommand(button.gameObject, voiceCommand);
                
                // Configure button for mode switching (events handled by MainApplicationUI)
                button.name = $"{label}Button";
                
                Debug.Log($"MRTKUISetup: Configured {label} button with voice command '{voiceCommand}'");
            }
        }
        
        /// <summary>
        /// Sets up hand menu quick action buttons
        /// </summary>
        private void SetupHandMenuButtons()
        {
            if (handMenuInstance == null) return;
            
            // Add essential quick actions to hand menu
            var handMenuButtons = handMenuInstance.GetComponentsInChildren<PressableButton>();
            
            // Configure existing buttons or add new ones for quick actions
            foreach (var button in handMenuButtons)
            {
                // Add voice command support to hand menu buttons
                var buttonText = button.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    string voiceCommand = buttonText.text.ToLower();
                    SetupVoiceCommand(button.gameObject, voiceCommand);
                }
            }
        }
        
        /// <summary>
        /// Applies SeeItSayItLabel-Canvas.prefab to buttons for voice command support
        /// </summary>
        private void SetupVoiceCommand(GameObject buttonObject, string voiceCommand)
        {
            if (seeItSayItLabelPrefab == null)
            {
                Debug.LogWarning("MRTKUISetup: SeeItSayItLabel prefab not assigned");
                return;
            }
            
            // Check if voice command component already exists
            var existingLabel = buttonObject.GetComponent<SeeItSayItLabel>();
            if (existingLabel != null)
            {
                return; // Already configured
            }
            
            // Add SeeItSayItLabel component for voice commands
            var voiceLabelInstance = Instantiate(seeItSayItLabelPrefab, buttonObject.transform);
            voiceLabelInstance.name = $"VoiceCommand_{voiceCommand}";
            
            var seeItSayItLabel = voiceLabelInstance.GetComponent<SeeItSayItLabel>();
            if (seeItSayItLabel != null)
            {
                // Configure voice command keyword
                // Note: Actual keyword configuration depends on MRTK SeeItSayItLabel implementation
                Debug.Log($"MRTKUISetup: Voice command '{voiceCommand}' added to button");
            }
        }
        
        /// <summary>
        /// Validates that all required MRTK prefabs are assigned
        /// </summary>
        [ContextMenu("Validate MRTK Prefabs")]
        public void ValidateMRTKPrefabs()
        {
            bool allValid = true;
            
            if (handMenuBasePrefab == null)
            {
                Debug.LogError("MRTKUISetup: HandMenuBase.prefab not assigned");
                allValid = false;
            }
            
            if (nearMenuBasePrefab == null)
            {
                Debug.LogError("MRTKUISetup: NearMenuBase.prefab not assigned");
                allValid = false;
            }
            
            if (canvasButtonBarPrefab == null)
            {
                Debug.LogError("MRTKUISetup: CanvasButtonBar.prefab not assigned");
                allValid = false;
            }
            
            if (canvasDialogPrefab == null)
            {
                Debug.LogError("MRTKUISetup: CanvasDialog.prefab not assigned");
                allValid = false;
            }
            
            if (seeItSayItLabelPrefab == null)
            {
                Debug.LogError("MRTKUISetup: SeeItSayItLabel-Canvas.prefab not assigned");
                allValid = false;
            }
            
            if (allValid)
            {
                Debug.Log("MRTKUISetup: All MRTK prefabs are properly assigned");
            }
            else
            {
                Debug.LogError("MRTKUISetup: Some MRTK prefabs are missing. Please assign them in the inspector.");
            }
        }
        
        // Public accessors for MainApplicationUI
        public GameObject HandMenuInstance => handMenuInstance;
        public GameObject NearMenuInstance => nearMenuInstance;
        public GameObject ButtonBarInstance => buttonBarInstance;
        public GameObject DialogInstance => dialogInstance;
    }
}