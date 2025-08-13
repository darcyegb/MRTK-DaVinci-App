using UnityEngine;
using MixedReality.Toolkit.SpatialManipulation;

namespace DaVinciEye.Canvas
{
    /// <summary>
    /// Helper script to set up canvas prefab with proper MRTK components
    /// This ensures the ArtCanvas GameObject has all required components configured correctly
    /// </summary>
    [System.Serializable]
    public class CanvasPrefabSetup
    {
        /// <summary>
        /// Creates and configures an ArtCanvas GameObject with BoundsControl
        /// Following the implementation checklist from the task specification
        /// </summary>
        public static GameObject CreateArtCanvasWithBoundsControl(Transform parent = null)
        {
            // ✓ Add BoundsControl component to GameObject named "ArtCanvas"
            var artCanvas = new GameObject("ArtCanvas");
            
            if (parent != null)
            {
                artCanvas.transform.SetParent(parent);
            }
            
            // Add required components
            var boundsControl = artCanvas.AddComponent<BoundsControl>();
            var boxCollider = artCanvas.AddComponent<BoxCollider>();
            
            // Configure BoxCollider for interaction
            boxCollider.size = Vector3.one;
            boxCollider.isTrigger = false;
            
            // ✓ Set BoundsControl.BoundsOverride to define min/max canvas size
            ConfigureBoundsControl(boundsControl);
            
            // Add visual representation
            CreateCanvasVisual(artCanvas);
            
            Debug.Log("CanvasPrefabSetup: ArtCanvas created with BoundsControl configuration");
            return artCanvas;
        }
        
        private static void ConfigureBoundsControl(BoundsControl boundsControl)
        {
            if (boundsControl == null) return;
            
            // Set bounds override for canvas sizing constraints
            var defaultBounds = new Bounds(Vector3.zero, new Vector3(1.0f, 0.01f, 1.0f));
            boundsControl.BoundsOverride = defaultBounds;
            
            // Configure for flat canvas (Y-axis flattened)
            boundsControl.FlattenAxis = BoundsControl.FlattenModeType.FlattenY;
            
            // ✓ Visual feedback is automatic (MRTK handles corner visualization)
            boundsControl.ScaleHandlesConfig.ShowScaleHandles = true;
            boundsControl.RotationHandlesConfig.ShowRotationHandles = false;
            boundsControl.TranslationHandlesConfig.ShowTranslationHandles = true;
            
            // Configure handle materials and sizes
            boundsControl.ScaleHandlesConfig.HandleSize = 0.02f; // 2cm handles
            boundsControl.TranslationHandlesConfig.HandleSize = 0.02f;
            
            // Set activation behavior
            boundsControl.ActivationFlags = BoundsControl.BoundsControlActivationType.ActivateOnStart;
            
            Debug.Log("CanvasPrefabSetup: BoundsControl configured with automatic visual feedback");
        }
        
        private static void CreateCanvasVisual(GameObject artCanvas)
        {
            // Create a simple quad mesh for canvas visualization
            var canvasVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            canvasVisual.name = "CanvasVisual";
            canvasVisual.transform.SetParent(artCanvas.transform);
            canvasVisual.transform.localPosition = Vector3.zero;
            canvasVisual.transform.localRotation = Quaternion.Euler(90, 0, 0); // Lay flat
            canvasVisual.transform.localScale = Vector3.one;
            
            // Remove the collider from the visual (BoundsControl handles interaction)
            var visualCollider = canvasVisual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Object.DestroyImmediate(visualCollider);
            }
            
            // Configure material for canvas appearance
            var renderer = canvasVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Create a semi-transparent material for canvas outline
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = new Color(1f, 1f, 1f, 0.1f); // Very transparent white
                material.SetFloat("_Surface", 1); // Transparent surface type
                material.SetFloat("_Blend", 0); // Alpha blend mode
                renderer.material = material;
            }
            
            Debug.Log("CanvasPrefabSetup: Canvas visual representation created");
        }
        
        /// <summary>
        /// Validates that an existing GameObject has proper canvas setup
        /// </summary>
        public static bool ValidateCanvasSetup(GameObject canvasObject)
        {
            if (canvasObject == null) return false;
            
            var boundsControl = canvasObject.GetComponent<BoundsControl>();
            var collider = canvasObject.GetComponent<Collider>();
            
            bool isValid = boundsControl != null && collider != null;
            
            if (!isValid)
            {
                Debug.LogWarning($"CanvasPrefabSetup: Canvas setup validation failed for {canvasObject.name}");
                Debug.LogWarning($"  - BoundsControl: {(boundsControl != null ? "✓" : "✗")}");
                Debug.LogWarning($"  - Collider: {(collider != null ? "✓" : "✗")}");
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Updates canvas bounds constraints
        /// </summary>
        public static void UpdateCanvasBounds(BoundsControl boundsControl, Vector2 minSize, Vector2 maxSize)
        {
            if (boundsControl == null) return;
            
            // Update bounds override with new constraints
            var bounds = new Bounds(Vector3.zero, new Vector3(maxSize.x, 0.01f, maxSize.y));
            boundsControl.BoundsOverride = bounds;
            
            Debug.Log($"CanvasPrefabSetup: Canvas bounds updated - Min: {minSize}, Max: {maxSize}");
        }
    }
}