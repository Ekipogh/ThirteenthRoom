using TMPro;
using UnityEngine;

public interface IScoreReceiver
{
    void BindScoreManager(ScoreManager manager);
}

public class ScoreManager : MonoBehaviour
{
    public float CurrentScore { get; private set; } = 0;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI ScoreText;

    public void BindHierarchy(GameObject root)
    {
        foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            if (component is IScoreReceiver receiver)
                receiver.BindScoreManager(this);
    }

    public void AddScore(float amount)
    {
        CurrentScore += amount;
        if (ScoreText != null)
        {
            ScoreText.text = $"Score: {Mathf.FloorToInt(CurrentScore)}";
        }
    }
}
