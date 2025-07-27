// SPDX-FileCopyrightText: 2023 DrSmugleaf
// SPDX-FileCopyrightText: 2023 metalgearsloth
// SPDX-FileCopyrightText: 2024 Killerqu00
// SPDX-FileCopyrightText: 2024 Nemanja
// SPDX-FileCopyrightText: 2024 Salvantrix
// SPDX-FileCopyrightText: 2024 wafehling
// SPDX-FileCopyrightText: 2025 Redrover1760
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._NF.Pirate;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._NF.Pirate.Components;

/// <summary>
/// Stores all active cargo bounties for a particular station.
/// </summary>
[RegisterComponent]
public sealed partial class SectorPirateBountyDatabaseComponent : Component
{
    /// <summary>
    /// Maximum amount of bounties a station can have.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxBounties = 8;

    /// <summary>
    /// A list of all the bounties currently active for a station.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<PirateBountyData> Bounties = new();

    /// <summary>
    /// Used to determine unique order IDs
    /// </summary>
    [DataField]
    public int TotalBounties;

    /// <summary>
    /// The time at which players will be able to skip the next bounty.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSkipTime = TimeSpan.Zero;

    /// <summary>
    /// The time between skipping bounties.
    /// </summary>
    [DataField]
    public TimeSpan SkipDelay = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The time between cancelling bounties.
    /// </summary>
    [DataField]
    public TimeSpan CancelDelay = TimeSpan.FromMinutes(15);
}
