# CODEX FINDINGS — Per-stage shop item name/description feasibility audit

Read-only diagnosis performed **2026-08-11** against the repo at `HEAD` (`07c873a`). No code changed, nothing staged, nothing committed. This report replaces the prior Plankton audit content — per this file's own established convention (untracked, one report per investigation, not an accumulating log).

## Executive answer

**No per-stage evolution of shop item names or descriptions exists today.** `BuildingData.buildingName`/`description` and `CashShopItemData.displayName`/`description` are single static strings, authored once per `.asset` file, with no stage/level-band/restoration-threshold-varying field anywhere in either schema.

**Adding it is not purely a display change.** `BuildingData.buildingName` is simultaneously (a) the display string, (b) the save-file identity key, (c) the in-memory ownership dictionary key, and (d) the dialogue-trigger match key — four roles collapsed into one field, with zero separation between "what the player reads" and "what the game uses to remember what they own." Every other data type with a comparable identity need in this codebase (`CashShopItemData.itemId`, `OutfitData.outfitId`) already decoupled identity from display text; `BuildingData` never did. Making the display string stage-dependent without first introducing a separate stable ID would silently break save-load matching, ownership lookups, and the 7 building-specific narrator lines. Full detail in §6.

---

## 1. BuildingData — full field list

`Assets/_Game/Scripts/Core/BuildingData.cs:1-37`. Complete field list, no omissions:

| Field | Type | Header |
|---|---|---|
| `buildingName` | `string` | Identity |
| `description` | `string` (`[TextArea(2,4)]`) | Identity |
| `unlockCumulativeBrainPower` | `double` | Progression |
| `costType` | `CostType` enum (`BrainPower`/`Cash`) | Progression |
| `baseCost` | `double` | Progression |
| `costMultiplier` | `double` | Progression |
| `baseBrainPowerPerSecond` | `double` | Production |
| `baseCashPerSecond` | `double` | Production |
| `tapBrainPowerPerLevel` | `double` | (no header) |

**`buildingName` and `description` are single static strings, full stop.** No array, no dictionary keyed by stage/level, no reference to `WorldRestorationStage`, no conditional text field of any kind. There is nothing in this schema to "read the current stage from" — the type doesn't carry stage-awareness at all.

## 2. Full building roster (16 buildings, actual data, not summarized)

Source: `Assets/_Game/Buildings/*.asset`, fields `buildingName`, `description`, `costType` (0 = BrainPower, 1 = Cash), `unlockCumulativeBrainPower`.

| Asset file | `buildingName` | `description` | `costType` | `unlockCumulativeBrainPower` |
|---|---|---|---|---|
| `JumperCables.asset` | Apex Brain Greens | A scoop of pond-colored powder, 47 ingredients nobody regulated. | BrainPower | 0 |
| `TheLiteralLibrary.asset` | The Literal Library | The Illumisnotty burned most of these. You're rebuilding. | BrainPower | 0 |
| `DefrostDrip.asset` | StupAid H2O | Everything your brain actually craves, in a bottle. Home-grown crops got banned so long ago even the elites forgot food was ever free. | BrainPower | 60 |
| `DoomscrollEngine.asset` | Loose Change Collective | Nobody misses what nobody notices. Multiply that by a hundred million wallets. | Cash | 150 |
| `CranialMicrowave.asset` | Tinfoil Headband | Looks like sportswear. Lined with tinfoil that jams the signals trying to hack your third eye. Block the leeches and the bandwidth is yours again. | BrainPower | 300 |
| `UndergroundEconomy.asset` | Snott Market Exchange | The whole exchange got bought out centuries ago and never sold back. This is where the war to reclaim it trades. | BrainPower | 500 |
| `LemonadeGriftStand.asset` | Charity Shell | Registered as a nonprofit for the restoration of something vague. Which -- technically, eventually -- it is. | Cash | 800 |
| `DoomscrollBillboard.asset` | Doomscroll Billboard | It scrolls the truth instead of the feed. The more it spreads, the madder the controllers get. | Cash | 2,000 |
| `SynapseSpaceHeater.asset` | Hyperbolic Brain Chamber | An hour inside is a year of thinking outside. Your mind trains in dilated dream-time and comes back sharper — assuming it comes back. | BrainPower | 4,000 |
| `IQOverclockChip.asset` | Pineal Overclock | Decalcify and overclock the third eye you were born with. No implant, no chip — just the gland they hoped you'd forget. Real IQ bump; mild forehead smoke is normal. | BrainPower | 12,000 |
| `HOAProtectionRacket.asset` | The Laundromat | Dirty money in, clean water out. Nobody upstairs has audited it because nobody upstairs can read the ledger. | Cash | 18,000 |
| `PodcasterSoundboard.asset` | Podcaster Soundboard | Three guys in a garage explain geopolitics for four hours using only vibes. Somehow, people are taking notes. | Cash | 25,000 |
| `CryoSludgeEspresso.asset` | Cryo Plunge Tank | Three minutes of screaming cold, then hours of clarity you didn't earn. The recovery-bros were right about exactly one thing. | BrainPower | 40,000 |
| `CryptoBroCompound.asset` | Crypto-Bro Compound | A walled compound of vape smoke and group chats screaming "WAGMI." | Cash | 110,000 |
| `RealityTVSyndicate.asset` | The Great Reversal | Every dollar the Illumisnotty ever took, routed backwards through their own pipes. They built the plumbing. They just never imagined the flow going this direction. | Cash | 185,000 |
| `BrainRotThinkTank.asset` | Brain-Rot Think Tank | A glass tower of PhDs paid handsomely to explain why the dumbest possible idea is, actually, very smart. The think tank that ends all thinking. | BrainPower | 725,000 |

Note the **asset filename and the in-game `buildingName` frequently don't match** (`JumperCables.asset` → "Apex Brain Greens", `RealityTVSyndicate.asset` → "The Great Reversal", `DoomscrollEngine.asset` → "Loose Change Collective", etc.) — this is pre-existing, historical drift from earlier renames, not something introduced by this audit. Flagging since it's directly relevant to "is the name the key" — the filename is not the key, `buildingName` (the C# field, not the `.asset` filename) is.

## 3. WorldRestorationManager — stages, thresholds, and reaction surface

`Assets/_Game/Scripts/Systems/WorldRestorationManager.cs:1-329`.

**6 stages** (`WorldRestorationStage.cs:1-19`, assets under `Assets/_Game/Restoration/`):

| `stageIndex` | `stageName` (actual asset field, not the filename) | `pointsRequired` |
|---|---|---|
| 0 | Cryo Chamber | 0 |
| 1 | Smog-Choked Sprawl | 20,000 |
| 2 | Patchwork Recovery Zone | 2,132,885 |
| 3 | Green Shoots Initiative | 5,658,229 |
| 4 | Renewed Skyline | 20,764,149 |
| 5 | Utopia Achieved | 250,000,000 |

Note: stage 0's actual `stageName` field value is **"Cryo Chamber,"** not "Toxic Wasteland" as the asset filename (`WorldRestorationStage_0_ToxicWasteland.asset`) and prior doc narrative suggest — filename and authored field have drifted apart here too, same pattern as building assets.

**Not polled — genuinely event-driven.** Two C# events (`WorldRestorationManager.cs:92,95`):
- `OnRestorationProgressChanged` (`Action<double>`) — fires on every progress increase (`TrySpendPointsOnRestoration`, `LoadState`, `ResetProgress`), not just stage crossings.
- `OnRestorationStageChanged` (`Action<WorldRestorationStage>`) — fires only when the *resolved* stage actually changes (`ApplyStageForCumulativePoints`, `WorldRestorationManager.cs:257-263`).

**Current subscribers to `OnRestorationStageChanged`** (i.e., things that already react to a stage crossing, beyond the bar visual):
- `GameManager.cs:271-283` — `HandleRestorationStageChangedForRank`
- `UI/BackgroundStageView.cs:61-73` — the actual background-art stage swap (separate from `WorldRestorationManager`'s own internal `SpriteRenderer` alpha-fade)
- `UI/HUDController.cs:250-253` — two independent handlers, `HandleStageChangedForRank` and `HandleRestorationMilestone`
- `Systems/DialogueManager.cs:259-260` — the `RestorationStageChange` narrator trigger

**Current subscribers to `OnRestorationProgressChanged`** (finer-grained, fires on every spend):
- `Systems/BackgroundPedestrianManager.cs:132-133`
- `Systems/DialogueManager.cs:257-258`
- `Systems/FTUEManager.cs:282-283`
- `UI/HUDController.cs:248-249` — `UpdateRestorationProgressText` (the bar visual)
- `UI/RebirthUIController.cs:84-85` — re-evaluates Snotting-button visibility

Neither event currently has a subscriber inside `ShopUIController.cs`/`UpgradeSlotUI.cs`/`CashShopSlotUI.cs` — see §6.

## 4. ClassificationTier.cs — tier input, current state

`Assets/_Game/Scripts/Core/ClassificationTier.cs:1-33`. Four tiers, purely numeric thresholds against a `double` parameter (`GetLabel(double unlockValue)`):

```
< 2,000            "??? CLASSIFIED ???"
2,000-19,999       "??? SECRET ???"
20,000-199,999     "??? TOP SECRET ???"
>= 200,000         "??? FORBIDDEN KNOWLEDGE ???"
```

**Input is per-building, not world-level.** Callers pass `boundData.unlockCumulativeBrainPower` (`UpgradeSlotUI.cs:158`) or `boundData.gateRebirthCount` (`CashShopSlotUI.cs:151`) — i.e., a fixed, authored value on the specific item being rendered. **Tier is derived from the building's own progression gate, not from `WorldRestorationManager.CurrentStage`/`RestorationPercent`/`CumulativePointsSpentOnRestoration` in any way.** `ClassificationTier.cs` has no reference to `WorldRestorationManager` at all, and neither call site passes anything restoration-derived into it. This is a static, world-state-independent lookup — it will return the same tier for the same building on every call, for the life of the run, regardless of what stage the world is in.

## 5. UpgradeSlotUI / CashShopSlotUI — where name/description reach the UI

**No shared path. Both duplicate the read-and-render logic independently.** The only thing they share is the `ClassificationTier.GetLabel()` call added in `07c873a`; name/description rendering itself is fully separate per file.

**`UpgradeSlotUI.cs`** (`RefreshState`, called from `ShopUIController.RefreshAllSlots`):
- Locked: `nameText.text = ClassificationTier.GetLabel(...)` (`:158`); description replaced with the literal `"Access restricted by the Ministry."` (`:149`).
- Unlocked: `nameText.text = boundData.buildingName;` — **exactly one line, `:180`** — direct field read, no intermediary. Description is a multi-part interpolated string built from `boundData.description` plus computed BPPS/CPS numbers (`:145`).

**`CashShopSlotUI.cs`** (`RefreshState`, called from `CashShopUIController`'s own refresh loop):
- The unconditional top-of-method assignment `nameText.text = boundData.displayName;` (`:83`) runs first, *before* any lock check, then gets overwritten by whichever terminal branch actually applies. Three terminal writes to `nameText.text`, one per state: owned (`:116`, prefixed with an "ONE-TIME UPGRADE" banner), locked (`:151`, now the `ClassificationTier` call, added in `07c873a`), unlocked-purchasable (`:175`, same prefix pattern as owned). Description similarly has three separate write sites (`:122`, `:156`, `:184`), each its own inline string.

**Refresh trigger, not per-frame.** `ShopUIController.cs:968-971` wires `RefreshAllSlots()` to `CurrencyManager.OnBrainPowerChanged`/`OnCumulativeBrainPowerChanged`, `UpgradeManager.OnBuildingsChanged`, plus initial open/build (`:906-920`) — genuinely event-driven, not polled. **This refresh chain has no subscription to `WorldRestorationManager`'s events at all** (confirmed by absence in `ShopUIController.cs`'s subscribe block, `:904-943`) — directly relevant to §6.

## 6. Fan-out if displayName/description became stage-dependent

### Rendering call sites that would need to change (the "safe" part)

Only two lines are the actual terminal render points for the *unlocked* name string — `UpgradeSlotUI.cs:180` and `CashShopSlotUI.cs:83/116/175` (three sites in one file, since it duplicates per-state instead of centralizing). Description has a comparable small number of sites: `UpgradeSlotUI.cs:145`, `CashShopSlotUI.cs:122/184`. In isolation this looks like a 5-6 call-site change — cheap.

**But the refresh trigger itself is missing.** None of `ShopUIController`, `UpgradeSlotUI`, or `CashShopSlotUI` currently subscribes to `WorldRestorationManager.OnRestorationStageChanged`/`OnRestorationProgressChanged` (§3, §5). Today the shop only redraws on currency/building-ownership events. A stage-dependent name would sit stale on screen from the moment the world heals past a breakpoint until the next currency tick happens to fire a redraw for an unrelated reason. This needs a new subscription added to `ShopUIController.cs`'s existing subscribe block (`:904-943`), not just a change to what the rendering reads.

### Identity-coupling — where a renamed/varying building breaks something else (the real cost)

`buildingName` is not just a display string. Confirmed by direct citation, four distinct roles on the exact same field value:

1. **In-memory ownership key.** `UpgradeManager.cs:33` — `private readonly Dictionary<string, int> buildingLevels` — keyed by `building.buildingName` at every read and write site (`:72`, `:162-163`, `:300`, `:309`). `GetBuildingLevel`, `TryBuyBuilding`, `LoadBuildingLevels`, and `MaxAllBuildings` all resolve "how many of this building does the player own" by looking up the *current* `BuildingData.buildingName` string against this dictionary.
2. **Save-file identity key.** `SaveManager.cs:29` (`PlayerData.buildingLevels: List<BuildingSaveEntry>`) persists `{buildingName = entry.Key, level = entry.Value}` (`:492`) — i.e., whatever string was the dictionary key at save time is what's written to disk. On load (`UpgradeManager.cs:295-300`), that saved string repopulates the dictionary directly. If a building's authored `buildingName` becomes stage-computed rather than fixed, **a save written while the world was at one stage would store a name that may not match the name the game computes for that building at a different stage on the next load** — the lookups in role 1 above would silently miss, and that building's owned levels would appear to reset to zero. This is the highest-severity break: real player progress loss, not a cosmetic glitch.
3. **Dialogue narrator-line match key.** `DialogueManager.cs:334` passes `building.buildingName` (read live, at the moment of purchase) into `TryFireLine`, which at `:455` does `line.buildingName == buildingName` — an exact string-equality match against each `NarratorLine.buildingName` (`NarratorLine.cs:35`), which is a **fixed, one-time-authored string per dialogue asset** (the 7 `BuildingPurchase_*` lines documented in CLAUDE.md). If the live building name varies by stage while the dialogue asset's comparison string stays fixed to (say) the stage-0 name, those 7 building-specific lines would only ever fire correctly during stage 0 and silently stop matching afterward.
4. **Dormant-code identity too, for completeness.** Even the parked `ShopQuery.cs` (`:153`, `:290`) derives its item ID as `$"building:{buildingName}"` — the same coupling would carry into that dead path if it were ever revived.

Lower-severity, cosmetic-only sites also found, listed for completeness but not blocking: `ShopUIController.cs:808` (sort tie-break uses `buildingName` for deterministic ordering — a stage-varying name could reorder rows unpredictably as the world heals, not a data-loss risk, just a UX one) and `ShopUIController.cs:830` (`slot.name = $"UpgradeSlot_{data.buildingName}"`, a scene-hierarchy GameObject label, purely for Editor/debug legibility).

### The direct answer to "is display name and identity already decoupled, or is the name the key somewhere"

**The name is the key — for `BuildingData` specifically.** And this is the *odd one out* in this codebase, not the norm: `CashShopItemData.cs:24-25` already carries a separate `itemId` field with an explicit doc comment stating it's the *"Stable save key, independent of displayName -- mirrors BuildingData.buildingName/OutfitData.outfitId's role."* That comment's own framing implies `buildingName` was expected to serve an ID-like role — it does, it's just never been split into a separate field the way `CashShopItemData`/`OutfitData` were. Every other item type with a persistence/identity need in this project already decoupled identity from display text; `BuildingData` is the one type that never got that split. Making its display name stage-dependent without first introducing a separate stable `buildingId` field would be building on the one place in the schema that still conflates the two.
