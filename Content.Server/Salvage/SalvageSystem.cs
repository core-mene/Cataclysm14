// SPDX-FileCopyrightText: 2022 20kdc
// SPDX-FileCopyrightText: 2022 Alex Evgrashin
// SPDX-FileCopyrightText: 2022 Chris V
// SPDX-FileCopyrightText: 2022 Errant
// SPDX-FileCopyrightText: 2022 Jackson
// SPDX-FileCopyrightText: 2022 Justin Trotter
// SPDX-FileCopyrightText: 2022 Júlio César Ueti
// SPDX-FileCopyrightText: 2022 Moony
// SPDX-FileCopyrightText: 2022 Morbo
// SPDX-FileCopyrightText: 2022 Paul Ritter
// SPDX-FileCopyrightText: 2022 Rane
// SPDX-FileCopyrightText: 2022 SpaceManiac
// SPDX-FileCopyrightText: 2022 Veritius
// SPDX-FileCopyrightText: 2022 metalgearsloth
// SPDX-FileCopyrightText: 2022 mirrorcult
// SPDX-FileCopyrightText: 2022 wrexbe
// SPDX-FileCopyrightText: 2023 Checkraze
// SPDX-FileCopyrightText: 2023 Nemanja
// SPDX-FileCopyrightText: 2023 Pieter-Jan Briers
// SPDX-FileCopyrightText: 2023 Slava0135
// SPDX-FileCopyrightText: 2023 Visne
// SPDX-FileCopyrightText: 2023 Vordenburg
// SPDX-FileCopyrightText: 2023 deltanedas
// SPDX-FileCopyrightText: 2023 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2024 Dvir
// SPDX-FileCopyrightText: 2024 ElectroJr
// SPDX-FileCopyrightText: 2024 Leon Friedrich
// SPDX-FileCopyrightText: 2024 SlamBamActionman
// SPDX-FileCopyrightText: 2024 Vasilis
// SPDX-FileCopyrightText: 2025 Whatstone
// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Radio.EntitySystems;
using Content.Shared.Radio;
using Content.Shared.Salvage;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Chat.Managers;
using Content.Server.Gravity;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Construction.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.EntitySerialization.Systems;
using Content.Server.Weather;
using Content.Shared.Weather;

namespace Content.Server.Salvage
{
    public sealed partial class SalvageSystem : SharedSalvageSystem
    {
        [Dependency] private readonly IChatManager _chat = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AnchorableSystem _anchorable = default!;
        [Dependency] private readonly BiomeSystem _biome = default!;
        [Dependency] private readonly WeatherSystem _weather = default!;
        [Dependency] private readonly DungeonSystem _dungeon = default!;
        [Dependency] private readonly GravitySystem _gravity = default!;
        [Dependency] private readonly MapLoaderSystem _loader = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;
        [Dependency] private readonly ShuttleSystem _shuttle = default!;
        [Dependency] private readonly ShuttleConsoleSystem _shuttleConsoles = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;

        private EntityQuery<MapGridComponent> _gridQuery;
        private EntityQuery<TransformComponent> _xformQuery;

        public override void Initialize()
        {
            base.Initialize();

            _gridQuery = GetEntityQuery<MapGridComponent>();
            _xformQuery = GetEntityQuery<TransformComponent>();

            InitializeExpeditions();
            InitializeMagnet();
            InitializeRunner();
        }

        // Frontier
        public override void Shutdown()
        {
            ShutdownExpeditions();
            base.Shutdown();
        }
        // End Frontier

        private void Report(EntityUid source, string channelName, string messageKey, params (string, object)[] args)
        {
            var message = args.Length == 0 ? Loc.GetString(messageKey) : Loc.GetString(messageKey, args);
            var channel = _prototypeManager.Index<RadioChannelPrototype>(channelName);
            _radioSystem.SendRadioMessage(source, message, channel, source);
        }

        public override void Update(float frameTime)
        {
            UpdateExpeditions();
            UpdateMagnet();
            UpdateRunner();
        }
    }
}

