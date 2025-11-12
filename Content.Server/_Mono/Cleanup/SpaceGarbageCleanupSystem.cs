// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 Redrover1760
// SPDX-FileCopyrightText: 2025 ScyronX
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Shuttles.Components;
using Content.Shared._Mono.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Mono.Cleanup;

/// <summary>
///     Deletes all entities with SpaceGarbageComponent.
/// </summary>
public sealed class SpaceGarbageCleanupSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ISawmill _log = default!;
    private TimeSpan _nextCleanup = TimeSpan.Zero;

    private EntityQuery<CleanupImmuneComponent> _immuneQuery;

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill("spacegarbagecleanup");

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

        // Find all entities with SpaceGarbageComponent and delete them
        var query = EntityQueryEnumerator<SpaceGarbageComponent, TransformComponent>();

        // Logging Var
        var entCount = 0;

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {

            // Skip deletion if entity is on a grid.
            if (xform.GridUid != null)
                continue;

            // Skip deletion if the component marks the entity as exempt.
            if (comp.CleanupExempt == true)
                continue;

            // Skip deletion if the entity is inside a container.
            if (_container.IsEntityInContainer(uid))
                continue;

            if (_immuneQuery.HasComp(uid))
                continue;

            // Adds entity to logging
            entCount += 1;
            // Delete the entity
            QueueDel(uid);
        }
        _log.Info($"Deleted {entCount} entities");
    }
}
