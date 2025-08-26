// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Shared._RMC14.Wieldable.Events;

[ByRefEvent]
public record struct GetWieldDelayEvent(
    TimeSpan Delay
);
