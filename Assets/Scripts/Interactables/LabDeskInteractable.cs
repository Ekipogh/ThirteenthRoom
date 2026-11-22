using UnityEngine;

public class LabDeskInteractable : MonoBehaviour, IHoldInteractable, IScoreReceiver
{
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] float scoreOnComplete = 50f;
    [SerializeField] ItemDefinition berryDef;
    [SerializeField] ItemDefinition potionDef;
    [SerializeField] AudioSource PotionMakingAudio;
    [SerializeField, Min(0.01f)] float _interactionTime = 5f;

    PlayerInteractor _activePlayer;
    float _elapsed;
    bool _isMakingPotion;
    bool _completed;

    public void BindScoreManager(ScoreManager manager) => scoreManager = manager;

    bool CanBrew(PlayerInteractor player) => isActiveAndEnabled &&
        player != null && player.Inventory != null && berryDef != null &&
        potionDef != null && player.Inventory.HasItem(berryDef);

    public void BeginHoldInteract(PlayerInteractor playerInteractor)
    {
        if (_activePlayer != null || !CanBrew(playerInteractor)) return;
        _activePlayer = playerInteractor;
        _elapsed = 0f;
        _completed = false;
        _isMakingPotion = true;
        if (PotionMakingAudio != null)
        {
            PotionMakingAudio.loop = true;
            PotionMakingAudio.Play();
        }
    }

    public void HoldInteract(PlayerInteractor playerInteractor, float deltaTime)
    {
        if (!_isMakingPotion || playerInteractor != _activePlayer) return;
        if (!CanBrew(playerInteractor))
        {
            ResetHold();
            return;
        }
        if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f) return;
        _elapsed += deltaTime;
        if (_elapsed < Mathf.Max(0.01f, _interactionTime)) return;

        // Finish this press before rewards so subsequent callbacks cannot repeat it.
        _isMakingPotion = false;
        StopAudio();
        if (!playerInteractor.Inventory.RemoveItem(berryDef)) return;
        playerInteractor.Inventory.AddItem(potionDef);
        _completed = true;
        if (scoreManager != null) scoreManager.AddScore(scoreOnComplete);
    }

    public void EndHoldInteract(PlayerInteractor playerInteractor)
    {
        if (playerInteractor == _activePlayer) ResetHold();
    }

    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        if (_completed && playerInteractor == _activePlayer)
            return "Potion made! Release E to brew again";
        if (berryDef == null || potionDef == null) return "Potion making is unavailable";
        if (!CanBrew(playerInteractor)) return "You need berries to make a potion";
        if (_isMakingPotion)
            return $"Hold E to make a potion ({Mathf.FloorToInt(Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _interactionTime)) * 100f)}%)";
        return "Hold E to make a potion";
    }

    void OnDisable() => ResetHold();

    void ResetHold()
    {
        _activePlayer = null;
        _elapsed = 0f;
        _isMakingPotion = false;
        _completed = false;
        StopAudio();
    }

    void StopAudio()
    {
        if (PotionMakingAudio != null) PotionMakingAudio.Stop();
    }
}
