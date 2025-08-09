using Robust.Shared.GameStates;

namespace Content.Shared._Mono.Ships;

/// <summary>
/// A component that enhances a shuttle's FTL range.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class FTLDriveComponent : Component
{
    /// <summary>
    /// The maximum FTL range this drive can achieve.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float Range = 512f;

    /// <summary>
    /// The FTL drive's cooldown between jumps before Mass Multiplier.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float Cooldown = 10f;


    /// <summary>
    /// The FTL jump duration before Mass Multiplier.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float HyperSpaceTime = 20f;

    /// <summary>
    /// The FTL duration until the jump starts before Mass Multiplier.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float StartupTime = 5.5f;

    /// <summary>
    /// Is the drive's FTL StartupTime, Travel Time, and Cooldown affected by the mass of the ship?
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool MassAffectedDrive = true;

    /// <summary>
    /// A multiplier of the effective mass a ship will have from mass calculations.
    /// Set MassAffectedDrive to false instead of setting this to Zero.
    /// i.e. 2f = 2 times the mass for calculations.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float DriveMassMultiplier = 1f;
}
