# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Brain Drain: Idle IQ — a satirical idle-clicker Unity game. Players tap to earn "Brains" (soft currency), which decays the world's IQ over time; cumulative Brains earned drives both an "Idiocracy Rank" (cosmetic title + diorama backdrop) and a decay-escalation "level". Brains are spent on buildings that produce passive income, most of which *accelerate* IQ decay (the satire: progress makes things dumber).

- Engine: Unity `6000.4.8f1`, Universal Render Pipeline (2D), target platform iOS (portrait).
- No `.asmdef` files — all scripts compile into the default `Assembly-CSharp` assembly.
- No automated tests currently exist, despite `com.unity.test-framework` being a package dependency.

## Working with this repo

This is a Unity project opened/built through the Unity Editor, not a CLI-driven codebase — there is no command-line build/lint/test workflow set up. Changes to scene/prefab/asset wiring (`.unity`, `.prefab`, `.asset` YAML files) generally need to be made or verified inside the Unity Editor; hand-editing the YAML is error-prone for anything beyond tuning serialized numeric fields (see "Editing ScriptableObject data" below). C# script changes under `Assets/_Game/Scripts` can be edited directly.

## Architecture

All gameplay code lives under `Assets/_Game/Scripts`, split into `BrainDrain.Core` (simulation/state) and `BrainDrain.UI` (presentation). Everything is wired together through Unity Inspector references and runtime `FindAnyObjectByType` fallbacks rather than dependency injection — when adding a new system, follow the existing pattern of a serialized field that falls back to `GameManager.Instance` lookups.

**`GameManager`** (`Core/GameManager.cs`) is the central hub: a thread-safe singleton (`GameManager.Instance`) created with `[DefaultExecutionOrder(-100)]` so it initializes before dependents. It owns:
- The single global simulation tick: `InvokeRepeating` fires `OnSecondTick` once per second. Every system that needs per-second logic (currency idle income, IQ decay) subscribes to this instead of running its own timer.
- `OnGameInitialized`, fired once from `Start()` after the tick loop is running — other systems use this (instead of their own `Start()`) to do initial UI/state sync, since it guarantees `GameManager` and its core references are ready.
- `RankDefinition[]` — ordered (ascending threshold) array of `{rankName, threshold}` mapped against cumulative Brains via `GetRankName(double)`. The same threshold list semantically drives both the HUD rank text and `DioramaManager`'s active-diorama index, but each consumer recomputes the index independently by walking the same array — there's no shared "current rank index" state.

**`CurrencyManager`** (`Core/CurrencyManager.cs`) tracks `Brains` (spendable, `double`), `CumulativeBrains` (lifetime earned, only ever increases, drives ranks/level/dioramas), and `Neurons` (int premium currency, currently unused elsewhere). `AddIdleBPS` accumulates passive income from buildings; the actual payout happens once per second on `GameManager.OnSecondTick`, separate from `UpgradeManager`'s own per-frame production loop (see below — there are two different production pathways).

**`IQDecaySystem`** (`Core/IQDecaySystem.cs`) tracks global `CurrentIQ` (0–100, starts at 100) and `CurrentLevel` (derived from cumulative Brains via `BrainsPerLevelUnit`/`LevelProgressionExponent` — an exponential curve, not linear). Decay rate escalates in tiers: every `LevelsPerTier` (10) levels multiplies the base decay rate by `tierEscalationMultiplier` (2.5x per tier), so decay grows much faster than linearly with level. External systems register named modifiers via `AddModifier(source, amount, multiplicative)` — additive modifiers subtract flat decay, multiplicative modifiers (e.g. `DecayModifierSources.University`, -15%) scale the result after additive reduction. `RestoreIQ(amount)` is the only way IQ increases (from taps and from buildings with positive `iqRecoveryPerSecond`).

**`UpgradeManager`** (`Core/UpgradeManager.cs`) owns building ownership (`Dictionary<string,int>` levels keyed by `buildingName`, not by `BuildingData` reference) and purchase validation (`unlockPlayerLevel` gate, exponential cost via `baseCost * costMultiplier^level`). Note its `Update()` loop independently recomputes total BPS/IQ-recovery from owned levels every frame and applies it via `Time.deltaTime` — this is a *second*, separate production pathway from `CurrencyManager.AddIdleBPS`/`OnSecondTick`; nothing currently calls `AddIdleBPS` for buildings. `OnBuildingsChanged` fires after a successful purchase for UI refresh.
  - Quirk to be aware of: `BuildingData.iqRecoveryPerSecond` can be negative (several buildings intentionally drain IQ recovery as the joke). `UpgradeManager.Update()` only calls `iqDecaySystem.RestoreIQ(...)` when the *summed* `iqRecoveryPerSecond > 0d`, and `RestoreIQ` itself no-ops on non-positive input — so negative-recovery buildings currently have no mechanical effect on decay; they read as flavor/cost-benefit framing only unless this is intentionally wired up further.

**`BuildingData`** (`Core/BuildingData.cs`) is a `ScriptableObject` (`CreateAssetMenu` under `BrainDrain/Building Data`) holding the authoring data for one building type. Instances live as `.asset` files in `Assets/_Game/Buildings/`. `UpgradeManager.buildingTemplates` is a manually-populated list of these assets in the Inspector — adding a new building means creating a new asset here *and* adding it to that list.

**`DioramaManager`** (`Core/DioramaManager.cs`) swaps which of N sibling `GameObject`s is active based on cumulative Brains vs. `GameManager.RankDefinitions`, mirroring the same threshold-walk logic as `GetRankName`.

**`PlayerTapHandler`** (`Core/PlayerTapHandler.cs`) is the only player input entry point (`OnTap()`, wired to a full-screen invisible Button's `OnClick`). Each tap adds `baseTapBrains * tapMultiplier` Brains and restores `iqRestorePerTap` IQ.

**UI layer** (`BrainDrain.UI` namespace) is purely reactive: `HUDController` and `ShopUIController` subscribe to Core events (`OnBrainsChanged`, `OnCumulativeBrainsChanged`, `OnIQChanged`, `OnLevelChanged`, `OnBuildingsChanged`) in their own `Start`/`OnGameInitialized` handlers and unsubscribe in `OnDestroy` — always follow this subscribe-in-init/unsubscribe-in-destroy symmetry when adding new UI. `ShopUIController` builds one `UpgradeSlotUI` instance per `BuildingData` template at runtime (no pre-authored rows); `UpgradeSlotUI` renders three visual states (locked / affordable / too-expensive) using fixed neon hex colors and calls back into `UpgradeManager.TryBuyBuilding` on click. `NumberFormatter.Format(double)` is the shared idle-game number formatter (e.g. `1.25M`) — use it for any new currency display rather than formatting doubles directly.

## Design plans

`Assets/Plans/*.md` are point-in-time implementation plans written before features were built (canvas/HUD setup, diorama system, rank system, shop UI). They describe intended scene hierarchy and step-by-step Editor actions, but the C# code has since evolved past some of their specifics (e.g. `HUDController` field names) — treat them as historical design context and rough scene-hierarchy reference, not as the current source of truth; the scripts and `.unity`/`.asset` files are authoritative.

## Editing ScriptableObject data

Building balance values (`baseCost`, `costMultiplier`, `baseBrainsPerSecond`, `iqRecoveryPerSecond`, `unlockPlayerLevel`) live in the `.asset` YAML files under `Assets/_Game/Buildings/` and can be tuned by editing those fields directly. `RankDefinition[]` thresholds live on the `GameManager` component instance in `Assets/Scenes/SampleScene.unity` (not in a separate asset), so changing rank thresholds means editing the scene file's serialized array or doing it in the Editor Inspector.
