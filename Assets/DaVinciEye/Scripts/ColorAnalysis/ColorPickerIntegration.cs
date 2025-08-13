using UnityEngine;
using DaVinciEye.ImageOverlay;

namespace DaVinciEye.ColorAnalysis
{
    /// <summary>
    /// Integration component that connects ColorPicker with ImageOverlayManager
    /// Automatically updates color picker when images are loaded or changed
    /// </summary>
    [RequireComponent(typeof(ColorPicker))]
    public class ColorPickerIntegration : MonoBehaviour
    {
        [Header("Integration Settings")]
        [SerializeField] private bool autoConnectToImageOverlay = true;
        [SerializeField] private bool updateOnImageChange = true;
        [SerializeField] private bool updateOnFilterChange = true;
        
        [Header("References")]
        [SerializeField] private ImageOverlayManager imageOverlayManager;
        [SerializeField] private ColorPicker colorPicker;
        
        // Events
        public System.Action<Color> OnColorSelectedFromImage;
        
        private void Awake()
        {
            if (colorPicker == null)
                colorPicker = GetComponent<ColorPicker>();
        }
        
        private void Start()
        {
            if (autoConnectToImageOverlay)
            {
                ConnectToImageOverlay();
            }
            
            SetupEventListeners();
        }
        
        private void ConnectToImageOverlay()
        {
            if (imageOverlayManager == null)
            {
                imageOverlayManager = FindObjectOfType<ImageOverlayManager>();
            }
            
            if (imageOverlayManager != null)
            {
                // Subscribe to image overlay events
                imageOverlayManager.OnImageLoaded += HandleImageLoaded;
                
                // If there's already an image loaded, update the color picker
                if (imageOverlayManager.CurrentImage != null)
                {
                    UpdateColorPickerTexture(imageOverlayManager.CurrentImage);
                }
                
                Debug.Log("ColorPickerIntegration: Connected to ImageOverlayManager");
            }
            else
            {
                Debug.LogWarning("ColorPickerIntegration: No ImageOverlayManager found in scene");
            }
        }
        
        private void SetupEventListeners()
        {
            if (colorPicker != null)
            {
                colorPicker.OnColorPicked += HandleColorPicked;
                colorPicker.OnColorChanged += HandleColorChanged;
            }
        }
        
        private void HandleImageLoaded(Texture2D loadedImage)
        {
            if (updateOnImageChange && loadedImage != null)
            {
                UpdateColorPickerTexture(loadedImage);
                Debug.Log($"ColorPickerIntegration: Updated color picker with new image {loadedImage.width}x{loadedImage.height}");
            }
        }
        
        private void HandleColorPicked(Color pickedColor)
        {
            OnColorSelectedFromImage?.Invoke(pickedColor);
            
            // Store the picked color for potential paint matching
            StorePickedColor(pickedColor);
            
            Debug.Log($"ColorPickerIntegration: Color picked from image - RGB({pickedColor.r:F3}, {pickedColor.g:F3}, {pickedColor.b:F3})");
        }
        
        private void HandleColorChanged(Color newColor)
        {
            // This handles manual color adjustments via HSV sliders
            Debug.Log($"ColorPickerIntegration: Color manually adjusted - RGB({newColor.r:F3}, {newColor.g:F3}, {newColor.b:F3})");
        }
        
        private void UpdateColorPickerTexture(Texture2D texture)
        {
            if (colorPicker != null && texture != null)
            {
                colorPicker.SetTexture(texture);
            }
        }
        
        private void StorePickedColor(Color color)
        {
            // Store in PlayerPrefs for session persistence
            string colorKey = $"PickedColor_{System.DateTime.Now.Ticks}";
            string colorJson = JsonUtility.ToJson(new SerializableColor(color));
            PlayerPrefs.SetString(colorKey, colorJson);
            
            // Also store as "last picked color" for quick access
            PlayerPrefs.SetString("LastPickedColor", colorJson);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Get the last picked color from storage
        /// </summary>
        public Color GetLastPickedColor()
        {
            string colorJson = PlayerPrefs.GetString("LastPickedColor", "");
            if (!string.IsNullOrEmpty(colorJson))
            {
                try
                {
                    SerializableColor serializableColor = JsonUtility.FromJson<SerializableColor>(colorJson);
                    return serializableColor.ToColor();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"ColorPickerIntegration: Failed to load last picked color - {e.Message}");
                }
            }
            
            return Color.white;
        }
        
        /// <summary>
        /// Manually set the image texture for color picking
        /// </summary>
        public void SetImageTexture(Texture2D texture)
        {
            UpdateColorPickerTexture(texture);
        }
        
        /// <summary>
        /// Get the current color picker component
        /// </summary>
        public ColorPicker GetColorPicker()
        {
            return colorPicker;
        }
        
        /// <summary>
        /// Enable or disable automatic updates
        /// </summary>
        public void SetAutoUpdate(bool imageChange, bool filterChange)
        {
            updateOnImageChange = imageChange;
            updateOnFilterChange = filterChange;
        }
        
        /// <summary>
        /// Force update the color picker with current overlay image
        /// </summary>
        public void RefreshColorPicker()
        {
            if (imageOverlayManager != null && imageOverlayManager.CurrentImage != null)
            {
                UpdateColorPickerTexture(imageOverlayManager.CurrentImage);
            }
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (imageOverlayManager != null)
            {
                imageOverlayManager.OnImageLoaded -= HandleImageLoaded;
            }
            
            if (colorPicker != null)
            {
                colorPicker.OnColorPicked -= HandleColorPicked;
                colorPicker.OnColorChanged -= HandleColorChanged;
            }
        }
        
        [System.Serializable]
        private class SerializableColor
        {
            public float r, g, b, a;
            
            public SerializableColor(Color color)
            {
                r = color.r;
                g = color.g;
                b = color.b;
                a = color.a;
            }
            
            public Color ToColor()
            {
                return new Color(r, g, b, a);
            }
        }
    }
}