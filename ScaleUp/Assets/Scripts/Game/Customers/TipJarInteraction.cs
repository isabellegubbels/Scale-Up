using UnityEngine;
using UnityEngine.InputSystem;

public class TipJarInteraction : MonoBehaviour
{
    [SerializeField] float maxTapScreenMovement = 20f;

    Collider2D tipJarCollider;
    Vector2 pressStartScreenPosition;
    bool pressStartedOnTipJar;

    void Awake()
    {
        tipJarCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (!enabled || !gameObject.activeInHierarchy || tipJarCollider == null) return;

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
            pressStartedOnTipJar = IsPointerOverTipJar(pointerPosition);
            pressStartScreenPosition = pointerPosition;
            return;
        }

        if (!pointerUp || !pressStartedOnTipJar) return;

        pressStartedOnTipJar = false;
        float dragDistance = Vector2.Distance(pressStartScreenPosition, pointerPosition);
        if (dragDistance > maxTapScreenMovement) return;
        if (!IsPointerOverTipJar(pointerPosition)) return;

        if (TipJar.instance != null) TipJar.instance.CollectTips();
    }

    bool IsPointerOverTipJar(Vector2 screenPosition)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector2 worldPoint = cam.ScreenToWorldPoint(screenPosition);
        return tipJarCollider.OverlapPoint(worldPoint);
    }
}
