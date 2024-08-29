using ContentPatcher;
using HarmonyLib;
using MailFrameworkMod;
using MarketTown.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.GameData.Characters;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xTile.Dimensions;
using xTile.Layers;
using static MarketTown.ModEntry;
using Object = StardewValley.Object;

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

            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;

            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            Helper.Events.Player.InventoryChanged += Player_InventoryChanged;

            helper.Events.Display.MenuChanged += this.OnMenuChanged;

            helper.Events.Content.AssetRequested += this.OnAssetRequested;

            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;

            helper.Events.Player.Warped += FarmOutside.PlayerWarp;

            helper.ConsoleCommands.Add("markettown", "Fix stuck/error NPC by reset them to their supposed location and schedule.\n\nUsage: markettown fix <Name>. Name: name of Npc (Lewis).\nUsage: markettown fix ALL: Fix all error NPC.\n\nTo auto-fix, see Config -> Advance -> Auto fix error NPC.", this.HandleCommand);

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

            // Restaurant Decor
            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), "drawAtNonTileSpot", new Type[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float) }),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(DrawAtNonTileSpot_Prefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), "loadDescription"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(LoadDescription_Postfix))
            );

            // Decor animal at Marnie animal shop Festival
            harmony.Patch(
                original: AccessTools.Method(typeof(FarmAnimal), "getSellPrice"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(GetSellPrice_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(FarmAnimal), "GetCursorPetBoundingBox"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(GetCursorPetBoundingBox_Postfix))
            );

            // Marnie animal shop at Festival
            harmony.Patch(
                original: AccessTools.Method(typeof(PurchaseAnimalsMenu), "setUpForReturnToShopMenu"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(SetUpForReturnToIslandAfterLivestockPurchase))
            );
            harmony.Patch(
               original: AccessTools.Method(typeof(PurchaseAnimalsMenu), "setUpForReturnAfterPurchasingAnimal"),
               postfix: new HarmonyMethod(typeof(ModEntry), nameof(SetUpForReturnToIslandAfterLivestockPurchase))
            );

            // shipping bin value with Ultimate Challenge
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), "_newDayAfterFade"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(NewDayAfterFade_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Object), "sellToStorePrice"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(SellToStorePrice_Postfix))
            );


            //##############################################################
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
            var api = this.Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");

            ConfigMenu(api, this.ModManifest, Helper);

        }       // **** Config Handle ****

        private void HandleCommand(string cmd, string[] args)
        {
            if (args.Length == 0) return; 

            if (args[0] == "fix")
            {
                List<NPC> npcList = new List<NPC>();

                var npcName = args[1];
                if (npcName.ToLower() == "all")
                {
                    foreach (var x in OutOfMapNpc)
                    {
                        if (x != null) npcList.Add(x);
                    }
                }
                else
                {
                    var x = Game1.getCharacterFromName(npcName);
                    if (x != null) npcList.Add(x);
                    else Console.WriteLine("Cannot find NPC: " + npcName);
                }

                foreach (var npc in npcList)
                {
                    ResetNpcSchedule(npc);
                }
            }
        }

        private void OneButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (Game1.activeClickableMenu is not null) return;
            if (Game1.currentLocation is null) return;
            if (!e.Button.IsActionButton()) return;
            if (Game1.player.currentLocation.Name != "Custom_MT_Island") return;

            var tile = e.Cursor.Tile;
            var player = Game1.player.Tile;

            if (tile.X > 75 && tile.X < 79 && tile.Y < 32 && tile.Y > 28 && player.X > 74 && player.X < 80 && player.Y > 27 && player.Y < 33)
                OpenPlayerStore(e);

            if (!Game1.IsMasterGame) return;

            if (tile.X > 62 && tile.X < 68 && tile.Y < 29 && tile.Y > 24 && player.X > 61 && player.X < 69 && player.Y > 23 && player.Y < 30
                && Game1.timeOfDay >= Config.FestivalTimeStart && Game1.timeOfDay <= Config.FestivalTimeEnd)
            {
                GameLocation island = Game1.getLocationFromName("Custom_MT_Island");
                Layer buildings1Layer = island.map.GetLayer("Buildings1");

                Location pixelPosition = new Location(64 * Game1.tileSize, 28 * Game1.tileSize);
                if (buildings1Layer != null && buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size) != null)
                {
                    var tileProperty = buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size).TileSheet.Id;
                    if (tileProperty == "z_marnie2")
                    {
                        Game1.currentLocation.ShowAnimalShopMenu();
                    }
                }
            }

            if (tile.X > 85 && tile.X < 89 && tile.Y == 31 && player.X > 84 && player.X < 90 && player.Y > 29 && player.Y < 33
                && Game1.timeOfDay >= Config.FestivalTimeStart && Game1.timeOfDay <= Config.FestivalTimeEnd)
            {
                GameLocation island = Game1.getLocationFromName("Custom_MT_Island");
                Layer buildings1Layer = island.map.GetLayer("Buildings1");

                Location pixelPosition = new Location(86 * Game1.tileSize, 31 * Game1.tileSize);
                if (buildings1Layer != null && buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size) != null)
                {
                    var tileProperty = buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size).TileSheet.Id;
                    if (tileProperty == "z_robin")
                    {
                        Game1.currentLocation.ShowConstructOptions("Robin");
                    }
                }
            }

            if (tile.X > 69 && tile.X < 73 && tile.Y > 22 && tile.Y < 25 && player.X > 68 && player.X < 74 && player.Y == 25
                && Game1.timeOfDay >= Config.FestivalTimeStart && Game1.timeOfDay <= Config.FestivalTimeEnd)
            {
                GameLocation island = Game1.getLocationFromName("Custom_MT_Island");
                Layer buildings1Layer = island.map.GetLayer("Buildings1");

                Location pixelPosition = new Location(70 * Game1.tileSize, 24 * Game1.tileSize);
                if (buildings1Layer != null && buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size) != null)
                {
                    var tileProperty = buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size).TileSheet.Id;
                    if (tileProperty == "z_clintgeodes")
                    {
                        Game1.activeClickableMenu = new GeodeMenu();
                    }
                }
            }
        }

        internal void OpenPlayerStore(ButtonPressedEventArgs e)
        {
            var location = Game1.getLocationFromName("Custom_MT_Island");
            var obj = location.getObjectAtTile(176, 31);
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

        public static void RestockPlayerFestival()
        {
            Random random = new Random();
            var location = Game1.getLocationFromName("Custom_MT_Island");
            var obj1 = location.getObjectAtTile(176, 31, true);
            var obj2 = location.getObjectAtTile(79, 29, true);


            List<int> emptySlot = new List<int>();

            if (obj1 != null && obj2 != null && obj1 is Chest displayChest && obj2 is Chest stockChest && stockChest.Items.Any())
            {
                for (int i = 0; i < 9; i++)
                    if (displayChest.Items.Count < i || displayChest.Items[i] == null) emptySlot.Add(i);

                foreach (var i in emptySlot)
                {
                    var itemList = stockChest.Items.Where(i => i != null && i.QualifiedItemId.StartsWith("(O)")).ToList();
                    if (!itemList.Any()) break;

                    var item = itemList[random.Next(itemList.Count)];
                    var index = itemList.IndexOf(item);
                    var stockChestIndex = stockChest.Items.IndexOf(item);

                    if (item != null)
                    {
                        addItemToGrangeDisplay(item.getOne(), i, false);
                    }
                    if (stockChest.Items[stockChestIndex] != null && stockChest.Items[stockChestIndex].Stack > 1) stockChest.Items[stockChestIndex].Stack -= 1;
                    else if (stockChest.Items[stockChestIndex] != null && stockChest.Items[stockChestIndex].Stack == 1) stockChest.Items.RemoveAt(stockChestIndex);

                }
            }
        }

        private static void addItemToGrangeDisplay(Item? i, int position, bool force)
        {

            var location = Game1.getLocationFromName("Custom_MT_Island");
            var obj = location.getObjectAtTile(176, 31);

            if (obj == null || obj is not Chest chest || location == null) return;

            while (chest.Items.Count < 9) chest.Items.Add(null);

            if (position < 0) return;
            if (position >= chest.Items.Count) return;
            if (chest.Items[position] != null && !force) return;

            chest.Items[position] = i;
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
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

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (!Game1.hasLoadedGame) return;

            if (e.NewMenu is ShopMenu shop)
            {
                if (shop.ShopId.Contains("MarketTown."))
                {
                    shop.onPurchase = (item, player, amount) =>
                    {
                        var location = Game1.getLocationFromName("Custom_MT_Island");
                        foreach (var tile in shopLocations)
                        {
                            var obj = location.getObjectAtTile((int)tile.X + 100, (int)tile.Y);
                            if (obj is not null && obj is Chest stockChest && stockChest.modData["hapyke.FoodStore/festivalChestName"] == shop.ShopId)
                            {
                                var itemItem = stockChest.Items.Where(i => i != null && i.QualifiedItemId == item.QualifiedItemId).FirstOrDefault();
                                var index = stockChest.Items.IndexOf(itemItem);
                                stockChest.Items[index] = null;

                                ShopStockUpdate = true;;
                                break;
                            }
                        }
                        
                        return false;
                    };
                }
            }

            if(e.NewMenu is CarpenterMenu centerMenu)
            {
                bool islandStatus = Game1.getLocationFromName("Custom_MT_Island").isAlwaysActive.Value;
                if (!islandStatus) { Game1.addHUDMessage(new HUDMessage(SHelper.Translation.Get("foodstore.island.building"), 5000, true)); }
            }
        }

        /// <summary>Check for Shop after Content Patcher patch the map, then generate shop stock and shop owner schedule.</summary>
        private void SetupShop(bool init)
        {
            try
            {
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

                if (init) OpenShopTile.Clear();

                GameLocation island = Game1.getLocationFromName("Custom_MT_Island");
                Layer buildings1Layer = island.map.GetLayer("Buildings1");


                foreach (var tile in shopLocations)
                {
                    Location pixelPosition = new Location((int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize);
                    if (buildings1Layer != null && buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size) != null)
                    {
                        var tileProperty = buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size).TileSheet.Id;

                        // if is Marnie livestock shop
                        if (tileProperty == "z_marnie2" && init) SetUpMarnieLivestockShop();


                        // if is Robin builder shop
                        if (tileProperty == "z_robin" && init) SetUpRobinBuilderShop();


                        // if is Clint geodes shop
                        if (tileProperty == "z_clintgeodes" && init) SetUpClintGeodesShop();

                        // if is normal shop
                        if (shopName.ContainsKey(tileProperty))
                        {
                            for (int i = (int)tile.X; i <= (int)tile.X + 2; i++)
                            {
                                for (int j = (int)tile.Y - 2; j <= (int)tile.Y; j++)
                                {
                                    if (init) OpenShopTile.Add(new Vector2(i, j), shopName[tileProperty]);
                                }
                            }

                            if (init && Game1.IsMasterGame)
                            {
                                var chest = new Chest(true);
                                chest.destroyOvernight = true;
                                chest.Fragility = 2;
                                chest.modData["hapyke.FoodStore/festivalChestName"] = shopName[tileProperty];
                                chest.modData["hapyke.FoodStore/festivalChestTile"] = $"{tile.X}, {tile.Y}";
                                if (Game1.IsMasterGame) island.setObjectAt(tile.X + 100, tile.Y, chest);

                                GenerateShop(shopName[tileProperty], tile, chest);
                                SetupChest(shopName[tileProperty], tile);
                                Monitor.Log($"Opening {shopName[tileProperty]}", LogLevel.Trace);
                            }
                            else if (!init && Game1.random.NextDouble() < 0.75 / Math.Sqrt(IslandProgress()) && Game1.IsMasterGame)
                            {
                                // renew shop stock
                                var obj1 = island.getObjectAtTile((int)(tile.X + 100), (int)(tile.Y));
                                if (obj1 is Chest chest && obj1 is not null)
                                {
                                    chest.Items.Clear();
                                    GenerateShop(shopName[tileProperty], tile, chest);
                                }

                                var ShopDataLoader = StardewValley.DataLoader.Shops(Game1.content);
                                SHelper.Multiplayer.SendMessage(ShopDataLoader, "10MinSyncShopDataLoader");
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { SMonitor.Log("Error while generating shop stock: " + ex.Message, LogLevel.Error); }
        }

        /// <summary>Set schedule during Festival for NPC.</summary>
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

            // Special schedule for shop owners
            if (npc.modData["hapyke.FoodStore/shopOwnerToday"] != "-1,-1")
            {
                string[] components = npc.modData["hapyke.FoodStore/shopOwnerToday"].Split(',');
                var shopStandTile = new Vector2(Int32.Parse(components[0]), Int32.Parse(components[1]));

                initSche += $"{Config.FestivalTimeStart} Custom_MT_Island {shopStandTile.X} {shopStandTile.Y} 2/";
                npc.TryLoadSchedule("default", initSche);

                return;

            }

            List<Vector2> shoplocation = new List<Vector2> { };
            GameLocation island = Game1.getLocationFromName("Custom_MT_Island");
            Layer buildings1Layer = island.map.GetLayer("Buildings1");

            List<Vector2> shopLocations = new List<Vector2>
            {
                new Vector2(86, 31),
                new Vector2(86, 36),
                new Vector2(76, 26),
                new Vector2(64, 28),
                new Vector2(76, 36),
                new Vector2(70, 24),
                new Vector2(70, 29),
                new Vector2(70, 34),
                new Vector2(64, 33),
                new Vector2(76, 31)
            };

            foreach (var tile in shopLocations)
            {
                Location pixelPosition = new Location((int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize);
                if (buildings1Layer != null && buildings1Layer.PickTile(pixelPosition, Game1.viewport.Size) != null && !shoplocation.Contains(tile))
                {
                    shoplocation.Add(tile);
                }
            }

            while (lastScheduleTime < Config.FestivalTimeEnd - 50)
            {
                var randomShopTile = shoplocation[random.Next(shoplocation.Count)];

                // player's shop has 20% higher chance than others
                Vector2 availableTile = new Vector2();
                if (random.NextDouble() < 0.2) availableTile = new Vector2(76, 31) + new Vector2(random.Next(0, 4) - 1, 1);
                else availableTile = randomShopTile + new Vector2(random.Next(0, 4) - 1, 1);

                // NPC walk around
                if (random.NextDouble() < 0.4)
                {
                    var otherTile = FarmOutside.getRandomOpenPointInLocation(npc, npc.currentLocation, false).ToVector2();
                    if (Utility.distance(67, otherTile.X, 20, otherTile.Y) < 25)
                    {
                        availableTile = otherTile;
                        finalFace = weightedFacing[random.Next(weightedFacing.Length)];
                    }
                }
                int storeScheduleTime = ConvertToHour(lastScheduleTime + weightedNumbers[random.Next(weightedNumbers.Length)] * 10);
                initSche += $"{storeScheduleTime} Custom_MT_Island {availableTile.X} {availableTile.Y} {finalFace}/ ";

                lastScheduleTime = storeScheduleTime;
            }

            var randomTile = FarmOutside.getRandomOpenPointInLocation(npc, npc.currentLocation, false).ToVector2();
            if (randomTile != Vector2.Zero)
            {
                FarmOutside.AddNextMoveSchedulePoint(npc, $"{ConvertToHour(Config.FestivalTimeEnd + 10)}", $"{npc.currentLocation.NameOrUniqueName}",
                    $"{randomTile.X}", $"{randomTile.Y}", $"{random.Next(0, 4)}");
            }

            npc.TryLoadSchedule("default", initSche);
        }

        /// <summary>Set Map Tile properties for Shop.</summary>
        private void OpenShop(IDictionary<Vector2, string> shopTile)
        {
            Game1.addHUDMessage(new HUDMessage(SHelper.Translation.Get("foodstore.festival.start")));
            var island = Game1.getLocationFromName("Custom_MT_Island");

            IsFestivalIsCurrent = true;
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

            if (Game1.IsMasterGame)
            {
                var ShopDataLoader = StardewValley.DataLoader.Shops(Game1.content);
                SHelper.Multiplayer.SendMessage(ShopDataLoader, "10MinSyncShopDataLoader");
            }
        }

        /// <summary>Remove Map Tile properties for Shop.</summary>
        private void CloseShop(bool endDay)
        {
            IsFestivalIsCurrent = false;
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
            }
            catch { }

            foreach (var islandVisitorName in IslandNPCList)
            {
                NPC npc = Game1.getCharacterFromName(islandVisitorName);
                if (npc != null) npc.modData["hapyke.FoodStore/shopOwnerToday"] = "-1,-1";
            }

            if (IsFestivalToday)
            {
                SHelper.Multiplayer.SendMessage(FestivalSellLog, "FestivalSellLog");

                string soldMessage = "";
                foreach (var i in FestivalSellLog)
                    soldMessage += i + "^";

                var letterTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("Assets/LtBG.png");
                MailRepository.SaveLetter(
                    new Letter(
                        "MT.SellLogMail",
                        soldMessage,
                    (l) => true)
                    {
                        LetterTexture = letterTexture
                    }
                );
            }
        }

        /// <summary>Set up Sign and Chest.</summary>
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

            var location = Game1.getLocationFromName("Custom_MT_Island");
            var obj = location.getObjectAtTile((int)tile.X + 100, (int)tile.Y);
            if (obj is not null && obj is Chest stockChest)
            {
                sign.displayItem.Value = stockChest.Items[Game1.random.Next(stockChest.Items.Count)];
                sign.displayType.Value = 1;
            }
            Game1.getLocationFromName("Custom_MT_Island").setObjectAt(chestTile.X, chestTile.Y, chest);
            Game1.getLocationFromName("Custom_MT_Island").setObjectAt(signTile.X, signTile.Y, sign);
        }

        /// <summary>Make NPC purchase during Festival.</summary>
        public static void NpcFestivalPurchase()
        {
            Random random = new Random();
            var islandInstance = Game1.getLocationFromName("Custom_MT_Island");

            List<Chest> chestList = new List<Chest>();
            List<Vector2> shopLocations = new List<Vector2>
            {
                new Vector2(86, 31),
                new Vector2(86, 36),
                new Vector2(76, 26),
                new Vector2(64, 28),
                new Vector2(76, 36),
                new Vector2(70, 24),
                new Vector2(70, 29),
                new Vector2(70, 34),
                new Vector2(64, 33),
                new Vector2(76, 31)
            };
            foreach (var tile in shopLocations)
            {
                var obj1 = islandInstance.getObjectAtTile((int)(tile.X + 100), (int)(tile.Y));
                if (obj1 is Chest chest && obj1 is not null && !chestList.Contains(chest))
                {
                    chestList.Add(chest);
                }
            }

            if (IsFestivalToday && Game1.timeOfDay > Config.FestivalTimeStart && Game1.timeOfDay < Config.FestivalTimeEnd)
            {
                foreach (var chest in chestList)
                {
                    var tileCheck = chest.TileLocation;
                    // back to the real tile
                    var npcL = Utility.GetNpcsWithinDistance(tileCheck + new Vector2(-99, 0), 2, islandInstance).ToList();
                    var npcList = new List<NPC>();
                    var itemList = chest.Items.Where(item => item != null).Select(item => item.QualifiedItemId).ToList();
                    var itemItem = chest.Items[random.Next(chest.Items.Count)];

                    if (npcL.Any())
                    {
                        foreach (var tempNPC in npcL)
                        {
                            if (tempNPC != null && tempNPC.Sprite.currentFrame == 8
                                    && Int32.Parse(tempNPC.modData["hapyke.FoodStore/festivalLastPurchase"]) <= Game1.timeOfDay - 10)
                                npcList.Add(tempNPC);
                        }
                    }

                    if (itemList.Count == 0 || itemList.All(item => item == null)
                        || !npcList.Any()) continue;

                    // random NPC buy random item
                    var npcBuy = npcList[Game1.random.Next(npcList.Count)];
                    var randomItemSold = itemList[Game1.random.Next(itemList.Count)];

                    Color randomColor = new Color((byte)random.Next(30, 150), (byte)random.Next(30, 150), (byte)random.Next(30, 150));

                    if (randomItemSold != null && npcBuy != null && itemList.Contains(randomItemSold))
                    {
                        int index = chest.Items.IndexOf(chest.Items.Where(i => i != null && i.QualifiedItemId == randomItemSold).FirstOrDefault());
                        if (index >= 0)
                        {
                            double nonPlayerChance = random.NextDouble();
                            double playerChance = random.NextDouble();
                            if ( chest.modData.ContainsKey("hapyke.FoodStore/festivalChestName") && chest.modData["hapyke.FoodStore/festivalChestName"] != "MarketTown.playerShop" && nonPlayerChance < Config.FestivalMaxSellChance / Math.Sqrt(IslandProgress()) + 0.3)
                            {
                                var shopObj = StardewValley.DataLoader.Shops(Game1.content)[chest.modData["hapyke.FoodStore/festivalChestName"]];

                                var shopItem = shopObj.Items.FirstOrDefault(item => item?.ItemId == randomItemSold);
                                if (shopItem != null)
                                {
                                    chest.Items[index] = null;

                                    string message = GetSoldMessage(randomItemSold, chest.modData["hapyke.FoodStore/festivalChestName"]);
                                    npcBuy.showTextAboveHead(message, randomColor, 2, 4000, 1000);
                                    var farmhandNpcBubbleMessage = new MyMessage($"{npcBuy.Name}///{message}");
                                    SHelper.Multiplayer.SendMessage(farmhandNpcBubbleMessage, "NpcShowText");

                                    shopObj.Items.Remove(shopItem);
                                    npcBuy.modData["hapyke.FoodStore/festivalLastPurchase"] = Game1.timeOfDay.ToString();

                                    ShopStockUpdate = true;
                                    break;
                                }
                            }
                            else if (chest.modData.ContainsKey("hapyke.FoodStore/festivalChestName") && chest.modData["hapyke.FoodStore/festivalChestName"] == "MarketTown.playerShop" && playerChance < Config.FestivalMaxSellChance / Math.Sqrt(IslandProgress()))
                            {
                                var itemObj = chest.Items.FirstOrDefault(item => item?.QualifiedItemId == randomItemSold);
                                if (itemObj != null)
                                {
                                    var itemIndex = chest.Items.IndexOf(itemObj);

                                    chest.Items[itemIndex] = null;

                                    string message = GetSoldMessage(randomItemSold, "PlayerShop");
                                    npcBuy.showTextAboveHead(message, randomColor, 2, 4000, 1000);
                                    var farmhandNpcBubbleMessage = new MyMessage($"{npcBuy.Name}///{message}");
                                    SHelper.Multiplayer.SendMessage(farmhandNpcBubbleMessage, "NpcShowText");

                                    npcBuy.modData["hapyke.FoodStore/festivalLastPurchase"] = Game1.timeOfDay.ToString();

                                    var price = (int)(itemObj.sellToStorePrice() * (random.NextDouble() / 2 + 1.5) * 2 / Math.Sqrt(IslandProgress()) * Config.IslandMoneyModifier * (Config.UltimateChallenge ? 4 : 1));
                                    if (Game1.IsMasterGame) AddToPlayerFunds(price);

                                    string quality = " ";
                                    switch (itemObj.Quality)
                                    {
                                        case 1:
                                            quality = " Silver ";
                                            break;
                                        case 2:
                                            quality = " Gold ";
                                            break;
                                        case 4:
                                            quality = " Iridium ";
                                            break;
                                    }

                                    TodayFestivalIncome += price;
                                    FestivalSellLog.Add($"  -{quality}{itemObj.DisplayName}: {price}G");

                                    SHelper.Multiplayer.SendMessage(TodayFestivalIncome, "TodayFestivalIncome");

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

        /// <summary>Add Money to all online players.</summary>
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
            }
            catch { return "Awesome!"; }
        }

        /// <summary>When NPC ready to make new purchase.</summary>
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

        /// <summary>When NPC ready to show new message.</summary>
        public static bool WantsToSay(NPC npc, int time)
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

        /// <summary>Time dalay between each try to make purchase.</summary>
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

        /// <summary>Get a score of decoration -0.2 -> 0.5. Higher mean better decoration.</summary>
        public static double GetDecorPoint(Vector2 foodLoc, GameLocation gameLocation, int range = 8)
        {
            double decorPoint = 0;

            bool foundWater = false;
            for (var y = foodLoc.Y - range; y < foodLoc.Y + range; y++)
            {
                for (var x = foodLoc.X - range; x < foodLoc.X + range; x++)
                {
                    // furniture or big craftable
                    Object obj = gameLocation.getObjectAtTile((int)x, (int)y) ?? null;
                    if (obj != null)
                    {
                        if (obj.QualifiedItemId.StartsWith("(F)") || obj.QualifiedItemId.StartsWith("(BC)"))
                        {
                            decorPoint += 0.75;
                            if (obj is Furniture furniture && furniture.heldObject.Value != null) decorPoint += 0.25;
                        }
                    }

                    // water
                    if (gameLocation.isWaterTile((int)x, (int)y) && !foundWater)
                    {
                        decorPoint += 5;
                        foundWater = true;
                    }

                    // crop
                    if (gameLocation.isCropAtTile((int)x, (int)y))
                    {
                        decorPoint += 0.1;
                    }
                }
            }

            // Check if player are below the pink tree in Town, Mountain and Forest
            if (gameLocation.Name == "Town" ||
                gameLocation.Name == "Mountain" ||
                gameLocation.Name == "Forest")
            {
                bool done = false;
                for (var y = foodLoc.Y - range; y < foodLoc.Y + range; y++)
                {
                    for (var x = foodLoc.X - range; x < foodLoc.X + range; x++)
                    {
                        int TileId = Game1.player.currentLocation.getTileIndexAt((int)x, (int)y, "Buildings");

                        if (TileId == 143 || TileId == 144 ||
                            TileId == 168 || TileId == 169)
                        {
                            decorPoint += 10;
                            done = true;
                            break;
                        }
                    }
                    if (done) break;
                }
            }

            if (gameLocation.Name.Contains("Custom_MT_Island")
                || gameLocation.GetParentLocation() != null && gameLocation.GetParentLocation().NameOrUniqueName == "Custom_MT_Island")
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

        /// <summary>Selected NPC will share their opinion, and nearby NPCs will response to that.</summary>
        public static void SaySomething(NPC thisCharacter, GameLocation thisLocation, double lastTasteRate, double lastDecorRate)
        {
            Random random = new Random();

            double chanceToVisit = lastTasteRate + lastDecorRate;
            double localNpcCount = 0.6;

            double getChance = random.NextDouble();
            List<NPC> localNpc = new List<NPC>();


            foreach (NPC newCharacter in thisLocation.characters.Where(i => i.IsVillager && i.getMasterScheduleRawData != null))
            {
                if (newCharacter != thisCharacter)
                {
                    var dx = newCharacter.Tile.X - thisCharacter.Tile.X;
                    var dy = newCharacter.Tile.Y - thisCharacter.Tile.Y;

                    if (Math.Abs(dx) <= 10 && Math.Abs(dy) <= 10 && localNpcCount > 0.2 && !localNpc.Contains(newCharacter))
                    {
                        localNpc.Add(newCharacter);
                        localNpcCount -= 0.0175;
                    }
                }
            }
            int randomIndex = random.Next(5);
            int randomIndex2 = random.Next(3);

            List<string> tastePool = new List<string> { };
            List<string> decorPool = new List<string> { };


            //taste string
            string tastestring = "string";
            if (lastTasteRate > 0.3)
            {
                tastestring = SHelper.Translation.Get("foodstore.positiveTasteint." + randomIndex);
                tastePool = new List<string> { "absolute amazing", "remarkable", "excellent and reliable" };
            }
            else if (lastTasteRate == 0.3)
            {
                tastestring = SHelper.Translation.Get("foodstore.normalTasteint." + randomIndex);
                tastePool = new List<string> { "just meets the standard", "pretty average", "just a reliable choice" };
            }
            else if (lastTasteRate < 0.3)
            {
                tastestring = SHelper.Translation.Get("foodstore.negativeTasteint." + randomIndex);
                tastePool = new List<string> { "extremely bad", "not up to the mark", "won't hold up for long." };
            }

            //decor string
            string decorstring = "string";
            if (lastDecorRate > 0)
            {
                decorstring = SHelper.Translation.Get("foodstore.positiveDecorint." + randomIndex);
                decorPool = new List<string> { "absolutely stunning!", "exquisite design, truly eye-catching.", "masterpiece of style and taste." };
            }
            else if (lastDecorRate == 0)
            {
                decorstring = SHelper.Translation.Get("foodstore.normalDecorint." + randomIndex);
                decorPool = new List<string> { "pretty standard, not stand out.", "just fit, nothing too fancy.", "blends in fine, nothing special."};
            }
            else if (lastDecorRate < 0)
            {
                decorstring = SHelper.Translation.Get("foodstore.negativeDecorint." + randomIndex);
                decorPool = new List<string> { "feels out of place", "not as impressive as expected.", "falls short of making an impact." };
            }


            // main NPC show bubble
            if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
            {
                string ageCategory = thisCharacter.Age == 0 ? "adult" : thisCharacter.Age == 1 ? "teens" : "child";
                string manner = thisCharacter.Manners == 0 ? "friendly." : thisCharacter.Manners == 1 ? "polite." : thisCharacter.Manners == 2 ? "rude." : "friendly.";
                Task.Run(() => SendMessageToAssistant(
                                npc: thisCharacter,
                                userMessage: $"",
                                systemMessage: $"As NPC {thisCharacter.Name} ({ageCategory}, {manner}) in Stardew Valley, you share your opinion about {Game1.MasterPlayer.Name}'s store to others that the items quality is {tastePool[random.Next(tastePool.Count)]} and decoration style is {decorPool[random.Next(decorPool.Count)]}, with under 27 text words")
                            );
            }
            else
                NPCShowTextAboveHead(thisCharacter, tastestring + ". " + decorstring);

            // other NPC
            foreach (var newCharacter in localNpc)
            {
                if (random.NextDouble() > localNpcCount) continue;

                if (getChance < chanceToVisit && lastTasteRate > 0.3 && lastDecorRate > 0)          //Will visit, positive Food, positive Decor
                {
                    if (newCharacter.modData["hapyke.FoodStore/LastFood"] == null) newCharacter.modData["hapyke.FoodStore/LastFood"] = "0";
                    newCharacter.modData["hapyke.FoodStore/LastFood"] = (Int32.Parse(newCharacter.modData["hapyke.FoodStore/LastFood"]) - Config.MinutesToHungry + 30).ToString();
                    NPCShowTextAboveHead(newCharacter, SHelper.Translation.Get("foodstore.willVisit." + randomIndex2));
                }
                else if (getChance < chanceToVisit)                                                 //Will visit, normal or negative Food , Decor
                {
                    if (newCharacter.modData["hapyke.FoodStore/LastFood"] == null) newCharacter.modData["hapyke.FoodStore/LastFood"] = "0";
                    if (Config.MinutesToHungry >= 60)
                        newCharacter.modData["hapyke.FoodStore/LastFood"] = (Int32.Parse(newCharacter.modData["hapyke.FoodStore/LastFood"]) - (Config.MinutesToHungry / 2)).ToString();
                    NPCShowTextAboveHead(newCharacter, SHelper.Translation.Get("foodstore.mayVisit." + randomIndex2));
                }
                else if (getChance >= chanceToVisit && lastTasteRate < 0.3 && lastDecorRate < 0)     //No visit, negative Food, negative Decor
                {
                    if (newCharacter.modData["hapyke.FoodStore/LastFood"] == null) newCharacter.modData["hapyke.FoodStore/LastFood"] = "2600";
                    newCharacter.modData["hapyke.FoodStore/LastFood"] = "2600";
                    NPCShowTextAboveHead(newCharacter, SHelper.Translation.Get("foodstore.noVisit." + randomIndex2));
                }
                else if (getChance >= chanceToVisit)                                                 //No visit, normal or positive Food, Decor
                {
                    NPCShowTextAboveHead(newCharacter, SHelper.Translation.Get("foodstore.mayVisit." + randomIndex2));
                }
            }
        }

        public static string GetRandomDish()
        {
            var resultList = Game1.objectData.Where(x => x.Value != null && x.Value.Category == -7).Select(x => x.Key ).ToList();

            if (resultList.Count > 0)
            {
                Random random = new Random();
                int randomIndex = random.Next(resultList.Count);
                string id = resultList[randomIndex];
                return id;
            }

            return "240";
        }

        public static int CountShedVisitor(GameLocation environment)
        {
            if (environment == null) return 99999;

            return environment.characters.ToList().Count;
        }

        /// <summary>Update Count for License progress.</summary>
        public static void UpdateCount(int category, int count = 1)
        {
            Dictionary<int, Action> categoryActions = new Dictionary<int, Action>
            {
                {-100,  () => {TodayClothesSold += count; } },

                {-81,   () => { TodayForageSold += count; } },

                {-80,   () => { TodayFlowerSold += count; } },

                {-79,   () => { TodayFruitSold += count; } },

                {-75,   () => { TodayVegetableSold += count; } },

                {-74,   () => { TodaySeedSold += count; } },

                {-28,   () => { TodayMonsterLootSold += count; } },

                {-27,   () => { TodayArtisanGoodSold += count; } },
                {-26,   () => { TodayArtisanGoodSold += count; } },

                {-18,   () => { TodayAnimalProductSold += count; } },
                {-17,   () => { TodayAnimalProductSold += count; } },
                {-6,    () => { TodayAnimalProductSold += count; } },
                {-5,    () => { TodayAnimalProductSold += count; } },

                {-16,   () => { TodayResourceMetalSold += count; } },
                {-15,   () => { TodayResourceMetalSold += count; } },

                {-12,   () => { TodayMineralSold += count; } },

                {-24,   () => { TodayCraftingSold += count; } },
                {-22,   () => { TodayCraftingSold += count; } },
                {-21,   () => { TodayCraftingSold += count; } },
                {-8,    () => { TodayCraftingSold += count; } },

                {-7,    () => { TodayCookingSold += count; } },

                {-23,   () => { TodayFishSold += count; } },
                {-4,    () => { TodayFishSold += count; } },

                {-2,    () => { TodayMineralSold += count; } }
            };

            if (categoryActions.ContainsKey(category))
            {
                categoryActions[category].Invoke();
            }
        }

        private static void NPCShowTextAboveHead(NPC npc, string message, bool bypass = false)
        {
            if (!WantsToSay(npc, 0) && !bypass) return;

            npc.modData["hapyke.FoodStore/LastSay"] = Game1.timeOfDay.ToString();
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

        /// <summary>Return a Double 4 - 1 score based on total grange selling. The lower the better progress.</summary>
        private static double IslandProgress()
        {
            MailData model = null;
            if (Game1.IsMasterGame) model = SHelper.Data.ReadSaveData<MailData>("MT.MailLog");
            else model = SHelper.Data.ReadJsonFile<MailData>("markettowndata.json") ?? new MailData();

            if (!Config.IslandProgress) return 1;
            if (model == null || model.TotalFestivalIncome == 0) return 4;

            var score = Config.GrangeSellProgress / model.TotalFestivalIncome;
            if (score > 4) return 4;
            if (score < 1) return 1;
            return score;

        }

        private static int MarketRecognition()
        {
            MailData model = null;
            if (Game1.IsMasterGame) model = SHelper.Data.ReadSaveData<MailData>("MT.MailLog");
            else model = SHelper.Data.ReadJsonFile<MailData>("markettowndata.json") ?? new MailData();

            if (model == null || model.TotalCustomerNote == 0) return 0;

            float totalCustomerNote = model.TotalCustomerNote;
            float totalCustomerNoteYes = model.TotalCustomerNoteYes;
            float totalCustomerNoteNo = model.TotalCustomerNoteNo + 1;

            if      (Game1.stats.DaysPlayed > 84 && totalCustomerNote > 150 && totalCustomerNoteYes / totalCustomerNote > 0.90) return 5;
            else if (Game1.stats.DaysPlayed > 56 && totalCustomerNote > 100 && totalCustomerNoteYes / totalCustomerNote > 0.85) return 4;
            else if (Game1.stats.DaysPlayed > 42 && totalCustomerNote > 70 &&  totalCustomerNoteYes / totalCustomerNote > 0.80) return 3;
            else if (Game1.stats.DaysPlayed > 28 && totalCustomerNote > 40 &&  totalCustomerNoteYes / totalCustomerNote > 0.75) return 2;
            else if (Game1.stats.DaysPlayed > 14 && totalCustomerNote > 20 &&  totalCustomerNoteYes / totalCustomerNote > 0.70) return 1;
            else return 0;
        }

        /// <summary>DEBUG: Output all items' id to a file.</summary>
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

        /// <summary>Convert minutes to HHMM.</summary>
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

        /// <summary>Convert HHMM to Minutes.</summary>
        public static int ConvertToMinute(int number)
        {
            int hours = number / 100;
            int minutes = number % 100;

            return hours * 60 + minutes;
        }

        /// <summary>Clear schedule, endpoint, controller, moving state.</summary>
        public static void CleanNpc(NPC __instance)
        {
            __instance.Halt();
            __instance.ClearSchedule();
            __instance.DirectionsToNewLocation = null;
            __instance.queuedSchedulePaths.Clear();
            __instance.previousEndPoint = __instance.TilePoint;
            __instance.temporaryController = null;
            __instance.controller = null;
            if (__instance.Sprite.CurrentFrame > 15) __instance.Sprite.CurrentFrame = 0;
            __instance.Halt();
        }

        /// <summary>Write Save.</summary>
        public static void EndOfDaySave()
        {
            if (Game1.IsMasterGame && (Game1.dayOfMonth == 7 || Game1.dayOfMonth == 14 || Game1.dayOfMonth == 21 || Game1.dayOfMonth == 28)) WeeklyFeatureDish = GetRandomDish();

            bool isOnSaveCreate = Game1.stats.DaysPlayed == 0;

            int totalVisitorVisited = 0;
            int totalMoney = 0;
            int totalFestivalIncome = 0;

            int totalFriendVisited = 0;

            int totalCustomerNote = 0;
            int totalCustomerNoteYes = 0;
            int totalCustomerNoteNo = 0;

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
            int totalClothesSold = 0;

            float totalPointTaste = 0;
            float totalPointDecor = 0;

            MailData model = null;

            if (Game1.IsMasterGame) model = SHelper.Data.ReadSaveData<MailData>("MT.MailLog");
            else model = SHelper.Data.ReadJsonFile<MailData>("markettowndata.json") ?? new MailData();

            if (model != null)
            {
                totalVisitorVisited = model.TotalVisitorVisited;
                totalFriendVisited = model.TotalFriendVisited;

                totalMoney = model.TotalEarning;
                totalFestivalIncome = model.TotalFestivalIncome;

                totalCustomerNote = model.TotalCustomerNote;
                totalCustomerNoteYes = model.TotalCustomerNoteYes;
                totalCustomerNoteNo = model.TotalCustomerNoteNo;

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
                totalClothesSold = model.TotalClothesSold;

                totalPointTaste = model.TotalPointTaste;
                totalPointDecor = model.TotalPointDecor;
            }

            MailData dataToSave = new MailData
            {
                InitTable = !isOnSaveCreate,

                WeeklyDish = WeeklyFeatureDish,

                TotalVisitorVisited = totalVisitorVisited + TodayVisitorVisited,
                TotalFriendVisited = totalFriendVisited + TodayFriendVisited,

                TotalEarning = totalMoney + TodayMoney,
                TotalFestivalIncome = totalFestivalIncome + TodayFestivalIncome,

                SellMoney = TodayMoney,
                YesterdaySellLog = TodaySell,

                TodayCustomerInteraction = TodayCustomerInteraction,

                TodayMuseumVisitor = TodayMuseumVisitor,
                TodayMuseumEarning = TodayMuseumEarning,

                TotalCustomerNote = TodayCustomerNoteYes + TodayCustomerNoteNo + TodayCustomerNoteNone + totalCustomerNote,
                TotalCustomerNoteYes = TodayCustomerNoteYes + totalCustomerNoteYes,
                TotalCustomerNoteNo = TodayCustomerNoteNo + totalCustomerNoteNo,

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
                TotalGemSold = TodayGemSold + totalGemSold,
                TotalClothesSold = TodayClothesSold + totalClothesSold,

                TotalPointTaste = TodayPointTaste + totalPointTaste,
                TotalPointDecor = TodayPointDecor + totalPointDecor,

                npcConversation = conversationSummaries

            };
            if (Game1.IsMasterGame)
            {
                SHelper.Data.WriteSaveData("MT.MailLog", dataToSave);
                new MailLoader(SHelper);

                SHelper.Multiplayer.SendMessage(dataToSave, "MT.MailLogUpdate", new[] { "d5a1lamdtd.MarketTown" });
            }
        }

        public static void SetUpMarnieLivestockShop()
        {
            if (!Game1.IsMasterGame) return;
            SMonitor.Log("Opening Marnie Livestock shop", LogLevel.Trace);

            GameLocation island = Game1.getLocationFromName("Custom_MT_Island");
            var thisNpc = Game1.getCharacterFromName("Marnie");
            if (!IslandNPCList.Contains("Marnie")) IslandNPCList.Add("Marnie");

            thisNpc.modData["hapyke.FoodStore/shopOwnerToday"] = "67,28";

            if (Game1.random.NextDouble() < 0.75)
            {
                List<string> animalType = new List<string> { "Brown Cow", "White Cow", "Blue Chicken", "Brown Chicken", "White Chicken", "Duck", "Goat", "Pig", "Rabbit", "Sheep" };
                island.Animals.Add(-999, new FarmAnimal(animalType[Game1.random.Next(animalType.Count)], -999, -999));
                island.Animals.Add(-998, new FarmAnimal(animalType[Game1.random.Next(animalType.Count)], -998, -999));


                var animalInstance1 = Utility.getAnimal(-999);
                animalInstance1.modData["hapyke.FoodStore/isFakeAnimal"] = "true";
                animalInstance1.setTileLocation(new Vector2(64, 27));

                var animalInstance2 = Utility.getAnimal(-998);
                animalInstance2.modData["hapyke.FoodStore/isFakeAnimal"] = "true";
                animalInstance2.setTileLocation(new Vector2(66, 27));
            }
            else
            {
                List<string> animalType = new List<string> { "Blue Chicken", "Brown Chicken", "White Chicken", "Duck" };
                List<Vector2> animalTile = new List<Vector2> { new(64, 27), new(65, 27), new(96, 27), new(64, 26), new(65, 26), new(66, 26) };
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

        public static void SetUpRobinBuilderShop()
        {
            if (!Game1.IsMasterGame) return;
            SMonitor.Log("Opening Robin builder shop", LogLevel.Trace);

            GameLocation island = Game1.getLocationFromName("Custom_MT_Island");
            var thisNpc = Game1.getCharacterFromName("Robin");
            if (!IslandNPCList.Contains("Robin")) IslandNPCList.Add("Robin");

            thisNpc.modData["hapyke.FoodStore/shopOwnerToday"] = "89,30";

        }

        public static void SetUpClintGeodesShop()
        {
            if (!Game1.IsMasterGame) return;
            SMonitor.Log("Opening Clint geodes shop", LogLevel.Trace);

            GameLocation island = Game1.getLocationFromName("Custom_MT_Island");
            var thisNpc = Game1.getCharacterFromName("Clint");
            if (!IslandNPCList.Contains("Clint")) IslandNPCList.Add("Clint");

            thisNpc.modData["hapyke.FoodStore/shopOwnerToday"] = "71,23";


        }

        /// <summary> Table try to restock from nearby Market Storage </summary>
        /// <param name="bypass">Force restock ( dayEnd or sleep )</param>  
        public static void RestockTable(bool bypass = false, bool clean = false)
        {
            if (!Game1.IsMasterGame) return;
            Random random = new Random();
            try
            {
                if (RecentSoldTable.Count > 0)
                {
                    foreach (var kvp in RecentSoldTable)
                    {
                        bool flag = false;
                        var table = kvp.Key;
                        var timeOfSold = kvp.Value;
                        var location = table.Location;

                        var baseTile = table.TileLocation;
                        int range = 10, x = 0, y = 0, dx = 0, dy = -1, max = range * 2 + 1;

                        if (bypass) timeOfSold = 0;

                        if (Game1.timeOfDay < timeOfSold + 20
                            || location.getObjectAtTile((int)baseTile.X, (int)baseTile.Y, true) == null
                            || location.getObjectAtTile((int)baseTile.X, (int)baseTile.Y, true).QualifiedItemId != table.QualifiedItemId
                            || table.heldObject.Value != null && table.heldObject.Value is not Chest)
                        {
                            if (location.getObjectAtTile((int)baseTile.X, (int)baseTile.Y, true) == null
                                || location.getObjectAtTile((int)baseTile.X, (int)baseTile.Y, true).QualifiedItemId != table.QualifiedItemId)
                                RecentSoldTable.Remove(kvp);
                            continue;
                        }

                        if (table.heldObject.Value is Chest fChest)
                        {
                            List<int> nullItem_Index = new List<int>();
                            int currentIndex = 0;
                            foreach (var i in fChest.Items)
                            {
                                if (i == null) nullItem_Index.Add(currentIndex);
                                currentIndex++;
                            }

                            for (int i = 0; i < max * max; i++)
                            {
                                int currentX = (int)baseTile.X + x;
                                int currentY = (int)baseTile.Y + y;

                                if (Math.Abs(x) <= range && Math.Abs(y) <= range)
                                {
                                    Vector2 currentTile = new Vector2(currentX, currentY);
                                    Object obj = location.getObjectAtTile(currentX, currentY, true);

                                    if (obj != null && obj is Chest mtChest && mtChest.Items.Count > 0
                                        && (obj.QualifiedItemId == "(BC)MT.Objects.MarketTownStorageLarge" && (random.NextDouble() < Config.RestockChance || bypass)
                                        || obj.QualifiedItemId == "(BC)MT.Objects.MarketTownStorageSmall" && Math.Abs(x) <= 5 && Math.Abs(y) <= 5 && (random.NextDouble() < Config.RestockChance / 1.5 || bypass)))
                                    {
                                        List<int> categoryKeys = new List<int> { -81, -80, -79, -75, -74, -28, -27, -26, -23, -22, -21, -20, -19, -18, -17, -16, -15, -12, -8, -7, -6, -5, -4, -2 };
                                        
                                        int tried = 10;
                                        while (fChest.Items.Count(i => i == null) > 0 && tried > 0)
                                        {
                                            tried--;
                                            List<Item> filteredItems = mtChest.Items.Where(item => item.QualifiedItemId.StartsWith("(O)") && categoryKeys.Contains(item.Category)).ToList();
                                            if (filteredItems.Count > 0)
                                            {
                                                int index = random.Next(filteredItems.Count);

                                                var itemItem = filteredItems[index];

                                                var fIndex = fChest.Items.IndexOf(fChest.Items.FirstOrDefault(i => i == null));
                                                if (fIndex >= 0) fChest.Items[fIndex] = itemItem.getOne();

                                                var chestItem = mtChest.Items.Where(item => item == itemItem).FirstOrDefault();
                                                var chestIndex = mtChest.Items.IndexOf(chestItem);

                                                if (mtChest.Items[chestIndex].Stack > 1) mtChest.Items[chestIndex].Stack -= 1;
                                                else mtChest.Items.Remove(chestItem);

                                                if (fChest.Items.Count(i => i == null) == 0) flag = true;
                                                continue;
                                            }
                                            else break;
                                        }

                                        if (flag) RecentSoldTable.Remove(kvp);
                                    }
                                }

                                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                                {
                                    int temp = dx;
                                    dx = -dy;
                                    dy = temp;
                                }

                                x += dx;
                                y += dy;
                            }
                        }
                        else if (table.heldObject.Value == null)
                        {
                            for (int i = 0; i < max * max; i++)
                            {
                                int currentX = (int)baseTile.X + x;
                                int currentY = (int)baseTile.Y + y;

                                if (Math.Abs(x) <= range && Math.Abs(y) <= range)
                                {
                                    Vector2 currentTile = new Vector2(currentX, currentY);
                                    Object obj = location.getObjectAtTile(currentX, currentY, true);

                                    if (obj != null && obj is Chest mtChest && mtChest.Items.Count > 0
                                        && (obj.QualifiedItemId == "(BC)MT.Objects.MarketTownStorageLarge" && (random.NextDouble() < Config.RestockChance || bypass)
                                        || obj.QualifiedItemId == "(BC)MT.Objects.MarketTownStorageSmall" && Math.Abs(x) <= 5 && Math.Abs(y) <= 5 && (random.NextDouble() < Config.RestockChance / 2 || bypass)))
                                    {
                                        List<int> categoryKeys = new List<int> { -81, -80, -79, -75, -74, -28, -27, -26, -23, -22, -21, -20, -19, -18, -17, -16, -15, -12, -8, -7, -6, -5, -4, -2 };

                                        List<Item> filteredItems = mtChest.Items.Where(item => item.QualifiedItemId.StartsWith("(O)") && categoryKeys.Contains(item.Category)).ToList();
                                        if (filteredItems.Count > 0)
                                        {
                                            int index = random.Next(filteredItems.Count);

                                            var itemItem = filteredItems[index];
                                            var itemToSell = itemItem.getOne();

                                            if (itemToSell is Object itemObject)
                                            {
                                                table.SetHeldObject(itemObject);

                                                var chestItem = mtChest.Items.Where(item => item == itemItem).FirstOrDefault();
                                                var chestIndex = mtChest.Items.IndexOf(chestItem);

                                                if (mtChest.Items[chestIndex].Stack > 1) mtChest.Items[chestIndex].Stack -= 1;
                                                else mtChest.Items.Remove(chestItem);

                                                RecentSoldTable.Remove(kvp);
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                                {
                                    int temp = dx;
                                    dx = -dy;
                                    dy = temp;
                                }

                                x += dx;
                                y += dy;
                            }
                        }
                        else if (table.heldObject.Value != null)
                            RecentSoldTable.Remove(kvp);
                    }
                }

                if (clean) RecentSoldTable.Clear();
            }
            catch (Exception ex) { SMonitor.Log("Error while restock table" + ex.Message, LogLevel.Error); }
        }

        public static void InitFurniture()
        {
            if (!Game1.IsMasterGame) return;

            var town = Game1.getLocationFromName("Town");
            var island = Game1.getLocationFromName("Custom_MT_Island");
            var islandHouse = Game1.getLocationFromName("Custom_MT_Island_House");

            List<Vector2> rugList = new List<Vector2> { new(16, 6), new(18, 6), new(22, 10), new(24, 10), new(28, 6), new(30, 6), new(34, 10), new(36, 10) };
            List<Vector2> tableList = new List<Vector2> { new(17, 6), new(23, 10), new(29, 6), new(35, 10) };
            List<Vector2> townList = new List<Vector2> { new(27, 66), new(28, 66), new(29, 66), new(30, 66), new(27, 67), new(29, 67), new(28, 64) };

            var lamp1 = new Furniture("2331", new Vector2(23, 61));
            var lamp2 = new Furniture("2331", new Vector2(23, 63));
            island.furniture.Add(lamp1);
            island.furniture.Add(lamp2);
            lamp1.IsOn = true;
            lamp2.IsOn = true;

            foreach (var rug in rugList)
            {
                var obj = new Furniture("1742", rug);
                islandHouse.furniture.Add(obj);
            }
            foreach (var table in tableList)
            {
                var obj = new Furniture("1138", table);
                islandHouse.furniture.Add(obj);
                obj.heldObject.Add(ItemRegistry.Create<Furniture>("MT.Objects.RestaurantDecor"));
            }

            bool flag = townList.All(tile => town.isTilePlaceable(tile));

            if (flag)
            {
                foreach (var tile in townList)
                {
                    switch (townList.IndexOf(tile))
                    {
                        case 0:
                            var obj0 = new Furniture("1397", tile);
                            town.furniture.Add(obj0);
                            obj0.heldObject.Add(ItemRegistry.Create<Object>("393"));
                            break;
                        case 1:
                            var obj1 = new Furniture("1397", tile);
                            town.furniture.Add(obj1);
                            obj1.heldObject.Add(ItemRegistry.Create<Object>("22"));
                            break;
                        case 2:
                            var obj2 = new Furniture("1397", tile);
                            town.furniture.Add(obj2);
                            obj2.heldObject.Add(ItemRegistry.Create<Object>("340"));
                            break;
                        case 3:
                            var obj3 = new Furniture("1397", tile);
                            town.furniture.Add(obj3);
                            obj3.heldObject.Add(ItemRegistry.Create<Object>("142"));
                            break;
                        case 4:
                            var obj4 = new Furniture("724", tile);
                            town.furniture.Add(obj4);
                            obj4.heldObject.Add(ItemRegistry.Create<Object>("209"));
                            break;
                        case 5:
                            var obj5 = new Furniture("724", tile);
                            town.furniture.Add(obj5);
                            obj5.heldObject.Add(ItemRegistry.Create<Object>("334"));
                            break;
                        case 6:
                            var obj6 = new Chest(true, tile, "MT.Objects.MarketTownStorageSmall");
                            town.setObjectAt(tile.X, tile.Y, obj6);
                            obj6.Items.Add(ItemRegistry.Create<Item>("(O)472", 10));

                            var obj7 = new Furniture("2742", tile - new Vector2(2, -1));
                            town.furniture.Add(obj7);

                            var obj8 = new Furniture("MT.Objects.MarketLog", tile + new Vector2(1, 0));
                            town.furniture.Add(obj8);

                            var obj9 = new Furniture("FancyTree1", tile - new Vector2(2, 0));
                            town.furniture.Add(obj9);
                            var obj10 = new Furniture("FancyTree2", tile + new Vector2(2, 0));
                            town.furniture.Add(obj10);

                            break;
                    }
                }
            }
        }

        public static void FindSomething(NPC npc)
        {
            Random random = new Random();
            var tile = npc.Tile;
            var facing = npc.FacingDirection;
            var location = npc.currentLocation;

            int tried = 0;


            List<Vector2> checkingTile = new List<Vector2>();


            if (facing == 0)
                for (var y = 0; y >= -4; y--)
                {
                    checkingTile.Add(tile + new Vector2(0, y));
                    checkingTile.Add(tile + new Vector2(-1, y));
                    checkingTile.Add(tile + new Vector2(1, y));
                }
            else if (facing == 1)
                for (var x = 0; x <= 4; x++)
                {
                    checkingTile.Add(tile + new Vector2(x, 0));
                    checkingTile.Add(tile + new Vector2(x, -1));
                    checkingTile.Add(tile + new Vector2(x, 1));
                }
            else if (facing == 2)
                for (var y = 0; y <= 4; y++)
                {
                    checkingTile.Add(tile + new Vector2(0, y));
                    checkingTile.Add(tile + new Vector2(-1, y));
                    checkingTile.Add(tile + new Vector2(1, y));
                }
            else if (facing == 3)
                for (var x = 0; x >= -4; x--)
                {
                    checkingTile.Add(tile + new Vector2(x, 0));
                    checkingTile.Add(tile + new Vector2(x, -1));
                    checkingTile.Add(tile + new Vector2(x, 1));
                }


            //--------------


            if ( random.NextDouble() < 0.5) {
                while (tried < 10 && checkingTile.Any())
                {
                    tried++;
                    var targetTile = checkingTile[random.Next(checkingTile.Count)];
                    checkingTile.Remove(targetTile);

                    location.terrainFeatures.TryGetValue(targetTile, out var value);
                    var obj = location.getObjectAtTile((int)targetTile.X, (int)targetTile.Y, true);

                    if (value != null && (random.NextBool() || obj == null))
                    {
                        if (value is FruitTree fTree)
                        {
                            if (fTree.fruit.Count > 2 && Config.SellFruitTree)
                            {
                                var fruitSold = fTree.fruit[0];
                                NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.viewchat.fruittreebuy.{random.Next(1, 10)}",
                                    new { fruitName = fruitSold.DisplayName, playerName = Game1.player.Name }));

                                if (Game1.IsMasterGame) AddToPlayerFunds((int)(fruitSold.sellToStorePrice() * 4 * Config.MoneyModifier * (Config.UltimateChallenge ? 4 : 1)));
                                fTree.fruit.RemoveAt(0);
                                return;
                            }
                            else
                            {
                                if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                                {
                                    List<string> contextChoice = new List<string> { "still young", "need more time to grow", "seem to be a great tree", "will bear a lot of fruits" };
                                    if (fTree.daysUntilMature.Value <= 0 && !fTree.fruit.Any()) contextChoice = new List<string> { "provides excellent shade", "strong and sturdy branches", "waiting for fruit", "beautiful colors", "birds nesting", "tall and impressive", "attractive foliage" };
                                    else if (fTree.daysUntilMature.Value <= 0 && fTree.fruit.Any()) contextChoice = new List<string> { "bursting with fruit", "full of fruits", "wonderful tree", "great looking tree", "ready to harvest", "fruitful and abundant", "excited to taste" };

                                    string ageCategory = npc.Age == 0 ? "adult" : npc.Age == 1 ? "teens" : "child";
                                    string manner = npc.Manners == 0 ? "friendly." : npc.Manners == 1 ? "polite." : npc.Manners == 2 ? "rude." : "friendly.";
                                    Task.Run(() => SendMessageToAssistant(
                                        npc: npc,
                                        userMessage: $"",
                                        systemMessage: $"As NPC {npc.Name} ({ageCategory}, {manner}) in Stardew Valley, you will share your comment that this {fTree.GetDisplayName()} tree is {contextChoice[random.Next(contextChoice.Count)]}, using under 25 text words")
                                    );
                                }
                                else 
                                    NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.viewchat.fruittree.{fTree.daysUntilMature.Value <= 0}.{fTree.fruit.Any()}.{random.Next(1, 16)}",
                                        new { treeName = fTree.GetDisplayName(), playerName = Game1.player.Name }));
                                return;
                            }
                        }
                        else if (value is Tree tree)
                        {
                            NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.viewchat.wildtree.{tree.growthStage.Value >= 5}.{random.Next(1, 16)}",
                                new {playerName = Game1.player.Name }));
                            return;
                        }
                        else if (value is HoeDirt hoeDirt)
                        {
                            if (hoeDirt.crop != null)
                            {
                                // 5 : ready to harvest
                                var id = hoeDirt.crop.indexOfHarvest.Value;
                                var growth = hoeDirt.crop.currentPhase;
                                var crop = ItemRegistry.GetData("(O)" + id);

                                if (crop != null)
                                {
                                    if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                                    {
                                        List<string> contextChoice = new List<string> { "good", "lovely", "great color" };
                                        string ageCategory = npc.Age == 0 ? "adult" : npc.Age == 1 ? "teens" : "child";
                                        string manner = npc.Manners == 0 ? "friendly." : npc.Manners == 1 ? "polite." : npc.Manners == 2 ? "rude." : "friendly.";
                                        string ready = growth.Value >= 5 ? "ready" : "not ready";
                                        Task.Run(() => SendMessageToAssistant(
                                            npc: npc,
                                            userMessage: $"",
                                            systemMessage: $"As NPC {npc.Name} ({ageCategory}, {manner}) in Stardew Valley, you will share your comment that this {crop.DisplayName} is looking {contextChoice[random.Next(contextChoice.Count)]} and {ready} to harvest, using under 22 text words")
                                        );
                                    }
                                    else
                                        NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.viewchat.crop.{growth.Value >= 5}.{random.Next(1, 16)}",
                                            new { playerName = Game1.player.Name, cropName = crop.DisplayName }));
                                    return;
                                }
                            }
                            else
                            {
                                NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.viewchat.emptydirt.{random.Next(1, 6)}",
                                        new { playerName = Game1.player.Name }));
                                return;
                            }
                        }
                    }
                    else if (obj != null && obj is Furniture furniture)
                    {
                        List<int> validCategory = new List<int> { 0, 1, 2, 3, 4, 6, 8, 10, 14, 17};
                        if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                        {
                            List<string> contextChoice = new List<string> { "good", "lovely", "great color" };
                            string ageCategory = npc.Age == 0 ? "adult" : npc.Age == 1 ? "teens" : "child";
                            string manner = npc.Manners == 0 ? "friendly." : npc.Manners == 1 ? "polite." : npc.Manners == 2 ? "rude." : "friendly.";
                            string furnitureType = furniture.furniture_type.Value switch
                            {
                                0 => "chair",
                                1 => "bench",
                                2 => "couch",
                                3 => "armchair",
                                4 => "dresser",
                                5 => "long table",
                                6 => "painting",
                                7 => "lamp",
                                8 => "decor",
                                9 => "furniture",
                                10 => "bookcase",
                                11 => "table",
                                12 => "rug",
                                13 => "window",
                                14 => "fireplace",
                                15 => "bed",
                                16 => "torch",
                                17 => "sconce",
                                _ => "furniture"
                            };

                            Task.Run(() => SendMessageToAssistant(
                                npc: npc,
                                userMessage: $"",
                                systemMessage: $"As NPC {npc.Name} ({ageCategory}, {manner}) in Stardew Valley, you will share your comment that this {furniture.DisplayName} {furnitureType}, using under 20 text words")
                            );
                        }
                        else if (validCategory.Contains(furniture.furniture_type.Value))
                        {
                            NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.viewchat.furniture.{furniture.furniture_type.Value}.{random.Next(1, 6)}",
                                    new { playerName = Game1.player.Name, furniName = furniture.DisplayName }));
                            return;
                        }
                    }
                }
            }
            else
            {
                foreach (var animal in location.Animals.Values)
                {
                    if (checkingTile.Contains(animal.Tile))
                    {
                        NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.viewchat.animal.{animal.age.Value >= animal.GetAnimalData().DaysToMature}.{random.Next(1, 15)}",
                            new { playerName = Game1.player.Name, animalType = animal.displayType.ToLower(), animalName = animal.Name }));
                        return;
                    }
                }
            }
        }

        public static void UpdateFurnitureTilePathProperties (GameLocation location)
        {
            if (location == null || !Game1.IsMasterGame) return;

            foreach (var x in location.terrainFeatures.Values)
            {
                if (x.isPassable()) continue;
                if (!location.doesEitherTileOrTileIndexPropertyEqual((int)x.Tile.X, (int)x.Tile.Y, "NoPath", "Back", "T"))
                {
                    location.setTileProperty((int)x.Tile.X, (int)x.Tile.Y, "Back", "NoPath", "T");

                    if (!TilePropertyChanged.ContainsKey(location))
                    {
                        TilePropertyChanged[location] = new List<Vector2>();
                    }
                    TilePropertyChanged[location].Add(x.Tile);
                }
            }

            foreach (var y in location.Objects)
            {
                foreach (var z in y.Values)
                {
                    if (z.isPassable()) continue;
                    if (!location.doesEitherTileOrTileIndexPropertyEqual((int)z.TileLocation.X, (int)z.TileLocation.Y, "NoPath", "Back", "T"))
                    {
                        location.setTileProperty((int)z.TileLocation.X, (int)z.TileLocation.Y, "Back", "NoPath", "T");

                        if (!TilePropertyChanged.ContainsKey(location))
                        {
                            TilePropertyChanged[location] = new List<Vector2>();
                        }
                        TilePropertyChanged[location].Add(z.TileLocation);
                    }
                }
            }

            foreach (var z in location.furniture)
            {
                if (z.isPassable()) continue;
                Vector2 tableTileLocation = z.TileLocation;

                int tableWidth = z.getTilesWide();
                int tableHeight = z.getTilesHigh();

                for (int x = 0; x < tableWidth; x++)
                {
                    for (int y = 0; y < tableHeight; y++)
                    {
                        if (!location.doesEitherTileOrTileIndexPropertyEqual((int)tableTileLocation.X + x, (int)tableTileLocation.Y + y, "NoPath", "Back", "T"))
                        {
                            location.setTileProperty((int)tableTileLocation.X + x, (int)tableTileLocation.Y + y, "Back", "NoPath", "T");

                            if (!TilePropertyChanged.ContainsKey(location))
                            {
                                TilePropertyChanged[location] = new List<Vector2>();
                            }
                            TilePropertyChanged[location].Add(new Vector2((int)tableTileLocation.X + x, (int)tableTileLocation.Y + y));
                        }
                    }
                }
            }

            foreach ( var z in location.buildings )
            {
                Vector2 buildingTile = new Vector2(z.tileX.Value, z.tileY.Value) ;

                int tileWide = z.tilesWide.Value;
                int tileHigh = z.tilesHigh.Value;

                for (int x = 0; x < tileWide; x++)
                {
                    for (int y = 0; y < tileHigh; y++)
                    {
                        if (!location.doesEitherTileOrTileIndexPropertyEqual((int)buildingTile.X + x, (int)buildingTile.Y + y, "NoPath", "Back", "T"))
                        {
                            location.setTileProperty((int)buildingTile.X + x, (int)buildingTile.Y + y, "Back", "NoPath", "T");

                            if (!TilePropertyChanged.ContainsKey(location))
                            {
                                TilePropertyChanged[location] = new List<Vector2>();
                            }
                            TilePropertyChanged[location].Add(new Vector2((int)buildingTile.X + x, (int)buildingTile.Y + y));
                        }
                    }
                }
            }
        }

        public static void NpcOneSecondUpdate()
        {
            if (!Game1.hasLoadedGame) return;
            Random random = new Random();

            if (Config.AdvanceDebug)
            {
                try
                {
                    PossibleMarketLocation.Clear();
                    foreach (var i in Game1.locations.Where(i => (i.IsOutdoors || !i.IsOutdoors && Config.AllowIndoorStore) && i.characters.Count > 0 && i.characters.Any(c => c.IsVillager && c.getMasterScheduleRawData() != null)))
                        PossibleMarketLocation.Add(i.NameOrUniqueName);

                    foreach (var i in ValidBuildingObjectPairs)
                        PossibleMarketLocation.Add(i.Building.GetIndoorsName());

                    foreach (var i in Game1.getLocationFromName("Custom_MT_Island").buildings)
                        PossibleMarketLocation.Add(i.GetIndoorsName());
                    PossibleMarketLocation.Add("Custom_MT_Island_House");

                    if (Game1.player.currentLocation != null && !PossibleMarketLocation.Contains(Game1.player.currentLocation.NameOrUniqueName))
                        PossibleMarketLocation.Add(Game1.player.currentLocation.NameOrUniqueName);
                }
                catch (Exception ex) { SMonitor.Log("Error when getting location:" + ex.Message, LogLevel.Error); }
            }

            // check for message and valid food
            foreach (var name in PossibleMarketLocation)
            {
                var location = Game1.getLocationFromName(name);
                if (location == null) continue;

                foreach (NPC npc in location.characters.Where(i => i.IsVillager && i.getMasterScheduleRawData() != null && i.getMasterScheduleRawData().Count > 0 
                                && !i.Name.Contains("Chicken") && !i.Name.Contains("Cow")))
                {
                    double talkChance = 0.05;
                    int localNpcCount = location.characters.Count() + 1;

                    //Send bubble about decoration, dish of the week
                    if (location == Game1.player.currentLocation && random.NextDouble() < talkChance / localNpcCount * 3
                        && Utility.isThereAFarmerWithinDistance(new Vector2(npc.Tile.X, npc.Tile.Y), 20, npc.currentLocation) != null
                        && npc != Game1.player.getSpouse() && Config.EnableDecor && !Config.DisableChatAll)
                    {

                        double chance = random.NextDouble();

                        if (chance < 0.04 && WantsToSay(npc, 360) && GetClosestFood(npc, location) is DataPlacedFood food && food != null)                    //If have item for sale
                        {
                            var decorPointComment = GetDecorPoint(food.foodTile, npc.currentLocation);
                            string decorRate = decorPointComment >= 0.2 ? "absolute amazing decoration" : decorPointComment <= 0 ? "really bad decoration choice" : "just normal decoration";

                            //Send decorPoint message
                            if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                            {
                                string ageCategory = npc.Age == 0 ? "adult" : npc.Age == 1 ? "teens" : "child";
                                string manner = npc.Manners == 0 ? "friendly." : npc.Manners == 1 ? "polite." : npc.Manners == 2 ? "rude." : "friendly.";
                                Task.Run(() => SendMessageToAssistant(
                                    npc: npc,
                                    userMessage: $"",
                                    systemMessage: $"As NPC {npc.Name} ({ageCategory}, {manner}) in Stardew Valley, you will share your comment that the store has {decorRate} and wonder if that say something about the quality of product, using under 20 text words")
                                );
                            }
                            else
                            {
                                if (decorPointComment >= 0.2)
                                {
                                    NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.gooddecor.{random.Next(5)}"));
                                }
                                else if (decorPointComment <= 0)
                                {
                                    NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.baddecor.{random.Next(5)}"));
                                }
                            }
                        }
                        else if (chance < 0.12 && WantsToSay(npc, 300) 
                            && (location is FarmHouse || location == Game1.getFarm() || location.Name.Contains("Custom_MT_Island")))      //if in FarmHouse and have no item for sale
                        {
                            var decorPointComment = GetDecorPoint(npc.Tile, npc.currentLocation, 12);
                            string decorRate = decorPointComment >= 0.2 ? "the best decoration" : decorPointComment <= 0 ? "terrible decoration" : "very normal simple decoration";

                            //Send decorPoint message
                            if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                            {
                                string ageCategory = npc.Age == 0 ? "adult" : npc.Age == 1 ? "teens" : "child";
                                string manner = npc.Manners == 0 ? "friendly." : npc.Manners == 1 ? "polite." : npc.Manners == 2 ? "rude." : "friendly.";
                                Task.Run(() => SendMessageToAssistant(
                                    npc: npc,
                                    userMessage: $"",
                                    systemMessage: $"As NPC {npc.Name} ({ageCategory}, {manner}) in Stardew Valley, you will share your comment that this place has {decorRate}, using under 23 text words")
                                );
                            }
                            else
                            {
                                if (decorPointComment >= 0.2)
                                {
                                    NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.generalgooddecor.{random.Next(20)}", new { playerName = Game1.player.displayName }));
                                }
                                else if (decorPointComment > 0)
                                {
                                    NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.generalnormaldecor.{random.Next(20)}", new { playerName = Game1.player.displayName }));
                                }
                                else if (decorPointComment <= 0)
                                {
                                    NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.generalbaddecor.{random.Next(20)}", new { playerName = Game1.player.displayName }));
                                }
                            }
                        }
                        else if ((location is FarmHouse || location == Game1.getFarm() || location.Name.Contains("Custom_MT_Island")
                                || location.GetParentLocation() != null && (location.GetParentLocation().Name.Contains("Custom_MT_Island") || location.GetParentLocation() == Game1.getFarm()))
                                && WantsToSay(npc, 130))
                        {
                            FindSomething(npc);
                        }
                        else if (random.NextDouble() < (talkChance / localNpcCount) && WantsToSay(npc, 400))            //Send Dish of Week message
                        {
                            NPCShowTextAboveHead(npc, SHelper.Translation.Get($"foodstore.dishweek.{random.Next(5)}", new { dishWeek = ItemRegistry.Create<Object>(WeeklyFeatureDish, allowNull:true)?.DisplayName ?? "" }));
                        }
                    }

                    // **************************** Control NPC walking to the food ****************************
                    string text = "";
                    if (npc != null && npc.queuedSchedulePaths.Count == 0 && Game1.IsMasterGame && Game1.timeOfDay < 2530 
                        && location.furniture.Where(i => i != null && (i.furniture_type.Value == 5 || i.furniture_type.Value == 9 || i.furniture_type.Value == 11) && i.heldObject.Value != null).Any()
                        && (!npc.modData.ContainsKey("hapyke.FoodStore/shopOwnerToday") || npc.modData["hapyke.FoodStore/shopOwnerToday"] == "-1,-1")
                        && (!IsFestivalIsCurrent || location.Name != "Custom_MT_Island"))
                    {
                        double moveToFoodChance = Config.MoveToFoodChance;
                        try
                        {
                            if (npc.currentLocation.Name == "Custom_MT_Island" || npc.currentLocation.GetParentLocation() != null && npc.currentLocation.GetParentLocation().Name == "Custom_MT_Island")
                                moveToFoodChance = Config.MoveToFoodChance * 2;
                            else if (npc.currentLocation.Name == "Custom_MT_Island_House") 
                                moveToFoodChance = Config.MoveToFoodChance * 3;
                            else if (npc.currentLocation.GetParentLocation() != null && npc.currentLocation.GetParentLocation() is Farm) 
                                moveToFoodChance = Config.ShedMoveToFoodChance;

                            if (Config.UltimateChallenge)
                                moveToFoodChance *= 2;

                            if (Config.RushHour && ((800 < Game1.timeOfDay && Game1.timeOfDay < 930) || (1200 < Game1.timeOfDay && Game1.timeOfDay < 1300) || (1800 < Game1.timeOfDay && Game1.timeOfDay < 2000)))
                                moveToFoodChance = moveToFoodChance * 1.5;
                        }
                        catch { }

                        try
                        {
                            if (npc != null && WantsToEat(npc) && Game1.random.NextDouble() < moveToFoodChance)
                            {
                                DataPlacedFood food = GetClosestFood(npc, location);

                                if (food == null || (!Config.AllowRemoveNonFood && food.foodObject.Edibility <= 0 && (npc.currentLocation == Game1.getFarm() || npc.currentLocation is FarmHouse)))
                                    continue;
                                
                                if (TryToEatFood(npc, food))
                                    continue;

                                Vector2 possibleLocation;
                                possibleLocation = food.foodTile;
                                int tries = 0;
                                int facingDirection = -3;

                                while (tries < 3)
                                {
                                    int xMove = Game1.random.Next(-1, 2);
                                    int yMove = Game1.random.Next(-1, 2);

                                    possibleLocation.X += xMove;
                                    if (xMove == 0)
                                    {
                                        possibleLocation.Y += yMove;
                                    }
                                    if (xMove == -1)
                                    {
                                        facingDirection = 1;
                                    }
                                    else if (xMove == 1)
                                    {
                                        facingDirection = 3;
                                    }
                                    else if (yMove == -1)
                                    {
                                        facingDirection = 2;
                                    }
                                    else if (yMove == 1)
                                    {
                                        facingDirection = 0;
                                    }
                                    if (!location.IsTileBlockedBy(possibleLocation, ignorePassables: CollisionMask.Flooring | CollisionMask.Furniture))
                                    {
                                        break;
                                    }
                                    tries++;
                                }
                                if (tries < 3 && TimeDelayCheck(npc))
                                {
                                    //Send message
                                    if (npc.currentLocation.Name != "Farm" && npc.currentLocation.Name != "FarmHouse" && Config.ExtraMessage && !Config.DisableChat && !Config.DisableChatAll)
                                    {
                                        text = SHelper.Translation.Get($"foodstore.coming.{random.Next(15)}", new { vName = npc.displayName });

                                        Game1.chatBox.addInfoMessage(text);
                                        MyMessage messageToSend = new MyMessage(text);
                                        SHelper.Multiplayer.SendMessage(messageToSend, "ExampleMessageType");
                                    }

                                    npc.modData["hapyke.FoodStore/LastCheck"] = Game1.timeOfDay.ToString();
                                    npc.modData["hapyke.FoodStore/gettingFood"] = "true";

                                    //Villager control

                                    var npcWalk = FarmOutside.AddNewPointToSchedule(npc, ConvertToHour(Game1.timeOfDay + 10).ToString(), location.NameOrUniqueName,
                                        possibleLocation.X.ToString(), possibleLocation.Y.ToString(), facingDirection.ToString());
                                    npc.addedSpeed = 2;
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        /// <summary>Warp NPC back to their schedule and reset if found</summary>
        public static void ResetNpcSchedule (NPC npc)
        {
            if (npc == null) return;

            CleanNpc(npc);
            npc.reloadDefaultLocation();
            npc.TryLoadSchedule();
            var schedule = npc.Schedule;
            CleanNpc(npc);


            var lastLocation = npc.DefaultMap;
            var lastPosition = npc.DefaultPosition / 64;
            var lastFacing = npc.DefaultFacingDirection;

            if (schedule == null) Game1.warpCharacter(npc, lastLocation, lastPosition);
            else
            {
                foreach (var piece in schedule)
                {
                    if (piece.Key > Game1.timeOfDay)
                    {
                        Game1.warpCharacter(npc, lastLocation, lastPosition);
                        npc.faceDirection(lastFacing);
                        npc.TryLoadSchedule();

                        Console.WriteLine($"Warped {npc.Name} to {lastLocation} at {lastPosition}");
                    }

                    lastLocation = piece.Value.targetLocationName;
                    lastPosition = piece.Value.targetTile.ToVector2();
                    lastFacing = piece.Value.facingDirection;
                }

                Game1.warpCharacter(npc, lastLocation, lastPosition);
                npc.faceDirection(lastFacing);
                npc.TryLoadSchedule();

                Console.WriteLine($"Warped {npc.Name} to {lastLocation} at {lastPosition}");
            }
        }

        public static void TenMinuteUpdateIslandNpc (NPC __instance, GameLocation __instanceLocation)
        {
            if (!Game1.IsMasterGame) return;
            Random random = new Random();

            // Reset stuck NPC
            if (__instance.temporaryController != null || __instance.controller != null || __instance.isMoving()
                || __instance.TilePoint != __instance.previousEndPoint || __instance.queuedSchedulePaths != null || __instance.queuedSchedulePaths.Count > 0)
                CleanNpc(__instance);

            double nextVisitChance = random.NextDouble();

            float islandChance = 1 - Config.VisitChanceIslandHouse - Config.VisitChanceIslandBuilding;
            float islandHouseChance = 1 - Config.VisitChanceIslandBuilding;

            if (__instanceLocation.GetWeather().Weather.ToLower() == "rain") islandChance /= 1.5f;
            else if (__instanceLocation.GetWeather().Weather.ToLower() == "storm") islandChance /= 2f;
            else if (__instanceLocation.GetWeather().Weather.ToLower() == "snow") islandChance /= 1.5f;
            else if (__instanceLocation.GetWeather().Weather.ToLower() == "greenrain") islandChance /= 3f;

            // if npc is on the island and not shop owner
            if (__instanceLocation.Name == "Custom_MT_Island" && __instance.modData["hapyke.FoodStore/shopOwnerToday"] == "-1,-1")
            {
                if (nextVisitChance < islandChance || Game1.timeOfDay > 2200) // walk around on the island or try to go home
                {
                    if (Game1.timeOfDay < 2300)
                    {
                        var randomTile = FarmOutside.getRandomOpenPointInLocation(__instance, __instanceLocation, false).ToVector2();
                        if (randomTile != Vector2.Zero)
                        {
                            FarmOutside.AddNextMoveSchedulePoint(__instance, $"{ConvertToHour(Game1.timeOfDay + 10)}", $"{__instanceLocation.NameOrUniqueName}",
                                $"{randomTile.X}", $"{randomTile.Y}", $"{random.Next(0, 4)}");
                        }
                    }
                    else if (Game1.timeOfDay > 2330 && Game1.timeOfDay < 2530)
                    {
                        __instance.reloadDefaultLocation();
                        if (!__instance.DefaultMap.Contains("HiddenWarp") && !__instance.DefaultMap.Contains("WarpRoom") )
                        {
                            FarmOutside.AddNextMoveSchedulePoint(__instance, $"{ConvertToHour(Game1.timeOfDay + 10)}", $"{__instance.DefaultMap}",
                                $"{__instance.DefaultPosition.X / 64}", $"{__instance.DefaultPosition.Y / 64}", $"{__instance.DefaultFacingDirection}");

                            __instance.modData.Remove(orderKey);
                        }

                    }
                }
                else if (nextVisitChance < islandHouseChance && Game1.timeOfDay >= 700 && Game1.timeOfDay <= 2200) // visit island house
                {
                    var randomTile = FarmOutside.getRandomOpenPointInLocation(__instance, Game1.getLocationFromName("Custom_MT_Island_House"), false).ToVector2();
                    if (randomTile != Vector2.Zero)
                    {
                        FarmOutside.AddNextMoveSchedulePoint(__instance, $"{ConvertToHour(Game1.timeOfDay + 10)}", $"{Game1.getLocationFromName("Custom_MT_Island_House").NameOrUniqueName}",
                            $"{randomTile.X}", $"{randomTile.Y}", $"{random.Next(0, 4)}");

                        __instance.modData.Remove(orderKey);
                    }
                }
                else if (Game1.timeOfDay >= 700 && Game1.timeOfDay <= 2200) // visit a selected building
                {
                    var selectedBuilding = IslandValidBuilding[random.Next(IslandValidBuilding.Count)].buildingLocation;
                    if (selectedBuilding != null && selectedBuilding != "Custom_MT_Island_House")
                    {
                        var randomTile = FarmOutside.getRandomOpenPointInLocation(__instance, Game1.getLocationFromName(selectedBuilding), false).ToVector2();
                        if (randomTile != Vector2.Zero)
                        {
                            FarmOutside.AddNextMoveSchedulePoint(__instance, $"{ConvertToHour(Game1.timeOfDay + 10)}", $"{Game1.getLocationFromName(selectedBuilding).NameOrUniqueName}",
                                $"{randomTile.X}", $"{randomTile.Y}", $"{random.Next(0, 4)}");

                            __instance.modData.Remove(orderKey);
                        }
                    }
                }
            }
            // if npc is inside island house
            else if (__instanceLocation.Name == "Custom_MT_Island_House")
            {
                // go back to island
                if (nextVisitChance < 0.2 && Game1.timeOfDay > Int32.Parse(__instance.modData["hapyke.FoodStore/islandSpecialOrderTime"]) + 30
                    || Game1.timeOfDay > 2200)
                {
                    var randomTile = FarmOutside.getRandomOpenPointInLocation(__instance, Game1.getLocationFromName("Custom_MT_Island"), false).ToVector2();
                    if (randomTile != Vector2.Zero)
                    {
                        FarmOutside.AddNextMoveSchedulePoint(__instance, $"{ConvertToHour(Game1.timeOfDay + 10)}", $"{Game1.getLocationFromName("Custom_MT_Island").NameOrUniqueName}",
                            $"{randomTile.X}", $"{randomTile.Y}", $"{random.Next(0, 4)}");

                        string[] specialOrderCoor = __instance.modData["hapyke.FoodStore/specialOrder"].Split(',');
                        Vector2 specialOrderTile = new Vector2(int.Parse(specialOrderCoor[0]), int.Parse(specialOrderCoor[1]));

                        if (RestaurantSpot.ContainsKey(__instanceLocation))
                        {
                            var tileList = RestaurantSpot[__instanceLocation];
                            if (tileList.Contains(specialOrderTile)) tileList.Remove(specialOrderTile);
                        }

                        __instance.modData.Remove(orderKey);
                    }
                }
                // stay in the house
                else if (Game1.timeOfDay > Int32.Parse(__instance.modData["hapyke.FoodStore/islandSpecialOrderTime"]) + 130)
                {
                    // make special order
                    if (nextVisitChance < 0.5 && Game1.timeOfDay < 2100 && Game1.timeOfDay > Int32.Parse(__instance.modData["hapyke.FoodStore/islandSpecialOrderTime"]) + 300)
                    {
                        Dictionary<Vector2, int> surroundingTiles = new Dictionary<Vector2, int>();

                        foreach (var table in __instanceLocation.furniture.Where(i => i.furniture_type.Value == 11))
                        {
                            if (table != null && table.heldObject.Value != null && table.heldObject.Value.QualifiedItemId == "(F)MT.Objects.RestaurantDecor")
                            {
                                Vector2 topLeft = table.TileLocation;
                                int width = table.getTilesWide();
                                int height = table.getTilesHigh();

                                for (int x = 0; x < width; x++)
                                {
                                    var tile = new Vector2(topLeft.X + x, topLeft.Y - 1); // down
                                    _ = surroundingTiles.ContainsKey(tile) ? false : surroundingTiles.TryAdd(tile, 2);
                                }

                                for (int x = 0; x < width; x++)
                                {
                                    var tile = new Vector2(topLeft.X + x, topLeft.Y + height); // up
                                    _ = surroundingTiles.ContainsKey(tile) ? false : surroundingTiles.TryAdd(tile, 0);
                                }

                                for (int y = 0; y < height; y++)
                                {
                                    var tile = new Vector2(topLeft.X - 1, topLeft.Y + y); // right
                                    _ = surroundingTiles.ContainsKey(tile) ? false : surroundingTiles.TryAdd(tile, 1);
                                }

                                for (int y = 0; y < height; y++)
                                {
                                    var tile = new Vector2(topLeft.X + width, topLeft.Y + y); // left
                                    _ = surroundingTiles.ContainsKey(tile) ? false : surroundingTiles.TryAdd(tile, 3);
                                }
                            }
                        }

                        int totalTile = surroundingTiles.Count;
                        while (totalTile > 0 && totalTile > 0)
                        {
                            totalTile--;
                            var randomPair = surroundingTiles.ElementAt(random.Next(surroundingTiles.Count));

                            var randomTile = randomPair.Key;
                            var randomDirection = randomPair.Value;


                            if (__instance.currentLocation.CanSpawnCharacterHere(randomTile) && (!RestaurantSpot.ContainsKey(__instance.currentLocation) || !RestaurantSpot[__instance.currentLocation].Contains(randomTile)))
                            {
                                FarmOutside.AddNextMoveSchedulePoint(__instance, $"{ConvertToHour(Game1.timeOfDay + 10)}", $"{__instance.currentLocation.NameOrUniqueName}", $"{randomTile.X}", $"{randomTile.Y}", $"{randomDirection}");
                                __instance.modData["hapyke.FoodStore/specialOrder"] = $"{randomTile.X},{randomTile.Y}";

                                if (!RestaurantSpot.ContainsKey(__instance.currentLocation)) RestaurantSpot[__instance.currentLocation] = new List<Vector2>();
                                RestaurantSpot[__instance.currentLocation].Add(randomTile);

                                __instance.modData.Remove(orderKey);

                                __instance.modData["hapyke.FoodStore/islandSpecialOrderTile"] = $"{randomTile.X},{randomTile.Y}";
                                __instance.modData["hapyke.FoodStore/islandSpecialOrderTime"] = Game1.timeOfDay.ToString();

                                Instance.CheckOrder(__instance, __instance.currentLocation, true);

                                break;
                            }
                        }
                    }
                    // walk around if no order
                    else if (Game1.timeOfDay > Int32.Parse(__instance.modData["hapyke.FoodStore/islandSpecialOrderTime"]) + 30)
                    {
                        var randomTile = FarmOutside.getRandomOpenPointInLocation(__instance, __instanceLocation, false).ToVector2();
                        if (randomTile != Vector2.Zero)
                        {
                            FarmOutside.AddNextMoveSchedulePoint(__instance, $"{ConvertToHour(Game1.timeOfDay + 10)}", $"{__instanceLocation.NameOrUniqueName}",
                                $"{randomTile.X}", $"{randomTile.Y}", $"{random.Next(0, 4)}");

                            string[] specialOrderCoor = __instance.modData["hapyke.FoodStore/specialOrder"].Split(',');
                            Vector2 specialOrderTile = new Vector2(int.Parse(specialOrderCoor[0]), int.Parse(specialOrderCoor[1]));

                            if (RestaurantSpot.ContainsKey(__instanceLocation))
                            {
                                var tileList = RestaurantSpot[__instanceLocation];
                                if (tileList.Contains(specialOrderTile)) tileList.Remove(specialOrderTile);
                            }

                            __instance.modData.Remove(orderKey);
                        }
                    }
                }
            }
            // if npc is in a building
            else if (__instanceLocation.parentLocationName.Value == "Custom_MT_Island")
            {
                if (Game1.player.currentLocation != __instanceLocation && (nextVisitChance < 0.2 || Game1.timeOfDay > 2200))
                {
                    IslandBuildingProperties matchingBuilding = IslandValidBuilding.FirstOrDefault(building => building.buildingLocation == __instanceLocation.NameOrUniqueName);
                    if (matchingBuilding != null)
                    {
                        Game1.warpCharacter(__instance, Game1.getLocationFromName("Custom_MT_Island"), matchingBuilding.outdoorDoor);
                        CleanNpc(__instance);
                        var randomTile = FarmOutside.getRandomOpenPointInLocation(__instance, Game1.getLocationFromName("Custom_MT_Island"), false).ToVector2();
                        if (randomTile != Vector2.Zero)
                        {
                            FarmOutside.AddNextMoveSchedulePoint(__instance, $"{ConvertToHour(Game1.timeOfDay + 10)}", $"{Game1.getLocationFromName("Custom_MT_Island").NameOrUniqueName}",
                                $"{randomTile.X}", $"{randomTile.Y}", $"{random.Next(0, 4)}");
                        }
                    }
                }
                else if (Game1.player.currentLocation == __instanceLocation)
                {
                    var randomTile = FarmOutside.getRandomOpenPointInLocation(__instance, __instanceLocation, false).ToVector2();
                    if (randomTile != Vector2.Zero)
                    {
                        FarmOutside.AddNextMoveSchedulePoint(__instance, $"{ConvertToHour(Game1.timeOfDay + 10)}", $"{__instanceLocation.NameOrUniqueName}",
                            $"{randomTile.X}", $"{randomTile.Y}", $"{random.Next(0, 4)}");
                    }
                }
            }
        }


        /// <summary>Call to OpenAI to generate a response</summary>
        public static async Task SendMessageToAssistant(NPC npc, string userMessage, string systemMessage, bool isConversation = false, bool isForBubbleMessage = true)
        {
            if (AILimitCount >= AILimitBlock) return;
            if (!Config.AdvanceDebug) AILimitCount++;

            string k0 = "sk-proj-5ZtgwwTdLDxtQVijgYcGge9LMWn81SfX9tu8IncYl-";
            string k1 = "IkSdH3qma7vBGiDgKmUEN0yh4iSVxN4UT3BlbkFJKL3VPfX5QfAhTlo8SziTERMlCjiizsk1z1s_";
            string k2 = "KBVrA88aNDKTezkGiz5V0Qfx0CZKY8IVwvR3UA";

            if (Config.AdvanceAiLanguage != "English") systemMessage += $".Use {Config.AdvanceAiLanguage} language";

            string responseMessage = "";
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AIKey1 + AIKey2 + AIKey3);
                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new { role = "system", content = systemMessage},
                        new { role = "user", content = userMessage },
                        new { role = "assistant", content = string.Join("\n", conversationSummaries) }
                    },
                    max_tokens = 50,
                    temperature = 1.2
                };

                var jsonRequest = JsonConvert.SerializeObject(requestBody);
                var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                    dynamic json = JsonConvert.DeserializeObject(jsonResponse);
                    responseMessage = json.choices[0].message.content.ToString();

                    SMonitor.Log($"Assistant Response: {json.choices[0].message.content.ToString()}\n{json.usage}\n", LogLevel.Trace);

                    if (isForBubbleMessage) NPCShowTextAboveHead(npc, responseMessage, true);
                    else npc.CurrentDialogue.Push(new Dialogue(npc, "key", responseMessage));


                    // --------------------------
                    // conversationSummaries.Clear();
                    conversationSummaries.TryGetValue(npc.Name, out string history);

                    if (isConversation)
                    {
                        var summaryRequestBody = new
                        {
                            model = "gpt-4o-mini",
                            messages = new[]
                            {
                            new { role = "system", content = "Summarize the user's conversation in under 45 words, focusing on key details and relevant points. Remove any extraneous information and provide an empty string if there's nothing noteworthy." },
                            new { role = "user", content = $"{userMessage}, {history}" }
                        },
                            max_tokens = 50,
                            temperature = 0.7
                        };

                        var summaryRequest = JsonConvert.SerializeObject(summaryRequestBody);
                        var summaryContent = new StringContent(summaryRequest, Encoding.UTF8, "application/json");
                        var summaryResponse = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", summaryContent);

                        if (summaryResponse.IsSuccessStatusCode)
                        {
                            var summaryJsonResponse = await summaryResponse.Content.ReadAsStringAsync();
                            dynamic summaryJson = JsonConvert.DeserializeObject(summaryJsonResponse);
                            string conversationSummary = summaryJson.choices[0].message.content.ToString();

                            conversationSummaries[npc.Name] = conversationSummary;

                            //SMonitor.Log($"conver sum: {conversationSummary}\n{summaryJson.usage}\n\n", LogLevel.Warn);
                        }
                    }
                }
                else
                {
                    SMonitor.Log("Unable to retrieve response from AI content. Check for mod update first, and if it still not work then report this error!", LogLevel.Error);
                }
            }
        }
    }
}