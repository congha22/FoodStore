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
        /// <summary>Get localized text for the market log and feedback menus.</summary>
        private static string MarketLogText(string key, object tokens = null)
        {
            return SHelper.Translation.Get("foodstore.log." + key, tokens);
        }

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
            private readonly ClickableComponent feedbackButton;
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
                this.feedbackButton = new ClickableComponent(
                    bounds: new Microsoft.Xna.Framework.Rectangle(this.xPositionOnScreen + this.width - 255, this.yPositionOnScreen + 58, 185, 48),
                    name: MarketLogText("view-feedback"));
                LoadData();
            }

            private void LoadData()
            {
                model = GetCurrentMailData();

                if (model == null)
                {
                    dataLines.Add(new string[] { MarketLogText("missing-save"), "", $"", "" });
                }
                else
                {
                    dataLines.Add(new string[] { MarketLogText("total-market-earning"), "", $"    {model.TotalEarning + TodayMoney}G", "" });
                    dataLines.Add(new string[] { MarketLogText("total-festival-earning"), "", $"    {model.TotalFestivalIncome + TodayFestivalIncome}G", "" });
                    dataLines.Add(new string[] { MarketLogText("reviews"), MarketLogText("review-total", new { count = model.TotalCustomerNote + TodayCustomerNote }), MarketLogText("review-positive", new { count = model.TotalCustomerNoteYes + TodayCustomerNoteYes }), MarketLogText("review-negative", new { count = model.TotalCustomerNoteNo + TodayCustomerNoteNo }) });

                    dataLines.Add(new string[] { "", "", "", "" });
                    dataLines.Add(new string[] { "---------------------------------------------------------------------------------", "", "", "" });
                    dataLines.Add(new string[] { MarketLogText("category"), MarketLogText("total"), MarketLogText("restaurant"), MarketLogText("market-town") });
                    dataLines.Add(new string[] { "---------------------------------------------------------------------------------", "", "", "" });
                    dataLines.Add(new string[] { MarketLogText("forage"), $"{model.TotalForageSold + TodayForageSold}", "-", $"{model.TotalForageSold + TodayForageSold} / 30" });
                    dataLines.Add(new string[] { MarketLogText("flower"), $"{model.TotalFlowerSold + TodayFlowerSold}", "-", $"{model.TotalFlowerSold + TodayFlowerSold} / 30" });
                    dataLines.Add(new string[] { MarketLogText("fruit"), $"{model.TotalFruitSold + TodayFruitSold}", "-", $"{model.TotalFruitSold + TodayFruitSold} / 30" });
                    dataLines.Add(new string[] { MarketLogText("vegetable"), $"{model.TotalVegetableSold + TodayVegetableSold}", "-", $"{model.TotalVegetableSold + TodayVegetableSold} / 30" });
                    dataLines.Add(new string[] { MarketLogText("artisan-good"), $"{model.TotalArtisanGoodSold + TodayArtisanGoodSold}", "-", $"{model.TotalArtisanGoodSold + TodayArtisanGoodSold} / 30" });
                    dataLines.Add(new string[] { MarketLogText("cooking"), $"{model.TotalCookingSold + TodayCookingSold}", $"{model.TotalCookingSold + TodayCookingSold} / 20", $"{model.TotalCookingSold + TodayCookingSold} / 30" });
                    dataLines.Add(new string[] { MarketLogText("fish"), $"{model.TotalFishSold + TodayFishSold}", "-", $"{model.TotalFishSold + TodayFishSold} / 30" });
                    dataLines.Add(new string[] { MarketLogText("animal-product"), $"{model.TotalAnimalProductSold + TodayAnimalProductSold}", "-", $"{model.TotalAnimalProductSold + TodayAnimalProductSold} / 30" });
                    dataLines.Add(new string[] { MarketLogText("mineral"), $"{model.TotalMineralSold + TodayMineralSold}", "-", $"{model.TotalMineralSold + TodayMineralSold} / 30" });
                    dataLines.Add(new string[] { MarketLogText("clothes"), $"{model.TotalClothesSold + TodayClothesSold}", "-", "-" });
                    dataLines.Add(new string[] { MarketLogText("seed"), $"{model.TotalSeedSold + TodaySeedSold}", "-", "-" });
                    dataLines.Add(new string[] { MarketLogText("monster-loot"), $"{model.TotalMonsterLootSold + TodayMonsterLootSold}", "-", "-" });
                    dataLines.Add(new string[] { MarketLogText("resource"), $"{model.TotalResourceMetalSold + TodayResourceMetalSold}", "-", "-" });
                    dataLines.Add(new string[] { MarketLogText("crafting"), $"{model.TotalCraftingSold + TodayCraftingSold}", "-", "-" });
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

                    dataLines.Add(new string[] { MarketLogText("total"), $"{totalAllCategory}", "", "" });
                    dataLines.Add(new string[] { MarketLogText("satisfaction"), MarketLogText("aesthetic") });
                    dataLines.Add(new string[] { "---------------------------------------------------------------------------------", "", "", "" });

                    dataLines.Add(new string[] { MarketLogText("museum-visitors"), "", $"{model.TodayMuseumVisitor + TodayMuseumVisitor}", "" });
                    dataLines.Add(new string[] { MarketLogText("island-visitors"), "", $"{model.TotalVisitorVisited + TodayVisitorVisited}", "" });
                    dataLines.Add(new string[] { MarketLogText("friends-invited"), "", $"{model.TotalFriendVisited}", "" });
                    dataLines.Add(new string[] { "---------------------------------------------------------------------------------", "", "", "" });

                    string level = MarketLogText("rank-0");
                    string nextLevel = "";
                    switch (MarketRecognition())
                    {
                        case 0:
                            level = MarketLogText("rank-0");
                            nextLevel = MarketLogText("rank-condition-0");
                            break;
                        case 1:
                            level = MarketLogText("rank-1");
                            nextLevel = MarketLogText("rank-condition-1");
                            break;
                        case 2:
                            level = MarketLogText("rank-2");
                            nextLevel = MarketLogText("rank-condition-2");
                            break;
                        case 3:
                            level = MarketLogText("rank-3");
                            nextLevel = MarketLogText("rank-condition-3");
                            break;
                        case 4:
                            level = MarketLogText("rank-4");
                            nextLevel = MarketLogText("rank-condition-4");
                            break;
                        case 5:
                            level = MarketLogText("rank-5");
                            break;
                    }

                    dataLines.Add(new string[] { MarketLogText("recognition"), $"{level}", "", "" }); // 26
                    dataLines.Add(new string[] { MarketLogText("island-progress"), "", "", "" });

                    dataLines.Add(new string[] { "", "", "", "" });
                    dataLines.Add(new string[] { MarketLogText("yesterday", new { amount = model.SellMoney }), "", MarketLogText("today", new { amount = TodayMoney }), "" });
                    dataLines.Add(new string[] { MarketLogText("today-log"), "", "", "" });
                    foreach (var line in TodaySell)
                    {
                        dataLines.Add(new string[] { $"{line}", "", "", "" });
                    }

                    // Populate tooltips
                    tooltips.Add(MarketLogText("total-market-earning"), MarketLogText("market-earning-tooltip"));
                    tooltips.Add(MarketLogText("total-festival-earning"), MarketLogText("festival-earning-tooltip"));
                    tooltips.Add(MarketLogText("reviews"), MarketLogText("reviews-tooltip"));
                    tooltips.Add(MarketLogText("category"), MarketLogText("category-tooltip"));
                    tooltips.Add(MarketLogText("forage"), MarketLogText("forage-tooltip"));
                    tooltips.Add(MarketLogText("flower"), MarketLogText("flower-tooltip"));
                    tooltips.Add(MarketLogText("fruit"), MarketLogText("fruit-tooltip"));
                    tooltips.Add(MarketLogText("vegetable"), MarketLogText("vegetable-tooltip"));
                    tooltips.Add(MarketLogText("artisan-good"), MarketLogText("artisan-good-tooltip"));
                    tooltips.Add(MarketLogText("cooking"), MarketLogText("cooking-tooltip"));
                    tooltips.Add(MarketLogText("fish"), MarketLogText("fish-tooltip"));
                    tooltips.Add(MarketLogText("animal-product"), MarketLogText("animal-product-tooltip"));
                    tooltips.Add(MarketLogText("mineral"), MarketLogText("mineral-tooltip"));
                    tooltips.Add(MarketLogText("clothes"), MarketLogText("clothes-tooltip"));
                    tooltips.Add(MarketLogText("seed"), MarketLogText("seed-tooltip"));
                    tooltips.Add(MarketLogText("monster-loot"), MarketLogText("monster-loot-tooltip"));
                    tooltips.Add(MarketLogText("resource"), MarketLogText("resource-tooltip"));
                    tooltips.Add(MarketLogText("crafting"), MarketLogText("crafting-tooltip"));

                    tooltips.Add(MarketLogText("satisfaction"), MarketLogText("taste-tooltip", new { score = ((model.TotalPointTaste + TodayPointTaste) / totalAllCategory).ToString("F2") }) +
                                                       MarketLogText("decor-tooltip", new { score = ((model.TotalPointDecor + TodayPointDecor) / totalAllCategory).ToString("F2") }));

                    tooltips.Add(MarketLogText("museum-visitors"), MarketLogText("museum-visitors-tooltip"));
                    tooltips.Add(MarketLogText("island-visitors"), MarketLogText("island-visitors-tooltip"));
                    tooltips.Add(MarketLogText("friends-invited"), MarketLogText("friends-invited-tooltip"));

                    var x = (model.TotalCustomerNote + TodayCustomerNote) == 0 ? 0 : ((float)(100 * (model.TotalCustomerNoteYes + TodayCustomerNoteYes) / (model.TotalCustomerNote + TodayCustomerNote)));
                    tooltips.Add(MarketLogText("recognition"), MarketLogText("recognition-tooltip", new { requirements = nextLevel, percent = x }));
                    tooltips.Add(MarketLogText("island-progress"), MarketLogText("island-progress-tooltip", new { amount = model.TotalFestivalIncome + TodayFestivalIncome, target = Config.GrangeSellProgress }));
                }
            }

            public override void draw(SpriteBatch b)
            {

                model = GetCurrentMailData();

                // Draw the menu background
                Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);

                // Fallback navigation when Smartphone API is not available.
                if (iSmartphoneApi == null && Config.AdvanceAiContent)
                {
                    bool hoverButton = this.feedbackButton.containsPoint(Game1.getMouseX(), Game1.getMouseY());
                    IClickableMenu.drawTextureBox(
                        b,
                        this.feedbackButton.bounds.X,
                        this.feedbackButton.bounds.Y,
                        this.feedbackButton.bounds.Width,
                        this.feedbackButton.bounds.Height,
                        hoverButton ? Color.Wheat : Color.White);

                    string buttonText = MarketLogText("view-feedback");
                    Vector2 textSize = Game1.smallFont.MeasureString(buttonText);
                    Vector2 textPosition = new Vector2(
                        this.feedbackButton.bounds.Center.X - textSize.X / 2f,
                        this.feedbackButton.bounds.Center.Y - textSize.Y / 2f);
                    Utility.drawTextWithShadow(b, buttonText, Game1.smallFont, textPosition, Color.DarkBlue);
                }

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
                if (iSmartphoneApi == null && this.feedbackButton.containsPoint(x, y)  && Config.AdvanceAiContent)
                {
                    Game1.playSound("bigSelect");
                    this.exitThisMenu();
                    Game1.activeClickableMenu = new FeedbackMenu();
                    return;
                }

                base.receiveLeftClick(x, y, playSound);
            }
        }

        /// <summary>Draw daily NPC feedback list.</summary>
        public class FeedbackMenu : IClickableMenu
        {
            private int currentScrollIndex;
            private readonly int rowHeight;
            private readonly int maxVisibleRows;

            public FeedbackMenu()
                : base((int)Utility.getTopLeftPositionForCenteringOnScreen(1100, 760).X, (int)Utility.getTopLeftPositionForCenteringOnScreen(1100, 760).Y,
                      1100, 760, showUpperRightCloseButton: true)
            {
                this.rowHeight = 112;
                this.maxVisibleRows = Math.Max(1, (this.height - 170) / this.rowHeight);
            }

            public override void receiveScrollWheelAction(int direction)
            {
                int count = GetFeedbackCount();

                int maxScroll = Math.Max(0, count - this.maxVisibleRows);
                if (direction > 0 && this.currentScrollIndex > 0)
                    this.currentScrollIndex--;
                else if (direction < 0 && this.currentScrollIndex < maxScroll)
                    this.currentScrollIndex++;

                base.receiveScrollWheelAction(direction);
            }

            public override void draw(SpriteBatch b)
            {
                List<FeedbackEntry> entries = GetFeedbackSnapshot();
                entries.Reverse();

                int maxScroll = Math.Max(0, entries.Count - this.maxVisibleRows);
                if (this.currentScrollIndex > maxScroll)
                    this.currentScrollIndex = maxScroll;

                Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);

                Utility.drawTextWithShadow(
                    b,
                    MarketLogText("feedback-title"),
                    Game1.dialogueFont,
                    new Vector2(this.xPositionOnScreen + 64, this.yPositionOnScreen + 32),
                    Color.DarkGreen);

                int contentX = this.xPositionOnScreen + 54;
                int contentY = this.yPositionOnScreen + 100;
                int contentWidth = this.width - 108;

                if (!entries.Any())
                {
                    Utility.drawTextWithShadow(
                        b,
                        MarketLogText("feedback-empty"),
                        Game1.smallFont,
                        new Vector2(contentX + 16, contentY + 12),
                        Color.Gray);

                    base.draw(b);
                    this.drawMouse(b);
                    return;
                }

                for (int i = this.currentScrollIndex; i < this.currentScrollIndex + this.maxVisibleRows && i < entries.Count; i++)
                {
                    int drawIndex = i - this.currentScrollIndex;
                    int rowY = contentY + drawIndex * this.rowHeight + 5;

                    IClickableMenu.drawTextureBox(b, contentX, rowY, contentWidth, this.rowHeight + 0, Color.White);

                    var entry = entries[i];
                    string npcName = entry.NpcName;
                    string feedback = entry.Message ?? string.Empty;

                    NPC npc = Game1.getCharacterFromName(npcName);
                    string displayName = npc?.displayName ?? npcName;

                    var portraitRect = new Microsoft.Xna.Framework.Rectangle(contentX + 18, rowY + 16, 64, 64);
                    Texture2D portrait = TryGetNpcPortrait(npcName, npc);
                    if (portrait != null)
                    {
                        int sourceW = Math.Min(64, portrait.Width);
                        int sourceH = Math.Min(64, portrait.Height);
                        b.Draw(portrait, portraitRect, new Microsoft.Xna.Framework.Rectangle(0, 0, sourceW, sourceH), Color.White);
                    }
                    else
                    {
                        b.Draw(Game1.mouseCursors, portraitRect, new Microsoft.Xna.Framework.Rectangle(338, 400, 8, 8), Color.LightGray);
                    }

                    Utility.drawTextWithShadow(
                        b,
                        displayName,
                        Game1.smallFont,
                        new Vector2(contentX + 98, rowY + 14),
                        Color.DarkBlue);

                    string wrapped = Game1.parseText(feedback, Game1.smallFont, contentWidth - 130);
                    Utility.drawTextWithShadow(
                        b,
                        wrapped,
                        Game1.smallFont,
                        new Vector2(contentX + 98, rowY + 44),
                        Color.DarkSlateGray);
                }

                string footer = MarketLogText("feedback-showing", new { shown = Math.Min(entries.Count, this.currentScrollIndex + this.maxVisibleRows), total = entries.Count });
                Utility.drawTextWithShadow(
                    b,
                    footer,
                    Game1.smallFont,
                    new Vector2(this.xPositionOnScreen + this.width - 280, this.yPositionOnScreen + this.height - 52),
                    Color.Gray);

                base.draw(b);
                this.drawMouse(b);
            }

            private static Texture2D TryGetNpcPortrait(string npcName, NPC npc)
            {
                if (npc?.Portrait != null)
                    return npc.Portrait;

                try
                {
                    return Game1.content.Load<Texture2D>($"Portraits/{npcName}");
                }
                catch
                {
                    return null;
                }
            }
        }



















    }
}