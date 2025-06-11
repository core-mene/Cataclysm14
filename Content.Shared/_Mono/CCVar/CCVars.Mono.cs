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
        CVarDef.Create("mono.space_garbage_cleanup_interval", 300.0f, CVar.SERVERONLY);
}
