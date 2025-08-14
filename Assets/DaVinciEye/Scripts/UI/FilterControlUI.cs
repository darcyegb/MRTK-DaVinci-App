using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;
using DaVinciEye.Filters;

namespace DaVinciEye.UI
{
    /// <summary>
    /// UI controller for filter selection and parameter adjustment
    /// Uses MRTK Checkbox.prefab and ToggleSwitch.prefab for filter on/off states
    /// Implements requirements 4.6 for filter selection interface
    /// </summary>
    public class FilterControlUI : MonoBehaviour
    {
        [Header("Filter Toggle Controls")]
        [SerializeField] private Toggle grayscaleToggle;
        [SerializeField] private Toggle edgeDetectionToggle;
        [SerializeField] private Toggle contrastEnhancementToggle;
        [SerializeField] private Toggle colorRangeToggle;
        [SerializeField] private Toggle colorReductionToggle;
        
        [Header("Filter Parameter Sliders")]
        [SerializeField] private Slider edgeDetectionIntensitySlider;
        [SerializeField] private Slider contrastEnhancementSlider;
        [SerializeField] private Slider colorRangeToleranceSlider;
        [SerializeField] private Slider colorReductionCountSlider;
        
        [Header("Parameter Value Displays")]
        [SerializeField] private Text edgeIntensityValueText;
        [SerializeField] private Text contrastEnhancementValueText;
        [SerializeField] private Text colorToleranceValueText;
        [SerializeField] private Text colorCountValueText;
        
        [Header("Filter Management Buttons")]
        [SerializeField] private PressableButton clearAllFiltersButton;
        [SerializeField] private PressableButton resetFilterParamsButton;
        [SerializeField] private PressableButton saveFilterPresetButton;
        [SerializeField] private PressableButton loadFilterPresetButton;
        
        [Header("Filter Preview")]
        [SerializeField] private RawImage filterPreviewImage;
        [SerializeField] private Toggle realTimePreviewToggle;
        
        [Header("System Integration")]
        [SerializeField] private bool autoConnectToSystems = true;
        [SerializeField] private bool showFilterLayering = true;
        
        // System references
        private IFilterProcessor filterProcessor;
        private UIManager uiManager;
        
        // Filter state tracking
        private Dictionary<FilterType, bool> activeFilters;
        private Dictionary<FilterType, FilterParameters> filterParameters;
        private bool realTimePreviewEnabled = true;
        
        // Events
        public event Action<FilterType, bool> OnFilterToggled;
        public event Action<FilterType, FilterParameters> OnFilterParametersChanged;
        public event Action OnAllFiltersCleared;
        public event Action<string> OnFilterPresetSaved;
        
        private void Awake()
        {
            InitializeFilterState();
        }
        
        private void Start()
        {
            SetupToggleControls();
            SetupParameterSliders();
            SetupManagementButtons();
            ConnectToSystems();
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            CleanupEventHandlers();
        }
        
        private void InitializeFilterState()
        {
            activeFilters = new Dictionary<FilterType, bool>
            {
                { FilterType.Grayscale, false },
                { FilterType.EdgeDetection, false },
                { FilterType.ContrastEnhancement, false },
                { FilterType.ColorRange, false },
                { FilterType.ColorReduction, false }
            };
            
            filterParameters = new Dictionary<FilterType, FilterParameters>
            {
                { FilterType.EdgeDetection, new FilterParameters { type = FilterType.EdgeDetection, intensity = 0.5f } },
                { FilterType.ContrastEnhancement, new FilterParameters { type = FilterType.ContrastEnhancement, intensity = 0.5f } },
                { FilterType.ColorRange, new FilterParameters { type = FilterType.ColorRange, colorTolerance = 0.1f } },
                { FilterType.ColorReduction, new FilterParameters { type = FilterType.ColorReduction, targetColorCount = 16 } }
            };
        }
        
        private void SetupToggleControls()
        {
            // Setup grayscale toggle
            if (grayscaleToggle != null)
            {
                grayscaleToggle.isOn = activeFilters[FilterType.Grayscale];
                grayscaleToggle.onValueChanged.AddListener((value) => OnFilterToggleChanged(FilterType.Grayscale, value));
            }
            
            // Setup edge detection toggle
            if (edgeDetectionToggle != null)
            {
                edgeDetectionToggle.isOn = activeFilters[FilterType.EdgeDetection];
                edgeDetectionToggle.onValueChanged.AddListener((value) => OnFilterToggleChanged(FilterType.EdgeDetection, value));
            }
            
            // Setup contrast enhancement toggle
            if (contrastEnhancementToggle != null)
            {
                contrastEnhancementToggle.isOn = activeFilters[FilterType.ContrastEnhancement];
                contrastEnhancementToggle.onValueChanged.AddListener((value) => OnFilterToggleChanged(FilterType.ContrastEnhancement, value));
            }
            
            // Setup color range toggle
            if (colorRangeToggle != null)
            {
                colorRangeToggle.isOn = activeFilters[FilterType.ColorRange];
                colorRangeToggle.onValueChanged.AddListener((value) => OnFilterToggleChanged(FilterType.ColorRange, value));
            }
            
            // Setup color reduction toggle
            if (colorReductionToggle != null)
            {
                colorReductionToggle.isOn = activeFilters[FilterType.ColorReduction];
                colorReductionToggle.onValueChanged.AddListener((value) => OnFilterToggleChanged(FilterType.ColorReduction, value));
            }
            
            // Setup real-time preview toggle
            if (realTimePreviewToggle != null)
            {
                realTimePreviewToggle.isOn = realTimePreviewEnabled;
                realTimePreviewToggle.onValueChanged.AddListener(OnRealTimePreviewToggled);
            }
            
            Debug.Log("FilterControlUI: Toggle controls configured");
        }
        
        private void SetupParameterSliders()
        {
            // Setup edge detection intensity slider
            if (edgeDetectionIntensitySlider != null)
            {
                edgeDetectionIntensitySlider.minValue = 0f;
                edgeDetectionIntensitySlider.maxValue = 1f;
                edgeDetectionIntensitySlider.value = filterParameters[FilterType.EdgeDetection].intensity;
                edgeDetectionIntensitySlider.onValueChanged.AddListener((value) => OnParameterChanged(FilterType.EdgeDetection, "intensity", value));
            }
            
            // Setup contrast enhancement slider
            if (contrastEnhancementSlider != null)
            {
                contrastEnhancementSlider.minValue = 0f;
                contrastEnhancementSlider.maxValue = 2f;
                contrastEnhancementSlider.value = filterParameters[FilterType.ContrastEnhancement].intensity;
                contrastEnhancementSlider.onValueChanged.AddListener((value) => OnParameterChanged(FilterType.ContrastEnhancement, "intensity", value));
            }
            
            // Setup color range tolerance slider
            if (colorRangeToleranceSlider != null)
            {
                colorRangeToleranceSlider.minValue = 0f;
                colorRangeToleranceSlider.maxValue = 1f;
                colorRangeToleranceSlider.value = filterParameters[FilterType.ColorRange].colorTolerance;
                colorRangeToleranceSlider.onValueChanged.AddListener((value) => OnParameterChanged(FilterType.ColorRange, "tolerance", value));
            }
            
            // Setup color reduction count slider
            if (colorReductionCountSlider != null)
            {
                colorReductionCountSlider.minValue = 2f;
                colorReductionCountSlider.maxValue = 256f;
                colorReductionCountSlider.value = filterParameters[FilterType.ColorReduction].targetColorCount;
                colorReductionCountSlider.onValueChanged.AddListener((value) => OnParameterChanged(FilterType.ColorReduction, "count", value));
            }
            
            Debug.Log("FilterControlUI: Parameter sliders configured");
        }
        
        private void SetupManagementButtons()
        {
            if (clearAllFiltersButton != null)
            {
                clearAllFiltersButton.OnClicked.AddListener(ClearAllFilters);
            }
            
            if (resetFilterParamsButton != null)
            {
                resetFilterParamsButton.OnClicked.AddListener(ResetFilterParameters);
            }
            
            if (saveFilterPresetButton != null)
            {
                saveFilterPresetButton.OnClicked.AddListener(SaveFilterPreset);
            }
            
            if (loadFilterPresetButton != null)
            {
                loadFilterPresetButton.OnClicked.AddListener(LoadFilterPreset);
            }
            
            Debug.Log("FilterControlUI: Management buttons configured");
        }
        
        private void ConnectToSystems()
        {
            if (!autoConnectToSystems) return;
            
            // Find UI manager
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                filterProcessor = uiManager.FilterProcessor;
            }
            
            // Setup system event handlers
            if (filterProcessor != null)
            {
                filterProcessor.OnFilterApplied += OnSystemFilterApplied;
                filterProcessor.OnFilterRemoved += OnSystemFilterRemoved;
            }
            
            Debug.Log("FilterControlUI: Connected to filter processor system");
        }
        
        private void CleanupEventHandlers()
        {
            // Cleanup toggle events
            if (grayscaleToggle != null)
                grayscaleToggle.onValueChanged.RemoveAllListeners();
            if (edgeDetectionToggle != null)
                edgeDetectionToggle.onValueChanged.RemoveAllListeners();
            if (contrastEnhancementToggle != null)
                contrastEnhancementToggle.onValueChanged.RemoveAllListeners();
            if (colorRangeToggle != null)
                colorRangeToggle.onValueChanged.RemoveAllListeners();
            if (colorReductionToggle != null)
                colorReductionToggle.onValueChanged.RemoveAllListeners();
            if (realTimePreviewToggle != null)
                realTimePreviewToggle.onValueChanged.RemoveAllListeners();
            
            // Cleanup slider events
            if (edgeDetectionIntensitySlider != null)
                edgeDetectionIntensitySlider.onValueChanged.RemoveAllListeners();
            if (contrastEnhancementSlider != null)
                contrastEnhancementSlider.onValueChanged.RemoveAllListeners();
            if (colorRangeToleranceSlider != null)
                colorRangeToleranceSlider.onValueChanged.RemoveAllListeners();
            if (colorReductionCountSlider != null)
                colorReductionCountSlider.onValueChanged.RemoveAllListeners();
            
            // Cleanup button events
            if (clearAllFiltersButton != null)
                clearAllFiltersButton.OnClicked.RemoveAllListeners();
            if (resetFilterParamsButton != null)
                resetFilterParamsButton.OnClicked.RemoveAllListeners();
            if (saveFilterPresetButton != null)
                saveFilterPresetButton.OnClicked.RemoveAllListeners();
            if (loadFilterPresetButton != null)
                loadFilterPresetButton.OnClicked.RemoveAllListeners();
            
            // Cleanup system events
            if (filterProcessor != null)
            {
                filterProcessor.OnFilterApplied -= OnSystemFilterApplied;
                filterProcessor.OnFilterRemoved -= OnSystemFilterRemoved;
            }
        }
        
        // Event handlers
        private void OnFilterToggleChanged(FilterType filterType, bool isEnabled)
        {
            activeFilters[filterType] = isEnabled;
            
            try
            {
                if (isEnabled)
                {
                    // Apply filter with current parameters
                    if (filterProcessor != null && filterParameters.ContainsKey(filterType))
                    {
                        filterProcessor.ApplyFilter(filterType, filterParameters[filterType]);
                    }
                }
                else
                {
                    // Remove filter
                    if (filterProcessor != null)
                    {
                        filterProcessor.RemoveFilter(filterType);
                    }
                }
                
                OnFilterToggled?.Invoke(filterType, isEnabled);
                UpdateFilterPreview();
                
                Debug.Log($"FilterControlUI: {filterType} filter {(isEnabled ? "enabled" : "disabled")}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"FilterControlUI: Failed to toggle {filterType} filter - {ex.Message}");
            }
        }
        
        private void OnParameterChanged(FilterType filterType, string parameterName, float value)
        {
            if (!filterParameters.ContainsKey(filterType)) return;
            
            var parameters = filterParameters[filterType];
            
            switch (parameterName)
            {
                case "intensity":
                    parameters.intensity = value;
                    break;
                case "tolerance":
                    parameters.colorTolerance = value;
                    break;
                case "count":
                    parameters.targetColorCount = Mathf.RoundToInt(value);
                    break;
            }
            
            filterParameters[filterType] = parameters;
            
            // Update value display
            UpdateParameterValueDisplay(filterType, parameterName, value);
            
            // Apply updated parameters if filter is active
            if (activeFilters[filterType] && filterProcessor != null)
            {
                try
                {
                    filterProcessor.UpdateFilterParameters(filterType, parameters);
                    OnFilterParametersChanged?.Invoke(filterType, parameters);
                    UpdateFilterPreview();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"FilterControlUI: Failed to update {filterType} parameters - {ex.Message}");
                }
            }
        }
        
        private void OnRealTimePreviewToggled(bool enabled)
        {
            realTimePreviewEnabled = enabled;
            
            if (enabled)
            {
                UpdateFilterPreview();
            }
            else if (filterPreviewImage != null)
            {
                filterPreviewImage.texture = null;
            }
        }
        
        private void UpdateParameterValueDisplay(FilterType filterType, string parameterName, float value)
        {
            Text targetText = null;
            string formattedValue = "";
            
            switch (filterType)
            {
                case FilterType.EdgeDetection when parameterName == "intensity":
                    targetText = edgeIntensityValueText;
                    formattedValue = $"{value:P0}";
                    break;
                    
                case FilterType.ContrastEnhancement when parameterName == "intensity":
                    targetText = contrastEnhancementValueText;
                    formattedValue = $"{value:F2}x";
                    break;
                    
                case FilterType.ColorRange when parameterName == "tolerance":
                    targetText = colorToleranceValueText;
                    formattedValue = $"{value:P0}";
                    break;
                    
                case FilterType.ColorReduction when parameterName == "count":
                    targetText = colorCountValueText;
                    formattedValue = $"{Mathf.RoundToInt(value)}";
                    break;
            }
            
            if (targetText != null)
            {
                targetText.text = formattedValue;
            }
        }
        
        private void ClearAllFilters()
        {
            try
            {
                // Clear all active filters
                foreach (var filterType in activeFilters.Keys)
                {
                    if (activeFilters[filterType])
                    {
                        activeFilters[filterType] = false;
                        
                        if (filterProcessor != null)
                        {
                            filterProcessor.RemoveFilter(filterType);
                        }
                    }
                }
                
                // Update UI
                UpdateUI();
                UpdateFilterPreview();
                
                OnAllFiltersCleared?.Invoke();
                
                Debug.Log("FilterControlUI: All filters cleared");
            }
            catch (Exception ex)
            {
                Debug.LogError($"FilterControlUI: Failed to clear all filters - {ex.Message}");
            }
        }
        
        private void ResetFilterParameters()
        {
            // Reset all parameters to defaults
            InitializeFilterState();
            
            // Update UI
            SetupParameterSliders();
            UpdateUI();
            
            // Reapply active filters with reset parameters
            foreach (var kvp in activeFilters)
            {
                if (kvp.Value && filterProcessor != null && filterParameters.ContainsKey(kvp.Key))
                {
                    filterProcessor.UpdateFilterParameters(kvp.Key, filterParameters[kvp.Key]);
                }
            }
            
            UpdateFilterPreview();
            
            Debug.Log("FilterControlUI: Filter parameters reset to defaults");
        }
        
        private void SaveFilterPreset()
        {
            // Create preset data
            var presetData = new FilterPresetData
            {
                name = $"Preset_{System.DateTime.Now:yyyyMMdd_HHmmss}",
                activeFilters = new Dictionary<FilterType, bool>(activeFilters),
                filterParameters = new Dictionary<FilterType, FilterParameters>(filterParameters)
            };
            
            // Save preset (implementation would depend on persistence system)
            string presetJson = JsonUtility.ToJson(presetData);
            PlayerPrefs.SetString($"FilterPreset_{presetData.name}", presetJson);
            
            OnFilterPresetSaved?.Invoke(presetData.name);
            
            Debug.Log($"FilterControlUI: Filter preset saved as {presetData.name}");
        }
        
        private void LoadFilterPreset()
        {
            // For now, load the most recent preset
            // In a full implementation, this would show a selection UI
            string[] presetKeys = new string[0]; // Would get from PlayerPrefs or file system
            
            if (presetKeys.Length > 0)
            {
                string presetJson = PlayerPrefs.GetString(presetKeys[0]);
                var presetData = JsonUtility.FromJson<FilterPresetData>(presetJson);
                
                if (presetData != null)
                {
                    activeFilters = presetData.activeFilters;
                    filterParameters = presetData.filterParameters;
                    
                    UpdateUI();
                    ApplyLoadedPreset();
                    
                    Debug.Log($"FilterControlUI: Filter preset {presetData.name} loaded");
                }
            }
            else
            {
                Debug.LogWarning("FilterControlUI: No filter presets found");
            }
        }
        
        private void ApplyLoadedPreset()
        {
            // Apply all active filters from loaded preset
            foreach (var kvp in activeFilters)
            {
                if (kvp.Value && filterProcessor != null && filterParameters.ContainsKey(kvp.Key))
                {
                    filterProcessor.ApplyFilter(kvp.Key, filterParameters[kvp.Key]);
                }
            }
            
            UpdateFilterPreview();
        }
        
        private void UpdateFilterPreview()
        {
            if (!realTimePreviewEnabled || filterPreviewImage == null || filterProcessor == null) return;
            
            try
            {
                var processedTexture = filterProcessor.ProcessedTexture;
                if (processedTexture != null)
                {
                    filterPreviewImage.texture = processedTexture;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"FilterControlUI: Failed to update filter preview - {ex.Message}");
            }
        }
        
        // System event handlers
        private void OnSystemFilterApplied(Texture2D processedTexture)
        {
            UpdateFilterPreview();
        }
        
        private void OnSystemFilterRemoved(FilterType filterType)
        {
            if (activeFilters.ContainsKey(filterType))
            {
                activeFilters[filterType] = false;
                UpdateUI();
            }
        }
        
        // Public methods
        public void UpdateUI()
        {
            // Update toggle states
            if (grayscaleToggle != null)
                grayscaleToggle.isOn = activeFilters[FilterType.Grayscale];
            if (edgeDetectionToggle != null)
                edgeDetectionToggle.isOn = activeFilters[FilterType.EdgeDetection];
            if (contrastEnhancementToggle != null)
                contrastEnhancementToggle.isOn = activeFilters[FilterType.ContrastEnhancement];
            if (colorRangeToggle != null)
                colorRangeToggle.isOn = activeFilters[FilterType.ColorRange];
            if (colorReductionToggle != null)
                colorReductionToggle.isOn = activeFilters[FilterType.ColorReduction];
            
            // Update parameter displays
            foreach (var kvp in filterParameters)
            {
                var filterType = kvp.Key;
                var parameters = kvp.Value;
                
                switch (filterType)
                {
                    case FilterType.EdgeDetection:
                        UpdateParameterValueDisplay(filterType, "intensity", parameters.intensity);
                        break;
                    case FilterType.ContrastEnhancement:
                        UpdateParameterValueDisplay(filterType, "intensity", parameters.intensity);
                        break;
                    case FilterType.ColorRange:
                        UpdateParameterValueDisplay(filterType, "tolerance", parameters.colorTolerance);
                        break;
                    case FilterType.ColorReduction:
                        UpdateParameterValueDisplay(filterType, "count", parameters.targetColorCount);
                        break;
                }
            }
        }
        
        // Properties
        public Dictionary<FilterType, bool> ActiveFilters => new Dictionary<FilterType, bool>(activeFilters);
        public Dictionary<FilterType, FilterParameters> FilterParameters => new Dictionary<FilterType, FilterParameters>(filterParameters);
        public bool RealTimePreviewEnabled => realTimePreviewEnabled;
        public int ActiveFilterCount => activeFilters.Values.Count(f => f);
    }
    
    /// <summary>
    /// Data structure for filter presets
    /// </summary>
    [System.Serializable]
    public class FilterPresetData
    {
        public string name;
        public Dictionary<FilterType, bool> activeFilters;
        public Dictionary<FilterType, FilterParameters> filterParameters;
    }
}