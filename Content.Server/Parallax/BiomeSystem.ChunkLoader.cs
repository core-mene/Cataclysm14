// SPDX-FileCopyrightText: 2023 Cheackraze
// SPDX-FileCopyrightText: 2023 Checkraze
// SPDX-FileCopyrightText: 2024 Dvir
// SPDX-FileCopyrightText: 2024 Ed
// SPDX-FileCopyrightText: 2024 Leon Friedrich
// SPDX-FileCopyrightText: 2024 MilenVolf
// SPDX-FileCopyrightText: 2024 Nemanja
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers
// SPDX-FileCopyrightText: 2024 Plykiya
// SPDX-FileCopyrightText: 2024 TemporalOroboros
// SPDX-FileCopyrightText: 2024 deltanedas
// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 Redrover1760
// SPDX-FileCopyrightText: 2025 bitcrushing
// SPDX-FileCopyrightText: 2025 metalgearsloth
// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mono - Refactored into smaller subsystems
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Parallax;

public sealed partial class BiomeSystem
{
    private readonly List<(Vector2i, Tile)> _chunkLoaderTiles = new();

    private void InitializeChunkLoader()
    {
        // ChunkLoader methods are now part of this partial class
    }

    /// <summary>
    /// Loads a particular queued chunk for a biome.
    /// </summary>
    private void LoadChunk(
        BiomeComponent component,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i chunk,
        int seed)
    {
        component.ModifiedTiles.TryGetValue(chunk, out var modified);
        modified ??= _tilePool.Get();
        _chunkLoaderTiles.Clear();

        LoadTiles(component, gridUid, grid, chunk, seed, modified);
        LoadEntities(component, gridUid, grid, chunk, seed, modified);
        LoadDecals(component, gridUid, grid, chunk, seed, modified);

        FinalizeChunk(component, chunk, modified);
    }

    private void LoadTiles(
        BiomeComponent component,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i chunk,
        int seed,
        HashSet<Vector2i> modified)
    {
        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (modified.Contains(indices))
                    continue;

                if (_mapSystem.TryGetTileRef(gridUid, grid, indices, out var tileRef) && !tileRef.Tile.IsEmpty)
                    continue;

                if (!TryGetBiomeTile(indices, component.Layers, seed, (gridUid, grid), out var biomeTile))
                    continue;

                _chunkLoaderTiles.Add((indices, biomeTile.Value));
            }
        }

        _mapSystem.SetTiles(gridUid, grid, _chunkLoaderTiles);
        _chunkLoaderTiles.Clear();
    }

    private void LoadEntities(
        BiomeComponent component,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i chunk,
        int seed,
        HashSet<Vector2i> modified)
    {
        var loadedEntities = new Dictionary<EntityUid, Vector2i>();
        component.LoadedEntities.Add(chunk, loadedEntities);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (modified.Contains(indices))
                    continue;

                var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, indices);

                if (anchored.MoveNext(out _) || !TryGetEntity(indices, component, (gridUid, grid), out var entPrototype))
                    continue;

                var ent = Spawn(entPrototype, _mapSystem.GridTileToLocal(gridUid, grid, indices));

                if (_xformQuery.TryGetComponent(ent, out var xform) && !xform.Anchored)
                {
                    _transform.AnchorEntity((ent, xform), (gridUid, grid), indices);
                }

                loadedEntities.Add(ent, indices);
            }
        }
    }

    private void LoadDecals(
        BiomeComponent component,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i chunk,
        int seed,
        HashSet<Vector2i> modified)
    {
        var loadedDecals = new Dictionary<uint, Vector2i>();
        component.LoadedDecals.Add(chunk, loadedDecals);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (modified.Contains(indices))
                    continue;

                var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, indices);

                if (anchored.MoveNext(out _) || !TryGetDecals(indices, component.Layers, seed, (gridUid, grid), out var decals))
                    continue;

                foreach (var decal in decals)
                {
                    if (!_decals.TryAddDecal(decal.ID, new EntityCoordinates(gridUid, decal.Position), out var dec))
                        continue;

                    loadedDecals.Add(dec, indices);
                }
            }
        }
    }

    private void FinalizeChunk(BiomeComponent component, Vector2i chunk, HashSet<Vector2i> modified)
    {
        if (modified.Count == 0)
        {
            _tilePool.Return(modified);
            component.ModifiedTiles.Remove(chunk);
        }
        else
        {
            component.ModifiedTiles[chunk] = modified;
        }
    }

    /// <summary>
    /// Unloads a specific biome chunk.
    /// </summary>
    private void UnloadChunk(BiomeComponent component, EntityUid gridUid, MapGridComponent grid, Vector2i chunk, int seed, List<(Vector2i, Tile)> tiles)
    {
        component.ModifiedTiles.TryGetValue(chunk, out var modified);
        modified ??= new HashSet<Vector2i>();

        UnloadDecals(component, gridUid, chunk, modified);
        UnloadEntities(component, gridUid, grid, chunk, modified);
        UnloadTiles(component, gridUid, grid, chunk, seed, modified, tiles);

        component.LoadedChunks.Remove(chunk);

        if (modified.Count == 0)
        {
            component.ModifiedTiles.Remove(chunk);
        }
        else
        {
            component.ModifiedTiles[chunk] = modified;
        }
    }

    private void UnloadDecals(BiomeComponent component, EntityUid gridUid, Vector2i chunk, HashSet<Vector2i> modified)
    {
        if (!component.LoadedDecals.TryGetValue(chunk, out var loadedDecals))
            return;

        foreach (var (dec, indices) in loadedDecals)
        {
            if (!_decals.RemoveDecal(gridUid, dec))
            {
                modified.Add(indices);
            }
        }

        component.LoadedDecals.Remove(chunk);
    }

    private void UnloadEntities(BiomeComponent component, EntityUid gridUid, MapGridComponent grid, Vector2i chunk, HashSet<Vector2i> modified)
    {
        if (!component.LoadedEntities.TryGetValue(chunk, out var loadedEntities))
            return;

        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var (ent, tile) in loadedEntities)
        {
            if (Deleted(ent) || !xformQuery.TryGetComponent(ent, out var xform))
            {
                modified.Add(tile);
                continue;
            }

            var entTile = _mapSystem.LocalToTile(gridUid, grid, xform.Coordinates);

            if (!xform.Anchored || entTile != tile)
            {
                modified.Add(tile);
                continue;
            }

            if (!EntityManager.IsDefault(ent))
            {
                modified.Add(tile);
                continue;
            }

            Del(ent);
        }

        component.LoadedEntities.Remove(chunk);
    }

    private void UnloadTiles(BiomeComponent component, EntityUid gridUid, MapGridComponent grid, Vector2i chunk, int seed, HashSet<Vector2i> modified, List<(Vector2i, Tile)> tiles)
    {
        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (modified.Contains(indices))
                    continue;

                var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, indices);

                if (anchored.MoveNext(out _))
                {
                    modified.Add(indices);
                    continue;
                }

                if (!TryGetBiomeTile(indices, component.Layers, seed, null, out var biomeTile) ||
                    _mapSystem.TryGetTileRef(gridUid, grid, indices, out var tileRef) && tileRef.Tile != biomeTile.Value)
                {
                    modified.Add(indices);
                    continue;
                }

                tiles.Add((indices, Tile.Empty));
            }
        }

        _mapSystem.SetTiles(gridUid, grid, tiles);
        tiles.Clear();
    }

    /// <summary>
    /// Handles all of the queued chunk unloads for a particular biome.
    /// </summary>
    private void UnloadChunks(BiomeComponent component, EntityUid gridUid, MapGridComponent grid, int seed)
    {
        var active = _activeChunks[component];
        List<(Vector2i, Tile)>? tiles = null;

        foreach (var chunk in component.LoadedChunks)
        {
            if (active.Contains(chunk) || !component.LoadedChunks.Remove(chunk))
                continue;

            tiles ??= new List<(Vector2i, Tile)>(ChunkSize * ChunkSize);
            UnloadChunk(component, gridUid, grid, chunk, seed, tiles);
        }
    }
}
