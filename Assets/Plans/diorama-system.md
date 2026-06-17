# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: An idle clicker game where players tap to earn Brains and restore decaying IQ, escalating in level and difficulty over time.
- Players: Single player
- Tone / Art Direction: Neon, high-contrast style
- Target Platform: iOS
- Render Pipeline: UniversalRP

# Game Mechanics
## Core Gameplay Loop
- Players tap to earn Brains and advance through ranks (Idiocracy Rank System).
- Based on the player's active rank, the 2D Diorama environment changes to reflect their status (e.g. from "Unregistered Outcast" up to "Mr. President").

## Controls and Input Methods
- Touch tapping/clicking triggers brain accumulation which automatically updates rank and transitions the visual diorama scene.

# UI
- No direct UI interactions for Diorama, but the visual backdrop changes dynamically as Ranks change on the HUD.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- `_DioramaContainer` (root GameObject)
- Child GameObjects:
  - `Diorama_0_Outcast` (active by default)
  - `Diorama_1_Inmate` (inactive by default)
  - `Diorama_2_Champion` (inactive by default)
  - `Diorama_3_Secretary` (inactive by default)
  - `Diorama_4_President` (inactive by default)
- Script: `Assets/_Game/Scripts/Core/DioramaManager.cs`

# Implementation Steps

### Step 1: Create DioramaManager Script
- **Description**: Implement `DioramaManager.cs` inside `Assets/_Game/Scripts/Core/`. This script references an array of 5 `GameObject`s representing the different rank dioramas. It subscribes to `CurrencyManager.Instance.OnCumulativeBrainsChanged` to dynamically calculate the active rank index and enable/disable the respective diorama GameObjects accordingly.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Create Diorama Container and Child GameObjects
- **Description**: In the root of `SampleScene.unity`, create an empty root GameObject named `_DioramaContainer`. Create 5 child GameObjects inside it named exactly:
  - `Diorama_0_Outcast` (Active)
  - `Diorama_1_Inmate` (Inactive)
  - `Diorama_2_Champion` (Inactive)
  - `Diorama_3_Secretary` (Inactive)
  - `Diorama_4_President` (Inactive)
- Add a `SpriteRenderer` component to each of the 5 child GameObjects to act as a placeholder baseline for diorama assets.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 3: Attach DioramaManager and Assign References
- **Description**: Attach the `DioramaManager` component to the `_DioramaContainer` GameObject. Automatically assign the 5 child dioramas to the script's `dioramaObjects` array in their correct index order.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 2
- **Parallelizable**: No

# Verification & Testing
- Open `SampleScene` in Unity.
- Verify `_DioramaContainer` exists in root hierarchy, and has the 5 children configured with `SpriteRenderer` components.
- Verify `DioramaManager` is attached and its array references are fully populated.
- Play the game, add Brains programmatically/via tapping, and verify the correct Diorama child becomes active as soon as the respective Rank threshold is crossed.
