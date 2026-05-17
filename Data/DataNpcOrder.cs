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
        private static readonly HashSet<string> AlwaysAvailableOrderIds = new() { };

        private static string NormalizeOrderDishId(string dishId)
        {
            if (string.IsNullOrWhiteSpace(dishId))
                return string.Empty;

            string normalized = dishId.Trim();
            if (normalized.StartsWith("(O)", StringComparison.OrdinalIgnoreCase))
                return normalized.Substring(3);

            if (normalized.Length > 1
                && (normalized[0] == 'O' || normalized[0] == 'o')
                && normalized.Substring(1).All(char.IsDigit))
            {
                return normalized.Substring(1);
            }

            return normalized;
        }

        private static string ResolveObjectDataKey(string dishId)
        {
            if (string.IsNullOrWhiteSpace(dishId))
                return null;

            if (Game1.objectData.ContainsKey(dishId))
                return dishId;

            string normalized = NormalizeOrderDishId(dishId);
            if (Game1.objectData.ContainsKey(normalized))
                return normalized;

            string qualified = "(O)" + normalized;
            if (Game1.objectData.ContainsKey(qualified))
                return qualified;

            return null;
        }

        private bool CanPlayerCookOrderDish(string dishId)
        {
            if (!Config.OnlyKnowRecipe)
                return true;

            string objectDataKey = ResolveObjectDataKey(dishId);
            if (objectDataKey is null || Game1.player is null)
                return false;

            string recipeName = Game1.objectData[objectDataKey].Name;
            return Game1.player.cookingRecipes?.ContainsKey(recipeName) == true;
        }

        private static void AddAlwaysAvailableFallbackOrderDishes(List<string> dishIds)
        {
            foreach (string fallbackId in AlwaysAvailableOrderIds)
            {
                if (!dishIds.Any(id => NormalizeOrderDishId(id) == fallbackId))
                    dishIds.Add(fallbackId);
            }
        }

        private static void AddAlwaysAvailableFallbackOrderItems(List<Object> orderItems)
        {
            foreach (string fallbackId in AlwaysAvailableOrderIds)
            {
                if (orderItems.Any(item => NormalizeOrderDishId(item?.ItemId) == fallbackId))
                    continue;

                Object fallbackItem = ItemRegistry.Create<Object>("(O)" + fallbackId, allowNull: true)
                    ?? ItemRegistry.Create<Object>(fallbackId, allowNull: true);
                if (fallbackItem != null)
                    orderItems.Add(fallbackItem);
            }
        }

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
            Random rand = Game1.random;
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
                    string objDataKey = ResolveObjectDataKey(obj.ItemId);
                    if (!loves.Contains(obj.ItemId) && !likes.Contains(obj.ItemId) && !neutral.Contains(obj.ItemId) 
                        && objDataKey != null
                        && CraftingRecipe.cookingRecipes.ContainsKey(Game1.objectData[objDataKey].Name) 
                        && !Game1.NPCGiftTastes["Universal_Hate"].Split(' ').Contains(obj.ItemId) 
                        && npc.getGiftTasteForThisItem(obj) != 6 ) 
                    {
                        neutral.Add(obj.ItemId);
                    }
                }

                if (Config.OnlyKnowRecipe)
                {
                    loves = loves.Where(CanPlayerCookOrderDish).Distinct().ToList();
                    likes = likes.Where(CanPlayerCookOrderDish).Distinct().ToList();
                    neutral = neutral.Where(CanPlayerCookOrderDish).Distinct().ToList();

                    // Keep Fried Egg/Bread as base fallback only when no known dish is available.
                    if (!loves.Any() && !likes.Any() && !neutral.Any())
                        AddAlwaysAvailableFallbackOrderDishes(neutral);
                }


                Random rand = Game1.random;

                string taste = "like";
                string dish = "240";
                var name = "Farmer's Lunch";
                int price = 150;

                if (!marketOrder)
                {
                    if (loves.Any() && (rand.NextDouble() < Config.LovedDishChance || !likes.Any() && !neutral.Any()))
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

                    string dishDataKey = ResolveObjectDataKey(dish);
                    if (dishDataKey != null)
                    {
                        dish = NormalizeOrderDishId(dish);
                        name = Game1.objectData[dishDataKey].Name;
                        price = Game1.objectData[dishDataKey].Price;
                    }
                }
                else
                {
                    List<Object> marketOrderCandidates = GiftableObject;
                    if (Config.OnlyKnowRecipe)
                    {
                        marketOrderCandidates = GiftableObject.Where(item => item != null && CanPlayerCookOrderDish(item.ItemId)).ToList();

                        // Keep Fried Egg/Bread as base fallback only when no known candidate exists.
                        if (!marketOrderCandidates.Any())
                            AddAlwaysAvailableFallbackOrderItems(marketOrderCandidates);
                    }

                    int tried = 0;
                    bool selectedMarketDish = false;
                    while (tried < 5 && marketOrderCandidates.Any())
                    {
                        tried++;

                        var selectItem = marketOrderCandidates[rand.Next(marketOrderCandidates.Count)];

                        int tasteInt = npc.getGiftTasteForThisItem(selectItem);
                        if (tasteInt == 6 || tasteInt == 4 && rand.NextBool()) continue;

                        dish = NormalizeOrderDishId(selectItem.ItemId);
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
                        selectedMarketDish = true;
                        break;
                    }

                    if (!selectedMarketDish && marketOrderCandidates.Any())
                    {
                        var fallbackItem = marketOrderCandidates[rand.Next(marketOrderCandidates.Count)];
                        dish = NormalizeOrderDishId(fallbackItem.ItemId);
                        name = fallbackItem.DisplayName;
                        price = fallbackItem.Price;
                        taste = "neutral";
                    }

                }

                npc.modData[orderKey] = JsonConvert.SerializeObject(new DataOrder(dish, name, price, taste));

            } catch (Exception ex) { SMonitor.Log($"Error while adding special order for {npc.Name}: {ex.Message}"); }
        }
    }
}