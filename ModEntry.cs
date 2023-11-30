
using FoodStore;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using Object = StardewValley.Object;

namespace FoodStore
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry : Mod
    {

        public static IMonitor SMonitor;
        public static IModHelper SHelper;
        public static ModConfig Config;

        public static ModEntry context;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            context = this;

            SMonitor = Monitor;
            SHelper = helper;

            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;

            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.Patch(
               original: AccessTools.Method(typeof(NPC), nameof(NPC.performTenMinuteUpdate)),
               postfix: new HarmonyMethod(typeof(ModEntry), nameof(NPC_performTenMinuteUpdate_Postfix))
            );
           harmony.Patch(
               original: AccessTools.Method(typeof(NPC), nameof(NPC.dayUpdate)),
               postfix: new HarmonyMethod(typeof(ModEntry), nameof(NPC_dayUpdate_Postfix))
            );
            harmony.Patch(
               original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.updateEvenIfFarmerIsntHere)),
               postfix: new HarmonyMethod(typeof(ModEntry), nameof(FarmHouse_updateEvenIfFarmerIsntHere_Postfix))
            );



        }

        private void sendMessage()
        {
            this.Monitor.Log("A new day dawns!", LogLevel.Info);
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(
            mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.enable"),
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.minutetohungry"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.minutetohungryText"),
                getValue: () => Config.MinutesToHungry,
                setValue: value => Config.MinutesToHungry = value
            );
			configMenu.AddTextOption(
				mod: ModManifest,
				name: () => SHelper.Translation.Get("foodstore.config.movetofoodchange"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.movetofoodchangeText"),
                getValue: () => "" + Config.MoveToFoodChance,
				setValue: delegate (string value) { try { Config.MoveToFoodChance = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
			);

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.maxdistancetofindfood"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.maxdistancetofindfoodText"),
                getValue: () => "" + Config.MaxDistanceToFind,
                setValue: delegate (string value) { try { Config.MaxDistanceToFind = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
            );
            configMenu.AddTextOption(
				mod: ModManifest,
				name: () => SHelper.Translation.Get("foodstore.config.maxdistancetoeat"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.maxdistancetoeatText"),
                getValue: () => "" + Config.MaxDistanceToEat,
				setValue: delegate (string value) { try { Config.MaxDistanceToEat = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
			);


            {
                //sell multiplier
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.enableprice"),
                    tooltip: () => SHelper.Translation.Get("foodstore.config.enablepriceText"),
                    getValue: () => Config.EnablePrice,
                    setValue: value => Config.EnablePrice = value
                );

                configMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.pricelovemulti"),
                    getValue: () => "" + Config.LoveMultiplier,
                    setValue: delegate (string value) { try { Config.LoveMultiplier = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
                );
                configMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.pricelikemulti"),
                    getValue: () => "" + Config.LikeMultiplier,
                    setValue: delegate (string value) { try { Config.LikeMultiplier = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
                );
                configMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.priceneutralmulti"),
                    getValue: () => "" + Config.NeutralMultiplier,
                    setValue: delegate (string value) { try { Config.NeutralMultiplier = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
                );
                configMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.pricedislikemulti"),
                    getValue: () => "" + Config.DislikeMultiplier,
                    setValue: delegate (string value) { try { Config.DislikeMultiplier = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
                );
                configMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.pricehatemulti"),
                    getValue: () => "" + Config.HateMultiplier,
                    setValue: delegate (string value) { try { Config.HateMultiplier = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
                );
            } //Sell multiplier


            {
                //Tip


                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.enabletip"),
                    tooltip: () => SHelper.Translation.Get("foodstore.config.enabletipText"),
                    getValue: () => Config.EnableTip,
                    setValue: value => Config.EnableTip = value
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.enabletipclose"),
                    tooltip: () => SHelper.Translation.Get("foodstore.config.enabletipcloseText"),
                    getValue: () => Config.TipWhenNeaBy,
                    setValue: value => Config.TipWhenNeaBy = value
                );

                configMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.enabletipcloselove"),
                    getValue: () => "" + Config.TipLove,
                    setValue: delegate (string value) { try { Config.TipLove = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
                );
                configMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.enabletipcloselike"),
                    getValue: () => "" + Config.TipLike,
                    setValue: delegate (string value) { try { Config.TipLike = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
                );
                configMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.enabletipcloseneutral"),
                    getValue: () => "" + Config.TipNeutral,
                    setValue: delegate (string value) { try { Config.TipNeutral = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
                );
                configMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.enabletipclosedislike"),
                    getValue: () => "" + Config.TipDislike,
                    setValue: delegate (string value) { try { Config.TipDislike = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
                );
                configMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.enabletipclosehate"),
                    getValue: () => "" + Config.TipHate,
                    setValue: delegate (string value) { try { Config.TipHate = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
                );
            } //Tip multiplier
        }

        private void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            if (!Config.EnableMod)
                return;
        }


		private static bool TryToEatFood(NPC __instance, PlacedFoodData food)
		{
            if (food != null && Vector2.Distance(food.foodTile, __instance.getTileLocation()) < Config.MaxDistanceToEat && !__instance.Name.EndsWith("_DA"))
			{
				//Game1.chatBox.addInfoMessage($"eating {food.foodObject.Name} at {food.foodTile}");
				using (IEnumerator<Furniture> enumerator = __instance.currentLocation.furniture.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{

                        int taste = __instance.getGiftTasteForThisItem(food.foodObject);
						string reply;
						int salePrice = food.foodObject.sellToStorePrice();
						int tip = 0;
                        Random rand = new Random();



                        if (taste == 0)			//Love
						{
                            reply = SHelper.Translation.Get("foodstore.loverep." + rand.Next(20).ToString());

                            if (Config.LoveMultiplier ==  -1 || !Config.EnablePrice)
                            {
                                salePrice = (int)(salePrice * (1.75 + rand.NextDouble()));
                            } else salePrice = (int)(salePrice * Config.LoveMultiplier);

                            if (Config.TipLove == -1 || !Config.EnableTip)
                            {
                                tip = (int)(salePrice * 0.3);
                            }
                            else tip = (int)(salePrice * Config.TipLove);

                            if (tip < 20) { tip = 20; }
                        }
						else if (taste == 2)    //Like
                        {
                            reply = SHelper.Translation.Get("foodstore.likerep." + rand.Next(20).ToString());

                            if (Config.LikeMultiplier == -1 || !Config.EnablePrice)
                            {
                                salePrice = (int)(salePrice * (1.25 + rand.NextDouble()/2));
                            }
                            else salePrice = (int)(salePrice * Config.LikeMultiplier);

                            if (Config.TipLike == -1 || !Config.EnableTip)
                            {
                                tip = (int)(salePrice * 0.2);
                            }
                            else tip = (int)(salePrice * Config.TipLike);

                            if (tip < 10) { tip = 10; }
                        }
						else if (taste == 4)    //Dislike
                        {
                            reply = SHelper.Translation.Get("foodstore.dislikerep." + rand.Next(20).ToString());

                            if (Config.DislikeMultiplier == -1 || !Config.EnablePrice)
                            {
                                salePrice = (int)(salePrice * (0.75 + rand.NextDouble()/3));
                            }
                            else salePrice = (int)(salePrice * Config.DislikeMultiplier);

                            if (Config.TipDislike == -1 || !Config.EnableTip)
                            {
                                tip = 2;
                            }
                            else tip = (int)(salePrice * Config.TipDislike);
                        }
						else if (taste == 6)    //Hate
                        {
                            reply = SHelper.Translation.Get("foodstore.haterep." + rand.Next(20).ToString());
                            if (Config.HateMultiplier == -1 || !Config.EnablePrice)
                            {
                                salePrice = (int)(salePrice / 2);
                            }
                            else salePrice = (int)(salePrice * Config.HateMultiplier);

                            if (Config.TipHate == -1 || !Config.EnableTip)
                            {
                                tip = 0;
                            }
                            else tip = (int)(salePrice * Config.TipHate);

                        }
						else                    //Neutral
                        {
                            reply = SHelper.Translation.Get("foodstore.neutralrep." + rand.Next(20).ToString());


                            if (Config.NeutralMultiplier == -1 || !Config.EnablePrice)
                            {
                                salePrice = (int)(salePrice * (1 + rand.NextDouble() / 5));
                            }
                            else salePrice = (int)(salePrice * Config.NeutralMultiplier);

                            if (Config.TipNeutral == -1 || !Config.EnableTip)
                            {
                                tip = (int)(salePrice * 0.1);
                            }
                            else tip = (int)(salePrice * Config.TipNeutral);

                            if (tip < 5) { tip  = 5; }

                        }


                        if (Config.TipWhenNeaBy && Vector2.Distance(Game1.player.getTileLocation(), food.foodTile) > 10) { tip = 0;}



                        if (enumerator.Current.boundingBox.Value != food.furniture.boundingBox.Value)
							continue;



                        //Remove food, add money
                        enumerator.Current.heldObject.Value = null;
                        if (tip != 0)
                            __instance.showTextAboveHead(reply + SHelper.Translation.Get("foodstore.tip", new { tipValue = tip }), default, default, 7000);
                        else
                            __instance.showTextAboveHead(reply, default, default, 7000);


                        if (Game1.IsMultiplayer)
                        {
                            Game1.chatBox.globalInfoMessage(SHelper.Translation.Get("foodstore.sold", new { foodObjName = food.foodObject.Name, locationString = __instance.currentLocation.Name, saleString = salePrice }));

                            if (tip != 0)
                                Game1.chatBox.globalInfoMessage($"   {__instance.Name}: " + reply + SHelper.Translation.Get("foodstore.tip", new { tipValue = tip }));
                            else
                                Game1.chatBox.globalInfoMessage($"   {__instance.Name}: " + reply);


                        }

                        else
                        {
                            Game1.chatBox.addInfoMessage(SHelper.Translation.Get("foodstore.sold", new { foodObjName = food.foodObject.Name, locationString = __instance.currentLocation.Name, saleString = salePrice }));
                            if (tip != 0)
                                Game1.chatBox.addInfoMessage($"   {__instance.Name}: " + reply + SHelper.Translation.Get("foodstore.tip", new { tipValue = tip }));
                            else
                                Game1.chatBox.addInfoMessage($"   {__instance.Name}: " + reply);
                        }


                        Game1.player.Money += salePrice + tip;
                        __instance.modData["aedenthorn.FoodOnTheTable/LastFood"] = Game1.timeOfDay.ToString();

                        
                        return true;
					}
				}
			}
			return false;
		}

        private static PlacedFoodData GetClosestFood(NPC npc, GameLocation location)
		{

			List<PlacedFoodData> foodList = new List<PlacedFoodData>();

			foreach (var f in location.furniture)
			{
				if (f.heldObject.Value != null && f.heldObject.Value.Edibility > 0)
				{
					int xLocation = f.boundingBox.X / 64 + (f.boundingBox.Width) / 64 / 2;
					int yLocation = f.boundingBox.Y / 64 + (f.boundingBox.Height) / 64 / 2;
                    var fLocation = new Vector2(xLocation, yLocation);


                    if (Vector2.Distance(fLocation, npc.getTileLocation()) < Config.MaxDistanceToFind)
                    {
                        foodList.Add(new PlacedFoodData(f, fLocation, f.heldObject.Value, -1));
                    }
				}
            }
            if (foodList.Count == 0)
			{
				//SMonitor.Log("Got no food");
				//SMonitor.Log("Got no food");
				return null;
			}
			List<string> favList = new List<string>(Game1.NPCGiftTastes["Universal_Love"].Split(' '));
			List<string> likeList = new List<string>(Game1.NPCGiftTastes["Universal_Like"].Split(' '));
			List<string> okayList = new List<string>(Game1.NPCGiftTastes["Universal_Neutral"].Split(' '));
            List<string> dislikeList = new List<string>(Game1.NPCGiftTastes["Universal_Dislike"].Split(' '));
            List<string> hateList = new List<string>(Game1.NPCGiftTastes["Universal_Hate"].Split(' '));

            if (Game1.NPCGiftTastes.TryGetValue(npc.Name, out string NPCLikes) && NPCLikes != null)
			{
				favList.AddRange(NPCLikes.Split('/')[1].Split(' '));
				likeList.AddRange(NPCLikes.Split('/')[3].Split(' '));
				okayList.AddRange(NPCLikes.Split('/')[5].Split(' '));
                dislikeList.AddRange(NPCLikes.Split('/')[7].Split(' '));
                hateList.AddRange(NPCLikes.Split('/')[9].Split(' '));
            }

            for (int i = foodList.Count - 1; i >= 0; i--)
			{
                foodList[i].value = 0;
            }

            if (foodList.Count == 0)
			{
                //Game1.chatBox.addInfoMessage($"Got no food");
				return null;
			}

			foodList.Sort(delegate (PlacedFoodData a, PlacedFoodData b)
			{
				var compare = b.value.CompareTo(a.value);
				if (compare != 0)
					return compare;
                return (Vector2.Distance(a.foodTile, npc.getTileLocation()).CompareTo(Vector2.Distance(b.foodTile, npc.getTileLocation())));
			});
            return foodList[0];
		}
		private static Object GetObjectFromID(string id, int amount, int quality)
		{
			if (int.TryParse(id, out int index))
			{
				//SMonitor.Log($"Spawning object with index {id}");
				return new Object(index, amount, false, -1, quality);
			}
			foreach (var kvp in Game1.objectInformation)
			{
				if (kvp.Value.StartsWith(id + "/"))
					return new Object(kvp.Key, amount, false, -1, quality);
			}
			return null;
		}
        private static bool WantsToEat(NPC npc)
		{
			if (!npc.modData.ContainsKey("aedenthorn.FoodOnTheTable/LastFood") || npc.modData["aedenthorn.FoodOnTheTable/LastFood"].Length == 0  )
			{
				return true;
			}

            int lastFoodTime = int.Parse(npc.modData["aedenthorn.FoodOnTheTable/LastFood"]);
            int minutesSinceLastFood = GetMinutes(Game1.timeOfDay) - GetMinutes(lastFoodTime);

            // Check if either the time since the last food or the time since the last check is greater than the configured thresholds
            return minutesSinceLastFood > Config.MinutesToHungry;
        }


        private static bool TimeDelayCheck(NPC npc)
        {
            if (!npc.modData.ContainsKey("hapyke.FoodStore/LastCheck") || npc.modData["hapyke.FoodStore/LastCheck"].Length == 0)
            {
                return true;
            }
            int lastCheckTime = int.Parse(npc.modData["hapyke.FoodStore/LastCheck"]);
            int minutesSinceLastCheck = GetMinutes(Game1.timeOfDay) - GetMinutes(lastCheckTime);

            // Check if either the time since the last food or the time since the last check is greater than the configured thresholds
            return minutesSinceLastCheck >  20;
        }

        private static int GetMinutes(int timeOfDay)
		{
			return timeOfDay % 100 + timeOfDay / 100 * 60;
		}
    }

}