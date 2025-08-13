using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// Enumeration of available filter types
    /// </summary>
    public enum FilterType
    {
        None,
        Grayscale,
        EdgeDetection,
        ContrastEnhancement,
        ColorRange,
        ColorReduction,
        Blur,
        Sharpen
    }
    
    /// <summary>
    /// Parameters for configuring filter effects
    /// </summary>
    [System.Serializable]
    public class FilterParameters
    {
        [Header("Basic Parameters")]
        public FilterType type = FilterType.None;
        
        [Range(0f, 1f)]
        public float intensity = 1f;
        
        [Header("Color-Specific Parameters")]
        public Color targetColor = Color.white;
        
        [Range(0f, 1f)]
        public float colorTolerance = 0.1f;
        
        [Range(2, 256)]
        public int targetColorCount = 16;
        
        [Header("Custom Parameters")]
        public Dictionary<string, float> customParameters = new Dictionary<string, float>();
        
        public FilterParameters()
        {
            customParameters = new Dictionary<string, float>();
        }
        
        public FilterParameters(FilterType filterType)
        {
            type = filterType;
            customParameters = new Dictionary<string, float>();
            SetDefaultsForType(filterType);
        }
        
        private void SetDefaultsForType(FilterType filterType)
        {
            switch (filterType)
            {
                case FilterType.Grayscale:
                    intensity = 1f;
                    break;
                case FilterType.EdgeDetection:
                    intensity = 0.5f;
                    customParameters["threshold"] = 0.1f;
                    break;
                case FilterType.ContrastEnhancement:
                    intensity = 0.5f;
                    break;
                case FilterType.ColorRange:
                    intensity = 1f;
                    colorTolerance = 0.2f;
                    break;
                case FilterType.ColorReduction:
                    intensity = 1f;
                    targetColorCount = 16;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Data structure for active filter information
    /// </summary>
    [System.Serializable]
    public class FilterData
    {
        public FilterType type;
        public FilterParameters parameters;
        public bool isActive;
        public int layerOrder;
        public DateTime appliedAt;
        
        public FilterData(FilterType filterType, FilterParameters filterParameters)
        {
            type = filterType;
            parameters = filterParameters;
            isActive = true;
            layerOrder = 0;
            appliedAt = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Performance benchmarking data for filter operations
    /// </summary>
    [System.Serializable]
    public class FilterPerformanceData
    {
        public FilterType filterType;
        public int iterations;
        public float startTime;
        public float endTime;
        public float totalTime;
        public float averageTime;
        public float minTime;
        public float maxTime;
        public float fps;
        
        public override string ToString()
        {
            return $"Filter: {filterType}, Iterations: {iterations}, Avg: {averageTime * 1000f:F2}ms, FPS: {fps:F1}";
        }
    }
    
    /// <summary>
    /// Real-time performance metrics for filter processing
    /// </summary>
    [System.Serializable]
    public class FilterPerformanceMetrics
    {
        public int activeFilterCount;
        public float memoryUsage; // MB
        public float frameTime; // seconds
        public float fps;
        public bool isRealTimeEnabled;
        
        public bool IsPerformanceGood => fps >= 60f && memoryUsage < 512f;
        
        public override string ToString()
        {
            return $"Filters: {activeFilterCount}, Memory: {memoryUsage:F1}MB, FPS: {fps:F1}, RealTime: {isRealTimeEnabled}";
        }
    }
}