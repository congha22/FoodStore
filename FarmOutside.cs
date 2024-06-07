using MarketTown;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using xTile.Dimensions;
using lv = StardewModdingAPI.LogLevel;

namespace MarketTown
{
    internal class FarmOutside
    {
        internal static bool NPCinScreen()
        {
            var x = 0;
            var y = 0;
            var farm = Game1.getLocationFromName("Farm");
            foreach (NPC who in Utility.getAllVillagers())
            {
                if (who.IsVillager && who.currentLocation.Name == "Farm" 
                    && who.modData.ContainsKey("hapyke.FoodStore/invited") && who.modData["hapyke.FoodStore/invited"] == "true")
                {
                    x = (int)(who.Position.X / 64);
                    y = (int)(who.Position.Y / 64);
                }
            }


            //return Utility.isOnScreen(who.Position.ToPoint(), 0, farm);
            return Utility.isOnScreen(new Point(x, y), 0, farm);
        }

        internal static void PlayerWarp(object sender, WarpedEventArgs e)
        {
            Random random = new Random();
            if (e.NewLocation.Name.Contains("Custom_MT_Island") && e.OldLocation is Beach || e.OldLocation.Name.Contains("Custom_MT_Island") && e.NewLocation is Beach)
            {
                try
                {
                    if (e.NewLocation.Name == ("Custom_MT_Island"))
                    {
                        Game1.chatBox.addInfoMessage("This is the last time you can make recommendation for Island/Island house map. Next major update will be permanent");
                        foreach (NPC __instance in Game1.player.currentLocation.characters)
                        {
                            Point zero = new Point((int)__instance.Tile.X, (int)__instance.Tile.Y);
                            var location = __instance.currentLocation;
                            bool isWaterTile = location.isWaterTile(zero.X, zero.Y);

                            bool isValid = location.isTileOnMap(__instance.Tile) && !isWaterTile
                                && location.isTilePassable(new Location(zero.X, zero.Y), Game1.viewport);


                            if (!isValid)
                            {
                                __instance.Halt();
                                Game1.warpCharacter(__instance,
                                    Game1.getLocationFromName("Custom_MT_Island"),
                                    ModEntry.islandWarp[random.Next(ModEntry.islandWarp.Count)]);
                            }
                        }

                    }
                } catch { }
                string weather = e.NewLocation.GetWeather().Weather;

                switch (weather)
                {
                    case "Rain":
                        e.Player.Stamina += (float)(e.Player.MaxStamina * random.Next(-15, -8) / 100);
                        if (!ModEntry.Config.DisableChatAll && !ModEntry.Config.DisableChat) Game1.addHUDMessage(new HUDMessage(ModEntry.SHelper.Translation.Get("foodstore.islandtravel.rain"), 3500, true));
                        break;
                    case "Wind":
                        e.Player.Stamina += (float)(e.Player.MaxStamina * random.Next(-8, 4) / 100);
                        if (!ModEntry.Config.DisableChatAll && !ModEntry.Config.DisableChat) Game1.addHUDMessage(new HUDMessage(ModEntry.SHelper.Translation.Get("foodstore.islandtravel.wind"), 3500, true));
                        break;
                    case "storm":
                        e.Player.Stamina += (float)(e.Player.MaxStamina * random.Next(-30, -20) / 100);
                        if (!ModEntry.Config.DisableChatAll && !ModEntry.Config.DisableChat) Game1.addHUDMessage(new HUDMessage(ModEntry.SHelper.Translation.Get("foodstore.islandtravel.storm"), 3500, true));
                        break;
                    case "GreenRain":
                        e.Player.Stamina += (float)(e.Player.MaxStamina * random.Next(-23, -18) / 100);
                        if (!ModEntry.Config.DisableChatAll && !ModEntry.Config.DisableChat) Game1.addHUDMessage(new HUDMessage(ModEntry.SHelper.Translation.Get("foodstore.islandtravel.greenrain"), 3500, true));
                        break;
                    case "Snow":
                        e.Player.Stamina += (float)(e.Player.MaxStamina * random.Next(-20, -8) / 100);
                        if (!ModEntry.Config.DisableChatAll && !ModEntry.Config.DisableChat) Game1.addHUDMessage(new HUDMessage(ModEntry.SHelper.Translation.Get("foodstore.islandtravel.snow"), 3500, true));
                        break;
                    default:
                        e.Player.Stamina += (float)(e.Player.MaxStamina * random.Next(-7, -3) / 100);
                        if (!ModEntry.Config.DisableChatAll && !ModEntry.Config.DisableChat) Game1.addHUDMessage(new HUDMessage(ModEntry.SHelper.Translation.Get("foodstore.islandtravel.sun"), 3500, true));
                        break;
                }
            }

            var isBusStop = e.NewLocation.Name.Contains("BusStop");

            if (isBusStop)
            {
                List<NPC> npcsToWarp = new List<NPC>();

                foreach (NPC who in Game1.getLocationFromName("BusStop").characters.ToList())
                {
                    if (who.Name.Contains("MT.Guest_"))
                    {
                        npcsToWarp.Add(who);
                    }
                }

                foreach (NPC npc in npcsToWarp)
                {
                    npc.Halt();
                    npc.temporaryController = null;
                    npc.controller = null;
                    npc.ClearSchedule();
                    Game1.warpCharacter(npc, npc.DefaultMap, npc.DefaultPosition / 64);
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
                ModEntry.IsOutside = true;

                door = Game1.getFarm().GetMainFarmHouseEntry();
                door.X += 3;
                door.Y += 2; //two more tiles down
                name = e.NewLocation.Name;
            }

            if (isFarmHouse)
            {
                ModEntry.IsOutside = false;

                var home = Utility.getHomeOfFarmer(Game1.player);
                door = home.getEntryLocation();
                door.X += 3;
                door.Y -= 2;
                name = home.Name;
            }

            foreach (NPC visit in Utility.getAllVillagers())
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

                        door.X--;
                        visit.controller = new PathFindController(visit, Game1.getFarm(), door, 2);
                    }
                }
                catch { }
            }
        }

        internal static void WalkAround(string who)
        {
            var c = Game1.getCharacterFromName(who);
            if (c == null) return;

            var newspot = getRandomOpenPointInFarm(c.currentLocation, Game1.random);

            try
            {
                c.controller = null;
                c.isCharging = true;

                c.controller = new PathFindController(
                    c,
                    c.currentLocation,
                    newspot,
                    Game1.random.Next(0, 4)
                    );
            }
            catch { }
        }


        internal static Point getRandomOpenPointInFarm(GameLocation location, Random r, int tries = 5, int maxDistance = 75)
        {
            foreach (NPC who in Utility.getAllVillagers())
            {
                if (who.IsVillager && (((who.currentLocation.Name == "Farm" || who.currentLocation.Name == "FarmHouse") && who.modData["hapyke.FoodStore/invited"] == "true") || (who.Name.Contains("MT.Guest_") && !who.currentLocation.Name.Contains("BusStop")) ))
                {

                    var map = location.map;

                    Point zero = Point.Zero;
                    bool CanGetHere = false;

                    for (int i = 0; i < tries; i++)
                    {
                        //we get random position using width and height of map
                        zero = new Point(r.Next(2, map.Layers[0].LayerWidth - 2), r.Next(2, map.Layers[0].LayerHeight - 2));

                        bool isFloorValid = location.isTileOnMap(zero.ToVector2()) && location.isTilePassable(new xTile.Dimensions.Location(zero.X, zero.Y), Game1.viewport) && !location.isWaterTile(zero.X, zero.Y);
                        bool IsBehindTree = location.isBehindTree(zero.ToVector2());
                        Warp WarpOrDoor = location.isCollidingWithWarpOrDoor(new Microsoft.Xna.Framework.Rectangle(zero, new Point(1, 1)));

                        //check that location is clear + not water tile + not behind tree + not a warp
                        CanGetHere = !location.IsTileBlockedBy(new Vector2(zero.X, zero.Y)) && location.CanItemBePlacedHere(new Vector2(zero.X, zero.Y)) 
                            && isFloorValid && !IsBehindTree && WarpOrDoor == null;

                        //if the new point is too far away
                        Point difference = new(Math.Abs(zero.X - (int)who.Position.X), Math.Abs(zero.Y - (int)who.Position.Y));
                        if (difference.X > maxDistance && difference.Y > maxDistance)
                        {
                            CanGetHere = false;
                        }

                        if (CanGetHere)
                        {
                            break;
                        }
                    }
                    return zero;
                }
            }
            return Point.Zero;
        }
    }
}
