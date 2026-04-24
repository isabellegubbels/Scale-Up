using System;
using UnityEngine;

public class TankManager : MonoBehaviour
{
    public static TankManager instance;

    TankSlotData[] slots = new TankSlotData[TankTier.SlotCount];

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) slots[i] = new TankSlotData { isOwned = i == 0 };
        }
    }

    void Start()
    {
        if (GameManager.instance != null) GameManager.instance.RestoreTankStateToManager(); // Apply saved tank state when entering game scene
    }

    public int SlotCount => TankTier.SlotCount;

    public TankSlotData GetSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    public int GetCapacity(int slotIndex) => TankTier.GetCapacity(slotIndex);

    public int GetMaintenanceCost(int slotIndex) => TankTier.GetMaintenanceCost(slotIndex);

    public bool IsOwned(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        return slot != null && slot.isOwned;
    }

    public void InitializeSlotsFromSave(TankSlotData[] savedSlots)
    {
        if (savedSlots == null || savedSlots.Length != TankTier.SlotCount) return;
        for (int i = 0; i < TankTier.SlotCount; i++) slots[i] = savedSlots[i].Clone();
    }

    public void InitializeDefaultSlots()
    {
        for (int i = 0; i < TankTier.SlotCount; i++)
        {
            slots[i] = new TankSlotData
            {
                isOwned = i == 0,
                lastMaintainedDay = 1,
                speciesId = null,
                fishCount = 0,
                acclimationEndTicks = 0,
                fedToday = false
            };
        }
    }

    public TankSlotData[] GetAllSlotsForSave()
    {
        var copy = new TankSlotData[slots.Length];
        for (int i = 0; i < slots.Length; i++) copy[i] = slots[i].Clone();
        return copy;
    }

    public int GetTotalFishCount()
    {
        int total = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) total += slots[i].fishCount;
        }
        return total;
    }

    public int GetPurchaseCost(int slotIndex) => TankTier.GetPurchaseCost(slotIndex);

    public bool CanPurchase(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        return slot != null && !slot.isOwned && GameManager.instance != null && GameManager.instance.moneyAmount >= TankTier.GetPurchaseCost(slotIndex);
    }

    public bool PurchaseSlot(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || slot.isOwned) return false;
        int cost = TankTier.GetPurchaseCost(slotIndex);
        if (GameManager.instance == null || !GameManager.instance.SpendMoney(cost)) return false;
        slot.isOwned = true;
        GameManager.instance.SaveGame();
        return true;
    }

    public bool FeedTank(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || !slot.isOwned || slot.fishCount <= 0) return false;
        if (FishResourceManager.instance == null || !FishResourceManager.instance.UseFishFood(slot.fishCount)) return false; // Cost = pellets per fish in tank
        slot.fedToday = true;
        GameManager.instance.SaveGame();
        return true;
    }

    public void ResetFedTodayForNewDay()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) slots[i].fedToday = false;
        }
    }

    public const float AcclimationHours = 2f;

    public bool CanTankAcceptFish(int slotIndex, string speciesId, int count)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || !slot.isOwned || count <= 0) return false;
        int capacity = TankTier.GetCapacity(slotIndex);
        if (!string.IsNullOrEmpty(slot.speciesId) && slot.speciesId != speciesId) return false; // One species per tank while it has fish
        if (slot.fishCount + count > capacity) return false;
        return true;
    }

    public int FindAvailableOwnedTank(string speciesId, int count)
    {
        if (string.IsNullOrEmpty(speciesId) || count <= 0) return -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (CanTankAcceptFish(i, speciesId, count)) return i;
        }
        return -1;
    }

    public bool AddFishToTank(int slotIndex, string speciesId, int count)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || !slot.isOwned || count <= 0) return false;
        int capacity = TankTier.GetCapacity(slotIndex);
        if (!string.IsNullOrEmpty(slot.speciesId) && slot.speciesId != speciesId) return false; // One species per tank while it has fish
        if (slot.fishCount + count > capacity) return false;
        slot.speciesId = speciesId;
        slot.fishCount += count;
        slot.acclimationEndTicks = System.DateTime.UtcNow.AddHours(AcclimationHours).Ticks;
        if (GameManager.instance != null) GameManager.instance.SaveGame();
        return true;
    }

    public void SetAcclimationEndTicks(int slotIndex, long ticks)
    {
        var slot = GetSlot(slotIndex);
        if (slot != null) slot.acclimationEndTicks = ticks;
    }

    public float GetAcclimationSecondsRemaining(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || slot.fishCount == 0 || slot.acclimationEndTicks <= 0) return 0f;
        long now = System.DateTime.UtcNow.Ticks;
        if (now >= slot.acclimationEndTicks) return 0f;
        return (float)new System.TimeSpan(slot.acclimationEndTicks - now).TotalSeconds;
    }

    public bool IsAcclimationComplete(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        return slot != null && (slot.fishCount == 0 || slot.acclimationEndTicks <= 0 || System.DateTime.UtcNow.Ticks >= slot.acclimationEndTicks);
    }

    /// <summary>When tank maintenance is implemented, tie this to lastMaintainedDay vs daysOpen.</summary>
    public bool IsTankClean(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        return slot != null && slot.isOwned;
    }

    public bool RemoveFishFromTank(int slotIndex, int count)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || !slot.isOwned || count <= 0 || slot.fishCount < count) return false;
        slot.fishCount -= count;
        if (slot.fishCount <= 0)
        {
            slot.fishCount = 0;
            slot.speciesId = null;
            slot.acclimationEndTicks = 0;
        }
        if (GameManager.instance != null) GameManager.instance.SaveGame();
        return true;
    }
}
