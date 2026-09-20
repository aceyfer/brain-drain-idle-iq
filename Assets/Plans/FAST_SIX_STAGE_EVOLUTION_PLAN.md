# Fast Six-Stage Evolution Plan

**Date:** 2026-09-15  
**Decision:** Ship the six current Leo/Leonardo backgrounds. Do not commission or regenerate a six-image set for launch polish.

## Outcome

Brain Drain already has enough world-evolution art to ship. The quickest credible treatment is a stable game frame with six milestone reveals, not six scenes rebuilt to look like the same exact street.

The current images already read as:

| World index | Existing asset | Immediate read | Launch decision |
|---:|---|---|---|
| 0 | `Assets/_Game/Sprites/Backgrounds/BG1.jpg` | isolated cryotube / black industrial room | Keep. This is Leo's integrated opening composition; do not add another tube. |
| 1 | `Assets/_Game/Sprites/Backgrounds/BG2.png` | advertising-saturated dystopian megacity | Keep. Strongest satirical identity image. |
| 2 | `Assets/_Game/Sprites/Backgrounds/BG3.png` | rainy city under repair, cranes and scaffolded tower | Keep. Clear first recovery step. |
| 3 | `Assets/_Game/Sprites/Backgrounds/BG4.png` | damaged city with gardens and organized cleanup | Keep. Clear nature/community return. |
| 4 | `Assets/_Game/Sprites/Backgrounds/BG5.png` | clean, green, modern city | Keep. Clear restored-world step. |
| 5 | `Assets/_Game/Sprites/Backgrounds/BG6.png` | golden solarpunk utopia | Keep. Strong final payoff. |

Stage lore is supporting copy, not art direction law. The world can move between districts, camera feeds, or time jumps. It does not need matching landmarks in all six images. If copy and the strongest art disagree, bend the copy.

## Commercial comparison

The profitable/downloaded idle games checked do not depend on a new cinematic backdrop for every progression tier:

- [Egg, Inc.](https://apps.apple.com/us/app/egg-inc/id993492744) is free with IAP and has a 4.8 rating from roughly 561K App Store ratings. Its evolution is legible because the farm/camera grammar persists while facilities and egg tiers improve.
- [Idle Miner Tycoon](https://play.google.com/store/apps/details?id=com.fluffyfairygames.idleminertycoon) is free with ads/IAP and shows 100M+ Google Play downloads. It reuses a familiar mine layout and communicates advancement through unlocked mines, resources, color, and upgraded machinery; Kolibri says its portfolio has passed [250M downloads](https://www.kolibrigames.com/).
- [Idle Theme Park Tycoon](https://play.google.com/store/apps/details?id=com.codigames.idle.theme.park.tycoon) is free with ads/IAP and shows 10M+ Google Play downloads. Its visual proof is modular expansion and upgraded attractions inside a stable interaction grammar.
- [AdVenture Capitalist](https://apps.apple.com/us/app/adventure-capitalist-tycoon/id927006017) is free with IAP and demonstrates the minimum case: strong numeric/UI progression can carry the loop with little environment evolution.

The transferable lesson is not to imitate their art style. It is to keep the interaction frame stable, make each milestone instantly distinguishable, and celebrate the state change. Brain Drain already exceeds the minimum art requirement with six distinct full-screen rewards.

## What the existing game already does

- `Assets/_Game/Scripts/Systems/WorldRestorationManager.cs:174-186` spends Restoration Points and resolves the stage; `:269-271` publishes one authoritative stage-change event.
- `Assets/_Game/Scripts/UI/BackgroundStageView.cs:54-64` listens to that event, but `:95-100` currently hard-swaps the Canvas background.
- `Assets/Scenes/SampleScene.unity:51789-51796` wires all six current background sprites into that Canvas view.
- `Assets/_Game/Scripts/UI/AccentBarStageView.cs:56-73` changes the top-bar art from the same event.
- `Assets/_Game/Scripts/UI/WeatherAtmosphereView.cs:28-35` supplies a six-step haze/clarity arc, and `:61-87` follows the same stage event.
- `Assets/_Game/Scripts/UI/UniversalButtonBorderApplier.cs:234-248` changes the global button theme from the same stage event.
- `Assets/_Game/Scripts/UI/HUDController.cs:450-474` already plays a restoration-bar milestone surge on stages 1-5.
- `Assets/_Game/Scripts/Systems/BackgroundPedestrianManager.cs:180-189` changes pedestrian behavior across five restoration-percentage bands. Its sprite selection intentionally remains Stage-1-only (`:255-340`), so new pedestrian sets are not required for this ship pass.
- COGS portrait evolution is separate and rebirth-driven, not part of the six world images: `Assets/_Game/Scripts/Systems/COGSPortraitController.cs:96-101` and `:137-176`.

This is already a multi-channel evolution system. The missing polish is a deliberate reveal connecting those channels.

## Live portrait QA result

Codex used the existing Editor-only stage buttons in Play Mode at `16:9 Portrait` and stepped through indices 0-5 on 2026-09-15. All six backgrounds read clearly at their settled stage. BG1-to-BG2 reads as a purposeful inside-to-outside jump; BG2-to-BG6 reads as a strong decay-to-utopia arc. No replacement art was justified.

Three implementation findings matter more than new art:

1. **The visible background hard-swaps.** This matches `BackgroundStageView` assigning the new `Image.sprite` directly (`Assets/_Game/Scripts/UI/BackgroundStageView.cs:95-100`). The existing stage dialogue then covers the upper portion of the new image for four to five seconds (`Assets/_Game/Dialogue/RestorationStageChange_Stage1_ZoningComplaint.asset:17-23` through `RestorationStageChange_Stage5_WellnessRetreat.asset:17-23`). A second title toast would be redundant and would increase overlay noise.
2. **The HUD stage name is one event behind after a threshold crossing.** `WorldRestorationManager` invokes `OnRestorationProgressChanged` before updating `CurrentStage` and invoking `OnRestorationStageChanged` (`Assets/_Game/Scripts/Systems/WorldRestorationManager.cs:181-185`, `:269-271`). `HUDController` refreshes the stage-name text only from the progress event (`Assets/_Game/Scripts/UI/HUDController.cs:263-272`, `:585-650`); its stage handlers only mark rank dirty and play the bar surge (`:445-474`). Live QA showed the previous name above the newly swapped background. Refreshing the existing restoration text from the stage-change path is P0.
3. **Stage 0's outdoor-smog tint muddies the black-room art.** The live cryotube is washed olive/brown by the hard-coded Stage-0 haze (`Assets/_Game/Scripts/UI/WeatherAtmosphereView.cs:28-35`). Test a neutral/transparent Stage-0 haze while retaining the current Stage-1 smog. This is a presentation correction, not a new asset request.

## Smallest coherent launch system

Think in three production beats while retaining six player rewards:

1. **Awake / Collapse:** BG1, BG2
2. **Rebuild:** BG3, BG4
3. **Renew:** BG5, BG6

Each of the six stages needs only:

1. one unmistakable environmental state (already present),
2. one palette/atmosphere step (already present), and
3. one milestone name plus a short reveal (name exists; reveal needs QA/polish).

The BG1-to-BG2 discontinuity should read as an intentional reveal from the sealed room to the outside world. No intermediate background is needed. The existing names `Cryo Chamber` and `Smog-Choked Sprawl` are already displayed by `Assets/_Game/Scripts/UI/HUDController.cs:595-650`; a short transition treatment can do the rest without making lore binding.

## Ship / defer line

### P0 — launch polish

1. Preserve BG1-BG6 and their current scene wiring.
2. Make the visible Canvas background transition deliberate: approximately 0.5-0.8 seconds, using a fade-through-dark or crossfade. Do not add a seventh image.
3. Fix the restoration HUD label so it refreshes after `CurrentStage` is updated; it currently displays the previous stage over the new background.
4. Remove or neutralize the brown Stage-0 haze if the black-room comparison confirms the current wash is reducing contrast. Keep the Stage-1 smog treatment.
5. During the same transition, verify the stage name, haze, top accent, button theme, and restoration surge settle to the same index.
6. Playtest all six thresholds using the existing debug stage jumps at two portrait sizes. Capture one screenshot per settled stage and one short recording of a stage change.
7. Fix only concrete readability/crop defects found in that pass. Do not regenerate a background because an old prompt or stage name describes something else.

### P1 — only if the transition still feels weak

- Add one shared sound sting and reuse it at all five crossings with modest pitch/intensity variation.
- Only consider a separate stage-title card if the corrected HUD label plus existing narrator dialogue still fails in testing. The live pass indicates it is probably unnecessary.

### Defer until after launch

- parallax layers;
- animated background scenery;
- six matching landmarks or a same-street repaint;
- stage-specific pedestrian sprite pools;
- new COGS portraits;
- extra intra-stage backgrounds;
- any new cryotube asset.

## Two technical cautions for the implementer

1. There are two serialized backdrop paths. `WorldRestorationManager` crossfades six world-space `SpriteRenderer`s (`Assets/_Game/Scripts/Systems/WorldRestorationManager.cs:143-166`; wired at `Assets/Scenes/SampleScene.unity:13853-13860`), while the visible Canvas `BackgroundStageView` enables its own `Image` and instantly swaps it (`Assets/_Game/Scripts/UI/BackgroundStageView.cs:18-24`, `:88-100`; wired at `Assets/Scenes/SampleScene.unity:51740-51796`). Confirm which is visible in Game view before changing anything. Avoid running two competing transition implementations.
2. All six full-size sprites are held by serialized arrays. BG2-BG6 are roughly 2.1-3.7 MB each as source PNGs, and their `.meta` files contain no explicit Android platform override (`Assets/_Game/Sprites/Backgrounds/BG2.png.meta:72-111` through `BG6.png.meta:72-111`). Source file size is not runtime memory or final AAB size. Check Unity's Android import preview/build report before changing format; do not infer a launch problem from PNG byte size alone.

## Pacing check, not an automatic retune

The authored thresholds are 0; 20,000; 2,132,885; 5,658,229; 20,764,149; and 250,000,000 RP (`Assets/_Game/Restoration/WorldRestorationStage_0_ToxicWasteland.asset:15-17` through `WorldRestorationStage_5_UtopiaAchieved.asset:15-17`). Numerically, Stage 4 begins at only 8.31% of the final total and occupies the long gap to Stage 5.

That ratio does **not** prove poor time pacing because idle income is exponential. It does create a required QA question: record real or simulated time-to-stage. If BG1-BG3 flash by or BG5 dominates the playtime, adjust economy pacing later; do not solve timing with more art.

## Claude Code continuation contract

When Claude returns, it should not re-audit the art or regenerate the six backgrounds. It should:

1. read this file and `Assets/Docs/CODEX_VISUAL_STYLE_HANDOFF_2026-09-15.md`;
2. answer in the handoff which backdrop path is actually visible in Game view and whether the world-space path is redundant;
3. implement only the smallest transition on that visible path, preserving all six sprite references;
4. correct the HUD stage-name event-order lag and evaluate a neutral Stage-0 haze before adding any presentation feature;
5. record stage-by-stage screenshots, crop/readability failures, approximate time-to-stage, compiler state, and exact files changed;
6. explicitly answer whether a title toast was necessary after testing the transition; current evidence says no;
7. leave all deferred items deferred unless Aceyfer changes scope.

The acceptance test is simple: a player who misses the label can still identify that the world improved, and a player who sees the label experiences one clean, synchronized milestone rather than an abrupt image swap.
