# Project Overview
- **Game Title:** Brain Drain
- **High-Level Concept:** An incremental/clicker mobile game themed around brain rot, IQ decay, and reclaiming human intellect to restore a dystopian society into a utopia.
- **Players:** Single player
- **Target Platform:** iOS (Mobile)
- **Render Pipeline:** Universal Render Pipeline (URP)

# Game Mechanics
## Core Gameplay Loop
Background pedestrian visual adjustment to ensure correct display above background, appropriate sizing, and non-null sprite assignments.

# UI
Not directly modified in this task.

# Key Asset & Context
### Target Objects to Modify:
- ROOT `PedestrianContainer` children (`Ped1_Prefab` through `Ped6_Prefab` instances in the scene).
- `SpriteRenderer` on each instance.
- `Sorting Layer` and `Order in Layer` properties on each instance's `SpriteRenderer`.
- `localScale` of each instance.

---

# Implementation Steps

### Step 1: Adjust SpriteRenderer Settings
- **Description:** Locate ROOT `PedestrianContainer` children (`Ped1_Prefab` to `Ped6_Prefab` instances in the scene).
  - Verify `SpriteRenderer` has a valid non-null sprite (Stage 1 sprite: `Ped{i}_Stage1_0` or similar).
  - Set `Sorting Layer` to `Default`.
  - Set `Order in Layer` to `10` so they render in front of background elements.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No

### Step 2: Set Scale of Pedestrian Instances
- **Description:** Set the `localScale` of all 6 pedestrian instances under `PedestrianContainer` to `(0.3, 0.3, 1)`. This ensures they are sized appropriately for the 2D street, since at scale `(1,1,1)` they are too large for the camera orthographic size of 5.
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** No

### Step 3: Save Scene Changes
- **Description:** Mark modified scene objects as dirty and save the scene.
- **Assigned role:** developer
- **Dependencies:** Step 2
- **Parallelizable:** No

---

# Verification & Testing
### Automated Validation Script
- Run a verification script that checks if `PedestrianContainer` has exactly 6 child GameObjects.
- Verify each child's localScale is `(0.3, 0.3, 1)`.
- Verify `Sorting Layer` is `Default` and `Order in Layer` is `10` on each child's `SpriteRenderer`.
- Verify `SpriteRenderer` sprite reference is not null.
