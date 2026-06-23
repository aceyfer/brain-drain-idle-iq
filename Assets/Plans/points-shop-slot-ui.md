# Project Overview
- **Game Title**: Brain Drain: Idle IQ
- **High-Level Concept**: An incremental idle clicker game where the player taps to generate Brain Power, grows IQ, battles the mysterious "Illumisnotti," and manages multiple shop tiers (including the Points Power Shop) to unlock Rebirths and purchase permanent upgrades.
- **Players**: Single player
- **Inspiration / Reference Games**: Adventure Capitalist, Cookie Clicker, Universal Paperclips
- **Tone / Art Direction**: Neon cyberpunk styling, tech-infused high-contrast glowing elements over dark grey panels.
- **Target Platform**: iOS + Android
- **Screen Orientation / Resolution**: Auto Rotation (supporting Portrait/Landscape auto-adjustment), 1920x1080 design canvas with custom safe area.
- **Render Pipeline**: Universal RP

# Game Mechanics
## Core Gameplay Loop
- Tapping or passive systems generate Brain Power and IQ.
- Reaching thresholds enables Rebirths, giving specialized rebirth points.
- Spending rebirth points in specialized shops (like the Points Power Shop) unlocks powerful multipliers and stage-progression boosts.
- Progressive weakening of the "Illumisnotti" faction allows reaching higher world stage indices.

## Controls and Input Methods
- Pure touch screen controls (tap, drag to scroll) utilizing the New Input System with event-driven canvas interactions.

# UI
- Menu screens and overlays are managed via Canvas-based uGUI.
- `PointsShopPanel` contains a VerticalLayoutGroup to stack active slots.
- Individual shop slot layouts:
  - Left-hand column: Informational Texts (ItemName, Effect) utilizing a VerticalLayoutGroup with flexible width stretching.
  - Right-hand column: Transaction Controls (Cost, PurchaseButton) with a fixed minimum width of 240px.
  - Full-stretching overlay: Locked screen displayed when conditions (Rebirth/World stage) are not yet met.

# Key Asset & Context
- **Script to target**: `Assets/_Game/Scripts/UI/PointsShopSlotUI.cs`
  - Needs to be attached to the root of the slot.
  - Field assignments:
    - `nameText` points to `ItemNameText`
    - `descriptionText` points to `EffectText`
    - `costText` points to `CostText`
    - `buyButton` points to `PurchaseButton`
    - `background` points to `StateTint` (the child Image component used for dynamic accent overlays)
- **Prefab to create**: `Assets/_Game/Prefabs/PointsShopSlotUI.prefab`
  - Root: `PointsShopSlotUI` (RectTransform size 1020.53 x 180, Image background color `rgba(0, 0, 0, 0.75)`, `PointsShopSlotUI` script component)
  - Children:
    - `StateTint` (Image, `RoundedRect8` sprite, color `rgba(0,0,0,0)`, stretch-anchored, `LayoutElement` with `ignoreLayout = true`, assigned to `background` field)
    - `InfoColumn` (VerticalLayoutGroup with spacing=4, `LayoutElement` with `flexibleWidth = 1`)
      - `ItemNameText` (TextMeshProUGUI, size 18, white, bold, font `LiberationSans SDF`)
      - `EffectText` (TextMeshProUGUI, size 14, grey, wrapping on, font `LiberationSans SDF`)
    - `BuyColumn` (VerticalLayoutGroup or RectTransform, `LayoutElement` with `minWidth = 240, preferredWidth = 240`)
      - `CostText` (TextMeshProUGUI, size 16, yellow, text "0 POINTS", right-aligned, font `LiberationSans SDF`)
      - `PurchaseButton` (Button, Image with sprite = `None`, normal color = `rgba(0, 0.94, 1, 0.25)`)
        - `ButtonText` (TextMeshProUGUI, size 16, text "SPEND", center-aligned, font `LiberationSans SDF`)
    - `LockedOverlay` (Image, color `rgba(0, 0, 0, 0.6)`, stretch-anchored, `LayoutElement` with `ignoreLayout = true`, inactive by default)
      - `LockedText` (TextMeshProUGUI, white, size 16, bold, "LOCKED", center-aligned, font `LiberationSans SDF`)

# Implementation Steps

### Step 1: Create UI Structure & Prefab Assets
- **Description**: Build the `PointsShopSlotUI` UI hierarchy programmatically or via a temporary scene object, matching the exact sizing (1020.53 x 180), components (Vertical/Horizontal layouts, TMP, Images, Buttons) and visual styling of Shop 1 (`Library` slot). Save it as a prefab to `Assets/_Game/Prefabs/PointsShopSlotUI.prefab`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Wire Prefab and Managers to PointsShopUIController
- **Description**: Select the `PointsShopPanel` in `SampleScene.unity` and locate the `PointsShopUIController` component. Set its `slotPrefab` property to point to the newly created prefab. Ensure `openButton` (assigned to `PointsShopButton`), `closeButton` (assigned to `CloseButton`), and manager references (`pointsShopManager` and `currencyManager` pointing to `_Systems`) are fully assigned.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

# Verification & Testing
1. **Scene Verification**: Run a validation script/check inside Unity Editor to verify that the `slotPrefab` is properly set in the `PointsShopUIController` component of the `PointsShopPanel` object, and that all critical references (`pointsShopManager`, `currencyManager`, `openButton`, `closeButton`) are non-null.
2. **Component Integrity Checks**:
   - Ensure `PointsShopSlotUI` prefab has its Text and Button fields correctly bound to its child GameObjects.
   - Verify that `LockedOverlay` is disabled by default in the prefab structure.
   - Ensure the correct font `LiberationSans SDF` is used on all text components.
3. **Play Mode test**:
   - Open and close the Points Power Shop in play mode using the UI buttons.
   - Ensure items are instantiated correctly based on the PointsShopManager's dataset.
   - Check that locked overlay and affordable color tints apply properly based on rebirth levels and points.
