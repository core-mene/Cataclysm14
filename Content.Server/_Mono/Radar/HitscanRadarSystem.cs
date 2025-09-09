using Robust.Shared.Map;
using System.Numerics;
using Content.Server._Mono.FireControl;
using Robust.Shared.Timing;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Hitscan.Components;

namespace Content.Server._Mono.Radar;

/// <summary>
/// System that handles radar visualization for hitscan projectiles
/// </summary>
public sealed partial class HitscanRadarSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Dictionary to track entities that should be deleted after a specific time
    private readonly Dictionary<EntityUid, TimeSpan> _pendingDeletions = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HitscanAmmoComponent, HitscanRaycastFiredEvent>(OnHitscanRaycastFired);
        SubscribeLocalEvent<HitscanRadarComponent, ComponentShutdown>(OnHitscanRadarShutdown);
    }

    private void OnHitscanRaycastFired(Entity<HitscanAmmoComponent> ent, ref HitscanRaycastFiredEvent ev)
    {
        var shooter = ev.Shooter ?? ev.Gun;

        // Only create hitscan radar blips for entities with FireControllable component
        if (!HasComp<FireControllableComponent>(shooter))
            return;

        // Create a new entity for the hitscan radar visualization
        // Use the shooter's position to spawn the entity
        var shooterCoords = new EntityCoordinates(shooter, Vector2.Zero);
        var uid = Spawn(null, shooterCoords);

        // Add the hitscan radar component
        var hitscanRadar = EnsureComp<HitscanRadarComponent>(uid);

        // Determine start position using proper coordinate transformation
        var startPos = _transform.ToMapCoordinates(ev.FromCoordinates).Position;

        // Compute end position using raycast distance
        var endPos = startPos + ev.ShotDirection.Normalized() * ev.DistanceTried;

        // Set the origin grid if available
        hitscanRadar.OriginGrid = Transform(shooter).GridUid;

        // Set the start and end coordinates
        hitscanRadar.StartPosition = startPos;
        hitscanRadar.EndPosition = endPos;

        // Inherit component settings from the shooter entity
        InheritShooterSettings(shooter, hitscanRadar);

        // Schedule entity for deletion after its lifetime expires
        var deleteTime = _timing.CurTime + TimeSpan.FromSeconds(hitscanRadar.LifeTime);
        _pendingDeletions[uid] = deleteTime;
    }

    /// <summary>
    /// Inherits radar settings from the shooter entity if available
    /// </summary>
    private void InheritShooterSettings(EntityUid shooter, HitscanRadarComponent hitscanRadar)
    {
        // Try to inherit from shooter's existing HitscanRadarComponent if present
        if (TryComp<HitscanRadarComponent>(shooter, out var shooterHitscanRadar))
        {
            hitscanRadar.RadarColor = shooterHitscanRadar.RadarColor;
            hitscanRadar.LineThickness = shooterHitscanRadar.LineThickness;
            hitscanRadar.Enabled = shooterHitscanRadar.Enabled;
            hitscanRadar.LifeTime = shooterHitscanRadar.LifeTime;
        }
    }

    private void OnHitscanRadarShutdown(Entity<HitscanRadarComponent> ent, ref ComponentShutdown args)
    {
        // Only delete the entity if it's a temporary hitscan trail entity (tracked in _pendingDeletions)
        // Don't delete legitimate entities that have the component added manually
        if (_pendingDeletions.ContainsKey(ent))
        {
            // This is a temporary hitscan trail entity, safe to delete
            QueueDel(ent);
            _pendingDeletions.Remove(ent);
        }
        // For legitimate entities, just remove from pending deletions if present (shouldn't be there anyway)
        else
        {
            _pendingDeletions.Remove(ent);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Handle pending deletions
        if (_pendingDeletions.Count > 0)
        {
            var currentTime = _timing.CurTime;
            var toRemove = new List<EntityUid>();

            foreach (var (entity, deleteTime) in _pendingDeletions)
            {
                if (currentTime >= deleteTime)
                {
                    if (!Deleted(entity))
                        QueueDel(entity);
                    toRemove.Add(entity);
                }
            }

            foreach (var entity in toRemove)
            {
                _pendingDeletions.Remove(entity);
            }
        }
    }
}
