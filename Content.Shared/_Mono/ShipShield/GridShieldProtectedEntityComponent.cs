using Robust.Shared.GameStates;

namespace Content.Shared._Mono.ShipShield;

/// <summary>
/// Component that marks an entity as being protected by grid shield fields
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GridShieldProtectedEntityComponent : Component;
