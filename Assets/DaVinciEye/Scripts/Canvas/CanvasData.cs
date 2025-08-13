using System;
using UnityEngine;

namespace DaVinciEye.Canvas
{
    /// <summary>
    /// Data structure for storing canvas configuration and spatial information
    /// </summary>
    [System.Serializable]
    public class CanvasData
    {
        [Header("Canvas Geometry")]
        public Vector3[] corners = new Vector3[4];  // Always 4 corners in clockwise order
        public Vector3 center;
        public Vector2 dimensions;  // width, height in meters
        
        [Header("Spatial Tracking")]
        public string anchorId;     // Spatial anchor identifier
        public DateTime createdAt;
        
        [Header("Validation")]
        public bool isValid;
        public float area;          // Canvas area in square meters
        
        public CanvasData()
        {
            corners = new Vector3[4];
            createdAt = DateTime.Now;
            isValid = false;
        }
        
        /// <summary>
        /// Validates canvas geometry and calculates derived properties
        /// </summary>
        public void ValidateAndCalculate()
        {
            if (corners == null || corners.Length != 4)
            {
                isValid = false;
                return;
            }
            
            // Calculate center point
            center = Vector3.zero;
            for (int i = 0; i < 4; i++)
            {
                center += corners[i];
            }
            center /= 4f;
            
            // Calculate dimensions (assuming rectangular canvas)
            dimensions.x = Vector3.Distance(corners[0], corners[1]);
            dimensions.y = Vector3.Distance(corners[1], corners[2]);
            
            // Calculate area
            area = dimensions.x * dimensions.y;
            
            // Basic validation
            isValid = area > 0.01f && area < 100f; // Between 1cm² and 100m²
        }
    }
}