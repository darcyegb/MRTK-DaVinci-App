using System;
using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;

namespace DaVinciEye.ImageOverlay
{
    /// <summary>
    /// Controls exposure and contrast adjustments with MRTK UI integration
    /// Provides real-time adjustment preview and validation
    /// </summary>
    public class ExposureContrastController : MonoBehaviour
    {
        [Header("UI Controls")]
        [SerializeField] private Slider exposureSlider;
        [SerializeField] private Slider contrastSlider;
        [SerializeField] private PressableButton resetButton;
        [SerializeField] private PressableButton applyButton;
        
        [Header("Value Display")]
        [SerializeField] private TMPro.TextMeshProUGUI exposureValueText;
        [SerializeField] private TMPro.TextMeshProUGUI contrastValueText;
        
        [Header("Settings")]
        [SerializeField] private float exposureMin = -2f;
        [SerializeField] private float exposureMax = 2f;
        [SerializeField] private float contrastMin = -1f;
        [SerializeField] private float contrastMax = 1f;
        [SerializeField] private bool enableRealTimePreview = true;
        [SerializeField] private float updateThreshold = 0.01f;
        
        // Private fields
        private ImageOverlayManager imageOverlay;
        private ImageAdjustmentProcessor adjustmentProcessor;
        private ImageAdjustments currentAdjustments;
        private float lastExposureValue;
        private float lastContrastValue;
        private bool isInitialized = false;
        
        // Events
        public event Action<float> OnExposureChanged;
        public event Action<float> OnContrastChanged;
        public event Action<ImageAdjustments> OnAdjustmentsApplied;
        public event Action OnAdjustmentsReset;
        
        // Properties
        public float ExposureValue => currentAdjustments?.exposure ?? 0f;
        public float ContrastValue => currentAdjustments?.contrast ?? 0f;
        public bool HasAdjustments => currentAdjustments != null && 
            (Mathf.Abs(currentAdjustments.exposure) > updateThreshold || 
             Mathf.Abs(currentAdjustments.contrast) > updateThreshold);
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            SetupComponents();
            SetupUI();
        }
        
        /// <summary>
        /// Initializes the exposure contrast controller
        /// </summary>
        private void Initialize()
        {
            currentAdjustments = new ImageAdjustments();
            lastExposureValue = 0f;
            lastContrastValue = 0f;
            
            Debug.Log("ExposureContrastController: Initialized");
        }
        
        /// <summary>
        /// Sets up component references
        /// </summary>
        private void SetupComponents()
        {
            // Find image overlay manager
            imageOverlay = GetComponent<ImageOverlayManager>();
            if (imageOverlay == null)
            {
                imageOverlay = FindObjectOfType<ImageOverlayManager>();
            }
            
            // Find adjustment processor
            adjustmentProcessor = GetComponent<ImageAdjustmentProcessor>();
            if (adjustmentProcessor == null)
            {
                adjustmentProcessor = FindObjectOfType<ImageAdjustmentProcessor>();
            }
            
            if (imageOverlay != null)
            {
                // Subscribe to image overlay events
                imageOverlay.OnImageLoaded += OnImageLoaded;
                imageOverlay.OnAdjustmentsApplied += OnImageAdjustmentsApplied;
                
                Debug.Log("ExposureContrastController: Connected to image overlay");
            }
            else
            {
                Debug.LogWarning("ExposureContrastController: No image overlay manager found");
            }
        }
        
        /// <summary>
        /// Sets up UI controls and event handlers
        /// </summary>
        private void SetupUI()
        {
            // Setup exposure slider
            if (exposureSlider != null)
            {
                exposureSlider.minValue = exposureMin;
                exposureSlider.maxValue = exposureMax;
                exposureSlider.value = 0f;
                exposureSlider.onValueChanged.AddListener(OnExposureSliderChanged);
                
                Debug.Log("ExposureContrastController: Exposure slider configured");
            }
            else
            {
                Debug.LogWarning("ExposureContrastController: No exposure slider assigned");
            }
            
            // Setup contrast slider
            if (contrastSlider != null)
            {
                contrastSlider.minValue = contrastMin;
                contrastSlider.maxValue = contrastMax;
                contrastSlider.value = 0f;
                contrastSlider.onValueChanged.AddListener(OnContrastSliderChanged);
                
                Debug.Log("ExposureContrastController: Contrast slider configured");
            }
            else
            {
                Debug.LogWarning("ExposureContrastController: No contrast slider assigned");
            }
            
            // Setup reset button
            if (resetButton != null)
            {
                resetButton.OnClicked.AddListener(ResetAdjustments);
                Debug.Log("ExposureContrastController: Reset button configured");
            }
            
            // Setup apply button
            if (applyButton != null)
            {
                applyButton.OnClicked.AddListener(ApplyAdjustments);
                Debug.Log("ExposureContrastController: Apply button configured");
            }
            
            // Update value displays
            UpdateValueDisplays();
            
            isInitialized = true;
        }
        
        /// <summary>
        /// Handles exposure slider value changes
        /// </summary>
        private void OnExposureSliderChanged(float value)
        {
            if (!isInitialized || Mathf.Abs(value - lastExposureValue) < updateThreshold)
            {
                return;
            }
            
            lastExposureValue = value;
            currentAdjustments.exposure = value;
            currentAdjustments.UpdateModifiedState();
            
            UpdateValueDisplays();
            OnExposureChanged?.Invoke(value);
            
            if (enableRealTimePreview)
            {
                ApplyAdjustmentsInternal();
            }
            
            Debug.Log($"ExposureContrastController: Exposure changed to {value:F2}");
        }
        
        /// <summary>
        /// Handles contrast slider value changes
        /// </summary>
        private void OnContrastSliderChanged(float value)
        {
            if (!isInitialized || Mathf.Abs(value - lastContrastValue) < updateThreshold)
            {
                return;
            }
            
            lastContrastValue = value;
            currentAdjustments.contrast = value;
            currentAdjustments.UpdateModifiedState();
            
            UpdateValueDisplays();
            OnContrastChanged?.Invoke(value);
            
            if (enableRealTimePreview)
            {
                ApplyAdjustmentsInternal();
            }
            
            Debug.Log($"ExposureContrastController: Contrast changed to {value:F2}");
        }
        
        /// <summary>
        /// Updates the value display texts
        /// </summary>
        private void UpdateValueDisplays()
        {
            if (exposureValueText != null)
            {
                exposureValueText.text = $"Exposure: {currentAdjustments.exposure:F2}";
            }
            
            if (contrastValueText != null)
            {
                contrastValueText.text = $"Contrast: {currentAdjustments.contrast:F2}";
            }
        }
        
        /// <summary>
        /// Applies current adjustments to the image
        /// </summary>
        public void ApplyAdjustments()
        {
            ApplyAdjustmentsInternal();
            OnAdjustmentsApplied?.Invoke(currentAdjustments);
            
            Debug.Log("ExposureContrastController: Applied adjustments");
        }
        
        /// <summary>
        /// Internal method to apply adjustments
        /// </summary>
        private void ApplyAdjustmentsInternal()
        {
            if (imageOverlay != null)
            {
                // Merge with existing adjustments
                ImageAdjustments mergedAdjustments = imageOverlay.CurrentAdjustments ?? new ImageAdjustments();
                mergedAdjustments.exposure = currentAdjustments.exposure;
                mergedAdjustments.contrast = currentAdjustments.contrast;
                mergedAdjustments.UpdateModifiedState();
                
                imageOverlay.ApplyAdjustments(mergedAdjustments);
            }
            else if (adjustmentProcessor != null)
            {
                adjustmentProcessor.ApplyAdjustments(currentAdjustments);
            }
        }
        
        /// <summary>
        /// Resets all adjustments to default values
        /// </summary>
        public void ResetAdjustments()
        {
            currentAdjustments.exposure = 0f;
            currentAdjustments.contrast = 0f;
            currentAdjustments.UpdateModifiedState();
            
            // Update UI controls
            if (exposureSlider != null)
            {
                exposureSlider.value = 0f;
            }
            
            if (contrastSlider != null)
            {
                contrastSlider.value = 0f;
            }
            
            UpdateValueDisplays();
            
            // Apply reset adjustments
            ApplyAdjustmentsInternal();
            
            OnAdjustmentsReset?.Invoke();
            
            Debug.Log("ExposureContrastController: Reset adjustments");
        }
        
        /// <summary>
        /// Sets exposure value programmatically
        /// </summary>
        public void SetExposure(float exposure)
        {
            exposure = Mathf.Clamp(exposure, exposureMin, exposureMax);
            
            currentAdjustments.exposure = exposure;
            currentAdjustments.UpdateModifiedState();
            
            if (exposureSlider != null)
            {
                exposureSlider.value = exposure;
            }
            
            UpdateValueDisplays();
            
            if (enableRealTimePreview)
            {
                ApplyAdjustmentsInternal();
            }
        }
        
        /// <summary>
        /// Sets contrast value programmatically
        /// </summary>
        public void SetContrast(float contrast)
        {
            contrast = Mathf.Clamp(contrast, contrastMin, contrastMax);
            
            currentAdjustments.contrast = contrast;
            currentAdjustments.UpdateModifiedState();
            
            if (contrastSlider != null)
            {
                contrastSlider.value = contrast;
            }
            
            UpdateValueDisplays();
            
            if (enableRealTimePreview)
            {
                ApplyAdjustmentsInternal();
            }
        }
        
        /// <summary>
        /// Sets both exposure and contrast values
        /// </summary>
        public void SetAdjustments(float exposure, float contrast)
        {
            exposure = Mathf.Clamp(exposure, exposureMin, exposureMax);
            contrast = Mathf.Clamp(contrast, contrastMin, contrastMax);
            
            currentAdjustments.exposure = exposure;
            currentAdjustments.contrast = contrast;
            currentAdjustments.UpdateModifiedState();
            
            if (exposureSlider != null)
            {
                exposureSlider.value = exposure;
            }
            
            if (contrastSlider != null)
            {
                contrastSlider.value = contrast;
            }
            
            UpdateValueDisplays();
            
            if (enableRealTimePreview)
            {
                ApplyAdjustmentsInternal();
            }
        }
        
        /// <summary>
        /// Enables or disables real-time preview
        /// </summary>
        public void SetRealTimePreview(bool enabled)
        {
            enableRealTimePreview = enabled;
            
            if (enabled)
            {
                ApplyAdjustmentsInternal();
            }
        }
        
        /// <summary>
        /// Handles image loaded events
        /// </summary>
        private void OnImageLoaded(Texture2D image)
        {
            // Reset adjustments when new image is loaded
            ResetAdjustments();
            
            Debug.Log("ExposureContrastController: Image loaded, reset adjustments");
        }
        
        /// <summary>
        /// Handles image adjustments applied events
        /// </summary>
        private void OnImageAdjustmentsApplied(ImageAdjustments adjustments)
        {
            if (adjustments != null)
            {
                // Update UI to reflect applied adjustments
                currentAdjustments.exposure = adjustments.exposure;
                currentAdjustments.contrast = adjustments.contrast;
                
                if (exposureSlider != null)
                {
                    exposureSlider.value = adjustments.exposure;
                }
                
                if (contrastSlider != null)
                {
                    contrastSlider.value = adjustments.contrast;
                }
                
                UpdateValueDisplays();
            }
        }
        
        /// <summary>
        /// Validates adjustment values are within acceptable ranges
        /// </summary>
        public bool ValidateAdjustments()
        {
            bool exposureValid = currentAdjustments.exposure >= exposureMin && currentAdjustments.exposure <= exposureMax;
            bool contrastValid = currentAdjustments.contrast >= contrastMin && currentAdjustments.contrast <= contrastMax;
            
            return exposureValid && contrastValid;
        }
        
        /// <summary>
        /// Gets the current adjustment values as a copy
        /// </summary>
        public ImageAdjustments GetCurrentAdjustments()
        {
            ImageAdjustments copy = new ImageAdjustments();
            copy.exposure = currentAdjustments.exposure;
            copy.contrast = currentAdjustments.contrast;
            copy.UpdateModifiedState();
            
            return copy;
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (imageOverlay != null)
            {
                imageOverlay.OnImageLoaded -= OnImageLoaded;
                imageOverlay.OnAdjustmentsApplied -= OnImageAdjustmentsApplied;
            }
            
            // Clean up UI event subscriptions
            if (exposureSlider != null)
            {
                exposureSlider.onValueChanged.RemoveListener(OnExposureSliderChanged);
            }
            
            if (contrastSlider != null)
            {
                contrastSlider.onValueChanged.RemoveListener(OnContrastSliderChanged);
            }
            
            if (resetButton != null)
            {
                resetButton.OnClicked.RemoveListener(ResetAdjustments);
            }
            
            if (applyButton != null)
            {
                applyButton.OnClicked.RemoveListener(ApplyAdjustments);
            }
        }
    }
}