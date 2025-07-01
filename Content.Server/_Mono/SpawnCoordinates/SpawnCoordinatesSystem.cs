// SPDX-FileCopyrightText: 2025 ark1368
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Managers;
using Content.Shared.GameTicking;

namespace Content.Server._Mono.SpawnCoordinates;

/// <summary>
/// System that displays spawn coordinates to players when they spawn.
/// </summary>
public sealed class SpawnCoordinatesSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        // Get the coordinates
        var coordinates = _transform.GetMapCoordinates(ev.Mob);

        // Format the coordinates message
        var message = $"Spawn Coordinates: X={(int)coordinates.X}, Y={(int)coordinates.Y}";

        // Send server message
        _chatManager.DispatchServerMessage(ev.Player, message);
    }
}
