using TMPro;
using UnityEngine;

public class CustomerNotificationUI : MonoBehaviour
{
    [SerializeField] GameObject notificationRoot;
    [SerializeField] TMP_Text customerCountText;
    [SerializeField] CameraSlider cameraSlider;
    [SerializeField] float registerXPosition = 10f;
    [SerializeField] bool showQueueCount = false;

    void Awake()
    {
        if (cameraSlider == null) cameraSlider = FindAnyObjectByType<CameraSlider>();
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
        if (notificationRoot != null) notificationRoot.SetActive(false);
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
        bool hasCustomers = queueCount > 0;
        if (notificationRoot != null) notificationRoot.SetActive(hasCustomers);
        if (customerCountText != null)
        {
            customerCountText.gameObject.SetActive(showQueueCount && hasCustomers);
            customerCountText.text = queueCount.ToString();
        }
    }
}
