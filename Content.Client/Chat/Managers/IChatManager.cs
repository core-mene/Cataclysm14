// SPDX-FileCopyrightText: 2019 Pieter-Jan Briers
// SPDX-FileCopyrightText: 2019 ZelteHonor
// SPDX-FileCopyrightText: 2021 Leo
// SPDX-FileCopyrightText: 2021 ike709
// SPDX-FileCopyrightText: 2022 DrSmugleaf
// SPDX-FileCopyrightText: 2022 Jezithyr
// SPDX-FileCopyrightText: 2022 metalgearsloth
// SPDX-FileCopyrightText: 2024 Leon Friedrich
// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        void Initialize();

        /// <summary>
        ///     Will refresh perms.
        /// </summary>
        event Action PermissionsUpdated;

        public void SendMessage(string text, ChatSelectChannel channel);
        public void UpdatePermissions();
    }
}
