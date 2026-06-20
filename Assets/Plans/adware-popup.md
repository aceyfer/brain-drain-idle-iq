# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: An idle clicker game where players tap to earn Brains and restore decaying IQ, with retro satirical adware popup events and other chaotic elements.
- Players: Single player
- Inspiration / Reference Games: Cookie Clicker, early internet browser adware pop-ups, Retro clickers.
- Tone / Art Direction: Neon, high-contrast, chaotic retro 2000s style.
- Target Platform: iOS / Mobile
- Screen Orientation / Resolution: Portrait 1080x1920 (16:9)
- Render Pipeline: UniversalRP (URP)

# Game Mechanics
## Core Gameplay Loop
- Players tap to generate Brains, advancing levels and causing their IQ to decay faster.
- Periodically, chaotic random pop-up events trigger, prompting the player to click a garish adware popup.
- The adware popup contains a dark-pattern "Fake Close Button" which dodges the mouse/finger on the first click and displays a "Nice try!" message. On the second click, it successfully dismisses the popup.
- Accepting the adware popup's action reward grants Brains/IQ or triggers penalties.

## Controls and Input Methods
- Touch input / Mouse clicks on pop-up buttons.
- The fake close button uses uGUI event listeners to intercept clicks, triggering a random positional dodge on its first click and dismissal on the second click.

# UI
The popup UI will be styled as a garish, bright, chaotic 2000s adware modal centered inside the screen.
- **Root Container**: `AdwareEventPopup` (centered, Width: 350, Height: 450)
- **Border style**: Option A (Nested Panels) - A thick Neon Cyan (`#00F0FF`) outer border with a glaring bright Hot Pink (`#FF007F`) inner background body. This ensures pixel-perfect sharpness at all screen resolutions without Outline shader artifacts.
- **Title**: Bold neon green text (`#00FF00`) shouting: `!!! YOU ARE THE 1,000,000th VISITOR !!!`
- **Description**: Italic white or black serif/sans-serif text describing the hilarious brain rot reward or penalty.
- **Action Button**: Large Neon Yellow button (`#FFFF00`) with dark bold text saying `CLAIM FREE BRAIN ROT`.
- **Fake Close Button**: Small Windows-style classic grey square button (`#CCCCCC`) with a bold dark red `"X"` at the top-right corner.
- **Nice Try Text**: Hidden small text underneath the close button saying `"Nice try!"`.

# Key Asset & Context
- Active Scene: `Assets/Scenes/SampleScene.unity`
- Canvas: `ChaosPopUpCanvas` (Sorting Order: 10, perfect for displaying popups on top of HUD)
- Script: `Assets/_Game/Scripts/UI/RandomEventUIController.cs`
  - Needs reference slots:
    - `eventPopupPanel` (GameObject)
    - `titleText` (TextMeshProUGUI)
    - `descriptionText` (TextMeshProUGUI)
    - `actionButtonText` (TextMeshProUGUI)
    - `niceTryText` (TextMeshProUGUI)
    - `actionButton` (Button)
    - `fakeCloseButton` (Button)

# Implementation Steps

### Step 1: Create the AdwareEventPopup GameObject Hierarchy
- **Description**: Implement an editor script to construct the complete pop-up hierarchy programmatically inside `ChaosPopUpCanvas`.
  - Parent Panel: `AdwareEventPopup` (RectTransform: Width: 350, Height: 450, Center-Middle anchor, Image color: `#00F0FF` for border effect).
  - Inner Body: `PopupInnerBody` (RectTransform: Anchors stretch, offset 8px on all sides, Image color: `#FF007F` Hot Pink).
  - **Children under PopupInnerBody**:
    - `TitleText` (TextMeshProUGUI): Top-aligned, Bold, Neon Green (`#00FF00`), size 24. Text: `"!!! YOU ARE THE 1,000,000th VISITOR !!!"`
    - `DescriptionText` (TextMeshProUGUI): Centered, Italic, Black (`#000000`) for high contrast, size 16. Text: `"Claim your prize now!"`
    - `ActionButton` (Button): Bottom-centered (size 260x60), Color: Neon Yellow (`#FFFF00`).
      - Child `Text (TMP)` (TextMeshProUGUI): Bold, Dark Grey (`#121212`), size 16. Text: `"CLAIM FREE BRAIN ROT"`
    - `FakeCloseButton` (Button): Top-right corner (size 30x30), Color: Classic Grey (`#CCCCCC`).
      - Child `Text (TMP)` (TextMeshProUGUI): Bold, Dark Red (`#C00000`), size 16. Text: `"X"`
    - `NiceTryText` (TextMeshProUGUI): Positioned right under close button (offset from corner), Color: Neon Yellow (`#FFFF00`), size 12, Text: `"Nice try!"`. Initially inactive.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Attach RandomEventUIController and Assign References
- **Description**: Add the `RandomEventUIController` script component to the `AdwareEventPopup` GameObject. Assign the serialized fields inside the component to their corresponding newly created child components:
  - `eventPopupPanel` -> `AdwareEventPopup` GameObject
  - `titleText` -> `TitleText` (TextMeshProUGUI)
  - `descriptionText` -> `DescriptionText` (TextMeshProUGUI)
  - `actionButtonText` -> `ActionButton/Text (TMP)` (TextMeshProUGUI)
  - `niceTryText` -> `NiceTryText` (TextMeshProUGUI)
  - `actionButton` -> `ActionButton` (Button)
  - `fakeCloseButton` -> `FakeCloseButton` (Button)
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Deactivate the AdwareEventPopup Root
- **Description**: Set `AdwareEventPopup` to inactive state (`SetActive(false)`) by default so it only displays when triggered.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Add Dynamic Raycast Blocking and Canvas Toggling to RandomEventUIController
- **Description**: Modify `RandomEventUIController.cs` to dynamically toggle its parent Canvas active/inactive state and configure/acquire a CanvasGroup when the popup opens/closes.
  - In `Awake`, search for the parent Canvas on `ChaosPopUpCanvas`. Set `parentCanvas.enabled = false` initially.
  - Add a `CanvasGroup` to `ChaosPopUpCanvas` via code or editor if not present, and toggle `interactable` and `blocksRaycasts` to match the popup's state.
  - In `HandleRandomEventTriggered` (Popup Show), enable the Canvas and the CanvasGroup interactions.
  - In `ClosePopup` (Popup Hide), disable the Canvas and the CanvasGroup interactions to prevent click-blocking on underlying elements.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 5: Clean Up Main Canvas Raycast Targets
- **Description**: Audit the Main Canvas and run a utility script to turn off 'Raycast Target' for all static, non-interactive text elements (e.g. BrainsCounterText, GlobalIQText) and any empty helper parent containers that do not require click detection.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 6: Verify MainTapButton Positioning and Overlap Bounds
- **Description**: Perform a layout bounding check to ensure `MainTapButton`'s RectTransform is anchored around `(0.5, 0.20)` and does not structurally block or overlap the ShopScrollView or the Library upgrade rows above it.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 7: Create and Repair the Static 'Library' Upgrade Slot Row
- **Description**: Construct a static 'Library' row under `Canvas/ShopPanel/ShopScrollView/Viewport/Content` by instantiating `UpgradeSlotPrefab`.
  - Name it `Library` or `UpgradeSlot_The Literal Library`.
  - Verify its root has an Image component with `Raycast Target` explicitly enabled.
  - Verify that a `Button` component is present on the slot/row, and configure its click events to link directly to `UpgradeManager.Instance.TryBuyBuilding()` passing `TheLiteralLibrary` as parameter.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

# Verification & Testing
- Open `SampleScene` in Unity.
- Verify `AdwareEventPopup` is situated under `ChaosPopUpCanvas` and is inactive.
- Temporarily set `AdwareEventPopup` to active to inspect layout quality:
  - Verify centered size is exactly 350x450.
  - Verify Hot Pink and Neon Cyan border look glaringly neon and retro.
  - Verify the elements (Title, Description, Buttons) are aligned and legible.
- Run the game in Unity Play Mode:
  - Click the "X" (FakeCloseButton) once. Verify it dodges to a random nearby position and displays "Nice try!".
  - Click the "X" a second time. Verify the pop-up deactivates and closes.
  - Wait for RandomEventManager to trigger an event, or trigger it programmatically (e.g., `RandomEventManager.Instance.TriggerRandomEvent()`).
  - Verify popup automatically becomes active, displays correct title, description, and action button text.
  - Click the action button "CLAIM FREE BRAIN ROT" and verify rewards are applied to the player.
