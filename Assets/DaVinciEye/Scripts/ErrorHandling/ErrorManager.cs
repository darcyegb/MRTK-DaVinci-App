using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DaVinciEye.ErrorHandling
{
    /// <summary>
    /// Centralized error management system for Da Vinci Eye application
    /// Handles error detection, logging, user notification, and recovery
    /// </summary>
    public class ErrorManager : MonoBehaviour
    {
        [Header("Error Handling Configuration")]
        [SerializeField] private bool enableDetailedLogging = true;
        [SerializeField] private bool showUserFriendlyMessages = true;
        [SerializeField] private float errorCooldownTime = 2f; // Prevent spam
        [SerializeField] private int maxErrorHistorySize = 100;

        [Header("Recovery Settings")]
        [SerializeField] private bool enableAutoRecovery = true;
        [SerializeField] private int maxRecoveryAttempts = 3;
        [SerializeField] private float recoveryDelaySeconds = 1f;

        // Error tracking
        private Dictionary<ErrorType, DateTime> lastErrorTimes = new Dictionary<ErrorType, DateTime>();
        private List<ErrorRecord> errorHistory = new List<ErrorRecord>();
        private Dictionary<ErrorType, int> recoveryAttempts = new Dictionary<ErrorType, int>();

        // Events
        public UnityEvent<ErrorInfo> OnErrorOccurred = new UnityEvent<ErrorInfo>();
        public UnityEvent<ErrorInfo> OnErrorRecovered = new UnityEvent<ErrorInfo>();
        public UnityEvent<string> OnUserMessageRequired = new UnityEvent<string>();

        public static ErrorManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeErrorHandling();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeErrorHandling()
        {
            // Set up Unity's built-in error handling
            Application.logMessageReceived += HandleUnityLogMessage;
            
            // Initialize error type tracking
            foreach (ErrorType errorType in Enum.GetValues(typeof(ErrorType)))
            {
                recoveryAttempts[errorType] = 0;
            }

            Debug.Log("[ErrorManager] Error handling system initialized");
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleUnityLogMessage;
        }

        /// <summary>
        /// Report an error to the error management system
        /// </summary>
        public void ReportError(ErrorType errorType, string message, Exception exception = null, string context = "")
        {
            // Check cooldown to prevent spam
            if (IsErrorInCooldown(errorType))
            {
                return;
            }

            var errorInfo = new ErrorInfo
            {
                errorType = errorType,
                message = message,
                exception = exception,
                context = context,
                timestamp = DateTime.Now,
                severity = GetErrorSeverity(errorType)
            };

            ProcessError(errorInfo);
        }

        /// <summary>
        /// Process an error through the complete handling pipeline
        /// </summary>
        private void ProcessError(ErrorInfo errorInfo)
        {
            // Log the error
            LogError(errorInfo);

            // Add to error history
            AddToErrorHistory(errorInfo);

            // Update cooldown
            lastErrorTimes[errorInfo.errorType] = DateTime.Now;

            // Notify listeners
            OnErrorOccurred.Invoke(errorInfo);

            // Show user message if appropriate
            if (showUserFriendlyMessages && errorInfo.severity >= ErrorSeverity.Warning)
            {
                string userMessage = GetUserFriendlyMessage(errorInfo);
                OnUserMessageRequired.Invoke(userMessage);
            }

            // Attempt recovery if enabled
            if (enableAutoRecovery && CanAttemptRecovery(errorInfo.errorType))
            {
                AttemptRecovery(errorInfo);
            }
        }

        /// <summary>
        /// Attempt to recover from an error
        /// </summary>
        private void AttemptRecovery(ErrorInfo errorInfo)
        {
            recoveryAttempts[errorInfo.errorType]++;

            Debug.Log($"[ErrorManager] Attempting recovery for {errorInfo.errorType} (attempt {recoveryAttempts[errorInfo.errorType]})");

            // Delay recovery attempt to allow system to stabilize
            Invoke(nameof(ExecuteRecovery), recoveryDelaySeconds);
            
            void ExecuteRecovery()
            {
                bool recoverySuccess = false;

                try
                {
                    recoverySuccess = ExecuteRecoveryStrategy(errorInfo);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ErrorManager] Recovery attempt failed: {e.Message}");
                    recoverySuccess = false;
                }

                if (recoverySuccess)
                {
                    Debug.Log($"[ErrorManager] Successfully recovered from {errorInfo.errorType}");
                    recoveryAttempts[errorInfo.errorType] = 0; // Reset counter on success
                    OnErrorRecovered.Invoke(errorInfo);
                }
                else if (recoveryAttempts[errorInfo.errorType] < maxRecoveryAttempts)
                {
                    Debug.LogWarning($"[ErrorManager] Recovery failed, will retry ({recoveryAttempts[errorInfo.errorType]}/{maxRecoveryAttempts})");
                    Invoke(nameof(ExecuteRecovery), recoveryDelaySeconds * 2); // Exponential backoff
                }
                else
                {
                    Debug.LogError($"[ErrorManager] Maximum recovery attempts reached for {errorInfo.errorType}");
                    HandleUnrecoverableError(errorInfo);
                }
            }
        }

        /// <summary>
        /// Execute specific recovery strategy based on error type
        /// </summary>
        private bool ExecuteRecoveryStrategy(ErrorInfo errorInfo)
        {
            switch (errorInfo.errorType)
            {
                case ErrorType.TrackingLoss:
                    return RecoverFromTrackingLoss();

                case ErrorType.ImageLoadFailure:
                    return RecoverFromImageLoadFailure();

                case ErrorType.FilterProcessingError:
                    return RecoverFromFilterProcessingError();

                case ErrorType.ColorAnalysisFailure:
                    return RecoverFromColorAnalysisFailure();

                case ErrorType.MemoryPressure:
                    return RecoverFromMemoryPressure();

                case ErrorType.PerformanceDegradation:
                    return RecoverFromPerformanceDegradation();

                case ErrorType.UIInteractionFailure:
                    return RecoverFromUIInteractionFailure();

                case ErrorType.SpatialAnchorFailure:
                    return RecoverFromSpatialAnchorFailure();

                case ErrorType.CameraAccessFailure:
                    return RecoverFromCameraAccessFailure();

                case ErrorType.SessionDataCorruption:
                    return RecoverFromSessionDataCorruption();

                default:
                    Debug.LogWarning($"[ErrorManager] No recovery strategy defined for {errorInfo.errorType}");
                    return false;
            }
        }

        #region Recovery Strategies

        private bool RecoverFromTrackingLoss()
        {
            Debug.Log("[ErrorManager] Attempting tracking loss recovery");

            var trackingMonitor = FindObjectOfType<SpatialTracking.TrackingQualityMonitor>();
            if (trackingMonitor != null)
            {
                // Reset tracking system
                trackingMonitor.ResetTracking();
                
                // Check if tracking recovered
                return trackingMonitor.CurrentQuality > 0.5f;
            }

            return false;
        }

        private bool RecoverFromImageLoadFailure()
        {
            Debug.Log("[ErrorManager] Attempting image load failure recovery");

            var imageOverlay = FindObjectOfType<ImageOverlay.ImageOverlayManager>();
            if (imageOverlay != null)
            {
                // Clear current image and reset to safe state
                imageOverlay.ClearImage();
                
                // Force garbage collection to free memory
                System.GC.Collect();
                
                return true; // Recovery successful if we can clear the image
            }

            return false;
        }

        private bool RecoverFromFilterProcessingError()
        {
            Debug.Log("[ErrorManager] Attempting filter processing error recovery");

            var filterManager = FindObjectOfType<Filters.FilterManager>();
            if (filterManager != null)
            {
                // Clear all filters and reset to original image
                filterManager.ClearAllFilters();
                
                // Force garbage collection
                System.GC.Collect();
                
                return true;
            }

            return false;
        }

        private bool RecoverFromColorAnalysisFailure()
        {
            Debug.Log("[ErrorManager] Attempting color analysis failure recovery");

            var colorAnalyzer = FindObjectOfType<ColorAnalysis.ColorAnalyzer>();
            if (colorAnalyzer != null)
            {
                // Reset color analysis system
                colorAnalyzer.ResetAnalysisSystem();
                return true;
            }

            return false;
        }

        private bool RecoverFromMemoryPressure()
        {
            Debug.Log("[ErrorManager] Attempting memory pressure recovery");

            // Force garbage collection
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            // Clear non-essential caches
            var imageOverlay = FindObjectOfType<ImageOverlay.ImageOverlayManager>();
            if (imageOverlay != null)
            {
                imageOverlay.ClearImageCache();
            }

            var filterManager = FindObjectOfType<Filters.FilterManager>();
            if (filterManager != null)
            {
                filterManager.ClearFilterCache();
            }

            // Check if memory pressure is relieved
            long currentMemory = System.GC.GetTotalMemory(false);
            long memoryLimit = 512 * 1024 * 1024; // 512MB
            
            return currentMemory < memoryLimit * 0.8f; // 80% of limit
        }

        private bool RecoverFromPerformanceDegradation()
        {
            Debug.Log("[ErrorManager] Attempting performance degradation recovery");

            // Reduce quality settings
            var filterManager = FindObjectOfType<Filters.FilterManager>();
            if (filterManager != null)
            {
                filterManager.ReduceFilterQuality();
            }

            var imageOverlay = FindObjectOfType<ImageOverlay.ImageOverlayManager>();
            if (imageOverlay != null)
            {
                imageOverlay.ReduceImageQuality();
            }

            // Force garbage collection
            System.GC.Collect();

            return true; // Assume quality reduction helps
        }

        private bool RecoverFromUIInteractionFailure()
        {
            Debug.Log("[ErrorManager] Attempting UI interaction failure recovery");

            var uiManager = FindObjectOfType<UI.UIManager>();
            if (uiManager != null)
            {
                // Reset UI to safe state
                uiManager.ResetToSafeState();
                return true;
            }

            return false;
        }

        private bool RecoverFromSpatialAnchorFailure()
        {
            Debug.Log("[ErrorManager] Attempting spatial anchor failure recovery");

            var canvasManager = FindObjectOfType<Canvas.CanvasDefinitionManager>();
            if (canvasManager != null)
            {
                // Clear current anchor and allow re-definition
                canvasManager.ClearSpatialAnchor();
                return true;
            }

            return false;
        }

        private bool RecoverFromCameraAccessFailure()
        {
            Debug.Log("[ErrorManager] Attempting camera access failure recovery");

            var colorAnalyzer = FindObjectOfType<ColorAnalysis.ColorAnalyzer>();
            if (colorAnalyzer != null)
            {
                // Switch to manual color input mode
                colorAnalyzer.EnableManualColorInput();
                return true;
            }

            return false;
        }

        private bool RecoverFromSessionDataCorruption()
        {
            Debug.Log("[ErrorManager] Attempting session data corruption recovery");

            var sessionManager = FindObjectOfType<SessionManagement.SessionDataManager>();
            if (sessionManager != null)
            {
                // Create new session with default settings
                sessionManager.CreateNewSession();
                return true;
            }

            return false;
        }

        #endregion

        /// <summary>
        /// Handle errors that cannot be recovered from
        /// </summary>
        private void HandleUnrecoverableError(ErrorInfo errorInfo)
        {
            Debug.LogError($"[ErrorManager] Unrecoverable error: {errorInfo.errorType}");

            string criticalMessage = GetCriticalErrorMessage(errorInfo);
            OnUserMessageRequired.Invoke(criticalMessage);

            // Log critical error for debugging
            LogCriticalError(errorInfo);

            // Consider graceful shutdown or safe mode
            if (errorInfo.severity == ErrorSeverity.Critical)
            {
                EnterSafeMode();
            }
        }

        /// <summary>
        /// Enter safe mode with minimal functionality
        /// </summary>
        private void EnterSafeMode()
        {
            Debug.Log("[ErrorManager] Entering safe mode");

            // Disable non-essential systems
            var filterManager = FindObjectOfType<Filters.FilterManager>();
            if (filterManager != null) filterManager.enabled = false;

            var colorAnalyzer = FindObjectOfType<ColorAnalysis.ColorAnalyzer>();
            if (colorAnalyzer != null) colorAnalyzer.enabled = false;

            // Keep only basic canvas and image overlay functionality
            OnUserMessageRequired.Invoke("Application is running in safe mode. Some features may be unavailable.");
        }

        /// <summary>
        /// Handle Unity's built-in log messages for automatic error detection
        /// </summary>
        private void HandleUnityLogMessage(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                // Analyze error message to determine type
                ErrorType errorType = AnalyzeUnityError(logString);
                
                if (errorType != ErrorType.Unknown)
                {
                    ReportError(errorType, logString, null, "Unity Log");
                }
            }
        }

        /// <summary>
        /// Analyze Unity error messages to determine error type
        /// </summary>
        private ErrorType AnalyzeUnityError(string errorMessage)
        {
            string lowerMessage = errorMessage.ToLower();

            if (lowerMessage.Contains("tracking") || lowerMessage.Contains("spatial"))
                return ErrorType.TrackingLoss;
            
            if (lowerMessage.Contains("texture") || lowerMessage.Contains("image"))
                return ErrorType.ImageLoadFailure;
            
            if (lowerMessage.Contains("memory") || lowerMessage.Contains("out of memory"))
                return ErrorType.MemoryPressure;
            
            if (lowerMessage.Contains("camera"))
                return ErrorType.CameraAccessFailure;
            
            if (lowerMessage.Contains("ui") || lowerMessage.Contains("interaction"))
                return ErrorType.UIInteractionFailure;

            return ErrorType.Unknown;
        }

        #region Helper Methods

        private bool IsErrorInCooldown(ErrorType errorType)
        {
            if (!lastErrorTimes.ContainsKey(errorType))
                return false;

            return (DateTime.Now - lastErrorTimes[errorType]).TotalSeconds < errorCooldownTime;
        }

        private bool CanAttemptRecovery(ErrorType errorType)
        {
            return recoveryAttempts[errorType] < maxRecoveryAttempts;
        }

        private ErrorSeverity GetErrorSeverity(ErrorType errorType)
        {
            switch (errorType)
            {
                case ErrorType.TrackingLoss:
                case ErrorType.MemoryPressure:
                case ErrorType.SessionDataCorruption:
                    return ErrorSeverity.Critical;

                case ErrorType.ImageLoadFailure:
                case ErrorType.FilterProcessingError:
                case ErrorType.CameraAccessFailure:
                case ErrorType.SpatialAnchorFailure:
                    return ErrorSeverity.Error;

                case ErrorType.ColorAnalysisFailure:
                case ErrorType.UIInteractionFailure:
                case ErrorType.PerformanceDegradation:
                    return ErrorSeverity.Warning;

                default:
                    return ErrorSeverity.Info;
            }
        }

        private void LogError(ErrorInfo errorInfo)
        {
            if (!enableDetailedLogging) return;

            string logMessage = $"[ErrorManager] {errorInfo.severity}: {errorInfo.errorType} - {errorInfo.message}";
            
            if (!string.IsNullOrEmpty(errorInfo.context))
            {
                logMessage += $" (Context: {errorInfo.context})";
            }

            switch (errorInfo.severity)
            {
                case ErrorSeverity.Critical:
                case ErrorSeverity.Error:
                    Debug.LogError(logMessage);
                    break;
                case ErrorSeverity.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                default:
                    Debug.Log(logMessage);
                    break;
            }

            if (errorInfo.exception != null)
            {
                Debug.LogException(errorInfo.exception);
            }
        }

        private void LogCriticalError(ErrorInfo errorInfo)
        {
            string criticalLog = $"CRITICAL ERROR: {errorInfo.errorType}\n" +
                               $"Message: {errorInfo.message}\n" +
                               $"Context: {errorInfo.context}\n" +
                               $"Time: {errorInfo.timestamp}\n" +
                               $"Recovery Attempts: {recoveryAttempts[errorInfo.errorType]}";

            Debug.LogError(criticalLog);

            // Save to persistent storage for debugging
            try
            {
                string logPath = Application.persistentDataPath + "/DaVinciEye_CriticalErrors.log";
                System.IO.File.AppendAllText(logPath, criticalLog + "\n\n");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save critical error log: {e.Message}");
            }
        }

        private void AddToErrorHistory(ErrorInfo errorInfo)
        {
            var record = new ErrorRecord
            {
                errorInfo = errorInfo,
                recoveryAttempted = enableAutoRecovery && CanAttemptRecovery(errorInfo.errorType)
            };

            errorHistory.Add(record);

            // Maintain history size limit
            if (errorHistory.Count > maxErrorHistorySize)
            {
                errorHistory.RemoveAt(0);
            }
        }

        private string GetUserFriendlyMessage(ErrorInfo errorInfo)
        {
            switch (errorInfo.errorType)
            {
                case ErrorType.TrackingLoss:
                    return "Tracking lost. Please move to a well-lit area with good spatial features.";

                case ErrorType.ImageLoadFailure:
                    return "Failed to load image. Please check the file format and try again.";

                case ErrorType.FilterProcessingError:
                    return "Filter processing failed. Filters have been reset.";

                case ErrorType.ColorAnalysisFailure:
                    return "Color analysis failed. Please ensure good lighting and try again.";

                case ErrorType.MemoryPressure:
                    return "Low memory detected. Some features may be temporarily disabled.";

                case ErrorType.PerformanceDegradation:
                    return "Performance issues detected. Quality settings have been adjusted.";

                case ErrorType.UIInteractionFailure:
                    return "UI interaction failed. Please try using voice commands or restart the app.";

                case ErrorType.SpatialAnchorFailure:
                    return "Spatial anchor failed. Please redefine your canvas area.";

                case ErrorType.CameraAccessFailure:
                    return "Camera access failed. Color matching may not work properly.";

                case ErrorType.SessionDataCorruption:
                    return "Session data corrupted. A new session has been created.";

                default:
                    return "An error occurred. The system is attempting to recover.";
            }
        }

        private string GetCriticalErrorMessage(ErrorInfo errorInfo)
        {
            return $"Critical error occurred: {GetUserFriendlyMessage(errorInfo)} " +
                   "Please restart the application if problems persist.";
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current error statistics
        /// </summary>
        public ErrorStatistics GetErrorStatistics()
        {
            var stats = new ErrorStatistics();
            
            foreach (var record in errorHistory)
            {
                stats.totalErrors++;
                
                if (record.recoveryAttempted)
                    stats.recoveryAttempts++;
                
                if (!recoveryAttempts.ContainsKey(record.errorInfo.errorType) || 
                    recoveryAttempts[record.errorInfo.errorType] == 0)
                    stats.successfulRecoveries++;
            }

            stats.errorHistory = new List<ErrorRecord>(errorHistory);
            return stats;
        }

        /// <summary>
        /// Clear error history
        /// </summary>
        public void ClearErrorHistory()
        {
            errorHistory.Clear();
            
            // Reset recovery attempt counters
            foreach (ErrorType errorType in Enum.GetValues(typeof(ErrorType)))
            {
                recoveryAttempts[errorType] = 0;
            }
        }

        /// <summary>
        /// Force recovery attempt for specific error type
        /// </summary>
        public bool ForceRecovery(ErrorType errorType)
        {
            var errorInfo = new ErrorInfo
            {
                errorType = errorType,
                message = "Manual recovery attempt",
                severity = GetErrorSeverity(errorType),
                timestamp = DateTime.Now
            };

            return ExecuteRecoveryStrategy(errorInfo);
        }

        #endregion
    }

    #region Data Structures

    public enum ErrorType
    {
        Unknown,
        TrackingLoss,
        ImageLoadFailure,
        FilterProcessingError,
        ColorAnalysisFailure,
        MemoryPressure,
        PerformanceDegradation,
        UIInteractionFailure,
        SpatialAnchorFailure,
        CameraAccessFailure,
        SessionDataCorruption
    }

    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    [System.Serializable]
    public class ErrorInfo
    {
        public ErrorType errorType;
        public string message;
        public Exception exception;
        public string context;
        public DateTime timestamp;
        public ErrorSeverity severity;
    }

    [System.Serializable]
    public class ErrorRecord
    {
        public ErrorInfo errorInfo;
        public bool recoveryAttempted;
        public bool recoverySuccessful;
    }

    [System.Serializable]
    public class ErrorStatistics
    {
        public int totalErrors;
        public int recoveryAttempts;
        public int successfulRecoveries;
        public List<ErrorRecord> errorHistory;
        
        public float RecoverySuccessRate => 
            recoveryAttempts > 0 ? (float)successfulRecoveries / recoveryAttempts : 0f;
    }

    #endregion
}