namespace MarketTown.Data
{
    internal class DataOrder
    {
        public string dish;
        public string dishName;
        public int dishPrice;
        public string loved;

        public DataOrder(string dish, string dishName, int dishPrice, string loved)
        {
            this.dish = dish;
            this.dishName = dishName;
            this.dishPrice = dishPrice;
            this.loved = loved;
        }
    }
}