// SPDX-FileCopyrightText: 2022 Paul Ritter
// SPDX-FileCopyrightText: 2022 Putnam3145
// SPDX-FileCopyrightText: 2022 Vera Aguilera Puerto
// SPDX-FileCopyrightText: 2022 mirrorcult
// SPDX-FileCopyrightText: 2022 wrexbe
// SPDX-FileCopyrightText: 2023 DrSmugleaf
// SPDX-FileCopyrightText: 2023 Nemanja
// SPDX-FileCopyrightText: 2023 Visne
// SPDX-FileCopyrightText: 2023 metalgearsloth
// SPDX-FileCopyrightText: 2025 Redrover1760
//
// SPDX-License-Identifier: AGPL-3.0-or-later


using Content.Server.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.GameTicking.Presets
{
    /// <summary>
    ///     A round-start setup preset, such as which antagonists to spawn.
    /// </summary>
    [Prototype]
    public sealed partial class GamePresetPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("alias")]
        public string[] Alias = Array.Empty<string>();

        [DataField("name")]
        public string ModeTitle = "????";

        [DataField("description")]
        public string Description = string.Empty;

        [DataField("showInVote")]
        public bool ShowInVote;

        [DataField("minPlayers")]
        public int? MinPlayers;

        [DataField("maxPlayers")]
        public int? MaxPlayers;

        [DataField("rules", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public IReadOnlyList<string> Rules { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// If specified, the gamemode will only be run with these maps.
        /// If none are elligible, the global fallback will be used.
        /// </summary>
        [DataField("supportedMaps", customTypeSerializer: typeof(PrototypeIdSerializer<GameMapPoolPrototype>))]
        public string? MapPool;
    }
}
