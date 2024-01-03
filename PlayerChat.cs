using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Objects;
using StardewValley.SDKs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xTile.Dimensions;
using static MarketTown.ModEntry;
using static StardewValley.Minigames.TargetGame;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using Object = StardewValley.Object;

namespace MarketTown
{
    internal class PlayerChat
    {

        private bool bHasInit;
        private Dictionary<string, NPC> NpcMap = new Dictionary<string, NPC>();
        public string Target = "";
        public string TextInput = "";

        private async Task TryToInitAsync()
        {
            if (!bHasInit && Context.IsWorldReady)
            {
                Game1.chatBox.chatBox.OnEnterPressed += new TextBoxEvent(ChatBox_OnEnterPressed);
                await Task.Delay(1);
                bHasInit = true;
            }
        }

        private void ChatBox_OnEnterPressed(TextBox sender)     //get player sent text
        {
            bHasInit = true;
            if (TextInput.Length == 0 || Target.Length == 0)
            {
                return;
            }
            string _TextInput = TextInput;
            string _Target = Target;
            try
            {
                if (NpcMap.ContainsKey(_Target))
                {
                    NPC npc2 = NpcMap[_Target];
                }

                if (NpcMap.ContainsKey(_Target))
                {
                    NPC npc = NpcMap[_Target];

                    if (!npc.isMoving())
                    {
                        npc.facePlayer(Game1.player);
                    }
                    OnPlayerSend(npc, _TextInput);
                    NpcMap.Clear();
                }
            }
            catch (Exception) { }
        }

        public void OnPlayerSend(NPC npc, string textInput)
        {

            Random random = new Random();
            // Available option
            string helpKey = "help";

            string[] helpListKey = { "h_ask_villager", "h_invite", "h_today_dish", "h_taste", "h_set_up" };

            string[] inviteKey = { "invite" };

            string[] foodKey = { "dish of the day", "today dish", "dish today", "today special",
                "special today", "popular today", "today popular", "best food today", "today best food",
                "today's special", "special today", "today's dish",
                "chef's recommendation", "featured dish", "daily special", "what's good today",
                "recommended today", "today's highlight", "top pick today", "today's favorite" };

            string[] tasteKey = { "taste of the dish", "last dish", "last meal", "dish taste", "dish_taste", "taste of the dish" };



            // Validate options

            bool askHelp = helpKey.Equals(textInput, StringComparison.OrdinalIgnoreCase);

            int index = Array.FindIndex(helpListKey, key => key.Equals(textInput, StringComparison.OrdinalIgnoreCase));
            bool askHelpIndex = index != -1;

            bool askVisit = inviteKey.All(value => textInput.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0);

            bool askFood = foodKey.Any(target => textInput.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0);

            bool askTaste = tasteKey.Any(target => textInput.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0);


            // Handle text message
            if (npc.isVillager() && (askHelp || askHelpIndex || askFood || askVisit || askTaste))
            {
                if (askHelp)       // Show main help option
                {
                    string listKey = "";
                    foreach (string key in helpListKey)
                        listKey += key + " | ";
                    if (!Config.DisableChatAll) npc.showTextAboveHead(listKey, default, default, 7000);
                }
                else if (askHelpIndex)        // Show detail of help option
                {
                    if (!Config.DisableChatAll) npc.showTextAboveHead(SHelper.Translation.Get("foodstore.help." + index), default, default, 7000);
                }                       // Invite to visit
                else if (askVisit && !bool.Parse(npc.modData["hapyke.FoodStore/inviteTried"]) && !bool.Parse(npc.modData["hapyke.FoodStore/invited"]))
                {
                    Random rand = new Random();
                    int heartLevel = Game1.player.getFriendshipHeartLevelForNPC(npc.Name);

                    int inviteIndex = rand.Next(7);

                    if (heartLevel < 2)
                    {
                        if (!Config.DisableChatAll) npc.showTextAboveHead(SHelper.Translation.Get("foodstore.noinvitevisit." + inviteIndex), default, default, 5000);
                    }
                    else if (heartLevel <= 5)
                    {
                        if (rand.NextDouble() > 0.5)
                        {
                            if (!Config.DisableChatAll) npc.showTextAboveHead(SHelper.Translation.Get("foodstore.willinvitevisit." + inviteIndex), default, default, 5000);
                            npc.modData["hapyke.FoodStore/invited"] = "true";
                            npc.modData["hapyke.FoodStore/inviteDate"] = Game1.stats.daysPlayed.ToString();
                        }
                        else
                            if (!Config.DisableChatAll) npc.showTextAboveHead(SHelper.Translation.Get("foodstore.cannotinvitevisit." + inviteIndex), default, default, 5000);

                    }
                    else
                    {
                        if (rand.NextDouble() > 0.25)
                        {
                            if (!Config.DisableChatAll) npc.showTextAboveHead(SHelper.Translation.Get("foodstore.willinvitevisit." + inviteIndex), default, default, 5000);
                            npc.modData["hapyke.FoodStore/invited"] = "true";
                            npc.modData["hapyke.FoodStore/inviteDate"] = Game1.stats.daysPlayed.ToString();
                        }
                        else
                            if (!Config.DisableChatAll) npc.showTextAboveHead(SHelper.Translation.Get("foodstore.cannotinvitevisit." + inviteIndex), default, default, 5000);

                    }
                    npc.modData["hapyke.FoodStore/inviteTried"] = "true";
                }
                else if (askFood)       // Ask dish of the day
                {
                    int randomIndex = random.Next(10);
                    if (!Config.DisableChatAll) npc.showTextAboveHead(SHelper.Translation.Get("foodstore.dishday." + randomIndex.ToString(), new { dishToday = DishPrefer.dishDay }), default, default, 5000);
                }
                else if (askTaste)       // Ask taste of the last dish
                {
                    string dishTaste = "";
                    if (npc.modData.ContainsKey("hapyke.FoodStore/LastFoodTaste")) dishTaste = npc.modData["hapyke.FoodStore/LastFoodTaste"];
                    int randomIndex = random.Next(3);

                    switch (dishTaste)
                    {
                        case "0":
                            dishTaste = SHelper.Translation.Get("foodstore.asktaste.love." + randomIndex.ToString());
                            break;
                        case "2":
                            dishTaste = SHelper.Translation.Get("foodstore.asktaste.like." + randomIndex.ToString());
                            break;
                        case "4":
                            dishTaste = SHelper.Translation.Get("foodstore.asktaste.dislike." + randomIndex.ToString());
                            break;
                        case "6":
                            dishTaste = SHelper.Translation.Get("foodstore.asktaste.hate." + randomIndex.ToString());
                            break;
                        case "8":
                            dishTaste = SHelper.Translation.Get("foodstore.asktaste.neutral." + randomIndex.ToString());
                            break;
                        default:
                            dishTaste = SHelper.Translation.Get("foodstore.asktaste.empty." + randomIndex.ToString());
                            break;
                    }

                    if (!Config.DisableChatAll) npc.showTextAboveHead(dishTaste, default, default, 5000);
                }
            }
            else                        // All other message
            {
                int randomIndex = random.Next(1, 8);
                string npcAge, npcManner, npcSocial;

                int age = npc.Age;
                int manner = npc.Manners;
                int social = npc.SocialAnxiety;

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
                if (!Config.DisableChatAll) npc.showTextAboveHead(SHelper.Translation.Get("foodstore.general." + npcAge + npcManner + npcSocial + randomIndex.ToString()), default, default, 5000);
            }
            ActionList.Clear();
        }

        internal async void Validate()
        {
            await TryToInitAsync();

            if (bHasInit && Game1.currentLocation != null)
            {
                Validate_TextInput();
                Validate_NPCMap();
                Validate_Target();
                Validate_Glow();
            }
        }

        private void Validate_TextInput()
        {
            if (Game1.chatBox.chatBox.finalText.Count > 0)
            {
                TextInput = Game1.chatBox.chatBox.finalText[0].message;
            }
        }

        private void Validate_NPCMap()          //Get NPC in map
        {
            NpcMap.Clear();
            foreach (NPC npc in Game1.currentLocation.characters)
            {
                if (npc.isVillager())
                {
                    string displayName = npc.displayName;
                    if (NpcMap.ContainsKey(displayName))
                    {
                        NPC newNPC = npc;
                        NPC oldNPC = NpcMap[displayName];
                        Microsoft.Xna.Framework.Vector2 val = Microsoft.Xna.Framework.Vector2.Subtract(Game1.player.getTileLocation(), oldNPC.getTileLocation());
                        float oldDistance = ((Microsoft.Xna.Framework.Vector2)val).Length();
                        val = Microsoft.Xna.Framework.Vector2.Subtract(Game1.player.getTileLocation(), newNPC.getTileLocation());
                        float newDistance = ((Microsoft.Xna.Framework.Vector2)val).Length();
                        if (oldDistance < newDistance)
                        {
                            continue;
                        }
                        NpcMap.Remove(displayName);
                    }
                    NpcMap.Add(displayName, npc);
                }
            }
        }

        private void Validate_Target()          //Get distance from NPC to Player
        {
            Target = "";
            if (!Game1.chatBox.isActive())
            {
                return;
            }
            float bestDistance = 6;
            foreach (KeyValuePair<string, NPC> pair in NpcMap)
            {
                Microsoft.Xna.Framework.Vector2 val = Microsoft.Xna.Framework.Vector2.Subtract(Game1.player.getTileLocation(), pair.Value.getTileLocation());
                float distance = ((Microsoft.Xna.Framework.Vector2)val).Length();
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    Target = pair.Key;
                }

            }
        }

        private void Validate_Glow()        //Check for NPC Glow
        {
            foreach (NPC npc in Game1.currentLocation.characters)
            {
                if (npc.isVillager() && npc.displayName != Target && npc.isGlowing)
                {
                    npc.stopGlowing();
                }
                else if (npc.isVillager() && npc.displayName == Target && !npc.isGlowing)
                {
                    npc.startGlowing(Color.Purple, false, 0.01f);
                }
            }
        }

    }
    internal class EntryQuestion : DialogueBox
    {
        private readonly List<Action> ResponseActions;
        internal EntryQuestion(string dialogue, List<Response> responses, List<Action> Actions) : base(dialogue, responses)
        {
            ResponseActions = Actions;
        }
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            int responseIndex = selectedResponse;
            base.receiveLeftClick(x, y, playSound);

            if (safetyTimer <= 0 && responseIndex > -1 && responseIndex < ResponseActions.Count && ResponseActions[responseIndex] != null)
            {
                ResponseActions[responseIndex]();
            }
        }
    }
}
