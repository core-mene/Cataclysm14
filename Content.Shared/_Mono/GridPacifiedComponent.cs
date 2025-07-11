// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 Redrover1760
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Mono;

/// <summary>
/// Component that applies Pacified status to all organic entities on a grid.
/// Entities with company affiliations matching the exempt companies will not be pacified.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class GridPacifiedComponent : Component
{
    /// <summary>
    /// A way to override with VV
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool Override = false;

    /// <summary>
    /// A check for if an entity is pre-pacified
    /// </summary>
    [DataField]
    public bool PrePacified = false;

    /// <summary>
    /// Until what time an entity will be pacified for
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan PacifiedTime;

    /// <summary>
    /// The time when the next periodic update should occur
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// How frequently to check the entity for changes
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The radius from a GridPacifier that a GridPacified entity is pacified.
    /// </summary>
    [DataField]
    public float PacifyRadius = 512f;
}
