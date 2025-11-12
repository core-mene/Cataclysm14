// SPDX-FileCopyrightText: 2025 Ilya246
//
// SPDX-License-Identifier: MPL-2.0

namespace Content.Server._Mono.Cleanup;

/// <summary>
///     Stores at which time will we have to be still meeting cleanup conditions for this grid to get cleaned up.
/// </summary>
[RegisterComponent]
public sealed partial class GridCleanupStateComponent : Component
{
    [ViewVariables]
    public TimeSpan? CleanupAtTime = null;
}
