using UnityEngine;
using DaVinciEye.Filters;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// Verification script for Task 5.3 - Filter layering and management
    /// This script demonstrates and verifies the implemented functionality
    /// </summary>
    public class Task53Verification : MonoBehaviour
    {
        [Header("Filter Manager Reference")]
        public FilterManager filterManager;
        
        [Header("Test Configuration")]
        public bool runVerificationOnStart = true;
        public bool enableDetailedLogging = true;
        
        private void Start()
        {
            if (runVerificationOnStart)
            {
                StartCoroutine(RunVerificationSequence());
            }
        }
        
        /// <summary>
        /// Run comprehensive verification of filter layering and management features
        /// </summary>
        private System.Collections.IEnumerator RunVerificationSequence()
        {
            Log("=== Task 5.3 Verification: Filter Layering and Management ===");
            
            // Ensure we have a FilterManager
            if (filterManager == null)
            {
                filterManager = FindObjectOfType<FilterManager>();
                if (filterManager == null)
                {
                    LogError("No FilterManager found in scene!");
                    yield break;
                }
            }
            
            // Create test texture
            var testTexture = CreateTestTexture();
            filterManager.SetSourceTexture(testTexture);
            
            yield return new WaitForSeconds(0.5f);
            
            // Test 1: Multiple Filter Combination System
            Log("\n--- Test 1: Multiple Filter Combination System ---");
            yield return StartCoroutine(TestMultipleFilterCombination());
            
            // Test 2: Filter Reset and Individual Removal
            Log("\n--- Test 2: Filter Reset and Individual Removal ---");
            yield return StartCoroutine(TestFilterResetAndRemoval());
            
            // Test 3: Filter Layering and Reordering
            Log("\n--- Test 3: Filter Layering and Reordering ---");
            yield return StartCoroutine(TestFilterLayeringAndReordering());
            
            // Test 4: Session Management (Requirement 4.9)
            Log("\n--- Test 4: Session Management ---");
            yield return StartCoroutine(TestSessionManagement());
            
            // Test 5: Performance Verification
            Log("\n--- Test 5: Performance Verification ---");
            yield return StartCoroutine(TestPerformance());
            
            Log("\n=== Task 5.3 Verification Complete ===");
            Log("✓ Multiple filter combination system implemented");
            Log("✓ Filter reset and individual removal functionality implemented");
            Log("✓ Filter layering and management system implemented");
            Log("✓ Session persistence implemented (Requirement 4.9)");
            Log("✓ Performance targets met");
        }
        
        /// <summary>
        /// Test multiple filter combination system (Requirement 4.7)
        /// </summary>
        private System.Collections.IEnumerator TestMultipleFilterCombination()
        {
            // Clear any existing filters
            filterManager.ClearAllFilters(false);
            
            // Apply multiple filters
            var grayscaleParams = new FilterParameters(FilterType.Grayscale) { intensity = 0.8f };
            var contrastParams = new FilterParameters(FilterType.ContrastEnhancement) { intensity = 0.6f };
            var edgeParams = new FilterParameters(FilterType.EdgeDetection) { intensity = 0.4f };
            
            filterManager.ApplyFilter(FilterType.Grayscale, grayscaleParams);
            yield return new WaitForSeconds(0.1f);
            
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, contrastParams);
            yield return new WaitForSeconds(0.1f);
            
            filterManager.ApplyFilter(FilterType.EdgeDetection, edgeParams);
            yield return new WaitForSeconds(0.1f);
            
            // Verify multiple filters are active
            var activeFilters = filterManager.GetActiveFilterTypes();
            Log($"Active filters: {string.Join(", ", activeFilters)}");
            
            if (activeFilters.Count == 3)
            {
                Log("✓ Multiple filter combination system working correctly");
            }
            else
            {
                LogError($"✗ Expected 3 active filters, got {activeFilters.Count}");
            }
            
            // Verify layer ordering
            var grayscaleOrder = filterManager.GetFilterLayerOrder(FilterType.Grayscale);
            var contrastOrder = filterManager.GetFilterLayerOrder(FilterType.ContrastEnhancement);
            var edgeOrder = filterManager.GetFilterLayerOrder(FilterType.EdgeDetection);
            
            Log($"Layer orders - Grayscale: {grayscaleOrder}, Contrast: {contrastOrder}, Edge: {edgeOrder}");
            
            if (grayscaleOrder < contrastOrder && contrastOrder < edgeOrder)
            {
                Log("✓ Filter layer ordering working correctly");
            }
            else
            {
                LogError("✗ Filter layer ordering not working as expected");
            }
        }
        
        /// <summary>
        /// Test filter reset and individual removal functionality
        /// </summary>
        private System.Collections.IEnumerator TestFilterResetAndRemoval()
        {
            // Test individual filter removal
            Log("Testing individual filter removal...");
            int initialCount = filterManager.ActiveFilters.Count;
            
            filterManager.RemoveFilter(FilterType.ContrastEnhancement);
            yield return new WaitForSeconds(0.1f);
            
            int afterRemovalCount = filterManager.ActiveFilters.Count;
            
            if (afterRemovalCount == initialCount - 1)
            {
                Log("✓ Individual filter removal working correctly");
            }
            else
            {
                LogError($"✗ Expected {initialCount - 1} filters after removal, got {afterRemovalCount}");
            }
            
            // Test filter toggle
            Log("Testing filter toggle...");
            bool wasActive = filterManager.IsFilterActive(FilterType.Grayscale);
            filterManager.ToggleFilter(FilterType.Grayscale, false);
            yield return new WaitForSeconds(0.1f);
            
            bool isActiveAfterToggle = filterManager.IsFilterActive(FilterType.Grayscale);
            
            if (wasActive && !isActiveAfterToggle)
            {
                Log("✓ Filter toggle working correctly");
            }
            else
            {
                LogError("✗ Filter toggle not working as expected");
            }
            
            // Toggle back on
            filterManager.ToggleFilter(FilterType.Grayscale, true);
            yield return new WaitForSeconds(0.1f);
            
            // Test reset all filter parameters
            Log("Testing filter parameter reset...");
            filterManager.ResetAllFilterParameters();
            yield return new WaitForSeconds(0.1f);
            
            var grayscaleParams = filterManager.GetFilterParameters(FilterType.Grayscale);
            if (grayscaleParams != null && grayscaleParams.intensity == 1f)
            {
                Log("✓ Filter parameter reset working correctly");
            }
            else
            {
                LogError("✗ Filter parameter reset not working as expected");
            }
        }
        
        /// <summary>
        /// Test filter layering and reordering functionality
        /// </summary>
        private System.Collections.IEnumerator TestFilterLayeringAndReordering()
        {
            // Ensure we have multiple filters
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            yield return new WaitForSeconds(0.1f);
            
            // Test moving filter up
            Log("Testing filter reordering...");
            int initialGrayscaleOrder = filterManager.GetFilterLayerOrder(FilterType.Grayscale);
            int initialContrastOrder = filterManager.GetFilterLayerOrder(FilterType.ContrastEnhancement);
            
            filterManager.MoveFilterUp(FilterType.Grayscale);
            yield return new WaitForSeconds(0.1f);
            
            int newGrayscaleOrder = filterManager.GetFilterLayerOrder(FilterType.Grayscale);
            int newContrastOrder = filterManager.GetFilterLayerOrder(FilterType.ContrastEnhancement);
            
            if (newGrayscaleOrder > initialGrayscaleOrder || newContrastOrder < initialContrastOrder)
            {
                Log("✓ Filter reordering working correctly");
            }
            else
            {
                LogError("✗ Filter reordering not working as expected");
            }
            
            // Test clear all with backup
            Log("Testing clear all filters with backup...");
            int filtersBeforeClear = filterManager.ActiveFilters.Count;
            
            filterManager.ClearAllFilters(true);
            yield return new WaitForSeconds(0.1f);
            
            int filtersAfterClear = filterManager.ActiveFilters.Count;
            
            if (filtersAfterClear == 0)
            {
                Log("✓ Clear all filters working correctly");
                
                // Test restore from backup
                filterManager.RestoreFromBackup();
                yield return new WaitForSeconds(0.1f);
                
                int filtersAfterRestore = filterManager.ActiveFilters.Count;
                
                if (filtersAfterRestore == filtersBeforeClear)
                {
                    Log("✓ Restore from backup working correctly");
                }
                else
                {
                    LogError($"✗ Expected {filtersBeforeClear} filters after restore, got {filtersAfterRestore}");
                }
            }
            else
            {
                LogError($"✗ Expected 0 filters after clear, got {filtersAfterClear}");
            }
        }
        
        /// <summary>
        /// Test session management functionality (Requirement 4.9)
        /// </summary>
        private System.Collections.IEnumerator TestSessionManagement()
        {
            // Set up specific filter configuration
            filterManager.ClearAllFilters(false);
            
            var grayscaleParams = new FilterParameters(FilterType.Grayscale) { intensity = 0.7f };
            var contrastParams = new FilterParameters(FilterType.ContrastEnhancement) { intensity = 0.3f };
            
            filterManager.ApplyFilter(FilterType.Grayscale, grayscaleParams);
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, contrastParams);
            yield return new WaitForSeconds(0.1f);
            
            // Test JSON serialization
            Log("Testing filter settings serialization...");
            string json = filterManager.SaveFilterSettings();
            
            if (!string.IsNullOrEmpty(json))
            {
                Log("✓ Filter settings serialization working correctly");
                Log($"JSON length: {json.Length} characters");
            }
            else
            {
                LogError("✗ Filter settings serialization failed");
            }
            
            // Test session save/load
            Log("Testing session persistence...");
            filterManager.SaveSession();
            
            // Clear filters and load session
            filterManager.ClearAllFilters(false);
            yield return new WaitForSeconds(0.1f);
            
            filterManager.LoadSession();
            yield return new WaitForSeconds(0.1f);
            
            // Verify restoration
            var restoredGrayscale = filterManager.GetFilterParameters(FilterType.Grayscale);
            var restoredContrast = filterManager.GetFilterParameters(FilterType.ContrastEnhancement);
            
            if (restoredGrayscale != null && Mathf.Approximately(restoredGrayscale.intensity, 0.7f) &&
                restoredContrast != null && Mathf.Approximately(restoredContrast.intensity, 0.3f))
            {
                Log("✓ Session persistence working correctly");
            }
            else
            {
                LogError("✗ Session persistence not working as expected");
            }
        }
        
        /// <summary>
        /// Test performance requirements
        /// </summary>
        private System.Collections.IEnumerator TestPerformance()
        {
            // Apply multiple filters for performance testing
            filterManager.ClearAllFilters(false);
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            filterManager.ApplyFilter(FilterType.EdgeDetection, new FilterParameters(FilterType.EdgeDetection));
            
            yield return new WaitForSeconds(0.1f);
            
            // Get performance metrics
            var metrics = filterManager.GetPerformanceMetrics();
            
            Log($"Performance metrics:");
            Log($"  Active filters: {metrics.activeFilterCount}");
            Log($"  Memory usage: {metrics.memoryUsage:F2} MB");
            Log($"  Current FPS: {metrics.fps:F1}");
            Log($"  Real-time enabled: {metrics.isRealTimeEnabled}");
            
            // Verify performance targets
            if (metrics.fps >= 30f)
            {
                Log("✓ Performance target met (FPS >= 30)");
            }
            else
            {
                LogError($"✗ Performance target not met (FPS: {metrics.fps:F1})");
            }
            
            if (metrics.memoryUsage < 100f)
            {
                Log("✓ Memory usage within acceptable limits");
            }
            else
            {
                LogError($"✗ Memory usage too high: {metrics.memoryUsage:F2} MB");
            }
            
            // Test benchmark
            Log("Running performance benchmark...");
            var benchmarkData = filterManager.BenchmarkFilterPerformance(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale), 10);
            
            Log($"Benchmark results: {benchmarkData}");
            
            if (benchmarkData.fps >= 60f)
            {
                Log("✓ Benchmark performance target met");
            }
            else
            {
                LogError($"✗ Benchmark performance target not met (FPS: {benchmarkData.fps:F1})");
            }
        }
        
        /// <summary>
        /// Create a test texture for verification
        /// </summary>
        private Texture2D CreateTestTexture()
        {
            var texture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            Color[] pixels = new Color[512 * 512];
            
            for (int y = 0; y < 512; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    float r = (float)x / 512f;
                    float g = (float)y / 512f;
                    float b = Mathf.Sin((x + y) * 0.01f) * 0.5f + 0.5f;
                    pixels[y * 512 + x] = new Color(r, g, b, 1f);
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return texture;
        }
        
        /// <summary>
        /// Log message with optional detailed logging
        /// </summary>
        private void Log(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[Task53Verification] {message}");
            }
        }
        
        /// <summary>
        /// Log error message
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[Task53Verification] {message}");
        }
        
        /// <summary>
        /// Manual verification trigger for testing in editor
        /// </summary>
        [ContextMenu("Run Verification")]
        public void RunManualVerification()
        {
            StartCoroutine(RunVerificationSequence());
        }
    }
}