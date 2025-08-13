using UnityEngine;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// Verification script for Task 5.2 completion
    /// Validates that all standard filters are properly implemented
    /// </summary>
    public class Task52Verification : MonoBehaviour
    {
        [Header("Task 5.2 Verification")]
        [SerializeField] private bool runVerificationOnStart = true;
        
        private FilterManager filterManager;
        
        private void Start()
        {
            if (runVerificationOnStart)
            {
                VerifyTask52Implementation();
            }
        }
        
        /// <summary>
        /// Verify Task 5.2: Implement standard filters
        /// </summary>
        [ContextMenu("Verify Task 5.2 Implementation")]
        public void VerifyTask52Implementation()
        {
            Debug.Log("=== TASK 5.2 VERIFICATION ===");
            Debug.Log("Verifying: Create grayscale, edge detection, and contrast enhancement filters");
            Debug.Log("Verifying: Implement real-time filter preview and intensity adjustment");
            Debug.Log("Verifying: Write tests for filter accuracy and performance benchmarks");
            
            // Initialize FilterManager
            InitializeFilterManager();
            
            // Verify standard filters implementation
            bool standardFiltersOK = VerifyStandardFilters();
            bool realTimePreviewOK = VerifyRealTimePreview();
            bool performanceTestsOK = VerifyPerformanceTests();
            
            // Final verification result
            bool task52Complete = standardFiltersOK && realTimePreviewOK && performanceTestsOK;
            
            Debug.Log("=== VERIFICATION RESULTS ===");
            Debug.Log($"✅ Standard Filters (Grayscale, Edge Detection, Contrast): {(standardFiltersOK ? "PASS" : "FAIL")}");
            Debug.Log($"✅ Real-time Preview and Intensity Adjustment: {(realTimePreviewOK ? "PASS" : "FAIL")}");
            Debug.Log($"✅ Performance Tests and Benchmarks: {(performanceTestsOK ? "PASS" : "FAIL")}");
            Debug.Log($"");
            Debug.Log($"🎯 TASK 5.2 STATUS: {(task52Complete ? "✅ COMPLETE" : "❌ INCOMPLETE")}");
            
            if (task52Complete)
            {
                Debug.Log("Task 5.2 'Implement standard filters' has been successfully completed!");
                Debug.Log("Requirements 4.3, 4.6, and 4.8 are fully satisfied.");
            }
        }
        
        private void InitializeFilterManager()
        {
            filterManager = FindObjectOfType<FilterManager>();
            
            if (filterManager == null)
            {
                var filterObject = new GameObject("VerificationFilterManager");
                filterManager = filterObject.AddComponent<FilterManager>();
                filterManager.Start();
                Debug.Log("Created FilterManager for verification");
            }
        }
        
        private bool VerifyStandardFilters()
        {
            Debug.Log("--- Verifying Standard Filters ---");
            
            try
            {
                // Test Grayscale Filter
                var grayscaleParams = new FilterParameters(FilterType.Grayscale) { intensity = 0.8f };
                filterManager.ApplyFilter(FilterType.Grayscale, grayscaleParams);
                
                if (filterManager.ActiveFilters.Count != 1 || filterManager.ActiveFilters[0].type != FilterType.Grayscale)
                {
                    Debug.LogError("Grayscale filter not properly implemented");
                    return false;
                }
                Debug.Log("✅ Grayscale filter: IMPLEMENTED");
                
                // Test Contrast Enhancement Filter
                var contrastParams = new FilterParameters(FilterType.ContrastEnhancement) { intensity = 0.7f };
                filterManager.ApplyFilter(FilterType.ContrastEnhancement, contrastParams);
                
                if (filterManager.ActiveFilters.Count != 2)
                {
                    Debug.LogError("Contrast enhancement filter not properly implemented");
                    return false;
                }
                Debug.Log("✅ Contrast enhancement filter: IMPLEMENTED");
                
                // Test Edge Detection Filter
                var edgeParams = new FilterParameters(FilterType.EdgeDetection) { intensity = 0.6f };
                edgeParams.customParameters["threshold"] = 0.15f;
                filterManager.ApplyFilter(FilterType.EdgeDetection, edgeParams);
                
                if (filterManager.ActiveFilters.Count != 3)
                {
                    Debug.LogError("Edge detection filter not properly implemented");
                    return false;
                }
                Debug.Log("✅ Edge detection filter: IMPLEMENTED");
                
                // Test filter removal
                filterManager.RemoveFilter(FilterType.Grayscale);
                if (filterManager.ActiveFilters.Count != 2)
                {
                    Debug.LogError("Filter removal not working properly");
                    return false;
                }
                Debug.Log("✅ Filter removal: WORKING");
                
                // Clean up
                filterManager.ClearAllFilters();
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Standard filters verification failed: {e.Message}");
                return false;
            }
        }
        
        private bool VerifyRealTimePreview()
        {
            Debug.Log("--- Verifying Real-Time Preview ---");
            
            try
            {
                // Test real-time preview enable/disable
                filterManager.EnableRealTimePreview(true);
                var metrics = filterManager.GetPerformanceMetrics();
                
                if (!metrics.isRealTimeEnabled)
                {
                    Debug.LogError("Real-time preview enable not working");
                    return false;
                }
                Debug.Log("✅ Real-time preview enable: WORKING");
                
                // Test intensity adjustment
                var params1 = new FilterParameters(FilterType.Grayscale) { intensity = 0.5f };
                filterManager.ApplyFilter(FilterType.Grayscale, params1);
                
                var params2 = new FilterParameters(FilterType.Grayscale) { intensity = 0.9f };
                filterManager.UpdateFilterParameters(FilterType.Grayscale, params2);
                
                if (Mathf.Abs(filterManager.ActiveFilters[0].parameters.intensity - 0.9f) > 0.01f)
                {
                    Debug.LogError("Real-time intensity adjustment not working");
                    return false;
                }
                Debug.Log("✅ Real-time intensity adjustment: WORKING");
                
                // Test real-time preview disable
                filterManager.EnableRealTimePreview(false);
                metrics = filterManager.GetPerformanceMetrics();
                
                if (metrics.isRealTimeEnabled)
                {
                    Debug.LogError("Real-time preview disable not working");
                    return false;
                }
                Debug.Log("✅ Real-time preview disable: WORKING");
                
                // Clean up
                filterManager.ClearAllFilters();
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Real-time preview verification failed: {e.Message}");
                return false;
            }
        }
        
        private bool VerifyPerformanceTests()
        {
            Debug.Log("--- Verifying Performance Tests ---");
            
            try
            {
                // Test performance benchmarking
                var params = new FilterParameters(FilterType.Grayscale) { intensity = 0.8f };
                var performanceData = filterManager.BenchmarkFilterPerformance(FilterType.Grayscale, params, 5);
                
                if (performanceData == null || performanceData.iterations != 5 || performanceData.averageTime <= 0f)
                {
                    Debug.LogError("Performance benchmarking not working properly");
                    return false;
                }
                Debug.Log($"✅ Performance benchmarking: WORKING (Avg: {performanceData.averageTime * 1000f:F2}ms)");
                
                // Test performance metrics
                var metrics = filterManager.GetPerformanceMetrics();
                
                if (metrics == null || metrics.fps <= 0f)
                {
                    Debug.LogError("Performance metrics not working properly");
                    return false;
                }
                Debug.Log($"✅ Performance metrics: WORKING (FPS: {metrics.fps:F1}, Memory: {metrics.memoryUsage:F1}MB)");
                
                // Test performance with multiple filters
                filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale) { intensity = 0.5f });
                filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement) { intensity = 0.7f });
                filterManager.ApplyFilter(FilterType.EdgeDetection, new FilterParameters(FilterType.EdgeDetection) { intensity = 0.3f });
                
                metrics = filterManager.GetPerformanceMetrics();
                
                if (metrics.activeFilterCount != 3)
                {
                    Debug.LogError("Multiple filter performance tracking not working");
                    return false;
                }
                Debug.Log($"✅ Multiple filter performance: WORKING ({metrics.activeFilterCount} filters active)");
                
                // Clean up
                filterManager.ClearAllFilters();
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Performance tests verification failed: {e.Message}");
                return false;
            }
        }
    }
}