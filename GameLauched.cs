using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Netcode;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Object = StardewValley.Object;
using MailFrameworkMod;
using System.Threading;
using System.Threading.Tasks;
using MarketTown.Framework;
using SpaceShared.APIs;
using StardewValley.Menus;
using MarketTown.Data;
using StardewValley.Objects;
using ContentPatcher;
using SpaceCore;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Shops;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;
using xTile;
using System.Reflection;
using System.Reflection.Metadata;
using System.Xml.Linq;
using SpaceCore.Spawnables;
using StardewValley.GameData.Objects;
using StardewValley.Pathfinding;
using System.Runtime.CompilerServices;
using StardewValley.Characters;
using SpaceCore.UI;
using StardewValley.GameData.FarmAnimals;
using StardewValley.Locations;

namespace MarketTown
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry
    {
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

            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            helper.Events.GameLoop.DayEnding += GameLoop_DayEnding;

            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            Helper.Events.GameLoop.OneSecondUpdateTicked += GameLoop_OneSecondUpdateTicked;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChange;

            Helper.Events.Player.InventoryChanged += Player_InventoryChanged;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;

            helper.ConsoleCommands.Add("markettown", "display", this.HandleCommand);

            helper.Events.Content.AssetRequested += this.OnAssetRequested;

            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;

            helper.Events.Player.Warped += FarmOutside.PlayerWarp;

            //
            Helper.Events.Input.ButtonPressed += this.OneButtonPressed;

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

            //-------------------------------------------------------------------------------------------------

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), "drawAtNonTileSpot", new Type[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float) }),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(DrawAtNonTileSpot_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), "loadDescription"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(LoadDescription_Postfix))
            );


            harmony.Patch(
                original: AccessTools.Method(typeof(FarmAnimal), "getSellPrice"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(GetSellPrice_Postfix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(FarmAnimal), "GetCursorPetBoundingBox"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(GetCursorPetBoundingBox_Postfix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(PurchaseAnimalsMenu), "setUpForReturnToShopMenu"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(SetUpForReturnToIslandAfterLivestockPurchase))
            );
            harmony.Patch(
               original: AccessTools.Method(typeof(PurchaseAnimalsMenu), "setUpForReturnAfterPurchasingAnimal"),
               postfix: new HarmonyMethod(typeof(ModEntry), nameof(SetUpForReturnToIslandAfterLivestockPurchase))
           );
            //harmony.Patch(original: AccessTools.Method(typeof(StardewValley.Objects.Furniture), nameof(StardewValley.Objects.Furniture.clicked)),
            //              prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(FurniturePatches.clicked_Prefix)));
            //harmony.Patch(original: AccessTools.Method(typeof(StardewValley.Objects.Furniture), nameof(StardewValley.Objects.Furniture.performObjectDropInAction)),
            //              postfix: new HarmonyMethoSystem.Reflection.AmbiguousMatchException: 'Ambiguous match in Harmony patch for Stard(typeof(FurniturePatches), nameof(FurniturePatches.performObjectDropInAction_Postfix)));

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

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            this.Monitor.Log("Loading Market Town", LogLevel.Trace);

            var sc = this.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore"); 
            sc.RegisterSerializerType(typeof(MtMannequin));
            var api = this.Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");

            api.RegisterToken(this.ModManifest, "IslandSign", () =>
            {
                if (Context.IsWorldReady && SHelper.Data.ReadSaveData<MailData>("MT.MailLog") != null && SHelper.Data.ReadSaveData<MailData>("MT.MailLog").TotalVisitorVisited != null)
                {
                    var totalVisitor = SHelper.Data.ReadSaveData<MailData>("MT.MailLog").TotalVisitorVisited;

                    GameLocation locat = Game1.getLocationFromName("Custom_MT_Island");
                    string returnValue = SHelper.Translation.Get("foodstore.islandsign", new { season = locat.GetSeason().ToString(), visitor = totalVisitor.ToString() });
                    return new[] { returnValue };
                }
                else if (Context.IsWorldReady && SHelper.Data.ReadSaveData<MailData>("MT.MailLog") == null)
                {
                    GameLocation locat = Game1.getLocationFromName("Custom_MT_Island");
                    string returnValue = SHelper.Translation.Get("foodstore.islandsign", new { season = locat.GetSeason().ToString(), visitor = "0" });
                    return new[] { returnValue };
                }
                return null;
            });
            
            api.RegisterToken(this.ModManifest, "IslandFestivalDay", () =>
            {
                if (Context.IsWorldReady)
                {
                    int dayOfWeek = Game1.dayOfMonth % 7;
                    bool festivalDay = false;

                    switch (dayOfWeek)
                    {
                        case 0:
                            festivalDay = Config.FestivalSun;
                            break;
                        case 1:
                            festivalDay = Config.FestivalMon;
                            break;
                        case 2:
                            festivalDay = Config.FestivalTue;
                            break;
                        case 3:
                            festivalDay = Config.FestivalWed;
                            break;
                        case 4:
                            festivalDay = Config.FestivalThu;
                            break;
                        case 5:
                            festivalDay = Config.FestivalFri;
                            break;
                        case 6:
                            festivalDay = Config.FestivalSat;
                            break;
                        default:
                            // Handle invalid dayOfWeek value
                            break;
                    }

                    return new[] { festivalDay.ToString().ToLower() };
                }
                return null;
            });

            api.RegisterToken(this.ModManifest, "IslandProgressLevel", () =>
            {
                if (Context.IsWorldReady && Game1.IsMasterGame && this.Helper.Data.ReadSaveData<MailData>("MT.MailLog") != null)
                {
                    MailData model = this.Helper.Data.ReadSaveData<MailData>("MT.MailLog");
                    int level = model.FestivalEarning;
                    string islandProgressLevel = "0";

                    if (30000 < level && level <= 100000) islandProgressLevel = "1";
                    else if (100000 < level && level <= 250000) islandProgressLevel = "2";
                    else if (level > 250000) islandProgressLevel = "3";

                    GameLocation locat = Game1.getLocationFromName("Custom_MT_Island");
                    var islandBrazier = locat.getObjectAtTile(22, 36);
                    if (locat == null || islandBrazier == null || islandBrazier.ItemId == null || islandBrazier.ItemId != "MT.Objects.ParadiseIslandBrazier" || !islandBrazier.IsOn)
                        return new[] { "-1" };

                    if (!Config.IslandProgress) islandProgressLevel = "3";

                    return new[] { islandProgressLevel };
                }
                return null;
            });

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
            var configMenu = Helper.ModRegistry.GetApi<MarketTown.Data.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
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

            configMenu.AddPageLink(mod: ModManifest, "island", () => SHelper.Translation.Get("foodstore.config.island"));
            configMenu.AddPageLink(mod: ModManifest, "shed", () => SHelper.Translation.Get("foodstore.config.shed"));
            configMenu.AddPageLink(mod: ModManifest, "dialogue", () => SHelper.Translation.Get("foodstore.config.dialogue"));
            configMenu.AddPageLink(mod: ModManifest, "inviteTime", () => SHelper.Translation.Get("foodstore.config.invitetime"));
            configMenu.AddPageLink(mod: ModManifest, "salePrice", () => SHelper.Translation.Get("foodstore.config.saleprice"));
            configMenu.AddPageLink(mod: ModManifest, "advance", () => SHelper.Translation.Get("foodstore.config.advance"));

            // Island setting
            configMenu.AddPage(mod: ModManifest, "island", () => SHelper.Translation.Get("foodstore.config.island"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.islandprogress"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.islandprogressText"),
                getValue: () => Config.IslandProgress,
                setValue: value => Config.IslandProgress = value
            );

            configMenu.AddNumberOption(
               mod: ModManifest,
               name: () => SHelper.Translation.Get("foodstore.config.maxislandNPC"),
               tooltip: () => SHelper.Translation.Get("foodstore.config.maxislandNPCText"),
               getValue: () => Config.ParadiseIslandNPC,
               setValue: value => Config.ParadiseIslandNPC = value,
               min: 0,
               max: 100,
               interval: 1
           );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.islandwalkaround"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.islandwalkaroundText"),
                getValue: () => Config.IslandWalkAround,
                setValue: value => Config.IslandWalkAround = value,
                min: 0.0f,
                max: 0.75f,
                interval: 0.01f
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.islandplantboost"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.islandplantboostText"),
                getValue: () => Config.IslandPlantBoost,
                setValue: value => Config.IslandPlantBoost = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.islandplantboostchance"),
                getValue: () => Config.IslandPlantBoostChance,
                setValue: value => Config.IslandPlantBoostChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.islandfestivalsellchance"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.islandfestivalsellchanceText"),
                getValue: () => Config.FestivalMaxSellChance,
                setValue: value => Config.FestivalMaxSellChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => SHelper.Translation.Get("foodstore.config.festivalschedule")
            );
            configMenu.AddNumberOption(
               mod: ModManifest,
               name: () => SHelper.Translation.Get("foodstore.config.festivaltimestart"),
               getValue: () => Config.FestivalTimeStart,
               setValue: value => Config.FestivalTimeStart = value,
               min: 630,
               max: 2400,
               interval: 10
           );
            configMenu.AddNumberOption(
               mod: ModManifest,
               name: () => SHelper.Translation.Get("foodstore.config.festivaltimeend"),
               getValue: () => Config.FestivalTimeEnd,
               setValue: value => Config.FestivalTimeEnd = value,
               min: 700,
               max: 2400,
               interval: 10
           );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.festivalmon"),
                getValue: () => Config.FestivalMon,
                setValue: value => Config.FestivalMon = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.festivaltue"),
                getValue: () => Config.FestivalTue,
                setValue: value => Config.FestivalTue = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.festivalwed"),
                getValue: () => Config.FestivalWed,
                setValue: value => Config.FestivalWed = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.festivalthu"),
                getValue: () => Config.FestivalThu,
                setValue: value => Config.FestivalThu = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.festivalfri"),
                getValue: () => Config.FestivalFri,
                setValue: value => Config.FestivalFri = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.festivalsat"),
                getValue: () => Config.FestivalSat,
                setValue: value => Config.FestivalSat = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.festivalsun"),
                getValue: () => Config.FestivalSun,
                setValue: value => Config.FestivalSun = value
            );

            // Shed setting
            configMenu.AddPage(mod: ModManifest, "shed", () => SHelper.Translation.Get("foodstore.config.shed"));

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.easylicense"),
                getValue: () => Config.EasyLicense,
                setValue: value => Config.EasyLicense = value
            );

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

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.museumpricemarkup"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.museumpricemarkupText"),
                getValue: () => Config.MuseumPriceMarkup,
                setValue: value => Config.MuseumPriceMarkup = value,
                min: 0.0f,
                max: 4.0f,
                interval: 0.025f
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
                min: 610,
                max: 2400f,
                interval: 10f
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.closehour"),
                getValue: () => Config.CloseHour,
                setValue: value => Config.CloseHour = (int)value,
                min: 610,
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
                name: () => SHelper.Translation.Get("foodstore.config.tablesit"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.tablesitText"),
                getValue: () => Config.TableSit,
                setValue: value => Config.TableSit = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
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
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.textchat"),
                getValue: () => Config.DisableTextChat,
                setValue: value => Config.DisableTextChat = value
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
                name: () => SHelper.Translation.Get("foodstore.config.multiplayermode"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.multiplayermodeText"),
                getValue: () => Config.MultiplayerMode,
                setValue: value => Config.MultiplayerMode = value
            );

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

            // Advance page
            configMenu.AddPage(mod: ModManifest, "advance", () => SHelper.Translation.Get("foodstore.config.advance"));
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.signrange"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.signrangeText"),
                getValue: () => "" + Config.SignRange,
                setValue: delegate (string value) { try { Config.SignRange = int.Parse(value, CultureInfo.InvariantCulture); } catch { } }
            );
            //configMenu.AddNumberOption(
            //    mod: ModManifest,
            //    name: () => SHelper.Translation.Get("foodstore.config.pathcheck"),
            //    tooltip: () => SHelper.Translation.Get("foodstore.config.pathcheckText"),
            //    getValue: () => Config.NPCCheckTimer,
            //    setValue: value => Config.NPCCheckTimer = (int)value,
            //    min: 0,
            //    max: 7,
            //    interval: 1
            //);
            //configMenu.AddBoolOption(
            //    mod: ModManifest,
            //    name: () => SHelper.Translation.Get("foodstore.config.advancenpcfix"),
            //    tooltip: () => SHelper.Translation.Get("foodstore.config.advancenpcfixText"),
            //    getValue: () => Config.AdvanceNpcFix,
            //    setValue: value => Config.AdvanceNpcFix = value
            //);
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => SHelper.Translation.Get("foodstore.config.advanceoutputitemid"),
                tooltip: () => SHelper.Translation.Get("foodstore.config.advanceoutputitemidText"),
                getValue: () => Config.AdvanceOutputItemId,
                setValue: value => Config.AdvanceOutputItemId = value
            );
        }       // **** Config Handle ****

        private void OneButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (Game1.activeClickableMenu is not null) return;
            if (Game1.currentLocation is null) return;
            if (!e.Button.IsActionButton()) return;
            if (Game1.player.currentLocation.Name != "Custom_MT_Island") return;

            var tile = e.Cursor.Tile;
            var player = Game1.player.Tile;

            if (tile.X > 65 && tile.X < 69 && tile.Y < 22 && tile.Y > 18 && player.X > 64 && player.X < 70 && player.Y > 17 && player.Y < 23)
                OnActionButton(e);
            else if (tile.X > 52 && tile.X < 58 && tile.Y < 19 && tile.Y > 14 && player.X > 51 && player.X < 59 && player.Y > 13 && player.Y < 20
                && Game1.timeOfDay >= Config.FestivalTimeStart && Game1.timeOfDay <= Config.FestivalTimeEnd)
            {

                GameLocation island = Game1.getLocationFromName("Custom_MT_Island");
                Layer buildings1Layer = island.map.GetLayer("Buildings1");

                Location pixelPosition = new Location(54 * Game1.tileSize, 18 * Game1.tileSize);
                if (buildings1Layer != null && buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size) != null)
                {
                    var tileProperty = buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size).TileSheet.Id;
                    if (tileProperty == "z_marnie2")
                    {
                        Game1.currentLocation.ShowAnimalShopMenu();
                    }
                }
            }
        }

        internal void OnActionButton(ButtonPressedEventArgs e)
        {
            var location = Game1.getLocationFromName("Custom_MT_Island");
            var obj = location.getObjectAtTile(999, 999);
            if (obj is Chest chest && obj is not null)
            {
                var container = new StorageContainer(chest.Items, 9,
                        3, onGrangeChange,
                        StardewValley.Utility.highlightShippableObjects);

                container.AllowExitWithHeldItem = true;
                Game1.activeClickableMenu = container;
            }
            
            SHelper.Input.Suppress(e.Button);
        }

        public bool onGrangeChange(Item i, int position, Item old, StorageContainer container, bool onRemoval)
        {
            if (!onRemoval)
            {
                if (i.Stack > 1 || i.Stack == 1 && old is { Stack: 1 } && i.canStackWith(old))
                {
                    if (old != null && old.canStackWith(i))
                    {

                        container.ItemsToGrabMenu.actualInventory[position].Stack = 1;
                        container.heldItem = old;
                        return false;
                    }

                    if (old != null)
                    {
                        StardewValley.Utility.addItemToInventory(old, position,
                            container.ItemsToGrabMenu.actualInventory);
                        container.heldItem = i;
                        return false;
                    }


                    int allButOne = i.Stack - 1;
                    Item reject = i.getOne();
                    reject.Stack = allButOne;
                    container.heldItem = reject;
                    i.Stack = 1;
                }
            }
            else if (old is { Stack: > 1 })
            {
                if (!old.Equals(i))
                {
                    return false;
                }
            }

            var itemToAdd = onRemoval && (old == null || old.Equals(i)) ? null : i;

            addItemToGrangeDisplay(itemToAdd, position, true);
            return true;
        }

        private void addItemToGrangeDisplay(Item? i, int position, bool force)
        {
            MarketShopData playerShop = TodayShopInventory.FirstOrDefault(shop => shop.Name == "PlayerShop");

            var location = Game1.getLocationFromName("Custom_MT_Island");
            var obj = location.getObjectAtTile(999, 999);

            if (obj == null || obj is not Chest chest || location == null || playerShop == null) return;

            while (chest.Items.Count < 9) chest.Items.Add(null);

            if (position < 0) return;
            if (position >= chest.Items.Count) return;
            if (chest.Items[position] != null && !force) return;

            chest.Items[position] = i;

            if (i != null) playerShop.ItemIds[position] = i.ItemId;
            else playerShop.ItemIds[position] = null; 
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Characters\\Farmer\\farmer_transparent"))
                e.LoadFromModFile<Texture2D>("FrameworkClothes/assets/farmer_transparent.png", AssetLoadPriority.Exclusive);

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            {
                e.Edit(
                    asset =>
                    {
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.bluemoonShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.clintShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.emeraldShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.emilyShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.evelynShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.fairhavenShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.fishShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.guntherShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.haleyShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.harveyShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.hatsShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.heapsShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.jodiShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.jvShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.labShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.leahShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.linusShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.marnieShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.pikaShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.primeShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.quillShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.roboshackShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.rockShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.saloonShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.serrupShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.teaShop", new ShopData());
                        asset.AsDictionary<string, ShopData>().Data.Add("MarketTown.weaponsShop", new ShopData());
                    });

                return;
            }
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
                item = new MtMannequin(mannType, mannGender, Vector2.Zero);
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

            //--------------------------------------------------------------------------------------------------------------------------------------------------
            //Game1.chatBox.addInfoMessage(e.NewMenu.ToString());
            //foreach (var x in Utility.getAllVillagers())
            //{
            //    if (x.Name.Contains("MT.Guest_"))
            //    {
            //        Game1.player.friendshipData.Remove(x.Name);
            //    }
            //}

            if (e.NewMenu is ShopMenu shop)
            {
                if (shop.ShopId == "Carpenter")
                {
                    var mm = new MtMannequin(MannequinType.Plain, MannequinGender.Male, Vector2.Zero);
                    var mf = new MtMannequin(MannequinType.Plain, MannequinGender.Female, Vector2.Zero);
                    shop.forSale.Add(mm);
                    shop.forSale.Add(mf);
                    shop.itemPriceAndStock.Add(mm, new ItemStockInformation(15000, int.MaxValue));
                    shop.itemPriceAndStock.Add(mf, new ItemStockInformation(15000, int.MaxValue));

                    bool islandStatus = Game1.getLocationFromName("Custom_MT_Island").isAlwaysActive;
                    if (!islandStatus) { Game1.addHUDMessage(new HUDMessage(SHelper.Translation.Get("foodstore.island.building"), 5000, true)); }
                }

                if (shop.ShopId.Contains("MarketTown."))
                {
                    shop.onPurchase = (item, player, amount) =>
                    {
                        foreach (var marketShop in TodayShopInventory)
                        {
                            if (marketShop.Name == shop.ShopId)
                            {
                                marketShop.ItemIds.Remove(item.QualifiedItemId);
                                break; // Stop searching after removing the item
                            }
                        }

                        return false;
                    };
                }
            }
        }

        // Check for Shop after Content Patcher patch the map, then generate shop stock and shop owner schedule
        private void SetupShop(bool init)
        {
            if (init)
            {
                List<Vector2> shopLocations = new List<Vector2>
                {
                    new Vector2(76, 21),
                    new Vector2(76, 26),
                    new Vector2(66, 16),
                    new Vector2(54, 18),
                    new Vector2(66, 26),
                    new Vector2(60, 14),
                    new Vector2(60, 19),
                    new Vector2(60, 24),
                    new Vector2(54, 23)
                };

                Dictionary<string, string> shopName = new Dictionary<string, string>
                {
                    { "z_bluemoon", "MarketTown.bluemoonShop"},
                    { "z_clint", "MarketTown.clintShop"},
                    { "z_emerald", "MarketTown.emeraldShop"},
                    { "z_emily", "MarketTown.emilyShop"},
                    { "z_evelyn", "MarketTown.evelynShop"},
                    { "z_fairhaven", "MarketTown.fairhavenShop"},
                    { "z_fish", "MarketTown.fishShop"},
                    { "z_gunther", "MarketTown.guntherShop"},
                    { "z_haley", "MarketTown.haleyShop"},
                    { "z_harvey", "MarketTown.harveyShop"},
                    { "z_hats", "MarketTown.hatsShop"},
                    { "z_heaps", "MarketTown.heapsShop"},
                    { "z_jodi", "MarketTown.jodiShop"},
                    { "z_jv", "MarketTown.jvShop"},
                    { "z_lab", "MarketTown.labShop"},
                    { "z_leah", "MarketTown.leahShop"},
                    { "z_linus", "MarketTown.linusShop"},
                    { "z_marnie", "MarketTown.marnieShop"},
                    { "z_pika", "MarketTown.pikaShop"},
                    { "z_prime", "MarketTown.primeShop"},
                    { "z_quill", "MarketTown.quillShop"},
                    { "z_roboshack", "MarketTown.roboshackShop"},
                    { "z_rock", "MarketTown.rockShop"},
                    { "z_saloon", "MarketTown.saloonShop"},
                    { "z_serrup", "MarketTown.serrupShop"},
                    { "z_tea", "MarketTown.teaShop"},
                    { "z_weapons", "MarketTown.weaponsShop"}
                };

                OpenShopTile.Clear();

                GameLocation island = Game1.getLocationFromName("Custom_MT_Island");
                Layer buildings1Layer = island.map.GetLayer("Buildings1");


                foreach (var tile in shopLocations)
                {
                    Location pixelPosition = new Location((int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize);
                    if (buildings1Layer != null && buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size) != null)
                    {
                        var tileProperty = buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size).TileSheet.Id;

                        // if is Marnie livestock shop
                        if (tileProperty == "z_marnie2") SetUpMarnieLivestockShop();

                        // if is normal shop
                        if (shopName.ContainsKey(tileProperty))
                        {
                            for (int i = (int)tile.X; i <= (int)tile.X + 2; i++)
                            {
                                for (int j = (int)tile.Y - 2; j <= (int)tile.Y; j++)
                                {
                                    OpenShopTile.Add(new Vector2(i, j), shopName[tileProperty]);
                                }
                            }

                            GenerateShop(shopName[tileProperty], tile);
                            SetupChest(shopName[tileProperty], tile);
                            Monitor.Log($"Opening {shopName[tileProperty]}", LogLevel.Trace);
                        }
                    }
                }
            }
        }

        private void SetVisitorSchedule(NPC npc)
        {
            Random random = new Random();
            var locat = Game1.getLocationFromName("Custom_MT_Island");

            if (npc == null || locat == null) return;

            int lastScheduleTime = Config.FestivalTimeStart;
            int[] weightedNumbers = { 1, 1, 1, 2, 2, 2, 2, 3, 3 };
            int[] weightedFacing = { 0, 0, 0, 0, 0, 1, 2, 3 };

            var initSche = "";
            int finalFace = 0;

            Dictionary<int, SchedulePathDescription> schedule = npc.Schedule;
            if (schedule != null)
            {
                foreach (KeyValuePair<int, SchedulePathDescription> kvp in schedule)
                {
                    SchedulePathDescription description = kvp.Value;
                    string time = kvp.Key.ToString();
                    string endLocation = description.targetLocationName;
                    string endX = description.targetTile.X.ToString();
                    string endY = description.targetTile.Y.ToString();
                    string endDirection = description.facingDirection.ToString();

                    initSche += $"{time} {endLocation} {endX} {endY} {endDirection}/";
                    lastScheduleTime = Int32.Parse(time);
                }
            }

            if (npc.modData["hapyke.FoodStore/shopOwnerToday"] != "-1,-1")
            {
                string[] components = npc.modData["hapyke.FoodStore/shopOwnerToday"].Split(',');
                var shopStandTile = new Vector2(Int32.Parse(components[0]), Int32.Parse(components[1]));

                initSche += $"{Config.FestivalTimeStart} Custom_MT_Island {shopStandTile.X} {shopStandTile.Y} 2/";
                npc.TryLoadSchedule("default", initSche);

                return;

            }

            while (lastScheduleTime < Config.FestivalTimeEnd)
            {
                MarketShopData randomShop = TodayShopInventory.Count > 0 ? TodayShopInventory[random.Next(TodayShopInventory.Count)] : null;

                Vector2 availableTile = randomShop.Tile + new Vector2(random.Next(0, 4) - 1, 1);
                if (random.NextDouble() < 0.25)
                {
                    var otherTile = FarmOutside.getRandomOpenPointInFarm(npc, npc.currentLocation, false).ToVector2();
                    if (Utility.distance(67, otherTile.X, 20, otherTile.Y) < 25)
                    {
                        availableTile = otherTile;
                        finalFace = random.Next(1, 4);
                    }
                }
                int storeScheduleTime = ConvertToHour(lastScheduleTime + weightedNumbers[random.Next(weightedNumbers.Length)] * 10);
                initSche += $"{storeScheduleTime} Custom_MT_Island {availableTile.X} {availableTile.Y} {finalFace}/ ";

                lastScheduleTime = storeScheduleTime;
            }
            npc.TryLoadSchedule("default", initSche);
        }


        private void OpenShop(IDictionary<Vector2, string> shopTile)
        {
            Game1.addHUDMessage(new HUDMessage(SHelper.Translation.Get("foodstore.festival.start")));
            var island = Game1.getLocationFromName("Custom_MT_Island");

            FestivalIsCurrent = true;
            foreach (var kvp in shopTile)
            {
                int tileX = (int)kvp.Key.X;
                int tileY = (int)kvp.Key.Y;
                string shopName = kvp.Value;

                for (int i = tileX; i <= tileX + 2; i++)
                {
                    for (int j = tileY - 2; j <= tileY; j++)
                    {
                        island.removeTileProperty(i, j, "Buildings", "Action");
                        island.setTileProperty(i, j, "Buildings", "Action", "OpenShop " + shopName);
                    }
                }
            }
        }

        private void CloseShop(bool endDay)
        {
            FestivalIsCurrent = false;
            List<Vector2> shopLocations = new List<Vector2>
            {
                new Vector2(76, 21),
                new Vector2(76, 26),
                new Vector2(66, 16),
                new Vector2(54, 18),
                new Vector2(66, 26),
                new Vector2(60, 14),
                new Vector2(60, 19),
                new Vector2(60, 24),
                new Vector2(54, 23)
            };
            try
            {

                GameLocation island = Game1.getLocationFromName("Custom_MT_Island");
            
                foreach (var tile in shopLocations)
                {
                    for (int i = (int)tile.X; i <= (int)tile.X + 2; i++)
                    {
                        for (int j = (int)tile.Y - 2; j <= (int)tile.Y; j++)
                        {
                            island.removeTileProperty(i, j, "Buildings", "Action");
                        }
                    }
                }

                if (!endDay) Game1.addHUDMessage(new HUDMessage(SHelper.Translation.Get("foodstore.festival.end")));
                if (endDay) TodayShopInventory.Clear();
            } catch { }

            foreach ( var islandVisitorName in IslandNPCList)
            {
                NPC npc = Game1.getCharacterFromName(islandVisitorName);
                if (npc != null) npc.modData["hapyke.FoodStore/shopOwnerToday"] = "-1,-1";
            }
        }

        private void SetupChest(string name, Vector2 tile)
        {
            var chestTile = new Vector2(tile.X + 3, tile.Y - 2);
            var signTile = new Vector2(tile.X + 3, tile.Y);

            var chest = new Chest(true);
            var sign = new Sign(signTile, "37");

            chest.destroyOvernight = true;
            chest.Fragility = 2;
            sign.destroyOvernight = true;
            sign.Fragility = 2;

            var randomGold = new Random().Next(1, 3);

            if (Game1.random.NextDouble() > 0.15) 
                while (chest.Items.Count < randomGold) chest.Items.Add(ItemRegistry.Create<Item>("(O)GoldCoin"));

            MarketShopData shop = TodayShopInventory.FirstOrDefault(shop => shop.Name == name);


            sign.displayItem.Value = ItemRegistry.Create<Item>(shop.ItemIds[Game1.random.Next(shop.ItemIds.Count)]);
            sign.displayType.Value = 1;

            Game1.getLocationFromName("Custom_MT_Island").setObjectAt(chestTile.X, chestTile.Y, chest);
            Game1.getLocationFromName("Custom_MT_Island").setObjectAt(signTile.X, signTile.Y, sign);
        }

        public static void NpcFestivalPurchase()
        {
            Random random = new Random();

            if (FestivalToday && Game1.timeOfDay > Config.FestivalTimeStart && Game1.timeOfDay < Config.FestivalTimeEnd)
            {
                GameLocation islandInstance = Game1.getLocationFromName("Custom_MT_Island");
                foreach (var shopData in TodayShopInventory)
                {
                    var npcList = Utility.GetNpcsWithinDistance(shopData.Tile + new Vector2(1, 0), 2, islandInstance).ToList();
                    var itemList = shopData.ItemIds;

                    if (!npcList.Any())
                    {
                        foreach (var tempNPC in npcList)
                        {
                            if (tempNPC != null && (npcList.Contains(tempNPC) && tempNPC.Sprite.currentFrame != 8
                                || tempNPC.modData["hapyke.FoodStore/festivalLastPurchase"] == Game1.timeOfDay.ToString())
                                || tempNPC.modData["hapyke.FoodStore/festivalLastPurchase"] == (Game1.timeOfDay + 10).ToString()) { npcList.Remove(tempNPC); }
                        }
                    }

                    if (itemList.Count == 0 || itemList.All(item => item == null)
                        || !npcList.Any()) continue;

                    // random NPC buy random item
                    var npcBuy = npcList[Game1.random.Next(npcList.Count)];
                    var randomItemSold = itemList[Game1.random.Next(shopData.ItemIds.Count)];

                    Color randomColor = new Color((byte)random.Next(30, 150), (byte)random.Next(30, 150), (byte)random.Next(30, 150));

                    if (randomItemSold != null && npcBuy != null && itemList.Contains(randomItemSold))
                    {
                        int index = itemList.IndexOf(randomItemSold);
                        if (index >= 0)
                        {
                            double nonPlayerChance = random.NextDouble();
                            double playerChance = random.NextDouble();
                            if (shopData.Name != "PlayerShop" && nonPlayerChance < 0.15)
                            {
                                var shopObj = StardewValley.DataLoader.Shops(Game1.content)[shopData.Name];

                                var shopItem = shopObj.Items.FirstOrDefault(item => item?.ItemId == randomItemSold);
                                if (shopItem != null)
                                {
                                    itemList[index] = null;

                                    npcBuy.showTextAboveHead(GetSoldMessage(randomItemSold, shopData.Name), randomColor, 2, 4000, 1000);
                                    shopObj.Items.Remove(shopItem);
                                    npcBuy.modData["hapyke.FoodStore/festivalLastPurchase"] = Game1.timeOfDay.ToString();

                                    break;
                                }
                            }
                            else if (shopData.Name == "PlayerShop" && playerChance < Config.FestivalMaxSellChance / IslandProgress())
                            {
                                var obj = islandInstance.getObjectAtTile(999, 999);
                                if (obj != null && obj is Chest chest && chest.Items.Any())
                                {
                                    var itemObj = chest.Items.FirstOrDefault(item => item?.ItemId == randomItemSold);
                                    var itemIndex = chest.Items.IndexOf(itemObj);

                                    itemList[index] = null;
                                    chest.Items[itemIndex] = null;

                                    npcBuy.showTextAboveHead(GetSoldMessage(randomItemSold, "PlayerShop"), randomColor, 2, 4000, 1000);
                                    npcBuy.modData["hapyke.FoodStore/festivalLastPurchase"] = Game1.timeOfDay.ToString();

                                    var price = itemObj.sellToStorePrice() * 2;
                                    AddToPlayerFunds(price);

                                    break;
                                }
                            }
                            else if (nonPlayerChance > 0.6 && playerChance > 0.6)
                            {
                                npcBuy.doEmote(10);
                                break;
                            }
                        }
                    }
                }
            }
        }
        private static void AddToPlayerFunds(int salePrice)
        {
            var farmers = Game1.getAllFarmers().Where(f => f.isActive()).ToList();
            var multiplayer = farmers.Count > 1;

            if (Game1.player.team.useSeparateWallets.Value && multiplayer)
            {
                try
                {
                    foreach (var farmer in farmers)
                    {
                        Game1.player.team.AddIndividualMoney(farmer, (int)salePrice / farmers.Count);
                    }
                    
                }
                catch { Game1.player.Money += salePrice; }
            }
            else
            {
                Game1.player.Money += salePrice;
            }
        }

        public class BuildingObjectPair
        {
            public Building Building { get; set; }
            public Object Object { get; set; }
            public string buildingType { get; set; }
            public int ticketValue { get; set; }

            public BuildingObjectPair(Building building, Object obj, string buildingType, int ticketValue)
            {
                Building = building;
                Object = obj;
                this.buildingType = buildingType;
                this.ticketValue = ticketValue;
            }
        }

        public class IslandBuildingProperties
        {
            public string buildingLocation { get; set; }
            public Vector2 indoorDoor { get; set; }
            public Vector2 outdoorDoor { get; set; }

            public IslandBuildingProperties(string buildingLocation, Vector2 indoorDoor, Vector2 outdoorDoor)
            {
                this.buildingLocation = buildingLocation;
                this.indoorDoor = indoorDoor;
                this.outdoorDoor = outdoorDoor;
            }
        }

        public class MarketShopData
        {
            public string Name { get; set; }
            public Vector2 Tile { get; set; }
            public List<string> ItemIds { get; set; }

            public MarketShopData(string name, Vector2 tile, List<string> itemIds)
            {
                Name = name;
                Tile = tile;
                ItemIds = itemIds;
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

        public static string GetSoldMessage(string itemId, string shop)
        {
            try
            {
                Random random = new Random();

                string result = Game1.player.farmName + " Shop";

                if (shop != "PlayerShop")
                {
                    string[] parts = shop.Split('.');
                    string inputString = parts[parts.Length - 1];

                    string lastPart = inputString.Substring(inputString.Length - 4);

                    string firstPart = inputString.Substring(0, inputString.Length - 4);

                    if (firstPart.Length > 0)
                    {
                        firstPart = char.ToUpper(firstPart[0]) + firstPart.Substring(1);
                    }

                    result = firstPart + " " + lastPart;
                }
                var displayName = ItemRegistry.GetData(itemId).DisplayName;

                var stringValue = SHelper.Translation.Get("foodstore.festival.sold." + random.Next(20), new { itemName = displayName, shopName = result });

                return stringValue;
            } catch { return "Awesome!"; }
        }

        public void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == this.ModManifest.UniqueID && e.Type == "ExampleMessageType" && !Config.DisableChatAll)
            {
                MyMessage message = e.ReadAs<MyMessage>();
                Game1.chatBox.addInfoMessage(message.MessageContent);
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
                foreach (var building in Game1.getFarm().buildings)
                {
                    if (npc.currentLocation != null && building != null && building.GetIndoorsName() != null && building.GetIndoorsName().Contains(npc.currentLocation.Name)) return minutesSinceLastFood > Config.ShedMinuteToHungry;
                    break;
                }
            }
            catch { }

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
                            decorPoint += 0.75;
                            if (obj is Furniture furniture && furniture.heldObject.Value != null) decorPoint += 0.25; 
                        }
                    }
                }
            }

            bool foundWater = false;
            //Water nearby check
            for (var y = foodLoc.Y - 7; y < foodLoc.Y + 7; y++)
            {
                for (var x = foodLoc.X - 7; x < foodLoc.X + 7; x++)
                {
                    if (Game1.player.currentLocation.isWaterTile((int)x, (int)y))
                    {
                        decorPoint += 5;
                        foundWater = true;
                        break;
                    }
                }
                if (foundWater) break;
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

            if (gameLocation.Name.Contains("Custom_MT_Island") 
                || gameLocation.GetParentLocation() != null && gameLocation.GetParentLocation().NameOrUniqueName == "Custom_MT_Island" )
                decorPoint += 8;

            if (decorPoint > 58) return 0.5;
            else if (decorPoint > 46) return 0.4;
            else if (decorPoint > 36) return 0.3;
            else if (decorPoint > 27) return 0.2;
            else if (decorPoint > 19) return 0.1;
            else if (decorPoint > 13) return 0.0;
            else if (decorPoint > 8) return -0.1;
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
                if (rand.NextDouble() < 0.66) continue;

                if (Vector2.Distance(newCharacter.Tile, thisCharacter.Tile) <= (float)10 && newCharacter.Name != thisCharacter.Name && !Config.DisableChatAll)
                {
                    Random random = new Random();
                    int randomIndex = random.Next(5);
                    int randomIndex2 = random.Next(3);

                    //taste string
                    string tastestring = "string";
                    if (lastTasteRate > 0.3) tastestring = SHelper.Translation.Get("foodstore.positiveTasteint." + randomIndex);
                    else if (lastTasteRate == 0.3) tastestring = SHelper.Translation.Get("foodstore.normalTasteint." + randomIndex);
                    else if (lastTasteRate < 0.3) tastestring = SHelper.Translation.Get("foodstore.negativeTasteint." + randomIndex);

                    //decor string
                    string decorstring = "string";
                    if (lastDecorRate > 0) decorstring = SHelper.Translation.Get("foodstore.positiveDecorint." + randomIndex);
                    else if (lastDecorRate == 0) decorstring = SHelper.Translation.Get("foodstore.normalDecorint." + randomIndex);
                    else if (lastDecorRate < 0) decorstring = SHelper.Translation.Get("foodstore.negativeDecorint." + randomIndex);

                    //do the work
                    if (getChance < chanceToVisit && lastTasteRate > 0.3 && lastDecorRate > 0)          //Will visit, positive Food, positive Decor
                    {
                        NPCShowTextAboveHead(thisCharacter, tastestring + ". " + decorstring);
                        if (rand.NextDouble() > localNpcCount) continue;

                        if (newCharacter.modData["hapyke.FoodStore/LastFood"] == null) newCharacter.modData["hapyke.FoodStore/LastFood"] = "0";
                        newCharacter.modData["hapyke.FoodStore/LastFood"] = (Int32.Parse(newCharacter.modData["hapyke.FoodStore/LastFood"]) - Config.MinutesToHungry + 30).ToString();
                        NPCShowTextAboveHead(newCharacter, SHelper.Translation.Get("foodstore.willVisit." + randomIndex2));
                    }
                    else if (getChance < chanceToVisit)                                                 //Will visit, normal or negative Food , Decor
                    {
                        NPCShowTextAboveHead(thisCharacter, tastestring + ". " + decorstring);
                        if (rand.NextDouble() > localNpcCount) continue;

                        if (newCharacter.modData["hapyke.FoodStore/LastFood"] == null) newCharacter.modData["hapyke.FoodStore/LastFood"] = "0";
                        if (Config.MinutesToHungry >= 60)
                            newCharacter.modData["hapyke.FoodStore/LastFood"] = (Int32.Parse(newCharacter.modData["hapyke.FoodStore/LastFood"]) - (Config.MinutesToHungry / 2)).ToString();
                        NPCShowTextAboveHead(newCharacter, SHelper.Translation.Get("foodstore.mayVisit." + randomIndex2));
                    }
                    else if (getChance >= chanceToVisit && lastTasteRate < 0.3 && lastDecorRate < 0)     //No visit, negative Food, negative Decor
                    {
                        NPCShowTextAboveHead(thisCharacter, tastestring + ". " + decorstring);
                        if (rand.NextDouble() > localNpcCount) continue;

                        if (newCharacter.modData["hapyke.FoodStore/LastFood"] == null) newCharacter.modData["hapyke.FoodStore/LastFood"] = "2600";
                        newCharacter.modData["hapyke.FoodStore/LastFood"] = "2600";
                        NPCShowTextAboveHead(newCharacter, SHelper.Translation.Get("foodstore.noVisit." + randomIndex2));
                    }
                    else if (getChance >= chanceToVisit)                                                 //No visit, normal or positive Food, Decor
                    {
                        NPCShowTextAboveHead(thisCharacter, tastestring + ". " + decorstring);
                        if (rand.NextDouble() > localNpcCount) continue;

                        NPCShowTextAboveHead(newCharacter, SHelper.Translation.Get("foodstore.mayVisit." + randomIndex2));
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

        public static int CountShedVisitor(GameLocation environment)
        {
            if (environment == null) return 99999;

            return environment.characters.ToList().Count + Game1.getLocationFromName("BusStop").characters.ToList().Count;
        }

        public static void UpdateCount(int category)
        {
            Dictionary<int, Action> categoryActions = new Dictionary<int, Action>
            {
                {-81, () => { TodayForageSold ++; } },
                {-80, () => { TodayFlowerSold ++; } },
                {-79, () => { TodayFruitSold ++; } },
                {-75, () => { TodayVegetableSold ++; } },
                {-74, () => { TodaySeedSold ++; } },
                {-28, () => { TodayMonsterLootSold ++; } },
                {-27, () => { TodaySyrupSold ++; } },
                {-26, () => { TodayArtisanGoodSold ++; } },
                {-18, () => { TodayAnimalProductSold ++; } },
                {-17, () => { TodayAnimalProductSold ++; } },
                {-6, () => { TodayAnimalProductSold ++; } },
                {-5, () => { TodayAnimalProductSold ++; } },
                {-15, () => { TodayResourceMetalSold ++; } },
                {-12, () => { TodayMineralSold ++; } },
                {-8,  () => { TodayCraftingSold ++; } },
                {-7,  () => { TodayCookingSold ++; } },
                {-4,  () => { TodayFishSold ++; } },
                {-2,  () => { TodayGemSold ++; } }
            };

            if (categoryActions.ContainsKey(category))
            {
                categoryActions[category].Invoke();
            }
        }

        private static void NPCShowTextAboveHead(NPC npc, string message)
        {
            Task.Run(async delegate
            {
                try
                {
                    int charCount = 0;
                    IEnumerable<string> splits = from w in message.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                 group w by (charCount += w.Length + 1) / 60 into g // Adjust the number to split longer chunks
                                                 select string.Join(" ", g);

                    foreach (string split in splits)
                    {
                        float minDisplayTime = 2000f;
                        float maxDisplayTime = 3500f;
                        float percentOfMax = (float)split.Length / (float)60;
                        int duration = (int)(minDisplayTime + (maxDisplayTime - minDisplayTime) * percentOfMax);
                        npc.showTextAboveHead(split, default, default, duration, default);
                        Thread.Sleep(duration);
                    }
                }
                catch (Exception ex) { }
            });
        }

        internal static void Player_InventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            foreach (Item item in e.Added)
            {
                if (item.Name == "Museum License")
                {
                    var letterTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("Assets/LtBG.png");
                    MailRepository.SaveLetter(
                        new Letter(
                            "MT.ReceiveMuseumLicense",
                            SHelper.Translation.Get("foodstore.letter.receivemuseumlicense"),
                            (Letter l) => !Game1.player.mailReceived.Contains("MT.ReceiveMuseumLicense"),
                            delegate (Letter l)
                            {
                                ((NetHashSet<string>)(object)Game1.player.mailReceived).Add(l.Id);
                            })
                        {
                            Title = "About Museum License",
                            LetterTexture = letterTexture
                        }
                    );
                }
                if (item.Name == "Restaurant License")
                {
                    var letterTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("Assets/LtBG.png");
                    MailRepository.SaveLetter(
                        new Letter(
                            "MT.ReceiveRestaurantLicense",
                            SHelper.Translation.Get("foodstore.letter.receiverestaurantlicense"),
                            (Letter l) => !Game1.player.mailReceived.Contains("MT.ReceiveRestaurantLicense"),
                            delegate (Letter l)
                            {
                                ((NetHashSet<string>)(object)Game1.player.mailReceived).Add(l.Id);
                            })
                        {
                            Title = "About Restaurant License",
                            LetterTexture = letterTexture
                        }
                    );
                }
                if (item.Name == "Market License")
                {
                    var letterTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("Assets/LtBG.png");
                    MailRepository.SaveLetter(
                        new Letter(
                            "MT.ReceiveMarketTownLicense",
                            SHelper.Translation.Get("foodstore.letter.receivemarkettownlicense"),
                            (Letter l) => !Game1.player.mailReceived.Contains("MT.ReceiveMarketTownLicense"),
                            delegate (Letter l)
                            {
                                ((NetHashSet<string>)(object)Game1.player.mailReceived).Add(l.Id);
                            })
                        {
                            Title = "About Market Town License",
                            LetterTexture = letterTexture
                        }
                    );
                }
            }
        }

        public class DishPrefer
        {
            // Declare a public static variable
            public static string dishDay = "Farmer's Lunch";
            public static string dishWeek = "Farmer's Lunch";
        }

        private static void KidJoin(Dictionary<string, int> todaySelectedKid)
        {
            foreach (var kvp in todaySelectedKid)
            {
                NPC __instance = Game1.getCharacterFromName(kvp.Key);
                __instance.modData["hapyke.FoodStore/invited"] = "true";
                __instance.modData["hapyke.FoodStore/inviteDate"] = (Game1.stats.DaysPlayed - 1).ToString();
                Game1.DrawDialogue(new Dialogue(__instance, "key", SHelper.Translation.Get("foodstore.kidresponselist.yay")));
            }
        }

        // Get Island Progress point 1 - 4. Lower is better
        private static double IslandProgress()
        {
            MailData model = null;
            if (Game1.IsMasterGame) model = SHelper.Data.ReadSaveData<MailData>("MT.MailLog");

            if (!Config.IslandProgress) return 1;
            if (model == null) return 4;

            int totalCustomerNote = model.TotalCustomerNote;
            int totalCustomerNoteYes = model.TotalCustomerNoteYes;
            int totalCustomerNoteNo = model.TotalCustomerNoteNo;

            float totalNoteBase = 3 - (totalCustomerNote / 100);
            if (totalNoteBase < 1) totalNoteBase = 1;

            float yesNoBase = ( totalCustomerNoteYes + totalCustomerNoteNo + 3 ) / ( totalCustomerNoteYes + 1 );
            float islandProgressValue = totalNoteBase * yesNoBase;


            if (islandProgressValue > 4) return 4;
            if (islandProgressValue < 1) return 1;
            return islandProgressValue;
        }


        // DEBUG: Get unique ID of all item
        private void OutputItemId()
        {
            Dictionary<string, List<string>> itemsByCategory = new Dictionary<string, List<string>>();

            // Iterate over each item in Game1.objectData
            foreach (var kvp in Game1.objectData)
            {
                var realItem = ItemRegistry.Create<Item>("(O)" + kvp.Key);
                //SMonitor.Log(realItem.Category+ "_______" + realItem.QualifiedItemId + "________" + realItem.Name  , LogLevel.Warn);

                var salePrice = realItem.salePrice().ToString() + "===" + realItem.sellToStorePrice().ToString();

                // Add item to the corresponding category list
                if (!itemsByCategory.ContainsKey(realItem.Category.ToString()))
                {
                    itemsByCategory[realItem.Category.ToString()] = new List<string>();
                }
                itemsByCategory[realItem.Category.ToString()].Add(realItem.QualifiedItemId + "________" + realItem.Name + "__" + salePrice.ToString() + "---GAME1-OBJECTDATA");
            }

            foreach (var kvp in Game1.weaponData)
            {
                var realItem = ItemRegistry.Create<Item>("(W)" + kvp.Key);
                //SMonitor.Log(realItem.Category + "_______" + realItem.QualifiedItemId + "________" + realItem.Name, LogLevel.Warn);

                var salePrice = realItem.salePrice().ToString() + "===" + realItem.sellToStorePrice().ToString();

                // Add item to the corresponding category list
                if (!itemsByCategory.ContainsKey(realItem.Category.ToString()))
                {
                    itemsByCategory[realItem.Category.ToString()] = new List<string>();
                }
                itemsByCategory[realItem.Category.ToString()].Add(realItem.QualifiedItemId + "________" + realItem.Name + "__" + salePrice.ToString() + "---WEAPON");
            }

            foreach (var kvp in Game1.shirtData)
            {
                var realItem = ItemRegistry.Create<Item>("(S)" + kvp.Key);
                //SMonitor.Log(realItem.Category + "_______" + realItem.QualifiedItemId + "________" + realItem.Name, LogLevel.Warn);

                var salePrice = realItem.salePrice().ToString() + "===" + realItem.sellToStorePrice().ToString();

                // Add item to the corresponding category list
                if (!itemsByCategory.ContainsKey(realItem.Category.ToString()))
                {
                    itemsByCategory[realItem.Category.ToString()] = new List<string>();
                }
                itemsByCategory[realItem.Category.ToString()].Add(realItem.QualifiedItemId + "________" + realItem.Name + "__" + salePrice.ToString() + "---SHIRT");
            }

            foreach (var kvp in Game1.pantsData)
            {
                var realItem = ItemRegistry.Create<Item>("(P)" + kvp.Key);
                //SMonitor.Log(realItem.Category + "_______" + realItem.QualifiedItemId + "________" + realItem.Name, LogLevel.Warn);

                var salePrice = realItem.salePrice().ToString() + "===" + realItem.sellToStorePrice().ToString();

                // Add item to the corresponding category list
                if (!itemsByCategory.ContainsKey(realItem.Category.ToString()))
                {
                    itemsByCategory[realItem.Category.ToString()] = new List<string>();
                }
                itemsByCategory[realItem.Category.ToString()].Add(realItem.QualifiedItemId + "________" + realItem.Name + "__" + salePrice.ToString() + "---PANT");
            }
            foreach (var kvp in StardewValley.DataLoader.Furniture(Game1.content))
            {
                var realItem = ItemRegistry.Create<Item>("(F)" + kvp.Key);
                //SMonitor.Log(realItem.Category + "_______" + realItem.QualifiedItemId + "________" + realItem.Name, LogLevel.Warn);

                var salePrice = realItem.salePrice().ToString() + "===" + realItem.sellToStorePrice().ToString();

                // Add item to the corresponding category list
                if (!itemsByCategory.ContainsKey(realItem.Category.ToString()))
                {
                    itemsByCategory[realItem.Category.ToString()] = new List<string>();
                }
                itemsByCategory[realItem.Category.ToString()].Add(realItem.QualifiedItemId + "________" + realItem.Name + "__" + salePrice.ToString() + "---FURNITURE");
            }
            foreach (var kvp in StardewValley.DataLoader.Hats(Game1.content))
            {
                var realItem = ItemRegistry.Create<Item>("(H)" + kvp.Key);
                //SMonitor.Log(realItem.Category + "_______" + realItem.QualifiedItemId + "________" + realItem.Name, LogLevel.Warn);

                var salePrice = realItem.salePrice().ToString() + "===" + realItem.sellToStorePrice().ToString();

                // Add item to the corresponding category list
                if (!itemsByCategory.ContainsKey(realItem.Category.ToString()))
                {
                    itemsByCategory[realItem.Category.ToString()] = new List<string>();
                }
                itemsByCategory[realItem.Category.ToString()].Add(realItem.QualifiedItemId + "________" + realItem.Name + "__" + salePrice.ToString() + "---HATS");
            }
            foreach (var kvp in StardewValley.DataLoader.BigCraftables(Game1.content))
            {
                var realItem = ItemRegistry.Create<Item>("(BC)" + kvp.Key);
                //SMonitor.Log(realItem.Category + "_______" + realItem.QualifiedItemId + "________" + realItem.Name, LogLevel.Warn);

                var salePrice = realItem.salePrice().ToString() + "===" + realItem.sellToStorePrice().ToString();

                // Add item to the corresponding category list
                if (!itemsByCategory.ContainsKey(realItem.Category.ToString()))
                {
                    itemsByCategory[realItem.Category.ToString()] = new List<string>();
                }
                itemsByCategory[realItem.Category.ToString()].Add(realItem.QualifiedItemId + "________" + realItem.Name + "__" + salePrice.ToString() + "---BIGCRAFTABLE");
            }
            foreach (var kvp in StardewValley.DataLoader.Boots(Game1.content))
            {
                var realItem = ItemRegistry.Create<Item>("(B)" + kvp.Key);
                //SMonitor.Log(realItem.Category + "_______" + realItem.QualifiedItemId + "________" + realItem.Name, LogLevel.Warn);

                var salePrice = realItem.salePrice().ToString() + "===" + realItem.sellToStorePrice().ToString();

                // Add item to the corresponding category list
                if (!itemsByCategory.ContainsKey(realItem.Category.ToString()))
                {
                    itemsByCategory[realItem.Category.ToString()] = new List<string>();
                }
                itemsByCategory[realItem.Category.ToString()].Add(realItem.QualifiedItemId + "________" + realItem.Name + "__" + salePrice.ToString() + "---BOOTS");
            }
            foreach (var kvp in StardewValley.DataLoader.Tools(Game1.content))
            {
                var realItem = ItemRegistry.Create<Item>("(T)" + kvp.Key);
                //SMonitor.Log(realItem.Category + "_______" + realItem.QualifiedItemId + "________" + realItem.Name, LogLevel.Warn);

                var salePrice = realItem.salePrice().ToString() + "===" + realItem.sellToStorePrice().ToString();

                // Add item to the corresponding category list
                if (!itemsByCategory.ContainsKey(realItem.Category.ToString()))
                {
                    itemsByCategory[realItem.Category.ToString()] = new List<string>();
                }
                itemsByCategory[realItem.Category.ToString()].Add(realItem.QualifiedItemId + "________" + realItem.Name + "__" + salePrice.ToString() + "---TOOLS");
            }


            // Write items by category to a JSON file
            string jsonFilePath = "_items_by_category.json";
            System.IO.File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(itemsByCategory, Formatting.Indented));

            Config.AdvanceOutputItemId = false;
        }

        // Convert minute to HHMM
        public static int ConvertToHour(int number)
        {
            string numberString = number.ToString();

            string hourString = numberString.Substring(0, numberString.Length - 2);
            string minuteString = numberString.Substring(numberString.Length - 2);

            int hour = int.Parse(hourString);
            int minute = int.Parse(minuteString);

            if (minute >= 60)
            {
                hour += minute / 60;
                minute %= 60;
            }

            string formattedHour = hour.ToString();
            string formattedMinute = minute.ToString("00");

            int result = Int32.Parse(formattedHour + formattedMinute);

            return result;
        }

        // Convert HHMM to minute
        public static int ConvertToMinute(int number)
        {
            int hours = number / 100;
            int minutes = number % 100;

            return hours * 60 + minutes;
        }

        public static void ResetErrorNpc (NPC __instance)
        {
            __instance.Halt();
            __instance.ClearSchedule();
            __instance.DirectionsToNewLocation = null;
            __instance.queuedSchedulePaths.Clear();
            __instance.previousEndPoint = __instance.TilePoint;
            __instance.temporaryController = null;
            __instance.controller = null;
            __instance.Halt();
        }

        public static void EndOfDaySave()
        {
            int totalVisitorVisited = 0;
            int totalMoney = 0;

            int totalCustomerNote = 0;
            int totalCustomerNoteYes = 0;
            int totalCustomerNoteNo = 0;
            int totalCustomerNoteNone = 0;

            int weeklyForageSold = 0;
            int weeklyFlowerSold = 0;
            int weeklyFruitSold = 0;
            int weeklyVegetableSold = 0;
            int weeklySeedSold = 0;
            int weeklyMonsterLootSold = 0;
            int weeklySyrupSold = 0;
            int weeklyArtisanGoodSold = 0;
            int weeklyAnimalProductSold = 0;
            int weeklyResourceMetalSold = 0;
            int weeklyMineralSold = 0;
            int weeklyCraftingSold = 0;
            int weeklyCookingSold = 0;
            int weeklyFishSold = 0;
            int weeklyGemSold = 0;

            int totalForageSold = 0;
            int totalFlowerSold = 0;
            int totalFruitSold = 0;
            int totalVegetableSold = 0;
            int totalSeedSold = 0;
            int totalMonsterLootSold = 0;
            int totalSyrupSold = 0;
            int totalArtisanGoodSold = 0;
            int totalAnimalProductSold = 0;
            int totalResourceMetalSold = 0;
            int totalMineralSold = 0;
            int totalCraftingSold = 0;
            int totalCookingSold = 0;
            int totalFishSold = 0;
            int totalGemSold = 0;

            MailData model = null;

            if (Game1.IsMasterGame)
            {
                model = SHelper.Data.ReadSaveData<MailData>("MT.MailLog");
            }

            if (model != null)
            {
                totalVisitorVisited = model.TotalVisitorVisited;

                totalMoney = model.TotalEarning;

                totalCustomerNote = model.TotalCustomerNote;
                totalCustomerNoteYes = model.TotalCustomerNoteYes;
                totalCustomerNoteNo = model.TotalCustomerNoteNo;

                weeklyForageSold = model.ForageSold;
                weeklyFlowerSold = model.FlowerSold;
                weeklyFruitSold = model.FruitSold;
                weeklyVegetableSold = model.VegetableSold;
                weeklySeedSold = model.SeedSold;
                weeklyMonsterLootSold = model.MonsterLootSold;
                weeklySyrupSold = model.SyrupSold;
                weeklyArtisanGoodSold = model.ArtisanGoodSold;
                weeklyAnimalProductSold = model.AnimalProductSold;
                weeklyResourceMetalSold = model.ResourceMetalSold;
                weeklyMineralSold = model.MineralSold;
                weeklyCraftingSold = model.CraftingSold;
                weeklyCookingSold = model.CookingSold;
                weeklyFishSold = model.FishSold;
                weeklyGemSold = model.GemSold;

                totalForageSold = model.TotalForageSold;
                totalFlowerSold = model.TotalFlowerSold;
                totalFruitSold = model.TotalFruitSold;
                totalVegetableSold = model.TotalVegetableSold;
                totalSeedSold = model.TotalSeedSold;
                totalMonsterLootSold = model.TotalMonsterLootSold;
                totalSyrupSold = model.TotalSyrupSold;
                totalArtisanGoodSold = model.TotalArtisanGoodSold;
                totalAnimalProductSold = model.TotalAnimalProductSold;
                totalResourceMetalSold = model.TotalResourceMetalSold;
                totalMineralSold = model.TotalMineralSold;
                totalCraftingSold = model.TotalCraftingSold;
                totalCookingSold = model.TotalCookingSold;
                totalFishSold = model.TotalFishSold;
                totalGemSold = model.TotalGemSold;

                if (Game1.dayOfMonth == 1 || Game1.dayOfMonth == 8 || Game1.dayOfMonth == 15 || Game1.dayOfMonth == 22)
                {
                    weeklyForageSold = 0;
                    weeklyFlowerSold = 0;
                    weeklyFruitSold = 0;
                    weeklyVegetableSold = 0;
                    weeklySeedSold = 0;
                    weeklyMonsterLootSold = 0;
                    weeklySyrupSold = 0;
                    weeklyArtisanGoodSold = 0;
                    weeklyAnimalProductSold = 0;
                    weeklyResourceMetalSold = 0;
                    weeklyMineralSold = 0;
                    weeklyCraftingSold = 0;
                    weeklyCookingSold = 0;
                    weeklyFishSold = 0;
                    weeklyGemSold = 0;
                }
            }

            MailData dataToSave = new MailData
            {
                TotalVisitorVisited = totalVisitorVisited + TodayVisitorVisited,
                TotalEarning = totalMoney + TodayMoney,
                SellMoney = TodayMoney,
                SellList = TodaySell,

                TodayCustomerInteraction = TodayCustomerInteraction,

                TodayMuseumVisitor = TodayMuseumVisitor,
                TodayMuseumEarning = TodayMuseumEarning,

                TotalCustomerNote = TodayCustomerNoteYes + TodayCustomerNoteNo + TodayCustomerNoteNone + totalCustomerNote,
                TotalCustomerNoteYes = TodayCustomerNoteYes + totalCustomerNoteYes,
                TotalCustomerNoteNo = TodayCustomerNoteNo + totalCustomerNoteNo,


                ForageSold = TodayForageSold + weeklyForageSold,
                FlowerSold = TodayFlowerSold + weeklyFlowerSold,
                FruitSold = TodayFruitSold + weeklyFruitSold,
                VegetableSold = TodayVegetableSold + weeklyVegetableSold,
                SeedSold = TodaySeedSold + weeklySeedSold,
                MonsterLootSold = TodayMonsterLootSold + weeklyMonsterLootSold,
                SyrupSold = TodaySyrupSold + weeklySyrupSold,
                ArtisanGoodSold = TodayArtisanGoodSold + weeklyArtisanGoodSold,
                AnimalProductSold = TodayAnimalProductSold + weeklyAnimalProductSold,
                ResourceMetalSold = TodayResourceMetalSold + weeklyResourceMetalSold,
                MineralSold = TodayMineralSold + weeklyMineralSold,
                CraftingSold = TodayCraftingSold + weeklyCraftingSold,
                CookingSold = TodayCookingSold + weeklyCookingSold,
                FishSold = TodayFishSold + weeklyFishSold,
                GemSold = TodayGemSold + weeklyGemSold,

                TotalForageSold = TodayForageSold + totalForageSold,
                TotalFlowerSold = TodayFlowerSold + totalFlowerSold,
                TotalFruitSold = TodayFruitSold + totalFruitSold,
                TotalVegetableSold = TodayVegetableSold + totalVegetableSold,
                TotalSeedSold = TodaySeedSold + totalSeedSold,
                TotalMonsterLootSold = TodayMonsterLootSold + totalMonsterLootSold,
                TotalSyrupSold = TodaySyrupSold + totalSyrupSold,
                TotalArtisanGoodSold = TodayArtisanGoodSold + totalArtisanGoodSold,
                TotalAnimalProductSold = TodayAnimalProductSold + totalAnimalProductSold,
                TotalResourceMetalSold = TodayResourceMetalSold + totalResourceMetalSold,
                TotalMineralSold = TodayMineralSold + totalMineralSold,
                TotalCraftingSold = TodayCraftingSold + totalCraftingSold,
                TotalCookingSold = TodayCookingSold + totalCookingSold,
                TotalFishSold = TodayFishSold + totalFishSold,
                TotalGemSold = TodayGemSold + totalGemSold
            };
            if (Game1.IsMasterGame)
            {
                SHelper.Data.WriteSaveData("MT.MailLog", dataToSave);
                new MailLoader(SHelper);
            }
        }

        public static void SetUpMarnieLivestockShop()
        {
            SMonitor.Log("Opening Marnie Livestock shop", LogLevel.Trace);

            GameLocation island = Game1.getLocationFromName("Custom_MT_Island");
            var thisNpc = new NPC();

            if ( 1 == 1)
            {
                thisNpc = Game1.getCharacterFromName("Marnie");
                if (!IslandNPCList.Contains("Marnie")) IslandNPCList.Add("Marnie");
            }
            else
            {
                thisNpc = Game1.getCharacterFromName("MT.Guest_Marnie");
                if (!IslandNPCList.Contains("MT.Guest_Marnie")) IslandNPCList.Add("MT.Guest_Marnie");
            }
            thisNpc.modData["hapyke.FoodStore/shopOwnerToday"] = "57,18";

            TodayShopInventory.Add(new MarketShopData("z_marnie2", new(54, 18), new List<string>()));

            if (Game1.random.NextDouble() < 0.75)
            {
                List<string> animalType = new List<string> { "Brown Cow", "White Cow", "Blue Chicken", "Brown Chicken", "White Chicken", "Duck", "Goat", "Pig", "Rabbit", "Sheep" };
                island.Animals.Add(-999, new FarmAnimal(animalType[Game1.random.Next(animalType.Count)], -999, -999));
                island.Animals.Add(-998, new FarmAnimal(animalType[Game1.random.Next(animalType.Count)], -998, -999));


                var animalInstance1 = Utility.getAnimal(-999);
                animalInstance1.modData["hapyke.FoodStore/isFakeAnimal"] = "true";
                animalInstance1.setTileLocation(new Vector2(54, 17));

                var animalInstance2 = Utility.getAnimal(-998);
                animalInstance2.modData["hapyke.FoodStore/isFakeAnimal"] = "true";
                animalInstance2.setTileLocation(new Vector2(56, 17));
            }
            else
            {
                List<string> animalType = new List<string> { "Blue Chicken", "Brown Chicken", "White Chicken", "Duck" };
                List<Vector2> animalTile = new List<Vector2> { new(54, 17), new(55, 17), new(56, 17), new(54, 16), new(55, 16), new(56, 16) };
                int count = 0;
                while (count < 6)
                {
                    int id = -999 + count;

                    island.Animals.Add(id, new FarmAnimal(animalType[Game1.random.Next(animalType.Count)], id, -999));

                    var animalInstance1 = Utility.getAnimal(id);
                    animalInstance1.modData["hapyke.FoodStore/isFakeAnimal"] = "true";
                    animalInstance1.setTileLocation(animalTile[count]);

                    count++;
                }
            }
        }
    }
}