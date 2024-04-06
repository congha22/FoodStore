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

        public DataPlacedFood(Furniture furniture, Vector2 foodTile, Object foodObject, int slot)
        {
            this.furniture = furniture;
            this.foodTile = foodTile;
            this.foodObject = foodObject;
            this.slot = slot;
        }

        public DataPlacedFood(Object obj, Vector2 foodTile, Object foodObject, int slot)
        {
            this.obj = obj;
            this.foodTile = foodTile;
            this.foodObject = foodObject;
            this.slot = slot;
        }
    }
}