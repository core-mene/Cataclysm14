// SPDX-FileCopyrightText: 2022 Moony
// SPDX-FileCopyrightText: 2022 wrexbe
// SPDX-FileCopyrightText: 2023 DrSmugleaf
// SPDX-FileCopyrightText: 2023 Ygg01
// SPDX-FileCopyrightText: 2023 metalgearsloth
// SPDX-FileCopyrightText: 2025 Redrover1760
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Prototypes;

/// <summary>
/// Prototype for a lobby background the game can choose.
/// </summary>
[Prototype]
public sealed partial class LobbyBackgroundPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; set; } = default!;

    /// <summary>
    /// The sprite to use as the background. This should ideally be 1920x1080.
    /// </summary>
    [DataField("background", required: true)]
    public ResPath Background = default!;
}
