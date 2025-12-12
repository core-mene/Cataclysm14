using Robust.Shared.Serialization;

namespace Content.Shared.Inventory;

/// <summary>
///     Defines what slot types an item can fit into.
/// </summary>
[Serializable, NetSerializable]
[Flags]
public enum SlotFlags
{
    NONE = 0,
    PREVENTEQUIP = 1 << 0,
    HEAD = 1 << 1,
    EYES = 1 << 2,
    EARS = 1 << 3,
    MASK = 1 << 4,
    OUTERCLOTHING = 1 << 5,
    INNERCLOTHING = 1 << 6,
    NECK = 1 << 7,
    BACK = 1 << 8,
    BELT = 1 << 9,
    GLOVES = 1 << 10,
    IDCARD = 1 << 11,
    POCKET = 1 << 12,
    FEET = 1 << 13,
    SUITSTORAGE = 1 << 14,
    WALLET = 1 << 15, // Frontier: using an unused slot, redefine to a new bit if/when it's used (goodbye ushort)
    BALACLAVA = 1 << 16, // Mono start
    ARMBANDRIGHT = 1 << 17,
    ARMBANDLEFT = 1 << 18,
    HELMETATTACHMENT = 1 << 19, //Mono end
    FACESHIELD = 1 << 20, // Cataclysm14 start
    PANTS = 1 << 21,
    SLINGFRONT = 1 << 22, // Cataclysm14 end
    All = ~NONE,

    WITHOUT_POCKET = All & ~POCKET
}
