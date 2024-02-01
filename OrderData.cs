namespace MarketTown
{
    internal class OrderData
    {
        public int dish;
        public string dishName;
        public int dishPrice;
        public string loved;

        public OrderData(int dish, string dishName, int dishPrice, string loved)
        {
            this.dish = dish;
            this.dishName = dishName;
            this.dishPrice = dishPrice;
            this.loved = loved;
        }
    }
}