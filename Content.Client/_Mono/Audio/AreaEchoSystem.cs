// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
//
// SPDX-License-Identifier: MPL-2.0

using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Content.Client.Light.EntitySystems;
using Content.Shared._Mono.CCVar;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Client._Mono.Audio;

// i would make this more generalised but theres not really any point
/// <summary>
///     Handles making sounds 'echo' in large, open spaces.
/// </summary>
public sealed class AreaEchoSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly AudioEffectSystem _audioEffectSystem = default!;
    [Dependency] private readonly RoofSystem _roofSystem = default!;

    /// <summary>
    ///     The directions that are raycasted to determine size for echo.
    ///         Used relative to the grid.
    /// </summary>
    private Angle[] _calculatedDirections = [Direction.North.ToAngle(), Direction.West.ToAngle(), Direction.South.ToAngle(), Direction.East.ToAngle()];

    /// <summary>
    ///     Values for the minimum arbitrary size at which a certain audio preset
    ///         is picked for sounds. The higher the highest distance here is,
    ///         the generally more calculations it has to do.
    /// </summary>
    /// <remarks>
    ///     Keep in ascending order.
    /// </remarks>
    private static readonly List<(float, ProtoId<AudioPresetPrototype>)> DistancePresets = new() { (15f, "Hallway"), (25f, "Auditorium"), (40f, "ConcertHall") };

    /// <summary>
    ///     When is the next time we should check all audio entities and see if they are eligible to be updated.
    /// </summary>
    private TimeSpan _nextExistingUpdate = TimeSpan.Zero;

    /// <summary>
    ///     Collision mask for echoes.
    /// </summary>
    private int _echoLayer = (int)(CollisionGroup.Opaque | CollisionGroup.Impassable); // this could be better but whatever

    private bool _echoEnabled = true;
    private float _calculationalFidelity = 5f;
    private TimeSpan _calculationInterval = TimeSpan.FromSeconds(15); // how often we should check existing audio re-apply or remove echo from them when necessary

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<RoofComponent> _roofQuery;

    public override void Initialize()
    {
        base.Initialize();

        _configurationManager.OnValueChanged(MonoCVars.AreaEchoEnabled, x => _echoEnabled = x, invokeImmediately: true);
        _configurationManager.OnValueChanged(MonoCVars.AreaEchoHighResolution, x => _calculatedDirections = GetEffectiveDirections(x), invokeImmediately: true);

        _configurationManager.OnValueChanged(MonoCVars.AreaEchoStepFidelity, x => _calculationalFidelity = x);
        _configurationManager.OnValueChanged(MonoCVars.AreaEchoRecalculationInterval, x => _calculationInterval = x, invokeImmediately: true);

        _gridQuery = GetEntityQuery<MapGridComponent>();
        _roofQuery = GetEntityQuery<RoofComponent>();

        SubscribeLocalEvent<AudioComponent, EntParentChangedMessage>(OnAudioParentChanged);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (!_echoEnabled ||
            _gameTiming.CurTime < _nextExistingUpdate)
            return;

        _nextExistingUpdate = _gameTiming.CurTime + _calculationInterval;

        var minimumMagnitude = DistancePresets.TryFirstOrNull(out var first) ? first.Value.Item1 : 0f;
        DebugTools.Assert(minimumMagnitude > 0f, "First distance preset was less than or equal to 0!");
        if (minimumMagnitude <= 0f)
            return;

        var maximumMagnitude = DistancePresets.Last().Item1;
        var audioEnumerator = EntityQueryEnumerator<AudioComponent>();

        while (audioEnumerator.MoveNext(out var uid, out var audioComponent))
        {
            if (!CanAudioEcho(audioComponent) ||
                !audioComponent.Playing)
                continue;

            ProcessAudioEntity((uid, audioComponent), Transform(uid), minimumMagnitude, maximumMagnitude);
        }
    }

    /// <summary>
    ///     Returns all four cardinal directions when <paramref name="highResolution"/> is false.
    ///         Otherwise, returns all eight intercardinal and cardinal directions as listed in
    ///         <see cref="DirectionExtensions.AllDirections"/>. 
    /// </summary>
    public static Angle[] GetEffectiveDirections(bool highResolution)
    {
        if (highResolution)
        {
            var allDirections = DirectionExtensions.AllDirections;
            var directions = new Angle[allDirections.Length];

            for (var i = 0; i < allDirections.Length; i++)
                directions[i] = allDirections[i].ToAngle();

            return directions;
        }

        return [Direction.North.ToAngle(), Direction.West.ToAngle(), Direction.South.ToAngle(), Direction.East.ToAngle()];
    }

    /// <summary>
    ///     Takes an entity's <see cref="TransformComponent"/>. Goes through every parent it
    ///         has before reaching one that is a map. Returns the hierarchy
    ///         discovered, which includes the given <paramref name="originEntity"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private List<Entity<TransformComponent>> TryGetHierarchyBeforeMap(Entity<TransformComponent> originEntity)
    {
        var hierarchy = new List<Entity<TransformComponent>>() { originEntity };

        ref var currentEntity = ref originEntity;
        ref var currentTransformComponent = ref currentEntity.Comp;

        var mapUid = currentEntity.Comp.MapUid;

        while (currentTransformComponent.ParentUid != mapUid /* break when the next entity is a map... */ &&
            currentTransformComponent.ParentUid.IsValid() /* ...or invalid */ )
        {
            // iterate to next entity
            var nextUid = currentTransformComponent.ParentUid;
            currentEntity.Owner = nextUid;
            currentTransformComponent = Transform(nextUid);

            hierarchy.Add(currentEntity);
        }

        DebugTools.Assert(hierarchy.Count >= 1, "Malformed entity hierarchy! Hierarchy must always contain one element, but it doesn't. How did this happen?");
        return hierarchy;
    }

    /// <summary>
    ///     Basic check for whether an audio can echo. Doesn't account for distance.
    /// </summary>
    public bool CanAudioEcho(AudioComponent audioComponent)
        => !audioComponent.Global && _echoEnabled;

    /// <summary>
    ///     Gets the length of the direction that reaches the furthest unobstructed
    ///         distance, in an attempt to get the size of the area. Aborts early
    ///         if either grid is missing or the tile isnt rooved.
    /// 
    ///     Returned magnitude is the longest valid length of the ray in each direction,
    ///         divided by the number of total processed angles.
    /// </summary>
    /// <returns>Whether anything was actually processed.</returns>
    // i am the total overengineering guy... and this, is my code.
    /*
        This works under a few assumptions:
        - An entity in space is invalid
        - Any spaced tile is invalid
        - If the grid has RoofComponent:
        - - Only now, will rays end on invalid tiles (space) or unrooved tiles
        - - This is checked every `_calculationalFidelity`-ish tiles. Not precisely. But somewhere around that. Its moreso just proportional to that.
    */
    public bool TryProcessAreaSpaceMagnitude(Entity<TransformComponent> entity, float maximumMagnitude, out float magnitude)
    {
        magnitude = 0f;
        var transformComponent = entity.Comp;

        // get either the grid or other parent entity this entity is on, and it's rotation
        var entityHierarchy = TryGetHierarchyBeforeMap(entity);
        if (entityHierarchy.Count <= 1) // hierarchy always starts with our entity. if it only has our entity, it means the next parent was the map, which we don't want
            return false; // means this entity is in space/otherwise not on a grid

        // at this point, we know that we are somewhere on a grid

        // e.g.: if a sound is inside a crate, this will now be the grid the crate is on; if the sound is just on the grid, this will be the grid that the sound is on.
        var entityGrid = entityHierarchy.Last();

        // this is the last entity, or this entity itself, that this entity has, before the parent is a grid/map. e.g.: if a sound is inside a crate, this will be the crate; if the sound is just on the grid, this will be the sound
        var lastEntityBeforeGrid = entityHierarchy[^2]; // `l[^x]` is analogous to `l[l.Count - x]`
        // `lastEntityBeforeGrid` is obviously directly before `entityGrid`
        // the earlier guard clause makes sure this will always be valid

        if (!_gridQuery.TryGetComponent(entityGrid, out var gridComponent))
            return false;

        var checkRoof = _roofQuery.TryGetComponent(entityGrid, out var roofComponent);
        var tileRef = _mapSystem.GetTileRef(entityGrid, gridComponent, lastEntityBeforeGrid.Comp.Coordinates);

        if (checkRoof &&
            tileRef.Tile.IsEmpty)
            return false;

        var gridRoofEntity = new Entity<MapGridComponent, RoofComponent?>(entityGrid, gridComponent, roofComponent);
        if (checkRoof &&
            !_roofSystem.IsRooved(gridRoofEntity!, tileRef.GridIndices))
            return false;

        var gridTileSize = gridComponent.TileSize;
        var originTileIndices = tileRef.GridIndices;
        var worldPosition = _transformSystem.GetWorldPosition(transformComponent);

        foreach (var direction in _calculatedDirections)
        {
            var directionVector = (direction + entityGrid.Comp.LocalRotation).ToVec();
            var directionFidelityStep = directionVector * _calculationalFidelity;
            var dSqStep = (directionFidelityStep * directionFidelityStep).LengthSquared(); // ???

            var ray = new CollisionRay(worldPosition, directionVector, _echoLayer);
            var rayResults = _physicsSystem.IntersectRay(transformComponent.MapID, ray, maxLength: maximumMagnitude, ignoredEnt: lastEntityBeforeGrid, returnOnFirstHit: true);

            // if we hit something, distance to that is magnitude but it must be lower than maximum. if we didnt hit anything, it's maximum magnitude
            var rayMagnitude = rayResults.TryFirstOrNull(out var firstResult) ?
                MathF.Min(firstResult.Value.Distance, maximumMagnitude) :
                maximumMagnitude;

            var rayMagnitudeSquared = rayMagnitude * rayMagnitude;
            var incrementedRayMagnitudeSquared = 0f;

            // find the furthest distance this ray reaches until its on an unrooved/dataless (space) tile
            var nextCheckedPosition = new Vector2(originTileIndices.X, originTileIndices.Y) + directionFidelityStep;

            for (; incrementedRayMagnitudeSquared < rayMagnitudeSquared;)
            {
                var nextCheckedTilePosition = new Vector2i(
                    (int)MathF.Floor(nextCheckedPosition.X / gridTileSize),
                    (int)MathF.Floor(nextCheckedPosition.Y / gridTileSize)
                );

                if (checkRoof)
                { // if we're checking roofs, end this ray if this tile is unrooved or dataless (latter is inherent of this method)
                    if (!_roofSystem.IsRooved(gridRoofEntity!, nextCheckedTilePosition))
                        break;
                } // if we're not checking roofs, end this ray if this tile is empty/space
                else if (!_mapSystem.TryGetTileRef(entityGrid, gridComponent, nextCheckedTilePosition, out var tile) ||
                    tile.Tile.IsSpace(_tileDefinitionManager))
                    break;

                nextCheckedPosition += directionFidelityStep;
                incrementedRayMagnitudeSquared += dSqStep;
            }

            // todo: more realistic estimation?
            magnitude += incrementedRayMagnitudeSquared > float.Epsilon ?
                MathF.Sqrt(incrementedRayMagnitudeSquared) :
                0f;
        }

        magnitude /= _calculatedDirections.Length;
        return true;
    }

    private void ProcessAudioEntity(Entity<AudioComponent> entity, TransformComponent transformComponent, float minimumMagnitude, float maximumMagnitude)
    {
        TryProcessAreaSpaceMagnitude((entity, transformComponent), maximumMagnitude, out var echoMagnitude);

        if (echoMagnitude > minimumMagnitude)
        {
            ProtoId<AudioPresetPrototype>? bestPreset = null;
            for (var i = DistancePresets.Count - 1; i >= 0; i--)
            {
                var preset = DistancePresets[i];
                if (preset.Item1 < echoMagnitude)
                    continue;

                bestPreset = preset.Item2;
            }

            if (bestPreset != null)
                _audioEffectSystem.TryAddEffect(entity, DistancePresets[0].Item2);
        }
        else
            _audioEffectSystem.TryRemoveEffect(entity);
    }

    private void OnAudioParentChanged(Entity<AudioComponent> entity, ref EntParentChangedMessage args)
    {
        if (args.Transform.MapID == MapId.Nullspace)
            return;

        if (!CanAudioEcho(entity))
            return;

        var minimumMagnitude = DistancePresets.TryFirstOrNull(out var first) ? first.Value.Item1 : 0f;
        DebugTools.Assert(minimumMagnitude > 0f, "First distance preset was less than or equal to 0!");
        if (minimumMagnitude <= 0f)
            return;

        var maximumMagnitude = DistancePresets.Last().Item1;

        ProcessAudioEntity(entity, args.Transform, minimumMagnitude, maximumMagnitude);
    }
}
