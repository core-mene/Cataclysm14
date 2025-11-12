// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 Redrover1760
// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Cargo.Systems;
using Content.Server.Power.Components;
using Content.Shared._Mono.CCVar;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._Mono.Cleanup;

/// TODO: Move to Mono Namespace

/// <summary>
/// This system cleans up small grid fragments that have less than a specified number of tiles after a delay.
/// </summary>
public sealed class GridCleanupSystem : EntitySystem
{
    [Dependency] private readonly CleanupHelperSystem _cleanup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private TimeSpan _cleanupInterval = TimeSpan.FromSeconds(120);
    private float _maxDistance;
    private float _maxValue;
    private TimeSpan _duration;

    private Queue<EntityUid> _checkQueue = new();
    private TimeSpan _nextCleanup = TimeSpan.Zero;
    private int _delCount = 0;

    private HashSet<Entity<ApcComponent>> _apcList = new();

    private EntityQuery<BatteryComponent> _batteryQuery;

    public override void Initialize()
    {
        base.Initialize();

        _batteryQuery = GetEntityQuery<BatteryComponent>();

        Subs.CVar(_cfg, MonoCVars.GridCleanupDistance, val => _maxDistance = val, true);
        Subs.CVar(_cfg, MonoCVars.GridCleanupMaxValue, val => _maxValue = val, true);
        Subs.CVar(_cfg, MonoCVars.GridCleanupDuration, val => _duration = TimeSpan.FromSeconds(val), true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // delete one queued entity per update
        if (_checkQueue.Count != 0)
        {
            var uid = _checkQueue.Dequeue();

            if (TerminatingOrDeleted(uid))
                return;

            var xform = Transform(uid);
            var parent = xform.ParentUid;

            var state = EnsureComp<GridCleanupStateComponent>(uid);

            if (HasComp<MapComponent>(uid) // if we're a planetmap ignore
                || !HasComp<MapGridComponent>(uid) // if we somehow lost MapGridComponent
                || HasComp<MapGridComponent>(parent) // do not delete anything on planetmaps either
                || TryComp<IFFComponent>(uid, out var iff) && (iff.Flags & IFFFlags.HideLabel) == 0 // delete only if IFF off
                || _cleanup.HasNearbyPlayers(xform.Coordinates, _maxDistance)
                || HasPoweredAPC((uid, xform)) // don't delete if it has powered APCs
                || _pricing.AppraiseGrid(uid) > _maxValue) // expensive to run, put last
            {
                state.CleanupAtTime = null;
                return;
            }
            // see if we should update timer or just be deleted
            else if (state.CleanupAtTime == null || state.CleanupAtTime < _timing.CurTime)
            {
                state.CleanupAtTime ??= _timing.CurTime + _duration;
                return;
            }

            _delCount += 1;
            Log.Info($"Cleanup deleting grid: {ToPrettyString(uid)}");
            QueueDel(uid); // hopefully deletes nothing important on the grid with all the prior checks
            return;
        }

        if (_delCount != 0)
        {
            Log.Info($"Cleanup deleted {_delCount} grids");
            _delCount = 0;
        }

        // we appear to be done with previous queue so try get another
        var curTime = _timing.CurTime;
        if (curTime < _nextCleanup)
            return;
        _nextCleanup = curTime + _cleanupInterval;

        // queue the next batch
        var query = EntityQueryEnumerator<MapGridComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _checkQueue.Enqueue(uid);
        }
    }

    bool HasPoweredAPC(Entity<TransformComponent> grid)
    {
        _apcList.Clear();
        var worldAABB = _lookup.GetWorldAABB(grid, grid.Comp);

        _lookup.GetEntitiesIntersecting<ApcComponent>(grid.Comp.MapID, worldAABB, _apcList);

        foreach (var apc in _apcList)
        {
            // charge check should ideally be a comparision to 0f but i don't trust that
            if (_batteryQuery.TryComp(apc, out var battery)
                && battery.CurrentCharge > battery.MaxCharge * 0.01f
                && apc.Comp.MainBreakerEnabled // if it's disabled consider it depowered
            )
                return true;
        }
        return false;
    }
}
