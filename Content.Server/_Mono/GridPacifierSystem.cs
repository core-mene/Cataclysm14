using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Mono;
using Content.Shared._Mono.Company;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Server.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._Mono;

/// <summary>
/// System that handles the GridPacifierComponent, which applies Pacified status to all organic entities on a grid.
/// </summary>
public sealed class GridPacifierSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridPacifierComponent, ComponentStartup>(OnGridPacifierStartup);
        SubscribeLocalEvent<GridPacifierComponent, ComponentShutdown>(OnGridPacifierShutdown);
        SubscribeLocalEvent<MoveEvent>(OnEntityMoved);
        SubscribeLocalEvent<EntParentChangedMessage>(OnEntityParentChanged);
        SubscribeLocalEvent<EntInsertedIntoContainerMessage>(OnEntityInsertedInContainer);
        SubscribeLocalEvent<EntRemovedFromContainerMessage>(OnEntityRemovedFromContainer);
    }

    private void OnGridPacifierStartup(EntityUid uid, GridPacifierComponent component, ComponentStartup args)
    {
        // Verify this is applied to a grid
        if (!HasComp<MapGridComponent>(uid))
        {
            Log.Warning($"GridPacifierComponent applied to non-grid entity {ToPrettyString(uid)}");
            return;
        }

        // Initialize the next update time for periodic checks
        component.NextUpdate = _gameTiming.CurTime + component.UpdateInterval;

        // Find all entities on the grid and process them (they'll be added to pending list with 5-second delay)
        var allEntitiesOnGrid = _lookup.GetEntitiesIntersecting(uid).ToHashSet();

        foreach (var entity in allEntitiesOnGrid)
        {
            // Skip the grid itself and entities inside containers (they'll be handled by container logic)
            if (entity == uid || _container.IsEntityInContainer(entity))
                continue;

            ProcessEntityOnGrid(uid, entity, component);
        }
    }

    private void OnGridPacifierShutdown(EntityUid uid, GridPacifierComponent component, ComponentShutdown args)
    {
        // When the component is removed, remove Pacified from all pacified entities
        foreach (var entity in component.PacifiedEntities.ToList())
        {
            if (EntityManager.EntityExists(entity))
            {
                RemovePacified(entity);
            }
        }

        component.PacifiedEntities.Clear();
        component.PendingEntities.Clear();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;

        // Find all grids with a GridPacifierComponent
        var query = EntityQueryEnumerator<GridPacifierComponent, MapGridComponent>();
        while (query.MoveNext(out var uid, out var component, out _))
        {
            // Process pending entities that have waited 5 seconds
            ProcessPendingEntities(uid, component, curTime);

            // Check if it's time for the periodic update
            if (curTime < component.NextUpdate)
                continue;

            // Schedule the next update
            component.NextUpdate = curTime + component.UpdateInterval;

            // Perform a complete re-check of all entities on the grid
            PerformGridwideCheck(uid, component);
        }
    }

    /// <summary>
    /// Processes entities that have been pending pacification for 1 second
    /// </summary>
    private void ProcessPendingEntities(EntityUid gridUid, GridPacifierComponent component, TimeSpan curTime)
    {
        var entitiesToProcess = new List<EntityUid>();

        // Find entities that have been pending for 1+ seconds
        foreach (var (entityUid, entryTime) in component.PendingEntities.ToList())
        {
            if (curTime - entryTime >= TimeSpan.FromSeconds(1))
            {
                // Check if entity still exists and is still on the grid
                if (EntityManager.EntityExists(entityUid) && IsEntityOnGrid(entityUid, gridUid))
                {
                    entitiesToProcess.Add(entityUid);
                }

                // Remove from pending regardless (either will be processed or no longer valid)
                component.PendingEntities.Remove(entityUid);
            }
        }

        // Re-run full checks for entities that have waited long enough
        foreach (var entityUid in entitiesToProcess)
        {
            ProcessEntityForPacification(gridUid, entityUid, component);
        }
    }

    /// <summary>
    /// Performs a complete check of all entities on the grid, applying or removing
    /// pacification as needed based on current conditions.
    /// </summary>
    private void PerformGridwideCheck(EntityUid gridUid, GridPacifierComponent component)
    {
        // First, get all entities currently on the grid
        var entitiesOnGrid = _lookup.GetEntitiesIntersecting(gridUid).ToHashSet();

        // Create a copy of the current pacified entities list for tracking which ones are no longer on the grid
        var stillPacifiedEntities = new HashSet<EntityUid>();
        var stillPendingEntities = new HashSet<EntityUid>();

        // Process all entities currently on the grid
        foreach (var entity in entitiesOnGrid)
        {
            // Skip the grid itself and entities inside containers
            if (entity == gridUid || _container.IsEntityInContainer(entity))
                continue;

            // For entities not yet tracked, add them to pending
            if (!component.PacifiedEntities.Contains(entity) && !component.PendingEntities.ContainsKey(entity))
            {
                ProcessEntityOnGrid(gridUid, entity, component);
            }

            // Track entities that are still on the grid
            if (component.PacifiedEntities.Contains(entity))
                stillPacifiedEntities.Add(entity);
            if (component.PendingEntities.ContainsKey(entity))
                stillPendingEntities.Add(entity);
        }

        // Find entities that are no longer on the grid or no longer meet pacification criteria
        var entitiesToRemove = component.PacifiedEntities.Where(e => !stillPacifiedEntities.Contains(e)).ToList();
        var pendingToRemove = component.PendingEntities.Keys.Where(e => !stillPendingEntities.Contains(e)).ToList();

        // Remove pacification from entities no longer on grid
        foreach (var entity in entitiesToRemove)
        {
            if (EntityManager.EntityExists(entity))
            {
                RemovePacified(entity);
                component.PacifiedEntities.Remove(entity);
            }
        }

        // Remove pending entities no longer on grid
        foreach (var entity in pendingToRemove)
        {
            component.PendingEntities.Remove(entity);
        }
    }

    private void OnEntityMoved(ref MoveEvent args)
    {
        // Check if the entity moved to or from a grid with GridPacifierComponent
        var entity = args.Entity;

        // Skip entities in containers as they're handled by container events
        if (_container.IsEntityInContainer(entity.Owner))
            return;

        // If the entity left a grid with GridPacifierComponent, clean up pacification/pending status
        if (TryGetGridPacifierComponent(args.OldPosition.EntityId, out var oldGridComp) &&
            oldGridComp != null && args.NewPosition.EntityId != args.OldPosition.EntityId)
        {
            if (oldGridComp.PacifiedEntities.Contains(entity.Owner))
            {
                RemovePacified(entity.Owner);
                oldGridComp.PacifiedEntities.Remove(entity.Owner);
            }
            oldGridComp.PendingEntities.Remove(entity.Owner);
        }

        // If the entity moved to a grid with GridPacifierComponent, check if it should get processed
        if (args.NewPosition.EntityId.IsValid() &&
            TryGetGridPacifierComponent(args.NewPosition.EntityId, out var newGridComp) &&
            newGridComp != null &&
            !newGridComp.PacifiedEntities.Contains(entity.Owner) &&
            !newGridComp.PendingEntities.ContainsKey(entity.Owner))
        {
            ProcessEntityOnGrid(args.NewPosition.EntityId, entity.Owner, newGridComp);
        }
    }

    private void OnEntityParentChanged(ref EntParentChangedMessage args)
    {
        var entity = args.Entity;

        // Skip entities in containers as they're handled by container events
        if (_container.IsEntityInContainer(entity))
            return;

        // If the entity was on a pacified grid and left
        if (args.OldParent.HasValue && args.OldParent.Value.IsValid() &&
            TryGetGridPacifierComponent(args.OldParent.Value, out var oldGridComp) &&
            oldGridComp != null)
        {
            // Entity moved away from a pacified grid - clean up pacification/pending status
            if (oldGridComp.PacifiedEntities.Contains(entity))
            {
                RemovePacified(entity);
                oldGridComp.PacifiedEntities.Remove(entity);
            }
            oldGridComp.PendingEntities.Remove(entity);
        }

        // If the entity moved to a pacified grid
        if (args.Transform.ParentUid.IsValid() &&
            TryGetGridPacifierComponent(args.Transform.ParentUid, out var newGridComp) &&
            newGridComp != null &&
            !newGridComp.PacifiedEntities.Contains(entity) &&
            !newGridComp.PendingEntities.ContainsKey(entity))
        {
            ProcessEntityOnGrid(args.Transform.ParentUid, entity, newGridComp);
        }
    }

    // Handler for entities inserted into containers
    private void OnEntityInsertedInContainer(EntInsertedIntoContainerMessage args)
    {
        var entity = args.Entity;
        // Entity was pacified or pending but is now in a container - remove protection/pending status
        // Iterate over all grids that might be pacifying this entity
        var query = EntityQueryEnumerator<GridPacifierComponent, TransformComponent>();
        while (query.MoveNext(out var gridUid, out var gridComp, out _))
        {
            if (gridComp.PacifiedEntities.Contains(entity))
            {
                RemovePacified(entity);
                gridComp.PacifiedEntities.Remove(entity);
            }
            gridComp.PendingEntities.Remove(entity);
        }
    }

    // Handler for entities removed from containers
    private void OnEntityRemovedFromContainer(EntRemovedFromContainerMessage args)
    {
        var entity = args.Entity;
        // If the entity is now directly on a pacified grid
        if (TryComp<TransformComponent>(entity, out var xform) &&
            xform.GridUid.HasValue &&
            TryGetGridPacifierComponent(xform.GridUid.Value, out var gridComp) &&
            gridComp != null &&
            !gridComp.PacifiedEntities.Contains(entity) &&
            !gridComp.PendingEntities.ContainsKey(entity))
        {
            ProcessEntityOnGrid(xform.GridUid.Value, entity, gridComp);
        }
    }

    /// <summary>
    /// Process an entity on a grid - adds to pending list for delayed processing
    /// </summary>
    private void ProcessEntityOnGrid(EntityUid gridUid, EntityUid entityUid, GridPacifierComponent component)
    {
        // Skip entities that are already pacified by this component or pending pacification
        if (component.PacifiedEntities.Contains(entityUid) || component.PendingEntities.ContainsKey(entityUid))
            return;

        // Add entity to pending list with current timestamp (1-second delay before checks)
        component.PendingEntities[entityUid] = _gameTiming.CurTime;
    }

    /// <summary>
    /// Performs the actual pacification checks and applies Pacified if appropriate
    /// </summary>
    private void ProcessEntityForPacification(EntityUid gridUid, EntityUid entityUid, GridPacifierComponent component)
    {
        // Only apply Pacified to organic entities
        if (!IsOrganic(entityUid))
            return;

        // Skip entities that already have the Pacified component
        if (HasComp<PacifiedComponent>(entityUid))
            return;

        // Skip if already pacified by this component
        if (component.PacifiedEntities.Contains(entityUid))
            return;

        // Check if this is a player entity and if so, check their playtime
        if (_playerManager.TryGetSessionByEntity(entityUid, out var session))
        {
            // Only affect players with less than 1 hour of overall playtime
            var overallPlaytime = _playTimeTracking.GetOverallPlaytime(session);
            if (overallPlaytime >= TimeSpan.FromHours(1))
            {
                return;
            }
        }

        // Check if the entity is from an exempt company
        if (TryComp<CompanyComponent>(entityUid, out var companyComp) &&
            !string.IsNullOrEmpty(companyComp.CompanyName))
        {
            // Check against all three exempt company slots
            if ((!string.IsNullOrEmpty(component.ExemptCompany1) && companyComp.CompanyName == component.ExemptCompany1) ||
                (!string.IsNullOrEmpty(component.ExemptCompany2) && companyComp.CompanyName == component.ExemptCompany2) ||
                (!string.IsNullOrEmpty(component.ExemptCompany3) && companyComp.CompanyName == component.ExemptCompany3))
            {
                // Entity is from an exempt company - don't pacify
                return;
            }
        }

        // All checks passed - apply pacification
        ApplyPacified(gridUid, entityUid, component);
    }

    /// <summary>
    /// Applies Pacified to an entity and adds it to the pacified entities list
    /// </summary>
    private void ApplyPacified(EntityUid gridUid, EntityUid entityUid, GridPacifierComponent component)
    {
        // Skip if the entity is already pacified by this component
        if (component.PacifiedEntities.Contains(entityUid))
            return;

        // Skip if the entity already has a Pacified component (safety check)
        if (HasComp<PacifiedComponent>(entityUid))
            return;

        // Apply Pacified
        EnsureComp<PacifiedComponent>(entityUid);
        component.PacifiedEntities.Add(entityUid);
    }

    /// <summary>
    /// Removes Pacified from an entity
    /// </summary>
    private void RemovePacified(EntityUid entityUid)
    {
        if (HasComp<PacifiedComponent>(entityUid))
        {
            RemComp<PacifiedComponent>(entityUid);
        }
    }

    /// <summary>
    /// Helper method to get the GridPacifierComponent from a grid entity
    /// </summary>
    private bool TryGetGridPacifierComponent(EntityUid? gridUid, [NotNullWhen(true)] out GridPacifierComponent? component)
    {
        component = null;

        if (gridUid == null || !gridUid.Value.IsValid() || !EntityManager.EntityExists(gridUid.Value))
            return false;

        return TryComp(gridUid.Value, out component);
    }

    /// <summary>
    /// Checks if an entity is currently on the specified grid
    /// </summary>
    private bool IsEntityOnGrid(EntityUid entityUid, EntityUid gridUid)
    {
        if (!TryComp<TransformComponent>(entityUid, out var xform))
            return false;

        return xform.GridUid == gridUid;
    }

    /// <summary>
    /// Checks if an entity is organic (i.e., has a mind or is a mob)
    /// </summary>
    private bool IsOrganic(EntityUid entityUid)
    {
        // Skip ghosts
        if (HasComp<GhostComponent>(entityUid))
            return false;

        // Check if we have a player entity that's either still around or alive and may come back
        if (_mind.TryGetMind(entityUid, out var mind, out var mindComp) &&
            (mindComp.Session != null || !_mind.IsCharacterDeadPhysically(mindComp)))
        {
            return true;
        }

        // Also consider anything with a MobStateComponent as organic
        if (HasComp<MobStateComponent>(entityUid))
        {
            return true;
        }

        return false;
    }
}
