using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared.Humanoid;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Mono.Humanoid;

/// <summary>
/// System that adjusts physics hitboxes of humanoid entities based on their height and weight (width).
/// </summary>
public sealed class HumanoidPhysicsScalingSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <summary>
    /// The default radius for humanoid hitboxes. This is the baseline from which we scale.
    /// </summary>
    private const float DefaultHitboxRadius = 0.35f;

    public override void Initialize()
    {
        base.Initialize();

        // Listen for when a humanoid appearance is loaded (character creation/spawning)
        SubscribeLocalEvent<HumanoidAppearanceComponent, ProfileLoadFinishedEvent>(OnProfileLoaded);

        // Listen for when humanoid appearance changes (admin commands, mutations, etc.)
        SubscribeLocalEvent<HumanoidAppearanceComponent, ComponentShutdown>(OnHumanoidShutdown);
    }

    private void OnProfileLoaded(EntityUid uid, HumanoidAppearanceComponent component, ProfileLoadFinishedEvent args)
    {
        UpdatePhysicsHitbox(uid, component);
    }

    private void OnHumanoidShutdown(EntityUid uid, HumanoidAppearanceComponent component, ComponentShutdown args)
    {
        // Reset hitbox to default when component is removed
        if (TryComp<FixturesComponent>(uid, out var fixtures))
        {
            ResetToDefaultHitbox(uid, fixtures);
        }
    }

    /// <summary>
    /// Public method to manually update a humanoid's hitbox
    /// </summary>
    /// <param name="uid">The entity to update</param>
    public void UpdateHitbox(EntityUid uid)
    {
        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            UpdatePhysicsHitbox(uid, humanoid);
        }
    }

    /// <summary>
    /// Public method to set specific height and width then update hitbox.
    /// </summary>
    /// <param name="uid">The entity to update</param>
    /// <param name="height">Height multiplier (1.0 = default)</param>
    /// <param name="width">Width multiplier (1.0 = default)</param>
    public void UpdateHitbox(EntityUid uid, float height, float width)
    {
        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            humanoid.Height = height;
            humanoid.Width = width;
            UpdatePhysicsHitbox(uid, humanoid);
        }
    }

    /// <summary>
    /// Updates the physics hitbox based on the humanoid's height and width.
    /// </summary>
    /// <param name="uid">The entity to update</param>
    /// <param name="humanoid">The humanoid appearance component</param>
    public void UpdatePhysicsHitbox(EntityUid uid, HumanoidAppearanceComponent humanoid)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        // Calculate the new radius based on height and width
        // We take the average of height and width for a circular hitbox
        var scale = (humanoid.Height + humanoid.Width) / 2.0f;
        var newRadius = DefaultHitboxRadius * scale;

        // Update all circular fixtures (most humanoids should have just one main fixture)
        foreach (var (fixtureId, fixture) in fixtures.Fixtures)
        {
            if (fixture.Shape is PhysShapeCircle circle)
            {
                _physics.SetRadius(uid, fixtureId, fixture, circle, newRadius, fixtures);
            }
        }

        // Log the change for debugging
        Log.Debug($"Updated physics hitbox for {ToPrettyString(uid)}: Height={humanoid.Height:F2}, Width={humanoid.Width:F2}, Radius={newRadius:F2}");
    }

    /// <summary>
    /// Resets a humanoid's hitbox to the default size.
    /// </summary>
    /// <param name="uid">The entity to reset</param>
    /// <param name="fixtures">The fixtures component</param>
    private void ResetToDefaultHitbox(EntityUid uid, FixturesComponent fixtures)
    {
        foreach (var (fixtureId, fixture) in fixtures.Fixtures)
        {
            if (fixture.Shape is PhysShapeCircle circle)
            {
                _physics.SetRadius(uid, fixtureId, fixture, circle, DefaultHitboxRadius, fixtures);
            }
        }
    }
}
