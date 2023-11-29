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
			__instance.modData["aedenthorn.FoodOnTheTable/LastFood"] = "0";
			__instance.modData["hapyke.FoodStore/LastCheck"] = "0";
			__instance.modData["hapyke.FoodStore/LocationControl"] = ",";
        }

		private static void NPC_performTenMinuteUpdate_Postfix(NPC __instance)
		{
			if (!Config.EnableMod || Game1.eventUp || __instance.currentLocation is null || !__instance.isVillager() || !WantsToEat(__instance))
				return;

            if (__instance.getTileLocation().X >= __instance.currentLocation.Map.DisplayWidth / 64 + 20 ||
                __instance.getTileLocation().Y >= __instance.currentLocation.Map.DisplayHeight / 64  + 20||
                __instance.getTileLocation().X <= -20 ||
                __instance.getTileLocation().Y <= -20 &&
                !__instance.IsReturningToEndPoint()
                )
            {
                //Game1.chatBox.addErrorMessage("HERE" + __instance.Name);
                 __instance.returnToEndPoint();
                __instance.MovePosition(Game1.currentGameTime, Game1.viewport, __instance.currentLocation);

//                Game1.chatBox.addInfoMessage(x.ToString());



            }
            //__instance.CurrentDialogue.Push(new Dialogue("message", __instance));
            //Game1.drawDialogue(__instance);


            //Game1.warpCharacter(__instance, __instance.defaultMap, __instance.DefaultPosition);
            //__instance.updateMovement(__instance.currentLocation, Game1.currentGameTime);
            //Game1.chatBox.addInfoMessage($" HERE {__instance.Name}");


            //if (
            //	 //__instance.Name == "Caroline" 
            //	 //&&
            //	 (__instance.getTileLocation().X > 130 || __instance.getTileLocation().Y > 130)
            //	)

            //{
            //             //Game1.warpCharacter(__instance, "Town", new Point(72, 72));
            //             Game1.warpCharacter(__instance, "town", new Point(73, 73));
            //             //__instance.controller = null ;
            //	//__instance.temporaryController = null;
            //	__instance.clearSchedule();
            //             __instance.checkSchedule(Game1.timeOfDay);
            //             __instance.updateMovement(__instance.currentLocation, Game1.currentGameTime);
            //	__instance.moveCharacterOnSchedulePath();

            //	//Game1.chatBox.addMessage($"MASTER: {__instance.currentLocation.map.DisplayWidth / 64}, {__instance.currentLocation.map.DisplayHeight / 64}", Color.Green);

            //	//foreach (var entry in masterScheduleRawData)
            //	//{
            //	//    // Print key-value pairs
            //	//    Game1.chatBox.addMessage($"Key: {entry.Key}, Value: {entry.Value}", Color.Green);
            //	//    Game1.chatBox.addMessage($"---------------------------------------------", Color.White);
            //	//}





            //	//if (x != null)
            //	//{
            //	//    // Iterate through the schedule entries
            //	//    foreach (var scheduleEntry in x)
            //	//    {
            //	//        // Print schedule information
            //	//        Game1.chatBox.addInfoMessage($"Schedule for {Game1.currentSeason} - Day {Game1.dayOfMonth}:");

            //	//        // Print route information
            //	//        Game1.chatBox.addInfoMessage($"Route: {string.Join(" -> ", scheduleEntry.Value.route)}");

            //	//        // Print facing direction
            //	//        Game1.chatBox.addInfoMessage($"Facing Direction: {scheduleEntry.Value.facingDirection}");

            //	//        // Print end of route behavior
            //	//        Game1.chatBox.addInfoMessage($"End of Route Behavior: {scheduleEntry.Value.endOfRouteBehavior}");

            //	//        // Print end of route message
            //	//        Game1.chatBox.addInfoMessage($"End of Route Message: {scheduleEntry.Value.endOfRouteMessage}");

            //	//        // Add a separator between schedule entries
            //	//        Game1.chatBox.addInfoMessage("--------------------");
            //	//    }
            //	//}

            //	//__instance.changeSchedulePathDirection();
            //	//__instance.moveCharacterOnSchedulePath();
            //}
            PlacedFoodData food = GetClosestFood(__instance, __instance.currentLocation);
			TryToEatFood(__instance, food);
        }
        private static void FarmHouse_updateEvenIfFarmerIsntHere_Postfix(GameLocation __instance)
        {
            if (!Config.EnableMod || !Game1.IsMasterGame)
                return;
			foreach (NPC npc in __instance.characters)
			{
				if (npc.isVillager() && !npc.Name.EndsWith("_DA")
					//aaaa&& npc.Name == "Linus"
					)
				{

					NPC villager = npc;
                    if (villager != null && WantsToEat(villager) && Game1.random.NextDouble() < Config.MoveToFoodChance / 100f 
						&& __instance.furniture.Count > 0 
						//&& TimeDelayCheck(villager)
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

                            ArrayList heading = new ArrayList();
                            heading.AddRange(new string[]
                            {
								$"{villager.Name} is strolling our way, curious about our store's offerings.",
								$"Amidst their day, {villager.Name} is choosing to spend some time at our store.",
								$"{villager.Name} is making an unplanned visit to our store, breaking from the routine.",
								$"{villager.Name} is leisurely heading to our store, expecting a delightful experience.",
								$"Breaking free from tasks, {villager.Name} is on the way to explore our store.",
								$"{villager.Name} is navigating toward our store, seeking a unique shopping experience.",
								$"Spontaneously, {villager.Name} is making their way to our store for a pleasant surprise.",
								$"{villager.Name} is steering towards our store, eager to discover something new.",
								$"With excitement, {villager.Name} is heading to our store for an impromptu visit.",
								$"{villager.Name} is veering off course to visit our store, anticipating something special.",
								$"In the midst of their day, {villager.Name} decided to drop by our store for a while.",
								$"{villager.Name} is casually making their way to our store, anticipating a good time.",
								$"{villager.Name} is diverting from the norm to visit our store and enjoy the offerings.",
								$"{villager.Name} is making a spontaneous detour to our store, eager for a unique experience.",
								$"In a delightful twist, {villager.Name} is heading to our store for an unexpected visit."
                            });


                            Random random = new Random();
                            int randomIndex = random.Next(heading.Count);
                            var randomHeading = heading[randomIndex];

                            if (Game1.IsMultiplayer)
                            {
                                Game1.chatBox.globalInfoMessage($"   {randomHeading}");
                            }
                            else
                            {
                                Game1.chatBox.addInfoMessage($"   {randomHeading}");
                            }

                            villager.addedSpeed = 2 ;

                            npc.modData["hapyke.FoodStore/LastCheck"] = Game1.timeOfDay.ToString();
                            //villager.modData["aedenthorn.HaPyke/TimeControl"] = Game1.timeOfDay.ToString();
                            villager.temporaryController = new PathFindController(villager, __instance, new Point((int)possibleLocation.X, (int)possibleLocation.Y), facingDirection, (character, location) => villager.updateMovement(villager.currentLocation, Game1.currentGameTime));
                        }

                    }
                }
			}
		}
    }
}