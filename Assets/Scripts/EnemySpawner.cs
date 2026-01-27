using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public ProceduralTerrainGenerator terrain;
    public TowerHealth tower;

    [Header("Spawn")]
    public float spawnInterval = 2f;
    public float enemySpeed = 2f;
    public float enemyMaxHealth = 5f;
    public float damageToTower = 1f;
    public float enemyHeightOffset = 0.5f;

    [Header("Spawn Locations")]
    public bool spawnOnePerInterval = true;
    public bool showSpawnMarkers = true;
    public float spawnMarkerScale = 0.5f;
    public Color spawnMarkerColor = new Color(0.85f, 0.2f, 0.9f);

    [Header("Defender Attack")]
    public float defenderAttackRange = 1.2f;
    public float defenderAttackInterval = 0.6f;
    public float damageToDefender = 1f;

    [Header("Visuals")]
    public Color enemyColor = new Color(1f, 0.3f, 0.3f);

    private readonly List<IReadOnlyList<Vector3>> paths = new List<IReadOnlyList<Vector3>>();
    private readonly List<Vector3> spawnLocations = new List<Vector3>();
    private readonly List<GameObject> spawnMarkers = new List<GameObject>();
    private float timer;
    private int spawnIndex;

    private void Start()
    {
        if (terrain == null)
        {
            terrain = FindFirstObjectByType<ProceduralTerrainGenerator>();
        }

        if (tower == null)
        {
            tower = FindFirstObjectByType<TowerHealth>();
        }

        if (terrain == null)
        {
            Debug.LogWarning("EnemySpawner: Terrain generator not found.");
            enabled = false;
            return;
        }

        CachePaths();
    }

    private void Update()
    {
        if (GameManager.IsGameOver)
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer < spawnInterval)
        {
            return;
        }

        timer = 0f;
        SpawnEnemies();
    }

    private void CachePaths()
    {
        paths.Clear();
        spawnLocations.Clear();
        ClearSpawnMarkers();
        var source = terrain.PathsWorld;
        if (source == null)
        {
            return;
        }

        foreach (var path in source)
        {
            if (path == null || path.Count == 0)
            {
                continue;
            }
            paths.Add(path);
            spawnLocations.Add(path[0]);
        }

        if (showSpawnMarkers)
        {
            for (int i = 0; i < spawnLocations.Count; i++)
            {
                CreateSpawnMarker(spawnLocations[i]);
            }
        }
    }

    private void SpawnEnemies()
    {
        if (paths.Count == 0)
        {
            CachePaths();
        }

        if (paths.Count == 0)
        {
            return;
        }

        if (spawnOnePerInterval)
        {
            spawnIndex = Mathf.Clamp(spawnIndex, 0, paths.Count - 1);
            IReadOnlyList<Vector3> path = paths[spawnIndex];
            Enemy enemy = CreateEnemy();
            enemy.Initialize(path, tower, enemySpeed, enemyMaxHealth, damageToTower, enemyHeightOffset,
                defenderAttackRange, defenderAttackInterval, damageToDefender);
            spawnIndex = (spawnIndex + 1) % paths.Count;
        }
        else
        {
            foreach (var path in paths)
            {
                Enemy enemy = CreateEnemy();
                enemy.Initialize(path, tower, enemySpeed, enemyMaxHealth, damageToTower, enemyHeightOffset,
                    defenderAttackRange, defenderAttackInterval, damageToDefender);
            }
        }
    }

    private Enemy CreateEnemy()
    {
        GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemyObject.name = "Enemy";
        enemyObject.transform.localScale = Vector3.one * 0.6f;
        var renderer = enemyObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = enemyColor;
        }

        return enemyObject.AddComponent<Enemy>();
    }

    private void CreateSpawnMarker(Vector3 position)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "SpawnMarker";
        marker.transform.SetParent(transform, false);
        marker.transform.position = position + Vector3.up * 0.1f;
        marker.transform.localScale = new Vector3(spawnMarkerScale, 0.05f, spawnMarkerScale);
        var renderer = marker.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = spawnMarkerColor;
        }
        spawnMarkers.Add(marker);
    }

    private void ClearSpawnMarkers()
    {
        for (int i = 0; i < spawnMarkers.Count; i++)
        {
            if (spawnMarkers[i] != null)
            {
                Destroy(spawnMarkers[i]);
            }
        }
        spawnMarkers.Clear();
    }
}
