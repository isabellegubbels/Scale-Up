using UnityEngine;

public class FishMovement : MonoBehaviour
{
    [SerializeField] private RectTransform swimmingArea;
    [SerializeField] private float speed,minSpeed = 30f, maxSpeed = 80f, padding = 20f;
    private Vector2 targetPosition;

    void Start()
    {
        transform.position = RandomPointInArea();
        PickNewTarget();
    }

    void Update()
    {
        Vector2 currentPos = transform.position;
        transform.position = Vector2.MoveTowards(currentPos, targetPosition, speed * Time.deltaTime);

        float direction = targetPosition.x - currentPos.x;
        Vector3 scale = transform.localScale;
        if (direction > 0.01f) scale.x = -Mathf.Abs(scale.x);
        else if (direction < -0.01f) scale.x = Mathf.Abs(scale.x);
        transform.localScale = scale;

        if (Vector2.Distance(currentPos, targetPosition) < 5f) PickNewTarget();
    }

    Vector2 RandomPointInArea()
    {
        Vector3[] corners = new Vector3[4];
        swimmingArea.GetWorldCorners(corners);
        float x = Random.Range(corners[0].x + padding, corners[2].x - padding);
        float y = Random.Range(corners[0].y + padding, corners[2].y - padding);
        return new Vector2(x, y);
    }

    void PickNewTarget()
    {
        targetPosition = RandomPointInArea();
        speed = Random.Range(minSpeed, maxSpeed);
    }
}
