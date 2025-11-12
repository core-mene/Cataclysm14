using Content.Server.Stack;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Mono.Spawning;

/// <summary>
/// This system handles spawning both stacked entities by consolidating them and non-stacked entities.
/// </summary>
public sealed class SpawnCountSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CountSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    public void OnMapInit(EntityUid uid, CountSpawnerComponent component, MapInitEvent args)
    {
        var count = _random.Next(component.MinimumCount, component.MaximumCount);

        SpawnCount(component.Prototype, Transform(uid).Coordinates, count);
        if (component.DespawnAfterSpawn)
            QueueDel(uid);
    }

    public void SpawnCount(EntProtoId prototype, EntityCoordinates coordinates, int count, int bound = 0)
    {
        var entProto = _proto.Index<EntityPrototype>(prototype);

        var spawnPerStack = 1;
        var stackCount = 1;

        if (entProto.TryGetComponent<StackComponent>(out var stack))
        {
            stackCount = stack.Count * count;
            var stackPrototype = _proto.Index<StackPrototype>(stack.StackTypeId);

            if (bound == 0)
                bound = stackPrototype.MaxCount ?? Int32.MaxValue;

            spawnPerStack = (stackCount + bound - 1) / bound;
        }

        for (var i = 0; i < count; i += spawnPerStack)
        {
            SpawnEntity(prototype, coordinates, stackCount);
            if (stackCount != 0)
                stackCount -= bound;
        }
    }


    private void SpawnEntity(string? prototype, EntityCoordinates coordinates, int stackCount)
    {
        var ent = Spawn(prototype, coordinates);

        if (TryComp<StackComponent>(ent, out var stack))
            _stack.SetCount(ent, stackCount, stack);
    }
}
