using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;

namespace DaVinciEye.Filters
{
    /// <summary>
    /// UI for managing multiple color ranges
    /// Implements Requirements: 4.1.4, 4.1.5
    /// </summary>
    public class MultipleColorRangeUI : MonoBehaviour
    {
        [Header("MRTK UI Components")]
        [SerializeField] private PressableButton addRangeButton;
        [SerializeField] private PressableButton removeRangeButton;
        [SerializeField] private PressableButton clearAllButton;
        [SerializeField] private PressableButton optimizeButton;
        
        [Header("Combination Mode")]
        [SerializeField] private Toggle unionModeToggle;
        [SerializeField] private Toggle intersectionModeToggle;
        [SerializeField] private Toggle exclusiveModeToggle;
        [SerializeField] private Toggle weightedModeToggle;
        
        [Header("Range List")]
        [SerializeField] private Transform rangeListParent;
        [SerializeField] private GameObject rangeItemPrefab;
        [SerializeField] private ScrollView rangeScrollView;
        [SerializeField] private Text rangeCountText;
        
        [Header("Statistics Display")]
        [SerializeField] private Text statisticsText;
        [SerializeField] private Text performanceText;
        
        [Header("Target Components")]
        [SerializeField] private MultipleColorRangeManager rangeManager;
        [SerializeField] private ColorRangePickerUI rangePicker;
        
        // UI state
        private List<GameObject> rangeUIItems = new List<GameObject>();
        private int selectedRangeIndex = -1;
        
        // Events
        public event Action<MultipleColorRangeManager.ColorRangeData> OnRangeSelected;
        public event Action<MultipleColorRangeManager.CombinationMode> OnModeChanged;
        
        private void Awake()
        {
            InitializeUI();
        }
        
        private void Start()
        {
            SetupUICallbacks();
            UpdateUI();
        }
        
        /// <summary>
        /// Initialize UI components
        /// </summary>
        private void InitializeUI()
        {
            // Find components if not assigned
            if (rangeManager == null)
            {
                rangeManager = FindObjectOfType<MultipleColorRangeManager>();
            }
            
            if (rangePicker == null)
            {
                rangePicker = FindObjectOfType<ColorRangePickerUI>();
            }
            
            Debug.Log("MultipleColorRangeUI: Initialized multiple color range UI");
        }     
   
        /// <summary>
        /// Setup UI callbacks and event handlers
        /// </summary>
        private void SetupUICallbacks()
        {
            // Button callbacks
            if (addRangeButton != null)
                addRangeButton.OnClicked.AddListener(AddNewRange);
            
            if (removeRangeButton != null)
                removeRangeButton.OnClicked.AddListener(RemoveSelectedRange);
            
            if (clearAllButton != null)
                clearAllButton.OnClicked.AddListener(ClearAllRanges);
            
            if (optimizeButton != null)
                optimizeButton.OnClicked.AddListener(OptimizeRanges);
            
            // Mode selection toggles
            if (unionModeToggle != null)
            {
                unionModeToggle.onValueChanged.AddListener((isOn) => {
                    if (isOn) SetCombinationMode(MultipleColorRangeManager.CombinationMode.Union);
                });
            }
            
            if (intersectionModeToggle != null)
            {
                intersectionModeToggle.onValueChanged.AddListener((isOn) => {
                    if (isOn) SetCombinationMode(MultipleColorRangeManager.CombinationMode.Intersection);
                });
            }
            
            if (exclusiveModeToggle != null)
            {
                exclusiveModeToggle.onValueChanged.AddListener((isOn) => {
                    if (isOn) SetCombinationMode(MultipleColorRangeManager.CombinationMode.Exclusive);
                });
            }
            
            if (weightedModeToggle != null)
            {
                weightedModeToggle.onValueChanged.AddListener((isOn) => {
                    if (isOn) SetCombinationMode(MultipleColorRangeManager.CombinationMode.Weighted);
                });
            }
            
            // Subscribe to range manager events
            if (rangeManager != null)
            {
                rangeManager.OnRangeAdded += OnRangeAdded;
                rangeManager.OnRangeRemoved += OnRangeRemoved;
                rangeManager.OnStatisticsUpdated += OnStatisticsUpdated;
            }
            
            Debug.Log("MultipleColorRangeUI: Setup UI callbacks complete");
        }
        
        /// <summary>
        /// Update UI to reflect current state
        /// </summary>
        private void UpdateUI()
        {
            UpdateRangeList();
            UpdateModeToggles();
            UpdateStatistics();
        }
        
        /// <summary>
        /// Update the range list display
        /// </summary>
        private void UpdateRangeList()
        {
            if (rangeManager == null) return;
            
            // Clear existing UI items
            foreach (var item in rangeUIItems)
            {
                if (item != null)
                    Destroy(item);
            }
            rangeUIItems.Clear();
            
            // Create UI items for each range
            var ranges = rangeManager.ColorRanges;
            for (int i = 0; i < ranges.Count; i++)
            {
                CreateRangeUIItem(ranges[i], i);
            }
            
            // Update range count
            if (rangeCountText != null)
            {
                rangeCountText.text = $"Ranges: {ranges.Count}/{rangeManager.MaxActiveRanges}";
            }
        }
        
        /// <summary>
        /// Create a UI item for a color range
        /// </summary>
        private void CreateRangeUIItem(MultipleColorRangeManager.ColorRangeData range, int index)
        {
            if (rangeItemPrefab == null || rangeListParent == null)
                return;
            
            GameObject rangeItem = Instantiate(rangeItemPrefab, rangeListParent);
            rangeUIItems.Add(rangeItem);
            
            // Setup range item components
            var rangeText = rangeItem.GetComponentInChildren<Text>();
            if (rangeText != null)
            {
                rangeText.text = $"{range.rangeName}\nH:{range.hueMin:F0}-{range.hueMax:F0}°\nPriority:{range.priority}";
            }
            
            // Add toggle for active state
            var activeToggle = rangeItem.GetComponentInChildren<Toggle>();
            if (activeToggle != null)
            {
                activeToggle.isOn = range.isActive;
                activeToggle.onValueChanged.AddListener((isOn) => {
                    rangeManager?.ToggleColorRange(range.rangeId, isOn);
                });
            }
            
            // Add selection button
            var selectButton = rangeItem.GetComponent<PressableButton>();
            if (selectButton != null)
            {
                int capturedIndex = index;
                selectButton.OnClicked.AddListener(() => SelectRange(capturedIndex));
            }
            
            // Color indicator
            var colorImage = rangeItem.GetComponentsInChildren<Image>()[1]; // Assuming second image is color indicator
            if (colorImage != null)
            {
                // Show representative color from range center
                float centerH = (range.hueMin + range.hueMax) / 2f / 360f;
                float centerS = (range.saturationMin + range.saturationMax) / 2f;
                float centerV = (range.valueMin + range.valueMax) / 2f;
                colorImage.color = Color.HSVToRGB(centerH, centerS, centerV);
            }
        }
        
        /// <summary>
        /// Update combination mode toggles
        /// </summary>
        private void UpdateModeToggles()
        {
            if (rangeManager == null) return;
            
            var currentMode = rangeManager.CurrentCombinationMode;
            
            if (unionModeToggle != null)
                unionModeToggle.isOn = (currentMode == MultipleColorRangeManager.CombinationMode.Union);
            
            if (intersectionModeToggle != null)
                intersectionModeToggle.isOn = (currentMode == MultipleColorRangeManager.CombinationMode.Intersection);
            
            if (exclusiveModeToggle != null)
                exclusiveModeToggle.isOn = (currentMode == MultipleColorRangeManager.CombinationMode.Exclusive);
            
            if (weightedModeToggle != null)
                weightedModeToggle.isOn = (currentMode == MultipleColorRangeManager.CombinationMode.Weighted);
        }
        
        /// <summary>
        /// Add a new range from the current picker settings
        /// </summary>
        private void AddNewRange()
        {
            if (rangeManager == null)
            {
                Debug.LogWarning("MultipleColorRangeUI: No range manager available");
                return;
            }
            
            // Get current range from picker if available
            MultipleColorRangeManager.ColorRangeData newRange;
            
            if (rangePicker != null)
            {
                var pickerRange = rangePicker.GetCurrentRange();
                if (pickerRange != null)
                {
                    // Convert from ColorRangeFilter.ColorRangeSettings to MultipleColorRangeManager.ColorRangeData
                    newRange = new MultipleColorRangeManager.ColorRangeData($"Range {rangeManager.ColorRanges.Count + 1}");
                    newRange.hueMin = pickerRange.hueMin;
                    newRange.hueMax = pickerRange.hueMax;
                    newRange.saturationMin = pickerRange.saturationMin;
                    newRange.saturationMax = pickerRange.saturationMax;
                    newRange.valueMin = pickerRange.valueMin;
                    newRange.valueMax = pickerRange.valueMax;
                    newRange.showOriginalColors = pickerRange.showOriginalColors;
                    newRange.showAsHighlight = pickerRange.showAsHighlight;
                    newRange.highlightColor = pickerRange.highlightColor;
                    newRange.highlightIntensity = pickerRange.highlightIntensity;
                }
                else
                {
                    // Create default range
                    newRange = new MultipleColorRangeManager.ColorRangeData($"Range {rangeManager.ColorRanges.Count + 1}");
                }
            }
            else
            {
                // Create default range
                newRange = new MultipleColorRangeManager.ColorRangeData($"Range {rangeManager.ColorRanges.Count + 1}");
            }
            
            rangeManager.AddColorRange(newRange);
            
            Debug.Log($"MultipleColorRangeUI: Added new range '{newRange.rangeName}'");
        }
        
        /// <summary>
        /// Remove the currently selected range
        /// </summary>
        private void RemoveSelectedRange()
        {
            if (rangeManager == null || selectedRangeIndex < 0)
            {
                Debug.LogWarning("MultipleColorRangeUI: No range selected for removal");
                return;
            }
            
            var ranges = rangeManager.ColorRanges;
            if (selectedRangeIndex < ranges.Count)
            {
                var rangeToRemove = ranges[selectedRangeIndex];
                rangeManager.RemoveColorRange(rangeToRemove.rangeId);
                selectedRangeIndex = -1;
            }
        }
        
        /// <summary>
        /// Clear all ranges
        /// </summary>
        private void ClearAllRanges()
        {
            if (rangeManager != null)
            {
                rangeManager.ClearAllRanges();
                selectedRangeIndex = -1;
            }
        }
        
        /// <summary>
        /// Optimize ranges by merging overlapping ones
        /// </summary>
        private void OptimizeRanges()
        {
            if (rangeManager != null)
            {
                rangeManager.OptimizeRanges();
            }
        }
        
        /// <summary>
        /// Select a range for editing
        /// </summary>
        private void SelectRange(int index)
        {
            selectedRangeIndex = index;
            
            if (rangeManager != null && index >= 0 && index < rangeManager.ColorRanges.Count)
            {
                var selectedRange = rangeManager.ColorRanges[index];
                OnRangeSelected?.Invoke(selectedRange);
                
                // Load range into picker if available
                if (rangePicker != null)
                {
                    // Convert to ColorRangeFilter.ColorRangeSettings format
                    var pickerRange = new ColorRangeFilter.ColorRangeSettings();
                    pickerRange.hueMin = selectedRange.hueMin;
                    pickerRange.hueMax = selectedRange.hueMax;
                    pickerRange.saturationMin = selectedRange.saturationMin;
                    pickerRange.saturationMax = selectedRange.saturationMax;
                    pickerRange.valueMin = selectedRange.valueMin;
                    pickerRange.valueMax = selectedRange.valueMax;
                    pickerRange.showOriginalColors = selectedRange.showOriginalColors;
                    pickerRange.showAsHighlight = selectedRange.showAsHighlight;
                    pickerRange.highlightColor = selectedRange.highlightColor;
                    pickerRange.highlightIntensity = selectedRange.highlightIntensity;
                    pickerRange.rangeName = selectedRange.rangeName;
                    
                    rangePicker.LoadRange(pickerRange);
                }
                
                Debug.Log($"MultipleColorRangeUI: Selected range '{selectedRange.rangeName}'");
            }
        }
        
        /// <summary>
        /// Set combination mode
        /// </summary>
        private void SetCombinationMode(MultipleColorRangeManager.CombinationMode mode)
        {
            if (rangeManager != null)
            {
                rangeManager.SetCombinationMode(mode, true);
                OnModeChanged?.Invoke(mode);
            }
        }
        
        /// <summary>
        /// Update statistics display
        /// </summary>
        private void UpdateStatistics()
        {
            if (rangeManager == null) return;
            
            var stats = rangeManager.GetStatistics();
            if (stats != null && statisticsText != null)
            {
                statisticsText.text = $"Active: {stats.activeRanges}/{stats.totalRanges}\n" +
                                    $"Mode: {stats.mode}\n" +
                                    $"Coverage: {stats.coveragePercentage:F1}%\n" +
                                    $"Overlaps: {stats.overlappingRanges}";
            }
            
            if (stats != null && performanceText != null)
            {
                performanceText.text = $"Processing: {stats.processingTime * 1000f:F1}ms\n" +
                                     $"Pixels: {stats.pixelsMatched}/{stats.totalPixels}";
            }
        }
        
        // Event handlers
        
        private void OnRangeAdded(MultipleColorRangeManager.ColorRangeData range)
        {
            UpdateRangeList();
        }
        
        private void OnRangeRemoved(int rangeId)
        {
            UpdateRangeList();
        }
        
        private void OnStatisticsUpdated(MultipleColorRangeManager.MultipleRangeStatistics stats)
        {
            UpdateStatistics();
        }
        
        /// <summary>
        /// Get the currently selected range
        /// </summary>
        public MultipleColorRangeManager.ColorRangeData GetSelectedRange()
        {
            if (rangeManager != null && selectedRangeIndex >= 0 && selectedRangeIndex < rangeManager.ColorRanges.Count)
            {
                return rangeManager.ColorRanges[selectedRangeIndex];
            }
            return null;
        }
        
        /// <summary>
        /// Set the maximum number of active ranges
        /// </summary>
        public void SetMaxActiveRanges(int maxRanges)
        {
            if (rangeManager != null)
            {
                rangeManager.MaxActiveRanges = maxRanges;
                UpdateUI();
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (rangeManager != null)
            {
                rangeManager.OnRangeAdded -= OnRangeAdded;
                rangeManager.OnRangeRemoved -= OnRangeRemoved;
                rangeManager.OnStatisticsUpdated -= OnStatisticsUpdated;
            }
        }
    }
}