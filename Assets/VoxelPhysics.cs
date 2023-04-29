using UnityEngine;

public static class VoxelPhysics
{
    public const float Gravity = 40f;
    
    public static bool HasBlockCollision(VoxelWorld voxelWorld, Vector3 position, Vector3 size)
    {
        // The position supplied is the center of the bounding box.
        var pos0 = position - size * 0.5f;

        var steps = Vector3Int.FloorToInt(size);
        steps.x += 1;
        steps.y += 1;
        steps.z += 1;
        var interpolated = Vector3.zero;

        // Interpolate between pos0 and pos1, checking each block between them.
        for (var x = 0; x <= steps.x; x++) {
            interpolated.x = pos0.x + (float)x / steps.x * size.x;

            for (var y = 0; y <= steps.y; y++) {
                interpolated.y = pos0.y + (float)y / steps.y * size.y;

                for (var z = 0; z <= steps.z; z++) {
                    interpolated.z = pos0.z + (float)z / steps.z * size.z;

                    var interpolatedBlock = Vector3Int.FloorToInt(interpolated);

                    if (voxelWorld.IsBlockOccupied(interpolatedBlock.x, interpolatedBlock.y, interpolatedBlock.z))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}