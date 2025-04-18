using Content.Shared.Clothing.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing.EntitySystems;

/// <summary>
/// This system allows replacing a worn clothing item with another of the same type by clicking on it.
/// </summary>
public sealed class ClothingSwapSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        // Subscribe to the interaction event for when a player clicks on another item with an item
        SubscribeLocalEvent<ClothingComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, ClothingComponent targetClothing, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Check if the item being used is also clothing
        if (!TryComp<ClothingComponent>(args.Used, out var heldClothing))
            return;

        // Skip if the clothing doesn't fit any of the same slots
        if ((heldClothing.Slots & targetClothing.Slots) == 0)
            return;

        // Get the owner of the target clothing (the person wearing it)
        if (!_containerSystem.TryGetContainingContainer(uid, out var container) || 
            container.Owner != args.User)
            return;

        // Get the slot the clothing is in
        var slot = targetClothing.InSlot;
        if (string.IsNullOrEmpty(slot))
            return;
            
        SwapClothing(args.User, args.Used, heldClothing, uid, targetClothing, slot);
        args.Handled = true;
    }
    
    /// <summary>
    /// Public method that can be called by SharedStrippableSystem to attempt clothing swap
    /// </summary>
    public bool TrySwapClothing(EntityUid user, EntityUid target, string slot)
    {
        // Check if the user has hands
        if (!TryComp<HandsComponent>(user, out var handsComp) || 
            handsComp.ActiveHandEntity == null)
            return false;
            
        var heldItem = handsComp.ActiveHandEntity.Value;
            
        // Skip if the item isn't clothing
        if (!TryComp<ClothingComponent>(heldItem, out var heldClothing))
            return false;
            
        // Check if there's an entity in the target's slot
        if (!_inventorySystem.TryGetSlotEntity(target, slot, out var slotEntity))
            return false;
            
        // Check if the entity in the slot is clothing
        if (!TryComp<ClothingComponent>(slotEntity, out var targetClothing))
            return false;
            
        // Make sure the slot types match
        if ((targetClothing.Slots & heldClothing.Slots) == 0)
            return false;
            
        // Perform the swap if compatible
        SwapClothing(user, heldItem, heldClothing, slotEntity.Value, targetClothing, slot);
        return true;
    }
    
    /// <summary>
    /// Handles the actual clothing swap logic.
    /// </summary>
    private void SwapClothing(
        EntityUid user, 
        EntityUid heldItem, 
        ClothingComponent heldClothing, 
        EntityUid targetItem, 
        ClothingComponent targetClothing, 
        string slot)
    {
        // Determine the entity whose clothes we're actually swapping
        // For the strip menu, this is the target entity, not the user
        // For self-use (clicking on your own clothing), this is the user
        var clothingOwner = targetClothing.InSlot != null ? 
            _containerSystem.TryGetContainingContainer(targetItem, out var container) ? container.Owner : user : 
            user;
            
        // Find all slots that depend on this clothing item (like pockets that depend on jumpsuit)
        var dependentSlotContents = new Dictionary<string, EntityUid>();
        if (_inventorySystem.TryGetSlots(clothingOwner, out var slotDefinitions))
        {
            foreach (var slotDef in slotDefinitions)
            {
                // Check if this slot depends on the one we're replacing
                if (slotDef.DependsOn == slot)
                {
                    // Try to get the item in this dependent slot (if any)
                    if (_inventorySystem.TryGetSlotEntity(clothingOwner, slotDef.Name, out var dependentEntity))
                    {
                        // Store it for later reinsertion
                        dependentSlotContents[slotDef.Name] = dependentEntity.Value;
                        
                        // Unequip the dependent item
                        _inventorySystem.TryUnequip(clothingOwner, slotDef.Name, predicted: true);
                    }
                }
            }
        }

        // Try to unequip the target clothing
        if (!_inventorySystem.TryUnequip(clothingOwner, slot, out var item, predicted: true))
            return;

        // Try to equip the held clothing
        if (!_inventorySystem.TryEquip(user, clothingOwner, heldItem, slot, silent: false, predicted: true))
        {
            // If equipping fails, put the original item back
            _inventorySystem.TryEquip(user, clothingOwner, item.Value, slot, silent: true, predicted: true);
            
            // Re-equip any dependent items we unequipped
            foreach (var (dependentSlot, dependentItem) in dependentSlotContents)
            {
                _inventorySystem.TryEquip(user, clothingOwner, dependentItem, dependentSlot, silent: true, predicted: true);
            }
            
            return;
        }

        // Re-equip all dependent items to the new clothing item
        foreach (var (dependentSlot, dependentItem) in dependentSlotContents)
        {
            _inventorySystem.TryEquip(user, clothingOwner, dependentItem, dependentSlot, silent: true, predicted: true);
        }

        // Pickup the unequipped item
        _handsSystem.PickupOrDrop(user, item.Value);
    }
} 