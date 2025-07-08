using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Mono.Research.PointDiskPrinter.Components;

[RegisterComponent]
public sealed partial class PointDiskConsoleComponent : Component
{
    /// <summary>
    /// How much it costs to print a 1k point disk
    /// </summary>
    [DataField("pricePerSmallDisk"), ViewVariables(VVAccess.ReadWrite)]
    public int PricePer1KDisk = 1000;

    /// <summary>
    /// How much it costs to print a 5k point disk
    /// </summary>
    [DataField("pricePerMediumDisk"), ViewVariables(VVAccess.ReadWrite)]
    public int PricePer5KDisk = 5000;

    /// <summary>
    /// How much it costs to print a 10k point disk
    /// </summary>
    [DataField("pricePerMediumDisk"), ViewVariables(VVAccess.ReadWrite)]
    public int PricePer10KDisk = 10000;

    /// <summary>
    /// The prototype of what's being printed
    /// </summary>
    [DataField("diskPrototype1K", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string Disk1KPrototype = "PointDisk1K";

    [DataField("diskPrototype5K", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string Disk5KPrototype = "PointDisk5K";

    [DataField("diskPrototype10K", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string Disk10KPrototype = "PointDisk10K";

    [DataField, ViewVariables(VVAccess.ReadWrite)] // Frontier
    public bool DiskRare = false; // Frontier

    /// <summary>
    /// How long it takes to print <see cref="DiskPrototype"/>
    /// </summary>
    [DataField("printDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PrintDuration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The sound made when printing occurs
    /// </summary>
    [DataField("printSound")]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");
}
