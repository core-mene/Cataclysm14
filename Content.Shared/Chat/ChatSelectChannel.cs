// SPDX-FileCopyrightText: 2021 Pieter-Jan Briers
// SPDX-FileCopyrightText: 2022 Chief-Engineer
// SPDX-FileCopyrightText: 2022 Michael Phillips
// SPDX-FileCopyrightText: 2022 Morbo
// SPDX-FileCopyrightText: 2022 metalgearsloth
// SPDX-FileCopyrightText: 2022 wrexbe
// SPDX-FileCopyrightText: 2025 Ilya246
// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Chat
{
    /// <summary>
    ///     Chat channels that the player can select in the chat box.
    /// </summary>
    /// <remarks>
    ///     Maps to <see cref="ChatChannel"/>, giving better names.
    /// </remarks>
    [Flags]
    public enum ChatSelectChannel : uint // Goobstation - Starlight collective mind port
    {
        None = 0,

        /// <summary>
        ///     Chat heard by players within earshot
        /// </summary>
        Local = ChatChannel.Local,

        /// <summary>
        ///     Chat heard by players right next to each other
        /// </summary>
        Whisper = ChatChannel.Whisper,

        /// <summary>
        ///     Radio messages
        /// </summary>
        Radio = ChatChannel.Radio,

        /// <summary>
        ///     Local out-of-character channel
        /// </summary>
        LOOC = ChatChannel.LOOC,

        /// <summary>
        ///     Out-of-character channel
        /// </summary>
        OOC = ChatChannel.OOC,

        /// <summary>
        ///     Emotes
        /// </summary>
        Emotes = ChatChannel.Emotes,

        /// <summary>
        ///     CollectiveMind
        /// </summary>
        CollectiveMind = ChatChannel.CollectiveMind,

        /// <summary>
        ///     Deadchat
        /// </summary>
        Dead = ChatChannel.Dead,

        /// <summary>
        ///     Admin chat
        /// </summary>
        Admin = ChatChannel.AdminChat,

        Console = ChatChannel.Unspecified
    }
}
