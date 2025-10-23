using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;

namespace Content.Shared._Mono.ArmorPlate;

/// <summary>
/// Handles movement speed modifiers for armor plates.
/// </summary>
public sealed class ArmorPlateSpeedModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArmorPlateComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMoveSpeed);
    }

    private void OnRefreshMoveSpeed(EntityUid uid, ArmorPlateComponent component, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        args.Args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }
}
