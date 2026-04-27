using TMPro;
using UnityEngine;

public class ScoreManger : MonoBehaviour
{
    public float CurrentScore { get; private set; } = 0;

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