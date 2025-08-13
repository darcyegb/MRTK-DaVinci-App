using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DaVinciEye.ColorAnalysis
{
    /// <summary>
    /// Side-by-side color comparison UI display
    /// Shows reference and captured colors with matching analysis
    /// </summary>
    public class ColorComparisonUI : MonoBehaviour
    {
        [Header("Color Display")]
        [SerializeField] private Image referenceColorSwatch;
        [SerializeField] private Image capturedColorSwatch;
        [SerializeField] private Image differenceIndicator;
        
        [Header("Color Information")]
        [SerializeField] private TextMeshProUGUI referenceColorText;
        [SerializeField] private TextMeshProUGUI capturedColorText;
        [SerializeField] private TextMeshProUGUI matchQualityText;
        [SerializeField] private TextMeshProUGUI matchAccuracyText;
        [SerializeField] private TextMeshProUGUI deltaEText;
        
        [Header("Difference Display")]
        [SerializeField] private Slider rgbDifferenceSlider;
        [SerializeField] private Slider hsvDifferenceSlider;
        [SerializeField] private TextMeshProUGUI rgbDifferenceText;
        [SerializeField] private TextMeshProUGUI hsvDifferenceText;
        
        [Header("Suggestions")]
        [SerializeField] private Transform suggestionsContainer;
        [SerializeField] private GameObject suggestionItemPrefab;
        [SerializeField] private ScrollRect suggestionsScrollRect;
        
        [Header("Animation")]
        [SerializeField] private bool animateUpdates = true;
        [SerializeField] private float animationDuration = 0.3f;
        
        // Properties
        public ColorMatchResult CurrentResult { get; private set; }
        public bool IsVisible { get; private set; }
        
        // Events
        public System.Action OnComparisonUpdated;
        public System.Action OnUIHidden;
        public System.Action OnUIShown;
        
        // Private fields
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        
        private void Awake()
        {
            InitializeComponents();
            SetupUI();
        }
        
        private void InitializeComponents()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            rectTransform = GetComponent<RectTransform>();
            
            // Auto-find components if not assigned
            if (referenceColorSwatch == null)
                referenceColorSwatch = transform.Find("ReferenceColor")?.GetComponent<Image>();
            
            if (capturedColorSwatch == null)
                capturedColorSwatch = transform.Find("CapturedColor")?.GetComponent<Image>();
        }
        
        private void SetupUI()
        {
            // Initially hide the UI
            SetVisible(false);
            
            // Setup default colors
            if (referenceColorSwatch != null)
                referenceColorSwatch.color = Color.white;
            
            if (capturedColorSwatch != null)
                capturedColorSwatch.color = Color.white;
        }
        
        /// <summary>
        /// Update the comparison display with new match result
        /// </summary>
        public void UpdateComparison(ColorMatchResult result)
        {
            if (result == null) return;
            
            CurrentResult = result;
            
            // Update color swatches
            UpdateColorSwatches(result.referenceColor, result.capturedColor);
            
            // Update color information text
            UpdateColorInformation(result);
            
            // Update difference indicators
            UpdateDifferenceDisplay(result);
            
            // Update suggestions
            UpdateSuggestions(result.adjustmentSuggestions);
            
            // Show UI if hidden
            if (!IsVisible)
                SetVisible(true);
            
            OnComparisonUpdated?.Invoke();
            
            Debug.Log($"ColorComparisonUI: Updated with {result.matchQuality} match (Accuracy: {result.matchAccuracy:F2})");
        }
        
        private void UpdateColorSwatches(Color referenceColor, Color capturedColor)
        {
            if (animateUpdates)
            {
                // Animate color transitions
                if (referenceColorSwatch != null)
                    AnimateColorChange(referenceColorSwatch, referenceColor);
                
                if (capturedColorSwatch != null)
                    AnimateColorChange(capturedColorSwatch, capturedColor);
            }
            else
            {
                // Immediate color update
                if (referenceColorSwatch != null)
                    referenceColorSwatch.color = referenceColor;
                
                if (capturedColorSwatch != null)
                    capturedColorSwatch.color = capturedColor;
            }
            
            // Update difference indicator color
            if (differenceIndicator != null)
            {
                Color diffColor = Color.Lerp(Color.green, Color.red, 1f - CurrentResult.matchAccuracy);
                differenceIndicator.color = diffColor;
            }
        }
        
        private void AnimateColorChange(Image targetImage, Color targetColor)
        {
            if (targetImage == null) return;
            
            Color startColor = targetImage.color;
            
            LeanTween.value(gameObject, 0f, 1f, animationDuration)
                .setOnUpdate((float t) =>
                {
                    targetImage.color = Color.Lerp(startColor, targetColor, t);
                });
        }
        
        private void UpdateColorInformation(ColorMatchResult result)
        {
            // Reference color info
            if (referenceColorText != null)
            {
                referenceColorText.text = $"Reference\nRGB: {FormatRGB(result.referenceColor)}\nHEX: #{ColorUtility.ToHtmlStringRGB(result.referenceColor)}";
            }
            
            // Captured color info
            if (capturedColorText != null)
            {
                capturedColorText.text = $"Captured\nRGB: {FormatRGB(result.capturedColor)}\nHEX: #{ColorUtility.ToHtmlStringRGB(result.capturedColor)}";
            }
            
            // Match quality
            if (matchQualityText != null)
            {
                matchQualityText.text = $"Match Quality: {result.matchQuality}";
                matchQualityText.color = GetQualityColor(result.matchQuality);
            }
            
            // Match accuracy
            if (matchAccuracyText != null)
            {
                matchAccuracyText.text = $"Accuracy: {result.matchAccuracy:P1}";
            }
            
            // Delta E
            if (deltaEText != null)
            {
                deltaEText.text = $"ΔE: {result.deltaE:F2}";
            }
        }
        
        private string FormatRGB(Color color)
        {
            return $"({(int)(color.r * 255)}, {(int)(color.g * 255)}, {(int)(color.b * 255)})";
        }
        
        private Color GetQualityColor(string quality)
        {
            switch (quality)
            {
                case "Excellent": return Color.green;
                case "Good": return Color.yellow;
                case "Fair": return new Color(1f, 0.5f, 0f); // Orange
                case "Poor": return Color.red;
                default: return Color.white;
            }
        }
        
        private void UpdateDifferenceDisplay(ColorMatchResult result)
        {
            // RGB difference
            if (rgbDifferenceSlider != null)
            {
                float rgbDiff = result.rgbDifference.magnitude;
                rgbDifferenceSlider.value = rgbDiff;
            }
            
            if (rgbDifferenceText != null)
            {
                rgbDifferenceText.text = $"RGB Diff: {result.rgbDifference.magnitude:F3}";
            }
            
            // HSV difference
            if (hsvDifferenceSlider != null)
            {
                float hsvDiff = result.hsvDifference.magnitude;
                hsvDifferenceSlider.value = hsvDiff;
            }
            
            if (hsvDifferenceText != null)
            {
                hsvDifferenceText.text = $"HSV Diff: {result.hsvDifference.magnitude:F3}";
            }
        }
        
        private void UpdateSuggestions(string[] suggestions)
        {
            if (suggestionsContainer == null) return;
            
            // Clear existing suggestions
            foreach (Transform child in suggestionsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Add new suggestions
            foreach (string suggestion in suggestions)
            {
                CreateSuggestionItem(suggestion);
            }
            
            // Reset scroll position
            if (suggestionsScrollRect != null)
            {
                suggestionsScrollRect.verticalNormalizedPosition = 1f;
            }
        }
        
        private void CreateSuggestionItem(string suggestionText)
        {
            GameObject suggestionItem;
            
            if (suggestionItemPrefab != null)
            {
                suggestionItem = Instantiate(suggestionItemPrefab, suggestionsContainer);
            }
            else
            {
                // Create simple text item
                suggestionItem = new GameObject("SuggestionItem");
                suggestionItem.transform.SetParent(suggestionsContainer, false);
                
                TextMeshProUGUI textComponent = suggestionItem.AddComponent<TextMeshProUGUI>();
                textComponent.text = $"• {suggestionText}";
                textComponent.fontSize = 14;
                textComponent.color = Color.white;
                
                // Add layout element
                LayoutElement layoutElement = suggestionItem.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 25;
            }
            
            // Set suggestion text if using prefab
            TextMeshProUGUI textComp = suggestionItem.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = $"• {suggestionText}";
            }
        }
        
        /// <summary>
        /// Show or hide the comparison UI
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (IsVisible == visible) return;
            
            IsVisible = visible;
            
            if (animateUpdates)
            {
                AnimateVisibility(visible);
            }
            else
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
            
            if (visible)
                OnUIShown?.Invoke();
            else
                OnUIHidden?.Invoke();
        }
        
        private void AnimateVisibility(bool visible)
        {
            float targetAlpha = visible ? 1f : 0f;
            Vector3 targetScale = visible ? Vector3.one : Vector3.one * 0.8f;
            
            LeanTween.alphaCanvas(canvasGroup, targetAlpha, animationDuration)
                .setEase(LeanTweenType.easeOutQuart);
            
            LeanTween.scale(rectTransform, targetScale, animationDuration)
                .setEase(LeanTweenType.easeOutBack);
            
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
        
        /// <summary>
        /// Clear the comparison display
        /// </summary>
        public void ClearComparison()
        {
            CurrentResult = null;
            
            if (referenceColorSwatch != null)
                referenceColorSwatch.color = Color.white;
            
            if (capturedColorSwatch != null)
                capturedColorSwatch.color = Color.white;
            
            if (referenceColorText != null)
                referenceColorText.text = "Reference\n-\n-";
            
            if (capturedColorText != null)
                capturedColorText.text = "Captured\n-\n-";
            
            if (matchQualityText != null)
                matchQualityText.text = "Match Quality: -";
            
            if (matchAccuracyText != null)
                matchAccuracyText.text = "Accuracy: -";
            
            if (deltaEText != null)
                deltaEText.text = "ΔE: -";
            
            UpdateSuggestions(new string[0]);
            
            SetVisible(false);
        }
        
        /// <summary>
        /// Create a complete comparison UI setup
        /// </summary>
        [ContextMenu("Setup Comparison UI")]
        public void SetupComparisonUI()
        {
            // This would create the full UI hierarchy
            // Implementation depends on specific UI framework being used
            Debug.Log("ColorComparisonUI: Setup method called - implement based on UI framework");
        }
        
        /// <summary>
        /// Export current comparison as image
        /// </summary>
        public Texture2D ExportComparisonImage()
        {
            if (CurrentResult == null) return null;
            
            // Create a simple comparison texture
            Texture2D comparisonTexture = new Texture2D(200, 100);
            
            // Fill left half with reference color
            for (int x = 0; x < 100; x++)
            {
                for (int y = 0; y < 100; y++)
                {
                    comparisonTexture.SetPixel(x, y, CurrentResult.referenceColor);
                }
            }
            
            // Fill right half with captured color
            for (int x = 100; x < 200; x++)
            {
                for (int y = 0; y < 100; y++)
                {
                    comparisonTexture.SetPixel(x, y, CurrentResult.capturedColor);
                }
            }
            
            comparisonTexture.Apply();
            return comparisonTexture;
        }
    }
}