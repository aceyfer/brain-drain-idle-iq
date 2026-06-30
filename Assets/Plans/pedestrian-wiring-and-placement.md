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
### Discovered Components:
- `BackgroundPedestrianManager` on `Canvas/BackgroundRoot/BottomBG/BackgroundPedestrianManager` GameObject in `SampleScene.unity`.
- Pedestrian prefabs `Ped1_Prefab` to `Ped6_Prefab` under `Assets/_Game/Prefabs/Pedestrians/`.

### Target Actions to Perform
1. Modify `BackgroundPedestrianManager.cs` to add `private GameObject[] pedestrianPrefabs;` field.
2. Wire the 6 pedestrian prefabs into the `pedestrianPrefabs` array on the `BackgroundPedestrianManager` component of the `BackgroundPedestrianManager` GameObject in the scene.
3. Create a root GameObject named `PedestrianContainer` at position `(0, 0, 0)`.
4. Instantiate one instance of each of the 6 prefabs as children of `PedestrianContainer` in the scene.

---

# Implementation Steps

### Step 1: Add Prefab Array to `BackgroundPedestrianManager.cs`
- **Description:** Edit `BackgroundPedestrianManager.cs` to add the `pedestrianPrefabs` serialized field under the `Sprite Pools` header.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No

### Step 2: Wire Prefabs to `BackgroundPedestrianManager` in Scene
- **Description:** Locate `BackgroundPedestrianManager` component on the `BackgroundPedestrianManager` GameObject in the scene. Set the `pedestrianPrefabs` array size to 6 and assign `Ped1_Prefab.prefab` through `Ped6_Prefab.prefab` in order.
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** No

### Step 3: Create `PedestrianContainer` and Instantiate Prefabs
- **Description:** Create a new root empty GameObject in the scene named `PedestrianContainer` positioned at `(0,0,0)`. Instantiate one instance of each of the 6 prefabs as children of this container.
- **Assigned role:** developer
- **Dependencies:** Step 2
- **Parallelizable:** No

### Step 4: Save Scene and Asset Changes
- **Description:** Mark modified scene objects as dirty, save the scene, save all assets, and refresh the database.
- **Assigned role:** developer
- **Dependencies:** Step 3
- **Parallelizable:** No

---

# Verification & Testing
### Automated Validation Script
- Run a verification script that checks if `BackgroundPedestrianManager` on `BackgroundPedestrianManager` GameObject has its `pedestrianPrefabs` array populated with the 6 correct prefabs in order.
- Verify `PedestrianContainer` exists at `(0, 0, 0)` in the scene and has exactly 6 child GameObjects named after each prefab, with correct components attached.
