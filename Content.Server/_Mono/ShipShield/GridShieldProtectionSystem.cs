using Content.Shared._Mono.ShipShield;
using Content.Shared.Explosion;
using Robust.Shared.Map;

namespace Content.Server._Mono.ShipShield;

/// <summary>
/// System that handles protection from explosions for grids with active shield generators
/// </summary>
public sealed class GridShieldProtectionSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridShieldProtectedEntityComponent, GetExplosionResistanceEvent>(OnGetExplosionResistance);
    }

    /// <summary>
    /// Checks if an entity being damaged by an explosion should be protected by shield fields
    /// </summary>
    private void OnGetExplosionResistance(EntityUid uid, GridShieldProtectedEntityComponent component, ref GetExplosionResistanceEvent args)
    {
        // Set damage coefficient to 0 to nullify explosion damage
        args.DamageCoefficient = 0;
    }
}
