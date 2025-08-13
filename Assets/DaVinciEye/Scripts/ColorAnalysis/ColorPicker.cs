using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DaVinciEye.ColorAnalysis
{
    /// <summary>
    /// Color picker component that allows selecting colors from reference images
    /// Implements simplified approach using Unity's built-in UI components
    /// </summary>
    public class ColorPicker : MonoBehaviour, IPointerClickHandler
    {
        [Header("Color Picker Configuration")]
        [SerializeField] private RawImage targetImage;
        [SerializeField] private Image colorPreview;
        [SerializeField] private Text colorValueText;
        [SerializeField] private GameObject crosshair;
        
        [Header("HSV Controls")]
        [SerializeField] private Slider hueSlider;
        [SerializeField] private Slider saturationSlider;
        [SerializeField] private Slider valueSlider;
        
        [Header("RGB Display")]
        [SerializeField] private Text rgbText;
        [SerializeField] private Text hexText;
        
        // Properties
        public Color CurrentColor { get; private set; } = Color.white;
        public Texture2D CurrentTexture { get; private set; }
        public bool IsActive { get; set; } = true;
        
        // Events
        public event Action<Color> OnColorChanged;
        public event Action<Color> OnColorPicked;
        
        // Private fields
        private Camera uiCamera;
        private RectTransform imageRectTransform;
        private bool isDragging = false;
        
        private void Awake()
        {
            InitializeComponents();
            SetupEventListeners();
        }
        
        private void Start()
        {
            uiCamera = Camera.main;
            if (targetImage != null)
            {
                imageRectTransform = targetImage.GetComponent<RectTransform>();
            }
            
            UpdateColorDisplay();
        }
        
        private void InitializeComponents()
        {
            // Auto-find components if not assigned
            if (targetImage == null)
                targetImage = GetComponentInChildren<RawImage>();
            
            if (colorPreview == null)
                colorPreview = transform.Find("ColorPreview")?.GetComponent<Image>();
            
            if (colorValueText == null)
                colorValueText = transform.Find("ColorValue")?.GetComponent<Text>();
            
            if (crosshair == null)
                crosshair = transform.Find("Crosshair")?.gameObject;
        }
        
        private void SetupEventListeners()
        {
            if (hueSlider != null)
            {
                hueSlider.onValueChanged.AddListener(OnHueChanged);
            }
            
            if (saturationSlider != null)
            {
                saturationSlider.onValueChanged.AddListener(OnSaturationChanged);
            }
            
            if (valueSlider != null)
            {
                valueSlider.onValueChanged.AddListener(OnValueChanged);
            }
        }
        
        /// <summary>
        /// Set the texture for color picking
        /// </summary>
        public void SetTexture(Texture2D texture)
        {
            if (texture == null) return;
            
            CurrentTexture = texture;
            
            if (targetImage != null)
            {
                targetImage.texture = texture;
            }
            
            Debug.Log($"ColorPicker: Texture set - {texture.width}x{texture.height}");
        }
        
        /// <summary>
        /// Pick color from image at specified coordinate (0-1 normalized)
        /// </summary>
        public Color PickColorFromImage(Vector2 normalizedCoordinate)
        {
            if (CurrentTexture == null) return Color.white;
            
            // Convert normalized coordinates to pixel coordinates
            int x = Mathf.RoundToInt(normalizedCoordinate.x * CurrentTexture.width);
            int y = Mathf.RoundToInt(normalizedCoordinate.y * CurrentTexture.height);
            
            // Clamp to texture bounds
            x = Mathf.Clamp(x, 0, CurrentTexture.width - 1);
            y = Mathf.Clamp(y, 0, CurrentTexture.height - 1);
            
            // Sample color from texture
            Color pickedColor = CurrentTexture.GetPixel(x, y);
            SetCurrentColor(pickedColor);
            
            OnColorPicked?.Invoke(pickedColor);
            
            return pickedColor;
        }
        
        /// <summary>
        /// Set the current color and update UI
        /// </summary>
        public void SetCurrentColor(Color color)
        {
            CurrentColor = color;
            UpdateColorDisplay();
            UpdateHSVSliders();
            OnColorChanged?.Invoke(color);
        }
        
        /// <summary>
        /// Handle pointer click on the image
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsActive || targetImage == null || CurrentTexture == null) return;
            
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                imageRectTransform, eventData.position, uiCamera, out localPoint))
            {
                // Convert local point to normalized coordinates
                Rect rect = imageRectTransform.rect;
                Vector2 normalizedPoint = new Vector2(
                    (localPoint.x - rect.x) / rect.width,
                    (localPoint.y - rect.y) / rect.height
                );
                
                // Clamp to 0-1 range
                normalizedPoint.x = Mathf.Clamp01(normalizedPoint.x);
                normalizedPoint.y = Mathf.Clamp01(normalizedPoint.y);
                
                // Pick color at this position
                PickColorFromImage(normalizedPoint);
                
                // Update crosshair position
                UpdateCrosshairPosition(localPoint);
            }
        }
        
        private void UpdateCrosshairPosition(Vector2 localPosition)
        {
            if (crosshair != null && imageRectTransform != null)
            {
                RectTransform crosshairRect = crosshair.GetComponent<RectTransform>();
                if (crosshairRect != null)
                {
                    crosshairRect.anchoredPosition = localPosition;
                    crosshair.SetActive(true);
                }
            }
        }
        
        private void UpdateColorDisplay()
        {
            if (colorPreview != null)
            {
                colorPreview.color = CurrentColor;
            }
            
            if (colorValueText != null)
            {
                colorValueText.text = $"RGB({CurrentColor.r:F2}, {CurrentColor.g:F2}, {CurrentColor.b:F2})";
            }
            
            if (rgbText != null)
            {
                rgbText.text = $"R:{(int)(CurrentColor.r * 255)} G:{(int)(CurrentColor.g * 255)} B:{(int)(CurrentColor.b * 255)}";
            }
            
            if (hexText != null)
            {
                hexText.text = $"#{ColorUtility.ToHtmlStringRGB(CurrentColor)}";
            }
        }
        
        private void UpdateHSVSliders()
        {
            Color.RGBToHSV(CurrentColor, out float h, out float s, out float v);
            
            if (hueSlider != null)
            {
                hueSlider.SetValueWithoutNotify(h);
            }
            
            if (saturationSlider != null)
            {
                saturationSlider.SetValueWithoutNotify(s);
            }
            
            if (valueSlider != null)
            {
                valueSlider.SetValueWithoutNotify(v);
            }
        }
        
        private void OnHueChanged(float value)
        {
            Color.RGBToHSV(CurrentColor, out float h, out float s, out float v);
            Color newColor = Color.HSVToRGB(value, s, v);
            SetCurrentColor(newColor);
        }
        
        private void OnSaturationChanged(float value)
        {
            Color.RGBToHSV(CurrentColor, out float h, out float s, out float v);
            Color newColor = Color.HSVToRGB(h, value, v);
            SetCurrentColor(newColor);
        }
        
        private void OnValueChanged(float value)
        {
            Color.RGBToHSV(CurrentColor, out float h, out float s, out float v);
            Color newColor = Color.HSVToRGB(h, s, value);
            SetCurrentColor(newColor);
        }
        
        /// <summary>
        /// Save current color to JSON format
        /// </summary>
        public string SaveColorToJson()
        {
            var colorData = new
            {
                r = CurrentColor.r,
                g = CurrentColor.g,
                b = CurrentColor.b,
                a = CurrentColor.a,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            return JsonUtility.ToJson(colorData);
        }
        
        /// <summary>
        /// Load color from JSON format
        /// </summary>
        public void LoadColorFromJson(string json)
        {
            try
            {
                var colorData = JsonUtility.FromJson<ColorData>(json);
                Color loadedColor = new Color(colorData.r, colorData.g, colorData.b, colorData.a);
                SetCurrentColor(loadedColor);
            }
            catch (Exception e)
            {
                Debug.LogError($"ColorPicker: Failed to load color from JSON - {e.Message}");
            }
        }
        
        [System.Serializable]
        private class ColorData
        {
            public float r, g, b, a;
            public string timestamp;
        }
        
        private void OnDestroy()
        {
            // Clean up event listeners
            if (hueSlider != null)
                hueSlider.onValueChanged.RemoveListener(OnHueChanged);
            
            if (saturationSlider != null)
                saturationSlider.onValueChanged.RemoveListener(OnSaturationChanged);
            
            if (valueSlider != null)
                valueSlider.onValueChanged.RemoveListener(OnValueChanged);
        }
    }
}