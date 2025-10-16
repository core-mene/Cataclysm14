// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 Daniel Lenrd
//
// SPDX-License-Identifier: MPL-2.0

using Content.Shared.ActionBlocker;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared._Goobstation.Interaction;

public sealed class BackEquipSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        CommandBinds.Builder
            // Mono Edit
            .Bind(ContentKeyFunctions.EquipBackpack, InputCmdHandler.FromDelegate(HandleEquipBackpack, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.EquipBelt, InputCmdHandler.FromDelegate(HandleEquipBelt, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.EquipPocket1, InputCmdHandler.FromDelegate(HandleEquipPocket1, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.EquipPocket2, InputCmdHandler.FromDelegate(HandleEquipPocket2, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.EquipSuitStorage, InputCmdHandler.FromDelegate(HandleEquipSuitStorage, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.EquipWallet, InputCmdHandler.FromDelegate(HandleEquipWallet, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.EquipID, InputCmdHandler.FromDelegate(HandleEquipID, handle: false, outsidePrediction: false))
            // Mono End
            .Register<BackEquipSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<BackEquipSystem>();
    }
    // Mono Edit
    // private void HandleEquipToBack(ICommonSession? session)
    // {
    //     HandleEquipToSlot(session, "suitstorage");
    // }
    private void HandleEquipBackpack(ICommonSession? session)
    {
        HandleEquipToSlot(session, "back");
    }

    private void HandleEquipBelt(ICommonSession? session)
    {
        HandleEquipToSlot(session, "belt");
    }

    private void HandleEquipPocket1(ICommonSession? session)
    {
        HandleEquipToSlot(session, "pocket1");
    }

    private void HandleEquipPocket2(ICommonSession? session)
    {
        HandleEquipToSlot(session, "pocket2");
    }

    private void HandleEquipSuitStorage(ICommonSession? session)
    {
        HandleEquipToSlot(session, "suitstorage");
    }

    private void HandleEquipWallet(ICommonSession? session)
    {
        HandleEquipToSlot(session, "wallet");
    }

    private void HandleEquipID(ICommonSession? session)
    {
        HandleEquipToSlot(session, "id");
    }
    // Mono End
    private void HandleEquipToSlot(ICommonSession? session, string equipmentSlot)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } uid || !Exists(uid))
            return;

        if (!TryComp<HandsComponent>(uid, out var hands) || hands.ActiveHand == null)
            return;

        var handItem = hands.ActiveHand.HeldEntity;

        if (!_actionBlocker.CanInteract(uid, handItem))
            return;

        if (!TryComp<InventoryComponent>(uid, out var inventory) || !_inventory.HasSlot(uid, equipmentSlot, inventory))
        {
            _popup.PopupClient(Loc.GetString("smart-equip-missing-equipment-slot", ("slotName", equipmentSlot)),
                uid,
                uid);
            return;
        }

        if (handItem != null && !_hands.CanDropHeld(uid, hands.ActiveHand))
        {
            _popup.PopupClient(Loc.GetString("smart-equip-cant-drop"), uid, uid);
            return;
        }

        _inventory.TryGetSlotEntity(uid, equipmentSlot, out var slotEntity);
        var emptyEquipmentSlotString = Loc.GetString("smart-equip-empty-equipment-slot", ("slotName", equipmentSlot));
        if (slotEntity is not { } slotItem)
        {
            if (handItem == null)
            {
                _popup.PopupClient(emptyEquipmentSlotString, uid, uid);
                return;
            }

            if (!_inventory.CanEquip(uid, handItem.Value, equipmentSlot, out var reason))
            {
                _popup.PopupClient(Loc.GetString(reason), uid, uid);
                return;
            }

            _hands.TryDrop(uid, hands.ActiveHand, handsComp: hands);
            _inventory.TryEquip(uid, handItem.Value, equipmentSlot, predicted: true, checkDoafter: true);
            return;
        }
        if (handItem != null)
            return;

        if (!_inventory.CanUnequip(uid, equipmentSlot, out var inventoryReason))
        {
            _popup.PopupClient(Loc.GetString(inventoryReason), uid, uid);
            return;
        }
        _inventory.TryUnequip(uid, equipmentSlot, inventory: inventory, predicted: true, checkDoafter: true);
        _hands.TryPickup(uid, slotItem, handsComp: hands);
    }
}
