using UnityEngine;
using UnityEngine.UI;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// Example script demonstrating how to use the FilterManager
    /// This shows the simplified approach using Unity's built-in post-processing
    /// </summary>
    public class FilterExample : MonoBehaviour
    {
        [Header("Filter System")]
        [SerializeField] private FilterManager filterManager;
        
        [Header("UI Controls (Optional)")]
        [SerializeField] private Button grayscaleButton;
        [SerializeField] private Button contrastButton;
        [SerializeField] private Button edgeDetectionButton;
        [SerializeField] private Button clearFiltersButton;
        [SerializeField] private Slider intensitySlider;
        
        [Header("Test Settings")]
        [SerializeField] private Texture2D testImage;
        [Range(0f, 1f)]
        [SerializeField] private float filterIntensity = 0.8f;
        
        private FilterType currentFilterType = FilterType.None;
        
        private void Start()
        {
            InitializeFilterManager();
            SetupUIControls();
            SetupEventListeners();
        }
        
        /// <summary>
        /// Initialize the FilterManager if not already set up
        /// </summary>
        private void InitializeFilterManager()
        {
            if (filterManager == null)
            {
                filterManager = FindObjectOfType<FilterManager>();
                
                if (filterManager == null)
                {
                    // Create FilterManager automatically
                    var filterObject = new GameObject("FilterManager");
                    filterManager = filterObject.AddComponent<FilterManager>();
                    
                    // Add setup component for automatic configuration
                    var setup = filterObject.AddComponent<FilterManagerSetup>();
                    
                    Debug.Log("FilterExample: Created FilterManager automatically");
                }
            }
            
            // Set test image if available
            if (testImage != null)
            {
                filterManager.SetSourceTexture(testImage);
            }
        }
        
        /// <summary>
        /// Setup UI controls if available
        /// </summary>
        private void SetupUIControls()
        {
            if (grayscaleButton != null)
                grayscaleButton.onClick.AddListener(() => ApplyGrayscaleFilter());
                
            if (contrastButton != null)
                contrastButton.onClick.AddListener(() => ApplyContrastFilter());
                
            if (edgeDetectionButton != null)
                edgeDetectionButton.onClick.AddListener(() => ApplyEdgeDetectionFilter());
                
            if (clearFiltersButton != null)
                clearFiltersButton.onClick.AddListener(() => ClearAllFilters());
                
            if (intensitySlider != null)
            {
                intensitySlider.value = filterIntensity;
                intensitySlider.onValueChanged.AddListener(OnIntensityChanged);
            }
        }
        
        /// <summary>
        /// Setup event listeners for filter manager
        /// </summary>
        private void SetupEventListeners()
        {
            if (filterManager != null)
            {
                filterManager.OnFilterApplied += OnFilterApplied;
                filterManager.OnFilterRemoved += OnFilterRemoved;
                filterManager.OnFiltersCleared += OnFiltersCleared;
            }
        }
        
        /// <summary>
        /// Apply grayscale filter - demonstrates the simplified approach
        /// </summary>
        public void ApplyGrayscaleFilter()
        {
            var parameters = new FilterParameters(FilterType.Grayscale);
            parameters.intensity = filterIntensity;
            
            filterManager.ApplyFilter(FilterType.Grayscale, parameters);
            currentFilterType = FilterType.Grayscale;
            
            Debug.Log($"FilterExample: Applied grayscale filter with intensity {filterIntensity}");
        }
        
        /// <summary>
        /// Apply contrast enhancement filter
        /// </summary>
        public void ApplyContrastFilter()
        {
            var parameters = new FilterParameters(FilterType.ContrastEnhancement);
            parameters.intensity = filterIntensity;
            
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, parameters);
            currentFilterType = FilterType.ContrastEnhancement;
            
            Debug.Log($"FilterExample: Applied contrast filter with intensity {filterIntensity}");
        }
        
        /// <summary>
        /// Apply edge detection filter using Bloom effect
        /// </summary>
        public void ApplyEdgeDetectionFilter()
        {
            var parameters = new FilterParameters(FilterType.EdgeDetection);
            parameters.intensity = filterIntensity;
            parameters.customParameters["threshold"] = 0.1f;
            
            filterManager.ApplyFilter(FilterType.EdgeDetection, parameters);
            currentFilterType = FilterType.EdgeDetection;
            
            Debug.Log($"FilterExample: Applied edge detection filter with intensity {filterIntensity}");
        }
        
        /// <summary>
        /// Clear all active filters
        /// </summary>
        public void ClearAllFilters()
        {
            filterManager.ClearAllFilters();
            currentFilterType = FilterType.None;
            
            Debug.Log("FilterExample: Cleared all filters");
        }
        
        /// <summary>
        /// Update filter intensity in real-time
        /// Real-time preview implementation for immediate visual feedback
        /// </summary>
        private void OnIntensityChanged(float newIntensity)
        {
            filterIntensity = newIntensity;
            
            // Update current filter with new intensity for real-time preview
            if (currentFilterType != FilterType.None)
            {
                var parameters = new FilterParameters(currentFilterType);
                parameters.intensity = filterIntensity;
                
                // Set filter-specific parameters
                switch (currentFilterType)
                {
                    case FilterType.EdgeDetection:
                        parameters.customParameters["threshold"] = 0.1f;
                        break;
                    case FilterType.Grayscale:
                        // No additional parameters needed
                        break;
                    case FilterType.ContrastEnhancement:
                        // No additional parameters needed
                        break;
                }
                
                filterManager.UpdateFilterParameters(currentFilterType, parameters);
                
                Debug.Log($"FilterExample: Real-time intensity update - {currentFilterType}: {filterIntensity:F2}");
            }
        }
        
        /// <summary>
        /// Handle filter applied event
        /// </summary>
        private void OnFilterApplied(Texture2D processedTexture)
        {
            Debug.Log($"FilterExample: Filter applied. Active filters: {filterManager.ActiveFilters.Count}");
        }
        
        /// <summary>
        /// Handle filter removed event
        /// </summary>
        private void OnFilterRemoved(FilterType filterType)
        {
            Debug.Log($"FilterExample: Filter {filterType} removed");
            
            if (filterType == currentFilterType)
            {
                currentFilterType = FilterType.None;
            }
        }
        
        /// <summary>
        /// Handle filters cleared event
        /// </summary>
        private void OnFiltersCleared()
        {
            Debug.Log("FilterExample: All filters cleared");
            currentFilterType = FilterType.None;
        }
        
        /// <summary>
        /// Demonstrate saving and loading filter settings
        /// </summary>
        [ContextMenu("Save Filter Settings")]
        public void SaveFilterSettings()
        {
            string settings = filterManager.SaveFilterSettings();
            PlayerPrefs.SetString("DaVinciEye_FilterSettings", settings);
            PlayerPrefs.Save();
            
            Debug.Log($"FilterExample: Saved filter settings: {settings}");
        }
        
        /// <summary>
        /// Load previously saved filter settings
        /// </summary>
        [ContextMenu("Load Filter Settings")]
        public void LoadFilterSettings()
        {
            string settings = PlayerPrefs.GetString("DaVinciEye_FilterSettings", "");
            
            if (!string.IsNullOrEmpty(settings))
            {
                filterManager.LoadFilterSettings(settings);
                Debug.Log("FilterExample: Loaded filter settings");
            }
            else
            {
                Debug.Log("FilterExample: No saved filter settings found");
            }
        }
        
        /// <summary>
        /// Demonstrate filter layering (multiple filters at once)
        /// </summary>
        [ContextMenu("Apply Multiple Filters")]
        public void ApplyMultipleFilters()
        {
            // Apply grayscale
            var grayscaleParams = new FilterParameters(FilterType.Grayscale);
            grayscaleParams.intensity = 0.5f;
            filterManager.ApplyFilter(FilterType.Grayscale, grayscaleParams);
            
            // Apply contrast
            var contrastParams = new FilterParameters(FilterType.ContrastEnhancement);
            contrastParams.intensity = 0.3f;
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, contrastParams);
            
            Debug.Log("FilterExample: Applied multiple filters (grayscale + contrast)");
        }
        
        /// <summary>
        /// Demonstrate performance benchmarking for all standard filters
        /// </summary>
        [ContextMenu("Benchmark All Filters")]
        public void BenchmarkAllFilters()
        {
            Debug.Log("FilterExample: Starting filter performance benchmarks...");
            
            // Benchmark Grayscale Filter
            var grayscaleParams = new FilterParameters(FilterType.Grayscale) { intensity = 0.8f };
            var grayscalePerf = filterManager.BenchmarkFilterPerformance(FilterType.Grayscale, grayscaleParams, 50);
            Debug.Log($"Grayscale Performance: {grayscalePerf}");
            
            // Benchmark Contrast Filter
            var contrastParams = new FilterParameters(FilterType.ContrastEnhancement) { intensity = 0.7f };
            var contrastPerf = filterManager.BenchmarkFilterPerformance(FilterType.ContrastEnhancement, contrastParams, 50);
            Debug.Log($"Contrast Performance: {contrastPerf}");
            
            // Benchmark Edge Detection Filter
            var edgeParams = new FilterParameters(FilterType.EdgeDetection) { intensity = 0.6f };
            edgeParams.customParameters["threshold"] = 0.15f;
            var edgePerf = filterManager.BenchmarkFilterPerformance(FilterType.EdgeDetection, edgeParams, 50);
            Debug.Log($"Edge Detection Performance: {edgePerf}");
            
            // Display overall performance metrics
            var metrics = filterManager.GetPerformanceMetrics();
            Debug.Log($"Current Performance Metrics: {metrics}");
        }
        
        /// <summary>
        /// Toggle real-time preview mode
        /// </summary>
        [ContextMenu("Toggle Real-Time Preview")]
        public void ToggleRealTimePreview()
        {
            var metrics = filterManager.GetPerformanceMetrics();
            bool currentState = metrics.isRealTimeEnabled;
            
            filterManager.EnableRealTimePreview(!currentState);
            
            Debug.Log($"FilterExample: Real-time preview {(!currentState ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Demonstrate real-time intensity adjustment for current filter
        /// </summary>
        [ContextMenu("Demo Real-Time Intensity")]
        public void DemoRealTimeIntensity()
        {
            if (currentFilterType == FilterType.None)
            {
                // Apply a filter first
                ApplyGrayscaleFilter();
            }
            
            // Enable real-time preview
            filterManager.EnableRealTimePreview(true);
            
            // Animate intensity changes
            StartCoroutine(AnimateIntensityDemo());
        }
        
        /// <summary>
        /// Coroutine to demonstrate real-time intensity changes
        /// </summary>
        private System.Collections.IEnumerator AnimateIntensityDemo()
        {
            Debug.Log("FilterExample: Starting real-time intensity animation demo...");
            
            float duration = 3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                // Animate intensity from 0 to 1 and back
                float normalizedTime = elapsed / duration;
                float intensity = Mathf.Sin(normalizedTime * Mathf.PI * 2f) * 0.5f + 0.5f;
                
                // Update slider if available
                if (intensitySlider != null)
                {
                    intensitySlider.value = intensity;
                }
                else
                {
                    // Update directly
                    OnIntensityChanged(intensity);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Debug.Log("FilterExample: Real-time intensity animation demo completed");
        }
        
        private void OnDestroy()
        {
            // Clean up event listeners
            if (filterManager != null)
            {
                filterManager.OnFilterApplied -= OnFilterApplied;
                filterManager.OnFilterRemoved -= OnFilterRemoved;
                filterManager.OnFiltersCleared -= OnFiltersCleared;
            }
        }
    }
}