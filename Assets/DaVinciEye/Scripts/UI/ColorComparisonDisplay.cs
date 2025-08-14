using System;
using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;
using DaVinciEye.ColorAnalysis;

namespace DaVinciEye.UI
{
    /// <summary>
    /// Specialized display component for color comparison with swatches and difference indicators
    /// Provides detailed color matching analysis and visual feedback
    /// </summary>
    public class ColorComparisonDisplay : MonoBehaviour
    {
        [Header("Color Swatches")]
        [SerializeField] private Image referenceColorSwatch;
        [SerializeField] private Image capturedColorSwatch;
        [SerializeField] private Image differenceIndicatorSwatch;
        [SerializeField] private float swatchSize = 80f;
        
        [Header("Color Information")]
        [SerializeField] private Text referenceColorHex;
        [SerializeField] private Text referenceColorRGB;
        [SerializeField] private Text referenceColorHSV;
        [SerializeField] private Text capturedColorHex;
        [SerializeField] private Text capturedColorRGB;
        [SerializeField] private Text capturedColorHSV;
        
        [Header("Difference Analysis")]
        [SerializeField] private Slider matchQualitySlider;
        [SerializeField] private Text matchQualityText;
        [SerializeField] private Text colorDifferenceText;
        [SerializeField] private Text hueDifferenceText;
        [SerializeField] private Text saturationDifferenceText;
        [SerializeField] private Text brightnessDifferenceText;
        
        [Header("Visual Indicators")]
        [SerializeField] private Image matchQualityIndicator;
        [SerializeField] private ParticleSystem matchSuccessEffect;
        [SerializeField] private GameObject differenceArrows;
        [SerializeField] private Text matchingAdviceText;
        
        [Header("Color Quality Thresholds")]
        [SerializeField] private float excellentMatchThreshold = 0.9f;
        [SerializeField] private float goodMatchThreshold = 0.7f;
        [SerializeField] private float fairMatchThreshold = 0.5f;
        
        [Header("Visual Feedback Colors")]
        [SerializeField] private Color excellentMatchColor = Color.green;
        [SerializeField] private Color goodMatchColor = Color.yellow;
        [SerializeField] private Color fairMatchColor = Color.orange;
        [SerializeField] private Color poorMatchColor = Color.red;
        
        // Current comparison data
        private Color currentReferenceColor;
        private Color currentCapturedColor;
        private ColorMatchResult currentMatchResult;
        
        // Events
        public event Action<ColorMatchResult> OnComparisonUpdated;
        public event Action<Color, Color> OnColorsChanged;
        
        private void Start()
        {
            SetupDisplay();
        }
        
        private void SetupDisplay()
        {
            // Setup color swatches
            SetupColorSwatches();
            
            // Setup match quality slider
            if (matchQualitySlider != null)
            {
                matchQualitySlider.minValue = 0f;
                matchQualitySlider.maxValue = 1f;
                matchQualitySlider.value = 0f;
                matchQualitySlider.interactable = false; // Read-only display
            }
            
            // Initialize with default colors
            SetColors(Color.white, Color.white);
        }
        
        private void SetupColorSwatches()
        {
            // Configure reference color swatch
            if (referenceColorSwatch != null)
            {
                var rectTransform = referenceColorSwatch.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = Vector2.one * swatchSize;
                }
            }
            
            // Configure captured color swatch
            if (capturedColorSwatch != null)
            {
                var rectTransform = capturedColorSwatch.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = Vector2.one * swatchSize;
                }
            }
            
            // Configure difference indicator swatch
            if (differenceIndicatorSwatch != null)
            {
                var rectTransform = differenceIndicatorSwatch.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = Vector2.one * (swatchSize * 0.6f);
                }
            }
        }
        
        public void SetColors(Color referenceColor, Color capturedColor)
        {
            currentReferenceColor = referenceColor;
            currentCapturedColor = capturedColor;
            
            // Update color swatches
            UpdateColorSwatches();
            
            // Update color information displays
            UpdateColorInformation();
            
            // Calculate and display color differences
            CalculateAndDisplayDifferences();
            
            OnColorsChanged?.Invoke(referenceColor, capturedColor);
        }
        
        private void UpdateColorSwatches()
        {
            if (referenceColorSwatch != null)
            {
                referenceColorSwatch.color = currentReferenceColor;
            }
            
            if (capturedColorSwatch != null)
            {
                capturedColorSwatch.color = currentCapturedColor;
            }
            
            // Update difference indicator with blended color
            if (differenceIndicatorSwatch != null)
            {
                Color blendedColor = Color.Lerp(currentReferenceColor, currentCapturedColor, 0.5f);
                differenceIndicatorSwatch.color = blendedColor;
            }
        }
        
        private void UpdateColorInformation()
        {
            // Update reference color information
            if (referenceColorHex != null)
                referenceColorHex.text = ColorToHex(currentReferenceColor);
            if (referenceColorRGB != null)
                referenceColorRGB.text = ColorToRGB(currentReferenceColor);
            if (referenceColorHSV != null)
                referenceColorHSV.text = ColorToHSV(currentReferenceColor);
            
            // Update captured color information
            if (capturedColorHex != null)
                capturedColorHex.text = ColorToHex(currentCapturedColor);
            if (capturedColorRGB != null)
                capturedColorRGB.text = ColorToRGB(currentCapturedColor);
            if (capturedColorHSV != null)
                capturedColorHSV.text = ColorToHSV(currentCapturedColor);
        }
        
        private void CalculateAndDisplayDifferences()
        {
            // Calculate color match result
            currentMatchResult = CalculateColorMatch(currentReferenceColor, currentCapturedColor);
            
            // Update difference displays
            UpdateDifferenceDisplays();
            
            // Update visual indicators
            UpdateVisualIndicators();
            
            // Update matching advice
            UpdateMatchingAdvice();
            
            OnComparisonUpdated?.Invoke(currentMatchResult);
        }
        
        private ColorMatchResult CalculateColorMatch(Color reference, Color captured)
        {
            // Convert colors to HSV for better comparison
            Color.RGBToHSV(reference, out float refH, out float refS, out float refV);
            Color.RGBToHSV(captured, out float capH, out float capS, out float capV);
            
            // Calculate differences
            float hueDiff = Mathf.Abs(refH - capH) * 360f;
            if (hueDiff > 180f) hueDiff = 360f - hueDiff; // Handle hue wraparound
            
            float satDiff = Mathf.Abs(refS - capS);
            float valDiff = Mathf.Abs(refV - capV);
            
            // Calculate overall color difference (Delta E approximation)
            float colorDifference = Mathf.Sqrt(
                Mathf.Pow((reference.r - captured.r) * 255f, 2) +
                Mathf.Pow((reference.g - captured.g) * 255f, 2) +
                Mathf.Pow((reference.b - captured.b) * 255f, 2)
            );
            
            // Calculate match quality (0-1, where 1 is perfect match)
            float maxDifference = 441.67f; // Maximum possible RGB difference
            float matchQuality = 1f - (colorDifference / maxDifference);
            matchQuality = Mathf.Clamp01(matchQuality);
            
            return new ColorMatchResult
            {
                referenceColor = reference,
                capturedColor = captured,
                matchQuality = matchQuality,
                colorDifference = colorDifference,
                hueDifference = hueDiff,
                saturationDifference = satDiff,
                brightnessDifference = valDiff,
                timestamp = System.DateTime.Now
            };
        }
        
        private void UpdateDifferenceDisplays()
        {
            // Update match quality slider and text
            if (matchQualitySlider != null)
            {
                matchQualitySlider.value = currentMatchResult.matchQuality;
            }
            
            if (matchQualityText != null)
            {
                matchQualityText.text = $"Match: {currentMatchResult.matchQuality:P1}";
            }
            
            // Update difference texts
            if (colorDifferenceText != null)
            {
                colorDifferenceText.text = $"ΔE: {currentMatchResult.colorDifference:F1}";
            }
            
            if (hueDifferenceText != null)
            {
                hueDifferenceText.text = $"Hue: {currentMatchResult.hueDifference:F1}°";
            }
            
            if (saturationDifferenceText != null)
            {
                saturationDifferenceText.text = $"Sat: {currentMatchResult.saturationDifference:P1}";
            }
            
            if (brightnessDifferenceText != null)
            {
                brightnessDifferenceText.text = $"Bright: {currentMatchResult.brightnessDifference:P1}";
            }
        }
        
        private void UpdateVisualIndicators()
        {
            // Update match quality indicator color
            if (matchQualityIndicator != null)
            {
                Color indicatorColor = GetQualityColor(currentMatchResult.matchQuality);
                matchQualityIndicator.color = indicatorColor;
            }
            
            // Show success effect for excellent matches
            if (currentMatchResult.matchQuality >= excellentMatchThreshold && matchSuccessEffect != null)
            {
                matchSuccessEffect.Play();
            }
            
            // Show/hide difference arrows based on match quality
            if (differenceArrows != null)
            {
                differenceArrows.SetActive(currentMatchResult.matchQuality < goodMatchThreshold);
            }
        }
        
        private void UpdateMatchingAdvice()
        {
            if (matchingAdviceText == null) return;
            
            string advice = GetMatchingAdvice(currentMatchResult);
            matchingAdviceText.text = advice;
        }
        
        private string GetMatchingAdvice(ColorMatchResult result)
        {
            if (result.matchQuality >= excellentMatchThreshold)
            {
                return "Excellent match! Colors are nearly identical.";
            }
            else if (result.matchQuality >= goodMatchThreshold)
            {
                return "Good match. Minor adjustments may improve accuracy.";
            }
            else if (result.matchQuality >= fairMatchThreshold)
            {
                return GetSpecificAdvice(result);
            }
            else
            {
                return "Poor match. Consider starting with a different base color.";
            }
        }
        
        private string GetSpecificAdvice(ColorMatchResult result)
        {
            string advice = "Fair match. ";
            
            // Prioritize the largest difference
            if (result.hueDifference > 20f)
            {
                advice += "Try adjusting the hue (color tone).";
            }
            else if (result.saturationDifference > 0.2f)
            {
                advice += "Try adjusting the saturation (color intensity).";
            }
            else if (result.brightnessDifference > 0.2f)
            {
                advice += "Try adjusting the brightness (lightness).";
            }
            else
            {
                advice += "Small adjustments to all color properties may help.";
            }
            
            return advice;
        }
        
        private Color GetQualityColor(float matchQuality)
        {
            if (matchQuality >= excellentMatchThreshold)
                return excellentMatchColor;
            else if (matchQuality >= goodMatchThreshold)
                return goodMatchColor;
            else if (matchQuality >= fairMatchThreshold)
                return fairMatchColor;
            else
                return poorMatchColor;
        }
        
        // Color conversion utility methods
        private string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }
        
        private string ColorToRGB(Color color)
        {
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);
            return $"RGB({r}, {g}, {b})";
        }
        
        private string ColorToHSV(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            int hue = Mathf.RoundToInt(h * 360);
            int sat = Mathf.RoundToInt(s * 100);
            int val = Mathf.RoundToInt(v * 100);
            return $"HSV({hue}°, {sat}%, {val}%)";
        }
        
        // Public methods
        public void SetReferenceColor(Color color)
        {
            SetColors(color, currentCapturedColor);
        }
        
        public void SetCapturedColor(Color color)
        {
            SetColors(currentReferenceColor, color);
        }
        
        public void UpdateComparison(ColorMatchResult matchResult)
        {
            currentMatchResult = matchResult;
            currentReferenceColor = matchResult.referenceColor;
            currentCapturedColor = matchResult.capturedColor;
            
            UpdateColorSwatches();
            UpdateColorInformation();
            UpdateDifferenceDisplays();
            UpdateVisualIndicators();
            UpdateMatchingAdvice();
            
            OnComparisonUpdated?.Invoke(matchResult);
        }
        
        public void SetSwatchSize(float size)
        {
            swatchSize = size;
            SetupColorSwatches();
        }
        
        public void SetQualityThresholds(float excellent, float good, float fair)
        {
            excellentMatchThreshold = excellent;
            goodMatchThreshold = good;
            fairMatchThreshold = fair;
            
            // Refresh display with new thresholds
            if (currentMatchResult != null)
            {
                UpdateVisualIndicators();
                UpdateMatchingAdvice();
            }
        }
        
        // Properties
        public Color ReferenceColor => currentReferenceColor;
        public Color CapturedColor => currentCapturedColor;
        public ColorMatchResult CurrentMatchResult => currentMatchResult;
        public float MatchQuality => currentMatchResult?.matchQuality ?? 0f;
        public float ColorDifference => currentMatchResult?.colorDifference ?? 0f;
        public float SwatchSize => swatchSize;
    }
}