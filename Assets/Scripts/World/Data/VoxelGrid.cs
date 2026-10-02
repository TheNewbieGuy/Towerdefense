using System;

namespace TowerDefense.World.Data
{
   
    public class VoxelGrid
    {
        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }

        private readonly VoxelType[] _voxels;

        public VoxelGrid(int width, int height, int depth)
        {
            if (width <= 0 || height <= 0 || depth <= 0)
                throw new ArgumentException("VoxelGrid dimensions must be positive.");

            Width = width;
            Height = height;
            Depth = depth;

            _voxels = new VoxelType[width * height * depth];
        }

        public bool IsInBounds(int x, int y, int z)
        {
            return x >= 0 && x < Width &&
                   y >= 0 && y < Height &&
                   z >= 0 && z < Depth;
        }

        public VoxelType GetVoxel(int x, int y, int z)
        {
            if (!IsInBounds(x, y, z))
                return VoxelType.Air; 

            return _voxels[FlattenIndex(x, y, z)];
        }

        public void SetVoxel(int x, int y, int z, VoxelType type)
        {
            if (!IsInBounds(x, y, z))
                return;

            _voxels[FlattenIndex(x, y, z)] = type;
        }

        public bool IsSolid(int x, int y, int z)
        {
            return GetVoxel(x, y, z) != VoxelType.Air;
        }

        private int FlattenIndex(int x, int y, int z)
        {
            return x + (Width * z) + (Width * Depth * y);
        }
    }
}
