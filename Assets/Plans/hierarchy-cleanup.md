# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: Satirical mobile idle clicker by AcEclipse Games.
- Players: Single player
- Tone / Art Direction: Satirical retro-dystopian cartoon, high-contrast colors.
- Target Platform: iOS / Android / Mobile
- Render Pipeline: UniversalRP (2D)

# Game Mechanics
- The `ShopPanel` (or buildings panel) under Canvas should be hidden by default. It is activated via the "SHOP" button at runtime.
- The `RandomEventManager` handles periodic in-game events. There should be only one instance under `_Systems` to prevent duplicate events or warnings.

# UI
- `ShopPanel` (located at `Canvas/CustomSafeArea/ShopPanel`) is deactivated.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- Duplicate GameObject to delete: `RandomEventManager (Auto)` (root of scene).
- Sibling under Canvas: `Canvas/CustomSafeArea/ShopPanel`.

# Implementation Steps

### Step 1: Deactivate ShopPanel in Scene
- **Description**:
  - Locate `Canvas/CustomSafeArea/ShopPanel` in `SampleScene.unity`.
  - Set the GameObject to inactive.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Delete Duplicate RandomEventManager
- **Description**:
  - Locate `RandomEventManager (Auto)` at the root of `SampleScene.unity`.
  - Delete it from the scene (leaving only the one under `_Systems/RandomEventManager`).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

# Verification & Testing
- Read back `Canvas/CustomSafeArea/ShopPanel` active status: confirm it is `false`.
- Find all `RandomEventManager` scripts in scene: confirm only 1 exists, located under `_Systems/RandomEventManager`.
- Save the scene.
