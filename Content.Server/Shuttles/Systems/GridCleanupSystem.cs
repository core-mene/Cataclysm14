using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.Shuttles.Systems;

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
    private const float CleanupDelay = 300.0f;

    // Dictionary to track grids scheduled for deletion
    private readonly Dictionary<EntityUid, TimeSpan> _pendingCleanup = new();

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to grid events
        SubscribeLocalEvent<GridStartupEvent>(OnGridStartup);
        SubscribeLocalEvent<MapGridComponent, TileChangedEvent>(OnTileChanged);
    }

    private void OnGridStartup(GridStartupEvent ev)
    {
        // Check newly created grids
        if (TryComp<MapGridComponent>(ev.EntityUid, out var grid))
            CheckGrid((ev.EntityUid, grid));
    }

    private void OnTileChanged(Entity<MapGridComponent> ent, ref TileChangedEvent args)
    {
        // When a grid is modified (tiles added/removed), check if it needs cleanup
        CheckGrid(ent);
    }

    private void CheckGrid(Entity<MapGridComponent> ent)
    {
        var gridUid = ent.Owner;
        var grid = ent.Comp;

        // Skip if already scheduled for deletion
        if (_pendingCleanup.ContainsKey(gridUid))
            return;

        // Count tiles
        var tileCount = CountTiles((gridUid, grid));

        // If the tile count is below our threshold, schedule it for deletion
        if (tileCount < MinimumTiles)
        {
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

            // Queue the grid for deletion
            QueueDel(gridUid);
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
        var localTL = new Vector2i((int) Math.Floor(aabb.Left), (int) Math.Floor(aabb.Bottom));
        var localBR = new Vector2i((int) Math.Ceiling(aabb.Right), (int) Math.Ceiling(aabb.Top));

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
