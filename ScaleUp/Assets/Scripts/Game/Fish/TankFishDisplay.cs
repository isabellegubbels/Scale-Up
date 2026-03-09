using UnityEngine;

public class TankFishDisplay : MonoBehaviour
{
    [SerializeField] int slotIndex;
    [SerializeField] Transform fishContainer; // Parent of fish placeholders
    [SerializeField] Sprite placeholderSprite;
    [SerializeField] float spacing = 0.15f;

    SpriteRenderer[] fishRenderers;
    int lastDisplayedCount = -1;
    bool lastAcclimating;

    int Capacity => TankTier.GetCapacity(slotIndex);

    void Start() => EnsurePool();

    void Update()
    {
        if (TankManager.instance == null) return;
        var slot = TankManager.instance.GetSlot(slotIndex);
        if (slot == null || !slot.isOwned) return;
        int count = slot.fishCount;
        bool acclimating = !TankManager.instance.IsAcclimationComplete(slotIndex);
        if (count != lastDisplayedCount || acclimating != lastAcclimating) RefreshDisplay(count, acclimating);
    }

    void EnsurePool()
    {
        int maxDisplayCount = Capacity;
        if (maxDisplayCount <= 0) return;
        if (fishRenderers != null && fishRenderers.Length >= maxDisplayCount) return;
        fishRenderers = new SpriteRenderer[maxDisplayCount];
        if (fishContainer == null) fishContainer = transform;
        Sprite sprite = placeholderSprite != null ? placeholderSprite : CreatePlaceholderSprite();
        for (int i = 0; i < maxDisplayCount; i++)
        {
            var go = new GameObject("FishPlaceholder_" + i);
            go.transform.SetParent(fishContainer, false);
            go.transform.localPosition = new Vector3(i * spacing - (maxDisplayCount - 1) * spacing * 0.5f, 0f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1;
            fishRenderers[i] = sr;
            go.SetActive(false);
        }
    }

    static Sprite CreatePlaceholderSprite()
    {
        var tex = new Texture2D(4, 4);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }

    void RefreshDisplay(int count, bool acclimating)
    {
        lastDisplayedCount = count;
        lastAcclimating = acclimating;
        EnsurePool();
        int maxDisplayCount = fishRenderers != null ? fishRenderers.Length : Capacity;
        int show = Mathf.Min(count, maxDisplayCount);
        Color c = acclimating ? new Color(0.6f, 0.6f, 0.6f) : Color.white; // Grey = still acclimating
        for (int i = 0; i < fishRenderers.Length; i++)
        {
            bool active = i < show;
            if (fishRenderers[i] != null)
            {
                fishRenderers[i].gameObject.SetActive(active);
                fishRenderers[i].color = c;
            }
        }
    }
}
