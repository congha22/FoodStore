using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Network;
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
            if (npc.IsVillager && (askHelp || askHelpIndex || askFood || askVisit || askTaste))
            {
                if (askHelp)       // Show main help option
                {
                    string listKey = "";
                    foreach (string key in helpListKey)
                        listKey += key + " | ";

                    NPCShowTextAboveHead(npc, listKey);
                }
                else if (askHelpIndex)        // Show detail of help option
                {
                    NPCShowTextAboveHead(npc, SHelper.Translation.Get("foodstore.help." + index));
                }                       // Invite to visit
                else if (askVisit && !npc.Name.Contains("MT.Guest_") && npc.modData.ContainsKey("hapyke.FoodStore/inviteTried") && npc.modData.ContainsKey("hapyke.FoodStore/invited")
                    && !bool.Parse(npc.modData["hapyke.FoodStore/inviteTried"]) && !bool.Parse(npc.modData["hapyke.FoodStore/invited"]))
                {
                    Random rand = new Random();
                    int heartLevel = Game1.player.getFriendshipHeartLevelForNPC(npc.Name);

                    int inviteIndex = rand.Next(7);

                    if (heartLevel < 2)
                    {
                        NPCShowTextAboveHead(npc, SHelper.Translation.Get("foodstore.noinvitevisit." + inviteIndex));
                    }
                    else if (heartLevel <= 5)
                    {
                        if (rand.NextDouble() > 0.5)
                        {
                            NPCShowTextAboveHead(npc, SHelper.Translation.Get("foodstore.willinvitevisit." + inviteIndex));
                            npc.modData["hapyke.FoodStore/invited"] = "true";
                            npc.modData["hapyke.FoodStore/inviteDate"] = Game1.stats.DaysPlayed.ToString();
                        }
                        else
                            NPCShowTextAboveHead(npc, SHelper.Translation.Get("foodstore.cannotinvitevisit." + inviteIndex));

                    }
                    else
                    {
                        if (rand.NextDouble() > 0.25)
                        {
                            NPCShowTextAboveHead(npc, SHelper.Translation.Get("foodstore.willinvitevisit." + inviteIndex));
                            npc.modData["hapyke.FoodStore/invited"] = "true";
                            npc.modData["hapyke.FoodStore/inviteDate"] = Game1.stats.DaysPlayed.ToString();
                        }
                        else
                            NPCShowTextAboveHead(npc, SHelper.Translation.Get("foodstore.cannotinvitevisit." + inviteIndex));

                    }
                    npc.modData["hapyke.FoodStore/inviteTried"] = "true";
                }
                else if (askFood)       // Ask dish of the day
                {
                    int randomIndex = random.Next(10);
                    NPCShowTextAboveHead(npc, SHelper.Translation.Get("foodstore.dishday." + randomIndex.ToString(), new { dishToday = ItemRegistry.Create<Object>(DailyFeatureDish, allowNull: true)?.DisplayName ?? "" }));
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
                    NPCShowTextAboveHead(npc, dishTaste);
                }
            }
            else                        // All other message
            {
                int randomIndex = random.Next(1, 8);
                string npcAge, npcManner, npcSocial, npcHeartLevel;

                int age = npc.Age;
                int manner = npc.Manners;
                int social = npc.SocialAnxiety;
                int heartLevel = 0;
                if (Game1.player.friendshipData.ContainsKey(npc.Name)) heartLevel = (int)Game1.player.friendshipData[npc.Name].Points / 250;

                npcAge = age == 0 ? "adult." : age == 1 ? "teens." : age == 2 ? "child." : "adult.";
                npcManner = manner == 0 ? "neutral." : manner == 1 ? "polite." : manner == 2 ? "rude." : "neutral.";
                npcSocial = social == 0 ? "outgoing." : social == 1 ? "shy." : social == 2 ? "neutral." : "neutral.";
                npcHeartLevel = heartLevel <= 2 ? ".0" : heartLevel <= 5 ? ".3" : ".6";

                string relation = heartLevel <= 2 ? "stranger" : heartLevel <= 5 ? "acquaintance" : "best friend";
                string bestFriend = "";
                foreach (var f in Game1.player.friendshipData)
                    foreach (var f2 in f.Where(f2 => f2.Value.Points >= 750).OrderByDescending(f2 => f2.Value.Points).Take(3))
                        bestFriend += $"{f2.Key}, ";
                    
                string data = $"Current location: {Game1.currentLocation.Name}; Current time: {Game1.timeOfDay}; Weather:{Game1.currentLocation.GetWeather().Weather}; Day of months: {Game1.dayOfMonth}; Current season: {Game1.currentLocation.GetSeason()};";
                
                if (bestFriend != "") data += $"Player's closet friends: {bestFriend}; ";

                conversationSummaries.TryGetValue(npc.Name, out string history);
                if (history != "") data += $"Previous user message: {history}";

                if (Config.AdvanceAiContent && AILimitCount < AILimitBlock)
                {
                    Task.Run(() => ModEntry.SendMessageToAssistant(
                        npc: npc,
                        userMessage: textInput,
                        systemMessage: $"As NPC {npc.Name} ({npcAge}, {npcManner} manner, {npcSocial} social anxiety, and in {relation} relationship with player {Game1.player.Name}), you will reply the user message if they ask question, or start a new conversation in context of Stardew Valley game. You can use this information if relevant: {data}. Limit to under 30 words",
                        isConversation: true)
                    );
                }
                else
                {
                    string text = SHelper.Translation.Get("foodstore.general." + npcAge + npcManner + npcSocial + randomIndex.ToString() + npcHeartLevel);
                    //SHelper.Events.Input.ButtonPressed += (sender, args) => { Game1.chatBox.addInfoMessage(args.Button.ToString()); };
                    NPCShowTextAboveHead(npc, text);
                }
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
            float closetDistance = 999f;
            float currentDistance = 999f;
            bool foundOne = false;
            NPC selectNpc = new NPC();
            foreach (NPC npc in Game1.currentLocation.characters)
            {
                if (npc.IsVillager)
                {
                    currentDistance = (Game1.player.Tile - npc.Tile).Length();
                    if (currentDistance > closetDistance) continue;
                    else
                    {
                        closetDistance = currentDistance;
                        selectNpc = npc;
                        foundOne = true;
                    }
                }
            }

            if ( selectNpc != null && foundOne ) { NpcMap.Add(selectNpc.displayName, selectNpc); }
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
                Microsoft.Xna.Framework.Vector2 val = Microsoft.Xna.Framework.Vector2.Subtract(Game1.player.Tile, pair.Value.Tile);
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
                if (npc.IsVillager && npc.displayName != Target && npc.isGlowing)
                {
                    npc.stopGlowing();
                }
                else if (npc.IsVillager && npc.displayName == Target && !npc.isGlowing)
                {
                    npc.startGlowing(Color.Purple, false, 0.01f);
                }
            }
        }



        public static void NPCShowTextAboveHead(NPC npc, string message)
        {
            if (!WantsToSay(npc, 0)) return;

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
    }
    internal class EntryQuestion : DialogueBox
    {
        private readonly List<Action> ResponseActions;
        internal EntryQuestion(string dialogue, List<Response> responses, List<Action> actions) : base(dialogue, responses.ToArray())
        {
            ResponseActions = actions;
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
