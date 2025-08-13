# Da Vinci Eye - Mixed Reality Art Creation Application

![Mixed Reality Toolkit](./Images/MRTK_Unity_header.png)

![MRTK3 Banner](./Images/MRTK3_banner.png)

**Da Vinci Eye** is a HoloLens 2 mixed reality application for artists, built on top of the **Mixed Reality Toolkit (MRTK3)**. This application assists artists in creating fine art by providing digital overlay capabilities, allowing users to define physical canvas spaces, overlay reference images with adjustable opacity, and apply various visual filters to enhance the artistic creation process.

This repository contains the complete Da Vinci Eye application built using **MRTK3**, which is the third generation of the Mixed Reality Toolkit for Unity. MRTK3 is built on top of [Unity's XR Interaction Toolkit (XRI)](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.1/manual/index.html) and OpenXR, providing a faster, cleaner, and more modular foundation for cross-platform mixed reality development.

## Da Vinci Eye Application Features

### Core Systems
- **Canvas Management**: Define and track physical canvas boundaries using hand gestures with spatial anchor persistence
- **Image Overlay**: Load reference images with real-time opacity adjustment and image processing capabilities  
- **Filter Processing**: Apply visual filters including grayscale, edge detection, color range filtering, and color reduction
- **Color Analysis**: Pick colors from reference images and capture physical paint colors using HoloLens cameras for matching
- **Input Management**: MRTK-powered hand gesture recognition with near/far interaction modes and voice commands

### Artist Workflow
1. **Canvas Definition**: Use hand gestures to define your physical canvas boundaries
2. **Image Loading**: Import reference images and adjust opacity for overlay on canvas
3. **Filter Application**: Apply real-time visual filters to enhance reference images
4. **Color Matching**: Pick colors from references and capture paint colors for accurate matching
5. **Session Persistence**: Save canvas settings and color history across sessions

### Getting Started with Da Vinci Eye
- Open the main scene: `Assets/DaVinciEye/Scenes/DaVinciEyeMain.unity`
- See detailed documentation: `Assets/DaVinciEye/README.md`
- Build for HoloLens 2 with ARM64 architecture

---

## MRTK3 Foundation - Key improvements

### Architecture

* Built on Unity XR Interaction Toolkit and the Unity Input System.
* Dedicated to OpenXR, with flexibility for other XRSDK backends
* Open-ended and extensible interaction paradigms across devices, platforms, and applications

### Performance

* Rewrote and redesigned most features and systems, from UX to input to subsystems.
* Zero per-frame memory allocation.
* Tuned for maximum performance on HoloLens 2 and other resource-constrained mobile platforms.

### UI

* New interaction models (gaze-pinch indirect manipulation).
* Updated Mixed Reality Design Language.
* Unity Canvas + 3D UX: production-grade dynamic auto-layout.
* Unified 2D & 3D input for gamepad, mouse, and accessibility support.
* Data binding for branding, theming, dynamic data, and complex lists.

## Requirements

MRTK3 requires Unity 2021.3.21 or higher. In addition, you need the [Mixed Reality Feature Tool for Unity](https://aka.ms/mrfeaturetool) to find, download, and add the packages to your project.

## Getting started

[Follow the documentation for setting up MRTK3 packages as dependencies in your project here.](https://learn.microsoft.com/windows/mixed-reality/mrtk-unity/mrtk3-overview/getting-started/setting-up/setup-new-project) Alternatively, you can clone this repo directly to experiment with our template project. However, we *strongly* recommend adding MRTK3 packages as dependencies through the Feature Tool, as it makes updating, managing, and consuming MRTK3 packages far easier and less error-prone.

## Supported devices

| Platform | Supported Devices |
|---|---|
| OpenXR devices | Microsoft HoloLens 2 <br> Magic Leap 2 <br> Meta Quest 1/2 <br> Windows Mixed Reality (experimental) <br> SteamVR (experimental) <br> Oculus Rift on OpenXR (experimental) <br> Varjo XR-3 (experimental) <br> **If your OpenXR device already works with MRTK3, let us know!**
| Windows | Traditional flat-screen desktop (experimental)
| And more coming soon! |

## Versioning

In previous versions of MRTK (HoloToolkit and MRTK v2), all packages were released as a complete set, marked with the same version number (ex: 2.8.0). Starting with MRTK3 GA, each package will be individually versioned, following the [Semantic Versioning 2.0.0 specification](https://semver.org/spec/v2.0.0.html). (As a result, the '3' in MRTK3 is not a version number!)


Individual versioning will enable faster servicing while providing improved developer understanding of the magnitude of changes and reducing the number of packages needing to be updated to acquire the desired fix(es).

For example, if a non-breaking new feature is added to the UX core package, which contains the logic for user interface behavior the minor version number will increase (from 3.0.x to 3.1.0). Since the change is non-breaking, the UX components package, which depends upon UX core, is not required to be updated. 

As a result of this change, there is not a unified MRTK3 product version.

To help identify specific packages and their versions, MRTK3 provides an about dialog that lists the relevant packages included in the project. To access this dialog, select `Mixed Reality` > `MRTK3` > `About MRTK` from the Unity Editor menu.

![About MRTK Panel](Images/AboutMRTK.png)

## Early preview packages

Some parts of MRTK3 are at earlier stages of the development process than others. Early preview packages can be identified in the Mixed Reality Feature Tool and Unity Package Manager by the `Early Preview` designation in their names.

As of June 2022, the following components are considered to be in early preview.

| Name | Package Name |
| --- | --- |
| Accessibility | org.mixedrealitytoolkit.accessibility |
| Data Binding and Theming | org.mixedrealitytoolkit.data |

The MRTK team is fully committed to releasing this functionality. It is important to note that the packages may not contain the complete feature set that is planned to be released or they may undergo major, breaking architectural changes before release.

We very much encourage you to provide any and all feedback to help shape the final form of these early preview features.

## Contributing

This project welcomes contributions, suggestions, and feedback. All contributions, suggestions, and feedback you submitted are accepted under the [Project's license](./LICENSE.md). You represent that if you do not own copyright in the code that you have the authority to submit it under the [Project's license](./LICENSE.md). All feedback, suggestions, or contributions are not confidential.

For more information on how to contribute Mixed Reality Toolkit for Unity Project, please read [CONTRIBUTING.md](./CONTRIBUTING.md).

## Governance

For information on how the Mixed Reality Toolkit for Unity Project is governed, please read [GOVERNANCE.md](./GOVERNANCE.md).

All projects under the Mixed Reality Toolkit organization are governed by the Steering Committee. The Steering Committee is responsible for all technical oversight, project approval and oversight, policy oversight, and trademark management for the Organization. To learn more about the Steering Committee, visit this link: https://github.com/MixedRealityToolkit/MixedRealityToolkit-MVG/blob/main/org-docs/CHARTER.md
