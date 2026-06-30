# Project Overview
- **Game Title:** Brain Drain
- **High-Level Concept:** An incremental/clicker mobile game themed around brain rot, IQ decay, and reclaiming human intellect to restore a dystopian society into a utopia.
- **Players:** Single player
- **Inspiration / Reference Games:** Adventure Capitalist, Cookie Clicker, Universal Paperclips
- **Tone / Art Direction:** Satirical, cyberpunk neon, meme-infused retro pixel art.
- **Target Platform:** iOS (Mobile)
- **Screen Orientation / Resolution:** Portrait / Responsive Mobile
- **Render Pipeline:** Universal Render Pipeline (URP)

# Game Mechanics
## Core Gameplay Loop
The player taps to generate IQ / currency, spends currency on upgrades to increase passive income (BPPS - Brain Power Per Second), unlocks chapters, and progresses through restoration stages. As the world restoration climbs from 0% to 100%, the environment and pedestrians transition from a brain-dead "slack-jawed" dystopian state to an "engaged" utopian society.

## Controls and Input Methods
The game features intuitive tap inputs on UI elements (Shop slots, upgrade buttons, and a central character/tap target) with satisfying physical visual feedback (squash/stretch, neon ring ripples, and reward text floats).

# UI
Not directly modified in this task. However, the background contains spawned pedestrian UI elements and world-space pedestrians that will utilize the newly created animators and sprites.

# Key Asset & Context
### Discovered Sprites in `Assets/_Game/Sprites/Pedestrians/`
- **Stage 1 (JPG):** `Ped1_Stage1.jpg` through `Ped6_Stage1.jpg`
- **Stages 2-6 (PNG):** `Ped1_Stage2.png` through `Ped6_Stage6.png`

### Target Assets to Create
- **Animator Controllers:** `Assets/_Game/Animators/Pedestrians/Ped1_AnimController.controller` through `Ped6_AnimController.controller`
- **Animation Clips:** `Assets/_Game/Animators/Pedestrians/Clips/Ped{i}_Walk_Stage{j}.anim` (36 clips total)

### Class/Method context for automated pipeline
We will write an editor script to execute these changes:
- `TextureImporter` setup code for sprite settings.
- `AnimatorController` setup with `StageIndex` parameter and "Any State" transition logic.
- `AnimationClip` generation utilizing both `SpriteRenderer` and `UnityEngine.UI.Image` `m_Sprite` curve bindings for maximum compatibility.

---

# Implementation Steps

### Step 1: Convert Stage 1 JPGs to PNG Format
- **Description:** For each of the 6 pedestrians, load `Ped{i}_Stage1.jpg`, save it as `Ped{i}_Stage1.png` inside `Assets/_Game/Sprites/Pedestrians/`, delete the old `.jpg` and `.jpg.meta` assets, and refresh the asset database.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No

### Step 2: Configure Sprite Import Settings
- **Description:** Find all PNG files from `Ped1_Stage1.png` through `Ped6_Stage6.png` (36 files total). Set each sprite's import settings:
  - Texture Type: Sprite (2D and UI)
  - Pixels Per Unit: 100
  - Filter Mode: Point
  - Compression: None
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** No

### Step 3: Create Single-Frame Animation Clips
- **Description:** Create 36 single-frame `AnimationClip` assets at `Assets/_Game/Animators/Pedestrians/Clips/`. Each clip will bind `m_Sprite` on both `SpriteRenderer` and `UnityEngine.UI.Image` to the respective sprite asset.
- **Assigned role:** developer
- **Dependencies:** Step 2
- **Parallelizable:** No

### Step 4: Create and Configure Animator Controllers
- **Description:** Create 6 Animator Controllers (`Ped1_AnimController` to `Ped6_AnimController`) in `Assets/_Game/Animators/Pedestrians/`.
  - Add integer parameter: `StageIndex` (default value: 1).
  - Add 6 states: `Walk_Stage1` to `Walk_Stage6`.
  - Assign the corresponding `AnimationClip` to each state's motion.
  - Set `Walk_Stage1` as the default state.
  - Create transitions from **Any State** to each state `Walk_Stage{j}` with the condition `StageIndex == j` (instant transition: `duration = 0`, `hasExitTime = false`, `canTransitionToSelf = false`).
- **Assigned role:** developer
- **Dependencies:** Step 3
- **Parallelizable:** No

---

# Verification & Testing
### Automated Script Compilation Check
- Run a quick compilation check in Unity to ensure no syntax errors in the editor setup script.

### Asset Integrity Audits
- Verify all 36 PNG files exist and have the correct Pixels Per Unit (100), Point Filter, and No Compression settings.
- Verify all 6 Animator Controllers exist in `Assets/_Game/Animators/Pedestrians/`.
- Inspect one of the controllers (e.g., `Ped1_AnimController`) to verify:
  - `StageIndex` (Int) parameter exists.
  - 6 states exist with proper clips assigned.
  - 6 "Any State" transitions exist with matching `StageIndex == X` conditions.

### Play Mode Verification (Optional but Recommended)
- Ensure no runtime/console warnings are generated during startup or scene loading.
