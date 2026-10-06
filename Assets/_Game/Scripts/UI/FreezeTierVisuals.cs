using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// 2026-10-06: shared tier color/icon lookup for the Brain Freeze family, extracted out of
    /// TimedPurchaseWalletUI (which had its own private copies) so FreezeTutorialPopupUI can use
    /// the exact same mapping without duplicating it. Pure lookup, no state beyond a lazy sprite
    /// cache -- GodTierStoreRarityTier -> Palette.RarityX / FreezeCup_X sprite.
    /// </summary>
    public static class FreezeTierVisuals
    {
        private static readonly Dictionary<GodTierStoreRarityTier, Sprite> TierIconSprites = new();
        private static bool spritesLoaded;

        private static void EnsureSpritesLoaded()
        {
            if (spritesLoaded) { return; }
            TierIconSprites[GodTierStoreRarityTier.Uncommon] = Resources.Load<Sprite>("UI/Generated/FreezeCup_Uncommon");
            TierIconSprites[GodTierStoreRarityTier.Rare] = Resources.Load<Sprite>("UI/Generated/FreezeCup_Rare");
            TierIconSprites[GodTierStoreRarityTier.Epic] = Resources.Load<Sprite>("UI/Generated/FreezeCup_Epic");
            spritesLoaded = true;
        }

        public static Color TierColor(GodTierStoreRarityTier tier) => tier switch
        {
            GodTierStoreRarityTier.Uncommon => Palette.RarityUncommon,
            GodTierStoreRarityTier.Rare => Palette.RarityRare,
            GodTierStoreRarityTier.Epic => Palette.RarityEpic,
            _ => Palette.White,
        };

        public static Sprite TierSprite(GodTierStoreRarityTier tier)
        {
            EnsureSpritesLoaded();
            return TierIconSprites.TryGetValue(tier, out Sprite sprite) ? sprite : null;
        }
    }
}
