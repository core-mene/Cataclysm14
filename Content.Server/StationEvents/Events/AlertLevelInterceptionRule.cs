// SPDX-FileCopyrightText: 2024 Mr. 27
// SPDX-FileCopyrightText: 2024 Whatstone
// SPDX-FileCopyrightText: 2024 deltanedas
// SPDX-FileCopyrightText: 2025 AwareFoxy
//
// SPDX-License-Identifier: MPL-2.0

using Content.Server.StationEvents.Components;
using Content.Server.AlertLevel;
﻿using Content.Shared.GameTicking.Components;

namespace Content.Server.StationEvents.Events;

public sealed class AlertLevelInterceptionRule : StationEventSystem<AlertLevelInterceptionRuleComponent>
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;

    protected override void Started(EntityUid uid, AlertLevelInterceptionRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args) // Goobstation - Changed an indent.
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;
        // Frontier - note: levels are globally set/gotten, regardless of arg
        if (_alertLevelSystem.GetLevel(chosenStation.Value) != "green")
            return;

        _alertLevelSystem.SetLevel(chosenStation.Value, component.AlertLevel, true, true, true, component.Locked); // Goobstation
    }
}
