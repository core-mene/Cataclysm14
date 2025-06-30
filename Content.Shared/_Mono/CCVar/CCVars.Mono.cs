// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 Redrover1760
// SPDX-FileCopyrightText: 2025 ark1368
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Mono.CCVar;

/// <summary>
/// Contains CVars used by Mono.
/// </summary>
[CVarDefs]
public sealed partial class MonoCVars
{
    /// <summary>
    ///     How often to clean up space garbage entities, in seconds.
    /// </summary>
    public static readonly CVarDef<float> SpaceGarbageCleanupInterval =
        CVarDef.Create("mono.space_garbage_cleanup_interval", 1800.0f, CVar.SERVERONLY);
		
	/// <summary>
    ///     Whether to play radio static/noise sounds when receiving radio messages on headsets.
    /// </summary>
    public static readonly CVarDef<bool> RadioNoiseEnabled =
        CVarDef.Create("mono.radio_noise_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

}
