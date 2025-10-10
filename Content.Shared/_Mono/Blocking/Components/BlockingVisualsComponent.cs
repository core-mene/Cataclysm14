// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared._Mono.Blocking;
using Robust.Shared.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Client._Mono.Blocking.Components;

/// <summary>
/// This component gets dynamically added to an Entity via the <see cref="BlockingSystem"/> if the IsClothing is true
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBlockingSystem))]
[AutoGenerateComponentState]
public sealed partial class BlockingVisualsComponent : Component
{
    /// <summary>
    /// Self-explanatory.
    /// </summary>
    [DataField("enabled")]
    [AutoNetworkedField]
    public bool Enabled = false;
}
