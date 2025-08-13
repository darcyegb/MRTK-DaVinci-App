using System;
using UnityEngine;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;

namespace DaVinciEye.ImageOverlay
{
    /// <summary>
    /// Controls image overlay opacity with real-time transparency adjustment
    /// Integrates with MRTK UI components for gesture-based interaction
    /// </summary>
    public class OpacityController : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Slider opacitySlider;
        [SerializeField] private PinchSlider mrtkPinchSlider;
        
        [Header("Target References")]
        [SerializeField] private IImageOverlay imageOverlay;
        [SerializeField] private Material targetMaterial;
        [SerializeField] private Renderer targetRenderer;
        
        [Header("Configuration")]
        [SerializeField] private float minOpacity = 0.0f;
        [SerializeField] private float maxOpacity = 1.0f;
        [SerializeField] private float defaultOpacity = 1.0f;
        [SerializeField] private bool updateInRealTime = true;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject opacityIndicator;
        [SerializeField] private TextMesh opacityValueText;
        
        // Private fields
        private float currentOpacity;
        private bool isInitialized = false;
        
        // Events
        public event Action<float> OnOpacityChanged;
        
        // Properties
        public float CurrentOpacity => currentOpacity;
        public float MinOpacity => minOpacity;
        public float MaxOpacity => maxOpacity;
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            SetupUIComponents();
            SetOpacity(defaultOpacity);
        }
        
        /// <summary>
        /// Initializes the opacity controller
        /// </summary>
        private void Initialize()
        {
            currentOpacity = defaultOpacity;
            
            // Find image overlay if not assigned
            if (imageOverlay == null)
            {
                var overlayManager = FindObjectOfType<ImageOverlayManager>();
                if (overlayManager != null)
                {
                    imageOverlay = overlayManager;
                }
            }
            
            isInitialized = true;
            Debug.Log("OpacityController: Initialized successfully");
        }
        
        /// <summary>
        /// Sets up UI component event handlers
        /// </summary>
        private void SetupUIComponents()
        {
            // Setup Unity UI Slider
            if (opacitySlider != null)
            {
                opacitySlider.minValue = minOpacity;
                opacitySlider.maxValue = maxOpacity;
                opacitySlider.value = currentOpacity;
                opacitySlider.onValueChanged.AddListener(OnSliderValueChanged);
            }
            
            // Setup MRTK PinchSlider
            if (mrtkPinchSlider != null)
            {
                mrtkPinchSlider.SliderValue = currentOpacity;
                mrtkPinchSlider.OnValueUpdated.AddListener(OnMRTKSliderValueChanged);
            }
            
            Debug.Log("OpacityController: UI components configured");
        }
        
        /// <summary>
        /// Handles Unity UI slider value changes
        /// </summary>
        private void OnSliderValueChanged(float value)
        {
            if (updateInRealTime)
            {
                SetOpacity(value);
            }
        }
        
        /// <summary>
        /// Handles MRTK PinchSlider value changes
        /// </summary>
        private void OnMRTKSliderValueChanged(SliderEventData eventData)
        {
            if (updateInRealTime)
            {
                SetOpacity(eventData.NewValue);
            }
        }
        
        /// <summary>
        /// Sets the opacity value and updates all connected systems
        /// </summary>
        public void SetOpacity(float opacity)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("OpacityController: Not initialized");
                return;
            }
            
            // Clamp opacity to valid range
            float clampedOpacity = Mathf.Clamp(opacity, minOpacity, maxOpacity);
            
            if (Mathf.Approximately(currentOpacity, clampedOpacity))
            {
                return; // No change needed
            }
            
            currentOpacity = clampedOpacity;
            
            // Update image overlay
            if (imageOverlay != null)
            {
                imageOverlay.SetOpacity(currentOpacity);
            }
            
            // Update material directly if available
            if (targetMaterial != null)
            {
                UpdateMaterialOpacity(targetMaterial, currentOpacity);
            }
            
            // Update renderer material if available
            if (targetRenderer != null && targetRenderer.material != null)
            {
                UpdateMaterialOpacity(targetRenderer.material, currentOpacity);
            }
            
            // Update UI components
            UpdateUIComponents();
            
            // Update visual feedback
            UpdateVisualFeedback();
            
            // Fire event
            OnOpacityChanged?.Invoke(currentOpacity);
            
            Debug.Log($"OpacityController: Opacity set to {currentOpacity:F2}");
        }
        
        /// <summary>
        /// Updates material opacity properties
        /// </summary>
        private void UpdateMaterialOpacity(Material material, float opacity)
        {
            if (material == null) return;
            
            // Update main color alpha
            Color color = material.color;
            color.a = opacity;
            material.color = color;
            
            // Update alpha property if available (for MRTK Graphics Tools shaders)
            if (material.HasProperty("_Alpha"))
            {
                material.SetFloat("_Alpha", opacity);
            }
            
            // Update base color alpha if available (for URP shaders)
            if (material.HasProperty("_BaseColor"))
            {
                Color baseColor = material.GetColor("_BaseColor");
                baseColor.a = opacity;
                material.SetColor("_BaseColor", baseColor);
            }
            
            // Update transparency mode for proper rendering
            UpdateMaterialTransparencyMode(material, opacity);
        }
        
        /// <summary>
        /// Updates material transparency rendering mode
        /// </summary>
        private void UpdateMaterialTransparencyMode(Material material, float opacity)
        {
            if (material == null) return;
            
            // Set rendering mode based on opacity
            bool isTransparent = opacity < 1.0f;
            
            if (material.HasProperty("_Mode"))
            {
                // Standard shader mode
                material.SetFloat("_Mode", isTransparent ? 3 : 0); // 3 = Transparent, 0 = Opaque
            }
            
            if (material.HasProperty("_SrcBlend") && material.HasProperty("_DstBlend"))
            {
                if (isTransparent)
                {
                    material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }
                else
                {
                    material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                }
            }
            
            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", isTransparent ? 0 : 1);
            }
            
            // Update render queue
            material.renderQueue = isTransparent ? 3000 : 2000; // Transparent vs Geometry queue
        }
        
        /// <summary>
        /// Updates UI component values without triggering events
        /// </summary>
        private void UpdateUIComponents()
        {
            // Update Unity UI Slider
            if (opacitySlider != null)
            {
                opacitySlider.SetValueWithoutNotify(currentOpacity);
            }
            
            // Update MRTK PinchSlider
            if (mrtkPinchSlider != null)
            {
                mrtkPinchSlider.SliderValue = currentOpacity;
            }
        }
        
        /// <summary>
        /// Updates visual feedback elements
        /// </summary>
        private void UpdateVisualFeedback()
        {
            // Update opacity indicator visibility
            if (opacityIndicator != null)
            {
                opacityIndicator.SetActive(currentOpacity < maxOpacity);
            }
            
            // Update opacity value text
            if (opacityValueText != null)
            {
                opacityValueText.text = $"{(currentOpacity * 100):F0}%";
            }
        }
        
        /// <summary>
        /// Increases opacity by a fixed amount
        /// </summary>
        public void IncreaseOpacity(float amount = 0.1f)
        {
            SetOpacity(currentOpacity + amount);
        }
        
        /// <summary>
        /// Decreases opacity by a fixed amount
        /// </summary>
        public void DecreaseOpacity(float amount = 0.1f)
        {
            SetOpacity(currentOpacity - amount);
        }
        
        /// <summary>
        /// Resets opacity to default value
        /// </summary>
        public void ResetOpacity()
        {
            SetOpacity(defaultOpacity);
        }
        
        /// <summary>
        /// Sets opacity to minimum (fully transparent)
        /// </summary>
        public void SetFullyTransparent()
        {
            SetOpacity(minOpacity);
        }
        
        /// <summary>
        /// Sets opacity to maximum (fully opaque)
        /// </summary>
        public void SetFullyOpaque()
        {
            SetOpacity(maxOpacity);
        }
        
        /// <summary>
        /// Validates opacity value is within acceptable range
        /// </summary>
        public bool IsValidOpacity(float opacity)
        {
            return opacity >= minOpacity && opacity <= maxOpacity;
        }
        
        /// <summary>
        /// Sets the target image overlay reference
        /// </summary>
        public void SetImageOverlay(IImageOverlay overlay)
        {
            imageOverlay = overlay;
        }
        
        /// <summary>
        /// Sets the target material reference
        /// </summary>
        public void SetTargetMaterial(Material material)
        {
            targetMaterial = material;
        }
        
        /// <summary>
        /// Sets the target renderer reference
        /// </summary>
        public void SetTargetRenderer(Renderer renderer)
        {
            targetRenderer = renderer;
        }
        
        private void OnDestroy()
        {
            // Clean up event handlers
            if (opacitySlider != null)
            {
                opacitySlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
            
            if (mrtkPinchSlider != null)
            {
                mrtkPinchSlider.OnValueUpdated.RemoveListener(OnMRTKSliderValueChanged);
            }
        }
    }
}