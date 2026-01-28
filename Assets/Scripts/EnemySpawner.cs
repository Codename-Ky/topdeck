using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    private struct EnemyTypeSettings
    {
        public string displayName;
        public GameObject prefab;
        public string prefabPath;
        [Min(0f)] public float spawnWeight;

        [Header("Stat Multipliers")]
        public float speedMultiplier;
        public float healthMultiplier;
        public float towerDamageMultiplier;
        public float defenderDamageMultiplier;

        [Header("Attack Overrides")]
        public bool overrideTowerAttack;
        public float towerAttackRange;
        public float towerAttackInterval;
        public bool overrideDefenderAttack;
        public float defenderAttackRange;
        public float defenderAttackInterval;

        [Header("Behavior")]
        public EnemyAttackPriority attackPriority;
        [Range(0.1f, 1f)] public float damageTakenMultiplier;
        public bool enrageOnLowHealth;
        [Range(0.05f, 0.9f)] public float enrageHealthFraction;
        public float enrageSpeedMultiplier;
        public float enrageDamageMultiplier;
        public float enrageAttackIntervalMultiplier;

        public static EnemyTypeSettings CreateRager(string prefabPathValue)
        {
            return new EnemyTypeSettings
            {
                displayName = "Rager",
                prefabPath = prefabPathValue,
                spawnWeight = 1f,
                speedMultiplier = 1.3f,
                healthMultiplier = 0.8f,
                towerDamageMultiplier = 1f,
                defenderDamageMultiplier = 1f,
                overrideTowerAttack = false,
                towerAttackRange = 1.4f,
                towerAttackInterval = 0.8f,
                overrideDefenderAttack = false,
                defenderAttackRange = 1.2f,
                defenderAttackInterval = 0.6f,
                attackPriority = EnemyAttackPriority.TowerFirst,
                damageTakenMultiplier = 1f,
                enrageOnLowHealth = true,
                enrageHealthFraction = 0.5f,
                enrageSpeedMultiplier = 1.6f,
                enrageDamageMultiplier = 1.25f,
                enrageAttackIntervalMultiplier = 0.7f
            };
        }

        public static EnemyTypeSettings CreateBrute(string prefabPathValue)
        {
            return new EnemyTypeSettings
            {
                displayName = "Brute",
                prefabPath = prefabPathValue,
                spawnWeight = 1f,
                speedMultiplier = 0.7f,
                healthMultiplier = 2.0f,
                towerDamageMultiplier = 1.5f,
                defenderDamageMultiplier = 1.2f,
                overrideTowerAttack = false,
                towerAttackRange = 1.4f,
                towerAttackInterval = 0.8f,
                overrideDefenderAttack = false,
                defenderAttackRange = 1.2f,
                defenderAttackInterval = 0.6f,
                attackPriority = EnemyAttackPriority.TowerFirst,
                damageTakenMultiplier = 0.6f,
                enrageOnLowHealth = false,
                enrageHealthFraction = 0.4f,
                enrageSpeedMultiplier = 1.4f,
                enrageDamageMultiplier = 1.2f,
                enrageAttackIntervalMultiplier = 0.8f
            };
        }

        public static EnemyTypeSettings CreateSkirmisher(string prefabPathValue)
        {
            return new EnemyTypeSettings
            {
                displayName = "Skirmisher",
                prefabPath = prefabPathValue,
                spawnWeight = 1f,
                speedMultiplier = 1.05f,
                healthMultiplier = 1f,
                towerDamageMultiplier = 0.9f,
                defenderDamageMultiplier = 1.1f,
                overrideTowerAttack = true,
                towerAttackRange = 1.1f,
                towerAttackInterval = 0.75f,
                overrideDefenderAttack = true,
                defenderAttackRange = 1.9f,
                defenderAttackInterval = 0.5f,
                attackPriority = EnemyAttackPriority.DefenderFirst,
                damageTakenMultiplier = 1f,
                enrageOnLowHealth = false,
                enrageHealthFraction = 0.4f,
                enrageSpeedMultiplier = 1.4f,
                enrageDamageMultiplier = 1.2f,
                enrageAttackIntervalMultiplier = 0.8f
            };
        }
    }
    [Header("References")]
    [SerializeField] private ProceduralTerrainGenerator terrain;
    [SerializeField] private TowerHealth tower;

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float enemySpeed = 2f;
    [SerializeField] private float enemyMaxHealth = 5f;
    [SerializeField] private float damageToTower = 1f;
    [SerializeField] private float enemyHeightOffset = 0.5f;

    [Header("Tower Attack")]
    [SerializeField] private float towerAttackRange = 1.4f;
    [SerializeField] private float towerAttackInterval = 0.8f;

    [Header("Spawn Locations")]
    [SerializeField] private bool spawnOnePerInterval = true;
    [SerializeField] private bool showSpawnMarkers = true;
    [SerializeField] private float spawnMarkerScale = 0.5f;
    [SerializeField] private Color spawnMarkerColor = new Color(0.85f, 0.2f, 0.9f);

    [Header("Rounds")]
    [SerializeField] private bool useRounds = true;

    [Header("Defender Attack")]
    [SerializeField] private float defenderAttackRange = 1.2f;
    [SerializeField] private float defenderAttackInterval = 0.6f;
    [SerializeField] private float damageToDefender = 1f;
    [SerializeField] private LayerMask defenderTargetMask = ~0;

    [Header("Enemy Types")]
    [SerializeField] private EnemyTypeSettings[] enemyTypes;

    [Header("Visuals")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private bool applyOverridesToPrefab = true;

    [Header("Pooling")]
    [SerializeField] private bool usePooling = true;
    [SerializeField, Min(0)] private int enemyPoolSize = 0;

    private readonly List<IReadOnlyList<Vector3>> paths = new List<IReadOnlyList<Vector3>>();
    private readonly List<Vector3> spawnLocations = new List<Vector3>();
    private readonly List<GameObject> spawnMarkers = new List<GameObject>();
    private readonly Queue<Enemy> enemyPool = new Queue<Enemy>();
    private readonly List<Queue<Enemy>> enemyTypePools = new List<Queue<Enemy>>();
    private readonly List<int> validTypeIndices = new List<int>();
    private Transform enemyContainer;
    private int enemyLayer = -1;
    private float timer;
    private int spawnIndex;
    private int enemiesToSpawn;
    private int enemiesSpawned;
    private int enemiesAlive;
    private bool roundActive;
    private float roundEnemyMaxHealth;
    private float roundEnemySpeed;
    private float roundDamageToTower;
    private float roundDamageToDefender;
    private bool useTypeSpawning;

    public event Action<EnemySpawner> RoundCompleted;

    private void Awake()
    {
        ResolveReferences();
        Transform existing = transform.Find("Enemies");
        enemyContainer = existing != null ? existing : new GameObject("Enemies").transform;
        enemyContainer.SetParent(transform, false);
        enemyLayer = LayerMask.NameToLayer("Enemy");
        SetupEnemyTypes();
        WarmPool();
    }

    private void Start()
    {
        ResolveReferences();

        if (terrain == null)
        {
            Debug.LogWarning("EnemySpawner: Terrain generator not found.");
            enabled = false;
            return;
        }

        CachePaths();
    }

    private void ResolveReferences()
    {
        if (terrain == null)
        {
            terrain = FindFirstObjectByType<ProceduralTerrainGenerator>();
        }

        if (tower == null)
        {
            tower = FindFirstObjectByType<TowerHealth>();
        }
    }

    private void SetupEnemyTypes()
    {
        validTypeIndices.Clear();
        enemyTypePools.Clear();
        useTypeSpawning = false;

        if (enemyTypes == null || enemyTypes.Length == 0)
        {
            return;
        }

        for (int i = 0; i < enemyTypes.Length; i++)
        {
            EnemyTypeSettings type = enemyTypes[i];
#if UNITY_EDITOR
            if (type.prefab == null && !string.IsNullOrEmpty(type.prefabPath))
            {
                type.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(type.prefabPath);
                enemyTypes[i] = type;
            }
#endif
            if (type.prefab == null || type.spawnWeight <= 0f)
            {
                continue;
            }

            validTypeIndices.Add(i);
        }

        useTypeSpawning = validTypeIndices.Count > 0;
        if (useTypeSpawning)
        {
            for (int i = 0; i < enemyTypes.Length; i++)
            {
                enemyTypePools.Add(new Queue<Enemy>());
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (enemyTypes == null || enemyTypes.Length == 0)
        {
            enemyTypes = new[]
            {
                EnemyTypeSettings.CreateRager("Assets/Prefabs/Bug_enemy1.prefab"),
                EnemyTypeSettings.CreateBrute("Assets/Prefabs/Bug_enemy2.prefab"),
                EnemyTypeSettings.CreateSkirmisher("Assets/Prefabs/Bug_enemy3.prefab")
            };
            applyOverridesToPrefab = false;
        }

        for (int i = 0; i < enemyTypes.Length; i++)
        {
            EnemyTypeSettings type = enemyTypes[i];
            if (type.prefab == null && !string.IsNullOrEmpty(type.prefabPath))
            {
                type.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(type.prefabPath);
                enemyTypes[i] = type;
            }
        }
    }
#endif

    private void Update()
    {
        if (GameManager.IsGameOver)
        {
            return;
        }

        if (useRounds)
        {
            if (!roundActive)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer < spawnInterval)
            {
                return;
            }

            timer = 0f;
            SpawnRoundTick();
        }
        else
        {
            timer += Time.deltaTime;
            if (timer < spawnInterval)
            {
                return;
            }

            timer = 0f;
            SpawnContinuousEnemies();
        }
    }

    private void CachePaths()
    {
        paths.Clear();
        spawnLocations.Clear();
        ClearSpawnMarkers();
        if (terrain == null)
        {
            ResolveReferences();
            if (terrain == null)
            {
                return;
            }
        }
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

    public void StartRound(int enemyCount, float healthMultiplier, float speedMultiplier, float damageMultiplier)
    {
        CachePaths();
        enemiesToSpawn = Mathf.Max(0, enemyCount);
        enemiesSpawned = 0;
        enemiesAlive = 0;
        spawnIndex = 0;
        roundEnemyMaxHealth = enemyMaxHealth * healthMultiplier;
        roundEnemySpeed = enemySpeed * speedMultiplier;
        roundDamageToTower = damageToTower * damageMultiplier;
        roundDamageToDefender = damageToDefender * damageMultiplier;
        roundActive = enemiesToSpawn > 0;
        timer = 0f;

        if (!roundActive)
        {
            RoundCompleted?.Invoke(this);
        }
    }

    private void SpawnRoundTick()
    {
        if (paths.Count == 0)
        {
            CachePaths();
        }

        if (paths.Count == 0)
        {
            return;
        }

        int remaining = enemiesToSpawn - enemiesSpawned;
        if (remaining <= 0)
        {
            CheckRoundComplete();
            return;
        }

        int spawnCount = spawnOnePerInterval ? 1 : paths.Count;
        spawnCount = Mathf.Min(spawnCount, remaining);

        for (int i = 0; i < spawnCount; i++)
        {
            spawnIndex = Mathf.Clamp(spawnIndex, 0, paths.Count - 1);
            IReadOnlyList<Vector3> path = paths[spawnIndex];
            SpawnEnemy(path, roundEnemySpeed, roundEnemyMaxHealth, roundDamageToTower, roundDamageToDefender, true);
            spawnIndex = (spawnIndex + 1) % paths.Count;
        }

        CheckRoundComplete();
    }

    private void SpawnContinuousEnemies()
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
            SpawnEnemy(path, enemySpeed, enemyMaxHealth, damageToTower, damageToDefender, false);
            spawnIndex = (spawnIndex + 1) % paths.Count;
        }
        else
        {
            foreach (var path in paths)
            {
                SpawnEnemy(path, enemySpeed, enemyMaxHealth, damageToTower, damageToDefender, false);
            }
        }
    }

    private void SpawnEnemy(IReadOnlyList<Vector3> path, float speed, float health, float towerDamage, float defenderDamage, bool trackRound)
    {
        int typeIndex = ChooseEnemyTypeIndex();
        EnemyTypeSettings type = default;
        if (useTypeSpawning && typeIndex >= 0 && typeIndex < enemyTypes.Length)
        {
            type = enemyTypes[typeIndex];
        }

        Enemy enemy = GetEnemy(typeIndex);
        if (usePooling)
        {
            enemy.gameObject.SetActive(false);
        }
        enemy.SetReleaseAction(usePooling ? ReleaseEnemy : null);
        if (trackRound)
        {
            enemy.Died += HandleEnemyDied;
            enemiesAlive += 1;
            enemiesSpawned += 1;
        }

        EnemyConfig config = BuildConfig(typeIndex, type, speed, health, towerDamage, defenderDamage);
        enemy.AssignTypeId(typeIndex);
        enemy.Initialize(path, tower, config);
        if (usePooling)
        {
            enemy.gameObject.SetActive(true);
        }
    }

    private void WarmPool()
    {
        if (!usePooling || enemyPoolSize <= 0)
        {
            return;
        }

        if (useTypeSpawning)
        {
            for (int i = 0; i < validTypeIndices.Count; i++)
            {
                int typeIndex = validTypeIndices[i];
                EnemyTypeSettings type = enemyTypes[typeIndex];
                for (int j = 0; j < enemyPoolSize; j++)
                {
                    Enemy enemy = CreateEnemyInstance(type.prefab, typeIndex);
                    ReleaseEnemy(enemy);
                }
            }
        }
        else
        {
            for (int i = 0; i < enemyPoolSize; i++)
            {
                Enemy enemy = CreateEnemyInstance(enemyPrefab, -1);
                ReleaseEnemy(enemy);
            }
        }
    }

    private int ChooseEnemyTypeIndex()
    {
        if (!useTypeSpawning || validTypeIndices.Count == 0)
        {
            return -1;
        }

        float totalWeight = 0f;
        for (int i = 0; i < validTypeIndices.Count; i++)
        {
            totalWeight += Mathf.Max(0f, enemyTypes[validTypeIndices[i]].spawnWeight);
        }

        if (totalWeight <= 0f)
        {
            return -1;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < validTypeIndices.Count; i++)
        {
            int typeIndex = validTypeIndices[i];
            cumulative += Mathf.Max(0f, enemyTypes[typeIndex].spawnWeight);
            if (roll <= cumulative)
            {
                return typeIndex;
            }
        }

        return validTypeIndices[validTypeIndices.Count - 1];
    }

    private EnemyConfig BuildConfig(int typeIndex, EnemyTypeSettings type, float speed, float health, float towerDamage, float defenderDamage)
    {
        float finalSpeed = speed;
        float finalHealth = health;
        float finalTowerDamage = towerDamage;
        float finalDefenderDamage = defenderDamage;
        float finalTowerRange = towerAttackRange;
        float finalTowerInterval = towerAttackInterval;
        float finalDefenderRange = defenderAttackRange;
        float finalDefenderInterval = defenderAttackInterval;
        EnemyAttackPriority priority = EnemyAttackPriority.TowerFirst;
        float damageTakenMultiplier = 1f;
        bool enrageOnLowHealth = false;
        float enrageHealthFraction = 0.4f;
        float enrageSpeedMultiplier = 1.4f;
        float enrageDamageMultiplier = 1.2f;
        float enrageAttackIntervalMultiplier = 0.8f;

        if (useTypeSpawning && typeIndex >= 0 && typeIndex < enemyTypes.Length)
        {
            finalSpeed *= type.speedMultiplier <= 0f ? 1f : type.speedMultiplier;
            finalHealth *= type.healthMultiplier <= 0f ? 1f : type.healthMultiplier;
            finalTowerDamage *= type.towerDamageMultiplier <= 0f ? 1f : type.towerDamageMultiplier;
            finalDefenderDamage *= type.defenderDamageMultiplier <= 0f ? 1f : type.defenderDamageMultiplier;

            if (type.overrideTowerAttack)
            {
                finalTowerRange = type.towerAttackRange;
                finalTowerInterval = type.towerAttackInterval;
            }

            if (type.overrideDefenderAttack)
            {
                finalDefenderRange = type.defenderAttackRange;
                finalDefenderInterval = type.defenderAttackInterval;
            }

            priority = type.attackPriority;
            damageTakenMultiplier = type.damageTakenMultiplier <= 0f ? 1f : type.damageTakenMultiplier;
            enrageOnLowHealth = type.enrageOnLowHealth;
            enrageHealthFraction = type.enrageHealthFraction;
            enrageSpeedMultiplier = type.enrageSpeedMultiplier;
            enrageDamageMultiplier = type.enrageDamageMultiplier;
            enrageAttackIntervalMultiplier = type.enrageAttackIntervalMultiplier;
        }

        return new EnemyConfig
        {
            TypeId = typeIndex,
            Speed = Mathf.Max(0.1f, finalSpeed),
            MaxHealth = Mathf.Max(1f, finalHealth),
            DamageToTower = Mathf.Max(0f, finalTowerDamage),
            HeightOffset = enemyHeightOffset,
            DefenderAttackRange = Mathf.Max(0.1f, finalDefenderRange),
            DefenderAttackInterval = Mathf.Max(0.05f, finalDefenderInterval),
            DamageToDefender = Mathf.Max(0f, finalDefenderDamage),
            TowerAttackRange = Mathf.Max(0.1f, finalTowerRange),
            TowerAttackInterval = Mathf.Max(0.05f, finalTowerInterval),
            DefenderTargetMask = defenderTargetMask,
            AttackPriority = priority,
            DamageTakenMultiplier = damageTakenMultiplier,
            EnrageOnLowHealth = enrageOnLowHealth,
            EnrageHealthFraction = enrageHealthFraction,
            EnrageSpeedMultiplier = enrageSpeedMultiplier,
            EnrageDamageMultiplier = enrageDamageMultiplier,
            EnrageAttackIntervalMultiplier = enrageAttackIntervalMultiplier
        };
    }

    private Enemy GetEnemy(int typeIndex)
    {
        if (usePooling)
        {
            if (useTypeSpawning && typeIndex >= 0 && typeIndex < enemyTypePools.Count)
            {
                Queue<Enemy> pool = enemyTypePools[typeIndex];
                if (pool.Count > 0)
                {
                    return pool.Dequeue();
                }
            }
            else if (enemyPool.Count > 0)
            {
                return enemyPool.Dequeue();
            }
        }

        GameObject prefab = useTypeSpawning && typeIndex >= 0 && typeIndex < enemyTypes.Length
            ? enemyTypes[typeIndex].prefab
            : enemyPrefab;
        return CreateEnemyInstance(prefab, typeIndex);
    }

    private Enemy CreateEnemyInstance(GameObject prefabOverride, int typeIndex)
    {
        GameObject enemyObject = prefabOverride != null
            ? Instantiate(prefabOverride)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        string enemyName = "Enemy";
        if (useTypeSpawning && typeIndex >= 0 && typeIndex < enemyTypes.Length)
        {
            string typeName = enemyTypes[typeIndex].displayName;
            if (!string.IsNullOrEmpty(typeName))
            {
                enemyName = "Enemy_" + typeName;
            }
        }
        enemyObject.name = enemyName;
        enemyObject.transform.SetParent(enemyContainer, false);
        LayerUtils.SetLayerRecursive(enemyObject, enemyLayer);

        if (enemyObject.GetComponentInChildren<Collider>() == null)
        {
            enemyObject.AddComponent<BoxCollider>();
        }

        Enemy enemy = ComponentUtils.GetOrAddComponent<Enemy>(enemyObject);
        enemy.AssignTypeId(typeIndex);

        if (prefabOverride == null || applyOverridesToPrefab)
        {
            enemyObject.transform.localScale = Vector3.one * 0.6f;
            var renderer = enemyObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                RendererUtils.SetColor(renderer, enemyColor);
            }
        }

        return enemy;
    }

    private void ReleaseEnemy(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (!usePooling)
        {
            Destroy(enemy.gameObject);
            return;
        }

        enemy.Died -= HandleEnemyDied;
        enemy.transform.SetParent(enemyContainer, false);
        enemy.gameObject.SetActive(false);
        int typeIndex = enemy.TypeId;
        if (useTypeSpawning && typeIndex >= 0 && typeIndex < enemyTypePools.Count)
        {
            enemyTypePools[typeIndex].Enqueue(enemy);
        }
        else
        {
            enemyPool.Enqueue(enemy);
        }
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        if (enemy != null)
        {
            enemy.Died -= HandleEnemyDied;
        }

        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        CheckRoundComplete();
    }

    private void CheckRoundComplete()
    {
        if (!roundActive)
        {
            return;
        }

        if (enemiesSpawned >= enemiesToSpawn && enemiesAlive <= 0)
        {
            roundActive = false;
            RoundCompleted?.Invoke(this);
        }
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
            RendererUtils.SetColor(renderer, spawnMarkerColor);
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
