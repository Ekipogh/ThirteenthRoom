using UnityEngine;

public class PianoInteractable : MonoBehaviour, IHoldInteractable, IScoreReceiver
{
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] AudioSource pianoSong;

    [SerializeField] float scoreReward = 100f;

    [SerializeField] AgitationEmitter agitationEmitter;

    private float _songLength = 2 * 60f + 10f;
    private float _playTimer = 0f;
    private bool _isPlaying = false;


    public void BeginHoldInteract(PlayerInteractor playerInteractor)
    {
        if (!isActiveAndEnabled || _isPlaying) return;

        _playTimer = 0f;
        _isPlaying = true;
        if (pianoSong != null)
        {
            pianoSong.loop = true;
            pianoSong.Play();
        }
    }

    public void BindScoreManager(ScoreManager manager) => scoreManager = manager;

    public void EndHoldInteract(PlayerInteractor playerInteractor)
    {
        if (!_isPlaying) return;

        RewardScore();
        ResetHold();
    }

    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        return "Hold E the piano to play the song";
    }

    public void HoldInteract(PlayerInteractor playerInteractor, float deltaTime)
    {
        if (!isActiveAndEnabled || !_isPlaying ||
            float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f) return;
        _playTimer += deltaTime;

        agitationEmitter?.TickContinuous(true, deltaTime);
    }

    private void ResetHold()
    {
        _isPlaying = false;
        _playTimer = 0f;
        agitationEmitter?.StopContinuous();
        if (pianoSong != null)
        {
            pianoSong.Stop();
        }
    }

    private void RewardScore()
    {
        float score = scoreReward * _playTimer / _songLength;
        scoreManager?.AddScore(score);
    }

    void OnDisable() => ResetHold();
}
