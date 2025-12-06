using System.Numerics;

// Mono - whole file

namespace Content.Server.Physics.Controllers;

public record struct ShuttleInput(Vector2 Strafe, float Rotation, float Brakes);

/// <summary>
///     Raised to get inputs given to a shuttle.
/// </summary>
[ByRefEvent]
public record struct GetShuttleInputsEvent(List<ShuttleInput> Inputs);
