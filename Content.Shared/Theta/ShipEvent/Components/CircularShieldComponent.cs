using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CircularShieldComponent : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? BoundConsole;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Powered;

    [DataField("consumptionPerM2"), ViewVariables(VVAccess.ReadWrite)]
    public float ConsumptionPerSquareMeter;

    //specified in degrees, for prototypes
    [DataField("maxWidth"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxWidth = 360;

    [DataField("maxRadius"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxRadius;

    [AutoNetworkedField, DataField("color"), ViewVariables(VVAccess.ReadWrite)]
    public Color Color;

    //(datafields are for map serialization, so it's possible for mappers to create shield presets)
    [AutoNetworkedField, DataField("angle"), ViewVariables(VVAccess.ReadWrite)]
    public Angle Angle;

    [AutoNetworkedField, DataField("width"), ViewVariables(VVAccess.ReadWrite)]
    public Angle Width;

    [AutoNetworkedField, DataField("radius"), ViewVariables(VVAccess.ReadWrite)]
    public int Radius;

    // Power surge mechanics when taking damage
    [DataField("projectileWattPerImpact"), ViewVariables(VVAccess.ReadWrite)]
    public float ProjectileWattPerImpact = 105f; // Watts per point of projectile damage

    [DataField("damageSurgeDuration"), ViewVariables(VVAccess.ReadWrite)]
    public float DamageSurgeDuration = 25f; // Duration in seconds before surge dissipates

    [DataField("currentSurgePower"), ViewVariables(VVAccess.ReadWrite)]
    public float CurrentSurgePower; // Additional watts of power usage from damage

    [DataField("surgeTimeRemaining"), ViewVariables(VVAccess.ReadWrite)]
    public float SurgeTimeRemaining; // Time remaining for the current power surge

    [DataField("effects", serverOnly: true)]
    public List<CircularShieldEffect> Effects = new();

    public bool CanWork => Enabled && Powered;

    // Calculate power draw including any damage surge
    public int DesiredDraw
    {
        get
        {
            if (!Enabled)
                return 0;

            // Base power draw from shield size
            double baseDraw = Radius * Radius * Width.Degrees * 0.5f * ConsumptionPerSquareMeter;

            // Add surge power
            return (int)(baseDraw + CurrentSurgePower);
        }
    }
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class CircularShieldEffect
{
    public virtual void OnShieldInit(EntityUid uid, CircularShieldComponent shield) { }
    public virtual void OnShieldShutdown(EntityUid uid, CircularShieldComponent shield) { }
    public virtual void OnShieldUpdate(EntityUid uid, CircularShieldComponent shield, float time) { }
    public virtual void OnShieldEnter(EntityUid uid, CircularShieldComponent shield) { }
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CircularShieldConsoleComponent : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? BoundShield;
}

[Serializable, NetSerializable]
public sealed class CircularShieldToggleMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class CircularShieldChangeParametersMessage : BoundUserInterfaceMessage
{
    public Angle? Angle;

    public Angle? Width;

    public int? Radius;

    public CircularShieldChangeParametersMessage(Angle? angle, Angle? width, int? radius)
    {
        Angle = angle;
        Width = width;
        Radius = radius;
    }
}
