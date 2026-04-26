using UnityEngine;
using UnityEngine.InputSystem;

public class CustomerWorldButton : MonoBehaviour
{
    [SerializeField] float maxTapScreenMovement = 70f;
    [SerializeField] bool requireReleaseOverButton = false;
    [SerializeField] float colliderPaddingMultiplier = 2.2f;

    RegisterCustomerListUI owner;
    string customerId;
    Collider2D buttonCollider;
    Vector2 pressStartScreenPosition;
    bool pressStartedOnButton;

    void Awake()
    {
        buttonCollider = GetComponent<Collider2D>();
        ExpandColliderHitArea();
    }

    public void Configure(RegisterCustomerListUI ownerRef, string customerIdValue)
    {
        owner = ownerRef;
        customerId = customerIdValue;
    }

    void Update()
    {
        if (!enabled || !gameObject.activeInHierarchy || owner == null || string.IsNullOrEmpty(customerId) || buttonCollider == null) return;

        Vector2 pointerPosition = Vector2.zero;
        bool pointerDown = false, pointerUp = false;

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                pointerDown = true;
                pointerPosition = touch.position.ReadValue();
            }
            else if (touch.press.wasReleasedThisFrame)
            {
                pointerUp = true;
                pointerPosition = touch.position.ReadValue();
            }
        }
        else if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                pointerDown = true;
                pointerPosition = Mouse.current.position.ReadValue();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                pointerUp = true;
                pointerPosition = Mouse.current.position.ReadValue();
            }
        }

        if (pointerDown)
        {
            pressStartedOnButton = IsPointerOverButton(pointerPosition);
            pressStartScreenPosition = pointerPosition;
            return;
        }

        if (!pointerUp) return;
        bool releaseOverButton = IsPointerOverButton(pointerPosition);
        if (!pressStartedOnButton && !releaseOverButton) return;

        pressStartedOnButton = false;
        float dragDistance = Vector2.Distance(pressStartScreenPosition, pointerPosition);
        if (dragDistance > maxTapScreenMovement) return;
        if (requireReleaseOverButton && !releaseOverButton) return;

        owner.HandleCustomerSelected(customerId);
    }

    bool IsPointerOverButton(Vector2 screenPosition)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector2 worldPoint = cam.ScreenToWorldPoint(screenPosition);
        return buttonCollider.OverlapPoint(worldPoint);
    }

    void ExpandColliderHitArea()
    {
        if (buttonCollider == null) return;
        if (buttonCollider is BoxCollider2D box)
        {
            float multiplier = Mathf.Max(1f, colliderPaddingMultiplier);
            box.size = new Vector2(box.size.x * multiplier, box.size.y * multiplier);
        }
    }
}
