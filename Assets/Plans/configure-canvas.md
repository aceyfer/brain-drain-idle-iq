# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: An idle clicker game where players tap to earn Brains and restore decaying IQ, escalating in level and difficulty over time.
- Players: Single player
- Inspiration / Reference Games: Cookie Clicker, Adventure Capitalist
- Tone / Art Direction: 2D Pixel Art / Stylized UI
- Target Platform: iOS
- Screen Orientation / Resolution: Landscape (1920x1080 default, Auto Rotation)
- Render Pipeline: UniversalRP (URP 2D)

# Game Mechanics
## Core Gameplay Loop
- Players tap on a full-screen tapping area to generate Brains (currency) and restore a small amount of global IQ (stat).
- IQ constantly decays based on the current level.
- As cumulative Brains increase, the player's level increases, which escalates the IQ decay rate.

## Controls and Input Methods
- High-frequency tapping on a full-screen invisible area (TapButton) on touch screens (iOS) or mouse clicks (Unity Editor).

# UI
- A clean, transparent full-screen tapping button overlaying the entire scene.
- UI Text elements showing:
  - Top-Center: Brains currency (`BrainsText`)
  - Center-Center: Current IQ level (`IQText`)
  - Bottom-Center: Current player level (`LevelText`)

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- Hierarchy inside `Canvas`:
  - `TapButton` (Button + Image, child of Canvas)
    - `Text (TMP)` (to be disabled/removed, child of TapButton)
  - `BrainsText` (new TextMeshProUGUI, child of Canvas)
  - `IQText` (new TextMeshProUGUI, child of Canvas)
  - `LevelText` (new TextMeshProUGUI, child of Canvas)

# Implementation Steps

### Step 1: Configure TapButton RectTransform, Image & Tap Handler
- **Description**: Open the active scene `SampleScene.unity`. Locate `TapButton` inside `Canvas`. Set its RectTransform Anchors to completely stretch and fill the parent Canvas (Min: 0,0 and Max: 1,1). Set left, top, right, and bottom offsets all to 0. Set the alpha of the Image component to 0 so it acts as an invisible button background. Disable or remove the default child `Text (TMP)` under `TapButton`. Attach the `PlayerTapHandler` component to the `TapButton` GameObject. Configure the `On Click()` event on the `Button` component to target the `TapButton` itself and trigger `PlayerTapHandler.OnTap()`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Configure BrainsText
- **Description**: Locate `BrainsText` in the scene (or create it as a direct child of the `Canvas` if missing). Set its RectTransform Anchors to Top-Center (Min: 0.5, 1; Max: 0.5, 1) and Pivot to (0.5, 1). Position it with Anchored Position X: 0, Y: -150. Configure the text value to "0 BRAINS", font size 36, and alignment to Center.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 3: Configure IQText
- **Description**: Locate `IQText` in the scene (or create it as a direct child of the `Canvas` if missing). Set its RectTransform Anchors to Center-Center (Min: 0.5, 0.5; Max: 0.5, 0.5) and Pivot to (0.5, 0.5). Position it at dead-center with Anchored Position X: 0, Y: 0. Configure the text value to "IQ: 100", font size 48, and alignment to Center.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 4: Configure LevelText
- **Description**: Locate `LevelText` in the scene (or create it as a direct child of the `Canvas` if missing). Set its RectTransform Anchors to Bottom-Center (Min: 0.5, 0; Max: 0.5, 0) and Pivot to (0.5, 0). Position it with Anchored Position X: 0, Y: 150. Configure the text value to "LEVEL 1", font size 36, and alignment to Center.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 5: Ensure Core Systems on _Systems GameObject
- **Description**: Verify that the `_Systems` GameObject in the scene has `GameManager`, `IQDecaySystem`, and `CurrencyManager` components attached to it.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 6: Create HUDController Script
- **Description**: Create the file `Assets/_Game/Scripts/UI/HUDController.cs` if it does not exist. Implement a component that references `brainsText`, `iqText`, and `levelText`. Subscribe to the events of `CurrencyManager` (`OnBrainsChanged`) and `IQDecaySystem` (`OnIQChanged`, `OnLevelChanged`) and update the UI texts dynamically.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 7: Attach and Configure HUDController on Canvas
- **Description**: Attach the `HUDController` component to the `Canvas` GameObject. Assign the UI text fields by dragging `BrainsText`, `IQText`, and `LevelText` into the respective serializable fields on the `HUDController` component.
- **Assigned role**: developer
- **Dependencies**: Step 2, Step 3, Step 4, Step 6
- **Parallelizable**: No

# Verification & Testing
- Open `SampleScene` in Unity.
- Verify `TapButton` covers the entire screen area in the Game view and is fully invisible.
- Verify `BrainsText` is anchored at top-center at Y: -150.
- Verify `IQText` is anchored at dead-center at Y: 0.
- Verify `LevelText` is anchored at bottom-center at Y: 150.
- Verify that `HUDController` is attached to `Canvas` with all text references fully assigned.
- Verify `PlayerTapHandler` is attached to `TapButton` and the button's `onClick` event is set up to trigger `PlayerTapHandler.OnTap()`.
- Enter Play Mode and verify:
  - Tapping anywhere on the screen registers taps and awards Brains.
  - The text displays update dynamically in response to taps and idle ticks.
  - No NullReferenceExceptions or errors in console logs.
