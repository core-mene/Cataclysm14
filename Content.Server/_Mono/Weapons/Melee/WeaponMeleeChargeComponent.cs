using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Server._Mono.Weapons.Melee;

/// <summary>
/// Toggles the weapon for <see cref="ActiveTime"/> amount of time. After this time passes, <see cref="Cooldown"/> is activated
/// Used in pair with ItemToggleMeleeWeaponComponent
///  </summary>
[RegisterComponent]
public sealed partial class WeaponMeleeChargeComponent : Component
{
    [DataField]
    public float ActiveTime = 1f;

    [DataField]
    public float Cooldown = 1f;

    [DataField]
    public bool IsActive = false;

    [DataField]
    public TimeSpan CurrentCooldown = TimeSpan.Zero;

    [DataField]
    public TimeSpan CurrentActiveTime = TimeSpan.Zero;

    [DataField]
    public DamageSpecifier CooldownDamagePenalty =  new DamageSpecifier();
}
