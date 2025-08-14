using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DaVinciEye.SpatialTracking
{
    /// <summary>
    /// Visual indicator component that displays tracking quality status to the user
    /// Works with TrackingQualityMonitor to provide real-time visual feedback
    /// </summary>
    public class TrackingQualityIndicator : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private Image statusIndicator;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject warningPanel;
        [SerializeField] private TextMeshProUGUI warningText;
        
        [Header("Animation Settings")]
        [SerializeField] private bool animateIndicator = true;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float warningFadeDuration = 0.5f;
        
        // Private fields
        private TrackingQualityMonitor trackingMonitor;
        private Coroutine warningAnimationCoroutine;
        private Color originalIndicatorColor;
        private bool isWarningVisible;
        
        private void Start()
        {
            InitializeIndicator();
            FindAndConnectTrackingMonitor();
        }
        
        private void OnDestroy()
        {
            DisconnectFromTrackingMonitor();
        }
        
        /// <summary>
        /// Initialize the visual indicator components
        /// </summary>
        private void InitializeIndicator()
        {
            if (statusIndicator != null)
            {
                originalIndicatorColor = statusIndicator.color;
            }
            
            if (warningPanel != null)
            {
                warningPanel.SetActive(false);
                isWarningVisible = false;
            }
            
            // Set initial status
            UpdateVisualStatus(TrackingQuality.Good, "Tracking: Good");
        }
        
        /// <summary>
        /// Find and connect to the TrackingQualityMonitor
        /// </summary>
        private void FindAndConnectTrackingMonitor()
        {
            trackingMonitor = FindObjectOfType<TrackingQualityMonitor>();
            
            if (trackingMonitor != null)
            {
                // Subscribe to tracking events
                trackingMonitor.OnTrackingQualityChanged += OnTrackingQualityChanged;
                trackingMonitor.OnTrackingStabilityChanged += OnTrackingStabilityChanged;
                trackingMonitor.OnTrackingWarning += OnTrackingWarning;
                
                // Update initial state
                OnTrackingQualityChanged(trackingMonitor.CurrentTrackingQuality);
                
                Debug.Log("TrackingQualityIndicator: Connected to TrackingQualityMonitor");
            }
            else
            {
                Debug.LogWarning("TrackingQualityIndicator: TrackingQualityMonitor not found in scene");
            }
        }
        
        /// <summary>
        /// Disconnect from the TrackingQualityMonitor
        /// </summary>
        private void DisconnectFromTrackingMonitor()
        {
            if (trackingMonitor != null)
            {
                trackingMonitor.OnTrackingQualityChanged -= OnTrackingQualityChanged;
                trackingMonitor.OnTrackingStabilityChanged -= OnTrackingStabilityChanged;
                trackingMonitor.OnTrackingWarning -= OnTrackingWarning;
            }
        }
        
        /// <summary>
        /// Handle tracking quality changes
        /// </summary>
        private void OnTrackingQualityChanged(TrackingQuality quality)
        {
            string statusMessage = quality switch
            {
                TrackingQuality.Good => "Tracking: Excellent",
                TrackingQuality.Fair => "Tracking: Fair",
                TrackingQuality.Poor => "Tracking: Poor",
                _ => "Tracking: Unknown"
            };
            
            UpdateVisualStatus(quality, statusMessage);
            
            // Start pulsing animation for poor tracking
            if (quality == TrackingQuality.Poor && animateIndicator)
            {
                StartPulsingAnimation();
            }
            else
            {
                StopPulsingAnimation();
            }
        }
        
        /// <summary>
        /// Handle tracking stability changes
        /// </summary>
        private void OnTrackingStabilityChanged(bool isStable)
        {
            if (!isStable && statusText != null)
            {
                statusText.text += " (Unstable)";
            }
        }
        
        /// <summary>
        /// Handle tracking warnings
        /// </summary>
        private void OnTrackingWarning(string warningMessage)
        {
            ShowWarningMessage(warningMessage);
        }
        
        /// <summary>
        /// Update the visual status display
        /// </summary>
        private void UpdateVisualStatus(TrackingQuality quality, string message)
        {
            // Update status indicator color
            if (statusIndicator != null && trackingMonitor != null)
            {
                statusIndicator.color = trackingMonitor.GetTrackingQualityColor();
            }
            
            // Update status text
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = GetTextColorForQuality(quality);
            }
        }
        
        /// <summary>
        /// Get appropriate text color for tracking quality
        /// </summary>
        private Color GetTextColorForQuality(TrackingQuality quality)
        {
            return quality switch
            {
                TrackingQuality.Good => Color.green,
                TrackingQuality.Fair => Color.yellow,
                TrackingQuality.Poor => Color.red,
                _ => Color.white
            };
        }
        
        /// <summary>
        /// Show warning message to user
        /// </summary>
        private void ShowWarningMessage(string message)
        {
            if (warningPanel != null && warningText != null)
            {
                warningText.text = message;
                
                if (warningAnimationCoroutine != null)
                {
                    StopCoroutine(warningAnimationCoroutine);
                }
                
                warningAnimationCoroutine = StartCoroutine(ShowWarningAnimation());
            }
        }
        
        /// <summary>
        /// Animate warning message display
        /// </summary>
        private System.Collections.IEnumerator ShowWarningAnimation()
        {
            // Fade in warning
            warningPanel.SetActive(true);
            isWarningVisible = true;
            
            CanvasGroup canvasGroup = warningPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = warningPanel.AddComponent<CanvasGroup>();
            }
            
            // Fade in
            float elapsedTime = 0f;
            while (elapsedTime < warningFadeDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / warningFadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            
            // Wait for display duration
            yield return new WaitForSeconds(3f);
            
            // Fade out
            elapsedTime = 0f;
            while (elapsedTime < warningFadeDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / warningFadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            
            warningPanel.SetActive(false);
            isWarningVisible = false;
            warningAnimationCoroutine = null;
        }
        
        /// <summary>
        /// Start pulsing animation for poor tracking
        /// </summary>
        private void StartPulsingAnimation()
        {
            if (statusIndicator != null)
            {
                StopPulsingAnimation();
                StartCoroutine(PulseIndicator());
            }
        }
        
        /// <summary>
        /// Stop pulsing animation
        /// </summary>
        private void StopPulsingAnimation()
        {
            StopAllCoroutines();
            if (statusIndicator != null)
            {
                statusIndicator.color = trackingMonitor?.GetTrackingQualityColor() ?? originalIndicatorColor;
            }
        }
        
        /// <summary>
        /// Pulse indicator animation coroutine
        /// </summary>
        private System.Collections.IEnumerator PulseIndicator()
        {
            Color baseColor = trackingMonitor?.GetTrackingQualityColor() ?? Color.red;
            Color pulseColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.3f);
            
            while (true)
            {
                // Pulse to dim
                float elapsedTime = 0f;
                float pulseDuration = 1f / pulseSpeed;
                
                while (elapsedTime < pulseDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / pulseDuration;
                    statusIndicator.color = Color.Lerp(baseColor, pulseColor, Mathf.Sin(t * Mathf.PI));
                    yield return null;
                }
                
                yield return null;
            }
        }
        
        /// <summary>
        /// Manually hide warning (can be called by UI button)
        /// </summary>
        public void HideWarning()
        {
            if (warningAnimationCoroutine != null)
            {
                StopCoroutine(warningAnimationCoroutine);
                warningAnimationCoroutine = null;
            }
            
            if (warningPanel != null)
            {
                warningPanel.SetActive(false);
                isWarningVisible = false;
            }
        }
        
        /// <summary>
        /// Get current tracking quality for external access
        /// </summary>
        public TrackingQuality GetCurrentTrackingQuality()
        {
            return trackingMonitor?.CurrentTrackingQuality ?? TrackingQuality.Good;
        }
        
        /// <summary>
        /// Check if warning is currently visible
        /// </summary>
        public bool IsWarningVisible => isWarningVisible;
    }
}