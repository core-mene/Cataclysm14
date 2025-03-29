using Content.Shared.Projectiles;
using Content.Shared.Theta.ShipEvent.Components;

namespace Content.Shared.Theta.ShipEvent.CircularShield;

/// <summary>
/// This system handles projectile phasing for circular shields without
/// creating duplicate subscriptions to the PreventCollideEvent.
/// </summary>
public sealed class CircularShieldProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedCircularShieldSystem _shieldSys = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to the ProjectileCollisionAttemptEvent to check if the projectile should phase through entities
        SubscribeLocalEvent<ProjectileCollisionAttemptEvent>(OnProjectileCollisionAttempt);
    }

    /// <summary>
    /// Handles the ProjectileCollisionAttemptEvent and cancels the collision if the projectile should phase through entities
    /// </summary>
    private void OnProjectileCollisionAttempt(ref ProjectileCollisionAttemptEvent args)
    {
        if (ShouldProjectilePhase(args.Projectile))
        {
            args.Cancelled = true;
        }
    }

    /// <summary>
    /// Checks if a projectile should phase through entities based on shield effects
    /// </summary>
    public bool ShouldProjectilePhase(EntityUid projectileUid)
    {
        // Don't do anything if the entity is not a projectile
        if (!HasComp<ProjectileComponent>(projectileUid))
            return false;

        var query = EntityQueryEnumerator<TransformComponent, CircularShieldComponent>();
        bool shouldPhase = false;

        while (query.MoveNext(out var shieldUid, out var transform, out var shield))
        {
            if (shield.Effects == null)
                continue;

            // Check if the projectile is in the shield range based on grid center
            if (!_shieldSys.EntityInShield(shieldUid, shield, projectileUid, _transform))
                continue;

            foreach (var effect in shield.Effects)
            {
                if (effect is not CircularShieldTempSpeedChangeEffect speedEffect || !speedEffect.ProjectilePhasing)
                    continue;

                // First check if the projectile is already in the tracked list
                bool wasAlreadyTracked = speedEffect.TrackedProjectiles.Contains(projectileUid);

                // If projectile is not in tracked list yet, try to add it through OnShieldEnter
                if (!wasAlreadyTracked)
                {
                    speedEffect.OnShieldEnter(projectileUid, shield);
                }

                // NOW check if the projectile was actually added to the tracked list
                // This handles the case where OnShieldEnter decided not to track it
                // (like if it's from the same grid)
                if (speedEffect.TrackedProjectiles.Contains(projectileUid))
                {
                    return true;
                }
            }
        }

        return shouldPhase;
    }
}
