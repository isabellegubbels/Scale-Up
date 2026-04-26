using TMPro;
using UnityEngine;

public class CustomerNotificationUI : MonoBehaviour
{
    [SerializeField] GameObject notificationRoot;
    [SerializeField] TMP_Text customerCountText;
    [SerializeField] CameraSlider cameraSlider;
    [SerializeField] float registerXPosition = 10f;
    [SerializeField] bool showQueueCount = false;

    bool suppressWhileQueueRemains;
    CanvasGroup localCanvasGroup;

    void Awake()
    {
        if (cameraSlider == null) cameraSlider = FindAnyObjectByType<CameraSlider>();
        localCanvasGroup = GetComponent<CanvasGroup>();
        if (localCanvasGroup == null) localCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        SubscribeToCustomerEvents(true);
        RefreshNotification();
    }

    void OnDisable()
    {
        SubscribeToCustomerEvents(false);
    }

    public void OnNotificationTapped()
    {
        if (cameraSlider != null) cameraSlider.SlideTo(registerXPosition);
        suppressWhileQueueRemains = true;
        SetNotificationVisible(false);
    }

    void SubscribeToCustomerEvents(bool subscribe)
    {
        if (CustomerManager.instance == null) return;

        if (subscribe)
        {
            CustomerManager.instance.OnCustomerArrived += HandleCustomerChanged;
            CustomerManager.instance.OnCustomerServed += HandleCustomerChanged;
            CustomerManager.instance.OnCustomerLeft += HandleCustomerChanged;
            CustomerManager.instance.OnCustomerDismissed += HandleCustomerChanged;
            CustomerManager.instance.OnQueueChanged += RefreshNotification;
        }
        else
        {
            CustomerManager.instance.OnCustomerArrived -= HandleCustomerChanged;
            CustomerManager.instance.OnCustomerServed -= HandleCustomerChanged;
            CustomerManager.instance.OnCustomerLeft -= HandleCustomerChanged;
            CustomerManager.instance.OnCustomerDismissed -= HandleCustomerChanged;
            CustomerManager.instance.OnQueueChanged -= RefreshNotification;
        }
    }

    void HandleCustomerChanged(CustomerInstance _) => RefreshNotification();

    void RefreshNotification()
    {
        int queueCount = CustomerManager.instance != null ? CustomerManager.instance.GetCurrentCustomerCount() : 0;
        if (queueCount <= 0) suppressWhileQueueRemains = false;
        bool hasCustomers = queueCount > 0;
        SetNotificationVisible(hasCustomers && !suppressWhileQueueRemains);
        if (customerCountText != null)
        {
            customerCountText.gameObject.SetActive(showQueueCount && hasCustomers);
            customerCountText.text = queueCount.ToString();
        }
    }

    void SetNotificationVisible(bool isVisible)
    {
        GameObject target = notificationRoot != null ? notificationRoot : gameObject;
        if (target != gameObject)
        {
            target.SetActive(isVisible);
            return;
        }

        localCanvasGroup.alpha = isVisible ? 1f : 0f;
        localCanvasGroup.interactable = isVisible;
        localCanvasGroup.blocksRaycasts = isVisible;
    }
}
