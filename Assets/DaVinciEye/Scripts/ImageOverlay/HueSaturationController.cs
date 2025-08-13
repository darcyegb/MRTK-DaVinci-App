using System;
using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;

namespace DaVinciEye.ImageOverlay
{
    /// <summary>
    /// Controls hue and saturation adjustments with MRTK UI integration
    /// Provides real-time HSV color space manipulation and preview
    /// </summary>
    public class HueSaturationController : MonoBehaviour
    {
        [Header("UI Controls")]
        [SerializeField] private Slider hueSlider;
        [SerializeField] private Slider saturationSlider;
        [SerializeField] private PressableButton resetButton;
        [SerializeField] private PressableButton applyButton;
        
        [Header("Value Display")]
        [SerializeField] private TMPro.TextMeshProUGUI hueValueText;
        [SerializeField] private TMPro.TextMeshProUGUI saturationValueText;
        
        [Header("Color Preview")]
        [SerializeField] private Image colorPreviewImage;
        [SerializeField] private RawImage hueWheelImage;
        [SerializeField] private Material hueWheelMaterial;
        
        [Header("Settings")]
        [SerializeField] private float hueMin = -180f;
        [SerializeField] private float hueMax = 180f;
        [SerializeField] private float saturationMin = -1f;
        [SerializeField] private float saturationMax = 1f;
        [SerializeField] private bool enableRealTimePreview = true;
        [SerializeField] private float updateThreshold = 0.01f;
        [SerializeField] private bool showColorWheel = true;
        
        // Private fields
        private ImageOverlayManager imageOverlay;
        private ImageAdjustmentProcessor adjustmentProcessor;
        private ImageAdjustments currentAdjustments;
        private float lastHueValue;
        private float lastSaturationValue;
        private bool isInitialized = false;
        private Texture2D hueWheelTexture;
        
        // Events
        public event Action<float> OnHueChanged;
        public event Action<float> OnSaturationChanged;
        public event Action<ImageAdjustments> OnAdjustmentsApplied;
        public event Action OnAdjustmentsReset;
        
        // Properties
        public float HueValue => currentAdjustments?.hue ?? 0f;
        public float SaturationValue => currentAdjustments?.saturation ?? 0f;
        public bool HasAdjustments => currentAdjustments != null && 
            (Mathf.Abs(currentAdjustments.hue) > updateThreshold || 
             Mathf.Abs(currentAdjustments.saturation) > updateThreshold);
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            SetupComponents();
            SetupUI();
            CreateHueWheel();
        }
        
        /// <summary>
        /// Initializes the hue saturation controller
        /// </summary>
        private void Initialize()
        {
            currentAdjustments = new ImageAdjustments();
            lastHueValue = 0f;
            lastSaturationValue = 0f;
            
            Debug.Log("HueSaturationController: Initialized");
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
                
                Debug.Log("HueSaturationController: Connected to image overlay");
            }
            else
            {
                Debug.LogWarning("HueSaturationController: No image overlay manager found");
            }
        }
        
        /// <summary>
        /// Sets up UI controls and event handlers
        /// </summary>
        private void SetupUI()
        {
            // Setup hue slider
            if (hueSlider != null)
            {
                hueSlider.minValue = hueMin;
                hueSlider.maxValue = hueMax;
                hueSlider.value = 0f;
                hueSlider.onValueChanged.AddListener(OnHueSliderChanged);
                
                Debug.Log("HueSaturationController: Hue slider configured");
            }
            else
            {
                Debug.LogWarning("HueSaturationController: No hue slider assigned");
            }
            
            // Setup saturation slider
            if (saturationSlider != null)
            {
                saturationSlider.minValue = saturationMin;
                saturationSlider.maxValue = saturationMax;
                saturationSlider.value = 0f;
                saturationSlider.onValueChanged.AddListener(OnSaturationSliderChanged);
                
                Debug.Log("HueSaturationController: Saturation slider configured");
            }
            else
            {
                Debug.LogWarning("HueSaturationController: No saturation slider assigned");
            }
            
            // Setup reset button
            if (resetButton != null)
            {
                resetButton.OnClicked.AddListener(ResetAdjustments);
                Debug.Log("HueSaturationController: Reset button configured");
            }
            
            // Setup apply button
            if (applyButton != null)
            {
                applyButton.OnClicked.AddListener(ApplyAdjustments);
                Debug.Log("HueSaturationController: Apply button configured");
            }
            
            // Update value displays
            UpdateValueDisplays();
            UpdateColorPreview();
            
            isInitialized = true;
        }
        
        /// <summary>
        /// Creates a hue wheel texture for visual reference
        /// </summary>
        private void CreateHueWheel()
        {
            if (!showColorWheel || hueWheelImage == null)
            {
                return;
            }
            
            int size = 128;
            hueWheelTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.4f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    Vector2 offset = pos - center;
                    float distance = offset.magnitude;
                    
                    if (distance <= radius)
                    {
                        // Calculate hue from angle
                        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
                        if (angle < 0) angle += 360f;
                        float hue = angle / 360f;
                        
                        // Calculate saturation from distance
                        float saturation = distance / radius;
                        
                        // Create color
                        Color color = Color.HSVToRGB(hue, saturation, 1f);
                        pixels[y * size + x] = color;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            
            hueWheelTexture.SetPixels(pixels);
            hueWheelTexture.Apply();
            
            hueWheelImage.texture = hueWheelTexture;
            
            Debug.Log("HueSaturationController: Created hue wheel texture");
        }
        
        /// <summary>
        /// Handles hue slider value changes
        /// </summary>
        private void OnHueSliderChanged(float value)
        {
            if (!isInitialized || Mathf.Abs(value - lastHueValue) < updateThreshold)
            {
                return;
            }
            
            lastHueValue = value;
            currentAdjustments.hue = value;
            currentAdjustments.UpdateModifiedState();
            
            UpdateValueDisplays();
            UpdateColorPreview();
            OnHueChanged?.Invoke(value);
            
            if (enableRealTimePreview)
            {
                ApplyAdjustmentsInternal();
            }
            
            Debug.Log($"HueSaturationController: Hue changed to {value:F1}°");
        }
        
        /// <summary>
        /// Handles saturation slider value changes
        /// </summary>
        private void OnSaturationSliderChanged(float value)
        {
            if (!isInitialized || Mathf.Abs(value - lastSaturationValue) < updateThreshold)
            {
                return;
            }
            
            lastSaturationValue = value;
            currentAdjustments.saturation = value;
            currentAdjustments.UpdateModifiedState();
            
            UpdateValueDisplays();
            UpdateColorPreview();
            OnSaturationChanged?.Invoke(value);
            
            if (enableRealTimePreview)
            {
                ApplyAdjustmentsInternal();
            }
            
            Debug.Log($"HueSaturationController: Saturation changed to {value:F2}");
        }
        
        /// <summary>
        /// Updates the value display texts
        /// </summary>
        private void UpdateValueDisplays()
        {
            if (hueValueText != null)
            {
                hueValueText.text = $"Hue: {currentAdjustments.hue:F0}°";
            }
            
            if (saturationValueText != null)
            {
                saturationValueText.text = $"Saturation: {currentAdjustments.saturation:F2}";
            }
        }
        
        /// <summary>
        /// Updates the color preview display
        /// </summary>
        private void UpdateColorPreview()
        {
            if (colorPreviewImage == null)
            {
                return;
            }
            
            // Create a preview color showing the hue and saturation adjustment
            float normalizedHue = (currentAdjustments.hue + 180f) / 360f; // Convert to 0-1 range
            float adjustedSaturation = Mathf.Clamp01(0.5f + currentAdjustments.saturation); // Base saturation + adjustment
            
            Color previewColor = Color.HSVToRGB(normalizedHue, adjustedSaturation, 1f);
            colorPreviewImage.color = previewColor;
        }
        
        /// <summary>
        /// Applies current adjustments to the image
        /// </summary>
        public void ApplyAdjustments()
        {
            ApplyAdjustmentsInternal();
            OnAdjustmentsApplied?.Invoke(currentAdjustments);
            
            Debug.Log("HueSaturationController: Applied adjustments");
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
                mergedAdjustments.hue = currentAdjustments.hue;
                mergedAdjustments.saturation = currentAdjustments.saturation;
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
            currentAdjustments.hue = 0f;
            currentAdjustments.saturation = 0f;
            currentAdjustments.UpdateModifiedState();
            
            // Update UI controls
            if (hueSlider != null)
            {
                hueSlider.value = 0f;
            }
            
            if (saturationSlider != null)
            {
                saturationSlider.value = 0f;
            }
            
            UpdateValueDisplays();
            UpdateColorPreview();
            
            // Apply reset adjustments
            ApplyAdjustmentsInternal();
            
            OnAdjustmentsReset?.Invoke();
            
            Debug.Log("HueSaturationController: Reset adjustments");
        }
        
        /// <summary>
        /// Sets hue value programmatically
        /// </summary>
        public void SetHue(float hue)
        {
            hue = Mathf.Clamp(hue, hueMin, hueMax);
            
            currentAdjustments.hue = hue;
            currentAdjustments.UpdateModifiedState();
            
            if (hueSlider != null)
            {
                hueSlider.value = hue;
            }
            
            UpdateValueDisplays();
            UpdateColorPreview();
            
            if (enableRealTimePreview)
            {
                ApplyAdjustmentsInternal();
            }
        }
        
        /// <summary>
        /// Sets saturation value programmatically
        /// </summary>
        public void SetSaturation(float saturation)
        {
            saturation = Mathf.Clamp(saturation, saturationMin, saturationMax);
            
            currentAdjustments.saturation = saturation;
            currentAdjustments.UpdateModifiedState();
            
            if (saturationSlider != null)
            {
                saturationSlider.value = saturation;
            }
            
            UpdateValueDisplays();
            UpdateColorPreview();
            
            if (enableRealTimePreview)
            {
                ApplyAdjustmentsInternal();
            }
        }
        
        /// <summary>
        /// Sets both hue and saturation values
        /// </summary>
        public void SetAdjustments(float hue, float saturation)
        {
            hue = Mathf.Clamp(hue, hueMin, hueMax);
            saturation = Mathf.Clamp(saturation, saturationMin, saturationMax);
            
            currentAdjustments.hue = hue;
            currentAdjustments.saturation = saturation;
            currentAdjustments.UpdateModifiedState();
            
            if (hueSlider != null)
            {
                hueSlider.value = hue;
            }
            
            if (saturationSlider != null)
            {
                saturationSlider.value = saturation;
            }
            
            UpdateValueDisplays();
            UpdateColorPreview();
            
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
        /// Converts RGB color to HSV and applies current adjustments
        /// </summary>
        public Color ApplyAdjustmentsToColor(Color inputColor)
        {
            // Convert to HSV
            Color.RGBToHSV(inputColor, out float h, out float s, out float v);
            
            // Apply hue adjustment
            if (Mathf.Abs(currentAdjustments.hue) > updateThreshold)
            {
                h += currentAdjustments.hue / 360f; // Convert degrees to 0-1 range
                h = h - Mathf.Floor(h); // Wrap around
            }
            
            // Apply saturation adjustment
            if (Mathf.Abs(currentAdjustments.saturation) > updateThreshold)
            {
                s = Mathf.Clamp01(s + currentAdjustments.saturation);
            }
            
            // Convert back to RGB
            return Color.HSVToRGB(h, s, v);
        }
        
        /// <summary>
        /// Gets the hue shift in normalized range (0-1)
        /// </summary>
        public float GetNormalizedHue()
        {
            return (currentAdjustments.hue + 180f) / 360f;
        }
        
        /// <summary>
        /// Gets the saturation adjustment in normalized range (0-1)
        /// </summary>
        public float GetNormalizedSaturation()
        {
            return (currentAdjustments.saturation + 1f) / 2f;
        }
        
        /// <summary>
        /// Handles image loaded events
        /// </summary>
        private void OnImageLoaded(Texture2D image)
        {
            // Reset adjustments when new image is loaded
            ResetAdjustments();
            
            Debug.Log("HueSaturationController: Image loaded, reset adjustments");
        }
        
        /// <summary>
        /// Handles image adjustments applied events
        /// </summary>
        private void OnImageAdjustmentsApplied(ImageAdjustments adjustments)
        {
            if (adjustments != null)
            {
                // Update UI to reflect applied adjustments
                currentAdjustments.hue = adjustments.hue;
                currentAdjustments.saturation = adjustments.saturation;
                
                if (hueSlider != null)
                {
                    hueSlider.value = adjustments.hue;
                }
                
                if (saturationSlider != null)
                {
                    saturationSlider.value = adjustments.saturation;
                }
                
                UpdateValueDisplays();
                UpdateColorPreview();
            }
        }
        
        /// <summary>
        /// Validates adjustment values are within acceptable ranges
        /// </summary>
        public bool ValidateAdjustments()
        {
            bool hueValid = currentAdjustments.hue >= hueMin && currentAdjustments.hue <= hueMax;
            bool saturationValid = currentAdjustments.saturation >= saturationMin && currentAdjustments.saturation <= saturationMax;
            
            return hueValid && saturationValid;
        }
        
        /// <summary>
        /// Gets the current adjustment values as a copy
        /// </summary>
        public ImageAdjustments GetCurrentAdjustments()
        {
            ImageAdjustments copy = new ImageAdjustments();
            copy.hue = currentAdjustments.hue;
            copy.saturation = currentAdjustments.saturation;
            copy.UpdateModifiedState();
            
            return copy;
        }
        
        /// <summary>
        /// Calculates color difference after applying adjustments
        /// </summary>
        public float CalculateColorDifference(Color originalColor, Color targetColor)
        {
            Color adjustedColor = ApplyAdjustmentsToColor(originalColor);
            
            // Calculate Euclidean distance in RGB space
            float deltaR = adjustedColor.r - targetColor.r;
            float deltaG = adjustedColor.g - targetColor.g;
            float deltaB = adjustedColor.b - targetColor.b;
            
            return Mathf.Sqrt(deltaR * deltaR + deltaG * deltaG + deltaB * deltaB);
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
            if (hueSlider != null)
            {
                hueSlider.onValueChanged.RemoveListener(OnHueSliderChanged);
            }
            
            if (saturationSlider != null)
            {
                saturationSlider.onValueChanged.RemoveListener(OnSaturationSliderChanged);
            }
            
            if (resetButton != null)
            {
                resetButton.OnClicked.RemoveListener(ResetAdjustments);
            }
            
            if (applyButton != null)
            {
                applyButton.OnClicked.RemoveListener(ApplyAdjustments);
            }
            
            // Clean up hue wheel texture
            if (hueWheelTexture != null)
            {
                DestroyImmediate(hueWheelTexture);
            }
        }
    }
}