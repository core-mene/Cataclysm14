// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 Ilya246
// SPDX-FileCopyrightText: 2025 Redrover1760
// SPDX-FileCopyrightText: 2025 ScyronX
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost.Roles.Components;
using Content.Server.NPC.HTN;
using Content.Shared._Mono.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Mono.Cleanup;

/// <summary>
///     Deletes all entities with SpaceGarbageComponent.
/// </summary>
public sealed class MobCleanupSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private ISawmill _log = default!;
    private TimeSpan _cleanupInterval = TimeSpan.FromSeconds(60);
    private float _maxDistance;

    private Queue<EntityUid> _checkQueue = new();
    private TimeSpan _nextCleanup = TimeSpan.Zero;
    private int _delCount = 0;

    private EntityQuery<GhostRoleComponent> _ghostQuery;
    private EntityQuery<CleanupImmuneComponent> _immuneQuery;

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill("mobcleanup");

        _ghostQuery = GetEntityQuery<GhostRoleComponent>();
        _immuneQuery = GetEntityQuery<CleanupImmuneComponent>();

        Subs.CVar(_cfg, MonoCVars.MobCleanupDistance, val => _maxDistance = val, true);
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

            if (xform.GridUid != null
                || _immuneQuery.HasComp(uid)
                || _ghostQuery.HasComp(uid)
                || HasNearbyPlayers(xform.Coordinates, _maxDistance)
            )
                return;

            // Adds entity to logging
            _delCount += 1;
            QueueDel(uid);
            return;
        }

        if (_delCount != 0)
        {
            _log.Info($"Deleted {_delCount} mobs");
            _delCount = 0;
        }

        // we appear to be done with previous queue so try get another
        var curTime = _timing.CurTime;
        if (curTime < _nextCleanup)
            return;
        _nextCleanup = curTime + _cleanupInterval;

        // queue the next batch
        var query = EntityQueryEnumerator<HTNComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _checkQueue.Enqueue(uid);
        }
    }

    public bool HasNearbyPlayers(EntityCoordinates coord, float radius) {
        var allPlayerData = _player.GetAllPlayerData();
        foreach (var playerData in allPlayerData)
        {
            var exists = _player.TryGetSessionById(playerData.UserId, out var session);

            if (!exists
                || session == null
                || session.AttachedEntity is not { Valid: true } playerEnt
                || HasComp<GhostComponent>(playerEnt)
                || _mobState.IsDead(playerEnt))
                continue;

            var playerCoords = Transform(playerEnt).Coordinates;

            if (coord.TryDistance(EntityManager, playerCoords, out var distance)
                && distance <= radius
            )
                return true;
        }
        return false;
    }
}
