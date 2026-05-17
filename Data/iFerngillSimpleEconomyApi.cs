using StardewValley;

namespace MarketTown.Data
{
    public interface IFerngillSimpleEconomyApi
    {

        /// <summary>
        /// Adjusts the supply value for the specified object. In general, the amount should be positive if the player sold an item. The value won't go outside the boundaries set in the config. Only one farmer should invoke this during multiplayer.
        /// </summary>
        /// <param name="obj">The object to adjust supply for.</param>
        /// <param name="amount">The amount to adjust supply by (can be negative).</param>
        void AdjustSupply(Object obj, int amount);
    }
}
