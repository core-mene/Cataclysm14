using Content.Shared._Mono.CorticalBorer;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server._Mono.CorticalBorer;

/// <summary>
/// This handles...
/// </summary>
public sealed class CorticalBorerInfestedSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CorticalBorerInfestedComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(Entity<CorticalBorerInfestedComponent> infested, ref MapInitEvent args)
    {
        infested.Comp.ControlContainer = _container.EnsureContainer<Container>(infested, "ControlContainer");
    }
}
