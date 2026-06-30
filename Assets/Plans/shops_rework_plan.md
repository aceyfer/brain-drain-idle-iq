# Project Overview
- Game Title: Brain Drain
- High-Level Concept: An addictive incremental idle clicker game where players harvest Brain Power (BP), convert it to Cash ($), and eventually points to restore the world and dismantle the Illumisnotti faction.
- Players: Single player
- Target Platform: iOS / PC
- Render Pipeline: URP (Universal RP)
- Screen Orientation: Portrait / Adaptive

# Game Mechanics
## Core Gameplay Loop
Players tap the brain to absorb Brain Power (BP). BP can be spent in the **BP Shop** (buildings and upgrades) to generate passive BP income. BP can be converted to in-game cash (**$**), which is used in the **$ Shop** to purchase companions and permanent passive income multipliers. Cash can eventually be converted to **Points** (unlocked via Rebirth/Snotting) to restore the world stage.

## Controls and Input Methods
Standard touch/click inputs on buttons, lists, and the main tap area.

# UI
This layout features the following adjustments:
1. **BP Shop**: Labeled cleanly as "BP SHOP" (was "SHOP"), displaying passive structures with calming non-flashy colors.
2. **$ Shop**: Labeled cleanly as "$ SHOP" (was "CASH SHOP"), displaying items purchasable for in-game Cash ($) with significantly enlarged text.
3. **Premium Shop**: A new shop panel labeled "PREMIUM SHOP" that sells premium microtransactions using Neurons (real-money proxy). It includes the **Bad Words (Profanity) Pack** as its flagship item.
4. **Convert Dialog Popup**: Fully interactive, clear, and readable exchange modal with large text.
5. **Very Large Fonts**: All text sizes inside the BP Shop slots, $ Shop slots, Points Shop slots, Premium Shop slots, and the Convert Dialog popup will be scaled up to at least **28–36 pt** for readability on high-DPI and regular monitors.

# Key Asset & Context
- `Assets/_Game/Scripts/UI/HUDController.cs`: Added/extended connections for the new `$ SHOP` label, Points Shop lock state, `PremiumShopButton`, and `PremiumShopPanel`.
- `Assets/_Game/Scripts/UI/ConvertUIController.cs`: Holds conversion logic. Text sizes enlarged.
- `Assets/_Game/Scripts/UI/UpgradeSlotUI.cs`: BP Shop slots. Muted colors applied, font sizes enlarged.
- `Assets/_Game/Scripts/UI/CashShopSlotUI.cs`: $ Shop slots. Font sizes enlarged.
- `Assets/_Game/Scripts/UI/PointsShopSlotUI.cs`: Points Shop slots. Font sizes enlarged.
- `Assets/_Game/Scripts/UI/PremiumShopUIController.cs` (New): Manages the Premium Shop panel, populated with premium slots.
- `Assets/_Game/Scripts/UI/PremiumShopSlotUI.cs` (New): Manages a single premium item row (like the Bad Words Pack).
- `Assets/_Game/Scripts/Systems/PremiumShopManager.cs` (New): Handles premium item catalog, purchases using Neurons, and triggers effects (such as unlocking profanity chatters).

# Implementation Steps
### Step 1: Create Premium Shop Systems
- **Description**: Implement `PremiumShopManager.cs` to hold catalog of premium items (including the Bad Words pack) bought with Neurons, and `PremiumShopUIController.cs`/`PremiumShopSlotUI.cs` to manage the premium store visual hierarchy.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Implement UI Font Size Polish
- **Description**: Modify slot scripts (`UpgradeSlotUI.cs`, `CashShopSlotUI.cs`, `PointsShopSlotUI.cs`) and Convert script (`ConvertUIController.cs`) to dynamically or statically increase TextMeshPro font sizes. Update slot prefabs (`CashShopSlotUI.prefab`, `PointsShopSlotUI.prefab`) and scene instances to have large font layouts (30-40pt titles, 24-28pt descriptions, 28-34pt buttons).
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Scene Construction & Wiring
- **Description**: Run a C# editor tool/command script to:
  - Rename CashShopButton to `$ SHOP` and update its label.
  - Create a new `PremiumShopButton` inside the `EconomyBar` layout, labeled `PREMIUM SHOP`.
  - Create a `PremiumShopPanel` under `Canvas/CustomSafeArea` styled like the other shops but displaying the Bad Words Dialogue Pack and other premium items.
  - Wire all click events, HUD controller references, and serialize appropriate fields.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

# Verification & Testing
1. **Font Size Check**: Open all panels (BP Shop, $ Shop, Points Shop, Premium Shop, Convert Panel) and verify that all labels are extremely large, clear, and perfectly readable.
2. **$ Shop Functionality**: Open $ Shop and verify items cost Cash ($).
3. **Premium Shop Functionality**: Ensure the "Bad Words" item is buyable with Neurons. Verify buying it unlocks Tier 3 profanity lines in `RandomChatterManager` immediately.
4. **Points Shop Gating**: Verify Points are locked/grayed out until Rebirth is activated.
5. **Convert Panel**: Confirm BP ➔ $ and $ ➔ Points function beautifully.
