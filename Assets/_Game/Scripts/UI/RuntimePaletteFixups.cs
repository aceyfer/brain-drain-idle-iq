using UnityEngine;
using UnityEngine.UI;

namespace BrainDrain.UI
{
    /// <summary>
    /// 2026-10-04 PALETTE LOCKDOWN: a handful of decorative Images in SampleScene.unity are
    /// scene-baked off-palette colors (green/magenta) with no owning controller -- unlike every
    /// other scene-baked color this pass found, which either gets overwritten every refresh by
    /// a real controller (shop rows' ApplyAccent, DialogueDisplayUI's Header override) or is
    /// forced invisible/white-tinted by UniversalButtonBorderApplier/RandomEventUIController's
    /// own style passes. These four are plain Images with no Button and nothing else touching
    /// their .color, found by a direct scan of every m_Color in the scene file against the 7
    /// Palette tokens. No .unity write allowed, so this overrides them at runtime instead,
    /// self-bootstrapping like DebugCheatPanel/UniversalButtonBorderApplier so no Inspector
    /// wiring is needed. Runs once; these are static decorative elements, never rebuilt.
    /// </summary>
    public static class RuntimePaletteFixups
    {
        private static readonly string[] TargetNames =
        {
            "HeaderSeparator",
            "PulseRing",
            "TapGlow",
            "PinkTopBorder",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
            Image[] images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null) { continue; }

                bool isTarget = false;
                for (int n = 0; n < TargetNames.Length; n++)
                {
                    if (image.gameObject.name == TargetNames[n]) { isTarget = true; break; }
                }
                if (!isTarget) { continue; }

                // Preserve whatever alpha the scene (or any future animation) has set -- only the
                // hue was ever off-palette.
                Color current = image.color;
                image.color = new Color(Palette.Glow.r, Palette.Glow.g, Palette.Glow.b, current.a);
            }
        }
    }
}
