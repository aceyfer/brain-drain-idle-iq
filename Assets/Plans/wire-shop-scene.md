# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: A satirical mobile idle clicker where players tap to earn Brains, climb "Idiocracy" ranks, and purchase brain-melting buildings to restore a decaying society.
- Players: Single player
- Inspiration / Reference Games: Adventure Capitalist, Cookie Clicker
- Tone / Art Direction: Retro-dystopian cartoon, neon, high-contrast, stylized UI
- Target Platform: iOS (Mobile Portrait)
- Screen Orientation / Resolution: Portrait 1080x1920 (reference)
- Render Pipeline: UniversalRP (URP 2D)

# Game Mechanics
## Core Gameplay Loop
- Players tap the central area to generate Brain Power (BP) and Cash ($) to restore the decaying IQ of the world.
- Players spend BP and Cash inside a unified Shop system to purchase upgrades and restore world stages using restoration points (RP).
## Controls and Input Methods
- Direct touch/click inputs on the main tapping button and the unified, tabbed shop UI.

# UI
## Unified Tabbed Shop Hierarchy
- **ShopRoot**: A parent UI GameObject containing the `ShopTabView` component, wrapped inside the mobile SafeArea to handle notched displays.
- **Tab child Canvases**: 
  - `Tab_BP` (Canvas, ScrollRect, Viewport, Content RectTransform)
  - `Tab_Cash` (Canvas, ScrollRect, Viewport, Content RectTransform)
  - `Tab_RP` (Canvas, ScrollRect, Viewport, Content RectTransform)
- **ShopRowView Prefab**: A virtualized, pooled UI list row template that displays the upgrade/item's name, description, cost, and buy button.

# Key Asset & Context
- **Scene**: `Assets/Scenes/SampleScene.unity`
- **Scripts**:
  - `Assets/_Game/Scripts/UI/ShopTabView.cs` (acts as the ShopTabController / tab coordinator)
  - `Assets/_Game/Scripts/UI/ShopRowView.cs` (visual controller for a single row)
  - `Assets/_Game/Scripts/UI/SafeAreaManager.cs` (applies dynamic screen safe area padding)
  - `Assets/_Game/Scripts/Systems/WorldRestorationManager.cs` (manages restoration stages)
  - `Assets/_Game/Scripts/Systems/CashShopManager.cs` / `PointsShopManager.cs` (manages item databases)
- **Database Assets**:
  - **Stage Database**: List of `WorldRestorationStage` scriptable objects (`WorldRestorationStage_0_ToxicWasteland` to `WorldRestorationStage_5_UtopiaAchieved`).
  - **Item Database**: List of `CashShopItemData` and `PointsShopItemData` scriptable objects.

# Implementation Steps

### Step 1: Create the ShopRowView Prefab
- **Description**: 
  - Create a new UI prefab at `Assets/_Game/Prefabs/UI/ShopRowViewPrefab.prefab` matching the structure of `ShopRowView` in `ShopRowView.cs`.
  - The prefab must contain:
    - TextMeshProUGUI components for `nameText`, `descriptionText`, `costText`, and `countText`.
    - A `Button` component for `buyButton`.
    - An `Image` component for `background`.
  - Attach the `ShopRowView` script component to the root of the prefab and assign the fields.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Set up ShopRoot and Child Canvases under CustomSafeArea
- **Description**: 
  - Open `Assets/Scenes/SampleScene.unity`.
  - Locate `Canvas/CustomSafeArea`.
  - Create a child GameObject named `ShopRoot` under `CustomSafeArea`. Ensure `SafeAreaManager` (which is attached to `CustomSafeArea`) wraps and properly contains it.
  - Attach the `ShopTabView` component to `ShopRoot` (acting as the tab controller).
  - Create 3 child GameObjects under `ShopRoot` named `Tab_BP`, `Tab_Cash`, and `Tab_RP`.
  - Add `Canvas` and `GraphicRaycaster` components to each of these 3 GameObjects to configure them as child Canvases.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 3: Configure ScrollViews under Child Canvases
- **Description**: 
  - Under each child Canvas (`Tab_BP`, `Tab_Cash`, `Tab_RP`), create a vertical `ScrollRect` (Scroll View).
  - Ensure each scroll view contains a `Viewport` child with a `Mask`, and a `Content` child with a `RectTransform`.
  - Disable any layout components on the `Content` child to let the virtualized list handle positioning procedurally (as expected by `ShopTabView.cs`).
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Wire ShopTabView TabPanel References and Prefab
- **Description**: 
  - Select `ShopRoot` and inspect the `ShopTabView` component.
  - Assign the `rowPrefab` field to the `ShopRowViewPrefab` created in Step 1.
  - Set the size of the `tabPanels` array to 3. Configure the three entries as follows:
    - **Element 0**: Tab = `BpUpgrades`, Tab Canvas = `Tab_BP`'s Canvas, Scroll Rect = `Tab_BP`'s ScrollRect, Content = `Tab_BP`'s Content RectTransform, Tab Button = `BpTabButton` (inside `ShopTabBar`).
    - **Element 1**: Tab = `CashInvestments`, Tab Canvas = `Tab_Cash`'s Canvas, Scroll Rect = `Tab_Cash`'s ScrollRect, Content = `Tab_Cash`'s Content RectTransform, Tab Button = `CashTabButton` (inside `ShopTabBar`).
    - **Element 2**: Tab = `RpRestorations`, Tab Canvas = `Tab_RP`'s Canvas, Scroll Rect = `Tab_RP`'s ScrollRect, Content = `Tab_RP`'s Content RectTransform, Tab Button = `RpTabButton` (inside `ShopTabBar`).
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 3
- **Parallelizable**: No

### Step 5: Hook Tab Bar Buttons to SelectTab Method
- **Description**: 
  - In `ShopTabView.cs`, the buttons are automatically wired to call `SelectTab` with the correct enum value in `Awake()` via `WireTabButtons()`.
  - To verify, ensure that the `tabButton` field in each `TabPanel` entry of the `tabPanels` array is correctly assigned to the tab bar buttons (`BpTabButton`, `CashTabButton`, `RpTabButton` inside `ShopTabBar`) so they are wired correctly on awake.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

### Step 6: Assign Stage and Item Database References on _Systems
- **Description**: 
  - Select the `_Systems` GameObject in `SampleScene.unity`.
  - Verify and assign all Stage Database references on the `WorldRestorationManager` component:
    - Drag `WorldRestorationStage_0_ToxicWasteland` to `WorldRestorationStage_5_UtopiaAchieved` assets into the `stages` field.
  - Verify and assign all Item Database references on the `CashShopManager` and `PointsShopManager` components:
    - Ensure `CashShopItemData` assets are assigned on `CashShopManager`.
    - Ensure `PointsShopItemData` assets are assigned on `PointsShopManager`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

# Verification & Testing
1. **Scene Hierarchy Check**:
   - Verify `Canvas/CustomSafeArea/ShopRoot` exists.
   - Verify `Tab_BP`, `Tab_Cash`, and `Tab_RP` are children of `ShopRoot`, and each has a `Canvas` and `GraphicRaycaster` component.
   - Confirm `SafeAreaManager` component is attached to `CustomSafeArea` and therefore wraps `ShopRoot` perfectly.
2. **ShopTabView Component Inspector Check**:
   - Verify `rowPrefab` is assigned to `ShopRowViewPrefab`.
   - Verify the `tabPanels` array has 3 elements fully assigned (Canvas, ScrollRect, Content, Tab Button) for BP, Cash, and RP.
3. **Systems Database Verification**:
   - Check `_Systems` GameObject components: `WorldRestorationManager`, `CashShopManager`, and `PointsShopManager`. Confirm that all stages and item assets are correctly assigned and none are missing.
4. **Play Mode Test**:
   - Enter Play Mode in the Unity Editor.
   - Click the "SHOP" HUD button to open the shop panel.
   - Click each of the three tab buttons (BP UPGRADES, CASH INVESTMENTS, RP RESTORATIONS) and verify that `ShopTabView.SelectTab` is triggered, updating the active tab canvas and highlighting the selected button.
   - Verify that the virtualized, pooled list correctly spawns and populates the `ShopRowView` rows with data from the databases.
