using Content.Shared._Mono.CorticalBorer;
using Content.Shared.Examine;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Mono.CorticalBorer;

public sealed class CorticalBorerInfestedSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CorticalBorerInfestedComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<CorticalBorerInfestedComponent, ExaminedEvent>(OnExaminedInfested);
    }

    private void OnInit(Entity<CorticalBorerInfestedComponent> infested, ref MapInitEvent args)
    {
        infested.Comp.ControlContainer = _container.EnsureContainer<Container>(infested, "ControlContainer");
    }

    private void OnExaminedInfested(Entity<CorticalBorerInfestedComponent> infected, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange
            || args.Examined != args.Examiner)
            return;

        if (infected.Comp.ControlTimeEnd is not { } cte)
            return;

        var timeRemaining = Math.Floor((cte - _timing.CurTime).TotalSeconds);
        args.PushMarkup(Loc.GetString("infested-control-examined", ("timeremaining", timeRemaining)));
    }

}
