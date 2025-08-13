using System;
using System.Threading.Tasks;
using UnityEngine;

namespace DaVinciEye.ImageOverlay
{
    /// <summary>
    /// Interface for image overlay functionality including loading, display, and adjustments
    /// </summary>
    public interface IImageOverlay
    {
        Texture2D CurrentImage { get; }
        float Opacity { get; set; }
        bool IsVisible { get; set; }
        ImageAdjustments CurrentAdjustments { get; }
        
        Task<bool> LoadImageAsync(string imagePath);
        void SetOpacity(float opacity);
        void ApplyAdjustments(ImageAdjustments adjustments);
        void SetCropArea(Rect cropRect);
        void ResetToOriginal();
        
        event Action<Texture2D> OnImageLoaded;
        event Action<float> OnOpacityChanged;
        event Action<ImageAdjustments> OnAdjustmentsApplied;
    }
}