# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: A satirical idle clicker by AcEclipse Games — tap to earn Brain Power, climb Idiocracy ranks, buy absurd buildings. This task adds a player character + dystopian city stage to the main game view.
- Players: Single player
- Tone / Art Direction: Neon, high-contrast retro-dystopian cartoon (Oswald/Bangers/Anton fonts; hot pink #FF1493, neon green #39FF14, cyan #00FFFF).
- Target Platform: iOS / Mobile
- Screen Orientation / Resolution: Portrait 1080x1920
- Render Pipeline: UniversalRP (2D)

# Game Mechanics
## Core Gameplay Loop
- Tap the ABSORB BRAIN POWER button to earn Brain Power. The on-screen character reacts to taps (squash/stretch) and idles between taps, making the world feel alive.

## Controls and Input Methods
- Touch/click on MainTapButton. No new input — uses the existing PlayerTapHandler.OnTap pipeline.

# UI
All new visuals live inside the existing Screen Space - Overlay Canvas (1080x1920), confined to the bottom "tap section" (canvas fraction y 0.0–0.45), behind the HUD and not overlapping the tap button.
- `WorldRoot` (dystopian city backdrop): container of simple geometric UI Images — a row of dark building rectangles of varying heights with scattered dim "lit window" rects and a faint sickly horizon glow. Sits behind the character and tap button.
- `CharacterRoot` (player character): a UI Image showing a procedurally-generated person silhouette, centered horizontally, standing just above the tap button (feet near the button's top edge), pivot bottom-center so squash/stretch reads as "standing on the ground."
- The purely-decorative `TapStarburst` (added in the prior overhaul) is removed to make room.

## Layout math (verified from scene readback; canvas fraction, bottom=0)
- CurrencyHeader (HUD): 0.875–1.0
- ShopPanel: 0.45–0.875
- EconomyBar: 0.385–0.445
- MainTapButton: 0.069–0.331 (center ~0.20)
- Free band for the character: ~0.331 (button top) → ~0.385 (EconomyBar bottom). Character height kept within this band (~95–105 px) to avoid overlap; the city backdrop spans the full bottom section behind the button as a background layer.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- Existing systems used as-is (NO code changes):
  - `PlayerTapHandler.OnTap()` (on `Canvas/MainTapButton`) already calls `PlayerCharacterController.Instance?.NotifyTap()` every tap, and is wired to `MainTapButton.onClick`.
  - `PlayerCharacterController` (Assets/_Game/Scripts/Systems/PlayerCharacterController.cs): singleton, self-bootstrapping. Fields: `characterVisualTarget` (Transform — animated), `appearanceRenderer` (SpriteRenderer — optional, left null), `appearanceStages` (optional, left empty). `NotifyTap()` → Tapping state → `AnimationController.PlayTapAnim`. Idle → `PlayIdleBreathing`, Bored → `PlayBoredFidget`, Excited (purchases/rebirth/IQ) → `PlayExcitedBounce`. All operate on `characterVisualTarget.localScale`, so a UI RectTransform works.
  - `AnimationController` static helpers already provide the squash/idle/bored/excited animations.
- New asset to generate (procedural, in-editor — no downloads):
  - `Assets/_Game/Sprites/Characters/PersonSilhouette.png` — dark humanoid silhouette (head + torso + legs) with a thin neon-cyan rim so it reads against the dark city; import as Sprite/Single, Uncompressed, alphaIsTransparency.
- City uses flat-colored UI Images only (no sprite). Palette: building bodies #0A0A10 / #12121A / #1A1A22; lit windows dim #C9A227 (amber) and #2E8B57 (sickly green) at low alpha; faint horizon glow a low-alpha purple #2A0A3A.
- Note: an AI city skyline (`Assets/_Game/Sprites/Backgrounds/CitySkylineFar.png`) exists but is intentionally NOT used — the request specifies simple geometric primitives.

# Implementation Steps

### Step 1: Generate the person-silhouette sprite
- **Description**: Editor script draws a simple humanoid silhouette (circle head, tapered torso, two legs) into a 256x512 texture: dark fill (#14141C) with a ~4px neon-cyan (#00FFFF) rim for contrast, transparent elsewhere. Save to `Assets/_Game/Sprites/Characters/PersonSilhouette.png`; set importer Sprite/Single, PPU 100, alphaIsTransparency, Uncompressed.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Build the dystopian city backdrop (WorldRoot)
- **Description**: Create `WorldRoot` under Canvas (sibling index right after `BackgroundRoot`, so it renders behind shop/HUD/button/character). Anchor to the bottom section. Add: (a) a faint horizon glow Image (low-alpha purple) along the skyline; (b) 7–9 building rectangles (UI Images, flat dark colors, varying widths/heights, bottom-aligned, never rising above ~0.40 so they stay clear of the tap button face); (c) ~12–18 small dim "window" rects scattered on buildings. All `raycastTarget=false`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 3: Remove the decorative TapStarburst
- **Description**: Destroy (registered) `Canvas/TapStarburst` to free the bottom-section space, per the approved approach.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 4: Build the character UI element (CharacterRoot)
- **Description**: Create `CharacterRoot` (UI Image, sprite = PersonSilhouette) under Canvas, above `WorldRoot` in sibling order. Anchor center-x; pivot (0.5, 0). Position feet at ~button top (fraction y≈0.331) and cap height to ~0.385 so it doesn't overlap the EconomyBar or tap button. `raycastTarget=false` (taps go through to the button). `preserveAspect=true`.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 3
- **Parallelizable**: No

### Step 5: Wire the character into PlayerCharacterController
- **Description**: If no `PlayerCharacterController` exists in the scene, create one on a dedicated root GameObject `PlayerCharacter` (root, to avoid the DontDestroyOnLoad-on-child warning). Set `characterVisualTarget` = CharacterRoot's Transform; leave `appearanceRenderer` null and `appearanceStages` empty. This makes idle breathing start automatically and tap squash fire via the existing `OnTap → NotifyTap` path — no script edits. (Optional: also set PlayerTapHandler.`tapButtonVisual` = MainTapButton for button juice — only if desired.)
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

# Verification & Testing
- Programmatic readback: confirm CharacterRoot and WorldRoot exist; confirm their pixel bounds do NOT overlap MainTapButton (≤0.331) or CurrencyHeader/EconomyBar (≥0.385); confirm TapStarburst is gone.
- Confirm `PlayerCharacterController` is present, is the singleton, and `characterVisualTarget` points to CharacterRoot; `appearanceRenderer` null is fine.
- Confirm console has no errors/warnings (especially no DontDestroyOnLoad warning).
- Play-mode check: enter Play, confirm the character plays idle breathing at rest; simulate a tap (invoke MainTapButton.onClick / PlayerTapHandler.OnTap) and confirm the character squashes/stretches and returns to idle; capture a Game-view screenshot at portrait to confirm the character sits centered above the button with the city behind it, not overlapping HUD or button. Exit Play.
