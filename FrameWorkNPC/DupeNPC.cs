using StardewValley;

namespace MarketTown
{
    internal class DupeNPC
    {
        internal static void SetVariables(NPC who)
        {
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
            result.clearSchedule();
            result.Halt();
            result.goingToDoEndOfRouteAnimation.Value = false;
            result.Sprite = sprite;
            result.Sprite.CurrentAnimation = null;
            result.modData["hapyke.FoodStore/gettingFood"] = "false";
            result.modData["hapyke.FoodStore/shedEntry"] = "-1,-1";

            return result;
        }
    }
}