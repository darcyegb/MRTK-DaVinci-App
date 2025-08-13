using UnityEngine;

namespace DaVinciEye.ImageOverlay
{
    /// <summary>
    /// Data structure for image adjustment parameters
    /// </summary>
    [System.Serializable]
    public class ImageAdjustments
    {
        [Header("Cropping")]
        public Rect cropArea = new Rect(0, 0, 1, 1);  // Normalized coordinates (0-1)
        public bool isCropped = false;
        
        [Header("Color Adjustments")]
        [Range(-1f, 1f)]
        public float contrast = 0f;
        
        [Range(-1f, 1f)]
        public float exposure = 0f;
        
        [Range(-180f, 180f)]
        public float hue = 0f;
        
        [Range(-1f, 1f)]
        public float saturation = 0f;
        
        [Header("State")]
        public bool isModified = false;
        
        /// <summary>
        /// Resets all adjustments to default values
        /// </summary>
        public void Reset()
        {
            cropArea = new Rect(0, 0, 1, 1);
            isCropped = false;
            contrast = 0f;
            exposure = 0f;
            hue = 0f;
            saturation = 0f;
            isModified = false;
        }
        
        /// <summary>
        /// Checks if any adjustments have been made
        /// </summary>
        public void UpdateModifiedState()
        {
            isModified = isCropped || 
                        Mathf.Abs(contrast) > 0.01f || 
                        Mathf.Abs(exposure) > 0.01f || 
                        Mathf.Abs(hue) > 0.01f || 
                        Mathf.Abs(saturation) > 0.01f;
        }
    }
}