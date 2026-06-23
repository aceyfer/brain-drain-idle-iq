# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: Satirical mobile idle clicker by AcEclipse Games. Tapping anywhere on the bottom half of the screen harvests brain power. This task adds atmospheric rain, a satisfying touch ripple feedback effect, and looping cyberpunk background music.
- Players: Single player
- Tone / Art Direction: Satirical retro-dystopian cartoon, thick black outlines, neon high-contrast colors, moody cyber atmosphere.
- Target Platform: iOS / Mobile
- Screen Orientation / Resolution: Portrait 1080x1920
- Render Pipeline: UniversalRP (2D)

# Game Mechanics
## Rain VFX (Moody Atmosphere)
- Global rain falling over the dystopian city skyline, adding a somber, high-contrast, cyberpunk feel.

## Touch Ripple Effect
- Every tap on the bottom half click zone (MainTapButton) spawns a clean, satisfiying, expanding neon ring ripple at the finger position, matching our arcade cartoon aesthetic.

## Background Music
- Looping cyberpunk track playing continuously in the background, set to a non-overpowering volume of 0.4, persisting across scene transitions.

# UI
- **Rain particles**: Rendered behind the player character and safe-area HUD, but in front of the skyline backdrop.
- **Touch Ripple**: Rendered right at the pointer tap position on top of the background, underneath floating text.
- **Background Music**: Invisible global audio player.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- Rain Asset: `Assets/Rain Particles/Prefabs/Rain Particles.prefab` (3D Particle System, Box shape 25x25).
- Music Track: `Assets/CyberWare - Game Music Assets/Gutters Filled with Light/Loops/0110_Gutters-Filled-With-Light_G1-1_65bpm4-4_L28M.wav` (BPM 65, moody loop).
- Slices / Sprites for Ripple:
  - `Assets/_Game/Sprites/UI/Generated/NeonRing.png` (hollow ring sprite).
  - `Assets/_Game/Sprites/UI/Generated/RadialGlowGreen.png` (soft glow sprite).

# Implementation Steps

### Step 1: Set Canvas to Screen Space - Camera
- **Description**: 
  - Change `Canvas` renderMode to `ScreenSpaceCamera`.
  - Set `worldCamera` to `Main Camera` (already in the scene).
  - Set `planeDistance` to `10` (standard distance for canvas rendering).
  - This step is completely compatible with `AnimationController.cs` which already contains automatic worldCamera fallbacks.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Integrate Rain Particles under Backdrop
- **Description**:
  - Instantiate `Assets/Rain Particles/Prefabs/Rain Particles.prefab` as a child of `Canvas/BackgroundRoot/BottomBG`. Rename to `RainParticlesInstance`.
  - Set its local position to `(0, 0, 0)`, local rotation to `(0, 0, 0)`, and local scale to `(1, 1, 1)`.
  - Via code/inspector, configure the `ParticleSystem.main.scalingMode` to `ParticleSystemScalingMode.Local` so Canvas UI scaling is ignored.
  - Set its `ParticleSystemRenderer`'s `sortingLayerName` to `Default` and `sortingOrder` to `1` so it renders in front of the background skyline (`0`) but behind the safe area UI elements (which naturally render on top or can be assigned custom sorting if needed).
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Implement Procedural Touch Ripple in AnimationController
- **Description**:
  - Add a public static method `PlayTouchRipple(Vector2 screenPosition, RectTransform parent)` inside `Assets/_Game/Scripts/Systems/AnimationController.cs`.
  - This method will spawn:
    1. A soft glowing green core (`RadialGlowGreen.png`) that expands from scale 0.1 to 1.2 and fades out over 0.3s.
    2. A crisp expanding neon green hollow ring (`NeonRing.png`) that expands from scale 0.1 to 1.8 and fades out over 0.4s.
  - We use the existing coroutine-based lightweight pooling and animation pipeline in `AnimationController` (similar to how `PlaySplatParticles` is written).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 4: Wire Touch Ripple into PlayerTapHandler
- **Description**:
  - Modify `Assets/_Game/Scripts/Core/PlayerTapHandler.cs` inside `OnTap()`:
    - Add a call to `AnimationController.PlayTouchRipple(pointerPosition, particleParent)` alongside splat particles and floating reward texts.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

### Step 5: Implement Loopable Background Music Manager
- **Description**:
  - Create a new C# script named `BackgroundMusicManager.cs` under `Assets/_Game/Scripts/Systems/`.
  - Implement a persistent singleton pattern using `DontDestroyOnLoad(gameObject)`.
  - Include an `AudioSource` component configured with `loop = true`, `volume = 0.4f`, `playOnAwake = true`.
  - Create a new GameObject named `BackgroundMusicManager` in the scene, attach this script to it, and assign the audio clip: `Assets/CyberWare - Game Music Assets/Gutters Filled with Light/Loops/0110_Gutters-Filled-With-Light_G1-1_65bpm4-4_L28M.wav`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Note on Simple Notifications (Omitted)
- Since the "Simple Notifications" local notification package is NOT imported in the project (verified only Plankton remote FCM exists), we will omit setting up local notifications to comply strictly with the "Only use what is imported" rule.

# Verification & Testing
- Read back `Canvas` settings: verify `renderMode` is `ScreenSpaceCamera` and `worldCamera` points to `Main Camera`.
- Verify `RainParticlesInstance` exists, is scaled `(1,1,1)`, and `sortingOrder` is `1`.
- Verify `BackgroundMusicManager` exists in scene, has loop=true, volume=0.4, and the correct track assigned.
- Run Play Mode:
  - Confirm the dark ambient cyberpunk track loops beautifully without clicks or pops.
  - Confirm the rain particles fall smoothly over the city skyline, rendering behind the player character.
  - Tap anywhere on the bottom half of the screen: confirm a highly polished neon ring ripple expands and fades out right at the tap position, accompanying the goo splat particles.
  - Verify console is clean with 0 warnings or errors.
