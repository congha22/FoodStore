using MarketTown.Data;
using Netcode;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using Object = StardewValley.Object;

namespace MarketTown
{
    public partial class ModEntry
    {
        private void UpdateOrders(bool marketOrder = false)
        {
            foreach (var c in Game1.player.currentLocation.characters)
            {
                if (c.IsVillager && c.temporaryController == null)
                {
                    CheckOrder(c, Game1.player.currentLocation, false, marketOrder);
                }
                else
                {
                    c.modData.Remove(orderKey);
                }
            }
        }

        private void CheckOrder(NPC npc, GameLocation location, bool bypass, bool marketOrder = false)
        {
            if (Context.ScreenId > 0) return;
            Random rand = new Random();
            if (npc.modData.TryGetValue(orderKey, out string orderData)  )
            {
                //npc.modData.Remove(orderKey);
                UpdateOrder(npc, JsonConvert.DeserializeObject<DataOrder>(orderData));
                return;
            }
            if (!Game1.NPCGiftTastes.ContainsKey(npc.Name) || npcOrderNumbers.Value.TryGetValue(npc.Name, out int amount) && amount >= Config.MaxNPCOrdersPerNight)
                return;
            if ( rand.NextDouble() < Config.OrderChance / 8
                || rand.NextDouble() < Config.OrderChance && !location.Name.Contains("Custom_MT_Island")
                || bypass)
            {
                StartOrder( npc, location, marketOrder );
            }
        }

        private void UpdateOrder(NPC npc, DataOrder orderData)
        {
            if (!npc.IsEmoting)
            {
                npc.doEmote(424242, false);
            }
        }

        private void StartOrder(NPC npc, GameLocation location, bool marketOrder = false)
        {
            try
            {
                List<string> loves = new();
                foreach (var str in Game1.NPCGiftTastes["Universal_Love"].Split(' '))
                {
                    if (Game1.objectData.ContainsKey(str) && CraftingRecipe.cookingRecipes.ContainsKey(Game1.objectData[str].Name))
                    {
                        loves.Add(str);
                    }
                }
                foreach (var str in Game1.NPCGiftTastes[npc.Name].Split('/')[1].Split(' '))
                {
                    if (Game1.objectData.ContainsKey(str) && CraftingRecipe.cookingRecipes.ContainsKey(Game1.objectData[str].Name))
                    {
                        loves.Add(str);
                    }
                }

                List<string> likes = new();
                foreach (var str in Game1.NPCGiftTastes["Universal_Like"].Split(' '))
                {
                    if (Game1.objectData.ContainsKey(str) && CraftingRecipe.cookingRecipes.ContainsKey(Game1.objectData[str].Name))
                    {
                        likes.Add(str);
                    }
                }
                foreach (var str in Game1.NPCGiftTastes[npc.Name].Split('/')[3].Split(' '))
                {
                    if (Game1.objectData.ContainsKey(str) && CraftingRecipe.cookingRecipes.ContainsKey(Game1.objectData[str].Name))
                    {
                        likes.Add(str);
                    }
                }

                List<string> neutral = new();
                foreach (var str in Game1.NPCGiftTastes["Universal_Neutral"].Split(' '))
                {
                    if (Game1.objectData.ContainsKey(str) && CraftingRecipe.cookingRecipes.ContainsKey(Game1.objectData[str].Name))
                    {
                        neutral.Add(str);
                    }
                }
                foreach (var str in Game1.NPCGiftTastes[npc.Name].Split('/')[9].Split(' '))
                {
                    if (Game1.objectData.ContainsKey(str) && CraftingRecipe.cookingRecipes.ContainsKey(Game1.objectData[str].Name))
                    {
                        neutral.Add(str);
                    }
                }
                foreach ( var obj in GiftableObject)
                {
                    if (!loves.Contains(obj.ItemId) && !likes.Contains(obj.ItemId) && !neutral.Contains(obj.ItemId) 
                        && CraftingRecipe.cookingRecipes.ContainsKey(obj.ItemId) 
                        && !Game1.NPCGiftTastes["Universal_Hate"].Split(' ').Contains(obj.ItemId) 
                        && npc.getGiftTasteForThisItem(obj) != 6 ) 
                    {
                        neutral.Add(obj.ItemId);
                    }
                }


                Random rand = new Random();

                string taste = "like";
                string dish = "240";
                var name = "Farmer's Lunch";
                int price = 150;

                if (!marketOrder)
                {
                    if (taste.Any() && (rand.NextDouble() < Config.LovedDishChance || !likes.Any() && !neutral.Any()))
                    {
                        taste = "love";
                        dish = loves[rand.Next(loves.Count)];
                    }
                    else if (likes.Any() && (rand.NextDouble() < Config.LovedDishChance || !loves.Any() && !neutral.Any()))
                    {
                        taste = "like";
                        dish = likes[rand.Next(likes.Count)];
                    }
                    else if (neutral.Any())
                    {
                        taste = "neutral";
                        dish = neutral[rand.Next(neutral.Count)];
                    }

                    name = Game1.objectData[dish.ToString()].Name;
                    price = Game1.objectData[dish.ToString()].Price;
                }
                else
                {
                    int tried = 0;
                    while (tried < 5)
                    {
                        tried++;

                        var selectItem = GiftableObject[rand.Next(GiftableObject.Count)];

                        int tasteInt = npc.getGiftTasteForThisItem(selectItem);
                        if (tasteInt == 6 || tasteInt == 4 && rand.NextBool()) continue;

                        dish = selectItem.ItemId;
                        name = selectItem.DisplayName;
                        price = selectItem.Price;
                        switch (tasteInt)
                        {
                            case 0:
                                taste = "love";
                                break;
                            case 2:
                                taste = "like";
                                break;
                            default:
                                taste = "neutral";
                                break;
                        }
                        break;
                    }

                }

                npc.modData[orderKey] = JsonConvert.SerializeObject(new DataOrder(dish, name, price, taste));

            } catch (Exception ex) { SMonitor.Log($"Error while adding special order for {npc.Name}: {ex.Message}"); }
        }
    }
}