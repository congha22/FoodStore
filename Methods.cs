using MarketTown;
using Netcode;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Object = StardewValley.Object;

namespace MarketTown
{
    public partial class ModEntry
    {
        private void UpdateOrders()
        {
            foreach (var c in Game1.player.currentLocation.characters)
            {

                if (c.isVillager())
                {
                    CheckOrder(c, Game1.player.currentLocation);
                }
                else
                {
                    c.modData.Remove(orderKey);
                }
            }
        }

        private void CheckOrder(NPC npc, GameLocation location)
        {
            Random rand = new Random();
            if (npc.modData.TryGetValue(orderKey, out string orderData) && rand.NextDouble() < Config.OrderChance)
            {
                //npc.modData.Remove(orderKey);
                UpdateOrder(npc, JsonConvert.DeserializeObject<OrderData>(orderData));
                return;
            }
            if (!Game1.NPCGiftTastes.ContainsKey(npc.Name) || npcOrderNumbers.Value.TryGetValue(npc.Name, out int amount) && amount >= Config.MaxNPCOrdersPerNight)
                return;
            if (rand.NextDouble() < Config.OrderChance)
            {
                //Game1.chatBox.addInfoMessage(Config.OrderChance.ToString());
                StartOrder(npc, location);
            }
        }

        private void UpdateOrder(NPC npc, OrderData orderData)
        {
            if (!npc.IsEmoting)
            {
                npc.doEmote(424242, false);
            }
        }

        private void StartOrder(NPC npc, GameLocation location)
        {
            List<int> loves = new();
            foreach (var str in Game1.NPCGiftTastes["Universal_Love"].Split(' '))
            {
                if (int.TryParse(str, out int i) && Game1.objectInformation.TryGetValue(i, out string data) && CraftingRecipe.cookingRecipes.ContainsKey(data.Split('/')[0]))
                {
                    loves.Add(int.Parse(str));
                }
            }
            foreach (var str in Game1.NPCGiftTastes[npc.Name].Split('/')[1].Split(' '))
            {
                if (int.TryParse(str, out int i) && Game1.objectInformation.TryGetValue(i, out string data) && CraftingRecipe.cookingRecipes.ContainsKey(data.Split('/')[0]))
                {
                    loves.Add(int.Parse(str));
                }
            }
            List<int> likes = new();
            foreach (var str in Game1.NPCGiftTastes["Universal_Like"].Split(' '))
            {
                if (int.TryParse(str, out int i) && Game1.objectInformation.TryGetValue(i, out string data) && CraftingRecipe.cookingRecipes.ContainsKey(data.Split('/')[0]))
                {
                    likes.Add(int.Parse(str));
                }
            }
            foreach (var str in Game1.NPCGiftTastes[npc.Name].Split('/')[3].Split(' '))
            {
                if (int.TryParse(str, out int i) && Game1.objectInformation.TryGetValue(i, out string data) && CraftingRecipe.cookingRecipes.ContainsKey(data.Split('/')[0]))
                {
                    likes.Add(int.Parse(str));
                }
            }

            List<int> neutral = new();
            foreach (var str in Game1.NPCGiftTastes["Universal_Neutral"].Split(' '))
            {
                if (int.TryParse(str, out int i) && Game1.objectInformation.TryGetValue(i, out string data) && CraftingRecipe.cookingRecipes.ContainsKey(data.Split('/')[0]))
                {
                    neutral.Add(int.Parse(str));
                }
            }
            foreach (var str in Game1.NPCGiftTastes[npc.Name].Split('/')[3].Split(' '))
            {
                if (int.TryParse(str, out int i) && Game1.objectInformation.TryGetValue(i, out string data) && CraftingRecipe.cookingRecipes.ContainsKey(data.Split('/')[0]))
                {
                    neutral.Add(int.Parse(str));
                }
            }
            Random rand = new Random();

            if (!loves.Any() && !likes.Any())
                return;
            string loved = "neutral";
            int dish;
            if (loves.Any() && (!likes.Any() || (rand.NextDouble() < Config.LovedDishChance)))
            {
                loved = "love";
                dish = loves[Game1.random.Next(loves.Count)];
            }
            else
            {
                if (rand.NextDouble() < 0.7)
                {
                    loved = "like";
                    dish = likes[Game1.random.Next(likes.Count)];
                }
                else
                {
                    loved = "neutral";
                    dish = neutral[Game1.random.Next(neutral.Count)];
                }
            }
            var name = Game1.objectInformation[dish].Split('/')[0];
            int price = 0;
            int.TryParse(Game1.objectInformation[dish].Split('/')[1], out price);


            npc.modData[orderKey] = JsonConvert.SerializeObject(new OrderData(dish, name, price, loved));
        }
    }
}