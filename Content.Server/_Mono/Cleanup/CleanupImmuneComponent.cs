// SPDX-FileCopyrightText: 2025 Ilya246
//
// SPDX-License-Identifier: MPL-2.0

namespace Content.Server._Mono.Cleanup;

/// <summary>
/// Prevents this entity from being cleaned up by automatic cleanup systems.
/// </summary>
[RegisterComponent]
public sealed partial class CleanupImmuneComponent : Component {}
