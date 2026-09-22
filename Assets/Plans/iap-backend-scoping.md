# §12 IAP Backend Scoping — Server-Authoritative Purchase Validation

*Scoped 2026-09-22. `TASKLIST_DETAILS.md` §12 decision 1 named the approach (server-authoritative, UGS Cloud Code recommended) but flagged it as "unless he says otherwise once this gets actually scoped" — this is that scoping pass. This is a design document, not implementation; nothing here has been built or deployed. The client side (`IapCommerceService`/`GodTierStoreManager`) already expects exactly this shape via `IPurchaseValidationService` — see §12's landed client work — so this plan is about the other half of that boundary.*

## Why this can't just be client-only

`UnconfiguredPurchaseValidationService` is what production currently resolves to, and it fails closed by design — no Buy button can grant anything for free until a real backend exists. That's correct and deliberate (§12), but it also means **the God Shop cannot sell anything in production until this plan is executed**, which makes it the actual remaining gate on §12, not the Play Console product setup — those two can happen in parallel, but products with no working backend don't matter yet.

## Recommended shape: one UGS Cloud Code module

A single Cloud Code script (JS/TS, hosted by Unity Gaming Services) that does exactly one job: take an opaque `(productId, purchaseToken, transactionId)` triple from the client — the same three fields `IPurchaseValidationService`'s boundary already only ever passes — and return a verified/denied result plus whether the purchase needs acknowledging.

**Server-side flow per purchase:**
1. Client calls the Cloud Code module (via Unity's `CloudCodeService`) right after `IapCommerceService` receives a pending purchase from Unity IAP, before granting anything locally.
2. The module calls the **Google Play Developer API** (`androidpublisher/v3`), method `purchases.products.get`, passing `packageName` (`com.eighthkind.braindrain`), `productId`, and `purchaseToken`.
3. Google returns `purchaseState` (0 = purchased, 1 = canceled, 2 = pending), `consumptionState`, and `acknowledgementState`.
4. If `purchaseState == 0` (purchased) and this exact `purchaseToken`/order ID hasn't been processed before (idempotency check — see below), the module records the grant and returns "verified" to the client.
5. If not yet acknowledged, the module also calls `purchases.products.acknowledge` — **this has a hard 3-day deadline from Google, after which an un-acknowledged purchase is automatically refunded to the player.** This is a real, easy-to-miss failure mode worth building a retry/alerting path around, not just a happy-path call.
6. Client receives "verified," runs `GrantVerifiedEntitlement`, saves, *then* confirms/consumes through Unity IAP — matching the confirm-then-save-is-wrong-ordering already documented in §12.

**Idempotency:** the client already persists processed transaction IDs (`PlayerData.godTierStoreProcessedTransactionIds`, §12). The server side needs its own durable idempotency record too — not just trust the client — since decision 1 was explicitly "server-authoritative," not "server-assisted." A simple keyed record (purchase token → grant result) in a UGS Cloud Save "system" bucket, or any small durable store the Cloud Code module can read/write, is enough; this doesn't need a full database for a 9-product catalog.

## Setup requirements (Aceyfer's side, outside the codebase)

*Updated 2026-09-22 after the script/adapter below were actually written — corrects one stale step and adds two deploy-time requirements found by re-checking Unity's and Google's current docs directly rather than trusting the original scoping pass.*

1. **Google Cloud service account.** Google Cloud Console → create/reuse a project → enable the "Google Play Developer API" → IAM & Admin → Service Accounts → create one. **Correction:** Google's current docs say Play Console no longer requires linking a Google Cloud project under Setup → API access — that flow is deprecated. Instead: Play Console → **Users and permissions** → **Invite new users** → paste the service account's email → grant exactly **"View financial data, orders, and cancellation survey responses"** + **"Manage orders and subscriptions"** (covers `purchases.products.get`/`acknowledge`, nothing broader needed).
2. **Store that service account's JSON key as a Cloud Code secret**, named exactly `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` (Unity Dashboard → your project → Secret Manager → Add secret) — never commit the key to this repo.
3. **`npm install` inside `Assets/CloudCode/`** (using the `package.json` already committed there) before deploying, to generate `node_modules` + `package-lock.json` for `axios`/`google-auth-library`. Commit `package-lock.json` once it exists; never commit `node_modules`.
4. **Enable bundling.** `validatePurchase.js` now has `module.exports.bundling = true` — required because `google-auth-library` isn't one of Cloud Code's built-in whitelisted imports (only `axios`, `lodash`, and the `@unity-services/*` SDKs are provided for free); without bundling the deploy would fail to resolve that import. **Flagged as unverified in the script's own header comment:** how the bundler treats the built-in `@unity-services/cloud-save-1.4` import once bundling is on for the rest of the file isn't documented anywhere I could find — should just work (that's what the whitelist/externals split is for) but watch the Dashboard's build log on first deploy for a bundling error naming cloud-save specifically.
5. **UGS project setup**: Cloud Code environment (dev/production split recommended, matching the dev-fake-vs-production-fail-closed split already in the client's `IPurchaseValidationService` implementations); deploy `validatePurchase.js` via the Editor's **Window → Deployment** window (needs the Cloud Code + Deployment packages installed) or the Unity Dashboard's Cloud Code section directly.
6. **Client wiring — DONE.** `UgsCloudCodeValidationService.cs` (calls the module via `CloudCodeService.CallEndpointAsync(...)` and maps the response to `PurchaseValidationResult`) is written and wired into `IapCommerceService`'s production branch. Still needs `com.unity.services.authentication` + `com.unity.services.cloudcode` added via Package Manager and a real compile pass before it's trusted — see `TASKLIST_DETAILS.md` §12.

## Consumable durability (decision 6) and account linking (decision 2)

Both already-resolved decisions point at the same backend: Brain Freeze time surviving reinstall, and purchases restoring on a new device, both need the grant record keyed to the player's **linked Google Play Games identity**, not the local device. That argues for keying the idempotency/grant record by `(linked account ID, productId)` for non-consumables and by `(linked account ID, transactionId)` for consumables (so freeze durations stack correctly, per the existing stacking-exception note in §12), rather than by device ID anywhere in this flow.

## Explicitly out of scope for this plan

- Real-time Developer Notifications (RTDN) — decision 7 already skipped this for v1.
- Refund/chargeback automation — decision 7 said handle manually for now; this plan's grant record gives Aceyfer something to manually reverse against if needed, but no automated revocation flow is scoped here.
- iOS/App Store server verification — Android-first per §14; the adapter boundary (`IPurchaseValidationService`) is already deliberately platform-agnostic so this slots in later without a rewrite.
- Actually creating the 9 products in Play Console, merchant/tax/banking verification — unchanged, still Aceyfer's manual Play Console work per §12.

## Rough sequencing

1. Google Cloud service account + Play Console API access (Aceyfer, ~30-60 min, no dependencies) — see the corrected steps above.
2. ~~UGS Cloud Code module written~~ — **DONE**, `Assets/CloudCode/validatePurchase.js` exists. Still needs `npm install` + bundling (step 4 above) + actually deploying via the Dashboard/Editor.
3. ~~New `UgsCloudCodeValidationService` C# adapter wired in~~ — **DONE**, see step 6 above.
4. End-to-end test against a real Play Console internal test track purchase — this is also where §14's hardware blocker and §12's "Phase 4 real verification gate" converge; can't be fully closed without the Android device either way.

## References

- [Unity Manual — Verify in-app purchases with Cloud Code (pattern reference; covers Xbox/Microsoft Store specifically, not Google Play — no equivalent turnkey Google Play toggle exists in Unity IAP today, which is why this plan calls for a custom Cloud Code module rather than a Project Settings checkbox)](https://docs.unity3d.com/6000.4/Documentation/Manual/windows-iap-verification.html)
- [Google Play Developer API — purchases.products.get](https://developers.google.com/android-publisher/api-ref/rest/v3/purchases.products/get)
- [Google Play Developer API — purchases.products.acknowledge](https://developers.google.com/android-publisher/api-ref/rest/v3/purchases.products/acknowledge)
- [Google Play Developer API — purchases.products resource overview](https://developers.google.com/android-publisher/api-ref/rest/v3/purchases.products)
