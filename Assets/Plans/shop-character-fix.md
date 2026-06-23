# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: Satirical mobile idle clicker by AcEclipse Games. Fixes visual alignment of the player character, shop layout transitions, and close button interactivity.
- Players: Single player
- Tone / Art Direction: Satirical retro-dystopian cartoon, high-contrast colors.
- Target Platform: iOS / Android / Mobile
- Render Pipeline: UniversalRP (2D)

# Game Mechanics
## Stand upright on ground level
- Player Character sits at the bottom-center of the screen at street level.
- Sprites must be upright, not inverted.

## Shop Panel Layout
- Opens by sliding DOWN from the TOP of the screen, covering only the top 60%.
- Bottom 40% (street level and character) remains fully visible so players can watch their character breathe and react while browsing upgrades.

# UI
- **ShopPanel**: Re-anchored to the top of the screen (`anchorMin=(0, 0.40), anchorMax=(1, 1), pivot=(0.50, 1)`).
- **PlayerCharacter_Anchor**: Re-anchored to the bottom center of the screen (`anchorMin=(0.5, 0), anchorMax=(0.5, 0), pivot=(0.5, 0)`).
- **MainTapButton**: Verified to only cover the bottom portion of the screen so it does not block top UI panel raycasts.
- **ShopPanel Sibling order**: Set to render on top of HUD and main tap button so its scroll view and close buttons receive raycasts without blockages.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- Controller: `ShopUIController.cs` on `ShopPanel`.
- Script: `PlaceholderArtGenerator.cs` (generating character sprites).

# Implementation Steps

### Step 1: Fix PlaceholderArtGenerator.cs to save sprites upright
- **Description**: 
  - Open `Assets/_Game/Scripts/Editor/PlaceholderArtGenerator.cs`.
  - Modify `SaveTextureAsSprite` to vertically flip the generated texture pixels before writing to PNG. This fixes the upside-down drawing coordinate offset elegantly without changing the complex rendering math.
  - Re-run the generator menu item `BrainDrain/Generate Placeholder Art/COGS + Player Character` to update all 5 sprite files on disk.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Correct PlayerCharacter_Anchor anchors and position
- **Description**:
  - Select `Canvas/CustomSafeArea/PlayerCharacter_Anchor`.
  - Set its anchors to `anchorMin = (0.5f, 0f)`, `anchorMax = (0.5f, 0f)`, and pivot = `(0.5f, 0f)`.
  - Set its `anchoredPosition` to `(0f, 60f)`. This places the character on the ground of the world stage.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Reposition ShopPanel and update slide animations in ShopUIController.cs
- **Description**:
  - Select `Canvas/CustomSafeArea/ShopPanel`.
  - Set its anchors to `anchorMin = (0f, 0.40f)`, `anchorMax = (1f, 1f)` and pivot = `(0.5f, 1f)`. This makes it cover the top 60% of the screen.
  - Open `Assets/_Game/Scripts/UI/ShopUIController.cs`.
  - Modify `OpenShop()` and `CloseShop()` to compute the offscreen position as `offscreenAbove = shopPanelRestingPosition + new Vector2(0f, shopPanelRect.rect.height)` so that the shop panel slides DOWN from the TOP of the screen, and slides back UP to close.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 4: Fix Shop Close Button interactivity and layer ordering
- **Description**:
  - Select `Canvas/CustomSafeArea/MainTapButton`. Set its anchors to `anchorMin = (0f, 0f)`, `anchorMax = (1f, 0.385f)` so it only covers the bottom half and does not block the top area.
  - Select `Canvas/CustomSafeArea/ShopPanel`. Set its sibling index to render on top of HUD elements so it is not blocked by any background raycasts.
  - Wire the persistent `onClick` event of the `CloseButton` on `ShopPanel` in the inspector to `ShopUIController.CloseShop()`.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

# Verification & Testing
- Read back `PlayerCharacter_Anchor` properties: verify it stands upright at bottom-center.
- Run Play Mode:
  - Confirm the character stands on the ground and is upright.
  - Open the shop: verify it slides down from the top, covering only the top 60%.
  - Verify you can see the character breathe in the bottom 40% while the shop is open.
  - Click the Close "X" button on the shop: verify the shop slides back up and deactivates correctly.
