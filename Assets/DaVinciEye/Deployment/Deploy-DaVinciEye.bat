@echo off
setlocal enabledelayedexpansion

:: Da Vinci Eye HoloLens 2 Deployment Script (Batch Version)
:: ===========================================================

echo.
echo 🎨 Da Vinci Eye HoloLens 2 Deployment Script
echo =============================================

:: Configuration
set "BUILD_PATH=Builds\HoloLens2"
set "CONFIGURATION=Release"
set "PLATFORM=ARM64"

:: Get HoloLens IP from user if not provided
if "%1"=="" (
    echo.
    echo Please provide your HoloLens 2 IP address.
    echo You can find this in: Settings ^> Network ^& Internet ^> Wi-Fi ^> Advanced options
    echo.
    set /p HOLOLENS_IP="Enter HoloLens IP address: "
) else (
    set "HOLOLENS_IP=%1"
)

echo.
echo Target Device: %HOLOLENS_IP%
echo Build Path: %BUILD_PATH%
echo Configuration: %CONFIGURATION%
echo Platform: %PLATFORM%
echo.

:: Check if Unity build exists
if not exist "%BUILD_PATH%\DaVinciEye.sln" (
    echo ❌ Unity build not found at %BUILD_PATH%\DaVinciEye.sln
    echo.
    echo Please build the project in Unity first:
    echo 1. Open Unity project
    echo 2. File → Build Settings
    echo 3. Select Universal Windows Platform
    echo 4. Click Build and select %BUILD_PATH% folder
    echo.
    pause
    exit /b 1
)

:: Find MSBuild
echo 🔍 Locating MSBuild...
set "MSBUILD_PATH="

:: Check Visual Studio 2022 locations
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)

:: Check Visual Studio 2019 locations if 2022 not found
if "%MSBUILD_PATH%"=="" (
    if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
    ) else if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    ) else if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )
)

if "%MSBUILD_PATH%"=="" (
    echo ❌ MSBuild not found. Please install Visual Studio 2019 or 2022 with UWP workload.
    pause
    exit /b 1
)

echo ✅ Found MSBuild: %MSBUILD_PATH%

:: Find WinAppDeployCmd
echo 🔍 Locating WinAppDeployCmd...
set "DEPLOY_CMD="

:: Check different Windows SDK versions
if exist "%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.22621.0\x64\WinAppDeployCmd.exe" (
    set "DEPLOY_CMD=%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.22621.0\x64\WinAppDeployCmd.exe"
) else if exist "%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.22000.0\x64\WinAppDeployCmd.exe" (
    set "DEPLOY_CMD=%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.22000.0\x64\WinAppDeployCmd.exe"
) else if exist "%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.19041.0\x64\WinAppDeployCmd.exe" (
    set "DEPLOY_CMD=%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.19041.0\x64\WinAppDeployCmd.exe"
) else if exist "%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.18362.0\x64\WinAppDeployCmd.exe" (
    set "DEPLOY_CMD=%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.18362.0\x64\WinAppDeployCmd.exe"
)

if "%DEPLOY_CMD%"=="" (
    echo ❌ WinAppDeployCmd not found. Please install Windows 10/11 SDK.
    pause
    exit /b 1
)

echo ✅ Found WinAppDeployCmd: %DEPLOY_CMD%

:: Test HoloLens connectivity
echo 🔗 Testing HoloLens connectivity...
ping -n 1 -w 3000 %HOLOLENS_IP% >nul 2>&1
if errorlevel 1 (
    echo ⚠️  Cannot reach HoloLens at %HOLOLENS_IP%
    echo.
    echo Make sure:
    echo - HoloLens is connected to the same network
    echo - Developer mode is enabled on HoloLens
    echo - Device Portal is enabled
    echo.
    set /p continue="Continue anyway? (y/N): "
    if /i not "!continue!"=="y" exit /b 1
) else (
    echo ✅ HoloLens is reachable at %HOLOLENS_IP%
)

:: Build the solution
echo.
echo 🔨 Building Visual Studio solution...
echo Configuration: %CONFIGURATION%
echo Platform: %PLATFORM%
echo.

"%MSBUILD_PATH%" "%BUILD_PATH%\DaVinciEye.sln" /p:Configuration=%CONFIGURATION% /p:Platform=%PLATFORM% /p:AppxBundlePlatforms=%PLATFORM% /p:AppxPackageDir=%BUILD_PATH%\AppPackages\ /p:AppxBundle=Always /verbosity:minimal

if errorlevel 1 (
    echo.
    echo ❌ Build failed!
    echo Check the build output above for errors.
    pause
    exit /b 1
)

echo ✅ Build completed successfully!

:: Find the built app package
echo.
echo 📦 Locating app package...

:: Look for .appx files in common locations
set "APPX_FILE="
for /r "%BUILD_PATH%" %%f in (*.appx) do (
    set "APPX_FILE=%%f"
    goto :found_appx
)

:found_appx
if "%APPX_FILE%"=="" (
    echo ❌ No .appx file found!
    echo Expected locations:
    echo - %BUILD_PATH%\**\*.appx
    echo - %BUILD_PATH%\AppPackages\**\*.appx
    pause
    exit /b 1
)

echo ✅ Found app package: %APPX_FILE%

:: Check for existing installation
echo.
echo 🔍 Checking for existing installation...
"%DEPLOY_CMD%" list -ip %HOLOLENS_IP% 2>nul | findstr /i "davincieye" >nul
if not errorlevel 1 (
    echo 📱 Found existing installation, uninstalling...
    :: Note: Batch version doesn't extract package name automatically
    :: User may need to uninstall manually if this fails
    "%DEPLOY_CMD%" uninstall -package "com.yourcompany.davincieye" -ip %HOLOLENS_IP% 2>nul
    echo ✅ Previous version uninstalled
)

:: Deploy to HoloLens
echo.
echo 🚀 Deploying Da Vinci Eye to HoloLens 2...
echo Target: %HOLOLENS_IP%
echo Package: %APPX_FILE%
echo.

"%DEPLOY_CMD%" install -file "%APPX_FILE%" -ip %HOLOLENS_IP%

if errorlevel 1 (
    echo.
    echo ❌ Deployment failed!
    echo.
    echo 🔧 Troubleshooting tips:
    echo 1. Ensure HoloLens Developer Mode is enabled
    echo 2. Check that Device Portal is accessible at https://%HOLOLENS_IP%
    echo 3. Try restarting the HoloLens device
    echo 4. Verify the HoloLens and PC are on the same network
    echo 5. Temporarily disable Windows Firewall
    echo.
    echo For more help, see: Assets\DaVinciEye\Deployment\HoloLens2_Deployment_Guide.md
    pause
    exit /b 1
)

:: Success!
echo.
echo ✅ Deployment successful!
echo 🎉 Da Vinci Eye is now installed on your HoloLens 2!
echo.
echo 📋 Next Steps:
echo 1. Put on your HoloLens 2
echo 2. Open the Start menu (bloom gesture or start button)
echo 3. Look for 'Da Vinci Eye' app
echo 4. Air tap to launch
echo 5. Follow the in-app tutorial to get started
echo.
echo 🎨 Happy creating with Da Vinci Eye! ✨
echo.

pause