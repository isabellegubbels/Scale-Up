using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSlider : MonoBehaviour
{
    [SerializeField] float minX = -10f, maxX = 10f;
    [SerializeField] float dragSensitivity = 0.02f;

    Vector2 startPointerPosition;
    Vector3 startCameraPosition;
    bool dragging;

    void Update()
    {
        Vector2 pointerPosition = Vector2.zero;
        bool pointerDown = false, pointerHeld = false, pointerUp = false;

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                pointerDown = true;
                pointerHeld = true;
                pointerPosition = touch.position.ReadValue();
            }
            else if (touch.press.isPressed)
            {
                pointerHeld = true;
                pointerPosition = touch.position.ReadValue();
            }
            else if (touch.press.wasReleasedThisFrame) pointerUp = true;
        }
        else if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                pointerDown = true;
                pointerHeld = true;
                pointerPosition = Mouse.current.position.ReadValue();
            }
            else if (Mouse.current.leftButton.isPressed)
            {
                pointerHeld = true;
                pointerPosition = Mouse.current.position.ReadValue();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame) pointerUp = true;
        }

        if (pointerDown)
        {
            dragging = true;
            startPointerPosition = pointerPosition;
            startCameraPosition = transform.position;
        }
        else if (dragging && pointerHeld)
        {
            float deltaX = (pointerPosition.x - startPointerPosition.x) * dragSensitivity;
            float targetX = Mathf.Clamp(startCameraPosition.x - deltaX, minX, maxX);

            Vector3 position = transform.position;
            position.x = targetX;
            transform.position = position;
        }

        if (pointerUp) dragging = false;
    }
}

