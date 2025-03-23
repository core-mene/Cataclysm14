using Robust.Shared.GameStates;

namespace Content.Shared._Mono.ShipShield;

/// <summary>
/// Component that marks a grid as being protected by shield fields
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GridShieldProtectionComponent : Component
{
    /// <summary>
    /// The UIDs of shield generators on this grid that are active
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> ActiveGenerators = new();
}
