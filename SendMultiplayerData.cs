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
                var targetModIds = new[] { this.ModManifest.UniqueID };

                MailData dataToSend = GetCurrentMailData();
                if (dataToSend != null)
                {
                    Helper.Multiplayer.SendMessage(dataToSend, "MT.MailLogUpdate", targetModIds, new[] { e.Peer.PlayerID });
                }

                Helper.Multiplayer.SendMessage($"{DailyFeatureDish}///{WeeklyFeatureDish}", "UpdateSpecialDish", targetModIds, new[] { e.Peer.PlayerID });
                Helper.Multiplayer.SendMessage(TodayFestivalIncome, "TodayFestivalIncome", targetModIds, new[] { e.Peer.PlayerID });

                Helper.Multiplayer.SendMessage(TodaySell.ToList(), "MT.TodaySellSync", targetModIds, new[] { e.Peer.PlayerID });
                Helper.Multiplayer.SendMessage(TodayMoney, "MT.TodayMoneySync", targetModIds, new[] { e.Peer.PlayerID });
                Helper.Multiplayer.SendMessage(TodayPointTaste, "MT.TodayTasteSync", targetModIds, new[] { e.Peer.PlayerID });
                Helper.Multiplayer.SendMessage(TodayPointDecor, "MT.TodayDecorSync", targetModIds, new[] { e.Peer.PlayerID });

                Helper.Multiplayer.SendMessage(GetFeedbackSnapshot(), "MT.FeedbackSync", targetModIds, new[] { e.Peer.PlayerID });

                var ShopDataLoader = StardewValley.DataLoader.Shops(Game1.content);
                Helper.Multiplayer.SendMessage(ShopDataLoader, "10MinSyncShopDataLoader", targetModIds, new[] { e.Peer.PlayerID });
            }
        }


        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (!Game1.IsMasterGame)
            {
                if (e.Type == "MT.MailLogUpdate" && e.FromModID == this.ModManifest.UniqueID)
                {
                    FarmhandSyncData = e.ReadAs<MailData>();
                }

                if (e.Type == "MT.TodaySellSync" && e.FromModID == this.ModManifest.UniqueID)
                {
                    TodaySell = e.ReadAs<List<string>>() ?? new List<string>();
                }

                if (e.Type == "MT.TodayMoneySync" && e.FromModID == this.ModManifest.UniqueID)
                {
                    TodayMoney = e.ReadAs<int>();
                }

                if (e.Type == "MT.TodayTasteSync" && e.FromModID == this.ModManifest.UniqueID)
                {
                    TodayPointTaste = e.ReadAs<float>();
                }

                if (e.Type == "MT.TodayDecorSync" && e.FromModID == this.ModManifest.UniqueID)
                {
                    TodayPointDecor = e.ReadAs<float>();
                }

                if (e.Type == "MT.FeedbackSync" && e.FromModID == this.ModManifest.UniqueID)
                {
                    ReplaceFeedbackList(e.ReadAs<List<FeedbackEntry>>());
                }

                if (e.Type == "MT.FeedbackAdd" && e.FromModID == this.ModManifest.UniqueID)
                {
                    AddFeedbackEntry(e.ReadAs<FeedbackEntry>());
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