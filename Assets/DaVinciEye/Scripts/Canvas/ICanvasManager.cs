using System;
using UnityEngine;

namespace DaVinciEye.Canvas
{
    /// <summary>
    /// Interface for managing canvas definition, tracking, and visualization
    /// </summary>
    public interface ICanvasManager
    {
        bool IsCanvasDefined { get; }
        Bounds CanvasBounds { get; }
        Transform CanvasTransform { get; }
        CanvasData CurrentCanvas { get; }
        
        void StartCanvasDefinition();
        void DefineCanvasCorner(Vector3 worldPosition);
        void CompleteCanvasDefinition();
        void RedefineCanvas();
        void LoadCanvas(CanvasData canvasData);
        
        event Action<CanvasData> OnCanvasDefined;
        event Action OnCanvasCleared;
    }
}