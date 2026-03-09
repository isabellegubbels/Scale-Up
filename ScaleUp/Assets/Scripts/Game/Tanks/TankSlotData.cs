using System;

[Serializable]
public class TankSlotData
{
    public bool isOwned;
    public int lastMaintainedDay;
    public string speciesId;
    public int fishCount;
    public long acclimationEndTicks;
    public bool fedToday;

    public TankSlotData Clone()
    {
        return new TankSlotData
        {
            isOwned = isOwned,
            lastMaintainedDay = lastMaintainedDay,
            speciesId = speciesId,
            fishCount = fishCount,
            acclimationEndTicks = acclimationEndTicks,
            fedToday = fedToday
        };
    }
}
