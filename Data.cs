using MailFrameworkMod;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using System.Collections.Generic;
using System;
using System.Text;
using StardewValley.Buildings;

namespace MarketTown;
class MailData
{

    public int TotalEarning { get; set; }
    public int SellMoney { get; set; } = 0;
    public string SellList { get; set; } = "";

    public int ForageSold { get; set; } = 0;
    public int FlowerSold { get; set; } = 0;
    public int FruitSold { get; set; } = 0;
    public int VegetableSold { get; set; } = 0;
    public int SeedSold { get; set; } = 0;
    public int MonsterLootSold { get; set; } = 0;
    public int SyrupSold { get; set; } = 0;
    public int ArtisanGoodSold { get; set; } = 0;
    public int AnimalProductSold { get; set; } = 0;
    public int ResourceMetalSold { get; set; } = 0;
    public int MineralSold { get; set; } = 0;
    public int CraftingSold { get; set; } = 0;
    public int CookingSold { get; set; } = 0;
    public int FishSold { get; set; } = 0;
    public int GemSold { get; set; } = 0;



    public int TotalForageSold { get; set; } = 0;
    public int TotalFlowerSold { get; set; } = 0;
    public int TotalFruitSold { get; set; } = 0;
    public int TotalVegetableSold { get; set; } = 0;
    public int TotalSeedSold { get; set; } = 0;
    public int TotalMonsterLootSold { get; set; } = 0;
    public int TotalSyrupSold { get; set; } = 0;
    public int TotalArtisanGoodSold { get; set; } = 0;
    public int TotalAnimalProductSold { get; set; } = 0;
    public int TotalResourceMetalSold { get; set; } = 0;
    public int TotalMineralSold { get; set; } = 0;
    public int TotalCraftingSold { get; set; } = 0;
    public int TotalCookingSold { get; set; } = 0;
    public int TotalFishSold { get; set; } = 0;
    public int TotalGemSold { get; set; } = 0;
}

internal class MailLoader
{
    public static ITranslationHelper I18N;

    public static ModConfig ModConfig;

    public MailLoader(IModHelper modHelper)
    {
        var letterTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("Assets/LtBG.png");

        // Init Market Town letter
        MailRepository.SaveLetter(
            new Letter(
                "MT.Welcome",
                "  Welcome to Market Town. Here you can set up your own store and sell goods to villagers!!!^^   " +
                "Let's start by choosing a good place for your store, then place down a table and start selling any item you prefer.^^   " +
                "If you want to turn your Shed into a restaurant, a convenience store, or any kind you want, you need to obtain either a Restaurant License from Saloon, or a Market Town License from Desert Trader.^^ " +
                "-Requirement for Restaurant License: Total earning more than 10000, and has sold at least 20 Cooking.^^" +
                " -Requirement for Market Town License: Total earning more than 20000, and has sold at least 30 of each Forage, Vegetable, Artisan Good, Animal Product, Cooking, Fish, and Mineral.^^" +
                "     -HaPyke",
                (Letter l) => !Game1.player.mailReceived.Contains("MT.Welcome"),
                delegate (Letter l)
                {
                    ((NetHashSet<string>)(object)Game1.player.mailReceived).Add(l.Id);
                })
            {
                Title = "Welcome to Market Town",
                LetterTexture = letterTexture
            }
        );


        var model = modHelper.Data.ReadSaveData<MailData>("MT.MailLog");

        if (model == null) { return; }

        // Restaurant License Letter
        MailRepository.SaveLetter(
            new Letter(
                "MT.RestaurantLicense",
                "Dear @.^^As you have meet the requirement to hold a Restaurant License, you can drop by the Saloon whenever you are free to purchase the license.^" +
                "Restaurant License allowing you to operate your Shed as a restaurant.^^" +
                "I will drop by for some meal sometime^-Gus",
                (Letter l) => !Game1.player.mailReceived.Contains("MT.RestaurantLicense") && model.TotalEarning >= 10000 && model.CookingSold >= 20,
                delegate (Letter l)
                {
                    ((NetHashSet<string>)(object)Game1.player.mailReceived).Add(l.Id);
                })
            {
                Title = "Restaurant License available",
                LetterTexture = letterTexture
            }
        );

        // Market Town License Letter
        MailRepository.SaveLetter(
            new Letter(
                "MT.MarketTownLicense",
                "Dear @.^^You are doing great with your business. Now you can obtain a Market Town License from Desert Trader.^Market Town License let you to sell anything you want!^^-Lewis",
                (Letter l) => !!Game1.player.mailReceived.Contains("MT.MarketTownLicense") && model.TotalEarning >= 20000 && model.ForageSold >= 30 && model.VegetableSold >= 30 && model.ArtisanGoodSold >= 30 && model.AnimalProductSold >= 30 && model.CookingSold >= 30 && model.FishSold >= 30 && model.MineralSold >= 30,
                delegate (Letter l)
                {
                    ((NetHashSet<string>)(object)Game1.player.mailReceived).Add(l.Id);
                })
            {
                Title = "Market Town License available",
                LetterTexture = letterTexture
            }
        );



        // Dynamic Letter --------------------------------------------------------------



        string categoryCountsString = GetCategoryCountsString(model);
        // Daily Log letter
        MailRepository.SaveLetter(
            new Letter(
                "MT.SellLogMail",
                modHelper.Translation.Get("foodstore.mailtotal", new { totalEarning = model.TotalEarning, sellMoney = model.SellMoney }) + model.SellList,
                (Letter l) => model.SellMoney != 0)
            {
                LetterTexture = letterTexture
            }
        );

        // Weekly Log letter
        MailRepository.SaveLetter(
            new Letter(
                "MT.WeeklyLogMail",
                categoryCountsString,
                (Letter l) => Game1.dayOfMonth == 1 || Game1.dayOfMonth == 8 || Game1.dayOfMonth == 15 || Game1.dayOfMonth == 22)
            {
                LetterTexture = letterTexture
            }
        );


    }

    public string GetCategoryCountsString(MailData model)
    {
        StringBuilder stringBuilder = new StringBuilder();

        stringBuilder.Append($"Weekly Log:^^");
        stringBuilder.Append($"Total Forage Sold: {model.TotalForageSold}^");
        stringBuilder.Append($"Total Flower Sold: {model.TotalFlowerSold}^");
        stringBuilder.Append($"Total Fruit Sold: {model.TotalFruitSold}^");
        stringBuilder.Append($"Total Vegetable Sold: {model.TotalVegetableSold}^");
        stringBuilder.Append($"Total Seed Sold: {model.TotalSeedSold}^");
        stringBuilder.Append($"Total Monster Loot Sold: {model.TotalMonsterLootSold}^");
        stringBuilder.Append($"Total Syrup Sold: {model.TotalSyrupSold}^");
        stringBuilder.Append($"Total Artisan Good Sold: {model.TotalArtisanGoodSold}^");
        stringBuilder.Append($"Total Animal Product Sold: {model.TotalAnimalProductSold}^");
        stringBuilder.Append($"Total Resource Metal Sold: {model.TotalResourceMetalSold}^");
        stringBuilder.Append($"Total Mineral Sold: {model.TotalMineralSold}^");
        stringBuilder.Append($"Total Crafting Sold: {model.TotalCraftingSold}^");
        stringBuilder.Append($"Total Cooking Sold: {model.TotalCookingSold}^");
        stringBuilder.Append($"Total Fish Sold: {model.TotalFishSold}^");
        stringBuilder.Append($"Total Gem Sold: {model.TotalGemSold}^");

        stringBuilder.Append($"------------------------------------------^");

        stringBuilder.Append($"Last week Forage Sold: {model.ForageSold}^");
        stringBuilder.Append($"Last week Flower Sold: {model.FlowerSold}^");
        stringBuilder.Append($"Last week Fruit Sold: {model.FruitSold}^");
        stringBuilder.Append($"Last week Vegetable Sold: {model.VegetableSold}^");
        stringBuilder.Append($"Last week Seed Sold: {model.SeedSold}^");
        stringBuilder.Append($"Last week Monster Loot Sold: {model.MonsterLootSold}^");
        stringBuilder.Append($"Last week Syrup Sold: {model.SyrupSold}^");
        stringBuilder.Append($"Last week Artisan Good Sold: {model.ArtisanGoodSold}^");
        stringBuilder.Append($"Last week Animal Product Sold: {model.AnimalProductSold}^");
        stringBuilder.Append($"Last week Resource Metal Sold: {model.ResourceMetalSold}^");
        stringBuilder.Append($"Last week Mineral Sold: {model.MineralSold}^");
        stringBuilder.Append($"Last week Crafting Sold: {model.CraftingSold}^");
        stringBuilder.Append($"Last week Cooking Sold: {model.CookingSold}^");
        stringBuilder.Append($"Last week Fish Sold: {model.FishSold}^");
        stringBuilder.Append($"Last week Gem Sold: {model.GemSold}");

        return stringBuilder.ToString();
    }
}