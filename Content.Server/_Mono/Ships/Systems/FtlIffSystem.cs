using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared._Mono.Ships.Components;
using Content.Shared.Shuttles.Components;

namespace Content.Server._Mono.Ships.Systems;

/// <summary>
/// Manages IFF flags for ships during FTL travel.
/// </summary>
public sealed class FtlIffSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ShuttleSystem _shuttleSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FTLStartedEvent>(OnFtlStarted);
        SubscribeLocalEvent<FTLCompletedEvent>(OnFtlCompleted);
    }

    private void OnFtlStarted(ref FTLStartedEvent args)
    {
        var gridUid = args.Entity;

        if (!_entityManager.TryGetComponent<IFFComponent>(gridUid, out var iffComp))
            return;

        var tempStorageComp = _entityManager.EnsureComponent<TemporaryFtlIffStorageComponent>(gridUid);
        tempStorageComp.OriginalFlags = iffComp.Flags;
        _entityManager.Dirty(gridUid, tempStorageComp);

        _shuttleSystem.AddIFFFlag(gridUid, IFFFlags.Hide, iffComp);

        Log.Debug($"FTL started for {ToPrettyString(gridUid)}. Saved IFF flags: {tempStorageComp.OriginalFlags}. Set IFF to Hide.");
    }

    private void OnFtlCompleted(ref FTLCompletedEvent args)
    {
        var gridUid = args.Entity;

        if (!_entityManager.TryGetComponent<TemporaryFtlIffStorageComponent>(gridUid, out var tempStorageComp))
            return;

        if (!_entityManager.TryGetComponent<IFFComponent>(gridUid, out var iffComp))
        {
            _entityManager.RemoveComponent<TemporaryFtlIffStorageComponent>(gridUid);
            Log.Warning($"FTL completed for {ToPrettyString(gridUid)}, but IFFComponent was missing. Removed TemporaryFtlIffStorageComponent.");
            return;
        }

        _shuttleSystem.RemoveIFFFlag(gridUid, IFFFlags.Hide, iffComp);
        _shuttleSystem.RemoveIFFFlag(gridUid, IFFFlags.HideLabel, iffComp);

        foreach (IFFFlags flagValue in Enum.GetValues(typeof(IFFFlags)))
        {
            if (flagValue == IFFFlags.None)
                continue;

            // If this specific flag was present in the original set, add it back.
            if ((tempStorageComp.OriginalFlags & flagValue) == flagValue)
            {
                _shuttleSystem.AddIFFFlag(gridUid, flagValue, iffComp);
            }
        }

        _entityManager.RemoveComponent<TemporaryFtlIffStorageComponent>(gridUid);

        Log.Debug($"FTL completed for {ToPrettyString(gridUid)}. Original flags were: {tempStorageComp.OriginalFlags}. Current flags after restoration: {iffComp.Flags}.");
    }
}
