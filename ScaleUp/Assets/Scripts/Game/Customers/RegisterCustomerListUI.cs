using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegisterCustomerListUI : MonoBehaviour
{
    [SerializeField] Transform customerButtonContainer;
    [SerializeField] GameObject customerButtonPrefab;
    [SerializeField] CustomerInteractionUI interactionPanel;

    void OnEnable()
    {
        SubscribeToQueueEvents(true);
        RefreshList();
    }

    void OnDisable()
    {
        SubscribeToQueueEvents(false);
    }

    public void RefreshList()
    {
        if (customerButtonContainer == null || customerButtonPrefab == null) return;

        ClearButtons();

        if (CustomerManager.instance == null) return;
        var queue = CustomerManager.instance.GetQueue();
        if (queue == null) return;

        for (int i = 0; i < queue.Count; i++)
        {
            var customer = queue[i];
            if (customer == null || !customer.IsActive) continue;
            CreateButtonForCustomer(customer);
        }
    }

    void SubscribeToQueueEvents(bool subscribe)
    {
        if (CustomerManager.instance == null) return;

        if (subscribe)
        {
            CustomerManager.instance.OnQueueChanged += RefreshList;
            CustomerManager.instance.OnCustomerArrived += HandleCustomerEvent;
            CustomerManager.instance.OnCustomerServed += HandleCustomerEvent;
            CustomerManager.instance.OnCustomerLeft += HandleCustomerEvent;
            CustomerManager.instance.OnCustomerDismissed += HandleCustomerEvent;
        }
        else
        {
            CustomerManager.instance.OnQueueChanged -= RefreshList;
            CustomerManager.instance.OnCustomerArrived -= HandleCustomerEvent;
            CustomerManager.instance.OnCustomerServed -= HandleCustomerEvent;
            CustomerManager.instance.OnCustomerLeft -= HandleCustomerEvent;
            CustomerManager.instance.OnCustomerDismissed -= HandleCustomerEvent;
        }
    }

    void HandleCustomerEvent(CustomerInstance _) => RefreshList();

    void CreateButtonForCustomer(CustomerInstance customer)
    {
        var buttonObject = Instantiate(customerButtonPrefab, customerButtonContainer);
        var button = buttonObject.GetComponent<Button>();
        var nameText = buttonObject.GetComponentInChildren<TMP_Text>(true);
        var portraitImage = buttonObject.GetComponentInChildren<Image>(true);
        string customerId = customer.customerId;

        if (nameText != null) nameText.text = customer.GetDisplayName();
        if (portraitImage != null && customer.personality != null && customer.personality.portrait != null)
            portraitImage.sprite = customer.personality.portrait;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnCustomerButtonPressed(customerId));
        }
    }

    void OnCustomerButtonPressed(string customerId)
    {
        if (string.IsNullOrEmpty(customerId) || CustomerManager.instance == null) return;
        CustomerManager.instance.GreetCustomer(customerId);
        if (interactionPanel != null) interactionPanel.OpenForCustomer(customerId);
    }

    void ClearButtons()
    {
        for (int i = customerButtonContainer.childCount - 1; i >= 0; i--)
        {
            var child = customerButtonContainer.GetChild(i);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
    }
}
