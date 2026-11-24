using UnityEngine;

public class PlantInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject fruitTransform;
    [SerializeField] private ItemDefinition fruitItemDefinition;
    [SerializeField] private float growTimer = 3 * 60f;
    [SerializeField] private AudioClip pickUpSound;
    private float _timer = 0f;
    private bool _isGrowing = true;

    private float _startGrownChance = 0.3f;

    // Update is called once per frame
    void Update()
    {
        if (_isGrowing)
        {
            _timer += Time.deltaTime;
        }
        if (_timer >= growTimer)
        {
            FinishGrowing();
        }
    }

    void Start()
    {
        // Small chance to start already grown
        if (Random.value < _startGrownChance)
        {
            FinishGrowing();
        }
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        if (_isGrowing || fruitItemDefinition == null || playerInteractor == null || playerInteractor.Inventory == null)
        {
            return;
        }

        Harvest(playerInteractor);
    }

    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        if (_isGrowing)
        {
            return "Plant is growing...";
        }
        return "Press E to pick up the fruit for a potion.";
    }

    private void FinishGrowing()
    {
        _isGrowing = false;
        fruitTransform.SetActive(true);
    }

    private void Harvest(PlayerInteractor playerInteractor)
    {
        playerInteractor.Inventory.AddItem(fruitItemDefinition);
        fruitTransform.SetActive(false);
        _timer = 0f;
        _isGrowing = true;
        playerInteractor.GetComponent<PlayerAudioManager>().PlayPickupSound(pickUpSound);
    }
}
