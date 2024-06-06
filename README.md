
# Metro

This is the unity project for the Human-AI Teaming mini metro clone for VR.

## API

API Documentation is available in [API.md](https://github.com/xrdesign/Metro/blob/main/API.md).

## Getting Started

Install Unity, this project is using Unity version 2020.3.28f1.

To get started using this repo you will have to import the MRTK and Oculus Integration packages manually, as I didn't want to bloat the git repo with readily available packages.

### Import MRTK 

The project currently uses MRTK 2.8, 
Install the following packages into the Metro unity project directory:
- "com.microsoft.mixedreality.toolkit.extensions"
- "com.microsoft.mixedreality.toolkit.foundation"
- "com.microsoft.mixedreality.toolkit.standardassets"
- "com.microsoft.mixedreality.toolkit.testutilities"
- "com.microsoft.mixedreality.toolkit.tools"

#### Windows
To install use the microsoft feature tool:
[https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/welcome-to-mr-feature-tool](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/welcome-to-mr-feature-tool)

#### Mac
MRTK packages can be directly downloaded from [https://github.com/microsoft/MixedRealityToolkit-Unity/releases/](github)


### Import Oculus Integration

The project currently uses Oculus Integration SDK v40.0, to install download from:

[https://developer.oculus.com/downloads/package/unity-integration/40.0](https://developer.oculus.com/downloads/package/unity-integration/40.0)

Open the Unity editor, import the Oculus Integration package by clicking the menu item "Assets > Import Package > Custom Package.." navigate to the downloaded SDK file.


### Additional Steps

- Run Oculus / MRTK integration: click menu item "Mixed Reality > Toolkit > Utilities > Oculus > Integrate Oculus Integration Unity Modules"

- Upgrad MRTK shaders for Universal Render Pipeline: click menu item "Mixed Reality > Toolkit > Utilities > Upgrade MRTK Standard Shader for Universal Render Pipeline"

- Open _Metro/Scenes/Metro.unity and select Import TMPro Essentials from the popup


## TODOs

- Main UI / Heads up Display
    - Main menu: 
    - Heads up Display for Score
    - Oveview map
    - Current Time / time controls
    - List of Lines / reset Line

- Passenger display
    - 2D bubble frames
    - passenger shape icons

- Line pathways as nice curves

- Game logic
    - passenger pickup / dropoff pathfinding
    - tuning station / passenger spawning logic and probabilities

- VR interface
    - drag to connect stations
    - drag track to insert station
    - drag trains / cars to move to other lines



