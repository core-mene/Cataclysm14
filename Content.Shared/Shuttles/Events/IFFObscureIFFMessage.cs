// SPDX-FileCopyrightText: 2025 Onezero0
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

[Serializable, NetSerializable] // Mono - this public classs is for _Mono obscure iff
public sealed class IFFObscureIFFMessage : BoundUserInterfaceMessage
{
    public bool Show;
}
