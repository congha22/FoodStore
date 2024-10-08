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

namespace MarketTown.Data;
class MailData
{
    // perma change
    public bool InitTable { get; set; } = false;


    // regular
    public string WeeklyDish = "240";

    public int TotalVisitorVisited { get; set; } = 0;

    public int FestivalEarning { get; set; } = 0;

    public int TotalEarning { get; set; }
    public int SellMoney { get; set; } = 0;
    public List<string> YesterdaySellLog { get; set; } = new List<string>();

    public int TodayCustomerInteraction = 0;

    public int TotalCustomerNote { get; set; } = 0;
    public int TotalCustomerNoteYes { get; set; } = 0;
    public int TotalCustomerNoteNo { get; set; } = 0;

    public int TodayMuseumVisitor { get; set; } = 0;
    public int TodayMuseumEarning { get; set; } = 0;

    public int TotalFriendVisited { get; set; } = 0;

    public int TotalFestivalIncome { get; set; } = 0;


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
    public int TotalClothesSold { get; set; } = 0;

    public float TotalPointTaste {  get; set; } = 0f;
    public float TotalPointDecor {  get; set; } = 0f;

    public IDictionary<string, string> npcConversation {  get; set; } = new Dictionary<string, string>();
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
                modHelper.Translation.Get("foodstore.letter.mtwelcome"),
                (l) => !Game1.player.mailReceived.Contains("MT.Welcome") || Game1.dayOfMonth == 1,
                delegate (Letter l)
                {
                    ((NetHashSet<string>)(object)Game1.player.mailReceived).Add(l.Id);
                })
            {
                Title = "Welcome to Market Town",
                LetterTexture = letterTexture
            }
        );
        MailData model = null;

        if (Game1.IsMasterGame)
        {
            model = modHelper.Data.ReadSaveData<MailData>("MT.MailLog");
        }

        if (model == null) { return; }

        // Museum License Letter
        MailRepository.SaveLetter(
            new Letter(
                "MT.MuseumLicense",
                modHelper.Translation.Get("foodstore.letter.mtmuseumlicense"),
                (l) => !Game1.player.mailReceived.Contains("MT.MuseumLicense") && (Game1.netWorldState.Value.MuseumPieces.Count() >= 30 || modHelper.ReadConfig<ModConfig>().EasyLicense),
                delegate (Letter l)
                {
                    ((NetHashSet<string>)(object)Game1.player.mailReceived).Add(l.Id);
                })
            {
                Title = "Museum License available",
                LetterTexture = letterTexture
            }
        );

        // Restaurant License Letter
        MailRepository.SaveLetter(
            new Letter(
                "MT.RestaurantLicense",
                modHelper.Translation.Get("foodstore.letter.mtrestaurantlicense"),
                (l) => !Game1.player.mailReceived.Contains("MT.RestaurantLicense") && (model.TotalEarning >= 10000 && model.TotalCookingSold >= 20 || modHelper.ReadConfig<ModConfig>().EasyLicense),
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
                modHelper.Translation.Get("foodstore.letter.mtmarkettownlicense"),
                (l) => !Game1.player.mailReceived.Contains("MT.MarketTownLicense") && (modHelper.ReadConfig<ModConfig>().EasyLicense
                || model.TotalEarning >= 30000 && model.TotalForageSold >= 30 && model.TotalVegetableSold >= 30 && model.TotalArtisanGoodSold >= 30 && model.TotalAnimalProductSold >= 30 && model.TotalCookingSold >= 30 && model.TotalFishSold >= 30 && model.TotalMineralSold >= 30),
                delegate (Letter l)
                {
                    ((NetHashSet<string>)(object)Game1.player.mailReceived).Add(l.Id);
                })
            {
                Title = "Market Town License available",
                LetterTexture = letterTexture
            }
        );

        // Island info
        MailRepository.SaveLetter(
            new Letter(
                "MT.IslandInfo",
                modHelper.Translation.Get("foodstore.letter.islandinfo"),
                (l) => !Game1.player.mailReceived.Contains("MT.IslandInfo") && (model.TotalVisitorVisited >= 50 || !modHelper.ReadConfig<ModConfig>().IslandProgress),
                delegate (Letter l)
                {
                    ((NetHashSet<string>)(object)Game1.player.mailReceived).Add(l.Id);
                })
            {
                Title = "Paradise Island tip",
                LetterTexture = letterTexture
            }
        );

        // Island bedroom open
        MailRepository.SaveLetter(
            new Letter(
                "MT.IslandBedroom",
                modHelper.Translation.Get("foodstore.letter.islandbedroom"),
                (l) => !Game1.player.mailReceived.Contains("MT.IslandBedroom") && (model.TotalVisitorVisited >= 200 || !modHelper.ReadConfig<ModConfig>().IslandProgress),
                delegate (Letter l)
                {
                    ((NetHashSet<string>)(object)Game1.player.mailReceived).Add(l.Id);
                })
            {
                Title = "Bedroom opened on Paradise island",
                LetterTexture = letterTexture
            }
        );


        // Dynamic Letter --------------------------------------------------------------

        // Daily Log letter
        //MailRepository.SaveLetter(
        //    new Letter(
        //        "MT.SellLogMail",
        //        modHelper.Translation.Get("foodstore.mailtotal",
        //        new { totalEarning = model.TotalEarning, sellMoney = model.SellMoney, todayCustomerInteraction = model.TodayCustomerInteraction })
        //                            + modHelper.Translation.Get("foodstore.todaymuseumvisitor", new { todayMMuseumVisitor = model.TodayMuseumVisitor, todayMuseumEarning = model.TodayMuseumEarning })
        //                            + model.YesterdaySellLog,
        //        (l) => model.SellMoney != 0 || model.TodayMuseumVisitor != 0)
        //    {
        //        LetterTexture = letterTexture
        //    }
        //);
    }

    public string GetCategoryCountsString(MailData model, IModHelper modHelper)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(modHelper.Translation.Get("foodstore.weeklytotallog", new { totalEarning = model.TotalEarning }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.weeklycustomerreview", new { total = model.TotalCustomerNote, pos = model.TotalCustomerNoteYes, neg = model.TotalCustomerNoteNo }));

        stringBuilder.Append(modHelper.Translation.Get("foodstore.forage", new { model.TotalForageSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.flower", new { model.TotalFlowerSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.fruit", new { model.TotalFruitSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.vegetable", new { model.TotalVegetableSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.seed", new { model.TotalSeedSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.monsterloot", new { model.TotalMonsterLootSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.syrup", new { model.TotalSyrupSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.artisangood", new { model.TotalArtisanGoodSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.animalproduct", new { model.TotalAnimalProductSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.resourcemetal", new { model.TotalResourceMetalSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.mineral", new { model.TotalMineralSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.crafting", new { model.TotalCraftingSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.cooking", new { model.TotalCookingSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.fish", new { model.TotalFishSold }));
        stringBuilder.Append(modHelper.Translation.Get("foodstore.gem", new { model.TotalGemSold }));


        stringBuilder.Append($"------------------------------------------^");

        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.forage", new { LastweekForageSold = model.ForageSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.flower", new { LastweekFlowerSold = model.FlowerSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.fruit", new { LastweekFruitSold = model.FruitSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.vegetable", new { LastweekVegetableSold = model.VegetableSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.seed", new { LastweekSeedSold = model.SeedSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.monsterloot", new { LastweekMonsterLootSold = model.MonsterLootSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.syrup", new { LastweekSyrupSold = model.SyrupSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.artisangood", new { LastweekArtisanGoodSold = model.ArtisanGoodSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.animalproduct", new { LastweekAnimalProductSold = model.AnimalProductSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.resourcemetal", new { LastweekResourceMetalSold = model.ResourceMetalSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.mineral", new { LastweekMineralSold = model.MineralSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.crafting", new { LastweekCraftingSold = model.CraftingSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.cooking", new { LastweekCookingSold = model.CookingSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.fish", new { LastweekFishSold = model.FishSold }));
        //stringBuilder.Append(modHelper.Translation.Get("foodstore.lastweek.gem", new { LastweekGemSold = model.GemSold }));



        return stringBuilder.ToString();
    }
}