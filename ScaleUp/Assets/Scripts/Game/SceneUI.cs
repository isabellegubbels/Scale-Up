using System;
using UnityEngine;
using TMPro;

public class SceneUI : MonoBehaviour
{
    [SerializeField] TMP_Text daysOpenText, acclimationSummaryText, moneyText, fishText, fishFoodText;
    [SerializeField] TMP_Text[] tankAcclimationTexts;
    [SerializeField] GameObject[] tankMaintainButtons;

    void Start()
    {
        HideAllMaintainButtons();
        RefreshAll();
    }

    void OnEnable()
    {
        HideAllMaintainButtons();
        RefreshAll();
    }

    void Update()
    {
        if (!GameManager.instance) return;
        UpdateDaysOpen();
        UpdateMoney();
        UpdateFish();
        UpdateFishFood();
        UpdateAcclimationTexts();
        UpdateMaintainButtons();
    }

    void RefreshAll()
    {
        if (!GameManager.instance) return;
        UpdateDaysOpen();
        UpdateMoney();
        UpdateFish();
        UpdateFishFood();
        UpdateAcclimationTexts();
        UpdateMaintainButtons();
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
        int count = TankManager.instance != null ? TankManager.instance.GetTotalFishCount() : GameManager.instance.fishAmount;
        fishText.text = count.ToString();
    }

    void UpdateFishFood()
    {
        if (!fishFoodText || !GameManager.instance) return;
        int food = FishResourceManager.instance != null ? FishResourceManager.instance.GetFishFood() : GameManager.instance.fishFood;
        fishFoodText.text = food.ToString();
    }

    void UpdateAcclimationTexts()
    {
        UpdateAcclimationSummary();
        UpdatePerTankAcclimationTexts();
    }

    void UpdateMaintainButtons()
    {
        if (tankMaintainButtons == null || tankMaintainButtons.Length == 0) return;

        for (int i = 0; i < tankMaintainButtons.Length; i++)
        {
            GameObject button = tankMaintainButtons[i];
            if (!button) continue;

            bool show = false;
            if (TankManager.instance != null && i < TankManager.instance.SlotCount)
            {
                var slot = TankManager.instance.GetSlot(i);
                show = slot != null && slot.isOwned && !TankManager.instance.IsTankClean(i);
            }
            button.SetActive(show);
        }
    }

    void HideAllMaintainButtons()
    {
        if (tankMaintainButtons == null || tankMaintainButtons.Length == 0) return;
        for (int i = 0; i < tankMaintainButtons.Length; i++)
        {
            if (tankMaintainButtons[i] != null) tankMaintainButtons[i].SetActive(false);
        }
    }

    void UpdateAcclimationSummary()
    {
        if (!acclimationSummaryText || !GameManager.instance) return;
        if (TankManager.instance == null)
        {
            acclimationSummaryText.text = "Fish Acclimation: None";
            return;
        }
        int acclimatingTankCount = 0;
        float soonestSeconds = float.MaxValue;
        for (int i = 0; i < TankManager.instance.SlotCount; i++)
        {
            var slot = TankManager.instance.GetSlot(i);
            if (slot == null || !slot.isOwned || slot.fishCount <= 0) continue;
            float remaining = TankManager.instance.GetAcclimationSecondsRemaining(i);
            if (remaining <= 0f) continue;
            acclimatingTankCount++;
            if (remaining < soonestSeconds) soonestSeconds = remaining;
        }
        if (acclimatingTankCount == 0)
        {
            acclimationSummaryText.text = "Fish Acclimation: None";
            return;
        }
        acclimationSummaryText.text = $"Fish Acclimation: {acclimatingTankCount} tank(s), next {FormatTime(soonestSeconds)}";
    }

    void UpdatePerTankAcclimationTexts()
    {
        if (tankAcclimationTexts == null || tankAcclimationTexts.Length == 0) return;

        for (int i = 0; i < tankAcclimationTexts.Length; i++)
        {
            TMP_Text text = tankAcclimationTexts[i];
            if (!text) continue;

            if (TankManager.instance == null || i >= TankManager.instance.SlotCount)
            {
                text.gameObject.SetActive(false);
                continue;
            }

            var slot = TankManager.instance.GetSlot(i);
            if (slot == null || !slot.isOwned || slot.fishCount <= 0)
            {
                text.gameObject.SetActive(false);
                continue;
            }

            float remaining = TankManager.instance.GetAcclimationSecondsRemaining(i);
            if (remaining <= 0f)
            {
                text.gameObject.SetActive(false);
                continue;
            }

            text.text = FormatTime(remaining);
            text.gameObject.SetActive(true);
        }
    }

    string FormatTime(float totalSeconds)
    {
        if (totalSeconds < 0f) totalSeconds = 0f;
        int roundedSeconds = Mathf.CeilToInt(totalSeconds);
        int minutes = roundedSeconds / 60;
        int seconds = roundedSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    public void MaintainTank(int slotIndex)
    {
        if (slotIndex < 0) return;
        if (TankMaintenanceManager.instance == null) return;
        TankMaintenanceManager.instance.PayMaintenance(slotIndex);
    }
}
