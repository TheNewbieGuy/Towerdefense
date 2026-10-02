using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.World.Tunneling
{
    public class TunnelPath
    {
        public int Id;
        public readonly List<Vector2Int> Voxels = new List<Vector2Int>();
        public bool IsFollowing;
    }

    public class TunnelNetwork
    {
        private readonly List<TunnelPath> _paths = new List<TunnelPath>();
        private int _nextId;

        public TunnelPath RegisterNewPath()
        {
            var path = new TunnelPath { Id = _nextId++ };
            _paths.Add(path);
            return path;
        }

        public bool IsPartOfOtherPath(Vector2Int position, int excludeId, out TunnelPath foundPath, out int foundIndex)
        {
            foreach (var path in _paths)
            {
                if (path.Id == excludeId || path.IsFollowing) continue;

                int index = path.Voxels.IndexOf(position);
                if (index >= 0)
                {
                    foundPath = path;
                    foundIndex = index;
                    return true;
                }
            }

            foundPath = null;
            foundIndex = -1;
            return false;
        }

        public bool TryFindNearbyPath(Vector2Int position, int excludeId, float radius, out TunnelPath foundPath, out int foundIndex)
        {
            float radiusSq = radius * radius;

            foreach (var path in _paths)
            {
                if (path.Id == excludeId || path.IsFollowing) continue;

                for (int i = 0; i < path.Voxels.Count; i++)
                {
                    Vector2 offset = path.Voxels[i] - position;
                    if (offset.sqrMagnitude <= radiusSq)
                    {
                        foundPath = path;
                        foundIndex = i;
                        return true;
                    }
                }
            }

            foundPath = null;
            foundIndex = -1;
            return false;
        }

        public bool TryGetRandomJoinablePoint(out TunnelPath path, out int index)
        {
            List<TunnelPath> candidates = null;

            foreach (var p in _paths)
            {
                if (p.Voxels.Count == 0 || p.IsFollowing) continue;
                candidates ??= new List<TunnelPath>();
                candidates.Add(p);
            }

            if (candidates == null)
            {
                path = null;
                index = -1;
                return false;
            }

            path = candidates[Random.Range(0, candidates.Count)];
            index = Random.Range(0, path.Voxels.Count);
            return true;
        }

        public bool TryGetRandomStartPoint(out TunnelPath path, out int index)
        {
            List<TunnelPath> candidates = null;

            foreach (var p in _paths)
            {
                if (p.Voxels.Count == 0 || p.IsFollowing) continue;
                candidates ??= new List<TunnelPath>();
                candidates.Add(p);
            }

            if (candidates == null)
            {
                path = null;
                index = -1;
                return false;
            }

            path = candidates[Random.Range(0, candidates.Count)];
            index = 0; 
            return true;
        }
    }
}