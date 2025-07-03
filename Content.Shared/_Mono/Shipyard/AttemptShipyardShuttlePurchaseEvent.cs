// SPDX-FileCopyrightText: 2025 sleepyyapril
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Mono.Shipyard;

public sealed class AttemptShipyardShuttlePurchaseEvent(EntityUid shuttle, EntityUid purchaser, LocId? cancelReason = null) : CancellableEntityEventArgs
{
    public EntityUid Shuttle { get;  } = shuttle;
    public EntityUid Purchaser { get; } = purchaser;
    public LocId CancelReason { get; set;  } = cancelReason ?? "shipyard-console-denied";
}
