using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class TowerHud : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerHealth tower;
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private VisualTreeAsset hudLayout;
    [SerializeField] private PanelSettings panelSettings;

    [Header("UI Element Names")]
    [SerializeField] private string towerHealthLabelName = "tower-health";
    [SerializeField] private string moneyLabelName = "money";
    [SerializeField] private string roundLabelName = "round";
    [SerializeField] private string menuOverlayName = "menu-overlay";
    [SerializeField] private string gameOverLabelName = "game-over";
    [SerializeField] private string restartButtonName = "restart-button";
    [SerializeField] private string exitButtonName = "exit-button";
    [SerializeField] private string startOverlayName = "start-overlay";
    [SerializeField] private string startButtonName = "start-button";
    [SerializeField] private string startExitButtonName = "start-exit-button";
    [SerializeField] private string defenderOptionsName = "defender-options";

    private GameManager boundGameManager;
    private TowerHealth boundTower;
    private Label towerHealthLabel;
    private Label moneyLabel;
    private Label roundLabel;
    private VisualElement menuOverlay;
    private Label gameOverLabel;
    private Button restartButton;
    private Button exitButton;
    private VisualElement startOverlay;
    private Button startButton;
    private Button startExitButton;
    private VisualElement defenderOptions;
    private readonly List<Button> defenderButtons = new List<Button>();
    private readonly Dictionary<Button, DefenderDefinition> defenderButtonLookup = new Dictionary<Button, DefenderDefinition>();
    private DefenderPlacementManager boundPlacementManager;

    private void Awake()
    {
        if (tower == null)
        {
            tower = FindFirstObjectByType<TowerHealth>();
        }
        EnsureDocument();
    }

    private void OnEnable()
    {
        EnsureDocument();
        CacheUi();
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
        UnbindButtons();
    }

    private void Bind()
    {
        if (tower == null)
        {
            tower = FindFirstObjectByType<TowerHealth>();
        }

        if (boundTower != null)
        {
            boundTower.HealthChanged -= HandleTowerHealthChanged;
        }

        boundTower = tower;
        if (boundTower != null)
        {
            boundTower.HealthChanged += HandleTowerHealthChanged;
        }

        if (boundGameManager != null)
        {
            boundGameManager.MoneyChanged -= HandleMoneyChanged;
            boundGameManager.RoundChanged -= HandleRoundChanged;
            boundGameManager.GameStarted -= HandleGameStarted;
            boundGameManager.GameOverTriggered -= HandleGameOverTriggered;
        }

        boundGameManager = GameManager.Instance;
        if (boundGameManager != null)
        {
            boundGameManager.MoneyChanged += HandleMoneyChanged;
            boundGameManager.RoundChanged += HandleRoundChanged;
            boundGameManager.GameStarted += HandleGameStarted;
            boundGameManager.GameOverTriggered += HandleGameOverTriggered;
        }

        if (boundPlacementManager != null)
        {
            boundPlacementManager.DefenderSelectionChanged -= HandleDefenderSelectionChanged;
        }

        boundPlacementManager = FindFirstObjectByType<DefenderPlacementManager>();
        if (boundPlacementManager != null)
        {
            boundPlacementManager.DefenderSelectionChanged += HandleDefenderSelectionChanged;
        }

        RefreshAll();
    }

    private void Unbind()
    {
        if (boundTower != null)
        {
            boundTower.HealthChanged -= HandleTowerHealthChanged;
            boundTower = null;
        }

        if (boundGameManager != null)
        {
            boundGameManager.MoneyChanged -= HandleMoneyChanged;
            boundGameManager.RoundChanged -= HandleRoundChanged;
            boundGameManager.GameStarted -= HandleGameStarted;
            boundGameManager.GameOverTriggered -= HandleGameOverTriggered;
            boundGameManager = null;
        }

        if (boundPlacementManager != null)
        {
            boundPlacementManager.DefenderSelectionChanged -= HandleDefenderSelectionChanged;
            boundPlacementManager = null;
        }
    }

    private void RefreshAll()
    {
        if (tower != null)
        {
            HandleTowerHealthChanged(tower.CurrentHealth);
        }

        if (boundGameManager != null)
        {
            HandleMoneyChanged(boundGameManager.CurrentMoney);
            if (boundGameManager.HasStarted)
            {
                HandleRoundChanged(boundGameManager.CurrentRound, boundGameManager.RoundInProgress);
            }
            else
            {
                SetRoundPreview(boundGameManager.StartingRound);
            }
        }

        SetMenuVisible(GameManager.IsGameOver);
        SetStartVisible(!GameManager.IsGameOver && !GameManager.IsGameStarted);
        BuildDefenderButtons();
        UpdateDefenderButtonStates();
    }

    private void HandleTowerHealthChanged(float health)
    {
        if (towerHealthLabel == null)
        {
            return;
        }
        towerHealthLabel.text = "Tower HP: " + Mathf.CeilToInt(health);
    }

    private void HandleMoneyChanged(int money)
    {
        if (moneyLabel == null)
        {
            return;
        }
        moneyLabel.text = "Money: $" + money;
        UpdateDefenderButtonStates();
    }

    private void HandleRoundChanged(int round, bool inProgress)
    {
        if (roundLabel == null)
        {
            return;
        }
        string stateLabel = inProgress ? "" : " (prep)";
        roundLabel.text = "Round " + round + stateLabel;
    }

    private void HandleGameStarted()
    {
        SetStartVisible(false);
    }

    private void SetRoundPreview(int round)
    {
        if (roundLabel == null)
        {
            return;
        }

        int displayRound = Mathf.Max(1, round);
        roundLabel.text = "Round " + displayRound + " (prep)";
    }

    private void HandleGameOverTriggered()
    {
        SetMenuVisible(true);
        SetStartVisible(false);
    }

    private void EnsureDocument()
    {
        if (hudDocument == null)
        {
            hudDocument = FindFirstObjectByType<UIDocument>();
        }

        if (hudDocument == null)
        {
            GameObject documentObject = new GameObject("HUDDocument");
            hudDocument = documentObject.AddComponent<UIDocument>();
        }

        if (panelSettings != null && hudDocument.panelSettings == null)
        {
            hudDocument.panelSettings = panelSettings;
        }

        if (hudLayout != null && hudDocument.visualTreeAsset == null)
        {
            hudDocument.visualTreeAsset = hudLayout;
        }
    }

    private void CacheUi()
    {
        if (hudDocument == null)
        {
            return;
        }

        VisualElement root = hudDocument.rootVisualElement;
        if (root == null)
        {
            return;
        }

        towerHealthLabel = root.Q<Label>(towerHealthLabelName);
        moneyLabel = root.Q<Label>(moneyLabelName);
        roundLabel = root.Q<Label>(roundLabelName);
        menuOverlay = root.Q<VisualElement>(menuOverlayName);
        gameOverLabel = root.Q<Label>(gameOverLabelName);
        restartButton = root.Q<Button>(restartButtonName);
        exitButton = root.Q<Button>(exitButtonName);
        startOverlay = root.Q<VisualElement>(startOverlayName);
        startButton = root.Q<Button>(startButtonName);
        startExitButton = root.Q<Button>(startExitButtonName);
        defenderOptions = root.Q<VisualElement>(defenderOptionsName);

        if (gameOverLabel != null && string.IsNullOrEmpty(gameOverLabel.text))
        {
            gameOverLabel.text = "GAME OVER";
        }

        BindButtons();
        SetMenuVisible(GameManager.IsGameOver);
        SetStartVisible(!GameManager.IsGameOver && !GameManager.IsGameStarted);
        BuildDefenderButtons();
        UpdateDefenderButtonStates();
    }

    private void SetMenuVisible(bool isVisible)
    {
        if (menuOverlay == null)
        {
            return;
        }
        menuOverlay.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetStartVisible(bool isVisible)
    {
        if (startOverlay == null)
        {
            return;
        }
        startOverlay.EnableInClassList("hidden", !isVisible);
    }

    private void BindButtons()
    {
        if (restartButton != null)
        {
            restartButton.clicked -= HandleRestartClicked;
            restartButton.clicked += HandleRestartClicked;
        }

        if (exitButton != null)
        {
            exitButton.clicked -= HandleExitClicked;
            exitButton.clicked += HandleExitClicked;
        }

        if (startButton != null)
        {
            startButton.clicked -= HandleStartClicked;
            startButton.clicked += HandleStartClicked;
        }

        if (startExitButton != null)
        {
            startExitButton.clicked -= HandleExitClicked;
            startExitButton.clicked += HandleExitClicked;
        }
    }

    private void UnbindButtons()
    {
        if (restartButton != null)
        {
            restartButton.clicked -= HandleRestartClicked;
        }

        if (exitButton != null)
        {
            exitButton.clicked -= HandleExitClicked;
        }

        if (startButton != null)
        {
            startButton.clicked -= HandleStartClicked;
        }

        if (startExitButton != null)
        {
            startExitButton.clicked -= HandleExitClicked;
        }
    }

    private void BuildDefenderButtons()
    {
        if (defenderOptions == null)
        {
            return;
        }

        defenderOptions.Clear();
        defenderButtons.Clear();
        defenderButtonLookup.Clear();

        if (boundPlacementManager == null || boundPlacementManager.DefenderTypes == null)
        {
            return;
        }

        foreach (DefenderDefinition definition in boundPlacementManager.DefenderTypes)
        {
            if (definition == null)
            {
                continue;
            }

            Button button = new Button();
            button.text = GetDefenderButtonLabel(definition);
            button.AddToClassList("defender-option");
            button.clicked += () => HandleDefenderButtonClicked(definition);

            defenderOptions.Add(button);
            defenderButtons.Add(button);
            defenderButtonLookup[button] = definition;
        }

        UpdateDefenderButtonStates();
    }

    private void HandleDefenderButtonClicked(DefenderDefinition definition)
    {
        if (boundPlacementManager == null || definition == null)
        {
            return;
        }

        boundPlacementManager.SelectDefender(definition);
        UpdateDefenderButtonStates();
    }

    private void HandleDefenderSelectionChanged(DefenderDefinition definition)
    {
        UpdateDefenderButtonStates();
    }

    private void UpdateDefenderButtonStates()
    {
        if (defenderButtons.Count == 0)
        {
            return;
        }

        int currentMoney = boundGameManager != null ? boundGameManager.CurrentMoney : 0;
        DefenderDefinition selected = boundPlacementManager != null ? boundPlacementManager.SelectedDefender : null;

        foreach (Button button in defenderButtons)
        {
            if (!defenderButtonLookup.TryGetValue(button, out DefenderDefinition definition) || definition == null)
            {
                continue;
            }

            bool isSelected = definition == selected;
            bool canAfford = definition.Cost <= currentMoney;
            button.SetEnabled(canAfford || isSelected);
            button.EnableInClassList("defender-option--selected", isSelected);
            button.text = GetDefenderButtonLabel(definition);
        }
    }

    private string GetDefenderButtonLabel(DefenderDefinition definition)
    {
        if (definition == null)
        {
            return "Defender";
        }

        string name = string.IsNullOrEmpty(definition.DisplayName) ? "Defender" : definition.DisplayName;
        return $"{name} ${definition.Cost}";
    }

    private void HandleStartClicked()
    {
        Time.timeScale = 1f;
        GameManager.Instance?.BeginGame();
        SetStartVisible(false);
    }

    private void HandleRestartClicked()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    private void HandleExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
