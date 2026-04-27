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
    public GameState currentState = GameState.Playing;

    [SerializeField] InputActionAsset InputActions;

    InputActionMap _playerActionMap;

    [SerializeField] GameObject GameOverPanel;

    [SerializeField] GameObject HUD;

    [SerializeField] string _gameSceneName;

    [SerializeField] ScoreManger ScoreManager;


    void Awake()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        _playerActionMap = InputActions != null ? InputActions.FindActionMap("Player", throwIfNotFound: false) : null;
        EnableDisableInput(true);
        SetGameOverUiVisible(false);
    }

    public void RestartGame()
    {
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
