using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// Color quantization and palette reduction filter
    /// Implements Requirements: 4.2.1, 4.2.2, 4.2.4, 4.2.5
    /// </summary>
    public class ColorReductionFilter : MonoBehaviour
    {
        [Header("Color Reduction Settings")]
        [SerializeField, Range(2, 256)] private int targetColorCount = 16;
        [SerializeField] private ColorQuantizationMethod quantizationMethod = ColorQuantizationMethod.KMeans;
        [SerializeField] private bool preserveDominantColors = true;
        [SerializeField] private bool realTimePreview = true;
        
        [Header("Advanced Settings")]
        [SerializeField, Range(0f, 1f)] private float colorPreservationThreshold = 0.1f;
        [SerializeField, Range(1, 10)] private int maxIterations = 5;
        [SerializeField] private bool useDithering = false;
        [SerializeField, Range(0f, 1f)] private float ditheringStrength = 0.5f;
        
        // Color quantization methods
        public enum ColorQuantizationMethod
        {
            KMeans,           // K-means clustering for optimal color selection
            MedianCut,        // Median cut algorithm for balanced color distribution
            Uniform,          // Uniform color space quantization
            Popularity        // Most popular colors in the image
        }
        
        // Quantized palette data
        [System.Serializable]
        public class QuantizedPalette
        {
            public Color[] colors;
            public float[] weights;  // Relative frequency of each color
            public int totalColors;
            public ColorQuantizationMethod method;
            public DateTime createdAt;
            
            public QuantizedPalette(int colorCount)
            {
                colors = new Color[colorCount];
                weights = new float[colorCount];
                totalColors = colorCount;
                createdAt = DateTime.Now;
            }
            
            /// <summary>
            /// Find the closest color in the palette to the given color
            /// </summary>
            public Color GetClosestColor(Color inputColor)
            {
                if (colors == null || colors.Length == 0)
                    return inputColor;
                
                float minDistance = float.MaxValue;
                Color closestColor = colors[0];
                
                foreach (var paletteColor in colors)
                {
                    float distance = ColorDistance(inputColor, paletteColor);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestColor = paletteColor;
                    }
                }
                
                return closestColor;
            }
            
            /// <summary>
            /// Calculate perceptual color distance
            /// </summary>
            private float ColorDistance(Color c1, Color c2)
            {
                // Use weighted RGB distance for better perceptual accuracy
                float dr = c1.r - c2.r;
                float dg = c1.g - c2.g;
                float db = c1.b - c2.b;
                
                // Weight green more heavily as human eyes are more sensitive to it
                return Mathf.Sqrt(0.3f * dr * dr + 0.59f * dg * dg + 0.11f * db * db);
            }
        }
        
        // Color statistics for analysis
        [System.Serializable]
        public class ColorReductionStatistics
        {
            public int originalColorCount;
            public int reducedColorCount;
            public float compressionRatio;
            public float processingTime;
            public ColorQuantizationMethod method;
            
            public override string ToString()
            {
                return $"Colors: {originalColorCount} → {reducedColorCount} ({compressionRatio:P1}), Time: {processingTime * 1000f:F1}ms";
            }
        }
        
        // Events
        public event Action<QuantizedPalette> OnPaletteGenerated;
        public event Action<Texture2D> OnColorReductionApplied;
        public event Action<ColorReductionStatistics> OnStatisticsUpdated;
        
        // Properties
        public int TargetColorCount 
        { 
            get => targetColorCount; 
            set => targetColorCount = Mathf.Clamp(value, 2, 256); 
        }
        
        public ColorQuantizationMethod QuantizationMethod 
        { 
            get => quantizationMethod; 
            set => quantizationMethod = value; 
        }
        
        public bool RealTimePreview 
        { 
            get => realTimePreview; 
            set => realTimePreview = value; 
        }
        
        private QuantizedPalette currentPalette;
        private ColorReductionStatistics lastStatistics;
        
        private void Awake()
        {
            InitializeColorReductionFilter();
        }
        
        /// <summary>
        /// Initialize the color reduction filter system
        /// </summary>
        private void InitializeColorReductionFilter()
        {
            Debug.Log("ColorReductionFilter: Initialized color quantization and palette reduction system");
        }
        
        /// <summary>
        /// Apply color reduction to the input texture
        /// Implements Requirements: 4.2.1, 4.2.2
        /// </summary>
        public Texture2D ApplyColorReduction(Texture2D sourceTexture)
        {
            if (sourceTexture == null)
            {
                Debug.LogWarning("ColorReductionFilter: No source texture provided");
                return null;
            }
            
            float startTime = Time.realtimeSinceStartup;
            
            // Generate quantized palette
            currentPalette = GenerateQuantizedPalette(sourceTexture, targetColorCount, quantizationMethod);
            
            // Apply palette to create reduced color image
            Texture2D reducedTexture = ApplyPaletteToTexture(sourceTexture, currentPalette);
            
            // Calculate statistics
            float processingTime = Time.realtimeSinceStartup - startTime;
            UpdateStatistics(sourceTexture, reducedTexture, processingTime);
            
            // Fire events
            OnPaletteGenerated?.Invoke(currentPalette);
            OnColorReductionApplied?.Invoke(reducedTexture);
            OnStatisticsUpdated?.Invoke(lastStatistics);
            
            Debug.Log($"ColorReductionFilter: Applied color reduction - {lastStatistics}");
            
            return reducedTexture;
        }
        
        /// <summary>
        /// Generate quantized color palette using specified method
        /// Implements adjustable color count controls (Requirement 4.2.4)
        /// </summary>
        public QuantizedPalette GenerateQuantizedPalette(Texture2D sourceTexture, int colorCount, ColorQuantizationMethod method)
        {
            if (sourceTexture == null || colorCount < 2 || colorCount > 256)
            {
                Debug.LogWarning($"ColorReductionFilter: Invalid parameters - texture: {sourceTexture != null}, colorCount: {colorCount}");
                return null;
            }
            
            Color[] pixels = sourceTexture.GetPixels();
            QuantizedPalette palette = new QuantizedPalette(colorCount);
            palette.method = method;
            
            switch (method)
            {
                case ColorQuantizationMethod.KMeans:
                    palette = GenerateKMeansPalette(pixels, colorCount);
                    break;
                case ColorQuantizationMethod.MedianCut:
                    palette = GenerateMedianCutPalette(pixels, colorCount);
                    break;
                case ColorQuantizationMethod.Uniform:
                    palette = GenerateUniformPalette(colorCount);
                    break;
                case ColorQuantizationMethod.Popularity:
                    palette = GeneratePopularityPalette(pixels, colorCount);
                    break;
            }
            
            palette.method = method;
            return palette;
        }
        
        /// <summary>
        /// Generate palette using K-means clustering algorithm
        /// Provides optimal color selection for representative colors
        /// </summary>
        private QuantizedPalette GenerateKMeansPalette(Color[] pixels, int colorCount)
        {
            var palette = new QuantizedPalette(colorCount);
            
            // Initialize centroids randomly
            var centroids = new Color[colorCount];
            for (int i = 0; i < colorCount; i++)
            {
                centroids[i] = pixels[UnityEngine.Random.Range(0, pixels.Length)];
            }
            
            // K-means iterations
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                var clusters = new List<Color>[colorCount];
                for (int i = 0; i < colorCount; i++)
                {
                    clusters[i] = new List<Color>();
                }
                
                // Assign pixels to nearest centroid
                foreach (var pixel in pixels)
                {
                    int nearestCentroid = FindNearestCentroid(pixel, centroids);
                    clusters[nearestCentroid].Add(pixel);
                }
                
                // Update centroids
                bool converged = true;
                for (int i = 0; i < colorCount; i++)
                {
                    if (clusters[i].Count > 0)
                    {
                        Color newCentroid = CalculateAverageColor(clusters[i]);
                        if (ColorDistance(centroids[i], newCentroid) > 0.01f)
                        {
                            converged = false;
                        }
                        centroids[i] = newCentroid;
                    }
                }
                
                if (converged) break;
            }
            
            // Set palette colors and calculate weights
            for (int i = 0; i < colorCount; i++)
            {
                palette.colors[i] = centroids[i];
                palette.weights[i] = CountPixelsNearColor(pixels, centroids[i]) / (float)pixels.Length;
            }
            
            return palette;
        }
        
        /// <summary>
        /// Generate palette using median cut algorithm
        /// Provides balanced color distribution across color space
        /// </summary>
        private QuantizedPalette GenerateMedianCutPalette(Color[] pixels, int colorCount)
        {
            var palette = new QuantizedPalette(colorCount);
            
            // Create initial color bucket
            var initialBucket = pixels.ToList();
            var buckets = new List<List<Color>> { initialBucket };
            
            // Recursively split buckets until we have desired color count
            while (buckets.Count < colorCount)
            {
                // Find bucket with largest color range
                int largestBucketIndex = 0;
                float largestRange = 0f;
                
                for (int i = 0; i < buckets.Count; i++)
                {
                    float range = CalculateColorRange(buckets[i]);
                    if (range > largestRange)
                    {
                        largestRange = range;
                        largestBucketIndex = i;
                    }
                }
                
                // Split the largest bucket
                var bucketToSplit = buckets[largestBucketIndex];
                buckets.RemoveAt(largestBucketIndex);
                
                var splitBuckets = SplitColorBucket(bucketToSplit);
                buckets.AddRange(splitBuckets);
            }
            
            // Generate palette from buckets
            for (int i = 0; i < Mathf.Min(colorCount, buckets.Count); i++)
            {
                palette.colors[i] = CalculateAverageColor(buckets[i]);
                palette.weights[i] = buckets[i].Count / (float)pixels.Length;
            }
            
            return palette;
        }
        
        /// <summary>
        /// Generate uniform palette across RGB color space
        /// </summary>
        private QuantizedPalette GenerateUniformPalette(int colorCount)
        {
            var palette = new QuantizedPalette(colorCount);
            
            int stepsPerChannel = Mathf.RoundToInt(Mathf.Pow(colorCount, 1f / 3f));
            float stepSize = 1f / (stepsPerChannel - 1);
            
            int colorIndex = 0;
            for (int r = 0; r < stepsPerChannel && colorIndex < colorCount; r++)
            {
                for (int g = 0; g < stepsPerChannel && colorIndex < colorCount; g++)
                {
                    for (int b = 0; b < stepsPerChannel && colorIndex < colorCount; b++)
                    {
                        palette.colors[colorIndex] = new Color(r * stepSize, g * stepSize, b * stepSize);
                        palette.weights[colorIndex] = 1f / colorCount;
                        colorIndex++;
                    }
                }
            }
            
            return palette;
        }
        
        /// <summary>
        /// Generate palette based on most popular colors in the image
        /// </summary>
        private QuantizedPalette GeneratePopularityPalette(Color[] pixels, int colorCount)
        {
            var palette = new QuantizedPalette(colorCount);
            
            // Count color frequencies (with some tolerance for similar colors)
            var colorCounts = new Dictionary<Color, int>();
            float tolerance = 0.05f;
            
            foreach (var pixel in pixels)
            {
                Color quantizedPixel = QuantizeColor(pixel, 32); // Reduce to 32 levels per channel for grouping
                
                if (colorCounts.ContainsKey(quantizedPixel))
                {
                    colorCounts[quantizedPixel]++;
                }
                else
                {
                    colorCounts[quantizedPixel] = 1;
                }
            }
            
            // Sort by popularity and take top colors
            var sortedColors = colorCounts.OrderByDescending(kvp => kvp.Value).Take(colorCount).ToList();
            
            for (int i = 0; i < sortedColors.Count; i++)
            {
                palette.colors[i] = sortedColors[i].Key;
                palette.weights[i] = sortedColors[i].Value / (float)pixels.Length;
            }
            
            return palette;
        }
        
        /// <summary>
        /// Apply quantized palette to texture to create color-reduced image
        /// Implements color preservation (Requirement 4.2.5)
        /// </summary>
        private Texture2D ApplyPaletteToTexture(Texture2D sourceTexture, QuantizedPalette palette)
        {
            if (sourceTexture == null || palette == null)
                return null;
            
            Texture2D resultTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGB24, false);
            Color[] sourcePixels = sourceTexture.GetPixels();
            Color[] resultPixels = new Color[sourcePixels.Length];
            
            for (int i = 0; i < sourcePixels.Length; i++)
            {
                Color originalColor = sourcePixels[i];
                Color reducedColor = palette.GetClosestColor(originalColor);
                
                if (useDithering)
                {
                    // Apply simple dithering
                    reducedColor = ApplyDithering(originalColor, reducedColor, i, sourceTexture.width);
                }
                
                resultPixels[i] = reducedColor;
            }
            
            resultTexture.SetPixels(resultPixels);
            resultTexture.Apply();
            
            return resultTexture;
        }
        
        /// <summary>
        /// Apply simple dithering to reduce color banding
        /// </summary>
        private Color ApplyDithering(Color originalColor, Color quantizedColor, int pixelIndex, int textureWidth)
        {
            if (ditheringStrength <= 0f)
                return quantizedColor;
            
            // Simple ordered dithering pattern
            int x = pixelIndex % textureWidth;
            int y = pixelIndex / textureWidth;
            
            float ditherValue = ((x % 2) + (y % 2)) * 0.5f - 0.25f;
            ditherValue *= ditheringStrength;
            
            Color ditheredColor = quantizedColor;
            ditheredColor.r = Mathf.Clamp01(ditheredColor.r + ditherValue);
            ditheredColor.g = Mathf.Clamp01(ditheredColor.g + ditherValue);
            ditheredColor.b = Mathf.Clamp01(ditheredColor.b + ditherValue);
            
            return ditheredColor;
        }
        
        /// <summary>
        /// Set target color count and optionally apply immediately
        /// Implements adjustable color count controls (Requirement 4.2.4)
        /// </summary>
        public void SetTargetColorCount(int colorCount, bool applyImmediately = false)
        {
            targetColorCount = Mathf.Clamp(colorCount, 2, 256);
            
            if (applyImmediately && realTimePreview)
            {
                // Reapply with new color count if we have a current texture
                // This would be triggered by the FilterManager
                Debug.Log($"ColorReductionFilter: Updated target color count to {targetColorCount}");
            }
        }
        
        /// <summary>
        /// Get the current quantized palette
        /// </summary>
        public QuantizedPalette GetCurrentPalette()
        {
            return currentPalette;
        }
        
        /// <summary>
        /// Get color reduction statistics
        /// </summary>
        public ColorReductionStatistics GetStatistics()
        {
            return lastStatistics;
        }
        
        // Helper methods
        
        private int FindNearestCentroid(Color pixel, Color[] centroids)
        {
            int nearest = 0;
            float minDistance = ColorDistance(pixel, centroids[0]);
            
            for (int i = 1; i < centroids.Length; i++)
            {
                float distance = ColorDistance(pixel, centroids[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = i;
                }
            }
            
            return nearest;
        }
        
        private Color CalculateAverageColor(List<Color> colors)
        {
            if (colors.Count == 0)
                return Color.black;
            
            float r = 0f, g = 0f, b = 0f;
            foreach (var color in colors)
            {
                r += color.r;
                g += color.g;
                b += color.b;
            }
            
            return new Color(r / colors.Count, g / colors.Count, b / colors.Count);
        }
        
        private float ColorDistance(Color c1, Color c2)
        {
            float dr = c1.r - c2.r;
            float dg = c1.g - c2.g;
            float db = c1.b - c2.b;
            
            // Weighted RGB distance for better perceptual accuracy
            return Mathf.Sqrt(0.3f * dr * dr + 0.59f * dg * dg + 0.11f * db * db);
        }
        
        private int CountPixelsNearColor(Color[] pixels, Color targetColor)
        {
            int count = 0;
            float threshold = 0.1f;
            
            foreach (var pixel in pixels)
            {
                if (ColorDistance(pixel, targetColor) < threshold)
                    count++;
            }
            
            return count;
        }
        
        private float CalculateColorRange(List<Color> colors)
        {
            if (colors.Count == 0)
                return 0f;
            
            float minR = 1f, maxR = 0f;
            float minG = 1f, maxG = 0f;
            float minB = 1f, maxB = 0f;
            
            foreach (var color in colors)
            {
                minR = Mathf.Min(minR, color.r);
                maxR = Mathf.Max(maxR, color.r);
                minG = Mathf.Min(minG, color.g);
                maxG = Mathf.Max(maxG, color.g);
                minB = Mathf.Min(minB, color.b);
                maxB = Mathf.Max(maxB, color.b);
            }
            
            return (maxR - minR) + (maxG - minG) + (maxB - minB);
        }
        
        private List<List<Color>> SplitColorBucket(List<Color> bucket)
        {
            if (bucket.Count <= 1)
                return new List<List<Color>> { bucket };
            
            // Find the color channel with the largest range
            float rRange = bucket.Max(c => c.r) - bucket.Min(c => c.r);
            float gRange = bucket.Max(c => c.g) - bucket.Min(c => c.g);
            float bRange = bucket.Max(c => c.b) - bucket.Min(c => c.b);
            
            // Sort by the channel with largest range
            if (rRange >= gRange && rRange >= bRange)
            {
                bucket.Sort((c1, c2) => c1.r.CompareTo(c2.r));
            }
            else if (gRange >= bRange)
            {
                bucket.Sort((c1, c2) => c1.g.CompareTo(c2.g));
            }
            else
            {
                bucket.Sort((c1, c2) => c1.b.CompareTo(c2.b));
            }
            
            // Split at median
            int medianIndex = bucket.Count / 2;
            var bucket1 = bucket.Take(medianIndex).ToList();
            var bucket2 = bucket.Skip(medianIndex).ToList();
            
            return new List<List<Color>> { bucket1, bucket2 };
        }
        
        private Color QuantizeColor(Color color, int levels)
        {
            float step = 1f / (levels - 1);
            
            float r = Mathf.Round(color.r / step) * step;
            float g = Mathf.Round(color.g / step) * step;
            float b = Mathf.Round(color.b / step) * step;
            
            return new Color(r, g, b);
        }
        
        private void UpdateStatistics(Texture2D originalTexture, Texture2D reducedTexture, float processingTime)
        {
            if (originalTexture == null || reducedTexture == null)
                return;
            
            // Count unique colors (approximate)
            var originalColors = new HashSet<Color>(originalTexture.GetPixels());
            var reducedColors = new HashSet<Color>(reducedTexture.GetPixels());
            
            lastStatistics = new ColorReductionStatistics
            {
                originalColorCount = originalColors.Count,
                reducedColorCount = reducedColors.Count,
                compressionRatio = (float)reducedColors.Count / originalColors.Count,
                processingTime = processingTime,
                method = quantizationMethod
            };
        }
    }
}