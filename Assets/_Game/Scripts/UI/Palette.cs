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

        /// <summary>Locked/unaffordable -- grey at 45% alpha.</summary>
        public static readonly Color Dim = new Color(0.5f, 0.5f, 0.5f, 0.45f);
    }
}
