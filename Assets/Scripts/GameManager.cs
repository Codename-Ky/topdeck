using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsGameOver => Instance != null && Instance.isGameOver;
    public static bool IsGameStarted => Instance != null && Instance.hasStarted;

    [Header("References")]
    [SerializeField] private TowerHealth tower;
    [SerializeField] private TowerAttack towerAttack;
    [SerializeField] private EnemySpawner[] spawners;

    [Header("Economy")]
    [SerializeField] private int startingMoney = 200;
    [SerializeField] private int rewardPerKill = 50;

    [Header("Rounds")]
    [SerializeField] private int startingRound = 1;
    [SerializeField] private float roundStartDelay = 2f;
    [SerializeField] private int baseEnemiesPerRound = 3;
    [SerializeField] private int enemiesPerRoundIncrement = 2;
    [SerializeField] private float healthMultiplierPerRound = 0.15f;
    [SerializeField] private float speedMultiplierPerRound = 0.05f;
    [SerializeField] private float damageMultiplierPerRound = 0.1f;
    [SerializeField] private float maxHealthBonus = 2.0f;
    [SerializeField] private float maxSpeedBonus = 0.6f;
    [SerializeField] private float maxDamageBonus = 1.5f;

    [Header("Round Density")]
    [SerializeField] private float spawnIntervalReductionPerRound = 0.03f;
    [SerializeField, Range(0.1f, 1f)] private float minSpawnIntervalMultiplier = 0.35f;
    [SerializeField] private float extraSpawnsPerIntervalPerRound = 0.05f;
    [SerializeField] private float maxExtraSpawnsPerInterval = 2f;

    [Header("Start Screen")]
    [SerializeField] private bool startOnAwake;

    [Header("State")]
    [SerializeField] private bool hasStarted;
    [SerializeField] private bool isGameOver;
    [SerializeField] private int currentRound;
    [SerializeField] private int currentMoney;
    [SerializeField] private bool roundInProgress;

    public bool HasStarted => hasStarted;
    public int CurrentRound => currentRound;
    public int CurrentMoney => currentMoney;
    public bool RoundInProgress => roundInProgress;
    public int StartingRound => startingRound;

    private int spawnersCompleted;
    private bool roundQueued;
    private readonly List<EnemySpawner> activeSpawners = new List<EnemySpawner>();

    public event System.Action<int> MoneyChanged;
    public event System.Action<int, bool> RoundChanged;
    public event System.Action GameStarted;
    public event System.Action GameOverTriggered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (tower == null)
        {
            tower = FindFirstObjectByType<TowerHealth>();
        }

        if (towerAttack == null)
        {
            towerAttack = FindFirstObjectByType<TowerAttack>();
        }

        if (spawners == null || spawners.Length == 0)
        {
            spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        }

        currentMoney = startingMoney;
        currentRound = Mathf.Max(0, startingRound - 1);
    }

    private void Start()
    {
        HookSpawnerEvents();
        if (startOnAwake)
        {
            BeginGame();
        }
    }

    private void OnDestroy()
    {
        CancelInvoke();
        if (spawners != null)
        {
            foreach (var spawner in spawners)
            {
                if (spawner != null)
                {
                    spawner.RoundCompleted -= OnSpawnerRoundComplete;
                }
            }
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void GameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        roundInProgress = false;
        roundQueued = false;
        CancelInvoke(nameof(BeginNextRound));
        RoundChanged?.Invoke(currentRound, roundInProgress);

        if (towerAttack != null)
        {
            towerAttack.enabled = false;
        }

        if (spawners != null)
        {
            foreach (var spawner in spawners)
            {
                if (spawner != null)
                {
                    spawner.enabled = false;
                }
            }
        }

        GameOverTriggered?.Invoke();
        Debug.Log("Game Over: Tower destroyed.");
    }

    public void BeginGame()
    {
        if (hasStarted || isGameOver)
        {
            return;
        }

        hasStarted = true;
        currentRound = Mathf.Max(0, startingRound - 1);
        roundInProgress = false;
        roundQueued = false;
        GameStarted?.Invoke();
        BeginNextRound();
    }

    public bool TryPurchaseDefender(int cost)
    {
        return TrySpend(cost);
    }

    public bool CanAfford(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        return currentMoney >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (currentMoney < amount)
        {
            return false;
        }

        currentMoney -= amount;
        MoneyChanged?.Invoke(currentMoney);
        return true;
    }

    public void OnEnemyKilled()
    {
        if (isGameOver)
        {
            return;
        }

        currentMoney += rewardPerKill;
        MoneyChanged?.Invoke(currentMoney);
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentMoney += amount;
        MoneyChanged?.Invoke(currentMoney);
    }

    private void BeginNextRound()
    {
        if (!hasStarted)
        {
            return;
        }

        if (isGameOver)
        {
            return;
        }

        RefreshSpawners();
        HookSpawnerEvents();
        if (activeSpawners.Count == 0)
        {
            return;
        }

        currentRound = Mathf.Max(1, currentRound + 1);
        roundInProgress = true;
        roundQueued = false;
        spawnersCompleted = 0;
        RoundChanged?.Invoke(currentRound, roundInProgress);

        int roundIndex = Mathf.Max(0, currentRound - 1);
        int totalEnemies = baseEnemiesPerRound + Mathf.Max(0, roundIndex * enemiesPerRoundIncrement);
        float healthBonus = ComputeSoftCappedBonus(roundIndex, healthMultiplierPerRound, maxHealthBonus);
        float speedBonus = ComputeSoftCappedBonus(roundIndex, speedMultiplierPerRound, maxSpeedBonus);
        float damageBonus = ComputeSoftCappedBonus(roundIndex, damageMultiplierPerRound, maxDamageBonus);
        float healthMultiplier = 1f + healthBonus;
        float speedMultiplier = 1f + speedBonus;
        float damageMultiplier = 1f + damageBonus;
        float spawnIntervalMultiplier = ComputeSoftCappedFactorDown(roundIndex, spawnIntervalReductionPerRound, minSpawnIntervalMultiplier);
        int spawnPerInterval = 1 + Mathf.FloorToInt(ComputeSoftCappedBonus(roundIndex, extraSpawnsPerIntervalPerRound, maxExtraSpawnsPerInterval));
        spawnPerInterval = Mathf.Max(1, spawnPerInterval);

        int spawnerCount = activeSpawners.Count;
        int perSpawner = spawnerCount > 0 ? totalEnemies / spawnerCount : totalEnemies;
        int remainder = spawnerCount > 0 ? totalEnemies % spawnerCount : 0;

        for (int i = 0; i < spawnerCount; i++)
        {
            int spawnCount = perSpawner + (i < remainder ? 1 : 0);
            activeSpawners[i].StartRound(currentRound, spawnCount, healthMultiplier, speedMultiplier, damageMultiplier, spawnIntervalMultiplier, spawnPerInterval);
        }
    }

    private static float ComputeSoftCappedBonus(int roundIndex, float perRound, float maxBonus)
    {
        if (roundIndex <= 0 || perRound <= 0f)
        {
            return 0f;
        }

        if (maxBonus <= 0f)
        {
            return roundIndex * perRound;
        }

        float k = perRound / Mathf.Max(0.0001f, maxBonus);
        return maxBonus * (1f - Mathf.Exp(-k * roundIndex));
    }

    private static float ComputeSoftCappedFactorDown(int roundIndex, float perRoundReduction, float minFactor)
    {
        minFactor = Mathf.Clamp(minFactor, 0.05f, 1f);
        if (roundIndex <= 0 || perRoundReduction <= 0f)
        {
            return 1f;
        }

        float maxReduction = 1f - minFactor;
        if (maxReduction <= 0f)
        {
            return 1f;
        }

        float k = perRoundReduction / Mathf.Max(0.0001f, maxReduction);
        float reduction = maxReduction * (1f - Mathf.Exp(-k * roundIndex));
        return Mathf.Clamp(1f - reduction, minFactor, 1f);
    }

    private void OnSpawnerRoundComplete(EnemySpawner spawner)
    {
        if (!this || !isActiveAndEnabled || isGameOver)
        {
            return;
        }

        spawnersCompleted++;
        if (spawnersCompleted < activeSpawners.Count)
        {
            return;
        }

        roundInProgress = false;
        RoundChanged?.Invoke(currentRound, roundInProgress);
        if (!roundQueued)
        {
            roundQueued = true;
            Invoke(nameof(BeginNextRound), roundStartDelay);
        }
    }

    private void HookSpawnerEvents()
    {
        if (spawners == null)
        {
            return;
        }

        foreach (var spawner in spawners)
        {
            if (spawner == null)
            {
                continue;
            }
            spawner.RoundCompleted -= OnSpawnerRoundComplete;
            if (spawner.enabled)
            {
                spawner.RoundCompleted += OnSpawnerRoundComplete;
            }
        }
    }

    private void RefreshSpawners()
    {
        if (spawners == null || spawners.Length == 0)
        {
            spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        }

        activeSpawners.Clear();
        if (spawners == null)
        {
            return;
        }

        foreach (var spawner in spawners)
        {
            if (spawner == null || !spawner.enabled)
            {
                continue;
            }
            activeSpawners.Add(spawner);
        }
    }
}
