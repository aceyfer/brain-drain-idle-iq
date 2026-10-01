using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// One reusable button "skin": border sprite, optional fill recolor/resprite, and optional
    /// label text styling (font material + color) -- everything a future reskin (remaining World
    /// Restoration stages 1-5, or a later novelty/seasonal pack like zombie or Christmas) needs
    /// to touch every button in the scene without any per-button editing or code changes.
    ///
    /// Fill and text fields are opt-in (each has its own override flag or null-check) rather than
    /// always-applied, so a minimal/placeholder theme can safely touch only the border and leave
    /// every button's own existing fill color and label styling exactly as authored -- e.g. the
    /// gold TMP underlay already set directly on specific text (Oswald_GoldUnderlay) is never
    /// silently clobbered by a theme that doesn't care about label styling.
    /// </summary>
    [CreateAssetMenu(fileName = "ButtonTheme", menuName = "BrainDrain/Button Theme")]
    public sealed class ButtonTheme : ScriptableObject
    {
        [Header("Border")]
        [Tooltip("Applied to every button's generated border child (UniversalButtonBorderApplier.EnsureBorderOn). Required for a theme to do anything visible at all.")]
        public Sprite borderSprite;

        [Header("Fill (optional)")]
        [Tooltip("If true, overwrites each button's own Image.color with fillColor below. Leave false for a theme that only changes the border -- most buttons in this project use a solid tint fill with no sprite, so blindly overriding this would flatten every button to one color.")]
        public bool overrideFillColor;
        public Color fillColor = Color.white;

        [Tooltip("If set, replaces each button's own Image.sprite. Leave null to keep each button's existing sprite (or lack of one).")]
        public Sprite fillSprite;

        [Header("Label Text (optional)")]
        [Tooltip("If set, applied to every button's label TextMeshProUGUI(s) via fontSharedMaterial -- mirrors the underlay-material approach already used on CashText (Oswald_GoldUnderlay), so a theme's legibility choice ships with the theme instead of being set per-button. Leave null to keep each label's existing material.")]
        public Material labelFontMaterial;

        [Tooltip("If true, overwrites every button label's text color with labelTextColor below.")]
        public bool overrideTextColor;
        public Color labelTextColor = Color.white;

        [Header("Label Hole Insets (framed buttons only)")]
        [Tooltip("2026-09-30: normalized (0-1) insets of this stage's borderSprite's own baked " +
            "fill hole, measured from the sprite's edges -- (x=left, y=bottom, z=right, w=top), " +
            "matching Unity's own Sprite.border component order. Produced by " +
            "Assets/Editor/ButtonHoleMeasurer.cs (BrainDrain/Tools/Measure Button Hole Insets). " +
            "UniversalButtonBorderApplier uses this to stretch a framed button's label within the " +
            "frame's actual hole instead of the button's own (often asymmetric relative to the " +
            "hole) flat rect -- see the Stage 5 label-sits-high bug this fixes.")]
        public Vector4 labelHoleInsets = new Vector4(0.1f, 0.1f, 0.1f, 0.1f);
    }
}
