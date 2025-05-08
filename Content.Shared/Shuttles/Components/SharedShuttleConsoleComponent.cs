using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Components
{
    /// <summary>
    /// Interact with to start piloting a shuttle.
    /// </summary>
    [NetworkedComponent]
    public abstract partial class SharedShuttleConsoleComponent : Component
    {
        public static string DiskSlotName = "disk_slot";

        /// <summary>
        /// Custom display names for network port buttons.
        /// Key is the port ID, value is the display name.
        /// </summary>
        public Dictionary<string, string> PortNames = new();
    }

    [Serializable, NetSerializable]
    public enum ShuttleConsoleUiKey : byte
    {
        Key,
    }
}
