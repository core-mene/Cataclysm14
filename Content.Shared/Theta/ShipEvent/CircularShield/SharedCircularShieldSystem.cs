using System.Numerics;
using Content.Shared.Theta.ShipEvent.Components;

namespace Content.Shared.Theta.ShipEvent.CircularShield;

public class SharedCircularShieldSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    /// <summary>
    /// Generates vertices for a circular shield with origin at (0,0)
    /// </summary>
    public Vector2[] GenerateConeVertices(int radius, Angle angle, Angle width, int extraArcPoints = 0)
    {
        // Check if this is a full or almost full circle
        bool isFullCircle = width.Degrees >= 359.0f;

        Vector2[] vertices;

        if (isFullCircle)
        {
            // For full circles, we'll still use a triangle fan but with a center point
            // and carefully placed vertices to avoid the visual artifacts
            int totalPoints = Math.Max(30, extraArcPoints);
            vertices = new Vector2[totalPoints + 2]; // +2 for center and closing the loop

            // First vertex is center point
            vertices[0] = Vector2.Zero;

            Angle step = Angle.FromDegrees(360.0f / totalPoints);

            for (var i = 0; i < totalPoints; i++)
            {
                Angle currentAngle = step * i;
                vertices[i + 1] = currentAngle.ToVec() * radius;
            }

            // Close the loop by duplicating the first edge vertex, but with a tiny offset
            // to prevent perfect overlap and shader artifacts
            vertices[totalPoints + 1] = vertices[1] * 0.999f;
        }
        else
        {
            // Original partial cone/arc implementation
            //central point + two edge points + extra arc points + central point again since this is used for drawing and input must be looped
            vertices = new Vector2[4 + extraArcPoints];
            vertices[0] = new Vector2(0, 0);

            Angle start = angle - width / 2;
            Angle step = width / (2 + extraArcPoints);

            for (var i = 1; i < 3 + extraArcPoints; i++)
            {
                vertices[i] = (start + step * (i - 1)).ToVec() * radius;
            }

            vertices[vertices.Length - 1] = vertices[0];
        }

        return vertices;
    }

    /// <summary>
    /// Generates vertices for a cone shape with origin at the specified offset
    /// </summary>
    public Vector2[] GenerateConeVerticesWithOffset(int radius, Angle angle, Angle width, Vector2 centerOffset, int extraArcPoints = 0)
    {
        var vertices = GenerateConeVertices(radius, angle, width, extraArcPoints);

        // Apply offset to all vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] += centerOffset;
        }

        return vertices;
    }

    public bool EntityInShield(EntityUid uid, CircularShieldComponent shield, EntityUid otherUid, SharedTransformSystem? transformSystem = null)
    {
        transformSystem ??= _transformSystem;

        // Get the shield's parent entity (grid)
        if (!_entityManager.TryGetComponent(uid, out TransformComponent? transform))
            return false;

        var gridUid = transform.ParentUid;

        // If no valid grid, fall back to the shield's position
        if (!_entityManager.EntityExists(gridUid))
        {
            Vector2 fallbackDelta = transformSystem.GetWorldPosition(otherUid) - transformSystem.GetWorldPosition(uid);
            Angle fallbackAngle = ThetaHelpers.AngNormal(new Angle(fallbackDelta) - transformSystem.GetWorldRotation(uid));
            Angle fallbackStart = ThetaHelpers.AngNormal(shield.Angle - shield.Width / 2);
            return ThetaHelpers.AngInSector(fallbackAngle, fallbackStart, shield.Width) &&
                fallbackDelta.Length() < shield.Radius + 0.1; //+0.1 to avoid being screwed over by rounding errors
        }

        // Use grid position and rotation for calculations
        if (!_entityManager.TryGetComponent(gridUid, out TransformComponent? gridTransform))
            return false;

        Vector2 gridPos = transformSystem.GetWorldPosition(gridTransform);
        Angle gridRot = transformSystem.GetWorldRotation(gridTransform);

        // Get center of mass offset if available
        Vector2 centerOffset = Vector2.Zero;
        if (_entityManager.TryGetComponent(gridUid, out Robust.Shared.Physics.Components.PhysicsComponent? physics))
        {
            centerOffset = physics.LocalCenter;
            // Apply center of mass offset to grid position
            var worldOffset = gridRot.RotateVec(centerOffset);
            gridPos += worldOffset;
        }

        Vector2 otherPos = transformSystem.GetWorldPosition(otherUid);

        Vector2 delta = otherPos - gridPos;
        Angle relativeAngle = ThetaHelpers.AngNormal(new Angle(delta) - gridRot);

        // Calculate shield start angle, accounting for center offset if needed
        Angle shieldStart = ThetaHelpers.AngNormal(shield.Angle - shield.Width / 2);

        return ThetaHelpers.AngInSector(relativeAngle, shieldStart, shield.Width) &&
               delta.Length() < shield.Radius + 0.1;
    }
}
