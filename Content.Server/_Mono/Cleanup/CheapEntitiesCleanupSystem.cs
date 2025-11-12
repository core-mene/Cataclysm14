// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 Redrover1760
// SPDX-FileCopyrightText: 2025 ScyronX
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Body.Components;
using Content.Shared._Mono.CCVar;
using Content.Shared.Cargo.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Stacks;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Mono.Cleanup;

/// <summary>
///     Deletes all entities with SpaceGarbageComponent.
/// </summary>
public sealed class CheapEntitiesCleanupSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ISawmill _log = default!;
    private TimeSpan _nextCleanup = TimeSpan.Zero;

    private EntityQuery<CleanupImmuneComponent> _immuneQuery;

    readonly private double _minValueToDelete = 100d;

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill("cheapentitycleanup");

        _immuneQuery = GetEntityQuery<CleanupImmuneComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        // Skip if it's not time for cleanup yet
        if (curTime < _nextCleanup)
            return;

        // Schedule next cleanup based on CVar
        var cleanupInterval = TimeSpan.FromSeconds(_cfg.GetCVar(MonoCVars.SpaceGarbageCleanupInterval));
        _nextCleanup = curTime + cleanupInterval;

        // EQE
        var staticquery = EntityQueryEnumerator<StaticPriceComponent, TransformComponent>();
        var stackquery = EntityQueryEnumerator<StaticPriceComponent, StackComponent, TransformComponent>();

        // Logging Var
        var entCount = 0;

        while (staticquery.MoveNext(out var uid, out var comp, out var xform))
        {

            if (HasComp<StackPriceComponent>(uid))
                continue;

            // Skip deletion if entity is on a grid.
            if (xform.GridUid != null)
                continue;

            // Skip deletion if the entity is inside a container.
            if (_container.IsEntityInContainer(uid))
                continue;

            // Check for being a container
            if (HasComp<ContainerManagerComponent>(uid))
                continue;

            // Price Check
            if (comp.Price > _minValueToDelete)
                continue;

            // Safety check for mobs. This shouldn't apply as mobs do not have static prices.
            if (HasComp<MobStateComponent>(uid))
                continue;

            // Safety check for players. This shouldn't apply as mobs do not have static prices.
            if (HasComp<ActorComponent>(uid))
                continue;

            // Final safety check.
            if (HasComp<BrainComponent>(uid))
                continue;

            if (_immuneQuery.HasComp(uid))
                continue;

            // Adds entity to logging
            entCount += 1;
            // Delete the entity
            QueueDel(uid);
        }

        while (stackquery.MoveNext(out var uid2, out var stackPrice, out var stack, out var xform))
        {
            // Skip deletion if entity is on a grid.
            if (xform.GridUid != null)
                continue;

            // Skip deletion if the entity is inside a container.
            if (_container.IsEntityInContainer(uid2))
                continue;

            // Check for being a container
            if (HasComp<ContainerManagerComponent>(uid2))
                continue;

            // Price Check
            if (stackPrice.Price * stack.Count > _minValueToDelete)
                continue;

            // Safety check for mobs. This shouldn't apply as mobs do not have stack prices.
            if (HasComp<MobStateComponent>(uid2))
                continue;

            // Safety check for players. This shouldn't apply as mobs do not have stack prices.
            if (HasComp<ActorComponent>(uid2))
                continue;

            // Final safety check.
            if (HasComp<BrainComponent>(uid2))
                continue;

            if (_immuneQuery.HasComp(uid2))
                continue;

            // Adds entity to logging
            entCount += 1;
            // Delete the entity
            QueueDel(uid2);
        }


        _log.Info($"Deleted {entCount} entities");
    }
}
