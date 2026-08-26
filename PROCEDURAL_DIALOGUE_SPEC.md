# Procedural Dialogue System — Phase 1

## Scope note
This system is additive to DialogueManager's existing trigger-based line pool, not a replacement. Existing NarratorLine assets keep firing exactly as they do today; this system adds a second, template-based source of lines that plugs into the same pipeline.

## Channels
"channel" is a new field, not tied to any existing enum. Two values for now:
- COGS: narrator-voice lines. Feeds into DialogueManager exactly like existing NarratorLine assets do.
- STREET: pedestrian ambient chatter. Feeds into ChatterBubble.cs's existing display path, which is separately tuned (different timing constants) from DialogueManager — do not merge their timing logic, only their authoring schema.
Phase 1 migration (step 3) only needs to cover COGS — that's where all 124 existing dialogue assets live today. STREET support can be schema-only for this pass (field exists, no STREET content is migrated).

## Stage
"stage" means a RestorationPercent band, 6 bands numbered 0-5 (not 0-6), using the exact same bands already established for the OfflineDecayReturn welcome-back lines (§34):
- Stage 0: 0-16
- Stage 1: 17-33
- Stage 2: 34-50
- Stage 3: 51-67
- Stage 4: 68-84
- Stage 5: 85-100
minStage/maxStage are authored as these band indices (0-5). At load time, resolve each to the actual minRestorationPercent/maxRestorationPercent range using this table, then gate exactly the way DialogueManager.TryFireLine already gates NarratorLine (currentRestorationPercent >= min && <= max). Do not introduce a second, parallel gating axis — this must resolve to the same RestorationPercent check already live in the code.

## Data schemas + loaders
Word bank JSON per category, fields: id, text, plural, article, minStage, maxStage, weight.
Template JSON, fields: id, channel, text, minStage, maxStage, weight, ending, triggerType, buildingId.
- triggerType: required, must match one of the existing NarratorTriggerType enum values exactly (FirstTap, IQMilestone, Rebirth, BuildingPurchase, EventOutcome, CashConverted, TapWithoutPurchase, OfflineDecayReturn, FirstCashEarned, FirstRestoreSpend, SnottingReady, RestorationStageChange, DailyCapThrottleOnset). This is what lets a migrated template still get picked by DialogueManager's existing trigger-matching in TryFireLine.
- buildingId: optional, empty string means wildcard (matches DialogueManager's existing `string.IsNullOrWhiteSpace(line.buildingId) || line.buildingId == buildingId` check exactly).
Never guess plurals or articles at runtime — the fields are authoritative.
Loaders validate on load and log the offending file and entry id on any malformed record.

## Resolver
Slot syntax: {category}, {category:N} for numbered instances (same N = same word, different N = guaranteed distinct), {category+} for plural, {a category} for article + word, {^category} for capitalize. Modifiers combine, e.g. {^a animal:1}.
- Weighted-random template selection filtered by channel, stage window, triggerType, buildingId, and ending == false.
- Weighted-random word selection per slot filtered by stage window.
- Anti-repeat: there is no existing "ring buffer" class in DialogueManager — the real mechanism is the `history` list (capped at 50 entries, with a last-10 lookback check in TryFireLine). Extend that same pattern with a second lookback list for resolved template-instance strings, same cap/window sizes, rather than inventing a new data structure.
- If the candidate set is empty after anti-repeat filtering, widen by dropping the anti-repeat filter rather than returning null. Never return null or an empty string.
- Optional RNG seed parameter so a bad line can be reproduced.

## Migrate existing dialogue
Every current handwritten NarratorLine asset becomes a zero-slot template that resolves to itself, carrying forward its original triggerType and buildingId unchanged (required — see schema above), and channel = COGS. Compute minStage/maxStage automatically from the asset's existing minRestorationPercent/maxRestorationPercent using the band table above — do not hand-pick stage numbers per line. Do not delete or reword any existing line.

## Tooling
Editor validator: for every stage 0-5 and every channel, assert at least one legal template exists, and for every category referenced by those templates, at least one legal word exists in that stage window. Fail loudly. Wire it into the existing validator setup.
Editor preview window: choose stage and channel, button dumps 50 resolved lines.

## Integration constraint
The resolver output feeds the existing length-aware display timing path (DialogueManager.Display). Nothing downstream of the resolver should need to change. Do not touch scene files.
