// SPDX-FileCopyrightText: 2024 Aviu00
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._White.Blocking;

[RegisterComponent]
public sealed partial class RechargeableBlockingComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DischargedRechargeRate = 2f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChargedRechargeRate = 3f;

    [ViewVariables]
    public bool Discharged;
}
