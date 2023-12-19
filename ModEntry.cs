
using FoodStore;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Netcode;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using static FoodStore.ModEntry;
using static FoodStore.PlayerChat;
using static System.Net.Mime.MediaTypeNames;
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

        internal static List<Response> ResponseList { get; private set; } = new();
        internal static List<Response> KidResponseList { get; private set; } = new();
        internal static List<Action> ActionList { get; private set; } = new();

        internal static List<string> GlobalKidList = new List<string>();

        internal static Dictionary<string, int> TodaySelectedKid = new Dictionary<string, int>();



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
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChange;
            helper.Events.Player.Warped += FarmOutside.PlayerWarp;
            helper.Events.GameLoop.DayEnding += GameLoop_DayEnding;

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
        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (e.IsMultipleOf(6))
            {
                PlayerChat playerChatInstance = new PlayerChat();
                playerChatInstance.Validate();
            }

            if (Game1.hasLoadedGame && e.IsMultipleOf(30))
            {

                Farmer farmerInstance = Game1.player;
                NetStringDictionary<Friendship, NetRef<Friendship>> friendshipData = farmerInstance.friendshipData;

                try
                {
                    foreach (NPC __instance in Utility.getAllCharacters())
                    {
                        if (__instance.isVillager() && friendshipData.TryGetValue(__instance.Name, out var friendship) && !Game1.isFestival())
                        {
                            if (friendshipData[__instance.Name].TalkedToToday)
                            {
                                try
                                {
                                    if (__instance.CurrentDialogue.Count == 0 && __instance.Name != "Krobus" && __instance.Name != "Dwarf")
                                    {
                                        Random random = new Random();
                                        int randomIndex = random.Next(19);
                                        __instance.CurrentDialogue.Push(new Dialogue(SHelper.Translation.Get("foodstore.customerresponse." + randomIndex), __instance));
                                        __instance.modData["hapyke.FoodStore/TotalCustomerResponse"] = (Int32.Parse(__instance.modData["hapyke.FoodStore/TotalCustomerResponse"]) + 1).ToString();

                                        if (__instance.modData["hapyke.FoodStore/finishedDailyChat"] == "true"
                                            && Int32.Parse(__instance.modData["hapyke.FoodStore/chatDone"]) < Config.DialogueTime)
                                        {
                                            __instance.modData["hapyke.FoodStore/chatDone"] = (Int32.Parse(__instance.modData["hapyke.FoodStore/chatDone"]) + 1).ToString();
                                            var formattedQuestion = string.Format(SHelper.Translation.Get("foodstore.responselist.main"), __instance);
                                            var entryQuestion = new EntryQuestion(formattedQuestion, ResponseList, ActionList);
                                            Game1.activeClickableMenu = entryQuestion;

                                            var pc = new PlayerChat();
                                            ActionList.Add(() => pc.OnPlayerSend(__instance, "hi"));
                                            ActionList.Add(() => pc.OnPlayerSend(__instance, "invite"));
                                            ActionList.Add(() => pc.OnPlayerSend(__instance, "last dish"));
                                            ActionList.Add(() => pc.OnPlayerSend(__instance, "special today"));
                                        }
                                        __instance.modData["hapyke.FoodStore/finishedDailyChat"] = "true";
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch (NullReferenceException) { }

            }
        }

        public class MyMessage
        {
            public string MessageContent { get; set; }
            public MyMessage() { }

            public MyMessage(string content)
            {
                MessageContent = content;
            }
        }       //Send and receive message

        public void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == this.ModManifest.UniqueID && e.Type == "ExampleMessageType" && !Config.DisableChatAll)
            {
                MyMessage message = e.ReadAs<MyMessage>();
                Game1.chatBox.addInfoMessage(message.MessageContent);
                // handle message fields here
            }
        }       //Send and receive message

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
            configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => SHelper.Translation.Get("foodstore.config.rushhour"),
                    tooltip: () => SHelper.Translation.Get("foodstore.config.rushhourText"),
                    getValue: () => Config.RushHour,
                    setValue: value => Config.RushHour = value
                );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.enabledecor"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.enabledecorText"),
                getValue: () => Config.EnableDecor,
                setValue: value => Config.EnableDecor = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.enabletipclose"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.enabletipcloseText"),
                getValue: () => Config.TipWhenNeaBy,
                setValue: value => Config.TipWhenNeaBy = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.chat"),
                getValue: () => Config.DisableChat,
                setValue: value => Config.DisableChat = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.disableallmessage"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.disableallmessageText"),
                getValue: () => Config.DisableChatAll,
                setValue: value => Config.DisableChatAll = value
            );

            configMenu.AddPageLink(mod: ModManifest, "inviteTime", () => SHelper.Translation.Get("foodstore.config.invitetime"));
            configMenu.AddPageLink(mod: ModManifest, "salePrice", () => SHelper.Translation.Get("foodstore.config.saleprice"));
            configMenu.AddPageLink(mod: ModManifest, "tipValue", () => SHelper.Translation.Get("foodstore.config.tipvalue"));


            //Villager invite
            configMenu.AddPage(mod: ModManifest, "inviteTime", () => SHelper.Translation.Get("foodstore.config.invitetime"));
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.dialoguetime"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.dialoguetimeText"),
                getValue: () => "" + Config.DialogueTime,
                setValue: delegate (string value) { try { Config.DialogueTime = Int32.Parse(value, CultureInfo.InvariantCulture); } catch { } }
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.enablevisitinside"),
                getValue: () => Config.EnableVisitInside,
                setValue: value => Config.EnableVisitInside = value
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.invitecometime"),
                getValue: () => "" + Config.InviteComeTime,
                setValue: delegate (string value) { try { Config.InviteComeTime = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.inviteleavetime"),
                getValue: () => "" + Config.InviteLeaveTime,
                setValue: delegate (string value) { try { Config.InviteLeaveTime = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
            );

            //sell multiplier

            configMenu.AddPage(mod: ModManifest, "salePrice", () => SHelper.Translation.Get("foodstore.config.saleprice"));
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

            //Tip


            configMenu.AddPage(mod: ModManifest, "tipValue", () => SHelper.Translation.Get("foodstore.config.tipvalue"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.enabletip"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.enabletipText"),
                getValue: () => Config.EnableTip,
                setValue: value => Config.EnableTip = value
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
        }       // **** Config Handle ****

        private void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            if (!Config.EnableMod)
                return;
            if (ResponseList?.Count is not 4)
            {
                ResponseList.Add(new Response("Talk", SHelper.Translation.Get("foodstore.responselist.talk")));
                ResponseList.Add(new Response("Invite", SHelper.Translation.Get("foodstore.responselist.invite")));
                ResponseList.Add(new Response("FoodTaste", SHelper.Translation.Get("foodstore.responselist.foodtaste")));
                ResponseList.Add(new Response("DailyDish", SHelper.Translation.Get("foodstore.responselist.dailydish")));
            }

            if (KidResponseList?.Count is not 2)
            {
                KidResponseList.Add(new Response("Yes", SHelper.Translation.Get("foodstore.kidresponselist.yes")));
                KidResponseList.Add(new Response("No", SHelper.Translation.Get("foodstore.kidresponselist.no")));
            }

            //Generate Dish of day and dish of week on Save Loaded
            DishPrefer.dishDay = GetRandomDish();
            if (Game1.dayOfMonth == 1 || Game1.dayOfMonth == 8 || Game1.dayOfMonth == 15 || Game1.dayOfMonth == 22)
            {
                DishPrefer.dishWeek = GetRandomDish();      //Get dish of the week
            }

            GlobalKidList.Clear();
            //Assign visit value
            foreach (NPC __instance in Utility.getAllCharacters())
            {
                if (__instance.isVillager())
                {
                    __instance.modData["hapyke.FoodStore/invited"] = "false";
                    __instance.modData["hapyke.FoodStore/inviteDate"] = "-99";
                    __instance.modData["hapyke.FoodStore/finishedDailyChat"] = "false";
                    __instance.modData["hapyke.FoodStore/chatDone"] = "0";
                }

                if (__instance.Age == 2)
                {
                    GlobalKidList.Add(__instance.Name);
                }
            }
        }

        private void GameLoop_DayEnding(object sender, DayEndingEventArgs e)    //Wipe invitation on the day supposed to visit
        {
            try
            {
                foreach (NPC __instance in Utility.getAllCharacters())
                {
                    if (__instance.isVillager() && __instance.modData["hapyke.FoodStore/invited"] == "true"
                        && __instance.modData["hapyke.FoodStore/inviteDate"] == (Game1.stats.daysPlayed - 1).ToString())
                    {
                        __instance.modData["hapyke.FoodStore/invited"] = "false";
                        __instance.modData["hapyke.FoodStore/inviteDate"] = "-99";
                    }
                }
            }
            catch { }
        }

        private static bool TryToEatFood(NPC __instance, PlacedFoodData food)
        {
            if (food != null && Vector2.Distance(food.foodTile, __instance.getTileLocation()) < Config.MaxDistanceToEat && !__instance.Name.EndsWith("_DA"))
            {
                using (IEnumerator<Furniture> enumerator = __instance.currentLocation.furniture.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        int taste = 8;
                        try
                        {
                            taste = __instance.getGiftTasteForThisItem(food.foodObject);
                            if (__instance.Name == "Gus" && (taste == 0 || taste == 2)) taste = 8;
                        }
                        catch (NullReferenceException) { }
                        string reply;
                        int salePrice = food.foodObject.sellToStorePrice();
                        int tip = 0;
                        double decorPoint = GetDecorPoint(food.foodTile, __instance.currentLocation);
                        Random rand = new Random();


                        {
                            // Get Reply, Sale Price, Tip for each taste
                            if (taste == 0)         //Love
                            {
                                reply = SHelper.Translation.Get("foodstore.loverep." + rand.Next(20).ToString());

                                if (Config.LoveMultiplier == -1 || !Config.EnablePrice)
                                {
                                    salePrice = (int)(salePrice * (1.75 + rand.NextDouble()));
                                }
                                else salePrice = (int)(salePrice * Config.LoveMultiplier);

                                if (Config.TipLove == -1 || !Config.EnableTip)
                                {
                                    tip = (int)(salePrice * 0.3);
                                }
                                else tip = (int)(salePrice * Config.TipLove);

                                if (tip < 20) { tip = 20; }
                            }               //love
                            else if (taste == 2)    //Like
                            {
                                reply = SHelper.Translation.Get("foodstore.likerep." + rand.Next(20).ToString());

                                if (Config.LikeMultiplier == -1 || !Config.EnablePrice)
                                {
                                    salePrice = (int)(salePrice * (1.25 + (rand.NextDouble() / 2)));
                                }
                                else salePrice = (int)(salePrice * Config.LikeMultiplier);

                                if (Config.TipLike == -1 || !Config.EnableTip)
                                {
                                    tip = (int)(salePrice * 0.2);
                                }
                                else tip = (int)(salePrice * Config.TipLike);

                                if (tip < 10) { tip = 10; }
                            }          //like
                            else if (taste == 4)    //Dislike
                            {
                                reply = SHelper.Translation.Get("foodstore.dislikerep." + rand.Next(20).ToString());

                                if (Config.DislikeMultiplier == -1 || !Config.EnablePrice)
                                {
                                    salePrice = (int)(salePrice * (0.75 + (rand.NextDouble() / 3)));
                                }
                                else salePrice = (int)(salePrice * Config.DislikeMultiplier);

                                if (Config.TipDislike == -1 || !Config.EnableTip)
                                {
                                    tip = 2;
                                }
                                else tip = (int)(salePrice * Config.TipDislike);
                            }          //dislike
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

                            }          //hate
                            else                    //Neutral
                            {
                                reply = SHelper.Translation.Get("foodstore.neutralrep." + rand.Next(20).ToString());


                                if (Config.NeutralMultiplier == -1 || !Config.EnablePrice)
                                {
                                    salePrice = (int)(salePrice * (1 + (rand.NextDouble() / 5)));
                                }
                                else salePrice = (int)(salePrice * Config.NeutralMultiplier);

                                if (Config.TipNeutral == -1 || !Config.EnableTip)
                                {
                                    tip = (int)(salePrice * 0.1);
                                }
                                else tip = (int)(salePrice * Config.TipNeutral);

                                if (tip < 5) { tip = 5; }

                            }                          //neutral
                        } // **** SALE and TIP block ****


                        //Multiply with decoration point
                        if (Config.EnableDecor) salePrice = (int)(salePrice * (1 + decorPoint));

                        //Feature dish
                        if (food.foodObject.Name == DishPrefer.dishDay) { salePrice = (int)(salePrice * 1.2); }
                        if (food.foodObject.Name == DishPrefer.dishWeek) { salePrice = (int)(salePrice * 1.1); }

                        //Config Rush hours Price
                        if (Config.RushHour && ((800 < Game1.timeOfDay && Game1.timeOfDay < 930) || (1200 < Game1.timeOfDay && Game1.timeOfDay < 1300) || (1800 < Game1.timeOfDay && Game1.timeOfDay < 2000)))
                        {
                            salePrice = (int)(salePrice * 0.8);
                            tip = (int)(tip * 2);
                        }

                        //Number of customer interaction
                        try
                        {
                            if (__instance.modData["hapyke.FoodStore/TotalCustomerResponse"] != null)
                            {
                                double totalInteract = Int32.Parse(__instance.modData["hapyke.FoodStore/TotalCustomerResponse"]) / 150;
                                if (totalInteract > 0.25) totalInteract = 0.25;
                                salePrice = (int)(salePrice * (1 + totalInteract));
                            }
                        }
                        catch (Exception) { }

                        //Config Tip when nearby
                        if (Config.TipWhenNeaBy && Utility.isThereAFarmerWithinDistance(food.foodTile, 15, __instance.currentLocation) == null) { tip = 0; }

                        //Remove food
                        if (enumerator.Current.boundingBox.Value != food.furniture.boundingBox.Value)
                            continue;
                        enumerator.Current.heldObject.Value = null;

                        //Money on/off farm
                        if (__instance.currentLocation is not FarmHouse && __instance.currentLocation is not Farm && !Config.DisableChatAll)
                        {
                            //Generate chat box
                            if (tip != 0)
                                __instance.showTextAboveHead(reply + SHelper.Translation.Get("foodstore.tip", new { tipValue = tip }), default, default, 7000);
                            else
                                __instance.showTextAboveHead(reply, default, default, 7000);

                            //Generate chat box
                            if (Game1.IsMultiplayer)
                            {
                                Game1.chatBox.addInfoMessage(SHelper.Translation.Get("foodstore.sold", new { foodObjName = food.foodObject.Name, locationString = __instance.currentLocation.Name, saleString = salePrice }));
                                MyMessage messageToSend = new MyMessage(SHelper.Translation.Get("foodstore.sold", new { foodObjName = food.foodObject.Name, locationString = __instance.currentLocation.Name, saleString = salePrice }));
                                SHelper.Multiplayer.SendMessage(messageToSend, "ExampleMessageType");

                                if (!Config.DisableChat)
                                {
                                    if (tip != 0)
                                    {
                                        Game1.chatBox.addInfoMessage($"   {__instance.Name}: " + reply + SHelper.Translation.Get("foodstore.tip", new { tipValue = tip }));
                                        messageToSend = new MyMessage($"   {__instance.Name}: " + reply + SHelper.Translation.Get("foodstore.tip", new { tipValue = tip }));
                                        SHelper.Multiplayer.SendMessage(messageToSend, "ExampleMessageType");
                                    }
                                    else
                                    {
                                        Game1.chatBox.addInfoMessage($"   {__instance.Name}: " + reply);
                                        messageToSend = new MyMessage($"   {__instance.Name}: " + reply);
                                        SHelper.Multiplayer.SendMessage(messageToSend, "ExampleMessageType");
                                    }
                                }
                            }
                            else
                            {
                                Game1.chatBox.addInfoMessage(SHelper.Translation.Get("foodstore.sold", new { foodObjName = food.foodObject.Name, locationString = __instance.currentLocation.Name, saleString = salePrice }));
                                if (!Config.DisableChat)
                                {
                                    if (tip != 0)
                                        Game1.chatBox.addInfoMessage($"   {__instance.Name}: " + reply + SHelper.Translation.Get("foodstore.tip", new { tipValue = tip }));
                                    else
                                        Game1.chatBox.addInfoMessage($"   {__instance.Name}: " + reply);
                                }
                            }
                        }           //Food outside farmhouse

                        else
                        {
                            if (__instance.currentLocation is FarmHouse)
                            {
                                Farmer owner = (__instance.currentLocation as FarmHouse).owner;
                                if (owner.friendshipData.ContainsKey(__instance.Name))
                                {
                                    int points = 5;
                                    switch (taste)
                                    {
                                        case 0:
                                            points = 15;
                                            break;
                                        case 2:
                                            points = 10;
                                            break;
                                        case 4:
                                            points = 0;
                                            break;
                                        case 6:
                                            points = -5;
                                            break;
                                        case 8:
                                            points = 5;
                                            break;
                                        default:
                                            __instance.doEmote(20);
                                            break;
                                    }
                                    if (owner.Name == "HaPyke" || owner.Name == "d5a1lamdtd") points = (int)(points * 2);
                                    owner.friendshipData[__instance.Name].Points += (int)points;
                                }
                            }

                            Random random = new Random();
                            int randomNumber = random.Next(12);
                            salePrice = tip = 0;

                            if (!Config.DisableChatAll) __instance.showTextAboveHead(SHelper.Translation.Get("foodstore.spouseeat." + randomNumber));
                        }           //Food in farmhouse

                        Game1.player.Money += salePrice + tip;
                        __instance.modData["hapyke.FoodStore/LastFood"] = Game1.timeOfDay.ToString();
                        __instance.modData["hapyke.FoodStore/LastFoodTaste"] = taste.ToString();
                        __instance.modData["hapyke.FoodStore/LastFoodDecor"] = decorPoint.ToString();

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
                    int xLocation = (f.boundingBox.X / 64) + (f.boundingBox.Width / 64 / 2);
                    int yLocation = (f.boundingBox.Y / 64) + (f.boundingBox.Height / 64 / 2);
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
                return Vector2.Distance(a.foodTile, npc.getTileLocation()).CompareTo(Vector2.Distance(b.foodTile, npc.getTileLocation()));
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
            if (!npc.modData.ContainsKey("hapyke.FoodStore/LastFood") || npc.modData["hapyke.FoodStore/LastFood"].Length == 0)
            {
                return true;
            }

            int lastFoodTime = int.Parse(npc.modData["hapyke.FoodStore/LastFood"]);
            int minutesSinceLastFood = GetMinutes(Game1.timeOfDay) - GetMinutes(lastFoodTime);

            // Check if either the time since the last food or the time since the last check is greater than the configured thresholds
            return minutesSinceLastFood > Config.MinutesToHungry;
        }

        private static bool WantsToSay(NPC npc, int time)
        {
            if (Config.DisableChatAll) { return false; }
            if (!npc.modData.ContainsKey("hapyke.FoodStore/LastSay") || npc.modData["hapyke.FoodStore/LastSay"].Length == 0)
            {
                return true;
            }

            int lastSayTime = int.Parse(npc.modData["hapyke.FoodStore/LastSay"]);
            int minutesSinceLastFood = GetMinutes(Game1.timeOfDay) - GetMinutes(lastSayTime);

            // Check if either the time since the last food or the time since the last check is greater than the configured thresholds
            return minutesSinceLastFood > time;
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
            return minutesSinceLastCheck > 20;
        }

        private static int GetMinutes(int timeOfDay)
        {
            return (timeOfDay % 100) + (timeOfDay / 100 * 60);
        }

        public static double GetDecorPoint(Vector2 foodLoc, GameLocation gameLocation)
        {
            //init
            double decorPoint = 0;

            //Furniture check
            for (var y = foodLoc.Y - 9; y < foodLoc.Y + 9; y++)
            {
                for (var x = foodLoc.X - 9; x < foodLoc.X + 9; x++)
                {
                    StardewValley.Object obj = gameLocation.getObjectAtTile((int)x, (int)y) ?? null;

                    if (obj != null)
                    {
                        if (obj.getCategoryName() == "Furniture" || obj.getCategoryName() == "Decoration")
                        {
                            decorPoint += 1;
                        }
                    }
                }
            }

            //Water nearby check
            for (var y = foodLoc.Y - 7; y < foodLoc.Y + 7; y++)
            {
                for (var x = foodLoc.X - 7; x < foodLoc.X + 7; x++)
                {
                    if (Game1.player.currentLocation.isWaterTile((int)x, (int)y))
                    {
                        decorPoint += 5;
                        break;
                    }
                }
            }

            // Check if player are below the pink tree in Town, Mountain and Forest
            if (gameLocation.Name == "Town" ||
                gameLocation.Name == "Mountain" ||
                gameLocation.Name == "Forest")
            {
                float pinkTreeRadius = 7;

                for (var y = foodLoc.Y - pinkTreeRadius; y < foodLoc.Y + pinkTreeRadius; y++)
                {
                    for (var x = foodLoc.X - pinkTreeRadius; x < foodLoc.X + pinkTreeRadius; x++)
                    {
                        int TileId = Game1.player.currentLocation.getTileIndexAt((int)x, (int)y, "Buildings");

                        if (TileId == 143 || TileId == 144 ||
                            TileId == 168 || TileId == 169)
                        {
                            decorPoint += 10;
                        }
                    }
                }
            }

            if (decorPoint > 85) return 0.5;
            else if (decorPoint > 67) return 0.4;
            else if (decorPoint > 52) return 0.3;
            else if (decorPoint > 48) return 0.2;
            else if (decorPoint > 30) return 0.1;
            else if (decorPoint > 20) return 0.0;
            else if (decorPoint > 15) return -0.1;
            else return -0.2;

        }

        public static void SaySomething(NPC thisCharacter, GameLocation thisLocation, double lastTasteRate, double lastDecorRate)
        {
            double chanceToVisit = lastTasteRate + lastDecorRate;
            double localNpcCount = 0.6;

            Random rand = new Random();
            double getChance = rand.NextDouble();

            foreach (NPC newCharacter in thisLocation.characters)
            {
                if (Utility.isThereAFarmerOrCharacterWithinDistance(new Microsoft.Xna.Framework.Vector2(thisCharacter.getTileLocation().X, thisCharacter.getTileLocation().Y), 13, thisCharacter.currentLocation) != null
                    && localNpcCount > 0.2) localNpcCount -= 0.0175;

            }

            foreach (NPC newCharacter in thisLocation.characters)
            {
                if (Vector2.Distance(newCharacter.getTileLocation(), thisCharacter.getTileLocation()) <= (float)15 && newCharacter.Name != thisCharacter.Name && !Config.DisableChatAll)
                {
                    Random random = new Random();
                    int randomIndex = random.Next(5);
                    int randomIndex2 = random.Next(3);

                    //taste string
                    string tasteString = "string";
                    if (lastTasteRate > 0.3) tasteString = SHelper.Translation.Get("foodstore.positiveTasteString." + randomIndex);
                    else if (lastTasteRate == 0.3) tasteString = SHelper.Translation.Get("foodstore.normalTasteString." + randomIndex);
                    else if (lastTasteRate < 0.3) tasteString = SHelper.Translation.Get("foodstore.negativeTasteString." + randomIndex);

                    //decor string
                    string decorString = "string";
                    if (lastDecorRate > 0) decorString = SHelper.Translation.Get("foodstore.positiveDecorString." + randomIndex);
                    else if (lastDecorRate == 0) decorString = SHelper.Translation.Get("foodstore.normalDecorString." + randomIndex);
                    else if (lastDecorRate < 0) decorString = SHelper.Translation.Get("foodstore.negativeDecorString." + randomIndex);

                    //do the work
                    if (getChance < chanceToVisit && lastTasteRate > 0.3 && lastDecorRate > 0)          //Will visit, positive Food, positive Decor
                    {
                        thisCharacter.showTextAboveHead(tasteString + ". " + decorString, -1, 2, 8000);
                        if (rand.NextDouble() > localNpcCount) continue;

                        if (newCharacter.modData["hapyke.FoodStore/LastFood"] == null) newCharacter.modData["hapyke.FoodStore/LastFood"] = "0";
                        newCharacter.modData["hapyke.FoodStore/LastFood"] = (Int32.Parse(newCharacter.modData["hapyke.FoodStore/LastFood"]) - Config.MinutesToHungry + 30).ToString();
                        newCharacter.showTextAboveHead(SHelper.Translation.Get("foodstore.willVisit." + randomIndex2), -1, 2, 7000);
                    }
                    else if (getChance < chanceToVisit)                                                 //Will visit, normal or negative Food , Decor
                    {
                        thisCharacter.showTextAboveHead(tasteString + ". " + decorString, -1, 2, 8000);
                        if (rand.NextDouble() > localNpcCount) continue;

                        if (newCharacter.modData["hapyke.FoodStore/LastFood"] == null) newCharacter.modData["hapyke.FoodStore/LastFood"] = "0";
                        if (Config.MinutesToHungry >= 60)
                            newCharacter.modData["hapyke.FoodStore/LastFood"] = (Int32.Parse(newCharacter.modData["hapyke.FoodStore/LastFood"]) - (Config.MinutesToHungry / 2)).ToString();
                        newCharacter.showTextAboveHead(SHelper.Translation.Get("foodstore.mayVisit." + randomIndex2), -1, 2, 7000);
                    }
                    else if (getChance >= chanceToVisit && lastTasteRate < 0.3 && lastDecorRate < 0)     //No visit, negative Food, negative Decor
                    {
                        thisCharacter.showTextAboveHead(tasteString + ". " + decorString, -1, 2, 8000);
                        if (rand.NextDouble() > localNpcCount) continue;

                        if (newCharacter.modData["hapyke.FoodStore/LastFood"] == null) newCharacter.modData["hapyke.FoodStore/LastFood"] = "2600";
                        newCharacter.modData["hapyke.FoodStore/LastFood"] = "2600";
                        newCharacter.showTextAboveHead(SHelper.Translation.Get("foodstore.noVisit." + randomIndex2), -1, 2, 7000);
                    }
                    else if (getChance >= chanceToVisit)                                                 //No visit, normal or positive Food, Decor
                    {
                        thisCharacter.showTextAboveHead(tasteString + ". " + decorString, -1, 2, 8000);
                        if (rand.NextDouble() > localNpcCount) continue;

                        newCharacter.showTextAboveHead(SHelper.Translation.Get("foodstore.mayVisit." + randomIndex2), -1, 2, 7000);
                    }
                    else { }    //Handle
                }
            }
        }

        public static string GetRandomDish()
        {

            List<string> resultList = new List<string>();

            foreach (var obj in Game1.objectInformation)
            {
                var key = obj.Key;
                var value = obj.Value;
                string[] splitStrings = value.Split('/');

                if (splitStrings.Length >= 3 && splitStrings[2] != "-300" && splitStrings[3] == "Cooking -7")
                {
                    resultList.Add(splitStrings[0]);
                }
            }

            if (resultList.Count > 0)
            {
                Random random = new Random();
                int randomIndex = random.Next(resultList.Count);
                string randomElement = resultList[randomIndex];
                return randomElement;
            }

            return "Farmer's Lunch";
        }


        private void OnTimeChange(object sender, TimeChangedEventArgs e)
        {
            if (Game1.timeOfDay > Config.InviteComeTime)
            {
                foreach (NPC c in Utility.getAllCharacters())
                {
                    if (c.isVillager() && c.currentLocation.Name == "Farm" && c.modData["hapyke.FoodStore/invited"] == "true" && c.modData["hapyke.FoodStore/inviteDate"] == (Game1.stats.daysPlayed - 1).ToString())
                    {
                        FarmOutside.WalkAroundFarm(c.Name);
                    }

                    if (c.isVillager() && c.currentLocation.Name == "FarmHouse" && c.modData["hapyke.FoodStore/invited"] == "true" && c.modData["hapyke.FoodStore/inviteDate"] == (Game1.stats.daysPlayed - 1).ToString())
                    {
                        FarmOutside.WalkAroundHouse(c.Name);
                    }
                }
            }

            if (Game1.timeOfDay == 630 && Game1.player.currentLocation.Name == "Farm")
            {
                TodaySelectedKid.Clear();
                double visitChance = 0.5; // Default value
                Random random = new Random();

                foreach (string kid in GlobalKidList)
                {
                    if (random.NextDouble() < 0.75)
                    {
                        int friendshipLevel = Game1.player.getFriendshipHeartLevelForNPC(kid);
                        double addKid = 0;
                        switch (friendshipLevel)
                        {
                            case int lv when lv < 2:
                                addKid = 0.2;
                                break;

                            case int lv when lv >= 2 && lv < 4:
                                addKid = 0.4;
                                break;

                            case int lv when lv >= 4 && lv < 6:
                                addKid = 0.6;
                                break;

                            case int lv when lv >= 6:
                                addKid = 0.8;
                                break;
                            default:
                                break;
                        }
                        if (random.NextDouble() < addKid && Game1.getCharacterFromName(kid).modData["hapyke.FoodStore/invited"] != "true") TodaySelectedKid.Add(kid, friendshipLevel);
                    }
                }
                if (random.NextDouble() < visitChance && TodaySelectedKid.Count != 0)
                {
                    string[] keysArray = new List<string>(TodaySelectedKid.Keys).ToArray();
                    string randomKey = keysArray[new Random().Next(keysArray.Length)];
                    var formattedQuestion = "";

                    if (TodaySelectedKid.Count() == 1) formattedQuestion = string.Format(SHelper.Translation.Get("foodstore.kidask", new { kidName = Game1.getCharacterFromName(randomKey).Name }), Game1.getCharacterFromName(randomKey).Name);
                    else formattedQuestion = string.Format(SHelper.Translation.Get("foodstore.groupkidask"));

                    var entryQuestion = new EntryQuestion(formattedQuestion, KidResponseList, ActionList);
                    Game1.activeClickableMenu = entryQuestion;

                    ActionList.Add(() => KidJoin(TodaySelectedKid));
                    ActionList.Add(() => Game1.drawDialogue(Game1.getCharacterFromName(randomKey), SHelper.Translation.Get("foodstore.kidresponselist.boring")));

                }
            }
        }

        public static bool IsOutside { get; internal set; }
        internal static List<string> FurnitureList { get; private set; } = new();
        internal static List<string> Animals { get; private set; } = new();
        internal static Dictionary<int, string> Crops { get; private set; } = new();
    }
}