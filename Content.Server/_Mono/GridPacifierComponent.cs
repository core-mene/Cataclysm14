namespace Content.Server._Mono;

/// <summary>
/// Component that applies Pacified status to all organic entities on a grid.
/// </summary>
[RegisterComponent]
public sealed partial class GridPacifierComponent : Component
{
    /// <summary>
    /// The list of entities that have been pacified by this component.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> PacifiedEntities = new();
}
