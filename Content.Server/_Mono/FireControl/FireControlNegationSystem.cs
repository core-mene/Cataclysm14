using System.Linq;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.Components.OnTrigger;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Tiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._Mono.FireControl;

/// <summary>
/// System that prevents ships gun from damaging entities or tiles on the same ship (grid). Kill me this is so painful and sloppy. Rewrite in the future.
/// </summary>
public sealed class SameGridDamageNegationSystem : EntitySystem
{
    // Define the delegate for the tile damage prevention
    public delegate bool DamageFloorTileDelegate(TileRef tileRef, ExplosionPrototype prototype);

    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly SharedExplosionSystem _explosions = default!;
    [Dependency] private readonly ExplosionSystem _serverExplosions = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    private ISawmill _sawmill = default!;
    private readonly Dictionary<EntityUid, EntityUid?> _projectileSourceGrids = new();
    private readonly Dictionary<EntityUid, int> _sameGridHitCount = new();
    private readonly Dictionary<EntityUid, EntityUid?> _explosionsToFilter = new();
    private readonly HashSet<EntityUid> _explosionExemptEntities = new();
    private readonly Dictionary<EntityUid, TimeSpan> _tileProtectedGrids = new();
    private readonly Dictionary<(EntityUid GridId, Vector2i Position), Tile> _originalTiles = new();
    private readonly TimeSpan _protectionDuration = TimeSpan.FromSeconds(10);
    private bool _explosionSystemPatched = false;
    private readonly List<(EntityUid GridUid, Vector2i Position, Tile OriginalTile)> _tilesToRestore = new();

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("sameGridDamage");
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(OnProjectileHit, before: new[] { typeof(DamageableSystem) });
        SubscribeLocalEvent<ProjectileComponent, ComponentInit>(OnProjectileInit);
        SubscribeLocalEvent<ProjectileComponent, ComponentStartup>(OnProjectileStartup);
        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged, before: new[] { typeof(DamageableSystem) });
        SubscribeLocalEvent<FireControlNegationComponent, GetExplosionResistanceEvent>(OnGetExplosionResistance);
        SubscribeLocalEvent<BeforeExplodeEvent>(OnBeforeExplode, before: new[] { typeof(ExplosionSystem) });
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged, after: new[] { typeof(ExplosionSystem) });
        SubscribeLocalEvent<FloorTileAttemptEvent>(OnFloorTileAttempt);
        PatchExplosionSystem();
    }

    private void PatchExplosionSystem()
    {
        if (_serverExplosions == null)
        {
            _sawmill.Error("ExplosionSystem not available for patching");
            return;
        }

        // Try patching the DamageFloorTile method via reflection
        try
        {
            // First try: direct delegate approach
            var eventField = typeof(ExplosionSystem).GetField("DamageFloorTileEvent",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (eventField != null)
            {
                var eventDelegate = new DamageFloorTileDelegate(OnDamageFloorTile);
                eventField.SetValue(_serverExplosions,
                    Delegate.Combine(eventField.GetValue(_serverExplosions) as Delegate, eventDelegate));

                _explosionSystemPatched = true;
                return;
            }

            // Second try: look for the method directly to check for different naming
            var method = typeof(ExplosionSystem).GetMethod("DamageFloorTile",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (method != null)
            {
                _explosionSystemPatched = true; // We'll rely on TileChangedEvent
                return;
            }

            // If all else fails, log a warning
            _sawmill.Warning("Could not find DamageFloorTileEvent or DamageFloorTile method in ExplosionSystem. " +
                            "Falling back to TileChangedEvent for tile protection.");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to patch ExplosionSystem: {ex}");
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();

        // Only try to unhook if we successfully patched
        if (_explosionSystemPatched && _serverExplosions != null)
        {
            try
            {
                var eventField = typeof(ExplosionSystem).GetField("DamageFloorTileEvent",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (eventField != null)
                {
                    var eventDelegate = new DamageFloorTileDelegate(OnDamageFloorTile);
                    eventField.SetValue(_serverExplosions,
                        Delegate.Remove(eventField.GetValue(_serverExplosions) as Delegate, eventDelegate));
                }
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to unhook from ExplosionSystem.DamageFloorTileEvent: {ex}");
            }
        }
    }

    /// <summary>
    /// Checks if a grid should be protected from tile damage
    /// </summary>
    private bool IsGridProtectedFromTileDamage(EntityUid gridUid)
    {
        if (_tileProtectedGrids.TryGetValue(gridUid, out var expiryTime))
        {
            // Check if protection has expired
            if (_gameTiming.CurTime < expiryTime)
                return true;

            // Clean up expired protection
            _tileProtectedGrids.Remove(gridUid);
        }

        return false;
    }

    /// <summary>
    /// Event handler that prevents tile damage on protected grids
    /// </summary>
    private bool OnDamageFloorTile(TileRef tileRef, ExplosionPrototype prototype)
    {
        // If this tile's grid is protected, prevent the damage
        if (IsGridProtectedFromTileDamage(tileRef.GridUid))
        {
            return true; // Return true to indicate we've handled this and the explosion system should skip default processing
        }

        // Return false to allow normal tile damage processing
        return false;
    }

    /// <summary>
    /// Additional method to catch any tile changes and revert them if needed
    /// </summary>
    private void OnTileChanged(ref TileChangedEvent args)
    {
        // Early check if we need to care about this grid
        if (!IsGridProtectedFromTileDamage(args.Entity))
            return;

        // If this is a grid we want to protect, check what changed
        var oldWasEmpty = args.OldTile.IsEmpty;
        var newIsEmpty = args.NewTile.Tile.IsEmpty;

        // If the tile was damaged (changed from not empty to empty, or changed type)
        if ((oldWasEmpty != newIsEmpty && newIsEmpty) ||
            (!oldWasEmpty && !newIsEmpty && args.OldTile.TypeId != args.NewTile.Tile.TypeId))
        {
            // Store the original tile if we haven't seen it yet
            var key = (args.Entity, args.NewTile.GridIndices);
            if (!_originalTiles.ContainsKey(key))
            {
                _originalTiles[key] = args.OldTile;
            }

            // Look up the original tile we stored
            if (_originalTiles.TryGetValue(key, out var originalTile))
            {
                // Add to restore queue, using a thread-safe approach with lock to prevent simultaneous modification
                lock (_tilesToRestore)
                {
                    _tilesToRestore.Add((args.Entity, args.NewTile.GridIndices, originalTile));
                }
            }
        }
    }

    /// <summary>
    /// Intercept floor tile attempts to prevent damage to tiles on protected grids
    /// </summary>
    private void OnFloorTileAttempt(ref FloorTileAttemptEvent args)
    {
        // Check all protected grids since we don't know which grid this event is for
        if (_tileProtectedGrids.Count > 0)
        {
            args = args with { Cancelled = true };
        }
    }

    /// <summary>
    /// Check if a grid should be protected from tile damage
    /// </summary>
    private void ProtectGridTiles(EntityUid? gridUid)
    {
        if (gridUid == null || !HasComp<MapGridComponent>(gridUid.Value))
            return;

        // Add the grid to our protected list with an expiry time
        var expiryTime = _gameTiming.CurTime + _protectionDuration;
        _tileProtectedGrids[gridUid.Value] = expiryTime;
    }

    /// <summary>
    /// Intercept damage from explosions for exempt entities
    /// </summary>
    private void OnBeforeExplode(ref BeforeExplodeEvent args)
    {
        // Check if this is an entity we want to exempt from explosions
        foreach (var entity in _explosionExemptEntities)
        {
            // Completely cancel all explosion damage for exempt entities
            args.DamageCoefficient = 0;
            break;
        }
    }

    /// <summary>
    /// Ensure entities with FireControlNegationComponent are completely immune to explosions
    /// </summary>
    private void OnGetExplosionResistance(EntityUid uid, FireControlNegationComponent component, ref GetExplosionResistanceEvent args)
    {
        // Make the entity completely immune to explosion damage
        args.DamageCoefficient = 0;
    }

    /// <summary>
    /// Additional method to check projectiles as they start up
    /// </summary>
    private void OnProjectileStartup(EntityUid uid, ProjectileComponent component, ComponentStartup args)
    {
        // If we already tracked this projectile, skip
        if (_projectileSourceGrids.ContainsKey(uid))
            return;

        TryRegisterProjectile(uid, component);
    }

    /// <summary>
    /// Checks if a projectile was fired from a gun controlled by a gunnery console
    /// </summary>
    private void OnProjectileInit(EntityUid uid, ProjectileComponent component, ComponentInit args)
    {
        TryRegisterProjectile(uid, component);
    }

    /// <summary>
    /// Try to register a projectile with its source grid
    /// </summary>
    private void TryRegisterProjectile(EntityUid uid, ProjectileComponent component)
    {
        // Skip if the projectile has no weapon reference
        if (component.Weapon == null)
        {
            // Still check shooter in case weapon isn't set
            if (component.Shooter != null)
            {
                var shooterUid = component.Shooter.Value;
                // Check if shooter has a weapon that's controllable
                var query = EntityQueryEnumerator<FireControllableComponent, TransformComponent>();
                while (query.MoveNext(out var weapon, out var controllable, out var xform))
                {
                    if (controllable.ControllingServer == null)
                        continue;

                    // Check if this projectile was fired by this weapon
                    if (TryComp<GunComponent>(weapon, out var gun) &&
                        (shooterUid == weapon || weapon == shooterUid))
                    {
                        _projectileSourceGrids[uid] = xform.GridUid;
                        _sameGridHitCount[uid] = 0; // Initialize hit counter
                        return;
                    }
                }
            }
            return;
        }

        var weaponUid = component.Weapon.Value;

        // IMPORTANT: Only track projectiles from gunnery consoles (FireControllable)
        // Check if the weapon is controlled by a gunnery console
        if (!TryComp<FireControllableComponent>(weaponUid, out var fireControllable) ||
            fireControllable.ControllingServer == null)
            return;

        // Get the grid UID of the firing weapon
        var gunTransform = Transform(weaponUid);
        var gridUid = gunTransform.GridUid;

        // Store the source grid for the projectile
        _projectileSourceGrids[uid] = gridUid;
        _sameGridHitCount[uid] = 0; // Initialize hit counter
    }

    /// <summary>
    /// Completely and aggressively prevents explosions from a projectile
    /// </summary>
    private void PreventExplosions(EntityUid uid, EntityUid? targetGridUid = null)
    {
        // Ensure this is only for FireControllable projectiles
        if (!IsFromFireControllableWeapon(uid))
            return;

        if (HasComp<ExplosiveComponent>(uid) || HasComp<ExplodeOnTriggerComponent>(uid))
        {
            // Track this explosion for filtering
            if (targetGridUid != null)
            {
                _explosionsToFilter[uid] = targetGridUid;

                // IMPORTANT: Protect the grid's tiles from the explosion
                ProtectGridTiles(targetGridUid);
            }

            // For any entity on this grid, add it to the exempt list
            if (targetGridUid != null)
            {
                var entitiesOnGrid = new List<EntityUid>();
                var query = EntityQueryEnumerator<TransformComponent>();
                while (query.MoveNext(out var entity, out var transform))
                {
                    if (transform.GridUid == targetGridUid)
                    {
                        // Add an exemption component to all entities on this grid
                        if (!HasComp<FireControlNegationComponent>(entity))
                        {
                            var gridExemptComp = EnsureComp<FireControlNegationComponent>(entity);
                            gridExemptComp.ProjectileSource = uid;
                            gridExemptComp.SourceGrid = targetGridUid;
                            _explosionExemptEntities.Add(entity);
                        }
                    }
                }
            }
            // 1. Add our custom component that will catch any explosion damage attempts
            var projectileExemptComp = EnsureComp<FireControlNegationComponent>(uid);
            projectileExemptComp.ProjectileSource = uid;
            projectileExemptComp.SourceGrid = targetGridUid;
            _explosionExemptEntities.Add(uid);

            // 2. Additional safety: clear projectile damage to prevent secondary effects
            if (TryComp<ProjectileComponent>(uid, out var projectile))
            {
                projectile.Damage = new();
            }

            // 3. Queue immediate deletion of the projectile to be absolutely sure
            // This is the most aggressive approach but guarantees no explosion
            QueueDel(uid);
        }
    }

    /// <summary>
    /// Helper method to check if a projectile was fired from a FireControllable weapon
    /// </summary>
    private bool IsFromFireControllableWeapon(EntityUid projectileUid)
    {
        if (TryComp<ProjectileComponent>(projectileUid, out var projectile))
        {
            if (projectile.Weapon != null)
            {
                var weaponUid = projectile.Weapon.Value;
                return TryComp<FireControllableComponent>(weaponUid, out var fireControllable) &&
                       fireControllable.ControllingServer != null;
            }

            if (projectile.Shooter != null)
            {
                var shooterUid = projectile.Shooter.Value;
                var query = EntityQueryEnumerator<FireControllableComponent, TransformComponent>();
                while (query.MoveNext(out var weapon, out var controllable, out var xform))
                {
                    if (controllable.ControllingServer == null)
                        continue;

                    if (TryComp<GunComponent>(weapon, out var gun) &&
                        (shooterUid == weapon || weapon == shooterUid))
                    {
                        return true;
                    }
                }
            }
        }

        // Default to not being from a FireControllable weapon
        return false;
    }

    /// <summary>
    /// Checks if a projectile is hitting an entity on the same grid it was fired from
    /// </summary>
    private void OnProjectileHit(EntityUid uid, ProjectileComponent component, ref ProjectileHitEvent args)
    {
        // Only apply negation for registered projectiles (those from FireControllable weapons)
        if (!_projectileSourceGrids.TryGetValue(uid, out var sourceGridUid) || sourceGridUid == null)
        {
            // Try to handle case where we didn't catch the projectile during init
            if (component.Weapon != null)
            {
                var weaponUid = component.Weapon.Value;

                // Only register if it's a fire controllable weapon
                if (TryComp<FireControllableComponent>(weaponUid, out var fireControllable) &&
                    fireControllable.ControllingServer != null)
                {
                    var gunTransform = Transform(weaponUid);
                    sourceGridUid = gunTransform.GridUid;
                    _projectileSourceGrids[uid] = sourceGridUid;
                    _sameGridHitCount[uid] = 0; // Initialize hit counter
                }
                else
                {
                    // Not from a fire controllable weapon, allow normal damage and explosion
                    return;
                }
            }
            else if (component.Shooter != null)
            {
                // Check if shooter has a weapon that's controllable
                var shooterUid = component.Shooter.Value;
                var hasFireControllable = false;

                var query = EntityQueryEnumerator<FireControllableComponent, TransformComponent>();
                while (query.MoveNext(out var weapon, out var controllable, out var xform))
                {
                    if (controllable.ControllingServer == null)
                        continue;

                    // Check if this projectile was fired by this weapon
                    if (TryComp<GunComponent>(weapon, out var gun) &&
                        (shooterUid == weapon || weapon == shooterUid))
                    {
                        sourceGridUid = xform.GridUid;
                        _projectileSourceGrids[uid] = sourceGridUid;
                        _sameGridHitCount[uid] = 0; // Initialize hit counter
                        hasFireControllable = true;
                        break;
                    }
                }

                if (!hasFireControllable)
                {
                    // Not from a fire controllable weapon, allow normal damage and explosion
                    return;
                }
            }

            if (sourceGridUid == null)
                return;
        }

        // Get the grid of the target
        var targetTransform = Transform(args.Target);
        var targetGridUid = targetTransform.GridUid;

        // If target is on the same grid as the source, negate damage
        if (targetGridUid != null && targetGridUid == sourceGridUid)
        {
            // Override damage to zero
            args.Damage = new();

            // Since we can't set Handled property (doesn't exist), we'll use preventExplosions
            // to ensure no additional damage occurs
            PreventExplosions(uid, targetGridUid);

            // Increment the hit counter for this projectile
            if (!_sameGridHitCount.ContainsKey(uid))
                _sameGridHitCount[uid] = 0;

            _sameGridHitCount[uid]++;

            // If the projectile has hit more than one entity on the same grid, delete it
            if (_sameGridHitCount[uid] > 1)
            {
                // Queue deletion for the end of this tick
                QueueDel(uid);
            }

            // Add explosion exemption for the target as well
            if (!HasComp<FireControlNegationComponent>(args.Target))
            {
                var targetExemptComp = EnsureComp<FireControlNegationComponent>(args.Target);
                targetExemptComp.ProjectileSource = uid;
                targetExemptComp.SourceGrid = targetGridUid;
                _explosionExemptEntities.Add(args.Target);
            }

            // Add tile protection for the grid
            ProtectGridTiles(targetGridUid);
        }
    }

    /// <summary>
    /// Last line of defense - catch any damage from projectiles on the same grid
    /// </summary>
    private void OnDamageChanged(EntityUid uid, DamageableComponent component, ref DamageChangedEvent args)
    {
        if (args.Origin == null || !args.DamageIncreased)
            return;

        // Skip if we don't have a valid origin
        if (!EntityManager.EntityExists(args.Origin.Value))
            return;

        // Check if damage source is a projectile
        if (!TryComp<ProjectileComponent>(args.Origin.Value, out var projectile))
            return;

        // Get projectile's grid
        EntityUid? sourceGridUid = null;

        // Check if we're tracking this projectile (only tracks fire-controllable weapons)
        if (_projectileSourceGrids.TryGetValue(args.Origin.Value, out var trackedGrid))
        {
            sourceGridUid = trackedGrid;
        }
        // If not, try to determine if it's from a fire-controllable weapon
        else if (projectile.Weapon != null)
        {
            var weaponUid = projectile.Weapon.Value;

            // IMPORTANT: Only apply for fire controllable weapons
            if (TryComp<FireControllableComponent>(weaponUid, out var fireControllable) &&
                fireControllable.ControllingServer != null)
            {
                var gunTransform = Transform(weaponUid);
                sourceGridUid = gunTransform.GridUid;
            }
            else
            {
                // Not from a fire controllable weapon, allow normal damage and explosions
                return;
            }
        }
        else if (projectile.Shooter != null)
        {
            // Check if shooter has a weapon that's controllable
            var shooterUid = projectile.Shooter.Value;
            var hasFireControllable = false;

            var query = EntityQueryEnumerator<FireControllableComponent, TransformComponent>();
            while (query.MoveNext(out var weapon, out var controllable, out var xform))
            {
                if (controllable.ControllingServer == null)
                    continue;

                // Check if this projectile was fired by this weapon
                if (TryComp<GunComponent>(weapon, out var gun) &&
                    (shooterUid == weapon || weapon == shooterUid))
                {
                    sourceGridUid = xform.GridUid;
                    hasFireControllable = true;
                    break;
                }
            }

            if (!hasFireControllable)
            {
                // Not from a fire controllable weapon, allow normal damage and explosions
                return;
            }
        }
        else
        {
            // No weapon reference, allow normal damage and explosions
            return;
        }

        if (sourceGridUid == null)
            return;

        // Check if target is on the same grid
        var targetTransform = Transform(uid);
        var targetGridUid = targetTransform.GridUid;

        if (targetGridUid != null && targetGridUid == sourceGridUid)
        {
            // For DamageChangedEvent we can't directly set the Damage/Handled properties
            // Instead we'll use the DamageableSystem to set the damage back to zero
            _damage.SetDamage(uid, component, new DamageSpecifier());

            // Use our more aggressive explosion prevention
            var projectileUid = args.Origin.Value;
            PreventExplosions(projectileUid, targetGridUid);

            // Also track the hit in our hit counter
            if (!_sameGridHitCount.ContainsKey(projectileUid))
                _sameGridHitCount[projectileUid] = 0;

            _sameGridHitCount[projectileUid]++;

            // If the projectile has hit more than one entity on the same grid, delete it
            if (_sameGridHitCount[projectileUid] > 1)
            {
                QueueDel(projectileUid);
            }

            // Add explosion exemption for the target as well
            if (!HasComp<FireControlNegationComponent>(uid))
            {
                var damageTargetExemptComp = EnsureComp<FireControlNegationComponent>(uid);
                damageTargetExemptComp.ProjectileSource = projectileUid;
                damageTargetExemptComp.SourceGrid = targetGridUid;
                _explosionExemptEntities.Add(uid);
            }

            // Add tile protection for the grid
            ProtectGridTiles(targetGridUid);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Process any pending tile restorations first
        if (_tilesToRestore.Count > 0)
        {
            // Create a safe copy of the list to prevent collection modification exception
            var tilesToRestoreCopy = _tilesToRestore.ToList();
            // Clear the original list before we start processing
            _tilesToRestore.Clear();

            foreach (var (gridUid, position, originalTile) in tilesToRestoreCopy)
            {
                if (IsGridProtectedFromTileDamage(gridUid) && TryComp<MapGridComponent>(gridUid, out var grid))
                {
                    // Safely restore the tile
                    try
                    {
                        // Get the current tile to see if it's still damaged
                        var currentTile = _mapSystem.GetTileRef(gridUid, grid, position);
                        if (currentTile.Tile.TypeId != originalTile.TypeId)
                        {
                            _mapSystem.SetTile(gridUid, grid, position, originalTile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _sawmill.Error($"Error restoring tile on grid {ToPrettyString(gridUid)} at position {position}: {ex}");
                    }
                }
            }
        }

        // Cleanup stale entries (projectiles that no longer exist)
        var toRemove = _projectileSourceGrids.Keys.Where(uid => !EntityManager.EntityExists(uid)).ToList();
        foreach (var uid in toRemove)
        {
            _projectileSourceGrids.Remove(uid);
            _sameGridHitCount.Remove(uid); // Also remove from hit counter dictionary
            _explosionsToFilter.Remove(uid); // Also clean up explosion filters
        }

        // Clean up explosion exemptions for entities that are no longer valid
        var exemptToRemove = _explosionExemptEntities.Where(uid => !EntityManager.EntityExists(uid)).ToList();
        foreach (var uid in exemptToRemove)
        {
            _explosionExemptEntities.Remove(uid);
        }

        // Clean up expired grid protections
        var currentTime = _gameTiming.CurTime;
        var expiredGrids = _tileProtectedGrids.Where(kvp => kvp.Value <= currentTime).Select(kvp => kvp.Key).ToList();
        foreach (var grid in expiredGrids)
        {
            _tileProtectedGrids.Remove(grid);
        }

        // After grid protection expires, also clear the original tiles cache to prevent memory leaks
        if (expiredGrids.Count > 0)
        {
            var keysToRemove = _originalTiles.Keys.Where(k => expiredGrids.Contains(k.GridId)).ToList();
            foreach (var key in keysToRemove)
            {
                _originalTiles.Remove(key);
            }
        }
    }
}
