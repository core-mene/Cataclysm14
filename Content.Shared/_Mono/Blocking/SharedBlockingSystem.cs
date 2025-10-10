// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: MPL-2.0

using Content.Client._Mono.Blocking.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Mono.Blocking;

public abstract class SharedBlockingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    public virtual void SetEnabled(EntityUid uid, bool value, BlockingVisualsComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Enabled == value)
            return;

        component.Enabled = value;
        Dirty(uid, component);
    }
}
