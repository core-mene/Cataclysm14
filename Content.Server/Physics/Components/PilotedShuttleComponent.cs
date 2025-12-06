using System.Numerics;

// Mono - whole file

namespace Content.Server.Physics.Controllers;

/// <summary>
///     Component used to store input data for shuttles that are currently being piloted.
/// </summary>
[RegisterComponent]
public sealed partial class PilotedShuttleComponent : Component
{
    /// <summary>
    ///     List of inputs currently given to this shuttle by any pilots.
    /// </summary>
    [DataField]
    public List<ShuttleInput> InputList = new();
}
