
# Metro

This is the unity project for the Human-AI Teaming mini metro clone for VR.

## API

API Documentation is available in [API.md](https://github.com/xrdesign/Metro/blob/main/API.md).

## Getting Started

Install Unity, this project is using Unity version 2022.3.45f1.

To get started using this repo you will have to import the MRTK manually (note: Oculus Integration packages is no longer needed), as I didn't want to bloat the git repo with readily available packages.

### Import MRTK (No longer needed since they are already included)

The project currently uses MRTK 2.8.3, 
Install the following packages into the Metro unity project directory:
- "com.microsoft.mixedreality.toolkit.extensions"
- "com.microsoft.mixedreality.toolkit.foundation"
- "com.microsoft.mixedreality.toolkit.standardassets"
- "com.microsoft.mixedreality.toolkit.testutilities"
- "com.microsoft.mixedreality.toolkit.tools"
- "com.microsoft.mixedreality.openxr" (under "Platform Support", Version == 1.11.2)

##### Windows
To install use the microsoft feature tool:
[https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/welcome-to-mr-feature-tool](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/welcome-to-mr-feature-tool)

##### Mac
MRTK packages can be directly downloaded from [https://github.com/microsoft/MixedRealityToolkit-Unity/releases/](https://github.com/microsoft/MixedRealityToolkit-Unity/releases/)

### Additional Steps

- Upgrad MRTK shaders for Universal Render Pipeline: click menu item "Mixed Reality > Toolkit > Utilities > Upgrade MRTK Standard Shader for Universal Render Pipeline"

- Open _Metro/Scenes/Metro.unity and select Import TMPro Essentials from the popup

- "The controllers are pink! How to fix?" 
- - Right-click on `Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/StandardAssets/Controllers/Visuals/Models` and `Reimport`


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



