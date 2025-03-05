using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Mono.Ships;
using Content.Shared.Power;

namespace Content.Server._Mono.Ships;

public sealed class FTLDriveSystem : EntitySystem
{
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FTLDriveComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FTLDriveComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnStartup(EntityUid uid, FTLDriveComponent component, ComponentStartup args)
    {
        // Set initial power state
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver))
        {
            component.Powered = powerReceiver.Powered;
        }
        else
        {
            // If no power receiver, assume it's powered
            component.Powered = true;
        }
    }

    private void OnPowerChanged(EntityUid uid, FTLDriveComponent component, ref PowerChangedEvent args)
    {
        component.Powered = args.Powered;
    }
}
