using StardewValley;
using StardewValley.SDKs;
using System;

namespace MarketTown
{
    internal class DupeNPC
    {
        internal static void SetVariables(NPC who)
        {
            Random random = new Random();
            int randomIndex = random.Next(1, 8);
            string npcAge, npcManner, npcSocial;

            int age = who.Age;
            int manner = who.Manners;
            int social = who.SocialAnxiety;

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

            //general data
            who.CurrentDialogue = null;
            who.ignoreScheduleToday = true;
            who.temporaryController = null;
            who.currentLocation = Utility.getHomeOfFarmer(Game1.player);
            who.Position = Utility.getHomeOfFarmer(Game1.player).getEntryLocation().ToVector2();
            who.Schedule?.Clear();
            who.Dialogue?.Clear();
            who.goingToDoEndOfRouteAnimation.Value = false;
            who.Breather = who.Breather;
        }

        internal static NPC Duplicate(NPC who)
        {
            string characterName = who.getTextureName();
            string filePath = $@"\Characters\{characterName.ToLower()}.xnb";

            var sprite = new AnimatedSprite(filePath, 0, who.Sprite.SpriteWidth, who.Sprite.SpriteHeight);

            //var sprite = new AnimatedSprite(who.getTextureName(), 0, who.Sprite.SpriteWidth, who.Sprite.SpriteHeight);

            var position = Utility.getHomeOfFarmer(Game1.player).getEntryLocation().ToVector2();
            var facing = who.FacingDirection;
            var name = who.Name + "_mt.guest";

            var result = new NPC(sprite, position, facing, name)
            {
                displayName = who.displayName + "_mt.guest",
                Gender = who.Gender,
                Age = who.Age,
                Portrait = who.Portrait,
                Manners = who.Manners,
                Optimism = who.Optimism,
                SocialAnxiety = who.SocialAnxiety,
                currentLocation = Utility.getHomeOfFarmer(Game1.player),
                CurrentDialogue = null,
                ignoreScheduleToday = true,
                temporaryController = null,
                Position = Utility.getHomeOfFarmer(Game1.player).getEntryLocation().ToVector2(),
                Breather = who.Breather
            };

            result.CurrentDialogue?.Clear();
            result.Schedule?.Clear();
            result.Dialogue?.Clear();
            result.CurrentDialogue.Push(new Dialogue(ModEntry.SHelper.Translation.Get("foodstore.shedguestgreeting"), result));
            result.clearSchedule();
            result.Halt();
            result.goingToDoEndOfRouteAnimation.Value = false;
            result.Sprite = sprite;
            result.Sprite.CurrentAnimation = null;


            result.modData["hapyke.FoodStore/gettingFood"] = "false";
            result.modData["hapyke.FoodStore/shedEntry"] = "-1,-1";

            result.modData["hapyke.FoodStore/timeVisitShed"] = Game1.timeOfDay.ToString();
            result.modData["hapyke.FoodStore/LastFood"] = "0";
            result.modData["hapyke.FoodStore/LastCheck"] = "0";
            result.modData["hapyke.FoodStore/LocationControl"] = ",";
            result.modData["hapyke.FoodStore/LastFoodTaste"] = "-1";
            result.modData["hapyke.FoodStore/LastFoodDecor"] = "-1";
            result.modData["hapyke.FoodStore/LastSay"] = "0";
            result.modData["hapyke.FoodStore/TotalCustomerResponse"] = "0";
            result.modData["hapyke.FoodStore/inviteTried"] = "false";
            result.modData["hapyke.FoodStore/finishedDailyChat"] = "false";
            result.modData["hapyke.FoodStore/chatDone"] = "0";

            return result;
        }
    }
}