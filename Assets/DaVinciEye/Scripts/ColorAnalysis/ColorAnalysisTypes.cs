using System;
using UnityEngine;

namespace DaVinciEye.ColorAnalysis
{
    /// <summary>
    /// Data structure for color matching results
    /// </summary>
    [System.Serializable]
    public class ColorMatchResult
    {
        [Header("Colors")]
        public Color referenceColor;
        public Color capturedColor;
        
        [Header("Analysis")]
        [Range(0f, 1f)]
        public float matchAccuracy;
        
        public float deltaE;        // CIE Delta E color difference
        public Vector3 rgbDifference;
        public Vector3 hsvDifference;
        
        [Header("Recommendations")]
        public string matchQuality;  // "Excellent", "Good", "Fair", "Poor"
        public string[] adjustmentSuggestions;
        
        public ColorMatchResult()
        {
            adjustmentSuggestions = new string[0];
        }
        
        public ColorMatchResult(Color reference, Color captured)
        {
            referenceColor = reference;
            capturedColor = captured;
            CalculateMatchMetrics();
        }
        
        private void CalculateMatchMetrics()
        {
            // Calculate RGB difference
            rgbDifference = new Vector3(
                Mathf.Abs(referenceColor.r - capturedColor.r),
                Mathf.Abs(referenceColor.g - capturedColor.g),
                Mathf.Abs(referenceColor.b - capturedColor.b)
            );
            
            // Calculate HSV difference
            Color.RGBToHSV(referenceColor, out float refH, out float refS, out float refV);
            Color.RGBToHSV(capturedColor, out float capH, out float capS, out float capV);
            
            hsvDifference = new Vector3(
                Mathf.Abs(refH - capH),
                Mathf.Abs(refS - capS),
                Mathf.Abs(refV - capV)
            );
            
            // Calculate simplified Delta E (Euclidean distance in RGB space)
            float rgbDistance = Mathf.Sqrt(
                rgbDifference.x * rgbDifference.x +
                rgbDifference.g * rgbDifference.g +
                rgbDifference.b * rgbDifference.b
            );
            
            deltaE = rgbDistance * 100f; // Scale to typical Delta E range
            matchAccuracy = Mathf.Clamp01(1f - (rgbDistance / 1.732f)); // Max distance is sqrt(3)
            
            // Determine match quality
            if (matchAccuracy >= 0.95f) matchQuality = "Excellent";
            else if (matchAccuracy >= 0.85f) matchQuality = "Good";
            else if (matchAccuracy >= 0.70f) matchQuality = "Fair";
            else matchQuality = "Poor";
        }
    }
    
    /// <summary>
    /// Data structure for storing color match history
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
        public DateTime timestamp;
        public string sessionId;
        public string notes;
        
        [Header("Environmental Data")]
        public float lightingCondition;  // 0-1 scale
        public string environmentType;   // "Indoor", "Outdoor", "Mixed"
        
        public ColorMatchData()
        {
            timestamp = DateTime.Now;
            sessionId = System.Guid.NewGuid().ToString();
        }
        
        public ColorMatchData(Color reference, Color captured, Vector3 position)
        {
            referenceColor = reference;
            capturedColor = captured;
            capturePosition = position;
            timestamp = DateTime.Now;
            sessionId = System.Guid.NewGuid().ToString();
            
            // Calculate match accuracy
            Vector3 diff = new Vector3(
                Mathf.Abs(reference.r - captured.r),
                Mathf.Abs(reference.g - captured.g),
                Mathf.Abs(reference.b - captured.b)
            );
            float distance = Mathf.Sqrt(diff.x * diff.x + diff.y * diff.y + diff.z * diff.z);
            matchAccuracy = Mathf.Clamp01(1f - (distance / 1.732f));
        }
    }
}