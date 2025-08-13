using System;
using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// UI controls for color reduction filtering
    /// Implements adjustable color count controls (Requirements 4.2.4, 4.2.5)
    /// </summary>
    public class ColorReductionUI : MonoBehaviour
    {
        [Header("MRTK UI Components")]
        [SerializeField] private Slider colorCountSlider;
        [SerializeField] private Text colorCountText;
        [SerializeField] private PressableButton applyButton;
        [SerializeField] private PressableButton resetButton;
        
        [Header("Method Selection")]
        [SerializeField] private Toggle kMeansToggle;
        [SerializeField] private Toggle medianCutToggle;
        [SerializeField] private Toggle uniformToggle;
        [SerializeField] private Toggle popularityToggle;
        
        [Header("Advanced Options")]
        [SerializeField] private Toggle preserveDominantToggle;
        [SerializeField] private Toggle useDitheringToggle;
        [SerializeField] private Slider ditheringStrengthSlider;
        [SerializeField] private Text ditheringStrengthText;
        
        [Header("Palette Display")]
        [SerializeField] private Transform paletteContainer;
        [SerializeField] private GameObject colorSwatchPrefab;
        [SerializeField] private Text statisticsText;
        
        [Header("Target Components")]
        [SerializeField] private ColorReductionFilter colorReductionFilter;
        [SerializeField] private FilterManager filterManager;
        
        // Current settings
        private int currentColorCount = 16;
        private ColorReductionFilter.ColorQuantizationMethod currentMethod = ColorReductionFilter.ColorQuantizationMethod.KMeans;
        
        // Events
        public event Action<int> OnColorCountChanged;
        public event Action<ColorReductionFilter.ColorQuantizationMethod> OnMethodChanged;
        public event Action OnFilterApplied;
        
        private void Awake()
        {
            InitializeUI();
        }
        
        private void Start()
        {
            SetupUICallbacks();
            UpdateUI();
        }
        
        /// <summary>
        /// Initialize UI components and find references
        /// </summary>
        private void InitializeUI()
        {
            // Find components if not assigned
            if (colorReductionFilter == null)
            {
                colorReductionFilter = FindObjectOfType<ColorReductionFilter>();
            }
            
            if (filterManager == null)
            {
                filterManager = FindObjectOfType<FilterManager>();
            }
            
            Debug.Log("ColorReductionUI: Initialized color reduction UI controls");
        }
        
        /// <summary>
        /// Setup UI callbacks and event handlers
        /// </summary>
        private void SetupUICallbacks()
        {
            // Color count slider
            if (colorCountSlider != null)
            {
                colorCountSlider.minValue = 2f;
                colorCountSlider.maxValue = 256f;
                colorCountSlider.value = currentColorCount;
                colorCountSlider.onValueChanged.AddListener(OnColorCountSliderChanged);
            }
            
            // Method selection toggles
            if (kMeansToggle != null)
            {
                kMeansToggle.onValueChanged.AddListener((isOn) => {
                    if (isOn) OnMethodSelected(ColorReductionFilter.ColorQuantizationMethod.KMeans);
                });
            }
            
            if (medianCutToggle != null)
            {
                medianCutToggle.onValueChanged.AddListener((isOn) => {
                    if (isOn) OnMethodSelected(ColorReductionFilter.ColorQuantizationMethod.MedianCut);
                });
            }
            
            if (uniformToggle != null)
            {
                uniformToggle.onValueChanged.AddListener((isOn) => {
                    if (isOn) OnMethodSelected(ColorReductionFilter.ColorQuantizationMethod.Uniform);
                });
            }
            
            if (popularityToggle != null)
            {
                popularityToggle.onValueChanged.AddListener((isOn) => {
                    if (isOn) OnMethodSelected(ColorReductionFilter.ColorQuantizationMethod.Popularity);
                });
            }
            
            // Dithering controls
            if (useDitheringToggle != null)
            {
                useDitheringToggle.onValueChanged.AddListener(OnDitheringToggleChanged);
            }
            
            if (ditheringStrengthSlider != null)
            {
                ditheringStrengthSlider.minValue = 0f;
                ditheringStrengthSlider.maxValue = 1f;
                ditheringStrengthSlider.value = 0.5f;
                ditheringStrengthSlider.onValueChanged.AddListener(OnDitheringStrengthChanged);
            }
            
            // Buttons
            if (applyButton != null)
            {
                applyButton.OnClicked.AddListener(ApplyColorReduction);
            }
            
            if (resetButton != null)
            {
                resetButton.OnClicked.AddListener(ResetToDefaults);
            }
            
            // Subscribe to filter events
            if (colorReductionFilter != null)
            {
                colorReductionFilter.OnPaletteGenerated += OnPaletteGenerated;
                colorReductionFilter.OnStatisticsUpdated += OnStatisticsUpdated;
            }
            
            Debug.Log("ColorReductionUI: Setup UI callbacks complete");
        }
        
        /// <summary>
        /// Update UI to reflect current settings
        /// </summary>
        private void UpdateUI()
        {
            // Update color count display
            if (colorCountText != null)
            {
                colorCountText.text = $"Colors: {currentColorCount}";
            }
            
            if (colorCountSlider != null)
            {
                colorCountSlider.value = currentColorCount;
            }
            
            // Update method toggles
            UpdateMethodToggles();
            
            // Update dithering display
            if (ditheringStrengthText != null && ditheringStrengthSlider != null)
            {
                ditheringStrengthText.text = $"Dithering: {ditheringStrengthSlider.value:F2}";
            }
        }
        
        /// <summary>
        /// Update method selection toggles
        /// </summary>
        private void UpdateMethodToggles()
        {
            if (kMeansToggle != null)
                kMeansToggle.isOn = (currentMethod == ColorReductionFilter.ColorQuantizationMethod.KMeans);
            
            if (medianCutToggle != null)
                medianCutToggle.isOn = (currentMethod == ColorReductionFilter.ColorQuantizationMethod.MedianCut);
            
            if (uniformToggle != null)
                uniformToggle.isOn = (currentMethod == ColorReductionFilter.ColorQuantizationMethod.Uniform);
            
            if (popularityToggle != null)
                popularityToggle.isOn = (currentMethod == ColorReductionFilter.ColorQuantizationMethod.Popularity);
        }
        
        /// <summary>
        /// Handle color count slider changes
        /// Implements real-time color count adjustment (Requirement 4.2.4)
        /// </summary>
        private void OnColorCountSliderChanged(float value)
        {
            currentColorCount = Mathf.RoundToInt(value);
            
            if (colorCountText != null)
            {
                colorCountText.text = $"Colors: {currentColorCount}";
            }
            
            // Update filter if real-time preview is enabled
            if (colorReductionFilter != null)
            {
                colorReductionFilter.TargetColorCount = currentColorCount;
                
                if (colorReductionFilter.RealTimePreview)
                {
                    ApplyColorReduction();
                }
            }
            
            OnColorCountChanged?.Invoke(currentColorCount);
            
            Debug.Log($"ColorReductionUI: Color count changed to {currentColorCount}");
        }
        
        /// <summary>
        /// Handle quantization method selection
        /// </summary>
        private void OnMethodSelected(ColorReductionFilter.ColorQuantizationMethod method)
        {
            currentMethod = method;
            
            if (colorReductionFilter != null)
            {
                colorReductionFilter.QuantizationMethod = method;
                
                if (colorReductionFilter.RealTimePreview)
                {
                    ApplyColorReduction();
                }
            }
            
            OnMethodChanged?.Invoke(method);
            
            Debug.Log($"ColorReductionUI: Quantization method changed to {method}");
        }
        
        /// <summary>
        /// Handle dithering toggle changes
        /// </summary>
        private void OnDitheringToggleChanged(bool useDithering)
        {
            // Enable/disable dithering strength slider
            if (ditheringStrengthSlider != null)
            {
                ditheringStrengthSlider.interactable = useDithering;
            }
            
            Debug.Log($"ColorReductionUI: Dithering {(useDithering ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Handle dithering strength changes
        /// </summary>
        private void OnDitheringStrengthChanged(float strength)
        {
            if (ditheringStrengthText != null)
            {
                ditheringStrengthText.text = $"Dithering: {strength:F2}";
            }
            
            Debug.Log($"ColorReductionUI: Dithering strength changed to {strength:F2}");
        }
        
        /// <summary>
        /// Apply color reduction filter
        /// </summary>
        private void ApplyColorReduction()
        {
            if (filterManager != null)
            {
                // Apply through FilterManager
                var parameters = new FilterParameters(FilterType.ColorReduction);
                parameters.targetColorCount = currentColorCount;
                parameters.intensity = 1f;
                
                // Add method-specific parameters
                parameters.customParameters["method"] = (float)currentMethod;
                parameters.customParameters["preserveDominant"] = preserveDominantToggle?.isOn == true ? 1f : 0f;
                parameters.customParameters["useDithering"] = useDitheringToggle?.isOn == true ? 1f : 0f;
                parameters.customParameters["ditheringStrength"] = ditheringStrengthSlider?.value ?? 0.5f;
                
                filterManager.ApplyFilter(FilterType.ColorReduction, parameters);
            }
            else if (colorReductionFilter != null)
            {
                // Apply directly to filter
                // This would need a source texture - in practice this comes from the FilterManager
                Debug.Log("ColorReductionUI: Applied color reduction filter directly");
            }
            
            OnFilterApplied?.Invoke();
        }
        
        /// <summary>
        /// Reset all settings to defaults
        /// </summary>
        private void ResetToDefaults()
        {
            currentColorCount = 16;
            currentMethod = ColorReductionFilter.ColorQuantizationMethod.KMeans;
            
            if (colorCountSlider != null)
                colorCountSlider.value = currentColorCount;
            
            if (kMeansToggle != null)
                kMeansToggle.isOn = true;
            
            if (preserveDominantToggle != null)
                preserveDominantToggle.isOn = true;
            
            if (useDitheringToggle != null)
                useDitheringToggle.isOn = false;
            
            if (ditheringStrengthSlider != null)
                ditheringStrengthSlider.value = 0.5f;
            
            UpdateUI();
            
            Debug.Log("ColorReductionUI: Reset to default settings");
        }
        
        /// <summary>
        /// Handle palette generation event
        /// </summary>
        private void OnPaletteGenerated(ColorReductionFilter.QuantizedPalette palette)
        {
            DisplayPalette(palette);
        }
        
        /// <summary>
        /// Display the generated color palette
        /// </summary>
        private void DisplayPalette(ColorReductionFilter.QuantizedPalette palette)
        {
            if (paletteContainer == null || colorSwatchPrefab == null || palette == null)
                return;
            
            // Clear existing swatches
            foreach (Transform child in paletteContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create new swatches
            for (int i = 0; i < palette.colors.Length; i++)
            {
                GameObject swatch = Instantiate(colorSwatchPrefab, paletteContainer);
                
                // Set swatch color
                var swatchImage = swatch.GetComponent<Image>();
                if (swatchImage != null)
                {
                    swatchImage.color = palette.colors[i];
                }
                
                // Set weight text if available
                var swatchText = swatch.GetComponentInChildren<Text>();
                if (swatchText != null)
                {
                    swatchText.text = $"{palette.weights[i]:P1}";
                }
            }
            
            Debug.Log($"ColorReductionUI: Displayed palette with {palette.colors.Length} colors");
        }
        
        /// <summary>
        /// Handle statistics update event
        /// </summary>
        private void OnStatisticsUpdated(ColorReductionFilter.ColorReductionStatistics stats)
        {
            if (statisticsText != null)
            {
                statisticsText.text = $"Original: {stats.originalColorCount} colors\n" +
                                    $"Reduced: {stats.reducedColorCount} colors\n" +
                                    $"Compression: {stats.compressionRatio:P1}\n" +
                                    $"Method: {stats.method}\n" +
                                    $"Time: {stats.processingTime * 1000f:F1}ms";
            }
            
            Debug.Log($"ColorReductionUI: Updated statistics - {stats}");
        }
        
        /// <summary>
        /// Set color count programmatically
        /// </summary>
        public void SetColorCount(int colorCount)
        {
            currentColorCount = Mathf.Clamp(colorCount, 2, 256);
            
            if (colorCountSlider != null)
            {
                colorCountSlider.value = currentColorCount;
            }
            
            UpdateUI();
        }
        
        /// <summary>
        /// Set quantization method programmatically
        /// </summary>
        public void SetQuantizationMethod(ColorReductionFilter.ColorQuantizationMethod method)
        {
            currentMethod = method;
            UpdateMethodToggles();
        }
        
        /// <summary>
        /// Get current color count
        /// </summary>
        public int GetColorCount()
        {
            return currentColorCount;
        }
        
        /// <summary>
        /// Get current quantization method
        /// </summary>
        public ColorReductionFilter.ColorQuantizationMethod GetQuantizationMethod()
        {
            return currentMethod;
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (colorReductionFilter != null)
            {
                colorReductionFilter.OnPaletteGenerated -= OnPaletteGenerated;
                colorReductionFilter.OnStatisticsUpdated -= OnStatisticsUpdated;
            }
        }
    }
}