using Content.Shared.Shuttles.Components;

namespace Content.Client.Shuttles;

[RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class ShuttleConsoleComponent : SharedShuttleConsoleComponent
{
    /// <summary>
    /// Custom display names for network port buttons.
    /// Key is the port ID, value is the display name.
    /// </summary>
    [DataField("portLabels"), AutoNetworkedField]
    public new Dictionary<string, string> PortNames = new();
}
