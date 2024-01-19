using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using lv = StardewModdingAPI.LogLevel;

namespace MarketTown
{

    internal static class Values
    {
        internal static bool IsFree(NPC who, bool isRandom = true)
        {
            //if character is robin and there's a construction ongoing
            if (who is null ||  who.Name == "Robin" && (Game1.getFarm().isThereABuildingUnderConstruction() || who.currentLocation.Equals(Game1.getFarm())))
            {
                return false;
            }

            var isAnimating = who.doingEndOfRouteAnimation.Value;
            var isInvisible = who.IsInvisible;
            var isHospitalDay = Utility.IsHospitalVisitDay(who.Name);
            var visitingIsland = Game1.IsVisitingIslandToday(who.Name);
            var isSleeping = who.isSleeping.Value;

            var defaultReqs = !isHospitalDay && !visitingIsland && !isSleeping && !isAnimating && !isInvisible;

            return defaultReqs;
        }
    }

}