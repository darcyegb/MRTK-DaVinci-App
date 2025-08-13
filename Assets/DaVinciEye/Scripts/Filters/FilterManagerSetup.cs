using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// Helper script to set up FilterManager in the scene with proper Volume configuration
    /// This script automates the setup process described in the implementation checklist
    /// </summary>
    [System.Serializable]
    public class FilterManagerSetup : MonoBehaviour
    {
        [Header("Setup Configuration")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool createVolumeProfile = true;
        [SerializeField] private string volumeProfileName = "DaVinciEyeFilterProfile";
        
        [Header("Components")]
        [SerializeField] private FilterManager filterManager;
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private Camera targetCamera;
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupFilterSystem();
            }
        }
        
        /// <summary>
        /// Automatically set up the complete filter system
        /// Implements the checklist from task 5.1
        /// </summary>
        [ContextMenu("Setup Filter System")]
        public void SetupFilterSystem()
        {
            Debug.Log("FilterManagerSetup: Starting automatic filter system setup...");
            
            // Step 1: Ensure we have a FilterManager
            SetupFilterManager();
            
            // Step 2: Setup Volume component with Post Process profile
            SetupVolumeComponent();
            
            // Step 3: Configure target camera for URP
            SetupCameraForURP();
            
            // Step 4: Verify URP Renderer Features
            VerifyURPConfiguration();
            
            Debug.Log("FilterManagerSetup: Filter system setup complete!");
        }
        
        /// <summary>
        /// Setup or find FilterManager component
        /// </summary>
        private void SetupFilterManager()
        {
            if (filterManager == null)
            {
                filterManager = GetComponent<FilterManager>();
                if (filterManager == null)
                {
                    filterManager = gameObject.AddComponent<FilterManager>();
                    Debug.Log("FilterManagerSetup: Added FilterManager component");
                }
            }
        }
        
        /// <summary>
        /// Setup Volume component with Post Process profile
        /// ✓ Add Volume component with Post Process profile to scene
        /// </summary>
        private void SetupVolumeComponent()
        {
            // Get or create Volume component
            if (postProcessVolume == null)
            {
                postProcessVolume = GetComponent<Volume>();
                if (postProcessVolume == null)
                {
                    postProcessVolume = gameObject.AddComponent<Volume>();
                    Debug.Log("FilterManagerSetup: Added Volume component");
                }
            }
            
            // Create Volume Profile if needed
            if (createVolumeProfile && postProcessVolume.profile == null)
            {
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = volumeProfileName;
                
                // Add built-in effects as specified in checklist
                // ✓ Use built-in effects: ColorAdjustments (grayscale, contrast), Bloom (edge detection)
                var colorAdjustments = profile.Add<ColorAdjustments>(false);
                var bloom = profile.Add<Bloom>(false);
                var vignette = profile.Add<Vignette>(false);
                
                postProcessVolume.profile = profile;
                postProcessVolume.isGlobal = true;
                postProcessVolume.weight = 1f;
                
                Debug.Log("FilterManagerSetup: Created and configured Volume Profile with built-in effects");
            }
        }
        
        /// <summary>
        /// Configure camera for URP post-processing
        /// </summary>
        private void SetupCameraForURP()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    targetCamera = FindObjectOfType<Camera>();
                }
            }
            
            if (targetCamera != null)
            {
                var cameraData = targetCamera.GetUniversalAdditionalCameraData();
                if (cameraData != null)
                {
                    cameraData.renderPostProcessing = true;
                    Debug.Log("FilterManagerSetup: Enabled post-processing on camera");
                }
            }
        }
        
        /// <summary>
        /// Verify URP configuration for post-processing
        /// </summary>
        private void VerifyURPConfiguration()
        {
            var urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                Debug.Log("FilterManagerSetup: URP is properly configured");
            }
            else
            {
                Debug.LogWarning("FilterManagerSetup: URP is not set as the active render pipeline. Post-processing may not work correctly.");
            }
        }
        
        /// <summary>
        /// Test the filter system with sample filters
        /// </summary>
        [ContextMenu("Test Filter System")]
        public void TestFilterSystem()
        {
            if (filterManager == null)
            {
                Debug.LogError("FilterManagerSetup: FilterManager not found. Run Setup Filter System first.");
                return;
            }
            
            Debug.Log("FilterManagerSetup: Testing filter system...");
            
            // Test grayscale filter
            var grayscaleParams = new FilterParameters(FilterType.Grayscale);
            grayscaleParams.intensity = 0.8f;
            filterManager.ApplyFilter(FilterType.Grayscale, grayscaleParams);
            
            Debug.Log($"FilterManagerSetup: Applied grayscale filter. Active filters: {filterManager.ActiveFilters.Count}");
            
            // Test saving settings
            string settings = filterManager.SaveFilterSettings();
            Debug.Log($"FilterManagerSetup: Saved filter settings: {settings}");
        }
        
        /// <summary>
        /// Clear all test filters
        /// </summary>
        [ContextMenu("Clear Test Filters")]
        public void ClearTestFilters()
        {
            if (filterManager != null)
            {
                filterManager.ClearAllFilters();
                Debug.Log("FilterManagerSetup: Cleared all test filters");
            }
        }
        
        /// <summary>
        /// Validate the current setup
        /// </summary>
        public bool ValidateSetup()
        {
            bool isValid = true;
            
            if (filterManager == null)
            {
                Debug.LogError("FilterManagerSetup: FilterManager is missing");
                isValid = false;
            }
            
            if (postProcessVolume == null)
            {
                Debug.LogError("FilterManagerSetup: Volume component is missing");
                isValid = false;
            }
            
            if (postProcessVolume != null && postProcessVolume.profile == null)
            {
                Debug.LogError("FilterManagerSetup: Volume Profile is missing");
                isValid = false;
            }
            
            var urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                Debug.LogError("FilterManagerSetup: URP is not configured");
                isValid = false;
            }
            
            return isValid;
        }
    }
}