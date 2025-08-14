using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DaVinciEye.SpatialTracking
{
    /// <summary>
    /// Manages environmental adaptation for optimal overlay visibility and performance
    /// Detects lighting changes and adjusts overlay visibility and performance settings
    /// </summary>
    public class EnvironmentalAdaptationManager : MonoBehaviour
    {
        [Header("Lighting Detection Settings")]
        [SerializeField] private float lightingCheckInterval = 1f;
        [SerializeField] private float lightingChangeThreshold = 0.2f;
        [SerializeField] private int lightingSampleCount = 5;
        [SerializeField] private bool enableAutomaticAdaptation = true;
        
        [Header("Overlay Adaptation Settings")]
        [SerializeField] private float minOverlayOpacity = 0.1f;
        [SerializeField] private float maxOverlayOpacity = 1.0f;
        [SerializeField] private float overlayAdaptationSpeed = 2f;
        [SerializeField] private AnimationCurve lightingToOpacityCurve = AnimationCurve.Linear(0, 1, 1, 0.3f);
        
        [Header("Performance Adaptation Settings")]
        [SerializeField] private bool enablePerformanceAdaptation = true;
        [SerializeField] private float performanceCheckInterval = 2f;
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] private float lowPerformanceThreshold = 45f;
        [SerializeField] private float highPerformanceThreshold = 55f;
        
        // Environmental state properties
        public LightingCondition CurrentLightingCondition { get; private set; } = LightingCondition.Normal;
        public float CurrentLightingIntensity { get; private set; } = 1.0f;
        public PerformanceLevel CurrentPerformanceLevel { get; private set; } = PerformanceLevel.High;
        public float CurrentFrameRate { get; private set; } = 60f;
        
        // Adaptation state
        public bool IsAdaptingToLighting { get; private set; }
        public bool IsAdaptingToPerformance { get; private set; }
        public float AdaptedOverlayOpacity { get; private set; } = 1.0f;
        
        // Events
        public event Action<LightingCondition> OnLightingConditionChanged;
        public event Action<PerformanceLevel> OnPerformanceAdaptationChanged;
        public event Action<float> OnOverlayOpacityAdapted;
        public event Action<string> OnEnvironmentalWarning;
        
        // Private fields
        private Camera mainCamera;
        private Light[] sceneLights;
        private Volume postProcessVolume;
        private UniversalRenderPipelineAsset urpAsset;
        
        private Coroutine lightingMonitorCoroutine;
        private Coroutine performanceMonitorCoroutine;
        private Coroutine overlayAdaptationCoroutine;
        
        private float[] lightingHistory;
        private int lightingHistoryIndex;
        private float previousLightingIntensity;
        private float previousFrameRate;
        private LightingCondition previousLightingCondition;
        private PerformanceLevel previousPerformanceLevel;
        
        private void Start()
        {
            InitializeEnvironmentalAdaptation();
            StartEnvironmentalMonitoring();
        }
        
        private void OnDestroy()
        {
            StopEnvironmentalMonitoring();
        }
        
        /// <summary>
        /// Initialize environmental adaptation system
        /// </summary>
        private void InitializeEnvironmentalAdaptation()
        {
            // Get main camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            
            // Get scene lights
            sceneLights = FindObjectsOfType<Light>();
            
            // Get post-processing volume
            postProcessVolume = FindObjectOfType<Volume>();
            
            // Get URP asset
            urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            
            // Initialize lighting history
            lightingHistory = new float[lightingSampleCount];
            lightingHistoryIndex = 0;
            
            // Initialize state
            previousLightingIntensity = GetCurrentLightingIntensity();
            previousFrameRate = GetCurrentFrameRate();
            previousLightingCondition = CurrentLightingCondition;
            previousPerformanceLevel = CurrentPerformanceLevel;
            
            Debug.Log("EnvironmentalAdaptationManager: Initialized successfully");
        }
        
        /// <summary>
        /// Start environmental monitoring coroutines
        /// </summary>
        public void StartEnvironmentalMonitoring()
        {
            if (enableAutomaticAdaptation)
            {
                if (lightingMonitorCoroutine == null)
                {
                    lightingMonitorCoroutine = StartCoroutine(MonitorLightingConditions());
                }
                
                if (performanceMonitorCoroutine == null && enablePerformanceAdaptation)
                {
                    performanceMonitorCoroutine = StartCoroutine(MonitorPerformance());
                }
                
                Debug.Log("EnvironmentalAdaptationManager: Started environmental monitoring");
            }
        }
        
        /// <summary>
        /// Stop environmental monitoring
        /// </summary>
        public void StopEnvironmentalMonitoring()
        {
            if (lightingMonitorCoroutine != null)
            {
                StopCoroutine(lightingMonitorCoroutine);
                lightingMonitorCoroutine = null;
            }
            
            if (performanceMonitorCoroutine != null)
            {
                StopCoroutine(performanceMonitorCoroutine);
                performanceMonitorCoroutine = null;
            }
            
            if (overlayAdaptationCoroutine != null)
            {
                StopCoroutine(overlayAdaptationCoroutine);
                overlayAdaptationCoroutine = null;
            }
            
            Debug.Log("EnvironmentalAdaptationManager: Stopped environmental monitoring");
        }
        
        /// <summary>
        /// Monitor lighting conditions coroutine
        /// </summary>
        private IEnumerator MonitorLightingConditions()
        {
            while (true)
            {
                UpdateLightingConditions();
                yield return new WaitForSeconds(lightingCheckInterval);
            }
        }
        
        /// <summary>
        /// Monitor performance coroutine
        /// </summary>
        private IEnumerator MonitorPerformance()
        {
            while (true)
            {
                UpdatePerformanceMetrics();
                yield return new WaitForSeconds(performanceCheckInterval);
            }
        }
        
        /// <summary>
        /// Update lighting conditions and adapt overlay
        /// </summary>
        private void UpdateLightingConditions()
        {
            float currentIntensity = GetCurrentLightingIntensity();
            CurrentLightingIntensity = currentIntensity;
            
            // Add to lighting history
            lightingHistory[lightingHistoryIndex] = currentIntensity;
            lightingHistoryIndex = (lightingHistoryIndex + 1) % lightingSampleCount;
            
            // Calculate average lighting over history
            float averageLighting = CalculateAverageLighting();
            
            // Determine lighting condition
            LightingCondition newCondition = DetermineLightingCondition(averageLighting);
            
            // Check for significant lighting change
            if (Mathf.Abs(currentIntensity - previousLightingIntensity) > lightingChangeThreshold ||
                newCondition != previousLightingCondition)
            {
                CurrentLightingCondition = newCondition;
                OnLightingConditionChanged?.Invoke(newCondition);
                
                // Adapt overlay opacity
                AdaptOverlayToLighting(averageLighting);
                
                previousLightingIntensity = currentIntensity;
                previousLightingCondition = newCondition;
                
                Debug.Log($"EnvironmentalAdaptationManager: Lighting changed to {newCondition} (intensity: {currentIntensity:F2})");
            }
        }
        
        /// <summary>
        /// Update performance metrics and adapt settings
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            float currentFrameRate = GetCurrentFrameRate();
            CurrentFrameRate = currentFrameRate;
            
            // Determine performance level
            PerformanceLevel newLevel = DeterminePerformanceLevel(currentFrameRate);
            
            if (newLevel != previousPerformanceLevel)
            {
                CurrentPerformanceLevel = newLevel;
                OnPerformanceAdaptationChanged?.Invoke(newLevel);
                
                // Adapt performance settings
                AdaptPerformanceSettings(newLevel);
                
                previousPerformanceLevel = newLevel;
                previousFrameRate = currentFrameRate;
                
                Debug.Log($"EnvironmentalAdaptationManager: Performance level changed to {newLevel} (FPS: {currentFrameRate:F1})");
            }
        }
        
        /// <summary>
        /// Get current lighting intensity from scene
        /// </summary>
        private float GetCurrentLightingIntensity()
        {
            float totalIntensity = 0f;
            int lightCount = 0;
            
            // Sample from scene lights
            if (sceneLights != null && sceneLights.Length > 0)
            {
                foreach (var light in sceneLights)
                {
                    if (light != null && light.enabled)
                    {
                        totalIntensity += light.intensity;
                        lightCount++;
                    }
                }
            }
            
            // If no lights found, use ambient lighting
            if (lightCount == 0)
            {
                totalIntensity = RenderSettings.ambientIntensity;
                lightCount = 1;
            }
            
            return lightCount > 0 ? totalIntensity / lightCount : 1.0f;
        }
        
        /// <summary>
        /// Get current frame rate
        /// </summary>
        private float GetCurrentFrameRate()
        {
            return 1.0f / Time.unscaledDeltaTime;
        }
        
        /// <summary>
        /// Calculate average lighting from history
        /// </summary>
        private float CalculateAverageLighting()
        {
            float sum = 0f;
            int count = 0;
            
            for (int i = 0; i < lightingHistory.Length; i++)
            {
                if (lightingHistory[i] > 0f)
                {
                    sum += lightingHistory[i];
                    count++;
                }
            }
            
            return count > 0 ? sum / count : CurrentLightingIntensity;
        }
        
        /// <summary>
        /// Determine lighting condition based on intensity
        /// </summary>
        private LightingCondition DetermineLightingCondition(float intensity)
        {
            if (intensity < 0.3f)
                return LightingCondition.Dark;
            else if (intensity > 2.0f)
                return LightingCondition.Bright;
            else
                return LightingCondition.Normal;
        }
        
        /// <summary>
        /// Determine performance level based on frame rate
        /// </summary>
        private PerformanceLevel DeterminePerformanceLevel(float frameRate)
        {
            if (frameRate < lowPerformanceThreshold)
                return PerformanceLevel.Low;
            else if (frameRate < highPerformanceThreshold)
                return PerformanceLevel.Medium;
            else
                return PerformanceLevel.High;
        }
        
        /// <summary>
        /// Adapt overlay opacity to lighting conditions
        /// </summary>
        private void AdaptOverlayToLighting(float lightingIntensity)
        {
            if (overlayAdaptationCoroutine != null)
            {
                StopCoroutine(overlayAdaptationCoroutine);
            }
            
            // Calculate target opacity using curve
            float normalizedLighting = Mathf.Clamp01(lightingIntensity / 3.0f);
            float targetOpacity = lightingToOpacityCurve.Evaluate(normalizedLighting);
            targetOpacity = Mathf.Clamp(targetOpacity, minOverlayOpacity, maxOverlayOpacity);
            
            overlayAdaptationCoroutine = StartCoroutine(AdaptOverlayOpacityCoroutine(targetOpacity));
            
            IsAdaptingToLighting = true;
        }
        
        /// <summary>
        /// Smoothly adapt overlay opacity
        /// </summary>
        private IEnumerator AdaptOverlayOpacityCoroutine(float targetOpacity)
        {
            float startOpacity = AdaptedOverlayOpacity;
            float elapsedTime = 0f;
            float adaptationDuration = 1f / overlayAdaptationSpeed;
            
            while (elapsedTime < adaptationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / adaptationDuration;
                
                AdaptedOverlayOpacity = Mathf.Lerp(startOpacity, targetOpacity, t);
                OnOverlayOpacityAdapted?.Invoke(AdaptedOverlayOpacity);
                
                yield return null;
            }
            
            AdaptedOverlayOpacity = targetOpacity;
            OnOverlayOpacityAdapted?.Invoke(AdaptedOverlayOpacity);
            
            IsAdaptingToLighting = false;
            overlayAdaptationCoroutine = null;
        }
        
        /// <summary>
        /// Adapt performance settings based on performance level
        /// </summary>
        private void AdaptPerformanceSettings(PerformanceLevel level)
        {
            IsAdaptingToPerformance = true;
            
            switch (level)
            {
                case PerformanceLevel.Low:
                    AdaptToLowPerformance();
                    OnEnvironmentalWarning?.Invoke("Performance is low. Reducing quality settings.");
                    break;
                    
                case PerformanceLevel.Medium:
                    AdaptToMediumPerformance();
                    break;
                    
                case PerformanceLevel.High:
                    AdaptToHighPerformance();
                    break;
            }
            
            IsAdaptingToPerformance = false;
        }
        
        /// <summary>
        /// Adapt settings for low performance
        /// </summary>
        private void AdaptToLowPerformance()
        {
            // Reduce render scale
            if (urpAsset != null)
            {
                urpAsset.renderScale = 0.7f;
            }
            
            // Reduce shadow quality
            if (postProcessVolume != null)
            {
                // Disable expensive post-processing effects
                // This would be implemented based on specific post-processing setup
            }
            
            Debug.Log("EnvironmentalAdaptationManager: Adapted to low performance settings");
        }
        
        /// <summary>
        /// Adapt settings for medium performance
        /// </summary>
        private void AdaptToMediumPerformance()
        {
            // Standard render scale
            if (urpAsset != null)
            {
                urpAsset.renderScale = 0.85f;
            }
            
            Debug.Log("EnvironmentalAdaptationManager: Adapted to medium performance settings");
        }
        
        /// <summary>
        /// Adapt settings for high performance
        /// </summary>
        private void AdaptToHighPerformance()
        {
            // Full render scale
            if (urpAsset != null)
            {
                urpAsset.renderScale = 1.0f;
            }
            
            Debug.Log("EnvironmentalAdaptationManager: Adapted to high performance settings");
        }
        
        /// <summary>
        /// Manually trigger environmental adaptation
        /// </summary>
        public void ForceEnvironmentalAdaptation()
        {
            Debug.Log("EnvironmentalAdaptationManager: Forcing environmental adaptation");
            
            UpdateLightingConditions();
            UpdatePerformanceMetrics();
        }
        
        /// <summary>
        /// Get current environmental status
        /// </summary>
        public EnvironmentalStatus GetEnvironmentalStatus()
        {
            return new EnvironmentalStatus
            {
                lightingCondition = CurrentLightingCondition,
                lightingIntensity = CurrentLightingIntensity,
                performanceLevel = CurrentPerformanceLevel,
                currentFrameRate = CurrentFrameRate,
                adaptedOverlayOpacity = AdaptedOverlayOpacity,
                isAdaptingToLighting = IsAdaptingToLighting,
                isAdaptingToPerformance = IsAdaptingToPerformance
            };
        }
        
        /// <summary>
        /// Set overlay adaptation parameters
        /// </summary>
        public void SetOverlayAdaptationParameters(float minOpacity, float maxOpacity, float adaptationSpeed)
        {
            minOverlayOpacity = Mathf.Clamp01(minOpacity);
            maxOverlayOpacity = Mathf.Clamp01(maxOpacity);
            overlayAdaptationSpeed = Mathf.Max(0.1f, adaptationSpeed);
            
            Debug.Log($"EnvironmentalAdaptationManager: Updated overlay adaptation parameters - Min: {minOverlayOpacity}, Max: {maxOverlayOpacity}, Speed: {overlayAdaptationSpeed}");
        }
        
        /// <summary>
        /// Enable or disable automatic adaptation
        /// </summary>
        public void SetAutomaticAdaptation(bool enabled)
        {
            enableAutomaticAdaptation = enabled;
            
            if (enabled)
            {
                StartEnvironmentalMonitoring();
            }
            else
            {
                StopEnvironmentalMonitoring();
            }
            
            Debug.Log($"EnvironmentalAdaptationManager: Automatic adaptation {(enabled ? "enabled" : "disabled")}");
        }
    }
    
    /// <summary>
    /// Enumeration for lighting conditions
    /// </summary>
    public enum LightingCondition
    {
        Dark,
        Normal,
        Bright
    }
    
    /// <summary>
    /// Enumeration for performance levels
    /// </summary>
    public enum PerformanceLevel
    {
        Low,
        Medium,
        High
    }
    
    /// <summary>
    /// Environmental status information
    /// </summary>
    [System.Serializable]
    public class EnvironmentalStatus
    {
        public LightingCondition lightingCondition;
        public float lightingIntensity;
        public PerformanceLevel performanceLevel;
        public float currentFrameRate;
        public float adaptedOverlayOpacity;
        public bool isAdaptingToLighting;
        public bool isAdaptingToPerformance;
    }
}