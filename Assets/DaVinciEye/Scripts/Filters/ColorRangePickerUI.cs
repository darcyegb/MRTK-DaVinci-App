using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// Interactive color range picker UI using MRTK components
    /// Implements Requirements: 4.1.1, 4.1.2, 4.1.3
    /// </summary>
    public class ColorRangePickerUI : MonoBehaviour
    {
        [Header("MRTK UI Components")]
        [SerializeField] private PressableButton addRangeButton;
        [SerializeField] private PressableButton removeRangeButton;
        [SerializeField] private PressableButton clearAllButton;
        [SerializeField] private PressableButton applyButton;
        
        [Header("HSV Sliders")]
        [SerializeField] private Slider hueMinSlider;
        [SerializeField] private Slider hueMaxSlider;
        [SerializeField] private Slider saturationMinSlider;
        [SerializeField] private Slider saturationMaxSlider;
        [SerializeField] private Slider valueMinSlider;
        [SerializeField] private Slider valueMaxSlider;
        
        [Header("Display Options")]
        [SerializeField] private Toggle showOriginalColorsToggle;
        [SerializeField] private Toggle showAsHighlightToggle;
        [SerializeField] private Slider highlightIntensitySlider;
        
        [Header("Color Preview")]
        [SerializeField] private Image colorPreviewImage;
        [SerializeField] private Image rangePreviewImage;
        [SerializeField] private Text rangeInfoText;
        
        [Header("Range Management")]
        [SerializeField] private Transform rangeListParent;
        [SerializeField] private GameObject rangeItemPrefab;
        [SerializeField] private ScrollView rangeScrollView;
        
        [Header("Target Components")]
        [SerializeField] private ColorRangeFilter colorRangeFilter;
        [SerializeField] private FilterManager filterManager;
        
        // Current range being edited
        private ColorRangeFilter.ColorRangeSettings currentEditingRange;
        private List<GameObject> rangeUIItems = new List<GameObject>();
        
        // Events
        public event Action<ColorRangeFilter.ColorRangeSettings> OnRangeCreated;
        public event Action<int> OnRangeRemoved;
        public event Action OnRangesCleared;
        
        private void Awake()
        {
            InitializeUI();
        }
        
        private void Start()
        {
            SetupUICallbacks();
            CreateDefaultRange();
        }
        
        /// <summary>
        /// Initialize UI components and default values
        /// </summary>
        private void InitializeUI()
        {
            // Find components if not assigned
            if (colorRangeFilter == null)
            {
                colorRangeFilter = FindObjectOfType<ColorRangeFilter>();
            }
            
            if (filterManager == null)
            {
                filterManager = FindObjectOfType<FilterManager>();
            }
            
            // Initialize current editing range
            currentEditingRange = new ColorRangeFilter.ColorRangeSettings();
            currentEditingRange.rangeName = "New Range";
            
            Debug.Log("ColorRangePickerUI: Initialized interactive color range picker");
        }
        
        /// <summary>
        /// Setup UI callbacks and event handlers
        /// </summary>
        private void SetupUICallbacks()
        {
            // Button callbacks
            if (addRangeButton != null)
                addRangeButton.OnClicked.AddListener(AddCurrentRange);
            
            if (removeRangeButton != null)
                removeRangeButton.OnClicked.AddListener(RemoveSelectedRange);
            
            if (clearAllButton != null)
                clearAllButton.OnClicked.AddListener(ClearAllRanges);
            
            if (applyButton != null)
                applyButton.OnClicked.AddListener(ApplyColorRangeFilter);
            
            // Slider callbacks for real-time preview
            if (hueMinSlider != null)
            {
                hueMinSlider.minValue = 0f;
                hueMinSlider.maxValue = 360f;
                hueMinSlider.onValueChanged.AddListener(OnHueMinChanged);
            }
            
            if (hueMaxSlider != null)
            {
                hueMaxSlider.minValue = 0f;
                hueMaxSlider.maxValue = 360f;
                hueMaxSlider.onValueChanged.AddListener(OnHueMaxChanged);
            }
            
            if (saturationMinSlider != null)
            {
                saturationMinSlider.minValue = 0f;
                saturationMinSlider.maxValue = 1f;
                saturationMinSlider.onValueChanged.AddListener(OnSaturationMinChanged);
            }
            
            if (saturationMaxSlider != null)
            {
                saturationMaxSlider.minValue = 0f;
                saturationMaxSlider.maxValue = 1f;
                saturationMaxSlider.onValueChanged.AddListener(OnSaturationMaxChanged);
            }
            
            if (valueMinSlider != null)
            {
                valueMinSlider.minValue = 0f;
                valueMinSlider.maxValue = 1f;
                valueMinSlider.onValueChanged.AddListener(OnValueMinChanged);
            }
            
            if (valueMaxSlider != null)
            {
                valueMaxSlider.minValue = 0f;
                valueMaxSlider.maxValue = 1f;
                valueMaxSlider.onValueChanged.AddListener(OnValueMaxChanged);
            }
            
            // Toggle callbacks
            if (showOriginalColorsToggle != null)
                showOriginalColorsToggle.onValueChanged.AddListener(OnShowOriginalColorsChanged);
            
            if (showAsHighlightToggle != null)
                showAsHighlightToggle.onValueChanged.AddListener(OnShowAsHighlightChanged);
            
            if (highlightIntensitySlider != null)
            {
                highlightIntensitySlider.minValue = 0f;
                highlightIntensitySlider.maxValue = 1f;
                highlightIntensitySlider.onValueChanged.AddListener(OnHighlightIntensityChanged);
            }
            
            Debug.Log("ColorRangePickerUI: Setup UI callbacks complete");
        }
        
        /// <summary>
        /// Create a default color range for initial setup
        /// </summary>
        private void CreateDefaultRange()
        {
            // Set default values
            if (hueMinSlider != null) hueMinSlider.value = 0f;
            if (hueMaxSlider != null) hueMaxSlider.value = 360f;
            if (saturationMinSlider != null) saturationMinSlider.value = 0f;
            if (saturationMaxSlider != null) saturationMaxSlider.value = 1f;
            if (valueMinSlider != null) valueMinSlider.value = 0f;
            if (valueMaxSlider != null) valueMaxSlider.value = 1f;
            
            if (showOriginalColorsToggle != null) showOriginalColorsToggle.isOn = true;
            if (showAsHighlightToggle != null) showAsHighlightToggle.isOn = false;
            if (highlightIntensitySlider != null) highlightIntensitySlider.value = 0.5f;
            
            UpdateCurrentRange();
            UpdatePreview();
        }
        
        /// <summary>
        /// Set color range from a target color with tolerance
        /// Convenient method for quick color selection
        /// </summary>
        public void SetRangeFromColor(Color targetColor, float tolerance = 0.2f)
        {
            // Convert RGB to HSV
            Color.RGBToHSV(targetColor, out float h, out float s, out float v);
            
            // Calculate range based on tolerance
            float hueRange = tolerance * 360f;
            float satRange = tolerance;
            float valueRange = tolerance;
            
            float hueMin = (h * 360f - hueRange / 2f + 360f) % 360f;
            float hueMax = (h * 360f + hueRange / 2f) % 360f;
            float satMin = Mathf.Clamp01(s - satRange / 2f);
            float satMax = Mathf.Clamp01(s + satRange / 2f);
            float valueMin = Mathf.Clamp01(v - valueRange / 2f);
            float valueMax = Mathf.Clamp01(v + valueRange / 2f);
            
            // Update sliders
            if (hueMinSlider != null) hueMinSlider.value = hueMin;
            if (hueMaxSlider != null) hueMaxSlider.value = hueMax;
            if (saturationMinSlider != null) saturationMinSlider.value = satMin;
            if (saturationMaxSlider != null) saturationMaxSlider.value = satMax;
            if (valueMinSlider != null) valueMinSlider.value = valueMin;
            if (valueMaxSlider != null) valueMaxSlider.value = valueMax;
            
            UpdateCurrentRange();
            UpdatePreview();
            
            Debug.Log($"ColorRangePickerUI: Set range from color {targetColor} with tolerance {tolerance}");
        }
        
        /// <summary>
        /// Update the current editing range with slider values
        /// </summary>
        private void UpdateCurrentRange()
        {
            if (currentEditingRange == null)
                currentEditingRange = new ColorRangeFilter.ColorRangeSettings();
            
            currentEditingRange.hueMin = hueMinSlider?.value ?? 0f;
            currentEditingRange.hueMax = hueMaxSlider?.value ?? 360f;
            currentEditingRange.saturationMin = saturationMinSlider?.value ?? 0f;
            currentEditingRange.saturationMax = saturationMaxSlider?.value ?? 1f;
            currentEditingRange.valueMin = valueMinSlider?.value ?? 0f;
            currentEditingRange.valueMax = valueMaxSlider?.value ?? 1f;
            
            currentEditingRange.showOriginalColors = showOriginalColorsToggle?.isOn ?? true;
            currentEditingRange.showAsHighlight = showAsHighlightToggle?.isOn ?? false;
            currentEditingRange.highlightIntensity = highlightIntensitySlider?.value ?? 0.5f;
            
            // Update color range filter with current range
            if (colorRangeFilter != null)
            {
                colorRangeFilter.SetColorRange(
                    currentEditingRange.hueMin,
                    currentEditingRange.hueMax,
                    currentEditingRange.saturationMin,
                    currentEditingRange.saturationMax,
                    currentEditingRange.valueMin,
                    currentEditingRange.valueMax
                );
            }
        }
        
        /// <summary>
        /// Update the visual preview of the current range
        /// </summary>
        private void UpdatePreview()
        {
            if (currentEditingRange == null) return;
            
            // Update color preview (show center color of range)
            if (colorPreviewImage != null)
            {
                float centerHue = (currentEditingRange.hueMin + currentEditingRange.hueMax) / 2f / 360f;
                float centerSat = (currentEditingRange.saturationMin + currentEditingRange.saturationMax) / 2f;
                float centerVal = (currentEditingRange.valueMin + currentEditingRange.valueMax) / 2f;
                
                Color centerColor = Color.HSVToRGB(centerHue, centerSat, centerVal);
                colorPreviewImage.color = centerColor;
            }
            
            // Update range info text
            if (rangeInfoText != null)
            {
                rangeInfoText.text = $"H: {currentEditingRange.hueMin:F0}-{currentEditingRange.hueMax:F0}°\n" +
                                   $"S: {currentEditingRange.saturationMin:F2}-{currentEditingRange.saturationMax:F2}\n" +
                                   $"V: {currentEditingRange.valueMin:F2}-{currentEditingRange.valueMax:F2}";
            }
        }
        
        // Slider callback methods
        private void OnHueMinChanged(float value)
        {
            UpdateCurrentRange();
            UpdatePreview();
        }
        
        private void OnHueMaxChanged(float value)
        {
            UpdateCurrentRange();
            UpdatePreview();
        }
        
        private void OnSaturationMinChanged(float value)
        {
            UpdateCurrentRange();
            UpdatePreview();
        }
        
        private void OnSaturationMaxChanged(float value)
        {
            UpdateCurrentRange();
            UpdatePreview();
        }
        
        private void OnValueMinChanged(float value)
        {
            UpdateCurrentRange();
            UpdatePreview();
        }
        
        private void OnValueMaxChanged(float value)
        {
            UpdateCurrentRange();
            UpdatePreview();
        }
        
        private void OnShowOriginalColorsChanged(bool value)
        {
            if (currentEditingRange != null)
            {
                currentEditingRange.showOriginalColors = value;
                if (value && showAsHighlightToggle != null)
                {
                    showAsHighlightToggle.isOn = false;
                }
            }
        }
        
        private void OnShowAsHighlightChanged(bool value)
        {
            if (currentEditingRange != null)
            {
                currentEditingRange.showAsHighlight = value;
                if (value && showOriginalColorsToggle != null)
                {
                    showOriginalColorsToggle.isOn = false;
                }
            }
        }
        
        private void OnHighlightIntensityChanged(float value)
        {
            if (currentEditingRange != null)
            {
                currentEditingRange.highlightIntensity = value;
            }
        }
        
        /// <summary>
        /// Add the current range to the active ranges list
        /// </summary>
        private void AddCurrentRange()
        {
            if (currentEditingRange == null || colorRangeFilter == null)
            {
                Debug.LogWarning("ColorRangePickerUI: Cannot add range - missing components");
                return;
            }
            
            // Create a copy of the current range
            var newRange = new ColorRangeFilter.ColorRangeSettings();
            newRange.hueMin = currentEditingRange.hueMin;
            newRange.hueMax = currentEditingRange.hueMax;
            newRange.saturationMin = currentEditingRange.saturationMin;
            newRange.saturationMax = currentEditingRange.saturationMax;
            newRange.valueMin = currentEditingRange.valueMin;
            newRange.valueMax = currentEditingRange.valueMax;
            newRange.showOriginalColors = currentEditingRange.showOriginalColors;
            newRange.showAsHighlight = currentEditingRange.showAsHighlight;
            newRange.highlightIntensity = currentEditingRange.highlightIntensity;
            newRange.rangeName = $"Range {colorRangeFilter.ActiveRanges.Count + 1}";
            
            // Add to color range filter
            colorRangeFilter.AddColorRange(newRange);
            
            // Create UI item for the range
            CreateRangeUIItem(newRange);
            
            OnRangeCreated?.Invoke(newRange);
            
            Debug.Log($"ColorRangePickerUI: Added new color range '{newRange.rangeName}'");
        }
        
        /// <summary>
        /// Remove the currently selected range
        /// </summary>
        private void RemoveSelectedRange()
        {
            // For now, remove the last added range
            // In a full implementation, this would remove the selected range from the list
            if (colorRangeFilter != null && colorRangeFilter.ActiveRanges.Count > 0)
            {
                var lastRange = colorRangeFilter.ActiveRanges[colorRangeFilter.ActiveRanges.Count - 1];
                colorRangeFilter.RemoveColorRange(lastRange.rangeId);
                
                // Remove corresponding UI item
                if (rangeUIItems.Count > 0)
                {
                    var lastUIItem = rangeUIItems[rangeUIItems.Count - 1];
                    rangeUIItems.RemoveAt(rangeUIItems.Count - 1);
                    Destroy(lastUIItem);
                }
                
                OnRangeRemoved?.Invoke(lastRange.rangeId);
                
                Debug.Log($"ColorRangePickerUI: Removed color range '{lastRange.rangeName}'");
            }
        }
        
        /// <summary>
        /// Clear all active ranges
        /// </summary>
        private void ClearAllRanges()
        {
            if (colorRangeFilter != null)
            {
                colorRangeFilter.ClearAllRanges();
            }
            
            // Clear UI items
            foreach (var uiItem in rangeUIItems)
            {
                if (uiItem != null)
                    Destroy(uiItem);
            }
            rangeUIItems.Clear();
            
            OnRangesCleared?.Invoke();
            
            Debug.Log("ColorRangePickerUI: Cleared all color ranges");
        }
        
        /// <summary>
        /// Apply the color range filter through the filter manager
        /// </summary>
        private void ApplyColorRangeFilter()
        {
            if (filterManager != null)
            {
                var parameters = new FilterParameters(FilterType.ColorRange);
                parameters.intensity = 1f;
                parameters.colorTolerance = 0.2f;
                
                filterManager.ApplyFilter(FilterType.ColorRange, parameters);
                
                Debug.Log("ColorRangePickerUI: Applied color range filter through FilterManager");
            }
            else if (colorRangeFilter != null)
            {
                colorRangeFilter.ApplyColorRangeFilter();
                
                Debug.Log("ColorRangePickerUI: Applied color range filter directly");
            }
        }
        
        /// <summary>
        /// Create a UI item for a color range in the list
        /// </summary>
        private void CreateRangeUIItem(ColorRangeFilter.ColorRangeSettings range)
        {
            if (rangeItemPrefab == null || rangeListParent == null)
            {
                Debug.LogWarning("ColorRangePickerUI: Missing prefab or parent for range UI item");
                return;
            }
            
            GameObject rangeItem = Instantiate(rangeItemPrefab, rangeListParent);
            rangeUIItems.Add(rangeItem);
            
            // Setup the range item (this would be more detailed in a full implementation)
            var rangeText = rangeItem.GetComponentInChildren<Text>();
            if (rangeText != null)
            {
                rangeText.text = $"{range.rangeName}\nH:{range.hueMin:F0}-{range.hueMax:F0}°";
            }
            
            // Add toggle functionality
            var rangeToggle = rangeItem.GetComponentInChildren<Toggle>();
            if (rangeToggle != null)
            {
                rangeToggle.isOn = range.isActive;
                rangeToggle.onValueChanged.AddListener((isOn) => {
                    colorRangeFilter?.ToggleColorRange(range.rangeId, isOn);
                });
            }
        }
        
        /// <summary>
        /// Get the current range settings for external use
        /// </summary>
        public ColorRangeFilter.ColorRangeSettings GetCurrentRange()
        {
            return currentEditingRange;
        }
        
        /// <summary>
        /// Load a range into the editor
        /// </summary>
        public void LoadRange(ColorRangeFilter.ColorRangeSettings range)
        {
            if (range == null) return;
            
            currentEditingRange = range;
            
            // Update sliders
            if (hueMinSlider != null) hueMinSlider.value = range.hueMin;
            if (hueMaxSlider != null) hueMaxSlider.value = range.hueMax;
            if (saturationMinSlider != null) saturationMinSlider.value = range.saturationMin;
            if (saturationMaxSlider != null) saturationMaxSlider.value = range.saturationMax;
            if (valueMinSlider != null) valueMinSlider.value = range.valueMin;
            if (valueMaxSlider != null) valueMaxSlider.value = range.valueMax;
            
            // Update toggles
            if (showOriginalColorsToggle != null) showOriginalColorsToggle.isOn = range.showOriginalColors;
            if (showAsHighlightToggle != null) showAsHighlightToggle.isOn = range.showAsHighlight;
            if (highlightIntensitySlider != null) highlightIntensitySlider.value = range.highlightIntensity;
            
            UpdatePreview();
            
            Debug.Log($"ColorRangePickerUI: Loaded range '{range.rangeName}' into editor");
        }
    }
}