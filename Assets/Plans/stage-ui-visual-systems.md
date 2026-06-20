# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: A satirical idle clicker where players tap to earn Brains, climb "Idiocracy" ranks, and buy brain-melting buildings; a 2D diorama backdrop evolves with rank.
- Players: Single player
- Tone / Art Direction: Neon, high-contrast, dark translucent panels with bloom.
- Target Platform: iOS (Portrait Mobile)
- Screen Orientation / Resolution: Portrait 1080x1920 (reference)
- Render Pipeline: UniversalRP (2D)

# Game Mechanics
## Core Gameplay Loop
- Tap to earn Brains, spend in the Shop on idle buildings; rank progression swaps the visible diorama via alpha cross-fade.
## Controls and Input Methods
- Full-screen TapButton; scrollable Shop with per-building Buy buttons.

# UI
- Shop ScrollView already staged under Canvas (Content has VerticalLayoutGroup + ContentSizeFitter).
- UpgradeSlotPrefab rows show Name, Cost, and Count (owned), plus a Buy button.
- Diorama backdrop rendered by a dedicated Diorama Camera into the top 40% of the screen.

# DECISIONS (from user clarification)
1. Dioramas remain world-space SpriteRenderers; fading is via `SpriteRenderer.color.a`. **CanvasGroup is intentionally NOT added** (it has no effect on SpriteRenderers).
2. A new dedicated Diorama Camera is created (none exists today).
3. The prefab's `LevelText` is renamed to `CountText` (with matching script field rename).

# NOTES / FLAGS
- **Viewport overlap:** Diorama Camera viewport (X:0, Y:0.6, W:1, H:0.4 = top 40%) overlaps the opaque `ShopPanel` (top 55%). The diorama will be hidden behind the shop unless the shop is closed/hidden in a "diorama view" state. Left for a later UI-state pass.
- **Overlay canvases & depth:** `Canvas` and `ChaosPopUpCanvas` are Screen Space - Overlay, which always draw above ALL cameras regardless of camera depth. "Diorama camera depth > UI camera" is satisfied relative to the Main Camera; Overlay UI still renders on top.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- Existing (verified):
  - `Canvas/ShopPanel/ShopScrollView/Viewport/Content` — VerticalLayoutGroup (spacing 12) + ContentSizeFitter (Vertical = Preferred Size). ALREADY CORRECT.
  - `Assets/_Game/Prefabs/UI/UpgradeSlotPrefab.prefab` — InfoColumn(NameText, DescriptionText, LevelText), BuyColumn(BuyButton(CostText)), `UpgradeSlotUI`.
  - `_DioramaContainer` > `Diorama_0_Outcast` … `Diorama_4_President`, each with `SpriteRenderer` only.
  - `Main Camera` (orthographic, depth -1, full viewport). No Diorama Camera.
- Scripts:
  - `Assets/_Game/Scripts/UI/UpgradeSlotUI.cs` — has `levelText` field/property, displays "LVL {level}".
  - `Assets/_Game/Scripts/Core/DioramaManager.cs` — currently toggles via `SetActive`; will move to alpha cross-fade.

# Implementation Steps

### Step 1: Verify Shop ScrollView/Content (no change)
- **Description**: Confirm `Canvas/ShopPanel/ShopScrollView/Viewport/Content` has VerticalLayoutGroup (spacing 12) and ContentSizeFitter (Vertical Fit = Preferred Size), ready to receive instantiated rows. Already verified present and correct — included for completeness; no edits expected.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Rename LevelText -> CountText (prefab + script)
- **Description**:
  - In `UpgradeSlotUI.cs`, rename the serialized field `levelText` -> `countText` and the property `LevelText` -> `CountText` (add `[UnityEngine.Serialization.FormerlySerializedAs("levelText")]` to preserve any existing wiring). Update `RefreshState` to display owned count, e.g. `countText.text = $"OWNED: {level}"`.
  - In `UpgradeSlotPrefab.prefab`, rename the child GameObject `LevelText` -> `CountText` and re-assign the `UpgradeSlotUI.CountText` reference to it.
  - The prefab already contains TextMeshPro elements for Name and Cost and a Button, satisfying the "Name, Cost, Count + Button" requirement after this rename.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No (script + prefab must stay consistent)

### Step 3: Diorama alpha-fade rigging (sprites, no CanvasGroup)
- **Description**:
  - Do NOT add CanvasGroup. Confirm each of the 5 children has a `SpriteRenderer` (already present).
  - Create a dedicated user layer `Diorama` (via TagManager) and assign all 5 diorama children to it (so a dedicated camera can render them in isolation).
  - Ensure consistent `SpriteRenderer.sortingOrder` and set all 5 GameObjects active so the manager controls visibility purely through alpha (current state has 1..4 inactive; for alpha cross-fade they should be active with alpha driven by code).
  - Update `DioramaManager.cs` to drive visibility via `SpriteRenderer.color.a` (target alpha 1 for the active rank, 0 for others), keeping its existing rank-index resolution from `GameManager.RankDefinitions`. Initialize non-active dioramas to alpha 0.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes (independent of Steps 1-2)

### Step 4: Create dedicated Diorama Camera
- **Description**:
  - Create a new GameObject `Diorama Camera` with a `Camera` component: orthographic, `depth` higher than Main Camera (e.g. Main stays -1, Diorama = 0), `rect` (Viewport Rect) set precisely to X:0, Y:0.6, W:1, H:0.4, clearFlags = SolidColor (dark neon background) so the top region reads as a self-contained diorama panel.
  - Set the Diorama Camera `cullingMask` to render ONLY the `Diorama` layer; remove the `Diorama` layer bit from the Main Camera `cullingMask` so dioramas render exclusively in the top 40% region (prevents the full-screen Main Camera from also drawing them).
  - Match orthographic size/position so the dioramas frame correctly in the top region (initial sensible default; fine-tuning left to art pass).
- **Assigned role**: developer
- **Dependencies**: Step 3 (the `Diorama` layer must exist first)
- **Parallelizable**: No

# Verification & Testing
- Confirm scripts compile with zero errors/warnings.
- Confirm `Content` still has VLG (spacing 12) + CSF (Preferred Size).
- Confirm `UpgradeSlotPrefab` now has `CountText` and `UpgradeSlotUI.CountText` is wired; Name, Cost, Count, and Buy Button all present.
- Confirm the 5 dioramas have SpriteRenderers, are on the `Diorama` layer, and have NO CanvasGroup.
- Confirm `Diorama Camera` exists: orthographic, depth > Main, rect exactly (0, 0.6, 1, 0.4), cullingMask = Diorama only; Main Camera no longer renders the Diorama layer.
- Play Mode (optional): drive cumulative Brains across rank thresholds and confirm `DioramaManager` cross-fades sprite alpha (active = 1, others = 0) with no errors.
