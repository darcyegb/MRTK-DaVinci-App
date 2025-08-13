using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaVinciEye.ColorAnalysis
{
    /// <summary>
    /// Color matching system with visual difference calculation
    /// Provides accurate color comparison and matching guidance for artists
    /// </summary>
    public class ColorMatcher : MonoBehaviour
    {
        [Header("Matching Configuration")]
        [SerializeField] private ColorMatchingMethod matchingMethod = ColorMatchingMethod.DeltaE;
        [SerializeField] private float excellentThreshold = 2.0f;
        [SerializeField] private float goodThreshold = 5.0f;
        [SerializeField] private float fairThreshold = 10.0f;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool enableVisualFeedback = true;
        [SerializeField] private GameObject comparisonUIPrefab;
        
        // Properties
        public ColorMatchResult LastMatchResult { get; private set; }
        public List<ColorMatchData> MatchHistory { get; private set; } = new List<ColorMatchData>();
        
        // Events
        public event Action<ColorMatchResult> OnColorMatched;
        public event Action<ColorMatchData> OnMatchSaved;
        public event Action<string> OnMatchingError;
        
        // Private fields
        private Dictionary<ColorSpace, Func<Color, Color, float>> colorDistanceFunctions;
        
        private void Awake()
        {
            InitializeColorDistanceFunctions();
        }
        
        private void InitializeColorDistanceFunctions()
        {
            colorDistanceFunctions = new Dictionary<ColorSpace, Func<Color, Color, float>>
            {
                { ColorSpace.RGB, CalculateRGBDistance },
                { ColorSpace.HSV, CalculateHSVDistance },
                { ColorSpace.LAB, CalculateLABDistance }
            };
        }
        
        /// <summary>
        /// Compare two colors and return detailed matching result
        /// </summary>
        public ColorMatchResult CompareColors(Color referenceColor, Color paintColor)
        {
            try
            {
                ColorMatchResult result = new ColorMatchResult(referenceColor, paintColor);
                
                // Calculate different color distance metrics
                result.deltaE = CalculateDeltaE(referenceColor, paintColor);
                result.rgbDifference = CalculateRGBDifference(referenceColor, paintColor);
                result.hsvDifference = CalculateHSVDifference(referenceColor, paintColor);
                
                // Calculate overall match accuracy
                result.matchAccuracy = CalculateMatchAccuracy(result.deltaE);
                
                // Determine match quality
                result.matchQuality = DetermineMatchQuality(result.deltaE);
                
                // Generate adjustment suggestions
                result.adjustmentSuggestions = GenerateAdjustmentSuggestions(referenceColor, paintColor);
                
                LastMatchResult = result;
                OnColorMatched?.Invoke(result);
                
                Debug.Log($"ColorMatcher: Colors compared - Delta E: {result.deltaE:F2}, Quality: {result.matchQuality}");
                
                return result;
            }
            catch (Exception e)
            {
                OnMatchingError?.Invoke($"Color comparison failed: {e.Message}");
                return new ColorMatchResult(referenceColor, paintColor);
            }
        }
        
        /// <summary>
        /// Calculate Delta E color difference (CIE76 approximation)
        /// </summary>
        private float CalculateDeltaE(Color color1, Color color2)
        {
            switch (matchingMethod)
            {
                case ColorMatchingMethod.DeltaE:
                    return CalculateDeltaE76(color1, color2);
                case ColorMatchingMethod.RGB:
                    return CalculateRGBDistance(color1, color2);
                case ColorMatchingMethod.HSV:
                    return CalculateHSVDistance(color1, color2);
                case ColorMatchingMethod.LAB:
                    return CalculateLABDistance(color1, color2);
                default:
                    return CalculateDeltaE76(color1, color2);
            }
        }
        
        private float CalculateDeltaE76(Color color1, Color color2)
        {
            // Convert RGB to LAB color space (simplified approximation)
            Vector3 lab1 = RGBToLAB(color1);
            Vector3 lab2 = RGBToLAB(color2);
            
            // Calculate Delta E using CIE76 formula
            float deltaL = lab1.x - lab2.x;
            float deltaA = lab1.y - lab2.y;
            float deltaB = lab1.z - lab2.z;
            
            return Mathf.Sqrt(deltaL * deltaL + deltaA * deltaA + deltaB * deltaB);
        }
        
        private Vector3 RGBToLAB(Color rgb)
        {
            // Simplified RGB to LAB conversion
            // In production, use proper XYZ intermediate conversion
            
            // Gamma correction
            float r = GammaCorrect(rgb.r);
            float g = GammaCorrect(rgb.g);
            float b = GammaCorrect(rgb.b);
            
            // Convert to XYZ (simplified sRGB matrix)
            float x = r * 0.4124f + g * 0.3576f + b * 0.1805f;
            float y = r * 0.2126f + g * 0.7152f + b * 0.0722f;
            float z = r * 0.0193f + g * 0.1192f + b * 0.9505f;
            
            // Normalize by D65 illuminant
            x /= 0.95047f;
            y /= 1.00000f;
            z /= 1.08883f;
            
            // Convert XYZ to LAB
            x = LabFunction(x);
            y = LabFunction(y);
            z = LabFunction(z);
            
            float L = 116f * y - 16f;
            float A = 500f * (x - y);
            float B = 200f * (y - z);
            
            return new Vector3(L, A, B);
        }
        
        private float GammaCorrect(float value)
        {
            return value > 0.04045f ? Mathf.Pow((value + 0.055f) / 1.055f, 2.4f) : value / 12.92f;
        }
        
        private float LabFunction(float t)
        {
            return t > 0.008856f ? Mathf.Pow(t, 1f / 3f) : (7.787f * t + 16f / 116f);
        }
        
        private float CalculateRGBDistance(Color color1, Color color2)
        {
            Vector3 diff = new Vector3(
                color1.r - color2.r,
                color1.g - color2.g,
                color1.b - color2.b
            );
            
            return diff.magnitude * 100f; // Scale to typical Delta E range
        }
        
        private float CalculateHSVDistance(Color color1, Color color2)
        {
            Color.RGBToHSV(color1, out float h1, out float s1, out float v1);
            Color.RGBToHSV(color2, out float h2, out float s2, out float v2);
            
            // Handle hue wraparound
            float hueDiff = Mathf.Abs(h1 - h2);
            if (hueDiff > 0.5f) hueDiff = 1f - hueDiff;
            
            Vector3 hsvDiff = new Vector3(hueDiff * 2f, s1 - s2, v1 - v2); // Weight hue more
            return hsvDiff.magnitude * 100f;
        }
        
        private float CalculateLABDistance(Color color1, Color color2)
        {
            // Use the Delta E calculation as LAB distance
            return CalculateDeltaE76(color1, color2);
        }
        
        private Vector3 CalculateRGBDifference(Color color1, Color color2)
        {
            return new Vector3(
                Mathf.Abs(color1.r - color2.r),
                Mathf.Abs(color1.g - color2.g),
                Mathf.Abs(color1.b - color2.b)
            );
        }
        
        private Vector3 CalculateHSVDifference(Color color1, Color color2)
        {
            Color.RGBToHSV(color1, out float h1, out float s1, out float v1);
            Color.RGBToHSV(color2, out float h2, out float s2, out float v2);
            
            float hueDiff = Mathf.Abs(h1 - h2);
            if (hueDiff > 0.5f) hueDiff = 1f - hueDiff;
            
            return new Vector3(hueDiff, Mathf.Abs(s1 - s2), Mathf.Abs(v1 - v2));
        }
        
        private float CalculateMatchAccuracy(float deltaE)
        {
            // Convert Delta E to 0-1 accuracy scale
            // Delta E of 0 = 100% accuracy, Delta E of 20+ = 0% accuracy
            return Mathf.Clamp01(1f - (deltaE / 20f));
        }
        
        private string DetermineMatchQuality(float deltaE)
        {
            if (deltaE <= excellentThreshold) return "Excellent";
            if (deltaE <= goodThreshold) return "Good";
            if (deltaE <= fairThreshold) return "Fair";
            return "Poor";
        }
        
        private string[] GenerateAdjustmentSuggestions(Color reference, Color paint)
        {
            List<string> suggestions = new List<string>();
            
            Vector3 rgbDiff = CalculateRGBDifference(reference, paint);
            Color.RGBToHSV(reference, out float refH, out float refS, out float refV);
            Color.RGBToHSV(paint, out float paintH, out float paintS, out float paintV);
            
            // RGB-based suggestions
            if (rgbDiff.x > 0.1f)
            {
                suggestions.Add(reference.r > paint.r ? "Add more red" : "Reduce red intensity");
            }
            if (rgbDiff.y > 0.1f)
            {
                suggestions.Add(reference.g > paint.g ? "Add more green" : "Reduce green intensity");
            }
            if (rgbDiff.z > 0.1f)
            {
                suggestions.Add(reference.b > paint.b ? "Add more blue" : "Reduce blue intensity");
            }
            
            // HSV-based suggestions
            float hueDiff = Mathf.Abs(refH - paintH);
            if (hueDiff > 0.5f) hueDiff = 1f - hueDiff;
            
            if (hueDiff > 0.05f)
            {
                if (refH > paintH)
                    suggestions.Add("Shift hue towards warmer tones");
                else
                    suggestions.Add("Shift hue towards cooler tones");
            }
            
            if (Mathf.Abs(refS - paintS) > 0.1f)
            {
                suggestions.Add(refS > paintS ? "Increase color saturation" : "Decrease color saturation");
            }
            
            if (Mathf.Abs(refV - paintV) > 0.1f)
            {
                suggestions.Add(refV > paintV ? "Make color brighter" : "Make color darker");
            }
            
            // If colors are very close, provide encouragement
            if (suggestions.Count == 0)
            {
                suggestions.Add("Excellent match! Colors are very close.");
            }
            
            return suggestions.ToArray();
        }
        
        /// <summary>
        /// Save a color match to history
        /// </summary>
        public void SaveColorMatch(ColorMatchData matchData)
        {
            if (matchData == null) return;
            
            MatchHistory.Add(matchData);
            OnMatchSaved?.Invoke(matchData);
            
            // Limit history size
            if (MatchHistory.Count > 100)
            {
                MatchHistory.RemoveAt(0);
            }
            
            // Save to persistent storage
            SaveMatchHistoryToDisk();
            
            Debug.Log($"ColorMatcher: Match saved - Accuracy: {matchData.matchAccuracy:F2}");
        }
        
        /// <summary>
        /// Get color match history
        /// </summary>
        public List<ColorMatchData> GetColorHistory()
        {
            return new List<ColorMatchData>(MatchHistory);
        }
        
        /// <summary>
        /// Clear match history
        /// </summary>
        public void ClearMatchHistory()
        {
            MatchHistory.Clear();
            PlayerPrefs.DeleteKey("ColorMatchHistory");
            Debug.Log("ColorMatcher: Match history cleared");
        }
        
        /// <summary>
        /// Set color matching method
        /// </summary>
        public void SetMatchingMethod(ColorMatchingMethod method)
        {
            matchingMethod = method;
            Debug.Log($"ColorMatcher: Matching method set to {method}");
        }
        
        /// <summary>
        /// Set match quality thresholds
        /// </summary>
        public void SetQualityThresholds(float excellent, float good, float fair)
        {
            excellentThreshold = excellent;
            goodThreshold = good;
            fairThreshold = fair;
        }
        
        private void SaveMatchHistoryToDisk()
        {
            try
            {
                ColorMatchHistoryData historyData = new ColorMatchHistoryData
                {
                    matches = MatchHistory.ToArray()
                };
                
                string json = JsonUtility.ToJson(historyData);
                PlayerPrefs.SetString("ColorMatchHistory", json);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"ColorMatcher: Failed to save match history - {e.Message}");
            }
        }
        
        private void LoadMatchHistoryFromDisk()
        {
            try
            {
                string json = PlayerPrefs.GetString("ColorMatchHistory", "");
                if (!string.IsNullOrEmpty(json))
                {
                    ColorMatchHistoryData historyData = JsonUtility.FromJson<ColorMatchHistoryData>(json);
                    MatchHistory = new List<ColorMatchData>(historyData.matches);
                    Debug.Log($"ColorMatcher: Loaded {MatchHistory.Count} matches from history");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"ColorMatcher: Failed to load match history - {e.Message}");
                MatchHistory = new List<ColorMatchData>();
            }
        }
        
        private void Start()
        {
            LoadMatchHistoryFromDisk();
        }
        
        /// <summary>
        /// Get statistics about match history
        /// </summary>
        public ColorMatchStatistics GetMatchStatistics()
        {
            if (MatchHistory.Count == 0)
                return new ColorMatchStatistics();
            
            float totalAccuracy = 0f;
            int excellentCount = 0, goodCount = 0, fairCount = 0, poorCount = 0;
            
            foreach (var match in MatchHistory)
            {
                totalAccuracy += match.matchAccuracy;
                
                ColorMatchResult result = CompareColors(match.referenceColor, match.capturedColor);
                switch (result.matchQuality)
                {
                    case "Excellent": excellentCount++; break;
                    case "Good": goodCount++; break;
                    case "Fair": fairCount++; break;
                    case "Poor": poorCount++; break;
                }
            }
            
            return new ColorMatchStatistics
            {
                totalMatches = MatchHistory.Count,
                averageAccuracy = totalAccuracy / MatchHistory.Count,
                excellentMatches = excellentCount,
                goodMatches = goodCount,
                fairMatches = fairCount,
                poorMatches = poorCount
            };
        }
    }
    
    /// <summary>
    /// Color matching methods
    /// </summary>
    public enum ColorMatchingMethod
    {
        DeltaE,     // CIE Delta E (most accurate)
        RGB,        // RGB Euclidean distance
        HSV,        // HSV color space distance
        LAB         // LAB color space distance
    }
    
    /// <summary>
    /// Color spaces for distance calculation
    /// </summary>
    public enum ColorSpace
    {
        RGB,
        HSV,
        LAB
    }
    
    /// <summary>
    /// Match history data for serialization
    /// </summary>
    [System.Serializable]
    public class ColorMatchHistoryData
    {
        public ColorMatchData[] matches;
    }
    
    /// <summary>
    /// Color match statistics
    /// </summary>
    [System.Serializable]
    public class ColorMatchStatistics
    {
        public int totalMatches;
        public float averageAccuracy;
        public int excellentMatches;
        public int goodMatches;
        public int fairMatches;
        public int poorMatches;
    }
}