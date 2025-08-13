using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// Validation script to verify standard filter implementations
    /// This script can be run in the Unity Editor to validate filter functionality
    /// </summary>
    public class FilterValidation : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private bool runValidationOnStart = true;
        [SerializeField] private bool logDetailedResults = true;
        
        private FilterManager filterManager;
        
        private void Start()
        {
            if (runValidationOnStart)
            {
                ValidateStandardFilters();
            }
        }
        
        /// <summary>
        /// Validate all standard filter implementations
        /// </summary>
        [ContextMenu("Validate Standard Filters")]
        public void ValidateStandardFilters()
        {
            Debug.Log("FilterValidation: Starting standard filter validation...");
            
            // Initialize filter manager
            InitializeFilterManager();
            
            // Validate each standard filter
            bool grayscaleValid = ValidateGrayscaleFilter();
            bool contrastValid = ValidateContrastFilter();
            bool edgeDetectionValid = ValidateEdgeDetectionFilter();
            
            // Validate real-time functionality
            bool realTimeValid = ValidateRealTimePreview();
            
            // Validate performance
            bool performanceValid = ValidatePerformanceBenchmarks();
            
            // Summary
            bool allValid = grayscaleValid && contrastValid && edgeDetectionValid && realTimeValid && performanceValid;
            
            string result = allValid ? "PASSED" : "FAILED";
            Debug.Log($"FilterValidation: Standard filter validation {result}");
            
            if (logDetailedResults)
            {
                Debug.Log($"  - Grayscale Filter: {(grayscaleValid ? "PASS" : "FAIL")}");
                Debug.Log($"  - Contrast Filter: {(contrastValid ? "PASS" : "FAIL")}");
                Debug.Log($"  - Edge Detection Filter: {(edgeDetectionValid ? "PASS" : "FAIL")}");
                Debug.Log($"  - Real-Time Preview: {(realTimeValid ? "PASS" : "FAIL")}");
                Debug.Log($"  - Performance Benchmarks: {(performanceValid ? "PASS" : "FAIL")}");
            }
        }
        
        /// <summary>
        /// Initialize or find FilterManager
        /// </summary>
        private void InitializeFilterManager()
        {
            filterManager = FindObjectOfType<FilterManager>();
            
            if (filterManager == null)
            {
                var filterObject = new GameObject("ValidationFilterManager");
                filterManager = filterObject.AddComponent<FilterManager>();
                
                // Wait for initialization
                filterManager.Start();
                
                Debug.Log("FilterValidation: Created FilterManager for validation");
            }
        }
        
        /// <summary>
        /// Validate grayscale filter implementation
        /// </summary>
        private bool ValidateGrayscaleFilter()
        {
            try
            {
                // Test various intensity values
                float[] testIntensities = { 0f, 0.5f, 1f };
                
                foreach (float intensity in testIntensities)
                {
                    var parameters = new FilterParameters(FilterType.Grayscale) { intensity = intensity };
                    filterManager.ApplyFilter(FilterType.Grayscale, parameters);
                    
                    // Verify filter is in active list
                    if (filterManager.ActiveFilters.Count != 1)
                    {
                        Debug.LogError($"FilterValidation: Grayscale filter not properly added (intensity: {intensity})");
                        return false;
                    }
                    
                    // Verify parameters are correct
                    var activeFilter = filterManager.ActiveFilters[0];
                    if (activeFilter.type != FilterType.Grayscale || 
                        Mathf.Abs(activeFilter.parameters.intensity - intensity) > 0.01f)
                    {
                        Debug.LogError($"FilterValidation: Grayscale filter parameters incorrect (intensity: {intensity})");
                        return false;
                    }
                    
                    filterManager.ClearAllFilters();
                }
                
                Debug.Log("FilterValidation: Grayscale filter validation passed");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FilterValidation: Grayscale filter validation failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Validate contrast filter implementation
        /// </summary>
        private bool ValidateContrastFilter()
        {
            try
            {
                // Test various intensity values
                float[] testIntensities = { 0f, 0.25f, 0.5f, 0.75f, 1f };
                
                foreach (float intensity in testIntensities)
                {
                    var parameters = new FilterParameters(FilterType.ContrastEnhancement) { intensity = intensity };
                    filterManager.ApplyFilter(FilterType.ContrastEnhancement, parameters);
                    
                    // Verify filter is active
                    if (filterManager.ActiveFilters.Count != 1)
                    {
                        Debug.LogError($"FilterValidation: Contrast filter not properly added (intensity: {intensity})");
                        return false;
                    }
                    
                    var activeFilter = filterManager.ActiveFilters[0];
                    if (activeFilter.type != FilterType.ContrastEnhancement)
                    {
                        Debug.LogError($"FilterValidation: Contrast filter type incorrect (intensity: {intensity})");
                        return false;
                    }
                    
                    filterManager.ClearAllFilters();
                }
                
                Debug.Log("FilterValidation: Contrast filter validation passed");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FilterValidation: Contrast filter validation failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Validate edge detection filter implementation
        /// </summary>
        private bool ValidateEdgeDetectionFilter()
        {
            try
            {
                // Test with custom parameters
                var parameters = new FilterParameters(FilterType.EdgeDetection) { intensity = 0.7f };
                parameters.customParameters["threshold"] = 0.15f;
                
                filterManager.ApplyFilter(FilterType.EdgeDetection, parameters);
                
                // Verify filter is active
                if (filterManager.ActiveFilters.Count != 1)
                {
                    Debug.LogError("FilterValidation: Edge detection filter not properly added");
                    return false;
                }
                
                var activeFilter = filterManager.ActiveFilters[0];
                if (activeFilter.type != FilterType.EdgeDetection)
                {
                    Debug.LogError("FilterValidation: Edge detection filter type incorrect");
                    return false;
                }
                
                // Verify custom parameters
                if (!activeFilter.parameters.customParameters.ContainsKey("threshold"))
                {
                    Debug.LogError("FilterValidation: Edge detection filter missing threshold parameter");
                    return false;
                }
                
                filterManager.ClearAllFilters();
                
                Debug.Log("FilterValidation: Edge detection filter validation passed");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FilterValidation: Edge detection filter validation failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Validate real-time preview functionality
        /// </summary>
        private bool ValidateRealTimePreview()
        {
            try
            {
                // Test enabling/disabling real-time preview
                filterManager.EnableRealTimePreview(true);
                var metrics = filterManager.GetPerformanceMetrics();
                
                if (!metrics.isRealTimeEnabled)
                {
                    Debug.LogError("FilterValidation: Real-time preview not properly enabled");
                    return false;
                }
                
                filterManager.EnableRealTimePreview(false);
                metrics = filterManager.GetPerformanceMetrics();
                
                if (metrics.isRealTimeEnabled)
                {
                    Debug.LogError("FilterValidation: Real-time preview not properly disabled");
                    return false;
                }
                
                Debug.Log("FilterValidation: Real-time preview validation passed");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FilterValidation: Real-time preview validation failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Validate performance benchmarking functionality
        /// </summary>
        private bool ValidatePerformanceBenchmarks()
        {
            try
            {
                // Test performance benchmarking
                var parameters = new FilterParameters(FilterType.Grayscale) { intensity = 0.8f };
                var performanceData = filterManager.BenchmarkFilterPerformance(FilterType.Grayscale, parameters, 5);
                
                if (performanceData == null)
                {
                    Debug.LogError("FilterValidation: Performance benchmark returned null");
                    return false;
                }
                
                if (performanceData.iterations != 5)
                {
                    Debug.LogError("FilterValidation: Performance benchmark iteration count incorrect");
                    return false;
                }
                
                if (performanceData.averageTime <= 0f || performanceData.fps <= 0f)
                {
                    Debug.LogError("FilterValidation: Performance benchmark metrics invalid");
                    return false;
                }
                
                // Test performance metrics
                var metrics = filterManager.GetPerformanceMetrics();
                if (metrics == null)
                {
                    Debug.LogError("FilterValidation: Performance metrics returned null");
                    return false;
                }
                
                Debug.Log("FilterValidation: Performance benchmarks validation passed");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FilterValidation: Performance benchmarks validation failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Validate filter layering (multiple filters active simultaneously)
        /// </summary>
        [ContextMenu("Validate Filter Layering")]
        public void ValidateFilterLayering()
        {
            Debug.Log("FilterValidation: Testing filter layering...");
            
            InitializeFilterManager();
            
            try
            {
                // Apply multiple filters
                filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale) { intensity = 0.5f });
                filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement) { intensity = 0.7f });
                filterManager.ApplyFilter(FilterType.EdgeDetection, new FilterParameters(FilterType.EdgeDetection) { intensity = 0.3f });
                
                // Verify all filters are active
                if (filterManager.ActiveFilters.Count != 3)
                {
                    Debug.LogError($"FilterValidation: Expected 3 active filters, got {filterManager.ActiveFilters.Count}");
                    return;
                }
                
                // Verify each filter type is present
                bool hasGrayscale = false, hasContrast = false, hasEdge = false;
                
                foreach (var filter in filterManager.ActiveFilters)
                {
                    switch (filter.type)
                    {
                        case FilterType.Grayscale:
                            hasGrayscale = true;
                            break;
                        case FilterType.ContrastEnhancement:
                            hasContrast = true;
                            break;
                        case FilterType.EdgeDetection:
                            hasEdge = true;
                            break;
                    }
                }
                
                if (!hasGrayscale || !hasContrast || !hasEdge)
                {
                    Debug.LogError("FilterValidation: Not all filter types present in layered filters");
                    return;
                }
                
                Debug.Log("FilterValidation: Filter layering validation PASSED");
                
                // Clean up
                filterManager.ClearAllFilters();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FilterValidation: Filter layering validation failed: {e.Message}");
            }
        }
    }
}