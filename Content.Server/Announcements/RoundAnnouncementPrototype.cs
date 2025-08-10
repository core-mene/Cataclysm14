// SPDX-FileCopyrightText: 2022 Leon Friedrich
// SPDX-FileCopyrightText: 2023 DrSmugleaf
// SPDX-FileCopyrightText: 2023 Visne
// SPDX-FileCopyrightText: 2023 metalgearsloth
// SPDX-FileCopyrightText: 2025 Redrover1760
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Announcements;

/// <summary>
/// Used for any announcements on the start of a round.
/// </summary>
[Prototype]
public sealed partial class RoundAnnouncementPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("sound")] public SoundSpecifier? Sound;

    [DataField("message")] public string? Message;
}
