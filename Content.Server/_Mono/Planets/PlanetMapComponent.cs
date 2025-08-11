// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Mono.Planets;

[RegisterComponent]
public sealed partial class PlanetMapComponent : Component
{
    [DataField]
    public string Parallax = "bedrock";
}
// Only excludes a grid from garbage clean really.
