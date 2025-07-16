// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CollectiveMind
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class CollectiveMindComponent : Component
    {
        [DataField("channel", required: true)]
        public ProtoId<RadioChannelPrototype> Channel;
    }
}
