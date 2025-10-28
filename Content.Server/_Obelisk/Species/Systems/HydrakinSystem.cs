using Content.Server.Species.Systems.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.Species.Systems;

public sealed class HydrakinSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HydrakinHeatBuildupComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var comp, out var temperature))
        {
            if (TryComp<MobStateComponent>(uid, out var mobState) &&
                mobState.CurrentState != MobState.Alive)
                return;

            if (temperature.CurrentTemperature < comp.MinTemperature ||
                temperature.CurrentTemperature > comp.MaxTemperature)
                return;

            temperature.CurrentTemperature += comp.Buildup * frameTime;
        }
    }

}
