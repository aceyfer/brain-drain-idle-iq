# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: Satirical mobile idle clicker by AcEclipse Games. The game parodies a dystopian corporate future (similar to the movie *Idiocracy*), where the player character wakes up from a cryo-sleep chamber in a vault jumpsuit and progresses through increasingly ridiculous cyberpunk and corporate upgrades.
- Tone / Art Direction: Satirical retro-dystopian cartoon, thick black outlines, neon high-contrast colors, funny caricature visuals.
- Target Platform: iOS / Mobile
- Render Pipeline: UniversalRP (2D)

# Game Mechanics
- As the player rebirths, their character's visual appearance progresses dynamically to represent their growing "IQ" and satirical stature in the corporate dystopia.
- Starts as a dazed cryo-chamber survivor in a blue-and-yellow Vault Jumpsuit, evolving into a corporate corporate leader with an exposed pulsating brain in a glass dome.

# UI / Layout
- The `PlayerCharacter_Anchor` image component in the canvas renders the active character sprite.
- The pre-built `PlaceholderArtGenerator.cs` will be redesigned to generate high-fidelity, hilarious procedural cartoon sprites for each progression stage.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- Controller: `PlayerCharacterController` drives the sprite changes based on the rebirth tier.
- Data Assets:
  - `CharacterAppearanceStage_0_DimOutline` (Rebirth 0) ->confused Vault Jumpsuit survivor.
  - `CharacterAppearanceStage_1_CyanOutline` (Rebirth 1) ->add neon cyber visor.
  - `CharacterAppearanceStage_2_MagentaOutline` (Rebirth 3) ->add neon corporate outcast vest.
  - `CharacterAppearanceStage_3_GoldOutline` (Rebirth 6) ->add gold crown of cables and wires.
  - `CharacterAppearanceStage_4_WhiteHotOutline` (Rebirth 11) ->add giant pulsating brain inside a glass space-dome helmet.
- Sprite sheet / Generator: Redesign `PlaceholderArtGenerator.cs` to render these funny characters using pixel-drawing primitives.

# Implementation Steps

### Step 1: Update PlayerCharacterController to support UI Images
- **Description**:
  - Open `Assets/_Game/Scripts/Systems/PlayerCharacterController.cs`.
  - Add a serialized field `[SerializeField] private UnityEngine.UI.Image appearanceImage;` so that Canvas UI rendering is supported natively without relying on a missing World SpriteRenderer.
  - Update `ApplyAppearanceForRebirthCount(int rebirthCount)` to assign the stage's sprite to `appearanceImage.sprite` if it is assigned.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Redesign PlaceholderArtGenerator.cs for Detailed Cartoon Sprites
- **Description**:
  - Open `Assets/_Game/Scripts/Editor/PlaceholderArtGenerator.cs`.
  - Rewrite `GenerateCharacterAppearanceStages()` to call a new procedural character drawing function: `CreateDetailedCharacterTexture(int stageIndex, Color outlineColor)`.
  - Implement drawing logic for each stage:
    - **Step 1 (Outline Pass)**: Draw the complete humanoid silhouette with thick padding in solid black (or outlineColor neon) to create our cartoon-style 4px thick black border.
    - **Step 2 (Body Pass)**: Draw the iconic blue Vault Jumpsuit with a thick, high-contrast yellow vertical stripe down the middle.
    - **Step 3 (Head Pass)**: Draw a round dazed face (peach color) with messy cryo-sleep brown hair and dizzy dazed eyes/mouth.
    - **Step 4 (Stage Upgrades)**:
      - *Stage 0*: Dazed survivor in jumpsuit.
      - *Stage 1*: Add a bright neon-cyan visor/cyber-goggles across the eyes.
      - *Stage 2*: Add a magenta corporate vest over the shoulders of the jumpsuit.
      - *Stage 3*: Add a golden crown of electrical nodes on the head, with green/yellow cables.
      - *Stage 4 (The President)*: Encase the head in a large light-grey glass space dome, replacing the face with a massive, funny pink exposed brain, and add gold chains/medallions to the chest.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 3: Run the Art Generator & Wire the Scene Component
- **Description**:
  - Invoke `BrainDrain/Generate Placeholder Art/COGS + Player Character` menu item in the editor.
  - This compiles and regenerates all 5 detailed stage PNGs, auto-imports them as high-quality uncompressed UI Sprites, dirty-marks the data assets, and auto-populates the list on `PlayerCharacterController` in the scene.
  - Select `PlayerCharacter` in the scene, and wire its new `appearanceImage` slot to `Canvas/CustomSafeArea/PlayerCharacter_Anchor`.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 2
- **Parallelizable**: No

# Verification & Testing
- Compile check: confirm no compilation warnings or errors exist.
- Menu execution check: verify the generator completes without warnings/errors and creates 5 beautifully styled, funny character PNGs under `Assets/_Game/Art/PlayerCharacter/`.
- Target wiring check: confirm `PlayerCharacterController`'s `appearanceImage` points to `PlayerCharacter_Anchor`.
- Enter Play Mode:
  - Confirm the character starts out as the confused Vault survivor in a blue-and-yellow jumpsuit.
  - Art check: Verify the character looks highly satirical, cartoony, and features a clean thick outline.
  - Confirm character breathing/squashing works flawlessly with the new procedural sprites.
