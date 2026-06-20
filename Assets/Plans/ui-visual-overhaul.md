# Brain Drain — Complete UI Visual Overhaul (Art & Layout Only)

## Constraints
- NO script/logic edits (.cs files untouched). Art, layout, materials, sprites, animation clips, and component wiring only.
- Scene: Assets/Scenes/SampleScene.unity. Canvas = Screen Space Overlay, 1080x1920 portrait.
- Dynamic states driven by existing scripts (locked/affordable on UpgradeSlotUI) must not require code; style base appearance only and note any script-gated limitations.

## Available assets
- Fonts: Oswald Bold SDF (chunky header), Bangers SDF (Comic-Sans substitute), Anton SDF (Impact), Roboto-Bold SDF.
- Sprite: Assets/_Game/Sprites/UI/StarburstBadge.png (behind tap button).

## Stage 1: Procedural helper assets (no scene conflict)
- VignettePurple.png (radial transparent center -> purple edge), Scanlines.png (tileable), RadialGlowGreen.png, TapRadialPinkPurple.png (circular pink->purple), NeonRing.png (hollow ring), RoundedRect8.png (9-slice rounded card), SoftGlowPink.png, GoldOrangeGradient.png (vertical).
- TMP material presets: Oswald cyan-glow, Bangers pink+purple-shadow, Anton white+black-stroke.
- NeonRing pulse AnimationClip (scale 1.0->1.08, 1s loop) + AnimatorController.

## Stage 2: Backgrounds
- Top (CurrencyHeader area): deep space black + purple vignette + scanline overlay @15%.
- Middle (ShopPanel): dark grimy #1a1a1a + hot pink #FF1493 top border 3px.
- Bottom (tap area): dark + radial green #39FF14 @8% glow centered on tap button.
- Neon green separator between header and middle.

## Stage 3: HUD text
- BrainsCounterText: Oswald Bold, white, 4px cyan #00FFFF glow, fill header width.
- GlobalIQText: Bangers, hot pink #FF1493, 2px dark-purple drop shadow.

## Stage 4: Main tap target
- StarburstBadge layered behind circle; circle fill pink->purple radial gradient; pulsing neon green ring 4px (anim); label white bold caps italic + 3px black stroke; add "TAP TO HARVEST" tiny Bangers hot pink below.

## Stage 5: Building slots (prefab + Library)
- Card bg #111111, neon green #39FF14 left border 4px, rounded corners 8px.
- Name white bold left; Cost yellow #FFD700 right.
- Locked desaturate + padlock icon; Affordable hot pink glow (note: state toggling is script-gated).

## Stage 6: Rebirth button
- Larger; gold #FFD700 -> orange gradient; bold white "REBIRTH" + black stroke; add U+21BA symbol prefix.

## Verification
- Programmatic readback of all changed properties; Game-view/scene capture at 16:9 portrait.
