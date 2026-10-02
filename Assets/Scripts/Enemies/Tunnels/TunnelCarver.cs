using UnityEngine;
using TowerDefense.World.Data;

namespace TowerDefense.World.Tunneling
{
   
    public static class TunnelCarver
    {
        public static void CarveRoundedVolume(
            VoxelGrid grid, int centerX, int centerY, int centerZ, float radiusXY, float radiusZ)
        {
            int rXY = Mathf.CeilToInt(radiusXY);
            int rZ = Mathf.CeilToInt(radiusZ);

            float radiusXYSq = radiusXY * radiusXY;
            float radiusZSq = radiusZ * radiusZ;

            for (int x = -rXY; x <= rXY; x++)
            {
                for (int y = -rXY; y <= rXY; y++)
                {
                    for (int z = -rZ; z <= rZ; z++)
                    {
                        
                        
                        float normalizedDistanceSq =
                            (x * x + y * y) / radiusXYSq +
                            (z * z) / radiusZSq;

                        if (normalizedDistanceSq <= 1f)
                        {
                            grid.SetVoxel(centerX + x, centerY + y, centerZ + z, VoxelType.Air);
                        }
                    }
                }
            }
        }
    }
}