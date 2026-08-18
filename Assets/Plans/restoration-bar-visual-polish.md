# Project Overview
- Game Title: Brain Drain
- High-Level Concept: Incremental/idle clicker game featuring brain power harvesting, Snotting (rebirth), and world restoration.
- Players: Single player
- Inspiration / Reference Games: Adventure Capitalist, Egg Inc.
- Tone / Art Direction: Sci-fi / Corporate dystopia, cartoon-photoreal hybrid UI.
- Target Platform: iOS / Mobile
- Screen Orientation / Resolution: Portrait 1080x1920 reference
- Render Pipeline: UniversalRP (URP)

# Game Mechanics
## Core Gameplay Loop
Players harvest brain power by tapping, convert brain power into points, and spend points on World Restoration to advance world stages and unlock Snotting/rebirth tiers.
## Controls and Input Methods
Touch/tap controls on UI buttons and vessel elements.

# UI
- `Canvas/CustomSafeArea/RestorationVesselRow`:
  - `RestorationBarTrack` (130px tall)
    - `RestorationBarFill` (Image, Filled, Horizontal/Left, tinted green) -> Inset to inner cavity `(59.4px, 18px)`
    - `RestorationBarPlunger` (Image, `xp_bar_plunger`) -> Sized to inner cavity height (`95px`), anchored at leading edge `leftInset + fraction * travelRange`
    - `RestorationBarVesselFrame` (Image, Sliced, `xp_bar_frame`, 340px borders) -> Spans vessel track
  - `RestorationLabel` (TMP, left)
  - `PointsText` (TMP, right)

# Key Asset & Context
- `Assets/Scenes/SampleScene.unity`
- `Assets/_Game/Scripts/UI/HUDController.cs`
- `Assets/_Game/Scripts/Systems/AnimationController.cs`
- `Assets/_Game/Scripts/Editor/RestorationBarWireFix.cs`
- Sprites: `xp_bar_frame.png`, `xp_bar_plunger.png`, `restoration_fill.png`

# Implementation Steps

### Step 1: Inset Fill Cavity and Align Plunger Trajectory
- **Description**:
  - Inset `RestorationBarFill`'s `offsetMin` to `(59.4, 18.0)` and `offsetMax` to `(-59.4, -18.0)` relative to `RestorationBarTrack` so the liquid fill is strictly confined to the inner glass cavity and does not draw over the copper end rings or top/bottom glass rims.
  - Sizing `RestorationBarPlunger`: Change height from 130px to `95px` (matching the 95px cavity height) and width to `~60px` (`95 * (1097 / 1724)`).
  - Update `HUDController.ComputePlungerTargetX` in `HUDController.cs` to add `restorationFillImage.rectTransform.offsetMin.x` (the `59.4px` left cavity inset) to the calculated position, so at `fraction = 0` the plunger sits flush at the left inner cavity wall rather than at the outer edge of the left copper cap.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Add Luminous Soft Glow Effect to Fill Liquid
- **Description**:
  - Create a secondary glow Image child `RestorationBarGlow` under `RestorationBarTrack` (placed behind `RestorationBarFill`), using soft radial/gradient glow graphics (`GetGlowSprite()`), driven by `restorationFillImage.fillAmount` in `HUDController.cs`.
  - Alternatively, enhance fill alpha/color curve and brightness handling to create a subtle ambient luminescence inside the glass tube without competing with active gameplay.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Implement Gain Pulse Animation (Fill Brightness + Plunger Reaction)
- **Description**:
  - Add `PlayRestorationGainPulse(Image fillImage, RectTransform plunger)` in `AnimationController.cs` matching the 0.3s duration and `Ease.OutQuad` style of `PlayPlungerMove`.
  - In `HUDController.UpdateRestorationProgressText`, when `fraction > previousFraction`:
    - Flash fill color slightly brighter (0.15s fade-in, 0.15s fade-out back to current stage color).
    - Apply a subtle scale punch (`DOPunchScale(Vector3.one * 0.12f, 0.3f, 1, 0.5f)`) to `restorationPlungerImage.rectTransform` to simulate liquid pressure reaction.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 2
- **Parallelizable**: No

### Step 4: Enforce Settings in Editor Tool (`RestorationBarWireFix.cs`)
- **Description**:
  - Update `RestorationBarWireFix.cs` so running `Tools/Eighth Kind/Fix Restoration Bar Wiring` programmatically enforces the exact cavity insets `(59.4, 18.0)` and plunger height/offset math, making scene setup completely idempotent and reproducible.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 2, Step 3
- **Parallelizable**: No

### Step 5: Verification & Testing
- **Description**:
  - Open `SampleScene.unity` and test restoration progress increases.
  - Verify liquid fill stays within glass tube cavity and does not bleed into copper end caps or outer frame boundaries.
  - Verify plunger travels smoothly from left cavity edge (`59.4px`) to right cavity edge, riding on the liquid surface.
  - Verify gain pulse animates smoothly with 0.3s DOTween curves.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

# Verification & Testing
- **Visual Checks**: Confirm in Scene view and Game view at 1080x1920 aspect ratio that fill is inset inside copper end caps.
- **Play Mode Testing**: Trigger restoration points increase in Play Mode to verify plunger tracking and gain pulse feedback.
- **Idempotency Check**: Execute `RestorationBarWireFix.Run` menu item to confirm layout persists cleanly.
