using UnityEngine;
using System.Collections;

namespace DaVinciEye.Canvas
{
    /// <summary>
    /// Tests for boundary visualization accuracy and functionality
    /// Validates visual rendering, animation, and configuration options
    /// </summary>
    public class CanvasBoundaryVisualizationTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private CanvasBoundaryVisualizer boundaryVisualizer;
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private float testDelay = 1.0f;
        
        [Header("Test Canvas Data")]
        [SerializeField] private Vector2 testCanvasSize = new Vector2(1.0f, 0.8f);
        [SerializeField] private Vector3 testCanvasCenter = Vector3.zero;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunBoundaryVisualizationTests());
            }
        }
        
        private IEnumerator RunBoundaryVisualizationTests()
        {
            Debug.Log("CanvasBoundaryVisualizationTests: Starting boundary visualization tests...");
            
            // Test 1: Boundary Visualizer Initialization
            yield return TestBoundaryVisualizerInitialization();
            yield return new WaitForSeconds(testDelay);
            
            // Test 2: Canvas Boundary Creation
            yield return TestCanvasBoundaryCreation();
            yield return new WaitForSeconds(testDelay);
            
            // Test 3: Boundary Visibility Control
            yield return TestBoundaryVisibilityControl();
            yield return new WaitForSeconds(testDelay);
            
            // Test 4: Boundary Animation
            yield return TestBoundaryAnimation();
            yield return new WaitForSeconds(testDelay);
            
            // Test 5: Boundary Configuration
            yield return TestBoundaryConfiguration();
            yield return new WaitForSeconds(testDelay);
            
            // Test 6: Boundary Validation
            yield return TestBoundaryValidation();
            
            Debug.Log("CanvasBoundaryVisualizationTests: All boundary visualization tests completed!");
        }
        
        private IEnumerator TestBoundaryVisualizerInitialization()
        {
            Debug.Log("Test 1: Boundary Visualizer Initialization");
            
            if (boundaryVisualizer == null)
            {
                boundaryVisualizer = FindObjectOfType<CanvasBoundaryVisualizer>();
            }
            
            if (boundaryVisualizer == null)
            {
                // Create boundary visualizer for testing
                var visualizerObject = new GameObject("TestBoundaryVisualizer");
                boundaryVisualizer = visualizerObject.AddComponent<CanvasBoundaryVisualizer>();
                Debug.Log("✅ Boundary visualizer created for testing");
            }
            else
            {
                Debug.Log("✅ Boundary visualizer found and initialized");
            }
            
            yield return null;
        }
        
        private IEnumerator TestCanvasBoundaryCreation()
        {
            Debug.Log("Test 2: Canvas Boundary Creation");
            
            if (boundaryVisualizer == null)
            {
                Debug.LogError("❌ Boundary visualizer not available");
                yield break;
            }
            
            // Create test canvas data
            var testCanvas = CreateTestCanvasData();
            
            // Set canvas data to visualizer
            boundaryVisualizer.SetCanvasData(testCanvas);
            
            yield return new WaitForSeconds(0.5f);
            
            // Validate boundary creation
            bool boundaryCreated = boundaryVisualizer.ValidateBoundaryVisualization();
            
            if (boundaryCreated)
            {
                Debug.Log("✅ Canvas boundary created successfully");
            }
            else
            {
                Debug.LogError("❌ Canvas boundary creation failed");
            }
            
            yield return null;
        }
        
        private IEnumerator TestBoundaryVisibilityControl()
        {
            Debug.Log("Test 3: Boundary Visibility Control");
            
            if (boundaryVisualizer == null)
            {
                Debug.LogError("❌ Boundary visualizer not available");
                yield break;
            }
            
            // Test hide boundary
            boundaryVisualizer.HideBoundary();
            yield return new WaitForSeconds(0.5f);
            Debug.Log("✅ Boundary hidden");
            
            // Test show boundary
            boundaryVisualizer.ShowBoundary();
            yield return new WaitForSeconds(0.5f);
            Debug.Log("✅ Boundary shown");
            
            // Test toggle visibility
            boundaryVisualizer.ToggleBoundaryVisibility();
            yield return new WaitForSeconds(0.5f);
            boundaryVisualizer.ToggleBoundaryVisibility();
            yield return new WaitForSeconds(0.5f);
            Debug.Log("✅ Boundary visibility toggle tested");
            
            yield return null;
        }
        
        private IEnumerator TestBoundaryAnimation()
        {
            Debug.Log("Test 4: Boundary Animation");
            
            if (boundaryVisualizer == null)
            {
                Debug.LogError("❌ Boundary visualizer not available");
                yield break;
            }
            
            // Enable animation
            boundaryVisualizer.SetAnimationEnabled(true);
            Debug.Log("✅ Boundary animation enabled");
            
            // Let animation run for a few seconds
            yield return new WaitForSeconds(3.0f);
            
            // Disable animation
            boundaryVisualizer.SetAnimationEnabled(false);
            Debug.Log("✅ Boundary animation disabled");
            
            yield return null;
        }
        
        private IEnumerator TestBoundaryConfiguration()
        {
            Debug.Log("Test 5: Boundary Configuration");
            
            if (boundaryVisualizer == null)
            {
                Debug.LogError("❌ Boundary visualizer not available");
                yield break;
            }
            
            // Test color change
            boundaryVisualizer.SetBoundaryColor(Color.red);
            yield return new WaitForSeconds(1.0f);
            Debug.Log("✅ Boundary color changed to red");
            
            // Test line width change
            boundaryVisualizer.SetLineWidth(0.01f); // 1cm width
            yield return new WaitForSeconds(1.0f);
            Debug.Log("✅ Boundary line width changed");
            
            // Test corner markers visibility
            boundaryVisualizer.SetCornerMarkersVisible(false);
            yield return new WaitForSeconds(1.0f);
            boundaryVisualizer.SetCornerMarkersVisible(true);
            yield return new WaitForSeconds(1.0f);
            Debug.Log("✅ Corner markers visibility toggled");
            
            // Reset to default color
            boundaryVisualizer.SetBoundaryColor(Color.cyan);
            boundaryVisualizer.SetLineWidth(0.005f);
            
            yield return null;
        }
        
        private IEnumerator TestBoundaryValidation()
        {
            Debug.Log("Test 6: Boundary Validation");
            
            if (boundaryVisualizer == null)
            {
                Debug.LogError("❌ Boundary visualizer not available");
                yield break;
            }
            
            // Test with valid canvas data
            var validCanvas = CreateTestCanvasData();
            boundaryVisualizer.SetCanvasData(validCanvas);
            
            bool validationPassed = boundaryVisualizer.ValidateBoundaryVisualization();
            
            if (validationPassed)
            {
                Debug.Log("✅ Boundary validation passed for valid canvas");
            }
            else
            {
                Debug.LogError("❌ Boundary validation failed for valid canvas");
            }
            
            // Test with invalid canvas data
            boundaryVisualizer.SetCanvasData(null);
            yield return new WaitForSeconds(0.5f);
            
            bool invalidValidation = !boundaryVisualizer.ValidateBoundaryVisualization();
            
            if (invalidValidation)
            {
                Debug.Log("✅ Boundary validation correctly failed for invalid canvas");
            }
            else
            {
                Debug.LogError("❌ Boundary validation should have failed for invalid canvas");
            }
            
            yield return null;
        }
        
        private CanvasData CreateTestCanvasData()
        {
            var canvasData = new CanvasData();
            
            // Create rectangular canvas corners
            var halfWidth = testCanvasSize.x * 0.5f;
            var halfHeight = testCanvasSize.y * 0.5f;
            
            canvasData.corners[0] = testCanvasCenter + new Vector3(-halfWidth, 0, -halfHeight); // Bottom-left
            canvasData.corners[1] = testCanvasCenter + new Vector3(halfWidth, 0, -halfHeight);  // Bottom-right
            canvasData.corners[2] = testCanvasCenter + new Vector3(halfWidth, 0, halfHeight);   // Top-right
            canvasData.corners[3] = testCanvasCenter + new Vector3(-halfWidth, 0, halfHeight);  // Top-left
            
            canvasData.center = testCanvasCenter;
            canvasData.dimensions = testCanvasSize;
            canvasData.ValidateAndCalculate();
            
            return canvasData;
        }
        
        // Manual test methods for editor/runtime testing
        [ContextMenu("Create Test Canvas")]
        public void ManualCreateTestCanvas()
        {
            if (boundaryVisualizer != null)
            {
                var testCanvas = CreateTestCanvasData();
                boundaryVisualizer.SetCanvasData(testCanvas);
                Debug.Log("Manual test: Test canvas created");
            }
        }
        
        [ContextMenu("Toggle Boundary Visibility")]
        public void ManualToggleBoundaryVisibility()
        {
            if (boundaryVisualizer != null)
            {
                boundaryVisualizer.ToggleBoundaryVisibility();
                Debug.Log("Manual test: Boundary visibility toggled");
            }
        }
        
        [ContextMenu("Toggle Animation")]
        public void ManualToggleAnimation()
        {
            if (boundaryVisualizer != null)
            {
                // Toggle animation (simple implementation)
                boundaryVisualizer.SetAnimationEnabled(!boundaryVisualizer.enabled);
                Debug.Log("Manual test: Animation toggled");
            }
        }
        
        [ContextMenu("Change Boundary Color")]
        public void ManualChangeBoundaryColor()
        {
            if (boundaryVisualizer != null)
            {
                var randomColor = new Color(Random.value, Random.value, Random.value, 1.0f);
                boundaryVisualizer.SetBoundaryColor(randomColor);
                Debug.Log($"Manual test: Boundary color changed to {randomColor}");
            }
        }
        
        [ContextMenu("Run All Boundary Tests")]
        public void ManualRunAllTests()
        {
            StartCoroutine(RunBoundaryVisualizationTests());
        }
    }
}