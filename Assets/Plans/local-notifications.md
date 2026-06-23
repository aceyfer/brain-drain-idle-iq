# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: Satirical mobile idle clicker by AcEclipse Games. Tracks player IQ decay offline and leverages local notifications to pull players back.
- Players: Single player
- Tone / Art Direction: Satirical retro-dystopian cartoon, high-contrast colors.
- Target Platform: iOS / Android / Mobile
- Render Pipeline: UniversalRP (2D)

# Game Mechanics
## Offline IQ Decay & Retention Hook
- While the app is closed, PlayerIQ decays linearly toward 60 over an 8-hour period.
- To encourage player return, we schedule a local push notification to fire exactly **2 hours** after the player leaves the game (app close or backgrounded).
- The notification message: `"Your IQ is slipping... come back."`
- The notification is cancelled immediately upon app launch, focus, or pause resumption so that it never fires while the player is active.

# UI / Feedback
- This is a silent system-level local notification integration using the imported Hippo `SimpleAndroidNotifications` package. No runtime visual UI changes are required in the Canvas.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- Hippo Package: `Assets/SimpleAndroidNotifications/NotificationManager.cs`
- Existing Save pipeline: `Assets/_Game/Scripts/Systems/SaveManager.cs`.
  - `SaveGame()` is invoked on app pause, focus loss, and quit via `GameManager.Instance.OnSaveRequested`.
  - It writes `lastActiveUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()`.
  - On launch, `ApplyLoadedDataToSystems()` loads `lastActiveUtc = DateTimeOffset.FromUnixTimeSeconds(data.lastActiveUnixSeconds).UtcDateTime` and calls `PlayerIQManager.Instance.LoadStateWithOfflineDecay(data.playerIQ, lastActiveUtc)`.

# Implementation Steps

### Step 1: Update SaveManager.cs to Schedule and Cancel Notifications
- **Description**:
  - Add standard unity event callbacks `OnApplicationPause(bool pauseStatus)` and `OnApplicationFocus(bool hasFocus)` to `SaveManager.cs`.
  - On focus regain or pause resume (`!pauseStatus` / `hasFocus`), call `CancelAllNotifications()`.
  - Inside `SaveGame()`, call `ScheduleIQDecayNotification()` to schedule the notification precisely when the save file is committed (captures quit, focus loss, and backgrounding).
  - Wrap the scheduling and cancellation logic in `#if UNITY_ANDROID && !UNITY_EDITOR` to avoid spamming the Unity Editor Console with Hippo's platform support warning logs during development, while ensuring full native execution on Android devices.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

# Verification & Testing
- Compile check: ensure no compilation errors or namespace conflicts occur.
- Log check: verify no warning/error logs are written to the console in the Unity Editor during saving, loading, or focusing.
- Code inspection: confirm `CancelAllNotifications` is called on start, focus, and pause resumption, and `ScheduleIQDecayNotification` is called during save state creation.
