using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Component that handles locking shuttle consoles until an ID card with the matching
/// shuttle deed is used to unlock it.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedShuttleConsoleLockSystem))]
public sealed partial class ShuttleConsoleLockComponent : Component
{
    /// <summary>
    /// Whether the console is currently locked
    /// </summary>
    [DataField("locked")]
    public bool Locked = true;

    /// <summary>
    /// The ID of the shuttle this console is locked to
    /// </summary>
    [DataField("shuttleId")]
    public string? ShuttleId;
}

[Serializable, NetSerializable]
public enum ShuttleConsoleLockVisuals : byte
{
    Locked,
}
