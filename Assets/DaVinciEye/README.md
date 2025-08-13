# Da Vinci Eye - Mixed Reality Art Assistant

## Overview

Da Vinci Eye is a Mixed Reality application for HoloLens 2 that assists artists in creating fine art by providing digital overlay capabilities. The app allows users to define a physical canvas space, overlay reference images with adjustable opacity, and apply various visual filters to enhance the artistic creation process.

## Project Structure

```
Assets/DaVinciEye/
├── Scripts/
│   ├── Core/                    # Main application management
│   │   ├── DaVinciEyeApp.cs    # Main app coordinator
│   │   ├── SceneSetup.cs       # Scene initialization
│   │   └── DaVinciEye.Core.asmdef
│   ├── Canvas/                  # Canvas management system
│   │   ├── ICanvasManager.cs   # Canvas interface
│   │   ├── CanvasData.cs       # Canvas data structures
│   │   └── DaVinciEye.Canvas.asmdef
│   ├── ImageOverlay/            # Image overlay system
│   │   ├── IImageOverlay.cs    # Image overlay interface
│   │   ├── ImageAdjustments.cs # Image adjustment data
│   │   └── DaVinciEye.ImageOverlay.asmdef
│   ├── Filters/                 # Filter processing system
│   │   ├── IFilterProcessor.cs # Filter interface
│   │   ├── FilterTypes.cs      # Filter data structures
│   │   └── DaVinciEye.Filters.asmdef
│   ├── ColorAnalysis/           # Color analysis system
│   │   ├── IColorAnalyzer.cs   # Color analysis interface
│   │   ├── ColorAnalysisTypes.cs # Color data structures
│   │   └── DaVinciEye.ColorAnalysis.asmdef
│   └── Input/                   # Input management system
│       ├── IInputManager.cs    # Input interface
│       ├── InputTypes.cs       # Input data structures
│       └── DaVinciEye.Input.asmdef
├── Scenes/
│   └── DaVinciEyeMain.unity    # Main application scene
└── README.md                   # This file
```

## Core Systems

### 1. Canvas Management System
- **Purpose**: Manages physical canvas definition and spatial tracking
- **Key Features**: 
  - Canvas boundary definition using hand gestures
  - Spatial anchor persistence
  - Canvas visualization and validation

### 2. Image Overlay System
- **Purpose**: Handles reference image loading and display
- **Key Features**:
  - Multi-format image loading (JPEG, PNG, BMP)
  - Real-time opacity adjustment
  - Image cropping and adjustments (contrast, exposure, hue, saturation)

### 3. Filter Processing System
- **Purpose**: Applies real-time visual filters to reference images
- **Key Features**:
  - Standard filters (grayscale, edge detection, contrast enhancement)
  - Color range filtering and isolation
  - Color reduction and palette simplification
  - Filter layering and management

### 4. Color Analysis System
- **Purpose**: Provides color picking and paint matching capabilities
- **Key Features**:
  - Color selection from reference images
  - Physical paint color capture using HoloLens cameras
  - Color comparison and matching accuracy
  - Color history and session management

### 5. Input Management System
- **Purpose**: Handles hand gesture recognition and UI interactions
- **Key Features**:
  - MRTK integration for hand tracking
  - Gesture recognition (air tap, pinch, drag)
  - Near and far interaction modes
  - Voice command support

## Dependencies

### Unity Packages
- Unity 2021.3+ with Universal Render Pipeline (URP)
- Mixed Reality Toolkit (MRTK3)
- XR Interaction Toolkit
- Unity Input System
- AR Foundation
- Graphics Tools

### MRTK Components
- `org.mixedrealitytoolkit.core`
- `org.mixedrealitytoolkit.input`
- `org.mixedrealitytoolkit.uxcomponents`

## Setup Instructions

### 1. Project Configuration
1. Ensure Unity 2021.3+ is installed
2. Install required packages via Package Manager
3. Configure XR settings for HoloLens 2:
   - Enable Windows Mixed Reality provider
   - Set scripting backend to IL2CPP
   - Set API compatibility to .NET Standard 2.1

### 2. Build Settings
- **Platform**: Universal Windows Platform (UWP)
- **Architecture**: ARM64
- **Target Device Family**: Universal
- **Minimum Platform Version**: 10.0.18362.0

### 3. MRTK Configuration
1. Use MRTK Project Configurator: `Window > MRTK3 > Project Configurator`
2. Apply recommended settings for HoloLens 2
3. Import MRTK prefabs for hand controllers

## Application Modes

### Canvas Definition Mode
- Define physical canvas boundaries using hand gestures
- Validate canvas dimensions and position
- Create spatial anchors for persistence

### Image Overlay Mode
- Load reference images from device storage
- Adjust opacity and alignment
- Apply image adjustments (crop, contrast, exposure, hue, saturation)

### Filter Application Mode
- Apply real-time visual filters
- Configure filter parameters
- Layer multiple filters

### Color Analysis Mode
- Pick colors from reference images
- Capture physical paint colors using cameras
- Compare colors and get matching guidance

## Performance Targets

- **Frame Rate**: 60 FPS minimum
- **Memory Usage**: <512MB for large reference images
- **UI Response**: <16ms (1 frame) for interactions
- **Battery Life**: 2+ hours continuous use

## Development Guidelines

### Code Organization
- Each system has its own namespace and assembly definition
- Interfaces define system boundaries and contracts
- Data structures are serializable for persistence
- Event-driven architecture for loose coupling

### Testing Strategy
- Unit tests for core algorithms
- Integration tests for MRTK components
- Performance tests for frame rate and memory
- Device tests on HoloLens 2

### Error Handling
- Graceful degradation when tracking is lost
- Clear user feedback for errors
- Automatic recovery mechanisms
- Comprehensive logging

## Getting Started

1. Open the main scene: `Assets/DaVinciEye/Scenes/DaVinciEyeMain.unity`
2. Configure MRTK settings using the Project Configurator
3. Build and deploy to HoloLens 2
4. Start with Canvas Definition mode to set up your workspace
5. Load reference images and begin creating art!

## Support

For technical issues or questions, refer to:
- MRTK3 Documentation: https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk3-overview/
- Unity XR Documentation: https://docs.unity3d.com/Manual/XR.html
- HoloLens 2 Development Guide: https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/unity-development-overview