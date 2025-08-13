using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// Manager for combining multiple color range filters
    /// Implements Requirements: 4.1.4, 4.1.5
    /// </summary>
    public class MultipleColorRangeManager : MonoBehaviour
    {
        [Header("Multiple Range Configuration")]
        [SerializeField] private List<ColorRangeData> colorRanges = new List<ColorRangeData>();
        [SerializeField] private CombinationMode combinationMode = CombinationMode.Union;
        [SerializeField] private bool realTimePreview = true;
        [SerializeField] private int maxActiveRanges = 8;
        
        [Header("Performance Settings")]
        [SerializeField] private bool useOptimizedProcessing = true;
        [SerializeField] private bool enableRangeOverlapDetection = true;
        [SerializeField, Range(0f, 1f)] private float overlapTolerance = 0.1f;
        
        // Combination modes for multiple ranges
        public enum CombinationMode
        {
            Union,          // Show pixels that match ANY range
            Intersection,   // Show pixels that match ALL ranges
            Exclusive,      // Show pixels that match ONLY ONE range
            Weighted        // Combine ranges with weighted blending
        }
        
        // Data structure for individual color ranges
        [System.Serializable]
        public class ColorRangeData
        {
            [Header("Range Identification")]
            public string rangeName = "Color Range";
            public int rangeId;
            public bool isActive = true;
            public int priority = 0;
            
            [Header("HSV Parameters")]
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
            [Range(0f, 1f)] public float weight = 1f;
            
            [Header("Advanced Options")]
            public bool invertRange = false;
            [Range(0f, 1f)] public float featherAmount = 0f;
            
            public ColorRangeData()
            {
                rangeId = UnityEngine.Random.Range(1000, 9999);
            }
            
            public ColorRangeData(string name) : this()
            {
                rangeName = name;
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
                
                bool inRange = hueInRange && saturationInRange && valueInRange;
                
                // Apply inversion if enabled
                return invertRange ? !inRange : inRange;
            }
            
            /// <summary>
            /// Get the match strength for a color (0-1, with feathering support)
            /// </summary>
            public float GetMatchStrength(Vector3 hsvColor)
            {
                if (!IsColorInRange(hsvColor))
                    return 0f;
                
                if (featherAmount <= 0f)
                    return 1f;
                
                // Calculate distance from range center for feathering
                float centerH = (hueMin + hueMax) / 2f;
                if (hueMin > hueMax) // Handle wraparound
                {
                    centerH = (hueMin + hueMax + 360f) / 2f;
                    if (centerH > 360f) centerH -= 360f;
                }
                
                float centerS = (saturationMin + saturationMax) / 2f;
                float centerV = (valueMin + valueMax) / 2f;
                
                float h = hsvColor.x * 360f;
                float s = hsvColor.y;
                float v = hsvColor.z;
                
                // Calculate normalized distance from center
                float hDist = Mathf.Abs(h - centerH);
                if (hDist > 180f) hDist = 360f - hDist; // Handle wraparound
                hDist /= 180f; // Normalize to 0-1
                
                float sDist = Mathf.Abs(s - centerS);
                float vDist = Mathf.Abs(v - centerV);
                
                float totalDistance = Mathf.Sqrt(hDist * hDist + sDist * sDist + vDist * vDist);
                
                // Apply feathering
                float strength = 1f - (totalDistance * featherAmount);
                return Mathf.Clamp01(strength);
            }
            
            /// <summary>
            /// Check if this range overlaps with another range
            /// </summary>
            public bool OverlapsWith(ColorRangeData other, float tolerance = 0.1f)
            {
                if (other == null) return false;
                
                // Check HSV overlap with tolerance
                bool hueOverlap = RangesOverlap(hueMin, hueMax, other.hueMin, other.hueMax, tolerance * 360f, true);
                bool satOverlap = RangesOverlap(saturationMin, saturationMax, other.saturationMin, other.saturationMax, tolerance, false);
                bool valueOverlap = RangesOverlap(valueMin, valueMax, other.valueMin, other.valueMax, tolerance, false);
                
                return hueOverlap && satOverlap && valueOverlap;
            }
            
            private bool RangesOverlap(float min1, float max1, float min2, float max2, float tolerance, bool isHue)
            {
                if (isHue && (min1 > max1 || min2 > max2))
                {
                    // Handle hue wraparound
                    if (min1 > max1)
                    {
                        return (min1 - tolerance <= max2 + tolerance) || (max1 + tolerance >= min2 - tolerance);
                    }
                    if (min2 > max2)
                    {
                        return (min2 - tolerance <= max1 + tolerance) || (max2 + tolerance >= min1 - tolerance);
                    }
                }
                
                return (min1 - tolerance <= max2 + tolerance) && (max1 + tolerance >= min2 - tolerance);
            }
        }
        
        // Statistics for multiple range processing
        [System.Serializable]
        public class MultipleRangeStatistics
        {
            public int totalRanges;
            public int activeRanges;
            public int overlappingRanges;
            public float processingTime;
            public int pixelsMatched;
            public int totalPixels;
            public float coveragePercentage;
            public CombinationMode mode;
            
            public override string ToString()
            {
                return $"Ranges: {activeRanges}/{totalRanges}, Coverage: {coveragePercentage:F1}%, Time: {processingTime * 1000f:F1}ms";
            }
        }
        
        // Events
        public event Action<ColorRangeData> OnRangeAdded;
        public event Action<int> OnRangeRemoved;
        public event Action<Texture2D> OnMultipleRangesApplied;
        public event Action<MultipleRangeStatistics> OnStatisticsUpdated;
        
        // Properties
        public List<ColorRangeData> ColorRanges => colorRanges.ToList();
        public CombinationMode CurrentCombinationMode { get => combinationMode; set => combinationMode = value; }
        public bool RealTimePreview { get => realTimePreview; set => realTimePreview = value; }
        public int MaxActiveRanges { get => maxActiveRanges; set => maxActiveRanges = Mathf.Clamp(value, 1, 16); }
        
        private MultipleRangeStatistics lastStatistics;
        
        private void Awake()
        {
            InitializeMultipleRangeManager();
        }
        
        /// <summary>
        /// Initialize the multiple color range manager
        /// </summary>
        private void InitializeMultipleRangeManager()
        {
            Debug.Log("MultipleColorRangeManager: Initialized multiple color range support system");
        }
        
        /// <summary>
        /// Add a new color range to the collection
        /// Implements multiple color range support (Requirement 4.1.4)
        /// </summary>
        public void AddColorRange(ColorRangeData range)
        {
            if (range == null)
            {
                Debug.LogWarning("MultipleColorRangeManager: Cannot add null color range");
                return;
            }
            
            // Check if we've reached the maximum number of ranges
            if (colorRanges.Count >= maxActiveRanges)
            {
                Debug.LogWarning($"MultipleColorRangeManager: Maximum number of ranges ({maxActiveRanges}) reached");
                return;
            }
            
            // Ensure unique range ID
            while (colorRanges.Any(r => r.rangeId == range.rangeId))
            {
                range.rangeId = UnityEngine.Random.Range(1000, 9999);
            }
            
            // Check for overlaps if detection is enabled
            if (enableRangeOverlapDetection)
            {
                var overlappingRanges = colorRanges.Where(r => r.OverlapsWith(range, overlapTolerance)).ToList();
                if (overlappingRanges.Count > 0)
                {
                    Debug.LogWarning($"MultipleColorRangeManager: Range '{range.rangeName}' overlaps with {overlappingRanges.Count} existing ranges");
                }
            }
            
            colorRanges.Add(range);
            OnRangeAdded?.Invoke(range);
            
            if (realTimePreview)
            {
                // Trigger reprocessing if we have a current texture
                // This would be handled by the FilterManager in practice
            }
            
            Debug.Log($"MultipleColorRangeManager: Added color range '{range.rangeName}' (ID: {range.rangeId}, Total: {colorRanges.Count})");
        }
        
        /// <summary>
        /// Remove a color range by ID
        /// </summary>
        public bool RemoveColorRange(int rangeId)
        {
            var rangeToRemove = colorRanges.FirstOrDefault(r => r.rangeId == rangeId);
            if (rangeToRemove != null)
            {
                colorRanges.Remove(rangeToRemove);
                OnRangeRemoved?.Invoke(rangeId);
                
                if (realTimePreview)
                {
                    // Trigger reprocessing
                }
                
                Debug.Log($"MultipleColorRangeManager: Removed color range ID {rangeId} (Remaining: {colorRanges.Count})");
                return true;
            }
            
            Debug.LogWarning($"MultipleColorRangeManager: Color range ID {rangeId} not found");
            return false;
        }
        
        /// <summary>
        /// Clear all color ranges
        /// </summary>
        public void ClearAllRanges()
        {
            int removedCount = colorRanges.Count;
            colorRanges.Clear();
            
            if (realTimePreview)
            {
                // Trigger reprocessing
            }
            
            Debug.Log($"MultipleColorRangeManager: Cleared {removedCount} color ranges");
        }
        
        /// <summary>
        /// Toggle a color range active state
        /// </summary>
        public void ToggleColorRange(int rangeId, bool isActive)
        {
            var range = colorRanges.FirstOrDefault(r => r.rangeId == rangeId);
            if (range != null)
            {
                range.isActive = isActive;
                
                if (realTimePreview)
                {
                    // Trigger reprocessing
                }
                
                Debug.Log($"MultipleColorRangeManager: Toggled range {rangeId} {(isActive ? "ON" : "OFF")}");
            }
        }
        
        /// <summary>
        /// Apply multiple color ranges to a texture
        /// Implements combining multiple color range filters (Requirement 4.1.5)
        /// </summary>
        public Texture2D ApplyMultipleColorRanges(Texture2D sourceTexture)
        {
            if (sourceTexture == null)
            {
                Debug.LogWarning("MultipleColorRangeManager: No source texture provided");
                return null;
            }
            
            float startTime = Time.realtimeSinceStartup;
            
            // Get active ranges
            var activeRanges = colorRanges.Where(r => r.isActive).ToList();
            if (activeRanges.Count == 0)
            {
                Debug.LogWarning("MultipleColorRangeManager: No active color ranges");
                return sourceTexture;
            }
            
            // Sort ranges by priority
            activeRanges = activeRanges.OrderByDescending(r => r.priority).ToList();
            
            // Create output texture
            Texture2D outputTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGB24, false);
            Color[] inputPixels = sourceTexture.GetPixels();
            Color[] outputPixels = new Color[inputPixels.Length];
            
            int pixelsMatched = 0;
            
            // Process each pixel
            for (int i = 0; i < inputPixels.Length; i++)
            {
                Color originalColor = inputPixels[i];
                Color resultColor = ProcessPixelWithMultipleRanges(originalColor, activeRanges, out bool matched);
                
                outputPixels[i] = resultColor;
                if (matched) pixelsMatched++;
            }
            
            outputTexture.SetPixels(outputPixels);
            outputTexture.Apply();
            
            // Update statistics
            float processingTime = Time.realtimeSinceStartup - startTime;
            UpdateStatistics(activeRanges, pixelsMatched, inputPixels.Length, processingTime);
            
            // Fire events
            OnMultipleRangesApplied?.Invoke(outputTexture);
            OnStatisticsUpdated?.Invoke(lastStatistics);
            
            Debug.Log($"MultipleColorRangeManager: Applied {activeRanges.Count} color ranges using {combinationMode} mode - {lastStatistics}");
            
            return outputTexture;
        }
        
        /// <summary>
        /// Process a single pixel with multiple color ranges based on combination mode
        /// </summary>
        private Color ProcessPixelWithMultipleRanges(Color originalColor, List<ColorRangeData> activeRanges, out bool matched)
        {
            matched = false;
            
            // Convert to HSV for range checking
            Color.RGBToHSV(originalColor, out float h, out float s, out float v);
            Vector3 hsvColor = new Vector3(h, s, v);
            
            switch (combinationMode)
            {
                case CombinationMode.Union:
                    return ProcessUnionMode(originalColor, hsvColor, activeRanges, out matched);
                
                case CombinationMode.Intersection:
                    return ProcessIntersectionMode(originalColor, hsvColor, activeRanges, out matched);
                
                case CombinationMode.Exclusive:
                    return ProcessExclusiveMode(originalColor, hsvColor, activeRanges, out matched);
                
                case CombinationMode.Weighted:
                    return ProcessWeightedMode(originalColor, hsvColor, activeRanges, out matched);
                
                default:
                    return Color.black;
            }
        }
        
        /// <summary>
        /// Process pixel in Union mode (show if matches ANY range)
        /// </summary>
        private Color ProcessUnionMode(Color originalColor, Vector3 hsvColor, List<ColorRangeData> activeRanges, out bool matched)
        {
            matched = false;
            
            // Check each range in priority order
            foreach (var range in activeRanges)
            {
                if (range.IsColorInRange(hsvColor))
                {
                    matched = true;
                    
                    if (range.showAsHighlight)
                    {
                        return Color.Lerp(originalColor, range.highlightColor, range.highlightIntensity);
                    }
                    else if (range.showOriginalColors)
                    {
                        return originalColor;
                    }
                    else
                    {
                        return Color.white;
                    }
                }
            }
            
            return Color.black; // No ranges matched
        }
        
        /// <summary>
        /// Process pixel in Intersection mode (show if matches ALL ranges)
        /// </summary>
        private Color ProcessIntersectionMode(Color originalColor, Vector3 hsvColor, List<ColorRangeData> activeRanges, out bool matched)
        {
            matched = true;
            
            // Check if pixel matches ALL ranges
            foreach (var range in activeRanges)
            {
                if (!range.IsColorInRange(hsvColor))
                {
                    matched = false;
                    return Color.black;
                }
            }
            
            // If we get here, pixel matches all ranges
            // Use the highest priority range for display
            var primaryRange = activeRanges.First();
            
            if (primaryRange.showAsHighlight)
            {
                return Color.Lerp(originalColor, primaryRange.highlightColor, primaryRange.highlightIntensity);
            }
            else if (primaryRange.showOriginalColors)
            {
                return originalColor;
            }
            else
            {
                return Color.white;
            }
        }
        
        /// <summary>
        /// Process pixel in Exclusive mode (show if matches ONLY ONE range)
        /// </summary>
        private Color ProcessExclusiveMode(Color originalColor, Vector3 hsvColor, List<ColorRangeData> activeRanges, out bool matched)
        {
            matched = false;
            ColorRangeData matchingRange = null;
            int matchCount = 0;
            
            // Count how many ranges match
            foreach (var range in activeRanges)
            {
                if (range.IsColorInRange(hsvColor))
                {
                    matchCount++;
                    matchingRange = range;
                }
            }
            
            // Only show if exactly one range matches
            if (matchCount == 1)
            {
                matched = true;
                
                if (matchingRange.showAsHighlight)
                {
                    return Color.Lerp(originalColor, matchingRange.highlightColor, matchingRange.highlightIntensity);
                }
                else if (matchingRange.showOriginalColors)
                {
                    return originalColor;
                }
                else
                {
                    return Color.white;
                }
            }
            
            return Color.black; // Zero or multiple ranges matched
        }
        
        /// <summary>
        /// Process pixel in Weighted mode (blend multiple range effects)
        /// </summary>
        private Color ProcessWeightedMode(Color originalColor, Vector3 hsvColor, List<ColorRangeData> activeRanges, out bool matched)
        {
            matched = false;
            Color resultColor = Color.black;
            float totalWeight = 0f;
            
            // Accumulate weighted contributions from all matching ranges
            foreach (var range in activeRanges)
            {
                float matchStrength = range.GetMatchStrength(hsvColor);
                if (matchStrength > 0f)
                {
                    matched = true;
                    float effectiveWeight = range.weight * matchStrength;
                    
                    Color rangeColor;
                    if (range.showAsHighlight)
                    {
                        rangeColor = Color.Lerp(originalColor, range.highlightColor, range.highlightIntensity);
                    }
                    else if (range.showOriginalColors)
                    {
                        rangeColor = originalColor;
                    }
                    else
                    {
                        rangeColor = Color.white;
                    }
                    
                    resultColor += rangeColor * effectiveWeight;
                    totalWeight += effectiveWeight;
                }
            }
            
            // Normalize by total weight
            if (totalWeight > 0f)
            {
                resultColor /= totalWeight;
                resultColor.a = 1f; // Ensure alpha is 1
                return resultColor;
            }
            
            return Color.black; // No ranges matched
        }
        
        /// <summary>
        /// Get overlapping ranges for a specific range
        /// </summary>
        public List<ColorRangeData> GetOverlappingRanges(int rangeId)
        {
            var targetRange = colorRanges.FirstOrDefault(r => r.rangeId == rangeId);
            if (targetRange == null)
                return new List<ColorRangeData>();
            
            return colorRanges.Where(r => r.rangeId != rangeId && r.OverlapsWith(targetRange, overlapTolerance)).ToList();
        }
        
        /// <summary>
        /// Optimize ranges by merging overlapping ones
        /// </summary>
        public void OptimizeRanges()
        {
            if (!enableRangeOverlapDetection)
                return;
            
            var optimizedRanges = new List<ColorRangeData>();
            var processedRanges = new HashSet<int>();
            
            foreach (var range in colorRanges)
            {
                if (processedRanges.Contains(range.rangeId))
                    continue;
                
                var overlappingRanges = GetOverlappingRanges(range.rangeId);
                if (overlappingRanges.Count > 0)
                {
                    // Merge overlapping ranges
                    var mergedRange = MergeRanges(new List<ColorRangeData> { range }.Concat(overlappingRanges).ToList());
                    optimizedRanges.Add(mergedRange);
                    
                    // Mark all merged ranges as processed
                    processedRanges.Add(range.rangeId);
                    foreach (var overlapping in overlappingRanges)
                    {
                        processedRanges.Add(overlapping.rangeId);
                    }
                }
                else
                {
                    optimizedRanges.Add(range);
                    processedRanges.Add(range.rangeId);
                }
            }
            
            int originalCount = colorRanges.Count;
            colorRanges = optimizedRanges;
            
            Debug.Log($"MultipleColorRangeManager: Optimized ranges from {originalCount} to {colorRanges.Count}");
        }
        
        /// <summary>
        /// Merge multiple ranges into a single range
        /// </summary>
        private ColorRangeData MergeRanges(List<ColorRangeData> ranges)
        {
            if (ranges.Count == 0)
                return null;
            
            if (ranges.Count == 1)
                return ranges[0];
            
            var mergedRange = new ColorRangeData($"Merged Range ({ranges.Count} ranges)");
            
            // Calculate merged HSV bounds
            float minHue = ranges.Min(r => r.hueMin);
            float maxHue = ranges.Max(r => r.hueMax);
            float minSat = ranges.Min(r => r.saturationMin);
            float maxSat = ranges.Max(r => r.saturationMax);
            float minVal = ranges.Min(r => r.valueMin);
            float maxVal = ranges.Max(r => r.valueMax);
            
            mergedRange.hueMin = minHue;
            mergedRange.hueMax = maxHue;
            mergedRange.saturationMin = minSat;
            mergedRange.saturationMax = maxSat;
            mergedRange.valueMin = minVal;
            mergedRange.valueMax = maxVal;
            
            // Use properties from the highest priority range
            var primaryRange = ranges.OrderByDescending(r => r.priority).First();
            mergedRange.showOriginalColors = primaryRange.showOriginalColors;
            mergedRange.showAsHighlight = primaryRange.showAsHighlight;
            mergedRange.highlightColor = primaryRange.highlightColor;
            mergedRange.highlightIntensity = primaryRange.highlightIntensity;
            mergedRange.priority = primaryRange.priority;
            
            // Average the weights
            mergedRange.weight = ranges.Average(r => r.weight);
            
            return mergedRange;
        }
        
        /// <summary>
        /// Update statistics for multiple range processing
        /// </summary>
        private void UpdateStatistics(List<ColorRangeData> activeRanges, int pixelsMatched, int totalPixels, float processingTime)
        {
            int overlappingCount = 0;
            
            if (enableRangeOverlapDetection)
            {
                for (int i = 0; i < activeRanges.Count; i++)
                {
                    for (int j = i + 1; j < activeRanges.Count; j++)
                    {
                        if (activeRanges[i].OverlapsWith(activeRanges[j], overlapTolerance))
                        {
                            overlappingCount++;
                        }
                    }
                }
            }
            
            lastStatistics = new MultipleRangeStatistics
            {
                totalRanges = colorRanges.Count,
                activeRanges = activeRanges.Count,
                overlappingRanges = overlappingCount,
                processingTime = processingTime,
                pixelsMatched = pixelsMatched,
                totalPixels = totalPixels,
                coveragePercentage = (float)pixelsMatched / totalPixels * 100f,
                mode = combinationMode
            };
        }
        
        /// <summary>
        /// Get current statistics
        /// </summary>
        public MultipleRangeStatistics GetStatistics()
        {
            return lastStatistics;
        }
        
        /// <summary>
        /// Get active color ranges
        /// </summary>
        public List<ColorRangeData> GetActiveRanges()
        {
            return colorRanges.Where(r => r.isActive).ToList();
        }
        
        /// <summary>
        /// Set combination mode and optionally reprocess
        /// </summary>
        public void SetCombinationMode(CombinationMode mode, bool reprocessImmediately = false)
        {
            combinationMode = mode;
            
            if (reprocessImmediately && realTimePreview)
            {
                // Trigger reprocessing
            }
            
            Debug.Log($"MultipleColorRangeManager: Set combination mode to {mode}");
        }
    }
}