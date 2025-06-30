// SPDX-FileCopyrightText: 2023 Checkraze
// SPDX-FileCopyrightText: 2025 sleepyyapril
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class CargoPalletConsoleInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// estimated apraised value of all the entities on top of pallets on the same grid as the console
    /// </summary>
    public int Appraisal;

    /// <summary>
    /// number of entities on top of pallets on the same grid as the console
    /// </summary>
    public int Count;

    /// <summary>
    /// are the buttons enabled
    /// </summary>
    public bool Enabled;

    /// <summary>
    /// the multiplier for the given cargo sell
    /// </summary>
    public float? Multiplier;

    public CargoPalletConsoleInterfaceState(int appraisal, int count, bool enabled, float multiplier = 1f)
    {
        Appraisal = appraisal;
        Count = count;
        Enabled = enabled;
        Multiplier = multiplier;
    }
}
