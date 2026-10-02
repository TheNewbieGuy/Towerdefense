using UnityEngine;
using TowerDefense.World.Data;
using TowerDefense.World.Generation;

namespace TowerDefense.World.Rendering
{
    
    [RequireComponent(typeof(Camera))]
    public class CrossSectionCameraRig : MonoBehaviour
    {
        [SerializeField] private WorldGenerator worldGenerator;

        [Tooltip("Extra breathing room around the world, as a fraction of the fitted size.")]
        [SerializeField] private float framingPadding = 0.1f;

        [Tooltip("How far in front of the world (along -Z) the camera sits.")]
        [SerializeField] private float distanceFromWorld = 20f;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
        }

        private void OnEnable()
        {
            if (worldGenerator != null)
                worldGenerator.OnWorldGenerated += FrameWorld;
        }

        private void OnDisable()
        {
            if (worldGenerator != null)
                worldGenerator.OnWorldGenerated -= FrameWorld;
        }

        private void FrameWorld(VoxelGrid grid)
        {
            WorldGenerationConfig config = worldGenerator.Config;
            float voxelSize = config.voxelSize;

            float worldWidth = grid.Width * voxelSize;
            float worldHeight = grid.Height * voxelSize;
            float worldDepth = grid.Depth * voxelSize;

            float sizeForHeight = worldHeight / 2f;
            float sizeForWidth = (worldWidth / 2f) / _camera.aspect;
            _camera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth) * (1f + framingPadding);

            Vector3 worldCenter = new Vector3(worldWidth / 2f, worldHeight / 2f, 0f);
            transform.position = worldCenter + new Vector3(0f, 0f, -distanceFromWorld);
            transform.rotation = Quaternion.identity; 

           
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = distanceFromWorld + worldDepth + 10f;
        }
    }
}