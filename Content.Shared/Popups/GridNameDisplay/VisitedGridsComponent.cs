using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Popups.GridNameDisplay;

/// <summary>
/// Component that tracks which station/grid entities a player has visited.
/// Used to prevent showing station name popups every time they enter a grid they've already visited.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(VisitedGridsSystem))]
public sealed partial class VisitedGridsComponent : Component
{
    /// <summary>
    /// Set of grid EntityUids that the player has visited.
    /// This is used for local tracking on both client and server.
    /// </summary>
    [DataField("visitedGrids")]
    public HashSet<EntityUid> VisitedGridUids = new();

    /// <summary>
    /// Set of grid NetEntities that the player has visited.
    /// This is used for safe network serialization of entity references.
    /// </summary>
    [DataField("visitedGridNetUids")]
    public HashSet<NetEntity> VisitedGridNetUids = new();
}

/// <summary>
/// State class for <see cref="VisitedGridsComponent"/> network serialization.
/// </summary>
[Serializable, NetSerializable]
public sealed class VisitedGridsComponentState : ComponentState
{
    public readonly HashSet<NetEntity> VisitedGridNetUids;

    public VisitedGridsComponentState(HashSet<NetEntity> visitedGridNetUids)
    {
        VisitedGridNetUids = visitedGridNetUids;
    }
}
