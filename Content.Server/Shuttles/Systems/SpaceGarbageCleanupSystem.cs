using Content.Server.Shuttles.Components;
using Robust.Shared.Timing;

namespace Content.Server.Shuttles.Systems;

/// <summary>
/// This system cleans up entities with SpaceGarbage component after a specified time delay.
/// </summary>
public sealed class SpaceGarbageCleanupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    // The delay before cleaning up garbage (in seconds)
    private const float CleanupDelay = 300.0f;

    // Dictionary to track garbage scheduled for deletion
    private readonly Dictionary<EntityUid, TimeSpan> _pendingCleanup = new();

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to component events
        SubscribeLocalEvent<SpaceGarbageComponent, ComponentStartup>(OnGarbageStartup);
        SubscribeLocalEvent<SpaceGarbageComponent, EntParentChangedMessage>(OnParentChanged);
    }

    private void OnGarbageStartup(EntityUid uid, SpaceGarbageComponent component, ComponentStartup args)
    {
        // Schedule garbage for cleanup as soon as it's created
        ScheduleGarbageCleanup(uid);
    }

    private void OnParentChanged(EntityUid uid, SpaceGarbageComponent component, ref EntParentChangedMessage args)
    {
        if (!_pendingCleanup.ContainsKey(uid))
            ScheduleGarbageCleanup(uid);
    }

    private void CheckSpaceGarbage(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return;

        // Skip if already scheduled for deletion
        if (_pendingCleanup.ContainsKey(uid))
            return;

        ScheduleGarbageCleanup(uid);
    }

    private void ScheduleGarbageCleanup(EntityUid uid)
    {
        // Skip if already scheduled
        if (_pendingCleanup.ContainsKey(uid))
            return;

        var targetTime = _timing.CurTime + TimeSpan.FromSeconds(CleanupDelay);
        _pendingCleanup[uid] = targetTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pendingCleanup.Count == 0)
            return;

        // Check if any garbage needs to be cleaned up
        var currentTime = _timing.CurTime;
        var toRemove = new List<EntityUid>();

        foreach (var (garbageUid, targetTime) in _pendingCleanup)
        {
            // Skip if the time hasn't elapsed yet
            if (currentTime < targetTime)
                continue;

            // Check if the entity still exists
            if (!EntityManager.EntityExists(garbageUid))
            {
                toRemove.Add(garbageUid);
                continue;
            }

            // Verify it still has the component
            if (!HasComp<SpaceGarbageComponent>(garbageUid))
            {
                toRemove.Add(garbageUid);
                continue;
            }

            // Queue the entity for deletion - regardless of whether it's on a grid or in space
            QueueDel(garbageUid);
            toRemove.Add(garbageUid);
        }

        // Remove processed garbage from the pending list
        foreach (var garbageUid in toRemove)
        {
            _pendingCleanup.Remove(garbageUid);
        }
    }
}
