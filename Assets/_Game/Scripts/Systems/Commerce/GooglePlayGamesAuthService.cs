using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace BrainDrain.Systems.Commerce
{
    /// <summary>
    /// Closes the gap flagged in UgsCloudCodeValidationService: signs the player into Unity Gaming
    /// Services via Google Play Games when possible, so §12 decision 2 (purchases/entitlements
    /// restore on reinstall or a new device, via the linked Google identity -- not an anonymous-
    /// only identity that dies with the install) is actually satisfied, not just anonymous sign-in
    /// with a comment saying it isn't enough.
    ///
    /// UNVERIFIED, written blind against Unity's/Google's documentation -- this session has no
    /// Unity Editor or Android device to test against (file-bridge only). The fresh-sign-in path
    /// (EnsureSignedInAsync, the one actually wired into UgsCloudCodeValidationService) follows the
    /// exact documented pattern for Authenticate -> RequestServerSideAccess -> SignInWithGoogle-
    /// PlayGamesAsync. TryLinkExistingAnonymousAccountAsync (NOT wired into anything automatically)
    /// is the less-exercised edge case and is flagged again at its own declaration.
    ///
    /// The `using GooglePlayGames` directives are deliberately guarded by the same
    /// `#if UNITY_ANDROID && !UNITY_EDITOR` as their usage below -- NOT just the method bodies --
    /// so this file compiles fine in the Editor and on non-Android platforms even before the
    /// Google Play Games plugin for Unity is imported. That plugin is a separate .unitypackage
    /// import (Assets > Import Package > Custom Package), not a Package Manager/manifest.json
    /// dependency -- see Assets/Plans/google-play-games-linking-setup.md for the full manual setup
    /// (Play Console Play Games Services configuration, OAuth Web client ID, plugin import,
    /// Android Setup wizard). It is only required to actually build for Android; this file itself
    /// does not block compiling in the Editor.
    /// </summary>
    public static class GooglePlayGamesAuthService
    {
        /// <summary>
        /// Ensures a UGS identity exists, preferring one linked to Google Play Games over an
        /// anonymous one. Call this before anything that needs a durable, cross-device identity --
        /// currently just UgsCloudCodeValidationService's purchase validation. Safe to call
        /// repeatedly; no-ops if already signed in under any identity.
        /// </summary>
        public static async Task EnsureSignedInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                // Already signed in -- whether from a prior Google Play Games sign-in this
                // session, or a legacy anonymous identity from before this file existed. Upgrading
                // that legacy case is TryLinkExistingAnonymousAccountAsync's job, not this method's
                // -- deliberately not called automatically here, see that method's own doc comment.
                return;
            }

            string authCode = await RequestGooglePlayGamesAuthCodeAsync();
            if (!string.IsNullOrEmpty(authCode))
            {
                try
                {
                    await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GooglePlayGamesAuthService] Google Play Games sign-in failed, falling back to anonymous: {ex.Message}");
                }
            }

            // Fallback -- Editor, non-Android platforms, no Google account on device, the player
            // declined the Play Games sign-in prompt, or the sign-in call itself failed. Purchases
            // still verify and grant correctly under an anonymous identity (UgsCloudCodeValidation
            // Service's Cloud Code call doesn't care which kind of identity it is), they just won't
            // restore on reinstall until a real Google-linked identity is established -- which is
            // exactly the state this whole file exists to avoid defaulting to silently.
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        private static Task<string> RequestGooglePlayGamesAuthCodeAsync()
        {
            var tcs = new TaskCompletionSource<string>();
#if UNITY_ANDROID && !UNITY_EDITOR
            PlayGamesPlatform.Activate();
            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                if (status != SignInStatus.Success)
                {
                    Debug.LogWarning($"[GooglePlayGamesAuthService] Google Play Games sign-in failed or was declined: {status}");
                    tcs.TrySetResult(null);
                    return;
                }

                PlayGamesPlatform.Instance.RequestServerSideAccess(false, code => tcs.TrySetResult(code));
            });
#else
            tcs.TrySetResult(null); // Editor / non-Android -- EnsureSignedInAsync falls back to anonymous.
#endif
            return tcs.Task;
        }

        /// <summary>
        /// NOT wired into anything automatically. Offered for a future "Link Google account"
        /// settings entry, for the case where a player is already anonymously signed in (e.g. they
        /// purchased once before this file shipped, or Google Play Games sign-in failed once and
        /// they never got prompted again) and wants to attach that identity to Google Play Games
        /// after the fact so their purchase history follows them going forward.
        ///
        /// LEAST-VERIFIED CODE PATH IN THIS FILE. The AccountAlreadyLinked recovery branch
        /// (sign out of the local anonymous identity, sign into the Google-linked one instead) is
        /// the documented pattern, but has not been exercised against a real device or a real
        /// already-linked account. Spot-check it for real before wiring a UI button to this method.
        /// </summary>
        public static async Task<bool> TryLinkExistingAnonymousAccountAsync()
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                return false; // nothing to link -- caller should use EnsureSignedInAsync instead
            }

            string authCode = await RequestGooglePlayGamesAuthCodeAsync();
            if (string.IsNullOrEmpty(authCode))
            {
                return false;
            }

            try
            {
                await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authCode);
                return true;
            }
            catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                // This Google identity already owns a different UGS player -- almost always the
                // player's real account from a prior install, carrying their actual purchase
                // history. Switch to it rather than staying on the local anonymous identity, which
                // would otherwise silently strand that history. See this method's own "least-
                // verified" warning above.
                Debug.LogWarning("[GooglePlayGamesAuthService] Google account already linked to a different player; switching to that account.");
                AuthenticationService.Instance.SignOut(clearCredentials: false);
                await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GooglePlayGamesAuthService] Link failed, staying on current identity: {ex.Message}");
                return false;
            }
        }
    }
}
