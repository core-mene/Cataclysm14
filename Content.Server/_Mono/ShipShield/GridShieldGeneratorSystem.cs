using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Mono.ShipShield;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Singularity.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._Mono.ShipShield;

public sealed class GridShieldGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AppearanceSystem _visualizer = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridShieldGeneratorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GridShieldGeneratorComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeLocalEvent<GridShieldGeneratorComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<GridShieldGeneratorComponent, ActivateInWorldEvent>(OnActivated);
        SubscribeLocalEvent<GridShieldGeneratorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<GridShieldGeneratorComponent, ReAnchorEvent>(OnReanchorEvent);
        SubscribeLocalEvent<GridShieldGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<GridShieldGeneratorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<GridShieldGeneratorComponent, PowerChangedEvent>(OnPowerStateChanged);
        SubscribeLocalEvent<GridShieldGeneratorComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, GridShieldGeneratorComponent component, MapInitEvent args)
    {
        // Check power status on map init
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver) && powerReceiver.Powered && component.Enabled)
        {
            TurnOn(uid, component);
        }
    }

    private void OnInit(EntityUid uid, GridShieldGeneratorComponent component, ComponentInit args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver))
        {
            _visualizer.SetData(uid, ContainmentFieldGeneratorVisuals.PowerLight,
                powerReceiver.Powered ? PowerLevelVisuals.HighPower : PowerLevelVisuals.NoPower);

            _visualizer.SetData(uid, PowerDeviceVisuals.Powered, powerReceiver.Powered);
        }
    }

    private void OnPowerStateChanged(EntityUid uid, GridShieldGeneratorComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
        {
            if (component.Enabled)
                TurnOn(uid, component);

            _visualizer.SetData(uid, PowerDeviceVisuals.Powered, true);
        }
        else
        {
            TurnOff(uid, component);
            _visualizer.SetData(uid, PowerDeviceVisuals.Powered, false);
        }

        _visualizer.SetData(uid, ContainmentFieldGeneratorVisuals.PowerLight,
            args.Powered ? PowerLevelVisuals.HighPower : PowerLevelVisuals.NoPower);
    }

    private void OnComponentShutdown(EntityUid uid, GridShieldGeneratorComponent component, ComponentShutdown args)
    {
        RemoveShieldFields(uid, component);
    }

    private void OnExamine(EntityUid uid, GridShieldGeneratorComponent component, ExaminedEvent args)
    {
        if (!component.Enabled)
        {
            args.PushText(Loc.GetString("shield-generator-examine-off"));
            return;
        }

        // Check if powered
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver) || !powerReceiver.Powered)
        {
            args.PushText(Loc.GetString("shield-generator-examine-no-power"));
            return;
        }

        args.PushText(Loc.GetString("shield-generator-examine-power-level", ("level", component.ShieldFields.Count)));
    }

    private void OnActivated(EntityUid uid, GridShieldGeneratorComponent component, ActivateInWorldEvent args)
    {
        if (args.User == null)
            return;

        if (component.Enabled)
        {
            TurnOff(uid, component);
            _popupSystem.PopupEntity(Loc.GetString("shield-generator-turned-off"), args.User);
        }
        else
        {
            // Check if we have power
            if (TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver) && powerReceiver.Powered)
            {
                TurnOn(uid, component);
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("shield-generator-examine-no-power"), args.User);
            }
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.User):player} toggled {ToPrettyString(uid):generator} to {(component.Enabled ? "on" : "off")}");
    }

    private void OnAnchorChanged(EntityUid uid, GridShieldGeneratorComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            if (TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver) && powerReceiver.Powered)
                TurnOn(uid, component);
        }
        else
        {
            TurnOff(uid, component);
            RemoveShieldFields(uid, component);
        }
    }

    private void OnReanchorEvent(EntityUid uid, GridShieldGeneratorComponent component, ref ReAnchorEvent args)
    {
        RemoveShieldFields(uid, component);
        // Regenerate shield after reanchoring if we have power
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver) && powerReceiver.Powered)
            TurnOn(uid, component);
    }

    private void OnUnanchorAttempt(EntityUid uid, GridShieldGeneratorComponent component,
        UnanchorAttemptEvent args)
    {
        if (component.IsConnected)
        {
            _popupSystem.PopupEntity(Loc.GetString("shield-generator-turned-off-first"), uid, args.User);
            args.Cancel();
        }
    }

    private void TurnOn(EntityUid uid, GridShieldGeneratorComponent component)
    {
        component.Enabled = true;

        // Only generate shield if we have power
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver) && powerReceiver.Powered)
        {
            ChangePowerVisualizer(uid, component);
            GenerateGridShield(uid, component);
        }
    }

    private void TurnOff(EntityUid uid, GridShieldGeneratorComponent component)
    {
        component.Enabled = false;
        RemoveShieldFields(uid, component);
    }

    private void OnComponentRemoved(EntityUid uid, GridShieldGeneratorComponent component, ref ComponentRemove args)
    {
        RemoveShieldFields(uid, component);
    }

    private void RemoveShieldFields(EntityUid uid, GridShieldGeneratorComponent component)
    {
        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        var gridUid = xform.GridUid;

        foreach (var field in component.ShieldFields.ToArray())
        {
            if (EntityManager.EntityExists(field))
            {
                // Get the tile position before deleting the field
                if (gridUid != null && TryComp<TransformComponent>(field, out var fieldXform) &&
                    TryComp<MapGridComponent>(gridUid.Value, out var grid))
                {
                    // Convert the field's position to grid coordinates
                    var tilePos = _mapSystem.LocalToTile(gridUid.Value, grid, fieldXform.Coordinates);

                    // Set the tile back to space (0 is typically the space tile ID)
                    _mapSystem.SetTile(gridUid.Value, grid, tilePos, new Tile(0));
                }

                QueueDel(field);
            }
        }

        component.ShieldFields.Clear();
        component.IsConnected = false;
        ChangeOnLightVisualizer(uid, component);
    }

    private void GenerateGridShield(EntityUid uid, GridShieldGeneratorComponent component)
    {
        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        // Get the grid this generator is on
        var gridUid = xform.GridUid;
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid.Value, out var grid))
            return;

        // Clear any existing shield fields
        RemoveShieldFields(uid, component);

        // Find the perimeter tiles of the grid
        var perimeterTiles = FindGridPerimeterTiles(gridUid.Value, grid);

        if (perimeterTiles.Count == 0)
        {
            _popupSystem.PopupEntity("Failed to find grid perimeter tiles.", uid);
            return;
        }

        // Create shield fields on the perimeter
        foreach (var tile in perimeterTiles)
        {
            // Get the grid indices for the tile where we'll place the shield field
            Vector2i gridIndices = _mapSystem.LocalToTile(gridUid.Value, grid, tile);

            // Place a custom shield lattice tile underneath the shield field
            // Using the special invincible ShieldLattice tile
            var tileDefinition = _tileDefinitionManager["ShieldLattice"];
            _mapSystem.SetTile(gridUid.Value, grid, gridIndices, new Tile(tileDefinition.TileId));

            var field = Spawn(component.CreatedField, tile);

            // Ensure the field is properly set up
            if (TryComp<TransformComponent>(field, out var fieldXform))
            {
                // Make sure it's on the same grid
                _transformSystem.SetParent(field, fieldXform, gridUid.Value);

                // We don't need to set visibility on the server side
                // The client will handle rendering the sprite
            }

            component.ShieldFields.Add(field);
        }

        if (component.ShieldFields.Count > 0)
        {
            component.IsConnected = true;
            ChangeOnLightVisualizer(uid, component);
            ChangeFieldVisualizer(uid, component);
            _popupSystem.PopupEntity(Loc.GetString("shield-generator-connection-established") + $" ({component.ShieldFields.Count} fields)", uid);
        }
        else
        {
            _popupSystem.PopupEntity("Failed to create shield fields.", uid);
        }
    }

    private HashSet<EntityCoordinates> FindGridPerimeterTiles(EntityUid gridUid, MapGridComponent grid)
    {
        var perimeter = new HashSet<EntityCoordinates>();
        var allTiles = _mapSystem.GetAllTiles(gridUid, grid).ToList();

        // Get grid bounds to help with perimeter detection
        var bounds = grid.LocalAABB;
        var minX = (int)Math.Floor(bounds.Left / grid.TileSize);
        var minY = (int)Math.Floor(bounds.Bottom / grid.TileSize);
        var maxX = (int)Math.Ceiling(bounds.Right / grid.TileSize);
        var maxY = (int)Math.Ceiling(bounds.Top / grid.TileSize);

        // Create a set of all tile indices for faster lookup
        var tileIndices = new HashSet<Vector2i>();
        foreach (var tile in allTiles)
        {
            tileIndices.Add(tile.GridIndices);
        }

        // Also track offset positions to avoid duplicates
        var offsetPositions = new HashSet<Vector2i>();

        // Check each tile to see if it's on the perimeter
        foreach (var tile in allTiles)
        {
            var indices = tile.GridIndices;

            // Check all 8 directions (including diagonals)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    // Skip the center (the tile itself)
                    if (dx == 0 && dy == 0)
                        continue;

                    var neighborPos = new Vector2i(indices.X + dx, indices.Y + dy);

                    // If the neighbor is outside the grid, this is a perimeter tile
                    if (!tileIndices.Contains(neighborPos))
                    {
                        // Calculate the offset position (1 tile outward from the perimeter)
                        // We place the shield field in the direction of the missing neighbor
                        var offsetPos = new Vector2i(indices.X + dx, indices.Y + dy);

                        // Only add if we haven't already added this position
                        if (offsetPositions.Add(offsetPos))
                        {
                            // Place the shield field at the center of the offset tile
                            perimeter.Add(_mapSystem.GridTileToLocal(gridUid, grid, offsetPos));
                        }
                    }
                }
            }
        }

        return perimeter;
    }

    private void ChangePowerVisualizer(EntityUid uid, GridShieldGeneratorComponent component)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver))
        {
            _visualizer.SetData(uid, ContainmentFieldGeneratorVisuals.PowerLight,
                powerReceiver.Powered ? PowerLevelVisuals.HighPower : PowerLevelVisuals.NoPower);
            _light.SetEnabled(uid, powerReceiver.Powered);
        }
    }

    private void ChangeFieldVisualizer(EntityUid uid, GridShieldGeneratorComponent component)
    {
        var fieldCount = component.ShieldFields.Count;

        switch (fieldCount)
        {
            case 0:
                _visualizer.SetData(uid, ContainmentFieldGeneratorVisuals.FieldLight, FieldLevelVisuals.NoLevel);
                break;
            case 1:
                _visualizer.SetData(uid, ContainmentFieldGeneratorVisuals.FieldLight, FieldLevelVisuals.OneField);
                break;
            default:
                _visualizer.SetData(uid, ContainmentFieldGeneratorVisuals.FieldLight, FieldLevelVisuals.MultipleFields);
                break;
        }
    }

    private void ChangeOnLightVisualizer(EntityUid uid, GridShieldGeneratorComponent component)
    {
        _visualizer.SetData(uid, ContainmentFieldGeneratorVisuals.OnLight, component.IsConnected);
    }
}
