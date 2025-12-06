// SPDX-FileCopyrightText: 2025 sneb
//
// SPDX-License-Identifier: MPL-2.0

using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.Species.Systems.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Audio;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Species.Systems;

public sealed class HydrakinSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HydrakinComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HydrakinComponent, HydrakinCoolOffActionEvent>(OnCoolOff);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HydrakinComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var comp, out var temperature))
        {
            if (!comp.HeatBuildupEnabled)
                continue;

            if (TryComp<MobStateComponent>(uid, out var mobState) &&
                mobState.CurrentState != MobState.Alive)
                return;

            if (temperature.CurrentTemperature < comp.MinTemperature ||
                temperature.CurrentTemperature > comp.MaxTemperature)
                return;

            temperature.CurrentTemperature += comp.Buildup * frameTime;
        }
    }

    private void OnInit(EntityUid uid, HydrakinComponent component, ComponentInit args)
    {
        if (component.CoolOffAction != null)
            return;

        _actionsSystem.AddAction(uid, ref component.CoolOffAction, component.CoolOffActionId);
    }

    private void OnCoolOff(EntityUid uid, HydrakinComponent component, HydrakinCoolOffActionEvent args)
    {
        _popupSystem.PopupEntity(Loc.GetString("hydrakin-cool-off-emote", ("name", Identity.Entity(uid, EntityManager))), uid);
        // TODO: sound effect
        // TODO: do after
        // TODO: cool down

        args.Handled = true;
    }
}
