// SPDX-FileCopyrightText: 2025 ark1368
//
// SPDX-License-Identifier: MPL-2.0

using Robust.Shared.GameStates;

namespace Content.Shared._Mono.Traits.Physical;

/// <summary>
/// Adds a self-examine verb for the Self-Aware trait.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SelfAwareComponent : Component;
