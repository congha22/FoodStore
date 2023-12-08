﻿using FoodStore;
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
using StardewValley.GameData;
using StardewModdingAPI;
using static System.Net.Mime.MediaTypeNames;
using StardewValley.Menus;
using static StardewValley.Minigames.TargetGame;
using System.Xml.Linq;
using Netcode;
using StardewValley.Network;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Input;

namespace FoodStore
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry
    {
        public class DishPrefer
        {
            // Declare a public static variable
            public static string dishDay = "Farmer's Lunch";
            public static string dishWeek = "Farmer's Lunch";

        }
        private static void NPC_dayUpdate_Postfix(NPC __instance)
		{
			if (!Config.EnableMod)
				return;

            __instance.modData["hapyke.FoodStore/LastFood"] = "0";
			__instance.modData["hapyke.FoodStore/LastCheck"] = "0";
			__instance.modData["hapyke.FoodStore/LocationControl"] = ",";
            __instance.modData["hapyke.FoodStore/LastFoodTaste"] = "-1";
            __instance.modData["hapyke.FoodStore/LastFoodDecor"] = "-1";
            __instance.modData["hapyke.FoodStore/LastSay"] = "0";
            __instance.modData["hapyke.FoodStore/TotalCustomerResponse"] = "0";

            if (__instance.Name == "Lewis")
            {

                DishPrefer.dishDay = GetRandomDish();
                if (Game1.dayOfMonth == 1 || Game1.dayOfMonth == 8 || Game1.dayOfMonth == 15 || Game1.dayOfMonth == 22)
                {
                    DishPrefer.dishWeek = GetRandomDish();      //Get dish of the week

                    //Send thanks
                    Game1.chatBox.addInfoMessage(SHelper.Translation.Get("foodstore.thankyou"));
                    MyMessage messageToSend = new MyMessage(SHelper.Translation.Get("foodstore.thankyou"));
                    SHelper.Multiplayer.SendMessage(messageToSend, "ExampleMessageType");

                    //Send mod Note
                    string modNote = SHelper.Translation.Get("foodstore.note");
                    if (modNote != "")
                    {
                        Game1.chatBox.addInfoMessage(modNote);
                        messageToSend = new MyMessage(modNote);
                        SHelper.Multiplayer.SendMessage(messageToSend, "ExampleMessageType");
                    }

                    //Send hidden reveal
                    Random random = new Random();
                    int randomIndex = random.Next(11);
                    Game1.chatBox.addInfoMessage(SHelper.Translation.Get("foodstore.hidden." + randomIndex.ToString()));
                    messageToSend = new MyMessage(SHelper.Translation.Get("foodstore.hidden." + randomIndex.ToString()));
                    SHelper.Multiplayer.SendMessage(messageToSend, "ExampleMessageType");
                }
            }
        }

        private static void NPC_performTenMinuteUpdate_Postfix(NPC __instance)
        {

            //Validate player talked to NPC
            Farmer farmerInstance = Game1.player;
            NetStringDictionary<Friendship, NetRef<Friendship>> friendshipData = farmerInstance.friendshipData;

            if (friendshipData.TryGetValue(__instance.Name, out var friendship))
            {
                if (friendshipData[__instance.Name].TalkedToToday)
                {
                    try
                    {
                        if (__instance.CurrentDialogue.Count == 0)
                        {
                            Random random = new Random();
                            int randomIndex = random.Next(19);
                            __instance.CurrentDialogue.Push(new Dialogue(SHelper.Translation.Get("foodstore.customerresponse." + randomIndex), __instance));
                            __instance.modData["hapyke.FoodStore/TotalCustomerResponse"] = (Int32.Parse(__instance.modData["hapyke.FoodStore/TotalCustomerResponse"]) + 1).ToString();
                        }
                    }
                    catch (Exception ex) { }
                }
            }

            //Send dish of the day
            if (__instance.Name == "Lewis" && Game1.timeOfDay == 900)
            {
                Random random = new Random();
                int randomIndex = random.Next(10);
                Game1.chatBox.addInfoMessage(SHelper.Translation.Get("foodstore.dishday." + randomIndex.ToString(), new { dishToday = DishPrefer.dishDay }));
                MyMessage messageToSend = new MyMessage(SHelper.Translation.Get("foodstore.dishday." + randomIndex.ToString(), new { dishToday = DishPrefer.dishDay }));
                SHelper.Multiplayer.SendMessage(messageToSend, "ExampleMessageType");
            }

            //Get taste and decoration score, call to SaySomething for NPC to send bubble text
            if (Config.EnableMod && !Game1.eventUp && __instance.currentLocation is not null && __instance.isVillager() && !WantsToEat(__instance) && Microsoft.Xna.Framework.Vector2.Distance(__instance.getTileLocation(), Game1.player.getTileLocation()) < 30)
			{
                if (Game1.random.NextDouble() < 0.1)
                {
                    Random random = new Random();
                    int randomIndex = random.Next(8);
                    double shareIdea = random.NextDouble();

                    //Get Taste score, Decoration score
                    int lastTaste;
                    if (__instance.modData.ContainsKey("hapyke.FoodStore/LastFoodTaste")) lastTaste = Int32.Parse(__instance.modData["hapyke.FoodStore/LastFoodTaste"]);
                    else lastTaste = 8;

                    double lastDecor;
                    if (__instance.modData.ContainsKey("hapyke.FoodStore/LastFoodDecor")) lastDecor = Convert.ToDouble(__instance.modData["hapyke.FoodStore/LastFoodDecor"]);
                    else lastDecor = 0;

                    double lastTasteRate; // Variable to store the result
                    switch (lastTaste)
                    {
                        case 0:
                            lastTasteRate = 0.4;
                            break;

                        case 2:
                            lastTasteRate = 0.35;
                            break;

                        case 4:
                            lastTasteRate = 0.25;
                            break;

                        case 6:
                            lastTasteRate = 0.2;
                            break;

                        default:
                            lastTasteRate = 0.3;
                            break;
                    }

                    double lastDecorRate; // Variable to store the result
                    switch (lastDecor)
                    {
                        case double n when n >= -0.2 && n < 0:
                            lastDecorRate = -0.2;
                            break;

                        case double n when n >= 0 && n < 0.2:
                            lastDecorRate = 0;
                            break;

                        default:
                            lastDecorRate = 0.2;
                            break;
                    }

                    if (lastTaste == 0) //love
                    {
                        __instance.showTextAboveHead(SHelper.Translation.Get("foodstore.randomchat.love." + randomIndex));
                        if (shareIdea < 0.3 + lastDecor / 2) SaySomething(__instance, __instance.currentLocation, lastTasteRate, lastDecorRate);
                    }
                    else if (lastTaste == 2) //like
                    {
                        __instance.showTextAboveHead(SHelper.Translation.Get("foodstore.randomchat.like." + randomIndex));
                        if (shareIdea < 0.15 + lastDecor / 2) SaySomething(__instance, __instance.currentLocation, lastTasteRate, lastDecorRate);
                    }
                    else if (lastTaste == 4) //dislike
                    {
                        __instance.showTextAboveHead(SHelper.Translation.Get("foodstore.randomchat.dislike." + randomIndex));
                        if (shareIdea < Math.Abs( -0.15 + lastDecor / 2.5)) SaySomething(__instance, __instance.currentLocation, lastTasteRate, lastDecorRate);
                    }
                    else if (lastTaste == 6) //hate
                    {
                        __instance.showTextAboveHead(SHelper.Translation.Get("foodstore.randomchat.hate." + randomIndex));
                        if (shareIdea < Math.Abs(-0.3 + lastDecor / 2.5)) SaySomething(__instance, __instance.currentLocation, lastTasteRate, lastDecorRate);
                    }
                    else if (lastTaste == 8) //neutral
                    {
                        __instance.showTextAboveHead(SHelper.Translation.Get("foodstore.randomchat.neutral." + randomIndex));
                        if (shareIdea < Math.Abs( lastDecor / 2.5)) SaySomething(__instance, __instance.currentLocation, lastTasteRate, lastDecorRate);
                    }
                    else { }
                }
				return;
			}
			

            //Fix position, do eating food
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

            PlacedFoodData food = GetClosestFood(__instance, __instance.currentLocation);
			TryToEatFood(__instance, food);
        }
        private static void FarmHouse_updateEvenIfFarmerIsntHere_Postfix(GameLocation __instance)
        {
            foreach (NPC npc in __instance.characters)
			{
                double talkChance = 0.00003;
                Random randomSayChance = new Random();

                //Send bubble about decoration, decor, dish of the week
                if (randomSayChance.NextDouble() < talkChance
                    && WantsToSay(npc, 360)
                    && Utility.isThereAFarmerWithinDistance(new Microsoft.Xna.Framework.Vector2(npc.getTileLocation().X, npc.getTileLocation().Y), 20, npc.currentLocation) != null
                    && npc.isVillager())
                {
                    PlacedFoodData tempFood = GetClosestFood(npc, npc.currentLocation);

                    int localNpcCount = 2;
                    if (Utility.isThereAFarmerOrCharacterWithinDistance(new Microsoft.Xna.Framework.Vector2(npc.getTileLocation().X, npc.getTileLocation().Y), 10, npc.currentLocation) != null) localNpcCount += 1;

                    Random random = new Random();
                    int randomIndex = random.Next(5);
                    if (tempFood != null)
                    {
                        var decorPointComment = GetDecorPoint(tempFood.foodTile, npc.currentLocation);


                        //Send message

                        if (decorPointComment >= 0.2)
                        { 
                            npc.showTextAboveHead(SHelper.Translation.Get("foodstore.gooddecor." + randomIndex.ToString()), -1, 2, 5000);
                            npc.modData["hapyke.FoodStore/LastSay"] = Game1.timeOfDay.ToString();
                            continue;
                        }
                        else if (decorPointComment <= 0)
                        {
                            npc.showTextAboveHead(SHelper.Translation.Get("foodstore.baddecor." + randomIndex.ToString()), -1 , 2, 5000);
                            npc.modData["hapyke.FoodStore/LastSay"] = Game1.timeOfDay.ToString();
                            continue;
                        }
                    }

                    if (randomSayChance.NextDouble() < (talkChance / localNpcCount / 1.5)) 
                    {
                        npc.showTextAboveHead(SHelper.Translation.Get("foodstore.dishweek." + randomIndex.ToString(), new { dishWeek = DishPrefer.dishWeek }), -1, 2, 8000);
                        npc.modData["hapyke.FoodStore/LastSay"] = Game1.timeOfDay.ToString();
                    }
                }


                //Control NPC walking to the food
                string text = "";
                if (npc.isVillager() && !npc.Name.EndsWith("_DA"))
				{
                    NPC villager = npc;
					double moveToFoodChance = Config.MoveToFoodChance;

					if( Config.RushHour && (800 < Game1.timeOfDay  && Game1.timeOfDay < 930 || 1200 < Game1.timeOfDay && Game1.timeOfDay < 1300 || 1800 < Game1.timeOfDay && Game1.timeOfDay < 2000))
					{
						moveToFoodChance = moveToFoodChance * 1.5;
					}

                    if ((villager != null && WantsToEat(villager) && Game1.random.NextDouble() < moveToFoodChance / 100f && __instance.furniture.Count > 0))
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
                        if (tries < 3 && TimeDelayCheck(villager))
                        {
                            //Send message
                            if (!Config.DisableChat) 
                            {
                                Random random = new Random();
                                int randomIndex = random.Next(15);
                                text = SHelper.Translation.Get("foodstore.coming." + randomIndex.ToString(), new { vName = villager.Name });

                                Game1.chatBox.addInfoMessage(text);
                                MyMessage messageToSend = new MyMessage(text);
                                SHelper.Multiplayer.SendMessage(messageToSend, "ExampleMessageType");

                            }
                            //Update LastCheck
                            npc.modData["hapyke.FoodStore/LastCheck"] = Game1.timeOfDay.ToString();

                            //Villager control
                            villager.addedSpeed = 2;
                            villager.temporaryController = new PathFindController(villager, __instance, new Point((int)possibleLocation.X, (int)possibleLocation.Y), facingDirection, (character, location) => villager.updateMovement(villager.currentLocation, Game1.currentGameTime));
                        }
                    }
                }
            }
        }
    }
}