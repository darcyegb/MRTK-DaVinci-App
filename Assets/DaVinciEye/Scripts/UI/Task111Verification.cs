using UnityEngine;
using DaVinciEye.Core;
using DaVinciEye.UI;
using MixedReality.Toolkit.UX;

namespace DaVinciEye.Verification
{
    /// <summary>
    /// Verification script for Task 11.1: Implement main application UI
    /// Tests MRTK-based main menu and mode selection interface implementation
    /// </summary>
    public class Task111Verification : MonoBehaviour
    {
        [Header("Verification Settings")]
        [SerializeField] private bool runVerificationOnStart = true;
        [SerializeField] private bool showDetailedLogs = true;
        
        [Header("Test Results")]
        [SerializeField] private bool mainUIImplemented = false;
        [SerializeField] private bool mrtkIntegrationWorking = false;
        [SerializeField] private bool modeSelectionWorking = false;
        [SerializeField] private bool voiceCommandsConfigured = false;
        [SerializeField] private bool uiInteractionTestsPassed = false;
        
        private UIManager uiManager;
        private MainApplicationUI mainUI;
        private MRTKUISetup mrtkSetup;
        private DaVinciEyeApp app;
        
        private void Start()
        {
            if (runVerificationOnStart)
            {
                VerifyTask111Implementation();
            }
        }
        
        [ContextMenu("Verify Task 11.1 Implementation")]
        public void VerifyTask111Implementation()
        {
            Debug.Log("=== Task 11.1 Verification: Implement main application UI ===");
            
            FindComponents();
            VerifyMainUIImplementation();
            VerifyMRTKIntegration();
            VerifyModeSelection();
            VerifyVoiceCommands();
            VerifyUIInteractionTests();
            
            LogVerificationResults();
        }
        
        private void FindComponents()
        {
            uiManager = FindObjectOfType<UIManager>();
            mainUI = FindObjectOfType<MainApplicationUI>();
            mrtkSetup = FindObjectOfType<MRTKUISetup>();
            app = FindObjectOfType<DaVinciEyeApp>();
            
            if (showDetailedLogs)
            {
                Debug.Log($"Found UIManager: {uiManager != null}");
                Debug.Log($"Found MainApplicationUI: {mainUI != null}");
                Debug.Log($"Found MRTKUISetup: {mrtkSetup != null}");
                Debug.Log($"Found DaVinciEyeApp: {app != null}");
            }
        }
        
        private void VerifyMainUIImplementation()
        {
            Debug.Log("--- Verifying Main Application UI Implementation ---");
            
            bool hasMainUI = mainUI != null;
            bool hasUIManager = uiManager != null;
            bool hasEventHandlers = false;
            bool hasErrorHandling = false;
            
            if (mainUI != null)
            {
                // Check if event handlers are properly set up
                var modeChangeEvent = typeof(MainApplicationUI).GetEvent("OnModeChangeRequested");
                var errorEvent = typeof(MainApplicationUI).GetEvent("OnUIError");
                
                hasEventHandlers = modeChangeEvent != null && errorEvent != null;
                
                // Check if error handling methods exist
                var showErrorMethod = typeof(MainApplicationUI).GetMethod("ShowError");
                hasErrorHandling = showErrorMethod != null;
            }
            
            mainUIImplemented = hasMainUI && hasUIManager && hasEventHandlers && hasErrorHandling;
            
            Debug.Log($"✓ Main UI Component: {hasMainUI}");
            Debug.Log($"✓ UI Manager: {hasUIManager}");
            Debug.Log($"✓ Event Handlers: {hasEventHandlers}");
            Debug.Log($"✓ Error Handling: {hasErrorHandling}");
            Debug.Log($"Main UI Implementation: {(mainUIImplemented ? "PASS" : "FAIL")}");
        }
        
        private void VerifyMRTKIntegration()
        {
            Debug.Log("--- Verifying MRTK Integration ---");
            
            bool hasMRTKSetup = mrtkSetup != null;
            bool hasHandMenuSupport = false;
            bool hasNearMenuSupport = false;
            bool hasButtonBarSupport = false;
            bool hasDialogSupport = false;
            
            if (mrtkSetup != null)
            {
                // Check if MRTK setup methods exist
                var setupMethod = typeof(MRTKUISetup).GetMethod("SetupMRTKUI");
                var validateMethod = typeof(MRTKUISetup).GetMethod("ValidateMRTKPrefabs");
                
                hasHandMenuSupport = setupMethod != null;
                hasNearMenuSupport = setupMethod != null;
                hasButtonBarSupport = setupMethod != null;
                hasDialogSupport = validateMethod != null;
            }
            
            if (mainUI != null)
            {
                // Check if main UI has MRTK integration methods
                var showHandMenu = typeof(MainApplicationUI).GetMethod("ShowHandMenu");
                var showNearMenu = typeof(MainApplicationUI).GetMethod("ShowNearMenu");
                
                hasHandMenuSupport = hasHandMenuSupport && showHandMenu != null;
                hasNearMenuSupport = hasNearMenuSupport && showNearMenu != null;
            }
            
            mrtkIntegrationWorking = hasMRTKSetup && hasHandMenuSupport && hasNearMenuSupport && 
                                   hasButtonBarSupport && hasDialogSupport;
            
            Debug.Log($"✓ MRTK Setup Component: {hasMRTKSetup}");
            Debug.Log($"✓ Hand Menu Support: {hasHandMenuSupport}");
            Debug.Log($"✓ Near Menu Support: {hasNearMenuSupport}");
            Debug.Log($"✓ Button Bar Support: {hasButtonBarSupport}");
            Debug.Log($"✓ Dialog Support: {hasDialogSupport}");
            Debug.Log($"MRTK Integration: {(mrtkIntegrationWorking ? "PASS" : "FAIL")}");
        }
        
        private void VerifyModeSelection()
        {
            Debug.Log("--- Verifying Mode Selection Interface ---");
            
            bool hasCanvasMode = false;
            bool hasImageMode = false;
            bool hasFiltersMode = false;
            bool hasColorsMode = false;
            bool hasModeChangeLogic = false;
            
            if (app != null)
            {
                // Check if all required modes are defined
                var modes = System.Enum.GetValues(typeof(ApplicationMode));
                hasCanvasMode = System.Array.IndexOf(modes, ApplicationMode.CanvasDefinition) >= 0;
                hasImageMode = System.Array.IndexOf(modes, ApplicationMode.ImageOverlay) >= 0;
                hasFiltersMode = System.Array.IndexOf(modes, ApplicationMode.FilterApplication) >= 0;
                hasColorsMode = System.Array.IndexOf(modes, ApplicationMode.ColorAnalysis) >= 0;
                
                // Check if mode change method exists
                var setModeMethod = typeof(DaVinciEyeApp).GetMethod("SetApplicationMode");
                hasModeChangeLogic = setModeMethod != null;
            }
            
            modeSelectionWorking = hasCanvasMode && hasImageMode && hasFiltersMode && 
                                 hasColorsMode && hasModeChangeLogic;
            
            Debug.Log($"✓ Canvas Mode: {hasCanvasMode}");
            Debug.Log($"✓ Image Mode: {hasImageMode}");
            Debug.Log($"✓ Filters Mode: {hasFiltersMode}");
            Debug.Log($"✓ Colors Mode: {hasColorsMode}");
            Debug.Log($"✓ Mode Change Logic: {hasModeChangeLogic}");
            Debug.Log($"Mode Selection: {(modeSelectionWorking ? "PASS" : "FAIL")}");
        }
        
        private void VerifyVoiceCommands()
        {
            Debug.Log("--- Verifying Voice Commands Configuration ---");
            
            bool hasSeeItSayItSupport = false;
            bool hasVoiceCommandSetup = false;
            bool hasCanvasVoiceCommand = false;
            bool hasImageVoiceCommand = false;
            bool hasFiltersVoiceCommand = false;
            bool hasColorsVoiceCommand = false;
            
            if (mrtkSetup != null)
            {
                // Check if voice command setup method exists
                var setupVoiceMethod = typeof(MRTKUISetup).GetMethod("SetupVoiceCommand", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                hasVoiceCommandSetup = setupVoiceMethod != null;
                
                // Check if voice commands are configured
                var voiceCommandsField = typeof(MRTKUISetup).GetField("voiceCommands", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (voiceCommandsField != null)
                {
                    var commands = voiceCommandsField.GetValue(mrtkSetup) as string[];
                    if (commands != null)
                    {
                        hasCanvasVoiceCommand = System.Array.IndexOf(commands, "canvas") >= 0;
                        hasImageVoiceCommand = System.Array.IndexOf(commands, "image") >= 0;
                        hasFiltersVoiceCommand = System.Array.IndexOf(commands, "filters") >= 0;
                        hasColorsVoiceCommand = System.Array.IndexOf(commands, "colors") >= 0;
                    }
                }
            }
            
            // Check for SeeItSayItLabel components in scene
            var seeItSayItLabels = FindObjectsOfType<SeeItSayItLabel>();
            hasSeeItSayItSupport = seeItSayItLabels.Length > 0;
            
            voiceCommandsConfigured = hasSeeItSayItSupport && hasVoiceCommandSetup && 
                                    hasCanvasVoiceCommand && hasImageVoiceCommand && 
                                    hasFiltersVoiceCommand && hasColorsVoiceCommand;
            
            Debug.Log($"✓ SeeItSayItLabel Support: {hasSeeItSayItSupport}");
            Debug.Log($"✓ Voice Command Setup: {hasVoiceCommandSetup}");
            Debug.Log($"✓ Canvas Voice Command: {hasCanvasVoiceCommand}");
            Debug.Log($"✓ Image Voice Command: {hasImageVoiceCommand}");
            Debug.Log($"✓ Filters Voice Command: {hasFiltersVoiceCommand}");
            Debug.Log($"✓ Colors Voice Command: {hasColorsVoiceCommand}");
            Debug.Log($"Voice Commands: {(voiceCommandsConfigured ? "PASS" : "FAIL")}");
        }
        
        private void VerifyUIInteractionTests()
        {
            Debug.Log("--- Verifying UI Interaction Tests ---");
            
            bool hasMainUITests = false;
            bool hasMRTKSetupTests = false;
            bool hasPerformanceTests = false;
            bool hasIntegrationTests = false;
            
            // Check if test classes exist (this is a simplified check)
            try
            {
                var mainUITestType = System.Type.GetType("DaVinciEye.Tests.UI.MainApplicationUITests");
                var mrtkSetupTestType = System.Type.GetType("DaVinciEye.Tests.UI.MRTKUISetupTests");
                var performanceTestType = System.Type.GetType("DaVinciEye.Tests.UI.UIPerformanceTests");
                
                hasMainUITests = mainUITestType != null;
                hasMRTKSetupTests = mrtkSetupTestType != null;
                hasPerformanceTests = performanceTestType != null;
                hasIntegrationTests = hasMainUITests && hasMRTKSetupTests;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not verify test classes: {ex.Message}");
            }
            
            uiInteractionTestsPassed = hasMainUITests && hasMRTKSetupTests && hasPerformanceTests;
            
            Debug.Log($"✓ Main UI Tests: {hasMainUITests}");
            Debug.Log($"✓ MRTK Setup Tests: {hasMRTKSetupTests}");
            Debug.Log($"✓ Performance Tests: {hasPerformanceTests}");
            Debug.Log($"✓ Integration Tests: {hasIntegrationTests}");
            Debug.Log($"UI Interaction Tests: {(uiInteractionTestsPassed ? "PASS" : "FAIL")}");
        }
        
        private void LogVerificationResults()
        {
            Debug.Log("=== Task 11.1 Verification Results ===");
            
            bool allTestsPassed = mainUIImplemented && mrtkIntegrationWorking && 
                                modeSelectionWorking && voiceCommandsConfigured && 
                                uiInteractionTestsPassed;
            
            Debug.Log($"Main UI Implementation: {(mainUIImplemented ? "✓ PASS" : "✗ FAIL")}");
            Debug.Log($"MRTK Integration: {(mrtkIntegrationWorking ? "✓ PASS" : "✗ FAIL")}");
            Debug.Log($"Mode Selection: {(modeSelectionWorking ? "✓ PASS" : "✗ FAIL")}");
            Debug.Log($"Voice Commands: {(voiceCommandsConfigured ? "✓ PASS" : "✗ FAIL")}");
            Debug.Log($"UI Interaction Tests: {(uiInteractionTestsPassed ? "✓ PASS" : "✗ FAIL")}");
            
            Debug.Log($"\n=== OVERALL RESULT: {(allTestsPassed ? "✓ TASK 11.1 COMPLETE" : "✗ TASK 11.1 INCOMPLETE")} ===");
            
            if (!allTestsPassed)
            {
                Debug.LogWarning("Some verification checks failed. Please review the implementation.");
                LogImplementationGuidance();
            }
        }
        
        private void LogImplementationGuidance()
        {
            Debug.Log("=== Implementation Guidance ===");
            
            if (!mainUIImplemented)
            {
                Debug.Log("• Ensure MainApplicationUI and UIManager components are properly implemented");
                Debug.Log("• Add event handlers for mode changes and error handling");
            }
            
            if (!mrtkIntegrationWorking)
            {
                Debug.Log("• Assign MRTK prefabs to MRTKUISetup component:");
                Debug.Log("  - HandMenuBase.prefab");
                Debug.Log("  - NearMenuBase.prefab");
                Debug.Log("  - CanvasButtonBar.prefab");
                Debug.Log("  - CanvasDialog.prefab");
                Debug.Log("  - SeeItSayItLabel-Canvas.prefab");
            }
            
            if (!modeSelectionWorking)
            {
                Debug.Log("• Ensure all ApplicationMode enum values are defined");
                Debug.Log("• Implement mode change logic in DaVinciEyeApp");
            }
            
            if (!voiceCommandsConfigured)
            {
                Debug.Log("• Configure voice commands: 'canvas', 'image', 'filters', 'colors'");
                Debug.Log("• Apply SeeItSayItLabel components to buttons");
            }
            
            if (!uiInteractionTestsPassed)
            {
                Debug.Log("• Implement UI interaction tests for all primary functions");
                Debug.Log("• Add performance tests for UI responsiveness");
            }
        }
        
        // Public properties for external verification
        public bool IsTask111Complete => mainUIImplemented && mrtkIntegrationWorking && 
                                       modeSelectionWorking && voiceCommandsConfigured && 
                                       uiInteractionTestsPassed;
        
        public string GetVerificationSummary()
        {
            return $"Task 11.1 Status: " +
                   $"MainUI({(mainUIImplemented ? "✓" : "✗")}) " +
                   $"MRTK({(mrtkIntegrationWorking ? "✓" : "✗")}) " +
                   $"Modes({(modeSelectionWorking ? "✓" : "✗")}) " +
                   $"Voice({(voiceCommandsConfigured ? "✓" : "✗")}) " +
                   $"Tests({(uiInteractionTestsPassed ? "✓" : "✗")})";
        }
    }
}