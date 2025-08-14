using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;
using DaVinciEye.ColorAnalysis;

namespace DaVinciEye.UI
{
    /// <summary>
    /// UI controller for color analysis components including color picker and comparison display
    /// Implements requirements 7.1, 7.2, 7.4, 7.5 for color analysis UI
    /// </summary>
    public class ColorAnalysisUI : MonoBehaviour
    {
        [Header("Color Picker UI")]
        [SerializeField] private GameObject colorPickerCrosshair;
        [SerializeField] private RawImage colorPickerPreview;
        [SerializeField] private Text colorPickerCoordinatesText;
        [SerializeField] private Button confirmColorSelectionButton;
        [SerializeField] private Button cancelColorSelectionButton;
        
        [Header("Color Comparison Display")]
        [SerializeField] private Image referenceColorSwatch;
        [SerializeField] private Image capturedColorSwatch;
        [SerializeField] private Text referenceColorValueText;
        [SerializeField] private Text capturedColorValueText;
        [SerializeField] private Text colorDifferenceText;
        [SerializeField] private Slider colorMatchQualitySlider;
        
        [Header("Color Selection Feedback")]
        [SerializeField] private GameObject selectionFeedbackRing;
        [SerializeField] private ParticleSystem selectionParticleEffect;
        [SerializeField] private AudioSource selectionAudioFeedback;
        [SerializeField] private float feedbackDuration = 0.5f;
        
        [Header("Color History Display")]
        [SerializeField] private Transform colorHistoryContainer;
        [SerializeField] private GameObject colorHistoryItemPrefab;
        [SerializeField] private ScrollRect colorHistoryScrollRect;
        [SerializeField] private Text colorHistoryCountText;
        
        [Header("Color Matching Guidance")]
        [SerializeField] private Text matchingGuidanceText;
        [SerializeField] private Image matchingQualityIndicator;
        [SerializeField] private Color excellentMatchColor = Color.green;
        [SerializeField] private Color goodMatchColor = Color.yellow;
        [SerializeField] private Color poorMatchColor = Color.red;
        
        [Header("System Integration")]
        [SerializeField] private bool autoConnectToSystems = true;
        [SerializeField] private bool showDebugInfo = false;
        
        // System references
        private IColorAnalyzer colorAnalyzer;
        private UIManager uiManager;
        
        // Color analysis state
        private Color currentReferenceColor;
        private Color currentCapturedColor;
        private Vector2 currentPickerPosition;
        private bool isColorPickingActive = false;
        private List<ColorMatchData> colorHistory;
        
        // Events
        public event Action<Color> OnReferenceColorSelected;
        public event Action<Color> OnPaintColorCaptured;
        public event Action<ColorMatchResult> OnColorComparisonComplete;
        public event Action<Vector2> OnColorPickerPositionChanged;
        
        private void Awake()
        {
            InitializeColorAnalysisState();
        }
        
        private void Start()
        {
            SetupColorPickerUI();
            SetupColorComparisonUI();
            SetupColorHistoryUI();
            ConnectToSystems();
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            CleanupEventHandlers();
        }
        
        private void InitializeColorAnalysisState()
        {
            currentReferenceColor = Color.white;
            currentCapturedColor = Color.white;
            currentPickerPosition = Vector2.zero;
            colorHistory = new List<ColorMatchData>();
        }
        
        private void SetupColorPickerUI()
        {
            // Setup color picker crosshair
            if (colorPickerCrosshair != null)
            {
                colorPickerCrosshair.SetActive(false);
            }
            
            // Setup color picker preview
            if (colorPickerPreview != null)
            {
                colorPickerPreview.color = Color.white;
            }
            
            // Setup picker control buttons
            if (confirmColorSelectionButton != null)
            {
                confirmColorSelectionButton.onClick.AddListener(ConfirmColorSelection);
            }
            
            if (cancelColorSelectionButton != null)
            {
                cancelColorSelectionButton.onClick.AddListener(CancelColorSelection);
            }
            
            // Setup selection feedback
            if (selectionFeedbackRing != null)
            {
                selectionFeedbackRing.SetActive(false);
            }
            
            Debug.Log("ColorAnalysisUI: Color picker UI configured");
        }
        
        private void SetupColorComparisonUI()
        {
            // Setup color swatches
            if (referenceColorSwatch != null)
            {
                referenceColorSwatch.color = currentReferenceColor;
            }
            
            if (capturedColorSwatch != null)
            {
                capturedColorSwatch.color = currentCapturedColor;
            }
            
            // Setup color match quality slider
            if (colorMatchQualitySlider != null)
            {
                colorMatchQualitySlider.minValue = 0f;
                colorMatchQualitySlider.maxValue = 1f;
                colorMatchQualitySlider.value = 0f;
                colorMatchQualitySlider.interactable = false; // Read-only display
            }
            
            // Setup matching quality indicator
            if (matchingQualityIndicator != null)
            {
                matchingQualityIndicator.color = poorMatchColor;
            }
            
            Debug.Log("ColorAnalysisUI: Color comparison UI configured");
        }
        
        private void SetupColorHistoryUI()
        {
            // Clear existing history items
            if (colorHistoryContainer != null)
            {
                foreach (Transform child in colorHistoryContainer)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Update history count
            UpdateColorHistoryCount();
            
            Debug.Log("ColorAnalysisUI: Color history UI configured");
        }
        
        private void ConnectToSystems()
        {
            if (!autoConnectToSystems) return;
            
            // Find UI manager
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                colorAnalyzer = uiManager.ColorAnalyzer;
            }
            
            // Setup system event handlers
            if (colorAnalyzer != null)
            {
                colorAnalyzer.OnColorAnalyzed += OnSystemColorAnalyzed;
                colorAnalyzer.OnColorMatchSaved += OnSystemColorMatchSaved;
            }
            
            Debug.Log("ColorAnalysisUI: Connected to color analysis system");
        }
        
        private void CleanupEventHandlers()
        {
            // Cleanup button events
            if (confirmColorSelectionButton != null)
                confirmColorSelectionButton.onClick.RemoveAllListeners();
            if (cancelColorSelectionButton != null)
                cancelColorSelectionButton.onClick.RemoveAllListeners();
            
            // Cleanup system events
            if (colorAnalyzer != null)
            {
                colorAnalyzer.OnColorAnalyzed -= OnSystemColorAnalyzed;
                colorAnalyzer.OnColorMatchSaved -= OnSystemColorMatchSaved;
            }
        }
        
        // Color picker functionality
        public void StartColorPicking()
        {
            isColorPickingActive = true;
            
            if (colorPickerCrosshair != null)
            {
                colorPickerCrosshair.SetActive(true);
            }
            
            // Enable picker UI elements
            SetColorPickerUIActive(true);
            
            Debug.Log("ColorAnalysisUI: Color picking started");
        }
        
        public void StopColorPicking()
        {
            isColorPickingActive = false;
            
            if (colorPickerCrosshair != null)
            {
                colorPickerCrosshair.SetActive(false);
            }
            
            // Disable picker UI elements
            SetColorPickerUIActive(false);
            
            Debug.Log("ColorAnalysisUI: Color picking stopped");
        }
        
        public void UpdateColorPickerPosition(Vector2 position)
        {
            currentPickerPosition = position;
            
            // Update crosshair position (convert screen coordinates to world/UI coordinates)
            if (colorPickerCrosshair != null)
            {
                var rectTransform = colorPickerCrosshair.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = position;
                }
            }
            
            // Update coordinates display
            if (colorPickerCoordinatesText != null)
            {
                colorPickerCoordinatesText.text = $"Position: ({position.x:F0}, {position.y:F0})";
            }
            
            // Try to pick color at current position
            if (isColorPickingActive && colorAnalyzer != null)
            {
                try
                {
                    Color pickedColor = colorAnalyzer.PickColorFromImage(position);
                    UpdateColorPickerPreview(pickedColor);
                }
                catch (Exception ex)
                {
                    if (showDebugInfo)
                    {
                        Debug.LogWarning($"ColorAnalysisUI: Failed to pick color at {position} - {ex.Message}");
                    }
                }
            }
            
            OnColorPickerPositionChanged?.Invoke(position);
        }
        
        private void UpdateColorPickerPreview(Color color)
        {
            if (colorPickerPreview != null)
            {
                colorPickerPreview.color = color;
            }
        }
        
        private void ConfirmColorSelection()
        {
            if (!isColorPickingActive) return;
            
            try
            {
                // Get color at current picker position
                if (colorAnalyzer != null)
                {
                    Color selectedColor = colorAnalyzer.PickColorFromImage(currentPickerPosition);
                    SetReferenceColor(selectedColor);
                    
                    // Show selection feedback
                    ShowSelectionFeedback();
                    
                    OnReferenceColorSelected?.Invoke(selectedColor);
                }
                
                StopColorPicking();
                
                Debug.Log($"ColorAnalysisUI: Color selection confirmed - {currentReferenceColor}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ColorAnalysisUI: Failed to confirm color selection - {ex.Message}");
            }
        }
        
        private void CancelColorSelection()
        {
            StopColorPicking();
            Debug.Log("ColorAnalysisUI: Color selection cancelled");
        }
        
        private void ShowSelectionFeedback()
        {
            // Show visual feedback ring
            if (selectionFeedbackRing != null)
            {
                selectionFeedbackRing.SetActive(true);
                StartCoroutine(HideFeedbackAfterDelay());
            }
            
            // Play particle effect
            if (selectionParticleEffect != null)
            {
                selectionParticleEffect.Play();
            }
            
            // Play audio feedback
            if (selectionAudioFeedback != null)
            {
                selectionAudioFeedback.Play();
            }
        }
        
        private System.Collections.IEnumerator HideFeedbackAfterDelay()
        {
            yield return new UnityEngine.WaitForSeconds(feedbackDuration);
            
            if (selectionFeedbackRing != null)
            {
                selectionFeedbackRing.SetActive(false);
            }
        }
        
        private void SetColorPickerUIActive(bool active)
        {
            if (confirmColorSelectionButton != null)
                confirmColorSelectionButton.gameObject.SetActive(active);
            if (cancelColorSelectionButton != null)
                cancelColorSelectionButton.gameObject.SetActive(active);
            if (colorPickerCoordinatesText != null)
                colorPickerCoordinatesText.gameObject.SetActive(active);
        }
        
        // Color comparison functionality
        public void SetReferenceColor(Color color)
        {
            currentReferenceColor = color;
            
            if (referenceColorSwatch != null)
            {
                referenceColorSwatch.color = color;
            }
            
            if (referenceColorValueText != null)
            {
                referenceColorValueText.text = ColorToHexString(color);
            }
            
            // Update comparison if we have both colors
            if (currentCapturedColor != Color.white)
            {
                UpdateColorComparison();
            }
            
            Debug.Log($"ColorAnalysisUI: Reference color set to {ColorToHexString(color)}");
        }
        
        public void SetCapturedColor(Color color)
        {
            currentCapturedColor = color;
            
            if (capturedColorSwatch != null)
            {
                capturedColorSwatch.color = color;
            }
            
            if (capturedColorValueText != null)
            {
                capturedColorValueText.text = ColorToHexString(color);
            }
            
            // Update comparison if we have both colors
            if (currentReferenceColor != Color.white)
            {
                UpdateColorComparison();
            }
            
            OnPaintColorCaptured?.Invoke(color);
            
            Debug.Log($"ColorAnalysisUI: Captured color set to {ColorToHexString(color)}");
        }
        
        private void UpdateColorComparison()
        {
            if (colorAnalyzer == null) return;
            
            try
            {
                // Get color comparison result
                ColorMatchResult matchResult = colorAnalyzer.CompareColors(currentReferenceColor, currentCapturedColor);
                
                // Update UI with comparison results
                UpdateColorMatchDisplay(matchResult);
                
                OnColorComparisonComplete?.Invoke(matchResult);
                
                Debug.Log($"ColorAnalysisUI: Color comparison updated - Match Quality: {matchResult.matchQuality:F2}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ColorAnalysisUI: Failed to update color comparison - {ex.Message}");
            }
        }
        
        private void UpdateColorMatchDisplay(ColorMatchResult matchResult)
        {
            // Update match quality slider
            if (colorMatchQualitySlider != null)
            {
                colorMatchQualitySlider.value = matchResult.matchQuality;
            }
            
            // Update color difference text
            if (colorDifferenceText != null)
            {
                colorDifferenceText.text = $"Difference: {matchResult.colorDifference:F2}";
            }
            
            // Update matching guidance
            UpdateMatchingGuidance(matchResult);
            
            // Update quality indicator color
            UpdateQualityIndicator(matchResult.matchQuality);
        }
        
        private void UpdateMatchingGuidance(ColorMatchResult matchResult)
        {
            if (matchingGuidanceText == null) return;
            
            string guidance = matchResult.matchQuality switch
            {
                >= 0.9f => "Excellent match! Colors are nearly identical.",
                >= 0.7f => "Good match. Minor adjustments may improve accuracy.",
                >= 0.5f => "Fair match. Consider adjusting hue or saturation.",
                >= 0.3f => "Poor match. Significant color differences detected.",
                _ => "Very poor match. Colors are quite different."
            };
            
            // Add specific suggestions based on color differences
            if (matchResult.hueDifference > 30f)
            {
                guidance += " Try adjusting the hue.";
            }
            else if (matchResult.saturationDifference > 0.3f)
            {
                guidance += " Try adjusting the saturation.";
            }
            else if (matchResult.brightnessDifference > 0.3f)
            {
                guidance += " Try adjusting the brightness.";
            }
            
            matchingGuidanceText.text = guidance;
        }
        
        private void UpdateQualityIndicator(float matchQuality)
        {
            if (matchingQualityIndicator == null) return;
            
            Color indicatorColor = matchQuality switch
            {
                >= 0.7f => excellentMatchColor,
                >= 0.4f => goodMatchColor,
                _ => poorMatchColor
            };
            
            matchingQualityIndicator.color = indicatorColor;
        }
        
        // Color history functionality
        public void AddColorToHistory(ColorMatchData matchData)
        {
            colorHistory.Add(matchData);
            
            // Limit history size
            if (colorHistory.Count > 50)
            {
                colorHistory.RemoveAt(0);
            }
            
            // Update history UI
            UpdateColorHistoryDisplay();
            UpdateColorHistoryCount();
            
            Debug.Log($"ColorAnalysisUI: Color added to history - Total: {colorHistory.Count}");
        }
        
        private void UpdateColorHistoryDisplay()
        {
            if (colorHistoryContainer == null || colorHistoryItemPrefab == null) return;
            
            // Clear existing items
            foreach (Transform child in colorHistoryContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create new history items
            foreach (var matchData in colorHistory)
            {
                CreateColorHistoryItem(matchData);
            }
            
            // Scroll to bottom to show latest items
            if (colorHistoryScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                colorHistoryScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        private void CreateColorHistoryItem(ColorMatchData matchData)
        {
            GameObject historyItem = Instantiate(colorHistoryItemPrefab, colorHistoryContainer);
            
            // Setup history item components
            var referenceImage = historyItem.transform.Find("ReferenceColor")?.GetComponent<Image>();
            var capturedImage = historyItem.transform.Find("CapturedColor")?.GetComponent<Image>();
            var matchText = historyItem.transform.Find("MatchText")?.GetComponent<Text>();
            var timestampText = historyItem.transform.Find("TimestampText")?.GetComponent<Text>();
            
            if (referenceImage != null)
                referenceImage.color = matchData.referenceColor;
            if (capturedImage != null)
                capturedImage.color = matchData.capturedColor;
            if (matchText != null)
                matchText.text = $"{matchData.matchAccuracy:P0}";
            if (timestampText != null)
                timestampText.text = matchData.timestamp.ToString("HH:mm");
            
            // Add click handler to restore colors
            var button = historyItem.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => RestoreColorsFromHistory(matchData));
            }
        }
        
        private void RestoreColorsFromHistory(ColorMatchData matchData)
        {
            SetReferenceColor(matchData.referenceColor);
            SetCapturedColor(matchData.capturedColor);
            
            Debug.Log("ColorAnalysisUI: Colors restored from history");
        }
        
        private void UpdateColorHistoryCount()
        {
            if (colorHistoryCountText != null)
            {
                colorHistoryCountText.text = $"History ({colorHistory.Count})";
            }
        }
        
        public void ClearColorHistory()
        {
            colorHistory.Clear();
            UpdateColorHistoryDisplay();
            UpdateColorHistoryCount();
            
            Debug.Log("ColorAnalysisUI: Color history cleared");
        }
        
        // System event handlers
        private void OnSystemColorAnalyzed(ColorMatchResult result)
        {
            UpdateColorMatchDisplay(result);
        }
        
        private void OnSystemColorMatchSaved(ColorMatchData matchData)
        {
            AddColorToHistory(matchData);
        }
        
        // Utility methods
        private string ColorToHexString(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }
        
        private string ColorToRGBString(Color color)
        {
            return $"RGB({Mathf.RoundToInt(color.r * 255)}, {Mathf.RoundToInt(color.g * 255)}, {Mathf.RoundToInt(color.b * 255)})";
        }
        
        // Public methods
        public void UpdateUI()
        {
            // Update color swatches
            if (referenceColorSwatch != null)
                referenceColorSwatch.color = currentReferenceColor;
            if (capturedColorSwatch != null)
                capturedColorSwatch.color = currentCapturedColor;
            
            // Update color value texts
            if (referenceColorValueText != null)
                referenceColorValueText.text = ColorToHexString(currentReferenceColor);
            if (capturedColorValueText != null)
                capturedColorValueText.text = ColorToHexString(currentCapturedColor);
            
            // Update history display
            UpdateColorHistoryDisplay();
            UpdateColorHistoryCount();
        }
        
        public void SetDebugMode(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        // Properties
        public Color CurrentReferenceColor => currentReferenceColor;
        public Color CurrentCapturedColor => currentCapturedColor;
        public Vector2 CurrentPickerPosition => currentPickerPosition;
        public bool IsColorPickingActive => isColorPickingActive;
        public int ColorHistoryCount => colorHistory.Count;
        public List<ColorMatchData> ColorHistory => new List<ColorMatchData>(colorHistory);
    }
}