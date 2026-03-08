using System;
using UnityEngine;
using TMPro;

public class SceneUI : MonoBehaviour
{
    [SerializeField] TMP_Text daysOpenText, fishAcclimationText, moneyText, fishText, fishFoodText;

    void Start() => RefreshAll();

    void OnEnable() => RefreshAll();

    void Update()
    {
        if (!GameManager.instance) return;
        UpdateDaysOpen();
        UpdateMoney();
        UpdateFish();
        UpdateFishFood();
        UpdateFishAcclimation();
    }

    void RefreshAll()
    {
        if (!GameManager.instance) return;
        UpdateDaysOpen();
        UpdateMoney();
        UpdateFish();
        UpdateFishFood();
        UpdateFishAcclimation();
    }

    void UpdateDaysOpen()
    {
        if (!daysOpenText || !GameManager.instance) return;
        daysOpenText.text = $"Days Open: {GameManager.instance.daysOpen}";
    }

    void UpdateMoney()
    {
        if (!moneyText || !GameManager.instance) return;
        moneyText.text = $"${GameManager.instance.moneyAmount}";
    }

    void UpdateFish()
    {
        if (!fishText || !GameManager.instance) return;
        fishText.text = GameManager.instance.fishAmount.ToString();
    }

    void UpdateFishFood()
    {
        if (!fishFoodText || !GameManager.instance) return;
        fishFoodText.text = GameManager.instance.fishFood.ToString();
    }

    void UpdateFishAcclimation()
    {
        if (!fishAcclimationText || !GameManager.instance) return;

        if (!GameManager.instance.fishAcclimationActive)
        {
            fishAcclimationText.text = "Fish Acclimation: None";
            return;
        }

        float secondsRemaining = GameManager.instance.GetFishAcclimationSecondsRemaining();

        if (secondsRemaining <= 0f)
        {
            fishAcclimationText.text = "Fish Acclimation: Complete";
            return;
        }

        fishAcclimationText.text = $"Fish Acclimation: {FormatTime(secondsRemaining)} remaining";
    }

    string FormatTime(float totalSeconds)
    {
        if (totalSeconds < 0f) totalSeconds = 0f;
        TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
        return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }
}
