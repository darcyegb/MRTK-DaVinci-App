using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// HSV-based color range filtering system for isolating specific color ranges
    /// Implements Requirements: 4.1.1, 4.1.2, 4.1.3
    /// </summary>
    public class ColorRangeFilter : MonoBehaviour
    {
        [Header("Color Range Configuration")]
        [SerializeField] private ColorRangeSettings currentRange;
        [SerializeField] private List<ColorRangeSettings> activeRanges = new List<ColorRangeSettings>();
        
        [Header("Processing Settings")]
        [SerializeField] private Material colorRangeMaterial;
        [SerializeField] private RenderTexture processedTexture;
        [SerializeField] private bool realTimePreview = true;
        
        // HSV color range parameters
        [System.Serializable]
        public class ColorRangeSettings
        {
            [Header("HSV Range Parameters")]
            [Range(0f, 360f)] public float hueMin = 0f;
            [Range(0f, 360f)] public float hueMax = 360f;
            [Range(0f, 1f)] public float saturationMin = 0f;
            [Range(0f, 1f)] public float saturationMax = 1f;
            [Range(0f, 1f)] public float valueMin = 0f;
            [Range(0f, 1f)] public float valueMax = 1f;
            
            [Header("Display Options")]
            public bool showOriginalColors = true;
            public bool showAsHighlight = false;
            public Color highlightColor = Color.yellow;
            [Range(0f, 1f)] public float highlightIntensity = 0.5f;
            
            [Header("Range Info")]
            public string rangeName = "Color Range";
            public bool isActive = true;
            public int rangeId;
            
            public ColorRangeSettings()
            {
                rangeId = UnityEngine.Random.Range(1000, 9999);
            }
            
            /// <summary>
            /// Check if a color (in HSV) falls within this range
            /// </summary>
            public bool IsColorInRange(Vector3 hsvColor)
            {
                float h = hsvColor.x * 360f; // Convert to 0-360 range
                float s = hsvColor.y;
                float v = hsvColor.z;
                
                // Handle hue wraparound (e.g., red range crossing 0/360)
                bool hueInRange;
                if (hueMin <= hueMax)
                {
                    hueInRange = h >= hueMin && h <= hueMax;
                }
                else
                {
                    // Wraparound case (e.g., 350-30 for red)
                    hueInRange = h >= hueMin || h <= hueMax;
                }
                
                bool saturationInRange = s >= saturationMin && s <= saturationMax;
                bool valueInRange = v >= valueMin && v <= valueMax;
                
                return hueInRange && saturationInRange && valueInRange;
            }
            
            /// <summary>
            /// Get the range as a Vector4 for shader use (hueMin, hueMax, satMin, satMax)
            /// </summary>
            public Vector4 GetHueSaturationRange()
            {
                return new Vector4(hueMin / 360f, hueMax / 360f, saturationMin, saturationMax);
            }
            
            /// <summary>
            /// Get the value range as a Vector2 for shader use
            /// </summary>
            public Vector2 GetValueRange()
            {
                return new Vector2(valueMin, valueMax);
            }
        }
        
        // Events
        public event Action<ColorRangeSettings> OnRangeAdded;
        public event Action<int> OnRangeRemoved;
        public event Action<Texture2D> OnFilterApplied;
        
        // Properties
        public List<ColorRangeSettings> ActiveRanges => activeRanges;
        public ColorRangeSettings CurrentRange => currentRange;
        public bool RealTimePreview { get => realTimePreview; set => realTimePreview = value; }
        
        private void Awake()
        {
            InitializeColorRangeFilter();
        }
        
        private void Start()
        {
            CreateDefaultColorRangeMaterial();
        }
        
        /// <summary>
        /// Initialize the color range filter system
        /// </summary>
        private void InitializeColorRangeFilter()
        {
            if (currentRange == null)
            {
                currentRange = new ColorRangeSettings();
                currentRange.rangeName = "Default Range";
            }
            
            Debug.Log("ColorRangeFilter: Initialized HSV-based color range filtering system");
        }
        
        /// <summary>
        /// Create default material for color range processing if not assigned
        /// </summary>
        private void CreateDefaultColorRangeMaterial()
        {
            if (colorRangeMaterial == null)
            {
                // Create a simple color range shader material
                Shader colorRangeShader = Shader.Find("Hidden/DaVinciEye/ColorRange");
                if (colorRangeShader == null)
                {
                    // Fallback to Unlit shader if custom shader not found
                    colorRangeShader = Shader.Find("Universal Render Pipeline/Unlit");
                }
                
                colorRangeMaterial = new Material(colorRangeShader);
                colorRangeMaterial.name = "ColorRangeFilterMaterial";
            }
        }
        
        /// <summary>
        /// Set the current color range parameters
        /// Implements real-time parameter adjustment (Requirement 4.1.3)
        /// </summary>
        public void SetColorRange(float hueMin, float hueMax, float satMin, float satMax, float valueMin, float valueMax)
        {
            if (currentRange == null)
            {
                currentRange = new ColorRangeSettings();
            }
            
            currentRange.hueMin = Mathf.Clamp(hueMin, 0f, 360f);
            currentRange.hueMax = Mathf.Clamp(hueMax, 0f, 360f);
            currentRange.saturationMin = Mathf.Clamp01(satMin);
            currentRange.saturationMax = Mathf.Clamp01(satMax);
            currentRange.valueMin = Mathf.Clamp01(valueMin);
            currentRange.valueMax = Mathf.Clamp01(valueMax);
            
            if (realTimePreview)
            {
                ApplyColorRangeFilter();
            }
            
            Debug.Log($"ColorRangeFilter: Set range - H:{hueMin}-{hueMax}, S:{satMin}-{satMax}, V:{valueMin}-{valueMax}");
        }
        
        /// <summary>
        /// Set color range from a target color with tolerance
        /// Convenient method for quick color selection
        /// </summary>
        public void SetColorRangeFromColor(Color targetColor, float tolerance = 0.2f)
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
            
            SetColorRange(hueMin, hueMax, satMin, satMax, valueMin, valueMax);
            
            Debug.Log($"ColorRangeFilter: Set range from color {targetColor} with tolerance {tolerance}");
        }
        
        /// <summary>
        /// Add a new color range to the active ranges list
        /// Implements multiple color range support (Requirement 4.1.4)
        /// </summary>
        public void AddColorRange(ColorRangeSettings range)
        {
            if (range == null)
            {
                Debug.LogWarning("ColorRangeFilter: Cannot add null color range");
                return;
            }
            
            // Ensure unique range ID
            while (activeRanges.Exists(r => r.rangeId == range.rangeId))
            {
                range.rangeId = UnityEngine.Random.Range(1000, 9999);
            }
            
            activeRanges.Add(range);
            OnRangeAdded?.Invoke(range);
            
            if (realTimePreview)
            {
                ApplyColorRangeFilter();
            }
            
            Debug.Log($"ColorRangeFilter: Added color range '{range.rangeName}' (ID: {range.rangeId})");
        }
        
        /// <summary>
        /// Remove a color range by ID
        /// </summary>
        public bool RemoveColorRange(int rangeId)
        {
            var rangeToRemove = activeRanges.Find(r => r.rangeId == rangeId);
            if (rangeToRemove != null)
            {
                activeRanges.Remove(rangeToRemove);
                OnRangeRemoved?.Invoke(rangeId);
                
                if (realTimePreview)
                {
                    ApplyColorRangeFilter();
                }
                
                Debug.Log($"ColorRangeFilter: Removed color range ID {rangeId}");
                return true;
            }
            
            Debug.LogWarning($"ColorRangeFilter: Color range ID {rangeId} not found");
            return false;
        }
        
        /// <summary>
        /// Clear all active color ranges
        /// </summary>
        public void ClearAllRanges()
        {
            int removedCount = activeRanges.Count;
            activeRanges.Clear();
            
            if (realTimePreview)
            {
                ApplyColorRangeFilter();
            }
            
            Debug.Log($"ColorRangeFilter: Cleared {removedCount} color ranges");
        }
        
        /// <summary>
        /// Toggle a color range active state
        /// </summary>
        public void ToggleColorRange(int rangeId, bool isActive)
        {
            var range = activeRanges.Find(r => r.rangeId == rangeId);
            if (range != null)
            {
                range.isActive = isActive;
                
                if (realTimePreview)
                {
                    ApplyColorRangeFilter();
                }
                
                Debug.Log($"ColorRangeFilter: Toggled range {rangeId} {(isActive ? "ON" : "OFF")}");
            }
        }
        
        /// <summary>
        /// Apply color range filtering to the source texture
        /// Implements HSV-based color isolation (Requirements 4.1.1, 4.1.2)
        /// </summary>
        public Texture2D ApplyColorRangeFilter(Texture2D sourceTexture = null)
        {
            if (sourceTexture == null && processedTexture == null)
            {
                Debug.LogWarning("ColorRangeFilter: No source texture provided");
                return null;
            }
            
            Texture2D inputTexture = sourceTexture ?? RenderTextureToTexture2D(processedTexture);
            if (inputTexture == null)
            {
                Debug.LogWarning("ColorRangeFilter: Invalid input texture");
                return null;
            }
            
            // Create output texture
            Texture2D outputTexture = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGB24, false);
            Color[] inputPixels = inputTexture.GetPixels();
            Color[] outputPixels = new Color[inputPixels.Length];
            
            // Process each pixel
            for (int i = 0; i < inputPixels.Length; i++)
            {
                Color originalColor = inputPixels[i];
                bool pixelInAnyRange = false;
                
                // Convert to HSV for range checking
                Color.RGBToHSV(originalColor, out float h, out float s, out float v);
                Vector3 hsvColor = new Vector3(h, s, v);
                
                // Check against all active ranges
                foreach (var range in activeRanges)
                {
                    if (range.isActive && range.IsColorInRange(hsvColor))
                    {
                        pixelInAnyRange = true;
                        
                        if (range.showAsHighlight)
                        {
                            // Show as highlight color
                            outputPixels[i] = Color.Lerp(originalColor, range.highlightColor, range.highlightIntensity);
                        }
                        else if (range.showOriginalColors)
                        {
                            // Show original color
                            outputPixels[i] = originalColor;
                        }
                        else
                        {
                            // Show as white/mask
                            outputPixels[i] = Color.white;
                        }
                        break; // Use first matching range
                    }
                }
                
                // Check current range if no active ranges matched
                if (!pixelInAnyRange && currentRange != null && currentRange.IsColorInRange(hsvColor))
                {
                    pixelInAnyRange = true;
                    
                    if (currentRange.showAsHighlight)
                    {
                        outputPixels[i] = Color.Lerp(originalColor, currentRange.highlightColor, currentRange.highlightIntensity);
                    }
                    else if (currentRange.showOriginalColors)
                    {
                        outputPixels[i] = originalColor;
                    }
                    else
                    {
                        outputPixels[i] = Color.white;
                    }
                }
                
                // If pixel not in any range, make it transparent or dark
                if (!pixelInAnyRange)
                {
                    outputPixels[i] = Color.black; // Or Color.clear for transparency
                }
            }
            
            outputTexture.SetPixels(outputPixels);
            outputTexture.Apply();
            
            OnFilterApplied?.Invoke(outputTexture);
            
            Debug.Log($"ColorRangeFilter: Applied color range filter to {inputTexture.width}x{inputTexture.height} texture");
            return outputTexture;
        }
        
        /// <summary>
        /// Get color statistics for the current ranges
        /// </summary>
        public ColorRangeStatistics GetColorStatistics(Texture2D sourceTexture)
        {
            if (sourceTexture == null)
            {
                return new ColorRangeStatistics();
            }
            
            var stats = new ColorRangeStatistics();
            Color[] pixels = sourceTexture.GetPixels();
            stats.totalPixels = pixels.Length;
            
            foreach (var pixel in pixels)
            {
                Color.RGBToHSV(pixel, out float h, out float s, out float v);
                Vector3 hsvColor = new Vector3(h, s, v);
                
                bool inAnyRange = false;
                
                // Check against active ranges
                foreach (var range in activeRanges)
                {
                    if (range.isActive && range.IsColorInRange(hsvColor))
                    {
                        stats.pixelsInRange++;
                        inAnyRange = true;
                        break;
                    }
                }
                
                // Check current range
                if (!inAnyRange && currentRange != null && currentRange.IsColorInRange(hsvColor))
                {
                    stats.pixelsInRange++;
                }
            }
            
            stats.percentageInRange = (float)stats.pixelsInRange / stats.totalPixels * 100f;
            
            return stats;
        }
        
        /// <summary>
        /// Convert RenderTexture to Texture2D
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
        /// Statistics for color range filtering
        /// </summary>
        [System.Serializable]
        public class ColorRangeStatistics
        {
            public int totalPixels;
            public int pixelsInRange;
            public float percentageInRange;
            
            public override string ToString()
            {
                return $"Pixels in range: {pixelsInRange}/{totalPixels} ({percentageInRange:F1}%)";
            }
        }
        
        private void OnDestroy()
        {
            if (processedTexture != null)
            {
                processedTexture.Release();
            }
        }
    }
}