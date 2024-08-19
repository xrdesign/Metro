# Photon Fusion Introduction Demo Game

> Supported SDK Version: Fusion 2.0
> Demo Version: 1.0.0

This demo game for Photon Fusion is a simple pick-and-place game that demonstrates how to use the Photon Fusion API.

## Prerequisites

- Unity 2021.3 LTS or newer
- Text Mesh Pro
- [Photon Fusion SDK](https://doc.photonengine.com/fusion/v2/getting-started/sdk-download)

## Getting Started

*Note: This demo uses URP, if you see any pink textures select the desired material and go to `Edit > Rendering > Materials > Convert Selected Built-in Materials to URP`. The materials used on the demo are located at `Assets\Photon\FusionDemos\IntroSample\Sample\Models\CapsuleDummy\Materials` and `Assets\Photon\FusionDemos\IntroSample\Sample\Models\Materials`. Check the [Render Pipeline Converter](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@12.0/manual/features/rp-converter.html) for a quick conversion.*

1. Import the `Photon Fusion SDK` package into your project.
2. Import the `Photon Fusion Menu` package into your project - it can be found at `Assets\Photon\FusionMenu`.
3. Import the `Text Mesh Pro` package into your project.
4. Import the `Photon Fusion Demo` package into your project.
5. If you have not entered your AppId yet, open `PhotonAppSettings` (`Tools/Fusion/Realtime Settings`) and set the `App Id Fusion`. You can obtain or create one via the [Photon Dashboard](https://dashboard.photonengine.com/).
6. Add the following scenes to the Build Settings:
    - [0] `Assets\Photon\FusionDemos\IntroSample\Sample\Scenes\FusionDemoMenu` 
    - [1] `Assets\Photon\FusionDemos\IntroSample\Sample\Scenes\FusionDemoGameplay`
7. Open the `NetworkProjectConfig` (`Tools/Fusion/Network Project Config`):
    - Update the Prefab Table (click on the `Rebuild Prefab Table`), ensuring Fusion knows all Demo prefabs.
    - Set the `Peer Mode` to `Single`.
    - Click on `Apply` to save the changes.
8. Open the `Assets\Photon\FusionDemos\IntroSample\Sample\Scenes\FusionDemoMenu` scene and press play.

## Playing the Game

The game is a simple pick-and-place game where players must pick up the correct colors and place them on the corresponding colored platforms. It supports up to 4 players.

### Menu

The Menu scene allows you to create a new session or join an existing one.

1. Join a random session by pressing the `Quick Player` button. If no session is available, a new one will be created.
2. Create a new session by pressing the `Party Menu` button and then the `Create` button.
3. Join an existing session by pressing the `Party Menu` button, entering the `Session Code`, and then pressing the `Join` button.

### Controls

- `WASD`: Move.
- `E`: Pick up/Place down the color.
