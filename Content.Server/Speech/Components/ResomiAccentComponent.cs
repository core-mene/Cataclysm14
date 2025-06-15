using Content.Server.Speech.EntitySystems;

namespace Content.Server.Speech.Components;

/// <summary>
///     Silly BAWK!
/// </summary>
[RegisterComponent]
[Access(typeof(ResomiAccentSystem))]
public sealed partial class ResomiAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("bawkChance")]
    public float BawkChance = 0.01f;




}
