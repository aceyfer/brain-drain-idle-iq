namespace BrainDrain.Core.Events
{
    /// <summary>Raised when any shop-relevant currency balance changes.</summary>
    public struct CurrencyChanged
    {
    }

    /// <summary>Raised after a shop item purchase completes.</summary>
    public struct ItemPurchased
    {
        public string ItemId;
    }

    /// <summary>Raised when world restoration advances to a new stage.</summary>
    public struct StageAdvanced
    {
        public int StageIndex;
    }
}
