# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: An idle clicker game where players tap to earn Brains and restore decaying IQ, upgrading various satirical 2000s and retro-style elements.
- Players: Single player
- Inspiration / Reference Games: Cookie Clicker, early internet browser adware pop-ups, Retro clickers.
- Tone / Art Direction: Neon, high-contrast, chaotic retro 2000s cartoon/cel-shaded style.
- Target Platform: iOS / Mobile
- Screen Orientation / Resolution: Portrait 1080x1920 (16:9)
- Render Pipeline: UniversalRP (URP)

# Game Mechanics
## Core Gameplay Loop
- Players tap to generate Brains/IQ, advancing levels and unlocking retro brain-drain devices.
- The "Brain-Juice Extraction Machine" will be used as a high-tier idle upgrade building or visual backdrop element.

## Controls and Input Methods
- Touch input on upgrade slots to purchase levels of the extraction machine.

# UI
The generated sprite will be displayed in the UI inside the Shop scroll list as an icon/artwork for a high-tier building slot, or placed in the Diorama backdrop.

# Key Asset & Context
- Active Scene: `Assets/Scenes/SampleScene.unity`
- Asset to generate: `Assets/_Game/Sprites/Buildings/BrainJuiceExtractor.png`
- Import Settings:
  - Texture Type: Sprite (2D and UI)
  - Sprite Mode: Single
  - Pixels Per Unit: 100
  - Filter Mode: Point (no filter) or Bilinear
  - Compression: Uncompressed (to avoid banding on glowing neon pink colors)

# Implementation Steps

### Step 1: Model Discovery
- **Description**: Query the AI asset generation engine for available image/sprite generation models. Identify a model that supports high-quality 2D cartoon assets (e.g. Seedream 4, Gemini 3.0 Pro, or GPT Image 1).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Generate Brain-Juice Extraction Machine Sprite
- **Description**: Call the asset generation tool with the optimized prompt to create the cartoon brain-juice extraction machine.
  - **Prompt**: "2D cartoon game sprite of an industrial brain-juice extraction machine. Style: thick black outlines, Saturday-morning cartoon aesthetic, cel-shaded flat colors. Large transparent tank filled with glowing pink liquid, chrome pipes and valves on the sides, neon sign reading 'INSERT BRAIN HERE' on the front, cartoon drinking straw at bottom. Isolated on transparent background. Square canvas. No shadows. Colors: hot pink #FF1493, chrome silver, neon green accents."
  - **Target resolution**: 1024x1024 (scaled to fit square canvas perfectly)
  - **Save Path**: `Assets/_Game/Sprites/Buildings/BrainJuiceExtractor.png`
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Remove Background from Generated Sprite
- **Description**: Run background removal on the generated sprite `Assets/_Game/Sprites/Buildings/BrainJuiceExtractor.png` to ensure absolute transparency around the outlines of the machine.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Configure Sprite Import Settings
- **Description**: Set Texture Importer properties on `BrainJuiceExtractor.png` so it renders sharply as a pixel-perfect 2D cartoon asset inside Unity.
  - Texture Type -> Sprite (2D and UI)
  - Filter Mode -> Point (filter) or Bilinear
  - Compression -> Uncompressed (to keep hot pink `#FF1493` glowing and clean)
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

# Verification & Testing
- Locate the generated sprite `BrainJuiceExtractor.png` in the Unity Project panel under `Assets/_Game/Sprites/Buildings/`.
- Inspect the sprite using the Inspector window or scene preview:
  - Verify it has thick black outlines, a transparent background, a neon sign reading "INSERT BRAIN HERE", a straw at the bottom, and glowing hot pink (#FF1493) liquid.
  - Verify the dimensions are 1024x1024 (or scaled appropriately) and there is no compression artifacting.
