using UnityEngine;
using UnityEngine.XR.Management;

namespace DaVinciEye.Core
{
    /// <summary>
    /// Handles initial scene setup and MRTK configuration
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private bool autoInitializeXR = true;
        [SerializeField] private bool enableSpatialMesh = true;
        [SerializeField] private bool enableHandTracking = true;
        
        [Header("Lighting Setup")]
        [SerializeField] private Light mainLight;
        [SerializeField] private Color ambientColor = new Color(0.2f, 0.2f, 0.3f);
        
        [Header("Performance Settings")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool enableVSync = false;
        
        private void Awake()
        {
            ConfigurePerformanceSettings();
            ConfigureLighting();
            
            if (autoInitializeXR)
            {
                InitializeXR();
            }
        }
        
        private void ConfigurePerformanceSettings()
        {
            // Set target frame rate for HoloLens 2
            Application.targetFrameRate = targetFrameRate;
            
            // Configure VSync
            QualitySettings.vSyncCount = enableVSync ? 1 : 0;
            
            // Optimize for mixed reality
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadows = ShadowQuality.HardOnly;
            
            Debug.Log($"SceneSetup: Performance configured - Target FPS: {targetFrameRate}, VSync: {enableVSync}");
        }
        
        private void ConfigureLighting()
        {
            // Set ambient lighting for mixed reality
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
            
            // Configure main light if available
            if (mainLight != null)
            {
                mainLight.type = LightType.Directional;
                mainLight.intensity = 1.0f;
                mainLight.shadows = LightShadows.Soft;
                mainLight.shadowStrength = 0.5f;
            }
            
            Debug.Log("SceneSetup: Lighting configured for mixed reality");
        }
        
        private void InitializeXR()
        {
            // Initialize XR if not already initialized
            if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
            {
                if (!XRGeneralSettings.Instance.Manager.isInitializationComplete)
                {
                    StartCoroutine(InitializeXRCoroutine());
                }
                else
                {
                    Debug.Log("SceneSetup: XR already initialized");
                }
            }
            else
            {
                Debug.LogWarning("SceneSetup: XR General Settings not found");
            }
        }
        
        private System.Collections.IEnumerator InitializeXRCoroutine()
        {
            Debug.Log("SceneSetup: Initializing XR...");
            
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
            
            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.LogError("SceneSetup: Failed to initialize XR");
            }
            else
            {
                Debug.Log("SceneSetup: XR initialized successfully");
                XRGeneralSettings.Instance.Manager.StartSubsystems();
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            // Handle application pause for HoloLens
            if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
            {
                if (pauseStatus)
                {
                    XRGeneralSettings.Instance.Manager.StopSubsystems();
                }
                else
                {
                    XRGeneralSettings.Instance.Manager.StartSubsystems();
                }
            }
        }
        
        private void OnDestroy()
        {
            // Clean up XR on scene destruction
            if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
            {
                XRGeneralSettings.Instance.Manager.StopSubsystems();
                XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            }
        }
    }
}