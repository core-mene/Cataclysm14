// SPDX-FileCopyrightText: 2025 Aiden
// SPDX-FileCopyrightText: 2025 BombasterDS
// SPDX-FileCopyrightText: 2025 BombasterDS2
// SPDX-FileCopyrightText: 2025 GoobBot
// SPDX-FileCopyrightText: 2025 Marty
// SPDX-FileCopyrightText: 2025 Misandry
// SPDX-FileCopyrightText: 2025 NotActuallyMarty
// SPDX-FileCopyrightText: 2025 gus
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Client.Clothing.Components;

[RegisterComponent]
public sealed partial class SealableClothingVisualsComponent : Component
{
    [DataField]
    public string SpriteLayer = "sealed";

    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new(); //just use ClothingVisuals like anything else
}
