using System;
using UnityEngine;
using DaVinciEye.Canvas;
using DaVinciEye.ImageOverlay;
using DaVinciEye.Filters;
using DaVinciEye.ColorAnalysis;
using DaVinciEye.Input;

namespace DaVinciEye.Core
{
    /// <summary>
    /// Main application manager that coordinates all Da Vinci Eye systems
    /// </summary>
    public class DaVinciEyeApp : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private GameObject canvasManagerPrefab;
        [SerializeField] private GameObject imageOverlayPrefab;
        [SerializeField] private GameObject filterProcessorPrefab;
        [SerializeField] private GameObject colorAnalyzerPrefab;
        [SerializeField] private GameObject inputManagerPrefab;
        
        [Header("Application State")]
        [SerializeField] private ApplicationMode currentMode = ApplicationMode.CanvasDefinition;
        [SerializeField] private bool isInitialized = false;
        
        // System interfaces
        private ICanvasManager canvasManager;
        private IImageOverlay imageOverlay;
        private IFilterProcessor filterProcessor;
        private IColorAnalyzer colorAnalyzer;
        private IInputManager inputManager;
        
        // Events
        public event Action<ApplicationMode> OnModeChanged;
        public event Action OnApplicationInitialized;
        public event Action<string> OnError;
        
        private void Awake()
        {
            InitializeSystems();
        }
        
        private void Start()
        {
            if (!isInitialized)
            {
                Debug.LogError("DaVinciEyeApp: Failed to initialize systems");
                return;
            }
            
            SetupEventHandlers();
            OnApplicationInitialized?.Invoke();
        }
        
        private void InitializeSystems()
        {
            try
            {
                // Initialize systems in dependency order
                InitializeInputManager();
                InitializeCanvasManager();
                InitializeImageOverlay();
                InitializeFilterProcessor();
                InitializeColorAnalyzer();
                
                isInitialized = true;
                Debug.Log("DaVinciEyeApp: All systems initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"DaVinciEyeApp: Initialization failed - {ex.Message}");
                OnError?.Invoke($"System initialization failed: {ex.Message}");
            }
        }
        
        private void InitializeInputManager()
        {
            if (inputManagerPrefab != null)
            {
                var inputObj = Instantiate(inputManagerPrefab, transform);
                inputManager = inputObj.GetComponent<IInputManager>();
            }
            
            if (inputManager == null)
            {
                Debug.LogWarning("DaVinciEyeApp: Input Manager not found, creating placeholder");
            }
        }
        
        private void InitializeCanvasManager()
        {
            if (canvasManagerPrefab != null)
            {
                var canvasObj = Instantiate(canvasManagerPrefab, transform);
                canvasManager = canvasObj.GetComponent<ICanvasManager>();
            }
            
            if (canvasManager == null)
            {
                Debug.LogWarning("DaVinciEyeApp: Canvas Manager not found, creating placeholder");
            }
        }
        
        private void InitializeImageOverlay()
        {
            if (imageOverlayPrefab != null)
            {
                var imageObj = Instantiate(imageOverlayPrefab, transform);
                imageOverlay = imageObj.GetComponent<IImageOverlay>();
            }
            
            if (imageOverlay == null)
            {
                Debug.LogWarning("DaVinciEyeApp: Image Overlay not found, creating placeholder");
            }
        }
        
        private void InitializeFilterProcessor()
        {
            if (filterProcessorPrefab != null)
            {
                var filterObj = Instantiate(filterProcessorPrefab, transform);
                filterProcessor = filterObj.GetComponent<IFilterProcessor>();
            }
            
            if (filterProcessor == null)
            {
                Debug.LogWarning("DaVinciEyeApp: Filter Processor not found, creating placeholder");
            }
        }
        
        private void InitializeColorAnalyzer()
        {
            if (colorAnalyzerPrefab != null)
            {
                var colorObj = Instantiate(colorAnalyzerPrefab, transform);
                colorAnalyzer = colorObj.GetComponent<IColorAnalyzer>();
            }
            
            if (colorAnalyzer == null)
            {
                Debug.LogWarning("DaVinciEyeApp: Color Analyzer not found, creating placeholder");
            }
        }
        
        private void SetupEventHandlers()
        {
            // Canvas events
            if (canvasManager != null)
            {
                canvasManager.OnCanvasDefined += OnCanvasDefined;
                canvasManager.OnCanvasCleared += OnCanvasCleared;
            }
            
            // Image overlay events
            if (imageOverlay != null)
            {
                imageOverlay.OnImageLoaded += OnImageLoaded;
                imageOverlay.OnOpacityChanged += OnOpacityChanged;
            }
            
            // Filter events
            if (filterProcessor != null)
            {
                filterProcessor.OnFilterApplied += OnFilterApplied;
                filterProcessor.OnFilterRemoved += OnFilterRemoved;
            }
            
            // Color analysis events
            if (colorAnalyzer != null)
            {
                colorAnalyzer.OnColorAnalyzed += OnColorAnalyzed;
                colorAnalyzer.OnColorMatchSaved += OnColorMatchSaved;
            }
            
            // Input events
            if (inputManager != null)
            {
                inputManager.OnGestureRecognized += OnGestureRecognized;
                inputManager.OnHandTrackingLost += OnHandTrackingLost;
            }
        }
        
        public void SetApplicationMode(ApplicationMode mode)
        {
            if (currentMode == mode) return;
            
            var previousMode = currentMode;
            currentMode = mode;
            
            Debug.Log($"DaVinciEyeApp: Mode changed from {previousMode} to {mode}");
            OnModeChanged?.Invoke(mode);
        }
        
        // System access properties
        public ICanvasManager CanvasManager => canvasManager;
        public IImageOverlay ImageOverlay => imageOverlay;
        public IFilterProcessor FilterProcessor => filterProcessor;
        public IColorAnalyzer ColorAnalyzer => colorAnalyzer;
        public IInputManager InputManager => inputManager;
        
        public ApplicationMode CurrentMode => currentMode;
        public bool IsInitialized => isInitialized;
        
        // Event handlers
        private void OnCanvasDefined(CanvasData canvasData)
        {
            Debug.Log($"DaVinciEyeApp: Canvas defined with area {canvasData.area:F2} m²");
            SetApplicationMode(ApplicationMode.ImageOverlay);
        }
        
        private void OnCanvasCleared()
        {
            Debug.Log("DaVinciEyeApp: Canvas cleared");
            SetApplicationMode(ApplicationMode.CanvasDefinition);
        }
        
        private void OnImageLoaded(Texture2D image)
        {
            Debug.Log($"DaVinciEyeApp: Image loaded - {image.width}x{image.height}");
        }
        
        private void OnOpacityChanged(float opacity)
        {
            Debug.Log($"DaVinciEyeApp: Opacity changed to {opacity:F2}");
        }
        
        private void OnFilterApplied(Texture2D processedTexture)
        {
            Debug.Log("DaVinciEyeApp: Filter applied");
        }
        
        private void OnFilterRemoved(FilterType filterType)
        {
            Debug.Log($"DaVinciEyeApp: Filter removed - {filterType}");
        }
        
        private void OnColorAnalyzed(ColorMatchResult result)
        {
            Debug.Log($"DaVinciEyeApp: Color analyzed - Match: {result.matchQuality}");
        }
        
        private void OnColorMatchSaved(ColorMatchData matchData)
        {
            Debug.Log($"DaVinciEyeApp: Color match saved - Accuracy: {matchData.matchAccuracy:F2}");
        }
        
        private void OnGestureRecognized(GestureData gestureData)
        {
            Debug.Log($"DaVinciEyeApp: Gesture recognized - {gestureData.type}");
        }
        
        private void OnHandTrackingLost()
        {
            Debug.LogWarning("DaVinciEyeApp: Hand tracking lost");
        }
    }
    
    /// <summary>
    /// Application modes for different workflow stages
    /// </summary>
    public enum ApplicationMode
    {
        CanvasDefinition,
        ImageOverlay,
        FilterApplication,
        ColorAnalysis,
        Settings
    }
}