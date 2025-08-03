// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Mono.Miner;

[RegisterComponent]
public sealed partial class ItemGeneratorComponent : Component
{

    /// <summary>
    /// Time since last update
    /// </summary>
    [DataField("lastUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastUpdate = TimeSpan.Zero;

    /// <summary>
    /// Generator working SFX
    /// </summary>
    [DataField("miningSound")]
    public SoundSpecifier MiningSound = new SoundPathSpecifier("/Audio/Machines/circuitprinter.ogg");

    /// <summary>
    /// Time between item generation actions
    /// </summary>
    [DataField("miningInterval")]
    public float MiningInterval = 10.0f;

    /// <summary>
    /// Power consumption in watts
    /// </summary>
    [DataField("powerDraw")]
    public float PowerDraw = 30000f;

    /// <summary>
    /// List of entities that can be spawned by this component.
    /// If you don't want it spawning inside the generator's container, set it to spawn a random spawner instead.
    /// Each will be randomly chosen separately.
    /// </summary>
    [DataField]
    public List<EntProtoId> Prototypes = [];

    /// <summary>
    ///     Origin map and grid of this [MINER].
    ///     If a station wasn't tied to a given grid when the bomb was spawned,
    ///     this will be filled in instead.
    /// </summary>
    public (MapId, EntityUid?)? OriginMapGrid; // totally not shitcode from nukecomponent btw

    /// <summary>
    ///     Origin station of this [MINER], if it exists.
    ///     If this doesn't exist, then the origin grid and map will be filled in, instead.
    /// </summary>
    public EntityUid? OriginStation;

    [DataField]
    public bool RequireExpedition = false;
}
