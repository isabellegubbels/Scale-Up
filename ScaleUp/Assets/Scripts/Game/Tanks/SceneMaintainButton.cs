using UnityEngine;
using UnityEngine.InputSystem;

public class SceneMaintainButton : MonoBehaviour
{
    [SerializeField] int slotIndex;
    [SerializeField] SceneUI sceneUi;
    [SerializeField] float maxTapScreenMovement = 20f;

    Collider2D buttonCollider;
    Vector2 pressStartScreenPosition;
    bool pressStartedOnThisButton;

    void Awake()
    {
        if (sceneUi == null) sceneUi = FindAnyObjectByType<SceneUI>();
        buttonCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (!enabled || !gameObject.activeInHierarchy || slotIndex < 0 || buttonCollider == null) return;

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
            pressStartedOnThisButton = IsPointerOverThisButton(pointerPosition);
            pressStartScreenPosition = pointerPosition;
            return;
        }

        if (!pointerUp || !pressStartedOnThisButton) return;

        pressStartedOnThisButton = false;
        float dragDistance = Vector2.Distance(pressStartScreenPosition, pointerPosition);
        if (dragDistance > maxTapScreenMovement) return;
        if (!IsPointerOverThisButton(pointerPosition)) return;

        if (sceneUi == null) sceneUi = FindAnyObjectByType<SceneUI>();
        if (sceneUi != null) sceneUi.MaintainTank(slotIndex);
    }

    bool IsPointerOverThisButton(Vector2 screenPosition)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector2 worldPoint = cam.ScreenToWorldPoint(screenPosition);
        return buttonCollider.OverlapPoint(worldPoint);
    }
}
