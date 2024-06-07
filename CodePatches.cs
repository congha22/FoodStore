using HarmonyLib;
using MarketTown.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.GameData;
using StardewValley.GameData.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using xTile.Dimensions;
using xTile.ObjectModel;
using xTile.Tiles;
using static StardewValley.Minigames.CraneGame;
using static StardewValley.Minigames.TargetGame;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using Object = StardewValley.Object;

namespace MarketTown
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

            __instance.modData["hapyke.FoodStore/timeVisitShed"] = "0";
            __instance.modData["hapyke.FoodStore/shedEntry"] = "-1,-1";
            __instance.modData["hapyke.FoodStore/gettingFood"] = "false";
            __instance.modData["hapyke.FoodStore/LastFood"] = "0";
            __instance.modData["hapyke.FoodStore/LastCheck"] = "0";
            __instance.modData["hapyke.FoodStore/LocationControl"] = ",";
            __instance.modData["hapyke.FoodStore/LastFoodTaste"] = "-1";
            __instance.modData["hapyke.FoodStore/LastFoodDecor"] = "-1";
            __instance.modData["hapyke.FoodStore/LastSay"] = "0";
            __instance.modData["hapyke.FoodStore/TotalCustomerResponse"] = "0";
            __instance.modData["hapyke.FoodStore/inviteTried"] = "false";
            __instance.modData["hapyke.FoodStore/walkingBlock"] = "false";

        }
        private static void KidJoin(Dictionary<string, int> todaySelectedKid)
        {
            foreach (var kvp in todaySelectedKid)
            {
                NPC __instance = Game1.getCharacterFromName(kvp.Key);
                __instance.modData["hapyke.FoodStore/invited"] = "true";
                __instance.modData["hapyke.FoodStore/inviteDate"] = (Game1.stats.DaysPlayed - 1).ToString();
                Game1.DrawDialogue(new Dialogue(__instance,"key", SHelper.Translation.Get("foodstore.kidresponselist.yay")));
            }
        }
        private static void NPC_performTenMinuteUpdate_Postfix(NPC __instance)
        {
            if (!Config.EnableMod || __instance == null || !__instance.IsVillager)
                return;

            Random random = new Random();

            Farmer farmerInstance = Game1.player;
            NetStringDictionary<Friendship, NetRef<Friendship>> friendshipData = farmerInstance.friendshipData;

            try     //Walk around
            {
                if (__instance.currentLocation != null && __instance.currentLocation == Game1.player.currentLocation 
                    && ( Game1.player.currentLocation.characters.ToList().Count > 10 && random.NextDouble() < 0.4 || __instance.currentLocation.Name.Contains("Custom_MT_Island")) )
                {
                    if (( __instance.currentLocation.Name == "Farm" || __instance.currentLocation.Name == "FarmHouse") && __instance.modData["hapyke.FoodStore/invited"] == "true" && __instance.modData["hapyke.FoodStore/inviteDate"] == (Game1.stats.DaysPlayed - 1).ToString())
                    {
                        FarmOutside.WalkAround(__instance.Name);
                    }

                    if (__instance.Name.Contains("MT.Guest_") && !__instance.currentLocation.Name.Contains("Custom_MT_Island")
                        && __instance.currentLocation.Name != __instance.DefaultMap && !__instance.currentLocation.Name.Contains("BusStop"))
                    {
                        FarmOutside.WalkAround(__instance.Name);
                    }

                    if (__instance.Name.Contains("MT.Guest_") && __instance.currentLocation.Name.Contains("Custom_MT_Island")
                        && (Game1.timeOfDay < 620 || random.NextDouble() < 0.1))
                    {
                        FarmOutside.WalkAround(__instance.Name);
                    }
                }
            }
            catch { }


            if ( random.NextDouble() < 0.3 && Game1.hasLoadedGame && __instance.currentLocation == Game1.player.currentLocation
                && ( (friendshipData.TryGetValue(__instance.Name, out var friendship) && !__instance.Name.Contains("MT.Guest_") && friendshipData[__instance.Name].TalkedToToday) || __instance.Name.Contains("MT.Guest_") )
                && __instance.CurrentDialogue.Count == 0 && __instance.Name != "Krobus" && __instance.Name != "Dwarf"
                && !(Game1.currentLocation == null
                    || Game1.eventUp
                    || Game1.isFestival()
                    || Game1.IsFading()
                    || Game1.activeClickableMenu != null))
            {
                try
                {
                    int randomIndex = random.Next(1, 8);

                    string npcAge, npcManner, npcSocial;

                    int age = __instance.Age;
                    int manner = __instance.Manners;
                    int social = __instance.SocialAnxiety;

                    switch (age)
                    {
                        case 0:
                            npcAge = "adult.";
                            break;
                        case 1:
                            npcAge = "teens.";
                            break;
                        case 2:
                            npcAge = "child.";
                            break;
                        default:
                            npcAge = "adult.";
                            break;
                    }
                    switch (manner)
                    {
                        case 0:
                            npcManner = "neutral.";
                            break;
                        case 1:
                            npcManner = "polite.";
                            break;
                        case 2:
                            npcManner = "rude.";
                            break;
                        default:
                            npcManner = "neutral.";
                            break;
                    }
                    switch (social)
                    {
                        case 0:
                            npcSocial = "outgoing.";
                            break;
                        case 1:
                            npcSocial = "shy.";
                            break;
                        case 2:
                            npcSocial = "neutral.";
                            break;
                        default:
                            npcSocial = "neutral";
                            break;
                    }
                    if (__instance.Name.Contains("MT.Guest_") || !Game1.player.friendshipData[__instance.Name].IsMarried() && !Config.DisableChatAll && Int32.Parse(__instance.modData["hapyke.FoodStore/TotalCustomerResponse"]) < 2
                       )
                    {
                        __instance.CurrentDialogue.Push(new Dialogue(__instance, "key", SHelper.Translation.Get("foodstore.general." + npcAge + npcManner + npcSocial + randomIndex.ToString())));
                        __instance.modData["hapyke.FoodStore/TotalCustomerResponse"] = (Int32.Parse(__instance.modData["hapyke.FoodStore/TotalCustomerResponse"]) + 1).ToString();
                    }
                }
                catch (NullReferenceException) { }
            }

            try
            {
                if (__instance.Name.Contains("MT.Guest_") && !bool.Parse(__instance.modData["hapyke.FoodStore/gettingFood"])
                    && !__instance.currentLocation.Name.Contains("Custom_MT_Island") && !__instance.currentLocation.Name.Contains("BusStop")
                    && (Int32.Parse(__instance.modData["hapyke.FoodStore/timeVisitShed"]) <= (Game1.timeOfDay - Config.TimeStay) || Game1.timeOfDay > Config.CloseHour || Game1.timeOfDay >= 2500))
                {
                    __instance.Halt();


                    if (Int32.Parse(__instance.modData["hapyke.FoodStore/timeVisitShed"]) <= (Game1.timeOfDay - Config.TimeStay * 2) || Game1.timeOfDay - 100 >= Config.CloseHour)                 // Force Remove
                    {
                        Game1.warpCharacter(__instance, __instance.DefaultMap, __instance.DefaultPosition / 64);
                    }
                    else if (__instance.modData["hapyke.FoodStore/shedEntry"] != "-1,-1" && __instance.modData["hapyke.FoodStore/shedEntry"] != null)        // Walk to Remove
                    {
                        string[] coordinates = __instance.modData["hapyke.FoodStore/shedEntry"].Split(',');

                        var shedEntryPoint = Point.Zero;
                        if (coordinates.Length == 2 && int.TryParse(coordinates[0], out int x) && int.TryParse(coordinates[1], out int y))
                        {
                            shedEntryPoint = new Point(x, y);
                        }

                        __instance.temporaryController = new PathFindController(__instance, __instance.currentLocation, shedEntryPoint, 0, 
                            (character, location) => Game1.warpCharacter(__instance, __instance.DefaultMap, __instance.DefaultPosition / 64));
                    }
                    else Game1.warpCharacter(__instance, __instance.DefaultMap, __instance.DefaultPosition / 64);
                }
            } catch { }

            try             //Warp invited NPC to and away
            {
                if (!Utility.isFestivalDay() && __instance.modData["hapyke.FoodStore/inviteDate"] == (Game1.stats.DaysPlayed - 1).ToString())
                {
                    Random rand = new Random();
                    int index = rand.Next(7);
                    if (__instance.modData["hapyke.FoodStore/invited"] == "true" && Game1.timeOfDay == Config.InviteComeTime && __instance.currentLocation.Name != "Farm" && __instance.currentLocation.Name != "FarmHouse")
                    {
                        Game1.DrawDialogue(new Dialogue(__instance,"key", SHelper.Translation.Get("foodstore.visitcome." + index)));
                        Game1.globalFadeToBlack();

                        __instance.Halt();

                        var door = Game1.getFarm().GetMainFarmHouseEntry();
                        door.X += 3 - index;
                        door.Y += 2;
                        var name = "Farm";

                        Game1.warpCharacter(__instance, name, door);

                        __instance.faceDirection(2);

                        door.X--;
                        __instance.controller = new PathFindController(__instance, Game1.getFarm(), door, 2);

                    }

                    if (__instance.modData["hapyke.FoodStore/invited"] == "true" && (__instance.currentLocation.Name == "Farm" || __instance.currentLocation.Name == "FarmHouse")
                        && (Game1.timeOfDay == Config.InviteLeaveTime || Game1.timeOfDay == Config.InviteLeaveTime + 30 || Game1.timeOfDay == Config.InviteLeaveTime + 100 || Game1.timeOfDay == Config.InviteLeaveTime + 130))
                    {
                        Game1.DrawDialogue(new Dialogue(__instance,"key", SHelper.Translation.Get("foodstore.visitleave." + index)));
                        Game1.globalFadeToBlack();
                        
                        __instance.Halt();
                        __instance.modData["hapyke.FoodStore/invited"] = "false";
                        __instance.controller = null;
                        __instance.ClearSchedule();
                        __instance.ignoreScheduleToday = true;
                        Game1.warpCharacter(__instance, __instance.DefaultMap, __instance.DefaultPosition / 64);
                    }
                }
            }
            catch { }

            //Get taste and decoration score, call to SaySomething for NPC to send bubble text
            if (random.NextDouble() < 0.033 && Config.EnableMod && !Game1.eventUp && __instance.currentLocation is not null && !WantsToEat(__instance)
                && Microsoft.Xna.Framework.Vector2.Distance(__instance.Tile, Game1.player.Tile) < 15
                && __instance.modData["hapyke.FoodStore/LastFoodTaste"] != "-1" && Config.EnableDecor && !Config.DisableChatAll)
            {
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
                    NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.love." + randomIndex));
                    if (shareIdea < 0.3 + (lastDecor / 2)) SaySomething(__instance, __instance.currentLocation, lastTasteRate, lastDecorRate);
                }
                else if (lastTaste == 2) //like
                {
                    NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.like." + randomIndex));
                    if (shareIdea < 0.15 + (lastDecor / 2)) SaySomething(__instance, __instance.currentLocation, lastTasteRate, lastDecorRate);
                }
                else if (lastTaste == 4) //dislike
                {
                    NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.dislike." + randomIndex));
                    if (shareIdea < Math.Abs(-0.15 + (lastDecor / 2.5))) SaySomething(__instance, __instance.currentLocation, lastTasteRate, lastDecorRate);
                }
                else if (lastTaste == 6) //hate
                {
                    NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.hate." + randomIndex));
                    if (shareIdea < Math.Abs(-0.3 + (lastDecor / 2.5))) SaySomething(__instance, __instance.currentLocation, lastTasteRate, lastDecorRate);
                }
                else if (lastTaste == 8) //neutral
                {
                    NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.neutral." + randomIndex));
                    if (shareIdea < Math.Abs(lastDecor / 2.5)) SaySomething(__instance, __instance.currentLocation, lastTasteRate, lastDecorRate);
                }
                else { }
            }


            //Fix position, do eating food
            if (!Config.EnableMod || Game1.eventUp || __instance is null || __instance.currentLocation is null || !__instance.IsVillager || !WantsToEat(__instance))
                return;

            if (listNPCTodayPurchaseTime.TryGetValue(__instance, out int purchaseTime))
            {
                if (Game1.timeOfDay - 400 > purchaseTime)
                {
                    try
                    {
                        listNPCTodayPurchaseTime.Remove(__instance);
                        __instance.modData["hapyke.FoodStore/walkingBlock"] = "false";
                    } catch { }
                }
            }

            if (listNPCTodayPurchaseTime.ContainsKey(__instance)
                && !__instance.IsReturningToEndPoint()
                && __instance.Tile != __instance.DefaultPosition / 64
                && ( __instance.Tile.X > __instance.currentLocation.Map.Layers[0].LayerWidth - 1
                || __instance.Tile.Y > __instance.currentLocation.Map.Layers[0].LayerHeight - 1
                || __instance.Tile.X < 1
                || __instance.Tile.Y < 1)
                )
            {
                __instance.isCharging = true;
                __instance.returnToEndPoint();
                __instance.MovePosition(Game1.currentGameTime, Game1.viewport, __instance.currentLocation);
            }

            foreach (var pair in validBuildingObjectPairs)
            {
                Building building = pair.Building;
                string buildingType = pair.buildingType;

                var museumCheck = Game1.getLocationFromName(building.GetIndoorsName());

                if (museumCheck == __instance.currentLocation && buildingType == "museum") return;
            }

            DataPlacedFood food = GetClosestFood(__instance, __instance.currentLocation);
            TryToEatFood(__instance, food);
        }

        private static void FarmHouse_updateEvenIfFarmerIsntHere_Postfix(GameLocation __instance)
        {
            if (!Config.EnableMod)
                return;

            foreach (NPC npc in __instance.characters)
            {
                double talkChance = 0.00002;
                Random randomSayChance = new Random();

                //Send bubble about decoration, dish of the week
                if (npc.IsVillager && __instance == Game1.player.currentLocation
                    && randomSayChance.NextDouble() < talkChance
                    && WantsToSay(npc, 360)
                    && Utility.isThereAFarmerWithinDistance(new Microsoft.Xna.Framework.Vector2(npc.Tile.X, npc.Tile.Y), 20, npc.currentLocation) != null
                    && Config.EnableDecor
                    && !Config.DisableChatAll)
                {
                    DataPlacedFood tempFood = GetClosestFood(npc, npc.currentLocation);

                    int localNpcCount = 2;
                    if (Utility.isThereAFarmerOrCharacterWithinDistance(new Microsoft.Xna.Framework.Vector2(npc.Tile.X, npc.Tile.Y), 10, npc.currentLocation) != null) localNpcCount += 1;

                    Random random = new Random();
                    int randomIndex = random.Next(5);
                    if (tempFood != null)       //If have item for sale
                    {
                        var decorPointComment = GetDecorPoint(tempFood.foodTile, npc.currentLocation);


                        //Send decorPoint message

                        if (decorPointComment >= 0.2)
                        {
                            NPCShowTextAboveHead(npc, SHelper.Translation.Get("foodstore.gooddecor." + randomIndex.ToString()));
                            npc.modData["hapyke.FoodStore/LastSay"] = Game1.timeOfDay.ToString();
                            continue;
                        }
                        else if (decorPointComment <= 0)
                        {
                            NPCShowTextAboveHead(npc, SHelper.Translation.Get("foodstore.baddecor." + randomIndex.ToString()));
                            npc.modData["hapyke.FoodStore/LastSay"] = Game1.timeOfDay.ToString();
                            continue;
                        }
                    }
                    else if (tempFood == null && npc.currentLocation is FarmHouse)      //if in FarmHouse and have no item for sale
                    {
                        var decorPointComment = GetDecorPoint(npc.Tile, npc.currentLocation);


                        //Send decorPoint message

                        if (decorPointComment >= 0.2)
                        {
                            NPCShowTextAboveHead(npc, SHelper.Translation.Get("foodstore.gooddecor." + randomIndex.ToString()));
                            npc.modData["hapyke.FoodStore/LastSay"] = Game1.timeOfDay.ToString();
                            continue;
                        }
                        else if (decorPointComment <= 0)
                        {
                            NPCShowTextAboveHead(npc, SHelper.Translation.Get("foodstore.baddecor." + randomIndex.ToString()));
                            npc.modData["hapyke.FoodStore/LastSay"] = Game1.timeOfDay.ToString();
                            continue;
                        }
                    }

                    if (randomSayChance.NextDouble() < (talkChance / localNpcCount / 2))            //Send Dish of Week message
                    {
                        NPCShowTextAboveHead(npc, SHelper.Translation.Get("foodstore.dishweek." + randomIndex.ToString(), new { dishWeek = DishPrefer.dishWeek }));
                        npc.modData["hapyke.FoodStore/LastSay"] = Game1.timeOfDay.ToString();
                    }
                }


                //Control NPC walking to the food
                string text = "";
                if (npc.IsVillager && !npc.Name.EndsWith("_DA") && !npc.Name.StartsWith("RNPC"))
                {
                    double moveToFoodChance = Config.MoveToFoodChance;
                    try
                    {
                        if (npc.Name != null && npc.Name.Contains("MT.Guest_"))
                        {
                            if (npc.currentLocation.Name == "Custom_MT_Island") moveToFoodChance = Config.MoveToFoodChance * 2.5;
                            else if (npc.currentLocation.Name == "Custom_MT_Island_House") moveToFoodChance = Config.MoveToFoodChance * 3.5;
                            else moveToFoodChance = Config.ShedMoveToFoodChance;
                        }
                    }
                    catch { }

                    if (Config.RushHour && ((800 < Game1.timeOfDay && Game1.timeOfDay < 930) || (1200 < Game1.timeOfDay && Game1.timeOfDay < 1300) || (1800 < Game1.timeOfDay && Game1.timeOfDay < 2000)))
                    {
                        moveToFoodChance = moveToFoodChance * 1.5;
                    }

                    try
                    {
                        if (npc != null && WantsToEat(npc) && Game1.random.NextDouble() < moveToFoodChance / 100f && npc.modData["hapyke.FoodStore/walkingBlock"] == "false")
                        {
                            DataPlacedFood food = GetClosestFood(npc, __instance);

                            foreach (var pair in validBuildingObjectPairs)
                            {
                                Building building = pair.Building;
                                string buildingType = pair.buildingType;

                                var museumCheck = Game1.getLocationFromName(building.GetIndoorsName());

                                if (museumCheck == npc.currentLocation && buildingType == "museum") return;
                            }

                            if (food == null || (!Config.AllowRemoveNonFood && food.foodObject.Edibility <= 0 && (npc.currentLocation is Farm || npc.currentLocation is FarmHouse)))
                                return;
                            if (TryToEatFood(npc, food))
                            {
                                return;
                            }

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
                                if (!__instance.IsTileBlockedBy(possibleLocation))
                                {
                                    break;
                                }
                                tries++;
                            }
                            if (tries < 3 && TimeDelayCheck(npc))
                            {
                                //Send message
                                if (npc.currentLocation.Name != "Farm" && npc.currentLocation.Name != "FarmHouse" && !Config.DisableChat && !Config.DisableChatAll)
                                {
                                    Random random = new Random();
                                    int randomIndex = random.Next(15);
                                    text = SHelper.Translation.Get("foodstore.coming." + randomIndex.ToString(), new { vName = npc.displayName });

                                    Game1.chatBox.addInfoMessage(text);
                                    MyMessage messageToSend = new MyMessage(text);
                                    SHelper.Multiplayer.SendMessage(messageToSend, "ExampleMessageType");

                                }
                                //Update LastCheck
                                if (listNPCTodayPurchaseTime.ContainsKey(npc)) listNPCTodayPurchaseTime[npc] = Game1.timeOfDay;
                                else listNPCTodayPurchaseTime.Add(npc, Game1.timeOfDay);

                                npc.modData["hapyke.FoodStore/LastCheck"] = Game1.timeOfDay.ToString();
                                npc.modData["hapyke.FoodStore/walkingBlock"] = "true";
                                npc.modData["hapyke.FoodStore/gettingFood"] = "true";

                                //Villager control
                                npc.addedSpeed = 1;
                                npc.temporaryController = new PathFindController(npc, __instance, new Point((int)possibleLocation.X, (int)possibleLocation.Y), facingDirection,
                                    (character, location) =>
                                    {
                                        npc.modData["hapyke.FoodStore/walkingBlock"] = "false";
                                        npc.addedSpeed = 0;
                                        npc.updateMovement(npc.currentLocation, Game1.currentGameTime);
                                    });
                            }
                        }
                    } catch { }
                }
            }
        }

        // NPC order part 

        [HarmonyPatch(typeof(NPC), nameof(NPC.draw))]
        public class NPC_draw_Patch
        {
            private static int emoteBaseIndex = 424242;

            public static void Prefix(NPC __instance, ref bool __state)
            {
                if (!Config.EnableMod || !__instance.IsEmoting || __instance.CurrentEmote != emoteBaseIndex)
                    return;
                __state = true;
                __instance.IsEmoting = false;
            }

            public static void Postfix(NPC __instance, SpriteBatch b, float alpha, ref bool __state)
            {
                if (!Config.EnableMod || !__state)
                    return;
                __instance.IsEmoting = true;
                if (!__instance.modData.TryGetValue(orderKey, out string data))
                    return;
                if (!Config.RestaurantLocations.Contains(__instance.currentLocation.Name))
                {
                    __instance.modData.Remove(orderKey);
                    return;
                }
                DataOrder orderData = JsonConvert.DeserializeObject<DataOrder>(data);
                int emoteIndex = __instance.CurrentEmoteIndex >= emoteBaseIndex ? __instance.CurrentEmoteIndex - emoteBaseIndex : __instance.CurrentEmoteIndex;
                if (__instance.CurrentEmoteIndex >= emoteBaseIndex + 3)
                {
                    AccessTools.Field(typeof(Character), "currentEmoteFrame").SetValue(__instance, emoteBaseIndex);
                }
                Microsoft.Xna.Framework.Vector2 emotePosition = __instance.getLocalPosition(Game1.viewport);
                emotePosition.Y -= 32 + __instance.Sprite.SpriteHeight * 4;
                if (SHelper.Input.IsDown(Config.ModKey))
                {
                    SpriteText.drawStringWithScrollCenteredAt(b, orderData.dishName, (int)emotePosition.X + 32, (int)emotePosition.Y, "", 1f, default, 1);
                }
                else
                {
                    b.Draw(emoteSprite, emotePosition, new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(emoteIndex * 16 % Game1.emoteSpriteSheet.Width, emoteIndex * 16 / emoteSprite.Width * 16, 16, 16)), Color.White, 0f, Microsoft.Xna.Framework.Vector2.Zero, 4f, SpriteEffects.None, __instance.StandingPixel.Y / 10000f);
                    b.Draw(Game1.objectSpriteSheet, emotePosition + new Microsoft.Xna.Framework.Vector2(16, 8), GameLocation.getSourceRectForObject(orderData.dish), Color.White, 0f, Microsoft.Xna.Framework.Vector2.Zero, 2f, SpriteEffects.None, (__instance.StandingPixel.Y + 1) / 10000f);
                }

            }
        }

        [HarmonyPatch(typeof(Utility), nameof(Utility.checkForCharacterInteractionAtTile))]
        public class Utility_checkForCharacterInteractionAtTile_Patch
        {
            public static bool Prefix(Microsoft.Xna.Framework.Vector2 tileLocation, Farmer who)
            {
                if (!Config.EnableMod)
                    return true;
                NPC npc = Game1.currentLocation.isCharacterAtTile(tileLocation);
                if (npc is null || !npc.modData.TryGetValue(orderKey, out string data))
                    return true;
                if (!Config.RestaurantLocations.Contains(Game1.currentLocation.Name))
                {
                    npc.modData.Remove(orderKey);
                    return true;
                }
                DataOrder orderData = JsonConvert.DeserializeObject<DataOrder>(data);
                if (who.ActiveObject != null && who.ActiveObject.canBeGivenAsGift() && who.ActiveObject.Name == orderData.dishName)
                {
                    Game1.mouseCursor = 6;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.tryToReceiveActiveObject))]
        public class NPC_tryToReceiveActiveObject_Patch
        {
            public static bool Prefix(NPC __instance, Farmer who)
            {
                if (!Config.EnableMod || !Config.RestaurantLocations.Contains(__instance.currentLocation.Name) || !__instance.modData.TryGetValue(orderKey, out string data))
                    return true;
                DataOrder orderData = JsonConvert.DeserializeObject<DataOrder>(data);
                if (who.ActiveObject?.ParentSheetIndex == orderData.dish)
                {
                    if (!npcOrderNumbers.Value.ContainsKey(__instance.Name))
                    {
                        npcOrderNumbers.Value[__instance.Name] = 1;
                    }
                    else
                    {
                        npcOrderNumbers.Value[__instance.Name]++;
                    }
                    List<string> possibleReactions = new();
                    if (orderData.loved == "love")
                    {
                        possibleReactions.Add(SHelper.Translation.Get("loved-order-reaction-1"));
                        possibleReactions.Add(SHelper.Translation.Get("loved-order-reaction-2"));
                        possibleReactions.Add(SHelper.Translation.Get("loved-order-reaction-3"));
                    }
                    else if (orderData.loved == "like")
                    {
                        possibleReactions.Add(SHelper.Translation.Get("liked-order-reaction-1"));
                        possibleReactions.Add(SHelper.Translation.Get("liked-order-reaction-2"));
                        possibleReactions.Add(SHelper.Translation.Get("liked-order-reaction-3"));
                    }
                    else
                    {
                        possibleReactions.Add(SHelper.Translation.Get("neutral-order-reaction-1"));
                        possibleReactions.Add(SHelper.Translation.Get("neutral-order-reaction-2"));
                        possibleReactions.Add(SHelper.Translation.Get("neutral-order-reaction-3"));
                    }
                    string reaction = possibleReactions[Game1.random.Next(possibleReactions.Count)];

                    switch (who.FacingDirection)
                    {
                        case 0:
                            ((FarmerSprite)who.Sprite).animateBackwardsOnce(80, 50f);
                            break;
                        case 1:
                            ((FarmerSprite)who.Sprite).animateBackwardsOnce(72, 50f);
                            break;
                        case 2:
                            ((FarmerSprite)who.Sprite).animateBackwardsOnce(64, 50f);
                            break;
                        case 3:
                            ((FarmerSprite)who.Sprite).animateBackwardsOnce(88, 50f);
                            break;
                    }

                    if (Config.PriceMarkup > 0)
                    {
                        int price = (int)Math.Round(who.ActiveObject.Price * Config.PriceMarkup);
                        Game1.player.Money += price;
                    }

                    who.reduceActiveItemByOne();
                    who.completelyStopAnimatingOrDoingAction();
                    Game1.DrawDialogue(new Dialogue(__instance, "key", reaction));
                    __instance.faceTowardFarmerForPeriod(2000, 3, false, who);
                    __instance.modData.Remove(orderKey);
                    return false;
                }
                return true;
            }
            public static void Postfix(ref bool __result, NPC __instance, Farmer who, bool probe)
            {
                Farmer farmerInstance = Game1.player;
                NetStringDictionary<Friendship, NetRef<Friendship>> friendshipData = farmerInstance.friendshipData;
                friendshipData.TryGetValue(__instance.Name, out var friendship);

                if (who.ActiveItem != null && (who.ActiveItem.Name == "Invite Letter" || who.ActiveItem.Name == "Customer Note"))
                {
                    if (!probe)
                    {
                        var pc = new PlayerChat();

                        if (who.ActiveItem.Name == "Invite Letter") pc.OnPlayerSend(__instance, "invite");
                        if (who.ActiveItem.Name == "Customer Note") pc.OnPlayerSend(__instance, "last dish");

                        __instance.receiveGift(who.ActiveObject, who, false, 0f, false);
                        who.reduceActiveItemByOne();
                        who.completelyStopAnimatingOrDoingAction();
                        __instance.faceTowardFarmerForPeriod(4000, 3, faceAway: false, who);

                        __result = true;
                    }
                }
            }
        }
    }
}