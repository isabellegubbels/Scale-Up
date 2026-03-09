using UnityEngine;
using UnityEngine.UI;

public class TankSlotUI : MonoBehaviour
{
    [SerializeField] int slotIndex;
    [SerializeField] Button purchaseButton;
    [SerializeField] Button feedButton;

    void Start()
    {
        if (purchaseButton != null) purchaseButton.onClick.AddListener(OnPurchaseClicked);
        if (feedButton != null) feedButton.onClick.AddListener(OnFeedClicked);
    }

    void Update()
    {
        if (TankManager.instance == null) return;
        if (purchaseButton != null) purchaseButton.gameObject.SetActive(!TankManager.instance.IsOwned(slotIndex));
        if (feedButton != null)
        {
            var slot = TankManager.instance.GetSlot(slotIndex);
            feedButton.gameObject.SetActive(slot != null && slot.isOwned && slot.fishCount > 0);
            feedButton.interactable = slot != null && !slot.fedToday;
        }
    }

    void OnPurchaseClicked()
    {
        if (TankManager.instance != null) TankManager.instance.PurchaseSlot(slotIndex);
    }

    void OnFeedClicked()
    {
        if (TankManager.instance != null) TankManager.instance.FeedTank(slotIndex);
    }
}
