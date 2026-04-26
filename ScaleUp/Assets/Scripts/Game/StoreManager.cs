using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Central hub for ordering stock, selling fish, and money. Replaces MoneyManager + FishPurchaseController.
/// </summary>
public class StoreManager : MonoBehaviour
{
    public static StoreManager instance;
    const string reasonNotEnoughMoneyForItem = "not enough money for item";
    const string reasonNotUnlockedYet = "not unlocked yet";
    const string reasonTanksAllFull = "all tanks are full";
    const string reasonNoSuitableTank = "can't fit fish in available tanks";
    const string reasonAlreadyOwned = "already owned";

    [Header("Fish food order")]
    [SerializeField] int fishFoodCost = 10;
    [SerializeField] int fishFoodPellets = 100;

    [Header("Employee")]
    [SerializeField] int employeeHireCost = 780;
    [SerializeField] int employeeContractDays = 30;

    [Header("Good Reviews")]
    [SerializeField] int goodReviewsCost = 500;
    [SerializeField] float goodReviewsRepGain = 0.1f;
    [Header("Purchase Feedback")]
    [SerializeField] GameObject purchasedFeedbackRoot;
    [SerializeField] TMP_Text purchasedFeedbackText;
    [SerializeField] GameObject notPurchasedFeedbackRoot;
    [SerializeField] TMP_Text notPurchasedFeedbackText;
    [SerializeField] float feedbackVisibleSeconds = 2.5f;
    [SerializeField] bool dismissFeedbackOnTap = true;

    Coroutine feedbackHideCoroutine;
    float feedbackShownAtTime = -10f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Update()
    {
        if (!dismissFeedbackOnTap) return;
        if (!IsAnyFeedbackVisible()) return;
        // Ignore the same tap that created the popup and dismiss on release for more stable mobile behavior.
        if (Time.unscaledTime - feedbackShownAtTime < 0.1f) return;
        if (!WasPointerReleasedThisFrame()) return;
        HideFeedbackImmediately();
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

    public List<string> GetUnlockedSpeciesIds()
    {
        var unlockedSpeciesIds = new List<string>();
        if (FishSpeciesRegistry.instance == null) return unlockedSpeciesIds;

        for (int i = 0; i < FishSpeciesRegistry.instance.SpeciesCount; i++)
        {
            var species = FishSpeciesRegistry.instance.GetSpeciesAt(i);
            if (species == null || string.IsNullOrEmpty(species.speciesId)) continue;
            if (!IsSpeciesUnlocked(species.speciesId)) continue;
            unlockedSpeciesIds.Add(species.speciesId);
        }

        return unlockedSpeciesIds;
    }

    public bool OrderFish(string speciesId, int count)
    {
        if (string.IsNullOrEmpty(speciesId) || count <= 0) return false;
        if (!IsSpeciesUnlocked(speciesId))
        {
            ShowNotPurchasedFeedback("fish order", reasonNotUnlockedYet);
            return false;
        }
        if (TankManager.instance == null || GameManager.instance == null) return false;

        var species = FishSpeciesRegistry.instance != null ? FishSpeciesRegistry.instance.GetSpecies(speciesId) : null;
        if (species == null) return false;
        string itemName = string.IsNullOrEmpty(species.displayName) ? "fish" : species.displayName;

        int totalCost = species.purchaseCost * count;
        int targetSlot = TankManager.instance.FindAvailableOwnedTank(speciesId, count);
        if (targetSlot < 0)
        {
            ShowNotPurchasedFeedback(itemName, GetFishTankFailureReason(speciesId, count));
            return false;
        }
        if (!CanAfford(totalCost))
        {
            ShowNotPurchasedFeedback(itemName, reasonNotEnoughMoneyForItem);
            return false;
        }
        if (!SpendMoney(totalCost))
        {
            ShowNotPurchasedFeedback(itemName, reasonNotEnoughMoneyForItem);
            return false;
        }

        bool succeeded = TankManager.instance.AddFishToTank(targetSlot, speciesId, count);
        if (succeeded) ShowPurchasedFeedback(itemName);
        else ShowNotPurchasedFeedback(itemName, GetFishTankFailureReason(speciesId, count));
        return succeeded;
    }

    public bool OrderFood()
    {
        if (GameManager.instance == null || FishResourceManager.instance == null) return false;
        if (!CanAfford(fishFoodCost))
        {
            ShowNotPurchasedFeedback("fish food", reasonNotEnoughMoneyForItem);
            return false;
        }
        if (!SpendMoney(fishFoodCost))
        {
            ShowNotPurchasedFeedback("fish food", reasonNotEnoughMoneyForItem);
            return false;
        }
        FishResourceManager.instance.AddFishFood(fishFoodPellets);
        ShowPurchasedFeedback("fish food");
        return true;
    }

    public bool OrderTank(int slotIndex)
    {
        if (TankManager.instance == null) return false;
        int capacity = TankTier.GetCapacity(slotIndex);
        string itemName = capacity > 0 ? $"tank ({capacity})" : "tank";
        int tankCost = TankManager.instance.GetPurchaseCost(slotIndex);

        if (!CanAfford(tankCost))
        {
            ShowNotPurchasedFeedback(itemName, reasonNotEnoughMoneyForItem);
            return false;
        }

        bool succeeded = TankManager.instance.PurchaseSlot(slotIndex);
        if (succeeded) ShowPurchasedFeedback(itemName);
        else ShowNotPurchasedFeedback(itemName, reasonNoSuitableTank);
        return succeeded;
    }

    public bool OrderDecor(string decorId)
    {
        if (string.IsNullOrEmpty(decorId) || GameManager.instance == null) return false;
        if (DecorRegistry.instance == null) return false;
        if (GameManager.instance.IsDecorOwned(decorId))
        {
            ShowNotPurchasedFeedback("decor", reasonAlreadyOwned);
            return false;
        }
        var data = DecorRegistry.instance != null ? DecorRegistry.instance.GetDecor(decorId) : null;
        if (data == null) return false;
        string itemName = string.IsNullOrEmpty(data.displayName) ? "decor" : data.displayName;
        if (!CanAfford(data.cost))
        {
            ShowNotPurchasedFeedback(itemName, reasonNotEnoughMoneyForItem);
            return false;
        }
        if (!SpendMoney(data.cost))
        {
            ShowNotPurchasedFeedback(itemName, reasonNotEnoughMoneyForItem);
            return false;
        }
        GameManager.instance.AddDecorOwned(decorId);
        if (DecorRegistry.instance != null) DecorRegistry.instance.ApplyOwnedDecor();
        ShowPurchasedFeedback(itemName);
        return true;
    }

    public bool HireEmployee()
    {
        if (GameManager.instance == null) return false;
        if (!CanAfford(employeeHireCost))
        {
            ShowNotPurchasedFeedback("employee", reasonNotEnoughMoneyForItem);
            return false;
        }
        if (!SpendMoney(employeeHireCost))
        {
            ShowNotPurchasedFeedback("employee", reasonNotEnoughMoneyForItem);
            return false;
        }
        DateTime baseUtc = DateTime.UtcNow;
        if (IsEmployeeActive() && GameManager.instance.employeeContractEndUtcTicks > baseUtc.Ticks)
            baseUtc = new DateTime(GameManager.instance.employeeContractEndUtcTicks, DateTimeKind.Utc);
        long endTicks = baseUtc.AddDays(employeeContractDays).Ticks;
        GameManager.instance.SetEmployeeHired(true, endTicks);
        if (TankMaintenanceManager.instance != null) TankMaintenanceManager.instance.TryAutoMaintainDirtyTanks();
        ShowPurchasedFeedback("employee");
        return true;
    }

    public bool BuyGoodReviews()
    {
        if (GameManager.instance == null) return false;
        if (!CanAfford(goodReviewsCost))
        {
            ShowNotPurchasedFeedback("good reviews", reasonNotEnoughMoneyForItem);
            return false;
        }
        if (!SpendMoney(goodReviewsCost))
        {
            ShowNotPurchasedFeedback("good reviews", reasonNotEnoughMoneyForItem);
            return false;
        }
        GameManager.instance.AddStoreRating(goodReviewsRepGain);
        ShowPurchasedFeedback("good reviews");
        return true;
    }

    public bool FranchiseProperty()
    {
        // to be implemented later on
        ShowNotPurchasedFeedback("franchise property", "not available");
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

    string GetFishTankFailureReason(string speciesId, int count)
    {
        if (TankManager.instance == null || count <= 0) return reasonNoSuitableTank;
        bool hasCompatibleTank = false;
        for (int i = 0; i < TankManager.instance.SlotCount; i++)
        {
            var slot = TankManager.instance.GetSlot(i);
            if (slot == null || !slot.isOwned) continue;
            bool speciesCompatible = string.IsNullOrEmpty(slot.speciesId) || slot.speciesId == speciesId;
            if (!speciesCompatible) continue;
            hasCompatibleTank = true;
            int capacity = TankManager.instance.GetCapacity(i);
            if (slot.fishCount + count <= capacity) return reasonNoSuitableTank;
        }
        return hasCompatibleTank ? reasonTanksAllFull : reasonNoSuitableTank;
    }

    void ShowPurchasedFeedback(string itemName)
    {
        string normalizedName = NormalizeItemName(itemName);
        if (purchasedFeedbackText != null) purchasedFeedbackText.text = $"{normalizedName} purchased!";
        if (purchasedFeedbackRoot != null)
        {
            purchasedFeedbackRoot.SetActive(true);
            purchasedFeedbackRoot.transform.SetAsLastSibling();
        }
        if (notPurchasedFeedbackRoot != null) notPurchasedFeedbackRoot.SetActive(false);
        feedbackShownAtTime = Time.unscaledTime;
        StartFeedbackHideCountdown();
    }

    void ShowNotPurchasedFeedback(string itemName, string reason)
    {
        string normalizedName = NormalizeItemName(itemName);
        string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "not purchased" : reason.Trim();
        if (notPurchasedFeedbackText != null) notPurchasedFeedbackText.text = $"{normalizedReason} {normalizedName} not purchased";
        if (notPurchasedFeedbackRoot != null)
        {
            notPurchasedFeedbackRoot.SetActive(true);
            notPurchasedFeedbackRoot.transform.SetAsLastSibling();
        }
        if (purchasedFeedbackRoot != null) purchasedFeedbackRoot.SetActive(false);
        feedbackShownAtTime = Time.unscaledTime;
        StartFeedbackHideCountdown();
    }

    void StartFeedbackHideCountdown()
    {
        if (feedbackHideCoroutine != null) StopCoroutine(feedbackHideCoroutine);
        feedbackHideCoroutine = StartCoroutine(HideFeedbackAfterDelay());
    }

    System.Collections.IEnumerator HideFeedbackAfterDelay()
    {
        float duration = Mathf.Max(0.1f, feedbackVisibleSeconds);
        yield return new WaitForSeconds(duration);
        HideFeedbackImmediately();
    }

    void HideFeedbackImmediately()
    {
        if (feedbackHideCoroutine != null)
        {
            StopCoroutine(feedbackHideCoroutine);
            feedbackHideCoroutine = null;
        }
        if (purchasedFeedbackRoot != null) purchasedFeedbackRoot.SetActive(false);
        if (notPurchasedFeedbackRoot != null) notPurchasedFeedbackRoot.SetActive(false);
    }

    bool IsAnyFeedbackVisible()
    {
        bool purchasedVisible = purchasedFeedbackRoot != null && purchasedFeedbackRoot.activeInHierarchy;
        bool notPurchasedVisible = notPurchasedFeedbackRoot != null && notPurchasedFeedbackRoot.activeInHierarchy;
        return purchasedVisible || notPurchasedVisible;
    }

    bool WasPointerReleasedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return true;
        return false;
    }

    string NormalizeItemName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return "item";
        return itemName.Trim().ToLowerInvariant();
    }
}
