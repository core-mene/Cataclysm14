// SPDX-FileCopyrightText: 2018 PJB3005
// SPDX-FileCopyrightText: 2019 DamianX
// SPDX-FileCopyrightText: 2019 Ephememory
// SPDX-FileCopyrightText: 2019 Pieter-Jan Briers
// SPDX-FileCopyrightText: 2019 PrPleGoo
// SPDX-FileCopyrightText: 2019 Silver
// SPDX-FileCopyrightText: 2019 ZelteHonor
// SPDX-FileCopyrightText: 2019 moneyl
// SPDX-FileCopyrightText: 2020 20kdc
// SPDX-FileCopyrightText: 2020 Clyybber
// SPDX-FileCopyrightText: 2020 ColdAutumnRain
// SPDX-FileCopyrightText: 2020 Exp
// SPDX-FileCopyrightText: 2020 FL-OZ
// SPDX-FileCopyrightText: 2020 Memory
// SPDX-FileCopyrightText: 2020 Metal Gear Sloth
// SPDX-FileCopyrightText: 2020 Swept
// SPDX-FileCopyrightText: 2020 Vince
// SPDX-FileCopyrightText: 2020 Víctor Aguilera Puerto
// SPDX-FileCopyrightText: 2020 chairbender
// SPDX-FileCopyrightText: 2020 py01
// SPDX-FileCopyrightText: 2020 zumorica
// SPDX-FileCopyrightText: 2021 Acruid
// SPDX-FileCopyrightText: 2021 Galactic Chimp
// SPDX-FileCopyrightText: 2021 Paul
// SPDX-FileCopyrightText: 2021 Paul Ritter
// SPDX-FileCopyrightText: 2021 ShadowCommander
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto
// SPDX-FileCopyrightText: 2021 Visne
// SPDX-FileCopyrightText: 2022 metalgearsloth
// SPDX-FileCopyrightText: 2022 mirrorcult
// SPDX-FileCopyrightText: 2022 wrexbe
// SPDX-FileCopyrightText: 2023 DrSmugleaf
// SPDX-FileCopyrightText: 2023 Leon Friedrich
// SPDX-FileCopyrightText: 2025 Ilya246
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Construction.Components
{
    [RegisterComponent, Access(typeof(ConstructionSystem))]
    public sealed partial class ConstructionComponent : SharedConstructionComponent // Goobstation
    {
        [DataField("graph", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<ConstructionGraphPrototype>))]
        public string Graph { get; set; } = string.Empty;

        [DataField("node", required:true)]
        public string Node { get; set; } = default!;

        [DataField("edge")]
        public int? EdgeIndex { get; set; } = null;

        [DataField("step")]
        public int StepIndex { get; set; } = 0;

        [DataField("containers")]
        public HashSet<string> Containers { get; set; } = new();

        [DataField("defaultTarget")]
        public string? TargetNode { get; set; } = null;

        [ViewVariables]
        public int? TargetEdgeIndex { get; set; } = null;

        [ViewVariables]
        public Queue<string>? NodePathfinding { get; set; } = null;

        [DataField("deconstructionTarget")]
        public string? DeconstructionNode { get; set; } = "start";

        [ViewVariables]
        // TODO Force flush interaction queue before serializing to YAML.
        // Otherwise you can end up with entities stuck in invalid states (e.g., waiting for DoAfters).
        public readonly Queue<object> InteractionQueue = new();
    }
}
