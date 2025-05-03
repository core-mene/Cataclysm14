using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Shuttles.Components;

namespace Content.Server.FTL;

/// <summary>
/// This system applies crushing damage to entities that fall into FTL maps without being on a grid
/// </summary>
public sealed class FTLDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to the event that's raised when an entity's map changes
        SubscribeLocalEvent<TransformComponent, EntParentChangedMessage>(OnEntParentChanged);
    }

    private void OnEntParentChanged(EntityUid uid, TransformComponent transform, ref EntParentChangedMessage args)
    {
        // Skip if the entity is deleted or queued for deletion
        if (EntityManager.Deleted(uid) || EntityManager.IsQueuedForDeletion(uid))
            return;
            
        if (!transform.MapUid.HasValue)
            return;

        var mapUid = transform.MapUid.Value;

        // Check if the entity has moved to an FTL map
        if (HasComp<FTLMapComponent>(mapUid))
        {
            // Only apply damage if the entity is not on a valid grid
            if (!transform.GridUid.HasValue)
            {
                ApplyCrushDamage(uid);
            }
        }
    }

    private void ApplyCrushDamage(EntityUid uid)
    {
        // Skip the damage if the entity doesn't have a damageable component
        if (!HasComp<DamageableComponent>(uid))
            return;

        // Create damage specification for 1000 blunt damage
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", FixedPoint2.New(1000));

        // Apply the damage to the entity
        _damageableSystem.TryChangeDamage(uid, damage, true);
    }
}
