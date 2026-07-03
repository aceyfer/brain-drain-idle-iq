# Project Overview
- Game Title: Brain Drain
- High-Level Concept: Mobile portrait idle/clicker focused on "brain rot" progression and satirical corporate/social media themes.
- Players: Single player
- Inspiration / Reference Games: Adventure Capitalist, Cookie Clicker
- Tone / Art Direction: Satirical, neon, chaotic
- Target Platform: iOS (Mobile Portrait)
- Screen Orientation / Resolution: Portrait 1080x1920
- Render Pipeline: URP

# Game Mechanics
## Core Gameplay Loop
- Tapping to generate Brain Power (BP).
- Purchasing buildings to generate idle BP and Cash.
- Converting Cash to Points for World Restoration.
- Rebirthing (The Snotting) for permanent multipliers.

## Controls and Input Methods
- Touch-based tapping and menu navigation.
- Tab-based shop system.

# UI
## Shop Overhaul (Phased)
- **Phase 1**: Consolidate HUD buttons into a single "SHOP" button.
- **Phase 2**: Create a unified shop panel with "BP", "$", and "Points" tabs.
- **Phase 3**: Separate "Premium Shop" for IAP/Neurons items.

# Key Asset & Context
- `BuildingData.cs`: ScriptableObject for building stats.
- `UpgradeManager.cs`: Manages building levels.
- `CurrencyManager.cs`: Manages BP, Cash, Points.
- `ShopUIController.cs`: Existing shop UI.
- `ConsolidateShopButton.cs`: Editor tool for HUD cleanup.

# Implementation Steps
## Phase 1: HUD Consolidation
- **Description**: Use `ConsolidateShopButton.cs` to relabel the BP Shop button to "SHOP" and hide the old Cash Shop button.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

## Phase 2: Unified Shop Panel
- **Description**: Update the existing Shop UI to include a tab switcher for BP, Cash, and Points.
- **Assigned role**: developer
- **Dependencies**: Phase 1
- **Parallelizable**: No

## Phase 3: Premium Shop
- **Description**: Create a separate UI panel or distinct entry point for Premium (Neurons) purchases.
- **Assigned role**: developer
- **Dependencies**: Phase 2
- **Parallelizable**: No

# Verification & Testing
- **Manual Verification**: Run `BrainDrain > Phase 1: Consolidate Shop Button` and verify HUD changes.
- **Play Mode Test**: Ensure the "SHOP" button opens the correct panel and currencies are displayed/spendable correctly.
- **Save/Load Test**: Ensure shop ownership persists after UI changes.
