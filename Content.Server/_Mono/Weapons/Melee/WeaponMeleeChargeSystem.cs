using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Server._Mono.Weapons.Melee;

/// <summary>
/// This handles...
/// </summary>
public sealed class MeleeChargeSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private HashSet<Entity<WeaponMeleeChargeComponent>> _activeWeapons = new();
    public override void Initialize()
    {
        SubscribeLocalEvent<WeaponMeleeChargeComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<WeaponMeleeChargeComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<WeaponMeleeChargeComponent, ItemToggledEvent>(OnToggle);
        SubscribeLocalEvent<WeaponMeleeChargeComponent, ItemToggleActivateAttemptEvent>(OnToggleAttempt);
    }

    private void OnExamined(Entity<WeaponMeleeChargeComponent> ent, ref ExaminedEvent args)
    {
        if (InCooldown(ent))
            args.PushMarkup(Loc.GetString("melee-charge-weakened"));
    }

    private void OnMeleeHit(Entity<WeaponMeleeChargeComponent> ent, ref MeleeHitEvent args)
    {
        if (InCooldown(ent))
        {
            args.BonusDamage += ent.Comp.CooldownDamagePenalty;
            return;
        }

        if (!ent.Comp.IsActive)
            return;

        TryDeactivate(ent);
    }

    public void OnToggleAttempt(Entity<WeaponMeleeChargeComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (!InCooldown(ent))
            return;

        _popup.PopupEntity(Loc.GetString("melee-charge-remaining-cooldown", ("remainingCooldown", CooldownToSeconds(ent))),
            args.User ?? ent);

        args.Cancelled = true;
    }

    private void OnToggle(Entity<WeaponMeleeChargeComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated)
        {
            ent.Comp.CurrentActiveTime = TimeSpan.FromSeconds(ent.Comp.ActiveTime) + _timing.CurTime;
            ent.Comp.IsActive = true;
            _activeWeapons.Add(ent);
        }
        else
        {
            TryDeactivate(ent);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_activeWeapons.Count == 0)
            return;

        foreach (var ent in _activeWeapons)
        {
            if (ent.Comp.CurrentActiveTime > _timing.CurTime)
                continue;

            TryDeactivate(ent);
        }
    }

    private void TryDeactivate(Entity<WeaponMeleeChargeComponent> ent)
    {
        if(!_toggle.TryDeactivate(ent.Owner))
            return;

        ent.Comp.IsActive = false;
        ent.Comp.CurrentCooldown = TimeSpan.FromSeconds(ent.Comp.Cooldown) + _timing.CurTime;
        _activeWeapons.Remove(ent);
    }

    private bool InCooldown(Entity<WeaponMeleeChargeComponent> ent)
    {
        return ent.Comp.CurrentCooldown > _timing.CurTime;
    }

    private int CooldownToSeconds(Entity<WeaponMeleeChargeComponent> ent)
    {
        return (ent.Comp.CurrentCooldown - _timing.CurTime).Seconds + 1; // Adding 1 for it to be more accurate
    }
}
