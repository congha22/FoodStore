﻿using HarmonyLib;
using MarketTown;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Netcode;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
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
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using static MarketTown.ModEntry;
using static MarketTown.PlayerChat;
using static System.Net.Mime.MediaTypeNames;
using Object = StardewValley.Object;
using SpaceShared;
using SpaceShared.APIs;
using MarketTown.Framework;
using static StardewValley.Minigames.TargetGame;
using StardewModdingAPI.Utilities;
using xTile.ObjectModel;
using System.Text.RegularExpressions;
using xTile;
using xTile.Dimensions;
using StardewValley.Pathfinding;

namespace MarketTown
{
    /// <summary>The mod entry point.</summary>

    public partial class ModEntry : Mod
    {

        public static ModEntry Instance;

        public static IMonitor SMonitor;
        public static IModHelper SHelper;
        public static ModConfig Config;

        public static ModEntry context;

        public static string orderKey = "marketTown/order";
        public static Texture2D emoteSprite;
        public static PerScreen<Dictionary<string, int>> npcOrderNumbers = new PerScreen<Dictionary<string, int>>();
        public static Dictionary<string, NetRef<Chest>> fridgeDict = new();

        private Harmony harmony;

        internal static List<Response> ResponseList { get; private set; } = new();
        internal static List<Response> KidResponseList { get; private set; } = new();
        internal static List<Action> ActionList { get; private set; } = new();

        internal static List<string> GlobalKidList = new List<string>();

        internal static List<string> GlobalNPCList = new List<string>();

        internal static List<string> CurrentShopper = new List<string>();

        internal static List<string> ShoppersToRemove = new List<string>();

        internal static Dictionary<string, int> TodaySelectedKid = new Dictionary<string, int>();
        internal static List<(string locationName, int x, int y)> ChairPositions { get; private set; } = new List<(string, int, int)>();



        //
        // *************************** ENTRY ***************************
        //


        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            npcOrderNumbers.Value = new Dictionary<string, int>();

            Config = Helper.ReadConfig<ModConfig>();

            context = this;
            ModEntry.Instance = this;

            SMonitor = Monitor;
            SHelper = helper;

            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChange;
            helper.Events.GameLoop.DayEnding += GameLoop_DayEnding;
            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;

            Helper.Events.GameLoop.OneSecondUpdateTicked += GameLoop_OneSecondUpdateTicked;

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;

            helper.ConsoleCommands.Add("markettown", "display", this.HandleCommand);

            helper.Events.Content.AssetRequested += this.OnAssetRequested;

            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;

            helper.Events.Player.Warped += FarmOutside.PlayerWarp;

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

            //harmony.Patch(original: AccessTools.Method(typeof(StardewValley.Objects.Furniture), nameof(StardewValley.Objects.Furniture.clicked)),
            //              prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(FurniturePatches.clicked_Prefix)));
            //harmony.Patch(original: AccessTools.Method(typeof(StardewValley.Objects.Furniture), nameof(StardewValley.Objects.Furniture.performObjectDropInAction)),
            //              postfix: new HarmonyMethod(typeof(FurniturePatches), nameof(FurniturePatches.performObjectDropInAction_Postfix)));

            ////bug draw behind chair
            //harmony.Patch(original: AccessTools.Method(typeof(StardewValley.Objects.Furniture), nameof(StardewValley.Objects.Furniture.draw),
            //              new Type[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
            //              prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(FurniturePatches.draw_Prefix)));

            //harmony.Patch(original: AccessTools.Method(typeof(StardewValley.Game1), nameof(StardewValley.Game1.pressActionButton)),
            //              prefix: new HarmonyMethod(typeof(Game1Patches), nameof(Game1Patches.pressActionButton_Prefix)));

            ////bug open door
            //harmony.Patch(original: AccessTools.Method(typeof(StardewValley.GameLocation), nameof(StardewValley.GameLocation.checkAction)),
            //              prefix: new HarmonyMethod(typeof(GameLocationPatches), nameof(GameLocationPatches.checkAction_Prefix)));

            //// Save handlers to prevent custom objects from being saved to file.
            //helper.Events.GameLoop.Saving += (s, e) => makePlaceholderObjects();
            //helper.Events.GameLoop.Saved += (s, e) => restorePlaceholderObjects();
            //helper.Events.GameLoop.SaveLoaded += (s, e) => restorePlaceholderObjects();

            harmony.PatchAll();
        }

        //
        // ***************************  END OF ENTRY ***************************
        //

        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            fridgeDict.Clear();
            npcOrderNumbers.Value.Clear();
            emoteSprite = SHelper.ModContent.Load<Texture2D>(Path.Combine("Assets", "emote.png"));
        }

        private void GameLoop_OneSecondUpdateTicked(object sender, StardewModdingAPI.Events.OneSecondUpdateTickedEventArgs e)
        {
            if (Config.EnableMod && Context.IsPlayerFree && Config.RestaurantLocations.Contains(Game1.player.currentLocation.Name))
            {
                UpdateOrders();
            }

            if (Game1.hasLoadedGame
                && !(Game1.player.isRidingHorse()
                    || Game1.currentLocation == null
                    || Game1.eventUp
                    || Game1.isFestival()
                    || Game1.IsFading()
                    || Game1.activeClickableMenu != null))
            {

                Farmer farmerInstance = Game1.player;
                NetStringDictionary<Friendship, NetRef<Friendship>> friendshipData = farmerInstance.friendshipData;

                try
                {
                    foreach (NPC __instance in Utility.getAllCharacters())
                    {

                        // ******* Check NPC valid tile *******

                        if (__instance != null && __instance.isVillager() && __instance.currentLocation != null && __instance.Tile != __instance.DefaultPosition / 64
                            && __instance.Sprite.CurrentFrame >= 0 && __instance.Sprite.CurrentFrame <= 15
                            && !ChairPositions.Any(chairPosition => chairPosition.locationName == __instance.currentLocation.Name &&
                                        chairPosition.x == (int)__instance.Tile.X &&
                                        chairPosition.y == (int)__instance.Tile.Y)
                            )
                        {
                            Point zero = new Point((int)__instance.Tile.X, (int)__instance.Tile.Y);
                            var location = __instance.currentLocation;

                            bool isValid = location.isTileOnMap(__instance.Tile) && !location.isWaterTile(zero.X, zero.Y)
                                && location.isTilePassable(new Location(zero.X, zero.Y), Game1.viewport);


                            if (!isValid)
                            {
                                __instance.isCharging = true;
                                __instance.addedSpeed = 1;
                                __instance.returnToEndPoint();
                                __instance.MovePosition(Game1.currentGameTime, Game1.viewport, __instance.currentLocation);
                            }
                        }

                        // ******* end of check *******


                        if (__instance != null && __instance.isVillager() && (friendshipData.TryGetValue(__instance.Name, out var friendship) || __instance.Name.Contains("MT.Guest_")))
                        {
                            if ((!__instance.Name.Contains("MT.Guest_") && friendshipData[__instance.Name].TalkedToToday) || __instance.Name.Contains("MT.Guest_"))
                            {
                                try
                                {
                                    if (__instance.CurrentDialogue.Count == 0 && __instance.Name != "Krobus" && __instance.Name != "Dwarf")
                                    {
                                        Random random = new Random();
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

                                        if (__instance.Name.Contains("MT.Guest_") || !Game1.player.friendshipData[__instance.Name].IsMarried())
                                        {
                                            __instance.CurrentDialogue.Push(new Dialogue(__instance, SHelper.Translation.Get("foodstore.general." + npcAge + npcManner + npcSocial + randomIndex.ToString())));
                                            __instance.modData["hapyke.FoodStore/TotalCustomerResponse"] = (Int32.Parse(__instance.modData["hapyke.FoodStore/TotalCustomerResponse"]) + 1).ToString();
                                        }
                                        else
                                        {
                                            if (Game1.timeOfDay == 900 || Game1.timeOfDay == 1200 || Game1.timeOfDay == 1500 || Game1.timeOfDay == 1800 || Game1.timeOfDay == 2100 || Game1.timeOfDay == 2400)
                                            {
                                                __instance.CurrentDialogue.Push(new Dialogue(__instance, SHelper.Translation.Get("foodstore.general." + npcAge + npcManner + npcSocial + randomIndex.ToString())));
                                                __instance.modData["hapyke.FoodStore/TotalCustomerResponse"] = (Int32.Parse(__instance.modData["hapyke.FoodStore/TotalCustomerResponse"]) + 1).ToString();
                                            }
                                        }



                                        if (__instance.modData["hapyke.FoodStore/finishedDailyChat"] == "true"
                                            && Int32.Parse(__instance.modData["hapyke.FoodStore/chatDone"]) < Config.DialogueTime
                                            && !__instance.Name.Contains("MT.Guest_"))
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

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var sc = this.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            sc.RegisterSerializerType(typeof(Mannequin));
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Characters\\Farmer\\farmer_transparent"))
                e.LoadFromModFile<Texture2D>("FrameworkClothes/assets/farmer_transparent.png", AssetLoadPriority.Exclusive);
        }

        private void HandleCommand(string cmd, string[] args)
        {
            if (!Context.IsPlayerFree)
                return;

            if (args.Length == 0)
            {
                return;
            }

            Item item = null;
            if (args[0] == "display")
            {
                var mannType = MannequinType.Plain;
                var mannGender = MannequinGender.Male;
                if (args.Length >= 2)
                {
                    switch (args[1].ToLower())
                    {
                        case "male":
                            mannGender = MannequinGender.Male;
                            break;
                        case "female":
                            mannGender = MannequinGender.Female;
                            break;
                        default:
                            return;
                    }
                }
                item = new Mannequin(mannType, mannGender, Vector2.Zero);
            }

            if (item == null)
            {
                return;
            }

            if (args.Length >= 3)
            {
                item.Stack = int.Parse(args[2]);
            }

            Game1.player.addItemByMenuIfNecessary(item);
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (!Game1.hasLoadedGame) return;

            foreach (var x in Utility.getAllCharacters())
            {
                if (x.Name.Contains("MT.Guest_"))
                {
                    Game1.player.friendshipData.Remove(x.Name);
                }
            }

            if (e.NewMenu is ShopMenu shop)
            {
                if (shop.ShopId == "Carpenter")
                {
                    var mm = new Mannequin(MannequinType.Plain, MannequinGender.Male, Vector2.Zero);
                    var mf = new Mannequin(MannequinType.Plain, MannequinGender.Female, Vector2.Zero);
                    shop.forSale.Add(mm);
                    shop.forSale.Add(mf);
                    shop.itemPriceAndStock.Add(mm, new ItemStockInformation(1000, int.MaxValue));
                    shop.itemPriceAndStock.Add(mf, new ItemStockInformation(1000, int.MaxValue));
                }
            }
        }

        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (e.IsMultipleOf(6))
            {
                PlayerChat playerChatInstance = new PlayerChat();
                playerChatInstance.Validate();
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
            Texture2D originalTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("Assets/markettown.png");

            int newWidth = (int)(originalTexture.Width / 1.35);
            int newHeight = (int)(originalTexture.Height / 1.35);
            Texture2D resizedTexture = new Texture2D(originalTexture.GraphicsDevice, newWidth, newHeight);

            Color[] originalData = new Color[originalTexture.Width * originalTexture.Height];
            originalTexture.GetData(originalData);
            Color[] resizedData = new Color[newWidth * newHeight];

            // Resize
            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    int originalX = (int)(x * 1.35);
                    int originalY = (int)(y * 1.35);    
                    resizedData[x + y * newWidth] = originalData[originalX + originalY * originalTexture.Width];
                }
            }
            resizedTexture.SetData(resizedData);
            Microsoft.Xna.Framework.Rectangle displayArea = new Microsoft.Xna.Framework.Rectangle(0, 0, newWidth, newHeight);

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

            configMenu.AddImage(mod: ModManifest, texture: () => resizedTexture, texturePixelArea: null, scale: 1);

            configMenu.AddBoolOption(
            mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.enable"),
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );

            configMenu.AddBoolOption(
            mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.disablenonfoodonfarm"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.disablenonfoodonfarmText"),
                getValue: () => Config.AllowRemoveNonFood,
                setValue: value => Config.AllowRemoveNonFood = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.minutetohungry"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.minutetohungryText"),
                getValue: () => Config.MinutesToHungry,
                setValue: value => Config.MinutesToHungry = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.movetofoodchange"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.movetofoodchangeText"),
                getValue: () => Config.MoveToFoodChance,
                setValue: value => Config.MoveToFoodChance = value,
                min: 0.0f,
                max: 0.33f,
                interval: 0.0025f
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
                name: () => SHelper.Translation.Get("foodstore.config.randompurchase"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.randompurchaseText"),
                getValue: () => Config.RandomPurchase,
                setValue: value => Config.RandomPurchase = value
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.signrange"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.signrangeText"),
                getValue: () => "" + Config.SignRange,
                setValue: delegate (string value) { try { Config.SignRange = int.Parse(value, CultureInfo.InvariantCulture); } catch { } }
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
            configMenu.AddPageLink(mod: ModManifest, "shed", () => SHelper.Translation.Get("foodstore.config.shed"));
            configMenu.AddPageLink(mod: ModManifest, "dialogue", () => SHelper.Translation.Get("foodstore.config.dialogue"));
            configMenu.AddPageLink(mod: ModManifest, "inviteTime", () => SHelper.Translation.Get("foodstore.config.invitetime"));
            configMenu.AddPageLink(mod: ModManifest, "salePrice", () => SHelper.Translation.Get("foodstore.config.saleprice"));
            configMenu.AddPageLink(mod: ModManifest, "tipValue", () => SHelper.Translation.Get("foodstore.config.tipvalue"));

            // Shed setting
            configMenu.AddPage(mod: ModManifest, "shed", () => SHelper.Translation.Get("foodstore.config.shed"));

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.shedvisitchance"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.shedvisitchanceText"),
                getValue: () => Config.ShedVisitChance,
                setValue: value => Config.ShedVisitChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.maxshedcapacity"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.maxshedcapacityText"),
                getValue: () => "" + Config.MaxShedCapacity,
                setValue: delegate (string value) { try { Config.MaxShedCapacity = Int32.Parse(value, CultureInfo.InvariantCulture); } catch { } }
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.timestay"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.timestayText"),
                getValue: () => "" + Config.TimeStay,
                setValue: delegate (string value) { try { Config.TimeStay = Int32.Parse(value, CultureInfo.InvariantCulture); } catch { } }
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.doorentry"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.doorentryText"),
                getValue: () => Config.DoorEntry,
                setValue: value => Config.DoorEntry = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.buswalk"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.buswalkText"),
                getValue: () => Config.BusWalk,
                setValue: value => Config.BusWalk = value
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.shedminutetohungry"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.shedminutetohungryText"),
                getValue: () => "" + Config.ShedMinuteToHungry,
                setValue: delegate (string value) { try { Config.ShedMinuteToHungry = Int32.Parse(value, CultureInfo.InvariantCulture); } catch { } }
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.shedbuychance"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.shedbuychanceText"),
                getValue: () => Config.ShedMoveToFoodChance,
                setValue: value => Config.ShedMoveToFoodChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.openhour"),
                getValue: () => Config.OpenHour,
                setValue: value => Config.OpenHour = (int)value,
                min: 600,
                max: 2400f,
                interval: 10f
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.closehour"),
                getValue: () => Config.CloseHour,
                setValue: value => Config.CloseHour = (int)value,
                min: 600,
                max: 2400f,
                interval: 10f
            );

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => SHelper.Translation.Get("foodstore.config.shedvisitor")
            );

            configMenu.AddKeybind(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.modkey"),
                getValue: () => Config.ModKey,
                setValue: value => Config.ModKey = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.orderchance"),
                getValue: () => Config.OrderChance,
                setValue: value => Config.OrderChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.lovedishchance"),
                getValue: () => Config.LovedDishChance,
                setValue: value => Config.LovedDishChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.pricemultiplier"),
                getValue: () => "" + Config.PriceMarkup,
                setValue: delegate (string value) { try { Config.PriceMarkup = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.maxordernight"),
                getValue: () => "" + Config.MaxNPCOrdersPerNight,
                setValue: delegate (string value) { try { Config.MaxNPCOrdersPerNight = Int32.Parse(value, CultureInfo.InvariantCulture); } catch { } }
            );

            //Dialogue setting
            configMenu.AddPage(mod: ModManifest, "dialogue", () => SHelper.Translation.Get("foodstore.config.dialogue"));
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.dialoguetime"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.dialoguetimeText"),
                getValue: () => "" + Config.DialogueTime,
                setValue: delegate (string value) { try { Config.DialogueTime = Int32.Parse(value, CultureInfo.InvariantCulture); } catch { } }
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.chat"),
                getValue: () => Config.DisableChat,
                setValue: value => Config.DisableChat = value
            );
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.kidaskchance"),
                getValue: () => "" + Config.KidAskChance,
                setValue: delegate (string value) { try { Config.KidAskChance = float.Parse(value, CultureInfo.InvariantCulture); } catch { } }
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.disablekidask"),
                getValue: () => Config.DisableKidAsk,
                setValue: value => Config.DisableKidAsk = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.disableallmessage"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.disableallmessageText"),
                getValue: () => Config.DisableChatAll,
                setValue: value => Config.DisableChatAll = value
            );


            //Villager invite
            configMenu.AddPage(mod: ModManifest, "inviteTime", () => SHelper.Translation.Get("foodstore.config.invitetime"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.enablevisitinside"),
                getValue: () => Config.EnableVisitInside,
                setValue: value => Config.EnableVisitInside = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.invitecometime"),
                getValue: () => Config.InviteComeTime,
                setValue: value => Config.InviteComeTime = (int)value,
                min: 600,
                max: 2400f,
                interval: 10f
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.inviteleavetime"),
                getValue: () => Config.InviteLeaveTime,
                setValue: value => Config.InviteLeaveTime = (int)value,
                min: 600,
                max: 2400f,
                interval: 10f
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
            GlobalNPCList.Clear();
            ShoppersToRemove.Clear();

            //Assign visit value
            foreach (NPC __instance in Utility.getAllCharacters())
            {
                if (__instance is not null && __instance.isVillager())
                {
                    __instance.modData["hapyke.FoodStore/shedEntry"] = "-1,-1";
                    __instance.modData["hapyke.FoodStore/gettingFood"] = "false";
                    __instance.modData["hapyke.FoodStore/invited"] = "false";
                    __instance.modData["hapyke.FoodStore/inviteDate"] = "-99";
                    __instance.modData["hapyke.FoodStore/finishedDailyChat"] = "false";
                    __instance.modData["hapyke.FoodStore/chatDone"] = "0";
                    __instance.modData["hapyke.FoodStore/timeVisitShed"] = "0";
                    __instance.modData["hapyke.FoodStore/LastFood"] = "0";
                    __instance.modData["hapyke.FoodStore/LastCheck"] = "0";
                    __instance.modData["hapyke.FoodStore/LocationControl"] = ",";
                    __instance.modData["hapyke.FoodStore/LastFoodTaste"] = "-1";
                    __instance.modData["hapyke.FoodStore/LastFoodDecor"] = "-1";
                    __instance.modData["hapyke.FoodStore/LastSay"] = "0";
                    __instance.modData["hapyke.FoodStore/TotalCustomerResponse"] = "0";
                    __instance.modData["hapyke.FoodStore/inviteTried"] = "false";

                    if (__instance.Name.Contains("MT.Guest_"))
                    {
                        GlobalNPCList.Add(__instance.Name);
                    }


                    if (__instance.Age == 2 && !__instance.Name.Contains("MT.Guest_"))
                    {
                        GlobalKidList.Add(__instance.Name);
                    }
                }
            }

            ChairPositions.Clear();
            foreach (GameLocation location in Game1.locations)
            {
                foreach (MapSeat chair in location.mapSeats)
                {
                    var chairLocations = chair.GetSeatPositions();

                    foreach (Vector2 chairPosition in chairLocations)
                    {
                        ChairPositions.Add((location.Name, (int)chairPosition.X, (int)chairPosition.Y));
                    }
                }
            }
        }

        private void GameLoop_DayEnding(object sender, DayEndingEventArgs e)
        {
            // Wipe invitation
            try
            {
                foreach (NPC __instance in Utility.getAllCharacters())
                {
                    if ( (__instance.isVillager() && __instance.modData.ContainsKey("hapyke.FoodStore/invited") && __instance.modData.ContainsKey("hapyke.FoodStore/inviteDate")
                        && __instance.modData["hapyke.FoodStore/invited"] == "true" && Int32.Parse(__instance.modData["hapyke.FoodStore/inviteDate"]) <= (Game1.stats.DaysPlayed - 1))
                        || (__instance.isVillager() && (!__instance.modData.ContainsKey("hapyke.FoodStore/invited")) || !__instance.modData.ContainsKey("hapyke.FoodStore/inviteDate")) )
                    {
                        __instance.modData["hapyke.FoodStore/invited"] = "false";
                        __instance.modData["hapyke.FoodStore/inviteDate"] = "-99";
                    }

                    if (__instance.Name.Contains("MT.Guest_"))
                    {
                        Game1.player.friendshipData.Remove(__instance.Name);
                    }
                }
            }
            catch { }

            // Wipe visitors
            try
            {
                foreach (var name in GlobalNPCList)
                {
                    NPC npc = Game1.getCharacterFromName(name);
                    npc.Halt();
                    npc.temporaryController = null;
                    npc.controller = null;
                    Game1.warpCharacter(npc, npc.DefaultMap, npc.DefaultPosition / 64);
                }
            }
            catch { }
        }

        private static bool TryToEatFood(NPC __instance, PlacedFoodData food)
        {
            try
            {
                if (food != null && __instance.isVillager() && food.furniture != null && Vector2.Distance(food.foodTile, __instance.Tile) < Config.MaxDistanceToEat && !__instance.Name.EndsWith("_DA") && bool.Parse(__instance.modData["hapyke.FoodStore/gettingFood"]))
                {
                    if ((__instance.currentLocation is Farm || __instance.currentLocation is FarmHouse) && !Config.AllowRemoveNonFood && food.foodObject.Edibility <= 0)
                    {
                        return false;
                    }
                    using (IEnumerator<Furniture> enumerator = __instance.currentLocation.furniture.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            int taste = 8;
                            try
                            {
                                //if (food.foodObject is not WeaponProxy || food.foodObject is not null) taste = __instance.getGiftTasteForThisItem(food.foodObject);
                                if (__instance.Name == "Gus" && (taste == 0 || taste == 2)) taste = 8;
                            }
                            catch { }
                            string reply = "";
                            int salePrice = food.foodObject.sellToStorePrice();
                            int tip = 0;
                            double decorPoint = GetDecorPoint(food.foodTile, __instance.currentLocation);
                            Random rand = new Random();
                            String itemName = "";

                            if (food.foodObject.Category == -7)
                            {
                                itemName = food.foodObject.Name;
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
                                try
                                {
                                    switch (food.foodObject.Quality)
                                    {
                                        case 4:
                                            salePrice = (int)(salePrice * 1.75);
                                            break;
                                        case 2:
                                            salePrice = (int)(salePrice * 1.4);
                                            break;
                                        case 1:
                                            salePrice = (int)(salePrice * 1.15);
                                            break;
                                        default:
                                            salePrice = (int)(salePrice * 1);
                                            break;
                                    }
                                } catch { }

                            } // **** SALE and TIP block ****
                            //else if (food.foodObject is WeaponProxy)
                            //{

                            //    WeaponProxy weaponProxy = (WeaponProxy)food.foodObject;
                            //    string weaponName = weaponProxy.WeaponName;
                            //    int weaponSalePrice = weaponProxy.SalePrice;

                            //    itemName = weaponName;
                            //    reply = SHelper.Translation.Get("foodstore.weaponText");
                            //    salePrice = (int)(weaponSalePrice * 1.5);
                            //    tip = 0;
                            //}
                            else    // Non-food case
                            {
                                itemName = food.foodObject.Name;
                                tip = 0;
                                switch (food.foodObject.Quality)
                                {
                                    case 4:
                                        reply = SHelper.Translation.Get("foodstore.nonfood." + food.foodObject.Quality.ToString() + "." + rand.Next(9));
                                        salePrice = (int)(salePrice * 3);
                                        break;
                                    case 2:
                                        reply = SHelper.Translation.Get("foodstore.nonfood." + food.foodObject.Quality.ToString() + "." + rand.Next(9));
                                        salePrice = (int)(salePrice * 2.5);
                                        break;
                                    case 1:
                                        reply = SHelper.Translation.Get("foodstore.nonfood." + food.foodObject.Quality.ToString() + "." + rand.Next(9));
                                        salePrice = (int)(salePrice * 2);
                                        break;
                                    default:
                                        reply = SHelper.Translation.Get("foodstore.nonfood." + food.foodObject.Quality.ToString() + "." + rand.Next(9));
                                        salePrice = (int)(salePrice * 1.5);
                                        break;
                                }
                            }

                            //Multiply with decoration point
                            if (Config.EnableDecor) salePrice = (int)(salePrice * (1 + decorPoint));

                            //Feature dish
                            if (food.foodObject.Name == DishPrefer.dishDay) { salePrice = (int)(salePrice * 1.2); }
                            if (food.foodObject.Name == DishPrefer.dishWeek) { salePrice = (int)(salePrice * 1.1); }

                            //Config Rush hours Price
                            if (Config.RushHour && tip != 0 && ((800 < Game1.timeOfDay && Game1.timeOfDay < 930) || (1200 < Game1.timeOfDay && Game1.timeOfDay < 1300) || (1800 < Game1.timeOfDay && Game1.timeOfDay < 2000)))
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
                                    Game1.chatBox.addInfoMessage(SHelper.Translation.Get("foodstore.sold", new { foodObjName = itemName, locationString = __instance.currentLocation.Name, saleString = salePrice }));
                                    MyMessage messageToSend = new MyMessage(SHelper.Translation.Get("foodstore.sold", new { foodObjName = itemName, locationString = __instance.currentLocation.Name, saleString = salePrice }));
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
                                    Game1.chatBox.addInfoMessage(SHelper.Translation.Get("foodstore.sold", new { foodObjName = itemName, locationString = __instance.currentLocation.Name, saleString = salePrice }));
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

                                if (!Config.DisableChatAll && food.foodObject.Edibility > 0) __instance.showTextAboveHead(SHelper.Translation.Get("foodstore.visitoreat." + randomNumber), default, default, 5000);
                                else if (!Config.DisableChatAll && food.foodObject.Edibility <= 0) __instance.showTextAboveHead(SHelper.Translation.Get("foodstore.visitorpickup." + randomNumber), default, default, 5000);
                            }           //Food in farmhouse

                            Game1.player.Money += salePrice + tip;
                            __instance.modData["hapyke.FoodStore/LastFood"] = Game1.timeOfDay.ToString();
                            if (food.foodObject.Category == -7)
                            {
                                __instance.modData["hapyke.FoodStore/LastFoodTaste"] = taste.ToString();
                            }
                            else
                            {
                                __instance.modData["hapyke.FoodStore/LastFoodTaste"] = "-1";
                            }
                            __instance.modData["hapyke.FoodStore/LastFoodDecor"] = decorPoint.ToString();
                            __instance.modData["hapyke.FoodStore/gettingFood"] = "false";

                            return true;
                        }
                    }
                }
                else if (food != null && __instance.isVillager() && food.obj != null && Vector2.Distance(food.foodTile, __instance.Tile) < Config.MaxDistanceToEat && !__instance.Name.EndsWith("_DA") && bool.Parse(__instance.modData["hapyke.FoodStore/gettingFood"]))
                {
                    using (IEnumerator<Object> enumerator = __instance.currentLocation.Objects.Values.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Object currentObject = enumerator.Current;
                            Random rand = new Random();
                            int salePrice = 0;

                            if (enumerator.Current.boundingBox.Value != food.obj.boundingBox.Value)
                                continue;

                            if (currentObject is Mannequin mannequin)
                            {
                                if (mannequin.Hat.Value != null)
                                {
                                    salePrice += rand.Next(700, 1500);
                                    mannequin.Hat.Value = null;
                                }
                                if (mannequin.Shirt.Value != null)
                                {
                                    salePrice += rand.Next(900, 1800);
                                    mannequin.Shirt.Value = null;
                                }
                                if (mannequin.Pants.Value != null)
                                {
                                    salePrice += rand.Next(1000, 2000);
                                    mannequin.Pants.Value = null;
                                }
                                if (mannequin.Boots.Value != null)
                                {
                                    salePrice += (int)(mannequin.Boots.Value.salePrice() * 3.5);
                                    mannequin.Boots.Value = null;
                                }
                            }
                            Game1.chatBox.addInfoMessage(SHelper.Translation.Get("foodstore.soldclothes", new { locationString = __instance.currentLocation.Name, saleString = salePrice }));
                            MyMessage messageToSend = new MyMessage(SHelper.Translation.Get("foodstore.soldclothes", new { locationString = __instance.currentLocation.Name, saleString = salePrice }));
                            SHelper.Multiplayer.SendMessage(messageToSend, "ExampleMessageType");

                            __instance.showTextAboveHead(SHelper.Translation.Get("foodstore.soldclothesText." + rand.Next(7).ToString()), default, default, 4500);

                            Game1.player.Money += salePrice;
                            __instance.modData["hapyke.FoodStore/LastFood"] = Game1.timeOfDay.ToString();
                            __instance.modData["hapyke.FoodStore/LastFoodTaste"] = "-1";
                            __instance.modData["hapyke.FoodStore/gettingFood"] = "false";

                            return true;

                        }
                    }
                }
            }
            catch { }

            __instance.modData["hapyke.FoodStore/gettingFood"] = "false";
            return false;
        }

        private static PlacedFoodData GetClosestFood(NPC npc, GameLocation location)
        {
            List<int> categoryKeys = new List<int> { -81, -80, -79, -75, -74, -28, -27, -26, -23, -22, -21, -20, -19, -18, -17, -16, -15, -12, -8, -7, -6, -5, -4, -2};

            List<PlacedFoodData> foodList = new List<PlacedFoodData>();

            foreach (var x in location.Objects)                 // Check valid Mannequin
            {
                foreach (var obj in x.Values)
                {
                    if (obj.name.Contains("nequin") && obj is Mannequin mannequin)
                    {
                        // Access the name property of the object
                        string objectName = obj.name;

                        bool hasHat = mannequin.Hat.Value != null;
                        bool hasShirt = mannequin.Shirt.Value != null;
                        bool hasPants = mannequin.Pants.Value != null;
                        bool hasBoots = mannequin.Boots.Value != null;

                        int xLocation = (obj.boundingBox.X / 64) + (obj.boundingBox.Width / 64 / 2);
                        int yLocation = (obj.boundingBox.Y / 64) + (obj.boundingBox.Height / 64 / 2);
                        var fLocation = new Vector2(xLocation, yLocation);

                        float signRange = Config.SignRange;
                        bool hasSignInRange = x.Values.Any(otherObj => otherObj is Sign sign && Vector2.Distance(fLocation, sign.TileLocation) <= signRange);

                        Sign foundSign = location.Objects.Values
                                        .OfType<Sign>()
                                        .FirstOrDefault(sign => sign.displayItem.Value != null);

                        if ((Config.SignRange == 0 && !location.Name.Contains("Shed")) 
                            || (location.Name.Contains("Shed") && foundSign != null && foundSign.displayItem != null && foundSign.displayItem.Value.Name == "Market License")) hasSignInRange = true;

                        // Add to foodList only if there is no sign within the range
                        if (hasSignInRange && Vector2.Distance(fLocation, npc.Tile) < Config.MaxDistanceToFind && (hasHat || hasPants || hasShirt || hasBoots))
                        {
                            foodList.Add(new PlacedFoodData( obj, fLocation, obj, -1));
                        }
                    }
                }
            }

            foreach (var f in location.furniture)
            {
                if (f.heldObject.Value != null
                    && (categoryKeys.Contains(f.heldObject.Value.Category)
                        // || (f.heldObject.Value is WeaponProxy && Config.EnableSaleWeapon))
                    ))         // ***** Validate category items *****
                {
                    int xLocation = (f.boundingBox.X / 64) + (f.boundingBox.Width / 64 / 2);
                    int yLocation = (f.boundingBox.Y / 64) + (f.boundingBox.Height / 64 / 2);
                    var fLocation = new Vector2(xLocation, yLocation);

                    float signRange = Config.SignRange;
                    bool hasSignInRange = location.Objects.Values.Any(obj => obj is Sign sign && Vector2.Distance(fLocation, sign.TileLocation) <= signRange);

                    Sign foundSign = location.Objects.Values
                                    .OfType<Sign>()
                                    .FirstOrDefault(sign => sign.displayItem.Value != null);

                    if ((Config.SignRange == 0 && !location.Name.Contains("Shed"))
                        || (location.Name.Contains("Shed") && foundSign != null
                        && foundSign.displayItem != null && foundSign.displayItem.Value.Name == "Market License")
                        || (location.Name.Contains("Shed") && foundSign != null && f.heldObject.Value.Category == -7
                        && foundSign.displayItem != null && foundSign.displayItem.Value.Name == "Restaurant License")) hasSignInRange = true;

                    // Add to foodList only if there is no sign within the range
                    if (hasSignInRange&& Vector2.Distance(fLocation, npc.Tile) < Config.MaxDistanceToFind)
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

            for (int i = foodList.Count - 1; i >= 0; i--)
            {
                foodList[i].value = 0;
            }

            foodList.Sort(delegate (PlacedFoodData a, PlacedFoodData b)
            {
                var compare = b.value.CompareTo(a.value);
                if (compare != 0)
                    return compare;
                return Vector2.Distance(a.foodTile, npc.Tile).CompareTo(Vector2.Distance(b.foodTile, npc.Tile));
            });

            if (!Config.RandomPurchase)         //Return the closest
            { 
                return foodList[0]; 
            }
            else                                //Return a random item
            {
                Random random = new Random();
                return foodList[random.Next(foodList.Count)];
            }
        }

        private static bool WantsToEat(NPC npc)
        {
            if (!npc.modData.ContainsKey("hapyke.FoodStore/LastFood") || npc.modData["hapyke.FoodStore/LastFood"].Length == 0)
            {
                return true;
            }

            int lastFoodTime = int.Parse(npc.modData["hapyke.FoodStore/LastFood"]);
            int minutesSinceLastFood = GetMinutes(Game1.timeOfDay) - GetMinutes(lastFoodTime);
            try
            {
                if (npc.currentLocation != null && npc.currentLocation.Name.Contains("Shed")) return minutesSinceLastFood > Config.ShedMinuteToHungry;
            } catch { }

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

            if (decorPoint > 70) return 0.5;
            else if (decorPoint > 56) return 0.4;
            else if (decorPoint > 42) return 0.3;
            else if (decorPoint > 30) return 0.2;
            else if (decorPoint > 20) return 0.1;
            else if (decorPoint > 14) return 0.0;
            else if (decorPoint > 9) return -0.1;
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
                if (Utility.isThereAFarmerOrCharacterWithinDistance(new Vector2(thisCharacter.Tile.X, thisCharacter.Tile.Y), 13, thisCharacter.currentLocation) != null
                    && localNpcCount > 0.2) localNpcCount -= 0.0175;
            }

            foreach (NPC newCharacter in thisLocation.characters)
            {
                if (Vector2.Distance(newCharacter.Tile, thisCharacter.Tile) <= (float)15 && newCharacter.Name != thisCharacter.Name && !Config.DisableChatAll)
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
                        thisCharacter.showTextAboveHead(tasteString + ". " + decorString, default, 2, 8000);
                        if (rand.NextDouble() > localNpcCount) continue;

                        if (newCharacter.modData["hapyke.FoodStore/LastFood"] == null) newCharacter.modData["hapyke.FoodStore/LastFood"] = "0";
                        newCharacter.modData["hapyke.FoodStore/LastFood"] = (Int32.Parse(newCharacter.modData["hapyke.FoodStore/LastFood"]) - Config.MinutesToHungry + 30).ToString();
                        newCharacter.showTextAboveHead(SHelper.Translation.Get("foodstore.willVisit." + randomIndex2), default, 2, 7000);
                    }
                    else if (getChance < chanceToVisit)                                                 //Will visit, normal or negative Food , Decor
                    {
                        thisCharacter.showTextAboveHead(tasteString + ". " + decorString, default, 2, 8000);
                        if (rand.NextDouble() > localNpcCount) continue;

                        if (newCharacter.modData["hapyke.FoodStore/LastFood"] == null) newCharacter.modData["hapyke.FoodStore/LastFood"] = "0";
                        if (Config.MinutesToHungry >= 60)
                            newCharacter.modData["hapyke.FoodStore/LastFood"] = (Int32.Parse(newCharacter.modData["hapyke.FoodStore/LastFood"]) - (Config.MinutesToHungry / 2)).ToString();
                        newCharacter.showTextAboveHead(SHelper.Translation.Get("foodstore.mayVisit." + randomIndex2), default, 2, 7000);
                    }
                    else if (getChance >= chanceToVisit && lastTasteRate < 0.3 && lastDecorRate < 0)     //No visit, negative Food, negative Decor
                    {
                        thisCharacter.showTextAboveHead(tasteString + ". " + decorString, default, 2, 8000);
                        if (rand.NextDouble() > localNpcCount) continue;

                        if (newCharacter.modData["hapyke.FoodStore/LastFood"] == null) newCharacter.modData["hapyke.FoodStore/LastFood"] = "2600";
                        newCharacter.modData["hapyke.FoodStore/LastFood"] = "2600";
                        newCharacter.showTextAboveHead(SHelper.Translation.Get("foodstore.noVisit." + randomIndex2), default, 2, 7000);
                    }
                    else if (getChance >= chanceToVisit)                                                 //No visit, normal or positive Food, Decor
                    {
                        thisCharacter.showTextAboveHead(tasteString + ". " + decorString, default, 2, 8000);
                        if (rand.NextDouble() > localNpcCount) continue;

                        newCharacter.showTextAboveHead(SHelper.Translation.Get("foodstore.mayVisit." + randomIndex2), default, 2, 7000);
                    }
                    else { }    //Handle
                }
            }
        }

        public static string GetRandomDish()
        {

            List<string> resultList = new List<string>();

            foreach (var obj in Game1.objectData)
            {
                var key = obj.Key;
                var value = obj.Value;

                if (value.Category == -7)
                {
                    resultList.Add(value.Name);
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
            Random random = new Random();

            if (Game1.timeOfDay > Config.InviteComeTime || Game1.timeOfDay > Config.OpenHour)
            {
                foreach (NPC c in Utility.getAllCharacters())
                {
                    try
                    {
                        if (c.isVillager() && c.currentLocation!= null && c.currentLocation.Name == "Farm" && c.modData["hapyke.FoodStore/invited"] == "true" && c.modData["hapyke.FoodStore/inviteDate"] == (Game1.stats.DaysPlayed - 1).ToString())
                        {
                            FarmOutside.WalkAround(c.Name);
                        }

                        if (c.isVillager() && c.currentLocation != null && c.currentLocation.Name == "FarmHouse" && c.modData["hapyke.FoodStore/invited"] == "true" && c.modData["hapyke.FoodStore/inviteDate"] == (Game1.stats.DaysPlayed - 1).ToString())
                        {
                            FarmOutside.WalkAround(c.Name);
                        }

                        if (c.isVillager() && c.currentLocation != null && c.currentLocation.Name.Contains("Shed"))
                        {
                            FarmOutside.WalkAround(c.Name);
                        }
                    }
                    catch { }
                }
            }

            if (Game1.timeOfDay == 630 && Game1.player.currentLocation.Name == "Farm" && !Config.DisableKidAsk && random.NextDouble() < Config.KidAskChance
                && !(Game1.player.isRidingHorse()
                    || Game1.currentLocation == null
                    || Game1.eventUp
                    || Game1.isFestival()
                    || Game1.IsFading()
                    || Game1.activeClickableMenu != null)
                )
            {
                TodaySelectedKid.Clear();

                foreach (string kid in GlobalKidList)
                {
                    if (random.NextDouble() < 0.75)
                    {
                        int friendshipLevel = Game1.player.getFriendshipHeartLevelForNPC(kid);
                        double addKid = 0;
                        switch (friendshipLevel)
                        {
                            case int lv when lv < 2:
                                addKid = 0.1;
                                break;

                            case int lv when lv >= 2 && lv < 4:
                                addKid = 0.25;
                                break;

                            case int lv when lv >= 4 && lv < 6:
                                addKid = 0.4;
                                break;

                            case int lv when lv >= 6:
                                addKid = 0.6;
                                break;
                            default:
                                break;
                        }
                        try
                        {
                            if (random.NextDouble() < addKid && Game1.getCharacterFromName(kid).modData["hapyke.FoodStore/invited"] != "true") TodaySelectedKid.Add(kid, friendshipLevel);
                        } catch { }
                    }
                }
                if (TodaySelectedKid.Count != 0)
                {
                    string[] keysArray = new List<string>(TodaySelectedKid.Keys).ToArray();
                    string randomKey = keysArray[new Random().Next(keysArray.Length)];
                    var formattedQuestion = "";

                    if (TodaySelectedKid.Count() == 1) formattedQuestion = string.Format(SHelper.Translation.Get("foodstore.kidask", new { kidName = Game1.getCharacterFromName(randomKey).Name }), Game1.getCharacterFromName(randomKey).Name);
                    else formattedQuestion = string.Format(SHelper.Translation.Get("foodstore.groupkidask"));

                    var entryQuestion = new EntryQuestion(formattedQuestion, KidResponseList, ActionList);
                    Game1.activeClickableMenu = entryQuestion;

                    ActionList.Add(() => KidJoin(TodaySelectedKid));
                    ActionList.Add(() => Game1.DrawDialogue(new Dialogue(Game1.getCharacterFromName(randomKey), "key", SHelper.Translation.Get("foodstore.kidresponselist.boring"))));

                }
            }

            // ******* Shed visitors *******

            if (random.NextDouble() < Config.ShedVisitChance && Game1.timeOfDay <= Config.CloseHour && Game1.timeOfDay >= Config.OpenHour)
            {
                foreach (Building building in Game1.getFarm().buildings)
                {
                    if (building != null && building.GetIndoorsName() != null && building.GetIndoorsName().Contains("Shed") && CountShedVisitor(Game1.getLocationFromName(building.GetIndoorsName())) < Config.MaxShedCapacity)
                    {
                        foreach (var obj in Game1.getLocationFromName(building.GetIndoorsName()).Objects.Values)
                        {
                            if (obj is Sign sign && sign.displayItem.Value != null && (sign.displayItem.Value.Name == "Market License" || sign.displayItem.Value.Name == "Restaurant License"))
                            {
                                if (GlobalNPCList.Count == 0) return;
                                string randomNPCName = GlobalNPCList[new Random().Next(0, GlobalNPCList.Count)];

                                string[] parts = randomNPCName.Split('_');
                                string realName = "";
                                if (parts.Length >= 2)
                                {
                                    realName = parts[1];
                                }

                                var visit = Game1.getCharacterFromName(randomNPCName);

                                bool blockedNPC = false;
                                try
                                {
                                    if (Game1.getCharacterFromName(realName) != null)
                                    {
                                        blockedNPC = Game1.getCharacterFromName(realName).currentLocation.IsFarm
                                            || Game1.player.friendshipData[realName].IsMarried()
                                            || Game1.player.friendshipData[realName].IsRoommate();
                                    }

                                }
                                catch { }

                                try
                                {
                                    blockedNPC = visit == null
                                        || Game1.getCharacterFromName(randomNPCName).currentLocation.Name.Contains("Shed")
                                        || (Int32.Parse(Game1.getCharacterFromName(randomNPCName).modData["hapyke.FoodStore/timeVisitShed"])
                                                >= (Game1.timeOfDay - Config.TimeStay * 3) && (Game1.timeOfDay >= 600 + Config.TimeStay * 3));
                                } catch { }

                                if (blockedNPC) return;


                                visit.modData["hapyke.FoodStore/initLocation"] = visit.Tile.X.ToString() + "," + visit.Tile.Y.ToString() ;
                                visit.modData["hapyke.FoodStore/initMap"] = visit.currentLocation.Name;

                                List<Vector2> clearTiles = new List<Vector2>();
                                if (Config.DoorEntry && building.buildingType == "Shed")          // Alow warp at door and Shed lv0
                                {
                                    for (int x = -1; x <= 1; x++)
                                    {
                                        for (int y = -1; y <= 1; y++)
                                        {
                                            Vector2 checkLocation = new Vector2(6f, 11f) + new Vector2(x, y);
                                            if (!Game1.currentLocation.IsTileBlockedBy(checkLocation)) clearTiles.Add(checkLocation);
                                        }
                                    }

                                    if (clearTiles.Count > 0)
                                    {
                                        Vector2 randomClearTile = clearTiles[Game1.random.Next(clearTiles.Count)];

                                        if (Game1.player.currentLocation.Name == "BusStop" && Config.BusWalk && Game1.MasterPlayer.mailReceived.Contains("ccVault"))
                                        {
                                            Game1.warpCharacter(visit, "BusStop", new Point(13, 11));
                                            visit.isCharging = true;
                                            visit.addedSpeed = 1;
                                            visit.temporaryController = new PathFindController(visit, visit.currentLocation, new Point(1, 24), 3,
                                            (character, location) => Game1.warpCharacter(visit, building.GetIndoorsName(), randomClearTile));
                                        }
                                        else Game1.warpCharacter(visit, building.GetIndoorsName(), randomClearTile);

                                        visit.modData["hapyke.FoodStore/shedEntry"] = $"{randomClearTile.X},{randomClearTile.Y}";
                                    }
                                    else
                                    {
                                        if (Game1.player.currentLocation.Name == "BusStop" && Config.BusWalk && Game1.MasterPlayer.mailReceived.Contains("ccVault"))
                                        {
                                            Game1.warpCharacter(visit, "BusStop", new Point(13, 11));
                                            visit.isCharging = true;
                                            visit.addedSpeed = 1;
                                            visit.temporaryController = new PathFindController(visit, visit.currentLocation, new Point(1, 24), 3,
                                            (character, location) => Game1.warpCharacter(visit, building.GetIndoorsName(), new Vector2(6f, 11f)));
                                        }
                                        else Game1.warpCharacter(visit, building.GetIndoorsName(), new Vector2(6f, 11f));

                                        visit.modData["hapyke.FoodStore/shedEntry"] = "6,11";
                                    }
                                    clearTiles.Clear();
                                }

                                else if (Config.DoorEntry)                                                          // Alow warp at door and shed lv1
                                {
                                    for (int x = -1; x <= 1; x++)
                                    {
                                        for (int y = -1; y <= 1; y++)
                                        {
                                            Vector2 checkLocation = new Vector2(9f, 14) + new Vector2(x, y);
                                            if (!Game1.currentLocation.IsTileBlockedBy(checkLocation)) clearTiles.Add(checkLocation);

                                        }
                                    }

                                    if (clearTiles.Count > 0)
                                    {
                                        Vector2 randomClearTile = clearTiles[Game1.random.Next(clearTiles.Count)];

                                        if (Game1.player.currentLocation.Name == "BusStop" && Config.BusWalk && Game1.MasterPlayer.mailReceived.Contains("ccVault"))
                                        {
                                            Game1.warpCharacter(visit, "BusStop", new Point(13, 11));
                                            visit.isCharging = true;
                                            visit.addedSpeed = 1;
                                            visit.temporaryController = new PathFindController(visit, visit.currentLocation, new Point(1, 24), 3,
                                            (character, location) => Game1.warpCharacter(visit, building.GetIndoorsName(), randomClearTile));
                                        }
                                        else Game1.warpCharacter(visit, building.GetIndoorsName(), randomClearTile);
                                        visit.modData["hapyke.FoodStore/shedEntry"] = $"{randomClearTile.X},{randomClearTile.Y}";
                                    }
                                    else
                                    {
                                        if (Game1.player.currentLocation.Name == "BusStop" && Config.BusWalk && Game1.MasterPlayer.mailReceived.Contains("ccVault"))
                                        {
                                            Game1.warpCharacter(visit, "BusStop", new Point(13, 11));
                                            visit.isCharging = true;
                                            visit.addedSpeed = 1;
                                            visit.temporaryController = new PathFindController(visit, visit.currentLocation, new Point(1, 24), 3,
                                            (character, location) => Game1.warpCharacter(visit, building.GetIndoorsName(), new Vector2(9f, 14)));
                                        }
                                        else Game1.warpCharacter(visit, building.GetIndoorsName(), new Vector2(9f, 14));


                                        visit.modData["hapyke.FoodStore/shedEntry"] = "9,14";
                                    }
                                    clearTiles.Clear();
                                }

                                else                                                                                // Warp at Sign
                                {
                                    for (int x = -1; x <= 1; x++)
                                    {
                                        for (int y = -1; y <= 1; y++)
                                        {
                                            Vector2 checkLocation = new Vector2(obj.TileLocation.X, obj.TileLocation.Y) + new Vector2(x, y);
                                            if (!Game1.currentLocation.IsTileBlockedBy(checkLocation)) clearTiles.Add(checkLocation);
                                        }
                                    }

                                    if (clearTiles.Count > 0)
                                    {
                                        Vector2 randomClearTile = clearTiles[Game1.random.Next(clearTiles.Count)];

                                        if (Game1.player.currentLocation.Name == "BusStop" && Config.BusWalk && Game1.MasterPlayer.mailReceived.Contains("ccVault"))
                                        {
                                            Game1.warpCharacter(visit, "BusStop", new Point(13, 11));
                                            visit.isCharging = true;
                                            visit.addedSpeed = 1;
                                            visit.temporaryController = new PathFindController(visit, visit.currentLocation, new Point(1, 24), 3,
                                            (character, location) => Game1.warpCharacter(visit, building.GetIndoorsName(), randomClearTile));
                                        }
                                        else Game1.warpCharacter(visit, building.GetIndoorsName(), randomClearTile);

                                        visit.modData["hapyke.FoodStore/shedEntry"] = $"{randomClearTile.X},{randomClearTile.Y}";
                                    }
                                    else
                                    {
                                        if (Game1.player.currentLocation.Name == "BusStop" && Config.BusWalk && Game1.MasterPlayer.mailReceived.Contains("ccVault"))
                                        {
                                            Game1.warpCharacter(visit, "BusStop", new Point(13, 11));
                                            visit.isCharging = true;
                                            visit.addedSpeed = 1;
                                            visit.temporaryController = new PathFindController(visit, visit.currentLocation, new Point(1, 24), 3,
                                            (character, location) => Game1.warpCharacter(visit, building.GetIndoorsName(), new Vector2(obj.TileLocation.X, obj.TileLocation.Y)));
                                        }
                                        else Game1.warpCharacter(visit, building.GetIndoorsName(), new Vector2(obj.TileLocation.X, obj.TileLocation.Y));


                                        visit.modData["hapyke.FoodStore/shedEntry"] = (obj.TileLocation.X).ToString() + "," + (obj.TileLocation.Y).ToString();
                                    }
                                    clearTiles.Clear();
                                }

                                visit.modData["hapyke.FoodStore/timeVisitShed"] = Game1.timeOfDay.ToString();
                            }
                        }
                    }
                }
            }   // ****** end of shed visitor ******
        }
        public static int CountShedVisitor( GameLocation environment)
        {
            int count = 0;

            foreach (var validName in GlobalNPCList)
            {
                NPC c = Game1.getCharacterFromName(validName);
                if (c.currentLocation.Name == environment.Name) count += 1;         // Count NPC in Shed
                
                if (c.currentLocation.Name.Contains("BusStop")                      // Count NPC walking at BusStop
                    && c.temporaryController != null
                    && c.Tile != c.DefaultPosition / 64) count += 1;

            }
            return count;
        }

        public static bool IsOutside { get; internal set; }
        internal static List<string> FurnitureList { get; private set; } = new();
        internal static List<string> Animals { get; private set; } = new();
        internal static Dictionary<int, string> Crops { get; private set; } = new();

        // unused for weapon
        /*
        private static T XmlDeserialize<T>(string toDeserialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringReader textReader = new StringReader(toDeserialize))
            {
                return (T)xmlSerializer.Deserialize(textReader);
            }
        }

        private static string XmlSerialize<T>(T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }

        private void makePlaceholderObjects()
        {
            foreach (GameLocation location in Game1.locations)
            {
                foreach (Furniture furniture in location.furniture)
                {
                    if (furniture.heldObject.Value is WeaponProxy weaponProxy)
                    {
                        StardewValley.Object placeholder = new StardewValley.Object(furniture.heldObject.Value.TileLocation, "(O)0");
                        placeholder.Name = $"WeaponProxy:{XmlSerialize(weaponProxy.Weapon)}";
                        furniture.heldObject.Set(placeholder);
                    }
                }
            }

            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building.indoors.Value != null && building.indoors.Value.furniture != null)
                {
                    foreach (Furniture furniture in building.indoors.Value.furniture)
                    {
                        if (furniture.heldObject.Value is WeaponProxy weaponProxy)
                        {
                            StardewValley.Object placeholder = new StardewValley.Object(furniture.heldObject.Value.TileLocation, "(O)0");
                            placeholder.Name = $"WeaponProxy:{XmlSerialize(weaponProxy.Weapon)}";
                            furniture.heldObject.Set(placeholder);
                        }
                    }
                }
            }
        }

        private void restoreMeleeWeapon(Furniture furniture, string xmlString)
        {
            MeleeWeapon weapon = XmlDeserialize<MeleeWeapon>(xmlString);
            furniture.heldObject.Set(new WeaponProxy(weapon));
        }

        private void restorePlaceholderObjects()
        {
            foreach (GameLocation location in Game1.locations)
            {
                foreach (Furniture furniture in location.furniture)
                {
                    if (furniture.heldObject.Value != null && furniture.heldObject.Value.Name.Contains("Proxy:"))
                    {
                        string xmlString = furniture.heldObject.Value.Name;
                        if (xmlString.StartsWith("WeaponProxy:"))
                        {
                            try
                            {
                                restoreMeleeWeapon(furniture, xmlString.Substring("WeaponProxy:".Length));
                            }
                            catch { }
                        }
                    }
                }
            }

            foreach (Building building in Game1.getFarm().buildings)
            {
                try
                {
                    if (building.indoors != null && building.indoors.Value != null && building.indoors.Value.furniture != null)
                    {
                        foreach (Furniture furniture in building.indoors.Value.furniture)
                        {
                            if (furniture.heldObject.Value != null && furniture.heldObject.Value.Name.Contains("Proxy:"))
                            {
                                string xmlString = furniture.heldObject.Value.Name;
                                if (xmlString.StartsWith("WeaponProxy:"))
                                {
                                    try
                                    {
                                        restoreMeleeWeapon(furniture, xmlString.Substring("WeaponProxy:".Length));
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                } catch { }
            }
        }
        */
    }
}