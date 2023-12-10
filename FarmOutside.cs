using FoodStore;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;
using StardewValley.TerrainFeatures;
using System;
using System.Linq;
using lv = StardewModdingAPI.LogLevel;

namespace FoodStore
{
    internal class FarmOutside
    {
        internal static bool NPCinScreen()
        {
            var x = 0;
            var y = 0;
            var farm = Game1.getLocationFromName("Farm");
            foreach (NPC who in Utility.getAllCharacters())
            {
                if (who.isVillager() && who.currentLocation.Name == "Farm" && who.modData["hapyke.FoodStore/invited"] == "true")
                {
                     x = ((int)(who.Position.X / 64));
                     y = ((int)(who.Position.Y / 64));
                }
            }


            //return Utility.isOnScreen(who.Position.ToPoint(), 0, farm);
            return Utility.isOnScreen(new Point(x, y), 0, farm);
        }

        /* NOTE:
         * There is a NPC barrier surrounding the door. Characters warped to that tile won't be able to move.
         * For this reason, the NPC must be warped 2 tiles below the door.
         * 
         * This could be fixed by editing map properties- but it'd only be compatible with vanilla maps (and might have side effects). This is the best workaround currently.
         */
        internal static void PlayerWarp(object sender, WarpedEventArgs e)
        {
            var isFarm = e.NewLocation.IsFarm;
            var isFarmHouse = e.NewLocation.Name.StartsWith("FarmHouse");

            if (!isFarm && !isFarmHouse) //if its neither the farm nor the farmhouse
                return;

            //if (!ModEntry.Config.WalkOnFarm)
            //    return; //if npcs can't follow or there's no visit


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

            foreach (NPC visit in Utility.getAllCharacters())
            {
                if (visit.isVillager() && (visit.currentLocation.Name == "Farm" || visit.currentLocation.Name == "FarmHouse") && visit.modData["hapyke.FoodStore/invited"] == "true" && Game1.timeOfDay > ModEntry.Config.InviteComeTime)
                {
                    if (visit.controller is not null)
                        visit.Halt();

                    Game1.warpCharacter(visit, name, door);

                    visit.faceDirection(2);

                    door.X--;
                    visit.controller = new PathFindController(visit, Game1.getFarm(), door, 2); //(this was made as test, but will stay commented-out just in case.) */
                }
            }

        }

        internal static void WalkAroundFarm(string who)
        {
            var c = Game1.getCharacterFromName(who);

            var gameLocation = Game1.getFarm();
            var newspot = getRandomOpenPointInFarm(gameLocation, Game1.random);

            try
            {
                c.PathToOnFarm(newspot);

            }
            catch {}
        }

        internal static void WalkAroundHouse(string who)
        {
            var c = Game1.getCharacterFromName(who);

            var gameLocation = Game1.getLocationFromName("FarmHouse");
            var newspot = getRandomOpenPointInFarm(gameLocation, Game1.random);

            try
            {
                c.PathToOnFarm(newspot);

            }
            catch { }
        }

        internal static Point getRandomOpenPointInFarm(GameLocation location, Random r, int tries = 30, int maxDistance = 10)
        {
            foreach (NPC who in Utility.getAllCharacters())
            {
                if (who.isVillager() && (who.currentLocation.Name == "Farm" || who.currentLocation.Name == "FarmHouse") && who.modData["hapyke.FoodStore/invited"] == "true")
                {

                    var map = location.map;

                    Point zero = Point.Zero;
                    bool CanGetHere = false;

                    for (int i = 0; i < tries; i++)
                    {
                        //we get random position using width and height of map
                        zero = new Point(r.Next(map.Layers[0].LayerWidth), r.Next(map.Layers[0].LayerHeight));

                        bool isFloorValid = location.isTileOnMap(zero.ToVector2()) && location.isTilePassable(new xTile.Dimensions.Location(zero.X, zero.Y), Game1.viewport) && !location.isWaterTile(zero.X, zero.Y);
                        bool IsBehindTree = location.isBehindTree(zero.ToVector2());
                        Warp WarpOrDoor = location.isCollidingWithWarpOrDoor(new Rectangle(zero, new Point(1, 1)));

                        //check that location is clear + not water tile + not behind tree + not a warp
                        CanGetHere = location.isTileLocationTotallyClearAndPlaceable(zero.X, zero.Y) && isFloorValid && !IsBehindTree && WarpOrDoor == null;

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
