using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;

namespace MarketTown
{
    public class ModConfig
    {
        public bool TipWhenNeaBy { get; set; } = true;
        public bool RushHour { get; set; } = true;
        public bool EnableDecor { get; set; } = true;
        public bool DisableChat { get; set; } = false;
        public bool DisableChatAll { get; set; } = false;

        public int MinutesToHungry { get; set; } = 120;
        public float MoveToFoodChance { get; set; } = 0.02f;
        public float MaxDistanceToFind { get; set; } = 40;
        public float MaxDistanceToEat { get; set; } = 4f;

        public float InviteComeTime { get; set; } = 1000;
        public float InviteLeaveTime { get; set; } = 2000;
        public bool EnableVisitInside { get; set; } = true;
        public bool AllowRemoveNonFood { get; set; } = false;

        public bool DisableKidAsk { get; set; } = false;
        public bool EnableSaleWeapon { get; set; } = true;
        public bool RandomPurchase { get; set; } = false;
        public int SignRange { get; set; } = 0;


        public bool DoorEntry { get; set; } = true;
        public float ShedVisitChance { get; set;} = 0.2f;
        public int MaxShedCapacity { get; set; } = 7;
        public int TimeStay { get; set; } = 130;
        public int OpenHour { get; set; } = 800;
        public int CloseHour { get;set; } = 2200;

        public float ShedMoveToFoodChance { get; set; } = 0.1f;
        public int ShedMinuteToHungry { get; set; } = 90;

        public float KidAskChance { get; set; } = 0.2f;


        public SButton ModKey { get; set; } = SButton.LeftAlt;
        public int MaxNPCOrdersPerNight { get; set; } = 4;
        public float PriceMarkup { get; set; } = 4.0f;
        public float TableSit { get; set; } = 0.3f;
        public float OrderChance { get; set; } = 0.03f;
        public float LovedDishChance { get; set; } = 0.65f;
        public List<string> RestaurantLocations { get; set; } = new List<string>();
        public int NPCCheckTimer { get; set; } = 1;
        public float MuseumPriceMarkup { get; set; } = 1.0f;
        public bool MultiplayerMode { get; set; } = false;
        public bool EasyLicense { get; set; } = false;
        public bool DisableTextChat { get; set; } = false;

        public int ParadiseIslandNPC { get; set; } = 40;
        public bool IslandProgress { get; set; } = true;
        public float IslandWalkAround { get; set; } = 0.2f;
        public bool IslandPlantBoost { get; set; } = true;
        public float IslandPlantBoostChance { get; set; } = 0.2f;
        public bool FestivalMon { get; set; } = false;
        public bool FestivalTue { get; set; } = false;
        public bool FestivalWed { get; set; } = false;
        public bool FestivalThu { get; set; } = false;
        public bool FestivalFri { get; set; } = false;
        public bool FestivalSat { get; set; } = true;
        public bool FestivalSun { get; set; } = false;

        public bool AdvanceOutputItemId { get; set; } = false;
        public bool AdvanceNpcFix { get; set; } = true;

        public int FestivalTimeStart { get; set; } = 800;
        public int FestivalTimeEnd { get; set; } = 1600;
        public float FestivalMaxSellChance { get; set; } = 0.4f;

        public float RestockChance { get; set; } = 0.66f;

        public float VisitChanceIslandHouse { get; set; } = 0.2f;
        public float VisitChanceIslandBuilding { get; set; } = 0.2f;


        public bool UltimateChallenge { get; set; } = false;
        public bool GlobalPathUpdate { get; set; } = false;
        
        public bool ExtraMessage { get; set; } = false;

        public bool AllowIndoorStore { get; set; } = false;

        public bool SellFruitTree { get; set; } = true;

        public float MoneyModifier { get; set; } = 1f;
        public float IslandMoneyModifier { get; set; } = 1f;

        public int GrangeSellProgress = 2000000;
        public float HardMode { get; set; } = 0.05f;

        public bool DisableSellNotice { get; set; } = false;


        public int AdvanceMenuOffsetX { get; set; } = 1000;
        public int AdvanceMenuOffsetY { get; set; } = 865;
        public int AdvanceMenuWidth { get; set; } = 1000;
        public int AdvanceMenuHeight { get; set; } = 910;
        public int AdvanceMenuRow { get; set; } = 22;
        public int AdvanceMenuSpace { get; set; } = 33;

        public bool AdvanceAutoFixNpc { get; set; } = false;

        public bool AdvanceDebug { get; set; } = false;

        //AI content
        public bool AdvanceAiContent = false;
        public string AdvanceAiLanguage = "English";
        public string AdvanceAiModel = "";
        public string AdvanceAiKey = "";
        public int AdvanceAiLimit = 0;
        //end of AI content

        public bool AdvanceResetProgress = false;
    }
}
