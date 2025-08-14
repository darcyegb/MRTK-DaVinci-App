using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DaVinciEye.ErrorHandling;

namespace DaVinciEye.UI.ErrorHandling
{
    /// <summary>
    /// User-friendly error notification UI system
    /// Displays error messages and recovery suggestions to users
    /// </summary>
    public class UserErrorNotificationUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject errorNotificationPanel;
        [SerializeField] private TextMeshProUGUI errorMessageText;
        [SerializeField] private TextMeshProUGUI errorTitleText;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Image errorIconImage;

        [Header("Error Icons")]
        [SerializeField] private Sprite warningIcon;
        [SerializeField] private Sprite errorIcon;
        [SerializeField] private Sprite criticalIcon;
        [SerializeField] private Sprite infoIcon;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        [SerializeField] private float autoHideDelay = 5f; // Auto-hide info messages

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip warningSound;
        [SerializeField] private AudioClip errorSound;
        [SerializeField] private AudioClip criticalSound;

        private AudioSource audioSource;
        private CanvasGroup canvasGroup;
        private Coroutine autoHideCoroutine;
        private ErrorInfo currentError;

        private void Awake()
        {
            InitializeComponents();
            SetupEventListeners();
        }

        private void Start()
        {
            // Subscribe to error manager events
            if (ErrorManager.Instance != null)
            {
                ErrorManager.Instance.OnUserMessageRequired.AddListener(ShowErrorMessage);
                ErrorManager.Instance.OnErrorOccurred.AddListener(ShowErrorNotification);
                ErrorManager.Instance.OnErrorRecovered.AddListener(ShowRecoveryNotification);
            }

            // Hide panel initially
            HideNotification(immediate: true);
        }

        private void InitializeComponents()
        {
            // Get or add canvas group for fading
            canvasGroup = errorNotificationPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = errorNotificationPanel.AddComponent<CanvasGroup>();
            }

            // Get or add audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
        }

        private void SetupEventListeners()
        {
            if (dismissButton != null)
            {
                dismissButton.onClick.AddListener(DismissNotification);
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(RetryOperation);
            }

            if (helpButton != null)
            {
                helpButton.onClick.AddListener(ShowHelp);
            }
        }

        /// <summary>
        /// Show a simple error message (legacy support)
        /// </summary>
        public void ShowErrorMessage(string message)
        {
            var errorInfo = new ErrorInfo
            {
                errorType = ErrorType.Unknown,
                message = message,
                severity = ErrorSeverity.Warning,
                timestamp = System.DateTime.Now
            };

            ShowErrorNotification(errorInfo);
        }

        /// <summary>
        /// Show detailed error notification
        /// </summary>
        public void ShowErrorNotification(ErrorInfo errorInfo)
        {
            currentError = errorInfo;

            // Set error content
            SetErrorContent(errorInfo);

            // Configure UI based on severity
            ConfigureUIForSeverity(errorInfo.severity);

            // Play audio feedback
            PlayAudioFeedback(errorInfo.severity);

            // Show the notification
            ShowNotification();

            // Auto-hide for info messages
            if (errorInfo.severity == ErrorSeverity.Info)
            {
                StartAutoHide();
            }
        }

        /// <summary>
        /// Show recovery success notification
        /// </summary>
        public void ShowRecoveryNotification(ErrorInfo errorInfo)
        {
            var recoveryInfo = new ErrorInfo
            {
                errorType = errorInfo.errorType,
                message = $"Recovered from {GetErrorTypeDisplayName(errorInfo.errorType)}",
                severity = ErrorSeverity.Info,
                timestamp = System.DateTime.Now
            };

            ShowErrorNotification(recoveryInfo);
        }

        private void SetErrorContent(ErrorInfo errorInfo)
        {
            // Set title based on severity
            if (errorTitleText != null)
            {
                errorTitleText.text = GetErrorTitle(errorInfo.severity);
            }

            // Set main message
            if (errorMessageText != null)
            {
                string displayMessage = GetDisplayMessage(errorInfo);
                errorMessageText.text = displayMessage;
            }

            // Set icon
            if (errorIconImage != null)
            {
                errorIconImage.sprite = GetErrorIcon(errorInfo.severity);
            }
        }

        private void ConfigureUIForSeverity(ErrorSeverity severity)
        {
            // Configure button visibility
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(severity >= ErrorSeverity.Warning);
            }

            if (helpButton != null)
            {
                helpButton.gameObject.SetActive(severity >= ErrorSeverity.Error);
            }

            // Configure colors based on severity
            Color panelColor = GetSeverityColor(severity);
            if (errorNotificationPanel.TryGetComponent<Image>(out Image panelImage))
            {
                panelImage.color = panelColor;
            }
        }

        private void ShowNotification()
        {
            // Cancel any existing auto-hide
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }

            // Show panel
            errorNotificationPanel.SetActive(true);

            // Fade in animation
            StartCoroutine(FadeIn());
        }

        private void HideNotification(bool immediate = false)
        {
            if (immediate)
            {
                errorNotificationPanel.SetActive(false);
                canvasGroup.alpha = 0f;
            }
            else
            {
                StartCoroutine(FadeOut());
            }
        }

        private void StartAutoHide()
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }

            autoHideCoroutine = StartCoroutine(AutoHideCoroutine());
        }

        private IEnumerator AutoHideCoroutine()
        {
            yield return new WaitForSeconds(autoHideDelay);
            HideNotification();
            autoHideCoroutine = null;
        }

        private IEnumerator FadeIn()
        {
            float elapsedTime = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeInDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, progress);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            float elapsedTime = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeOutDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            errorNotificationPanel.SetActive(false);
        }

        private void PlayAudioFeedback(ErrorSeverity severity)
        {
            if (audioSource == null) return;

            AudioClip clipToPlay = null;

            switch (severity)
            {
                case ErrorSeverity.Warning:
                    clipToPlay = warningSound;
                    break;
                case ErrorSeverity.Error:
                    clipToPlay = errorSound;
                    break;
                case ErrorSeverity.Critical:
                    clipToPlay = criticalSound;
                    break;
            }

            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }

        #region UI Event Handlers

        private void DismissNotification()
        {
            HideNotification();
        }

        private void RetryOperation()
        {
            if (currentError != null && ErrorManager.Instance != null)
            {
                // Attempt recovery
                bool recoverySuccess = ErrorManager.Instance.ForceRecovery(currentError.errorType);
                
                if (recoverySuccess)
                {
                    ShowRecoveryNotification(currentError);
                }
                else
                {
                    // Show retry failed message
                    var retryFailedError = new ErrorInfo
                    {
                        errorType = currentError.errorType,
                        message = "Retry failed. Please try again or restart the application.",
                        severity = ErrorSeverity.Error,
                        timestamp = System.DateTime.Now
                    };
                    ShowErrorNotification(retryFailedError);
                }
            }

            HideNotification();
        }

        private void ShowHelp()
        {
            if (currentError != null)
            {
                string helpMessage = GetDetailedHelpMessage(currentError.errorType);
                
                var helpError = new ErrorInfo
                {
                    errorType = currentError.errorType,
                    message = helpMessage,
                    severity = ErrorSeverity.Info,
                    timestamp = System.DateTime.Now
                };
                
                ShowErrorNotification(helpError);
            }
        }

        #endregion

        #region Helper Methods

        private string GetErrorTitle(ErrorSeverity severity)
        {
            switch (severity)
            {
                case ErrorSeverity.Critical:
                    return "Critical Error";
                case ErrorSeverity.Error:
                    return "Error";
                case ErrorSeverity.Warning:
                    return "Warning";
                case ErrorSeverity.Info:
                    return "Information";
                default:
                    return "Notification";
            }
        }

        private string GetDisplayMessage(ErrorInfo errorInfo)
        {
            // Use user-friendly message if available, otherwise use technical message
            string baseMessage = GetUserFriendlyErrorMessage(errorInfo.errorType);
            
            if (string.IsNullOrEmpty(baseMessage))
            {
                baseMessage = errorInfo.message;
            }

            // Add recovery suggestion if appropriate
            if (errorInfo.severity >= ErrorSeverity.Warning)
            {
                string recoverySuggestion = GetRecoverySuggestion(errorInfo.errorType);
                if (!string.IsNullOrEmpty(recoverySuggestion))
                {
                    baseMessage += "\n\n" + recoverySuggestion;
                }
            }

            return baseMessage;
        }

        private string GetUserFriendlyErrorMessage(ErrorType errorType)
        {
            switch (errorType)
            {
                case ErrorType.TrackingLoss:
                    return "Spatial tracking has been lost. The overlay may not align properly with your canvas.";

                case ErrorType.ImageLoadFailure:
                    return "Unable to load the selected image. The file may be corrupted or in an unsupported format.";

                case ErrorType.FilterProcessingError:
                    return "Image filter processing failed. Filters have been reset to prevent further issues.";

                case ErrorType.ColorAnalysisFailure:
                    return "Color analysis could not be completed. This may be due to poor lighting conditions.";

                case ErrorType.MemoryPressure:
                    return "The application is running low on memory. Some features may be temporarily disabled.";

                case ErrorType.PerformanceDegradation:
                    return "Performance issues detected. Quality settings have been automatically adjusted.";

                case ErrorType.UIInteractionFailure:
                    return "User interface interaction failed. Try using voice commands or gestures.";

                case ErrorType.SpatialAnchorFailure:
                    return "Unable to save or restore canvas position. You may need to redefine your canvas.";

                case ErrorType.CameraAccessFailure:
                    return "Camera access failed. Color matching features may not work properly.";

                case ErrorType.SessionDataCorruption:
                    return "Session data was corrupted and has been reset. Your previous settings may be lost.";

                default:
                    return "";
            }
        }

        private string GetRecoverySuggestion(ErrorType errorType)
        {
            switch (errorType)
            {
                case ErrorType.TrackingLoss:
                    return "Try moving to a well-lit area with distinct visual features. Avoid reflective surfaces.";

                case ErrorType.ImageLoadFailure:
                    return "Try selecting a different image or check that the file is not corrupted.";

                case ErrorType.FilterProcessingError:
                    return "Try applying filters one at a time or use a smaller image.";

                case ErrorType.ColorAnalysisFailure:
                    return "Ensure good lighting and avoid shadows when analyzing colors.";

                case ErrorType.MemoryPressure:
                    return "Try closing other applications or using smaller images.";

                case ErrorType.PerformanceDegradation:
                    return "Reduce the number of active filters or use lower resolution images.";

                case ErrorType.UIInteractionFailure:
                    return "Try using voice commands like 'Select' or 'Back' if gestures aren't working.";

                case ErrorType.SpatialAnchorFailure:
                    return "Try redefining your canvas in a different location with better tracking.";

                case ErrorType.CameraAccessFailure:
                    return "Check camera permissions in system settings and restart the application.";

                case ErrorType.SessionDataCorruption:
                    return "Your settings have been reset to defaults. You can reconfigure them as needed.";

                default:
                    return "Try restarting the application if the problem persists.";
            }
        }

        private string GetDetailedHelpMessage(ErrorType errorType)
        {
            switch (errorType)
            {
                case ErrorType.TrackingLoss:
                    return "Spatial Tracking Help:\n\n" +
                           "• Move to a well-lit area\n" +
                           "• Look for areas with distinct visual features\n" +
                           "• Avoid reflective surfaces like mirrors or glass\n" +
                           "• Keep the HoloLens sensors clean\n" +
                           "• Move slowly to allow tracking to recover";

                case ErrorType.ImageLoadFailure:
                    return "Image Loading Help:\n\n" +
                           "• Supported formats: JPG, PNG, BMP\n" +
                           "• Maximum size: 2048x2048 pixels\n" +
                           "• Ensure file is not corrupted\n" +
                           "• Try a different image file\n" +
                           "• Check available storage space";

                case ErrorType.ColorAnalysisFailure:
                    return "Color Analysis Help:\n\n" +
                           "• Ensure good, even lighting\n" +
                           "• Avoid shadows on the paint surface\n" +
                           "• Hold steady when analyzing colors\n" +
                           "• Clean the HoloLens cameras\n" +
                           "• Use matte paint surfaces when possible";

                default:
                    return "For additional help, please refer to the user manual or contact support.";
            }
        }

        private string GetErrorTypeDisplayName(ErrorType errorType)
        {
            switch (errorType)
            {
                case ErrorType.TrackingLoss:
                    return "tracking loss";
                case ErrorType.ImageLoadFailure:
                    return "image loading error";
                case ErrorType.FilterProcessingError:
                    return "filter processing error";
                case ErrorType.ColorAnalysisFailure:
                    return "color analysis error";
                case ErrorType.MemoryPressure:
                    return "memory pressure";
                case ErrorType.PerformanceDegradation:
                    return "performance issues";
                case ErrorType.UIInteractionFailure:
                    return "UI interaction error";
                case ErrorType.SpatialAnchorFailure:
                    return "spatial anchor error";
                case ErrorType.CameraAccessFailure:
                    return "camera access error";
                case ErrorType.SessionDataCorruption:
                    return "session data corruption";
                default:
                    return "system error";
            }
        }

        private Sprite GetErrorIcon(ErrorSeverity severity)
        {
            switch (severity)
            {
                case ErrorSeverity.Critical:
                    return criticalIcon;
                case ErrorSeverity.Error:
                    return errorIcon;
                case ErrorSeverity.Warning:
                    return warningIcon;
                case ErrorSeverity.Info:
                    return infoIcon;
                default:
                    return infoIcon;
            }
        }

        private Color GetSeverityColor(ErrorSeverity severity)
        {
            switch (severity)
            {
                case ErrorSeverity.Critical:
                    return new Color(0.8f, 0.1f, 0.1f, 0.9f); // Dark red
                case ErrorSeverity.Error:
                    return new Color(0.9f, 0.3f, 0.3f, 0.9f); // Red
                case ErrorSeverity.Warning:
                    return new Color(0.9f, 0.7f, 0.2f, 0.9f); // Orange
                case ErrorSeverity.Info:
                    return new Color(0.2f, 0.6f, 0.9f, 0.9f); // Blue
                default:
                    return new Color(0.5f, 0.5f, 0.5f, 0.9f); // Gray
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Show a custom error message with specified severity
        /// </summary>
        public void ShowCustomError(string title, string message, ErrorSeverity severity)
        {
            var customError = new ErrorInfo
            {
                errorType = ErrorType.Unknown,
                message = message,
                severity = severity,
                timestamp = System.DateTime.Now
            };

            if (errorTitleText != null)
            {
                errorTitleText.text = title;
            }

            ShowErrorNotification(customError);
        }

        /// <summary>
        /// Hide any currently displayed notification
        /// </summary>
        public void HideCurrentNotification()
        {
            HideNotification();
        }

        /// <summary>
        /// Check if a notification is currently being displayed
        /// </summary>
        public bool IsNotificationVisible()
        {
            return errorNotificationPanel.activeInHierarchy && canvasGroup.alpha > 0f;
        }

        #endregion
    }
}