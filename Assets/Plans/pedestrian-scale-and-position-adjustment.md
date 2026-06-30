# Project Overview
- **Game Title:** Brain Drain
- **High-Level Concept:** An incremental/clicker mobile game themed around brain rot, IQ decay, and reclaiming human intellect to restore a dystopian society into a utopia.
- **Players:** Single player
- **Target Platform:** iOS (Mobile)
- **Render Pipeline:** Universal Render Pipeline (URP)

# Game Mechanics
## Core Gameplay Loop
The player taps to generate IQ / currency, spends currency on upgrades to increase passive income (BPPS - Brain Power Per Second), unlocks chapters, and progresses through restoration stages. As the world restoration climbs from 0% to 100%, the environment and pedestrians transition from a brain-dead "slack-jawed" dystopian state to an "engaged" utopian society.

# UI
The background contains spawned pedestrian UI elements and world-space pedestrians that utilize the newly created animators, sprites, and prefabs.

# Key Asset & Context
### Target Objects to Modify:
- ROOT `PedestrianContainer` children (`Ped1_Prefab` through `Ped6_Prefab` instances).
- Scale of each instance needs to be strictly verified/set to `(1, 1, 1)`.
- Y position of each instance needs to be spread out along the street baseline (Y from `-2.0f` to `2.0f` based on `streetMinY` and `streetMaxY`).
- X position of each instance can be spread out (e.g., from `-6.0f` to `6.0f`) to prevent them from stacking directly on top of each other.
- Verify `SpriteRenderer` on each is displaying the Stage 1 sprite correctly (`Ped{i}_Stage1_0` or `Ped{i}_Stage1`).

---

# Implementation Steps

### Step 1: Adjust Scale and Position of Scene Instances
- **Description:** Locate ROOT `PedestrianContainer`. For each of the 6 child GameObjects (`Ped1_Prefab` to `Ped6_Prefab`):
  - Set its `transform.localScale` to `(1f, 1f, 1f)`.
  - Distribute Y positions along the street baseline: `Y` values distributed evenly between `-2.0f` and `2.0f` (i.e., `-2.0f`, `-1.2f`, `-0.4f`, `0.4f`, `1.2f`, `2.0f`).
  - Distribute X positions along the street length to avoid vertical overlap (e.g., `-6.0f` to `6.0f`: `-6.0f`, `-3.6f`, `-1.2f`, `1.2f`, `3.6f`, `6.0f`).
  - Ensure SpriteRenderer is assigned with the matching Stage 1 sprite from `Assets/_Game/Sprites/Pedestrians/`.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No

### Step 2: Save Scene and Asset Changes
- **Description:** Mark modified scene objects as dirty, save the scene, and refresh the asset database.
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** No

---

# Verification & Testing
### Automated Validation Script
- Run a verification script that checks if `PedestrianContainer` has exactly 6 child GameObjects.
- Verify each child's localScale is `(1, 1, 1)`.
- Verify each child's Y position is correctly set along the baseline.
- Verify `SpriteRenderer` is active and displays the correct Stage 1 sprite.
