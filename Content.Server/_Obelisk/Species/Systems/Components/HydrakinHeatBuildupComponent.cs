

using Content.Shared.Atmos;

namespace Content.Server.Species.Systems.Components;

[RegisterComponent]
public sealed partial class HydrakinHeatBuildupComponent : Component
{
    [DataField]
    public float MinTemperature = Atmospherics.T20C;

    [DataField]
    public float MaxTemperature = 340f;

    [DataField]
    public float Buildup = 0f;
}