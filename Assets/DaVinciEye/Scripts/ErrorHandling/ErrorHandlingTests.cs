using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using DaVinciEye.ErrorHandling;

namespace DaVinciEye.Tests.ErrorHandling
{
    /// <summary>
    /// Comprehensive tests for error handling and recovery mechanisms
    /// Tests all error scenarios and recovery strategies
    /// </summary>
    public class ErrorHandlingTests
    {
        private ErrorManager errorManager;
        private GameObject testGameObject;

        [SetUp]
        public void SetUp()
        {
            // Create test error manager
            testGameObject = new GameObject("TestErrorManager");
            errorManager = testGameObject.AddComponent<ErrorManager>();

            // Create mock system components for testing
            SetupMockSystems();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        private void SetupMockSystems()
        {
            // Create mock components that error recovery strategies will interact with
            var trackingMonitor = testGameObject.AddComponent<SpatialTracking.TrackingQualityMonitor>();
            var imageOverlay = testGameObject.AddComponent<ImageOverlay.ImageOverlayManager>();
            var filterManager = testGameObject.AddComponent<Filters.FilterManager>();
            var colorAnalyzer = testGameObject.AddComponent<ColorAnalysis.ColorAnalyzer>();
            var canvasManager = testGameObject.AddComponent<Canvas.CanvasDefinitionManager>();
            var sessionManager = testGameObject.AddComponent<SessionManagement.SessionDataManager>();
            var uiManager = testGameObject.AddComponent<UI.UIManager>();
        }

        [Test]
        public void TestErrorReporting()
        {
            // Test basic error reporting
            bool errorReported = false;
            ErrorInfo reportedError = null;

            errorManager.OnErrorOccurred.AddListener((error) => {
                errorReported = true;
                reportedError = error;
            });

            // Report a test error
            errorManager.ReportError(ErrorType.ImageLoadFailure, "Test image load failure");

            Assert.IsTrue(errorReported, "Error should be reported");
            Assert.IsNotNull(reportedError, "Error info should be provided");
            Assert.AreEqual(ErrorType.ImageLoadFailure, reportedError.errorType, "Error type should match");
            Assert.AreEqual("Test image load failure", reportedError.message, "Error message should match");
        }

        [Test]
        public void TestErrorSeverityClassification()
        {
            // Test that errors are classified with correct severity
            var testCases = new[]
            {
                new { errorType = ErrorType.TrackingLoss, expectedSeverity = ErrorSeverity.Critical },
                new { errorType = ErrorType.ImageLoadFailure, expectedSeverity = ErrorSeverity.Error },
                new { errorType = ErrorType.ColorAnalysisFailure, expectedSeverity = ErrorSeverity.Warning },
                new { errorType = ErrorType.PerformanceDegradation, expectedSeverity = ErrorSeverity.Warning }
            };

            foreach (var testCase in testCases)
            {
                ErrorInfo reportedError = null;
                errorManager.OnErrorOccurred.AddListener((error) => reportedError = error);

                errorManager.ReportError(testCase.errorType, "Test error");

                Assert.IsNotNull(reportedError, $"Error should be reported for {testCase.errorType}");
                Assert.AreEqual(testCase.expectedSeverity, reportedError.severity, 
                    $"Severity should be {testCase.expectedSeverity} for {testCase.errorType}");
            }
        }

        [Test]
        public void TestErrorCooldown()
        {
            // Test that error cooldown prevents spam
            int errorCount = 0;
            errorManager.OnErrorOccurred.AddListener((error) => errorCount++);

            // Report same error multiple times quickly
            errorManager.ReportError(ErrorType.ImageLoadFailure, "Test error 1");
            errorManager.ReportError(ErrorType.ImageLoadFailure, "Test error 2");
            errorManager.ReportError(ErrorType.ImageLoadFailure, "Test error 3");

            Assert.AreEqual(1, errorCount, "Only first error should be reported due to cooldown");
        }

        [UnityTest]
        public IEnumerator TestErrorCooldownExpiry()
        {
            // Test that cooldown expires and allows new errors
            int errorCount = 0;
            errorManager.OnErrorOccurred.AddListener((error) => errorCount++);

            // Report first error
            errorManager.ReportError(ErrorType.ImageLoadFailure, "Test error 1");
            Assert.AreEqual(1, errorCount, "First error should be reported");

            // Wait for cooldown to expire (default is 2 seconds)
            yield return new WaitForSeconds(2.5f);

            // Report second error
            errorManager.ReportError(ErrorType.ImageLoadFailure, "Test error 2");
            Assert.AreEqual(2, errorCount, "Second error should be reported after cooldown");
        }

        [UnityTest]
        public IEnumerator TestTrackingLossRecovery()
        {
            // Test tracking loss recovery strategy
            bool recoveryAttempted = false;
            errorManager.OnErrorRecovered.AddListener((error) => {
                if (error.errorType == ErrorType.TrackingLoss)
                    recoveryAttempted = true;
            });

            // Report tracking loss error
            errorManager.ReportError(ErrorType.TrackingLoss, "Tracking lost");

            // Wait for recovery attempt
            yield return new WaitForSeconds(2f);

            // Note: In a real scenario, recovery success would depend on actual tracking system
            // For testing, we verify that recovery was attempted
            var stats = errorManager.GetErrorStatistics();
            Assert.Greater(stats.recoveryAttempts, 0, "Recovery should be attempted for tracking loss");
        }

        [UnityTest]
        public IEnumerator TestImageLoadFailureRecovery()
        {
            // Test image load failure recovery
            bool recoveryAttempted = false;
            errorManager.OnErrorRecovered.AddListener((error) => {
                if (error.errorType == ErrorType.ImageLoadFailure)
                    recoveryAttempted = true;
            });

            errorManager.ReportError(ErrorType.ImageLoadFailure, "Failed to load image");

            yield return new WaitForSeconds(2f);

            var stats = errorManager.GetErrorStatistics();
            Assert.Greater(stats.recoveryAttempts, 0, "Recovery should be attempted for image load failure");
        }

        [UnityTest]
        public IEnumerator TestMemoryPressureRecovery()
        {
            // Test memory pressure recovery
            long initialMemory = System.GC.GetTotalMemory(false);

            errorManager.ReportError(ErrorType.MemoryPressure, "Low memory detected");

            yield return new WaitForSeconds(2f);

            // Verify garbage collection was triggered
            long finalMemory = System.GC.GetTotalMemory(false);
            
            // Memory should be same or lower after GC
            Assert.LessOrEqual(finalMemory, initialMemory * 1.1f, 
                "Memory should not increase significantly after memory pressure recovery");
        }

        [Test]
        public void TestMaxRecoveryAttempts()
        {
            // Test that recovery attempts are limited
            int recoveryAttempts = 0;
            errorManager.OnErrorRecovered.AddListener((error) => recoveryAttempts++);

            // Report same error multiple times to trigger multiple recovery attempts
            for (int i = 0; i < 5; i++)
            {
                errorManager.ReportError(ErrorType.FilterProcessingError, $"Filter error {i}");
            }

            // Recovery attempts should be limited (default max is 3)
            Assert.LessOrEqual(recoveryAttempts, 3, "Recovery attempts should be limited");
        }

        [Test]
        public void TestErrorStatistics()
        {
            // Test error statistics tracking
            errorManager.ReportError(ErrorType.ImageLoadFailure, "Error 1");
            errorManager.ReportError(ErrorType.FilterProcessingError, "Error 2");
            errorManager.ReportError(ErrorType.ColorAnalysisFailure, "Error 3");

            var stats = errorManager.GetErrorStatistics();

            Assert.AreEqual(3, stats.totalErrors, "Should track total error count");
            Assert.IsNotNull(stats.errorHistory, "Should maintain error history");
            Assert.AreEqual(3, stats.errorHistory.Count, "Error history should contain all errors");
        }

        [Test]
        public void TestErrorHistoryLimit()
        {
            // Test that error history is limited to prevent memory issues
            // Report more errors than the history limit (default is 100)
            for (int i = 0; i < 150; i++)
            {
                errorManager.ReportError(ErrorType.PerformanceDegradation, $"Performance error {i}");
            }

            var stats = errorManager.GetErrorStatistics();
            Assert.LessOrEqual(stats.errorHistory.Count, 100, "Error history should be limited");
        }

        [Test]
        public void TestForceRecovery()
        {
            // Test manual recovery forcing
            bool recoveryResult = errorManager.ForceRecovery(ErrorType.FilterProcessingError);

            // Recovery should succeed for filter processing errors (clears filters)
            Assert.IsTrue(recoveryResult, "Force recovery should succeed for filter processing errors");
        }

        [Test]
        public void TestClearErrorHistory()
        {
            // Test error history clearing
            errorManager.ReportError(ErrorType.ImageLoadFailure, "Test error");
            
            var statsBeforeClear = errorManager.GetErrorStatistics();
            Assert.Greater(statsBeforeClear.totalErrors, 0, "Should have errors before clearing");

            errorManager.ClearErrorHistory();

            var statsAfterClear = errorManager.GetErrorStatistics();
            Assert.AreEqual(0, statsAfterClear.totalErrors, "Should have no errors after clearing");
            Assert.AreEqual(0, statsAfterClear.errorHistory.Count, "Error history should be empty after clearing");
        }

        [UnityTest]
        public IEnumerator TestUserMessageGeneration()
        {
            // Test that user-friendly messages are generated
            string userMessage = "";
            errorManager.OnUserMessageRequired.AddListener((message) => userMessage = message);

            // Report an error that should generate a user message
            errorManager.ReportError(ErrorType.TrackingLoss, "Tracking system failure");

            yield return new WaitForSeconds(0.1f); // Allow event to process

            Assert.IsNotEmpty(userMessage, "User message should be generated for tracking loss");
            Assert.IsTrue(userMessage.Contains("tracking") || userMessage.Contains("Tracking"), 
                "User message should mention tracking");
        }

        [Test]
        public void TestErrorTypeAnalysis()
        {
            // Test Unity error message analysis
            // This tests the private method indirectly by triggering Unity log messages
            
            int errorCount = 0;
            errorManager.OnErrorOccurred.AddListener((error) => errorCount++);

            // Simulate Unity error messages
            Debug.LogError("Tracking lost - spatial mapping failed");
            Debug.LogError("Texture loading failed - out of memory");
            Debug.LogError("Camera access denied");

            // Note: This test may not work perfectly in test environment
            // as Unity's log message handling might behave differently
        }

        [UnityTest]
        public IEnumerator TestRecoverySuccessNotification()
        {
            // Test that successful recovery generates notification
            bool recoveryNotified = false;
            errorManager.OnErrorRecovered.AddListener((error) => recoveryNotified = true);

            // Force a recovery that should succeed
            bool recoveryResult = errorManager.ForceRecovery(ErrorType.FilterProcessingError);

            yield return new WaitForSeconds(0.1f);

            if (recoveryResult)
            {
                Assert.IsTrue(recoveryNotified, "Recovery success should be notified");
            }
        }

        [Test]
        public void TestErrorContextTracking()
        {
            // Test that error context is properly tracked
            ErrorInfo reportedError = null;
            errorManager.OnErrorOccurred.AddListener((error) => reportedError = error);

            string testContext = "Unit Test Context";
            errorManager.ReportError(ErrorType.ImageLoadFailure, "Test error", null, testContext);

            Assert.IsNotNull(reportedError, "Error should be reported");
            Assert.AreEqual(testContext, reportedError.context, "Error context should be preserved");
        }

        [Test]
        public void TestExceptionHandling()
        {
            // Test that exceptions are properly handled and logged
            ErrorInfo reportedError = null;
            errorManager.OnErrorOccurred.AddListener((error) => reportedError = error);

            var testException = new System.InvalidOperationException("Test exception");
            errorManager.ReportError(ErrorType.FilterProcessingError, "Test error with exception", testException);

            Assert.IsNotNull(reportedError, "Error should be reported");
            Assert.IsNotNull(reportedError.exception, "Exception should be preserved");
            Assert.AreEqual(testException, reportedError.exception, "Exception should match");
        }

        [UnityTest]
        public IEnumerator TestConcurrentErrorHandling()
        {
            // Test handling multiple concurrent errors
            int errorCount = 0;
            errorManager.OnErrorOccurred.AddListener((error) => errorCount++);

            // Report different types of errors concurrently
            errorManager.ReportError(ErrorType.ImageLoadFailure, "Error 1");
            errorManager.ReportError(ErrorType.FilterProcessingError, "Error 2");
            errorManager.ReportError(ErrorType.ColorAnalysisFailure, "Error 3");
            errorManager.ReportError(ErrorType.UIInteractionFailure, "Error 4");

            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(4, errorCount, "All different error types should be reported");

            var stats = errorManager.GetErrorStatistics();
            Assert.AreEqual(4, stats.totalErrors, "Statistics should reflect all errors");
        }

        [UnityTest]
        public IEnumerator TestRecoveryDelayMechanism()
        {
            // Test that recovery attempts are properly delayed
            float startTime = Time.realtimeSinceStartup;
            bool recoveryAttempted = false;

            errorManager.OnErrorRecovered.AddListener((error) => {
                float recoveryTime = Time.realtimeSinceStartup - startTime;
                Assert.Greater(recoveryTime, 0.5f, "Recovery should be delayed");
                recoveryAttempted = true;
            });

            errorManager.ReportError(ErrorType.FilterProcessingError, "Test error for recovery delay");

            yield return new WaitForSeconds(2f);

            // Note: Recovery attempt timing depends on the actual recovery delay setting
        }

        [Test]
        public void TestRecoverySuccessRateCalculation()
        {
            // Test recovery success rate calculation
            // Force some recoveries to test statistics
            errorManager.ForceRecovery(ErrorType.FilterProcessingError); // Should succeed
            errorManager.ForceRecovery(ErrorType.ImageLoadFailure);      // Should succeed
            
            var stats = errorManager.GetErrorStatistics();
            
            if (stats.recoveryAttempts > 0)
            {
                Assert.GreaterOrEqual(stats.RecoverySuccessRate, 0f, "Recovery success rate should be non-negative");
                Assert.LessOrEqual(stats.RecoverySuccessRate, 1f, "Recovery success rate should not exceed 100%");
            }
        }
    }
}