# Project Overview
- **Game Title**: Brain Drain: Idle IQ
- **High-Level Concept**: A satirical idle clicker game where players tap to earn Brains and restore decaying IQ, escalating in level and difficulty over time, eventually cashing out ("selling their brains for parts") to gain permanent progression multipliers.
- **Players**: Single player
- **Inspiration / Reference Games**: AdVenture Capitalist, Cookie Clicker, Universal Paperclips
- **Tone / Art Direction**: Satirical corporate-dystopian, featuring dark synthwave colors and high-contrast neon visual accents.
- **Target Platform**: iOS (mobile)
- **Screen Orientation / Resolution**: Portrait 1080x1920
- **Render Pipeline**: UniversalRP

# Game Mechanics
## Core Gameplay Loop
1. **Tap & Idle Income**: Players tap the screen or hire automated upgrade systems to generate "Brains" (the primary resource).
2. **IQ Decay**: The player's IQ decay rate constantly pressures their overall multiplier.
3. **Rebirth (The "Sell Brain for Parts" Loop)**: When progression slows, the player resets their soft currency, decay rate, and building upgrades in exchange for a permanent income multiplier boost calculated from their cumulative run earnings.

## Controls and Input Methods
- **Tap Input**: Handled via the New Input System on the main full-screen TapButton.
- **UI Button Taps**: Standard Unity UI button events mapped to HUD interactions, scroll lists, and confirmation modals.

# UI
## Active UI Elements in Scene
- **BrainsText**: Displays soft currency (Brains).
- **IQText**: Displays active decaying IQ.
- **LevelText**: Displays current progression level/rank.
- **ShopPanel**: Contains slots for hiring building templates (Literal Library, Podcaster Soundboard, Crypto Bro Compound, Reality TV Syndicate, Brain Rot Think Tank, Doomscroll Engine).
- **RebirthTriggerButton**: Custom HUD button in the bottom right to trigger the RebirthModal popup.
- **RebirthModal**: Full-width, bottom 60% overlay containing:
  - Title: "SELL BRAIN FOR PARTS?"
  - Multiplier Increase: "+X.XXx MULTIPLIER" (dynamically formatted using `NumberFormatter`).
  - Buttons: Confirm ("SELL OUT" in neon cyan outline) and Cancel ("ABORT" in neon magenta outline).

# Key Asset & Context
## New Files to Create
1. **`Assets/PlayModeTestBootstrapper.cs`**
   - A self-destructing editor script that hooks into the Unity Editor update loop to automate playtesting, take screenshots, and gracefully exit play mode.

# Implementation Steps
## Step 1: Create the Automated Playtest Bootstrapper Script
- **Description**: Implement a static editor bootstrapper (`PlayModeTestBootstrapper.cs`) marked with `[InitializeOnLoad]`. It uses `SessionState` to manage states:
  1. **Start Play Mode**: Transition editor state, setting `EditorApplication.isPlaying = true`.
  2. **Wait & Initialize**: Spawn a transient MonoBehaviour `PlaytestHelper` in the scene. Wait 3.0 seconds in real time. This ensures all awake/start methods run, HUD updates, shop templates load, and UI settles.
  3. **Capture Screenshot**: Call `ScreenCapture.CaptureScreenshot("PlaytestScreenshot.png")` to dump the current Game view buffer.
  4. **Exit Play Mode**: Transition state, set `EditorApplication.isPlaying = false`, destroy the helper, delete itself (`PlayModeTestBootstrapper.cs` and `.meta` files) to keep the project clean, and refresh assets.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

## Step 2: Execute the Playtest Run
- **Description**: Allow Unity to compile the bootstrapper. The static constructor automatically triggers the loop, boots up Play Mode, takes the screenshot, and shuts down safely.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

## Step 3: Screenshot Playability Analysis
- **Description**: Inspect the output file `PlaytestScreenshot.png` (using system or image inspection tools if available) to ensure:
  - The HUD elements render cleanly and do not clip.
  - The Diorama Camera renders the top 40% area with no overlapping overlays.
  - The shop panel and the Rebirth trigger button are placed correctly.
- **Assigned role**: explorer
- **Dependencies**: Step 2
- **Parallelizable**: No

# Verification & Testing
## What to copy-paste to Gemini/Claude if Playtest fails or has errors:
If any script compilation, layout misalignment, or runtime error occurs during playtesting, compile a diagnostic packet containing:
1. **Unity Console Logs**: Get all console messages, warning counts, and complete stack traces (especially NullReferenceExceptions or reference issues) using `GetConsoleLogs`.
2. **Current Scene Hierarchy**: Print the Canvas and Active camera structures using a scene hierarchy dump.
3. **Core Controller Scripts**: Copy the contents of:
   - `Assets\_Game\Scripts\Systems\RebirthManager.cs`
   - `Assets\_Game\Scripts\UI\RebirthUIController.cs`
   - `Assets\_Game\Scripts\Core\GameManager.cs`
4. **Active UI Canvas Layout Bounds**: Export RectTransform coordinates of the `Canvas`, `ShopPanel`, `RebirthModal`, and cameras.
