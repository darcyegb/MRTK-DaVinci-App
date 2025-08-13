using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DaVinciEye.ColorAnalysis
{
    /// <summary>
    /// Paint color analyzer using HoloLens cameras for real-world color capture
    /// Implements AR Foundation camera access with lighting compensation and calibration
    /// </summary>
    public class PaintColorAnalyzer : MonoBehaviour
    {
        [Header("Camera Configuration")]
        [SerializeField] private ARCameraManager cameraManager;
        [SerializeField] private Camera arCamera;
        
        [Header("Color Analysis Settings")]
        [SerializeField] private int samplingRadius = 2; // 5x5 pixel sampling area
        [SerializeField] private float lightingCompensationStrength = 0.5f;
        [SerializeField] private bool enableWhiteBalance = true;
        [SerializeField] private bool enableExposureCompensation = true;
        
        [Header("Calibration")]
        [SerializeField] private bool useColorCalibration = true;
        [SerializeField] private ColorCalibrationData calibrationData;
        
        [Header("Environmental Adaptation")]
        [SerializeField] private LightingCondition currentLightingCondition = LightingCondition.Indoor;
        [SerializeField] private float ambientLightLevel = 0.5f;
        
        // Properties
        public bool IsCameraReady { get; private set; }
        public Texture2D CurrentCameraFrame { get; private set; }
        public LightingCondition CurrentLighting => currentLightingCondition;
        
        // Events
        public event Action<Color> OnColorCaptured;
        public event Action<bool> OnCameraStatusChanged;
        public event Action<LightingCondition> OnLightingConditionChanged;
        
        // Private fields
        private bool isInitialized = false;
        private Texture2D cameraTexture;
        private Matrix4x4 displayMatrix;
        private Vector2Int textureSize;
        
        // Color calibration matrices
        private Matrix4x4 whiteBalanceMatrix = Matrix4x4.identity;
        private Vector3 exposureCompensation = Vector3.one;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            InitializeCameraSystem();
        }
        
        private void InitializeComponents()
        {
            if (cameraManager == null)
                cameraManager = FindObjectOfType<ARCameraManager>();
            
            if (arCamera == null)
                arCamera = Camera.main;
            
            if (calibrationData == null)
                calibrationData = CreateDefaultCalibrationData();
        }
        
        private async void InitializeCameraSystem()
        {
            if (cameraManager == null)
            {
                Debug.LogError("PaintColorAnalyzer: ARCameraManager not found");
                return;
            }
            
            // Subscribe to camera events
            cameraManager.frameReceived += OnCameraFrameReceived;
            
            // Request camera permission and start
            await RequestCameraPermission();
            
            if (cameraManager.enabled)
            {
                IsCameraReady = true;
                OnCameraStatusChanged?.Invoke(true);
                Debug.Log("PaintColorAnalyzer: Camera system initialized successfully");
            }
            
            isInitialized = true;
        }
        
        private async Task RequestCameraPermission()
        {
            // In a real HoloLens app, camera permission is typically granted through app manifest
            // For now, we'll simulate the permission request
            await Task.Delay(100);
            
            if (cameraManager != null)
            {
                cameraManager.enabled = true;
            }
        }
        
        private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            if (!isInitialized) return;
            
            // Get camera frame texture
            if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                UpdateCameraTexture(image);
                image.Dispose();
            }
            
            // Update lighting conditions
            UpdateLightingConditions();
        }
        
        private void UpdateCameraTexture(XRCpuImage cpuImage)
        {
            // Convert CPU image to Texture2D
            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
                outputDimensions = new Vector2Int(cpuImage.width / 2, cpuImage.height / 2), // Downsample for performance
                outputFormat = TextureFormat.RGB24,
                transformation = XRCpuImage.Transformation.MirrorY
            };
            
            int size = cpuImage.GetConvertedDataSize(conversionParams);
            var buffer = new byte[size];
            
            cpuImage.Convert(conversionParams, buffer);
            
            // Create or update texture
            if (cameraTexture == null || 
                cameraTexture.width != conversionParams.outputDimensions.x || 
                cameraTexture.height != conversionParams.outputDimensions.y)
            {
                if (cameraTexture != null)
                    Destroy(cameraTexture);
                
                cameraTexture = new Texture2D(
                    conversionParams.outputDimensions.x,
                    conversionParams.outputDimensions.y,
                    TextureFormat.RGB24,
                    false);
            }
            
            cameraTexture.LoadRawTextureData(buffer);
            cameraTexture.Apply();
            
            CurrentCameraFrame = cameraTexture;
            textureSize = conversionParams.outputDimensions;
        }
        
        /// <summary>
        /// Analyze paint color at a world position using camera
        /// </summary>
        public async Task<Color> AnalyzePaintColorAsync(Vector3 worldPosition)
        {
            if (!IsCameraReady || CurrentCameraFrame == null)
            {
                Debug.LogWarning("PaintColorAnalyzer: Camera not ready for color analysis");
                return Color.white;
            }
            
            // Convert world position to screen coordinates
            Vector3 screenPoint = arCamera.WorldToScreenPoint(worldPosition);
            
            // Convert screen coordinates to texture coordinates
            Vector2 textureCoord = ScreenToTextureCoordinate(screenPoint);
            
            // Sample color from camera texture with averaging
            Color rawColor = SampleColorWithAveraging(textureCoord);
            
            // Apply lighting compensation and calibration
            Color compensatedColor = ApplyColorCompensation(rawColor);
            
            // Store the captured color
            OnColorCaptured?.Invoke(compensatedColor);
            
            Debug.Log($"PaintColorAnalyzer: Captured color at world pos {worldPosition} - RGB({compensatedColor.r:F3}, {compensatedColor.g:F3}, {compensatedColor.b:F3})");
            
            return compensatedColor;
        }
        
        private Vector2 ScreenToTextureCoordinate(Vector3 screenPoint)
        {
            // Convert screen coordinates to normalized coordinates (0-1)
            Vector2 normalizedScreen = new Vector2(
                screenPoint.x / Screen.width,
                screenPoint.y / Screen.height
            );
            
            // Convert to texture coordinates
            Vector2 textureCoord = new Vector2(
                normalizedScreen.x * textureSize.x,
                normalizedScreen.y * textureSize.y
            );
            
            return textureCoord;
        }
        
        private Color SampleColorWithAveraging(Vector2 textureCoord)
        {
            if (CurrentCameraFrame == null) return Color.white;
            
            int centerX = Mathf.RoundToInt(textureCoord.x);
            int centerY = Mathf.RoundToInt(textureCoord.y);
            
            Vector3 colorSum = Vector3.zero;
            int sampleCount = 0;
            
            // Sample in a radius around the center point (5x5 area)
            for (int x = centerX - samplingRadius; x <= centerX + samplingRadius; x++)
            {
                for (int y = centerY - samplingRadius; y <= centerY + samplingRadius; y++)
                {
                    // Clamp to texture bounds
                    int clampedX = Mathf.Clamp(x, 0, CurrentCameraFrame.width - 1);
                    int clampedY = Mathf.Clamp(y, 0, CurrentCameraFrame.height - 1);
                    
                    Color pixelColor = CurrentCameraFrame.GetPixel(clampedX, clampedY);
                    colorSum += new Vector3(pixelColor.r, pixelColor.g, pixelColor.b);
                    sampleCount++;
                }
            }
            
            // Average the sampled colors
            if (sampleCount > 0)
            {
                Vector3 averageColor = colorSum / sampleCount;
                return new Color(averageColor.x, averageColor.y, averageColor.z, 1f);
            }
            
            return Color.white;
        }
        
        private Color ApplyColorCompensation(Color rawColor)
        {
            Color compensatedColor = rawColor;
            
            // Apply white balance correction
            if (enableWhiteBalance)
            {
                compensatedColor = ApplyWhiteBalance(compensatedColor);
            }
            
            // Apply exposure compensation
            if (enableExposureCompensation)
            {
                compensatedColor = ApplyExposureCompensation(compensatedColor);
            }
            
            // Apply calibration if available
            if (useColorCalibration && calibrationData != null)
            {
                compensatedColor = ApplyColorCalibration(compensatedColor);
            }
            
            // Apply environmental adaptation
            compensatedColor = ApplyEnvironmentalAdaptation(compensatedColor);
            
            return compensatedColor;
        }
        
        private Color ApplyWhiteBalance(Color color)
        {
            // Simple white balance based on current lighting condition
            Vector3 whiteBalanceFactors = GetWhiteBalanceFactors();
            
            return new Color(
                color.r * whiteBalanceFactors.x,
                color.g * whiteBalanceFactors.y,
                color.b * whiteBalanceFactors.z,
                color.a
            );
        }
        
        private Vector3 GetWhiteBalanceFactors()
        {
            switch (currentLightingCondition)
            {
                case LightingCondition.Indoor:
                    return new Vector3(1.1f, 1.0f, 0.9f); // Compensate for warm indoor lighting
                case LightingCondition.Outdoor:
                    return new Vector3(0.95f, 1.0f, 1.05f); // Compensate for cool outdoor lighting
                case LightingCondition.Mixed:
                    return new Vector3(1.0f, 1.0f, 1.0f); // Neutral
                default:
                    return Vector3.one;
            }
        }
        
        private Color ApplyExposureCompensation(Color color)
        {
            // Adjust exposure based on ambient light level
            float exposureFactor = Mathf.Lerp(1.2f, 0.8f, ambientLightLevel);
            
            return new Color(
                Mathf.Clamp01(color.r * exposureFactor),
                Mathf.Clamp01(color.g * exposureFactor),
                Mathf.Clamp01(color.b * exposureFactor),
                color.a
            );
        }
        
        private Color ApplyColorCalibration(Color color)
        {
            if (calibrationData == null) return color;
            
            // Apply calibration matrix transformation
            Vector4 colorVector = new Vector4(color.r, color.g, color.b, 1f);
            Vector4 calibratedVector = calibrationData.calibrationMatrix * colorVector;
            
            return new Color(
                Mathf.Clamp01(calibratedVector.x),
                Mathf.Clamp01(calibratedVector.y),
                Mathf.Clamp01(calibratedVector.z),
                color.a
            );
        }
        
        private Color ApplyEnvironmentalAdaptation(Color color)
        {
            // Adjust color based on environmental conditions
            float adaptationStrength = lightingCompensationStrength;
            
            // Reduce saturation in low light conditions
            if (ambientLightLevel < 0.3f)
            {
                Color.RGBToHSV(color, out float h, out float s, out float v);
                s *= Mathf.Lerp(0.7f, 1f, ambientLightLevel / 0.3f);
                color = Color.HSVToRGB(h, s, v);
            }
            
            return Color.Lerp(color, color, 1f - adaptationStrength);
        }
        
        private void UpdateLightingConditions()
        {
            // Simple lighting condition detection based on camera exposure
            // In a real implementation, this could use additional sensors
            
            float averageBrightness = CalculateAverageBrightness();
            ambientLightLevel = averageBrightness;
            
            LightingCondition newCondition = DetermineLightingCondition(averageBrightness);
            
            if (newCondition != currentLightingCondition)
            {
                currentLightingCondition = newCondition;
                OnLightingConditionChanged?.Invoke(newCondition);
                Debug.Log($"PaintColorAnalyzer: Lighting condition changed to {newCondition}");
            }
        }
        
        private float CalculateAverageBrightness()
        {
            if (CurrentCameraFrame == null) return 0.5f;
            
            // Sample brightness from center region of camera frame
            int sampleSize = Mathf.Min(CurrentCameraFrame.width, CurrentCameraFrame.height) / 4;
            int centerX = CurrentCameraFrame.width / 2;
            int centerY = CurrentCameraFrame.height / 2;
            
            float brightnessSum = 0f;
            int sampleCount = 0;
            
            for (int x = centerX - sampleSize/2; x < centerX + sampleSize/2; x += 4)
            {
                for (int y = centerY - sampleSize/2; y < centerY + sampleSize/2; y += 4)
                {
                    if (x >= 0 && x < CurrentCameraFrame.width && y >= 0 && y < CurrentCameraFrame.height)
                    {
                        Color pixel = CurrentCameraFrame.GetPixel(x, y);
                        brightnessSum += (pixel.r + pixel.g + pixel.b) / 3f;
                        sampleCount++;
                    }
                }
            }
            
            return sampleCount > 0 ? brightnessSum / sampleCount : 0.5f;
        }
        
        private LightingCondition DetermineLightingCondition(float brightness)
        {
            if (brightness < 0.3f)
                return LightingCondition.Indoor;
            else if (brightness > 0.7f)
                return LightingCondition.Outdoor;
            else
                return LightingCondition.Mixed;
        }
        
        /// <summary>
        /// Calibrate color capture using known color swatches
        /// </summary>
        public async Task<bool> CalibrateColorCapture(Color[] knownColors, Vector3[] swatchPositions)
        {
            if (knownColors.Length != swatchPositions.Length)
            {
                Debug.LogError("PaintColorAnalyzer: Known colors and swatch positions arrays must have same length");
                return false;
            }
            
            Color[] capturedColors = new Color[knownColors.Length];
            
            // Capture colors from each swatch position
            for (int i = 0; i < swatchPositions.Length; i++)
            {
                capturedColors[i] = await AnalyzePaintColorAsync(swatchPositions[i]);
                await Task.Delay(100); // Small delay between captures
            }
            
            // Calculate calibration matrix
            calibrationData = CalculateCalibrationMatrix(knownColors, capturedColors);
            
            Debug.Log("PaintColorAnalyzer: Color calibration completed");
            return true;
        }
        
        private ColorCalibrationData CalculateCalibrationMatrix(Color[] knownColors, Color[] capturedColors)
        {
            // Simple calibration using average color differences
            // In a real implementation, this could use more sophisticated matrix calculations
            
            Vector3 colorDifference = Vector3.zero;
            
            for (int i = 0; i < knownColors.Length; i++)
            {
                colorDifference += new Vector3(
                    knownColors[i].r - capturedColors[i].r,
                    knownColors[i].g - capturedColors[i].g,
                    knownColors[i].b - capturedColors[i].b
                );
            }
            
            colorDifference /= knownColors.Length;
            
            // Create calibration matrix
            Matrix4x4 calibrationMatrix = Matrix4x4.identity;
            calibrationMatrix.m00 = 1f + colorDifference.x;
            calibrationMatrix.m11 = 1f + colorDifference.y;
            calibrationMatrix.m22 = 1f + colorDifference.z;
            
            return new ColorCalibrationData
            {
                calibrationMatrix = calibrationMatrix,
                isCalibrated = true,
                calibrationDate = DateTime.Now
            };
        }
        
        private ColorCalibrationData CreateDefaultCalibrationData()
        {
            return new ColorCalibrationData
            {
                calibrationMatrix = Matrix4x4.identity,
                isCalibrated = false,
                calibrationDate = DateTime.Now
            };
        }
        
        /// <summary>
        /// Set sampling radius for color averaging
        /// </summary>
        public void SetSamplingRadius(int radius)
        {
            samplingRadius = Mathf.Clamp(radius, 1, 10);
        }
        
        /// <summary>
        /// Enable or disable lighting compensation features
        /// </summary>
        public void SetLightingCompensation(bool whiteBalance, bool exposure, float strength)
        {
            enableWhiteBalance = whiteBalance;
            enableExposureCompensation = exposure;
            lightingCompensationStrength = Mathf.Clamp01(strength);
        }
        
        private void OnDestroy()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived -= OnCameraFrameReceived;
            }
            
            if (cameraTexture != null)
            {
                Destroy(cameraTexture);
            }
        }
    }
    
    /// <summary>
    /// Lighting condition enumeration
    /// </summary>
    public enum LightingCondition
    {
        Indoor,
        Outdoor,
        Mixed
    }
    
    /// <summary>
    /// Color calibration data structure
    /// </summary>
    [System.Serializable]
    public class ColorCalibrationData
    {
        public Matrix4x4 calibrationMatrix;
        public bool isCalibrated;
        public DateTime calibrationDate;
    }
}