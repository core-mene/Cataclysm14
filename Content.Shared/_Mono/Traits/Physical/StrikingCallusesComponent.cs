// SPDX-FileCopyrightText: 2025 ark1368
//
// SPDX-License-Identifier: MPL-2.0

namespace Content.Shared._Mono.Traits.Physical;

/// <summary>
/// Increases unarmed strike base damage by a flat amount.
/// </summary>
[RegisterComponent]
public sealed partial class StrikingCallusesComponent : Component
{
    /// <summary>
    /// Flat bonus to add to unarmed base damage (Blunt).
    /// </summary>
    [DataField]
    public int BluntBonus = 2;
}
