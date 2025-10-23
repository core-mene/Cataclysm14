using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Mono.ArmorPlate;

/// <summary>
/// Converts incoming damage to stamina damage.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ArmorPlateComponent : Component
{
    /// <summary>
    /// Armor plate whitelist. Hardcode!!!
    /// </summary>
    public static readonly Dictionary<string, FixedPoint2> AcceptedPlates = new()
    {
        { "ArmorPlateLight", FixedPoint2.New(25) },
        { "ArmorPlateMedium", FixedPoint2.New(50) },
        { "ArmorPlateTactical", FixedPoint2.New(75) },
        { "ArmorPlatePlasteel", FixedPoint2.New(100) }, // ADMEME ONLY
    };

    /// <summary>
    /// Speed modifiers for different armor plate types.
    /// </summary>
    public static readonly Dictionary<string, (float walk, float sprint)> PlateSpeedModifiers = new()
    {
        { "ArmorPlateLight", (0.94f, 0.94f) },
        { "ArmorPlateMedium", (0.92f, 0.92f) },
        { "ArmorPlateTactical", (0.90f, 0.90f) },
        { "ArmorPlatePlasteel", (1.0f, 1.0f) }, // ADMEME ONLY
    };

    /// <summary>
    /// Maximum amount of damage a single plate can absorb before being destroyed.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 DamageCapacity = FixedPoint2.New(100);

    /// <summary>
    /// Current accumulated damage absorbed by the active plate.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 CurrentDamage = FixedPoint2.Zero;

    /// <summary>
    /// Multiplier applied when converting damage to stamina damage.
    /// </summary>
    [DataField]
    public float StaminaDamageMultiplier = 1.0f;

    /// <summary>
    /// Whether to show a popup notification when the plate breaks.
    /// </summary>
    [DataField]
    public bool ShowBreakPopup = true;

    /// <summary>
    /// Cached reference to track if we have a valid plate.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool HasActivePlate;

    /// <summary>
    /// Walk speed modifier based on the current armor plate type.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float WalkSpeedModifier = 1.0f;

    /// <summary>
    /// Sprint speed modifier based on the current armor plate type.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float SprintSpeedModifier = 1.0f;

    /// <summary>
    /// The currently active plate entity in storage.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EntityUid? ActivePlate;
}
