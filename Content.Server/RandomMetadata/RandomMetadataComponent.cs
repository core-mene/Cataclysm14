// SPDX-FileCopyrightText: 2022 Kara
// SPDX-FileCopyrightText: 2022 Moony
// SPDX-FileCopyrightText: 2022 Nemanja
// SPDX-FileCopyrightText: 2023 DrSmugleaf
// SPDX-FileCopyrightText: 2025 mikus
//
// SPDX-License-Identifier: AGPL-3.0-or-later


using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server.RandomMetadata;

/// <summary>
///     Randomizes the description and/or the name for an entity by creating it from list of dataset prototypes or strings.
/// </summary>
[RegisterComponent]
public sealed partial class RandomMetadataComponent : Component
{

    [DataField("descriptionSegments")]
    public List<string>? DescriptionSegments;

    [DataField("nameSegments")]
    public List<string>? NameSegments;

    [DataField("nameSeparator")]
    public string NameSeparator = " ";

    [DataField("descriptionSeparator")]
    public string DescriptionSeparator = " ";
    
    /// Goobstation start
    /// <summary>
    /// LocId of the formatting string to use to assemble the <see cref="NameSegments"/> into the entity's name.
    /// Segments will be passed to the localization system with this string as variables named $part0, $part1, $part2, etc.
    /// </summary>
    [DataField]
    public LocId NameFormat = "random-metadata-name-format-default";

    /// <summary>
    /// LocId of the formatting string to use to assemble the <see cref="DescriptionSegments"/> into the entity's description.
    /// Segments will be passed to the localization system with this string as variables named $part0, $part1, $part2, etc.
    /// </summary>
    [DataField]
    public LocId DescriptionFormat = "random-metadata-description-format-default"; /// Goobstation end
}
