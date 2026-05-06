using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MarketTown
{
    public interface ISmartPhoneApi
    {
        /// <summary>
        /// Gets a list of NPCs that have been registered to receive smartphone messages. 
        /// This list is used to populate the dropdown menu in the smartphone UI, allowing players to select which NPC they want to send messages to.
        /// </summary>
        /// <returns>A list of NPC names.</returns>
        List<string> GetPhoneNpcList();

        /// <summary>
        /// Sends a message from an NPC to the player. This method is used to simulate receiving messages on the player's smartphone from NPCs in the game.
        /// </summary>
        /// <param name="npcName">The name of the NPC sending the message (case-sensitive).</param>
        /// <param name="message">The content of the message being sent.</param>
        void SendSmartphoneMessageFromNPC(string npcName, string message);

        /// <summary>
        /// Sends a message from the player to an NPC. This method is used to simulate sending messages from the player's smartphone to NPCs in the game.
        /// </summary>
        /// <param name="npcName">The name of the NPC receiving the message (case-sensitive).</param>
        /// <param name="message">The content of the message being sent.</param>
        void SendSmartphoneMessageFromPlayer(string npcName, string message);

        /// <summary>
        /// Sends a notification to the player's smartphone.
        /// </summary>
        /// <param name="message">The content of the notification (shown in the phone notification message).</param>
        /// <param name="notificationName">(optional) The name of the notification (show on game notification popup).</param>
        void SendSmartphoneNotification(string message, string notificationName = "");

        /// <summary>
        /// Registers a custom app icon on the smartphone home screen.
        /// </summary>
        /// <param name="ownerModId">The unique ID of the mod that owns this app (e.g. this.ModManifest.UniqueID).</param>
        /// <param name="appId">A unique app ID within the owner mod (e.g. d5a1lamdtd.markettown.marketlog).</param>
        /// <param name="displayName">Name shown as a label under the app icon (e.g. Market Log).</param>
        /// <param name="iconTexture">Texture used as the app icon (84x84 pixels).</param>
        /// <param name="onClick">Callback invoked when the app icon is clicked.</param>
        /// <param name="closePhoneOnLaunch">Whether the phone menu should close before invoking <paramref name="onClick"/>.</param>
        /// <param name="sortOrder">Lower values are shown earlier in the app grid.</param>
        /// <param name="sourceRect">Optional source rectangle if the icon is part of a spritesheet (should be 84x84 pixels).</param>
        /// <param name="isVisible">Optional callback to decide whether the icon should currently be visible (e.g. () => Context.IsWorldReady).</param>
        /// <param name="getBadgeCount">Optional callback to draw a badge count on the icon.</param>
        /// <returns>True if registration succeeded; otherwise false.</returns>
        bool RegisterPhoneApp(
            string ownerModId,
            string appId,
            string displayName,
            Texture2D iconTexture,
            Action onClick,
            bool closePhoneOnLaunch = true,
            int sortOrder = 0,
            Rectangle? sourceRect = null,
            Func<bool>? isVisible = null,
            Func<int>? getBadgeCount = null
        );

        /// <summary>
        /// Unregisters a previously registered custom app.
        /// </summary>
        /// <param name="ownerModId">The unique ID of the mod that owns this app.</param>
        /// <param name="appId">The app ID that was used during registration.</param>
        /// <returns>True if an app was removed; otherwise false.</returns>
        bool UnregisterPhoneApp(string ownerModId, string appId);

        /// <summary>
        /// Registers a grouped app on the smartphone home screen. Clicking it opens a built-in
        /// app-group page managed by Smartphone.
        /// </summary>
        /// <param name="ownerModId">The unique ID of the mod that owns this app group.</param>
        /// <param name="groupId">A unique group ID within the owner mod.</param>
        /// <param name="displayName">Name shown as a label under the app icon.</param>
        /// <param name="iconTexture">Texture used as the app icon (84x84 recommended).</param>
        /// <param name="sortOrder">Lower values are shown earlier in the app grid.</param>
        /// <param name="sourceRect">Optional source rectangle if the icon is part of a spritesheet.</param>
        /// <param name="isVisible">Optional callback to decide whether the icon should currently be visible.</param>
        /// <param name="getBadgeCount">Optional callback to draw a badge count on the icon.</param>
        /// <returns>True if registration succeeded; otherwise false.</returns>
        bool RegisterPhoneAppGroup(
            string ownerModId,
            string groupId,
            string displayName,
            Texture2D iconTexture,
            int sortOrder = 0,
            Rectangle? sourceRect = null,
            Func<bool>? isVisible = null,
            Func<int>? getBadgeCount = null
        );

        /// <summary>
        /// Unregisters a previously registered app group and all of its items.
        /// </summary>
        /// <param name="ownerModId">The unique ID of the mod that owns this app group.</param>
        /// <param name="groupId">The group ID used during registration.</param>
        /// <returns>True if a group was removed; otherwise false.</returns>
        bool UnregisterPhoneAppGroup(string ownerModId, string groupId);

        /// <summary>
        /// Registers or updates an item inside a phone app group.
        /// </summary>
        /// <param name="ownerModId">The unique ID of the mod that owns this app group.</param>
        /// <param name="groupId">The group ID that should contain this item.</param>
        /// <param name="itemId">A unique item ID within the app group.</param>
        /// <param name="displayName">Name shown below the item icon in the group page.</param>
        /// <param name="iconTexture">Texture used as the item icon (84x84 recommended).</param>
        /// <param name="onClick">Callback invoked when the item is clicked.</param>
        /// <param name="closePhoneOnLaunch">Whether the phone menu should close before invoking <paramref name="onClick"/>.</param>
        /// <param name="sortOrder">Lower values are shown earlier in the group grid.</param>
        /// <param name="sourceRect">Optional source rectangle if the icon is part of a spritesheet.</param>
        /// <param name="isVisible">Optional callback to decide whether the item should currently be visible.</param>
        /// <param name="getBadgeCount">Optional callback to draw a badge count on the item icon.</param>
        /// <returns>True if registration succeeded; otherwise false.</returns>
        bool RegisterPhoneAppGroupItem(
            string ownerModId,
            string groupId,
            string itemId,
            string displayName,
            Texture2D iconTexture,
            Action onClick,
            bool closePhoneOnLaunch = true,
            int sortOrder = 0,
            Rectangle? sourceRect = null,
            Func<bool>? isVisible = null,
            Func<int>? getBadgeCount = null
        );

        /// <summary>
        /// Unregisters a previously registered app group item.
        /// </summary>
        /// <param name="ownerModId">The unique ID of the mod that owns this app group.</param>
        /// <param name="groupId">The group ID that contains this item.</param>
        /// <param name="itemId">The item ID used during registration.</param>
        /// <returns>True if an item was removed; otherwise false.</returns>
        bool UnregisterPhoneAppGroupItem(string ownerModId, string groupId, string itemId);
    }
}
