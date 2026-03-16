using UnityEngine;

public class FishPurchaseController : MonoBehaviour
{
    [SerializeField] string speciesId;
    [SerializeField] int purchaseCount = 5;

    public void PurchaseSelectedFish()
    {
        if (string.IsNullOrEmpty(speciesId) || purchaseCount <= 0) return;
        if (TankManager.instance == null || MoneyManager.instance == null) return;

        var species = FishSpeciesRegistry.instance != null ? FishSpeciesRegistry.instance.GetSpecies(speciesId) : null;
        if (species == null) return;

        int totalCost = species.purchaseCost * purchaseCount;
        int targetSlot = TankManager.instance.FindAvailableOwnedTank(speciesId, purchaseCount);
        if (targetSlot < 0) return; // No owned tank can accept this purchase

        if (!MoneyManager.instance.CanAfford(totalCost)) return;
        if (!MoneyManager.instance.SpendMoney(totalCost)) return;

        TankManager.instance.AddFishToTank(targetSlot, speciesId, purchaseCount);
    }
}

