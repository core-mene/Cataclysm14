// SPDX-FileCopyrightText: 2025 Avalon
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.Damage;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Network;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanExplosionSystem : EntitySystem
{
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanExplosionComponent, HitscanRaycastFiredEvent>(OnHitscanHit, after: [ typeof(HitscanReflectSystem) ]);
    }

    private void OnHitscanHit(Entity<HitscanExplosionComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Canceled || args.HitEntity == null)
            return;

        if (_net.IsClient)
            return;

        var explosion = Spawn(ent.Comp.Explosive, Transform(args.HitEntity.Value).Coordinates);

        _explosion.TriggerExplosive(explosion);

        // TODO: maybe split up the effects component or something - this wont play sounds and stuff (maybe that's ok?)
    }
}
