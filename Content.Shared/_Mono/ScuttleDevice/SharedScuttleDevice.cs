// SPDX-FileCopyrightText: 2025 Ilya246
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Mono.ScuttleDevice;

[Serializable, NetSerializable]
public sealed partial class ScuttleArmDoAfterEvent : SimpleDoAfterEvent {}

[Serializable, NetSerializable]
public sealed partial class ScuttleDisarmDoAfterEvent : SimpleDoAfterEvent {}
