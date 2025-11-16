// SPDX-FileCopyrightText: 2025 sneb
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.Atmos;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Species.Systems.Components;

[RegisterComponent]
public sealed partial class HydrakinComponent : Component
{
    [DataField]
    public float MinTemperature = Atmospherics.T20C;

    [DataField]
    public float MaxTemperature = 340f;

    [DataField]
    public float Buildup = 0f;

    [DataField]
    public bool HeatBuildupEnabled = true;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? CoolOffActionId = "ActionHydrakinCoolOff";

    [DataField]
    public EntityUid? CoolOffAction;
}