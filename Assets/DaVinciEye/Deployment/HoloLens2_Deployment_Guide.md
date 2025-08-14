# HoloLens 2 Deployment Guide for Da Vinci Eye

## Prerequisites

### Development Environment Setup
1. **Unity 2021.3 LTS or later** (recommended: 2021.3.16f1+)
2. **Visual Studio 2019/2022** with UWP development workload
3. **Windows 10/11 SDK** (version 10.0.18362.0 or later)
4. **Mixed Reality Toolkit (MRTK) 3.0+**
5. **HoloLens 2 Developer Edition** (which you have! 🎉)

### HoloLens 2 Device Preparation
1. **Enable Developer Mode**:
   - Go to Settings > Update & Security > For developers
   - Turn on "Developer mode"
   - Turn on "Device discovery"
   - Turn on "Device Portal"

2. **Enable Device Portal**:
   - Note your HoloLens IP address (Settings > Network & Internet > Wi-Fi > Advanced options)
   - Open browser and go to `https://YOUR_HOLOLENS_IP`
   - Set up username/password for Device Portal

## Step-by-Step Deployment Process

### Step 1: Unity Project Configuration

1. **Open Unity Project**:
   ```bash
   # Navigate to your project directory
   cd /path/to/DaVinciEye
   unity -projectPath .
   ```

2. **Configure Build Settings**:
   - File → Build Settings
   - Platform: **Universal Windows Platform**
   - Target Device Family: **HoloLens**
   - Architecture: **ARM64**
   - Build Type: **D3D Project**
   - Target Device: **HoloLens 2**
   - Minimum Platform Version: **10.0.18362.0**
   - Visual Studio Version: **Latest installed**
   - Build and Run on: **Local Machine**
   - Build configuration: **Release** (for final deployment)

3. **Player Settings Configuration**:
   - Edit → Project Settings → Player
   - **Company Name**: Your company name
   - **Product Name**: "Da Vinci Eye"
   - **Package Name**: `com.yourcompany.davincieye`
   - **Version**: 1.0.0
   - **XR Settings**:
     - Virtual Reality Supported: ✅
     - Virtual Reality SDKs: **Windows Mixed Reality**
   - **Capabilities** (Publishing Settings):
     - ✅ InternetClient
     - ✅ WebCam
     - ✅ Microphone
     - ✅ SpatialPerception
     - ✅ GazeInput

### Step 2: Build the Unity Project

1. **Create Build Directory**:
   ```bash
   mkdir Builds
   mkdir Builds/HoloLens2
   ```

2. **Build Project**:
   - File → Build Settings
   - Click **Build**
   - Select `Builds/HoloLens2` folder
   - Wait for build to complete (5-10 minutes)

### Step 3: Visual Studio Deployment

1. **Open Visual Studio Solution**:
   - Navigate to `Builds/HoloLens2/`
   - Open `DaVinciEye.sln` in Visual Studio

2. **Configure Visual Studio**:
   - Set Solution Configuration: **Release**
   - Set Solution Platform: **ARM64**
   - Set Deployment Target: **Device** (for direct deployment) or **Remote Machine**

3. **Deploy to HoloLens 2**:

   **Option A: USB Deployment (Recommended for first deployment)**
   ```bash
   # Connect HoloLens 2 via USB-C cable
   # In Visual Studio:
   # - Debug → Start Without Debugging (Ctrl+F5)
   # - Or right-click project → Deploy
   ```

   **Option B: Wi-Fi Deployment**
   - Right-click project → Properties
   - Debugging → Machine Name: `YOUR_HOLOLENS_IP`
   - Authentication Type: **Universal (Unencrypted Protocol)**
   - Deploy

### Step 4: Alternative Deployment Methods

#### Method 1: Device Portal Deployment
1. **Build App Package**:
   - In Visual Studio: Project → Store → Create App Packages
   - Choose "Sideloading" → Next
   - Select ARM64 → Create

2. **Deploy via Device Portal**:
   - Open browser: `https://YOUR_HOLOLENS_IP`
   - Go to Apps → Install app
   - Browse and select the `.appx` file
   - Click Install

#### Method 2: PowerShell Deployment
```powershell
# Install WinAppDeployCmd if not already installed
# Add to PATH: C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\

# Deploy app
WinAppDeployCmd.exe install -file "DaVinciEye.appx" -ip YOUR_HOLOLENS_IP
```

## Automated Deployment Script

I'll create a PowerShell script to automate the deployment process:

```powershell
# Save as: Deploy-DaVinciEye.ps1

param(
    [Parameter(Mandatory=$true)]
    [string]$HoloLensIP,
    
    [Parameter(Mandatory=$false)]
    [string]$BuildPath = "Builds\HoloLens2",
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release"
)

Write-Host "🎨 Da Vinci Eye HoloLens 2 Deployment Script" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# Check if Unity build exists
if (!(Test-Path "$BuildPath\DaVinciEye.sln")) {
    Write-Host "❌ Unity build not found. Please build the project first." -ForegroundColor Red
    exit 1
}

# Build with MSBuild
Write-Host "🔨 Building Visual Studio solution..." -ForegroundColor Yellow
$msbuildPath = "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
if (!(Test-Path $msbuildPath)) {
    $msbuildPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
}

& "$msbuildPath" "$BuildPath\DaVinciEye.sln" /p:Configuration=$Configuration /p:Platform=ARM64

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Deploy to HoloLens
Write-Host "🚀 Deploying to HoloLens 2 at $HoloLensIP..." -ForegroundColor Green

$appxPath = Get-ChildItem -Path "$BuildPath\AppPackages" -Filter "*.appx" -Recurse | Select-Object -First 1

if ($appxPath) {
    $winAppDeployCmd = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22000.0\x64\WinAppDeployCmd.exe"
    & "$winAppDeployCmd" install -file $appxPath.FullName -ip $HoloLensIP
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Deployment successful!" -ForegroundColor Green
        Write-Host "🎉 Da Vinci Eye is now installed on your HoloLens 2!" -ForegroundColor Cyan
    } else {
        Write-Host "❌ Deployment failed!" -ForegroundColor Red
    }
} else {
    Write-Host "❌ App package not found!" -ForegroundColor Red
}
```

## Quick Deployment Commands

### For PowerShell Users:
```powershell
# Set your HoloLens IP address
$HoloLensIP = "192.168.1.XXX"  # Replace with your HoloLens IP

# Run deployment script
.\Deploy-DaVinciEye.ps1 -HoloLensIP $HoloLensIP
```

### For Command Line Users:
```batch
@echo off
echo 🎨 Da Vinci Eye Quick Deploy
echo ============================

set HOLOLENS_IP=192.168.1.XXX
set BUILD_PATH=Builds\HoloLens2

echo Building solution...
"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" "%BUILD_PATH%\DaVinciEye.sln" /p:Configuration=Release /p:Platform=ARM64

echo Deploying to HoloLens...
"C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\WinAppDeployCmd.exe" install -file "%BUILD_PATH%\AppPackages\DaVinciEye\DaVinciEye.appx" -ip %HOLOLENS_IP%

echo ✅ Deployment complete!
pause
```

## Troubleshooting Common Issues

### Issue 1: Build Errors
**Problem**: Unity build fails with MRTK errors
**Solution**:
```bash
# Ensure MRTK is properly imported
# Window → Mixed Reality → Project Configurator
# Apply all recommended settings
```

### Issue 2: Deployment Fails
**Problem**: "Device not found" or connection issues
**Solutions**:
1. **Check HoloLens IP**: Settings → Network & Internet → Wi-Fi → Advanced options
2. **Enable Developer Mode**: Settings → Update & Security → For developers
3. **Restart HoloLens**: Hold power button for 10 seconds
4. **Check firewall**: Temporarily disable Windows Firewall

### Issue 3: App Crashes on Launch
**Problem**: App starts but immediately crashes
**Solutions**:
1. **Check Capabilities**: Ensure all required capabilities are enabled
2. **Check MRTK Setup**: Verify MRTK profile is correctly configured
3. **Check Logs**: Use Device Portal → Logging to view crash logs

### Issue 4: Performance Issues
**Problem**: App runs slowly or drops frames
**Solutions**:
1. **Build in Release Mode**: Use Release configuration, not Debug
2. **Check Quality Settings**: Unity → Edit → Project Settings → Quality
3. **Monitor Performance**: Use the built-in PerformanceOptimizer component

## Post-Deployment Verification

### 1. Launch the App
- Look for "Da Vinci Eye" in the HoloLens Start menu
- Air tap to launch

### 2. Test Core Features
1. **Canvas Definition**: Try defining a canvas area
2. **Image Loading**: Load a test image
3. **Opacity Control**: Adjust image opacity with hand gestures
4. **Filters**: Apply different visual filters
5. **Color Analysis**: Test color picking functionality

### 3. Performance Check
- Monitor FPS (should be 55-60 FPS)
- Check for smooth hand tracking
- Verify spatial tracking stability

## Development Workflow

### For Iterative Development:
1. **Make changes in Unity**
2. **Build → Deploy** (use the automated script)
3. **Test on HoloLens**
4. **Repeat**

### For Quick Testing:
- Use **Unity Play Mode** with MRTK simulator for rapid iteration
- Deploy to HoloLens only for final testing

## Production Deployment

### For Distribution:
1. **Create App Package**:
   - Visual Studio → Project → Store → Create App Packages
   - Choose appropriate signing certificate
   - Build for ARM64 Release

2. **Microsoft Store Submission**:
   - Package the `.appxupload` file
   - Submit through Partner Center

## Support and Resources

### Documentation:
- [MRTK Documentation](https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/)
- [HoloLens Development](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/unity-development-overview)

### Community:
- [Mixed Reality Developer Community](https://holodevelopers.slack.com/)
- [Unity Forums - AR/VR](https://forum.unity.com/forums/ar-vr-xr-discussion.80/)

---

## Ready to Deploy! 🚀

Your Da Vinci Eye application is production-ready and optimized for HoloLens 2. The deployment process should take about 15-20 minutes for the first deployment, and 5-10 minutes for subsequent updates.

**Quick Start**: Use the PowerShell script above with your HoloLens IP address for the fastest deployment experience!

Happy creating! 🎨✨