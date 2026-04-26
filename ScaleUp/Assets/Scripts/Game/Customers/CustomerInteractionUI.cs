using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomerInteractionUI : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] Transform portraitSlot;
    [SerializeField] Image portraitImage;
    [SerializeField] Transform fishIconSlot;
    [SerializeField] Image fishIconImage;
    [SerializeField] TMP_Text dialogueText;
    [SerializeField] TMP_Text offerText;
    [SerializeField] GameObject sellButton;
    [SerializeField] GameObject counterOfferButton;
    [SerializeField] GameObject dismissButton;
    [SerializeField] TMP_Text counterOfferButtonText;
    [SerializeField] float counterRejectedCloseDelaySeconds = 0.9f;

    string activeCustomerId;
    Coroutine delayedCloseCoroutine;

    void OnEnable()
    {
        SubscribeToEvents(true);
    }

    void OnDisable()
    {
        SubscribeToEvents(false);
    }

    void Update()
    {
        if (!IsPanelOpen()) return;
        var customer = GetActiveCustomer();
        if (customer == null || !customer.IsActive)
        {
            ClosePanel();
            return;
        }

        SetSellButtonInteractable(CustomerManager.instance != null && CustomerManager.instance.CanServeCustomer(activeCustomerId));
    }

    public void OpenForCustomer(string customerId)
    {
        if (string.IsNullOrEmpty(customerId) || CustomerManager.instance == null) return;

        var customer = CustomerManager.instance.GetCustomerById(customerId);
        if (customer == null || !customer.IsActive) return;

        activeCustomerId = customerId;
        ApplyCustomerToPanel(customer);
        SetPanelActive(true);
    }

    public void ClosePanel()
    {
        if (delayedCloseCoroutine != null)
        {
            StopCoroutine(delayedCloseCoroutine);
            delayedCloseCoroutine = null;
        }
        activeCustomerId = null;
        SetPanelActive(false);
    }

    public void OnSellButtonPressed()
    {
        if (CustomerManager.instance == null || string.IsNullOrEmpty(activeCustomerId)) return;
        bool sold = CustomerManager.instance.SellToCustomer(activeCustomerId);
        if (sold) ClosePanel();
    }

    public void OnCounterOfferButtonPressed()
    {
        if (CustomerManager.instance == null || string.IsNullOrEmpty(activeCustomerId)) return;

        bool acceptedAndSold = CustomerManager.instance.CounterOffer(activeCustomerId);
        if (acceptedAndSold)
        {
            ClosePanel();
            return;
        }

        var customerStillActive = CustomerManager.instance.GetCustomerById(activeCustomerId);
        if (customerStillActive == null)
        {
            ShowCounterMessage("Counter offer rejected.");
            StartDelayedClose(counterRejectedCloseDelaySeconds);
            return;
        }

        ApplyCustomerToPanel(customerStillActive);
    }

    public void OnDismissButtonPressed()
    {
        if (CustomerManager.instance == null || string.IsNullOrEmpty(activeCustomerId)) return;
        CustomerManager.instance.DismissCustomer(activeCustomerId);
        ClosePanel();
    }

    void SubscribeToEvents(bool subscribe)
    {
        if (CustomerManager.instance == null) return;

        if (subscribe)
        {
            CustomerManager.instance.OnCustomerLeft += HandleCustomerLeft;
            CustomerManager.instance.OnCustomerServed += HandleCustomerResolved;
            CustomerManager.instance.OnCustomerDismissed += HandleCustomerResolved;
            CustomerManager.instance.OnQueueChanged += HandleQueueChanged;
        }
        else
        {
            CustomerManager.instance.OnCustomerLeft -= HandleCustomerLeft;
            CustomerManager.instance.OnCustomerServed -= HandleCustomerResolved;
            CustomerManager.instance.OnCustomerDismissed -= HandleCustomerResolved;
            CustomerManager.instance.OnQueueChanged -= HandleQueueChanged;
        }
    }

    void HandleCustomerLeft(CustomerInstance customer)
    {
        if (customer == null || string.IsNullOrEmpty(activeCustomerId)) return;
        if (customer.customerId != activeCustomerId) return;
        ClosePanel();
    }

    void HandleCustomerResolved(CustomerInstance customer)
    {
        if (customer == null || string.IsNullOrEmpty(activeCustomerId)) return;
        if (customer.customerId != activeCustomerId) return;
        ClosePanel();
    }

    void HandleQueueChanged()
    {
        if (!IsPanelOpen()) return;
        var customer = GetActiveCustomer();
        if (customer == null || !customer.IsActive)
        {
            ClosePanel();
            return;
        }
        ApplyCustomerToPanel(customer);
    }

    CustomerInstance GetActiveCustomer()
    {
        if (CustomerManager.instance == null || string.IsNullOrEmpty(activeCustomerId)) return null;
        return CustomerManager.instance.GetCustomerById(activeCustomerId);
    }

    void ApplyCustomerToPanel(CustomerInstance customer)
    {
        if (customer == null) return;

        if (portraitImage != null)
        {
            var portrait = customer.personality != null ? customer.personality.portrait : null;
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
            if (portraitSlot != null) portraitImage.transform.SetParent(portraitSlot, false);
        }

        if (fishIconImage != null)
        {
            Sprite fishSprite = null;
            if (FishSpeciesRegistry.instance != null)
            {
                var species = FishSpeciesRegistry.instance.GetSpecies(customer.wantedSpeciesId);
                if (species != null) fishSprite = species.placeholderSprite;
            }
            fishIconImage.sprite = fishSprite;
            fishIconImage.enabled = fishSprite != null;
            if (fishIconSlot != null) fishIconImage.transform.SetParent(fishIconSlot, false);
        }

        SetDialogue(customer.currentLine);
        HideCounterMessage();

        string fishName = customer.wantedSpeciesId;
        if (FishSpeciesRegistry.instance != null)
        {
            var species = FishSpeciesRegistry.instance.GetSpecies(customer.wantedSpeciesId);
            if (species != null && !string.IsNullOrWhiteSpace(species.displayName)) fishName = species.displayName;
        }
        if (offerText != null)
            offerText.text = $"Wants {customer.wantedFishCount} {fishName} for ${customer.offerPrice}";

        if (counterOfferButton != null)
            counterOfferButton.SetActive(customer.CanCounterOffer);
        if (dismissButton != null)
            dismissButton.SetActive(true);

        bool canServe = CustomerManager.instance != null && CustomerManager.instance.CanServeCustomer(customer.customerId);
        SetSellButtonInteractable(canServe);
    }

    void SetSellButtonInteractable(bool canInteract)
    {
        if (sellButton == null) return;
        var button = sellButton.GetComponent<Button>();
        if (button != null) button.interactable = canInteract;
    }

    void SetDialogue(string line)
    {
        if (dialogueText == null) return;
        dialogueText.gameObject.SetActive(true);
        dialogueText.text = string.IsNullOrWhiteSpace(line) ? "..." : line;
    }

    void ShowCounterMessage(string line)
    {
        if (dialogueText != null) dialogueText.gameObject.SetActive(false);
        if (counterOfferButtonText == null) return;
        counterOfferButtonText.gameObject.SetActive(true);
        counterOfferButtonText.text = string.IsNullOrWhiteSpace(line) ? "Counter offer rejected." : line;
    }

    void HideCounterMessage()
    {
        if (counterOfferButtonText != null) counterOfferButtonText.gameObject.SetActive(false);
        if (dialogueText != null) dialogueText.gameObject.SetActive(true);
    }

    void StartDelayedClose(float delaySeconds)
    {
        if (delayedCloseCoroutine != null) StopCoroutine(delayedCloseCoroutine);
        delayedCloseCoroutine = StartCoroutine(DelayedClose(delaySeconds));
    }

    System.Collections.IEnumerator DelayedClose(float delaySeconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delaySeconds));
        delayedCloseCoroutine = null;
        ClosePanel();
    }

    void SetPanelActive(bool isActive)
    {
        if (panelRoot != null) panelRoot.SetActive(isActive);
    }

    bool IsPanelOpen() => panelRoot != null && panelRoot.activeInHierarchy;

}
