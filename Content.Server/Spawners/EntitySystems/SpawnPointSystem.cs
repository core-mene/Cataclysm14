// SPDX-FileCopyrightText: 2022 Moony
// SPDX-FileCopyrightText: 2022 Pieter-Jan Briers
// SPDX-FileCopyrightText: 2023 DrSmugleaf
// SPDX-FileCopyrightText: 2023 Flipp Syder
// SPDX-FileCopyrightText: 2023 ShadowCommander
// SPDX-FileCopyrightText: 2023 Tom Leys
// SPDX-FileCopyrightText: 2023 Visne
// SPDX-FileCopyrightText: 2024 Dvir
// SPDX-FileCopyrightText: 2024 Errant
// SPDX-FileCopyrightText: 2024 Krunklehorn
// SPDX-FileCopyrightText: 2024 Leon Friedrich
// SPDX-FileCopyrightText: 2024 Whatstone
// SPDX-FileCopyrightText: 2024 metalgearsloth
// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 Redrover1760
// SPDX-FileCopyrightText: 2025 ark1368
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems;

public sealed class SpawnPointSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        // TODO: Cache all this if it ends up important.
        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();

        while ( points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                continue;

            // Delta-V: Allow setting a desired SpawnPointType
            if (args.DesiredSpawnPointType != SpawnPointType.Unset)
            {
                var isMatchingJob = spawnPoint.SpawnType == SpawnPointType.Job &&
                    (args.Job == null || spawnPoint.Job == args.Job);

                switch (args.DesiredSpawnPointType)
                {
                    case SpawnPointType.Job when isMatchingJob:
                    case SpawnPointType.LateJoin when spawnPoint.SpawnType == SpawnPointType.LateJoin:
                    case SpawnPointType.Observer when spawnPoint.SpawnType == SpawnPointType.Observer:
                        possiblePositions.Add(xform.Coordinates);
                        break;
                    default:
                        continue;
                }
            }

            if (_gameTicker.RunLevel == GameRunLevel.InRound && spawnPoint.SpawnType == SpawnPointType.LateJoin)
            {
                possiblePositions.Add(xform.Coordinates);
            }

            if (_gameTicker.RunLevel != GameRunLevel.InRound &&
                spawnPoint.SpawnType == SpawnPointType.Job &&
                (args.Job == null || spawnPoint.Job == args.Job))
            {
                possiblePositions.Add(xform.Coordinates);
            }
        }

        if (possiblePositions.Count == 0)
        {
            // Ok we've still not returned, but we need to put them /somewhere/.
            // However, we should only use spawn points from the same station to prevent cross-faction spawning.
            var points2 = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            var stationFallbackPositions = new List<EntityCoordinates>();

            while (points2.MoveNext(out var uid, out var spawnPoint, out var xform))
            {
                // Only use spawn points from the same station to prevent cross-faction spawning
                if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                    continue;

                stationFallbackPositions.Add(xform.Coordinates);
            }

            if (stationFallbackPositions.Count > 0)
            {
                possiblePositions.AddRange(stationFallbackPositions);
            }
            else
            {
                if (TryComp<MetaDataComponent>(args.Station, out var data) && data.EntityPrototype != null)
                    Log.Error($"No spawn points were available for station {data.EntityPrototype}! (uid: {args.Station}, name: {data.EntityName})");
                else
                    Log.Error($"No spawn points were available for station {args.Station}!, station prototype is null.");
                return;
            }
        }

        var spawnLoc = _random.Pick(possiblePositions);

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station,
            session: args.Session); // Frontier
    }
}
