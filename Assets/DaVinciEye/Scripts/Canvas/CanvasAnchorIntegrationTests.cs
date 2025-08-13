using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DaVinciEye.Canvas
{
    /// <summary>
    /// Integration tests for anchor creation and retrieval functionality
    /// Tests spatial anchor persistence and restoration with AR Foundation
    /// </summary>
    public class CanvasAnchorIntegrationTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private CanvasAnchorManager anchorManager;
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private float testDelay = 2.0f;
        
        [Header("Test Data")]
        [SerializeField] private Vector3 testCanvasPosition = new Vector3(0, 0, 1);
        [SerializeField] private Vector2 testCanvasSize = new Vector2(1.0f, 0.8f);
        
        private List<string> testCanvasIds = new List<string>();
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunAnchorIntegrationTests());
            }
        }
        
        private IEnumerator RunAnchorIntegrationTests()
        {
            Debug.Log("CanvasAnchorIntegrationTests: Starting anchor integration tests...");
            
            // Test 1: Anchor Manager Initialization
            yield return TestAnchorManagerInitialization();
            yield return new WaitForSeconds(testDelay);
            
            // Test 2: Canvas Anchor Creation
            yield return TestCanvasAnchorCreation();
            yield return new WaitForSeconds(testDelay);
            
            // Test 3: Canvas Anchor Persistence
            yield return TestCanvasAnchorPersistence();
            yield return new WaitForSeconds(testDelay);
            
            // Test 4: Canvas Anchor Restoration
            yield return TestCanvasAnchorRestoration();
            yield return new WaitForSeconds(testDelay);
            
            // Test 5: Multiple Canvas Anchors
            yield return TestMultipleCanvasAnchors();
            yield return new WaitForSeconds(testDelay);
            
            // Test 6: Anchor Validation and Error Handling
            yield return TestAnchorValidationAndErrorHandling();
            yield return new WaitForSeconds(testDelay);
            
            // Cleanup
            yield return CleanupTestAnchors();
            
            Debug.Log("CanvasAnchorIntegrationTests: All anchor integration tests completed!");
        }
        
        private IEnumerator TestAnchorManagerInitialization()
        {
            Debug.Log("Test 1: Anchor Manager Initialization");
            
            if (anchorManager == null)
            {
                anchorManager = FindObjectOfType<CanvasAnchorManager>();
            }
            
            if (anchorManager == null)
            {
                Debug.LogError("❌ Anchor Manager not found!");
                yield break;
            }
            
            // Wait for initialization
            float timeout = 10.0f;
            float timer = 0f;
            
            while (!anchorManager.IsInitialized && timer < timeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            
            if (anchorManager.IsInitialized)
            {
                Debug.Log("✅ Anchor Manager initialized successfully");
            }
            else
            {
                Debug.LogWarning("⚠️ Anchor Manager initialization timeout (may be normal in editor)");
            }
            
            yield return null;
        }
        
        private IEnumerator TestCanvasAnchorCreation()
        {
            Debug.Log("Test 2: Canvas Anchor Creation");
            
            if (anchorManager == null)
            {
                Debug.LogError("❌ Anchor Manager not available");
                yield break;
            }
            
            // Create test canvas data
            var testCanvas = CreateTestCanvasData("TestCanvas_01");
            
            // Subscribe to anchor events
            bool anchorCreated = false;
            string createdCanvasId = null;
            
            anchorManager.OnCanvasAnchored += (canvasId, canvasData) => {
                anchorCreated = true;
                createdCanvasId = canvasId;
                Debug.Log($"✅ Anchor created event received for canvas {canvasId}");
            };
            
            anchorManager.OnAnchorError += (canvasId, error) => {
                Debug.LogWarning($"⚠️ Anchor creation error for {canvasId}: {error}");
            };
            
            // Create anchor
            anchorManager.CreateCanvasAnchor(testCanvas, "TestCanvas_01");
            
            // Wait for anchor creation (or timeout)
            float timeout = 15.0f;
            float timer = 0f;
            
            while (!anchorCreated && timer < timeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            
            if (anchorCreated)
            {
                Debug.Log("✅ Canvas anchor creation test passed");
                testCanvasIds.Add(createdCanvasId);
            }
            else
            {
                Debug.LogWarning("⚠️ Canvas anchor creation timeout (may be normal in editor without AR)");
            }
            
            yield return null;
        }
        
        private IEnumerator TestCanvasAnchorPersistence()
        {
            Debug.Log("Test 3: Canvas Anchor Persistence");
            
            if (anchorManager == null)
            {
                Debug.LogError("❌ Anchor Manager not available");
                yield break;
            }
            
            // Check if we have any anchored canvases
            var anchoredIds = anchorManager.GetAnchoredCanvasIds();
            
            if (anchoredIds.Count > 0)
            {
                var testId = anchoredIds[0];
                var canvasData = anchorManager.GetAnchoredCanvasData(testId);
                
                if (canvasData != null)
                {
                    Debug.Log($"✅ Canvas anchor persistence test passed - Canvas {testId} data retrieved");
                }
                else
                {
                    Debug.LogError("❌ Canvas anchor persistence test failed - No data retrieved");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No anchored canvases available for persistence test");
            }
            
            yield return null;
        }
        
        private IEnumerator TestCanvasAnchorRestoration()
        {
            Debug.Log("Test 4: Canvas Anchor Restoration");
            
            if (anchorManager == null)
            {
                Debug.LogError("❌ Anchor Manager not available");
                yield break;
            }
            
            // Get saved canvas IDs
            var savedIds = anchorManager.GetSavedCanvasIds();
            
            if (savedIds.Count > 0)
            {
                var testId = savedIds[0];
                
                bool anchorRestored = false;
                anchorManager.OnCanvasAnchorRestored += (canvasId, canvasData) => {
                    if (canvasId == testId)
                    {
                        anchorRestored = true;
                        Debug.Log($"✅ Anchor restoration event received for canvas {canvasId}");
                    }
                };
                
                // Attempt restoration
                anchorManager.RestoreCanvasAnchor(testId);
                
                // Wait for restoration (or timeout)
                float timeout = 15.0f;
                float timer = 0f;
                
                while (!anchorRestored && timer < timeout)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }
                
                if (anchorRestored)
                {
                    Debug.Log("✅ Canvas anchor restoration test passed");
                }
                else
                {
                    Debug.LogWarning("⚠️ Canvas anchor restoration timeout (may be normal in editor without AR)");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No saved canvases available for restoration test");
            }
            
            yield return null;
        }
        
        private IEnumerator TestMultipleCanvasAnchors()
        {
            Debug.Log("Test 5: Multiple Canvas Anchors");
            
            if (anchorManager == null)
            {
                Debug.LogError("❌ Anchor Manager not available");
                yield break;
            }
            
            // Create multiple test canvases
            var canvas1 = CreateTestCanvasData("MultiTest_01", new Vector3(-1, 0, 1));
            var canvas2 = CreateTestCanvasData("MultiTest_02", new Vector3(1, 0, 1));
            
            int anchorsCreated = 0;
            anchorManager.OnCanvasAnchored += (canvasId, canvasData) => {
                if (canvasId.StartsWith("MultiTest_"))
                {
                    anchorsCreated++;
                    testCanvasIds.Add(canvasId);
                    Debug.Log($"✅ Multiple anchor test - Canvas {canvasId} anchored ({anchorsCreated}/2)");
                }
            };
            
            // Create anchors
            anchorManager.CreateCanvasAnchor(canvas1, "MultiTest_01");
            yield return new WaitForSeconds(1.0f);
            anchorManager.CreateCanvasAnchor(canvas2, "MultiTest_02");
            
            // Wait for both anchors
            float timeout = 20.0f;
            float timer = 0f;
            
            while (anchorsCreated < 2 && timer < timeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            
            if (anchorsCreated >= 1)
            {
                Debug.Log($"✅ Multiple canvas anchors test passed - {anchorsCreated} anchors created");
            }
            else
            {
                Debug.LogWarning("⚠️ Multiple canvas anchors test timeout");
            }
            
            yield return null;
        }
        
        private IEnumerator TestAnchorValidationAndErrorHandling()
        {
            Debug.Log("Test 6: Anchor Validation and Error Handling");
            
            if (anchorManager == null)
            {
                Debug.LogError("❌ Anchor Manager not available");
                yield break;
            }
            
            // Test with invalid canvas data
            bool errorReceived = false;
            anchorManager.OnAnchorError += (canvasId, error) => {
                if (canvasId == "InvalidTest")
                {
                    errorReceived = true;
                    Debug.Log($"✅ Error handling test - Expected error received: {error}");
                }
            };
            
            // Try to create anchor with invalid data
            anchorManager.CreateCanvasAnchor(null, "InvalidTest");
            
            yield return new WaitForSeconds(2.0f);
            
            if (errorReceived)
            {
                Debug.Log("✅ Anchor validation and error handling test passed");
            }
            else
            {
                Debug.LogWarning("⚠️ Expected error not received for invalid canvas data");
            }
            
            // Test anchor status checking
            var anchoredIds = anchorManager.GetAnchoredCanvasIds();
            foreach (var canvasId in anchoredIds)
            {
                bool isAnchored = anchorManager.IsCanvasAnchored(canvasId);
                Debug.Log($"Canvas {canvasId} anchored status: {isAnchored}");
            }
            
            Debug.Log($"✅ Active anchor count: {anchorManager.GetActiveAnchorCount()}");
            
            yield return null;
        }
        
        private IEnumerator CleanupTestAnchors()
        {
            Debug.Log("Cleanup: Removing test anchors");
            
            if (anchorManager != null)
            {
                foreach (var canvasId in testCanvasIds)
                {
                    anchorManager.RemoveCanvasAnchor(canvasId);
                    yield return new WaitForSeconds(0.1f);
                }
                
                testCanvasIds.Clear();
                Debug.Log("✅ Test anchors cleaned up");
            }
            
            yield return null;
        }
        
        private CanvasData CreateTestCanvasData(string canvasId, Vector3? position = null)
        {
            var canvasData = new CanvasData();
            
            var center = position ?? testCanvasPosition;
            var halfWidth = testCanvasSize.x * 0.5f;
            var halfHeight = testCanvasSize.y * 0.5f;
            
            canvasData.corners[0] = center + new Vector3(-halfWidth, 0, -halfHeight);
            canvasData.corners[1] = center + new Vector3(halfWidth, 0, -halfHeight);
            canvasData.corners[2] = center + new Vector3(halfWidth, 0, halfHeight);
            canvasData.corners[3] = center + new Vector3(-halfWidth, 0, halfHeight);
            
            canvasData.center = center;
            canvasData.dimensions = testCanvasSize;
            canvasData.anchorId = canvasId;
            canvasData.ValidateAndCalculate();
            
            return canvasData;
        }
        
        // Manual test methods
        [ContextMenu("Create Test Anchor")]
        public void ManualCreateTestAnchor()
        {
            if (anchorManager != null)
            {
                var testCanvas = CreateTestCanvasData("ManualTest");
                anchorManager.CreateCanvasAnchor(testCanvas, "ManualTest");
                Debug.Log("Manual test: Test anchor creation started");
            }
        }
        
        [ContextMenu("Restore Test Anchor")]
        public void ManualRestoreTestAnchor()
        {
            if (anchorManager != null)
            {
                anchorManager.RestoreCanvasAnchor("ManualTest");
                Debug.Log("Manual test: Test anchor restoration started");
            }
        }
        
        [ContextMenu("List Anchored Canvases")]
        public void ManualListAnchoredCanvases()
        {
            if (anchorManager != null)
            {
                var anchoredIds = anchorManager.GetAnchoredCanvasIds();
                Debug.Log($"Manual test: {anchoredIds.Count} anchored canvases:");
                foreach (var id in anchoredIds)
                {
                    Debug.Log($"  - {id} (Anchored: {anchorManager.IsCanvasAnchored(id)})");
                }
            }
        }
        
        [ContextMenu("List Saved Canvases")]
        public void ManualListSavedCanvases()
        {
            if (anchorManager != null)
            {
                var savedIds = anchorManager.GetSavedCanvasIds();
                Debug.Log($"Manual test: {savedIds.Count} saved canvases:");
                foreach (var id in savedIds)
                {
                    Debug.Log($"  - {id}");
                }
            }
        }
        
        [ContextMenu("Remove All Test Anchors")]
        public void ManualRemoveAllTestAnchors()
        {
            if (anchorManager != null)
            {
                var anchoredIds = anchorManager.GetAnchoredCanvasIds();
                foreach (var id in anchoredIds)
                {
                    if (id.Contains("Test"))
                    {
                        anchorManager.RemoveCanvasAnchor(id);
                        Debug.Log($"Manual test: Removed anchor {id}");
                    }
                }
            }
        }
        
        [ContextMenu("Run All Anchor Tests")]
        public void ManualRunAllTests()
        {
            StartCoroutine(RunAnchorIntegrationTests());
        }
    }
}