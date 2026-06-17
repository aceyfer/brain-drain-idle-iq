# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: A satirical idle clicker where players tap to earn Brains, climb "Idiocracy" ranks, and buy brain-melting buildings.
- Players: Single player
- Tone / Art Direction: Neon, high-contrast, dark translucent panels with bloom.
- Target Platform: iOS (Portrait Mobile)
- Screen Orientation / Resolution: Portrait 1080x1920 (reference)
- Render Pipeline: UniversalRP

# Game Mechanics
## Core Gameplay Loop
- Players tap to earn Brains, then spend Brains in the Shop to purchase/upgrade idle buildings that generate passive Brains (and modify IQ decay).
## Controls and Input Methods
- Full-screen `TapButton` for tapping; a scrollable Shop panel with per-building "Buy" buttons.

# UI
- **ShopPanel**: A dark translucent panel anchored to the top, filling the top 55% of the screen.
- **ShopScrollView**: A vertical ScrollRect inside ShopPanel listing one row per building.
- **UpgradeSlotPrefab**: A reusable row showing building Name, Description, Cost, owned Level, and a Buy button.
- **ChaosPopUpCanvas**: A separate overlay Canvas (sort order 10) reserved for future pop-up notifications.

# ASSUMPTIONS (clarification timed out — veto at review if undesired)
1. **Canvas scaling**: The existing `Canvas` will be switched from Constant Pixel Size (800x600) to **Scale With Screen Size, Reference Resolution 1080x1920, Match Width Or Height = 0.5**. This is required for the "top 55%" panel and HUD to scale consistently on mobile. This may slightly change the on-screen size of existing HUD text (BrainsText/IQText/LevelText), which is expected and visually correct for mobile.
2. **UpgradeManager additions**: Minor, backward-compatible additions to `UpgradeManager.cs`:
   - A public read-only accessor `IReadOnlyList<BuildingData> BuildingTemplates`.
   - A public `event Action OnBuildingsChanged` invoked inside `TryBuyBuilding` on a successful purchase, so the shop can refresh slot states.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- Existing Canvas hierarchy: `Canvas` (Overlay, sort 0) > `TapButton`, `BrainsText`, `IQText`, `LevelText`; `HUDController` on Canvas.
- Existing scripts:
  - `Assets/_Game/Scripts/Core/UpgradeManager.cs` — `TryBuyBuilding(BuildingData)`, `GetCurrentCost(BuildingData)`, `GetBuildingLevel(BuildingData)`. `buildingTemplates` currently private.
  - `Assets/_Game/Scripts/Core/BuildingData.cs` — fields: `buildingName`, `description`, `unlockPlayerLevel`, `baseCost`, `costMultiplier`, `baseBrainsPerSecond`, `iqRecoveryPerSecond`.
  - `Assets/_Game/Scripts/Core/CurrencyManager.cs` — `Brains`, `OnBrainsChanged`, `CanAffordBrains`.
  - `Assets/_Game/Scripts/Core/IQDecaySystem.cs` — `CurrentLevel`, `OnLevelChanged`.
  - `Assets/_Game/Scripts/Core/NumberFormatter.cs` — `Format(double)` for compact currency strings (e.g. 1.25M).
  - `Assets/_Game/Scripts/Core/GameManager.cs` — singleton hub.
- New scripts to create:
  - `Assets/_Game/Scripts/UI/UpgradeSlotUI.cs`
  - `Assets/_Game/Scripts/UI/ShopUIController.cs`
- New prefab: `Assets/_Game/Prefabs/UI/UpgradeSlotPrefab.prefab`

# Target Shop Hierarchy (uGUI)
```
Canvas (Scale With Screen Size 1080x1920)
└── ShopPanel (RectTransform: top-stretch, top 55%; Image dark translucent; ShopUIController)
    └── ShopScrollView (ScrollRect: vertical only, horizontal off)
        ├── Viewport (RectTransform full-stretch; Mask; Image)
        │   └── Content (RectTransform top-stretch, pivot 0.5,1; VerticalLayoutGroup; ContentSizeFitter)
        │         └── [UpgradeSlotPrefab instances added at runtime]
        └── Scrollbar Vertical (optional, right edge)
```
UpgradeSlotPrefab internal layout:
```
UpgradeSlotPrefab (RectTransform height ~180; Image bg; HorizontalLayoutGroup; LayoutElement minHeight; UpgradeSlotUI)
├── InfoColumn (VerticalLayoutGroup)
│   ├── NameText (TextMeshProUGUI)
│   ├── DescriptionText (TextMeshProUGUI)
│   └── LevelText (TextMeshProUGUI)
└── BuyColumn (VerticalLayoutGroup)
    └── BuyButton (Button + Image)
        └── CostText (TextMeshProUGUI)
```

# Implementation Steps

### Step 1: Extend UpgradeManager.cs (accessor + event)
- **Description**: In `Assets/_Game/Scripts/Core/UpgradeManager.cs`, add `using System;`. Add public `IReadOnlyList<BuildingData> BuildingTemplates => buildingTemplates;`. Add `public event Action OnBuildingsChanged;` and invoke it at the end of a successful `TryBuyBuilding` (after incrementing the building level). No behavior change to existing logic.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Create UpgradeSlotUI.cs
- **Description**: Create `Assets/_Game/Scripts/UI/UpgradeSlotUI.cs` in namespace `BrainDrain.UI`. Serialized fields: `TextMeshProUGUI nameText, descriptionText, costText, levelText`; `UnityEngine.UI.Button buyButton`; `UnityEngine.UI.Image background`. Public method `Bind(BuildingData data, UpgradeManager manager)` caches references and wires `buyButton.onClick` to call `manager.TryBuyBuilding(data)`. Public method `RefreshState(CurrencyManager currency, IQDecaySystem decay)` that:
  - Computes `unlocked = decay.CurrentLevel >= data.unlockPlayerLevel`, `cost = manager.GetCurrentCost(data)`, `level = manager.GetBuildingLevel(data)`, `affordable = currency.CanAffordBrains(cost)`.
  - Sets text: name (`data.buildingName`, redacted "???" when locked), description, `levelText = "LVL " + level`, `costText = NumberFormatter.Format(cost)` (or "LEVEL X REQUIRED" when locked).
  - Applies the three visual states using neon hex colors:
    - Locked: muted gray `#4A4E5D`, button non-interactable.
    - Affordable: neon cyan `#00F0FF` accent, button interactable.
    - Too expensive: neon pink `#FF007F` accent, button interactable (kept clickable; purchase will silently fail until affordable).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes (with Step 5/6 hierarchy work, but Step 4 prefab needs this compiled)

### Step 3: Create ShopUIController.cs
- **Description**: Create `Assets/_Game/Scripts/UI/ShopUIController.cs` in namespace `BrainDrain.UI`. Serialized fields: `UpgradeManager upgradeManager`, `CurrencyManager currencyManager`, `IQDecaySystem iqDecaySystem` (dependencies from `_Systems`); `RectTransform content`; `UpgradeSlotUI slotPrefab`. On `Start`/`OnGameInitialized`: clear `content` children, instantiate one `slotPrefab` per `upgradeManager.BuildingTemplates`, call `Bind` then `RefreshState` on each. Subscribe to `currencyManager.OnBrainsChanged`, `iqDecaySystem.OnLevelChanged`, and `upgradeManager.OnBuildingsChanged` to call `RefreshAllSlots()`. Unsubscribe in `OnDestroy`. Keep a cached list of instantiated slots paired with their `BuildingData`.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 2
- **Parallelizable**: No

### Step 4: Build UpgradeSlotPrefab and save as project asset
- **Description**: Construct the `UpgradeSlotPrefab` row hierarchy (see layout above) in the scene temporarily, add TextMeshPro elements (Name, Description, Cost, Level) and a Buy `Button` (fully qualified `UnityEngine.UI.Button`/`Image`). Add `LayoutElement` (min height ~180, flexible width) and `HorizontalLayoutGroup`. Attach `UpgradeSlotUI` and wire its serialized references to the child elements. Save it as a prefab at `Assets/_Game/Prefabs/UI/UpgradeSlotPrefab.prefab`, then remove the temporary scene instance.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 5: Switch Canvas scaler + create ShopPanel and ScrollView
- **Description**:
  - Update the existing `Canvas` `CanvasScaler` to UI Scale Mode = Scale With Screen Size, Reference Resolution = (1080, 1920), Screen Match Mode = Match Width Or Height, Match = 0.5 (per ASSUMPTION 1).
  - Create `ShopPanel` under `Canvas`: RectTransform anchored top-stretch (anchorMin (0,1), anchorMax (1,1), pivot (0.5,1)), height = 55% of 1920 = 1056, top offset 0, full width. Add `UnityEngine.UI.Image` with dark translucent color (`#0B0C10` at alpha ~0.85). Order it as the last/topmost sibling so its raycast sits above the full-screen `TapButton` in the top region.
  - Create `ShopScrollView` under `ShopPanel` with `ScrollRect` (Horizontal = false, Vertical = true). Build `Viewport` (full-stretch, `Mask` + `Image`) and `Content` (top-stretch, pivot 0.5,1). Add `VerticalLayoutGroup` (spacing 12, padding 20, Control Child Width = true, Control Child Height = true, Child Force Expand Width = true, Height = false) and `ContentSizeFitter` (Vertical Fit = Preferred Size, Horizontal = Unconstrained) to `Content`. Assign ScrollRect.content = Content, ScrollRect.viewport = Viewport.
- **Assigned role**: developer
- **Dependencies**: None (can run before scripts, but final wiring needs Step 3/4)
- **Parallelizable**: Partially

### Step 6: Attach + wire ShopUIController on ShopPanel
- **Description**: Attach `ShopUIController` to `ShopPanel`. Assign `upgradeManager`, `currencyManager`, `iqDecaySystem` from the `_Systems` GameObject components; assign `content` to the ScrollView Content RectTransform; assign `slotPrefab` to the `UpgradeSlotPrefab` asset created in Step 4.
- **Assigned role**: developer
- **Dependencies**: Step 3, Step 4, Step 5
- **Parallelizable**: No

### Step 7: Create ChaosPopUpCanvas overlay
- **Description**: Create a new root GameObject `ChaosPopUpCanvas` with `Canvas` (Screen Space - Overlay, Sort Order = 10), `CanvasScaler` (Scale With Screen Size 1080x1920, Match 0.5 to match the main Canvas), and `GraphicRaycaster`. Leave it empty as a host for future pop-ups. Ensure exactly one `EventSystem` remains in the scene (do not add a second).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

# Verification & Testing
- Confirm scripts compile with zero errors/warnings.
- Confirm `ShopPanel` covers the top 55% with a dark translucent background; HUD text below remains visible.
- Confirm `ShopScrollView` scrolls vertically only; `Content` grows with rows via ContentSizeFitter and 12px spacing.
- Enter Play Mode:
  - Verify 5 building rows are generated (The Literal Library, Clickbait Farm, Doomscroll Assembly, Influencer Pod, The Broadcast Tower).
  - Verify locked rows (player level below unlock) show the muted/locked state; affordable rows show cyan; too-expensive rows show pink.
  - Tap to accumulate Brains; verify the first affordable building's Buy button purchases it, deducts Brains, increments the level label, and refreshes all slot states.
- Confirm `ChaosPopUpCanvas` exists at sort order 10 above the main Canvas, and only one EventSystem is present.
- Confirm taps in the bottom (non-shop) region still register on `TapButton`, and taps on the shop do not fall through to `TapButton`.
