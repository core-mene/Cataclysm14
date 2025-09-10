// SPDX-FileCopyrightText: 2022 Kara
// SPDX-FileCopyrightText: 2022 metalgearsloth
// SPDX-FileCopyrightText: 2023 DrSmugleaf
// SPDX-FileCopyrightText: 2025 beck-thompson
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanBatteryAmmoProviderComponent : BatteryAmmoProviderComponent
{
    [DataField("proto", required: true)]
    public EntProtoId HitscanEntityProto;
}
