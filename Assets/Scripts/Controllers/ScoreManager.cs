using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public float CurrentScore { get; private set; } = 0;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI ScoreText;

    public void AddScore(float amount)
    {
        CurrentScore += amount;
        if (ScoreText != null)
        {
            ScoreText.text = $"Score: {Mathf.FloorToInt(CurrentScore)}";
        }
    }
}
