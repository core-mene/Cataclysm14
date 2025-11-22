// SPDX-FileCopyrightText: 2025 Androidonator
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.CombatMode;
using Content.Shared.MouseRotator;

namespace Content.Shared._Goobstation.CombatMode;

public sealed class SmoothMouseRotationSystem : EntitySystem
{
    private EntityQuery<CombatModeComponent>
        _combatQuery; // i love optimizing the code that doesn't even lag for no reason

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MouseRotatorComponent, MapInitEvent>(OnRotatorInit);
        _combatQuery = GetEntityQuery<CombatModeComponent>();
    }

    private void OnRotatorInit(Entity<MouseRotatorComponent> ent, ref MapInitEvent args)
    {
        if (!_combatQuery.HasComp(ent.Owner))
            return;

        ent.Comp.AngleTolerance = Angle.FromDegrees(5); // arbitrary
        ent.Comp.Simple4DirMode = false;
    }
}
