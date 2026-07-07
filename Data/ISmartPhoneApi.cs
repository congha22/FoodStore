using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MarketTown
{
    public enum AppSize
    {
        Size1x1,
        Size2x1,
        Size2x2,
        Size2x3,
        Size2x4,
        Size4x2,
        Size4x3,
        Size4x4,
    }

    public interface ISmartPhoneApi
    {
        /// ======================================
        /// API to register custom apps on the smartphone home screen.
        /// ======================================

        /// <summary>
        /// Registers a custom app icon on the smartphone home screen.
        /// </summary>
        /// <param name="ownerModId">The unique ID of the mod that owns this app (e.g. d5a1lamdtd.markettown).</param>
        /// <param name="appId">A unique app ID within the owner mod (e.g. d5a1lamdtd.markettown.marketlog).</param>
        /// <param name="displayName">Name shown as a label under the app icon (e.g. Market Log).</param>
        /// <param name="onClick">Callback invoked when the app icon is clicked.</param>
        /// <param name="closePhoneOnLaunch">Whether the phone menu should close before invoking <paramref name="onClick"/>.</param>
        /// <param name="sourceRect">Optional source rectangle if the icon is part of a spritesheet. If null, the full texture is used.</param>
        /// <param name="getBadgeCount">Optional callback to draw a badge count on the icon.</param>
        /// <param name="supportedSizes">Optional list of <see cref="AppSize"/> values the app icon supports as widget sizes.
        /// Defaults to <see cref="AppSize.Size1x1"/> only when null or empty.</param>
        /// <returns>True if registration succeeded; otherwise false.</returns>
        bool RegisterPhoneApp(
            string ownerModId,
            string appId,
            string displayName,
            Action onClick,
            bool closePhoneOnLaunch = true,
            Rectangle? sourceRect = null,
            Func<int>? getBadgeCount = null,
            AppSize[]? supportedSizes = null,
            Action<SpriteBatch, Rectangle, AppSize>? onDrawWidget = null,
            Dictionary<string, Texture2D>? themedIconTextures = null
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
