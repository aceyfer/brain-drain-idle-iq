using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BrainDrain.UI
{
    /// <summary>
    /// 2026-10-05 ART PASS 2 (item 3, consistency sweep): the "Dia-Log"/"POCKET"/"WALLET"/
    /// "RECOVER" side buttons were still a flat tinted-rect fill (the original Pocket/Wallet art
    /// pass styled the PANELS they open, never these open buttons themselves) -- plain enough to
    /// read as "MS Paint" next to the Alert_Frame-framed event popup. Gives them the same
    /// Alert_Frame 9-slice (Base fill + Glow border baked into the sprite already, so a plain
    /// white tint is a pure pass-through -- no new art) and Cyan label text the event popup uses.
    ///
    /// Deliberately NOT routed through UniversalButtonBorderApplier (the ornate
    /// ButtonBorder_Stage0-5 frames are explicitly out of scope for this palette work, and that
    /// system re-themes every managed button on every World Restoration stage change, which
    /// would fight this static look) -- ManagedButtonNames is read by
    /// UniversalButtonBorderApplier's own IsExcludedSideButton check to make sure it never
    /// touches these four, however they first enter its scan.
    /// </summary>
    public static class AlertFrameButtonStyle
    {
        public static readonly string[] ManagedButtonNames =
        {
            "LogOpenButton",
            "PocketOpenButton",
            "WalletOpenButton",
            "RecoverIQButton",
            "RestorePurchasesButton",
            // 2026-10-11 play-test fix: RebirthModal's SELL OUT/ABORT buttons (the only two
            // Buttons named exactly this in the scene -- confirmed via grep before adding) were
            // getting UniversalButtonBorderApplier's own ButtonBorder_Stage 9-slice, designed for
            // a compact ~190x50 button -- stretched across their new wide/short rect, the frame's
            // fixed-size corner pieces were the only recognizable gold left, reading as "black
            // box with gold nubs at the edges." Excluded here so they use this class's Alert_Frame
            // look instead (same system WALLET/POCKET/RECOVER IQ already use, which fits a wide
            // pill shape correctly) -- see RebirthUIController.OpenModal.
            "CancelButton",
            "ConfirmButton",
        };

        // 2026-10-05 play-test fix: labels were stretched edge-to-edge with zero inset (each
        // caller's own CreateStretchedLabel-style helper, or LogOpenButton's scene-authored label,
        // all anchored 0,0-1,1 with no offset) so the text touched the Alert_Frame border. Fixed
        // centrally here, once, rather than in each of the four builder call sites.
        private const float HorizontalPadding = 12f;
        private const float VerticalPadding = 6f;
        private const float AutoSizeMinFloor = 16f;

        private static Sprite alertFrameSprite;
        private static bool spriteLoaded;

        public static void Apply(Button button)
        {
            if (button == null) { return; }
            EnsureSpriteLoaded();

            Image image = button.GetComponent<Image>();
            if (image != null && alertFrameSprite != null)
            {
                image.sprite = alertFrameSprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = Palette.Cyan;

                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(HorizontalPadding, VerticalPadding);
                labelRect.offsetMax = new Vector2(-HorizontalPadding, -VerticalPadding);

                if (label.enableAutoSizing) { label.fontSizeMin = AutoSizeMinFloor; }
            }
        }

        private static void EnsureSpriteLoaded()
        {
            if (spriteLoaded) { return; }
            spriteLoaded = true;
            alertFrameSprite = Resources.Load<Sprite>("UI/Generated/Alert_Frame");
        }
    }
}
