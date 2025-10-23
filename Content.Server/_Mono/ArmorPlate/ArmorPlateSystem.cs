using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared._Mono.ArmorPlate;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Mono.ArmorPlate;

/// <summary>
/// Handles armor plates. >3
/// </summary>
public sealed class ArmorPlateSystem : EntitySystem
{
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    /// <summary>
    /// Tracks individual armor plates and their durability across all armor.
    /// </summary>
    private readonly Dictionary<EntityUid, FixedPoint2> _plateDurability = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageableComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<ArmorPlateComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ArmorPlateComponent, EntInsertedIntoContainerMessage>(OnPlateInserted);
        SubscribeLocalEvent<ArmorPlateComponent, EntRemovedFromContainerMessage>(OnPlateRemoved);
        SubscribeLocalEvent<ArmorPlateComponent, EntityTerminatingEvent>(OnArmorPlateTerminating);
    }

    private void OnPlateInserted(Entity<ArmorPlateComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != StorageComponent.ContainerId)
            return;

        var armorComp = ent.Comp;
        var insertedEntity = args.Entity;

        if (!IsArmorPlate(insertedEntity, out var platePrototypeId))
            return;

        var keysToRemove = new List<EntityUid>();
        foreach (var kvp in _plateDurability)
        {
            if (!Exists(kvp.Key))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _plateDurability.Remove(key);
        }

        if (!_plateDurability.ContainsKey(insertedEntity))
        {
            _plateDurability[insertedEntity] = FixedPoint2.Zero;
        }

        armorComp.ActivePlate = insertedEntity;
        armorComp.HasActivePlate = true;

        if (platePrototypeId != null && ArmorPlateComponent.AcceptedPlates.TryGetValue(platePrototypeId, out var plateCapacity))
        {
            armorComp.DamageCapacity = plateCapacity;
        }

        UpdateSpeedModifiers(ent, armorComp, platePrototypeId);

        armorComp.CurrentDamage = _plateDurability[insertedEntity];

        Dirty(ent, armorComp);
    }

    private void OnPlateRemoved(Entity<ArmorPlateComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != StorageComponent.ContainerId)
            return;

        var armorComp = ent.Comp;
        var removedEntity = args.Entity;

        if (!IsArmorPlate(removedEntity, out _))
            return;

        if (armorComp.ActivePlate == removedEntity)
        {
            _plateDurability[removedEntity] = armorComp.CurrentDamage;
        }

        if (armorComp.ActivePlate == removedEntity)
        {
            armorComp.ActivePlate = null;
            armorComp.HasActivePlate = false;
            armorComp.CurrentDamage = FixedPoint2.Zero;

            if (TryComp<StorageComponent>(ent, out var storage))
            {
                foreach (var item in storage.Container.ContainedEntities)
                {
                    if (IsArmorPlate(item, out var newPlatePrototypeId))
                    {
                        armorComp.ActivePlate = item;
                        armorComp.HasActivePlate = true;

                        if (newPlatePrototypeId != null && ArmorPlateComponent.AcceptedPlates.TryGetValue(newPlatePrototypeId, out var plateCapacity))
                        {
                            armorComp.DamageCapacity = plateCapacity;
                        }

                        UpdateSpeedModifiers(ent, armorComp, newPlatePrototypeId);

                        armorComp.CurrentDamage = _plateDurability.GetValueOrDefault(item, FixedPoint2.Zero);
                        break;
                    }
                }
            }

            UpdateSpeedModifiers(ent, armorComp, null);
        }

        Dirty(ent, armorComp);
    }

    private void OnArmorPlateTerminating(Entity<ArmorPlateComponent> ent, ref EntityTerminatingEvent args)
    {
        var armorComp = ent.Comp;

        var keysToRemove = new List<EntityUid>();
        foreach (var kvp in _plateDurability)
        {
            if (!Exists(kvp.Key))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _plateDurability.Remove(key);
        }

        if (armorComp.ActivePlate != null && !Exists(armorComp.ActivePlate.Value))
        {
            armorComp.ActivePlate = null;
            armorComp.HasActivePlate = false;
            armorComp.CurrentDamage = FixedPoint2.Zero;
            UpdateSpeedModifiers(ent, armorComp, null);
        }

        Dirty(ent, armorComp);
    }

    private bool IsArmorPlate(EntityUid entity, out string? prototypeId)
    {
        prototypeId = null;
        var metadata = MetaData(entity);
        prototypeId = metadata.EntityPrototype?.ID;
        return prototypeId != null && ArmorPlateComponent.AcceptedPlates.ContainsKey(prototypeId);
    }

    private void OnBeforeDamageChanged(Entity<DamageableComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled || args.Damage.Empty)
            return;

        if (!TryComp<InventoryComponent>(ent, out var inventory))
            return;

        if (!_inventory.TryGetSlots(ent, out var slots))
            return;

        foreach (var slot in slots)
        {
            if (!_inventory.TryGetSlotEntity(ent, slot.Name, out var equipped, inventory))
                continue;

            if (!TryComp<ArmorPlateComponent>(equipped, out var armorComp))
                continue;

            if (!TryComp<StorageComponent>(equipped, out var storage))
            {
                Log.Warning($"Armor {ToPrettyString(equipped.Value)} has ArmorPlateComponent but no StorageComponent!");
                continue;
            }

            if (armorComp.ActivePlate == null || !armorComp.HasActivePlate)
            {
                foreach (var item in storage.Container.ContainedEntities)
                {
                    if (IsArmorPlate(item, out var platePrototypeId))
                    {
                        armorComp.ActivePlate = item;
                        armorComp.HasActivePlate = true;

                        if (platePrototypeId != null && ArmorPlateComponent.AcceptedPlates.TryGetValue(platePrototypeId, out var plateCapacity))
                        {
                            armorComp.DamageCapacity = plateCapacity;
                        }

                        UpdateSpeedModifiers(equipped.Value, armorComp, platePrototypeId);

                        armorComp.CurrentDamage = _plateDurability.GetValueOrDefault(item, FixedPoint2.Zero);
                        break;
                    }
                }

                if (armorComp.ActivePlate == null)
                {
                    armorComp.HasActivePlate = false;
                    UpdateSpeedModifiers(equipped.Value, armorComp, null);
                    Dirty(equipped.Value, armorComp);
                    continue;
                }
            }

            if (!args.Damage.DamageDict.TryGetValue("Piercing", out var piercingDamage) || piercingDamage <= 0)
                return;

            armorComp.CurrentDamage += piercingDamage;

            if (armorComp.ActivePlate != null)
            {
                _plateDurability[armorComp.ActivePlate.Value] = armorComp.CurrentDamage;
            }

            if (armorComp.CurrentDamage >= armorComp.DamageCapacity)
            {
                if (armorComp.ActivePlate != null)
                {
                    _plateDurability[armorComp.ActivePlate.Value] = armorComp.CurrentDamage;

                    QueueDel(armorComp.ActivePlate.Value);

                    armorComp.ActivePlate = null;
                    armorComp.HasActivePlate = false;
                    armorComp.CurrentDamage = 0;

                    UpdateSpeedModifiers(equipped.Value, armorComp, null);
                }

                if (armorComp.ShowBreakPopup)
                {
                    _popup.PopupEntity(
                        Loc.GetString("armor-plate-break"),
                        ent,
                        ent,
                        PopupType.MediumCaution
                    );
                }
            }

            Dirty(equipped.Value, armorComp);

            var staminaDamage = piercingDamage.Float() * armorComp.StaminaDamageMultiplier;
            _stamina.TakeStaminaDamage(ent, staminaDamage);

            args.Damage.DamageDict.Remove("Piercing");

            return;
        }
    }

    private void OnExamined(Entity<ArmorPlateComponent> ent, ref ExaminedEvent args)
    {
        var armor = ent.Comp;

        if (!TryComp<StorageComponent>(ent, out var storage))
        {
            args.PushMarkup(Loc.GetString("armor-plate-examine-no-storage"));
            return;
        }

        if (armor.ActivePlate != null && armor.HasActivePlate)
        {
            var platePrototypeId = MetaData(armor.ActivePlate.Value).EntityPrototype?.ID;

            if (platePrototypeId != null && ArmorPlateComponent.AcceptedPlates.TryGetValue(platePrototypeId, out var plateCapacity))
            {
                armor.DamageCapacity = plateCapacity;
            }

            var durabilityPercent = ((armor.DamageCapacity - armor.CurrentDamage) / armor.DamageCapacity) * 100;
            var durabilityPercentFloat = durabilityPercent.Float();

            var plateName = "unknown";
            if (platePrototypeId != null && _prototypeManager.TryIndex<EntityPrototype>(platePrototypeId, out var platePrototype))
            {
                plateName = platePrototype.Name;
            }

            var durabilityColor = durabilityPercentFloat switch
            {
                > 66f => "green",
                >= 33f => "yellow",
                _ => "red"
            };

            args.PushMarkup(Loc.GetString("armor-plate-examine-with-plate",
                ("plateName", plateName),
                ("percent", durabilityPercent),
                ("durabilityColor", durabilityColor)));
        }
        else
        {
            args.PushMarkup(Loc.GetString("armor-plate-examine-no-plate"));
        }
    }

    /// <summary>
    /// Updates the speed modifiers based on the current armor plate.
    /// </summary>
    private void UpdateSpeedModifiers(EntityUid armorUid, ArmorPlateComponent armorComp, string? platePrototypeId)
    {
        if (platePrototypeId == null || !ArmorPlateComponent.PlateSpeedModifiers.TryGetValue(platePrototypeId, out var modifiers))
        {
            armorComp.WalkSpeedModifier = 1.0f;
            armorComp.SprintSpeedModifier = 1.0f;
        }
        else
        {
            armorComp.WalkSpeedModifier = modifiers.walk;
            armorComp.SprintSpeedModifier = modifiers.sprint;
        }

        if (_inventory.TryGetContainingEntity(armorUid, out var wearer))
        {
            _movementSpeed.RefreshMovementSpeedModifiers(wearer.Value);
        }
    }
}
