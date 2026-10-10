using UnityEngine;

namespace BrainDrain.UI
{
    /// <summary>
    /// 2026-10-04 PALETTE LOCKDOWN: the single source of truth for every UI color in the game.
    /// Lime (#39FF14) and Magenta (#FF1493) are retired -- nothing in Assets/_Game or
    /// Assets/Editor should reference them, or any other green/pink/orange/yellow/gold value,
    /// going forward. Painted art (backdrops, ButtonBorder_Stage* frames, BizCard_Paper,
    /// character sprites) is explicitly out of scope and untouched by this token set.
    /// </summary>
    public static class Palette
    {
        /// <summary>Panels, fills.</summary>
        public static readonly Color Base = new Color32(0x1B, 0x0F, 0x2E, 0xFF);

        /// <summary>Raised panels, rows, cards on dark.</summary>
        public static readonly Color Surface = new Color32(0x2A, 0x1A, 0x45, 0xFF);

        /// <summary>Primary/active text, borders, buttons, positive values.</summary>
        public static readonly Color Cyan = new Color32(0x00, 0xDD, 0xEB, 0xFF);

        /// <summary>Bar fills, pressed states, secondary accents.</summary>
        public static readonly Color DeepCyan = new Color32(0x00, 0x83, 0x8C, 0xFF);

        /// <summary>Highlights, sheen, pulses, "ready" glow.</summary>
        public static readonly Color Glow = new Color32(0x80, 0xF4, 0xFF, 0xFF);

        /// <summary>Body text.</summary>
        public static readonly Color White = Color.white;

        /// <summary>
        /// Locked/unaffordable -- grey at 45% alpha. 2026-10-05 audit follow-up: retuned from
        /// 0.5 to 0.6 (byte 153) to exactly match the grey already established and widely used
        /// for locked-state labels before this token existed (MainUIController.DimLabelColor,
        /// RebirthUIController.LockedLabelColor) -- the old 0.5 value was a fresh guess that
        /// landed just outside the audit's +/-12 tolerance of the real convention.
        /// </summary>
        public static readonly Color Dim = new Color(0.6f, 0.6f, 0.6f, 0.45f);

        /// <summary>
        /// 2026-10-12 scoreboard redesign (Unity AI's audit, Aceyfer-approved with changes):
        /// NEW BRAND COLORS -- black / deep blue / neon blue backbone, purple only as an energy
        /// accent. "NeonBlue" in the redesign spec is this file's existing Cyan (#00DDEB), not a
        /// new token -- deliberately not duplicated here as a second field with the same value,
        /// which would just invite the two to drift. Reach for Palette.Cyan wherever the
        /// scoreboard spec says "NeonBlue."
        /// </summary>
        public static readonly Color Black = new Color32(0x05, 0x07, 0x0D, 0xFF);

        /// <summary>Scoreboard backbone -- deep blue, paired with Black and Cyan ("NeonBlue").</summary>
        public static readonly Color DeepBlue = new Color32(0x0B, 0x2A, 0x5B, 0xFF);

        /// <summary>
        /// Energy accent ONLY (milestone sparks, not a general-purpose fill/text color) -- same
        /// exact value as RarityEpic, kept as a separate token rather than reusing that one
        /// directly: RarityEpic is gated to rarity-tagged UI by PaletteAudit's own RarityTier name
        /// allowlist (see that token's doc comment), and the scoreboard's milestone sparks aren't
        /// rarity UI. Using RarityEpic here would either fail that gate or require loosening it
        /// for an unrelated feature.
        /// </summary>
        public static readonly Color EnergyPurple = new Color32(0xA3, 0x35, 0xEE, 0xFF);

        /// <summary>
        /// 2026-10-06 FREEZE INVENTORY rarity tiers (Aceyfer's explicit color spec, standard
        /// MMO-loot-rarity hues) -- Brain Freeze family tier icons/names ONLY. Deliberately NOT
        /// general-purpose UI colors like the 7 tokens above: PaletteAudit only allows these on
        /// objects it can identify as rarity UI (name-tagged "RarityTier" -- see that file's
        /// RarityAllowedNameContains), everywhere else they're still violations same as any other
        /// off-palette color. Do not reach for these outside the freeze-tier icon/name system.
        /// </summary>
        public static readonly Color RarityUncommon = new Color32(0x1E, 0xFF, 0x00, 0xFF);
        public static readonly Color RarityRare = new Color32(0x00, 0x70, 0xDD, 0xFF);
        public static readonly Color RarityEpic = new Color32(0xA3, 0x35, 0xEE, 0xFF);
    }
}
