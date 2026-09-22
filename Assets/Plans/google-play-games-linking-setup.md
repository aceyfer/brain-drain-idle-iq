# Google Play Games Account Linking — Manual Setup

*Scoped + code written 2026-09-22, closing the decision-2 gap flagged when `UgsCloudCodeValidationService` first shipped (§12). This is a step-by-step for Aceyfer — all of it is Play Console / Google Cloud / Unity Editor GUI work this session cannot do itself (file-bridge only, no Editor access). The code side (`GooglePlayGamesAuthService.cs`) is already written and compiles fine in the Editor without any of this — it's only required to actually sign a real player in on a real Android build. References at the bottom.*

## Why this exists

§12 decision 2 says purchases and entitlements must restore on reinstall/new device via a linked Google account, not an anonymous-only identity. `UgsCloudCodeValidationService` originally only signed in anonymously — functionally fine for verify-and-grant, but an anonymous UGS identity doesn't survive an uninstall, so decision 2 wasn't actually satisfied. `GooglePlayGamesAuthService.cs` fixes the client code; this doc is the external setup that code depends on.

## Step 1 — Play Games Services project setup (Play Console)

1. Play Console → **Grow users → Play Games Services → Setup and management → Configuration**.
2. Set up Play Games Services for the `com.eighthkind.braindrain` app if not already done.
3. On the **Configuration** page, click **Get resources → Android (XML) tab**, copy the whole block — this is Play Games' own generated `AndroidManifest.xml` resource snippet, needed in Step 4.

## Step 2 — OAuth Web client for server-side access

Needed because `RequestServerSideAccess` returns an auth code that Unity Authentication exchanges server-side — without a Web client ID configured, `SignInWithGooglePlayGamesAsync` has nothing valid to authenticate against.

1. Same Play Console **Configuration** page → **Add credential → Game server** (this generates/links a Web application OAuth 2.0 client ID in the associated Google Cloud project).
2. Note the **Web client ID** and **Web client secret** — needed in Step 5.

## Step 3 — Import the Google Play Games plugin for Unity

This is a `.unitypackage`, not a Package Manager/`manifest.json` dependency — don't look for it there.

1. Download `GooglePlayGamesPluginForUnity-X.YY.ZZ.unitypackage` from the [plugin's GitHub releases](https://github.com/playgameservices/play-games-plugin-for-unity/releases) (v11.01+ required — matches the `SignInStatus`/`RequestServerSideAccess` API `GooglePlayGamesAuthService.cs` is written against).
2. In the Unity Editor: **Assets → Import Package → Custom Package** → select the downloaded file.

## Step 4 — Android Setup wizard (in-Editor)

1. **Window → Google Play Games → Setup → Android Setup.**
2. Paste the `AndroidManifest.xml` resource block from Step 1 into the resources-definition field.
3. Paste the **Web client ID** from Step 2 into the Client ID field (this is what enables server-side access / `RequestServerSideAccess`).
4. Pick a constants-class name/output folder (cosmetic — any reasonable value, e.g. `BrainDrain.PlayGamesConstants`) and click **Setup**.

## Step 5 — Unity Authentication provider configuration

1. **Edit → Project Settings → Services → Authentication.**
2. Set the ID provider to **Google Play Games**.
3. Enter the **Web client ID** and **Web client secret** from Step 2.

## Step 6 — Nearby Connections setup (required by the plugin, even though this project doesn't use Nearby)

1. **Window → Google Play Games → Setup → Nearby Connections Setup.**
2. Nearby connection service ID: the app's package name, `com.eighthkind.braindrain`.
3. Click **Setup**. This is a plugin requirement, not something this project needs functionally — skipping it is known to leave the plugin's setup incomplete.

## Step 7 — Verify

- Build an Android build (needs §14's device or at minimum the Editor + a signed build for Play Console internal testing).
- First launch should prompt native Google sign-in, then silently exchange the auth code — `GooglePlayGamesAuthService.EnsureSignedInAsync()` logs a warning and falls back to anonymous sign-in if anything in this chain fails, so a broken setup here degrades to "purchases work but don't survive reinstall" rather than a hard crash. Confirm no such warning appears in `adb logcat` on a clean install with a real Google account signed in on-device.
- The `TryLinkExistingAnonymousAccountAsync` path (upgrading a pre-existing anonymous player) isn't wired into any UI yet — worth a deliberate real-device test before ever exposing a "link my account" button to players, per that method's own "least-verified" flag in code.

## References

- [Set up Google Play Games for Unity and authenticate — Android Developers](https://developer.android.com/games/pgs/unity/unity-start) (primary source for steps 1-6 above, last updated 2026-07-30)
- [Unity Authentication — Google Play Games platform sign-in](https://docs.unity.com/en-us/authentication/platform-signin/google-play-games) (source for the `SignInWithGooglePlayGamesAsync`/`LinkWithGooglePlayGamesAsync` code `GooglePlayGamesAuthService.cs` implements)
- [Google Play Games plugin for Unity — GitHub](https://github.com/playgameservices/play-games-plugin-for-unity)
- [Migrate to Play Games Services v2 (Unity)](https://developer.android.com/games/pgs/unity/migrate-to-v2) — relevant if this Play Console project has any pre-v2 Play Games Services history to be aware of; not checked here, unknown whether it applies to this app.
