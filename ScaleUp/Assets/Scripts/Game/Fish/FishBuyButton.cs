using UnityEngine;

/// <summary>Per-variant fish order. Button OnClick → <see cref="Buy"/>.</summary>
public class FishBuyButton : MonoBehaviour
{
    [SerializeField] FishSpeciesData species;
    [SerializeField] int count = 5;

    public void Buy()
    {
        if (species == null || string.IsNullOrEmpty(species.speciesId) || StoreManager.instance == null) return;
        StoreManager.instance.OrderFish(species.speciesId, count);
    }
}
