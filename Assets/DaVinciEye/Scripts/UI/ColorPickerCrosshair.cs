using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.Input;

namespace DaVinciEye.UI
{
    /// <summary>
    /// Specialized crosshair component for color picking with visual feedback
    /// Provides precise color selection feedback and animation
    /// </summary>
    public class ColorPickerCrosshair : MonoBehaviour
    {
        [Header("Crosshair Visual Components")]
        [SerializeField] private Image crosshairCenter;
        [SerializeField] private Image crosshairHorizontal;
        [SerializeField] private Image crosshairVertical;
        [SerializeField] private Image selectionRing;
        
        [Header("Animation Settings")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseIntensity = 0.3f;
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private bool animateWhenActive = true;
        
        [Header("Color Feedback")]
        [SerializeField] private bool showColorPreview = true;
        [SerializeField] private float colorPreviewSize = 20f;
        [SerializeField] private Image colorPreviewImage;
        
        [Header("Interaction Settings")]
        [SerializeField] private float snapDistance = 10f;
        [SerializeField] private bool enableSnapping = true;
        [SerializeField] private LayerMask snapTargetLayers = -1;
        
        // Animation state
        private float pulseTimer = 0f;
        private float rotationTimer = 0f;
        private Vector3 originalScale;
        private Color originalColor;
        
        // Interaction state
        private Camera targetCamera;
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        
        // Events
        public System.Action<Vector2> OnPositionChanged;
        public System.Action<Color> OnColorChanged;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            targetCamera = Camera.main;
            
            if (crosshairCenter != null)
            {
                originalScale = crosshairCenter.transform.localScale;
                originalColor = crosshairCenter.color;
            }
        }
        
        private void Start()
        {
            SetupCrosshairVisuals();
        }
        
        private void Update()
        {
            if (animateWhenActive && gameObject.activeInHierarchy)
            {
                UpdateAnimations();
            }
        }
        
        private void SetupCrosshairVisuals()
        {
            // Setup crosshair lines
            if (crosshairHorizontal != null)
            {
                crosshairHorizontal.color = Color.white;
                crosshairHorizontal.rectTransform.sizeDelta = new Vector2(40f, 2f);
            }
            
            if (crosshairVertical != null)
            {
                crosshairVertical.color = Color.white;
                crosshairVertical.rectTransform.sizeDelta = new Vector2(2f, 40f);
            }
            
            // Setup center dot
            if (crosshairCenter != null)
            {
                crosshairCenter.color = Color.red;
                crosshairCenter.rectTransform.sizeDelta = Vector2.one * 6f;
            }
            
            // Setup selection ring
            if (selectionRing != null)
            {
                selectionRing.color = new Color(1f, 1f, 1f, 0.5f);
                selectionRing.rectTransform.sizeDelta = Vector2.one * 60f;
                selectionRing.gameObject.SetActive(false);
            }
            
            // Setup color preview
            if (colorPreviewImage != null)
            {
                colorPreviewImage.rectTransform.sizeDelta = Vector2.one * colorPreviewSize;
                colorPreviewImage.rectTransform.anchoredPosition = new Vector2(30f, 30f);
                colorPreviewImage.gameObject.SetActive(showColorPreview);
            }
        }
        
        private void UpdateAnimations()
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            rotationTimer += Time.deltaTime * rotationSpeed;
            
            // Pulse animation for center dot
            if (crosshairCenter != null)
            {
                float pulseScale = 1f + Mathf.Sin(pulseTimer) * pulseIntensity;
                crosshairCenter.transform.localScale = originalScale * pulseScale;
                
                // Color pulse
                float colorPulse = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;
                crosshairCenter.color = Color.Lerp(originalColor, Color.white, colorPulse * 0.3f);
            }
            
            // Rotation animation for selection ring
            if (selectionRing != null && selectionRing.gameObject.activeInHierarchy)
            {
                selectionRing.transform.rotation = Quaternion.Euler(0f, 0f, rotationTimer);
            }
        }
        
        public void SetPosition(Vector2 screenPosition)
        {
            if (rectTransform == null) return;
            
            Vector2 localPosition;
            
            // Convert screen position to local canvas position
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                localPosition = screenPosition;
            }
            else if (parentCanvas != null && targetCamera != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    screenPosition,
                    targetCamera,
                    out localPosition);
            }
            else
            {
                localPosition = screenPosition;
            }
            
            // Apply snapping if enabled
            if (enableSnapping)
            {
                localPosition = ApplySnapping(localPosition);
            }
            
            rectTransform.anchoredPosition = localPosition;
            OnPositionChanged?.Invoke(localPosition);
        }
        
        private Vector2 ApplySnapping(Vector2 position)
        {
            // Simple grid snapping (can be extended for more complex snapping)
            if (snapDistance > 0)
            {
                float snappedX = Mathf.Round(position.x / snapDistance) * snapDistance;
                float snappedY = Mathf.Round(position.y / snapDistance) * snapDistance;
                return new Vector2(snappedX, snappedY);
            }
            
            return position;
        }
        
        public void SetColor(Color color)
        {
            if (colorPreviewImage != null)
            {
                colorPreviewImage.color = color;
            }
            
            // Update crosshair color based on picked color for better visibility
            Color contrastColor = GetContrastColor(color);
            
            if (crosshairHorizontal != null)
                crosshairHorizontal.color = contrastColor;
            if (crosshairVertical != null)
                crosshairVertical.color = contrastColor;
            
            OnColorChanged?.Invoke(color);
        }
        
        private Color GetContrastColor(Color backgroundColor)
        {
            // Calculate luminance to determine if we should use black or white for contrast
            float luminance = 0.299f * backgroundColor.r + 0.587f * backgroundColor.g + 0.114f * backgroundColor.b;
            return luminance > 0.5f ? Color.black : Color.white;
        }
        
        public void ShowSelectionFeedback()
        {
            if (selectionRing != null)
            {
                selectionRing.gameObject.SetActive(true);
                StartCoroutine(HideSelectionFeedbackAfterDelay());
            }
            
            // Trigger selection animation
            if (crosshairCenter != null)
            {
                StartCoroutine(SelectionPulseAnimation());
            }
        }
        
        private System.Collections.IEnumerator HideSelectionFeedbackAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            
            if (selectionRing != null)
            {
                selectionRing.gameObject.SetActive(false);
            }
        }
        
        private System.Collections.IEnumerator SelectionPulseAnimation()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 startScale = crosshairCenter.transform.localScale;
            Vector3 targetScale = startScale * 1.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // Scale animation
                float scaleProgress = Mathf.Sin(progress * Mathf.PI);
                crosshairCenter.transform.localScale = Vector3.Lerp(startScale, targetScale, scaleProgress);
                
                // Color animation
                Color flashColor = Color.Lerp(originalColor, Color.yellow, scaleProgress);
                crosshairCenter.color = flashColor;
                
                yield return null;
            }
            
            // Reset to original state
            crosshairCenter.transform.localScale = startScale;
            crosshairCenter.color = originalColor;
        }
        
        public void SetVisibility(bool visible)
        {
            gameObject.SetActive(visible);
        }
        
        public void SetAnimationEnabled(bool enabled)
        {
            animateWhenActive = enabled;
        }
        
        public void SetColorPreviewEnabled(bool enabled)
        {
            showColorPreview = enabled;
            
            if (colorPreviewImage != null)
            {
                colorPreviewImage.gameObject.SetActive(enabled);
            }
        }
        
        public void SetSnapDistance(float distance)
        {
            snapDistance = distance;
        }
        
        public void SetSnappingEnabled(bool enabled)
        {
            enableSnapping = enabled;
        }
        
        // Properties
        public Vector2 Position => rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        public Color CurrentColor => colorPreviewImage != null ? colorPreviewImage.color : Color.white;
        public bool IsVisible => gameObject.activeInHierarchy;
        public bool AnimationEnabled => animateWhenActive;
        public bool ColorPreviewEnabled => showColorPreview;
        public float SnapDistance => snapDistance;
        public bool SnappingEnabled => enableSnapping;
    }
}