using UnityEngine;

public class FishResourceManager : MonoBehaviour
{
    public static FishResourceManager instance;

    int fishFood;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        if (GameManager.instance != null) fishFood = GameManager.instance.fishFood; // Sync from persisted value when entering game scene
    }

    public int GetFishFood() => fishFood;

    public void SetFishFood(int amount)
    {
        fishFood = amount < 0 ? 0 : amount;
    }

    public void AddFishFood(int amount)
    {
        if (amount <= 0) return;
        fishFood += amount;
        if (GameManager.instance != null) GameManager.instance.SaveGame();
    }

    public bool UseFishFood(int amount)
    {
        if (amount <= 0) return true;
        if (fishFood < amount) return false;
        fishFood -= amount;
        if (GameManager.instance != null) GameManager.instance.SaveGame();
        return true;
    }
}
