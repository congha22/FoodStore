﻿using MailFrameworkMod;
using MarketTown;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using xTile.Dimensions;
using Netcode;
using StardewValley.Network;
using xTile.Tiles;
using xTile;
using System.Xml.Linq;
using System.Threading;
using System.Timers;
using StardewValley.Minigames;

namespace MarketTown
{
    internal class FarmOutside
    {
        internal static void PlayerWarp(object sender, WarpedEventArgs e)
        {
            Random random = new Random();

            if ( (e.NewLocation.Name.Contains("Custom_MT_Island") && (e.OldLocation is Beach || e.OldLocation is BeachNightMarket || e.OldLocation.Name == "Custom_Ridgeside_RSVCliff" || e.OldLocation.Name == "EastScarp_Village"))
                || ( e.OldLocation.Name.Contains("Custom_MT_Island") && (e.NewLocation is Beach || e.NewLocation is BeachNightMarket || e.NewLocation.Name == "Custom_Ridgeside_RSVCliff" || e.NewLocation.Name == "EastScarp_Village")) )
            {
                var letterTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("Assets/LtBG.png");
                MailRepository.SaveLetter(
                    new Letter(
                        "MT.IslandInit",
                        ModEntry.SHelper.Translation.Get("foodstore.letter.islandinit"),
                        (Letter l) => !Game1.player.mailReceived.Contains("MT.IslandInit"),
                        delegate (Letter l)
                        {
                            ((NetHashSet<string>)(object)Game1.player.mailReceived).Add(l.Id);
                        })
                    {
                        Title = "Paradise Island",
                        LetterTexture = letterTexture
                    }
                );

                string weather = e.NewLocation.GetWeather().Weather.ToLower();

                switch (weather)
                {
                    case "rain":
                        e.Player.Stamina += (float)(e.Player.MaxStamina * random.Next(-25, -15) / 100);
                        if (!ModEntry.Config.DisableChatAll && !ModEntry.Config.DisableChat) Game1.addHUDMessage(new HUDMessage(ModEntry.SHelper.Translation.Get("foodstore.islandtravel.rain"), HUDMessage.newQuest_type));
                        break;
                    case "wind":
                        e.Player.Stamina += (float)(e.Player.MaxStamina * random.Next(-15, 10) / 100);
                        if (!ModEntry.Config.DisableChatAll && !ModEntry.Config.DisableChat) Game1.addHUDMessage(new HUDMessage(ModEntry.SHelper.Translation.Get("foodstore.islandtravel.wind"), HUDMessage.newQuest_type));
                        break;
                    case "storm":
                        e.Player.Stamina += (float)(e.Player.MaxStamina * random.Next(-35, -27) / 100);
                        if (!ModEntry.Config.DisableChatAll && !ModEntry.Config.DisableChat) Game1.addHUDMessage(new HUDMessage(ModEntry.SHelper.Translation.Get("foodstore.islandtravel.storm"), HUDMessage.newQuest_type));
                        break;
                    case "greenrain":
                        e.Player.Stamina += (float)(e.Player.MaxStamina * random.Next(-30, -23) / 100);
                        if (!ModEntry.Config.DisableChatAll && !ModEntry.Config.DisableChat) Game1.addHUDMessage(new HUDMessage(ModEntry.SHelper.Translation.Get("foodstore.islandtravel.greenrain"), HUDMessage.newQuest_type));
                        break;
                    case "snow":
                        e.Player.Stamina += (float)(e.Player.MaxStamina * random.Next(-25, -20) / 100);
                        if (!ModEntry.Config.DisableChatAll && !ModEntry.Config.DisableChat) Game1.addHUDMessage(new HUDMessage(ModEntry.SHelper.Translation.Get("foodstore.islandtravel.snow"), HUDMessage.newQuest_type));
                        break;
                    default:
                        e.Player.Stamina += (float)(e.Player.MaxStamina * random.Next(-10, -5) / 100);
                        if (!ModEntry.Config.DisableChatAll && !ModEntry.Config.DisableChat) Game1.addHUDMessage(new HUDMessage(ModEntry.SHelper.Translation.Get("foodstore.islandtravel.sun"), HUDMessage.newQuest_type));
                        break;
                }
            }

            if (e.NewLocation.Name == "Custom_MT_Island")
            {
                if (e.OldLocation.Name == "Beach" || e.OldLocation.Name == "EastScarp_Village") Game1.currentMinigame = new ParadiseBoat();
                else if (e.OldLocation.Name == "Custom_Ridgeside_RSVCliff") Game1.currentMinigame = new ParadiseBalloon();

                foreach (var i in ModEntry.IslandNPCList)
                {
                    var islandVisitor = Game1.getCharacterFromName(i);
                    if (islandVisitor != null)
                    {
                        islandVisitor.TryLoadSprites("Characters/" + islandVisitor.getTextureName() + "_Beach", out var error);
                        islandVisitor.TryLoadPortraits("Portraits/" + islandVisitor.getTextureName() + "_Beach", out var error1);
                    }
                }
            }

            if (!e.Player.IsMainPlayer)
            {
                return;
            }

            var isFarm = e.NewLocation.Name.StartsWith("Farm");
            var isFarmHouse = e.NewLocation.Name.StartsWith("FarmHouse");

            if (!isFarm && !isFarmHouse) //if its neither the farm nor the farmhouse
                return;

            if (isFarmHouse && !ModEntry.Config.EnableVisitInside)      //If not enable visit inside
                return;

            string name = null;
            Point door = new();

            if (isFarm)
            {
                door = Game1.getFarm().GetMainFarmHouseEntry();
                door.X += 3;
                door.Y += 2; //two more tiles down
                name = e.NewLocation.Name;
            }

            if (isFarmHouse)
            {
                var home = Utility.getHomeOfFarmer(Game1.player);
                door = home.getEntryLocation();
                door.X += 3;
                door.Y -= 2;
                name = home.Name;
            }

            List<NPC> oldLocationNpc = e.OldLocation.characters.ToList();

            foreach (NPC visit in oldLocationNpc)
            {
                try
                {
                    if (visit.IsVillager && (visit.currentLocation.Name == "Farm" || visit.currentLocation.Name == "FarmHouse") 
                        && visit.modData.ContainsKey("hapyke.FoodStore/invited") && visit.modData["hapyke.FoodStore/invited"] == "true" 
                        && Game1.timeOfDay > ModEntry.Config.InviteComeTime)
                    {
                        if (visit.controller is not null)
                            visit.Halt();

                        Game1.warpCharacter(visit, name, door);

                        visit.faceDirection(2);

                        ModEntry.CleanNpc(visit);

                        door.X--;
                    }
                }
                catch { }
            }
        }

        // UNUSED. Can be use to add dynamic movement for npc, but cause many issue. Use AddNewPointToSchedule
        internal static void WalkAround(string who, bool update = false)
        {

            var c = Game1.getCharacterFromName(who);
            if (c == null || !c.IsVillager) return;

            var newspot = getRandomOpenPointInLocation(c, c.currentLocation, update);

            try
            {
                c.controller = null;
                //c.isCharging = true;

                c.controller = new PathFindController(
                    c,
                    c.currentLocation,
                    newspot,
                    Game1.random.Next(0, 4)
                    );
            }
            catch { }
        }

        /// <summary>
        /// Add a new stop to the middle of a schedule and retain the structure.
        /// Might not work all the time, and does not work if NPC has queued schedule.
        /// </summary>
        /// <param name="npc">The NPC to which the schedule will be added.</param>
        /// <param name="addTime">The time at which the schedule starts, and must be Game1.timeOfDay + 10 </param>
        /// <param name="addEndLocation">The location where the schedule ends. Should be current NPC location, not tested if not current location</param>
        /// <param name="addEndX">The X coordinate of the end tile.</param>
        /// <param name="addEndY">The Y coordinate of the end tile.</param>
        /// <param name="addEndDirection">The direction the NPC will face at the end location.</param>
        internal static bool AddNewPointToSchedule(NPC npc, string addTime, string addEndLocation, string addEndX, string addEndY, string addEndDirection)
        {
            if (npc.queuedSchedulePaths.Count != 0 || !Game1.IsMasterGame)  return false; 

            try 
            {
                var initSche = "";
                var currentDirection = npc.DirectionsToNewLocation;

                Dictionary<int, SchedulePathDescription> schedule = npc.Schedule;
                SortedDictionary<int, string> tempSche = new SortedDictionary<int, string>();

                if (schedule != null && schedule.Count > 0)
                {
                    if (currentDirection == null && !npc.isMoving())
                    {
                        string addSche = $"{addTime} {addEndLocation} {addEndX} {addEndY} {addEndDirection}/";
                        tempSche.Add(Int32.Parse(addTime), addSche);

                        foreach (var piece in schedule)
                        {
                            SchedulePathDescription description = piece.Value;

                            string time = $"{piece.Key}";
                            string endLocation = description.targetLocationName;
                            string endX = $"{description.targetTile.X}";
                            string endY = $"{description.targetTile.Y}";
                            string endDirection = $"{description.facingDirection}";

                            if (!tempSche.ContainsKey(piece.Key))
                            {
                                tempSche.Add(piece.Key, $"{time} {endLocation} {endX} {endY} {endDirection}/");
                            }
                            else
                            {
                                int timeModify = 10;
                                int tried = 0;
                                while (true && tried < 5)
                                {
                                    if (tried == 4) return false;
                                    if (!tempSche.ContainsKey(piece.Key + timeModify))
                                    {
                                        tempSche.Add(ModEntry.ConvertToHour(piece.Key + timeModify), $"{ModEntry.ConvertToHour(piece.Key + timeModify)} {endLocation} {endX} {endY} {endDirection}/");
                                        break;
                                    }
                                    timeModify += 10;
                                    tried++;
                                }
                            }
                        }
                    }
                    // if npc is moving
                    else if (currentDirection != null)
                    {
                        // add current tile BEFORE request
                        tempSche.Add(Game1.timeOfDay,
                        $"{Game1.timeOfDay} {npc.currentLocation.NameOrUniqueName} {npc.Tile.X} {npc.Tile.Y} {npc.FacingDirection}/");

                        // add requested schedule
                        string addSche = $"{addTime} {addEndLocation} {addEndX} {addEndY} {addEndDirection}/";
                        tempSche.Add(Int32.Parse(addTime), addSche);

                        // re-add current direction AFTER request
                        string currentDirectionString = 
                            $"{ModEntry.ConvertToHour(Int32.Parse(addTime) + 10)} {currentDirection.targetLocationName} {currentDirection.targetTile.X} {currentDirection.targetTile.Y} {currentDirection.facingDirection}/";
                        tempSche.Add(ModEntry.ConvertToHour(Int32.Parse(addTime) + 10), currentDirectionString);

                        foreach (var piece in schedule)
                        {
                            if (piece.Value.time == currentDirection.time) continue; // already added

                            SchedulePathDescription description = piece.Value;

                            string time = $"{piece.Key}";
                            string endLocation = description.targetLocationName;
                            string endX = $"{description.targetTile.X}";
                            string endY = $"{description.targetTile.Y}";
                            string endDirection = $"{description.facingDirection}";

                            if (!tempSche.ContainsKey(piece.Key))
                            {
                                tempSche.Add(piece.Key, $"{time} {endLocation} {endX} {endY} {endDirection}/");
                            }
                            else
                            {
                                int timeModify = 10;
                                int tried = 0;
                                while (true && tried < 5)
                                {
                                    if (tried == 4) return false;
                                    if (!tempSche.ContainsKey(piece.Key + timeModify))
                                    {
                                        tempSche.Add(ModEntry.ConvertToHour(piece.Key + timeModify), $"{ModEntry.ConvertToHour(piece.Key + timeModify)} {endLocation} {endX} {endY} {endDirection}/");
                                        break;
                                    }
                                    timeModify += 10;
                                    tried++;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // add current tile BEFORE request
                    tempSche.Add(Game1.timeOfDay,
                    $"{Game1.timeOfDay} {npc.currentLocation.NameOrUniqueName} {npc.Tile.X} {npc.Tile.Y} {npc.FacingDirection}/");

                    // add requested schedule
                    string addSche = $"{addTime} {addEndLocation} {addEndX} {addEndY} {addEndDirection}/";
                    tempSche.Add(Int32.Parse(addTime), addSche);

                    // add current tile AFTER request
                    tempSche.Add(ModEntry.ConvertToHour(Int32.Parse(addTime) + 10),
                    $"{ModEntry.ConvertToHour(Int32.Parse(addTime) + 10)} {npc.currentLocation.NameOrUniqueName} {npc.Tile.X} {npc.Tile.Y} {npc.FacingDirection}/");
                }

                if (tempSche.Any())
                {
                    foreach (var x in tempSche)
                        initSche += x.Value;
                }

                if (initSche == null || initSche == "") return false;

                ModEntry.CleanNpc(npc);
                npc.TryLoadSchedule("default", initSche);
                return true;
            }
            catch { 
                ModEntry.SMonitor.Log($"Error while adding new schedule for {npc.Name}", StardewModdingAPI.LogLevel.Warn);
                return false;
            }
        }



        /// <summary>Add a random Tile to the Schedule at the next time change (10 minutes).</summary>
        internal static void AddNextMoveSchedulePoint(NPC npc, string addTime, string addEndLocation, string addEndX, string addEndY, string addEndDirection)
        {
            if (!Game1.IsMasterGame) return;
            // initSche is the last of the current schedule, then added the new piece
            // if schedule is null, initSche will be the current position of NPC
            var initSche = "";
            ModEntry.CleanNpc(npc);
            Dictionary<int, SchedulePathDescription> schedule = npc.Schedule;

            if (schedule != null)
            {
                var kvp = schedule.LastOrDefault();
                SchedulePathDescription description = kvp.Value;

                if (description != null)
                {
                    string time = $"{kvp.Key}";
                    string endLocation = description.targetLocationName;
                    string endX = $"{description.targetTile.X}";
                    string endY = $"{description.targetTile.Y}";
                    string endDirection = $"{description.facingDirection}";

                    initSche += $"{time} {endLocation} {endX} {endY} {endDirection}/";
                }
            }
            else
            {
                initSche += $"{Game1.timeOfDay} {npc.currentLocation.NameOrUniqueName} {npc.Tile.X} {npc.Tile.Y} {npc.FacingDirection}/";
            }

            initSche += $"{addTime} {addEndLocation} {addEndX} {addEndY} {addEndDirection}/";

            npc.TryLoadSchedule("default", initSche);
        }


        /// <summary>Update the list of 'valid' tile in a location, which can then be selected for NPC schedule.</summary>
        internal static void UpdateRandomLocationOpenTile(GameLocation location)
        {
            try
            {
                Random r = new Random();
                var map = location.Map;

                int possibleWidth = map.Layers[0].LayerWidth - 1;
                int possibleHeight = map.Layers[0].LayerHeight - 1;

                if (!ModEntry.RandomOpenSpot.ContainsKey(location)) ModEntry.RandomOpenSpot[location] = new List<Vector2>();
                else ModEntry.RandomOpenSpot[location].Clear();

                int count = 0;
                int tried = 0;
                while (tried < 250 && count < 150 && count < possibleWidth * possibleHeight / 1.5
                        && (count < location.characters.Count * 3 || count < 20 || count < 60 && location.NameOrUniqueName.Contains("Custom_MT_Island")) )
                {
                    tried++;
                    Vector2 tile = new Vector2(r.Next(1, possibleWidth), r.Next(7, possibleHeight));
                    if (location.CanItemBePlacedHere(tile) && !location.warps.Any(i => i.X == tile.X && i.Y == tile.Y))
                    {
                        ModEntry.RandomOpenSpot[location].Add(tile);
                        count++;
                    }
                }
                //Console.WriteLine("Updating open tile at " + location.NameOrUniqueName + " for " + count);

            }
            catch { Console.WriteLine( $"Error while updating open tile at {location.NameOrUniqueName}"); }
        }


        /// <summary>Get a 'valid' tile for NPC schedule from this Location.</summary>
        internal static Point getRandomOpenPointInLocation(NPC who, GameLocation location, bool update, bool bypass = false)
        {
            try
            {
                if (bypass || who != null && who.IsVillager && location != null
                    && ( ((who.currentLocation == Game1.getFarm() || who.currentLocation.Name == "FarmHouse") && who.modData["hapyke.FoodStore/invited"] == "true")
                        || (who.Name.Contains("MT.Guest_") && !who.currentLocation.Name.Contains("BusStop"))
                        || location.NameOrUniqueName.Contains("Custom_MT_Island")
                        || location.GetParentLocation() != null && location.GetParentLocation().Name == "Custom_MT_Island") ) // ************ check shed name
                {
                    if (update || !ModEntry.RandomOpenSpot.ContainsKey(location) || ModEntry.RandomOpenSpot[location].Count() == 0) 
                        UpdateRandomLocationOpenTile(location);

                    if (ModEntry.RandomOpenSpot.ContainsKey(location))
                       return ModEntry.RandomOpenSpot[location][Game1.random.Next(ModEntry.RandomOpenSpot[location].Count())].ToPoint();
                }
                return Point.Zero;
            }
            catch { return Point.Zero; }
        }
    }
}
