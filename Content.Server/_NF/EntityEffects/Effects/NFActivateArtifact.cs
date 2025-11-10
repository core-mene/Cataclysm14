// SPDX-FileCopyrightText: 2024 Dvir
// SPDX-FileCopyrightText: 2024 Whatstone
// SPDX-FileCopyrightText: 2025 tonotom1
//
// SPDX-License-Identifier: MPL-2.0

using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntityEffects;

public sealed partial class NFActivateArtifact : EntityEffect
{
    /// <summary>
    /// Disintegrate chance
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ProbabilityBase = 0.012f; // MONO: 0.05 >> 0.012 (1.2%)

    /// <summary>
    /// Disintegrate chance bonus on grids that are not a station
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ProbabilityBonusOffStationGrid = 0.15f; // 15%

    /// <summary>
    /// The range around the artifact that it will spawn the entity
    /// </summary>
    [DataField]
    public float Range = 0.5f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs)
            return;
        var artifact = args.EntityManager.EntitySysManager.GetEntitySystem<ArtifactSystem>();
        artifact.NFActivateArtifact(reagentArgs.TargetEntity, ProbabilityBase, ProbabilityBonusOffStationGrid, Range);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        null;
}
