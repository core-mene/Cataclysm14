using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Popups.GridNameDisplay;

/// <summary>
/// This system handles the <see cref="VisitedGridsComponent"/> which tracks which grids
/// a player has visited to avoid showing redundant grid name popups.
/// </summary>
public sealed class VisitedGridsSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        // Component state handling
        SubscribeLocalEvent<VisitedGridsComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<VisitedGridsComponent, ComponentHandleState>(OnHandleState);

        // Grid removal events
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
    }

    private void OnGetState(EntityUid uid, VisitedGridsComponent component, ref ComponentGetState args)
    {
        // Create a clean list of NetEntities, ensuring we only include entities that still exist
        var netUids = new HashSet<NetEntity>();

        foreach (var gridUid in component.VisitedGridUids)
        {
            if (!_entityManager.TryGetComponent(gridUid, out MetaDataComponent? meta))
                continue;

            netUids.Add(_entityManager.GetNetEntity(gridUid));
        }

        // Update the component's NetEntity set for future reference
        component.VisitedGridNetUids = netUids;

        // Return the clean state
        args.State = new VisitedGridsComponentState(netUids);
    }

    private void OnHandleState(EntityUid uid, VisitedGridsComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not VisitedGridsComponentState state)
            return;

        // Update NetEntity tracking
        component.VisitedGridNetUids = state.VisitedGridNetUids;

        // Resolve NetEntities back to EntityUids, filtering out any that don't exist
        var gridUids = new HashSet<EntityUid>();
        foreach (var netUid in state.VisitedGridNetUids)
        {
            if (_entityManager.TryGetEntity(netUid, out var gridUid) && _entityManager.EntityExists(gridUid.Value))
                gridUids.Add(gridUid.Value);
        }

        component.VisitedGridUids = gridUids;
    }

    private void OnGridRemoval(GridRemovalEvent ev)
    {
        var removedUid = ev.EntityUid;
        var removedNetUid = _entityManager.GetNetEntity(removedUid);

        // Iterate through all components and remove references to the deleted grid
        var query = EntityQueryEnumerator<VisitedGridsComponent>();
        while (query.MoveNext(out var uid, out var visitedGrids))
        {
            var modified = false;

            if (visitedGrids.VisitedGridUids.Remove(removedUid))
                modified = true;

            if (visitedGrids.VisitedGridNetUids.Remove(removedNetUid))
                modified = true;

            // Mark the component as dirty if it was modified
            if (modified)
                Dirty(uid, visitedGrids);
        }
    }

    /// <summary>
    /// Adds a grid to the list of visited grids for the specified entity
    /// </summary>
    public void AddVisitedGrid(EntityUid uid, EntityUid gridUid, VisitedGridsComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.VisitedGridUids.Add(gridUid))
        {
            component.VisitedGridNetUids.Add(_entityManager.GetNetEntity(gridUid));
            Dirty(uid, component);
        }
    }

    /// <summary>
    /// Checks if an entity has visited a specific grid
    /// </summary>
    public bool HasVisitedGrid(EntityUid uid, EntityUid gridUid, VisitedGridsComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.VisitedGridUids.Contains(gridUid);
    }
}
