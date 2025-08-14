param(
    [Parameter(Mandatory=$true)]
    [string]$HoloLensIP,
    
    [Parameter(Mandatory=$false)]
    [string]$BuildPath = "Builds\HoloLens2",
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$LaunchApp = $true
)

# Da Vinci Eye HoloLens 2 Deployment Script
# ==========================================

Write-Host ""
Write-Host "🎨 Da Vinci Eye HoloLens 2 Deployment Script" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Target Device: $HoloLensIP" -ForegroundColor White
Write-Host "Build Path: $BuildPath" -ForegroundColor White
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host ""

# Function to check if a command exists
function Test-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

# Function to find MSBuild
function Find-MSBuild {
    $msbuildPaths = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )
    
    foreach ($path in $msbuildPaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    
    return $null
}

# Function to find WinAppDeployCmd
function Find-WinAppDeployCmd {
    $deployPaths = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64\WinAppDeployCmd.exe",
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22000.0\x64\WinAppDeployCmd.exe",
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.19041.0\x64\WinAppDeployCmd.exe",
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.18362.0\x64\WinAppDeployCmd.exe"
    )
    
    foreach ($path in $deployPaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    
    return $null
}

# Validate prerequisites
Write-Host "🔍 Checking prerequisites..." -ForegroundColor Yellow

# Check if Unity build exists
if (!(Test-Path "$BuildPath\DaVinciEye.sln")) {
    Write-Host "❌ Unity build not found at $BuildPath\DaVinciEye.sln" -ForegroundColor Red
    Write-Host "   Please build the project in Unity first:" -ForegroundColor Red
    Write-Host "   1. Open Unity project" -ForegroundColor Red
    Write-Host "   2. File → Build Settings" -ForegroundColor Red
    Write-Host "   3. Select Universal Windows Platform" -ForegroundColor Red
    Write-Host "   4. Click Build and select $BuildPath folder" -ForegroundColor Red
    exit 1
}

# Find MSBuild
$msbuildPath = Find-MSBuild
if (!$msbuildPath) {
    Write-Host "❌ MSBuild not found. Please install Visual Studio 2019 or 2022 with UWP workload." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Found MSBuild: $msbuildPath" -ForegroundColor Green

# Find WinAppDeployCmd
$winAppDeployCmd = Find-WinAppDeployCmd
if (!$winAppDeployCmd) {
    Write-Host "❌ WinAppDeployCmd not found. Please install Windows 10/11 SDK." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Found WinAppDeployCmd: $winAppDeployCmd" -ForegroundColor Green

# Test HoloLens connectivity
Write-Host "🔗 Testing HoloLens connectivity..." -ForegroundColor Yellow
$pingResult = Test-NetConnection -ComputerName $HoloLensIP -Port 80 -InformationLevel Quiet -WarningAction SilentlyContinue
if (!$pingResult) {
    Write-Host "⚠️  Cannot reach HoloLens at $HoloLensIP" -ForegroundColor Yellow
    Write-Host "   Make sure:" -ForegroundColor Yellow
    Write-Host "   - HoloLens is connected to the same network" -ForegroundColor Yellow
    Write-Host "   - Developer mode is enabled on HoloLens" -ForegroundColor Yellow
    Write-Host "   - Device Portal is enabled" -ForegroundColor Yellow
    $continue = Read-Host "Continue anyway? (y/N)"
    if ($continue -ne "y" -and $continue -ne "Y") {
        exit 1
    }
} else {
    Write-Host "✅ HoloLens is reachable at $HoloLensIP" -ForegroundColor Green
}

# Build the solution
if (!$SkipBuild) {
    Write-Host ""
    Write-Host "🔨 Building Visual Studio solution..." -ForegroundColor Yellow
    Write-Host "   Configuration: $Configuration" -ForegroundColor White
    Write-Host "   Platform: ARM64" -ForegroundColor White
    
    $buildArgs = @(
        "$BuildPath\DaVinciEye.sln",
        "/p:Configuration=$Configuration",
        "/p:Platform=ARM64",
        "/p:AppxBundlePlatforms=ARM64",
        "/p:AppxPackageDir=$BuildPath\AppPackages\",
        "/p:AppxBundle=Always",
        "/p:UapAppxPackageBuildMode=StoreUpload",
        "/verbosity:minimal"
    )
    
    $buildProcess = Start-Process -FilePath $msbuildPath -ArgumentList $buildArgs -Wait -PassThru -NoNewWindow
    
    if ($buildProcess.ExitCode -ne 0) {
        Write-Host "❌ Build failed with exit code $($buildProcess.ExitCode)" -ForegroundColor Red
        Write-Host "   Check the build output above for errors." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "⏭️  Skipping build (SkipBuild flag set)" -ForegroundColor Yellow
}

# Find the built app package
Write-Host ""
Write-Host "📦 Locating app package..." -ForegroundColor Yellow

$appxFiles = Get-ChildItem -Path "$BuildPath" -Filter "*.appx" -Recurse | Where-Object { $_.Name -like "*ARM64*" -or $_.Directory.Name -like "*ARM64*" }

if (!$appxFiles) {
    # Try alternative locations
    $appxFiles = Get-ChildItem -Path "$BuildPath\AppPackages" -Filter "*.appx" -Recurse -ErrorAction SilentlyContinue
}

if (!$appxFiles) {
    Write-Host "❌ No .appx file found!" -ForegroundColor Red
    Write-Host "   Expected locations:" -ForegroundColor Red
    Write-Host "   - $BuildPath\**\*.appx" -ForegroundColor Red
    Write-Host "   - $BuildPath\AppPackages\**\*.appx" -ForegroundColor Red
    exit 1
}

$appxPath = $appxFiles[0].FullName
Write-Host "✅ Found app package: $appxPath" -ForegroundColor Green

# Check if app is already installed and uninstall if needed
Write-Host ""
Write-Host "🔍 Checking for existing installation..." -ForegroundColor Yellow

$checkArgs = @("list", "-ip", $HoloLensIP)
$installedApps = & $winAppDeployCmd @checkArgs 2>$null

if ($installedApps -match "DaVinciEye" -or $installedApps -match "com\..*\.davincieye") {
    Write-Host "📱 Found existing installation, uninstalling..." -ForegroundColor Yellow
    
    # Extract package name from installed apps list
    $packageLine = $installedApps | Where-Object { $_ -match "DaVinciEye" -or $_ -match "davincieye" } | Select-Object -First 1
    if ($packageLine -match "(\S+)\s+:\s+(.+)") {
        $packageName = $matches[1]
        $uninstallArgs = @("uninstall", "-package", $packageName, "-ip", $HoloLensIP)
        & $winAppDeployCmd @uninstallArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Previous version uninstalled successfully" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Uninstall may have failed, continuing with installation..." -ForegroundColor Yellow
        }
    }
}

# Deploy to HoloLens
Write-Host ""
Write-Host "🚀 Deploying Da Vinci Eye to HoloLens 2..." -ForegroundColor Green
Write-Host "   Target: $HoloLensIP" -ForegroundColor White
Write-Host "   Package: $(Split-Path $appxPath -Leaf)" -ForegroundColor White

$deployArgs = @("install", "-file", $appxPath, "-ip", $HoloLensIP)
$deployProcess = Start-Process -FilePath $winAppDeployCmd -ArgumentList $deployArgs -Wait -PassThru -NoNewWindow

if ($deployProcess.ExitCode -eq 0) {
    Write-Host ""
    Write-Host "✅ Deployment successful!" -ForegroundColor Green
    Write-Host "🎉 Da Vinci Eye is now installed on your HoloLens 2!" -ForegroundColor Cyan
    
    if ($LaunchApp) {
        Write-Host ""
        Write-Host "🚀 Attempting to launch the app..." -ForegroundColor Yellow
        
        # Try to launch the app (this may not work on all HoloLens versions)
        $launchArgs = @("launch", "-package", "com.yourcompany.davincieye_1.0.0.0_arm64__pzq3xp76mxafg", "-ip", $HoloLensIP)
        & $winAppDeployCmd @launchArgs 2>$null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ App launched successfully!" -ForegroundColor Green
        } else {
            Write-Host "ℹ️  Auto-launch failed. Please launch manually from HoloLens Start menu." -ForegroundColor Cyan
        }
    }
    
    Write-Host ""
    Write-Host "📋 Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Put on your HoloLens 2" -ForegroundColor White
    Write-Host "2. Open the Start menu (bloom gesture or start button)" -ForegroundColor White
    Write-Host "3. Look for 'Da Vinci Eye' app" -ForegroundColor White
    Write-Host "4. Air tap to launch" -ForegroundColor White
    Write-Host "5. Follow the in-app tutorial to get started" -ForegroundColor White
    Write-Host ""
    Write-Host "🎨 Happy creating with Da Vinci Eye! ✨" -ForegroundColor Cyan
    
} else {
    Write-Host ""
    Write-Host "❌ Deployment failed with exit code $($deployProcess.ExitCode)" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔧 Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "1. Ensure HoloLens Developer Mode is enabled" -ForegroundColor White
    Write-Host "2. Check that Device Portal is accessible at https://$HoloLensIP" -ForegroundColor White
    Write-Host "3. Try restarting the HoloLens device" -ForegroundColor White
    Write-Host "4. Verify the HoloLens and PC are on the same network" -ForegroundColor White
    Write-Host "5. Temporarily disable Windows Firewall" -ForegroundColor White
    Write-Host ""
    Write-Host "For more help, see the deployment guide: Assets/DaVinciEye/Deployment/HoloLens2_Deployment_Guide.md" -ForegroundColor Cyan
    exit 1
}

Write-Host ""
Write-Host "🏁 Deployment script completed!" -ForegroundColor Green