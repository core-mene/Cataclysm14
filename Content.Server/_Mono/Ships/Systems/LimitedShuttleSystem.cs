// SPDX-FileCopyrightText: 2025 sleepyyapril
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Power.Components;
using Content.Shared._Mono.Ships.Components;
using Content.Shared._Mono.Shipyard;
using Content.Shared._NF.Shipyard;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Mono.Ships.Systems;

/// <summary>
/// This handles shuttles with a limit.
/// </summary>
public sealed class LimitedShuttleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ShuttleDeedSystem _shuttleDeed = default!;

    private TimeSpan _lastUpdate = TimeSpan.Zero;
    private TimeSpan _interval = TimeSpan.FromMinutes(1);

    private const double PercentageUnpoweredForInactive = 0.75;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VesselComponent, AttemptShipyardShuttlePurchaseEvent>(OnAttemptShuttlePurchase);
        SubscribeLocalEvent<VesselComponent, ShipyardShuttlePurchaseEvent>(OnShuttlePurchase);
    }

    private void OnShuttlePurchase(Entity<VesselComponent> ent, ref ShipyardShuttlePurchaseEvent args)
    {
        EnsureComp<ShipActivityComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<VesselComponent>();

        if (_lastUpdate + _interval > _gameTiming.CurTime)
            return;

        _lastUpdate = _gameTiming.CurTime;

        while (query.MoveNext(out var uid, out _))
        {
            var inactivity = EnsureComp<ShipActivityComponent>(uid);

            if (inactivity.LastChecked + inactivity.CheckInterval > _gameTiming.CurTime)
                continue;

            inactivity.LastChecked = _gameTiming.CurTime;

            var isActive = IsActive(uid);

            if (isActive && inactivity.TimesInactive > 0)
                inactivity.TimesInactive = 0;

            if (!isActive)
                inactivity.TimesInactive++;

            inactivity.InactiveLastCheck = !isActive;

            if (!isActive && inactivity.GetMinutesInactive() >= inactivity.InactiveThresholdMinutes)
                inactivity.InactivePastThreshold = true;

            Dirty(uid, inactivity);
        }
    }

    private void OnAttemptShuttlePurchase(Entity<VesselComponent> ent, ref AttemptShipyardShuttlePurchaseEvent ev)
    {
        var query = EntityQueryEnumerator<VesselComponent>();
        var shuttleCount = 0;

        if (!_prototypeManager.TryIndex(ent.Comp.VesselId, out var vessel))
            return;

        while (query.MoveNext(out var uid, out var targetVessel))
        {
            if (targetVessel.VesselId != ent.Comp.VesselId)
                continue;

            // InactiveShipComponent isn't like a tag, it's more like ApcPowerReceiver. You need to check if it's inactive.
            if (!TryComp<ShipActivityComponent>(uid, out var inactivity) || inactivity.InactivePastThreshold)
                continue;

            shuttleCount++;
        }

        if (shuttleCount >= vessel.LimitActive)
        {
            ev.CancelReason = "shipyard-console-limited";
            ev.Cancel();
        }
    }

    private bool IsActive(Entity<VesselComponent?> vessel)
    {
        var powerEntities = new HashSet<Entity<ApcPowerReceiverComponent>>();
        _lookup.GetGridEntities(vessel.Owner, powerEntities);

        var totalPowerEntities = 0;
        var powered = 0;

        // If the deed has no owner, it's inactive.
        if (!_shuttleDeed.HasOwner(vessel.Owner))
            return false;

        foreach (var ent in powerEntities.Where(ent => ent.Comp.NeedsPower))
        {
            if (ent.Comp.Powered) // should be powered even if not switched on.
                powered++;

            totalPowerEntities++;
        }

        var percentage = totalPowerEntities != 0 && powered != 0 ? powered / totalPowerEntities : 0;

        if (percentage >= PercentageUnpoweredForInactive)
            return true;

        return false;
    }
}
