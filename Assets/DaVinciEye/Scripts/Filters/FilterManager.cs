using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// Manages real-time filter processing using Unity's URP Volume system
    /// Implements the simplified approach using built-in post-processing effects
    /// </summary>
    public class FilterManager : MonoBehaviour, IFilterProcessor
    {
        [Header("Volume Configuration")]
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private VolumeProfile volumeProfile;
        
        [Header("Filter Settings")]
        [SerializeField] private List<FilterData> activeFilters = new List<FilterData>();
        [SerializeField] private Texture2D sourceTexture;
        [SerializeField] private RenderTexture processedRenderTexture;
        
        // Post-processing components
        private ColorAdjustments colorAdjustments;
        private Bloom bloom;
        private Vignette vignette;
        
        // Advanced filter components
        private ColorRangeFilter colorRangeFilter;
        private ColorReductionFilter colorReductionFilter;
        
        // Interface implementation
        public Texture2D ProcessedTexture { get; private set; }
        public List<FilterData> ActiveFilters => activeFilters.ToList();
        
        // Events
        public event Action<Texture2D> OnFilterApplied;
        public event Action<FilterType> OnFilterRemoved;
        public event Action OnFiltersCleared;
        
        private void Awake()
        {
            InitializeVolumeSystem();
        }
        
        private void Start()
        {
            SetupPostProcessingComponents();
            
            // Load previous session if available
            LoadSession();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Save session when app is paused (HoloLens suspend)
                SaveSession();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Save session when app loses focus
                SaveSession();
            }
        }
        
        private void OnDestroy()
        {
            // Save session before destruction
            SaveSession();
            
            // Unsubscribe from events
            if (colorRangeFilter != null)
            {
                colorRangeFilter.OnFilterApplied -= OnColorRangeFilterApplied;
            }
            
            if (colorReductionFilter != null)
            {
                colorReductionFilter.OnColorReductionApplied -= OnColorReductionFilterApplied;
            }
            
            // Clean up render texture
            if (processedRenderTexture != null)
            {
                processedRenderTexture.Release();
            }
        }
        
        /// <summary>
        /// Initialize the Volume system for post-processing
        /// </summary>
        private void InitializeVolumeSystem()
        {
            // Create Volume component if not assigned
            if (postProcessVolume == null)
            {
                postProcessVolume = gameObject.GetComponent<Volume>();
                if (postProcessVolume == null)
                {
                    postProcessVolume = gameObject.AddComponent<Volume>();
                }
            }
            
            // Create Volume Profile if not assigned
            if (volumeProfile == null)
            {
                volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                postProcessVolume.profile = volumeProfile;
            }
            
            // Set volume to global
            postProcessVolume.isGlobal = true;
            postProcessVolume.weight = 1f;
            
            Debug.Log("FilterManager: Volume system initialized");
        }
        
        /// <summary>
        /// Setup post-processing components in the volume profile
        /// </summary>
        private void SetupPostProcessingComponents()
        {
            // Add ColorAdjustments for grayscale and contrast
            if (!volumeProfile.TryGet<ColorAdjustments>(out colorAdjustments))
            {
                colorAdjustments = volumeProfile.Add<ColorAdjustments>(false);
            }
            
            // Add Bloom for edge detection effects
            if (!volumeProfile.TryGet<Bloom>(out bloom))
            {
                bloom = volumeProfile.Add<Bloom>(false);
            }
            
            // Add Vignette for additional effects
            if (!volumeProfile.TryGet<Vignette>(out vignette))
            {
                vignette = volumeProfile.Add<Vignette>(false);
            }
            
            // Setup advanced filter components
            SetupAdvancedFilterComponents();
            
            // Initialize with default disabled state
            ResetAllEffects();
            
            Debug.Log("FilterManager: Post-processing components setup complete");
        }
        
        /// <summary>
        /// Setup advanced filter components (ColorRangeFilter, ColorReductionFilter, etc.)
        /// </summary>
        private void SetupAdvancedFilterComponents()
        {
            // Get or create ColorRangeFilter component
            colorRangeFilter = GetComponent<ColorRangeFilter>();
            if (colorRangeFilter == null)
            {
                colorRangeFilter = gameObject.AddComponent<ColorRangeFilter>();
            }
            
            // Get or create ColorReductionFilter component
            colorReductionFilter = GetComponent<ColorReductionFilter>();
            if (colorReductionFilter == null)
            {
                colorReductionFilter = gameObject.AddComponent<ColorReductionFilter>();
            }
            
            // Subscribe to filter events
            colorRangeFilter.OnFilterApplied += OnColorRangeFilterApplied;
            colorReductionFilter.OnColorReductionApplied += OnColorReductionFilterApplied;
            
            Debug.Log("FilterManager: Advanced filter components setup complete");
        }
        
        /// <summary>
        /// Handle color range filter applied event
        /// </summary>
        private void OnColorRangeFilterApplied(Texture2D filteredTexture)
        {
            if (filteredTexture != null)
            {
                ProcessedTexture = filteredTexture;
                OnFilterApplied?.Invoke(ProcessedTexture);
            }
        }
        
        /// <summary>
        /// Handle color reduction filter applied event
        /// </summary>
        private void OnColorReductionFilterApplied(Texture2D filteredTexture)
        {
            if (filteredTexture != null)
            {
                ProcessedTexture = filteredTexture;
                OnFilterApplied?.Invoke(ProcessedTexture);
            }
        }
        
        /// <summary>
        /// Apply a filter with specified parameters
        /// Enhanced to support multiple filter layering
        /// </summary>
        public void ApplyFilter(FilterType filterType, FilterParameters parameters)
        {
            if (parameters == null)
            {
                parameters = new FilterParameters(filterType);
            }
            
            // Check if filter already exists
            var existingFilter = activeFilters.FirstOrDefault(f => f.type == filterType);
            if (existingFilter != null)
            {
                // Update existing filter parameters
                existingFilter.parameters = parameters;
                Debug.Log($"FilterManager: Updated existing {filterType} filter");
            }
            else
            {
                // Add new filter with proper layer ordering
                var filterData = new FilterData(filterType, parameters);
                filterData.layerOrder = GetNextLayerOrder();
                activeFilters.Add(filterData);
                Debug.Log($"FilterManager: Added new {filterType} filter at layer {filterData.layerOrder}");
            }
            
            // Apply all filters in layer order for proper combination
            ApplyFilterLayers();
            
            OnFilterApplied?.Invoke(ProcessedTexture);
            
            Debug.Log($"FilterManager: Applied {filterType} filter with intensity {parameters.intensity} (Total active: {activeFilters.Count})");
        }
        
        /// <summary>
        /// Apply multiple filters in the correct layer order
        /// Implements filter layering system for compatible effects
        /// </summary>
        private void ApplyFilterLayers()
        {
            // Reset all effects first
            ResetAllEffects();
            
            // Sort filters by layer order for proper application sequence
            var sortedFilters = activeFilters.OrderBy(f => f.layerOrder).ToList();
            
            // Apply filters in order, checking for compatibility
            foreach (var filterData in sortedFilters)
            {
                if (filterData.isActive && IsFilterCompatible(filterData.type, sortedFilters))
                {
                    ApplyIndividualFilter(filterData.type, filterData.parameters);
                }
            }
            
            // Update processed texture with all applied filters
            UpdateProcessedTexture();
        }
        
        /// <summary>
        /// Apply an individual filter without affecting other filters
        /// </summary>
        private void ApplyIndividualFilter(FilterType filterType, FilterParameters parameters)
        {
            switch (filterType)
            {
                case FilterType.Grayscale:
                    ApplyGrayscaleFilter(parameters);
                    break;
                case FilterType.ContrastEnhancement:
                    ApplyContrastFilter(parameters);
                    break;
                case FilterType.EdgeDetection:
                    ApplyEdgeDetectionFilter(parameters);
                    break;
                case FilterType.ColorRange:
                    ApplyColorRangeFilter(parameters);
                    break;
                case FilterType.ColorReduction:
                    ApplyColorReductionFilter(parameters);
                    break;
                default:
                    Debug.LogWarning($"FilterManager: Filter type {filterType} not implemented yet");
                    break;
            }
        }
        
        /// <summary>
        /// Check if a filter is compatible with currently active filters
        /// Implements compatibility rules for filter layering
        /// </summary>
        private bool IsFilterCompatible(FilterType filterType, List<FilterData> activeFilters)
        {
            // Define compatibility rules
            var incompatibleCombinations = new Dictionary<FilterType, FilterType[]>
            {
                // Edge detection conflicts with heavy color modifications
                { FilterType.EdgeDetection, new[] { FilterType.Grayscale } },
                // Add more incompatible combinations as needed
            };
            
            if (incompatibleCombinations.ContainsKey(filterType))
            {
                var incompatibleTypes = incompatibleCombinations[filterType];
                foreach (var activeFilter in activeFilters)
                {
                    if (activeFilter.type != filterType && incompatibleTypes.Contains(activeFilter.type))
                    {
                        Debug.LogWarning($"FilterManager: {filterType} is incompatible with {activeFilter.type}");
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Get the next layer order for new filters
        /// </summary>
        private int GetNextLayerOrder()
        {
            if (activeFilters.Count == 0)
                return 0;
            
            return activeFilters.Max(f => f.layerOrder) + 1;
        }
        
        /// <summary>
        /// Update parameters for an existing filter
        /// </summary>
        public void UpdateFilterParameters(FilterType filterType, FilterParameters parameters)
        {
            var existingFilter = activeFilters.FirstOrDefault(f => f.type == filterType);
            if (existingFilter != null)
            {
                existingFilter.parameters = parameters;
                ApplyFilter(filterType, parameters);
            }
        }
        
        /// <summary>
        /// Remove a specific filter and reapply remaining filters
        /// Enhanced to maintain proper filter layering
        /// </summary>
        public void RemoveFilter(FilterType filterType)
        {
            var filterToRemove = activeFilters.FirstOrDefault(f => f.type == filterType);
            if (filterToRemove != null)
            {
                activeFilters.Remove(filterToRemove);
                
                // Reapply all remaining filters to maintain proper layering
                ApplyFilterLayers();
                
                OnFilterRemoved?.Invoke(filterType);
                
                Debug.Log($"FilterManager: Removed {filterType} filter (Remaining active: {activeFilters.Count})");
            }
            else
            {
                Debug.LogWarning($"FilterManager: Attempted to remove {filterType} filter that is not active");
            }
        }
        
        /// <summary>
        /// Toggle a filter on/off without removing it from the layer stack
        /// </summary>
        public void ToggleFilter(FilterType filterType, bool isActive)
        {
            var filter = activeFilters.FirstOrDefault(f => f.type == filterType);
            if (filter != null)
            {
                filter.isActive = isActive;
                ApplyFilterLayers();
                
                Debug.Log($"FilterManager: Toggled {filterType} filter {(isActive ? "ON" : "OFF")}");
            }
        }
        
        /// <summary>
        /// Reorder filters by changing their layer order
        /// </summary>
        public void ReorderFilter(FilterType filterType, int newLayerOrder)
        {
            var filter = activeFilters.FirstOrDefault(f => f.type == filterType);
            if (filter != null)
            {
                filter.layerOrder = newLayerOrder;
                
                // Normalize layer orders to prevent gaps
                NormalizeLayerOrders();
                
                // Reapply filters in new order
                ApplyFilterLayers();
                
                Debug.Log($"FilterManager: Reordered {filterType} filter to layer {newLayerOrder}");
            }
        }
        
        /// <summary>
        /// Normalize layer orders to ensure sequential ordering without gaps
        /// </summary>
        private void NormalizeLayerOrders()
        {
            var sortedFilters = activeFilters.OrderBy(f => f.layerOrder).ToList();
            for (int i = 0; i < sortedFilters.Count; i++)
            {
                sortedFilters[i].layerOrder = i;
            }
        }
        
        /// <summary>
        /// Move a filter up in the layer order
        /// </summary>
        public void MoveFilterUp(FilterType filterType)
        {
            var filter = activeFilters.FirstOrDefault(f => f.type == filterType);
            if (filter != null && filter.layerOrder < activeFilters.Max(f => f.layerOrder))
            {
                var nextFilter = activeFilters.Where(f => f.layerOrder > filter.layerOrder)
                                             .OrderBy(f => f.layerOrder)
                                             .FirstOrDefault();
                
                if (nextFilter != null)
                {
                    // Swap layer orders
                    int tempOrder = filter.layerOrder;
                    filter.layerOrder = nextFilter.layerOrder;
                    nextFilter.layerOrder = tempOrder;
                    
                    ApplyFilterLayers();
                    Debug.Log($"FilterManager: Moved {filterType} filter up in layer order");
                }
            }
        }
        
        /// <summary>
        /// Move a filter down in the layer order
        /// </summary>
        public void MoveFilterDown(FilterType filterType)
        {
            var filter = activeFilters.FirstOrDefault(f => f.type == filterType);
            if (filter != null && filter.layerOrder > 0)
            {
                var previousFilter = activeFilters.Where(f => f.layerOrder < filter.layerOrder)
                                                 .OrderByDescending(f => f.layerOrder)
                                                 .FirstOrDefault();
                
                if (previousFilter != null)
                {
                    // Swap layer orders
                    int tempOrder = filter.layerOrder;
                    filter.layerOrder = previousFilter.layerOrder;
                    previousFilter.layerOrder = tempOrder;
                    
                    ApplyFilterLayers();
                    Debug.Log($"FilterManager: Moved {filterType} filter down in layer order");
                }
            }
        }
        
        /// <summary>
        /// Clear all active filters with optional backup for undo functionality
        /// </summary>
        public void ClearAllFilters(bool createBackup = true)
        {
            if (createBackup && activeFilters.Count > 0)
            {
                // Create backup for potential undo operation
                SaveFilterBackup();
            }
            
            activeFilters.Clear();
            ResetAllEffects();
            UpdateProcessedTexture();
            OnFiltersCleared?.Invoke();
            
            Debug.Log("FilterManager: Cleared all filters");
        }
        
        /// <summary>
        /// Reset all filters to their default state without removing them
        /// </summary>
        public void ResetAllFilterParameters()
        {
            foreach (var filter in activeFilters)
            {
                filter.parameters = new FilterParameters(filter.type);
            }
            
            ApplyFilterLayers();
            Debug.Log("FilterManager: Reset all filter parameters to defaults");
        }
        
        /// <summary>
        /// Get a list of currently active filter types in layer order
        /// </summary>
        public List<FilterType> GetActiveFilterTypes()
        {
            return activeFilters.OrderBy(f => f.layerOrder)
                               .Where(f => f.isActive)
                               .Select(f => f.type)
                               .ToList();
        }
        
        /// <summary>
        /// Check if a specific filter type is currently active
        /// </summary>
        public bool IsFilterActive(FilterType filterType)
        {
            return activeFilters.Any(f => f.type == filterType && f.isActive);
        }
        
        /// <summary>
        /// Get the parameters for a specific active filter
        /// </summary>
        public FilterParameters GetFilterParameters(FilterType filterType)
        {
            var filter = activeFilters.FirstOrDefault(f => f.type == filterType);
            return filter?.parameters;
        }
        
        /// <summary>
        /// Get the layer order of a specific filter
        /// </summary>
        public int GetFilterLayerOrder(FilterType filterType)
        {
            var filter = activeFilters.FirstOrDefault(f => f.type == filterType);
            return filter?.layerOrder ?? -1;
        }
        
        /// <summary>
        /// Apply grayscale filter using ColorAdjustments saturation
        /// Real-time implementation with proper intensity scaling
        /// </summary>
        private void ApplyGrayscaleFilter(FilterParameters parameters)
        {
            colorAdjustments.active = true;
            
            // Convert intensity (0-1) to saturation (-100 to 0)
            // 0 intensity = no effect (saturation = 0)
            // 1 intensity = full grayscale (saturation = -100)
            colorAdjustments.saturation.value = -100f * parameters.intensity;
            
            Debug.Log($"FilterManager: Applied grayscale filter - saturation: {colorAdjustments.saturation.value}");
        }
        
        /// <summary>
        /// Apply contrast enhancement filter with proper scaling
        /// Real-time implementation with balanced contrast adjustment
        /// </summary>
        private void ApplyContrastFilter(FilterParameters parameters)
        {
            colorAdjustments.active = true;
            
            // Convert intensity (0-1) to contrast range (-50 to +50)
            // 0 intensity = no effect (contrast = 0)
            // 0.5 intensity = no effect (contrast = 0) 
            // 1 intensity = maximum contrast (contrast = +50)
            // Values below 0.5 reduce contrast, above 0.5 increase contrast
            float contrastValue = (parameters.intensity - 0.5f) * 100f;
            colorAdjustments.contrast.value = Mathf.Clamp(contrastValue, -50f, 50f);
            
            Debug.Log($"FilterManager: Applied contrast filter - contrast: {colorAdjustments.contrast.value}");
        }
        
        /// <summary>
        /// Apply edge detection filter using Bloom with enhanced parameters
        /// Real-time implementation with customizable threshold and intensity
        /// </summary>
        private void ApplyEdgeDetectionFilter(FilterParameters parameters)
        {
            bloom.active = true;
            
            // Configure bloom for edge detection effect
            // Higher threshold creates more selective edge detection
            float threshold = parameters.customParameters.GetValueOrDefault("threshold", 0.1f);
            float edgeIntensity = parameters.intensity * 3f; // Scale for visible effect
            
            bloom.threshold.value = Mathf.Clamp(threshold, 0.01f, 1f);
            bloom.intensity.value = Mathf.Clamp(edgeIntensity, 0f, 5f);
            
            // Additional bloom parameters for better edge detection
            if (bloom.scatter != null)
                bloom.scatter.value = 0.7f; // Reduce scatter for sharper edges
            
            Debug.Log($"FilterManager: Applied edge detection filter - intensity: {bloom.intensity.value}, threshold: {bloom.threshold.value}");
        }
        
        /// <summary>
        /// Reset grayscale filter to default state
        /// </summary>
        private void ResetGrayscaleFilter()
        {
            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = 0f;
                
                // Disable ColorAdjustments if no other adjustments are active
                if (Mathf.Approximately(colorAdjustments.contrast.value, 0f) && 
                    Mathf.Approximately(colorAdjustments.saturation.value, 0f))
                {
                    colorAdjustments.active = false;
                }
                
                Debug.Log("FilterManager: Reset grayscale filter");
            }
        }
        
        /// <summary>
        /// Reset contrast filter to default state
        /// </summary>
        private void ResetContrastFilter()
        {
            if (colorAdjustments != null)
            {
                colorAdjustments.contrast.value = 0f;
                
                // Disable ColorAdjustments if no other adjustments are active
                if (Mathf.Approximately(colorAdjustments.contrast.value, 0f) && 
                    Mathf.Approximately(colorAdjustments.saturation.value, 0f))
                {
                    colorAdjustments.active = false;
                }
                
                Debug.Log("FilterManager: Reset contrast filter");
            }
        }
        
        /// <summary>
        /// Reset edge detection filter to default state
        /// </summary>
        private void ResetEdgeDetectionFilter()
        {
            if (bloom != null)
            {
                bloom.active = false;
                bloom.intensity.value = 0f;
                bloom.threshold.value = 1f;
                
                // Reset scatter if available
                if (bloom.scatter != null)
                    bloom.scatter.value = 0.7f;
                
                Debug.Log("FilterManager: Reset edge detection filter");
            }
        }
        
        /// <summary>
        /// Apply color range filter using ColorRangeFilter component
        /// Implements HSV-based color isolation (Requirements 4.1.1, 4.1.2, 4.1.3)
        /// </summary>
        private void ApplyColorRangeFilter(FilterParameters parameters)
        {
            // Get or create ColorRangeFilter component
            var colorRangeFilter = GetComponent<ColorRangeFilter>();
            if (colorRangeFilter == null)
            {
                colorRangeFilter = gameObject.AddComponent<ColorRangeFilter>();
            }
            
            // Configure color range from parameters
            if (parameters.customParameters.ContainsKey("hueMin") && 
                parameters.customParameters.ContainsKey("hueMax"))
            {
                float hueMin = parameters.customParameters["hueMin"];
                float hueMax = parameters.customParameters["hueMax"];
                float satMin = parameters.customParameters.GetValueOrDefault("satMin", 0f);
                float satMax = parameters.customParameters.GetValueOrDefault("satMax", 1f);
                float valueMin = parameters.customParameters.GetValueOrDefault("valueMin", 0f);
                float valueMax = parameters.customParameters.GetValueOrDefault("valueMax", 1f);
                
                colorRangeFilter.SetColorRange(hueMin, hueMax, satMin, satMax, valueMin, valueMax);
            }
            else if (parameters.targetColor != Color.clear)
            {
                // Use target color with tolerance
                colorRangeFilter.SetColorRangeFromColor(parameters.targetColor, parameters.colorTolerance);
            }
            
            // Apply the filter to current source texture
            if (sourceTexture != null)
            {
                var filteredTexture = colorRangeFilter.ApplyColorRangeFilter(sourceTexture);
                if (filteredTexture != null)
                {
                    // Update processed texture
                    ProcessedTexture = filteredTexture;
                }
            }
            
            Debug.Log($"FilterManager: Applied color range filter with target color {parameters.targetColor} and tolerance {parameters.colorTolerance}");
        }
        
        /// <summary>
        /// Apply color reduction filter using ColorReductionFilter component
        /// Implements color quantization and palette reduction (Requirements 4.2.1, 4.2.2, 4.2.4, 4.2.5)
        /// </summary>
        private void ApplyColorReductionFilter(FilterParameters parameters)
        {
            // Get or create ColorReductionFilter component
            if (colorReductionFilter == null)
            {
                colorReductionFilter = gameObject.AddComponent<ColorReductionFilter>();
            }
            
            // Configure color reduction from parameters
            colorReductionFilter.TargetColorCount = parameters.targetColorCount;
            
            // Set quantization method if specified
            if (parameters.customParameters.ContainsKey("method"))
            {
                int methodValue = Mathf.RoundToInt(parameters.customParameters["method"]);
                colorReductionFilter.QuantizationMethod = (ColorReductionFilter.ColorQuantizationMethod)methodValue;
            }
            
            // Apply the filter to current source texture
            if (sourceTexture != null)
            {
                var filteredTexture = colorReductionFilter.ApplyColorReduction(sourceTexture);
                if (filteredTexture != null)
                {
                    // Update processed texture
                    ProcessedTexture = filteredTexture;
                }
            }
            
            Debug.Log($"FilterManager: Applied color reduction filter with {parameters.targetColorCount} colors using {colorReductionFilter.QuantizationMethod} method");
        }
        
        /// <summary>
        /// Reset all post-processing effects
        /// </summary>
        private void ResetAllEffects()
        {
            if (colorAdjustments != null)
            {
                colorAdjustments.active = false;
                colorAdjustments.saturation.value = 0f;
                colorAdjustments.contrast.value = 0f;
            }
            
            if (bloom != null)
            {
                bloom.active = false;
                bloom.intensity.value = 0f;
                bloom.threshold.value = 1f;
            }
            
            if (vignette != null)
            {
                vignette.active = false;
            }
        }
        
        /// <summary>
        /// Update the processed texture with current filter effects
        /// Real-time processing using render texture for immediate preview
        /// </summary>
        private void UpdateProcessedTexture()
        {
            if (sourceTexture == null)
            {
                ProcessedTexture = null;
                return;
            }
            
            // Create or update render texture for processing
            if (processedRenderTexture == null || 
                processedRenderTexture.width != sourceTexture.width || 
                processedRenderTexture.height != sourceTexture.height)
            {
                if (processedRenderTexture != null)
                {
                    processedRenderTexture.Release();
                }
                
                processedRenderTexture = new RenderTexture(sourceTexture.width, sourceTexture.height, 0);
                processedRenderTexture.filterMode = FilterMode.Bilinear;
                processedRenderTexture.Create();
            }
            
            // Copy source texture to render texture for processing
            Graphics.Blit(sourceTexture, processedRenderTexture);
            
            // Convert render texture to Texture2D for interface compatibility
            ProcessedTexture = RenderTextureToTexture2D(processedRenderTexture);
            
            Debug.Log($"FilterManager: Updated processed texture ({ProcessedTexture.width}x{ProcessedTexture.height})");
        }
        
        /// <summary>
        /// Convert RenderTexture to Texture2D for compatibility
        /// </summary>
        private Texture2D RenderTextureToTexture2D(RenderTexture renderTexture)
        {
            if (renderTexture == null) return null;
            
            RenderTexture.active = renderTexture;
            Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;
            
            return texture2D;
        }
        
        /// <summary>
        /// Enable real-time filter preview updates
        /// </summary>
        public void EnableRealTimePreview(bool enable)
        {
            if (enable)
            {
                // Update processed texture every frame for real-time preview
                InvokeRepeating(nameof(UpdateProcessedTexture), 0f, 1f / 30f); // 30 FPS update rate
            }
            else
            {
                CancelInvoke(nameof(UpdateProcessedTexture));
            }
            
            Debug.Log($"FilterManager: Real-time preview {(enable ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Set the source texture for filtering
        /// </summary>
        public void SetSourceTexture(Texture2D texture)
        {
            sourceTexture = texture;
            UpdateProcessedTexture();
        }
        
        // Session management and backup functionality
        private FilterSettings lastFilterBackup;
        private const string SESSION_PREFS_KEY = "DaVinciEye_FilterSession";
        
        /// <summary>
        /// Save current filter settings to JSON
        /// Enhanced with session persistence
        /// </summary>
        public string SaveFilterSettings()
        {
            var settings = new FilterSettings
            {
                filters = activeFilters.ToArray(),
                sessionId = System.Guid.NewGuid().ToString(),
                savedAt = DateTime.Now
            };
            
            return JsonUtility.ToJson(settings, true);
        }
        
        /// <summary>
        /// Load filter settings from JSON
        /// Enhanced with validation and error recovery
        /// </summary>
        public void LoadFilterSettings(string json)
        {
            try
            {
                var settings = JsonUtility.FromJson<FilterSettings>(json);
                
                if (settings?.filters == null)
                {
                    Debug.LogWarning("FilterManager: Invalid filter settings data");
                    return;
                }
                
                // Create backup before loading new settings
                SaveFilterBackup();
                
                ClearAllFilters(false); // Don't create another backup
                
                // Load filters in layer order
                var sortedFilters = settings.filters.OrderBy(f => f.layerOrder);
                foreach (var filterData in sortedFilters)
                {
                    if (filterData.parameters != null)
                    {
                        ApplyFilter(filterData.type, filterData.parameters);
                        
                        // Restore active state and layer order
                        var loadedFilter = activeFilters.LastOrDefault();
                        if (loadedFilter != null)
                        {
                            loadedFilter.isActive = filterData.isActive;
                            loadedFilter.layerOrder = filterData.layerOrder;
                        }
                    }
                }
                
                // Reapply filters to ensure proper layering
                ApplyFilterLayers();
                
                Debug.Log($"FilterManager: Loaded {settings.filters.Length} filter settings from JSON");
            }
            catch (Exception e)
            {
                Debug.LogError($"FilterManager: Failed to load filter settings: {e.Message}");
                
                // Attempt to restore from backup if available
                if (lastFilterBackup != null)
                {
                    RestoreFromBackup();
                }
            }
        }
        
        /// <summary>
        /// Save current filter state as backup for undo functionality
        /// </summary>
        private void SaveFilterBackup()
        {
            lastFilterBackup = new FilterSettings
            {
                filters = activeFilters.ToArray(),
                sessionId = "backup",
                savedAt = DateTime.Now
            };
            
            Debug.Log("FilterManager: Created filter backup");
        }
        
        /// <summary>
        /// Restore filters from the last backup
        /// </summary>
        public void RestoreFromBackup()
        {
            if (lastFilterBackup?.filters != null)
            {
                ClearAllFilters(false);
                
                foreach (var filterData in lastFilterBackup.filters.OrderBy(f => f.layerOrder))
                {
                    ApplyFilter(filterData.type, filterData.parameters);
                    
                    var restoredFilter = activeFilters.LastOrDefault();
                    if (restoredFilter != null)
                    {
                        restoredFilter.isActive = filterData.isActive;
                        restoredFilter.layerOrder = filterData.layerOrder;
                    }
                }
                
                ApplyFilterLayers();
                Debug.Log("FilterManager: Restored filters from backup");
            }
            else
            {
                Debug.LogWarning("FilterManager: No backup available to restore");
            }
        }
        
        /// <summary>
        /// Save current session to PlayerPrefs for persistence across app sessions
        /// Implements requirement 4.9 - maintain filter preferences for current session
        /// </summary>
        public void SaveSession()
        {
            try
            {
                string sessionData = SaveFilterSettings();
                PlayerPrefs.SetString(SESSION_PREFS_KEY, sessionData);
                PlayerPrefs.Save();
                
                Debug.Log("FilterManager: Session saved to PlayerPrefs");
            }
            catch (Exception e)
            {
                Debug.LogError($"FilterManager: Failed to save session: {e.Message}");
            }
        }
        
        /// <summary>
        /// Load session from PlayerPrefs to restore previous filter state
        /// </summary>
        public void LoadSession()
        {
            try
            {
                if (PlayerPrefs.HasKey(SESSION_PREFS_KEY))
                {
                    string sessionData = PlayerPrefs.GetString(SESSION_PREFS_KEY);
                    LoadFilterSettings(sessionData);
                    
                    Debug.Log("FilterManager: Session loaded from PlayerPrefs");
                }
                else
                {
                    Debug.Log("FilterManager: No saved session found");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"FilterManager: Failed to load session: {e.Message}");
            }
        }
        
        /// <summary>
        /// Clear saved session data
        /// </summary>
        public void ClearSession()
        {
            PlayerPrefs.DeleteKey(SESSION_PREFS_KEY);
            PlayerPrefs.Save();
            
            Debug.Log("FilterManager: Session data cleared");
        }
        
        /// <summary>
        /// Performance benchmarking for filter processing
        /// </summary>
        public FilterPerformanceData BenchmarkFilterPerformance(FilterType filterType, FilterParameters parameters, int iterations = 100)
        {
            var performanceData = new FilterPerformanceData
            {
                filterType = filterType,
                iterations = iterations,
                startTime = Time.realtimeSinceStartup
            };
            
            float totalTime = 0f;
            float minTime = float.MaxValue;
            float maxTime = 0f;
            
            for (int i = 0; i < iterations; i++)
            {
                float startTime = Time.realtimeSinceStartup;
                
                // Apply filter
                ApplyFilter(filterType, parameters);
                
                float endTime = Time.realtimeSinceStartup;
                float frameTime = endTime - startTime;
                
                totalTime += frameTime;
                minTime = Mathf.Min(minTime, frameTime);
                maxTime = Mathf.Max(maxTime, frameTime);
                
                // Remove filter for next iteration
                RemoveFilter(filterType);
            }
            
            performanceData.endTime = Time.realtimeSinceStartup;
            performanceData.averageTime = totalTime / iterations;
            performanceData.minTime = minTime;
            performanceData.maxTime = maxTime;
            performanceData.totalTime = totalTime;
            performanceData.fps = 1f / performanceData.averageTime;
            
            Debug.Log($"FilterManager: Benchmark {filterType} - Avg: {performanceData.averageTime * 1000f:F2}ms, FPS: {performanceData.fps:F1}");
            
            return performanceData;
        }
        
        /// <summary>
        /// Get current filter processing performance metrics
        /// </summary>
        public FilterPerformanceMetrics GetPerformanceMetrics()
        {
            return new FilterPerformanceMetrics
            {
                activeFilterCount = activeFilters.Count,
                memoryUsage = GetEstimatedMemoryUsage(),
                frameTime = Time.deltaTime,
                fps = 1f / Time.deltaTime,
                isRealTimeEnabled = IsInvoking(nameof(UpdateProcessedTexture))
            };
        }
        
        /// <summary>
        /// Estimate memory usage of current filter setup
        /// </summary>
        private float GetEstimatedMemoryUsage()
        {
            float memoryUsage = 0f;
            
            if (sourceTexture != null)
            {
                memoryUsage += sourceTexture.width * sourceTexture.height * 4; // RGBA32
            }
            
            if (processedRenderTexture != null)
            {
                memoryUsage += processedRenderTexture.width * processedRenderTexture.height * 4;
            }
            
            if (ProcessedTexture != null)
            {
                memoryUsage += ProcessedTexture.width * ProcessedTexture.height * 3; // RGB24
            }
            
            return memoryUsage / (1024f * 1024f); // Convert to MB
        }
        
        /// <summary>
        /// Serializable class for saving filter settings
        /// Enhanced with session management data
        /// </summary>
        [System.Serializable]
        private class FilterSettings
        {
            public FilterData[] filters;
            public string sessionId;
            public DateTime savedAt;
        }
    }
}