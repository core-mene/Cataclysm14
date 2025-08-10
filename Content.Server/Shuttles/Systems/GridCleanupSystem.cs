// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 Redrover1760
// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Mono.Planets;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Content.Server.Salvage.Expeditions;
using Content.Shared.Mobs.Components;

namespace Content.Server.Shuttles.Systems;

/// TODO: Move to Mono Namespace

/// <summary>
/// This system cleans up small grid fragments that have less than a specified number of tiles after a delay.
/// </summary>
public sealed class GridCleanupSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    // The minimum number of tiles a grid needs to avoid being cleaned up
    private const int MinimumTiles = 10;

    // The delay before cleaning up a small grid (in seconds)
    private const float CleanupDelay = 1800.0f;

    // Dictionary to track grids scheduled for deletion
    private readonly Dictionary<EntityUid, TimeSpan> _pendingCleanup = new();

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to grid events
        SubscribeLocalEvent<GridStartupEvent>(OnGridStartup);
        SubscribeLocalEvent<MapGridComponent, TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<SalvageExpeditionComponent, ComponentStartup>(OnExpeditionStartup);
    }

    private void OnGridStartup(GridStartupEvent ev)
    {
        // Check newly created grids
        if (TryComp<MapGridComponent>(ev.EntityUid, out var grid))
            CheckGrid((ev.EntityUid, grid));
        if (HasComp<PlanetMapComponent>(ev.EntityUid))
            _pendingCleanup.Remove(ev.EntityUid);
    }

    private void OnTileChanged(Entity<MapGridComponent> ent, ref TileChangedEvent args)
    {
        // When a grid is modified (tiles added/removed), check if it needs cleanup
        CheckGrid(ent);
    }

    private void OnExpeditionStartup(EntityUid uid, SalvageExpeditionComponent component, ComponentStartup args)
    {
        // Make sure any grid that gets the expedition component is removed from cleanup
        if (_pendingCleanup.ContainsKey(uid))
        {
            Logger.DebugS("gridcleanup", $"Expedition startup: Removing grid {uid} from cleanup queue");
            _pendingCleanup.Remove(uid);
        }

        // Check if this entity also has a grid component and ensure it's not marked for cleanup
        if (TryComp<MapGridComponent>(uid, out var grid))
        {
            // Make sure we don't clean up very small expedition grids
            var tileCount = CountTiles((uid, grid));
            Logger.DebugS("gridcleanup", $"Expedition grid {uid} has {tileCount} tiles");
        }
    }

    private void CheckGrid(Entity<MapGridComponent> ent)
    {
        var gridUid = ent.Owner;
        var grid = ent.Comp;

        // Skip if already scheduled for deletion
        if (_pendingCleanup.ContainsKey(gridUid))
            return;

        // Skip if this is a planet expedition grid
        if (HasComp<SalvageExpeditionComponent>(gridUid))
        {
            Logger.DebugS("gridcleanup", $"CheckGrid: Skipping grid {gridUid} with SalvageExpeditionComponent");
            return;
        }

        // Skip if this is a planet grid in general
        if (HasComp<PlanetMapComponent>(gridUid))
        {
            Logger.DebugS("gridcleanup", $"CheckGrid: Skipping grid {gridUid} with PlanetMapComponent");
            return;
        }

        // Skip if the parent map has a SalvageExpeditionComponent
        var transform = Transform(gridUid);
        var mapId = transform.MapID;
        var mapUid = _mapManager.GetMapEntityId(mapId);

        if (HasComp<SalvageExpeditionComponent>(mapUid))
        {
            Logger.DebugS("salvage", $"CheckGrid: Skipping grid {gridUid} on expedition map {mapUid}");
            return;
        }

        if (HasComp<PlanetMapComponent>(mapUid))
        {
            Logger.DebugS("salvage", $"CheckGrid: Skipping grid {gridUid} on planet map {mapUid}");
            return;
        }

        // Count tiles
        var tileCount = CountTiles((gridUid, grid));

        // If the tile count is below our threshold, schedule it for deletion
        if (tileCount < MinimumTiles)
        {
            Logger.DebugS("salvage", $"CheckGrid: Scheduling grid {gridUid} for cleanup with {tileCount} tiles");
            ScheduleGridCleanup(gridUid);
        }
    }

    private void ScheduleGridCleanup(EntityUid gridUid)
    {
        // Skip if already scheduled
        if (_pendingCleanup.ContainsKey(gridUid))
            return;

        var targetTime = _timing.CurTime + TimeSpan.FromSeconds(CleanupDelay);
        _pendingCleanup[gridUid] = targetTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pendingCleanup.Count == 0)
            return;

        // Check if any grids need to be cleaned up
        var currentTime = _timing.CurTime;
        var toRemove = new List<EntityUid>();

        foreach (var (gridUid, targetTime) in _pendingCleanup)
        {
            // Skip if the time hasn't elapsed yet
            if (currentTime < targetTime)
                continue;

            // Check if the entity still exists
            if (!EntityManager.EntityExists(gridUid))
            {
                toRemove.Add(gridUid);
                continue;
            }

            // Skip if this is a planet expedition grid
            if (HasComp<SalvageExpeditionComponent>(gridUid))
            {
                Logger.DebugS("gridcleanup", $"Update: Removing expedition grid {gridUid} from cleanup queue");
                toRemove.Add(gridUid);
                continue;
            }

            // Skip if this is a planet grid
            if (HasComp<PlanetMapComponent>(gridUid))
            {
                Logger.DebugS("gridcleanup", $"Update: Removing planet grid {gridUid} from cleanup queue");
                toRemove.Add(gridUid);
                continue;
            }

            // Skip if the parent map has an expedition component
            var xform = Transform(gridUid);
            var mapId = xform.MapID;
            var mapUid = _mapManager.GetMapEntityId(mapId);

            if (HasComp<SalvageExpeditionComponent>(mapUid))
            {
                Logger.DebugS("gridcleanup", $"Update: Removing grid {gridUid} on expedition map {mapUid} from cleanup queue");
                toRemove.Add(gridUid);
                continue;
            }

            if (HasComp<PlanetMapComponent>(mapUid))
            {
                Logger.DebugS("gridcleanup", $"Update: Removing grid {gridUid} on planet map {mapUid} from cleanup queue");
                toRemove.Add(gridUid);
                continue;
            }

            // Verify it still has a grid component
            if (!TryComp<MapGridComponent>(gridUid, out var grid))
            {
                toRemove.Add(gridUid);
                continue;
            }

            // Check tile count again to make sure it still needs to be deleted
            var tileCount = CountTiles((gridUid, grid));
            if (tileCount >= MinimumTiles)
            {
                toRemove.Add(gridUid);
                continue;
            }

            // Check to ensure no mobs will be deleted
            var mobQuery = AllEntityQuery<MobStateComponent, TransformComponent>();
            var entityCheck = false;
            while (mobQuery.MoveNext(out var mobUid, out _, out var mobxform))
            {
                if (mobxform.GridUid == null || mobxform.MapUid == null || mobxform.GridUid != xform.GridUid)
                    continue;

                Logger.DebugS("gridcleanup", $"Update: Mob {mobUid} detected on {gridUid}, removing grid from cleanup queue");
                toRemove.Add(gridUid);
                entityCheck = true;
                break;
            }

            if (entityCheck == true)
                continue;

            // Delete the grid immediately to prevent the possibility of a mob entering after deletion is queued
            Del(gridUid);
            Logger.DebugS("gridcleanup", $"Update: Deleting {gridUid} with {CountTiles((gridUid, grid))} tiles");
            toRemove.Add(gridUid);
        }

        // Remove processed grids from the pending list
        foreach (var gridUid in toRemove)
        {
            _pendingCleanup.Remove(gridUid);
        }
    }

    private int CountTiles(Entity<MapGridComponent> ent)
    {
        var grid = ent.Comp;
        var tileCount = 0;

        // Get AABB of the grid
        var aabb = grid.LocalAABB;

        // Convert to grid coordinates
        var localTL = new Vector2i((int)Math.Floor(aabb.Left), (int)Math.Floor(aabb.Bottom));
        var localBR = new Vector2i((int)Math.Ceiling(aabb.Right), (int)Math.Ceiling(aabb.Top));

        // Iterate through all tiles in the grid's area
        for (var x = localTL.X; x < localBR.X; x++)
        {
            for (var y = localTL.Y; y < localBR.Y; y++)
            {
                var position = new Vector2i(x, y);

                // Check if tile exists at position and is not empty
                var tile = grid.GetTileRef(position);
                if (!tile.Tile.IsEmpty)
                {
                    tileCount++;

                    // Early return if we've found enough tiles
                    if (tileCount >= MinimumTiles)
                        return tileCount;
                }
            }
        }

        return tileCount;
    }
}
