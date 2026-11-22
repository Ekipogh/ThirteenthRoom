using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    [Header("State")]
    public GameState currentState = GameState.Playing;

    [Header("Input")]
    [SerializeField] InputActionAsset InputActions;

    [Header("UI")]
    [SerializeField] GameObject GameOverPanel;
    [SerializeField] GameObject HUD;

    [Header("Scene")]
    [SerializeField] string _gameSceneName;

    [Header("Managers")]
    [SerializeField] ScoreManager ScoreManager;
    [SerializeField] MansionGenerator MansionGenerator;
    [SerializeField] MonsterSpawner MonsterSpawner;
    [SerializeField] ItemSpawner ItemSpawner;

    [Header("Monster Aggression")]
    [SerializeField, Range(-99f, 99f)] float monsterDifficultyOffset;
    public MonsterAggression Aggression { get; } = new MonsterAggression();

    public void SetAggressionModifier(string id, float amount, float durationSeconds = 0f) => Aggression.SetModifier(id, amount, durationSeconds);
    public void RemoveAggressionModifier(string id) => Aggression.RemoveModifier(id);
    public void SetMaximumHunt(string id, bool active) => Aggression.SetMaximumHunt(id, active);
    public void SetMonsterDifficultyOffset(float offset)
    {
        monsterDifficultyOffset = Mathf.Clamp(offset, -99f, 99f);
        Aggression.DifficultyOffset = monsterDifficultyOffset;
    }

    readonly HashSet<MonsterController> _agitationListeners = new HashSet<MonsterController>();
    internal void RegisterMonster(MonsterController monster) => _agitationListeners.Add(monster);
    internal void UnregisterMonster(MonsterController monster) => _agitationListeners.Remove(monster);

    public void ReportAgitation(Vector3 origin, AgitationProfile profile)
    {
        if (currentState != GameState.Playing || profile == null || !profile.IsValid ||
            !AgitationProfile.Finite(origin.x) || !AgitationProfile.Finite(origin.y) || !AgitationProfile.Finite(origin.z)) return;
        foreach (MonsterController monster in _agitationListeners)
            if (monster != null) monster.ReceiveAgitation(origin, profile);
    }

    public void ResetAgitation()
    {
        foreach (MonsterController monster in _agitationListeners)
            if (monster != null) monster.ClearInvestigation();
        foreach (AgitationEmitter emitter in FindObjectsByType<AgitationEmitter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (emitter.Owner == this) emitter.StopContinuous();
    }

    void Update()
    {
        Aggression.DifficultyOffset = monsterDifficultyOffset;
        if (currentState == GameState.Playing) Aggression.Advance(Time.deltaTime);
    }

    public void RegisterRitual(RitualInteractable ritual)
    {
        ritual.StateChanged -= OnRitualStateChanged;
        ritual.StateChanged += OnRitualStateChanged;
        Aggression.SetRitualFailed(ritual, !ritual.IsComplete);
    }
    public void UnregisterRitual(RitualInteractable ritual)
    {
        ritual.StateChanged -= OnRitualStateChanged;
        Aggression.UnregisterRitual(ritual);
    }
    void OnRitualStateChanged(RitualInteractable ritual, bool complete) => Aggression.SetRitualFailed(ritual, !complete);

    InputActionMap _playerActionMap;
    MansionModel currentMansion;


    void Awake()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        _playerActionMap = InputActions != null ? InputActions.FindActionMap("Player", throwIfNotFound: false) : null;
        EnableDisableInput(true);
        SetGameOverUiVisible(false);
    }

    void Start()
    {
        if (MansionGenerator != null)
        {
            currentMansion = MansionGenerator.GenerateMansion();
        }
        if (MonsterSpawner != null)
        {
            if (MansionGenerator != null && MansionGenerator.NavigationReady)
                MonsterSpawner.SpawnMonster(currentMansion);
        }
        if (currentMansion != null)
            foreach (Room room in currentMansion.Rooms)
            {
                if (ScoreManager != null) ScoreManager.BindHierarchy(room.gameObject);
                foreach (RitualInteractable ritual in room.GetComponentsInChildren<RitualInteractable>(true))
                    ritual.BindAggressionOwner(this);
            }
        // Includes the player and generated room emitters, even on inactive objects.
        foreach (AgitationEmitter emitter in FindObjectsByType<AgitationEmitter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (emitter.gameObject.scene == gameObject.scene) emitter.BindOwner(this);
        if (ItemSpawner != null)
        {
            ItemSpawner.SpawnInitialItems(currentMansion, ScoreManager);
        }
    }

    public void RestartGame()
    {
        ResetAgitation();
        Aggression.Reset();
        Time.timeScale = 1f;
        currentState = GameState.Playing;
        EnableDisableInput(true);
        SceneManager.LoadScene(GetRestartSceneName());
    }

    void EnableDisableInput(bool enable)
    {
        if (InputActions == null)
        {
            Debug.LogWarning("GameManager: InputActions is not assigned.");
            return;
        }
        if (_playerActionMap == null)
        {
            Debug.LogWarning("GameManager: Player action map was not found.");
            return;
        }

        if (enable)
        {
            _playerActionMap.Enable();
        }
        else
        {
            _playerActionMap.Disable();
        }
    }

    void DisplayScore()
    {
        Transform scoreTextTransform = GameOverPanel != null ? GameOverPanel.transform.Find("ScoreText") : null;
        TextMeshProUGUI scoreText = scoreTextTransform != null ? scoreTextTransform.GetComponent<TextMeshProUGUI>() : null;
        if (scoreText != null)
        {
            float currentScore = ScoreManager != null ? ScoreManager.CurrentScore : 0f;
            scoreText.text = $"Score: {Mathf.FloorToInt(currentScore)}";
        }
    }

    string GetRestartSceneName()
    {
        return string.IsNullOrEmpty(_gameSceneName)
            ? SceneManager.GetActiveScene().name
            : _gameSceneName.Trim();
    }

    void SetGameOverUiVisible(bool visible)
    {
        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(visible);
        }
        if (HUD != null)
        {
            HUD.SetActive(!visible);
        }
    }

    public void OnPlayerDeath()
    {
        if (currentState == GameState.GameOver)
        {
            return;
        }

        currentState = GameState.GameOver;
        Debug.Log("Game Over!");
        Cursor.lockState = CursorLockMode.None;
        SetGameOverUiVisible(true);
        EnableDisableInput(false);
        DisplayScore();
    }
}
