# Project Overview
- **Game Title**: Brain Drain: Idle IQ
- **High-Level Concept**: A satirical idle clicker game where players tap to earn Brains and restore decaying IQ, escalating in level and difficulty over time, eventually cashing out ("selling their brains for parts") to gain permanent progression multipliers.
- **Players**: Single player
- **Inspiration / Reference Games**: AdVenture Capitalist, Cookie Clicker, Universal Paperclips
- **Tone / Art Direction**: Satirical corporate-dystopian, featuring dark synthwave colors and high-contrast neon visual accents.
- **Target Platform**: iOS (mobile)
- **Screen Orientation / Resolution**: Portrait 1080x1920 (Landscape supported but optimized for portrait)
- **Render Pipeline**: UniversalRP

# Game Mechanics
## Core Gameplay Loop
1. **Tap & Idle Income**: Players tap the screen or hire automated upgrade systems to generate "Brains" (the primary resource).
2. **IQ Decay**: The player's IQ decay rate constantly pressures their overall multiplier.
3. **Rebirth (The "Sell Brain for Parts" Loop)**: When progression slows, the player resets their soft currency, decay rate, and building upgrades in exchange for a permanent income multiplier boost calculated from their cumulative run earnings.

## Controls and Input Methods
- **Tap Input**: Handled via the New Input System on the main full-screen TapButton.
- **UI Button Taps**: Standard Unity UI button events mapped to HUD interactions, scroll lists, and confirmation modals.

# UI
## Rebirth Modal Layout (Bottom 60% Screen Space Overlay)
To layer cleanly on top of the Screen Space - Overlay HUD without clipping into the top 40% Diorama Camera viewport (Y = 0.60 to 1.00):
```
+---------------------------------------+
|                                       |
|        DIORAMA VIEWPORT (3D)          |
|       (Occupies Top 40% Y: 0.6-1.0)   |
|                                       |
+=======================================+ <--- Anchor Max Y: 0.60
|                                       |
|            REBIRTH MODAL              |
|        (Occupies Bottom 60% Y: 0-0.6) |
|                                       |
|        "SELL BRAIN FOR PARTS?"        |
|                                       |
|       Pending multiplier increase:    |
|             +X.XXx MULTIPLIER         |
|                                       |
|      [ SELL BRAIN ]    [ ABORT ]      |
|                                       |
+---------------------------------------+ <--- Anchor Min Y: 0.00
```

### RectTransform Configuration (RebirthModal)
- **Parent**: `Canvas` (Screen Space - Overlay)
- **Anchor Min**: `X: 0.0, Y: 0.0`
- **Anchor Max**: `X: 1.0, Y: 0.6`
- **Pivot**: `X: 0.5, Y: 0.5`
- **Anchored Position**: `X: 0, Y: 0`
- **Size Delta**: `X: 0, Y: 0`
- **Offsets**: `Left: 0, Right: 0, Top: 0, Bottom: 0`
- **Result**: The panel precisely scales and bounds itself to the bottom 60% of the screen under any aspect ratio, completely avoiding clipping into the top 40% diorama viewport.

### High-Contrast Neon Outline Button Style
To construct high-contrast neon outlines without requiring custom external sprite assets, we employ a nested uGUI approach:
1. **Parent GameObject (`ConfirmButton`)**:
   - `RectTransform`: Width = `200`, Height = `50`.
   - `Image` component: Color = `#00F0FF` (Neon Cyan).
   - `Button` component.
2. **Inner Fill GameObject (`InnerFill` - child of ConfirmButton)**:
   - `RectTransform` Anchors: Stretch-Stretch (`Min: 0,0; Max: 1,1`).
   - `RectTransform` Margins: `Left: 2, Right: 2, Top: 2, Bottom: 2`.
   - `Image` component: Color = `#0D0E12` (Dark black-grey to match modal background).
3. **Text GameObject (`ButtonText` - child of InnerFill)**:
   - `TextMeshProUGUI` component: Text = "SELL BRAIN", Style = **Bold**, Size = `16`, Color = `#00F0FF` (Neon Cyan), Alignment = Center/Middle.

This creates a perfect, resolution-independent, crisp 2-pixel neon cyan border button. A symmetrical layout with a magenta-themed `#FF007F` Cancel button ("ABORT") is added alongside it.

# Key Asset & Context
## Existing Files to Modify
1. **`Assets\_Game\Scripts\Systems\RebirthManager.cs`**
   - Add a public property to access the pending multiplier increase calculation.
   - Signature:
     ```csharp
     public double PendingMultiplierIncrease => currencyManager != null ? (currencyManager.CumulativeBrains / cumulativeBrainsPerMultiplierPoint) : 0d;
     ```

## New Files to Create
1. **`Assets\_Game\Scripts\UI\RebirthUIController.cs`**
   - Coordinates the visual state of the Rebirth modal, pulls data from `RebirthManager.Instance`, and handles button clicks.
   - Script blueprint:
     ```csharp
     using UnityEngine;
     using UnityEngine.UI;
     using TMPro;
     using BrainDrain.Systems;

     namespace BrainDrain.UI
     {
         public sealed class RebirthUIController : MonoBehaviour
         {
             [Header("UI Panels")]
             [SerializeField] private GameObject rebirthModalPanel;

             [Header("Visual Fields")]
             [SerializeField] private TextMeshProUGUI multiplierText;

             [Header("Interactive Buttons")]
             [SerializeField] private Button confirmButton;
             [SerializeField] private Button cancelButton;

             private void Awake()
             {
                 if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
                 if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
             }

             public void OpenModal()
             {
                 if (rebirthModalPanel != null)
                 {
                     rebirthModalPanel.SetActive(true);
                     UpdateVisuals();
                 }
             }

             public void CloseModal()
             {
                 if (rebirthModalPanel != null)
                 {
                     rebirthModalPanel.SetActive(false);
                 }
             }

             private void UpdateVisuals()
             {
                 if (multiplierText != null && RebirthManager.Instance != null)
                 {
                     double pending = RebirthManager.Instance.PendingMultiplierIncrease;
                     multiplierText.text = $"+{pending:F2}x MULTIPLIER";
                 }
             }

             private void OnConfirmClicked()
             {
                 if (RebirthManager.Instance != null)
                 {
                     RebirthManager.Instance.TriggerBrainReset();
                 }
                 CloseModal();
             }

             private void OnCancelClicked()
             {
                 CloseModal();
             }
         }
     }
     ```

# Implementation Steps
## Step 1: Add API to `RebirthManager.cs`
- **Description**: Add public getter properties in `RebirthManager.cs` to access `cumulativeBrainsPerMultiplierPoint` and calculate the pending multiplier increase on demand.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 2: Create `RebirthUIController.cs` script
- **Description**: Create the controller class to manage modal visibility, update the TMPro multiplier text on open, and execute the rebirth wipe via `RebirthManager` or close the modal on click.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

## Step 3: Construct the `RebirthModal` in Canvas hierarchy
- **Description**: Setup the uGUI game object hierarchy inside the Screen Space - Overlay `Canvas`:
  - Place `RebirthModal` as the last child of `Canvas` (to ensure top overlay rendering order).
  - Configure `RectTransform` anchors to `Min: (0, 0), Max: (1, 0.6)` and offsets to zero to bound Y limits.
  - Add dark, semi-transparent background Image (`Color(0.04f, 0.04f, 0.06f, 0.92f)`).
  - Add Title TextMeshProUGUI element with "SELL BRAIN FOR PARTS?".
  - Add Description TextMeshProUGUI element with pending multiplier.
  - Create standard high-contrast Neon Cyan button structure for `ConfirmButton` and Magenta button structure for `CancelButton`.
  - Disable the parent `RebirthModal` GameObject by default so it remains hidden.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

## Step 4: Attach and wire `RebirthUIController`
- **Description**: 
  - Attach the `RebirthUIController` component to the `Canvas` object or directly to the parent `RebirthModal` panel.
  - Drag and drop scene references for `RebirthModalPanel`, `MultiplierText`, `ConfirmButton`, and `CancelButton` into their inspector slots.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

## Step 5: (Optional Hook) Wire Trigger to HUD or Shop
- **Description**: Add a temporary HUD or Shop trigger button (or leverage a keyboard shortcut like 'R' in developer mode) to call `RebirthUIController.OpenModal()`.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

## Step 6: Create RebirthManager GameObject in Scene
- **Description**: Create an empty GameObject named 'RebirthManager' in the active scene and attach the 'RebirthManager.cs' script as a component to it.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 7: Add DoomscrollEngine to UpgradeManager Templates
- **Description**: Locate the UpgradeManager component on the '_Systems' GameObject in the active scene and add the 'DoomscrollEngine.asset' ScriptableObject to its 'buildingTemplates' array.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

# Verification & Testing
1. **Viewport Separation Audit**: Enter Play Mode and change resolutions (e.g. Portrait 16:9, Portrait 19.5:9, iPad 4:3). Verify that the `RebirthModal`'s upper edge always stays locked exactly below the 40% screen threshold (the Diorama Camera rendering area).
2. **Neon Border Outline Rendering**: Confirm in Game View that both confirmation and cancel buttons display a pixel-perfect 2px solid neon colored outline around a dark fill, with crisp bold text centered.
3. **State Visibility Test**: Stop/Play state cycle should verify that `RebirthModal` starts inactive. Triggering `OpenModal()` displays the window.
4. **Multiplier Calculation & Format Test**: Check that the text displays `+X.XXx MULTIPLIER` with two decimal precision matching current cumulative brains divided by `1,000,000`.
5. **Rebirth Functionality Test**: Click `SELL BRAIN` and verify:
   - Currency resets to zero.
   - Buildings/Upgrades are wiped.
   - IQ Decay state resets.
   - Multplier increases permanently.
   - Modal closes automatically.
6. **Cancel / Abort Integrity Test**: Click `ABORT` and verify:
   - Modal closes.
   - No currency/building state is modified.
