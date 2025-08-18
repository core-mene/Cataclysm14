// SPDX-FileCopyrightText: 2020 Metal Gear Sloth
// SPDX-FileCopyrightText: 2020 Víctor Aguilera Puerto
// SPDX-FileCopyrightText: 2020 metalgearsloth
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto
// SPDX-FileCopyrightText: 2022 wrexbe
// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.Atmos
{
    public struct Hotspot
    {
        [ViewVariables]
        public bool Valid;

        [ViewVariables]
        public bool SkippedFirstProcess;

        [ViewVariables]
        public bool Bypassing;

        [ViewVariables]
        public float Temperature;

        [ViewVariables]
        public float Volume;

        /// <summary>
        ///     State for the fire sprite.
        /// </summary>
        [ViewVariables]
        public byte State;

        [ViewVariables]
        public HotspotType Type;
    }
}

public enum HotspotType : byte
{
    Gas = 0,
    Puddle = 1
}
