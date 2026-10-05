using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BrainDrain.UI
{
    /// <summary>
    /// 2026-10-04 PALETTE LOCKDOWN: a handful of decorative Images/labels in SampleScene.unity
    /// are scene-baked off-palette colors with no owning controller -- unlike every other
    /// scene-baked color this pass found, which either gets overwritten every refresh by a real
    /// controller (shop rows' ApplyAccent, DialogueDisplayUI's Header override) or is forced
    /// invisible/white-tinted by UniversalButtonBorderApplier/RandomEventUIController's own
    /// style passes. These are plain Images with no Button, or orphaned TMP labels left over
    /// from the pre-redesign "vessel" restoration bar (RestorationBarWireFix.cs), and nothing
    /// else touches their .color -- found by a direct scan of every m_Color in the scene file
    /// against the 7 Palette tokens. No .unity write allowed, so this overrides them at runtime
    /// instead, self-bootstrapping like DebugCheatPanel/UniversalButtonBorderApplier so no
    /// Inspector wiring is needed. Runs once; these are static decorative elements, never rebuilt.
    /// </summary>
    public static class RuntimePaletteFixups
    {
        // Each name confirmed unique scene-wide before being added here (a generic name like
        // "ContentArea" would risk retinting an unrelated object -- those are handled by their
        // owning controller directly instead, e.g. DialogueDisplayUI's own ContentArea/
        // AvatarFrame overrides).
        private static readonly (string Name, Color Target)[] ImageFixes =
        {
            ("HeaderSeparator", Palette.Glow),
            ("PulseRing", Palette.Glow),
            ("TapGlow", Palette.Glow),
            ("PinkTopBorder", Palette.Glow),
            // Economy bar's background chip -- scene-baked near-black non-token grey, never
            // touched by any controller.
            ("CurrencyHeader", Palette.Base),
        };

        private static readonly (string Name, Color Target)[] LabelFixes =
        {
            // Orphaned "vessel"-era restoration label (RestorationBarWireFix.cs) -- the live
            // "paint bucket" bar (HUDController.BuildRestorationBar) never touches this object.
            ("RestorationLabel", Palette.White),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
            Image[] images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null) { continue; }
                Color? target = FindTarget(image.gameObject.name, ImageFixes);
                if (target == null) { continue; }

                // Preserve whatever alpha the scene (or any future animation) has set -- only the
                // hue was ever off-palette.
                Color t = target.Value;
                Color current = image.color;
                image.color = new Color(t.r, t.g, t.b, current.a);
            }

            TMP_Text[] labels = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label == null) { continue; }
                Color? target = FindTarget(label.gameObject.name, LabelFixes);
                if (target == null) { continue; }

                Color t = target.Value;
                Color current = label.color;
                label.color = new Color(t.r, t.g, t.b, current.a);
            }
        }

        private static Color? FindTarget(string name, (string Name, Color Target)[] fixes)
        {
            for (int i = 0; i < fixes.Length; i++)
            {
                if (name == fixes[i].Name) { return fixes[i].Target; }
            }
            return null;
        }
    }
}
