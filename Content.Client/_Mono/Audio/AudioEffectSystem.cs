// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
//
// SPDX-License-Identifier: MPL-2.0

// some parts taken and modified from https://github.com/TornadoTechnology/finster/blob/1af5daf6270477a512ee9d515371311443e97878/Content.Shared/_Finster/Audio/SharedAudioEffectsSystem.cs#L13 , credit to docnite
// they're under WTFPL so its quite allowed

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Content.Shared.GameTicking;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Client._Mono.Audio;

/// <summary>
///     Handler for client-side audio effects.
/// </summary>
public sealed class AudioEffectSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    /// <summary>
    ///     Whether creating new auxiliaries is safe.
    ///         This is done because integration tests
    ///         apparently can't handle them.
    /// 
    ///     Null means this value wasn't determined yet.
    /// 
    ///     Any not-null value here is the final result,
    ///         and no more attempts to determine this
    ///         will be made afterwards.
    /// </summary>
    private bool? _auxiliariesSafe = null;

    private static readonly Dictionary<ProtoId<AudioPresetPrototype>, (EntityUid AuxiliaryUid, EntityUid EffectUid)> CachedEffects = new();

    /// <summary>
    ///     An auxiliary with no effect; for removing effects.
    /// </summary>
    // TODO: remove this when an rt method to actually remove effects gets added
    private EntityUid _cachedBlankAuxiliaryUid;

    public override void Initialize()
    {
        base.Initialize();

        // You can't keep references to this past round-end so it must be cleaned up.
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(_ => Cleanup()); // its not raised on client
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        Cleanup();
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<AudioPresetPrototype>())
            return;

        // get rid of all old cached entities, and replace them with new ones
        var oldPresets = new List<ProtoId<AudioPresetPrototype>>();
        foreach (var cache in CachedEffects)
        {
            oldPresets.Add(cache.Key);

            TryQueueDel(cache.Value.AuxiliaryUid);
            TryQueueDel(cache.Value.EffectUid);
        }
        CachedEffects.Clear();

        foreach (var oldPreset in oldPresets)
        {
            if (!ResolveCachedEffect(oldPreset, out var cachedAuxiliaryUid, out var cachedEffectUid))
                continue;

            CachedEffects[oldPreset] = (cachedAuxiliaryUid.Value, cachedEffectUid.Value);
        }
    }

    private void Cleanup()
    {
        foreach (var cache in CachedEffects)
        {
            TryQueueDel(cache.Value.AuxiliaryUid);
            TryQueueDel(cache.Value.EffectUid);
        }
        CachedEffects.Clear();

        if (_cachedBlankAuxiliaryUid.IsValid())
            TryQueueDel(_cachedBlankAuxiliaryUid);

        _cachedBlankAuxiliaryUid = EntityUid.Invalid;
    }

    /// <summary>
    ///     Tries to resolve a cached audio auxiliary entity corresponding to the prototype to apply
    ///         to the given entity.
    /// </summary>
    public bool TryAddEffect(in Entity<AudioComponent> entity, in ProtoId<AudioPresetPrototype> preset)
    {
        if (_auxiliariesSafe == false)
            return false;

        if (!ResolveCachedEffect(preset, out var auxiliaryUid, out _))
            return false;

        _audioSystem.SetAuxiliary(entity, entity.Comp, auxiliaryUid);
        return true;
    }

    /// <summary>
    ///     Tries to remove effects from the given audio. Returns whether the attempt was successful,
    ///         or no auxiliary is applied to the audio.
    /// </summary>
    public bool TryRemoveEffect(in Entity<AudioComponent> entity)
    {
        if (_auxiliariesSafe == false)
            return false;

        if (entity.Comp.Auxiliary is not { } existingAuxiliaryUid ||
            !existingAuxiliaryUid.IsValid())
            return true;

        // resolve the cached auxiliary
        if (!_cachedBlankAuxiliaryUid.IsValid())
            _cachedBlankAuxiliaryUid = _audioSystem.CreateAuxiliary().Entity;

        _audioSystem.SetAuxiliary(entity, entity.Comp, _cachedBlankAuxiliaryUid);
        return true;
    }

    /// <summary>
    ///     Tries to resolve an audio auxiliary and effect entity, creating and caching one if one doesn't already exist,
    ///         for a prototype. Do not modify it in any way.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ResolveCachedEffect(in ProtoId<AudioPresetPrototype> preset, [NotNullWhen(true)] out EntityUid? auxiliaryUid, [NotNullWhen(true)] out EntityUid? effectUid)
    {
        EntityUid? maybeAuxiliaryUid = null;
        EntityUid? maybeEffectUid = null;

        if (CachedEffects.TryGetValue(preset, out var cached) ||
            TryCacheEffect(preset, out maybeAuxiliaryUid, out maybeEffectUid))
        {
            auxiliaryUid = maybeAuxiliaryUid ?? cached.AuxiliaryUid;
            effectUid = maybeEffectUid ?? cached.EffectUid;
            return true;
        }

        auxiliaryUid = null;
        effectUid = null;
        return false;
    }

    /// <summary>
    ///     Tries to initialise and cache effect and auxiliary entities corresponding to a prototype,
    ///         in the system's internal cache.
    /// 
    ///     Does nothing if the entity already exists in the cache.
    /// </summary>
    /// <returns>Whether the entity was successfully initialised, and it did not previously exist in the cache.</returns>
    public bool TryCacheEffect(in ProtoId<AudioPresetPrototype> preset, [NotNullWhen(true)] out EntityUid? auxiliaryUid, [NotNullWhen(true)] out EntityUid? effectUid)
    {
        effectUid = null;
        auxiliaryUid = null;

        if (!_prototypeManager.TryIndex(preset, out var presetPrototype))
            return false;

        var effectEntity = _audioSystem.CreateEffect();
        _audioSystem.SetEffectPreset(effectEntity.Entity, effectEntity.Component, presetPrototype);

        (EntityUid Entity, AudioAuxiliaryComponent Component)? maybeAuxiliaryEntity = null;
        if (_auxiliariesSafe == null)
        {
            try
            {
                maybeAuxiliaryEntity = _audioSystem.CreateAuxiliary();
                _auxiliariesSafe = true;
            }
            catch (Exception ex)
            {
                Log.Info($"Determined audio auxiliaries are unsafe in this run! If this is not an integration test, report this immediately. Exception: {ex}");
                _auxiliariesSafe = false;

                return false;
            }
        }
        else if (!_auxiliariesSafe.Value)
            return false;

        // i cant `??=` it
        if (maybeAuxiliaryEntity is not { } auxiliaryEntity)
            auxiliaryEntity = _audioSystem.CreateAuxiliary();

        _audioSystem.SetEffect(auxiliaryEntity.Entity, auxiliaryEntity.Component, effectEntity.Entity);

        if (!Exists(auxiliaryEntity.Entity))
            return false;

        effectUid = effectEntity.Entity;
        auxiliaryUid = auxiliaryEntity.Entity;

        return CachedEffects.TryAdd(preset, (auxiliaryEntity.Entity, effectEntity.Entity));
    }
}
