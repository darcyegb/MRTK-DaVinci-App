using System;
using UnityEngine;

namespace DaVinciEye.Input
{
    /// <summary>
    /// Enumeration of interaction modes
    /// </summary>
    public enum InteractionMode
    {
        Near,           // Direct hand interaction
        Far,            // Ray-based interaction
        Voice,          // Voice commands
        Automatic       // Context-aware mode switching
    }
    
    /// <summary>
    /// Enumeration of gesture types
    /// </summary>
    public enum GestureType
    {
        None,
        AirTap,
        Pinch,
        Drag,
        TwoHandPinch,
        Palm,
        Point,
        Grab
    }
    
    /// <summary>
    /// Data structure for gesture information
    /// </summary>
    [System.Serializable]
    public class GestureData
    {
        [Header("Gesture Information")]
        public GestureType type;
        public Vector3 position;
        public Vector3 direction;
        public float confidence;
        
        [Header("Hand Information")]
        public bool isRightHand;
        public Vector3 handPosition;
        public Quaternion handRotation;
        
        [Header("Timing")]
        public float duration;
        public DateTime timestamp;
        
        public GestureData()
        {
            timestamp = DateTime.Now;
        }
        
        public GestureData(GestureType gestureType, Vector3 pos, bool rightHand = true)
        {
            type = gestureType;
            position = pos;
            isRightHand = rightHand;
            timestamp = DateTime.Now;
            confidence = 1f;
        }
    }
    
    /// <summary>
    /// Data structure for pinch gesture information
    /// </summary>
    [System.Serializable]
    public class PinchData
    {
        [Header("Pinch Information")]
        public Vector3 startPosition;
        public Vector3 currentPosition;
        public Vector3 deltaPosition;
        
        [Header("Pinch State")]
        public float pinchStrength;     // 0-1 how "closed" the pinch is
        public bool isActive;
        public float duration;
        
        [Header("Hand Information")]
        public bool isRightHand;
        public Vector3 thumbPosition;
        public Vector3 indexPosition;
        
        [Header("Timing")]
        public DateTime startTime;
        public DateTime lastUpdateTime;
        
        public PinchData()
        {
            startTime = DateTime.Now;
            lastUpdateTime = DateTime.Now;
        }
        
        public void UpdatePosition(Vector3 newPosition)
        {
            deltaPosition = newPosition - currentPosition;
            currentPosition = newPosition;
            lastUpdateTime = DateTime.Now;
            duration = (float)(lastUpdateTime - startTime).TotalSeconds;
        }
    }
    
    /// <summary>
    /// Configuration for input sensitivity and thresholds
    /// </summary>
    [System.Serializable]
    public class InputConfiguration
    {
        [Header("Gesture Thresholds")]
        [Range(0f, 1f)]
        public float pinchThreshold = 0.7f;
        
        [Range(0f, 1f)]
        public float airTapThreshold = 0.8f;
        
        [Range(0.01f, 0.5f)]
        public float dragSensitivity = 0.1f;
        
        [Header("Hand Tracking")]
        [Range(0f, 1f)]
        public float handConfidenceThreshold = 0.5f;
        
        public float handLossTimeout = 2f;  // seconds
        
        [Header("Interaction")]
        public float nearInteractionDistance = 0.1f;  // meters
        public float farInteractionMaxDistance = 10f; // meters
        
        public InputConfiguration()
        {
            // Set default values
            pinchThreshold = 0.7f;
            airTapThreshold = 0.8f;
            dragSensitivity = 0.1f;
            handConfidenceThreshold = 0.5f;
            handLossTimeout = 2f;
            nearInteractionDistance = 0.1f;
            farInteractionMaxDistance = 10f;
        }
    }
}