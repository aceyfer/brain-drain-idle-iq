# Overnight Report — 2026-07-04

**Branch:** `overnight/singleton-guards`
**Status:** Complete. 3 commits. No merges. No pushes. No scene/asset edits.

---

## What was done

### Pre-work (not part of this mission)

Before starting the overnight mission, the 7 commits that were sitting unpushed on `main` were pushed to `origin/main` (commit range `22fd423..2ef7f79`). This was explicitly requested by the user before going AFK.

---

### Task 1 — Singleton teardown guard pattern

**Commit:** `9639860`
**Message:** `fix: prevent (Auto) singleton resurrection during teardown (#9)`

**The problem:** When Play Mode exits in the Unity Editor, Unity destroys all scene GameObjects and calls `OnDestroy` on their MonoBehaviours. UI components' `OnDestroy` methods unsubscribe from manager events by calling `Manager.Instance`. Each manager's `Instance` getter has a self-bootstrapping path that calls `new GameObject("ManagerName (Auto)")` when `instance` is null. During teardown, `instance` is null (already destroyed), so the getter creates a new `(Auto)` GameObject — which Unity immediately flags as uncleaned, producing the "Some objects were not cleaned up" warning.

**The fix:** Added a `private static bool isShuttingDown` flag to each of the 15 affected managers. The flag works as three interlocked pieces:

1. **Reset in Awake (first line, before duplicate check):** `isShuttingDown = false;`
   Clears the static flag at the start of every new Play Mode session, even without a domain reload. Placed before the duplicate-instance guard so that a duplicate being destroyed in Awake cannot accidentally re-trip the flag through its own OnDestroy (the `if (instance == this)` check in OnDestroy handles that correctly — a duplicate is never the active instance).

2. **Set in OnApplicationQuit:** `isShuttingDown = true;`
   Covers the real-device quit path and the Editor's Application.Quit path.

3. **Set in OnDestroy when this is the active instance:** `if (instance == this) { isShuttingDown = true; instance = null; }`
   Covers the Editor play-stop path (which does NOT fire OnApplicationQuit). Only fires for the real active instance — duplicates destroyed in Awake never satisfy `instance == this`.

4. **Guard in Instance getter:** `if (isShuttingDown) return null;` placed immediately before `new GameObject(...)`.
   Returns null instead of creating a new (Auto) object during teardown. All callers in OnDestroy already use null-checks (`Manager.Instance?.Method()` or `if (Manager.Instance != null)`) — verified by grepping all 15 files' OnDestroy bodies before writing.

**The 15 files touched (all in `Assets/_Game/Scripts/Systems/`):**

| File | Had existing OnDestroy? | Notes |
|------|------------------------|-------|
| RebirthManager.cs | NO — new OnDestroy added | Mission-priority file |
| RandomEventManager.cs | YES — modified | Mission-priority file |
| CompanionManager.cs | NO — new OnDestroy added | Mission-priority file |
| RandomChatterManager.cs | NO — new OnDestroy added | Mission-priority file |
| WorldRestorationManager.cs | YES — modified | Mission-priority file |
| CashShopManager.cs | NO — new OnDestroy added | |
| ChapterManager.cs | YES — modified | |
| COGSPortraitController.cs | YES — modified | |
| DialogueManager.cs | YES — modified | |
| GodTierStoreManager.cs | NO — new OnDestroy added | |
| PlayerCharacterController.cs | YES — modified | |
| PointsShopManager.cs | NO — new OnDestroy added | |
| PremiumShopManager.cs | NO — new OnDestroy added | PremiumShopManager's Instance getter uses an inline one-liner style (`if (instance != null) return instance;`) — guard semantics are identical, style preserved |
| SaveManager.cs | YES — modified | |
| WardrobeManager.cs | YES — modified | |

**Files intentionally NOT touched:**
- `AnimationController.cs` — already had `isShuttingDown`/`isQuitting` guard from commit `22fd423` (the VFX cleanup fix)
- `DebugCheatPanel.cs` — entirely `#if UNITY_EDITOR`, compiles out in any build; its self-bootstrapping creates a debug overlay, not a game system
- `GameManager.cs` — does NOT self-bootstrap a new GameObject in its Instance getter (getter only calls `FindAnyObjectByType`, never creates one)
- `PlayerIQManager.cs`, `CurrencyManager.cs`, `UpgradeManager.cs`, `PlayerTapHandler.cs`, `DioramaManager.cs` — scene-placed components, no self-bootstrapping (Auto) pattern

**Awake placement verification:** All 15 Awake methods follow the pattern `if (instance != null && instance != this)` as their first logic, before any `instance = this` assignment. No file assigns `instance` before the duplicate check. The `isShuttingDown = false` line was placed as the absolute first line of each Awake, before the duplicate check — confirmed safe.

---

### Task 2 — Doc housekeeping

**Commit 1 — PROJECT_BIBLE.md:** Already committed in an earlier session (`8f744e3`). Nothing to re-commit; the mission's intent (Bible as source of truth) is already satisfied.

**Commit 2:** `03d5939`
**Message:** `docs: archive superseded root handoff docs per Bible doc map`

- `SESSION_HANDOFF.md` → `Assets/Docs/archive/SESSION_HANDOFF_2026-06.md`
- `OVERNIGHT_REPORT.md` → `Assets/Docs/archive/OVERNIGHT_REPORT_2026-06.md`
- `Assets/Docs/archive/` directory created.
- Both files preserved (not deleted) — `git mv`, not `rm`.

---

### Task 3 — This report

**Commit:** pending (this file)

---

## What was NOT done and why

- **No scene or .asset edits** — mission rules prohibit them without Unity open to verify.
- **No merge to main** — mission rules explicitly prohibit it.
- **No push** — mission rules explicitly prohibit it.
- No new gameplay systems, economy values, or save format changes.
- `AnimationController.cs` was not re-touched — it already has the correct guard from a prior session's fix.
- `DebugCheatPanel.cs` was not guarded — it is `#if UNITY_EDITOR` only, compiles out completely in builds, and its self-bootstrap creates a debug overlay that is acceptable to leak in the Editor.

---

## Verification steps (Aceyfer must run in Unity before merging)

Run these in order. Each one must pass before proceeding to the next.

### 1. Compile check
- Open Unity with the project.
- Switch to the `overnight/singleton-guards` branch (or merge it first).
- Wait for compilation in the Console. **Expected: zero errors, zero warnings about the changed files.**
- If any compile error appears, read it carefully — the most likely cause is a namespace or using-directive mismatch on one of the 15 files. None were added (all files already had the types they needed), so this would be a copy-paste error in the edits.

### 2. Play Mode enter/exit — clean teardown
- Enter Play Mode (press Play).
- Wait 5–10 seconds for the game to fully initialize (all managers awake, tick running, save loaded).
- Stop Play Mode (press Stop).
- **Expected: NO "Some objects were not cleaned up when closing the scene" warning in the Console.**
- **Expected: NO "(Auto)" GameObjects listed in the warning if it does appear.**
- If the warning still appears, check which GameObject name is listed — it will tell you exactly which manager is still escaping.

### 3. Rebirth still works
- Enter Play Mode.
- Use the debug cheat panel (triple-tap the IQ display) or `BrainDrain > Testing` menu to max buildings.
- Wait for or manually trigger the Rebirth button (or use the debug menu `Trigger Rebirth`).
- Confirm: Rebirth count increments, buildings reset, multipliers apply, save is written.
- Stop Play Mode cleanly.

### 4. Random events still fire
- Enter Play Mode.
- Open `BrainDrain > Testing > Trigger Random Event` (if that menu item exists) or wait 90s for an event to fire naturally.
- Confirm: event popup appears, accepting/declining it works.

### 5. Companion/Shop managers still load
- Enter Play Mode.
- Confirm no null-reference errors in the Console related to `CompanionManager`, `CashShopManager`, `PointsShopManager`, `PremiumShopManager`, or `GodTierStoreManager`.

### 6. Second Play Mode cycle (the critical Editor-specific test)
- After step 5, stop Play Mode.
- **Immediately** enter Play Mode again (without restarting the Editor).
- Confirm: game initializes normally — no managers are null, no "(Auto)" objects created where scene-placed versions should be.
- **This is the test that catches the isShuttingDown = false reset failing.** If the second Play Mode session has missing managers, the Awake reset is broken.

---

## Branch state

```
overnight/singleton-guards commits (not yet on main):
  03d5939  docs: archive superseded root handoff docs per Bible doc map
  9639860  fix: prevent (Auto) singleton resurrection during teardown (#9)
  [morning report commit — this file]
```

Remaining dirty files on this branch (not committed, intentionally ignored):
- TMP font assets (Unity-generated noise)
- Assets/Plans/shop-overhaul.md (untracked, not part of this task)
- Assets/_Recovery/ (explicitly excluded by standing rules)
- Packages/app.rive.rive-unity/ (explicitly excluded)
