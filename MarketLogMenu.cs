using HarmonyLib;
using MarketTown.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Shops;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using xTile.Dimensions;

namespace MarketTown
{
    public partial class ModEntry
    {
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // Check if the player clicked on the custom furniture
            if (e.Button.IsActionButton())
            {
                var tile = Game1.currentCursorTile;
                var furniture = Game1.currentLocation.getObjectAtTile((int)tile.X, (int)tile.Y) as Furniture;

                if (furniture != null && furniture.QualifiedItemId == "(F)MT.Objects.MarketLog")
                {
                    // Show custom menu
                    Game1.activeClickableMenu = new MarketLogMenu(this.Helper);
                }
            }
        }

        /// <summary>Draw Market Log menu </summary>
        public class MarketLogMenu : IClickableMenu
        {
            private MailData model;
            private readonly IModHelper helper;
            private readonly List<string[]> dataLines;
            private int currentScrollIndex;
            private int maxVisibleLines = Config.AdvanceMenuRow;
            private int lineHeight = Config.AdvanceMenuSpace;
            private Dictionary<string, string> tooltips;

            public MarketLogMenu(IModHelper helper)
                : base((int)Utility.getTopLeftPositionForCenteringOnScreen(Config.AdvanceMenuOffsetX, Config.AdvanceMenuOffsetY).X, (int)Utility.getTopLeftPositionForCenteringOnScreen(Config.AdvanceMenuOffsetX, Config.AdvanceMenuOffsetY).Y,
                      Config.AdvanceMenuWidth, Config.AdvanceMenuHeight, showUpperRightCloseButton: true)
            {
                this.helper = helper;
                this.dataLines = new List<string[]>();
                this.tooltips = new Dictionary<string, string>();
                LoadData();
            }

            private void LoadData()
            {
                if (Game1.IsMasterGame) model = helper.Data.ReadSaveData<MailData>("MT.MailLog") ?? new MailData();
                else model = SHelper.Data.ReadJsonFile<MailData>("markettowndata.json") ?? new MailData();

                if (model == null)
                {
                    dataLines.Add(new string[] { "Cannot find save data.", "", $"", "" });
                }
                else
                {
                    dataLines.Add(new string[] { "Total market earning:", "", $"    {model.TotalEarning + TodayMoney}G", "" });
                    dataLines.Add(new string[] { "Total festival earning:", "", $"    {model.TotalFestivalIncome + TodayFestivalIncome}G", "" });
                    dataLines.Add(new string[] { "Reviews:", $"Total:{model.TotalCustomerNote + TodayCustomerNote}", $"  Positive: {model.TotalCustomerNoteYes + TodayCustomerNoteYes}", $"  Negative: {model.TotalCustomerNoteNo + TodayCustomerNoteNo}" });

                    dataLines.Add(new string[] { "", "", "", "" });
                    dataLines.Add(new string[] { "---------------------------------------------------------------------------------", "", "", "" });
                    dataLines.Add(new string[] { "Category", "Total", "Restaurant", "Market Town" });
                    dataLines.Add(new string[] { "---------------------------------------------------------------------------------", "", "", "" });
                    dataLines.Add(new string[] { "Forage", $"{model.TotalForageSold + TodayForageSold}", "-", $"{model.TotalForageSold + TodayForageSold} / 30" });
                    dataLines.Add(new string[] { "Flower", $"{model.TotalFlowerSold + TodayFlowerSold}", "-", $"{model.TotalFlowerSold + TodayFlowerSold} / 30" });
                    dataLines.Add(new string[] { "Fruit", $"{model.TotalFruitSold + TodayFruitSold}", "-", $"{model.TotalFruitSold + TodayFruitSold} / 30" });
                    dataLines.Add(new string[] { "Vegetable", $"{model.TotalVegetableSold + TodayVegetableSold}", "-", $"{model.TotalVegetableSold + TodayVegetableSold} / 30" });
                    dataLines.Add(new string[] { "Artisan Good", $"{model.TotalArtisanGoodSold + TodayArtisanGoodSold}", "-", $"{model.TotalArtisanGoodSold + TodayArtisanGoodSold} / 30" });
                    dataLines.Add(new string[] { "Cooking", $"{model.TotalCookingSold + TodayCookingSold}", $"{model.TotalCookingSold + TodayCookingSold} / 20", $"{model.TotalCookingSold + TodayCookingSold} / 30" });
                    dataLines.Add(new string[] { "Fish", $"{model.TotalFishSold + TodayFishSold}", "-", $"{model.TotalFishSold + TodayFishSold} / 30" });
                    dataLines.Add(new string[] { "Animal Product", $"{model.TotalAnimalProductSold + TodayAnimalProductSold}", "-", $"{model.TotalAnimalProductSold + TodayAnimalProductSold} / 30" });
                    dataLines.Add(new string[] { "Gem & Mineral", $"{model.TotalMineralSold + TodayMineralSold}", "-", $"{model.TotalMineralSold + TodayMineralSold} / 30" });
                    dataLines.Add(new string[] { "Clothes", $"{model.TotalClothesSold + TodayClothesSold}", "-", "-" });
                    dataLines.Add(new string[] { "Seed", $"{model.TotalSeedSold + TodaySeedSold}", "-", "-" });
                    dataLines.Add(new string[] { "Monster Loot", $"{model.TotalMonsterLootSold + TodayMonsterLootSold}", "-", "-" });
                    dataLines.Add(new string[] { "Resource", $"{model.TotalResourceMetalSold + TodayResourceMetalSold}", "-", "-" });
                    dataLines.Add(new string[] { "Crafting", $"{model.TotalCraftingSold + TodayCraftingSold}", "-", "-" });
                    dataLines.Add(new string[] { "---------------------------------------------------------------------------------", "", "", "" });

                    int totalAllCategory = model.TotalForageSold
                                         + model.TotalFlowerSold
                                         + model.TotalFruitSold
                                         + model.TotalVegetableSold
                                         + model.TotalSeedSold
                                         + model.TotalMonsterLootSold
                                         + model.TotalArtisanGoodSold
                                         + model.TotalAnimalProductSold
                                         + model.TotalResourceMetalSold
                                         + model.TotalMineralSold
                                         + model.TotalCraftingSold
                                         + model.TotalCookingSold
                                         + model.TotalFishSold
                                         + model.TotalClothesSold
                                         + TodayForageSold
                                         + TodayFlowerSold
                                         + TodayFruitSold
                                         + TodayVegetableSold
                                         + TodaySeedSold
                                         + TodayMonsterLootSold
                                         + TodayArtisanGoodSold
                                         + TodayAnimalProductSold
                                         + TodayResourceMetalSold
                                         + TodayMineralSold
                                         + TodayCraftingSold
                                         + TodayCookingSold
                                         + TodayFishSold
                                         + TodayClothesSold;

                    dataLines.Add(new string[] { "Total", $"{totalAllCategory}", "", "" });
                    dataLines.Add(new string[] { $"Satisfaction score", $"Aesthetic Rating" });
                    dataLines.Add(new string[] { "---------------------------------------------------------------------------------", "", "", "" });

                    dataLines.Add(new string[] { "Museum visitors:", "", $"{model.TodayMuseumVisitor + TodayMuseumVisitor}", "" });
                    dataLines.Add(new string[] { "Paradise Island visitors:", "", $"{model.TotalVisitorVisited + TodayVisitorVisited}", "" });
                    dataLines.Add(new string[] { "Friends invited:", "", $"{model.TotalFriendVisited}", "" });
                    dataLines.Add(new string[] { "---------------------------------------------------------------------------------", "", "", "" });

                    string level = "Mysterious Stalls";
                    string nextLevel = "";
                    switch (MarketRecognition())
                    {
                        case 0:
                            level = "Mysterious Stalls";
                            nextLevel = "Day played > 14; Customer Note > 20; Feedback > 70%";
                            break;
                        case 1:
                            level = "Humble Stand";
                            nextLevel = "Day played > 28; Customer Note > 40; Feedback > 75%";
                            break;
                        case 2:
                            level = "Emerging Bazaar";
                            nextLevel = "Day played > 42; Customer Note > 70; Feedback > 80%";
                            break;
                        case 3:
                            level = "Popular Marketplace";
                            nextLevel = "Day played > 56; Customer Note > 100; Feedback > 85%";
                            break;
                        case 4:
                            level = "Trusted Farmer";
                            nextLevel = "Day played > 84; Customer Note > 150; Feedback > 90%";
                            break;
                        case 5:
                            level = "Prestigious Market";
                            break;
                    }

                    dataLines.Add(new string[] { "Market recognition:", $"{level}", "", "" }); // 26
                    dataLines.Add(new string[] { "Paradise Island Progress:", "", "", "" });

                    dataLines.Add(new string[] { "", "", "", "" });
                    dataLines.Add(new string[] { $"      Yesterday: {model.SellMoney}G", "", $"    Today: {TodayMoney}G", "" });
                    dataLines.Add(new string[] { "   Today log:", "", "", "" });
                    foreach (var line in TodaySell)
                    {
                        dataLines.Add(new string[] { $"{line}", "", "", "" });
                    }

                    // Populate tooltips
                    tooltips.Add("Total market earning:", "Total earning though selling items");
                    tooltips.Add("Total festival earning:", "Total earning during Paradise Island's festival day");
                    tooltips.Add("Reviews:", "Total number of 'Customer Note' given.");
                    tooltips.Add("Category", "Total items sold in each category and progress to unlock License");
                    tooltips.Add("Forage", "e.g. Leek, Spring Onion, Dandelion,...");
                    tooltips.Add("Flower", "e.g. Tulip, Sunflower, Poppy, ...");
                    tooltips.Add("Fruit", "e.g. Apple, Blackberry, Crystalfruit, ...");
                    tooltips.Add("Vegetable", "e.g. Carrot, Parsnip, Tomato, ...");
                    tooltips.Add("Artisan Good", "e.g. Wine, Honey, Cheese, ...");
                    tooltips.Add("Cooking", "e.g. Salad, Baked Fish, Cookie, ...");
                    tooltips.Add("Fish", "e.g. Squid, Carp, Pike, ...");
                    tooltips.Add("Animal Product", "e.g. Egg, Milk, Truffle, ...");
                    tooltips.Add("Gem & Mineral", "e.g. Ruby, Quartz, Sandstone, ...");
                    tooltips.Add("Clothes", "Hats, Shirts, Pants, Boots");
                    tooltips.Add("Seed", "umm... seeds");
                    tooltips.Add("Monster Loot", "e.g. Slime, Bat Wings, Bug Meat, ...");
                    tooltips.Add("Resource", "e.g. Gold bar, Batterry, Hardwood, ...");
                    tooltips.Add("Crafting", "e.g. Fences, Sprinker, Paths");

                    tooltips.Add("Satisfaction score", $"How much customer like the item ( taste and quality ). Current: {((model.TotalPointTaste + TodayPointTaste) / totalAllCategory):F2}\n" +
                                                       $"How well is the decoration in the market. Current: {((model.TotalPointDecor + TodayPointDecor) / totalAllCategory):F2}");

                    tooltips.Add("Museum visitors:", "Number of visitors to building with Museum License");
                    tooltips.Add("Paradise Island visitors:", "Number of visitors to Paradise Island");
                    tooltips.Add("Friends invited:", "Total friends accepted 'Invite Letter'");

                    var x = (model.TotalCustomerNote + TodayCustomerNote) == 0 ? 0 : ((float)(100 * (model.TotalCustomerNoteYes + TodayCustomerNoteYes) / (model.TotalCustomerNote + TodayCustomerNote)));
                    tooltips.Add("Market recognition:", $"Next level: {nextLevel}." +
                        $"\nCurrent customer positive feedback: {x}%");
                    tooltips.Add("Paradise Island Progress:", $"Grange earning: {model.TotalFestivalIncome + TodayFestivalIncome}G / {Config.GrangeSellProgress}G");
                }
            }

            public override void draw(SpriteBatch b)
            {

                if (Game1.IsMasterGame) model = helper.Data.ReadSaveData<MailData>("MT.MailLog") ?? new MailData();
                else model = SHelper.Data.ReadJsonFile<MailData>("markettowndata.json") ?? new MailData();

                // Draw the menu background
                Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);

                // Draw the headers
                Vector2 position = new Vector2(this.xPositionOnScreen + 50, this.yPositionOnScreen + 105);

                // Draw the scrollable content
                for (int i = currentScrollIndex; i < currentScrollIndex + maxVisibleLines && i < dataLines.Count; i++)
                {
                    // Check if the mouse is over this line and draw a tooltip if necessary
                    if (Game1.getMouseX() > position.X && Game1.getMouseX() < position.X + 900
                        && Game1.getMouseY() > position.Y && Game1.getMouseY() < position.Y + lineHeight)
                    {
                        string tooltipText = tooltips.ContainsKey(dataLines[i][0]) ? tooltips[dataLines[i][0]] : string.Empty;
                        if (!string.IsNullOrEmpty(tooltipText))
                        {
                            drawHoverText(b, tooltipText, Game1.smallFont, 10, 10);
                        }
                    }


                    if (i == 23)
                    {
                        int totalAllCategory = model.TotalForageSold
                                         + model.TotalFlowerSold
                                         + model.TotalFruitSold
                                         + model.TotalVegetableSold
                                         + model.TotalSeedSold
                                         + model.TotalMonsterLootSold
                                         + model.TotalArtisanGoodSold
                                         + model.TotalAnimalProductSold
                                         + model.TotalResourceMetalSold
                                         + model.TotalMineralSold
                                         + model.TotalCraftingSold
                                         + model.TotalCookingSold
                                         + model.TotalFishSold
                                         + model.TotalClothesSold
                                         + TodayForageSold
                                         + TodayFlowerSold
                                         + TodayFruitSold
                                         + TodayVegetableSold
                                         + TodaySeedSold
                                         + TodayMonsterLootSold
                                         + TodayArtisanGoodSold
                                         + TodayAnimalProductSold
                                         + TodayResourceMetalSold
                                         + TodayMineralSold
                                         + TodayCraftingSold
                                         + TodayCookingSold
                                         + TodayFishSold
                                         + TodayClothesSold;

                        float decorLevel = 0f;
                        float tasteLevel = 0f;
                        decorLevel = (model.TotalPointDecor + TodayPointDecor) / totalAllCategory;
                        tasteLevel = (model.TotalPointTaste + TodayPointTaste) / totalAllCategory;

                        SpriteFont font = Game1.smallFont;
                        var color = Color.DarkGreen;

                        b.DrawString(font, dataLines[i][0], new Vector2(position.X + 80, position.Y), color);
                        b.DrawString(font, dataLines[i][1], new Vector2(position.X + 560, position.Y), color);

                        // taste star
                        for (int x = 0; x < 5; x++)
                            b.Draw(Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle((int)position.X + 85 + x * 40, (int)position.Y + 35, 40, 40), new Microsoft.Xna.Framework.Rectangle(338, 400, 8, 8), Color.LightGray);
                        for (int x = 0; x < 5; x++)
                        {
                            if (tasteLevel >= x + 1)
                                b.Draw(Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle((int)position.X + 85 + x * 40, (int)position.Y + 35, 40, 40), new Microsoft.Xna.Framework.Rectangle(338, 400, 8, 8), Color.Yellow);
                            else if (tasteLevel > x)
                                b.Draw(Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle((int)position.X + 85 + x * 40, (int)position.Y + 35, (int)(40 * (float)(tasteLevel - x)), 40), new Microsoft.Xna.Framework.Rectangle(338, 400, (int)(8 * (float)(tasteLevel - x)), 8), Color.Yellow);
                        }

                        // decor star
                        for (int x = 0; x < 5; x++)
                            b.Draw(Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle((int)position.X + 555 + x * 40, (int)position.Y + 35, 40, 40), new Microsoft.Xna.Framework.Rectangle(338, 400, 8, 8), Color.LightGray);
                        for (int x = 0; x < 5; x++)
                        {
                            if (decorLevel >= x + 1)
                                b.Draw(Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle((int)position.X + 555 + x * 40, (int)position.Y + 35, 40, 40), new Microsoft.Xna.Framework.Rectangle(338, 400, 8, 8), Color.Yellow);
                            else if (decorLevel > x)
                                b.Draw(Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle((int)position.X + 555 + x * 40, (int)position.Y + 35, (int)(40 * (float)(decorLevel - x)), 40), new Microsoft.Xna.Framework.Rectangle(338, 400, (int)(8 * (float)(decorLevel - x)), 8), Color.Yellow);
                        }

                        position.Y += 80;
                    }
                    else if (i == 29)
                    {
                        int level = MarketRecognition();
                        int barWidth = 200;
                        int barHeight = 20;
                        int filledWidth = (int)(barWidth * (level / (float)5));

                        SpriteFont font = Game1.smallFont;
                        var color = Color.DarkGreen;

                        b.DrawString(font, dataLines[i][0], position, color);
                        b.DrawString(font, dataLines[i][1], new Vector2(position.X + 260, position.Y), color);
                        b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle((int)position.X + 500, (int)position.Y, barWidth, barHeight), Color.Gray);
                        b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle((int)position.X + 500, (int)position.Y, filledWidth, barHeight), Color.Green);

                        position.Y += 40;
                    }
                    else if (i == 30)
                    {
                        int barWidth = 200;
                        int barHeight = 20;
                        float progress = (float)(model.TotalFestivalIncome + TodayFestivalIncome);
                        if (progress > Config.GrangeSellProgress) progress = Config.GrangeSellProgress;

                        int filledWidth = (int)(barWidth * (progress / (float)(Config.GrangeSellProgress)));

                        SpriteFont font = Game1.smallFont;
                        var color = Color.DarkGreen;

                        b.DrawString(font, dataLines[i][0], position, color);
                        b.DrawString(font, dataLines[i][1], new Vector2(position.X + 260, position.Y), color);
                        b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle((int)position.X + 500, (int)position.Y, barWidth, barHeight), Color.Gray);
                        b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle((int)position.X + 500, (int)position.Y, filledWidth, barHeight), Color.Green);

                        position.Y += 40;
                    }
                    else
                    {
                        SpriteFont font = Game1.smallFont;
                        var color = Color.DarkGreen;
                        int space = lineHeight;
                        if (i < 3 || i == 32 || i == 22)
                        {
                            color = Color.Green;
                            font = Game1.dialogueFont;
                            space += 10;
                            if (i == 21) space += 15;
                        }

                        b.DrawString(font, dataLines[i][0], position, color);
                        b.DrawString(font, dataLines[i][1], new Vector2(position.X + 260, position.Y), color);
                        b.DrawString(font, dataLines[i][2], new Vector2(position.X + 420, position.Y), color);
                        b.DrawString(font, dataLines[i][3], new Vector2(position.X + 650, position.Y), color);
                        position.Y += space;

                    }
                }

                base.draw(b);
                this.drawMouse(b);
            }

            public override void receiveScrollWheelAction(int direction)
            {
                // Handle scroll wheel input
                if (direction > 0 && currentScrollIndex > 0)
                {
                    currentScrollIndex -= 4;
                }
                else if (direction < 0 && currentScrollIndex < dataLines.Count - maxVisibleLines)
                {
                    currentScrollIndex += 4;
                }

                base.receiveScrollWheelAction(direction);
            }

            public override void receiveLeftClick(int x, int y, bool playSound = true)
            {
                base.receiveLeftClick(x, y, playSound);
            }
        }



















    }
}