# Ad Network Partner List + GDPR/CCPA Consent Setup

*Scoped 2026-09-22, unblocks the ad-related rows in `PRIVACY_POLICY_DRAFT.md` §3 and `STORE_LISTING_DRAFT.md` §6 (Play Console Data Safety). This is a step-by-step for Aceyfer to run in the Unity Editor + Google's console — this session has no shell access to run Editor menu items directly (file-bridge only). References at the bottom.*

## Why this is blocking two other documents

Unity LevelPlay (`com.unity.services.levelplay` 9.5.1, already installed per §57) is a mediation layer, not a single ad network. It doesn't ship with actual ad-serving SDKs — those get pulled in by its **Network Manager**, an Editor-only tool, when you pick which networks to mediate. Until that's run once, nobody (including this session) can know the real list of third parties that will actually touch a player's device identifier — which is exactly the list `PRIVACY_POLICY_DRAFT.md` §3 and the Play Console Data Safety form both need to be accurate. Guessing at that list and publishing it anyway risks the same "policy doesn't match actual behavior" problem Google Play enforces against.

## Step 1 — Run LevelPlay's Network Manager

1. In the Unity Editor: **LevelPlay → Network Manager** (or wherever the current SDK places it — 9.5.1's menu path may differ slightly from older tutorials; if it's not under `LevelPlay`, check `Window → Unity LevelPlay`).
2. Sign in with the ironSource/LevelPlay account tied to this app (create one at [platform.ironsrc.com](https://platform.ironsrc.com) if it doesn't exist yet — needed regardless, since ad revenue reporting lives there too).
3. Register the app if it isn't already (Android package `com.eighthkind.braindrain`, matching the bundle ID already set in §14).
4. Select which mediated networks to enable. For a v1 launch, the common minimal set is AdMob (Google) + Meta Audience Network + Unity Ads (LevelPlay's own) — but this is Aceyfer's call based on expected fill rate/eCPM in Brain Drain's target markets, not something to default silently. Whatever's picked here becomes the real answer to "which ad networks" for both downstream documents.
5. The Network Manager will modify `Packages/manifest.json` (adding each network's adapter package) and may drop files under `Assets/MobileDependencyResolver/` (EDM4U) or similar, similar to what §57 already saw when LevelPlay itself installed. Expect a Unity recompile after this step — flag any compile errors back rather than guessing past them.
6. Note the exact list of networks enabled. This list is what goes into `PRIVACY_POLICY_DRAFT.md` §3 and the Data Safety form's ad-related rows.

## Step 2 — Set up a real consent flow (CMP)

§57 already flagged this as unbuilt. As of LevelPlay SDK 7.7.0+, if a standard CMP is wired in, LevelPlay automatically picks up consent signals — no per-network manual plumbing needed for the common case:

- **Recommended: Google's own CMP, Google UMP (User Messaging Platform).** It's free, Play-Console-integrated, and LevelPlay's docs specifically call out automatic compatibility with it.
- Runs through Google Play Console's **App content → Ads / Consent** flow to register the app for UMP, then the Android SDK-side `UserMessagingPlatform` integration shows the actual consent dialog to EU/UK users before any ad request goes out.

**Code-side, once a CMP is wired in (LevelPlay SDK 9.5.0+, the version already installed):**

```csharp
// Call BEFORE initializing the LevelPlay SDK, once the CMP has resolved the user's choice.
LevelPlayPrivacySettings.SetGDPRConsent(consentGiven); // bool, from the CMP's result
```

If finer per-network control is ever needed instead of the global flag, 9.4.0 also exposed a per-network dictionary form (`SetGDPRConsents(Dictionary<string,bool>)`), but the global boolean is the right default here — no evidence this project needs per-network granularity.

**This part is genuinely new code, not just an Editor-tool run** — small, but real: wiring the UMP SDK's consent-check-and-show-dialog call into the app's startup sequence (likely near wherever `IapCommerceService`/ad-manager initialization already happens), before the LevelPlay SDK itself initializes. Worth a short follow-up task once Step 1's network list is locked, rather than folding it silently into this scoping doc.

## Step 3 — CCPA (US) note

Google Play and Apple both also expect a CCPA-compliant "opt out of sale/sharing" path for California users if the app monetizes via targeted ads. Google UMP's newer versions can present a combined GDPR+US-states consent message; confirm this is enabled when setting up Step 2 rather than treating GDPR and CCPA as the same checkbox.

## What this unblocks once done

- `PRIVACY_POLICY_DRAFT.md` §3's placeholder line gets replaced with the real network list.
- `STORE_LISTING_DRAFT.md` §6's ad-related Data Safety rows can be finalized (which networks, whether they're shared with — not just processed by — third parties, whether location is collected).
- Play Console's own "Ads" and "Data safety" declarations (separate from this project's docs, filled in directly in Play Console) can be answered accurately.

## References

- [Unity LevelPlay — Regulation advanced settings (GDPR/consent API)](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/regulation-advanced-settings)
- [Unity LevelPlay — GDPR compliance overview](https://docs.unity.com/en-us/grow/ads/privacy/gdpr-compliance)
- [Unity — Set up a TCF or non-TCF consent configuration](https://docs.unity.com/en-us/grow/ads/privacy/gdpr/tcf-and-non-tcf-consent)
- [ironSource/LevelPlay GDPR compliance](https://docs.unity.com/en-us/grow/levelplay/platform/legal-resources/ironsource-gdpr-compliance)
