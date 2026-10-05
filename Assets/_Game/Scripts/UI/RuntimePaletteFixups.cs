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
        private static readonly string[] GlowImageTargetNames =
        {
            "HeaderSeparator",
            "PulseRing",
            "TapGlow",
            "PinkTopBorder",
        };

        private static readonly string[] WhiteLabelTargetNames =
        {
            // Orphaned "vessel"-era restoration label (RestorationBarWireFix.cs) -- the live
            // "paint bucket" bar (HUDController.BuildRestorationBar) never touches this object.
            "RestorationLabel",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
            Image[] images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || !Matches(image.gameObject.name, GlowImageTargetNames)) { continue; }

                // Preserve whatever alpha the scene (or any future animation) has set -- only the
                // hue was ever off-palette.
                Color current = image.color;
                image.color = new Color(Palette.Glow.r, Palette.Glow.g, Palette.Glow.b, current.a);
            }

            TMP_Text[] labels = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label == null || !Matches(label.gameObject.name, WhiteLabelTargetNames)) { continue; }

                Color current = label.color;
                label.color = new Color(Palette.White.r, Palette.White.g, Palette.White.b, current.a);
            }
        }

        private static bool Matches(string name, string[] targets)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (name == targets[i]) { return true; }
            }
            return false;
        }
    }
}
