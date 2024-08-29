using HarmonyLib;
using MarketTown.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.GameData.Characters;
using StardewValley.GameData.FruitTrees;
using StardewValley.GameData.LocationContexts;
using StardewValley.GameData.Shops;
using StardewValley.GameData.SpecialOrders;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace MarketTown
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry
    {
        private static void NPC_dayUpdate_Postfix(NPC __instance)
        {
            if (!__instance.IsVillager) return;

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
            __instance.modData["hapyke.FoodStore/stuckCounter"] = "0";
            __instance.modData["hapyke.FoodStore/festivalLastPurchase"] = "600";
            __instance.modData["hapyke.FoodStore/specialOrder"] = "-1,-1";
            __instance.modData["hapyke.FoodStore/shopOwnerToday"] = "-1,-1";
            __instance.modData["hapyke.FoodStore/islandSpecialOrderTile"] = "-1,-1";
            __instance.modData["hapyke.FoodStore/islandSpecialOrderTime"] = "0";
            __instance.modData["hapyke.FoodStore/lastStoreType"] = "";
            __instance.modData["hapyke.FoodStore/LastPurchase"] = "item";
            __instance.modData.Remove(orderKey);

        }

        private static void NPC_performTenMinuteUpdate_Postfix(NPC __instance)
        {
            if (!Game1.hasLoadedGame || __instance == null || !__instance.IsVillager || !GlobalNPCList.Contains(__instance.Name) || __instance.currentLocation == null || __instance.getMasterScheduleRawData() == null || Context.ScreenId > 0)
                return;
            Random random = new Random();

            // add extra dialogue
            NetStringDictionary<Friendship, NetRef<Friendship>> friendshipData = Game1.player.friendshipData;
            GameLocation __instanceLocation = __instance.currentLocation;

            if (random.NextDouble() < 0.5 && Game1.hasLoadedGame && __instanceLocation == Game1.player.currentLocation
                && ((friendshipData.TryGetValue(__instance.Name, out var friendship) && !__instance.Name.Contains("MT.Guest_") && friendshipData[__instance.Name].TalkedToToday) || __instance.Name.Contains("MT.Guest_"))
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

                    string __instanceAge, __instanceManner, __instanceSocial, __instanceHeartLevel;

                    int age = __instance.Age;
                    int manner = __instance.Manners;
                    int social = __instance.SocialAnxiety;
                    int heartLevel = 0;
                    if (Game1.player.friendshipData.ContainsKey(__instance.Name)) heartLevel = (int)Game1.player.friendshipData[__instance.Name].Points / 250;

                    __instanceAge = age == 0 ? "adult." : age == 1 ? "teens." : age == 2 ? "child." : "adult.";
                    __instanceManner = manner == 0 ? "neutral." : manner == 1 ? "polite." : manner == 2 ? "rude." : "neutral.";
                    __instanceSocial = social == 0 ? "outgoing." : social == 1 ? "shy." : social == 2 ? "neutral." : "neutral.";
                    __instanceHeartLevel = heartLevel <= 2 ? ".0" : heartLevel <= 5 ? ".3" : ".6";

                    if (__instance.Name.Contains("MT.Guest_") || !Game1.player.friendshipData[__instance.Name].IsMarried() && !Config.DisableChatAll && Int32.Parse(__instance.modData["hapyke.FoodStore/TotalCustomerResponse"]) < 2)
                    {
                        if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                        {
                            string relation = heartLevel <= 2 ? "stranger" : heartLevel <= 5 ? "acquaintance" : "best friend";
                            string bestFriend = "";
                            foreach (var f in Game1.player.friendshipData)
                                foreach (var f2 in f.Where(f2 => f2.Value.Points >= 750).OrderByDescending(f2 => f2.Value.Points).Take(3))
                                    bestFriend += $"{f2.Key}, ";

                            string data = $"Current location: {Game1.currentLocation.Name}; Current time: {Game1.timeOfDay}; Weather:{Game1.currentLocation.GetWeather().Weather}; Day of months: {Game1.dayOfMonth}; Current season: {Game1.currentLocation.GetSeason()};";

                            if (bestFriend != "") data += $"Player's closet friends: {bestFriend}; ";

                            conversationSummaries.TryGetValue(__instance.Name, out string history);
                            if (history != "") data += $"Previous user message: {history}";

                            Task.Run(() => ModEntry.SendMessageToAssistant(
                                npc: __instance,
                                userMessage: "",
                                systemMessage: $"As NPC {__instance.Name} ({__instanceAge}, {__instanceManner} manner, {__instanceSocial} social anxiety, and in {relation} relationship with player {Game1.player.Name}), you will start a new conversation in context of Stardew Valley game. You can use this information if relevant: {data}. Limit to under 30 words",
                                isConversation: true,
                                isForBubbleMessage: false)
                            );

                        }
                        else
                            __instance.CurrentDialogue.Push(new Dialogue(__instance, "key", SHelper.Translation.Get("foodstore.general." + __instanceAge + __instanceManner + __instanceSocial + randomIndex.ToString() + __instanceHeartLevel)));
                        __instance.modData["hapyke.FoodStore/TotalCustomerResponse"] = (Int32.Parse(__instance.modData["hapyke.FoodStore/TotalCustomerResponse"]) + 1).ToString();
                    }
                }
                catch (NullReferenceException) { }
            }

            //Get taste and decoration score, call to SaySomething for __instance to send bubble text
            if (random.NextDouble() < 0.033 && !Game1.eventUp && __instanceLocation is not null && !WantsToEat(__instance)
                && Microsoft.Xna.Framework.Vector2.Distance(__instance.Tile, Game1.player.Tile) < 15
                && __instance.modData["hapyke.FoodStore/LastFoodTaste"] != "-1" && Config.EnableDecor && !Config.DisableChatAll)
            {
                int randomIndex = random.Next(8);
                double shareIdea = random.NextDouble();

                //Get Taste score, Decoration score, Store Category
                int lastTaste;
                if (__instance.modData.ContainsKey("hapyke.FoodStore/LastFoodTaste")) lastTaste = Int32.Parse(__instance.modData["hapyke.FoodStore/LastFoodTaste"]);
                else lastTaste = 8;

                double lastDecor;
                if (__instance.modData.ContainsKey("hapyke.FoodStore/LastFoodDecor")) lastDecor = Convert.ToDouble(__instance.modData["hapyke.FoodStore/LastFoodDecor"]);
                else lastDecor = 0;

                string storeType = "-19309";
                if (__instance.modData.ContainsKey("hapyke.FoodStore/lastStoreType")) storeType = __instance.modData["hapyke.FoodStore/lastStoreType"];

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

                if (!Config.EnableDecor) lastDecorRate = 0.2;

                string storeTypeName = "";
                switch (storeType)
                {
                    case "-102":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-102");
                        break;
                    case "-100":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-100");
                        break;
                    case "-95":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-95");
                        break;
                    case "-81":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-81");
                        break;
                    case "-80":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-80");
                        break;
                    case "-79":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-79");
                        break;
                    case "-75":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-75");
                        break;
                    case "-74":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-74");
                        break;
                    case "-28":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-28");
                        break;
                    case "-27":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-27");
                        break;
                    case "-26":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-26");
                        break;
                    case "-22":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-22");
                        break;
                    case "-21":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-21");
                        break;
                    case "-20":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-20");
                        break;
                    case "-19":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-19");
                        break;
                    case "-18":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-18");
                        break;
                    case "-16":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-16");
                        break;
                    case "-15":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-15");
                        break;
                    case "-12":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-12");
                        break;
                    case "-8":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-8");
                        break;
                    case "-7":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-7");
                        break;
                    case "-6":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-6");
                        break;
                    case "-5":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-5");
                        break;
                    case "-4":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-4");
                        break;
                    case "-2":
                        storeTypeName = SHelper.Translation.Get("foodstore.storetype.-2");
                        break;
                    default:
                        storeTypeName = "";
                        break;
                }


                if (lastTaste == 0) //love
                {
                    if (shareIdea < 0.3 + (lastDecor / 2)) SaySomething(__instance, __instanceLocation, lastTasteRate, lastDecorRate);
                    else
                    {
                        if (storeTypeName != "") NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.loveWithType." + randomIndex, new { player = Game1.MasterPlayer.displayName, shopTypeName = storeTypeName }));
                        else
                        {
                            if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                            {
                                List<string> contextChoice = new List<string> { "love shopping there", "always has the best choice", "high-quality merchandise", "personalized shopping experience" };
                                string ageCategory = __instance.Age == 0 ? "adult" : __instance.Age == 1 ? "teens" : "child";
                                string manner = __instance.Manners == 0 ? "friendly." : __instance.Manners == 1 ? "polite." : __instance.Manners == 2 ? "rude." : "friendly.";
                                Task.Run(() => SendMessageToAssistant(
                                                npc: __instance,
                                                userMessage: $"How do you feel about my {Game1.MasterPlayer.Name}'s store?",
                                                systemMessage: $"As NPC {__instance.Name} ({ageCategory}, {manner}) in Stardew Valley, you will reply the user with a text only message under 25 words that your feeling with the {Game1.MasterPlayer.Name} store is that {contextChoice[random.Next(contextChoice.Count)]}")
                                            );
                            }
                            else
                                NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.love." + randomIndex));
                        }
                    }
                }
                else if (lastTaste == 2) //like
                {
                    if (shareIdea < 0.15 + (lastDecor / 2)) SaySomething(__instance, __instanceLocation, lastTasteRate, lastDecorRate);
                    else
                    {
                        if (storeTypeName != "") NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.likeWithType." + randomIndex, new { player = Game1.MasterPlayer.displayName, shopTypeName = storeTypeName }));
                        else
                        {
                            if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                            {
                                List<string> contextChoice = new List<string> { "kinda like shopping here", "decent selection of products", "pleasant shopping experience", "solid range of option" };
                                string ageCategory = __instance.Age == 0 ? "adult" : __instance.Age == 1 ? "teens" : "child";
                                string manner = __instance.Manners == 0 ? "friendly." : __instance.Manners == 1 ? "polite." : __instance.Manners == 2 ? "rude." : "friendly.";
                                Task.Run(() => SendMessageToAssistant(
                                                npc: __instance,
                                                userMessage: $"How do you feel about my {Game1.MasterPlayer.Name}'s store?",
                                                systemMessage: $"As NPC {__instance.Name} ({ageCategory}, {manner}) in Stardew Valley, you will reply the user with a text only message under 20 words that your feeling with the {Game1.MasterPlayer.Name} store is that {contextChoice[random.Next(contextChoice.Count)]}")
                                            );
                            }
                            else
                                NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.like." + randomIndex));
                        }
                    }
                }
                else if (lastTaste == 4) //dislike
                {
                    if (shareIdea < Math.Abs(-0.15 + (lastDecor / 2.5))) SaySomething(__instance, __instanceLocation, lastTasteRate, lastDecorRate);
                    else
                    {
                        if (storeTypeName != "") NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.dislikeWithType." + randomIndex, new { player = Game1.MasterPlayer.displayName, shopTypeName = storeTypeName }));
                        else
                        {
                            if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                            {
                                List<string> contextChoice = new List<string> { "not my first choice", "not very inviting atmosphere", "prices seem high for the quality", "not even that good" };
                                string ageCategory = __instance.Age == 0 ? "adult" : __instance.Age == 1 ? "teens" : "child";
                                string manner = __instance.Manners == 0 ? "friendly." : __instance.Manners == 1 ? "polite." : __instance.Manners == 2 ? "rude." : "friendly.";
                                Task.Run(() => SendMessageToAssistant(
                                                npc: __instance,
                                                userMessage: $"How do you feel about my {Game1.MasterPlayer.Name}'s store?",
                                                systemMessage: $"As NPC {__instance.Name} ({ageCategory}, {manner}) in Stardew Valley, you will reply the user with a text only message under 20 words that your feeling with the {Game1.MasterPlayer.Name} store is that {contextChoice[random.Next(contextChoice.Count)]}")
                                            );
                            }
                            else
                                NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.dislike." + randomIndex));
                        }
                    }
                }
                else if (lastTaste == 6) //hate
                {
                    if (shareIdea < Math.Abs(-0.3 + (lastDecor / 2.5))) SaySomething(__instance, __instanceLocation, lastTasteRate, lastDecorRate);
                    else
                    {
                        if (storeTypeName != "") NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.hateWithType." + randomIndex, new { player = Game1.MasterPlayer.displayName, shopTypeName = storeTypeName }));
                        else
                        {
                            if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                            {
                                List<string> contextChoice = new List<string> { "ultra low-quality items", "way to overpriced for the quality", "consistently bad experiences", "very disappointing overall" };
                                string ageCategory = __instance.Age == 0 ? "adult" : __instance.Age == 1 ? "teens" : "child";
                                string manner = __instance.Manners == 0 ? "friendly." : __instance.Manners == 1 ? "polite." : __instance.Manners == 2 ? "rude." : "friendly.";
                                Task.Run(() => SendMessageToAssistant(
                                                npc: __instance,
                                                userMessage: $"How do you feel about my {Game1.MasterPlayer.Name}'s store?",
                                                systemMessage: $"As NPC {__instance.Name} ({ageCategory}, {manner}) in Stardew Valley, you will reply the user with a text only message under 20 words that your feeling with the {Game1.MasterPlayer.Name} store is that {contextChoice[random.Next(contextChoice.Count)]}")
                                            );
                            }
                            else
                                NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.hate." + randomIndex));
                        }
                    }
                }
                else if (lastTaste == 8) //neutral
                {
                    if (shareIdea < Math.Abs(lastDecor / 2.5)) SaySomething(__instance, __instanceLocation, lastTasteRate, lastDecorRate);
                    else
                    {
                        if (storeTypeName != "") NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.neutralWithType." + randomIndex, new { player = Game1.MasterPlayer.displayName, shopTypeName = storeTypeName }));
                        else
                        {
                            if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                            {
                                List<string> contextChoice = new List<string> { "just minimum service and quality", "just meets expectations", "nothing exceptional", "just typical items" };
                                string ageCategory = __instance.Age == 0 ? "adult" : __instance.Age == 1 ? "teens" : "child";
                                string manner = __instance.Manners == 0 ? "friendly." : __instance.Manners == 1 ? "polite." : __instance.Manners == 2 ? "rude." : "friendly.";
                                Task.Run(() => SendMessageToAssistant(
                                                npc: __instance,
                                                userMessage: $"How do you feel about my {Game1.MasterPlayer.Name}'s store?",
                                                systemMessage: $"As NPC {__instance.Name} ({ageCategory}, {manner}) in Stardew Valley, you will reply the user with a text only message under 20 words that your feeling with the {Game1.MasterPlayer.Name} store is that {contextChoice[random.Next(contextChoice.Count)]}")
                                            );
                            }
                            else
                                NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.neutral." + randomIndex));
                        }
                    }
                }
                else { }
            }

            // Rest is on Master logic only
            if (!Game1.IsMasterGame) return;

            // Every island visitor will be faster
            if ( __instanceLocation.Name != null && ( __instanceLocation.Name.Contains("Custom_MT_Island") && random.NextDouble() < 0.5
                || __instanceLocation.GetParentLocation() != null && __instanceLocation.GetParentLocation().Name == "Custom_MT_Island" ) )
            {
                if (__instance.Age == 0) __instance.addedSpeed = random.Next(0, 2);
                else if (__instance.Age == 1) __instance.addedSpeed = random.Next(0, 3);
                else if (__instance.Age == 2) __instance.addedSpeed = random.Next(1, 3);
            }

            // This will generate a random schedule for island visitor in the next Game time change 
            if (__instanceLocation.Name != null && __instance.timerSinceLastMovement > 10000 && Game1.timeOfDay > 620 && Game1.timeOfDay % 20 == 0
                && ( random.NextDouble() < Config.IslandWalkAround || random.NextDouble() < Config.IslandWalkAround * 2 && Game1.timeOfDay > 2300 )
                && ( __instance.temporaryController == null && __instance.controller == null && !__instance.isMoving() &&  __instance.TilePoint == __instance.previousEndPoint || __instance.timerSinceLastMovement > 20000 )
                && ( __instanceLocation.Name.Contains("Custom_MT_Island") || __instanceLocation.GetParentLocation() != null && __instanceLocation.GetParentLocation().Name == "Custom_MT_Island")
                && ( !IsFestivalToday || Game1.timeOfDay > Config.FestivalTimeEnd + 100 || Game1.timeOfDay < Config.FestivalTimeStart - 130)
                 )
            {
                TenMinuteUpdateIslandNpc(__instance, __instanceLocation);
            }

            // for invited visitor
            if ((__instanceLocation.Name == "Farm" || __instanceLocation.Name == "FarmHouse") && __instance.modData["hapyke.FoodStore/invited"] == "true" && __instance.modData["hapyke.FoodStore/inviteDate"] == (Game1.stats.DaysPlayed - 1).ToString() 
                && !__instance.isMoving() && __instance.controller == null && __instance.temporaryController == null && __instance.timerSinceLastMovement >= 3000 && Game1.timeOfDay % 20 == 0)
            {
                var randomTile = FarmOutside.getRandomOpenPointInLocation(__instance, __instanceLocation, false).ToVector2();
                if (randomTile != Vector2.Zero)
                {
                    FarmOutside.AddNextMoveSchedulePoint(__instance, $"{ConvertToHour(Game1.timeOfDay + 10)}", $"{__instanceLocation.NameOrUniqueName}",
                        $"{randomTile.X}", $"{randomTile.Y}", $"{random.Next(0, 4)}");
                }
            }
            
            
            //======================================================================================================================================
            // Farm building store's visitors moving logic
            try
            {
                // leave the NPC when time up
                if (__instance.currentLocation.GetParentLocation() != null && __instance.currentLocation.GetParentLocation() == Game1.getFarm() 
                    && !bool.Parse(__instance.modData["hapyke.FoodStore/gettingFood"]) && __instance.modData["hapyke.FoodStore/timeVisitShed"] != "0"
                    && (Int32.Parse(__instance.modData["hapyke.FoodStore/timeVisitShed"]) <= (Game1.timeOfDay - Config.TimeStay) || Game1.timeOfDay > Config.CloseHour || Game1.timeOfDay >= 2500))
                {
                    CleanNpc(__instance);
                    __instance.reloadDefaultLocation();
                    __instance.TryLoadSchedule();

                    var schedule = __instance.Schedule;
                    var lastLocation = __instance.DefaultMap;
                    var lastPoint = __instance.DefaultPosition / 64;
                    var lastDirection = __instance.DefaultFacingDirection;
                    CleanNpc(__instance);

                    // Get the tile location where NPC should be at the current time
                    if (schedule != null && schedule.Count > 0)
                    {
                        foreach (var piece in schedule)
                        {
                            if (piece.Key > Game1.timeOfDay) break;

                            SchedulePathDescription description = piece.Value;
                            lastLocation = description.targetLocationName;
                            lastPoint = description.targetTile.ToVector2();
                            lastDirection = description.facingDirection;
                        }
                    }
                    else Game1.warpCharacter(__instance, lastLocation, lastPoint);

                    if (Int32.Parse(__instance.modData["hapyke.FoodStore/timeVisitShed"]) <= (Game1.timeOfDay - Config.TimeStay * 2) || Game1.timeOfDay - 100 >= Config.CloseHour)                 // Force Remove
                    {
                        Game1.warpCharacter(__instance, lastLocation, lastPoint);
                        __instance.faceDirection(lastDirection);
                        CleanNpc(__instance);
                        __instance.TryLoadSchedule();
                    }
                    else if (__instance.modData["hapyke.FoodStore/shedEntry"] != null)        // Walk to Remove
                    {
                        CleanNpc(__instance);
                        string[] coordinates = __instance.modData["hapyke.FoodStore/shedEntry"].Split(',');

                        var shedEntryPoint = Point.Zero;
                        if (coordinates.Length == 2 && int.TryParse(coordinates[0], out int x) && int.TryParse(coordinates[1], out int y))
                        {
                            shedEntryPoint = new Point(x, y);
                        }
                        __instance.temporaryController = new PathFindController(__instance, __instanceLocation, shedEntryPoint, 0,
                            (character, location) =>
                            {
                                Game1.warpCharacter(__instance, lastLocation, lastPoint);

                                CleanNpc(__instance);
                                __instance.faceDirection(lastDirection);
                                __instance.TryLoadSchedule();
                            } );
                    }
                    else
                    {
                        Game1.warpCharacter(__instance, lastLocation, lastPoint);

                        __instance.faceDirection(lastDirection);
                        CleanNpc(__instance);
                        __instance.TryLoadSchedule();
                    }

                    string[] specialOrderCoor = __instance.modData["hapyke.FoodStore/specialOrder"].Split(',');
                    Vector2 specialOrderTile = new Vector2(int.Parse(specialOrderCoor[0]), int.Parse(specialOrderCoor[1]));

                    if (RestaurantSpot.ContainsKey(__instanceLocation))
                    {
                        var tileList = RestaurantSpot[__instanceLocation];
                        if (tileList.Contains(specialOrderTile)) tileList.Remove(specialOrderTile);
                    }

                    __instance.modData["hapyke.FoodStore/specialOrder"] = "-1,-1";
                    __instance.modData.Remove(orderKey);
                }
                // else random move around
                else if (__instanceLocation.GetParentLocation() != null && __instanceLocation.GetParentLocation() == Game1.getFarm()
                    && __instance.modData["hapyke.FoodStore/specialOrder"] == "-1,-1" && __instance.modData["hapyke.FoodStore/invited"] == "false"
                    && !__instance.isMoving() && __instance.controller == null && __instance.temporaryController == null && __instance.timerSinceLastMovement >= 3000
                    && (Game1.player.friendshipData.ContainsKey(__instance.Name) && !Game1.player.friendshipData[__instance.Name].IsMarried() && !Game1.player.friendshipData[__instance.Name].IsRoommate() || !Game1.player.friendshipData.ContainsKey(__instance.Name)))
                {
                    var randomTile = FarmOutside.getRandomOpenPointInLocation(__instance, __instanceLocation, false, true).ToVector2();
                    if (randomTile != Vector2.Zero)
                    {
                        FarmOutside.AddNextMoveSchedulePoint(__instance, $"{ConvertToHour(Game1.timeOfDay + 10)}", $"{__instanceLocation.NameOrUniqueName}",
                            $"{randomTile.X}", $"{randomTile.Y}", $"{random.Next(0, 4)}");
                    }
                }
            }
            catch { }

            try             //Warp invited guest __instance to and away
            {
                if (!Utility.isFestivalDay() && __instance.modData["hapyke.FoodStore/inviteDate"] == (Game1.stats.DaysPlayed - 1).ToString())
                {
                    Random rand = new Random();
                    int index = rand.Next(7);
                    if (__instance.modData["hapyke.FoodStore/invited"] == "true" && Game1.timeOfDay == Config.InviteComeTime && __instanceLocation.Name != "Farm" && __instanceLocation.Name != "FarmHouse" && !IslandNPCList.Contains(__instance.Name))
                    {
                        if (Game1.player.currentLocation == Game1.getFarm() || Game1.player.currentLocation == Game1.getLocationFromName("FarmHouse"))
                        {
                            Game1.DrawDialogue(new Dialogue(__instance, "key", SHelper.Translation.Get("foodstore.visitcome." + index)));
                            Game1.globalFadeToBlack();
                        }

                        UpdateFurnitureTilePathProperties(Game1.getFarm());
                        UpdateFurnitureTilePathProperties(Game1.getLocationFromName("FarmHouse"));

                        FarmOutside.UpdateRandomLocationOpenTile(Game1.getFarm());
                        FarmOutside.UpdateRandomLocationOpenTile(Game1.getLocationFromName("FarmHouse"));

                        var door = Game1.getFarm().GetMainFarmHouseEntry();
                        door.X += 3 - index;
                        door.Y += 2;
                        var name = "Farm";

                        Game1.warpCharacter(__instance, name, door);
                        CleanNpc(__instance);

                        __instance.faceDirection(2);

                        door.X--;
                        TodayFriendVisited++;
                    }

                    if (__instance.modData["hapyke.FoodStore/invited"] == "true" && (__instanceLocation.Name == "Farm" || __instanceLocation.Name == "FarmHouse")
                        && (Game1.timeOfDay == Config.InviteLeaveTime || Game1.timeOfDay == Config.InviteLeaveTime + 30 || Game1.timeOfDay == Config.InviteLeaveTime + 100 || Game1.timeOfDay == Config.InviteLeaveTime + 130))
                    {
                        if (Game1.player.currentLocation == Game1.getFarm() || Game1.player.currentLocation == Game1.getLocationFromName("FarmHouse"))
                        {
                            Game1.DrawDialogue(new Dialogue(__instance, "key", SHelper.Translation.Get("foodstore.visitleave." + index)));
                            Game1.globalFadeToBlack();
                        }
                        __instance.modData["hapyke.FoodStore/invited"] = "false";
                        CleanNpc(__instance);
                        Game1.warpCharacter(__instance, __instance.DefaultMap, __instance.DefaultPosition / 64);
                        CleanNpc(__instance);
                    }
                }
            }
            catch { }


            //Fix position, do eating food
            if (Game1.eventUp || __instance is null || __instanceLocation is null || !__instance.IsVillager || !GlobalNPCList.Contains(__instance.Name) || !WantsToEat(__instance) || !Game1.IsMasterGame)
                return;


            DataPlacedFood food = GetClosestFood(__instance, __instanceLocation);
            TryToEatFood(__instance, food);
        }


        // NPC order part 
        [HarmonyPatch(typeof(NPC), nameof(NPC.draw))]
        public class NPC_draw_Patch
        {
            private static int emoteBaseIndex = 424242;

            public static void Prefix(NPC __instance, ref bool __state)
            {
                if (!__instance.IsEmoting || __instance.CurrentEmote != emoteBaseIndex)
                    return;
                __state = true;
                __instance.IsEmoting = false;
            }

            public static void Postfix(NPC __instance, SpriteBatch b, float alpha, ref bool __state)
            {
                if (!__state)
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
                    ParsedItemData? i = ItemRegistry.GetData(orderData.dish);
                    if (i != null)
                    {
                        b.Draw(emoteSprite, emotePosition, new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(emoteIndex * 16 % Game1.emoteSpriteSheet.Width, emoteIndex * 16 / emoteSprite.Width * 16, 16, 16)), Color.White, 0f, Microsoft.Xna.Framework.Vector2.Zero, 4f, SpriteEffects.None, __instance.StandingPixel.Y / 10000f);
                        b.Draw(i.GetTexture(), emotePosition + new Vector2(16, 8), i.GetSourceRect(), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, (__instance.StandingPixel.Y + 1) / 10000f);
                    }
                    else return;
                }

            }
        }

        [HarmonyPatch(typeof(Utility), nameof(Utility.checkForCharacterInteractionAtTile))]
        public class Utility_checkForCharacterInteractionAtTile_Patch
        {
            public static bool Prefix(Microsoft.Xna.Framework.Vector2 tileLocation, Farmer who)
            {
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
            public static bool Prepare()
            {
                return Context.ScreenId == 0;
            }

            public static bool Prefix(NPC __instance, Farmer who)
            {
                if (!Config.RestaurantLocations.Contains(__instance.currentLocation.Name) || !__instance.modData.TryGetValue(orderKey, out string data))
                    return true;
                DataOrder orderData = JsonConvert.DeserializeObject<DataOrder>(data);
                if (who.ActiveObject?.ItemId == orderData.dish)
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
                        AddToPlayerFunds((int)(price * Config.MoneyModifier * (Config.UltimateChallenge ? 4 : 1)));
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
                try
                {
                    if (who.ActiveItem != null && (who.ActiveItem.Name == "Invite Letter" || who.ActiveItem.Name == "Customer Note"))
                    {
                        if (!probe)
                        {
                            var pc = new PlayerChat();

                            if (who.ActiveItem.Name == "Invite Letter")
                            {
                                pc.OnPlayerSend(__instance, "invite");
                                __instance.receiveGift(who.ActiveObject, who, false, 0f, false);
                                who.reduceActiveItemByOne();
                                who.completelyStopAnimatingOrDoingAction();
                                __instance.faceTowardFarmerForPeriod(4000, 3, faceAway: false, who);

                                __result = true;
                            }
                            if (who.ActiveItem.Name == "Customer Note")
                            {
                                if (UpdateCustomerNote(__instance, who))
                                {
                                    __instance.receiveGift(who.ActiveObject, who, false, 0f, false);
                                    who.reduceActiveItemByOne();
                                    who.completelyStopAnimatingOrDoingAction();
                                    __instance.faceTowardFarmerForPeriod(4000, 3, faceAway: false, who);
                                }
                                __result = true;
                            }

                        }
                    }
                }
                catch { }
            }

            public static bool UpdateCustomerNote(NPC __instance, Farmer who)
            {
                Random random = new Random();

                //Get Taste score, Decoration score
                int lastTaste = -1;
                if (__instance.modData.ContainsKey("hapyke.FoodStore/LastFoodTaste")) lastTaste = Int32.Parse(__instance.modData["hapyke.FoodStore/LastFoodTaste"]);


                double lastDecor = -1;
                if (__instance.modData.ContainsKey("hapyke.FoodStore/LastFoodDecor")) lastDecor = Convert.ToDouble(__instance.modData["hapyke.FoodStore/LastFoodDecor"]);

                if (lastTaste == -1 || lastDecor == -1) // have not buy anything yet
                {
                    NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.asktaste.empty." + random.Next(3)));
                    return true;
                }
                double lastTasteRate; // get the taste point
                lastTasteRate = lastTaste == 0 ? 0.4 : lastTaste == 2 ? 0.35 : lastTaste == 4 ? 0.25 : lastTaste == 6 ? 0.2 : 0.3;

                double lastDecorRate; // get the decor point
                lastDecorRate = lastDecor >= -0.2 && lastDecor < 0 ? -0.2 : lastDecor >= 0 && lastDecor < 0.2 ? 0 : 0.2;


                if (!Config.EnableDecor) lastDecorRate = 0.2;

                if (!TodayCustomerNoteName.Contains(__instance.Name) && (lastTasteRate >= 0.3 && lastDecorRate > 0 || lastTasteRate > 0.3 && lastDecorRate >= 0))          // Normal food, good decor or like food, normal decor
                {
                    if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                    {
                        int wordCount = 20;
                        List<string> contextChoice = new List<string> { "amazing.", "the best ever.", "loved.", "absolute amazing." };
                        if (lastTasteRate > 0.3 && lastDecorRate > 0)
                        {
                            contextChoice = new List<string> { "great quality and nicely decoration.", "premium quality with a refined stylish.", "outstanding quality and visually appealing.", "first-rate quality and an attractive style." };
                            wordCount = 23;
                        }
                        string ageCategory = __instance.Age == 0 ? "adult" : __instance.Age == 1 ? "teens" : "child";
                        string manner = __instance.Manners == 0 ? "friendly." : __instance.Manners == 1 ? "polite." : __instance.Manners == 2 ? "rude." : "friendly.";
                        Task.Run(() => SendMessageToAssistant(
                                        npc: __instance,
                                        userMessage: $"What you think about the {__instance.modData["hapyke.FoodStore/LastPurchase"]} you purchased?",
                                        systemMessage: $"As NPC {__instance.Name} ({ageCategory}, {manner}) in Stardew Valley, you will reply the user with a text only message under {wordCount} words that your feeling with the {__instance.modData["hapyke.FoodStore/LastPurchase"]} is {contextChoice[random.Next(contextChoice.Count)]}")
                                    );
                        TodayCustomerNoteName.Add(__instance.Name);
                        TodayCustomerNoteYes++;
                        TodayCustomerNote++;
                        return true;
                    }
                    else
                    {
                        NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.customernote.yes." + random.Next(7).ToString()), true);

                        TodayCustomerNoteName.Add(__instance.Name);
                        TodayCustomerNoteYes++;
                        TodayCustomerNote++;
                        return true;
                    }
                }
                else if (!TodayCustomerNoteName.Contains(__instance.Name) && (lastTasteRate <= 0.25 || lastTasteRate == 0.3 && lastDecorRate < 0))     // Dishlike food, or neutral food and bad decor
                {
                    if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                    {
                        List<string> contextChoice = new List<string> { "much disappointed", "never again", "disgusting", "absolute terrible" };
                        if (lastTasteRate <= 0.25 && lastDecorRate == -0.2) 
                            contextChoice = new List<string> { "poorly made and unappealing", "shoddy quality and a lack of style", "dull decoration and lack of quality"};

                        string ageCategory = __instance.Age == 0 ? "adult" : __instance.Age == 1 ? "teens" : "child";
                        string manner = __instance.Manners == 0 ? "friendly." : __instance.Manners == 1 ? "polite." : __instance.Manners == 2 ? "rude." : "friendly.";
                        Task.Run(() => SendMessageToAssistant(
                                        npc: __instance,
                                        userMessage: $"What you think about the {__instance.modData["hapyke.FoodStore/LastPurchase"]} you purchased?",
                                        systemMessage: $"As NPC {__instance.Name} ({ageCategory}, {manner}) in Stardew Valley, you will reply the user with a text only message under 20 words that your feeling with the {__instance.modData["hapyke.FoodStore/LastPurchase"]} is {contextChoice[random.Next(contextChoice.Count)]}")
                                    );
                        TodayCustomerNoteName.Add(__instance.Name);
                        TodayCustomerNoteYes++;
                        TodayCustomerNote++;
                        return true;
                    }
                    else
                    {
                        NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.customernote.no." + random.Next(7).ToString()), true);
                        TodayCustomerNoteName.Add(__instance.Name);
                        TodayCustomerNoteNo++;
                        TodayCustomerNote++;
                        return true;
                    }
                }
                else if (!TodayCustomerNoteName.Contains(__instance.Name))          // Other case
                {
                    if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                    {
                        List<string> contextChoice = new List<string> { "normal", "fine", "middle class", "just meet the expectation" };
                        string ageCategory = __instance.Age == 0 ? "adult" : __instance.Age == 1 ? "teens" : "child";
                        string manner = __instance.Manners == 0 ? "friendly." : __instance.Manners == 1 ? "polite." : __instance.Manners == 2 ? "rude." : "friendly.";
                        Task.Run(() => SendMessageToAssistant(
                                        npc: __instance,
                                        userMessage: $"What you think about the {__instance.modData["hapyke.FoodStore/LastPurchase"]} you purchased?",
                                        systemMessage: $"You are NPC {__instance.Name} ({ageCategory}, {manner}) in Stardew Valley, you will reply the user with a text only message under 20 words that your feeling with the {__instance.modData["hapyke.FoodStore/LastPurchase"]} is {contextChoice[random.Next(contextChoice.Count)]}")
                                    );
                        TodayCustomerNoteName.Add(__instance.Name);
                        TodayCustomerNoteYes++;
                        TodayCustomerNote++;
                        return true;
                    }
                    else
                    {
                        NPCShowTextAboveHead(__instance, SHelper.Translation.Get("foodstore.randomchat.neutral." + random.Next(8)), true);
                        TodayCustomerNoteName.Add(__instance.Name);
                        TodayCustomerNoteNone++;
                        TodayCustomerNote++;
                        return true;
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(FruitTree), nameof(FruitTree.dayUpdate))]      // Set Fruit tree grow
        public class FruitTree_Patch
        {
            static void Postfix(FruitTree __instance)
            {
                if (__instance != null && __instance.Location != null && __instance.Location.Name == "Custom_MT_Island"
                    && Config.IslandPlantBoost && Game1.random.NextDouble() < Config.IslandPlantBoostChance && __instance.growthStage.Value < 4)
                {
                    __instance.daysUntilMature.Value -= __instance.growthRate.Value;
                }
            }
        }

        [HarmonyPatch(typeof(FruitTree), nameof(FruitTree.TryAddFruit))]    // Set Fruit tree fruit
        public class FruitTree_TryAddFruit_Patch
        {
            public static bool Prepare()
            {
                return !SHelper.ModRegistry.IsLoaded("chiccen.FruitTreeTweaks");
            }

            static bool Prefix(FruitTree __instance, ref bool __result)
            {
                if (__instance != null && __instance.Location != null && __instance.Location.Name.Contains("Custom_MT_Island") && Config.IslandPlantBoost)
                {
                    if (!__instance.stump.Value && __instance.daysUntilMature.Value <= 0 && __instance.fruit.Count < 9
                        && (__instance.IsInSeasonHere() || __instance.Location.GetSeason().ToString() == "Spring"
                        || (__instance.struckByLightningCountdown.Value > 0 && !__instance.IsWinterTreeHere())))
                    {
                        Random random = new Random();
                        FruitTreeData data = __instance.GetData();
                        if (data?.Fruit != null && (__instance.IsInSeasonHere() || !__instance.IsInSeasonHere() && random.NextDouble() < Config.IslandPlantBoostChance))
                        {
                            foreach (FruitTreeFruitData item2 in data.Fruit)
                            {
                                Item item = InvokeTryCreateFruit(__instance, item2);
                                if (item != null)
                                {
                                    __instance.fruit.Add(item);
                                    // Add a chance to add another fruit
                                    if (__instance.IsInSeasonHere() && random.NextDouble() < Config.IslandPlantBoostChance)
                                    {
                                        Item extraItem = InvokeTryCreateFruit(__instance, item2);
                                        if (extraItem != null)
                                            __instance.fruit.Add(extraItem);
                                    }

                                    __result = true;
                                    return false;
                                }
                            }
                        }
                    }
                    __result = false;
                    return false;
                }
                return true;
            }

            static Item InvokeTryCreateFruit(FruitTree tree, FruitTreeFruitData drop)
            {
                MethodInfo method = typeof(FruitTree).GetMethod("TryCreateFruit", BindingFlags.NonPublic | BindingFlags.Instance);
                return (Item)method.Invoke(tree, new object[] { drop });
            }
        }

        [HarmonyPatch(typeof(FruitTree), nameof(FruitTree.draw))]
        public class FruitTree_Draw_Patch
        {
            public static bool Prepare()
            {
                return !SHelper.ModRegistry.IsLoaded("chiccen.FruitTreeTweaks");
            }

            static void Postfix(FruitTree __instance, SpriteBatch spriteBatch)
            {
                var tileLocation = __instance.Tile;
                List<Vector2> tileOffset = new List<Vector2> {new(0,0), new(0,0), new(0,0), new(-55, -135), new(-20, -245), new(10, -230), new(75, -155), new(0, -195), new(50, -195), new(-30, -180), new(50, -180), new(50, -180), new(50, -180), new(50, -180), new(50, -180) };
                if (__instance != null && __instance.fruit.Count > 3)
                {
                    for (int i = 3; i < __instance.fruit.Count; i++)
                    {
                        SpriteEffects flip = SpriteEffects.None;
                        if(i % 2 == 0) flip = SpriteEffects.FlipHorizontally;

                        Vector2 fruitPosition = tileLocation * 64f + tileOffset[i];
                        spriteBatch.Draw(
                            Game1.objectSpriteSheet,
                            Game1.GlobalToLocal(Game1.viewport, fruitPosition),
                            Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, __instance.fruit[i].ParentSheetIndex, 16, 16),
                            Color.White,
                            0f,
                            Vector2.Zero,
                            4f,
                            flip,
                            (float)((tileLocation.Y + 1.0) * 64.0 / 10000.0 + 0.001)
                        );
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.GetSeason))]    // Set 3 season
        public class GameLocation_GetSeason_Patch
        {
            static void Postfix(GameLocation __instance, ref Season __result)
            {
                if (__instance != null && __instance.Name != null && __instance.Name.Contains("Custom_MT_Island"))
                {
                    int customDay = (int)Game1.stats.DaysPlayed % 84;

                    if (1 <= customDay && customDay <= 28) __result = Season.Spring;
                    else if (29 <= customDay && customDay <= 56) __result = Season.Summer;
                    else __result = Season.Fall;
                }
            }
        }


        // Draw Festival item
        [HarmonyPatch(typeof(Chest))]
        [HarmonyPatch("draw")]
        [HarmonyPatch(new Type[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) })]
        public class Postfix_draw
        {
            public static void Postfix(Chest __instance, SpriteBatch spriteBatch, int x, int y)
            {
                if (__instance.Location.Name != "Custom_MT_Island" || __instance.TileLocation != new Vector2(73, 32)) return;

                List<Vector2> tileList = new List<Vector2>
                {
                    new Vector2(86, 31),
                    new Vector2(86, 36),
                    new Vector2(76, 26),
                    new Vector2(64, 28),
                    new Vector2(76, 36),
                    new Vector2(70, 24),
                    new Vector2(70, 29),
                    new Vector2(70, 34),
                    new Vector2(64, 33),
                    new Vector2(76, 31)
                };

                List<Chest> chestList = new List<Chest>();

                foreach ( var tile in tileList ) 
                {
                    var location = Game1.getLocationFromName("Custom_MT_Island");
                    var obj1 = location.getObjectAtTile((int)(tile.X + 100), (int)(tile.Y));
                    if (obj1 is Chest chest && obj1 is not null && !chestList.Contains(chest))
                    {
                        chestList.Add(chest);
                    }
                }

                foreach (var chest in chestList)
                {
                    var tileLocation = chest.TileLocation + new Vector2(-100, 0);

                    var drawLayer = Math.Max(0f, (tileLocation.Y * Game1.tileSize - 24) / 10000f) + tileLocation.X * 1E-05f;
                    drawGrangeItems(tileLocation, spriteBatch, drawLayer, chest.Items.Select(item => item != null ? item.QualifiedItemId : "").ToList()   );
                }

            }
        }

        public static void drawGrangeItems(Vector2 tileLocation, SpriteBatch spriteBatch, float layerDepth, List<string> shopItem)
        {
            var start = Game1.GlobalToLocal(Game1.viewport, tileLocation * 64);

            start.X += 13f;
            start.Y -= 170f;

            float xStep = 0;
            float yStep = 0f;

            foreach (var itemId in shopItem)
            {
                if (itemId is null || itemId == "")
                {
                    xStep += 54f;
                    if (xStep > 108f)
                    {
                        xStep = 0f;
                        yStep += 55f;
                    }
                    continue;
                }
                // Draw shadow
                spriteBatch.Draw(Game1.shadowTexture, new Vector2(start.X + xStep + 10f, start.Y + yStep + 40f),
                    Game1.shadowTexture.Bounds, Color.Red, 0f,
                    Vector2.Zero, 3.2f, SpriteEffects.None, layerDepth + 0.01f);

                var item = ItemRegistry.GetDataOrErrorItem(itemId);

                float xModify = 0f;
                float yModify = 0f;

                int originalHeight = item.GetSourceRect().Height;
                int originalWidth = item.GetSourceRect().Width;

                int maxRectSize = 14;

                float scale = Math.Min((float)maxRectSize / originalHeight * 3.7f, (float)maxRectSize / originalWidth * 3.7f);


                if (itemId.Contains("(S)"))
                {
                    scale *= 0.75f;
                    xModify = 5f;
                    yModify = 10f;
                }
                spriteBatch.Draw(item.GetTexture(), new Vector2(start.X + xStep + xModify, start.Y + yStep + yModify),
                    item.GetSourceRect(), Color.White, 0f,
                    Vector2.Zero, scale, SpriteEffects.None, layerDepth + 0.02f);


                xStep += 54f;

                if (xStep > 108f)
                {
                    xStep = 0f;
                    yStep += 55f;
                }
            }
        }


        [HarmonyPatch(typeof(Building))]
        [HarmonyPatch("getPointForHumanDoor")]
        public class Building_GetPointForHumanDoor_Patch
        {
            static void Postfix(Building __instance, ref Point __result)
            {
                if (__instance.parentLocationName.Value == "Custom_MT_Island")
                {
                    // Modify the Y coordinate of the result
                    __result = new Point((int)__instance.tileX.Value + __instance.humanDoor.Value.X, (int)__instance.tileY.Value + __instance.humanDoor.Value.Y + 1);
                    __instance.GetParentLocation().setTileProperty(__result.X - 1, __result.Y, "Back", "NoPath", "T");
                    __instance.GetParentLocation().setTileProperty(__result.X + 1, __result.Y, "Back", "NoPath", "T");

                }
            }
        }
        public static void DrawAtNonTileSpot_Prefix(Furniture __instance, ref Vector2 location, float layerDepth, float alpha)
        {
            if (__instance != null && __instance.QualifiedItemId == "(F)MT.Objects.RestaurantDecor")
            {
                location.X -= 32;
                location.Y += 12;
            }
        }
        public static void LoadDescription_Postfix(Furniture __instance, ref string __result)
        {
            if (__instance != null && __instance.QualifiedItemId == "(F)MT.Objects.RestaurantDecor")
            {
                __result = SHelper.Translation.Get("foodstore.items.RestaurantDecor");
            }
            if (__instance != null && __instance.QualifiedItemId == "(F)MT.Objects.MarketLog")
            {
                __result = SHelper.Translation.Get("foodstore.items.MarketLog");
            }
        }

        public static void GetSellPrice_Postfix(ref int __result, FarmAnimal __instance)
        {
            if (__instance.modData != null &&
                __instance.modData.TryGetValue("hapyke.FoodStore/isFakeAnimal", out string isFakeAnimal) &&
                isFakeAnimal != null &&
                isFakeAnimal == "true")
            {
                __result = 0;
            }
        }

        public static void GetCursorPetBoundingBox_Postfix(ref Microsoft.Xna.Framework.Rectangle __result, FarmAnimal __instance)
        {
            if (__instance.modData != null &&
                __instance.modData.TryGetValue("hapyke.FoodStore/isFakeAnimal", out string isFakeAnimal) &&
                isFakeAnimal != null &&
                isFakeAnimal == "true")
            {
                __result = Microsoft.Xna.Framework.Rectangle.Empty;
            }
        }

        public static void SetUpForReturnToIslandAfterLivestockPurchase(PurchaseAnimalsMenu __instance)
        {
            if (Game1.player.currentLocation.Name != "Custom_MT_Island") return;

            LocationRequest locationRequest = Game1.getLocationRequest("Custom_MT_Island");
            locationRequest.OnWarp += delegate
            {
                __instance.onFarm = false;
                Game1.player.viewingLocation.Value = null;
                Game1.displayHUD = true;
                Game1.viewportFreeze = false;
                __instance.namingAnimal = false;
                __instance.textBox.OnEnterPressed -= __instance.textBoxEvent;
                __instance.textBox.Selected = false;
                Game1.exitActiveMenu();
            };
            Game1.warpFarmer(locationRequest, Game1.player.TilePoint.X, Game1.player.TilePoint.Y, Game1.player.FacingDirection);
        }

        public static void NewDayAfterFade_Postfix()
        {
            if (Config.UltimateChallenge)
                IsCalculatingSellPrice = true;
        }

        public static void SellToStorePrice_Postfix(ref int __result)
        {
            if (IsCalculatingSellPrice)
            {
                SMonitor.Log("Challenge accepted. Changing shipping bin value for tonight!", LogLevel.Debug);
                __result /= 2;
            }
        }
    }
}   