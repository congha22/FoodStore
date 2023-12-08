using StardewModdingAPI;
using StardewValley.Menus;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoodStore;
using Microsoft.Xna.Framework;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Characters;
using System.Collections;
using System.Numerics;
using System.Security.AccessControl;
using xTile.Dimensions;
using Object = StardewValley.Object;
using StardewValley.GameData;
using static System.Net.Mime.MediaTypeNames;
using StardewValley.Minigames;
using System.Threading;
using StardewValley.SDKs;
using static FoodStore.ModEntry;
using System.ComponentModel;
using HarmonyLib;
using static StardewValley.Minigames.TargetGame;
using static System.Collections.Specialized.BitVector32;

namespace FoodStore
{
    internal class PlayerChat
    {

        private bool bHasInit;
        private Dictionary<string, NPC> NpcMap = new Dictionary<string, NPC>();
        public string Target = "";
        public string TextInput = "";

        private async Task TryToInitAsync()
        {
            if (!this.bHasInit && Context.IsWorldReady)
            {
                ((TextBox)Game1.chatBox.chatBox).OnEnterPressed += new TextBoxEvent(ChatBox_OnEnterPressed);
                await Task.Delay(1);
                this.bHasInit = true;
            }
        }

        private void ChatBox_OnEnterPressed(TextBox sender)     //get player sent text
        {
            this.bHasInit = true;
            if (this.TextInput.Length == 0 || this.Target.Length == 0)
            {
                return;
            }
            string _TextInput = this.TextInput;
            string _Target = this.Target;
            try
            {
                if (this.NpcMap.ContainsKey(_Target))
                {
                    NPC npc2 = this.NpcMap[_Target];
                }

                if (this.NpcMap.ContainsKey(_Target))
                {
                    NPC npc = this.NpcMap[_Target];

                    if (!((Character)npc).isMoving())
                    {
                        npc.facePlayer(Game1.player);
                    }
                    OnPlayerSend(npc, _TextInput, _Target);
                    this.NpcMap.Clear();
                }
            }
            catch (Exception){}
        }

        private void OnPlayerSend(NPC npc, string textInput, string npcName)
        {

            Random random = new Random();

            // Available option
            string helpKey = "help";

            string[] helpListKey = {"h_ask_villager", "h_invite", "h_today_dish", "h_taste", "h_set_up" };

            string[] inviteKey = { "house", "invite", "tomorrow" };

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
            if (askHelp || askHelpIndex || askFood || askVisit || askTaste)
            {
                if (askHelp)       // Show main help option
                {
                    string listKey = "";
                    foreach (string key in helpListKey)
                        listKey += key + " | ";
                    npc.showTextAboveHead(listKey, default, default, 7000);
                }
                else if(askHelpIndex)        // Show detail of help option
                {
                    npc.showTextAboveHead(SHelper.Translation.Get("foodstore.help." + index), default, default, 7000);
                }                       // Invite to visit
                else if (askVisit)
                {
                    npc.showTextAboveHead("Maybe");
                }
                else if (askFood)       // Ask dish of the day
                {
                    int randomIndex = random.Next(10);
                    npc.showTextAboveHead(SHelper.Translation.Get("foodstore.dishday." + randomIndex.ToString(), new { dishToday = DishPrefer.dishDay }), default, default, 5000);
                }
                else if (askTaste)       // Ask taste of the last dish
                {
                    string dishTaste = "";
                    if (npc.modData.ContainsKey("hapyke.FoodStore/LastFoodTaste"))  dishTaste = npc.modData["hapyke.FoodStore/LastFoodTaste"];
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

                    npc.showTextAboveHead(dishTaste, default, default, 5000);
                }
            }
            else                        // All other message
            {
                int randomIndex = random.Next(19);
                npc.showTextAboveHead(SHelper.Translation.Get("foodstore.customerresponse." + randomIndex.ToString()), default, default, 5000);
            }
        }

        internal async void Validate()
        {
            await TryToInitAsync();

            if (this.bHasInit && Game1.currentLocation != null)
            {
                this.Validate_TextInput();
                this.Validate_NPCMap();
                this.Validate_Target();
                this.Validate_Glow();
            }
        }

        private void Validate_TextInput()
        {
            if (Game1.chatBox.chatBox.finalText.Count > 0)
            {
                this.TextInput = Game1.chatBox.chatBox.finalText[0].message;
            }
        }

        private void Validate_NPCMap()          //Get NPC in map
        {
            this.NpcMap.Clear();
            foreach (NPC npc in Game1.currentLocation.characters)
            {
                string displayName = ((Character)npc).displayName;
                if (this.NpcMap.ContainsKey(displayName))
                {
                    NPC newNPC = npc;
                    NPC oldNPC = this.NpcMap[displayName];
                    Microsoft.Xna.Framework.Vector2 val = Microsoft.Xna.Framework.Vector2.Subtract(((Character)Game1.player).getTileLocation(), ((Character)oldNPC).getTileLocation());
                    float oldDistance = ((Microsoft.Xna.Framework.Vector2)(val)).Length();
                    val = Microsoft.Xna.Framework.Vector2.Subtract(((Character)Game1.player).getTileLocation(), ((Character)newNPC).getTileLocation());
                    float newDistance = ((Microsoft.Xna.Framework.Vector2)(val)).Length();
                    if (oldDistance < newDistance)
                    {
                        continue;
                    }
                    this.NpcMap.Remove(displayName);
                }
                this.NpcMap.Add(displayName, npc);
            }
        }

        private void Validate_Target()          //Get distance from NPC to Player
        {
            this.Target = "";
            if (!Game1.chatBox.isActive())
            {
                return;
            }
            float bestDistance = 6;
            foreach (KeyValuePair<string, NPC> pair in this.NpcMap)
            {
                Microsoft.Xna.Framework.Vector2 val = Microsoft.Xna.Framework.Vector2.Subtract(((Character)Game1.player).getTileLocation(), ((Character)pair.Value).getTileLocation());
                float distance = ((Microsoft.Xna.Framework.Vector2)(val)).Length(); 
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    this.Target = pair.Key;
                }

            }
        }

        private void Validate_Glow()        //Check for NPC Glow
        {
            foreach (NPC npc in Game1.currentLocation.characters)
            {
                if (((Character)npc).displayName != this.Target && ((Character)npc).isGlowing)
                {
                    ((Character)npc).stopGlowing();
                }
                else if (((Character)npc).displayName == this.Target && !((Character)npc).isGlowing)
                {
                    ((Character)npc).startGlowing(Color.Purple, false, 0.01f);
                }
            }
        }

    }
}
