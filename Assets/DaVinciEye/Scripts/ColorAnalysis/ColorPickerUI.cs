using UnityEngine;
using UnityEngine.UI;

namespace DaVinciEye.ColorAnalysis
{
    /// <summary>
    /// Helper component for setting up Color Picker UI elements
    /// Creates a complete color picker interface using Unity's built-in UI components
    /// </summary>
    public class ColorPickerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ColorPicker colorPicker;
        [SerializeField] private Canvas parentCanvas;
        
        [Header("Auto-Setup Configuration")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private Vector2 panelSize = new Vector2(400, 600);
        [SerializeField] private Vector3 worldPosition = new Vector3(0, 1.5f, 2);
        
        private GameObject colorPickerPanel;
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupColorPickerUI();
            }
        }
        
        /// <summary>
        /// Create a complete color picker UI setup
        /// </summary>
        [ContextMenu("Setup Color Picker UI")]
        public void SetupColorPickerUI()
        {
            if (parentCanvas == null)
            {
                parentCanvas = FindObjectOfType<Canvas>();
                if (parentCanvas == null)
                {
                    CreateWorldSpaceCanvas();
                }
            }
            
            CreateColorPickerPanel();
            SetupImageArea();
            SetupColorPreview();
            SetupHSVControls();
            SetupColorInfo();
            SetupCrosshair();
            
            Debug.Log("ColorPickerUI: Complete UI setup created");
        }
        
        private void CreateWorldSpaceCanvas()
        {
            GameObject canvasGO = new GameObject("ColorPickerCanvas");
            parentCanvas = canvasGO.AddComponent<Canvas>();
            parentCanvas.renderMode = RenderMode.WorldSpace;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            GraphicRaycaster raycaster = canvasGO.AddComponent<GraphicRaycaster>();
            
            // Position canvas in world space
            canvasGO.transform.position = worldPosition;
            canvasGO.transform.localScale = Vector3.one * 0.001f; // Scale down for world space
            
            Debug.Log("ColorPickerUI: Created world space canvas");
        }
        
        private void CreateColorPickerPanel()
        {
            colorPickerPanel = new GameObject("ColorPickerPanel");
            colorPickerPanel.transform.SetParent(parentCanvas.transform, false);
            
            RectTransform panelRect = colorPickerPanel.AddComponent<RectTransform>();
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = Vector2.zero;
            
            Image panelImage = colorPickerPanel.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            // Add ColorPicker component
            if (colorPicker == null)
            {
                colorPicker = colorPickerPanel.AddComponent<ColorPicker>();
            }
        }
        
        private void SetupImageArea()
        {
            GameObject imageArea = new GameObject("ImageArea");
            imageArea.transform.SetParent(colorPickerPanel.transform, false);
            
            RectTransform imageRect = imageArea.AddComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.05f, 0.4f);
            imageRect.anchorMax = new Vector2(0.95f, 0.95f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            
            RawImage rawImage = imageArea.AddComponent<RawImage>();
            rawImage.color = Color.white;
            
            // Assign to color picker
            var colorPickerField = typeof(ColorPicker).GetField("targetImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            colorPickerField?.SetValue(colorPicker, rawImage);
        }
        
        private void SetupColorPreview()
        {
            GameObject previewArea = new GameObject("ColorPreview");
            previewArea.transform.SetParent(colorPickerPanel.transform, false);
            
            RectTransform previewRect = previewArea.AddComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.05f, 0.25f);
            previewRect.anchorMax = new Vector2(0.3f, 0.35f);
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;
            
            Image previewImage = previewArea.AddComponent<Image>();
            previewImage.color = Color.white;
            
            // Assign to color picker
            var colorPickerField = typeof(ColorPicker).GetField("colorPreview", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            colorPickerField?.SetValue(colorPicker, previewImage);
        }
        
        private void SetupHSVControls()
        {
            SetupSlider("HueSlider", new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.2f), "Hue", 0f, 1f);
            SetupSlider("SaturationSlider", new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.15f), "Saturation", 0f, 1f);
            SetupSlider("ValueSlider", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.1f), "Value", 0f, 1f);
        }
        
        private void SetupSlider(string name, Vector2 anchorMin, Vector2 anchorMax, string labelText, float minValue, float maxValue)
        {
            GameObject sliderGO = new GameObject(name);
            sliderGO.transform.SetParent(colorPickerPanel.transform, false);
            
            RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.anchorMin = anchorMin;
            sliderRect.anchorMax = anchorMax;
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;
            
            Slider slider = sliderGO.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = name == "ValueSlider" ? 1f : 0f;
            
            // Create background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderGO.transform, false);
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            
            // Create handle area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGO.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;
            
            // Create handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            
            // Assign slider components
            slider.targetGraphic = handleImage;
            slider.handleRect = handleRect;
            
            // Assign to color picker based on name
            var fieldName = name.Replace("Slider", "").ToLower() + "Slider";
            var colorPickerField = typeof(ColorPicker).GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            colorPickerField?.SetValue(colorPicker, slider);
        }
        
        private void SetupColorInfo()
        {
            // RGB Text
            GameObject rgbText = CreateText("RGBText", new Vector2(0.35f, 0.25f), new Vector2(0.95f, 0.3f), "RGB: 255, 255, 255");
            var rgbField = typeof(ColorPicker).GetField("rgbText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rgbField?.SetValue(colorPicker, rgbText.GetComponent<Text>());
            
            // Hex Text
            GameObject hexText = CreateText("HexText", new Vector2(0.35f, 0.3f), new Vector2(0.95f, 0.35f), "HEX: #FFFFFF");
            var hexField = typeof(ColorPicker).GetField("hexText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            hexField?.SetValue(colorPicker, hexText.GetComponent<Text>());
        }
        
        private GameObject CreateText(string name, Vector2 anchorMin, Vector2 anchorMax, string text)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(colorPickerPanel.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = anchorMin;
            textRect.anchorMax = anchorMax;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleLeft;
            
            return textGO;
        }
        
        private void SetupCrosshair()
        {
            GameObject crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(colorPickerPanel.transform, false);
            
            RectTransform crosshairRect = crosshair.AddComponent<RectTransform>();
            crosshairRect.sizeDelta = new Vector2(20, 20);
            
            Image crosshairImage = crosshair.AddComponent<Image>();
            crosshairImage.color = Color.red;
            
            // Create crosshair shape (simple cross)
            crosshairImage.sprite = CreateCrosshairSprite();
            
            crosshair.SetActive(false);
            
            // Assign to color picker
            var crosshairField = typeof(ColorPicker).GetField("crosshair", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            crosshairField?.SetValue(colorPicker, crosshair);
        }
        
        private Sprite CreateCrosshairSprite()
        {
            // Create a simple crosshair texture
            Texture2D texture = new Texture2D(20, 20);
            Color[] pixels = new Color[400];
            
            for (int i = 0; i < 400; i++)
            {
                pixels[i] = Color.clear;
            }
            
            // Draw horizontal line
            for (int x = 0; x < 20; x++)
            {
                pixels[10 * 20 + x] = Color.red;
            }
            
            // Draw vertical line
            for (int y = 0; y < 20; y++)
            {
                pixels[y * 20 + 10] = Color.red;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 20, 20), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// Get the color picker component
        /// </summary>
        public ColorPicker GetColorPicker()
        {
            return colorPicker;
        }
        
        /// <summary>
        /// Set the image texture for color picking
        /// </summary>
        public void SetImageTexture(Texture2D texture)
        {
            if (colorPicker != null)
            {
                colorPicker.SetTexture(texture);
            }
        }
    }
}