using UnityEngine;
using System.Collections;

namespace DaVinciEye.Canvas
{
    /// <summary>
    /// Simple test script for canvas definition functionality
    /// Tests the core canvas management features and MRTK integration
    /// </summary>
    public class CanvasDefinitionTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private CanvasDefinitionManager canvasManager;
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private float testDelay = 2.0f;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunCanvasTests());
            }
        }
        
        private IEnumerator RunCanvasTests()
        {
            Debug.Log("CanvasDefinitionTests: Starting canvas definition tests...");
            
            // Test 1: Canvas Manager Initialization
            yield return TestCanvasManagerInitialization();
            yield return new WaitForSeconds(testDelay);
            
            // Test 2: Canvas Definition Process
            yield return TestCanvasDefinition();
            yield return new WaitForSeconds(testDelay);
            
            // Test 3: Canvas Data Validation
            yield return TestCanvasDataValidation();
            yield return new WaitForSeconds(testDelay);
            
            // Test 4: Canvas Persistence (JSON serialization)
            yield return TestCanvasPersistence();
            
            Debug.Log("CanvasDefinitionTests: All tests completed!");
        }
        
        private IEnumerator TestCanvasManagerInitialization()
        {
            Debug.Log("Test 1: Canvas Manager Initialization");
            
            if (canvasManager == null)
            {
                canvasManager = FindObjectOfType<CanvasDefinitionManager>();
            }
            
            if (canvasManager == null)
            {
                Debug.LogError("❌ Canvas Manager not found!");
                yield break;
            }
            
            // Check initial state
            bool initialStateCorrect = !canvasManager.IsCanvasDefined && 
                                     canvasManager.CurrentCanvas == null;
            
            if (initialStateCorrect)
            {
                Debug.Log("✅ Canvas Manager initialized correctly");
            }
            else
            {
                Debug.LogError("❌ Canvas Manager initial state incorrect");
            }
            
            yield return null;
        }
        
        private IEnumerator TestCanvasDefinition()
        {
            Debug.Log("Test 2: Canvas Definition Process");
            
            if (canvasManager == null)
            {
                Debug.LogError("❌ Canvas Manager not available for testing");
                yield break;
            }
            
            // Subscribe to events
            bool canvasDefinedEventFired = false;
            canvasManager.OnCanvasDefined += (canvasData) => {
                canvasDefinedEventFired = true;
                Debug.Log($"✅ OnCanvasDefined event fired - Area: {canvasData.area:F2} m²");
            };
            
            // Start canvas definition
            canvasManager.StartCanvasDefinition();
            
            // Simulate canvas bounds change (in real scenario, user would manipulate BoundsControl)
            yield return new WaitForSeconds(1.0f);
            
            // Complete definition
            canvasManager.CompleteCanvasDefinition();
            
            yield return new WaitForSeconds(0.5f);
            
            // Verify results
            if (canvasManager.IsCanvasDefined && canvasDefinedEventFired)
            {
                Debug.Log("✅ Canvas definition process completed successfully");
            }
            else
            {
                Debug.LogError("❌ Canvas definition process failed");
            }
            
            yield return null;
        }
        
        private IEnumerator TestCanvasDataValidation()
        {
            Debug.Log("Test 3: Canvas Data Validation");
            
            // Create test canvas data
            var testCanvas = new CanvasData();
            testCanvas.corners[0] = new Vector3(-0.5f, 0, -0.5f);
            testCanvas.corners[1] = new Vector3(0.5f, 0, -0.5f);
            testCanvas.corners[2] = new Vector3(0.5f, 0, 0.5f);
            testCanvas.corners[3] = new Vector3(-0.5f, 0, 0.5f);
            
            testCanvas.ValidateAndCalculate();
            
            // Check validation results
            bool validationCorrect = testCanvas.isValid && 
                                   testCanvas.area > 0 && 
                                   testCanvas.dimensions.x > 0 && 
                                   testCanvas.dimensions.y > 0;
            
            if (validationCorrect)
            {
                Debug.Log($"✅ Canvas data validation passed - Area: {testCanvas.area:F2} m², Dimensions: {testCanvas.dimensions}");
            }
            else
            {
                Debug.LogError("❌ Canvas data validation failed");
            }
            
            yield return null;
        }
        
        private IEnumerator TestCanvasPersistence()
        {
            Debug.Log("Test 4: Canvas Persistence (JSON Serialization)");
            
            if (canvasManager?.CurrentCanvas == null)
            {
                Debug.LogError("❌ No canvas data available for persistence test");
                yield break;
            }
            
            // Test JSON serialization as specified in task requirements
            var canvasData = canvasManager.CurrentCanvas;
            string jsonData = JsonUtility.ToJson(canvasData, true);
            
            Debug.Log($"Canvas JSON: {jsonData}");
            
            // Test deserialization
            var deserializedCanvas = JsonUtility.FromJson<CanvasData>(jsonData);
            
            bool persistenceCorrect = deserializedCanvas != null && 
                                    deserializedCanvas.isValid &&
                                    Mathf.Approximately(deserializedCanvas.area, canvasData.area);
            
            if (persistenceCorrect)
            {
                Debug.Log("✅ Canvas persistence (JSON) test passed");
            }
            else
            {
                Debug.LogError("❌ Canvas persistence test failed");
            }
            
            yield return null;
        }
        
        // Manual test methods for editor/runtime testing
        [ContextMenu("Start Canvas Definition")]
        public void ManualStartCanvasDefinition()
        {
            if (canvasManager != null)
            {
                canvasManager.StartCanvasDefinition();
                Debug.Log("Manual test: Canvas definition started");
            }
        }
        
        [ContextMenu("Complete Canvas Definition")]
        public void ManualCompleteCanvasDefinition()
        {
            if (canvasManager != null)
            {
                canvasManager.CompleteCanvasDefinition();
                Debug.Log("Manual test: Canvas definition completed");
            }
        }
        
        [ContextMenu("Redefine Canvas")]
        public void ManualRedefineCanvas()
        {
            if (canvasManager != null)
            {
                canvasManager.RedefineCanvas();
                Debug.Log("Manual test: Canvas redefinition started");
            }
        }
        
        [ContextMenu("Run All Tests")]
        public void ManualRunAllTests()
        {
            StartCoroutine(RunCanvasTests());
        }
    }
}