using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged.Prediction;

[Serializable, NetSerializable]
public sealed class PredictedProjectileHitEvent : EntityEventArgs
{
    public readonly NetEntity Projectile;
    public readonly HashSet<(NetEntity Id, MapCoordinates Coordinates)> Hit;

    public PredictedProjectileHitEvent(NetEntity projectile, HashSet<(NetEntity Id, MapCoordinates Coordinates)> hit)
    {
        Projectile = projectile;
        Hit = hit;
    }
}
