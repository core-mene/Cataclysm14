namespace Content.Server._Mono.FireControl;

/// <summary>
/// Tag component to mark entities that should be exempt from explosion damage
/// from projectiles fired on the same grid
/// </summary>
[RegisterComponent]
public sealed partial class FireControlNegationComponent : Component
{
    /// <summary>
    /// The projectile that would have caused the explosion
    /// </summary>
    [DataField("projectileSource")]
    public EntityUid? ProjectileSource;

    /// <summary>
    /// The grid this projectile came from
    /// </summary>
    [DataField("sourceGrid")]
    public EntityUid? SourceGrid;
}
