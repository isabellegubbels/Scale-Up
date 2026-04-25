using UnityEngine;

public class TankMaintenanceManager : MonoBehaviour
{
    public static TankMaintenanceManager instance;

    [SerializeField] int employeeAutoMaintenanceCostPerTank = 5;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public bool PayMaintenance(int slotIndex)
    {
        if (TankManager.instance == null || GameManager.instance == null) return false;
        var slot = TankManager.instance.GetSlot(slotIndex);
        if (slot == null || !slot.isOwned) return false;

        int maintenanceCost = TankManager.instance.GetMaintenanceCost(slotIndex);
        if (maintenanceCost <= 0) return false;
        if (!GameManager.instance.SpendMoney(maintenanceCost)) return false;

        return TankManager.instance.MaintainTank(slotIndex);
    }

    public void OnNewDay()
    {
        if (TankManager.instance == null || GameManager.instance == null || StoreManager.instance == null) return;
        if (!StoreManager.instance.IsEmployeeActive()) return;
        TryAutoMaintainDirtyTanks();
    }

    public bool TryAutoMaintainDirtyTanks()
    {
        if (TankManager.instance == null || GameManager.instance == null) return false;
        bool anyUpdated = false;
        for (int i = 0; i < TankManager.instance.SlotCount; i++)
        {
            var slot = TankManager.instance.GetSlot(i);
            if (slot == null || !slot.isOwned) continue;
            if (TankManager.instance.IsTankClean(i)) continue;
            if (!GameManager.instance.SpendMoney(employeeAutoMaintenanceCostPerTank)) continue;
            if (TankManager.instance.MaintainTankWithoutSaving(i)) anyUpdated = true;
        }

        if (anyUpdated) GameManager.instance.SaveGame();
        return anyUpdated;
    }
}
