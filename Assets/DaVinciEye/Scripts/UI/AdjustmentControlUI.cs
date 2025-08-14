using System;
using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;
using DaVinciEye.ImageOverlay;
using DaVinciEye.Filters;

namespace DaVinciEye.UI
{
    /// <summary>
    /// UI controller for image adjustment controls (opacity, contrast, exposure, hue, saturation)
    /// Uses MRTK CanvasSlider.prefab components for real-time adjustment
    /// Implements requirements 3.1, 4.6, 6.4, 6.5, 6.6, 6.7
    /// </summary>
    public class AdjustmentControlUI : MonoBehaviour
    {
        [Header("MRTK Slider References")]
        [SerializeField] private Slider opacitySlider;
        [SerializeField] private Slider contrastSlider;
        [SerializeField] private Slider exposureSlider;
        [SerializeField] private Slider hueSlider;
        [SerializeField] private Slider saturationSlider;
        
        [Header("Slider Configuration")]
        [SerializeField] private float opacityMin = 0f;
        [SerializeField] private float opacityMax = 1f;
        [SerializeField] private float contrastMin = -1f;
        [SerializeField] private float contrastMax = 1f;
        [SerializeField] private float exposureMin = -2f;
        [SerializeField] private float exposureMax = 2f;
        [SerializeField] private float hueMin = -180f;
        [SerializeField] private float hueMax = 180f;
        [SerializeField] private float saturationMin = -1f;
        [SerializeField] private float saturationMax = 1f;
        
        [Header("Value Display")]
        [SerializeField] private Text opacityValueText;
        [SerializeField] private Text contrastValueText;
        [SerializeField] private Text exposureValueText;
        [SerializeField] private Text hueValueText;
        [SerializeField] private Text saturationValueText;
        
        [Header("Reset Buttons")]
        [SerializeField] private PressableButton resetOpacityButton;
        [SerializeField] private PressableButton resetContrastButton;
        [SerializeField] private PressableButton resetExposureButton;
        [SerializeField] private PressableButton resetHueButton;
        [SerializeField] private PressableButton resetSaturationButton;
        [SerializeField] private PressableButton resetAllButton;
        
        [Header("System Integration")]
        [SerializeField] private bool autoConnectToSystems = true;
        [SerializeField] private bool showRealTimePreview = true;
        
        // System references
        private IImageOverlay imageOverlay;
        private IFilterProcessor filterProcessor;
        private UIManager uiManager;
        
        // Current adjustment values
        private ImageAdjustments currentAdjustments;
        
        // Events
        public event Action<ImageAdjustments> OnAdjustmentsChanged;
        public event Action<string, float> OnAdjustmentValueChanged;
        public event Action OnAdjustmentsReset;
        
        private void Awake()
        {
            InitializeAdjustments();
        }
        
        private void Start()
        {
            SetupSliders();
            SetupButtons();
            ConnectToSystems();
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            CleanupEventHandlers();
        }
        
        private void InitializeAdjustments()
        {
            currentAdjustments = new ImageAdjustments
            {
                opacity = 1f,
                contrast = 0f,
                exposure = 0f,
                hue = 0f,
                saturation = 0f,
                isModified = false
            };
        }
        
        private void SetupSliders()
        {
            // Configure opacity slider
            if (opacitySlider != null)
            {
                opacitySlider.minValue = opacityMin;
                opacitySlider.maxValue = opacityMax;
                opacitySlider.value = currentAdjustments.opacity;
                opacitySlider.onValueChanged.AddListener(OnOpacityChanged);
            }
            
            // Configure contrast slider
            if (contrastSlider != null)
            {
                contrastSlider.minValue = contrastMin;
                contrastSlider.maxValue = contrastMax;
                contrastSlider.value = currentAdjustments.contrast;
                contrastSlider.onValueChanged.AddListener(OnContrastChanged);
            }
            
            // Configure exposure slider
            if (exposureSlider != null)
            {
                exposureSlider.minValue = exposureMin;
                exposureSlider.maxValue = exposureMax;
                exposureSlider.value = currentAdjustments.exposure;
                exposureSlider.onValueChanged.AddListener(OnExposureChanged);
            }
            
            // Configure hue slider
            if (hueSlider != null)
            {
                hueSlider.minValue = hueMin;
                hueSlider.maxValue = hueMax;
                hueSlider.value = currentAdjustments.hue;
                hueSlider.onValueChanged.AddListener(OnHueChanged);
            }
            
            // Configure saturation slider
            if (saturationSlider != null)
            {
                saturationSlider.minValue = saturationMin;
                saturationSlider.maxValue = saturationMax;
                saturationSlider.value = currentAdjustments.saturation;
                saturationSlider.onValueChanged.AddListener(OnSaturationChanged);
            }
            
            Debug.Log("AdjustmentControlUI: All sliders configured");
        }
        
        private void SetupButtons()
        {
            // Setup individual reset buttons
            if (resetOpacityButton != null)
            {
                resetOpacityButton.OnClicked.AddListener(() => ResetAdjustment("opacity"));
            }
            
            if (resetContrastButton != null)
            {
                resetContrastButton.OnClicked.AddListener(() => ResetAdjustment("contrast"));
            }
            
            if (resetExposureButton != null)
            {
                resetExposureButton.OnClicked.AddListener(() => ResetAdjustment("exposure"));
            }
            
            if (resetHueButton != null)
            {
                resetHueButton.OnClicked.AddListener(() => ResetAdjustment("hue"));
            }
            
            if (resetSaturationButton != null)
            {
                resetSaturationButton.OnClicked.AddListener(() => ResetAdjustment("saturation"));
            }
            
            // Setup reset all button
            if (resetAllButton != null)
            {
                resetAllButton.OnClicked.AddListener(ResetAllAdjustments);
            }
            
            Debug.Log("AdjustmentControlUI: Reset buttons configured");
        }
        
        private void ConnectToSystems()
        {
            if (!autoConnectToSystems) return;
            
            // Find UI manager
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                imageOverlay = uiManager.ImageOverlay;
                filterProcessor = uiManager.FilterProcessor;
            }
            
            // Setup system event handlers
            if (imageOverlay != null)
            {
                imageOverlay.OnImageLoaded += OnImageLoaded;
                imageOverlay.OnOpacityChanged += OnSystemOpacityChanged;
            }
            
            Debug.Log("AdjustmentControlUI: Connected to systems");
        }
        
        private void CleanupEventHandlers()
        {
            // Cleanup slider events
            if (opacitySlider != null)
                opacitySlider.onValueChanged.RemoveAllListeners();
            if (contrastSlider != null)
                contrastSlider.onValueChanged.RemoveAllListeners();
            if (exposureSlider != null)
                exposureSlider.onValueChanged.RemoveAllListeners();
            if (hueSlider != null)
                hueSlider.onValueChanged.RemoveAllListeners();
            if (saturationSlider != null)
                saturationSlider.onValueChanged.RemoveAllListeners();
            
            // Cleanup button events
            if (resetOpacityButton != null)
                resetOpacityButton.OnClicked.RemoveAllListeners();
            if (resetContrastButton != null)
                resetContrastButton.OnClicked.RemoveAllListeners();
            if (resetExposureButton != null)
                resetExposureButton.OnClicked.RemoveAllListeners();
            if (resetHueButton != null)
                resetHueButton.OnClicked.RemoveAllListeners();
            if (resetSaturationButton != null)
                resetSaturationButton.OnClicked.RemoveAllListeners();
            if (resetAllButton != null)
                resetAllButton.OnClicked.RemoveAllListeners();
            
            // Cleanup system events
            if (imageOverlay != null)
            {
                imageOverlay.OnImageLoaded -= OnImageLoaded;
                imageOverlay.OnOpacityChanged -= OnSystemOpacityChanged;
            }
        }
        
        // Slider event handlers
        private void OnOpacityChanged(float value)
        {
            currentAdjustments.opacity = value;
            currentAdjustments.isModified = true;
            
            UpdateValueDisplay("opacity", value);
            ApplyAdjustments();
            
            OnAdjustmentValueChanged?.Invoke("opacity", value);
        }
        
        private void OnContrastChanged(float value)
        {
            currentAdjustments.contrast = value;
            currentAdjustments.isModified = true;
            
            UpdateValueDisplay("contrast", value);
            ApplyAdjustments();
            
            OnAdjustmentValueChanged?.Invoke("contrast", value);
        }
        
        private void OnExposureChanged(float value)
        {
            currentAdjustments.exposure = value;
            currentAdjustments.isModified = true;
            
            UpdateValueDisplay("exposure", value);
            ApplyAdjustments();
            
            OnAdjustmentValueChanged?.Invoke("exposure", value);
        }
        
        private void OnHueChanged(float value)
        {
            currentAdjustments.hue = value;
            currentAdjustments.isModified = true;
            
            UpdateValueDisplay("hue", value);
            ApplyAdjustments();
            
            OnAdjustmentValueChanged?.Invoke("hue", value);
        }
        
        private void OnSaturationChanged(float value)
        {
            currentAdjustments.saturation = value;
            currentAdjustments.isModified = true;
            
            UpdateValueDisplay("saturation", value);
            ApplyAdjustments();
            
            OnAdjustmentValueChanged?.Invoke("saturation", value);
        }
        
        private void ApplyAdjustments()
        {
            if (!showRealTimePreview) return;
            
            try
            {
                // Apply opacity directly to image overlay
                if (imageOverlay != null)
                {
                    imageOverlay.SetOpacity(currentAdjustments.opacity);
                }
                
                // Apply other adjustments through image overlay system
                if (imageOverlay != null)
                {
                    imageOverlay.ApplyAdjustments(currentAdjustments);
                }
                
                OnAdjustmentsChanged?.Invoke(currentAdjustments);
            }
            catch (Exception ex)
            {
                Debug.LogError($"AdjustmentControlUI: Failed to apply adjustments - {ex.Message}");
            }
        }
        
        private void UpdateValueDisplay(string adjustmentType, float value)
        {
            Text targetText = adjustmentType switch
            {
                "opacity" => opacityValueText,
                "contrast" => contrastValueText,
                "exposure" => exposureValueText,
                "hue" => hueValueText,
                "saturation" => saturationValueText,
                _ => null
            };
            
            if (targetText != null)
            {
                string formattedValue = adjustmentType switch
                {
                    "opacity" => $"{value:P0}",  // Percentage
                    "hue" => $"{value:F0}°",     // Degrees
                    _ => $"{value:F2}"           // Decimal
                };
                
                targetText.text = formattedValue;
            }
        }
        
        private void ResetAdjustment(string adjustmentType)
        {
            switch (adjustmentType)
            {
                case "opacity":
                    if (opacitySlider != null)
                    {
                        opacitySlider.value = 1f;
                    }
                    break;
                    
                case "contrast":
                    if (contrastSlider != null)
                    {
                        contrastSlider.value = 0f;
                    }
                    break;
                    
                case "exposure":
                    if (exposureSlider != null)
                    {
                        exposureSlider.value = 0f;
                    }
                    break;
                    
                case "hue":
                    if (hueSlider != null)
                    {
                        hueSlider.value = 0f;
                    }
                    break;
                    
                case "saturation":
                    if (saturationSlider != null)
                    {
                        saturationSlider.value = 0f;
                    }
                    break;
            }
            
            Debug.Log($"AdjustmentControlUI: Reset {adjustmentType} adjustment");
        }
        
        private void ResetAllAdjustments()
        {
            // Reset all sliders to default values
            ResetAdjustment("opacity");
            ResetAdjustment("contrast");
            ResetAdjustment("exposure");
            ResetAdjustment("hue");
            ResetAdjustment("saturation");
            
            OnAdjustmentsReset?.Invoke();
            
            Debug.Log("AdjustmentControlUI: All adjustments reset");
        }
        
        // System event handlers
        private void OnImageLoaded(Texture2D image)
        {
            // Reset adjustments when new image is loaded
            ResetAllAdjustments();
            Debug.Log("AdjustmentControlUI: Adjustments reset for new image");
        }
        
        private void OnSystemOpacityChanged(float opacity)
        {
            // Update UI when opacity is changed externally
            if (opacitySlider != null && Mathf.Abs(opacitySlider.value - opacity) > 0.01f)
            {
                opacitySlider.value = opacity;
            }
        }
        
        // Public methods
        public void SetAdjustments(ImageAdjustments adjustments)
        {
            currentAdjustments = adjustments;
            UpdateUI();
        }
        
        public void UpdateUI()
        {
            // Update all sliders to match current adjustments
            if (opacitySlider != null)
                opacitySlider.value = currentAdjustments.opacity;
            if (contrastSlider != null)
                contrastSlider.value = currentAdjustments.contrast;
            if (exposureSlider != null)
                exposureSlider.value = currentAdjustments.exposure;
            if (hueSlider != null)
                hueSlider.value = currentAdjustments.hue;
            if (saturationSlider != null)
                saturationSlider.value = currentAdjustments.saturation;
            
            // Update value displays
            UpdateValueDisplay("opacity", currentAdjustments.opacity);
            UpdateValueDisplay("contrast", currentAdjustments.contrast);
            UpdateValueDisplay("exposure", currentAdjustments.exposure);
            UpdateValueDisplay("hue", currentAdjustments.hue);
            UpdateValueDisplay("saturation", currentAdjustments.saturation);
        }
        
        public void SetRealTimePreview(bool enabled)
        {
            showRealTimePreview = enabled;
        }
        
        // Properties
        public ImageAdjustments CurrentAdjustments => currentAdjustments;
        public bool IsModified => currentAdjustments.isModified;
        public bool RealTimePreviewEnabled => showRealTimePreview;
    }
    
    /// <summary>
    /// Data structure for image adjustments
    /// Matches the interface expected by IImageOverlay.ApplyAdjustments()
    /// </summary>
    [System.Serializable]
    public class ImageAdjustments
    {
        public float opacity = 1f;
        public float contrast = 0f;
        public float exposure = 0f;
        public float hue = 0f;
        public float saturation = 0f;
        public bool isModified = false;
        
        public void Reset()
        {
            opacity = 1f;
            contrast = 0f;
            exposure = 0f;
            hue = 0f;
            saturation = 0f;
            isModified = false;
        }
        
        public ImageAdjustments Clone()
        {
            return new ImageAdjustments
            {
                opacity = this.opacity,
                contrast = this.contrast,
                exposure = this.exposure,
                hue = this.hue,
                saturation = this.saturation,
                isModified = this.isModified
            };
        }
    }
}