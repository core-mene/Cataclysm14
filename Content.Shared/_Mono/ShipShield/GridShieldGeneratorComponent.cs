using Content.Shared.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Mono.ShipShield;

[RegisterComponent, NetworkedComponent]
public sealed partial class GridShieldGeneratorComponent : Component
{
    /// <summary>
    /// Is the generator toggled on?
    /// </summary>
    [DataField]
    public bool Enabled;

    /// <summary>
    /// Is this generator connected to fields?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsConnected;

    /// <summary>
    /// Whether the shield fields are currently active and protecting the grid
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool FieldsActive;

    /// <summary>
    /// The masks the raycast should not go through
    /// </summary>
    [DataField("collisionMask")]
    public int CollisionMask = (int) (CollisionGroup.MobMask | CollisionGroup.Impassable | CollisionGroup.MachineMask | CollisionGroup.Opaque);

    /// <summary>
    /// A collection of shield field entities created by this generator
    /// </summary>
    [ViewVariables]
    public List<EntityUid> ShieldFields = new();

    /// <summary>
    /// What fields should this spawn?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("createdField", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CreatedField = "GridShieldField";
}
