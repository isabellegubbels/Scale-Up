using UnityEngine;

public class TankFishDisplay : MonoBehaviour
{
    [SerializeField] int slotIndex;
    [SerializeField] Transform fishContainer; // Parent for placeholder sprites; null = this transform.
    [SerializeField] float spacing = 0.15f;

    SpriteRenderer[] fishRenderers;
    int lastDisplayedCount = -1;
    bool lastAcclimating;
    string lastSpeciesId;

    int Capacity => TankTier.GetCapacity(slotIndex);

    void Start() => EnsurePool();

    void Update()
    {
        if (TankManager.instance == null) return;
        var slot = TankManager.instance.GetSlot(slotIndex);
        if (slot == null || !slot.isOwned) return;
        int count = slot.fishCount;
        string speciesId = slot.speciesId;
        bool acclimating = !TankManager.instance.IsAcclimationComplete(slotIndex);
        if (count != lastDisplayedCount || acclimating != lastAcclimating || speciesId != lastSpeciesId) RefreshDisplay(count, acclimating, speciesId);
    }

    void EnsurePool()
    {
        int maxDisplayCount = Capacity;
        if (maxDisplayCount <= 0) return;
        if (fishRenderers != null && fishRenderers.Length >= maxDisplayCount) return;
        fishRenderers = new SpriteRenderer[maxDisplayCount];
        if (fishContainer == null) fishContainer = transform;
        for (int i = 0; i < maxDisplayCount; i++)
        {
            var go = new GameObject("FishPlaceholder_" + i);
            go.transform.SetParent(fishContainer, false);
            go.transform.localPosition = new Vector3(i * spacing - (maxDisplayCount - 1) * spacing * 0.5f, 0f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
            fishRenderers[i] = sr;
            go.SetActive(false);
        }
    }

    void RefreshDisplay(int count, bool acclimating, string speciesId)
    {
        lastDisplayedCount = count;
        lastAcclimating = acclimating;
        lastSpeciesId = speciesId;
        EnsurePool();

        Sprite sprite = null;
        if (FishSpeciesRegistry.instance != null && !string.IsNullOrEmpty(speciesId))
        {
            var data = FishSpeciesRegistry.instance.GetSpecies(speciesId);
            if (data != null) sprite = data.placeholderSprite;
        }

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
                fishRenderers[i].sprite = sprite;
            }
        }
    }
}
