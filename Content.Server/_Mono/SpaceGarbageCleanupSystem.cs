using Content.Server.Light.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Shuttles.Components;
using Content.Shared._Mono.CCVar;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Light.Components;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Mono;

/// <summary>
///     Deletes all entities with SpaceGarbageComponent.
/// </summary>
public sealed class SpaceGarbageCleanupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private TimeSpan _nextCleanup = TimeSpan.Zero;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        // Skip if it's not time for cleanup yet
        if (curTime < _nextCleanup)
            return;

        // Schedule next cleanup based on CVar
        var cleanupInterval = TimeSpan.FromSeconds(_cfg.GetCVar(MonoCVars.SpaceGarbageCleanupInterval));
        _nextCleanup = curTime + cleanupInterval;

        // Find all entities with SpaceGarbageComponent and delete them
        var query = EntityQueryEnumerator<SpaceGarbageComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            // Skip deletion if the entity is inside a container.
            if (_container.IsEntityInContainer(uid))
                continue;

            // Skip deletion if the entity has a LightBulb component
            if (HasComp<LightBulbComponent>(uid))
                continue;

            // Skip deletion if the entity has a Food component. Protect my pizzas!
            if (HasComp<FoodComponent>(uid))
                continue;

            // Skip deletion if the entity has a Utensil component. Protect my sporks!
            if (HasComp<UtensilComponent>(uid))
                continue;

            // Skip deletion if the entity has a Hypospray component. Protect my medipens!
            if (HasComp<HyposprayComponent>(uid))
                continue;

            // Skip deletion if the entity has an ExpendableLightComponent.
            if (HasComp<ExpendableLightComponent>(uid))
                continue;

            // For drinks, only skip deletion if they have solution (are not empty)
            if (HasComp<DrinkComponent>(uid))
            {
                if (TryComp<DrinkComponent>(uid, out var drinkComp) &&
                    _solutionContainer.TryGetSolution(uid, drinkComp.Solution, out _, out var solution) &&
                    solution.Volume > 0)
                {
                    continue; // Skip deletion, drink has solution
                }
                // If no solution or empty solution, fall through to delete
            }

            // Delete the entity
            QueueDel(uid);
        }
    }
}
