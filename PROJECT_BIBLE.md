# Brain Drain: Idle IQ — Project Bible

*Generated 2026-06-20. Reflects the actual code/asset state in the repo, not original design docs — treat this as the source of truth over anything older.*

## 1. What the game is

A satirical idle-clicker (AdVenture Capitalist-style pure positive accumulation) by **AcEclipse Games**. The player is framed as the evil mogul running the operation: tap to harvest **Brain Power** from the population, spend it on buildings and infrastructure, watch the player's own **PlayerIQ** climb forever, and periodically **Rebirth** for permanent bonuses. A narrator (**COGS**) comments on milestones in an increasingly unhinged tone as Rebirth count climbs, and a 12-chapter narrative arc unlocks alongside progression.

- **Engine:** Unity `6000.4.8f1`, Universal Render Pipeline (2D), target platform iOS (portrait).
- **Repo:** `https://github.com/aceyfer/brain-drain-idle-iq`, default branch `main`. (An older `master` branch exists locally with the pre-push history; not pushed.)
- No `.asmdef` files — everything compiles into `Assembly-CSharp`. No automated tests despite the test-framework package being present. No CLI build/lint pipeline — this is an Editor-driven project.

## 2. Economy / currency flow

Three connected tiers, each layered additively on top of the last (none replace each other):

```
TAP ──▶ Brain Power (primary) ──▶ buys 7 Buildings + Infrastructure
                                        │
                                        ├──▶ Buildings produce idle Brain Power/sec (BPPS)
                                        ├──▶ Buildings + Infrastructure raise PlayerIQ (+1/purchase, or 1:1 on infra spend)
                                        └──▶ Underground Economy building bridges into:

Cash (secondary) ──▶ produced by Underground Economy only, paid out per-tick
        │
        └──▶ ConvertCashToPoints (on-demand or auto every 10s) ──▶ Points (tertiary)
```

- **PlayerIQ**: starts at **100**, pure positive accumulation, no decay (an earlier decay-based design — `IQDecaySystem` — was fully and deliberately removed; do not resurrect it without a real design conversation). Crosses a milestone every 1000 points (celebration flash + dialogue trigger).
- **Rebirth**: resets current Brain Power + building levels to zero; grants a permanent, stacking **+5% Brain Power multiplier, +10% Cash multiplier, +5% Points conversion rate** per rebirth. Cash/Points balances themselves are untouched by Rebirth.

### The 7 buildings (unlock gate = cumulative Brain Power ever earned, not each building's base cost)
| Building | Unlocks at | Notes |
|---|---|---|
| The Literal Library | 0 | |
| Doomscroll Engine | 0 | |
| Podcaster Soundboard | 10,000 | |
| Underground Economy | 500 | Only Cash producer (0 BPPS / 0.5 CPS) — the bridge into the Cash tier |
| Crypto Bro Compound | 65,000 | |
| Reality TV Syndicate | 185,000 | |
| Brain Rot Think Tank | 725,000 | |

## 3. Architecture

Code lives under `Assets/_Game/Scripts`, split `BrainDrain.Core` (simulation/state) and `BrainDrain.Systems` / `BrainDrain.UI` (everything else). No dependency injection — everything wires through Unity Inspector references with `FindAnyObjectByType`/self-bootstrapping singleton fallbacks. One global tick (`GameManager.OnSecondTick`, 1/sec) drives all per-second logic; nothing else runs its own timer for core economy.

### Core simulation
- **`GameManager`** — central singleton hub, owns the tick loop, the Idiocracy Rank ladder (cosmetic title + diorama backdrop vs. cumulative Brain Power), and three save-trigger hooks (pause/focus-loss/quit) plus a 60s autosave counter.
- **`CurrencyManager`** — Brain Power, Cash, Points, all multipliers/conversion rates, Rebirth execution, save/load.
- **`PlayerIQManager`** — the IQ stat described above.
- **`UpgradeManager`** — building ownership, purchase validation, cost scaling (`baseCost * costMultiplier^level`), registers BPPS/CPS once per purchase (no per-frame recompute).
- **`PlayerTapHandler`** — sole input entry point; awards Brain Power per tap, notifies `PlayerCharacterController` for visual feedback, spawns splat particles at the tap location.
- **`DioramaManager`** — swaps active backdrop GameObject by rank.

### Systems layer
- **`RebirthManager`** — Rebirth trigger + bonuses (see above).
- **`RandomEventManager`** / **`BrainRotEventData`** — 8 satirical chaos events, fires every 90–180s, apply Brain Power +/- and PlayerIQ +/- on accept.
- **`SaveManager`** — JSON to `Application.persistentDataPath/braindrain_save.json`; persists Brain Power, Cash, Points, PlayerIQ, Rebirth count, building levels, all multipliers, autoConvert toggle. BPPS/CPS are deliberately *not* saved directly — re-derived from building levels on load to avoid drift.
- **`DialogueManager`** / **`NarratorLine`** — 30 seeded lines (5 RebirthCount tiers × 6 trigger types), narrates 5 trigger types, never repeats the immediately-previous line, queues up to 2.
- **`COGSPortraitController`** / **`COGSStage`** — narrator portrait visual progression, 6 stages keyed on RebirthCount (0/1/3/6/11/20). No art yet.
- **`ChapterManager`** / **`ChapterData`** — 12-chapter sequential narrative arc, checked every 10s + on currency/rebirth events.
- **`PlayerCharacterController`** / **`CharacterAppearanceStage`** — on-screen Player Character behavioral state machine: `Idle` → `Bored` (after 20s untouched) / `Tapping` (every tap) / `Excited` (building purchase, rebirth, IQ milestone), each a hand-rolled coroutine loop, no Animator Controller asset. Has its own independent appearance progression by RebirthCount — deliberately separate from the COGS portrait's. No art yet.
- **`WardrobeManager`** / **`OutfitData`** *(new)* — Outfit/Wardrobe system, the last item from the original build queue. Outfits auto-unlock by RebirthCount (same model as everything else — unlock status is derived live, never separately tracked, since RebirthCount only increases), but unlike pure-progression systems, the player actively **chooses** which unlocked outfit to wear (`TryEquipOutfit`). Purely cosmetic, no gameplay effect — confirmed. Defaults to the lowest unlocked outfit until the player makes a real choice, so the character is never left bare. 6 outfits exist (Cheap Knockoff Suit → Ascended Silk Kimono, same RebirthCount tiers as the COGS portrait: 0/1/3/6/11/20), no art yet. Has its own wardrobe-selection UI (`WardrobeUIController`/`WardrobeSlotUI`), separate from the HUD.
- **`WorldRestorationManager`** / **`WorldRestorationStage`** — the dystopia-to-utopia visual transformation. Spending Points (`TrySpendPointsOnRestoration`) permanently raises `CumulativePointsSpentOnRestoration`; stages resolve the same threshold-walk way as rank/COGS, but the visual swap cross-fades sibling backdrop GameObjects exactly like `DioramaManager` does for rank. 6 stages exist (Toxic Wasteland → Utopia Achieved, thresholds 0/250/1,000/5,000/25,000/100,000 Points spent). Exposes `RestorationPercent` (0–100), now wired into the previously-dead `ChapterUnlockConditionType.WorldRestorationPercent` condition. HUD has a progress readout + a "RESTORE" button (spends all current Points at once, same pattern as the CONVERT button). Additive to `PlayerIQ`, not a replacement — confirmed intentional.
- **`AnimationController`** — coroutine-based singleton (no DOTween/LeanTween anywhere in the project) backing all of the above: tap squash, idle breathing, bored fidget, excited bounce, splat particles, affordable-slot pulse, popup-spawn shake, IQ-milestone celebration flash.

### UI layer
- **`HUDController`** / **`ShopUIController`** / **`UpgradeSlotUI`** / **`RebirthUIController`** / **`RandomEventUIController`** / **`DialogueDisplayUI`** — purely reactive, subscribe to Core/Systems events in Start/OnGameInitialized, unsubscribe in OnDestroy.
- **`SafeAreaManager`** — iOS notch/Dynamic Island inset via a generated `SafeArea` child RectTransform.
- **`NumberFormatter`** — shared idle-game number formatting (`1.25M`, etc.).

## 4. Known gaps / inconsistencies (not blocking, just open)

- **Dialogue presentation vs. content mismatch**: `DialogueDisplayUI`'s font-degradation effect is still keyed off `PlayerIQ` (always climbing, so it reads "clean" forever), while which *line* plays is keyed off `RebirthCount` (the actual degrading-tone driver). Not reconciled.
- **`BrainRotEventData.multiplierSpike`** — exists, never read by anything.
- **`ChapterUnlockConditionType.PointsSpent`** — still aliased to current Points balance, not lifetime spend. Now that `WorldRestorationManager` tracks real cumulative Points *spent* (`CumulativePointsSpentOnRestoration`), this could be pointed at that instead of a separate tracker, if a chapter actually needs "lifetime Points spent" rather than "current balance" — not changed yet since no chapter currently uses this condition type in a way that requires it.
- **No art** for COGS portrait stages, the Player Character's appearance stages, the 6 World Restoration backdrop stages, or the 6 outfits — all four data types exist and are ready to receive sprites/GameObjects.
- **Pending manual Editor work** (not code — scene/Inspector wiring, handled outside this workflow): asset-pool lists (`RandomEventManager.potentialEvents`, `DialogueManager.narratorLines`, `COGSPortraitController.stages`, `ChapterManager.chapters`) need the newest assets dragged in; `SafeAreaManager` needs attaching to the Canvas and existing HUD content re-parented under it; the new Player Character GameObject needs creating and wiring into `PlayerCharacterController`/`PlayerTapHandler`.

## 5. What's next

The original build queue (Player Character → Outfit/Wardrobe → World Restoration) is now fully built end-to-end in code — backend, save persistence, and UI hooks for all three. Nothing is queued next; the only thing blocking any of it from looking/feeling finished in Play Mode is art (COGS portraits, Player Character body + 6 outfits, 6 World Restoration backdrops — none exist yet) and the Inspector wiring to drop that art into the relevant manager fields.
