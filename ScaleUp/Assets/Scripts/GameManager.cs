using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public int moneyAmount, fishAmount, fishFood, storeRating, daysOpen;
    public float timePlayed;

    public bool fishAcclimationActive;
    public long fishAcclimationEndTicks;

    const string moneyKey = "GM_Money";
    const string fishKey = "GM_Fish";
    const string fishFoodKey = "GM_FishFood";
    const string ratingKey = "GM_Rating";
    const string timeKey = "GM_Time";
    const string daysOpenKey = "GM_DaysOpen";
    const string lastRealKey = "GM_LastRealTicks";
    const string fishAcclActiveKey = "GM_FishAcclActive";
    const string fishAcclEndKey = "GM_FishAcclEndTicks";
    const string tankSlotKeyPrefix = "GM_Tank_";

    const double realSecondsPerGameDay = 12 * 60 * 60; // 12 real hours per in-game day

    long lastRealTimeTicks;
    TankSlotData[] loadedTankSlots;

    [Tooltip("Species id for the 5 fish in the first tank on New Game (e.g. \"100\" for Snail)")]
    [SerializeField] string defaultNewGameSpeciesId = "100";

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadGame();
    }

    void Update()
    {
        timePlayed += Time.deltaTime;
        RefreshRealTimeProgress();
    }

    void RefreshRealTimeProgress()
    {
        DateTime now = DateTime.UtcNow;

        if (lastRealTimeTicks == 0)
        {
            lastRealTimeTicks = now.Ticks;
            return;
        }

        double deltaSeconds = new TimeSpan(now.Ticks - lastRealTimeTicks).TotalSeconds;

        if (deltaSeconds > 0d)
        {
            int additionalDays = Mathf.FloorToInt((float)(deltaSeconds / realSecondsPerGameDay));
            if (additionalDays > 0)
            {
                daysOpen += additionalDays;
                if (TankManager.instance != null) TankManager.instance.ResetFedTodayForNewDay();
            }
        }

        lastRealTimeTicks = now.Ticks;

        if (fishAcclimationActive && now.Ticks >= fishAcclimationEndTicks) fishAcclimationActive = false;
    }

    public void NewGame()
    {
        moneyAmount = 200;
        fishAmount = 5;
        fishFood = 5;
        storeRating = 0;
        timePlayed = 0f;
        daysOpen = 1;

        fishAcclimationActive = false;
        fishAcclimationEndTicks = 0;

        lastRealTimeTicks = DateTime.UtcNow.Ticks;
        loadedTankSlots = GetDefaultTankSlots();
    }

    TankSlotData[] GetDefaultTankSlots()
    {
        var arr = new TankSlotData[TankTier.SlotCount];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = new TankSlotData
            {
                isOwned = i == 0,
                lastMaintainedDay = 1,
                speciesId = i == 0 ? defaultNewGameSpeciesId : null,
                fishCount = i == 0 ? 5 : 0,
                acclimationEndTicks = 0,
                fedToday = false
            };
        }
        return arr;
    }

    public void SaveGame()
    {
        RefreshRealTimeProgress();

        if (TankManager.instance != null)
        {
            loadedTankSlots = TankManager.instance.GetAllSlotsForSave();
            fishAmount = TankManager.instance.GetTotalFishCount();
        }
        if (loadedTankSlots != null)
        {
            for (int i = 0; i < loadedTankSlots.Length && i < TankTier.SlotCount; i++)
                PlayerPrefs.SetString(tankSlotKeyPrefix + i, JsonUtility.ToJson(loadedTankSlots[i]));
        }

        int foodToSave = FishResourceManager.instance != null ? FishResourceManager.instance.GetFishFood() : fishFood;
        PlayerPrefs.SetInt(moneyKey, moneyAmount);
        PlayerPrefs.SetInt(fishKey, fishAmount);
        PlayerPrefs.SetInt(fishFoodKey, foodToSave);
        PlayerPrefs.SetInt(ratingKey, storeRating);
        PlayerPrefs.SetFloat(timeKey, timePlayed);
        PlayerPrefs.SetInt(daysOpenKey, daysOpen);
        PlayerPrefs.SetString(lastRealKey, lastRealTimeTicks.ToString());

        PlayerPrefs.SetInt(fishAcclActiveKey, fishAcclimationActive ? 1 : 0);
        PlayerPrefs.SetString(fishAcclEndKey, fishAcclimationEndTicks.ToString());

        PlayerPrefs.Save();
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey(moneyKey))
        {
            NewGame();
            SaveGame();
            return;
        }

        moneyAmount = PlayerPrefs.GetInt(moneyKey, 0);
        fishAmount = PlayerPrefs.GetInt(fishKey, 0);
        fishFood = PlayerPrefs.GetInt(fishFoodKey, 0);
        if (FishResourceManager.instance != null) FishResourceManager.instance.SetFishFood(fishFood);
        storeRating = PlayerPrefs.GetInt(ratingKey, 0);
        timePlayed = PlayerPrefs.GetFloat(timeKey, 0f);
        daysOpen = PlayerPrefs.GetInt(daysOpenKey, 0);
        if (daysOpen <= 0) daysOpen = 1;

        string lastTicksString = PlayerPrefs.GetString(lastRealKey, "0");
        if (!long.TryParse(lastTicksString, out lastRealTimeTicks)) lastRealTimeTicks = DateTime.UtcNow.Ticks;

        int fishAcclInt = PlayerPrefs.GetInt(fishAcclActiveKey, 0);
        fishAcclimationActive = fishAcclInt == 1;

        string fishTicksString = PlayerPrefs.GetString(fishAcclEndKey, "0");
        if (!long.TryParse(fishTicksString, out fishAcclimationEndTicks)) fishAcclimationEndTicks = 0;

        loadedTankSlots = new TankSlotData[TankTier.SlotCount];
        var defaults = GetDefaultTankSlots();
        for (int i = 0; i < TankTier.SlotCount; i++)
        {
            string json = PlayerPrefs.GetString(tankSlotKeyPrefix + i, "");
            if (string.IsNullOrEmpty(json)) loadedTankSlots[i] = defaults[i].Clone();
            else loadedTankSlots[i] = JsonUtility.FromJson<TankSlotData>(json);
        }

        RefreshRealTimeProgress();
    }

    public void RestoreTankStateToManager()
    {
        if (TankManager.instance == null || loadedTankSlots == null) return;
        TankManager.instance.InitializeSlotsFromSave(loadedTankSlots);
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        moneyAmount += amount;
        SaveGame();
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return true;
        if (moneyAmount < amount)
        {
            Debug.Log("purchase failed, REMINDER: implement user feedback for failed purchases");
            return false;
        }
        moneyAmount -= amount;
        SaveGame();
        Debug.Log("purchase succeeded, REMINDER: implement user feedback for successfull purchases");
        return true;
    }

    public void AddFishFood(int amount)
    {
        if (FishResourceManager.instance != null) FishResourceManager.instance.AddFishFood(amount);
        else
        {
            if (amount <= 0) return;
            fishFood += amount;
            SaveGame();
        }
    }

    public bool UseFishFood(int amount)
    {
        if (FishResourceManager.instance != null) return FishResourceManager.instance.UseFishFood(amount);
        if (amount <= 0) return true;
        if (fishFood < amount) return false;
        fishFood -= amount;
        SaveGame();
        return true;
    }

    public void StartFishAcclimationHours(float hours)
    {
        if (hours <= 0f)
        {
            fishAcclimationActive = false;
            fishAcclimationEndTicks = 0;
            return;
        }

        DateTime now = DateTime.UtcNow;
        fishAcclimationActive = true;
        fishAcclimationEndTicks = now.AddHours(hours).Ticks;
        SaveGame();
    }

    public bool IsFishAcclimationComplete()
    {
        if (!fishAcclimationActive) return false;
        return DateTime.UtcNow.Ticks >= fishAcclimationEndTicks;
    }

    public float GetFishAcclimationSecondsRemaining()
    {
        if (!fishAcclimationActive) return 0f;

        double remaining = new TimeSpan(fishAcclimationEndTicks - DateTime.UtcNow.Ticks).TotalSeconds;
        if (remaining <= 0d)
        {
            fishAcclimationActive = false;
            return 0f;
        }

        return (float)remaining;
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) SaveGame();
    }

    void OnApplicationQuit() => SaveGame();
}
