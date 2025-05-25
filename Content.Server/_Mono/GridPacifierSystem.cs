using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;

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

        // Find all entities on the grid and apply Pacified to them if they're organic
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
    }

    private void OnEntityMoved(ref MoveEvent args)
    {
        // Check if the entity moved to or from a grid with GridPacifierComponent
        var entity = args.Entity;

        // Skip entities in containers as they're handled by container events
        if (_container.IsEntityInContainer(entity.Owner))
            return;

        // If the entity is already pacified by a GridPacifierComponent, check if it left the grid
        if (TryGetGridPacifierComponent(args.OldPosition.EntityId, out var oldGridComp) &&
            oldGridComp != null && oldGridComp.PacifiedEntities.Contains(entity.Owner) &&
            args.NewPosition.EntityId != args.OldPosition.EntityId)
        {
            RemovePacified(entity.Owner);
            oldGridComp.PacifiedEntities.Remove(entity.Owner);
        }

        // If the entity moved to a grid with GridPacifierComponent, check if it should get Pacified
        if (args.NewPosition.EntityId.IsValid() &&
            TryGetGridPacifierComponent(args.NewPosition.EntityId, out var newGridComp) &&
            newGridComp != null && !newGridComp.PacifiedEntities.Contains(entity.Owner))
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
            oldGridComp != null && oldGridComp.PacifiedEntities.Contains(entity))
        {
            // Entity moved away from a pacified grid - remove Pacified
            RemovePacified(entity);
            oldGridComp.PacifiedEntities.Remove(entity);
        }

        // If the entity moved to a pacified grid
        if (args.Transform.ParentUid.IsValid() &&
            TryGetGridPacifierComponent(args.Transform.ParentUid, out var newGridComp) &&
            newGridComp != null && !newGridComp.PacifiedEntities.Contains(entity))
        {
            ProcessEntityOnGrid(args.Transform.ParentUid, entity, newGridComp);
        }
    }

    // Handler for entities inserted into containers
    private void OnEntityInsertedInContainer(EntInsertedIntoContainerMessage args)
    {
        var entity = args.Entity;
        // Entity was pacified but is now in a container - remove protection
        // Iterate over all grids that might be pacifying this entity
        var query = EntityQueryEnumerator<GridPacifierComponent, TransformComponent>();
        while (query.MoveNext(out var gridUid, out var gridComp, out _))
        {
            if (gridComp.PacifiedEntities.Contains(entity))
            {
                RemovePacified(entity);
                gridComp.PacifiedEntities.Remove(entity);
            }
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
            !gridComp.PacifiedEntities.Contains(entity))
        {
            ProcessEntityOnGrid(xform.GridUid.Value, entity, gridComp);
        }
    }

    /// <summary>
    /// Process an entity on a grid and apply Pacified if appropriate
    /// </summary>
    private void ProcessEntityOnGrid(EntityUid gridUid, EntityUid entityUid, GridPacifierComponent component)
    {
        // Only apply Pacified to organic entities
        if (!IsOrganic(entityUid))
            return;

        ApplyPacified(gridUid, entityUid, component);
    }

    /// <summary>
    /// Applies Pacified to an entity and adds it to the pacified entities list
    /// </summary>
    private void ApplyPacified(EntityUid gridUid, EntityUid entityUid, GridPacifierComponent component)
    {
        // Skip if the entity is already pacified
        if (component.PacifiedEntities.Contains(entityUid))
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