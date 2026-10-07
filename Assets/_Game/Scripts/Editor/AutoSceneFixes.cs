#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BrainDrain.Systems;
using BrainDrain.UI;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Auto-runs scene fixes on every Editor script reload, since the menu-item-based fixes
    /// (ShopPanelLayoutFix, SceneManagerWiring) require someone to manually invoke them, which
    /// hasn't reliably happened given multiple sessions touching this project concurrently.
    /// Each fix is internally guarded to only act when something's actually wrong, so this is
    /// safe and cheap to re-check on every reload, including self-correcting if a regression
    /// (e.g. a duplicate manager) reappears later.
    /// </summary>
    public static class AutoSceneFixes
    {
        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            // Defer past domain-reload/initialization -- GameObject.Find and scene queries
            // aren't reliable yet inside InitializeOnLoadMethod itself.
            EditorApplication.delayCall += RunFixes;
        }

        private static void RunFixes()
        {
            // All fixes below exist to correct a *saved scene* problem (something baked into
            // the .unity file in a state that would prevent Awake() from wiring correctly on
            // the next load). Once actually in Play Mode, Awake() has already run for this
            // session -- an inactive ShopPanel at that point is the CORRECT post-Awake hidden
            // state, not a sign anything is wrong. Without this guard, any script recompile that
            // happens to land while the shop is legitimately closed mid-Play-Session gets
            // mistaken for "Awake never ran," reactivates the panel (which does NOT re-run
            // Awake on an object that already initialized once), and then saves that broken
            // permanently-visible state into the scene file -- a real regression hit on
            // 2026-06-21.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            RemoveDuplicateRandomEventManagers();
            ReactivateShopPanelIfNeeded();
            FixShopPanelIfUnwired();
            ReactivateRewardedAdRecoveryPopupIfNeeded();
        }

        /// <summary>
        /// ShopUIController lives on the ShopPanel GameObject itself. If that GameObject is
        /// saved inactive, Unity never calls Awake() on it at all (Awake only runs for objects
        /// active at scene load, or on first activation) -- which silently breaks
        /// shopButton/closeButton's onClick wiring, since that happens in Awake(). The correct
        /// "hidden by default" behavior is ShopUIController.Awake() calling SetActive(false)
        /// itself, AFTER registering the listeners -- which only works if the object starts
        /// active so Awake() actually gets to run once. Only ever called outside Play Mode (see
        /// the guard in RunFixes) -- inside Play Mode, "inactive" just means "correctly hidden."
        /// </summary>
        private static void ReactivateShopPanelIfNeeded()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            ShopUIController shopUI = Object.FindAnyObjectByType<ShopUIController>(FindObjectsInactive.Include);
            if (shopUI == null || shopUI.gameObject.activeSelf)
            {
                return;
            }

            Debug.Log("[AutoSceneFixes] ShopPanel was saved inactive, which prevents its own ShopUIController.Awake() from ever running -- reactivating it.");
            shopUI.gameObject.SetActive(true);
            MarkAndSaveScene(nameof(ReactivateShopPanelIfNeeded));
        }

        /// <summary>
        /// 2026-10-10 play-test fix: confirmed via the saved scene YAML (m_IsActive: 0 on the
        /// "RewardedAdRecoveryPopup" GameObject) that this is the exact same class of bug as
        /// ReactivateShopPanelIfNeeded above, just on a different panel -- RewardedAdRecoveryUIController
        /// lives directly on that root GameObject (not a child), so a saved-inactive root means its
        /// own Awake()/Start() (onClick wiring, OnRecoveryStateChanged subscription, initial
        /// SetCanvasState(false)) never ran at all. The RECOVER IQ HUD button is a completely
        /// separate self-bootstrapping system that only needs RewardedAdRecoveryManager to exist to
        /// show itself, so it rendered fine regardless -- but its own FindAnyObjectByType lookup for
        /// the popup controller (active-objects-only by default) silently returned null, so clicking
        /// it no-opped with zero error/log. Same correct fix as Shop: the GameObject must start
        /// active so its own SetCanvasState(false) can hide it properly post-Awake, never saved
        /// pre-hidden in the scene file directly.
        /// </summary>
        private static void ReactivateRewardedAdRecoveryPopupIfNeeded()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            RewardedAdRecoveryUIController popup = Object.FindAnyObjectByType<RewardedAdRecoveryUIController>(FindObjectsInactive.Include);
            if (popup == null || popup.gameObject.activeSelf)
            {
                return;
            }

            Debug.Log("[AutoSceneFixes] RewardedAdRecoveryPopup was saved inactive, which prevents its own RewardedAdRecoveryUIController.Awake()/Start() from ever running -- reactivating it.");
            popup.gameObject.SetActive(true);
            MarkAndSaveScene(nameof(ReactivateRewardedAdRecoveryPopupIfNeeded));
        }

        private static void RemoveDuplicateRandomEventManagers()
        {
            RandomEventManager[] all = Object.FindObjectsByType<RandomEventManager>(FindObjectsInactive.Include);
            if (all.Length <= 1)
            {
                return;
            }

            GameObject systemsParent = GameObject.Find("_Systems");
            RandomEventManager keeper = null;

            // Prefer the one already parented under _Systems.
            foreach (RandomEventManager candidate in all)
            {
                if (systemsParent != null && candidate.transform.IsChildOf(systemsParent.transform))
                {
                    keeper = candidate;
                    break;
                }
            }

            if (keeper == null)
            {
                keeper = all[0];
            }

            bool removedAny = false;
            foreach (RandomEventManager candidate in all)
            {
                if (candidate == keeper)
                {
                    continue;
                }

                Debug.Log($"[AutoSceneFixes] Removing duplicate RandomEventManager GameObject '{candidate.gameObject.name}' (keeping the one under _Systems).");
                Object.DestroyImmediate(candidate.gameObject);
                removedAny = true;
            }

            if (removedAny)
            {
                MarkAndSaveScene(nameof(RemoveDuplicateRandomEventManagers));
            }
        }

        private static void FixShopPanelIfUnwired()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            ShopUIController shopUI = Object.FindAnyObjectByType<ShopUIController>(FindObjectsInactive.Include);
            if (shopUI == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(shopUI);
            SerializedProperty shopPanelProp = so.FindProperty("shopPanel");
            bool unwired = shopPanelProp == null || shopPanelProp.objectReferenceValue == null;

            if (unwired)
            {
                Debug.Log("[AutoSceneFixes] ShopUIController.shopPanel is unwired -- running ShopPanelLayoutFix automatically.");
                ShopPanelLayoutFix.FixShopPanelLayout();
            }
        }

        private static void MarkAndSaveScene(string requestedBy)
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning($"[AutoSceneFixes] Skipped scene save requested by {requestedBy}: play mode became active after the initial guard check (delayCall timing hole).");
                return;
            }

            try
            {
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            catch (System.InvalidOperationException)
            {
                Debug.LogWarning($"[AutoSceneFixes] Scene save requested by {requestedBy} was rejected mid-call — play mode transition raced the guard. Safe to ignore; the underlying scene state wasn't corrupted, just not saved this pass.");
            }
        }
    }
}
#endif
