public static class TankTier
{
    public const int SlotCount = 13;
    // Layout: slots 0–2 = 5 fish, 3–6 = 10, 7–11 = 15, 12 = 25.

    public static int GetCapacity(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return 0;
        if (slotIndex < 3) return 5;
        if (slotIndex < 7) return 10;
        if (slotIndex < 12) return 15;
        return 25;
    }

    public static int GetMaintenanceCost(int slotIndex)
    {
        int cap = GetCapacity(slotIndex);
        if (cap == 5) return 5;
        if (cap == 10) return 10;
        if (cap == 15) return 15;
        if (cap == 25) return 25;
        return 0;
    }

    public static int GetPurchaseCost(int slotIndex)
    {
        int cap = GetCapacity(slotIndex);
        if (cap == 5) return 300;
        if (cap == 10) return 500;
        if (cap == 15) return 800;
        if (cap == 25) return 1300;
        return 0;
    }
}
