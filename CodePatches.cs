using FoodStore;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Characters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.AccessControl;
using xTile.Dimensions;
using Object = StardewValley.Object;

namespace FoodStore
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry
    {
        private static void NPC_dayUpdate_Postfix(NPC __instance)
		{
			if (!Config.EnableMod)
				return;
			__instance.modData["hapyke.FoodStore/LastFood"] = "0";
			__instance.modData["hapyke.FoodStore/LastCheck"] = "0";
			__instance.modData["hapyke.FoodStore/LocationControl"] = ",";
            __instance.modData["hapyke.FoodStore/LastFoodTaste"] = "-1";
        }

		private static void NPC_performTenMinuteUpdate_Postfix(NPC __instance)
		{
			if (Config.EnableMod && !Game1.eventUp && __instance.currentLocation is not null && __instance.isVillager() && !WantsToEat(__instance) && Microsoft.Xna.Framework.Vector2.Distance(__instance.getTileLocation(), Game1.player.getTileLocation()) < 30)
			{
				if (Game1.random.NextDouble() < 0.15)
				{
                    Random random = new Random();
                    int randomIndex = random.Next(8);
					string text = "Got food";

                    if (__instance.modData["hapyke.FoodStore/LastFoodTaste"] == "0") //love
					{
                        text = SHelper.Translation.Get("foodstore.randomchat.love." + randomIndex);
                    }
                    else if (__instance.modData["hapyke.FoodStore/LastFoodTaste"] == "2") //like
                    {
                        text = SHelper.Translation.Get("foodstore.randomchat.like." + randomIndex);
                    }
                    else if (__instance.modData["hapyke.FoodStore/LastFoodTaste"] == "4") //dislike
                    {
                        text = SHelper.Translation.Get("foodstore.randomchat.dislike." + randomIndex);
                    }
                    else if (__instance.modData["hapyke.FoodStore/LastFoodTaste"] == "6") //hate
                    {
                        text = SHelper.Translation.Get("foodstore.randomchat.hate." + randomIndex);
                    }
                    else
                    {
                        text = SHelper.Translation.Get("foodstore.randomchat.neutral." + randomIndex);
                    }
                    __instance.showTextAboveHead(text);
				}
				return;
			}
			
			if (!Config.EnableMod || Game1.eventUp || __instance.currentLocation is null || !__instance.isVillager() || !WantsToEat(__instance))
				return;

            if (__instance.getTileLocation().X >= __instance.currentLocation.Map.DisplayWidth / 64 + 20 ||
                __instance.getTileLocation().Y >= __instance.currentLocation.Map.DisplayHeight / 64  + 20||
                __instance.getTileLocation().X <= -20 ||
                __instance.getTileLocation().Y <= -20 &&
                !__instance.IsReturningToEndPoint()
                )
            {
                __instance.returnToEndPoint();
                __instance.MovePosition(Game1.currentGameTime, Game1.viewport, __instance.currentLocation);
            }
			//Game1.chatBox.addErrorMessage(Game1.timeOfDay.ToString());
            PlacedFoodData food = GetClosestFood(__instance, __instance.currentLocation);
			TryToEatFood(__instance, food);
        }
        private static void FarmHouse_updateEvenIfFarmerIsntHere_Postfix(GameLocation __instance)
        {
            if (!Config.EnableMod || !Game1.IsMasterGame)
                return;
			foreach (NPC npc in __instance.characters)
			{
				if (npc.isVillager() && !npc.Name.EndsWith("_DA"))
				{

					NPC villager = npc;
					double moveToFoodChance = Config.MoveToFoodChance;

					if( Config.RushHour && (800 < Game1.timeOfDay  && Game1.timeOfDay < 930 || 1200 < Game1.timeOfDay && Game1.timeOfDay < 1300 || 1800 < Game1.timeOfDay && Game1.timeOfDay < 2000))
					{
						moveToFoodChance = moveToFoodChance * 1.5;
					}

                    if (villager != null && WantsToEat(villager) && Game1.random.NextDouble() < moveToFoodChance / 100f 
						&& __instance.furniture.Count > 0 
						)
					{
                        PlacedFoodData food = GetClosestFood(npc, __instance);
						if (food == null)
							return;
						if (TryToEatFood(villager, food))
							return;

                        Microsoft.Xna.Framework.Vector2 possibleLocation;
                        possibleLocation = food.foodTile;
						int tries = 0;
						int facingDirection = -3;


                        while (tries < 3)
						{
							int xMove = Game1.random.Next(-1, 2);
							int yMove = Game1.random.Next(-1, 2);

                            possibleLocation.X += xMove;
							if (xMove == 0)
							{
								possibleLocation.Y += yMove;
							}
							if (xMove == -1)
							{
								facingDirection = 1;
							}
							else if (xMove == 1)
							{
								facingDirection = 3;
							}
							else if (yMove == -1)
							{
								facingDirection = 2;
							}
							else if (yMove == 1)
							{
								facingDirection = 0;
							}
							if (__instance.isTileLocationTotallyClearAndPlaceable(possibleLocation))
							{
								break;
							}
							tries++;
						}
						if (tries < 3 
							&& TimeDelayCheck(villager)
							)
						{
                            Random random = new Random();
                            int randomIndex = random.Next(15);
                            string text = SHelper.Translation.Get("foodstore.coming." + randomIndex.ToString() , new { vName = villager.Name });

                            if (Game1.IsMultiplayer)
                            {
                                Game1.chatBox.globalInfoMessage($"   {text}");
                            }
                            else
                            {
                                Game1.chatBox.addInfoMessage($"   {text}");
                            }

                            villager.addedSpeed = 2 ;

                            npc.modData["hapyke.FoodStore/LastCheck"] = Game1.timeOfDay.ToString();
                            villager.temporaryController = new PathFindController(villager, __instance, new Point((int)possibleLocation.X, (int)possibleLocation.Y), facingDirection, (character, location) => villager.updateMovement(villager.currentLocation, Game1.currentGameTime));
                        }

                    }
                }
			}
		}
    }
}