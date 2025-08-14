using System;
using UnityEngine;
using DaVinciEye.Core;
using DaVinciEye.Canvas;
using DaVinciEye.ImageOverlay;
using DaVinciEye.Filters;
using DaVinciEye.ColorAnalysis;

namespace DaVinciEye.UI
{
    /// <summary>
    /// Central UI manager that coordinates all UI systems and integrates with application systems
    /// Implements requirements 1.1, 2.1, 4.1, 6.1 for main application UI
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private MainApplicationUI mainApplicationUI;
        [SerializeField] private MRTKUISetup mrtkUISetup;
        
        [Header("System Integration")]
        [SerializeField] private bool autoConnectToSystems = true;
        [SerializeField] private bool showDebugInfo = true;
        
        // System references
        private DaVinciEyeApp app;
        private ICanvasManager canvasManager;
        private IImageOverlay imageOverlay;
        private IFilterProcessor filterProcessor;
        private IColorAnalyzer colorAnalyzer;
        
        // UI state
        private bool isInitialized = false;
        private ApplicationMode lastMode;
        
        // Events
        public event Action OnUIInitialized;
        public event Action<ApplicationMode> OnModeUIChanged;
        public event Action<string> OnUIStatusChanged;
        
        private void Awake()
        {
            // Find or create UI components
            if (mainApplicationUI == null)
            {
                mainApplicationUI = GetComponent<MainApplicationUI>();
                if (mainApplicationUI == null)
                {
                    mainApplicationUI = gameObject.AddComponent<MainApplicationUI>();
                }
            }
            
            if (mrtkUISetup == null)
            {
                mrtkUISetup = GetComponent<MRTKUISetup>();
                if (mrtkUISetup == null)
                {
                    mrtkUISetup = gameObject.AddComponent<MRTKUISetup>();
                }
            }
        }
        
        private void Start()
        {
            InitializeUI();
        }
        
        private void OnDestroy()
        {
            CleanupEventHandlers();
        }
        
        private void InitializeUI()
        {
            try
            {
                // Find application systems
                if (autoConnectToSystems)
                {
                    ConnectToApplicationSystems();
                }
                
                // Setup UI event handlers
                SetupUIEventHandlers();
                
                // Initialize MRTK UI components
                if (mrtkUISetup != null)
                {
                    mrtkUISetup.SetupMRTKUI();
                }
                
                isInitialized = true;
                OnUIInitialized?.Invoke();
                
                LogStatus("UI Manager initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"UIManager: Initialization failed - {ex.Message}");
                OnUIStatusChanged?.Invoke($"UI initialization failed: {ex.Message}");
            }
        }
        
        private void ConnectToApplicationSystems()
        {
            // Find main application
            app = FindObjectOfType<DaVinciEyeApp>();
            if (app == null)
            {
                Debug.LogWarning("UIManager: DaVinciEyeApp not found in scene");
                return;
            }
            
            // Get system references from app
            canvasManager = app.CanvasManager;
            imageOverlay = app.ImageOverlay;
            filterProcessor = app.FilterProcessor;
            colorAnalyzer = app.ColorAnalyzer;
            
            // Setup app event handlers
            app.OnModeChanged += OnApplicationModeChanged;
            app.OnApplicationInitialized += OnApplicationInitialized;
            app.OnError += OnApplicationError;
            
            LogStatus("Connected to application systems");
        }
        
        private void SetupUIEventHandlers()
        {
            if (mainApplicationUI != null)
            {
                mainApplicationUI.OnModeChangeRequested += OnModeChangeRequested;
                mainApplicationUI.OnUIError += OnUIError;
            }
        }
        
        private void CleanupEventHandlers()
        {
            if (app != null)
            {
                app.OnModeChanged -= OnApplicationModeChanged;
                app.OnApplicationInitialized -= OnApplicationInitialized;
                app.OnError -= OnApplicationError;
            }
            
            if (mainApplicationUI != null)
            {
                mainApplicationUI.OnModeChangeRequested -= OnModeChangeRequested;
                mainApplicationUI.OnUIError -= OnUIError;
            }
        }
        
        // Event handlers
        private void OnApplicationModeChanged(ApplicationMode newMode)
        {
            lastMode = newMode;
            OnModeUIChanged?.Invoke(newMode);
            
            // Update UI based on mode
            UpdateUIForMode(newMode);
            
            LogStatus($"UI updated for {newMode} mode");
        }
        
        private void OnApplicationInitialized()
        {
            LogStatus("Application systems initialized");
            
            // Ensure UI is properly configured for initial mode
            if (app != null)
            {
                UpdateUIForMode(app.CurrentMode);
            }
        }
        
        private void OnApplicationError(string error)
        {
            if (mainApplicationUI != null)
            {
                mainApplicationUI.ShowError(error);
            }
            
            OnUIStatusChanged?.Invoke($"Application error: {error}");
        }
        
        private void OnModeChangeRequested(ApplicationMode requestedMode)
        {
            if (app != null)
            {
                app.SetApplicationMode(requestedMode);
            }
            else
            {
                Debug.LogWarning("UIManager: Cannot change mode - application not connected");
            }
        }
        
        private void OnUIError(string error)
        {
            Debug.LogError($"UIManager: UI Error - {error}");
            OnUIStatusChanged?.Invoke($"UI error: {error}");
        }
        
        private void UpdateUIForMode(ApplicationMode mode)
        {
            // Show/hide UI panels based on current mode
            switch (mode)
            {
                case ApplicationMode.CanvasDefinition:
                    ShowCanvasDefinitionUI();
                    break;
                    
                case ApplicationMode.ImageOverlay:
                    ShowImageOverlayUI();
                    break;
                    
                case ApplicationMode.FilterApplication:
                    ShowFilterApplicationUI();
                    break;
                    
                case ApplicationMode.ColorAnalysis:
                    ShowColorAnalysisUI();
                    break;
                    
                case ApplicationMode.Settings:
                    ShowSettingsUI();
                    break;
            }
        }
        
        private void ShowCanvasDefinitionUI()
        {
            // Show UI relevant to canvas definition
            if (mainApplicationUI != null)
            {
                mainApplicationUI.ShowNearMenu(); // Show main controls
            }
            
            LogStatus("Canvas definition UI active");
        }
        
        private void ShowImageOverlayUI()
        {
            // Show UI relevant to image overlay
            if (mainApplicationUI != null)
            {
                mainApplicationUI.ShowNearMenu(); // Show main controls
                mainApplicationUI.ShowHandMenu(); // Show quick actions
            }
            
            LogStatus("Image overlay UI active");
        }
        
        private void ShowFilterApplicationUI()
        {
            // Show UI relevant to filter application
            if (mainApplicationUI != null)
            {
                mainApplicationUI.ShowNearMenu(); // Show filter controls
                mainApplicationUI.ShowHandMenu(); // Show quick actions
            }
            
            LogStatus("Filter application UI active");
        }
        
        private void ShowColorAnalysisUI()
        {
            // Show UI relevant to color analysis
            if (mainApplicationUI != null)
            {
                mainApplicationUI.ShowNearMenu(); // Show color controls
                mainApplicationUI.ShowHandMenu(); // Show quick actions
            }
            
            LogStatus("Color analysis UI active");
        }
        
        private void ShowSettingsUI()
        {
            // Show settings UI
            if (mainApplicationUI != null)
            {
                mainApplicationUI.ShowNearMenu(); // Show settings
            }
            
            LogStatus("Settings UI active");
        }
        
        // Public methods for external control
        public void ShowMainMenu()
        {
            if (mainApplicationUI != null)
            {
                mainApplicationUI.ShowNearMenu();
            }
        }
        
        public void HideMainMenu()
        {
            if (mainApplicationUI != null)
            {
                mainApplicationUI.HideNearMenu();
            }
        }
        
        public void ShowQuickActions()
        {
            if (mainApplicationUI != null)
            {
                mainApplicationUI.ShowHandMenu();
            }
        }
        
        public void HideQuickActions()
        {
            if (mainApplicationUI != null)
            {
                mainApplicationUI.HideHandMenu();
            }
        }
        
        public void ShowError(string message)
        {
            if (mainApplicationUI != null)
            {
                mainApplicationUI.ShowError(message);
            }
        }
        
        public void RefreshUI()
        {
            if (app != null)
            {
                UpdateUIForMode(app.CurrentMode);
            }
        }
        
        // Validation and diagnostics
        public bool ValidateUISetup()
        {
            bool isValid = true;
            
            if (mainApplicationUI == null)
            {
                Debug.LogError("UIManager: MainApplicationUI not found");
                isValid = false;
            }
            
            if (mrtkUISetup == null)
            {
                Debug.LogError("UIManager: MRTKUISetup not found");
                isValid = false;
            }
            
            if (app == null && autoConnectToSystems)
            {
                Debug.LogWarning("UIManager: DaVinciEyeApp not connected");
            }
            
            return isValid;
        }
        
        private void LogStatus(string message)
        {
            if (showDebugInfo)
            {
                Debug.Log($"UIManager: {message}");
            }
            
            OnUIStatusChanged?.Invoke(message);
        }
        
        // Properties
        public bool IsInitialized => isInitialized;
        public ApplicationMode CurrentMode => app?.CurrentMode ?? ApplicationMode.CanvasDefinition;
        public MainApplicationUI MainUI => mainApplicationUI;
        public MRTKUISetup MRTKSetup => mrtkUISetup;
        
        // System access (for other UI components)
        public DaVinciEyeApp Application => app;
        public ICanvasManager CanvasManager => canvasManager;
        public IImageOverlay ImageOverlay => imageOverlay;
        public IFilterProcessor FilterProcessor => filterProcessor;
        public IColorAnalyzer ColorAnalyzer => colorAnalyzer;
    }
}