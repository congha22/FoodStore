using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace MarketTown.Data
{
    internal class DataPlacedFood
    {
        public Furniture furniture;
        public Object obj;
        public Vector2 foodTile;
        public Object foodObject;
        public int slot;
        public int value;
        public float multiplier;

        public DataPlacedFood(Furniture furniture, Vector2 foodTile, Object foodObject, int slot, float multiplier = 1f)
        {
            this.furniture = furniture;
            this.foodTile = foodTile;
            this.foodObject = foodObject;
            this.slot = slot;
            this.multiplier = multiplier;
        }

        public DataPlacedFood(Object obj, Vector2 foodTile, Object foodObject, int slot, float multiplier = 1f)
        {
            this.obj = obj;
            this.foodTile = foodTile;
            this.foodObject = foodObject;
            this.slot = slot;
            this.multiplier = multiplier;
        }
    }
}