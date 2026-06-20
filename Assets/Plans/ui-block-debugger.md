# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: An idle clicker game where players tap to earn Brains and restore decaying IQ, with retro satirical adware popup events and other chaotic elements.
- Players: Single player
- Inspiration / Reference Games: Cookie Clicker, early internet browser adware pop-ups, Retro clickers.
- Tone / Art Direction: Neon, high-contrast, chaotic retro 2000s style.
- Target Platform: iOS / Mobile
- Screen Orientation / Resolution: Portrait 1080x1920 (16:9)
- Render Pipeline: UniversalRP (URP)

# Game Mechanics
## Core Gameplay Loop
- Clicker mechanics with idle upgrades and random adware popups.

# UI
## Troubleshooting Tool
- Create a troubleshooting script to print out which UI elements are under the pointer during a click, helping debug click-blocking issues.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- Script to create: `Assets/_Game/Scripts/UI/UIBlockDebugger.cs`
- GameObject to create: `_UIBlockDebugger` in the scene root.

# Implementation Steps

### Step 1: Create UIBlockDebugger C# Script
- **Description**: Implement `UIBlockDebugger.cs` inside `Assets/_Game/Scripts/UI/`. 
  - The script should check for mouse click inside `Update()` via `Input.GetMouseButtonDown(0)`.
  - When clicked, construct a `PointerEventData` using the current `EventSystem.current` and mouse position.
  - Call `EventSystem.current.RaycastAll(pointerEventData, raycastResults)`.
  - If there are any results, `Debug.Log` the name of the absolute top-most GameObject (`raycastResults[0].gameObject`).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Create GameObject and Attach Script in Scene
- **Description**: Create an empty GameObject named `_UIBlockDebugger` in the active scene `SampleScene.unity`. Attach the `UIBlockDebugger` component to it, and save the scene.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

# Verification & Testing
- Open `SampleScene` in Unity.
- Verify `_UIBlockDebugger` exists in the hierarchy and has the component attached.
- Enter Play Mode.
- Click on various UI elements (e.g. Buttons, Texts, backgrounds).
- Verify the Unity Console logs the exact name of the top-most UI element under the pointer.
