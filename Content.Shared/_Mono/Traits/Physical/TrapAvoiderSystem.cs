// SPDX-FileCopyrightText: 2025 ark1368
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;

namespace Content.Shared._Mono.Traits.Physical;

/// <summary>
/// Cancels step triggers for entities that have TrapAvoiderComponent.
/// </summary>
public sealed class TrapAvoiderSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<StepTriggerComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
    }

    private void OnStepTriggerAttempt(Entity<StepTriggerComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (HasComp<TrapAvoiderComponent>(args.Tripper))
            args.Cancelled = true;
    }
}
