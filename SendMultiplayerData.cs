using HarmonyLib;
using MarketTown.Data;
using Microsoft.Xna.Framework;
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
using StardewValley.GameData.FruitTrees;
using StardewValley.GameData.LocationContexts;
using StardewValley.GameData.Shops;
using StardewValley.GameData.SpecialOrders;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace MarketTown
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry
    {
        private void OnPeerConnected(object sender, PeerConnectedEventArgs e)
        {
            if (Game1.IsMasterGame)
            {
                MailData dataToSend = Helper.Data.ReadSaveData<MailData>("MT.MailLog");
                if (dataToSend != null)
                {
                    Helper.Multiplayer.SendMessage(dataToSend, "MT.MailLogUpdate", new[] { "d5a1lamdtd.MarketTown" }, new[] { e.Peer.PlayerID });
                }

                SHelper.Multiplayer.SendMessage($"{DailyFeatureDish}///{WeeklyFeatureDish}", "UpdateSpecialDish");
                //SHelper.Multiplayer.SendMessage(TodayShopInventory, "TodayShopInventory");
                SHelper.Multiplayer.SendMessage(TodayFestivalIncome, "TodayFestivalIncome");

                var ShopDataLoader = StardewValley.DataLoader.Shops(Game1.content);
                SHelper.Multiplayer.SendMessage(ShopDataLoader, "10MinSyncShopDataLoader");
            }
        }


        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (!Game1.IsMasterGame)
            {
                if (e.Type == "MT.MailLogUpdate" && e.FromModID == this.ModManifest.UniqueID)
                {
                    FarmhandSyncData = e.ReadAs<MailData>();
                    Monitor.Log($"Received data: {FarmhandSyncData}", LogLevel.Warn);
                    SHelper.Data.WriteJsonFile("markettowndata.json", FarmhandSyncData);
                }

                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "ExampleMessageType" && !Config.DisableChatAll)
                {
                    MyMessage message = e.ReadAs<MyMessage>();
                    Game1.chatBox.addInfoMessage(message.MessageContent);
                }

                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "NpcShowText" && !Config.DisableChatAll)
                {
                    string content = e.ReadAs<MyMessage>().MessageContent;
                    string[] data = content.Split("///");
                    NPC i = Game1.getCharacterFromName(data[0]);
                    if (i != null) NPCShowTextAboveHead(i, data[1], true);
                }

                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "UpdateSpecialDish" && !Config.DisableChatAll)
                {
                    string content = e.ReadAs<string>();
                    string[] data = content.Split("///");
                    DailyFeatureDish = data[0];
                    WeeklyFeatureDish = data[1];
                }

                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "10MinSyncShopDataLoader")
                {
                    var data = e.ReadAs<Dictionary<string, ShopData>>();
                    var shops = StardewValley.DataLoader.Shops(Game1.content);

                    foreach (var shop in shops)
                    {
                        if (shop.Key.StartsWith("MarketTown.") && data.ContainsKey(shop.Key))
                        {
                            var shopData = data[shop.Key];
                            shop.Value.Items.Clear();

                            foreach (var item in shopData.Items)
                            {
                                shop.Value.Items.Add(item);
                            }
                        }
                    }
                }

                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "UpdateLog")
                {
                    string content = e.ReadAs<MyMessage>().MessageContent;
                    TodaySell.Add(content);
                }

                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "UpdateTodayMoney")
                {
                    string content = e.ReadAs<MyMessage>().MessageContent;
                    TodayMoney = Int32.Parse(content);
                }

                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "UpdateTodayTaste")
                {
                    TodayPointTaste = e.ReadAs<float>();
                }
                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "UpdateTodayDecor")
                {
                    TodayPointDecor = e.ReadAs<float>();
                }

                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "TodayFestivalIncome")
                {
                    TodayFestivalIncome = e.ReadAs<int>();
                }
                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "FestivalSellLog")
                {
                    FestivalSellLog = e.ReadAs<List<string>>();
                }

            }
        }
    }
}