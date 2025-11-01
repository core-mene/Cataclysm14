// SPDX-FileCopyrightText: 2022 Emisse
// SPDX-FileCopyrightText: 2022 Leon Friedrich
// SPDX-FileCopyrightText: 2022 keronshb
// SPDX-FileCopyrightText: 2024 Krunklehorn
// SPDX-FileCopyrightText: 2025 ark1368
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.Inventory;
using Content.Shared.Strip;
using Content.Shared.Strip.Components;

namespace Content.Shared.Strip;

public sealed class ThievingSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThievingComponent, BeforeStripEvent>(OnBeforeStrip);
        SubscribeLocalEvent<ThievingComponent, InventoryRelayedEvent<BeforeStripEvent>>((e, c, ev) => OnBeforeStrip(e, c, ev.Args));
    }

    private void OnBeforeStrip(EntityUid uid, ThievingComponent component, BeforeStripEvent args)
    {
        args.Stealth |= component.Stealthy;
        args.Additive -= component.StripTimeReduction;
        args.Multiplier *= component.TimeMultiplier; // Mono
    }
}
