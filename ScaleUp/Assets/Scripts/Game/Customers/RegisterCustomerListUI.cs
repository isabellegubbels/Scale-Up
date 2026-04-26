using UnityEngine;

public class RegisterCustomerListUI : MonoBehaviour
{
    [SerializeField] Transform customerButtonContainer;
    [SerializeField] GameObject customerButtonPrefab;
    [SerializeField] CustomerInteractionUI interactionPanel;
    [SerializeField] float worldButtonVerticalSpacing = -1.2f;
    [SerializeField] Vector3 firstButtonWorldOffset = Vector3.zero;

    readonly System.Collections.Generic.List<GameObject> spawnedCustomerButtons = new System.Collections.Generic.List<GameObject>();
    bool isSubscribed;

    void Update()
    {
        if (!isSubscribed) TrySubscribeToQueueEvents();
    }

    void OnEnable()
    {
        TrySubscribeToQueueEvents();
        RefreshList();
    }

    void OnDisable()
    {
        SubscribeToQueueEvents(false);
        isSubscribed = false;
        ClearButtons();
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
            CreateButtonForCustomer(customer, spawnedCustomerButtons.Count);
        }
    }

    void TrySubscribeToQueueEvents()
    {
        if (isSubscribed || CustomerManager.instance == null) return;
        SubscribeToQueueEvents(true);
        isSubscribed = true;
        RefreshList();
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

    void CreateButtonForCustomer(CustomerInstance customer, int spawnIndex)
    {
        if (customerButtonContainer == null) return;
        Vector3 spawnPosition = customerButtonContainer.position + firstButtonWorldOffset + Vector3.up * (worldButtonVerticalSpacing * spawnIndex);
        var buttonObject = Instantiate(customerButtonPrefab, spawnPosition, Quaternion.identity);

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
        spawnedCustomerButtons.Add(buttonObject);
    }

    public void HandleCustomerSelected(string customerId)
    {
        if (string.IsNullOrEmpty(customerId) || CustomerManager.instance == null) return;
        CustomerManager.instance.GreetCustomer(customerId);
        if (interactionPanel != null) interactionPanel.OpenForCustomer(customerId);
    }

    void ClearButtons()
    {
        for (int i = spawnedCustomerButtons.Count - 1; i >= 0; i--)
        {
            var buttonObject = spawnedCustomerButtons[i];
            if (buttonObject == null) continue;
            if (Application.isPlaying) Destroy(buttonObject);
            else DestroyImmediate(buttonObject);
        }
        spawnedCustomerButtons.Clear();
    }
}
