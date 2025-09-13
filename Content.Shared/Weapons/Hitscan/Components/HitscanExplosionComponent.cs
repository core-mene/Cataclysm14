// SPDX-FileCopyrightText: 2025 Avalon
//
// SPDX-License-Identifier: MPL-2.0

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// When this hitscan hits a target, it will explode.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanExplosionComponent : Component
{
    /// <summary>
    /// Explosive that will be spawned and then triggered when the hitscan hits its target.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Explosive;
};
