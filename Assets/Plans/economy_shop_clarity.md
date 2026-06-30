# Project Overview
- Game Title: Brain Drain
- High-Level Concept: An addictive, dark-humor idle clicker game where the player clicks to generate Brain Power, builds an automated empire of brain-rot media, takes economic control of the world via Cash, and wages war against the shadow council.
- Players: Single player
- Inspiration / Reference Games: Adventure Capitalist, Cookie Clicker, Universal Paperclips
- Tone / Art Direction: Retro-futuristic, vaporwave corporate neon, cyber-gothic snot conspiracies
- Target Platform: Mobile (Portrait)
- Screen Orientation / Resolution: Portrait 1080x1920
- Render Pipeline: UniversalRP (URP)

# Game Mechanics
## Core Gameplay Loop
The player taps the screen to gain Brain Power (BP), which is spent on purchasing and upgrading automated buildings (e.g., Doomscroll Engines, Crypto-Bro Compounds). The player establishes economic dominance by generating Cash per second from specialized ventures (e.g. Underground Economy, Reality TV Syndicate). Gained Cash is spent on high-profile permanent items in the $ SHOP to permanently amplify Tap Power and production multipliers, or converted into Restoration Points to restore a polluted, toxic world. Once sufficient world restoration is complete, the player performs "The Snotting" (Rebirth) to reset Brain Power and buildings in exchange for massive, permanent multiplicative boosts.

## Controls and Input Methods
The game uses simple, portrait-oriented mobile-optimized tap controls. Buttons are designed as large touch targets (at least 180x80px for buying, 80x80px for closing) with high-contrast text and neon indicators to prevent mis-taps on small handheld devices.

# UI
All interfaces (BP Shop, $ SHOP, Points Shop, Convert Panel, Rebirth Modal) fit within the safe area of a portrait mobile view. Elements use scalable TextMeshPro components with word-wrapping and overflow settings enabled to prevent text from overlapping or extending past panel borders. Interactive panels contain a prominent neon-pink close button ("X") set as the topmost rendering layer to remain fully clickable.

# Key Asset & Context
We will modify the following assets:
- **`UpgradeSlotUI.cs`**: Displays building level, next costs, next BP/sec and Cash/sec gains, and current cumulative production contribution.
- **`CashShopSlotUI.cs`**: Displays Cash Shop items, cost, and exact permanent multiplicative effects.
- **`PointsShopSlotUI.cs`**: Displays Point-spent upgrades and exact permanent conversion effects.
- **`RebirthUIController.cs`**: Keeps the "THE SNOTTING" (Rebirth) trigger button visible at all times, showing locking status and plain language progress (spent Points / required threshold) directly on the HUD / button.
- **`HUDController.cs`**: Displays restoration progress and snotting unlock progress in plain language in the main HUD header.
- **`RebirthManager.cs`**: Returns updated player-facing display titles using "Illumisnotty" spelling.
- **`RandomChatterManager.cs`**: Refactors chatter string arrays to use "Illumisnotty" instead of "Illumisnotti".
- **`COGSPortraitController.cs`** & **`COGSStage.cs`**: Updates developer docs/comments to outline the COGS progression arc (Stage 1 corrupted to Stage 6 godlike).
- **Building Assets**:
  - `UndergroundEconomy.asset` (Increase `baseCashPerSecond` from 0.5 to 2.5)
  - `CryptoBroCompound.asset` (Add `baseCashPerSecond` = 5.0)
  - `RealityTVSyndicate.asset` (Add `baseCashPerSecond` = 20.0)
  - `BrainRotThinkTank.asset` (Add `baseCashPerSecond` = 100.0)
- **$ SHOP (CashShopPanel) Close Button**: Fix clickability, target graphic, RectTransform size, topmost sibling sorting, and correct CloseShop wiring.

# Implementation Steps

## Step 1: Fix $ SHOP (CashShopPanel) Close Button Clickability & Layout
- **Description**: Ensure the Close X button on the CashShopPanel is fully functional, large enough, and intercepts raycasts correctly:
  - Add/ensure a valid **`UnityEngine.UI.Button`** component exists on the `CloseButton` GameObject inside `Canvas/CustomSafeArea/CashShopPanel`.
  - Set its `RectTransform` sizeDelta to at least **80x80** for an easy mobile tap target.
  - Position it inside the visible panel top-right area (e.g., anchor `(1, 1)`, pivot `(1, 1)`, anchoredPosition `(-20, -20)`).
  - Ensure the `UnityEngine.UI.Image` (targetGraphic) component has **`raycastTarget = true`** enabled.
  - Wire its onClick event to trigger **`CashShopUIController.CloseShop()`**.
  - Programmatically set `CloseButton` as the **last sibling** (`transform.SetAsLastSibling()`) in `CashShopUIController` Awake or via the Scene to ensure it is topmost and not blocked by any overlay/companion rows.
  - Verify utilizing `UIBlockDebugger` that clicks on the X are registered by `CloseButton` and not intercepted by parent/adjacent components.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 2: Audit & Adjust Cash/sec Building Economy Values
- **Description**: Open and update building `.asset` files in `Assets/_Game/Buildings/` to introduce meaningful, scaling Cash/sec generation as a player moves from early to late game. No changes to core C# mathematical formulas/scripts.
  - Set `UndergroundEconomy.asset` -> `baseCashPerSecond` = 2.5 (early Cash production).
  - Set `CryptoBroCompound.asset` -> `baseCashPerSecond` = 5.0 (mid-game Cash production).
  - Set `RealityTVSyndicate.asset` -> `baseCashPerSecond` = 20.0 (late-mid game Cash production).
  - Set `BrainRotThinkTank.asset` -> `baseCashPerSecond` = 100.0 (end-game Cash production).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 3: Refactor UpgradeSlotUI to Display Full Production Contributions
- **Description**: Edit `UpgradeSlotUI.cs` in `RefreshState()` to calculate and display both immediate purchase gains and total current production contribution.
  - Do not assume the next production gain is only `baseCashPerSecond`/`baseBrainPowerPerSecond`. Use the actual global multiplier formulas from `CurrencyManager.cs`:
    - BPPS Gain: `baseBrainPowerPerSecond * currency.RebirthMultiplier * currency.ShopAllMultiplier`
    - Cash/sec Gain: `baseCashPerSecond * currency.CashMultiplier * currency.ShopCashMultiplier * currency.ShopAllMultiplier`
  - BPPS Total Contribution: `level * baseBrainPowerPerSecond * currency.RebirthMultiplier * currency.ShopAllMultiplier`
  - Cash/sec Total Contribution: `level * baseCashPerSecond * currency.CashMultiplier * currency.ShopCashMultiplier * currency.ShopAllMultiplier`
  - Render these dynamically inside `descriptionText.text` as beautifully formatted, color-coded strings.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

## Step 4: Refactor RebirthUIController to Show Unlock Progress
- **Description**: Modify `RebirthUIController.cs` so that the `RebirthTriggerButton` is always active in the scene but *disabled* (non-interactable) when locked.
  - Set `rebirthTriggerButton.SetActive(true)` on start and keep it active.
  - Get the `Button` component and set `interactable = shouldBeVisible`.
  - Update the child `TextMeshProUGUI` component dynamically:
    - If unlocked: `"[THE SNOTTING]"`
    - If locked: `$"[SNOTTING: {NumberFormatter.Format(spent)}/{NumberFormatter.Format(pointsSpentUnlockThreshold)} PTS]"` with text auto-sizing enabled so it fits perfectly on the button.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 5: Enhance HUDController to Show Unlock Requirements
- **Description**: Update `HUDController.cs` in `UpdateRestorationProgress()` to display plain language snotting progress:
  - Text format: `"{stageName.ToUpper()} ({percent:F1}% RESTORED)\n<size=18>Spend {NumberFormatter.Format(pointsSpentUnlockThreshold)} Restoration Points to unlock The Snotting (Spent: {NumberFormatter.Format(spent)})</size>"`
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

## Step 6: Update Terminology to "Illumisnotty"
- **Description**: Update player-facing text inside files and assets to use the preferred "Illumisnotty" spelling. Do not rename C# methods, class names, serialized fields, asset filenames, or saved data keys.
  - In `RebirthManager.cs` (`GetIllumisnottiTitle` -> return `"Grand Illumisnotty"` inside title mapping).
  - In `PointsShopSlotUI.cs` -> `"The Illumisnotty haven't been weakened enough yet."`.
  - In `RandomChatterManager.cs` -> update all string literals containing `"The Illumisnotti"` to `"The Illumisnotty"`.
  - In `.asset` files containing "Illumisnotti" inside descriptions/quotes (e.g. `GoldenCardboardCrown.asset`, `TheLiteralLibrary.asset`, `TheGrandSnotting.asset`, etc.), update their YAML text directly.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 7: Update COGS Progression Comments & Documentation
- **Description**: Update header XML documentation in `COGSPortraitController.cs` and `COGSStage.cs` to add the design/progression comments:
  - Note: "COGS progresses from a corrupted/hostile Stage 1 to a godlike/supportive Stage 6. Early stages feature cynical/antagonistic comments; later stages reflect clarity and a supportive attitude."
  - **Constraints**: Do not mention CRT, patched CRT, flatscreen, hologram, or old hardware-stage names in the comment. Do not rename assets or wire art yet.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

# Verification & Testing
1. **Compilation Check**: Confirm the project compiles with no errors.
2. **Shop Readability & Interaction Check**: Open the BP Shop in Play Mode. Confirm each row displays:
   - Owned level.
   - Cost of next level.
   - BP/sec and Cash/sec gained on next level (scaled by active global multipliers).
   - Total current contribution (scaled by active global multipliers).
3. **Cash Shop Close Click Test**: Open $ SHOP in Play Mode. Confirm clicking the Close X closes the panel immediately, and `UIBlockDebugger` logs that `CloseButton` received the click.
4. **Rebirth Trigger Check**: Start a new game (spent points = 0). Confirm `RebirthTriggerButton` is visible at the bottom right, is non-interactable, and shows the current spending progress. Spend points via the "Restore" button and verify the progress increases.
5. **Terminology Verification**: Search user-facing UI text in play mode to confirm "Illumisnotty" is spelling-consistent.
6. **No Automated Play Mode integration tests or test runner scripts will be created or run.** All testing will be manual inside the Unity Editor.
