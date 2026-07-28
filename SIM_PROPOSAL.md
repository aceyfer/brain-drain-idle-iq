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
