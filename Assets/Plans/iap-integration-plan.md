# Real-Money IAP Integration Plan — God Shop

**Task:** `TASKLIST.md` open task #12, “Real IAP wiring for direct-currency purchases”  
**Status:** planning only; no package installation, code, scene, store-console, or tasklist changes are part of this document.  
**Platform order:** Android first. iOS remains post-launch because the current Windows development machine cannot produce an iOS build.

## Binding project rules

- The player-facing name is **God Shop**.
- Every God Shop purchase uses real money through the platform store. There is no premium soft currency and no conversion path from Brain Power, Cash, or Points.
- `GodTierStoreManager` remains the sole owner of God Shop entitlements and effects. Do not resurrect the deleted Premium Shop manager family.
- Store product IDs, not hardcoded dollar amounts, identify purchases.
- A displayed price is never proof of payment. No effect may be granted until the platform purchase has completed and its receipt/order has passed the approved validation path.
- Paid effects remain cosmetics or quality-of-life items, never progression power.

## Current-state audit

- Project editor version: Unity `6000.4.8f1`.
- Android application ID: `com.eighthkind.braindrain`.
- `Packages/manifest.json` does **not** currently list `com.unity.purchasing`.
- `GodTierStoreItemData` already has separate `itemId`, `productId`, `realMoneyPriceDisplay`, and `isConsumable` fields.
- `realMoneyPriceDisplay` is display-only. It must be replaced in production UI by the localized price returned by Google Play.
- `GodTierStoreManager.StubPurchase` currently grants an effect immediately without charging or validating anything. It is a development placeholder and must not remain reachable from a production Buy button.
- The current catalog contains nine items, despite the manager’s older “5 items” class comment.

## Packages

### Required

Install **Unity In-App Purchasing** (`com.unity.purchasing`) through Package Manager. At implementation time, select the newest **released, non-preview** version that Package Manager marks compatible with Unity `6000.4.8f1`, then pin that resolved version in the manifest. Unity IAP supplies the Google Play billing integration, product metadata, purchase callbacks, pending/confirmed order handling, acknowledgement/consumption, and a later iOS-compatible abstraction.

Do **not** add the Google Play Billing Library manually as a Gradle dependency; Unity IAP owns that native dependency. Do not add Unity Economy, Ads, subscriptions, or a second store SDK for this task.

### Conditional: secure backend

The recommended receipt design below needs a secure server but does not mandate a particular vendor:

- **External/custom backend:** no additional Unity package is inherently required; the client can call a narrow HTTPS purchase-validation endpoint.
- **Unity Gaming Services backend:** if Aceyfer chooses this route, add only the packages needed for the approved design—normally Authentication, Cloud Code, and Cloud Save, with Services Core resolved as a dependency. These are not prerequisites for platform-native Unity IAP itself and must not be added speculatively.

## Product-ID map

Use one Google Play one-time product per God Shop row. Preserve the existing reverse-domain convention:

`com.eighthkind.braindrain.<lowercase-stable-sku>`

The suffix should contain lowercase ASCII letters and digits only. Never encode a price, platform, locale, or product type in the ID. Once an ID is registered or sold, treat it as permanent; display names, descriptions, and prices may change without changing the ID. `itemId` remains the local save/content key and `productId` remains the platform-commerce key.

| God Shop item | Local `itemId` | Google Play type | Product ID | Current display-only price |
|---|---|---|---|---:|
| Bad Words Pack | `bad_words_pack` | Non-consumable | `com.eighthkind.braindrain.badwordspack` (already in asset) | $3.99 |
| Brain Freeze — 24 hours | `brain_freeze` | Consumable | `com.eighthkind.braindrain.brainfreeze` (already in asset) | $1.50 |
| Brain Freeze: 48 | `brain_freeze_48` | Consumable | `com.eighthkind.braindrain.brainfreeze48` (already in asset) | $2.50 |
| Deep Freeze — 168 hours | `deep_freeze` | Consumable | `com.eighthkind.braindrain.deepfreeze` (already in asset) | $5.99 |
| COGS Voicepack: Pure Disdain | `cogs_voicepack_disdain` | Non-consumable | `com.eighthkind.braindrain.cogsvoicepackdisdain` (proposed) | $1.99 |
| Y2K Glitch-Slum UI Theme | `y2k_glitch_slum_ui_theme` | Non-consumable | `com.eighthkind.braindrain.y2kglitchslumuitheme` (proposed) | $4.99 |
| 24-Hour Corporate Cloak | `twenty_four_hour_corporate_cloak` | Non-consumable | `com.eighthkind.braindrain.twentyfourhourcorporatecloak` (proposed) | $9.99 |
| The Illumisnotty Membership Card | `illumisnotty_membership_card` | Non-consumable | `com.eighthkind.braindrain.illumisnottymembershipcard` (proposed) | $14.99 |
| Holographic Trash Can Flex | `holographic_trash_can_flex` | Non-consumable | `com.eighthkind.braindrain.holographictrashcanflex` (proposed) | $29.99 |

“Consumable” describes fulfillment behavior: after a verified Brain Freeze purchase is durably granted, the transaction is consumed so the same product can be bought again. All other rows are permanent, one-time entitlements and must not become repurchasable merely because a local save was deleted.

The table’s prices document current asset copy only. Google Play Console is authoritative for price and localization, and the runtime UI must display the store-returned localized price. If product metadata is unavailable, show an unavailable/loading state rather than a possibly false static price.

## Android-first receipt and entitlement design

### Recommended launch path: server-authoritative validation

Use Unity IAP on the device to start purchases and receive pending orders, but validate every order on a secure backend before granting it. The client sends the minimum receipt/order data and authenticated player identifier over HTTPS. The backend extracts the Google purchase token and checks it with the Google Play Developer API, preferably `purchases.productsv2.getproductpurchasev2` for current one-time products.

The backend must verify all of the following before accepting a purchase:

- The package name is exactly `com.eighthkind.braindrain`.
- The purchase state is `PURCHASED`, not pending, canceled, or unspecified.
- The returned product ID is one of the approved catalog IDs and matches the requested God Shop item.
- The purchase token/order has not already been fulfilled for a different grant.
- The purchase belongs to the expected game account when an obfuscated external account ID or equivalent binding is used.
- The quantity and product type are expected.

Maintain an idempotent server ledger keyed by Google purchase token/transaction ID. A retry, app restart, callback replay, or network timeout must produce the same result without adding a second Brain Freeze duration or applying another permanent entitlement.

Fulfillment order:

1. Unity IAP reports a pending order.
2. The client marks the row busy and submits the order to the backend; it grants nothing yet.
3. The backend verifies the token with Google and durably records the entitlement or consumable grant exactly once.
4. The client synchronizes the authoritative entitlement state, applies the local effect idempotently, and saves it.
5. Only after durable fulfillment succeeds does the integration confirm the order through Unity IAP. Confirmation acknowledges a non-consumable or consumes a consumable through the store integration.

Do not confirm first and save later. A crash in that gap can permanently lose a consumed Brain Freeze purchase. Conversely, an interrupted callback before confirmation must safely replay because the backend ledger recognizes the same token.

For non-consumables, fetch existing purchases at startup and reconcile them with the entitlement service. An empty or partial Google fetch while offline must not be interpreted as proof that ownership was revoked. For consumed Brain Freeze purchases, Google will not restore the already-consumed purchase; their accumulated duration/expiry therefore needs durable server-side state if it must survive reinstall or device replacement.

Enable Real-time Developer Notifications and voided-purchase checks if the chosen backend scope includes refund, revocation, and chargeback reconciliation. Store credentials and Google service-account secrets belong only in the backend secret store—never in Unity assets, source, PlayerPrefs, logs, or the repository.

### Rejected as the default: client-only validation

Unity IAP’s local Google receipt validator and obfuscated validation data can be used as defense in depth or for a deliberately limited prototype. They are not equivalent to server verification because a modified client can bypass local checks, and local-only state cannot reliably recover consumed products. If Aceyfer explicitly approves a client-only launch, the implementation must document that fraud and consumable-recovery risk, generate the Google validation data with Unity’s IAP receipt-validation tooling, keep a transaction-ID dedupe ledger, and still confirm purchases only after saving. This is the lower-security fallback, not the recommended release architecture.

## Future changes to `GodTierStoreManager.cs` and its purchase boundary

The eventual implementation should preserve the manager’s existing effect and save responsibilities while replacing the free-grant entry point with a verified purchase boundary. Narrative scope:

- Build and validate a runtime lookup between each non-empty `productId` and exactly one `GodTierStoreItemData`. Initialization should fail closed on blank IDs, duplicates, unknown SKUs, or a mismatch between Unity IAP product type and `isConsumable`.
- Connect to Google Play, fetch all nine product definitions, then fetch existing purchases before enabling Buy buttons. Expose explicit initialization/readiness/error state so the UI does not pretend the shop is purchasable while IAP is unavailable.
- Route a Buy tap to an asynchronous purchase request for the selected item’s `productId`. Prevent double taps and overlapping transactions for the same row.
- Surface cancel, decline, pending payment, network failure, unavailable product, and deferred states without granting content. Pending payment must remain pending across restart.
- Feed the UI’s price label from Unity IAP’s localized product metadata. `realMoneyPriceDisplay` may remain an editor/offline preview only; it must not be the production source of truth.
- On a pending-order callback, locate the item by store product ID, send the order for validation, deduplicate it, durably grant it, and only then confirm it. Unknown product IDs must be logged safely and left unfulfilled for investigation, never mapped heuristically.
- Replace or encapsulate public `StubPurchase` with an internal “grant verified entitlement” path that cannot be called by a production button. Development fake-store behavior must be compiler-gated or otherwise impossible in a release build.
- Make grants idempotent. Permanent flags should be set to their target state, not toggled or stacked. The Corporate Cloak’s restored offline-extension total must be reconciled to the owned entitlement without double-adding 24 hours on every boot. Brain Freeze duration is the deliberate stacking exception, keyed to a unique verified transaction.
- Persist the entitlement/grant result immediately through the existing save pipeline. Add whatever save fields are required for processed transaction IDs and commerce-state migration, while keeping sensitive receipt data out of the save.
- Reconcile non-consumable ownership on initialization and app resume. Restoring Bad Words Pack must unlock profanity without overwriting the player’s separate on/off preference, matching the existing `LoadState` behavior.
- Emit `OnItemsChanged` only after a real entitlement-state change. Add separate purchase-state feedback for spinner/error messaging instead of abusing the ownership event.
- Avoid logging raw receipts, purchase tokens, account identifiers, or backend credentials. Log a redacted correlation ID and product ID instead.
- Keep platform-specific validation behind a small purchase/entitlement adapter so the later iOS implementation can supply StoreKit data without rewriting effect logic.

The implementation may place Unity IAP lifecycle code in a dedicated commerce service and let `GodTierStoreManager` remain the catalog/effect owner. That separation is preferable if putting connection, validation, restore, UI state, and effect logic in one MonoBehaviour would make the manager difficult to audit.

## Google Play Console work — manual, owned by Aceyfer

These steps happen outside the codebase and cannot be completed by the Unity implementation alone:

1. Finish Google Play developer identity verification. Create/link the Google payments profile and complete merchant, tax, banking, and payout information. Confirm the publisher identity is **Eighth Kind Studios** before creating irreversible account/store metadata.
2. Create or verify the Play Console app whose package name is exactly `com.eighthkind.braindrain`.
3. Configure Play App Signing and upload a signed Android App Bundle to an internal test track. Keep the upload/release signing material backed up and access-controlled.
4. Under **Monetize with Play → Products → One-time products**, create the approved product IDs exactly as written. Add accurate names/descriptions, create a **Buy** purchase option, configure regional availability and prices, and activate each product intended for the test.
5. Confirm which six products are non-consumable and which three Brain Freeze products are consumable in the implementation/store workflow. Do not create any of these as subscriptions.
6. If server validation is approved, link/configure the Google Cloud project and Play Developer API access, create the least-privileged service identity, grant only the required financial/order permissions, and store its credentials in the backend secret manager. Configure Pub/Sub and Real-time Developer Notifications if refund/revocation sync is in scope.
7. Add Google accounts under **Settings → License testing**. Also add them to the internal test track, publish the release, use the opt-in link, and install the app from Google Play with the same tester account. A random sideload is not the final end-to-end billing test.
8. On a real Android phone, test approved, declined, canceled, delayed-approved, and delayed-declined payments; restart during fulfillment; network loss; repeated callbacks; repeat purchase of every consumable; blocked repurchase and restore of every non-consumable; reinstall; refund/revoke; and chargeback behavior. Check that every completed order is acknowledged/consumed and that no grant occurs for pending or failed payment.
9. Review Play policy, Data safety, privacy policy, refund handling, content rating, and the Bad Words Pack’s effect on age-rating/store copy before production rollout.

The project currently has no Android test device, and `TASKLIST.md` §14 explicitly says real-device IAP testing cannot be replaced by the Unity Device Simulator. Task #12 cannot be called release-verified until the internal-track flow passes on physical hardware.

## Implementation phases and acceptance gates

### Phase 1 — decisions and external prerequisites

- Resolve every decision below.
- Create/verify the merchant account, app entry, backend ownership, and test accounts.
- Freeze the exact nine-product ID/type/price catalog before any product is activated.

### Phase 2 — package and commerce foundation

- Install/pin Unity IAP only after Phase 1 approval.
- Add the purchase adapter, product mapping, readiness state, localized metadata, and fake-store-only development path.
- Add validation/backend integration and idempotent transaction storage before connecting Buy buttons.

### Phase 3 — entitlement integration

- Replace the production stub route.
- Reconcile permanent entitlements and restore behavior.
- Make Brain Freeze fulfillment durable and exactly-once.
- Define migration behavior for pre-IAP local ownership flags.

### Phase 4 — Google Play internal test

- Upload the signed AAB and activate test products.
- Pass all happy-path, failure, pending, restart, restore, refund, and replay cases on a physical Android device.
- Verify localized prices against Play Console in more than one test region.

### Release acceptance

- No production UI path can call a free grant.
- No unvalidated, pending, canceled, unknown, or replayed order grants an effect.
- Every verified order grants exactly once before acknowledgement/consumption.
- All six non-consumables restore after local save deletion/reinstall according to the approved account policy.
- All three consumables remain repurchasable and their granted duration survives according to the approved persistence policy.
- Store-returned localized prices are shown; display-only asset prices cannot misrepresent the checkout price.
- Raw receipts/tokens and credentials never appear in logs or repository files.

## DECISION REQUIRED — Aceyfer approval before implementation

### DECISION REQUIRED — Validation/backend owner

Approve one path:

- **Recommended:** server-authoritative Google validation with an idempotent entitlement ledger.
- **Fallback:** client-only Unity receipt validation with explicitly accepted fraud and consumed-purchase recovery limitations.

If server-authoritative, choose UGS Cloud Code/Auth/Cloud Save or an external backend, and identify who owns deployment, secrets, monitoring, and operating cost.

### DECISION REQUIRED — Player identity and recovery

Choose how purchases bind to a player. Anonymous-only identity is easy to start but may be unrecoverable after uninstall unless it is linked or otherwise recoverable. Decide whether account linking/cross-device recovery is a launch requirement and which login method, if any, is acceptable.

### DECISION REQUIRED — Launch catalog and final product IDs

Approve all nine launch rows and the five proposed IDs before they are created in Play Console. Alternatively, choose a smaller launch subset consisting only of effects that are visibly complete and tested. Once registered/sold, IDs should not be renamed.

### DECISION REQUIRED — Prices and regional pricing

Approve the base prices and region strategy in Play Console. There is a specific documentation conflict to resolve: the current Bad Words Pack asset and main tasklist say **$3.99**, while an older `TASKLIST_DETAILS.md` decision record says **$5.00**. The current asset also contains unusual `.50` price points for the 24- and 48-hour consumables; confirm the intended Play Console price options rather than assuming the display strings are valid store prices.

### DECISION REQUIRED — Pre-IAP save migration

Existing development saves may contain God Shop ownership or Brain Freeze state granted for free through `StubPurchase`. Choose one policy before production:

- Reset all unverified God Shop entitlements in production builds.
- Grandfather specific development/test accounts only.
- Trust existing local flags for everyone (not recommended because they are not proof of purchase).

The migration must never silently convert an unverified local flag into a server-verified transaction record.

### DECISION REQUIRED — Consumable durability

Decide whether purchased Brain Freeze time must survive reinstall, device replacement, and account recovery. **Recommended: yes**, with the authoritative expiry/duration and processed-token ledger stored server-side. If no, the store description and support policy must clearly match that limitation.

### DECISION REQUIRED — Refund, revocation, and chargeback policy

Decide whether and how each entitlement is removed after a refund or chargeback, including what happens to already-used Brain Freeze time and the Corporate Cloak’s permanent offline-window extension. Approve whether Real-time Developer Notifications and Voided Purchases reconciliation are launch requirements.

### DECISION REQUIRED — Offline shop behavior

Approve the offline UX: recommended behavior is to show owned local entitlements but disable new purchases and avoid treating an empty store response as loss of ownership. Decide whether a last-known localized price may be cached or whether unavailable products should show only “UNAVAILABLE.”

### DECISION REQUIRED — iOS compatibility boundary

Approve reusing the same product-ID strings on App Store Connect later where possible and keeping store-specific validation behind the adapter now. No iOS build or App Store setup should block the Android launch, but Android implementation must not bake Google-only assumptions into entitlement/effect logic.

## Official implementation references

- [Unity IAP overview](https://docs.unity.com/en-us/iap)
- [Unity IAP setup and purchase fetching](https://docs.unity.com/iap/set-up-in-app-purchasing)
- [Unity IAP purchase lifecycle, confirmation, replay, and consumable persistence](https://docs.unity.com/en-us/iap/purchases)
- [Unity IAP receipt validation](https://docs.unity.com/en-us/iap/receipt-validation)
- [Unity IAP restore behavior](https://docs.unity.com/en-us/iap/restore-purchases)
- [Google Play one-time purchase lifecycle and acknowledgement](https://developer.android.com/google/play/billing/lifecycle/one-time)
- [Google Play backend integration](https://developer.android.com/google/play/billing/backend)
- [Google Play one-time product setup](https://support.google.com/googleplay/android-developer/answer/16430488)
- [Google Play Billing testing](https://developer.android.com/google/play/billing/test)
