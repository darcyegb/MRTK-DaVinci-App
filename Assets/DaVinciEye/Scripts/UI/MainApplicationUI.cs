using System;
using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;
using DaVinciEye.Core;

namespace DaVinciEye.UI
{
    /// <summary>
    /// Main application UI manager that handles mode selection and primary navigation
    /// Implements MRTK-based UI components for HoloLens 2 interaction
    /// </summary>
    public class MainApplicationUI : MonoBehaviour
    {
        [Header("UI Panel References")]
        [SerializeField] private GameObject handMenuPanel;
        [SerializeField] private GameObject nearMenuPanel;
        [SerializeField] private GameObject buttonBarPanel;
        [SerializeField] private GameObject dialogPanel;
        
        [Header("Mode Selection Buttons")]
        [SerializeField] private PressableButton canvasButton;
        [SerializeField] private PressableButton imageButton;
        [SerializeField] private PressableButton filtersButton;
        [SerializeField] private PressableButton colorsButton;
        
        [Header("Dialog Components")]
        [SerializeField] private Dialog confirmationDialog;
        [SerializeField] private Text dialogTitle;
        [SerializeField] private Text dialogMessage;
        
        [Header("Voice Command Labels")]
        [SerializeField] private SeeItSayItLabel canvasVoiceLabel;
        [SerializeField] private SeeItSayItLabel imageVoiceLabel;
        [SerializeField] private SeeItSayItLabel filtersVoiceLabel;
        [SerializeField] private SeeItSayItLabel colorsVoiceLabel;
        
        // Application reference
        private DaVinciEyeApp app;
        private ApplicationMode currentMode;
        
        // Events
        public event Action<ApplicationMode> OnModeChangeRequested;
        public event Action<string> OnUIError;
        
        private void Awake()
        {
            app = FindObjectOfType<DaVinciEyeApp>();
            if (app == null)
            {
                Debug.LogError("MainApplicationUI: DaVinciEyeApp not found in scene");
            }
        }
        
        private void Start()
        {
            InitializeUI();
            SetupButtonEvents();
            SetupVoiceCommands();
            
            if (app != null)
            {
                app.OnModeChanged += OnApplicationModeChanged;
                currentMode = app.CurrentMode;
                UpdateUIForMode(currentMode);
            }
        }
        
        private void OnDestroy()
        {
            if (app != null)
            {
                app.OnModeChanged -= OnApplicationModeChanged;
            }
            
            CleanupButtonEvents();
        }
        
        private void InitializeUI()
        {
            // Ensure all UI panels are properly configured
            if (handMenuPanel != null)
            {
                // Hand menu should follow hand automatically (MRTK handles this)
                Debug.Log("MainApplicationUI: Hand menu panel initialized");
            }
            
            if (nearMenuPanel != null)
            {
                // Near menu should be draggable (MRTK handles this with GrabBar)
                Debug.Log("MainApplicationUI: Near menu panel initialized");
            }
            
            if (buttonBarPanel != null)
            {
                // Button bar for mode selection
                Debug.Log("MainApplicationUI: Button bar panel initialized");
            }
            
            if (confirmationDialog != null)
            {
                // Initially hide dialog
                confirmationDialog.gameObject.SetActive(false);
            }
        }
        
        private void SetupButtonEvents()
        {
            if (canvasButton != null)
            {
                canvasButton.OnClicked.AddListener(() => RequestModeChange(ApplicationMode.CanvasDefinition));
            }
            
            if (imageButton != null)
            {
                imageButton.OnClicked.AddListener(() => RequestModeChange(ApplicationMode.ImageOverlay));
            }
            
            if (filtersButton != null)
            {
                filtersButton.OnClicked.AddListener(() => RequestModeChange(ApplicationMode.FilterApplication));
            }
            
            if (colorsButton != null)
            {
                colorsButton.OnClicked.AddListener(() => RequestModeChange(ApplicationMode.ColorAnalysis));
            }
        }
        
        private void CleanupButtonEvents()
        {
            if (canvasButton != null)
            {
                canvasButton.OnClicked.RemoveAllListeners();
            }
            
            if (imageButton != null)
            {
                imageButton.OnClicked.RemoveAllListeners();
            }
            
            if (filtersButton != null)
            {
                filtersButton.OnClicked.RemoveAllListeners();
            }
            
            if (colorsButton != null)
            {
                colorsButton.OnClicked.RemoveAllListeners();
            }
        }
        
        private void SetupVoiceCommands()
        {
            // Voice commands are handled by SeeItSayItLabel components
            // These should be configured in the inspector with appropriate keywords
            if (canvasVoiceLabel != null)
            {
                Debug.Log("MainApplicationUI: Canvas voice command configured");
            }
            
            if (imageVoiceLabel != null)
            {
                Debug.Log("MainApplicationUI: Image voice command configured");
            }
            
            if (filtersVoiceLabel != null)
            {
                Debug.Log("MainApplicationUI: Filters voice command configured");
            }
            
            if (colorsVoiceLabel != null)
            {
                Debug.Log("MainApplicationUI: Colors voice command configured");
            }
        }
        
        private void RequestModeChange(ApplicationMode newMode)
        {
            if (currentMode == newMode)
            {
                Debug.Log($"MainApplicationUI: Already in {newMode} mode");
                return;
            }
            
            // Check if mode change requires confirmation
            if (ShouldConfirmModeChange(currentMode, newMode))
            {
                ShowModeChangeConfirmation(newMode);
            }
            else
            {
                ExecuteModeChange(newMode);
            }
        }
        
        private bool ShouldConfirmModeChange(ApplicationMode from, ApplicationMode to)
        {
            // Confirm when leaving modes that might have unsaved work
            return from == ApplicationMode.ImageOverlay || from == ApplicationMode.FilterApplication;
        }
        
        private void ShowModeChangeConfirmation(ApplicationMode targetMode)
        {
            if (confirmationDialog == null) return;
            
            string modeText = GetModeDisplayName(targetMode);
            
            if (dialogTitle != null)
            {
                dialogTitle.text = "Change Mode";
            }
            
            if (dialogMessage != null)
            {
                dialogMessage.text = $"Switch to {modeText} mode? Current work may be lost.";
            }
            
            confirmationDialog.gameObject.SetActive(true);
            
            // Setup dialog buttons (assuming standard MRTK dialog structure)
            var dialogButtons = confirmationDialog.GetComponentsInChildren<PressableButton>();
            foreach (var button in dialogButtons)
            {
                if (button.name.Contains("Confirm") || button.name.Contains("Yes"))
                {
                    button.OnClicked.RemoveAllListeners();
                    button.OnClicked.AddListener(() => {
                        ExecuteModeChange(targetMode);
                        HideConfirmationDialog();
                    });
                }
                else if (button.name.Contains("Cancel") || button.name.Contains("No"))
                {
                    button.OnClicked.RemoveAllListeners();
                    button.OnClicked.AddListener(HideConfirmationDialog);
                }
            }
        }
        
        private void HideConfirmationDialog()
        {
            if (confirmationDialog != null)
            {
                confirmationDialog.gameObject.SetActive(false);
            }
        }
        
        private void ExecuteModeChange(ApplicationMode newMode)
        {
            try
            {
                OnModeChangeRequested?.Invoke(newMode);
                
                if (app != null)
                {
                    app.SetApplicationMode(newMode);
                }
                
                Debug.Log($"MainApplicationUI: Mode changed to {newMode}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"MainApplicationUI: Failed to change mode - {ex.Message}");
                OnUIError?.Invoke($"Failed to change mode: {ex.Message}");
            }
        }
        
        private void OnApplicationModeChanged(ApplicationMode newMode)
        {
            currentMode = newMode;
            UpdateUIForMode(newMode);
        }
        
        private void UpdateUIForMode(ApplicationMode mode)
        {
            // Update button states to show current mode
            UpdateButtonState(canvasButton, mode == ApplicationMode.CanvasDefinition);
            UpdateButtonState(imageButton, mode == ApplicationMode.ImageOverlay);
            UpdateButtonState(filtersButton, mode == ApplicationMode.FilterApplication);
            UpdateButtonState(colorsButton, mode == ApplicationMode.ColorAnalysis);
            
            Debug.Log($"MainApplicationUI: UI updated for {mode} mode");
        }
        
        private void UpdateButtonState(PressableButton button, bool isActive)
        {
            if (button == null) return;
            
            // Update button visual state (MRTK handles most of this automatically)
            var buttonRenderer = button.GetComponent<Renderer>();
            if (buttonRenderer != null)
            {
                // Could modify material properties here for active state indication
                // For now, rely on MRTK's built-in state management
            }
            
            // Update interactability
            button.enabled = !isActive; // Disable button for current mode
        }
        
        private string GetModeDisplayName(ApplicationMode mode)
        {
            return mode switch
            {
                ApplicationMode.CanvasDefinition => "Canvas Definition",
                ApplicationMode.ImageOverlay => "Image Overlay",
                ApplicationMode.FilterApplication => "Filter Application",
                ApplicationMode.ColorAnalysis => "Color Analysis",
                ApplicationMode.Settings => "Settings",
                _ => mode.ToString()
            };
        }
        
        // Public methods for external UI control
        public void ShowHandMenu()
        {
            if (handMenuPanel != null)
            {
                handMenuPanel.SetActive(true);
            }
        }
        
        public void HideHandMenu()
        {
            if (handMenuPanel != null)
            {
                handMenuPanel.SetActive(false);
            }
        }
        
        public void ShowNearMenu()
        {
            if (nearMenuPanel != null)
            {
                nearMenuPanel.SetActive(true);
            }
        }
        
        public void HideNearMenu()
        {
            if (nearMenuPanel != null)
            {
                nearMenuPanel.SetActive(false);
            }
        }
        
        public void ShowError(string message)
        {
            // Use dialog to show error messages
            if (confirmationDialog != null)
            {
                if (dialogTitle != null)
                {
                    dialogTitle.text = "Error";
                }
                
                if (dialogMessage != null)
                {
                    dialogMessage.text = message;
                }
                
                confirmationDialog.gameObject.SetActive(true);
            }
            
            OnUIError?.Invoke(message);
        }
        
        // Properties
        public ApplicationMode CurrentMode => currentMode;
        public bool IsDialogVisible => confirmationDialog != null && confirmationDialog.gameObject.activeInHierarchy;
    }
}