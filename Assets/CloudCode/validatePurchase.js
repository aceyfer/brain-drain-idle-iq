// Brain Drain: Idle IQ -- §12 IAP backend.
// Verifies a Google Play purchase against the Google Play Developer API using a service-account
// credential (never sent to the client), grants idempotently, and acknowledges the purchase so
// Google doesn't auto-refund it after 3 unacknowledged days.
//
// Deploy target: Unity Cloud Code SCRIPT named "validatePurchase" (Cloud Code window in the
// Editor, or the Unity Dashboard). Design doc: Assets/Plans/iap-backend-scoping.md.
//
// SETUP REQUIRED BEFORE THIS CAN RUN (all outside the codebase, Aceyfer's own steps):
//   1. Google Cloud service account: Google Cloud Console (console.cloud.google.com) > create a
//      project (or reuse one) > enable the "Google Play Developer API" > IAM & Admin > Service
//      Accounts > create one. NOTE (checked against Google's current docs 2026-09-22, corrects an
//      earlier draft of this plan): Play Console no longer requires a separate "link a Google
//      Cloud project" step under Setup > API access -- that flow is deprecated. Instead, invite the
//      service account's own email as a user directly: Play Console > Users and permissions >
//      Invite new users > paste the service account email > grant exactly "View financial data,
//      orders, and cancellation survey responses" + "Manage orders and subscriptions" (those two
//      cover purchases.products.get and purchases.products.acknowledge; nothing broader is needed).
//   2. That service account's JSON key stored as a Cloud Code SECRET named exactly
//      GOOGLE_PLAY_SERVICE_ACCOUNT_JSON (Unity Dashboard > your project > Secret Manager > Add
//      secret) -- the whole downloaded JSON file's contents, as one string value. Never commit that
//      JSON file to this repo.
//   3. Run `npm install` inside Assets/CloudCode/ (uses the package.json already committed here) to
//      generate node_modules + package-lock.json for axios and google-auth-library -- required
//      before deploying, per Cloud Code's JavaScript-project convention. Commit package-lock.json
//      too once it exists (Unity's own docs recommend keeping it in source control so dependency
//      resolution is repeatable); node_modules itself should NOT be committed.
//   4. `bundling = true` below is required for this to compile at deploy time -- google-auth-library
//      is NOT one of Cloud Code's built-in whitelisted imports (only axios, lodash, and the
//      @unity-services/* SDKs are provided for free), so without bundling this script would fail to
//      resolve that require() the moment it's deployed. UNVERIFIED, flagging honestly: I could not
//      find documentation confirming how the bundler treats the built-in @unity-services/cloud-save
//      import once bundling is turned on for the rest of the file -- it almost certainly stays
//      external/runtime-provided (that's the whole point of the whitelist), but this exact
//      combination (one bundled custom package + one built-in import, in the same script) hasn't
//      been deploy-tested. Watch the Unity Dashboard's deploy/build log the first time this goes up
//      for any bundling error naming cloud-save specifically -- if it chokes, the likely fix is
//      excluding that one import from the bundle via whatever "externals" option the bundler exposes
//      (see Assets/Plans/iap-backend-scoping.md for the doc links this was researched against).
//   5. This script deployed/published in the Unity Dashboard's Cloud Code section (or via Window >
//      Deployment in the Editor once the Cloud Code + Deployment packages are installed).
//
// Not implemented here, by design (see the plan doc): Real-time Developer Notifications (RTDN,
// decision 7 skipped it for v1), refund/chargeback automation (decision 7, manual for v1), and
// anything iOS-specific (Android-first, §14).

module.exports.bundling = true;

const { JWT } = require('google-auth-library');
const axios = require('axios');
const { DataApi } = require('@unity-services/cloud-save-1.4');

const PACKAGE_NAME = 'com.eighthkind.braindrain';
const ANDROID_PUBLISHER_SCOPE = 'https://www.googleapis.com/auth/androidpublisher';
const IDEMPOTENCY_KEY_PREFIX = 'iap_processed_';

module.exports = async ({ params, context, logger, secretManager }) => {
    const { projectId, playerId, accessToken } = context;
    const { productId, purchaseToken, transactionId } = params;

    if (!productId || !purchaseToken) {
        return { approved: false, failureReason: 'Missing productId or purchaseToken.' };
    }

    // Player-scoped Cloud Save access (accessToken, not serviceToken) -- this script only ever
    // reads/writes the CALLING player's own idempotency record, never another player's, matching
    // the same "only an opaque token crosses this boundary" spirit as the C# side.
    const cloudSave = new DataApi({ accessToken });
    const idempotencyKey = IDEMPOTENCY_KEY_PREFIX + purchaseToken;

    // 1. Idempotency check FIRST. A replayed call (client retry, IapCommerceService reprocessing
    //    an unconfirmed order on a later connect, etc.) must return the exact same decision it
    //    returned the first time, never re-verify or re-grant.
    try {
        const existing = await cloudSave.getItems(projectId, playerId, [idempotencyKey]);
        const existingItem = existing?.data?.results?.find((r) => r.key === idempotencyKey);
        if (existingItem) {
            logger.info(`Purchase token already processed for player ${playerId}, replaying stored result.`);
            return existingItem.value; // { approved, grantTransactionId, failureReason }
        }
    } catch (err) {
        logger.error('Cloud Save lookup failed during idempotency check', { 'error.message': err.message });
        // Fail closed: if we can't confirm this token hasn't already been processed, don't risk a
        // double-grant by proceeding. IapCommerceService leaves the order unconfirmed on Failure,
        // so this is safely retryable, not a lost purchase.
        return { approved: false, failureReason: 'Could not verify purchase state, please try again.' };
    }

    // 2. Authenticate to the Google Play Developer API as the service account.
    let googleAccessToken;
    try {
        const secret = await secretManager.getSecret('GOOGLE_PLAY_SERVICE_ACCOUNT_JSON');
        const serviceAccount = JSON.parse(secret.value);
        const jwtClient = new JWT({
            email: serviceAccount.client_email,
            key: serviceAccount.private_key,
            scopes: [ANDROID_PUBLISHER_SCOPE],
        });
        const tokenResponse = await jwtClient.authorize();
        googleAccessToken = tokenResponse.access_token;
    } catch (err) {
        logger.error('Failed to authenticate with the Google Play Developer API', { 'error.message': err.message });
        return { approved: false, failureReason: 'Backend could not reach the store for verification.' };
    }

    // 3. Verify the purchase itself.
    let purchase;
    try {
        const url = `https://androidpublisher.googleapis.com/androidpublisher/v3/applications/${PACKAGE_NAME}/purchases/products/${encodeURIComponent(productId)}/tokens/${encodeURIComponent(purchaseToken)}`;
        const response = await axios.get(url, { headers: { Authorization: `Bearer ${googleAccessToken}` } });
        purchase = response.data;
    } catch (err) {
        const status = err.response?.status;
        logger.error('Google Play purchase verification request failed', { status, 'error.message': err.message });
        // A 400/404 from Google means the token/product combination is invalid or forged -- a
        // genuine rejection, not a transient failure, but still returned as approved:false so the
        // client-side flow (and this same idempotency record) treats it uniformly either way.
        const result = {
            approved: false,
            failureReason: status === 404 ? 'Purchase token not recognized by the store.' : 'Store verification failed, please try again.',
        };
        await safelyStoreIdempotency(cloudSave, projectId, playerId, idempotencyKey, result, logger);
        return result;
    }

    // purchaseState: 0 = purchased, 1 = canceled, 2 = pending. Only 0 is a grantable purchase.
    if (purchase.purchaseState !== 0) {
        const result = { approved: false, failureReason: 'Purchase was not completed (canceled or still pending).' };
        await safelyStoreIdempotency(cloudSave, projectId, playerId, idempotencyKey, result, logger);
        return result;
    }

    // 4. Acknowledge if Google doesn't already show it acknowledged. Google auto-refunds an
    //    unacknowledged purchase after 3 days -- this must not be skipped or silently deferred.
    if (purchase.acknowledgementState !== 1) {
        try {
            const ackUrl = `https://androidpublisher.googleapis.com/androidpublisher/v3/applications/${PACKAGE_NAME}/purchases/products/${encodeURIComponent(productId)}/tokens/${encodeURIComponent(purchaseToken)}:acknowledge`;
            await axios.post(ackUrl, {}, { headers: { Authorization: `Bearer ${googleAccessToken}` } });
        } catch (err) {
            // Log loudly but do NOT fail the grant over this -- the player already paid and
            // Google already confirmed purchaseState 0. Losing the entitlement over an
            // acknowledge-call hiccup would be worse than a rare, harmless double-acknowledge
            // attempt on a future retry. This needs monitoring/alerting before the 3-day window
            // closes, which is a Cloud Code logging/ops concern, not something to solve here.
            logger.error(
                'Failed to acknowledge purchase with Google Play -- entitlement still granted, but needs follow-up before the 3-day auto-refund window closes',
                { productId, purchaseToken, 'error.message': err.message },
            );
        }
    }

    // 5. Grant, and persist the idempotency record so any replay returns this exact decision.
    const grantTransactionId = purchase.orderId || transactionId || purchaseToken;
    const result = { approved: true, grantTransactionId };
    await safelyStoreIdempotency(cloudSave, projectId, playerId, idempotencyKey, result, logger);
    return result;
};

async function safelyStoreIdempotency(cloudSave, projectId, playerId, key, value, logger) {
    try {
        await cloudSave.setItem(projectId, playerId, { key, value });
    } catch (err) {
        // Do not fail the response over this -- the approve/reject decision already happened and
        // must reach the client either way. A failed write here only risks a future re-verification
        // (not a double-grant, since Google's purchaseState/acknowledgementState are themselves
        // idempotent), which is a follow-up concern, not a reason to change what's returned now.
        logger.error('Failed to persist idempotency record', { key, 'error.message': err.message });
    }
}

module.exports.params = {
    productId: { type: 'String', required: true },
    purchaseToken: { type: 'String', required: true },
    transactionId: { type: 'String', required: false },
};
