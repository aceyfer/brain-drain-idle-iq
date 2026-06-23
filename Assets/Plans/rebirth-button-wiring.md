# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: Satirical idle clicker.
- Tone / Art Direction: Neon retro-dystopian cartoon.
- Target Platform: iOS / Mobile
- Render Pipeline: UniversalRP (2D)

# Game Mechanics
- Rebirth: players trigger a rebirth by pressing the REBIRTH button, which pops up the Rebirth Modal.
- Character feedback: character squashes on tap, idles, and reacts to events.

# UI
- `RebirthTriggerButton` (on Canvas) needs to be wired to the `RebirthUIController` in the scene so that the UI can capture the click and present the Rebirth Modal.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- `RebirthUIController` (on Canvas/RebirthModal or another GameObject in scene) has a serialized `rebirthTriggerButton` field which is currently null.
- `RebirthTriggerButton` (on Canvas) has the Button component to assign.
- `PlayerCharacterController` is in the scene and is already properly connected to `CharacterRoot`.

# Implementation Steps

### Step 1: Wire RebirthTriggerButton to RebirthUIController
- **Description**: Locate `RebirthUIController` in the scene. Get its Button component reference from `Canvas/RebirthTriggerButton` and assign it to the `rebirthTriggerButton` serialized field using `SerializedObject`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Confirm PlayerCharacterController Connection
- **Description**: Re-verify `PlayerCharacterController` is connected to `CharacterRoot` in the scene and that it's functioning as expected.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

# Verification & Testing
- Read back `RebirthUIController.rebirthTriggerButton` to confirm it points to `RebirthTriggerButton`.
- Read back `PlayerCharacterController.characterVisualTarget` to confirm it points to `CharacterRoot`.
- Confirm console logs have no warnings or errors.
