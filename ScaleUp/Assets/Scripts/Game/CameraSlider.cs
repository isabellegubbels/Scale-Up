using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSlider : MonoBehaviour
{
    [SerializeField] float minX = -10f, maxX = 10f;
    [SerializeField] float dragSensitivity = 0.02f;
    [SerializeField] float defaultSlideDuration = 0.4f;

    Vector2 startPointerPosition;
    Vector3 startCameraPosition;
    bool dragging;
    Coroutine slideCoroutine;

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
            if (slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
                slideCoroutine = null;
            }
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

    public void SlideTo(float targetX, float duration = -1f)
    {
        float clampedTargetX = Mathf.Clamp(targetX, minX, maxX);
        float slideDuration = duration >= 0f ? duration : defaultSlideDuration;

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideRoutine(clampedTargetX, slideDuration));
    }

    System.Collections.IEnumerator SlideRoutine(float targetX, float duration)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition;
        endPosition.x = targetX;

        if (duration <= 0f)
        {
            transform.position = endPosition;
            slideCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        transform.position = endPosition;
        slideCoroutine = null;
    }
}

