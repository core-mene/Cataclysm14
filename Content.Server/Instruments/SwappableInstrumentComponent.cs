// SPDX-FileCopyrightText: 2022 EmoGarbage404
// SPDX-FileCopyrightText: 2023 DrSmugleaf
// SPDX-FileCopyrightText: 2024 Whatstone
// SPDX-FileCopyrightText: 2025 bitcrushing
//
// SPDX-License-Identifier: MPL-2.0

namespace Content.Server.Instruments;

[RegisterComponent]
public sealed partial class SwappableInstrumentComponent : Component
{
    /// <summary>
    /// Used to store the different instruments that can be swapped between.
    /// string = display name of the instrument
    /// byte 1 = instrument midi program
    /// byte 2 = instrument midi bank
    /// </summary>
    [DataField("instrumentList", required: true)]
    public Dictionary<string, (byte, byte)> InstrumentList = new();
}
