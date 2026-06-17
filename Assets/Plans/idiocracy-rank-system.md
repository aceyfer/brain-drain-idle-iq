# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: An idle clicker game where players tap to earn Brains and restore decaying IQ, escalating in level and difficulty over time.
- Players: Single player
- Tone / Art Direction: Neon, high-contrast style
- Target Platform: iOS
- Render Pipeline: UniversalRP

# Game Mechanics
## Core Gameplay Loop
- Players tap to earn Brains. Accumulating Brains increases cumulative earnings, unlocking new Ranks (the "Idiocracy rank system").
- These ranks correspond to specific milestone thresholds of cumulative Brains, advancing from "Unregistered Outcast" all the way to "Mr. President".

# UI
- **Capacity Text** (`BrainsText` GameObject): Displays `% ABSORBED` based on current brains/cumulative brains relative to the ultimate milestone (500,000).
- **IQ Text** (`IQText` GameObject): Displays current player IQ status.
- **Rank Text** (`LevelText` GameObject): Displays the current rank name corresponding to the highest unlocked threshold.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- `_Systems` GameObject (hosts `GameManager`, `CurrencyManager`, `UpgradeManager`, and `IQDecaySystem`).
- `Canvas` GameObject (hosts `HUDController` component and UI Text elements).
- Scriptable Types & structs: `RankDefinition` inside `GameManager.cs`.

# Implementation Steps

### Step 1: Update GameManager.cs with RankDefinition
- **Description**: Add the `RankDefinition` struct and array serialization to `GameManager.cs`. Implement `GetRankName(double brains)` to evaluate which rank name matches the player's cumulative or current brains balance based on configured thresholds.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Update HUDController.cs with Capacity/Rank Text Mapping
- **Description**: Rename or expose serializable text fields in `HUDController.cs` as `capacityText`, `iqText`, and `rankText`. Update the text formatting logic:
  - `capacityText` formatted to display `% ABSORBED` (e.g. `(brains / 500000.0) * 100.0` formatted to % percentage).
  - `rankText` formatted to display the current rank name fetched from `GameManager.Instance.GetRankName(...)`.
  - Ensure events are correctly subscribed so values update dynamically on changes.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Configure Scene Hierarchies and Component Serialized References
- **Description**: 
  - Ensure `GameManager`, `CurrencyManager`, and `UpgradeManager` are attached to the `_Systems` GameObject.
  - Set the `RankDefinition` array on `GameManager` with the 5 tiers:
    - Element 0: "Unregistered Outcast", Threshold = 0
    - Element 1: "Inmate #418293", Threshold = 500
    - Element 2: "IQ Test Champion", Threshold = 5000
    - Element 3: "Secretary of Interior", Threshold = 50000
    - Element 4: "Mr. President", Threshold = 500000
  - Ensure `HUDController` is attached to `Canvas`.
  - Wire up `capacityText` $\rightarrow$ `BrainsText` GameObject.
  - Wire up `iqText` $\rightarrow$ `IQText` GameObject.
  - Wire up `rankText` $\rightarrow$ `LevelText` GameObject.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 2
- **Parallelizable**: No

### Step 4: Configure Script Execution Order
- **Description**: Apply editor-only `MonoImporter` scripts to set execution orders:
  - `CurrencyManager` to `-200`
  - `GameManager` to `-100`
  - `HUDController` to `0`
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

# Verification & Testing
- In Play Mode, verify the UI displays "% ABSORBED" and the dynamic rank status perfectly.
- Verify transitioning through thresholds updates the HUD text dynamically with zero delay.
- Check script execution orders in the Project Settings inspector to verify correct priorities.
