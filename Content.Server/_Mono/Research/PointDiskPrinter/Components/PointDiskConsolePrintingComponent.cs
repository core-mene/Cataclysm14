// SPDX-FileCopyrightText: 2022 Nemanja
// SPDX-FileCopyrightText: 2023 DrSmugleaf
// SPDX-FileCopyrightText: 2025 Onezero0
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Mono.Research.PointDiskPrinter.Components;

[RegisterComponent]
public sealed partial class PointDiskConsolePrintingComponent : Component
{
    public TimeSpan FinishTime;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Disk1K = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Disk5K = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Disk10K = false;
}
