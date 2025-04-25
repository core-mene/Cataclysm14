using Content.Server.GameTicking;
using Content.Shared._NF.Shipyard;
using Content.Shared._NF.Shipyard.Components;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._NF.Shipyard.Systems;

/// <summary>
/// Manages ship ownership and handles cleanup of ships when owners are offline too long
/// </summary>
public sealed class ShipOwnershipSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private readonly HashSet<EntityUid> _pendingDeletionShips = new();

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to player events to track when they join/leave
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        // Initialize tracking for ships
        SubscribeLocalEvent<ShipOwnershipComponent, ComponentStartup>(OnShipOwnershipStartup);
        SubscribeLocalEvent<ShipOwnershipComponent, ComponentShutdown>(OnShipOwnershipShutdown);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    /// <summary>
    /// Register a ship as being owned by a player
    /// </summary>
    public void RegisterShipOwnership(EntityUid gridUid, ICommonSession owningPlayer)
    {
        // Don't register ownership if the entity isn't valid
        if (!EntityManager.EntityExists(gridUid))
            return;

        // Add ownership component to the ship
        var comp = EnsureComp<ShipOwnershipComponent>(gridUid);
        comp.OwnerUserId = owningPlayer.UserId;
        comp.IsOwnerOnline = true;
        comp.LastStatusChangeTime = _gameTiming.CurTime;

        Dirty(gridUid, comp);

        // Log ship registration
        Logger.InfoS("shipOwnership", $"Registered ship {ToPrettyString(gridUid)} to player {owningPlayer.Name} ({owningPlayer.UserId})");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Check for ships that need to be deleted due to owner absence
        var query = EntityQueryEnumerator<ShipOwnershipComponent>();
        while (query.MoveNext(out var uid, out var ownership))
        {
            // Skip ships with online owners
            if (ownership.IsOwnerOnline)
                continue;

            // Calculate how long the owner has been offline
            var offlineTime = _gameTiming.CurTime - ownership.LastStatusChangeTime;
            var timeoutSeconds = TimeSpan.FromSeconds(ownership.DeletionTimeoutSeconds);

            // Check if we've passed the timeout
            if (offlineTime >= timeoutSeconds)
            {
                // Queue ship for deletion
                _pendingDeletionShips.Add(uid);
            }
        }

        // Process deletions outside of enumeration
        foreach (var shipUid in _pendingDeletionShips)
        {
            if (!EntityManager.EntityExists(shipUid))
                continue;

            // Only handle deletion if this entity has a transform and is a grid
            if (TryComp<TransformComponent>(shipUid, out var transform) && transform.GridUid == shipUid)
            {
                Logger.InfoS("shipOwnership", $"Deleting abandoned ship {ToPrettyString(shipUid)}");

                // Delete the grid entity
                QueueDel(shipUid);
            }
        }

        _pendingDeletionShips.Clear();
    }

    private void OnShipOwnershipStartup(EntityUid uid, ShipOwnershipComponent component, ComponentStartup args)
    {
        // If player is already online, mark them as such
        if (_playerManager.TryGetSessionById(component.OwnerUserId, out var player))
        {
            component.IsOwnerOnline = true;
            component.LastStatusChangeTime = _gameTiming.CurTime;
            Dirty(uid, component);
        }
    }

    private void OnShipOwnershipShutdown(EntityUid uid, ShipOwnershipComponent component, ComponentShutdown args)
    {
        // Nothing to do here for now
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.Session == null)
            return;

        var userId = e.Session.UserId;
        var query = EntityQueryEnumerator<ShipOwnershipComponent>();

        // Update all ships owned by this player
        while (query.MoveNext(out var shipUid, out var ownership))
        {
            if (ownership.OwnerUserId != userId)
                continue;

            switch (e.NewStatus)
            {
                case SessionStatus.Connected:
                case SessionStatus.InGame:
                    // Player has connected, update ownership
                    ownership.IsOwnerOnline = true;
                    ownership.LastStatusChangeTime = _gameTiming.CurTime;
                    Logger.DebugS("shipOwnership", $"Owner of ship {ToPrettyString(shipUid)} has connected");
                    break;

                case SessionStatus.Disconnected:
                    // Player has disconnected, update ownership
                    ownership.IsOwnerOnline = false;
                    ownership.LastStatusChangeTime = _gameTiming.CurTime;
                    Logger.DebugS("shipOwnership", $"Owner of ship {ToPrettyString(shipUid)} has disconnected");
                    break;
            }

            Dirty(shipUid, ownership);
        }
    }
}
