# Stage Progression Sim Proposal (ANALYSIS ONLY — 2026-07-28)

No asset was edited, no balance value was changed, nothing was committed. This is a read-only
economy simulation against `DESIGN_STAGE_PROGRESSION.md`'s target pacing curve, run with a
disposable per-second sim (same style as `balance_sim.js` / the earlier tap-power balance pass),
using the real current values from `Assets/_Game/Buildings/*.asset` and `Assets/_Game/Restoration/*.asset`.

## Assumptions

- **Normal mixed-play player**: 1.5 taps/sec while actively playing, greedy-buys the *cheapest
  affordable+unlocked* building in each of the two independent currency pools (Brain Power / Cash)
  every tick — i.e. uses all 16 buildings, not just Snott Market Exchange (SME). Cash is converted
  to Points and immediately spent on Restoration every 10 simulated seconds (matches the real
  `AutoConvertCash` cadence; an all-cash-every-tick model was tried first and rejected — see
  "Modeling note" below, it produces an artifact, not a real result).
- Since this game has **no offline currency accrual** (`PlayerIQManager` offline decay is the only
  offline-time effect; `CurrencyManager`/`UpgradeManager` only tick while the app is open), progress
  is purely a function of *cumulative active seconds*, independent of how that time is split across
  calendar days. A "normal" cadence of **30 active minutes/day** is used only to translate active-seconds
  into calendar-day equivalents for comparison against the design brief's day/week targets.
  `pointsConversionRate` (0.1) and SME's `baseCost`/`costMultiplier` (75 / 1.38) are held fixed per
  the brief's constraints.
- **Pure-tap grinder** (task 2): flat 1-BP taps, buys *only* SME, ignores all 15 other buildings —
  this is the literal policy CODEX_FINDINGS.md modeled and the literal policy requested here.

## Target active-time checkpoints used (from DESIGN_STAGE_PROGRESSION.md, at 30 min/day)

| Stage | Target | Active-seconds used |
|---|---|---:|
| S1 | 30–60 min (single session) | 2,700s (45 min) |
| S2 | several days of 30-min sessions | 7,200s (4 days) |
| S3 | ~1 week | 12,600s (7 days) |
| S4 | ~2–3 weeks | 31,500s (17.5 days) |
| S5 | ~1 month+ | 63,000s (35 days) |
| S6 | not reachable in first month | 162,000s (90 days) |

## 1. Back-solved SME cash/s + six thresholds (normal mixed-play player)

**Recommended `baseCashPerSecond`: 1.5** (down from 5.0, a 70% cut). `baseCost`/`costMultiplier`
unchanged (75 / 1.38) — the sim did not show a need to move them for this policy.

**Six `pointsRequired` thresholds** (all six now populated; current values only cover S1–S3):

| Stage | Current `pointsRequired` | Proposed `pointsRequired` |
|---|---:|---:|
| S1 | *(none)* | 146,514 |
| S2 | 2,500 | 2,132,885 |
| S3 | 10,000 | 5,658,229 |
| S4 | 50,000 | 20,764,149 |
| S5 | *(none)* | 49,723,516 |
| S6 | *(none)* | 152,334,201 |

**Resulting times for the normal mixed-play player** (1.5 taps/sec, all 16 buildings), verified by
re-running the sim against these exact thresholds:

| Stage | Time | Calendar-equivalent @ 30 min/day |
|---|---|---|
| S1 | 45.0 min | same-day |
| S2 | 2.00 hr active | ~4 days |
| S3 | 3.50 hr active | ~7 days |
| S4 | 8.75 hr active | ~17.5 days |
| S5 | 17.50 hr active | ~35 days |
| S6 | 45.0 hr active | ~90 days (3 months) |

**Robustness check**: re-ran the same six thresholds at tap rates 1.0/1.5/2.0/2.5 per sec. All six
stage-crossing times moved by only 1–3%. The full-roster economy is *not* sensitive to a normal
player's exact tap speed — idle building income dominates over raw tap contribution at these
timescales, so this curve holds up across realistic "normal player" variance.

### Important caveat — SME is not actually the controlling lever here

Re-running the identical mixed-play policy with SME's cash/s forced to **0** reproduces the *same*
runaway growth (Stage-3-equivalent Points crossed by ~t=12,600s regardless). The real driver is the
**Brain Power building ladder** (StupAid H2O, The Literal Library, Apex Brain Greens, Tinfoil
Headband, Hyperbolic Brain Chamber, Cryo Plunge Tank, Pineal Overclock, Brain-Rot Think Tank) —
none of which need SME or Cash at all to compound. That ladder alone reaches Brain-Rot Think Tank's
725,000-cumulative-BP unlock in ~2.5–3 hours of active time, and Brain-Rot Think Tank's own
200 Cash/sec/level then ignites the entire Cash tier from scratch (buying Doomscroll Engine,
HOA Protection Racket, Crypto-Bro Compound, Reality TV Syndicate, etc.), independent of SME.
Four of those nine BP buildings (Tinfoil Headband `1.11`, Hyperbolic Brain Chamber `1.12`, Cryo
Plunge Tank `1.13`, Pineal Overclock `1.15`) have `costMultiplier` values well below the `1.21–1.38`
range the 2026-06-21 rebalance pass tuned into the original seven — they were added later (the BP-tab
copy pass) and were never balance-tested. **Nerfing only SME's cash/s (as this task's scope requires)
does not meaningfully control pacing for a normal mixed-play player** — the thresholds above only
work because they were fit *to* this uncontrolled growth curve, not because the SME nerf tames it.
A real fix likely needs the BP-ladder's cost multipliers in scope too, which this task's lever set
explicitly excludes — flagging it rather than silently going out of scope.

*(Modeling note: an initial version of this sim converted 100% of Cash to Points every single tick,
which made any building costing more than one tick's income permanently unaffordable and produced a
false, knife-edge-looking discontinuity. Fixed to match the real 10-second `AutoConvertCash` cadence
before drawing any conclusions above.)*

## 2. Pure-tap grinder stress test (SME-only, against the proposed values)

Flat-tap, SME-only policy, `baseCashPerSecond = 1.5`, proposed thresholds above:

| Tap rate | S1 | S2 | S3 | S4 | S5 | S6 |
|---:|---|---|---|---|---|---|
| 2/sec | 16.6 hr | **7.0 days** | 16.7 days | 53.7 days | NEVER (<57d horizon) | NEVER |
| 3/sec | 15.6 hr | **6.7 days** | 16.0 days | 51.8 days | NEVER (<57d horizon) | NEVER |
| 5/sec | 14.5 hr | **6.3 days** | 15.2 days | 49.5 days | 110.0 days | NEVER |

All three continuous, nonstop tap rates: Stage 2 takes **6.3–7.0 days of uninterrupted tapping**
(not 1 day), Stage 3 takes 15–17 days continuous, Stage 4 ~50 days continuous, and Stage 5/6 are
effectively unreachable by this policy within any realistic horizon. This comfortably clears the
"≥ ~1 day of active grinding" floor for Stage 2 and does not trivialize S3–S6 — if anything it
overshoots in the safe direction, because SME's own `1.38` cost multiplier eventually outgrows flat
tap income and the strategy self-limits (it's a *worse* strategy than the normal mixed-play policy,
which is the correct incentive direction).

## 3. Conclusion — is a separate cap still required?

**Yes — but not for the reason originally framed.** The SME-only pure-tap grinder (task 2) is not the
real risk; it's already self-punishing. The real risk is a **dedicated mixed-play grinder** — someone
who plays the full, efficient 16-building economy (not just SME) for many hours per real day instead
of the assumed 30 minutes. Because this design has **no daily/session cap of any kind** — pacing is
driven purely by cumulative active seconds — Stage 6 only requires 45 hours of *total* active
engagement. A player active 6–8 hours/day (not unrealistic for a "dedicated" player, and far short of
nonstop tapping) reaches Stage 6 in **under a week**, collapsing the entire month-plus arc the design
brief calls for.

This reframes the open sub-problem in `DESIGN_STAGE_PROGRESSION.md`: the needed cap is not really a
**tap-specific** income cap (SME-only flat-tapping is already safely slow) — it's a broader **daily
active-engagement cap on total production** (tap *and* idle together), since the exploit path is
"play more hours/day with a normal efficient build," not "tap faster."

**Rough quantification**: cap counted production (tap + idle, in whichever combination) at roughly
**30–60 minutes of full-rate progress per real calendar day**, with steep diminishing returns (or a
hard stop) beyond that — e.g. after ~45–60 active minutes in a day, scale all Brain Power/Cash gains
down toward ~10–20% for the remainder of that calendar day, resetting at day rollover. That single
change would make the six thresholds above hold regardless of how many hours someone plays in one
sitting, without needing any per-building nerf beyond what's proposed here. This was not implemented
— design-direction only, per this task's analysis-only scope.

---

## 2026-07-28 — Daily active-engagement cap: Phase 1 sim re-run (ANALYSIS ONLY)

Approved design being tested: full rate for the first **45 minutes** of counted productive time per
calendar day (tap + idle combined, not tap-specific), then all Brain Power/Cash gains scale to **15%**
for the remainder of that day. Resets at calendar-day rollover. No asset or `.cs` edit in this section
— sim only, per Phase 1 scope.

### Modeling correction: PlayerIQ offline decay now has a real currency effect

The prior run treated the IQ multiplier as a wash. Modeled explicitly this time, matching
`PlayerIQManager.cs`/`CurrencyManager.cs` exactly:

- `GetIQProductionMultiplier()`: `Lerp(0.25, 1.0)` across IQ 1→100, then `Lerp(1.0, 1.25)` across IQ
  100→200. Applies to **idle BPPS/CPS only** — tap income stays exempt, per the existing code comment.
- Offline decay (`PlayerIQManager.ApplyOfflineDecay`): linear toward the floor (`MinPlayerIQ = 1`,
  **not** the stale "floor 60" figure from `CLAUDE.md` — the actual constant is `1`), reaching the
  floor at 8 hours offline (`OfflineDecayMaxHours`) and no further past that.
- Recovery (`RestoreIQFromTap`): +1 IQ per tap while IQ < 100, no effect at/above 100.
- Overcharge decay: −0.1 IQ/sec while IQ > 100, only while the app is running (`DecayOvercharge`).
- Building/infrastructure purchases still add flat `+1` IQ each, uncapped by the new throttle (that's
  a separate mechanic from currency gains).

### Three profiles, six proposed thresholds, cap applied to tap+idle

Tap rate 1.5/sec while actively engaged (unchanged assumption); full 16-building greedy mixed-play
policy (same as section 1 above).

| Stage | CASUAL (30 min/day, decays to IQ 1 daily) | ENGAGED (2 hr/day, decays daily) | GRINDER (7 hr/day, no decay) |
|---|---|---|---|
| S1 | day 2 (40.8 min active) | day 1 (40.2 min active) | day 1 (40.2 min active) |
| S2 | day 4 (1.94 hr active) | day 2 (3.77 hr active) | day 2 (7.16 hr active) |
| S3 | day 7 (3.46 hr active) | day 4 (6.55 hr active) | day 2 (13.75 hr active) |
| S4 | day 18 (8.78 hr active) | day 10 (18.22 hr active) | day 6 (1.47 days active) |
| S5 | day 36 (17.65 hr active) | day 19 (1.52 days active) | day 11 (2.94 days active) |
| S6 | day 92 (163,910s active) | day 49 (346,210s active) | **day 27 (663,400s active)** |

CASUAL and ENGAGED never reach Stage 6 inside 30 days (day 92 and day 49 respectively — comfortably
clear). **GRINDER reaches Stage 6 on day 27 — inside the first month.**

### Acceptance criterion: FAILED

The single acceptance criterion ("Stage 6 must be unreachable within the first month by any of these
profiles") is **not met** as specified (45 min full-rate / 15% throttle, current proposed S6 threshold
of 152,334,201). Not silently retuned — three options below, with numbers, for Aceyfer to pick from:

| Option | Change | GRINDER's new S6 day | Notes |
|---|---|---:|---|
| **A1 — tighter throttle** | Keep 45-min full-rate window, drop throttle 15%→**10%** | day 33 | Smallest single-number change; ~3-day buffer past day 30, thin but not a knife-edge — CASUAL/ENGAGED barely move (they rarely hit the throttled tail) |
| **A2 — tighter window + throttle** | 45min→**30min** full-rate, 15%→**10%** throttle | day 40 | More comfortable ~10-day buffer; changes two numbers instead of one |
| **B — raise S6 threshold only** | `pointsRequired` 152,334,201 → **~210,000,000** (+38%), cap stays 45min/15% as approved | day 35 | Keeps the approved cap numbers untouched; only moves the one threshold this task already has in scope to edit later |
| **C — non-economic gate** | Add a hard calendar-day floor to Stage 6 (e.g. `daysSinceFirstLaunch >= 35`) *in addition to* the Points threshold | N/A — structural, not economy-tunable | Most robust to future economy/building changes (doesn't rely on re-tuning if buildings are added/rebalanced later), but is a new gate type `WorldRestorationManager` doesn't currently have |

No option was applied. All three are compatible with the approved cap design (tap+idle combined,
45min/15%, calendar-day reset) as stated — A1/A2 adjust the cap's own numbers, B/C leave the cap as
approved and adjust something else instead.

### Apex Brain Greens under the cap

Compared the existing greedy mixed-play policy (which buys Apex Brain Greens like any other
BP-pool building when it's cheapest-affordable) against an otherwise-identical policy with Apex
excluded entirely, under ENGAGED and GRINDER:

| Profile | Stage | WITH Apex | Apex EXCLUDED |
|---|---|---|---|
| ENGAGED | S2 | day 2 (13,580s) | day 2 (10,370s) |
| ENGAGED | S3 | day 4 (23,590s) | day 4 (22,940s) |
| ENGAGED | S6 | NEVER (<40d) | NEVER (<40d) |
| GRINDER | S3 | day 2 (49,510s) | day 2 (45,230s) |
| GRINDER | S4 | day 6 (126,600s) | day 5 (123,010s) |
| GRINDER | S6 | day 27 (663,400s) | day 27 (655,590s) |

**Apex Brain Greens saves zero minutes of the daily 45-minute allowance.** The cap is gated on
real elapsed clock-seconds, not on currency earned — buying Apex changes *how much* BP a tap yields,
not *how many real seconds* count toward the daily budget, so by construction it cannot shrink the
allowance-consumption side of the equation at all.

Worse: under the greedy-cheapest-first policy, buildings WITH Apex in rotation are consistently the
same or **slightly slower** to each stage than Apex-excluded (e.g. ENGAGED Stage 2: 13,580s vs 10,370s
active — Apex-excluded is ~24% faster to the same threshold). Apex's cost competes against StupAid
H2O/The Literal Library for the same early BP budget, and those buy idle BPPS that compounds forever,
while Apex only pays off during the fraction of session time actually spent tapping. Under this cap,
where idle production dominates total output regardless of engagement level, **Apex Brain Greens is
effectively dead weight** in an efficient build — this was already marginal before the cap (see the
robustness check in section 1: tap rate barely moved outcomes even without any cap), and the cap does
not change that calculus in Apex's favor since it throttles tap and idle identically.

### Status (superseded below — Option B chosen)

~~Phase 1 complete. STOP — awaiting Aceyfer's approval and choice among Options A1/A2/B/C before any
Phase 2 implementation work begins.~~ Resolved: Option B, with a larger buffer (250,000,000, not
210,000,000). See the next section.

---

## 2026-07-28 (addendum) — Option B confirmation, Points Shop rescale, Apex diagnosis

Cap left exactly as approved (45 min full-rate, 15% throttle, tap+idle combined, calendar-day reset).
A1/A2 numbers untouched (not applied — Option B was chosen instead). All of this section is analysis
only: no asset or `.cs` edit, no commit besides this file.

### Option B confirmed with the larger buffer

Re-ran GRINDER (7 hr/day, no decay) against Stage 6 `pointsRequired = 250,000,000` (not the originally
proposed 210,000,000):

**GRINDER reaches Stage 6 on day 41** (1,030,130s cumulative active) — an 11-day buffer past day 30,
versus the thin ~5-day buffer the original 210,000,000 figure would have given. CASUAL (day 92) and
ENGAGED (day 49) both already cleared 30 days comfortably and are unaffected by raising S6 further.
Rationale accepted: Stage 6 is terminal content, so the overshoot costs nothing, and the wider margin
is more defensible against a more efficient build than the one this sim models.

**Final six thresholds** (supersedes the section-1 table above for `pointsRequired`, which used
152,334,201 for S6):

| Stage | `pointsRequired` |
|---|---:|
| S1 | 146,514 |
| S2 | 2,132,885 |
| S3 | 5,658,229 |
| S4 | 20,764,149 |
| S5 | 49,723,516 |
| S6 | **250,000,000** |

### 1. Points Shop rescale

**Current data** (`Assets/_Game/PointsShop/*.asset`):

| Item | Cost | Gate (RebirthCount / WorldStage) |
|---|---:|---|
| Snott County Redistricting | 500 | 0 / stage 1 |
| Illumisnotti Leak Network | 2,500 | 1 / none |
| Neighborhood Defrost | 10,000 | 0 / none |
| The Snotty Guard | 10,000 | 2 / none |
| Dream Insertion Broadcast | 50,000 | 3 / stage 3 |
| Snott Family Crest Takeover | 250,000 | 5 / stage 4 |
| The Grand Snotting | 1,000,000 | 7 / stage 5 |

Total: 1,323,000 RP — 0.53% of the new S6 threshold.

**When each becomes raw-RP-affordable under the new curve** (cumulative RP earned, ignoring gates,
capped economy, ENGAGED and GRINDER profiles):

| Item | ENGAGED | GRINDER |
|---|---|---|
| Snott County Redistricting (500) | day 1, 16.7 min active | day 1, 16.7 min active |
| Illumisnotti Leak Network (2,500) | day 1, 22.3 min active | day 1, 22.3 min active |
| Neighborhood Defrost / Snotty Guard (10,000) | day 1, 25.8 min active | day 1, 25.8 min active |
| Dream Insertion Broadcast (50,000) | day 1, 32.8 min active | day 1, 32.8 min active |
| Snott Family Crest Takeover (250,000) | day 1, 49.3 min active | day 1, 49.3 min active |
| The Grand Snotting (1,000,000) | day 2, 2.33 hr active | day 1, 4.00 hr active |

Confirmed the concern: every item is raw-affordable within the first day or two — the *only* thing
currently delaying a purchase in practice is each item's `gateRebirthCount`/`gateWorldStageIndex`, not
its cost. Cost is not doing any pacing work at all right now.

**Why the old costs make sense, and how to rescale them**: checking each cost against the currently-authored
(if not fully scene-wired — see caveat below) six restoration thresholds (2,500 / 10,000 / 50,000 /
250,000 / 1,000,000 for S1–S5) shows the original design is a clean, clearly-intentional pattern —
every item costs exactly **100% of one stage's `pointsRequired`**, except the cheapest item at a
discounted 20%:

| Item | Cost | = % of old stage... |
|---|---:|---|
| Snott County Redistricting | 500 | 20% of S1 (2,500) |
| Illumisnotti Leak Network | 2,500 | 100% of S1 (2,500) |
| Neighborhood Defrost | 10,000 | 100% of S2 (10,000) |
| The Snotty Guard | 10,000 | 100% of S2 (10,000) |
| Dream Insertion Broadcast | 50,000 | 100% of S3 (50,000) |
| Snott Family Crest Takeover | 250,000 | 100% of S4 (250,000) |
| The Grand Snotting | 1,000,000 | 100% of S5 (1,000,000) |

**Proposed rescale** — apply the identical ratio against the corresponding *new* threshold:

| Item | Old cost | Proposed new cost | Basis |
|---|---:|---:|---|
| Snott County Redistricting | 500 | **30,000** | 20% of new S1 (146,514) |
| Illumisnotti Leak Network | 2,500 | **145,000** | 100% of new S1 (146,514) |
| Neighborhood Defrost | 10,000 | **2,130,000** | 100% of new S2 (2,132,885) |
| The Snotty Guard | 10,000 | **2,130,000** | 100% of new S2 (2,132,885) |
| Dream Insertion Broadcast | 50,000 | **5,660,000** | 100% of new S3 (5,658,229) |
| Snott Family Crest Takeover | 250,000 | **20,760,000** | 100% of new S4 (20,764,149) |
| The Grand Snotting | 1,000,000 | **49,720,000** | 100% of new S5 (49,723,516) |

New total ≈ 80,575,000 RP — 32% of the new S6 threshold (up from the old total's 132% of *old* S5, so
this is actually slightly more conservative proportionally, since the new curve's higher stages have
much more headroom above S5 than the old curve did). Since each item is now priced at a full extra
stage-threshold's worth of RP (or a fifth of one, for the starter item), and `ExecuteRebirth` zeroes
`currentPoints` while stage/RebirthCount gates persist, buying one still means dedicating an entire
additional stage's worth of RP generation within *some* rebirth cycle — not spending it on Restoration
that cycle. That preserves the "real commitment" property across the Snotting loop the same way the
original numbers did, just rescaled to the new curve's dollar amounts. Not applied.

**Two adjacent findings, flagged but out of this task's scope, not actioned:**
- `CODEX_FINDINGS.md` and this sim both only ever exercised the **3 stages actually wired into
  `SampleScene.unity`'s `WorldRestorationManager.stages` list** (S1/S2/S3). The S4/S5 `.asset` files
  already exist on disk with real `pointsRequired` values (250,000 / 1,000,000) — they were used above
  as the "old" reference values on the assumption they're the intended full 6-stage curve — but if
  they're not actually wired into the scene's array, they're currently inert regardless of what this
  task does to the Points Shop. Worth confirming scene-wiring status before or alongside applying any
  of these rescales.
- `RebirthUIController.pointsSpentUnlockThreshold` (the Rebirth button's own visibility gate) is
  currently `50,000` — **less than the new S1 threshold (146,514)**. Under the new curve, the Rebirth
  button would become visible *before* a normal player even reaches Stage 1, which was presumably not
  the intent (the 2026-06-21 rebalance pass explicitly set 50,000 to land Rebirth well past the old
  S2/S3 range). Every `gateRebirthCount`-only Points Shop item (Illumisnotti Leak Network, Neighborhood
  Defrost, The Snotty Guard) is reachable only after at least one Rebirth, so this gate's mistuning
  directly affects when those three become purchasable in practice, independent of their own cost.
  Not rescaled here — not asked for, and touching Rebirth pacing is a separate task.

### 2. Apex Brain Greens — diagnosis

**Confirmed, not refuted.** Flat tap income structurally cannot keep pace with compounding idle
income, for two independent reasons, and this was true from the moment `02d5ef6` shipped — the daily
cap did not create the problem, it just gave the first controlled with/without comparison that made it
visible:

1. **Tap income is bounded by a human action-rate ceiling; idle income is not.** Apex's bonus is
   `+0.02 Brain-Power-per-tap per owned level`, realized at most `tap_rate` times per second (≈1–5 in
   every model used across this whole task). A building's `baseBrainPowerPerSecond` instead pays out
   every single second the app is open, tapping or not, with zero ceiling on attention. Any BP-cost
   idle building will out-produce Apex once owned in enough quantity, and quantity is exactly what
   compounds.
2. **Apex's own numbers are weak even before that ceiling matters.** At tap rate 1.5/sec, one Apex
   level is worth `1.5 × 0.02 = 0.03` BP/sec. StupAid H2O gives a flat `0.5` BP/sec/level for a *higher*
   base cost (25 vs 15) but a *slower* cost-growth curve (`1.10` vs `1.12`) — matching one StupAid level
   requires roughly **17 Apex levels'** worth of spend. Apex was underpriced for what it does from the
   start; the cap didn't introduce this, the with/without-Apex sim in Phase 1 just quantified it for
   the first time (WITH Apex: Stage 3 in 49,510s active for GRINDER; Apex EXCLUDED: 45,230s — i.e. Apex
   made that build ~9% *slower*, not faster).

### Options, with numbers, none implemented

**(a) Scale the tap bonus off current idle BPPS instead of a flat per-level amount** (e.g. each tap
grants `+X% of current idleBpps × level`, instead of `+0.02 × level`):
- Fixes the structural ceiling problem directly — Apex's payoff would compound alongside the idle
  economy instead of being capped by tap rate.
- **Risk, not fully quantified here**: this creates a *second* multiplicative feedback channel on top
  of the building-compounding one already flagged in section 1 as the real driver of runaway growth.
  Even a small `X` (e.g. 0.05%/level) at a high level count (Apex reached level ~150–156 in every
  GRINDER run above) would add roughly `1.5 taps/sec × 150 × 0.0005 ≈ 11%` extra BP/sec on top of
  whatever idleBpps already is — money that then buys more buildings, raising idleBpps, raising the
  next tap's absolute bonus again. This is the same shape of problem SME was originally flagged for
  (an unbounded funder), just relocated to tap. **Needs its own full time-stepped sim before any `X`
  is chosen** — do not treat this as a drop-in fix.
- **Stage-2 tap balance (`02d5ef6`) impact**: that commit's whole finding was that flat, rate-bounded
  tap income is what makes Stage 2 reachable in under an hour by pure tapping, and `g=0.02` was chosen
  as the best *containable* mitigation given tap had to stay flat. If tap bonus instead scales with
  idle BPPS, tap is no longer flat or bounded, and the entire `02d5ef6` analysis (and this task's own
  Phase 1 conclusion that pure-tapping is now safely self-limiting) would need to be rebuilt from
  scratch, not re-tuned.
- **Shop description / lore**: holds narratively as-is ("every tap hits harder") — the flavor text
  doesn't commit to a specific formula, only "hits harder," which remains true either way.

**(b) Give Apex a second, non-tap benefit (hybrid idle+tap building)** — simulated concretely:
adding `baseBrainPowerPerSecond` alongside the existing `tapBrainPowerPerLevel: 0.02`, tap bonus
unchanged:

| `baseBrainPowerPerSecond` | GRINDER Stage 3 | vs. current (49,510s) | vs. Apex-excluded (45,230s) |
|---:|---:|---|---|
| 0 (current) | 49,510s | — | +9.4% slower |
| 0.3 | 47,570s | 3.9% faster | +5.2% slower |
| 0.5 | 46,510s | 6.1% faster | +2.8% slower |
| 0.7 | 45,930s | 7.2% faster | +1.5% slower |

`0.5`/level (matching StupAid H2O's own per-level rate, justified by Apex's cheaper `1.12` vs `1.10`
cost multiplier being close enough to a wash) brings Apex to within ~3% of the Apex-excluded baseline
while keeping its unique tap-scaling identity as a bonus on top — no longer *worse* than skipping it.
- **Stage-2 tap balance impact**: minimal and well-contained. This is the same category of change as
  any of the other 8 BP-cost idle buildings (a small `baseBrainPowerPerSecond` addition), not a change
  to tap's own flatness — `02d5ef6`'s Stage-2 tap-funding analysis is untouched since tap income itself
  doesn't change. It would shift the runaway-BP-ladder timing found in section 1 by a small amount
  (one more idle producer in the mix) but doesn't reopen the specific vulnerability that analysis was
  about.
- **Shop description / lore**: needs a small addition, not a rewrite — one line noting steady baseline
  output alongside the existing "hits harder" tap framing, in both the shop `description` field and the
  `PROJECT_BIBLE.md` lore entry.

**(c) Retire the tap-scaler mechanic and repurpose the building**: set `tapBrainPowerPerLevel: 0` and
give it a standalone `baseBrainPowerPerSecond` (comparable range to option (b)'s number, ~0.3–0.5,
since without the tap bonus it needs to fully carry its own weight as a plain idle building at its
cost/mult).
- **Stage-2 tap balance impact**: the largest of the three. This fully undoes `02d5ef6`'s deliverable —
  Apex Brain Greens was that commit's *only* tap-scaling building by design ("becomes the ONLY building
  that scales Brain-Power-per-tap"); removing it means **no building in the game scales tap income at
  all** anymore, tap reverts to flat-forever exactly as it was pre-`02d5ef6`. `UpgradeManager.
  GetTotalTapBrainPowerBonus()` and `UpgradeSlotUI`'s tap-bonus display lines (added in the same
  commit) become permanently dead code (always reads 0) unless separately removed.
- **Shop description / lore**: needs a full rewrite. The current description ("Every tap hits harder.
  The whole feed swears by it.") and the `PROJECT_BIBLE.md` lore entry are both written specifically
  around the tap-power identity; repurposing the building without rewriting them would leave flavor
  text that describes a mechanic the building no longer has.

### Status

**STOP — awaiting Aceyfer's choice of Apex direction (a/b/c) and approval of the Points Shop rescale
numbers above before Phase 2 implementation begins.**
