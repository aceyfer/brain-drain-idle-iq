# CLAUDE.md Findings

Audit of `CLAUDE.md` against the live repo (2026-08-07/08). Every finding below was verified directly against current code, the current scene file, `PROJECT_BIBLE.md`, or `git log` — nothing here is inferred from `CLAUDE.md`'s own text alone. Findings only; `CLAUDE.md` itself was not edited.

---

## 1. CLAUDE.md never references PROJECT_BIBLE.md, TASKLIST.md, or TASKLIST_DETAILS.md, or states which file wins on disagreement

**CLAUDE.md claim:** No such reference exists anywhere in the file (confirmed by reading `CLAUDE.md` in full, all 133 lines).

**Verified correct state:** `PROJECT_BIBLE.md` line 4 states explicitly: *"Deep architecture lives in CLAUDE.md — this file tells you what the game is, what's true, what's broken, what's protected, and what 'done' means. If this file and any other doc disagree, this file wins."* `PROJECT_BIBLE.md` is dated "Last updated: 2026-07-05" and is actively maintained (its §4 blocker table, §8 scar-tissue log, and §9 checklist all carry entries dated into 2026-08). `TASKLIST.md`/`TASKLIST_DETAILS.md` are also live, commit-updated documents (e.g. commit `3fcb630`, "TASKLIST: settle notification design, add classification ladder and Wiley Karn VII").

**Proof:** `PROJECT_BIBLE.md:4`; existence of `TASKLIST.md`/`TASKLIST_DETAILS.md` at repo root; `git log --oneline` showing recent commits actively updating these files (`3fcb630`, `98b0608`, `0e62412`, `e0bcd25`, etc.).

**Why this is first:** every other finding below is downstream of a session reading only `CLAUDE.md`, with no signal that a newer, authoritative, actively-maintained doc exists and takes precedence. `CLAUDE.md` currently presents itself as a complete, standalone picture of the architecture with no forwarding pointer.

---

## 2. Target platform is stated as iOS; the actual launch platform is Android

**CLAUDE.md claim (line 9):** *"Engine: Unity `6000.4.8f1`, Universal Render Pipeline (2D), target platform iOS (portrait)."*

**Verified correct state:** `PROJECT_BIBLE.md` §7 rule 14 (line 133): *"Android is the launch platform; iOS is post-launch (decided 2026-08-05). The development machine is Windows-only, so iOS is blocked by the absence of Mac hardware... Bundle identifier is `com.eighthkind.braindrain` on both Android and iPhone."* Also `PROJECT_BIBLE.md` line 13 states platform as iOS but that line itself predates the 2026-08-05 decision and is likewise superseded by §7 rule 14 — confirming the drift is real, not a misread.

**Proof:** `PROJECT_BIBLE.md:133`; commits `bb87823` ("record Android-first platform decision"), `c6d6b90` ("Set Android/iOS bundle identifier and lock portrait orientation for device build").

---

## 3. The Illumisnotty title ladder documented in CLAUDE.md does not match the code

**CLAUDE.md claim (line 103):** *"New `RebirthManager.GetIllumisnottiTitle(int rebirthCount)` (static) maps tier → title: 0 → none, 1 → 'Junior Associate Snott', 2 → 'Regional Snott Manager', 3 → 'Vice President of Snottery', 4 → 'Lord Snott (Provisional)', 5 → 'Grand Illumisnotti', 6+ → 'Supreme Snott Eternal'."* Same line also claims: *"HUDController gained `illumisnottiTitleText` (new optional serialized field, not yet wired to a scene GameObject...)"*

**Verified correct state:** The actual method (note the spelling — `Illumisnotty`, not `Illumisnotti`) is `RebirthManager.GetIllumisnottyTitle(int rebirthCount)`, `RebirthManager.cs:125-131`:
```
if (rebirthCount >= 11) return "BUNKER SUPREME";
if (rebirthCount >= 6) return "ILLUMISNOTTY INTERN";
if (rebirthCount >= 4) return "BUNKER BUREAUCRAT";
if (rebirthCount >= 2) return "UNDER-SNOT ELITE";
return "SNOTTY ROOKIE";
```
i.e. 0–1 SNOTTY ROOKIE / 2–3 UNDER-SNOT ELITE / 4–5 BUNKER BUREAUCRAT / 6–10 ILLUMISNOTTY INTERN / 11+ BUNKER SUPREME — a complete replacement of the six-tier ladder CLAUDE.md documents, not a drift within it. `PROJECT_BIBLE.md` §7 rule 11 (line 128-130) confirms this ladder as the decided, single source of truth ("decided 2026-07-03: the SNOTTY ROOKIE ladder is the one and only Illumisnotty title ladder").

The "not yet wired" claim is also stale: `HUDController.cs:24-25` shows the field renamed via `[FormerlySerializedAs]` chain to `illumisnottyTitleText`, and `SampleScene.unity:38148` shows it wired (`illumisnottiTitleText: {fileID: 2051647702}` — nonzero fileID under the old serialized name, which Unity's `FormerlySerializedAs` resolves onto the renamed field).

**Proof:** `RebirthManager.cs:125-131`; `PROJECT_BIBLE.md:128-130`; `HUDController.cs:24-25`; `SampleScene.unity:38148`.

---

## 4. God Tier Store's premium-currency history and player-facing name are undocumented, creating resurrection risk for an explicitly banned pattern

**CLAUDE.md claim (line 26):** *"`CurrencyManager` ... tracks three connected currency tiers (premium purchases are direct real currency via `GodTierStoreManager`, no premium soft currency exists)"* — stated as a plain architectural fact, with no history and no policy attached.

**Verified correct state:** A Neuron premium soft-currency system was built and then deliberately, repo-wide purged. `PROJECT_BIBLE.md` §7 rule 13 (line 132): *"Premium = direct real currency only, via `GodTierStoreManager` exclusively (decided 2026-07-09/10, `TASKLIST_DETAILS.md` §10/§16). Neuron premium currency was purged repo-wide (`939222f`–`34841b7`); `PremiumShopManager`/`PremiumShopUIController`/`PremiumShopSlotUI` are deleted, not dormant. **No soft-currency path to premium content may ever exist** — the 2,500-Cash `ProfanityPack` was killed outright rather than repriced specifically because it was such a path... The store is called the **God Shop**."* CLAUDE.md's God Tier Store section (lines 93, 111) never uses the "God Shop" player-facing name and never mentions Neuron or the ban, even though the ban is a binding constraint on any future currency-flow work (e.g. Convert-panel or RP-well changes) that CLAUDE.md as written gives no reason to check for.

**Proof:** `PROJECT_BIBLE.md:132`; commits `939222f` and `34841b7` confirmed present in `git log` (`git cat-file -e` on both succeeded); commit `d62fbee` ("wire real store SKUs on four God Shop items") confirms "God Shop" as the live player-facing term in current commit history.

---

## 5. MainUIController.cs — an entire controller layer — is undocumented

**CLAUDE.md claim:** No section of CLAUDE.md's "UI layer" description (lines 67-71) or anywhere else mentions `MainUIController`.

**Verified correct state:** `MainUIController.cs` is the sole owner of bottom-navigation wiring (SHOP, CONVERT, RESTORE buttons, `MainUIController.cs:16-18`), owns shop/convert panel mutual-exclusion logic (`OnShopClicked`/`OnConvertClicked` each force-close the other panel, `MainUIController.cs:147-177`), and owns the RESTORE action itself (`OnRestoreClicked`, `MainUIController.cs:179-188`, calling `WorldRestorationManager.Instance.TrySpendPointsOnRestoration(currency.CurrentPoints)`). It is wired into the scene via a dedicated Editor tool, `MainUIControllerWireFix.cs`.

**Proof:** `MainUIController.cs` (whole file, 199 lines); `MainUIControllerWireFix.cs:38-83`; `SampleScene.unity:38192-38208` (live `MainUIController` component instance with wired button/controller refs).

---

## 6. ConvertUIController.cs is undocumented, and CLAUDE.md's model of the CONVERT button is stale — it omits the RESTORE button entirely

**CLAUDE.md claim (line 67):** *"`HUDController`'s 'CONVERT' button calls `ConvertCashToPoints(CurrentCash)` (convert everything on demand); there's currently no UI for toggling `AutoConvertCash`."*

**Verified correct state:** CONVERT is no longer a single-action button. It opens a full panel (`ConvertUIController.cs`, 250 lines) with three distinct actions: Convert 50% BP→Cash, Convert 100% BP→Cash, and Convert All Cash→RP (`ConvertUIController.cs:207-248`). Separately, and not mentioned anywhere in CLAUDE.md, a second button — **RESTORE** — exists and is what actually spends RP on World Restoration (`MainUIController.OnRestoreClicked`, `MainUIController.cs:179-188`; also documented correctly, but only in passing, at `CLAUDE.md:51`'s `WorldRestorationManager` paragraph, which is inconsistent with line 67's model of a single CONVERT button doing everything).

**Proof:** `ConvertUIController.cs:15-250` (whole file); `MainUIController.cs:179-188`; `HUDMobileOverhaul.cs:125,152,166` (RESTORE button's scene wiring/label, entirely absent from CLAUDE.md).

---

## 7. The RP/Points shop tab is described as pending scene-wiring; it was actually cut by design decision and will never be built as a shop

**CLAUDE.md claim (line 110):** *"Scene wiring: all 3 new shops' C# is code-complete (managers + UI controllers + slot prefabs), but no panel/button/Content GameObject hierarchy exists yet in `SampleScene.unity` for any of them..."* — grouping the Points/RP shop (Shop 3) with the Cash Shop and God Tier Store as simply "not yet built."

**Verified correct state:** `PROJECT_BIBLE.md` §8 (line 146, "Shop is dual-wired to the same GameObjects" entry): *"Decided 2026-07-09: the RP tab (`rpTabPanel`/`rpContent`, World Restoration stage rows) is cut — that third tab slot becomes the Premium real-currency store instead... World Restoration progression itself stays in the game, just not presented as a shop tab. `ShopUIController`'s RP-specific code (`BuildRestorationTab`, the lazy-rebuild retry in `SelectTab`) is left in place as harmless dead code for now, not ripped out."* This is a closed design decision, not an open scene-wiring task — `RestorationSlotUI.cs` and `ShopUIController.BuildRestorationTab` exist in code but are confirmed dead, not "pending."

**Proof:** `PROJECT_BIBLE.md:146`; `RestorationSlotUI.cs` (whole file, confirmed present but unreferenced from any active shop-tab build path).

---

## 8. "Illumisnotti" spelling is wrong throughout CLAUDE.md — the correct, decided player-facing spelling is "Illumisnotty"

**CLAUDE.md claim:** Uses "Illumisnotti" consistently — in the section header "Illumisnotti narrative/economy rewrite" (line 83), in prose (line 7), and in code-identifier claims (line 103's `GetIllumisnottiTitle`, `illumisnottiTitleText`).

**Verified correct state:** Current code and docs use "Illumisnotty." `RebirthManager.cs:125` — the actual method is `GetIllumisnottyTitle`. `HUDController.cs:25` — the current field name is `illumisnottyTitleText` (with `[FormerlySerializedAs("illumisnottiTitleText")]` above it, confirming the old spelling was deliberately renamed away from, not merely inconsistent). `PROJECT_BIBLE.md` §7 rule 11 (line 130): *"Player-facing spelling is 'Illumisnotty'."* Dialogue assets also live under `Assets/_Game/Dialogue/Illumisnotty/` (the folder itself uses the corrected spelling).

**Proof:** `RebirthManager.cs:125`; `HUDController.cs:24-25`; `PROJECT_BIBLE.md:130`; folder path `Assets/_Game/Dialogue/Illumisnotty/`.

---

## 9. "Points" is not documented as player-facing-renamed to "Restoration Points (RP)"

**CLAUDE.md claim:** Refers to the tertiary currency as "Points" throughout (e.g. line 7, line 29, line 87, line 91), with no mention of any player-facing rename.

**Verified correct state:** `PROJECT_BIBLE.md` §2 rule 4 (line 30): *"'Points' is renamed **Restoration Points (RP)** in all player-facing text; C# names stay unchanged (same convention as The Snotting rename)."* This is the same category of rename CLAUDE.md already documents correctly for Rebirth→"The Snotting" (line 103) but omits entirely for Points→RP, despite both being decided under the same convention.

**Proof:** `PROJECT_BIBLE.md:30`.

---

## 10. Editor tooling section is incomplete — two active, in-use Editor tools are undocumented

**CLAUDE.md claim (line 71):** Documents `Editor/ShopPanelLayoutFix.cs` in detail as the project's example of scene-wiring Editor tooling, with no mention of any other tool of this kind.

**Verified correct state:** Two further Editor tools of the identical pattern exist and are actively used for HUD layout and wiring: `Editor/HUDMobileOverhaul.cs` (385 lines — owns the current bottom-nav two-row button layout, the EconomyStrip extraction, and Snotting-button repositioning) and `Editor/MainUIControllerWireFix.cs` (307 lines — wires `MainUIController`'s button/controller refs, and separately wires `BackgroundStageView`/`GameManager.rankDefinitions`). Neither appears anywhere in CLAUDE.md.

**Proof:** `Editor/HUDMobileOverhaul.cs` (whole file); `Editor/MainUIControllerWireFix.cs` (whole file).

---

## Pre-existing bugs (not part of any current design discussion — logged here as artifacts of this audit)

### Bug A — `CashConverted_VeryResponsible` narrator line describes the wrong conversion

**File:** `Assets/_Game/Dialogue/CashConverted_VeryResponsible.asset:19`
**Text:** `dialogueLine: Converting brain power to cash. Very responsible. Very sad.`

This line is gated on `triggerType: 6` (`CashConverted`, `Assets/_Game/Dialogue/CashConverted_VeryResponsible.asset:15`, cross-referenced against `NarratorTriggerType` in `NarratorLine.cs:14`). `OnCashConverted` is invoked exactly once in the codebase, inside `CurrencyManager.ConvertCashToPoints` (`CurrencyManager.cs:368-383`, invocation at line 381) — i.e. it fires only on Cash→RP conversion. `ConvertHalfBP`/`ConvertAllBP` (`ConvertUIController.cs:207-240`, the actual BP→Cash actions) call `CurrencyManager.SpendBrainPower`/`AddCash` directly and never touch `OnCashConverted`. So this line's text ("brain power to cash") describes a conversion its own trigger can never fire on. Confirmed pre-existing and independent of any decision made in this session — the trigger wiring is correct, only this one line's copy is wrong.

### Bug B — two controllers independently hold `convertButton`/`convertUIController` references; one path is live, the other looks vestigial, and neither should be trimmed without further confirmation

`HUDController.cs:36-37` declares `private Button convertButton` and `private ConvertUIController convertUIController`, plus a public `ConfigureConvertPanel(ConvertUIController controller, Button pointsButton)` method (`HUDController.cs:148-152`). `MainUIController.cs:17,22` independently declares the same two field types. Both are wired in the live scene to the *same* underlying GameObjects — `SampleScene.unity:38156-38157` (`HUDController.convertButton`/`convertUIController`) and `SampleScene.unity:38205,38208` (`MainUIController.convertButton`/`convertUIController`) resolve to identical `fileID`s (`179850116` and `370575159` respectively).

Load-bearing path (confirmed active): `MainUIController.Awake()` registers `convertButton.onClick.AddListener(OnConvertClicked)` (`MainUIController.cs:40-44`), and `OnConvertClicked` calls `convertUIController.TogglePanel()` (`MainUIController.cs:163-177`). This is also the path the Editor tool actively maintains — `MainUIControllerWireFix.cs:49,57-60` assigns both fields by name/type lookup on every run.

Appears vestigial (not confirmed dead, just unreferenced): a repo-wide search for `ConfigureConvertPanel(` found zero call sites anywhere outside its own declaration. A search for `convertButton.`/`convertUIController.` usage inside `HUDController.cs`'s own method bodies found no matches — the fields are serialized and scene-wired (non-null), but nothing in `HUDController.cs` reads or calls through them.

Per instruction: nothing has been trimmed, and this file does not recommend trimming either field — confirming that `HUDController`'s copies are truly dead (versus reachable through some path not caught by a text search, e.g. reflection or an Inspector-only event hookup) requires a live Play Mode check, not a static read.

---

## UNVERIFIED

Nothing from this audit is listed here — every finding and both bugs above were checked directly against a specific file/line, the live scene file, `PROJECT_BIBLE.md`, or `git log`/`git cat-file`, not inferred from `CLAUDE.md`'s own claims.
