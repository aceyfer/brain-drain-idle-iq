#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.UI;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// 2026-10-04 PALETTE LOCKDOWN verification tool. Play-mode-only: walks every active
    /// Graphic (Image) and TMP_Text in the loaded scene(s) and logs any whose RGB isn't within
    /// +/-12 (0-255 scale) of one of Palette's 7 tokens or the Dim grey. Alpha is ignored in the
    /// comparison -- legitimate UI (overlays, sheens, disabled states) varies alpha on an
    /// on-palette hue constantly, and that's not a violation.
    ///
    /// Skips Images whose sprite is clearly painted art rather than a tinted UI primitive:
    /// backdrops/background stage art, the ButtonBorder_Stage* frames, BizCard_Paper, and
    /// character sprites -- matched by sprite name and GameObject/ancestor name heuristics since
    /// there's no component-level tag separating "art" from "tintable UI sprite" in this
    /// codebase. A sprite that doesn't match any skip pattern (e.g. the restoration bar's own
    /// Fill/Sheen/Sprout/Frame sprites) is still checked -- those are plain shapes meant to be
    /// tinted, and checking them is exactly how the Restoration Bar commit's correctness gets
    /// verified live.
    /// </summary>
    public static class PaletteAudit
    {
        private const float ToleranceByte = 12f;

        private static readonly (string Name, Color Value)[] Tokens =
        {
            ("Base", Palette.Base),
            ("Surface", Palette.Surface),
            ("Cyan", Palette.Cyan),
            ("DeepCyan", Palette.DeepCyan),
            ("Glow", Palette.Glow),
            ("White", Palette.White),
            ("Dim", Palette.Dim),
        };

        /// <summary>2026-10-06 FREEZE INVENTORY: the 3 rarity tokens are valid ONLY on objects
        /// identifiable as rarity UI by name -- everywhere else they're still violations like any
        /// other off-palette color. Checked by GameObject/ancestor name, same mechanism as
        /// SkipNameContains below, but these still count toward checkedCount (they're being
        /// actively verified against a real token, just a wider one) rather than skippedCount.</summary>
        private static readonly (string Name, Color Value)[] RarityTokens =
        {
            ("RarityUncommon", Palette.RarityUncommon),
            ("RarityRare", Palette.RarityRare),
            ("RarityEpic", Palette.RarityEpic),
        };

        private static readonly string[] RarityAllowedNameContains =
        {
            "RarityTier",
        };

        private static readonly string[] SkipSpriteNames =
        {
            "BizCard_Paper",
        };

        private static readonly string[] SkipSpriteNamePrefixes =
        {
            "ButtonBorder_Stage",
        };

        private static readonly string[] SkipNameContains =
        {
            "Backdrop", "Background", "BackgroundStage", "Character", "Pedestrian",
            "Portrait", "COGSPortrait", "WorldRestorationStage", "DioramaFigure",
        };

        [MenuItem("BrainDrain/Validate/Palette Audit")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PaletteAudit] This only works in Play Mode (it needs live, populated UI to check).");
                return;
            }

            int checkedCount = 0;
            int skippedCount = 0;
            var violations = new List<string>();

            Image[] images = Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (!image.isActiveAndEnabled) { continue; }
                // 2026-10-05 audit follow-up: fully-transparent hit-area Images (ConvertButton,
                // RestoreButton, ShopButton, RebirthTriggerButton/InnerFill, PopupInnerBody,
                // CelebrationFlashOverlay, etc.) have an off-palette baked/transitional color that
                // never actually renders -- alpha 0 means the hue is moot, so these are false
                // violations, not real ones.
                if (image.color.a <= 0f) { skippedCount++; continue; }
                if (ShouldSkip(image)) { skippedCount++; continue; }

                checkedCount++;
                string closest = ClosestTokenOrNull(image.color, image.transform);
                if (closest == null)
                {
                    violations.Add($"Image '{Path(image.transform)}' color={image.color} (RGB {ToByteString(image.color)})");
                }
            }

            TMP_Text[] labels = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (!label.isActiveAndEnabled) { continue; }
                if (label.color.a <= 0f) { skippedCount++; continue; }

                checkedCount++;
                string closest = ClosestTokenOrNull(label.color, label.transform);
                if (closest == null)
                {
                    violations.Add($"TMP '{Path(label.transform)}' color={label.color} (RGB {ToByteString(label.color)})");
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[PaletteAudit] Checked {checkedCount}, skipped {skippedCount} (art sprites), {violations.Count} violation(s).");
            for (int i = 0; i < violations.Count; i++)
            {
                sb.AppendLine("  VIOLATION: " + violations[i]);
            }

            if (violations.Count > 0) { Debug.LogWarning(sb.ToString()); }
            else { Debug.Log(sb.ToString()); }
        }

        private static bool ShouldSkip(Image image)
        {
            Sprite sprite = image.sprite;
            if (sprite != null)
            {
                for (int i = 0; i < SkipSpriteNames.Length; i++)
                {
                    if (sprite.name == SkipSpriteNames[i]) { return true; }
                }
                for (int i = 0; i < SkipSpriteNamePrefixes.Length; i++)
                {
                    if (sprite.name.StartsWith(SkipSpriteNamePrefixes[i])) { return true; }
                }
            }

            for (Transform t = image.transform; t != null; t = t.parent)
            {
                for (int i = 0; i < SkipNameContains.Length; i++)
                {
                    if (t.name.Contains(SkipNameContains[i])) { return true; }
                }
            }

            return false;
        }

        private static string ClosestTokenOrNull(Color color, Transform context)
        {
            for (int i = 0; i < Tokens.Length; i++)
            {
                Color t = Tokens[i].Value;
                if (Mathf.Abs(color.r - t.r) * 255f <= ToleranceByte
                    && Mathf.Abs(color.g - t.g) * 255f <= ToleranceByte
                    && Mathf.Abs(color.b - t.b) * 255f <= ToleranceByte)
                {
                    return Tokens[i].Name;
                }
            }

            if (IsRarityTaggedName(context))
            {
                for (int i = 0; i < RarityTokens.Length; i++)
                {
                    Color t = RarityTokens[i].Value;
                    if (Mathf.Abs(color.r - t.r) * 255f <= ToleranceByte
                        && Mathf.Abs(color.g - t.g) * 255f <= ToleranceByte
                        && Mathf.Abs(color.b - t.b) * 255f <= ToleranceByte)
                    {
                        return RarityTokens[i].Name;
                    }
                }
            }

            return null;
        }

        private static bool IsRarityTaggedName(Transform t)
        {
            for (Transform current = t; current != null; current = current.parent)
            {
                for (int i = 0; i < RarityAllowedNameContains.Length; i++)
                {
                    if (current.name.Contains(RarityAllowedNameContains[i])) { return true; }
                }
            }
            return false;
        }

        private static string ToByteString(Color c)
        {
            return $"{Mathf.RoundToInt(c.r * 255f)},{Mathf.RoundToInt(c.g * 255f)},{Mathf.RoundToInt(c.b * 255f)}";
        }

        private static string Path(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                path = p.name + "/" + path;
            }
            return path;
        }
    }
}
#endif
