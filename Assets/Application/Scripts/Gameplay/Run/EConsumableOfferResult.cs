namespace GridBattle.Gameplay.Run
{
    /// <summary>Outcome of <c>RunManager.ChooseConsumableOffer</c>.</summary>
    public enum EConsumableOfferResult
    {
        /// <summary>There is no offer pending, or the index is out of range.</summary>
        Invalid,

        /// <summary>The item went into a free slot; the node is done.</summary>
        Added,

        /// <summary>The inventory was full and the oldest item was discarded for it (policy ReplaceOldest); the node is done.</summary>
        ReplacedOldest,

        /// <summary>The inventory was full and the item was lost (policy DiscardNew); the node is done.</summary>
        DiscardedNew,

        /// <summary>
        /// The inventory is full (policy AskPlayer): the choice is remembered; the UI asks which carried item to
        /// discard and calls <c>RunManager.ReplaceConsumable(slot)</c>, or <c>DeclineConsumableOffer()</c>.
        /// </summary>
        NeedsSlotChoice
    }
}
