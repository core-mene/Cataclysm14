using Robust.Shared.GameStates;

namespace Content.Shared._Mono.Ships;

/// <summary>
/// A component that enhances a shuttle's FTL capabilities when powered.
/// FTL travel is still possible without this component, but the range is limited.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class FTLDriveComponent : Component
{
    /// <summary>
    /// Whether the FTL drive is currently powered and operational.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("powered")]
    [AutoNetworkedField]
    public bool Powered = true;

    /// <summary>
    /// The maximum FTL range this drive can achieve when powered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("range")]
    [AutoNetworkedField]
    public float Range = 512f;
}
