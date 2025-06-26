// SPDX-FileCopyrightText: 2025 Whatstone
// SPDX-FileCopyrightText: 2025 monolith8319
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._NF.RoundNotifications.Events;

[Serializable]
public sealed class RoundStartedEvent : EntityEventArgs
{
    public int RoundId { get; }

    public RoundStartedEvent(int roundId)
    {
        RoundId = roundId;
    }
}
