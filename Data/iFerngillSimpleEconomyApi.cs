using StardewValley;

namespace MarketTown.Data
{
    public interface IFerngillSimpleEconomyApi
    {
        /// <summary>
        /// Gets whether the economy has been loaded. No actions can be taken if the economy is not loaded. Occurs during GameLoop.SaveLoaded.
        /// </summary>
        /// <returns>True if the economy has been loaded, false otherwise.</returns>
        bool IsLoaded();

        /// <summary>
        /// Adjusts the supply value for the specified object. In general, the amount should be positive if the player sold an item. The value won't go outside the boundaries set in the config. Only one farmer should invoke this during multiplayer.
        /// </summary>
        /// <param name="obj">The object to adjust supply for.</param>
        /// <param name="amount">The amount to adjust supply by (can be negative).</param>
        void AdjustSupply(Object obj, int amount);

        /// <summary>
        /// Adjusts the daily delta value for the specified object. Only one farmer should invoke this during multiplayer.
        /// </summary>
        /// <param name="obj">The object to adjust daily delta for.</param>
        /// <param name="amount">The amount to adjust daily delta by (can be negative).</param>
        void AdjustDelta(Object obj, int amount);

        /// <summary>
        /// Gets the current supply value for the specified object.
        /// </summary>
        /// <param name="obj">The object to get supply for.</param>
        /// <returns>The supply value, or null if the object is not in the economy.</returns>
        int? GetSupply(Object obj);

        /// <summary>
        /// Gets the current daily delta value for the specified object.
        /// </summary>
        /// <param name="obj">The object to get daily delta for.</param>
        /// <returns>The daily delta value, or null if the object is not in the economy.</returns>
        int? GetDelta(Object obj);

        /// <summary>
        /// Checks if the specified object is tracked in the economy. It is recommended to call other methods without checking this first as they will handle the item not existing in the economy gracefully. 
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is in the economy, false otherwise.</returns>
        bool ItemIsInEconomy(Object obj);
    }
}
