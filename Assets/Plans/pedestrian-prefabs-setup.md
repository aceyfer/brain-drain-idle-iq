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
The background contains spawned pedestrian UI elements and world-space pedestrians that will utilize the newly created animators, sprites, and prefabs.

# Key Asset & Context
### Discovered Sprites in `Assets/_Game/Sprites/Pedestrians/`
- **Stage 1 (PNG):** `Ped1_Stage1.png` through `Ped6_Stage1.png`
- **Stages 2-6 (PNG):** `Ped1_Stage2.png` through `Ped6_Stage6.png`

### Discovered Animator Controllers in `Assets/_Game/Animators/Pedestrians/`
- `Ped1_AnimController.controller` through `Ped6_AnimController.controller`

### Discovered Components:
- `PedestrianWalkController.cs` (BrainDrain.Systems namespace)
- `PedestrianWobble.cs` (BrainDrain.Systems namespace)

### Target Assets to Create
- **Prefabs:** `Assets/_Game/Prefabs/Pedestrians/Ped1_Prefab.prefab` through `Ped6_Prefab.prefab`

---

# Implementation Steps

### Step 1: Ensure Prefab Directory Exists
- **Description:** Ensure the folder path `Assets/_Game/Prefabs/Pedestrians` exists. If not, create it using standard C# `Directory.CreateDirectory` or Unity Editor APIs.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No

### Step 2: Create and Configure Pedestrian Prefabs
- **Description:** For each pedestrian index $i \in [1, 6]$:
  1. Create a temporary GameObject named `Ped{i}_Prefab`.
  2. Add a `SpriteRenderer` component.
  3. Add an `Animator` component and assign its `runtimeAnimatorController` to the matching Animator Controller at `Assets/_Game/Animators/Pedestrians/Ped{i}_AnimController.controller`.
  4. Add the `PedestrianWalkController` component.
  5. Add the `PedestrianWobble` component.
  6. On the `PedestrianWalkController` component, wire the `spriteRenderer` field to the GameObject's `SpriteRenderer` component.
  7. On the `PedestrianWalkController` component, fill the `stageSprites` array (size 6) with `Ped{i}_Stage1.png` to `Ped{i}_Stage6.png` in order. Set Stage 1 sprite as the default sprite on the `SpriteRenderer` component as well.
  8. Save the GameObject as a Prefab to `Assets/_Game/Prefabs/Pedestrians/Ped{i}_Prefab.prefab`.
  9. Destroy the temporary GameObject.
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** No

### Step 3: Save Assets and Refresh Database
- **Description:** Save all dirty assets and refresh the Unity `AssetDatabase` to ensure the newly created prefabs are fully imported and indexed.
- **Assigned role:** developer
- **Dependencies:** Step 2
- **Parallelizable:** No

---

# Verification & Testing
### Automated Script Compilation Check
- Run a quick compilation check in Unity to ensure no syntax errors.

### Prefab Integrity Audits
- Verify all 6 Prefabs exist at `Assets/_Game/Prefabs/Pedestrians/`.
- Inspect each prefab to verify:
  - `SpriteRenderer` component is attached.
  - `Animator` component is attached and has the correct `Ped{i}_AnimController` controller assigned.
  - `PedestrianWalkController` component is attached, its `spriteRenderer` field is assigned, and its `stageSprites` array contains exactly 6 non-null elements matching `Ped{i}_Stage1` through `Ped{i}_Stage6`.
  - `PedestrianWobble` component is attached.
