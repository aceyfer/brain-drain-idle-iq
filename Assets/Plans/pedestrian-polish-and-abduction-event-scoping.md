# Pedestrian Animation Polish + Spaceship Abduction Event — Scoping

*Scoped 2026-09-22, from the idea Aceyfer captured the same day (TASKLIST.md PARKED). Grounded directly against the live `BackgroundPedestrianManager.cs` and `RandomEventManager.cs`/`BrainRotEventData.cs` — not guessed. Design/scoping only, nothing built yet; this is what "not scoped, not started" in the PARKED note is meant to be resolved by.*

## Part 1 — What "pedestrian animations" actually are today

Worth being precise about this before talking about "polishing" it, because there's no frame-based animation to polish yet: `BackgroundPedestrianManager` pedestrians are a single static `Sprite` per pedestrian (`Image.sprite`, set once at spawn), not a sprite-sheet walk cycle and not an `Animator`. Everything that reads as "movement" is procedural math in `MoveRoutine`:

- Horizontal walk: constant velocity, `speed * StageSpeedMultiplier[stage]` (0.5x–1.5x across the 5 `PedestrianBehaviorStage` bands).
- Vertical "step" bounce: `Mathf.Abs(Sin(elapsed * wobbleFrequency)) * wobbleVerticalAmplitude * StageWobbleAmplitudeMultiplier[stage]` — a one-directional bounce, fading out band by band (1.0x at SlackJawed down to 0x at Engaged).
- Rotation "stumble" tilt: `Sin(...) * wobbleRotationAmplitude * multiplier`, same fade.
- Shuffling-band-only stumble: 1% per-frame chance to freeze for 0.3s (`StumbleChancePerStep`).
- Engaged band ("god stage"): the wobble/tilt above is fully replaced by a symmetric hover (sine on Y, gentle sway on rotation) — no walking at all.
- Left/right facing is a flipped `localScale.x`, not a mirrored sprite or animation.
- No idle animation, no arrival/departure animation (pedestrians just appear off-screen and get `Destroy()`-ed the instant they cross the far edge), no per-pedestrian personality beyond which of the 6 prefab sprites got picked.

So "polish the animations" could mean several genuinely different projects depending on which of these Aceyfer actually means:

1. **Replace the static sprite with a real walk-cycle** (sprite-sheet or per-frame swap) — the biggest lift, needs new art (multiple frames per pedestrian, likely per stage-1..6 art too) and an `Animator`/manual frame-swap layer under the existing procedural bounce, not instead of it necessarily.
2. **Keep static sprites, improve the procedural motion feel** — easing curves instead of raw sine, squash/stretch on the bounce, variable per-pedestrian speed/gait so they don't all move identically, a subtle shadow under each pedestrian. Much smaller lift, no new art required, pure code tuning.
3. **Add reaction/state animations** — e.g. a distinct pose or flinch when an event affects them (ties directly into Part 2 below), or varied idle micro-animation while a chatter bubble is up (right now the pedestrian keeps walking/wobbling identically while "talking").
4. **Visual variety, not motion** — recolors/outfit variants so 6 prefabs don't feel so repetitive at high pedestrian density.

**Question for Aceyfer before any of this is scoped further:** which of these (or something else entirely) is the actual complaint? "Polish" is too open to start building against — worth a quick back-and-forth rather than guessing and building the wrong one.

## Part 2 — Spaceship abduction as a random event

This one has a clear, buildable shape, because it slots into an existing pattern rather than inventing a new system.

### Where it fits

`RandomEventManager` already does exactly the "periodic satirical thing happens, player sees a popup" job (§51/§63 in TASKLIST_DETAILS.md) — a `BrainRotEventData` ScriptableObject pool, fired every 90–180s via `GameManager.OnSecondTick`, deferred while an FTUE card is up. Today every event is currency/IQ-only with no visual effect beyond whatever `RandomEventUIController`'s popup shows. An abduction event would be the first one with an actual world-space visual beat layered on top of the popup, not a replacement for it.

### Proposed shape

1. **New `BrainRotEventData` field**, e.g. `bool triggersPedestrianAbduction` (or a small enum if more visual-beat types are wanted later, but a bool is enough for one effect) — keeps this purely additive to the existing asset type, no breaking change to the 8+ existing event assets (§33 catalog).
2. **New listener**, most naturally on `BackgroundPedestrianManager` itself (it already owns `activePedestrians` and the spawn/despawn lifecycle) subscribing to `RandomEventManager.OnRandomEventTriggered`: when the fired event has `triggersPedestrianAbduction` set, pick a random live `activePedestrians` entry (empty-list case: just skip the visual, the currency/IQ effect and popup still fire normally — never block the core event on "no pedestrian happened to be on screen").
3. **The beat itself**, roughly: spawn a new UFO/spaceship sprite above the chosen pedestrian, play a short tractor-beam-down effect (could reuse `Image` + a simple scale/alpha tween, matching how cheaply `RainEffectView`/`WeatherAtmosphereView` (§61) already do UI-native effects rather than a real `ParticleSystem` — same Screen-Space-Overlay Canvas constraint applies here too, so this should be another UI-native effect, not a raw prefab drop), pedestrian scales down/fades while "beamed up," then the ship exits off the top of the screen. Reuses the exact removal path `MoveRoutine` already uses at the end of a normal walk (`activePedestrians.Remove` + `Destroy`), just triggered early and dressed up.
4. **Timing relative to the popup:** simplest and safest is to fire the visual beat immediately on `OnRandomEventTriggered` (same moment the popup would appear), independent of whether the player accepts/declines the popup — the abduction is flavor for an event that's already been rolled, not a consequence of the player's choice. Worth Aceyfer's sign-off since it's a specific interpretation, not the only valid one.

### What's genuinely new work vs. reuse

- **Reused as-is:** `RandomEventManager`'s scheduling/pooling, `BrainRotEventData`'s currency/IQ effect application, `activePedestrians`' lifecycle/removal pattern.
- **New code:** the field on `BrainRotEventData`, the subscription + pedestrian-picking + visual-beat coroutine (probably lives in `BackgroundPedestrianManager` or a small new sibling component to keep that class from growing further — it's already 20KB/465 lines).
- **New art, not code-generatable:** a UFO/spaceship sprite and a beam effect — same category as §55's mesh-overlay finding (organic/novel visual content needs real generative art, not procedural C#). Leonardo.ai spec would follow the same pattern as `Assets/Plans/leonardo-mesh-overlay-spec.md` and `world-restoration-backdrop-art-spec.md` once Aceyfer confirms the visual direction (realistic sci-fi beam? cartoonish flying-saucer-with-googly-tractor-beam, matching the game's satirical tone? — worth deciding before generating art, not after).

### Open questions before this gets built

1. Does Part 1's "polish" and this event share any dependency (e.g. if Part 1 turns into a real walk-cycle/Animator rewrite, should the abduction beat wait for that so it doesn't get built twice)? Recommend: no — the abduction beat only needs the existing static-sprite pedestrian to scale/fade, which works identically before or after any Part 1 animation upgrade.
2. Should abduction be its own new `BrainRotEventData` asset (a "UFO Sighting" style event with its own title/flavor text) or a flag that could apply to an *existing* event as a visual flourish? Recommend a new dedicated event — simpler, and the existing 8+ events' text was written without this beat in mind.
3. Frequency: should this be rare/special (a low-weight pool entry, or a separate rarer timer) rather than appearing as often as a normal 90–180s event? A recurring "everyone keeps getting abducted" might undercut the novelty. Aceyfer's call.
4. Any gameplay effect at all beyond spectacle (e.g. removing a pedestrian slightly reduces chatter-bubble frequency for a moment since the pool is smaller) — or purely cosmetic, no mechanical effect? Recommend purely cosmetic to start, matching how §61's weather system was scoped as "no economy or gameplay effect."

## Next step

This is a design doc, not a build queue — next move is Aceyfer answering Part 1's framing question and Part 2's four open questions (or just saying "go with your recommendations" for Part 2, which are called out above), then this can turn into an actual implementation task with a real commit, same as every other §-numbered item in TASKLIST.md.

## Resolved 2026-09-22 — written the same day

Aceyfer answered directly rather than picking from the options above:

- **Beam visual:** "like the rapture on This Is the End movie" — a bright descending light shaft, comedic tone. Used as a style/tone reference only in the implementation's doc comments (not a reproduction of that film's actual footage/art — same guardrail this project already applies to its other film/game references, e.g. the DBZ-aura and Overwatch-Moira PARKED ideas).
- **Animation:** "spin like a cartwheel when a ufo gets abducted" — this turned out to answer Part 1's framing question too: the ask was specifically the abducted pedestrian's spin, not a general walk-cycle overhaul. Implemented as fast continuous rotation (1080°, i.e. 3 full spins, over the ~2.2s abduction) rather than the walk-cycle's gentle procedural tilt.
- **Frequency:** "every 10-20 mins" — resolves open question 3 decisively toward "separate rarer timer," not a pool entry in the 90-180s `RandomEventManager` cycle. This also settled open question 2 in the same direction: since it's not going through that pool at all, there's no "new event asset vs. flag on an existing one" choice to make.
- **Tone:** "keeping it funny" — matches open question 4's cosmetic-only recommendation; implemented with zero currency/IQ/economy effect.

**Implemented same day:**
- `BackgroundPedestrianManager.cs` — added `TryStealRandomPedestrianForEffect(out RectTransform)` (cancels that pedestrian's own `MoveRoutine` coroutine so it stops fighting the abduction animation for the RectTransform, matching the per-pedestrian coroutine-tracking this required adding) and a `ContainerRect` getter, both purely additive to the existing class.
- `Assets/_Game/Scripts/Systems/UfoAbductionController.cs` (new) — the timer (10-20 real minutes, independent of `RandomEventManager`) + the full visual sequence (UFO descends, beam fades in, pedestrian cartwheels/shrinks/rises into it, UFO exits). Deliberately NOT routed through `BrainRotEventData`/`RandomEventManager` — no popup, no player choice, closer to `WeatherManager`'s ambient-timer pattern (§61) than to a chaos event.
- UFO and beam art are procedurally drawn at runtime (no image-gen tool available in this session, same constraint as §55d/§60) — a flying-saucer silhouette is geometric enough to read fine without hand-drawn art. Real Leonardo-generated art can swap in later by only touching the two texture-builder methods; nothing else would need to change.
- `Assets/_Game/Scripts/Editor/UfoAbductionWireFix.cs` (new) — idempotent `BrainDrain > Fix UFO Abduction Event` menu tool, same pattern as `WeatherSystemWireFix`/`RewardedAdRecoveryWireFix`, wires the new controller under `_Systems` and points it at the scene's `BackgroundPedestrianManager`.
- `DebugCheats.cs`/`TestingMenuShortcuts.cs` — added `Force UFO Abduction Cooldown Elapsed`, mirroring the existing `Force Random Event Cooldown Elapsed` hook, so this doesn't require sitting through a real 10-20 minute wait to verify.

**UNVERIFIED, same caveat as every blind-written pass this session:** no Unity Editor/compiler access from this session (file-bridge only). Unlike §12's UGS work, no new packages or unfamiliar external APIs are involved here — it's plain `UnityEngine.UI`/`Coroutine` code following `BackgroundPedestrianManager`'s and `WeatherManager`'s existing patterns exactly — so the risk profile is much lower, but this still needs a real compile pass, then running `BrainDrain > Fix UFO Abduction Event` once, then `BrainDrain > Testing > Force UFO Abduction Cooldown Elapsed` in Play Mode to actually see it.

**Still open / deliberately not done:** no narrator/COGS one-liner hook on this event (flagged as a natural future addition, not built to avoid scope creep beyond what was asked); no settings toggle to disable it (same "fast-follow, not forgotten" treatment §61's rain got); real generated art is still a placeholder swap away, not required to ship.
