namespace BrainDrain.Core
{
    /// <summary>
    /// Locked-shop-row banner labels ("??? CLASSIFIED ???" through "??? FORBIDDEN KNOWLEDGE
    /// ???"), keyed purely on a numeric gate value. Single source of truth for both the threshold
    /// cutoffs and the label text -- UpgradeSlotUI and CashShopSlotUI both call GetLabel() instead
    /// of holding their own copies, the same single-owner pattern RebirthManager.SnottingUnlockThreshold
    /// already established after the six-copy hardcoded-pointsSpentUnlockThreshold bug. Label-only:
    /// this is deliberately NOT the World Restoration stage-gated shop-copy system described in
    /// TASKLIST.md -- that stays parked.
    /// </summary>
    public static class ClassificationTier
    {
        private const double SecretThreshold = 2000d;
        private const double TopSecretThreshold = 20000d;
        private const double ForbiddenKnowledgeThreshold = 200000d;

        /// <summary>
        /// Returns the locked-row banner text for a given gate value. Callers pass whatever their
        /// own item's gate field is (BuildingData.unlockCumulativeBrainPower,
        /// CashShopItemData.gateRebirthCount, ...) -- the thresholds are the same regardless of
        /// which currency/unit the caller's gate happens to be denominated in.
        /// </summary>
        public static string GetLabel(double unlockValue)
        {
            if (unlockValue >= ForbiddenKnowledgeThreshold) return "??? FORBIDDEN KNOWLEDGE ???";
            if (unlockValue >= TopSecretThreshold) return "??? TOP SECRET ???";
            if (unlockValue >= SecretThreshold) return "??? SECRET ???";
            return "??? CLASSIFIED ???";
        }
    }
}
