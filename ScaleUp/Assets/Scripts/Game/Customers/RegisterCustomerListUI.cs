using UnityEngine;

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
        var portraitSpriteRenderer = buttonObject.GetComponentInChildren<SpriteRenderer>(true);
        string customerId = customer.customerId;

        Sprite portrait = customer.personality != null ? customer.personality.portrait : null;
        if (portraitSpriteRenderer != null)
        {
            portraitSpriteRenderer.sprite = portrait;
            portraitSpriteRenderer.enabled = portrait != null;
        }

        var worldButton = buttonObject.GetComponent<CustomerWorldButton>();
        if (worldButton == null) worldButton = buttonObject.AddComponent<CustomerWorldButton>();
        worldButton.Configure(this, customerId);
    }

    public void HandleCustomerSelected(string customerId)
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
