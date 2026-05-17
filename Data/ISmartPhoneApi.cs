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
        /// ======================================
        /// API to register custom apps or app groups on the smartphone home screen.
        /// ======================================

        /// <summary>
        /// Registers a custom app icon on the smartphone home screen.
        /// </summary>
        /// <param name="ownerModId">The unique ID of the mod that owns this app (e.g. d5a1lamdtd.markettown).</param>
        /// <param name="appId">A unique app ID within the owner mod (e.g. d5a1lamdtd.markettown.marketlog).</param>
        /// <param name="displayName">Name shown as a label under the app icon (e.g. Market Log).</param>
        /// <param name="iconTexture">Texture used as the app icon (any size but should be square and above 84*84).</param>
        /// <param name="onClick">Callback invoked when the app icon is clicked.</param>
        /// <param name="closePhoneOnLaunch">Whether the phone menu should close before invoking <paramref name="onClick"/>.</param>
        /// <param name="sortOrder">Lower values are shown earlier in the app grid.</param>
        /// <param name="sourceRect">Optional source rectangle if the icon is part of a spritesheet. If null, the full texture is used.</param>
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
        /// Registers a grouped app on the smartphone home screen. Clicking it opens a built-in
        /// app-group page managed by Smartphone.
        /// </summary>
        /// <param name="ownerModId">The unique ID of the mod that owns this app group.</param>
        /// <param name="groupId">A unique group ID within the owner mod.</param>
        /// <param name="displayName">Name shown as a label under the app icon.</param>
        /// <param name="iconTexture">Texture used as the app icon (any size; it is fit into the phone icon slot while preserving aspect ratio).</param>
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
        /// Registers or updates an item inside a phone app group.
        /// </summary>
        /// <param name="ownerModId">The unique ID of the mod that owns this app group.</param>
        /// <param name="groupId">The group ID that should contain this item.</param>
        /// <param name="itemId">A unique item ID within the app group.</param>
        /// <param name="displayName">Name shown below the item icon in the group page.</param>
        /// <param name="iconTexture">Texture used as the item icon (any size; it is fit into the phone icon slot while preserving aspect ratio).</param>
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



        /// ======================================
        /// API for interacting with the smartphone messenger app
        /// ======================================

        /// <summary>
        /// Sends a notification to the player's smartphone.
        /// </summary>
        /// <param name="message">The content of the notification (shown in the phone notification message).</param>
        /// <param name="notificationName">(optional) The name of the notification (shown on ingame notification HUD).</param>
        /// <param name="playerId">(optional) The target player's UniqueMultiplayerID as string. If null/empty/invalid, this is broadcast to all online players.</param>
        void SendSmartphoneNotification(string message, string notificationName = "", string playerId = "");

    }
}
