using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TowerDefense.World.Data;
using TowerDefense.World.Generation;
using TowerDefense.World.Tunneling;
using TowerDefense.Waves;

namespace TowerDefense.Towers
{
    public enum DefenderType
    {
        StandardTurret = 0,
        Wall = 1,
        SlowField = 2,
        ResourceGenerator = 3
    }

    public class TurretPlacementManager : MonoBehaviour
    {
        [Header("Placement Setup - Defender Prefabs")]
        [SerializeField] private GameObject standardTurretPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject slowFieldPrefab;
        [SerializeField] private GameObject resourceGeneratorPrefab;

        [Header("Defender Gold Costs")]
        [SerializeField] private int standardTurretCost = 100;
        [SerializeField] private int wallCost = 75;
        [SerializeField] private int slowFieldCost = 120;
        [SerializeField] private int resourceGeneratorCost = 125;

        [Header("Defender Indent Carving Settings")]
        [SerializeField] private Vector2 standardIndentRadius = new Vector2(0.8f, 2.0f);

        [SerializeField] private Vector2 wallIndentRadius = new Vector2(1.2f, 2.0f);

        [SerializeField] private Vector2 slowFieldIndentRadius = new Vector2(1.5f, 1.5f);

        [SerializeField] private Vector2 resourceGeneratorIndentRadius = new Vector2(0.9f, 2.0f);

        [Header("Placement Settings")]
        [SerializeField] private LayerMask terrainLayer;
        [SerializeField] private float frontZOffset = -1.5f;

        [Header("Wave Integration")]
        [SerializeField] private WaveManager waveManager;

        private DefenderType _selectedDefenderType = DefenderType.StandardTurret;
        private WorldGenerator _worldGenerator;
        private Camera _mainCamera;

        public DefenderType SelectedDefenderType => _selectedDefenderType;

        private void Awake()
        {
            _worldGenerator = FindFirstObjectByType<WorldGenerator>();
            _mainCamera = Camera.main;

            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                TryPlaceTurret();
            }
        }

       
        public void SelectDefenderType(int typeIndex)
        {
            _selectedDefenderType = (DefenderType)typeIndex;
            Debug.Log($"Selected Defender Type: {_selectedDefenderType} (Cost: {GetCostForSelectedType()}g)");
        }

        public void SelectDefenderType(DefenderType type)
        {
            _selectedDefenderType = type;
            Debug.Log($"Selected Defender Type: {_selectedDefenderType} (Cost: {GetCostForSelectedType()}g)");
        }

        public int GetCostForSelectedType()
        {
            return _selectedDefenderType switch
            {
                DefenderType.StandardTurret => standardTurretCost,
                DefenderType.Wall => wallCost,
                DefenderType.SlowField => slowFieldCost,
                DefenderType.ResourceGenerator => resourceGeneratorCost,
                _ => standardTurretCost
            };
        }

        private GameObject GetSelectedPrefab()
        {
            return _selectedDefenderType switch
            {
                DefenderType.StandardTurret => standardTurretPrefab,
                DefenderType.Wall => wallPrefab,
                DefenderType.SlowField => slowFieldPrefab,
                DefenderType.ResourceGenerator => resourceGeneratorPrefab,
                _ => standardTurretPrefab
            };
        }

        private Vector2 GetSelectedIndentRadius()
        {
            return _selectedDefenderType switch
            {
                DefenderType.StandardTurret => standardIndentRadius,
                DefenderType.Wall => wallIndentRadius,
                DefenderType.SlowField => slowFieldIndentRadius,
                DefenderType.ResourceGenerator => resourceGeneratorIndentRadius,
                _ => standardIndentRadius
            };
        }

        private void TryPlaceTurret()
        {
            int cost = GetCostForSelectedType();

            if (waveManager != null && waveManager.CurrentGold < cost)
            {
                Debug.LogWarning($"Placement failed: Insufficient gold! Required: {cost}g, Current: {waveManager.CurrentGold}g");
                return;
            }

            if (_mainCamera == null || _worldGenerator == null)
            {
                Debug.LogError("Placement failed: Main Camera or WorldGenerator is missing!");
                return;
            }

            GameObject prefabToSpawn = GetSelectedPrefab();
            if (prefabToSpawn == null)
            {
                Debug.LogError($"Placement failed: No prefab assigned for selection {_selectedDefenderType}!");
                return;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(mousePosition);

            float voxelSize = _worldGenerator.Config.voxelSize;
            float terrainFrontZ = 0f;

            Plane terrainPlane = new Plane(Vector3.back, new Vector3(0, 0, terrainFrontZ));

            if (terrainPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                int voxelX = Mathf.FloorToInt(hitPoint.x / voxelSize);
                int voxelY = Mathf.FloorToInt(hitPoint.y / voxelSize);
                int voxelZ = Mathf.FloorToInt(terrainFrontZ / voxelSize);

                VoxelGrid grid = _worldGenerator.Grid;

                // Checked using IsInBounds as declared in VoxelGrid.cs
                if (grid != null && grid.IsInBounds(voxelX, voxelY, voxelZ))
                {
                    if (waveManager != null && !waveManager.TrySpendGold(cost))
                    {
                        Debug.LogWarning("Placement failed: Gold transaction rejected.");
                        return;
                    }

                    Vector2 indentRadius = GetSelectedIndentRadius();
                    TunnelCarver.CarveRoundedVolume(grid, voxelX, voxelY, voxelZ, indentRadius.x, indentRadius.y);
                    _worldGenerator.NotifyTerrainModified();

                    Vector3 voxelWorldPos = _worldGenerator.VoxelToWorld(new Vector3Int(voxelX, voxelY, voxelZ));
                    Vector3 placementPosition = new Vector3(voxelWorldPos.x, voxelWorldPos.y, voxelWorldPos.z + frontZOffset);

                    Instantiate(prefabToSpawn, placementPosition, Quaternion.identity);

                    if (waveManager != null)
                    {
                        waveManager.RegisterPlacedDefender(_selectedDefenderType);
                    }

                    Debug.Log($"{_selectedDefenderType} placed successfully at [{voxelX}, {voxelY}, {voxelZ}] for {cost}g! (Indent Carve: XY={indentRadius.x}, Z={indentRadius.y})");
                }
                else
                {
                    Debug.Log("Placement failed: Targeted voxel is out of bounds.");
                }
            }
        }
    }
}