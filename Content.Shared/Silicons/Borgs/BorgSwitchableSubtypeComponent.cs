// SPDX-FileCopyrightText: 2025 BeBright
// SPDX-FileCopyrightText: 2025 GoobBot
// SPDX-FileCopyrightText: 2025 Ilya246
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Borgs;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class BorgSwitchableSubtypeComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<BorgSubtypePrototype>? BorgSubtype;
}
