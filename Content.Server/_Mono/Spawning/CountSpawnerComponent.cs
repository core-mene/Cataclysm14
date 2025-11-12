using Robust.Shared.Prototypes;

namespace Content.Server._Mono.Spawning;

/// <summary>
/// Simple spawner that is made to use SpawnCount system
/// Lets to spawn entities with randomized stack counts.
/// </summary>
[RegisterComponent]
public sealed partial class CountSpawnerComponent : Component
{
    [DataField("prototype")]
    public EntProtoId Prototype { get; set; } = "";

    [DataField("minimumCount")]
    public int MinimumCount { get; set; } = 1;

    [DataField("maximumCount")]
    public int MaximumCount { get; set; } = 1;

    [DataField]
    public bool DespawnAfterSpawn = true;
}
