using Robust.Shared.Prototypes;
using Content.Shared.Roles;

namespace Content.Server._NF.CryoSleep;

/// <summary>
/// Stores a player's current job prototype ID on their entity.
/// Used to ensure job slots can be properly reopened when a player enters cryosleep,
/// even if they've disconnected and don't have an active mind/session.
/// </summary>
[RegisterComponent]
public sealed partial class PlayerJobComponent : Component
{
    /// <summary>
    /// The current job prototype ID for this player.
    /// </summary>
    [DataField("jobPrototype")]
    public ProtoId<JobPrototype>? JobPrototype;
    
    /// <summary>
    /// The station entity where this player initially spawned.
    /// Used to make sure job slots are only reopened on the correct station.
    /// </summary>
    [DataField("spawnStation")]
    public EntityUid? SpawnStation;
} 