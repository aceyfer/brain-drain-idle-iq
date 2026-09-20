# Stage 1 Visual Slice — Implementation Map

**Date:** 2026-09-15  
**Status:** Ready for approval and implementation. Mapping only; no scene, prefab, art, or runtime code changed.

## Objective

Prove `BRAIN_DRAIN_VISUAL_STYLE_GUIDE.md` on one bounded Stage 1 gameplay slice without restyling the entire project. The slice covers:

- world backdrop and foreground separation;
- top resources;
- bottom navigation;
- one BP shop row and one Cash shop row;
- COGS dialogue presentation;
- tap feedback;
- restoration progress;
- one modal/scrim behavior.

## Exact scene and prefab scope

| Slice element | Current scene/prefab authority | Evidence |
|---|---|---|
| Full portrait UI root | `Assets/Scenes/SampleScene.unity` → `CustomSafeArea` | scene line 37632 |
| Stage background | `SampleScene.unity` → `SkylineBG` | scene line 51715; runtime sprite swap in `BackgroundStageView.cs:96-99` |
| Top panel art | `SampleScene.unity` → `TopBG` | scene line 45461 |
| Main BP number | `SampleScene.unity` → `BrainsCounterText` | scene line 15550; wired through `HUDController.cs:27` |
| IQ readout | `SampleScene.unity` → `IQText` | scene line 56368; runtime text/color tags in `HUDController.cs:416` |
| BP/sec | `SampleScene.unity` → `BPPSText` | scene line 33082; wired through `HUDController.cs:30` |
| Cash readout | `SampleScene.unity` → `CashText` | scene line 6620; wired through `HUDController.cs:31` |
| Tap target | `SampleScene.unity` → `MainTapButton` | scene line 499 |
| Shop navigation | `SampleScene.unity` → `ShopButton` | scene line 2339; owned by `MainUIController.cs:16,34-43` |
| Convert navigation | `SampleScene.unity` → `ConvertButton` | scene line 5161; owned by `MainUIController.cs:17,45-49` |
| Restore navigation | `SampleScene.unity` → `RestoreButton` | scene line 18138; owned by `MainUIController.cs:18,51-55` |
| Navigation lower surface | `SampleScene.unity` → `BottomBG` | scene line 55420 |
| Shop backdrop | `SampleScene.unity` → `ShopPanel` | scene line 10045; visibility in `ShopUIController.cs:193-260` |
| Shop content root | `SampleScene.unity` → `ShopRoot` | scene line 42658; resolved/owned in `ShopUIController.cs:77-78` |
| BP tab | `SampleScene.unity` → `Tab_BP`, `Tab_BP_ScrollView` | scene lines 1188 and 895 |
| Cash tab | `SampleScene.unity` → `Tab_Cash`, `Tab_Cash_ScrollView` | scene lines 54966 and 24155 |
| BP/Cash row prefab | `Assets/_Game/Prefabs/UI/UpgradeSlotPrefab.prefab` | instantiated via `ShopUIController.cs:45` |
| COGS dialogue host | `SampleScene.unity` → `COGS_Narrator_Panel` | scene line 6019; shared by `DialogueDisplayUI` and `COGSPortraitController` per `COGSPortraitController.cs:34-37` |
| COGS text | `SampleScene.unity` → `DialogueText` | scene line 35932 |
| Restoration track/fill | `SampleScene.unity` → `RestorationBarTrack`, `RestorationBarFill` | scene lines 8914 and 46939; runtime effects in `HUDController.cs:597-606` |
| Restoration label | `SampleScene.unity` → `RestorationLabel` | scene line 16202; runtime rich text in `HUDController.cs:634-639` |
| General shop dimmer | `MainUIController.shopOverlayShade` | visibility owned by `MainUIController.cs:60-69,261-267` |
| Settings modal | `SampleScene.unity` → `SettingsPanel` | scene line 56252; owned by `SettingsUIController.cs:69-99` |
| Adware interruption | `SampleScene.unity` → `AdwareEventPopup` | scene line 19444 |
| Reward recovery interruption | `SampleScene.unity` → `RewardedAdRecoveryPopup` | scene line 18419 |
| Separate popup canvas | `SampleScene.unity` → `ChaosPopUpCanvas` | scene line 17549 |

## Property ownership: where styling must live

### Safe to author in scene/prefab, subject to validation

- Base sprites, 9-slice types, panel colors, masks, outline components, layout padding, spacing, and non-runtime typography on `TopBG`, `BottomBG`, `ShopPanel`, and the static navigation surfaces.
- Base geometry of the HUD, tap target, restoration track, and static modal framing, provided no legacy editor fix is run afterward.
- `UpgradeSlotPrefab.prefab` hierarchy, padding, image sprites, and layout groups.
- Background crop/mask framing on `SkylineBG`; the sprite itself is swapped by code, but the receiving Image geometry is scene-authored.

### Runtime-owned: changes must be made in code or deliberately stop code ownership

| Property | Runtime owner | Consequence |
|---|---|---|
| SHOP/CONVERT/RESTORE label text | `MainUIController.RefreshButtonFaces`, `MainUIController.cs:274-299` | Inspector label text is overwritten as currency changes. |
| Restore interactable state | `MainUIController.cs:298` | Disabled-state styling must use Button transitions or code-compatible state. |
| Shop tab active states | `ShopUIController.cs:276-287` | Do not style by leaving multiple panels active in scene. |
| Shop tab geometry | `ShopUIController.NormalizeTabGeometry`, called at `ShopUIController.cs:158` | Inspector anchor changes may be overwritten at runtime. |
| Shop Canvas sorting/raycast state | `ShopUIController.cs:280-287,824-838` | Sorting order must be changed at the code owner, not only on Canvas components. |
| Upgrade-row text content and sizes | `UpgradeSlotUI.cs:134-265` | Current hardcoded sizes include 28, 26, 32, and 30. Prefab font-size edits will not survive refresh. |
| Upgrade-row colors | `UpgradeSlotUI.cs:370-385` | Background, name, description, and price colors are code-owned. |
| Cash-row typography/colors | `CashShopSlotUI.cs:84-200` | Hardcoded sizes/colors similarly overwrite prefab presentation. |
| IQ overcharge label/color | `HUDController.cs:416` | Keep rich-text color or replace it at its runtime owner. |
| Restoration fill/glow alpha/color | `HUDController.cs:597-606` | Static Image color is mutated during affordability feedback. |
| Restoration label contents/colors/sizes | `HUDController.cs:634-639` | Embedded rich-text tags are runtime-owned. |
| COGS portrait sprite | `DialogueDisplayUI.cs:128-134` | Frame/crop is safe; image content swaps with COGS stage. |
| COGS panel hidden state | `DialogueDisplayUI.cs:198-205` | Must remain active; visibility is CanvasGroup alpha/raycast state. |
| COGS font size/spacing/font | `DialogueDisplayUI.cs:211-251` | Presentation intentionally changes with Snotting count. Base Inspector values are not authoritative. |
| COGS hot-pink pulse outline | `DialogueDisplayUI.cs:49-67,254-273` | An additional static outline risks doubling effects. |
| Settings open/close and track colors | `SettingsUIController.cs:69-99,125-146` | Static selected colors may be overwritten. |
| Convert copy and font sizes | `ConvertUIController.cs:134-202` | Current sizes are hardcoded during refresh. |

## Existing modal ownership and the stacking risk

`MainUIController` provides mutual exclusion only among Shop, Convert, and Settings (`MainUIController.cs:185-244`). It does not own COGS dialogue, `AdwareEventPopup`, `RewardedAdRecoveryPopup`, `RebirthModal`, `DialogueLogPanel`, or `ChaosPopUpCanvas`.

`DialogueDisplayUI` independently exposes itself whenever `DialogueManager` emits a line (`DialogueDisplayUI.cs:151-176`) and only gates its own CanvasGroup (`DialogueDisplayUI.cs:198-205`). This makes the live overlap observed during inspection structurally plausible: navigation modals and event/dialogue overlays have separate owners.

**Implementation decision for the slice:** do not introduce a new global UI manager yet. Add the smallest existing-controller coordination needed after tracing the two adware/reward popup scripts. The target behavior is:

1. one blocking modal at a time;
2. COGS dialogue may queue but not cover a blocking modal;
3. opening Settings/Shop/Convert dismisses or suppresses non-critical transient dialogue;
4. event/recovery prompts suppress lower-priority navigation panels and dialogue;
5. closing the top overlay restores only the appropriate previous state, never every sibling blindly.

## Legacy editor tools: do not run during the slice

These tools stamp geometry or typography and can overwrite hand tuning:

- `HUDMobileOverhaul.cs` — hardcodes navigation/HUD anchors and text sizes (`:25-36`, `:190-192`, `:263-266`, `:322-325`, `:352-365`).
- `FixCOGSDialogueLayout.cs` — hardcodes narrator panel/portrait/text geometry and font ranges (`:87-160`, `:211-214`).
- `ShopPanelLayoutFix.cs` — hardcodes shop height, close-button geometry, and typography (`:55-58`, `:162-191`).
- `ShopThreeTabWireFix.cs` — reparents/recreates tab structures and hardcodes tab geometry/type (`:75-80`, `:117-160`, `:174-235`).
- `MainUIControllerWireFix.cs` — reparents the background, can create the shade, and stamps geometry (`:107-145`, `:296-313`, `:350-354`).

Before any of these is used again, its constants must be reconciled with the approved style guide and current scene.

## Smallest safe implementation set

### Pass A — prefab/code-owned row styling

Files:

- `Assets/_Game/Prefabs/UI/UpgradeSlotPrefab.prefab`
- `Assets/_Game/Scripts/UI/UpgradeSlotUI.cs`
- potentially `Assets/_Game/Scripts/UI/CashShopSlotUI.cs` only if the Cash tab uses that controller in the active unified shop path; confirm before editing.

Goal: one coherent row design that survives every `RefreshState` call. This is the safest visible proof because it avoids broad scene hierarchy changes.

### Pass B — HUD/navigation semantics

Files:

- `Assets/_Game/Scripts/UI/MainUIController.cs`
- `Assets/_Game/Scripts/UI/HUDController.cs`
- narrowly scoped serialized fields in `Assets/Scenes/SampleScene.unity`

Goal: compact readable top resources, smaller visible button faces with preserved touch targets, correct semantic colors, and no loss of live preview text.

### Pass C — COGS/dialogue frame

Files:

- `Assets/_Game/Scripts/UI/DialogueDisplayUI.cs`
- `Assets/Scenes/SampleScene.unity` only for the existing `COGS_Narrator_Panel` structure

Goal: remove excessive continuous glow, enforce opaque text readability, normalize portrait crop, and preserve CanvasGroup-based hidden state.

### Pass D — overlay coordination

Files: bounded after locating the popup-controller scripts. Expected owners include `MainUIController.cs`, `DialogueDisplayUI.cs`, and the controllers bound to `AdwareEventPopup` / `RewardedAdRecoveryPopup`.

Goal: demonstrate one-modal-at-a-time behavior without a new global subsystem.

### Pass E — Stage 1 presentation QA

No regeneration required. Use the current Stage 1 background as the test source, tune its receiver/crop and overlay contrast, then capture:

- default gameplay;
- BP tab with one affordable and one locked row;
- Cash tab;
- COGS dialogue;
- restoration progress;
- Settings;
- adware/recovery prompt with no competing overlay.

## Proposed visual property targets for the slice

These values are starting targets, not final approvals:

| Component | Current pattern | Slice target |
|---|---|---|
| HUD surface | Near-black content that merges into backdrop | `#080A0D` at 88–94% alpha with a thin cyan diagnostic edge |
| Resource text | Similar-weight dispersed labels | Bone-white values; semantic-color icon/abbreviation; strong number hierarchy |
| Bottom buttons | Large visible blocks | Preserve touch regions; reduce visible icon/label footprint by ~25–35% |
| BP row | Full-row state tint and several bright text lines | Carbon card, cyan identity rail, gold price, green affordability cue only |
| Cash row | Cyan-tinted panel despite Cash semantics | Carbon card, improvised gold/amber identity, green affordability cue |
| COGS frame | Hot-pink animated outline up to 8 px continuously | 1–2 px static frame; brief pulse only on appearance/emphasis |
| Modal scrim | Multiple independent dark layers can stack | One 60–72% `#080A0D` scrim owned by top blocking modal |
| Restoration | Green glow can compete with other accents | Narrow green progress fill; glow only when spend/action is available |

## Approval gate before implementation

Confirm only these two creative choices:

1. Keep cinematic backgrounds + cartoon foreground assets as a deliberate layered style.
2. Use the semantic palette defined in `BRAIN_DRAIN_VISUAL_STYLE_GUIDE.md`.

If both stand, Pass A can begin without another audit.

