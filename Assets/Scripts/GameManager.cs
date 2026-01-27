using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsGameOver => Instance != null && Instance.isGameOver;

    [Header("References")]
    public TowerHealth tower;
    public TowerAttack towerAttack;
    public EnemySpawner[] spawners;

    [Header("State")]
    [SerializeField] private bool isGameOver;

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
            tower = FindObjectOfType<TowerHealth>();
        }

        if (towerAttack == null)
        {
            towerAttack = FindObjectOfType<TowerAttack>();
        }

        if (spawners == null || spawners.Length == 0)
        {
            spawners = FindObjectsOfType<EnemySpawner>();
        }
    }

    public void GameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;

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

        Debug.Log("Game Over: Tower destroyed.");
    }
}
