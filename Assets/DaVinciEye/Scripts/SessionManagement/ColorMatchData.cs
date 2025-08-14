using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DaVinciEye.SessionManagement
{
    /// <summary>
    /// Data structure for storing color match information
    /// Contains reference color, captured color, and matching metadata
    /// </summary>
    [System.Serializable]
    public class ColorMatchData
    {
        [Header("Color Information")]
        public Color referenceColor;
        public Color capturedColor;
        public float matchAccuracy;
        
        [Header("Spatial Information")]
        public Vector3 capturePosition;
        public Vector2 imageCoordinate;
        
        [Header("Session Information")]
        public string sessionId;
        public DateTime timestamp;
        public string notes;
        
        [Header("Additional Metadata")]
        public float deltaE;
        public string matchQuality;
        public string[] adjustmentSuggestions;
        public ColorMatchingMethod matchingMethod;
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public ColorMatchData()
        {
            referenceColor = Color.white;
            capturedColor = Color.white;
            matchAccuracy = 0f;
            capturePosition = Vector3.zero;
            imageCoordinate = Vector2.zero;
            sessionId = "";
            timestamp = DateTime.Now;
            notes = "";
            deltaE = 0f;
            matchQuality = "Unknown";
            adjustmentSuggestions = new string[0];
            matchingMethod = ColorMatchingMethod.DeltaE;
        }
        
        /// <summary>
        /// Constructor with basic color information
        /// </summary>
        public ColorMatchData(Color reference, Color captured, Vector3 position)
        {
            referenceColor = reference;
            capturedColor = captured;
            capturePosition = position;
            timestamp = DateTime.Now;
            
            // Initialize other fields with defaults
            matchAccuracy = 0f;
            imageCoordinate = Vector2.zero;
            sessionId = "";
            notes = "";
            deltaE = 0f;
            matchQuality = "Unknown";
            adjustmentSuggestions = new string[0];
            matchingMethod = ColorMatchingMethod.DeltaE;
        }
        
        /// <summary>
        /// Constructor with full color match result
        /// </summary>
        public ColorMatchData(ColorMatchResult matchResult, Vector3 position, Vector2 imageCoord = default)
        {
            referenceColor = matchResult.referenceColor;
            capturedColor = matchResult.paintColor;
            matchAccuracy = matchResult.matchAccuracy;
            deltaE = matchResult.deltaE;
            matchQuality = matchResult.matchQuality;
            adjustmentSuggestions = matchResult.adjustmentSuggestions ?? new string[0];
            
            capturePosition = position;
            imageCoordinate = imageCoord;
            timestamp = DateTime.Now;
            
            // Initialize other fields with defaults
            sessionId = "";
            notes = "";
            matchingMethod = ColorMatchingMethod.DeltaE;
        }
        
        /// <summary>
        /// Get a human-readable description of the match
        /// </summary>
        public string GetMatchDescription()
        {
            return $"Match Quality: {matchQuality}, Accuracy: {matchAccuracy:P1}, Delta E: {deltaE:F1}";
        }
        
        /// <summary>
        /// Get RGB difference as a Vector3
        /// </summary>
        public Vector3 GetRGBDifference()
        {
            return new Vector3(
                Mathf.Abs(referenceColor.r - capturedColor.r),
                Mathf.Abs(referenceColor.g - capturedColor.g),
                Mathf.Abs(referenceColor.b - capturedColor.b)
            );
        }
        
        /// <summary>
        /// Get HSV difference as a Vector3
        /// </summary>
        public Vector3 GetHSVDifference()
        {
            Color.RGBToHSV(referenceColor, out float refH, out float refS, out float refV);
            Color.RGBToHSV(capturedColor, out float capH, out float capS, out float capV);
            
            float hueDiff = Mathf.Abs(refH - capH);
            if (hueDiff > 0.5f) hueDiff = 1f - hueDiff; // Handle hue wraparound
            
            return new Vector3(hueDiff, Mathf.Abs(refS - capS), Mathf.Abs(refV - capV));
        }
        
        /// <summary>
        /// Check if this match is considered good quality
        /// </summary>
        public bool IsGoodMatch(float threshold = 0.7f)
        {
            return matchAccuracy >= threshold;
        }
        
        /// <summary>
        /// Get formatted timestamp string
        /// </summary>
        public string GetFormattedTimestamp()
        {
            return timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        /// <summary>
        /// Clone this color match data
        /// </summary>
        public ColorMatchData Clone()
        {
            var clone = new ColorMatchData
            {
                referenceColor = referenceColor,
                capturedColor = capturedColor,
                matchAccuracy = matchAccuracy,
                capturePosition = capturePosition,
                imageCoordinate = imageCoordinate,
                sessionId = sessionId,
                timestamp = timestamp,
                notes = notes,
                deltaE = deltaE,
                matchQuality = matchQuality,
                adjustmentSuggestions = (string[])adjustmentSuggestions.Clone(),
                matchingMethod = matchingMethod
            };
            
            return clone;
        }
        
        /// <summary>
        /// Convert to JSON string
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
        
        /// <summary>
        /// Create from JSON string
        /// </summary>
        public static ColorMatchData FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<ColorMatchData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"ColorMatchData: Failed to parse JSON - {e.Message}");
                return new ColorMatchData();
            }
        }
    }
    
    /// <summary>
    /// Color matching result data structure
    /// </summary>
    [System.Serializable]
    public class ColorMatchResult
    {
        public Color referenceColor;
        public Color paintColor;
        public float matchAccuracy;
        public float deltaE;
        public Vector3 rgbDifference;
        public Vector3 hsvDifference;
        public string matchQuality;
        public string[] adjustmentSuggestions;
        
        public ColorMatchResult()
        {
            referenceColor = Color.white;
            paintColor = Color.white;
            matchAccuracy = 0f;
            deltaE = 0f;
            rgbDifference = Vector3.zero;
            hsvDifference = Vector3.zero;
            matchQuality = "Unknown";
            adjustmentSuggestions = new string[0];
        }
        
        public ColorMatchResult(Color reference, Color paint)
        {
            referenceColor = reference;
            paintColor = paint;
            matchAccuracy = 0f;
            deltaE = 0f;
            rgbDifference = Vector3.zero;
            hsvDifference = Vector3.zero;
            matchQuality = "Unknown";
            adjustmentSuggestions = new string[0];
        }
    }
    
    /// <summary>
    /// Color matching session data
    /// </summary>
    [System.Serializable]
    public class ColorMatchSession
    {
        public string sessionId;
        public DateTime startTime;
        public DateTime endTime;
        public List<ColorMatchData> matches;
        public string sessionName;
        public string notes;
        
        public ColorMatchSession()
        {
            sessionId = Guid.NewGuid().ToString();
            startTime = DateTime.Now;
            endTime = default;
            matches = new List<ColorMatchData>();
            sessionName = "";
            notes = "";
        }
        
        public TimeSpan GetDuration()
        {
            if (endTime == default)
                return DateTime.Now - startTime;
            return endTime - startTime;
        }
        
        public int GetMatchCount()
        {
            return matches?.Count ?? 0;
        }
        
        public float GetAverageAccuracy()
        {
            if (matches == null || matches.Count == 0)
                return 0f;
            
            return matches.Average(m => m.matchAccuracy);
        }
    }
    
    /// <summary>
    /// Filter criteria for color match queries
    /// </summary>
    [System.Serializable]
    public class ColorMatchFilter
    {
        public DateTime? startDate;
        public DateTime? endDate;
        public float? minAccuracy;
        public float? maxAccuracy;
        public string sessionId;
        public Color? referenceColor;
        public float? colorTolerance;
        public string matchQuality;
        public ColorMatchingMethod? matchingMethod;
        
        public ColorMatchFilter()
        {
            startDate = null;
            endDate = null;
            minAccuracy = null;
            maxAccuracy = null;
            sessionId = null;
            referenceColor = null;
            colorTolerance = null;
            matchQuality = null;
            matchingMethod = null;
        }
    }
    
    /// <summary>
    /// Color match statistics
    /// </summary>
    [System.Serializable]
    public class ColorMatchStatistics
    {
        public int totalMatches;
        public int sessionMatches;
        public float averageAccuracy;
        public ColorMatchData bestMatch;
        public ColorMatchData worstMatch;
        public int excellentMatches;
        public int goodMatches;
        public int fairMatches;
        public int poorMatches;
        public int totalSessions;
        public int recentMatches;
        
        public ColorMatchStatistics()
        {
            totalMatches = 0;
            sessionMatches = 0;
            averageAccuracy = 0f;
            bestMatch = null;
            worstMatch = null;
            excellentMatches = 0;
            goodMatches = 0;
            fairMatches = 0;
            poorMatches = 0;
            totalSessions = 0;
            recentMatches = 0;
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
    /// Data structure for history persistence
    /// </summary>
    [System.Serializable]
    public class ColorHistoryData
    {
        public string version;
        public DateTime saveDate;
        public ColorMatchData[] matches;
        
        public ColorHistoryData()
        {
            version = "1.0";
            saveDate = DateTime.Now;
            matches = new ColorMatchData[0];
        }
    }
    
    /// <summary>
    /// Data structure for history export/import
    /// </summary>
    [System.Serializable]
    public class ColorHistoryExportData
    {
        public DateTime exportDate;
        public int totalMatches;
        public ColorMatchData[] matches;
        public string exportVersion;
        public string applicationVersion;
        
        public ColorHistoryExportData()
        {
            exportDate = DateTime.Now;
            totalMatches = 0;
            matches = new ColorMatchData[0];
            exportVersion = "1.0";
            applicationVersion = Application.version;
        }
    }
}