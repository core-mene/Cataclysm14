// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 Ilya246
// SPDX-FileCopyrightText: 2025 ark1368
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared._Mono.Radar;
using Content.Shared.Projectiles;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Mono.Radar;

public sealed partial class RadarBlipSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private EntityQuery<PhysicsComponent> _physQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestBlipsEvent>(OnBlipsRequested);

        _physQuery = GetEntityQuery<PhysicsComponent>();
    }

    private void OnBlipsRequested(RequestBlipsEvent ev, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(ev.Radar, out var radarUid))
            return;

        if (!TryComp<RadarConsoleComponent>(radarUid, out var radar))
            return;

        var blips = AssembleBlipsReport((EntityUid)radarUid, radar);
        var hitscans = AssembleHitscanReport((EntityUid)radarUid, radar);

        // Combine the blips and hitscan lines
        var giveEv = new GiveBlipsEvent(blips, hitscans);
        RaiseNetworkEvent(giveEv, args.SenderSession);
    }

    private List<(NetCoordinates Position, Vector2 Vel, float Scale, Color Color, RadarBlipShape Shape)> AssembleBlipsReport(EntityUid uid, RadarConsoleComponent? component = null)
    {
        var blips = new List<(NetCoordinates Position, Vector2 Vel, float Scale, Color Color, RadarBlipShape Shape)>();

        if (Resolve(uid, ref component))
        {
            var radarXform = Transform(uid);
            var radarPosition = _xform.GetWorldPosition(uid);
            var radarGrid = _xform.GetGrid(uid);
            var radarMapId = radarXform.MapID;

            // Check if the radar is on an FTL map
            var isFtlMap = HasComp<FTLComponent>(radarXform.GridUid);

            var blipQuery = EntityQueryEnumerator<RadarBlipComponent, TransformComponent>();

            while (blipQuery.MoveNext(out var blipUid, out var blip, out var blipXform))
            {
                if (!blip.Enabled)
                    continue;

                // This prevents blips from showing on radars that are on different maps
                if (blipXform.MapID != radarMapId)
                    continue;

                var blipGrid = blipXform.GridUid;

                // if (HasComp<CircularShieldRadarComponent>(blipUid))
                // {
                //     // Skip if in FTL
                //     if (isFtlMap)
                //         continue;
                //
                //     // Skip if no grid
                //     if (blipGrid == null)
                //         continue;
                //
                //     // Ensure the grid is a valid MapGrid
                //     if (!HasComp<MapGridComponent>(blipGrid.Value))
                //         continue;
                //
                //     // Ensure the shield is a direct child of the grid
                //     if (blipXform.ParentUid != blipGrid)
                //         continue;
                // }

                var blipVelocity = _physics.GetMapLinearVelocity(blipUid);

                var distance = (blipXform.WorldPosition - radarPosition).Length();
                if (distance > component.MaxRange)
                    continue;

                if (blip.RequireNoGrid && blipGrid != null // if we want no grid but we are on a grid
                    || !blip.VisibleFromOtherGrids && blipGrid != radarGrid) // or if we don't want to be visible from other grids but we're on another grid
                    continue; // don't show this blip

                // due to PVS being a thing, things will break if we try to parent to not the map or a grid
                var coord = blipXform.Coordinates;
                if (blipXform.ParentUid != blipXform.MapUid && blipXform.ParentUid != blipGrid)
                    coord = _xform.WithEntityId(coord, blipGrid ?? blipXform.MapUid!.Value);
                // we're parented to either the map or a grid and this is relative velocity so account for grid movement
                if (blipGrid != null)
                    blipVelocity -= _physics.GetLinearVelocity(blipGrid.Value, coord.Position);

                blips.Add((GetNetCoordinates(coord), blipVelocity, blip.Scale, blip.RadarColor, blip.Shape));
            }
        }

        return blips;
    }

    /// <summary>
    /// Assembles trajectory information for hitscan projectiles to be displayed on radar
    /// </summary>
    private List<(Vector2 Start, Vector2 End, float Thickness, Color Color)> AssembleHitscanReport(EntityUid uid, RadarConsoleComponent? component = null)
    {
        var hitscans = new List<(Vector2 Start, Vector2 End, float Thickness, Color Color)>();

        if (!Resolve(uid, ref component))
            return hitscans;

        var radarXform = Transform(uid);
        var radarPosition = _xform.GetWorldPosition(uid);
        var radarGrid = _xform.GetGrid(uid);
        var radarMapId = radarXform.MapID;

        var hitscanQuery = EntityQueryEnumerator<HitscanRadarComponent>();

        while (hitscanQuery.MoveNext(out var hitscanUid, out var hitscan))
        {
            if (!hitscan.Enabled)
                continue;

            // Check if either the start or end point is within radar range
            var startDistance = (hitscan.StartPosition - radarPosition).Length();
            var endDistance = (hitscan.EndPosition - radarPosition).Length();

            if (startDistance > component.MaxRange && endDistance > component.MaxRange)
                continue;

            hitscans.Add((hitscan.StartPosition, hitscan.EndPosition, hitscan.LineThickness, hitscan.RadarColor));
        }

        return hitscans;
    }
}
