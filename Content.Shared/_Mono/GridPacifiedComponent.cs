using Robust.Shared.GameStates;

namespace Content.Shared._Mono;

/// <summary>
/// Component that applies Pacified status to all organic entities on a grid.
/// Entities with company affiliations matching the exempt companies will not be pacified.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class GridPacifiedComponent : Component
{
    /// <summary>
    /// The list of entities that have been pacified by this component.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> PacifiedEntities = new();

    /// <summary>
    /// Entities that are pending pacification with their entry timestamps.
    /// After 1 second, they will be moved to PacifiedEntities.
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, TimeSpan> PendingEntities = new();

    /// <summary>
    /// A check for if an entity is pre-pacified
    /// </summary>
    [DataField]
    public bool PrePacified = false;

    /// <summary>
    /// Until what time an entity will be pacified for
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan PacifiedTime;

    /// <summary>
    /// The time when the next periodic update should occur
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// How frequently to check all entities on the grid for changes (in seconds)
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The radius from a GridPacifier that a GridPacified entity is pacified.
    /// </summary>
    [DataField]
    public float PacifyRadius = 512f;
}
