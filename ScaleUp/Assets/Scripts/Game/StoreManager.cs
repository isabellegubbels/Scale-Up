using System;
using UnityEngine;

/// <summary>
/// Central hub for ordering stock, selling fish, and money. Replaces MoneyManager + FishPurchaseController.
/// </summary>
public class StoreManager : MonoBehaviour
{
    public static StoreManager instance;

    [Header("Fish food order")]
    [SerializeField] int fishFoodCost = 10;
    [SerializeField] int fishFoodPellets = 100;

    [Header("Employee")]
    [SerializeField] int employeeHireCost = 780;
    [SerializeField] int employeeContractDays = 30;

    [Header("Good Reviews")]
    [SerializeField] int goodReviewsCost = 500;
    [SerializeField] int goodReviewsRepGain = 100;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public int GetBalance() => GameManager.instance != null ? GameManager.instance.moneyAmount : 0;

    public bool CanAfford(int amount) => GameManager.instance != null && GameManager.instance.moneyAmount >= amount;

    public void AddMoney(int amount)
    {
        if (GameManager.instance != null) GameManager.instance.AddMoney(amount);
    }

    public bool SpendMoney(int amount)
    {
        return GameManager.instance != null && GameManager.instance.SpendMoney(amount);
    }

    public bool IsSpeciesUnlocked(string speciesId)
    {
        if (string.IsNullOrEmpty(speciesId) || FishSpeciesRegistry.instance == null) return false;
        var species = FishSpeciesRegistry.instance.GetSpecies(speciesId);
        if (species == null) return false;
        if (species.startsUnlocked) return true;
        if (species.unlockAfterSales <= 0) return false;
        int sold = GameManager.instance != null ? GameManager.instance.totalFishSold : 0;
        return sold >= species.unlockAfterSales;
    }

    public bool OrderFish(string speciesId, int count)
    {
        if (string.IsNullOrEmpty(speciesId) || count <= 0) return false;
        if (!IsSpeciesUnlocked(speciesId)) return false;
        if (TankManager.instance == null || GameManager.instance == null) return false;

        var species = FishSpeciesRegistry.instance != null ? FishSpeciesRegistry.instance.GetSpecies(speciesId) : null;
        if (species == null) return false;

        int totalCost = species.purchaseCost * count;
        int targetSlot = TankManager.instance.FindAvailableOwnedTank(speciesId, count);
        if (targetSlot < 0) return false;
        if (!CanAfford(totalCost)) return false;
        if (!SpendMoney(totalCost)) return false;

        return TankManager.instance.AddFishToTank(targetSlot, speciesId, count);
    }

    public bool OrderFood()
    {
        if (GameManager.instance == null || FishResourceManager.instance == null) return false;
        if (!CanAfford(fishFoodCost)) return false;
        if (!SpendMoney(fishFoodCost)) return false;
        FishResourceManager.instance.AddFishFood(fishFoodPellets);
        return true;
    }

    public bool OrderTank(int slotIndex) => TankManager.instance != null && TankManager.instance.PurchaseSlot(slotIndex);

    public bool OrderDecor(string decorId)
    {
        if (string.IsNullOrEmpty(decorId) || GameManager.instance == null) return false;
        if (DecorRegistry.instance == null) return false;
        if (GameManager.instance.IsDecorOwned(decorId)) return false;
        var data = DecorRegistry.instance != null ? DecorRegistry.instance.GetDecor(decorId) : null;
        if (data == null) return false;
        if (!CanAfford(data.cost)) return false;
        if (!SpendMoney(data.cost)) return false;
        GameManager.instance.AddDecorOwned(decorId);
        if (DecorRegistry.instance != null) DecorRegistry.instance.ApplyOwnedDecor();
        return true;
    }

    public bool HireEmployee()
    {
        if (GameManager.instance == null) return false;
        if (!CanAfford(employeeHireCost)) return false;
        if (!SpendMoney(employeeHireCost)) return false;
        DateTime baseUtc = DateTime.UtcNow;
        if (IsEmployeeActive() && GameManager.instance.employeeContractEndUtcTicks > baseUtc.Ticks)
            baseUtc = new DateTime(GameManager.instance.employeeContractEndUtcTicks, DateTimeKind.Utc);
        long endTicks = baseUtc.AddDays(employeeContractDays).Ticks;
        GameManager.instance.SetEmployeeHired(true, endTicks);
        return true;
    }

    public bool BuyGoodReviews()
    {
        if (GameManager.instance == null) return false;
        if (!CanAfford(goodReviewsCost)) return false;
        if (!SpendMoney(goodReviewsCost)) return false;
        GameManager.instance.storeRating += goodReviewsRepGain;
        GameManager.instance.SaveGame();
        return true;
    }

    public bool FranchiseProperty()
    {
        // to be implemented later on
        return false;
    }

    public bool IsEmployeeActive()
    {
        if (GameManager.instance == null || !GameManager.instance.employeeHiredActive) return false;
        return DateTime.UtcNow.Ticks < GameManager.instance.employeeContractEndUtcTicks;
    }

    /// <summary>Customer sale: remove fish from first suitable tank, add money, bump totalFishSold.</summary>
    public bool SellFish(string speciesId, int count, int salePrice)
    {
        if (string.IsNullOrEmpty(speciesId) || count <= 0 || TankManager.instance == null || GameManager.instance == null) return false;

        int slot = FindSellableTankSlot(speciesId, count);
        if (slot < 0) return false;

        if (!TankManager.instance.RemoveFishFromTank(slot, count)) return false;
        if (salePrice > 0) GameManager.instance.AddMoney(salePrice);
        GameManager.instance.AddTotalFishSold(count);
        return true;
    }

    int FindSellableTankSlot(string speciesId, int count)
    {
        for (int i = 0; i < TankManager.instance.SlotCount; i++)
        {
            var slot = TankManager.instance.GetSlot(i);
            if (slot == null || !slot.isOwned) continue;
            if (string.IsNullOrEmpty(slot.speciesId) || slot.speciesId != speciesId) continue;
            if (slot.fishCount < count) continue;
            if (!TankManager.instance.IsAcclimationComplete(i)) continue;
            if (!TankManager.instance.IsTankClean(i)) continue;
            return i;
        }
        return -1;
    }
}
