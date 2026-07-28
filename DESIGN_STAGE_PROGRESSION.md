# Stage Progression Redesign -- Design Brief (2026-07-27)

## Problem
Codex confirmed (see CODEX_FINDINGS.md): flat 1-BP tapping into Snott Market Exchange reaches Stage 2 in 37-47 min of active tapping -- far too fast. Root cause: taps are an endless BP faucet funding an overtuned cash engine. SME pumps 5 Cash/s per level at a 0.1 conversion rate against a 10,000 RP threshold (= 100,000 Cash). SME baseCost is a WEAK lever (taps outrun it). The real dials are: SME cash/s/level (local), the stage thresholds (global pacing), and pointsConversionRate (global -- AVOID, it punishes all restoration everywhere). Not caused by the Apex Brain Greens tap feature -- Codex reproduced it with zero Greens.

## Target pacing (design intent; Claude Code back-solves exact numbers via sim)
Model: MapleStory Ludibrium-era exp curve -- each level ~hours of grind, escalating per level. Each restoration stage should take progressively longer, ratio growing ~2-3x real-time per step.
- Stage 1: ~30-60 min active (quick hook, per SS11 bar)
- Stage 2: several days of 30-min sessions for a normal player; ~1 day for a dedicated grinder is acceptable
- Stage 3: ~1 week
- Stage 4: ~2-3 weeks
- Stage 5: ~1 month+
- Stage 6: NOT reachable by any player within the first month; multi-month grind
Current thresholds (2,500 / 10,000 / 50,000) only cover S1-S3 and are far too shallow at the top.

## Open sub-problems (each needs its own decision)
1. Tap income cap. Pure-tapping must not trivialize S3+. Options: (a) diminishing tap value under sustained tapping, recovering when idle (anti-autoclicker); (b) per-session/daily soft cap on tap-BP. Grinding should help but hit limits. Grind-all-day to Stage 2 in a day stays acceptable.
2. Retention dialogue. Each stage should unlock new COGS/Literates lines + a visible world change, as the return hook. Content workstream (more dialogue to write).

## Implementation direction for the eventual fix
- Primary lever: cut SME cash/s/level (overtuned for a 500-BP-unlock building). Local; doesn't punish other players.
- Rescale all six stage thresholds to the target curve above.
- Do NOT touch pointsConversionRate (global; slows every player's restoration).
- Claude Code to run the per-second economy sim (as in the SS22 / tap-balance passes) to back-solve exact SME cash/s + the six thresholds that hit the target times, then report before any asset change.

## Status
Design brief only. No balance values changed yet. Next session: pick the tap-cap approach, then have the sim back-solve numbers.
