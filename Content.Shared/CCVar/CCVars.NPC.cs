using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<int> NPCMaxUpdates =
        CVarDef.Create("npc.max_updates", 256); // Frontier: 128<256 - praying to clang

    public static readonly CVarDef<bool> NPCEnabled = CVarDef.Create("npc.enabled", true);

    /// <summary>
    ///     Should NPCs pathfind when steering. For debug purposes.
    /// </summary>
    public static readonly CVarDef<bool> NPCPathfinding = CVarDef.Create("npc.pathfinding", true);

    /// <summary>
    ///     Should NPCs check player distances when moving? Mostly because fuck debugging this reliably.
    /// </summary>
    public static readonly CVarDef<bool> NPCMovementCheckPlayerDistances = CVarDef.Create("npc.movement_check_player_distances", false);
}
