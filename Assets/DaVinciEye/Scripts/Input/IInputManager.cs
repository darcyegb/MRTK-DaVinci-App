using System;
using UnityEngine;

namespace DaVinciEye.Input
{
    /// <summary>
    /// Interface for input management and gesture recognition
    /// </summary>
    public interface IInputManager
    {
        bool IsHandTrackingActive { get; }
        Vector3 DominantHandPosition { get; }
        Vector3 DominantHandForward { get; }
        
        void EnableGestureRecognition();
        void DisableGestureRecognition();
        void SetInteractionMode(InteractionMode mode);
        
        event Action<GestureData> OnGestureRecognized;
        event Action<Vector3> OnAirTap;
        event Action<PinchData> OnPinchStart;
        event Action<PinchData> OnPinchUpdate;
        event Action<PinchData> OnPinchEnd;
        event Action OnHandTrackingLost;
        event Action OnHandTrackingRestored;
    }
}