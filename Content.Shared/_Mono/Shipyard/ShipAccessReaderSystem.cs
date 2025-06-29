// SPDX-FileCopyrightText: 2025 ark1368
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Doors;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Lock;
using Robust.Shared.Map;

namespace Content.Shared._Mono.Shipyard;

/// <summary>
/// System that handles ship deed-based access control.
/// </summary>
public sealed class ShipAccessReaderSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShipAccessReaderComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<ShipAccessReaderComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<ShipAccessReaderComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
    }

    private void OnStorageOpenAttempt(EntityUid uid, ShipAccessReaderComponent component, ref StorageOpenAttemptEvent args)
    {
        if (!component.Enabled)
            return;

        // If the locker is unlocked, allow anyone to open it
        if (TryComp<LockComponent>(uid, out var lockComp) && !lockComp.Locked)
            return;

        // If the locker is locked, require ship deed access
        if (!HasShipAccess(args.User, uid, component, args.Silent))
        {
            args.Cancelled = true;
        }
    }

    private void OnBeforeDoorOpened(EntityUid uid, ShipAccessReaderComponent component, ref BeforeDoorOpenedEvent args)
    {
        if (!component.Enabled)
            return;

        if (args.User == null)
            return;

        if (!HasShipAccess(args.User.Value, uid, component, false))
        {
            args.Cancel();
        }
    }

    private void OnLockToggleAttempt(EntityUid uid, ShipAccessReaderComponent component, ref LockToggleAttemptEvent args)
    {
        if (!component.Enabled)
            return;

        if (!HasShipAccess(args.User, uid, component, args.Silent))
        {
            args.Cancelled = true;
        }
    }

    /// <summary>
    /// Checks if a user has access to a ship entity by verifying they have the correct ship deed.
    /// </summary>
    /// <param name="user">The user trying to access the entity</param>
    /// <param name="target">The entity being accessed</param>
    /// <param name="component">The ship access reader component</param>
    /// <param name="silent">Whether to suppress popup messages</param>
    /// <returns>True if access is granted, false otherwise</returns>
    public bool HasShipAccess(EntityUid user, EntityUid target, ShipAccessReaderComponent component, bool silent = false)
    {
        // Get the grid the target entity is on
        var targetTransform = Transform(target);
        if (targetTransform.GridUid == null)
        {
            // Log.Debug("ShipAccess: Target {0} not on a grid, allowing access", target);
            return true; // Not on a grid, allow access
        }

        var gridUid = targetTransform.GridUid.Value;

        // Check if the grid has a ship deed (is a purchased ship)
        if (!TryComp<ShuttleDeedComponent>(gridUid, out var shipDeed))
        {
            // Log.Debug("ShipAccess: Grid {0} has no ShuttleDeedComponent, allowing normal access", gridUid);
            return true; // Not a ship with a deed, allow normal access
        }

        // Find all accessible ID cards for the user
        var accessibleCards = FindAccessibleIdCards(user);
        // Log.Debug("ShipAccess: User {0} has {1} accessible ID cards: {2}", user, accessibleCards.Count, string.Join(", ", accessibleCards));

        // Check if any of the user's ID cards have a deed for this specific ship
        foreach (var cardUid in accessibleCards)
        {
            if (TryComp<ShuttleDeedComponent>(cardUid, out var cardDeed))
            {
                // Log.Debug("ShipAccess: ID card {0} has deed for shuttle {1}, target ship is {2}", cardUid, cardDeed.ShuttleUid, shipDeed.ShuttleUid);
                // Check if this deed is for the same ship
                if (cardDeed.ShuttleUid == shipDeed.ShuttleUid)
                {
                    // Log.Debug("ShipAccess: User {0} has correct deed access via card {1}", user, cardUid);
                    return true; // User has the correct deed
                }
            }
        }

        // Check if any of the user's ID cards have guest access to this ship
        if (TryComp<ShipGuestAccessComponent>(gridUid, out var guestAccess))
        {
            // Log.Debug("ShipAccess: Grid {0} has guest access component with {1} guest cards: {2}",
            //     gridUid, guestAccess.GuestIdCards.Count, string.Join(", ", guestAccess.GuestIdCards));

            foreach (var cardUid in accessibleCards)
            {
                if (guestAccess.GuestIdCards.Contains(cardUid))
                {
                    // Log.Debug("ShipAccess: User {0} has guest access via card {1}", user, cardUid);
                    return true; // User's ID card has guest access
                }
            }
        }
        // else
        // {
        //     Log.Debug("ShipAccess: Grid {0} has no ShipGuestAccessComponent", gridUid);
        // }

        // Log.Debug("ShipAccess: User {0} denied access to target {1} on grid {2}", user, target, gridUid);

        // Access denied - show popup if not silent
        if (!silent && component.ShowDeniedPopup)
        {
            _popup.PopupClient(Loc.GetString(component.DeniedMessage), target, user);
        }

        return false;
    }

    /// <summary>
    /// Finds all ID cards that the user can access (in hands, inventory, or inside PDAs).
    /// </summary>
    /// <param name="user">The user to check</param>
    /// <returns>Collection of accessible ID card entities</returns>
    private HashSet<EntityUid> FindAccessibleIdCards(EntityUid user)
    {
        var cards = new HashSet<EntityUid>();

        // Check items in hands for direct ID cards or PDAs with ID cards
        foreach (var item in _handsSystem.EnumerateHeld(user))
        {
            // Check if the item itself is an ID card (with or without deed)
            if (HasComp<IdCardComponent>(item))
                cards.Add(item);

            // Check if it's a PDA with an ID card
            if (_idCardSystem.TryGetIdCard(item, out var idCard))
                cards.Add(idCard.Owner);
        }

        // Check ID slot in inventory (could be direct ID or PDA)
        if (_inventorySystem.TryGetSlotEntity(user, "id", out var idUid))
        {
            // Check if the item itself is an ID card (with or without deed)
            if (HasComp<IdCardComponent>(idUid.Value))
                cards.Add(idUid.Value);

            // Check if it's a PDA with an ID card
            if (_idCardSystem.TryGetIdCard(idUid.Value, out var idCard))
                cards.Add(idCard.Owner);
        }

        return cards;
    }
}
