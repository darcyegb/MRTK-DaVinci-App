using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DaVinciEye.SessionManagement
{
    /// <summary>
    /// Complete session data structure containing all app state information
    /// </summary>
    [System.Serializable]
    public class SessionData
    {
        [Header("Session Information")]
        public string sessionId;
        public DateTime createdAt;
        public DateTime lastModified;
        
        [Header("Image and Canvas Data")]
        public string currentImagePath;
        public CanvasData canvasData;
        public ImageAdjustments imageAdjustments;
        
        [Header("Filter and Processing")]
        public FilterSettings filterSettings;
        
        [Header("Application State")]
        public AppState appState;
        
        public SessionData()
        {
            sessionId = Guid.NewGuid().ToString();
            createdAt = DateTime.Now;
            lastModified = DateTime.Now;
            currentImagePath = "";
            canvasData = null;
            imageAdjustments = new ImageAdjustments();
            filterSettings = new FilterSettings();
            appState = new AppState();
        }
        
        /// <summary>
        /// Get session duration
        /// </summary>
        public TimeSpan GetSessionDuration()
        {
            return lastModified - createdAt;
        }
        
        /// <summary>
        /// Check if session has any modifications
        /// </summary>
        public bool HasModifications()
        {
            return imageAdjustments.isModified || 
                   filterSettings.HasActiveFilters() || 
                   canvasData != null || 
                   !string.IsNullOrEmpty(currentImagePath);
        }
        
        /// <summary>
        /// Get formatted session info
        /// </summary>
        public string GetSessionInfo()
        {
            return $"Session: {sessionId.Substring(0, 8)}..., Created: {createdAt:yyyy-MM-dd HH:mm}, Duration: {GetSessionDuration().TotalMinutes:F1}min";
        }
    }
    
    /// <summary>
    /// Canvas data structure from design document
    /// </summary>
    [System.Serializable]
    public class CanvasData
    {
        public Vector3[] corners;
        public Vector3 center;
        public Vector2 dimensions;
        public string anchorId;
        public DateTime createdAt;
        
        public CanvasData()
        {
            corners = new Vector3[4];
            center = Vector3.zero;
            dimensions = Vector2.zero;
            anchorId = "";
            createdAt = DateTime.Now;
        }
        
        public bool IsValid()
        {
            return corners != null && corners.Length == 4 && dimensions.magnitude > 0;
        }
    }
    
    /// <summary>
    /// Image adjustments data structure from design document
    /// </summary>
    [System.Serializable]
    public class ImageAdjustments
    {
        [Header("Crop Settings")]
        public Rect cropArea;
        
        [Header("Color Adjustments")]
        public float contrast;
        public float exposure;
        public float hue;
        public float saturation;
        public float opacity;
        
        [Header("State")]
        public bool isModified;
        
        public ImageAdjustments()
        {
            cropArea = new Rect(0, 0, 1, 1); // Full image by default
            contrast = 0f;
            exposure = 0f;
            hue = 0f;
            saturation = 0f;
            opacity = 1f;
            isModified = false;
        }
        
        /// <summary>
        /// Reset all adjustments to default values
        /// </summary>
        public void Reset()
        {
            cropArea = new Rect(0, 0, 1, 1);
            contrast = 0f;
            exposure = 0f;
            hue = 0f;
            saturation = 0f;
            opacity = 1f;
            isModified = false;
        }
        
        /// <summary>
        /// Check if any adjustments have been made
        /// </summary>
        public bool HasAdjustments()
        {
            return isModified || 
                   contrast != 0f || 
                   exposure != 0f || 
                   hue != 0f || 
                   saturation != 0f || 
                   opacity != 1f ||
                   !cropArea.Equals(new Rect(0, 0, 1, 1));
        }
        
        /// <summary>
        /// Clone the adjustments
        /// </summary>
        public ImageAdjustments Clone()
        {
            return new ImageAdjustments
            {
                cropArea = cropArea,
                contrast = contrast,
                exposure = exposure,
                hue = hue,
                saturation = saturation,
                opacity = opacity,
                isModified = isModified
            };
        }
    }
    
    /// <summary>
    /// Filter settings data structure
    /// </summary>
    [System.Serializable]
    public class FilterSettings
    {
        [Header("Standard Filters")]
        public FilterData grayscaleFilter;
        public FilterData edgeDetectionFilter;
        public FilterData contrastEnhancementFilter;
        
        [Header("Color Filters")]
        public List<ColorRangeFilterData> colorRangeFilters;
        public ColorReductionFilterData colorReductionFilter;
        
        [Header("Filter State")]
        public bool filtersEnabled;
        public float globalFilterIntensity;
        
        public FilterSettings()
        {
            grayscaleFilter = new FilterData { type = FilterType.Grayscale, enabled = false, intensity = 1f };
            edgeDetectionFilter = new FilterData { type = FilterType.EdgeDetection, enabled = false, intensity = 1f };
            contrastEnhancementFilter = new FilterData { type = FilterType.ContrastEnhancement, enabled = false, intensity = 1f };
            
            colorRangeFilters = new List<ColorRangeFilterData>();
            colorReductionFilter = new ColorReductionFilterData();
            
            filtersEnabled = true;
            globalFilterIntensity = 1f;
        }
        
        /// <summary>
        /// Check if any filters are active
        /// </summary>
        public bool HasActiveFilters()
        {
            return filtersEnabled && (
                grayscaleFilter.enabled ||
                edgeDetectionFilter.enabled ||
                contrastEnhancementFilter.enabled ||
                colorRangeFilters.Any(f => f.enabled) ||
                colorReductionFilter.enabled
            );
        }
        
        /// <summary>
        /// Get count of active filters
        /// </summary>
        public int GetActiveFilterCount()
        {
            if (!filtersEnabled) return 0;
            
            int count = 0;
            if (grayscaleFilter.enabled) count++;
            if (edgeDetectionFilter.enabled) count++;
            if (contrastEnhancementFilter.enabled) count++;
            count += colorRangeFilters.Count(f => f.enabled);
            if (colorReductionFilter.enabled) count++;
            
            return count;
        }
        
        /// <summary>
        /// Reset all filters
        /// </summary>
        public void ResetAllFilters()
        {
            grayscaleFilter.enabled = false;
            edgeDetectionFilter.enabled = false;
            contrastEnhancementFilter.enabled = false;
            
            colorRangeFilters.Clear();
            colorReductionFilter.enabled = false;
            
            globalFilterIntensity = 1f;
        }
        
        /// <summary>
        /// Clone the filter settings
        /// </summary>
        public FilterSettings Clone()
        {
            var clone = new FilterSettings
            {
                grayscaleFilter = grayscaleFilter.Clone(),
                edgeDetectionFilter = edgeDetectionFilter.Clone(),
                contrastEnhancementFilter = contrastEnhancementFilter.Clone(),
                colorReductionFilter = colorReductionFilter.Clone(),
                filtersEnabled = filtersEnabled,
                globalFilterIntensity = globalFilterIntensity
            };
            
            clone.colorRangeFilters = colorRangeFilters.Select(f => f.Clone()).ToList();
            
            return clone;
        }
    }
    
    /// <summary>
    /// Application state data
    /// </summary>
    [System.Serializable]
    public class AppState
    {
        [Header("Current Mode")]
        public AppMode currentMode;
        
        [Header("UI State")]
        public bool mainMenuVisible;
        public bool adjustmentPanelVisible;
        public bool filterPanelVisible;
        public bool colorAnalysisPanelVisible;
        
        [Header("Interaction Settings")]
        public bool handGesturesEnabled;
        public bool voiceCommandsEnabled;
        public bool realTimePreviewEnabled;
        
        [Header("Performance Settings")]
        public QualityLevel qualityLevel;
        public bool adaptiveQualityEnabled;
        
        public AppState()
        {
            currentMode = AppMode.CanvasDefinition;
            mainMenuVisible = true;
            adjustmentPanelVisible = false;
            filterPanelVisible = false;
            colorAnalysisPanelVisible = false;
            handGesturesEnabled = true;
            voiceCommandsEnabled = true;
            realTimePreviewEnabled = true;
            qualityLevel = QualityLevel.High;
            adaptiveQualityEnabled = true;
        }
    }
    
    /// <summary>
    /// Filter data structure
    /// </summary>
    [System.Serializable]
    public class FilterData
    {
        public FilterType type;
        public bool enabled;
        public float intensity;
        public Dictionary<string, float> parameters;
        
        public FilterData()
        {
            type = FilterType.None;
            enabled = false;
            intensity = 1f;
            parameters = new Dictionary<string, float>();
        }
        
        public FilterData Clone()
        {
            return new FilterData
            {
                type = type,
                enabled = enabled,
                intensity = intensity,
                parameters = new Dictionary<string, float>(parameters)
            };
        }
    }
    
    /// <summary>
    /// Color range filter data
    /// </summary>
    [System.Serializable]
    public class ColorRangeFilterData
    {
        public bool enabled;
        public Color targetColor;
        public float hueRange;
        public float saturationRange;
        public float brightnessRange;
        public float intensity;
        public bool showOriginalColors;
        
        public ColorRangeFilterData()
        {
            enabled = false;
            targetColor = Color.white;
            hueRange = 0.1f;
            saturationRange = 0.3f;
            brightnessRange = 0.3f;
            intensity = 1f;
            showOriginalColors = true;
        }
        
        public ColorRangeFilterData Clone()
        {
            return new ColorRangeFilterData
            {
                enabled = enabled,
                targetColor = targetColor,
                hueRange = hueRange,
                saturationRange = saturationRange,
                brightnessRange = brightnessRange,
                intensity = intensity,
                showOriginalColors = showOriginalColors
            };
        }
    }
    
    /// <summary>
    /// Color reduction filter data
    /// </summary>
    [System.Serializable]
    public class ColorReductionFilterData
    {
        public bool enabled;
        public int targetColorCount;
        public float intensity;
        public ColorQuantizationMethod quantizationMethod;
        
        public ColorReductionFilterData()
        {
            enabled = false;
            targetColorCount = 16;
            intensity = 1f;
            quantizationMethod = ColorQuantizationMethod.KMeans;
        }
        
        public ColorReductionFilterData Clone()
        {
            return new ColorReductionFilterData
            {
                enabled = enabled,
                targetColorCount = targetColorCount,
                intensity = intensity,
                quantizationMethod = quantizationMethod
            };
        }
    }
    
    /// <summary>
    /// Session summary for quick overview
    /// </summary>
    [System.Serializable]
    public class SessionSummary
    {
        public string sessionId;
        public DateTime createdAt;
        public DateTime lastModified;
        public bool hasCanvasData;
        public bool hasImageLoaded;
        public bool hasImageAdjustments;
        public int activeFilterCount;
        public AppMode currentMode;
        
        public SessionSummary()
        {
            sessionId = "";
            createdAt = default;
            lastModified = default;
            hasCanvasData = false;
            hasImageLoaded = false;
            hasImageAdjustments = false;
            activeFilterCount = 0;
            currentMode = AppMode.CanvasDefinition;
        }
        
        public string GetSummaryText()
        {
            var parts = new List<string>();
            
            if (hasCanvasData) parts.Add("Canvas Defined");
            if (hasImageLoaded) parts.Add("Image Loaded");
            if (hasImageAdjustments) parts.Add("Adjustments Applied");
            if (activeFilterCount > 0) parts.Add($"{activeFilterCount} Filters");
            
            return parts.Count > 0 ? string.Join(", ", parts) : "Empty Session";
        }
    }
    
    /// <summary>
    /// Application modes
    /// </summary>
    public enum AppMode
    {
        CanvasDefinition,
        ImageOverlay,
        ImageAdjustment,
        FilterApplication,
        ColorAnalysis
    }
    
    /// <summary>
    /// Filter types
    /// </summary>
    public enum FilterType
    {
        None,
        Grayscale,
        EdgeDetection,
        ContrastEnhancement,
        ColorRange,
        ColorReduction
    }
    
    /// <summary>
    /// Quality levels
    /// </summary>
    public enum QualityLevel
    {
        Low,
        Medium,
        High,
        Ultra
    }
    
    /// <summary>
    /// Color quantization methods
    /// </summary>
    public enum ColorQuantizationMethod
    {
        KMeans,
        MedianCut,
        Octree,
        Uniform
    }
}