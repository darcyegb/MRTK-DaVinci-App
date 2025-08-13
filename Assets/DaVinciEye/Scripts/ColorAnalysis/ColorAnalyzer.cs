using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DaVinciEye.ColorAnalysis
{
    /// <summary>
    /// Complete color analyzer implementation that integrates all color analysis components
    /// Implements IColorAnalyzer interface and coordinates color picking, paint capture, and matching
    /// </summary>
    public class ColorAnalyzer : MonoBehaviour, IColorAnalyzer
    {
        [Header("Component References")]
        [SerializeField] private ColorPicker colorPicker;
        [SerializeField] private PaintColorAnalyzer paintAnalyzer;
        [SerializeField] private ColorMatcher colorMatcher;
        [SerializeField] private ColorComparisonUI comparisonUI;
        
        [Header("Integration Settings")]
        [SerializeField] private bool autoSetupComponents = true;
        [SerializeField] private bool enableRealTimeComparison = true;
        
        // IColorAnalyzer Events
        public event Action<ColorMatchResult> OnColorAnalyzed;
        public event Action<ColorMatchData> OnColorMatchSaved;
        public event Action<Color> OnColorPicked;
        
        // Properties
        public Color LastPickedColor { get; private set; } = Color.white;
        public Color LastCapturedColor { get; private set; } = Color.white;
        public ColorMatchResult LastMatchResult { get; private set; }
        
        private void Awake()
        {
            if (autoSetupComponents)
            {
                SetupComponents();
            }
        }
        
        private void Start()
        {
            ConnectComponents();
        }
        
        private void SetupComponents()
        {
            // Auto-find or create components
            if (colorPicker == null)
                colorPicker = GetComponent<ColorPicker>() ?? gameObject.AddComponent<ColorPicker>();
            
            if (paintAnalyzer == null)
                paintAnalyzer = GetComponent<PaintColorAnalyzer>() ?? gameObject.AddComponent<PaintColorAnalyzer>();
            
            if (colorMatcher == null)
                colorMatcher = GetComponent<ColorMatcher>() ?? gameObject.AddComponent<ColorMatcher>();
            
            if (comparisonUI == null)
                comparisonUI = GetComponentInChildren<ColorComparisonUI>();
        }
        
        private void ConnectComponents()
        {
            // Connect color picker events
            if (colorPicker != null)
            {
                colorPicker.OnColorPicked += HandleColorPicked;
                colorPicker.OnColorChanged += HandleColorChanged;
            }
            
            // Connect paint analyzer events
            if (paintAnalyzer != null)
            {
                paintAnalyzer.OnColorCaptured += HandlePaintColorCaptured;
            }
            
            // Connect color matcher events
            if (colorMatcher != null)
            {
                colorMatcher.OnColorMatched += HandleColorMatched;
                colorMatcher.OnMatchSaved += HandleMatchSaved;
            }
        }
        
        /// <summary>
        /// Pick color from image at specified coordinate (0-1 normalized)
        /// </summary>
        public Color PickColorFromImage(Vector2 imageCoordinate)
        {
            if (colorPicker == null)
            {
                Debug.LogError("ColorAnalyzer: ColorPicker component not available");
                return Color.white;
            }
            
            Color pickedColor = colorPicker.PickColorFromImage(imageCoordinate);
            LastPickedColor = pickedColor;
            
            OnColorPicked?.Invoke(pickedColor);
            
            // If real-time comparison is enabled and we have a captured color, compare them
            if (enableRealTimeComparison && LastCapturedColor != Color.clear)
            {
                CompareColors(pickedColor, LastCapturedColor);
            }
            
            return pickedColor;
        }
        
        /// <summary>
        /// Analyze paint color at world position using camera
        /// </summary>
        public async Task<Color> AnalyzePaintColorAsync(Vector3 worldPosition)
        {
            if (paintAnalyzer == null)
            {
                Debug.LogError("ColorAnalyzer: PaintColorAnalyzer component not available");
                return Color.white;
            }
            
            try
            {
                Color capturedColor = await paintAnalyzer.AnalyzePaintColorAsync(worldPosition);
                LastCapturedColor = capturedColor;
                
                // If real-time comparison is enabled and we have a picked color, compare them
                if (enableRealTimeComparison && LastPickedColor != Color.clear)
                {
                    CompareColors(LastPickedColor, capturedColor);
                }
                
                return capturedColor;
            }
            catch (Exception e)
            {
                Debug.LogError($"ColorAnalyzer: Paint color analysis failed - {e.Message}");
                return Color.white;
            }
        }
        
        /// <summary>
        /// Compare two colors and return detailed matching result
        /// </summary>
        public ColorMatchResult CompareColors(Color referenceColor, Color paintColor)
        {
            if (colorMatcher == null)
            {
                Debug.LogError("ColorAnalyzer: ColorMatcher component not available");
                return new ColorMatchResult(referenceColor, paintColor);
            }
            
            ColorMatchResult result = colorMatcher.CompareColors(referenceColor, paintColor);
            LastMatchResult = result;
            
            OnColorAnalyzed?.Invoke(result);
            
            // Update comparison UI if available
            if (comparisonUI != null)
            {
                comparisonUI.UpdateComparison(result);
            }
            
            return result;
        }
        
        /// <summary>
        /// Save color match data to history
        /// </summary>
        public void SaveColorMatch(ColorMatchData matchData)
        {
            if (colorMatcher == null)
            {
                Debug.LogError("ColorAnalyzer: ColorMatcher component not available");
                return;
            }
            
            colorMatcher.SaveColorMatch(matchData);
            OnColorMatchSaved?.Invoke(matchData);
        }
        
        /// <summary>
        /// Get color match history
        /// </summary>
        public List<ColorMatchData> GetColorHistory()
        {
            if (colorMatcher == null)
            {
                Debug.LogWarning("ColorAnalyzer: ColorMatcher component not available");
                return new List<ColorMatchData>();
            }
            
            return colorMatcher.GetColorHistory();
        }
        
        /// <summary>
        /// Set the texture for color picking
        /// </summary>
        public void SetImageTexture(Texture2D texture)
        {
            if (colorPicker != null)
            {
                colorPicker.SetTexture(texture);
            }
        }
        
        /// <summary>
        /// Set color matching method
        /// </summary>
        public void SetColorMatchingMethod(ColorMatchingMethod method)
        {
            if (colorMatcher != null)
            {
                colorMatcher.SetMatchingMethod(method);
            }
        }
        
        /// <summary>
        /// Calibrate paint color capture system
        /// </summary>
        public async Task<bool> CalibrateColorCapture(Color[] knownColors, Vector3[] swatchPositions)
        {
            if (paintAnalyzer == null)
            {
                Debug.LogError("ColorAnalyzer: PaintColorAnalyzer component not available for calibration");
                return false;
            }
            
            return await paintAnalyzer.CalibrateColorCapture(knownColors, swatchPositions);
        }
        
        /// <summary>
        /// Quick color match workflow - pick from image and capture from paint
        /// </summary>
        public async Task<ColorMatchResult> QuickColorMatch(Vector2 imageCoordinate, Vector3 paintPosition)
        {
            // Pick color from image
            Color referenceColor = PickColorFromImage(imageCoordinate);
            
            // Capture color from paint
            Color paintColor = await AnalyzePaintColorAsync(paintPosition);
            
            // Compare colors
            ColorMatchResult result = CompareColors(referenceColor, paintColor);
            
            // Auto-save the match
            ColorMatchData matchData = new ColorMatchData(referenceColor, paintColor, paintPosition)
            {
                imageCoordinate = imageCoordinate,
                notes = $"Quick match - Quality: {result.matchQuality}"
            };
            
            SaveColorMatch(matchData);
            
            return result;
        }
        
        /// <summary>
        /// Get color analysis statistics
        /// </summary>
        public ColorMatchStatistics GetAnalysisStatistics()
        {
            if (colorMatcher == null)
            {
                return new ColorMatchStatistics();
            }
            
            return colorMatcher.GetMatchStatistics();
        }
        
        /// <summary>
        /// Clear all color analysis data
        /// </summary>
        public void ClearAnalysisData()
        {
            LastPickedColor = Color.white;
            LastCapturedColor = Color.white;
            LastMatchResult = null;
            
            if (colorMatcher != null)
            {
                colorMatcher.ClearMatchHistory();
            }
            
            if (comparisonUI != null)
            {
                comparisonUI.ClearComparison();
            }
        }
        
        /// <summary>
        /// Enable or disable real-time color comparison
        /// </summary>
        public void SetRealTimeComparison(bool enabled)
        {
            enableRealTimeComparison = enabled;
        }
        
        // Event handlers
        private void HandleColorPicked(Color color)
        {
            LastPickedColor = color;
            Debug.Log($"ColorAnalyzer: Color picked from image - {color}");
        }
        
        private void HandleColorChanged(Color color)
        {
            // Handle manual color adjustments
            Debug.Log($"ColorAnalyzer: Color manually adjusted - {color}");
        }
        
        private void HandlePaintColorCaptured(Color color)
        {
            LastCapturedColor = color;
            Debug.Log($"ColorAnalyzer: Paint color captured - {color}");
        }
        
        private void HandleColorMatched(ColorMatchResult result)
        {
            LastMatchResult = result;
            Debug.Log($"ColorAnalyzer: Colors matched - Quality: {result.matchQuality}, Accuracy: {result.matchAccuracy:F2}");
        }
        
        private void HandleMatchSaved(ColorMatchData matchData)
        {
            Debug.Log($"ColorAnalyzer: Color match saved - Accuracy: {matchData.matchAccuracy:F2}");
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (colorPicker != null)
            {
                colorPicker.OnColorPicked -= HandleColorPicked;
                colorPicker.OnColorChanged -= HandleColorChanged;
            }
            
            if (paintAnalyzer != null)
            {
                paintAnalyzer.OnColorCaptured -= HandlePaintColorCaptured;
            }
            
            if (colorMatcher != null)
            {
                colorMatcher.OnColorMatched -= HandleColorMatched;
                colorMatcher.OnMatchSaved -= HandleMatchSaved;
            }
        }
        
        /// <summary>
        /// Get component references for external access
        /// </summary>
        public ColorPicker GetColorPicker() => colorPicker;
        public PaintColorAnalyzer GetPaintAnalyzer() => paintAnalyzer;
        public ColorMatcher GetColorMatcher() => colorMatcher;
        public ColorComparisonUI GetComparisonUI() => comparisonUI;
    }
}