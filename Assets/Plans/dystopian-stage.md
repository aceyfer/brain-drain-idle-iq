# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: Satirical mobile idle clicker by AcEclipse Games. Surfaces an on-screen dystopian world stage where players tap to extract brain power, climb ranks, and buy buildings.
- Players: Single player
- Tone / Art Direction: Satirical retro-dystopian cartoon, thick black outlines, neon high-contrast colors.
- Target Platform: iOS / Mobile
- Screen Orientation / Resolution: Portrait 1080x1920
- Render Pipeline: UniversalRP (2D)

# Game Mechanics
## Bottom-Half Tapping
- The visual circular "MainTapButton" button, text labels, and ring are removed.
- The tap interaction is expanded to cover the *entire* bottom half of the screen (everything below the EconomyBar, from y=0 to y=0.385 canvas height). Tapping anywhere in this region triggers a extraction tap.

## Visual Feedback
- Tapping triggers a squash/stretch animation on the player character, spawns floating reward texts, and emits randomized goo splat particles at the pointer coordinates.
- Character idles with natural breathing when not being tapped.

# UI
A clean, premium dystopian landscape visual stage in the bottom half of the screen:
- **City Skyline Backdrop**: The uncompressed AI-generated landscape sprite (`CitySkylineFar.png`) is applied as the skyline background covering the bottom area, replacing the cheap procedural geometric primitives.
- **Player Visibility**: The player character (`PlayerCharacter_Anchor` image with `PersonSilhouette` sprite) is sized up significantly to be highly detailed and centered on the stage, ready to take a flat cartoon sprite sheet with thick 4px outlines.
- **Invisible Click Target**: `MainTapButton` is made invisible but stretched to cover the entire bottom half, serving as a clean, responsive touch zone.
- **COGS Narrator Window**: Inside `CustomSafeArea`, create `COGS_Narrator_Panel` as a garish fake desktop pop-up with hot pink or neon green border, fake window chrome header, and attach `COGSPortraitController` and `DialogueDisplayUI` to it.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- Background Asset: `Assets/_Game/Sprites/Backgrounds/CitySkylineFar.png` (verified Sprite, 1024x512, Uncompressed, Repeat-wrap).
- Character Asset: `Assets/_Game/Sprites/Characters/PersonSilhouette.png` (verified Sprite, 256x512, Uncompressed).
- Controller: `PlayerCharacterController` is attached to `PlayerCharacter` in the scene, driving character state changes and animations.
- Sibling Order under Canvas:
  - `BackgroundRoot` (index 0)
    - `BottomBG` (index 2) - should render a dark gradient or sky, and hold a nested stretched Image `SkylineBG` rendering `CitySkylineFar.png`.
  - `WorldRoot` (index 1) - contains old cheap primitives. Will be disabled/deactivated.
  - `CustomSafeArea` (index 2)
    - `COGS_Narrator_Panel` (new UI Panel named 'COGS_Narrator_Panel' containing image and TMP text, with COGSPortraitController and DialogueDisplayUI attached).
    - `PlayerCharacter_Anchor` (renamed from `CharacterRoot`) - our player character. Sized up, bottom-centered.
    - `MainTapButton` (index 9) - stretched over the full bottom half, made invisible but raycast-enabled.
- Pedestrian Manager: `BackgroundPedestrianManager.cs` under `Assets/_Game/Scripts/BrainDrain.Systems` to manage spawning and moving of pedestrians behind the player character.

# Implementation Steps

### Step 1: Create COGS Narrator Window in Scene
- **Description**: 
  - Create a new UI Panel named `COGS_Narrator_Panel` under `Canvas/CustomSafeArea`.
  - Position it near the top of the middle zone (e.g., resting position anchored at min=(0, 0.5), max=(0, 0.5), pivot=(0, 0.5), width=960, height=280).
  - Style it like a garish popup: Outer panel is hot pink (`#FF1493`) with a thick border, has a neon green (`#39FF14`) header bar child ("fake chrome window header").
  - Inside `COGS_Narrator_Panel`, add an Image element `AvatarImage` (for COGSPortraitController) and a `TextMeshProUGUI` text element `DialogueText`.
  - Attach `COGSPortraitController` and `DialogueDisplayUI` to `COGS_Narrator_Panel`.
  - Assign fields of `DialogueDisplayUI`: `panelRect` -> `COGS_Narrator_Panel` RectTransform, `lineText` -> `DialogueText`, `avatarImage` -> `AvatarImage`.
  - Populate the `COGSPortraitController.stages` list with the 6 stage assets under `Assets/_Game/Dialogue/COGSStages/` (in order).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Fix Player Character Anchor and Sizing
- **Description**:
  - Rename `CharacterRoot` under `Canvas/CustomSafeArea` to `PlayerCharacter_Anchor`.
  - Clear any custom materials (set `material = null`) so it has a clean default UI material ready to accept any flat cartoon outline sprite sheets.
  - Sized up to `250x500` positioned bottom-center as in original step.
  - Update `PlayerCharacterController`'s `characterVisualTarget` to point to the renamed `PlayerCharacter_Anchor`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 3: Write BackgroundPedestrianManager.cs Script
- **Description**:
  - Create `BackgroundPedestrianManager.cs` under `Assets/_Game/Scripts/BrainDrain.Systems/`.
  - Define class `BackgroundPedestrianManager : MonoBehaviour`.
  - Listen to `WorldRestorationManager.Instance` or `GameManager.Instance` (or fallback safely) to observe progress and update active pedestrian pool.
  - Define public arrays `dystopianPedestrianSprites` and `utopianPedestrianSprites`.
  - Spawn pedestrian UI Images inside a new `PedestrianContainer` child under `CustomSafeArea` (index 0, so they move behind the player character index 1).
  - Implement a simple coroutine loop that instantiates a pedestrian UI Image at the left boundary of the stage, translates them horizontally to the right screen boundary, and destroys them upon exit.
  - Attach `BackgroundPedestrianManager` to a new GameObject `BackgroundPedestrianManager` or `_Systems/BackgroundPedestrianManager` in the scene.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 4: Deactivate primitive WorldRoot and setup SkylineBG
- **Description**: Same as original step 1 (deactivate WorldRoot, add SkylineBG under BackgroundRoot/BottomBG).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 5: Make MainTapButton invisible and stretch over bottom half
- **Description**: Same as original step 2 (stretch MainTapButton transparently across the full bottom half).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

# Verification & Testing
- Read back hierarchy: confirm `COGS_Narrator_Panel` exists under `CustomSafeArea` with `COGSPortraitController` and `DialogueDisplayUI` fully wired.
- Confirm `PlayerCharacter_Anchor` is correctly renamed, sized, and assigned to `PlayerCharacterController.characterVisualTarget`.
- Confirm `BackgroundPedestrianManager.cs` is written, compiles cleanly with 0 warnings/errors, and is attached in the scene.
- Run play-mode test: confirm COGS dialogue panels slide in and out of view on dialogue events without errors, and that pedestrians spawn and walk horizontally behind the player.
