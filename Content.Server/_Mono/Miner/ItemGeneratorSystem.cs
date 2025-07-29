// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Power.Components;
using Content.Server.Chat.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Server.Station.Systems;
using Content.Server.Power.EntitySystems;
using Content.Server.Salvage.Expeditions;
using Robust.Shared.Containers;
using Content.Shared.Stacks;
using Content.Server.Stack;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._Mono.Miner;

public sealed class ItemGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly BatterySystem _batterySystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemGeneratorComponent, MapInitEvent>(OnStartup);
    }

    private void OnStartup(EntityUid uid, ItemGeneratorComponent component, MapInitEvent args)
    {
        var originStation = _station.GetOwningStation(uid);

        if (originStation != null)
        {
            component.OriginStation = originStation;
        } // shitcode from nukesystem
        else
        {
            var transform = Transform(uid);
            component.OriginMapGrid = (transform.MapID, transform.GridUid);
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ItemGeneratorComponent, BatteryComponent, PowerConsumerComponent>();
        var currentTime = _gameTiming.CurTime;

        while (query.MoveNext(out var entity, out var miner, out var battery, out var powerConsumer))
        {
            // checking station
            var xformQuery = GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(entity);
            if (miner.RequireExpedition && !HasComp<SalvageExpeditionComponent>(xform.MapUid))
            {
                continue;
            }

            powerConsumer.NetworkLoad.DesiredPower = miner.PowerDraw;
            if (powerConsumer.NetworkLoad.ReceivingPower < miner.PowerDraw)
                continue;

            _batterySystem.SetCharge(entity, miner.PowerDraw, battery);

            var elapsed = (currentTime - miner.LastUpdate).TotalSeconds;
            if (elapsed < miner.MiningInterval)
                continue;

            miner.LastUpdate = currentTime;

            if (!_containerSystem.TryGetContainer(entity, "output_slot", out var container))
                continue;

            if (container.ContainedEntities.Count == 0)
            {
                var newSpawn = _entityManager.SpawnEntity(_random.Pick(miner.Prototypes), Transform(entity).Coordinates);
                if (TryComp(newSpawn, out StackComponent? newStack))
                {
                    _stackSystem.SetCount(newSpawn, 1);
                    _containerSystem.Insert(newSpawn, container);
                }
            }
            else if (TryComp(container.ContainedEntities[0], out StackComponent? stack))
            {
                _stackSystem.SetCount(container.ContainedEntities[0], stack.Count + 1);
            }

            _audio.PlayPvs(miner.MiningSound, entity);
        }
    }
}
