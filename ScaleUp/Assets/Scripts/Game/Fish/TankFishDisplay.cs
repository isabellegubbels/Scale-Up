using UnityEngine;
using TMPro;

public class TankFishDisplay : MonoBehaviour
{
    [SerializeField] int slotIndex;
    [SerializeField] Transform fishContainer; // Parent for placeholder sprites; null = this transform.
    [SerializeField] float spacing = 0.15f;
    [SerializeField] float targetFishWorldWidth = 0.45f;
    [SerializeField] float spacingPerFishWidth = 0.65f;
    [SerializeField] bool useScatterPlacement = true;
    [SerializeField] Vector2 scatterAreaUsage = new Vector2(0.72f, 0.42f);
    [SerializeField] float minimumScatterDistanceMultiplier = 1.05f;
    [SerializeField] int scatterAttemptsPerFish = 60;
    [SerializeField] bool useGentleDrift = true;
    [SerializeField] float driftAmplitude = 0.015f;
    [SerializeField] float driftSpeed = 0.8f;
    [SerializeField] bool showTankAcclimationLabel = true;
    [SerializeField] float acclimationLabelVerticalPadding = 0.08f;
    [SerializeField] float acclimationLabelFontSize = 1.2f;

    SpriteRenderer[] fishRenderers;
    Vector3[] baseLocalPositions;
    TextMeshPro acclimationLabel;
    int lastDisplayedCount = -1;
    bool lastAcclimating;
    string lastSpeciesId;
    int lastAcclimationWholeSeconds = -1;
    bool lastAcclimationVisible;

    int Capacity => TankTier.GetCapacity(slotIndex);

    void Start() => EnsurePool();

    void Update()
    {
        if (TankManager.instance == null) return;
        var slot = TankManager.instance.GetSlot(slotIndex);
        if (slot == null || !slot.isOwned)
        {
            SetAcclimationLabelVisible(false);
            return;
        }
        int count = slot.fishCount;
        string speciesId = slot.speciesId;
        bool acclimating = !TankManager.instance.IsAcclimationComplete(slotIndex);
        if (count != lastDisplayedCount || acclimating != lastAcclimating || speciesId != lastSpeciesId) RefreshDisplay(count, acclimating, speciesId);
        if (useScatterPlacement && useGentleDrift) ApplyGentleDrift(Mathf.Min(count, fishRenderers != null ? fishRenderers.Length : 0));
        UpdateAcclimationLabel(slot);
    }

    void EnsurePool()
    {
        int maxDisplayCount = Capacity;
        if (maxDisplayCount <= 0) return;
        if (fishRenderers != null && fishRenderers.Length >= maxDisplayCount) return;
        fishRenderers = new SpriteRenderer[maxDisplayCount];
        baseLocalPositions = new Vector3[maxDisplayCount];
        if (fishContainer == null) fishContainer = transform;
        for (int i = 0; i < maxDisplayCount; i++)
        {
            var go = new GameObject("FishPlaceholder_" + i);
            go.transform.SetParent(fishContainer, false);
            go.transform.localPosition = Vector3.zero;
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

        float fishScale = GetScaleForSprite(sprite);
        float effectiveSpacing = GetSpacing(sprite, fishScale);
        int maxDisplayCount = fishRenderers != null ? fishRenderers.Length : Capacity;
        if (useScatterPlacement) ScatterFish(maxDisplayCount, count, speciesId, sprite, fishScale);
        else LayoutFish(maxDisplayCount, effectiveSpacing);
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
                fishRenderers[i].transform.localScale = Vector3.one * fishScale;
            }
        }
    }

    void UpdateAcclimationLabel(TankSlotData slot)
    {
        if (!showTankAcclimationLabel || slot == null || !slot.isOwned || slot.fishCount <= 0 || TankManager.instance == null)
        {
            SetAcclimationLabelVisible(false);
            return;
        }

        float secondsRemaining = TankManager.instance.GetAcclimationSecondsRemaining(slotIndex);
        bool isActive = secondsRemaining > 0f;
        if (!isActive)
        {
            SetAcclimationLabelVisible(false);
            return;
        }

        int wholeSeconds = Mathf.CeilToInt(secondsRemaining);
        if (lastAcclimationVisible && wholeSeconds == lastAcclimationWholeSeconds) return;

        EnsureAcclimationLabel();
        if (acclimationLabel == null) return;
        lastAcclimationVisible = true;
        lastAcclimationWholeSeconds = wholeSeconds;
        acclimationLabel.text = $"Acclimating {FormatTime(wholeSeconds)}";
        acclimationLabel.gameObject.SetActive(true);
        PositionAcclimationLabel();
    }

    void SetAcclimationLabelVisible(bool visible)
    {
        lastAcclimationVisible = visible;
        if (!visible) lastAcclimationWholeSeconds = -1;
        if (acclimationLabel != null) acclimationLabel.gameObject.SetActive(visible);
    }

    void EnsureAcclimationLabel()
    {
        if (acclimationLabel != null) return;
        Transform parent = fishContainer != null ? fishContainer : transform;
        var labelGo = new GameObject("Acclimation Label");
        labelGo.transform.SetParent(parent, false);
        labelGo.layer = parent.gameObject.layer;
        acclimationLabel = labelGo.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null) acclimationLabel.font = TMP_Settings.defaultFontAsset;
        acclimationLabel.alignment = TextAlignmentOptions.Center;
        acclimationLabel.fontSize = acclimationLabelFontSize;
        acclimationLabel.enableAutoSizing = false;
        acclimationLabel.enableWordWrapping = false;
        acclimationLabel.text = string.Empty;
        acclimationLabel.color = Color.white;
        labelGo.transform.localScale = Vector3.one * 0.1f;

        var labelRenderer = labelGo.GetComponent<MeshRenderer>();
        if (labelRenderer != null)
        {
            if (fishRenderers != null && fishRenderers.Length > 0 && fishRenderers[0] != null)
            {
                labelRenderer.sortingLayerID = fishRenderers[0].sortingLayerID;
                labelRenderer.sortingOrder = fishRenderers[0].sortingOrder + 3;
            }
            else labelRenderer.sortingOrder = 4;
        }
        PositionAcclimationLabel();
        labelGo.SetActive(false);
    }

    void PositionAcclimationLabel()
    {
        if (acclimationLabel == null) return;
        Vector2 areaSize = GetScatterAreaSize();
        float y = areaSize.y * 0.5f + targetFishWorldWidth * 0.5f + acclimationLabelVerticalPadding;
        acclimationLabel.transform.localPosition = new Vector3(0f, y, 0f);
    }

    string FormatTime(int totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    float GetScaleForSprite(Sprite sprite)
    {
        if (sprite == null || targetFishWorldWidth <= 0f) return 1f;
        float spriteWidth = sprite.bounds.size.x;
        if (spriteWidth <= 0f) return 1f;
        return targetFishWorldWidth / spriteWidth;
    }

    float GetSpacing(Sprite sprite, float fishScale)
    {
        if (spacingPerFishWidth <= 0f) return spacing;
        float normalizedWidth = targetFishWorldWidth;
        if (sprite != null)
        {
            float spriteWidth = sprite.bounds.size.x;
            if (spriteWidth > 0f) normalizedWidth = spriteWidth * fishScale;
        }
        return Mathf.Max(spacing, normalizedWidth * spacingPerFishWidth);
    }

    void LayoutFish(int maxDisplayCount, float effectiveSpacing)
    {
        if (fishRenderers == null || fishRenderers.Length == 0) return;
        for (int i = 0; i < maxDisplayCount; i++)
        {
            if (fishRenderers[i] == null) continue;
            fishRenderers[i].transform.localPosition = new Vector3(i * effectiveSpacing - (maxDisplayCount - 1) * effectiveSpacing * 0.5f, 0f, 0f);
        }
    }

    void ScatterFish(int maxDisplayCount, int fishCount, string speciesId, Sprite sprite, float fishScale)
    {
        if (fishRenderers == null || fishRenderers.Length == 0) return;

        Vector2 areaSize = GetScatterAreaSize();
        float halfWidth = areaSize.x * 0.5f;
        float halfHeight = areaSize.y * 0.5f;
        float fishWidth = targetFishWorldWidth;
        float fishHeight = fishWidth * 0.5f;
        if (sprite != null)
        {
            fishWidth = sprite.bounds.size.x * fishScale;
            fishHeight = sprite.bounds.size.y * fishScale;
        }

        halfWidth = Mathf.Max(0f, halfWidth - fishWidth * 0.5f);
        halfHeight = Mathf.Max(0f, halfHeight - fishHeight * 0.5f);
        float minDistance = Mathf.Max(0.01f, fishWidth * minimumScatterDistanceMultiplier);
        int clampedCount = Mathf.Clamp(fishCount, 0, maxDisplayCount);
        Vector3[] placed = new Vector3[clampedCount];
        int attemptsPerFish = Mathf.Clamp(scatterAttemptsPerFish, 12, 200);

        System.Random random = new System.Random(GetPlacementSeed(speciesId, clampedCount));
        for (int i = 0; i < maxDisplayCount; i++)
        {
            if (fishRenderers[i] == null) continue;
            if (i >= clampedCount)
            {
                fishRenderers[i].transform.localPosition = Vector3.zero;
                continue;
            }

            Vector3 pos = Vector3.zero;
            bool placedSuccessfully = false;
            for (int attempt = 0; attempt < attemptsPerFish; attempt++)
            {
                float x = Mathf.Lerp(-halfWidth, halfWidth, (float)random.NextDouble());
                float y = Mathf.Lerp(-halfHeight, halfHeight, (float)random.NextDouble());
                pos = new Vector3(x, y, 0f);
                if (IsFarEnough(placed, i, pos, minDistance))
                {
                    placedSuccessfully = true;
                    break;
                }
            }

            if (!placedSuccessfully) pos = GetGridFallbackPosition(i, clampedCount, halfWidth, halfHeight);
            placed[i] = pos;
            baseLocalPositions[i] = pos;
            fishRenderers[i].transform.localPosition = pos;
        }
    }

    Vector3 GetGridFallbackPosition(int fishIndex, int fishCount, float halfWidth, float halfHeight)
    {
        if (fishCount <= 1) return Vector3.zero;
        int columns = Mathf.CeilToInt(Mathf.Sqrt(fishCount));
        int rows = Mathf.CeilToInt((float)fishCount / columns);
        int columnIndex = fishIndex % columns;
        int rowIndex = fishIndex / columns;
        float x = columns <= 1 ? 0f : Mathf.Lerp(-halfWidth, halfWidth, (float)columnIndex / (columns - 1));
        float y = rows <= 1 ? 0f : Mathf.Lerp(-halfHeight, halfHeight, (float)rowIndex / (rows - 1));
        return new Vector3(x, y, 0f);
    }

    Vector2 GetScatterAreaSize()
    {
        if (fishContainer != null)
        {
            SpriteRenderer areaSprite = fishContainer.GetComponent<SpriteRenderer>();
            if (areaSprite != null && areaSprite.sprite != null)
            {
                Vector2 rawSize = areaSprite.sprite.bounds.size;
                return new Vector2(rawSize.x * Mathf.Max(0.05f, scatterAreaUsage.x), rawSize.y * Mathf.Max(0.05f, scatterAreaUsage.y));
            }
        }
        return new Vector2(Mathf.Max(spacing * Capacity, targetFishWorldWidth), targetFishWorldWidth);
    }

    bool IsFarEnough(Vector3[] placed, int placedCount, Vector3 candidate, float minDistance)
    {
        float minDistanceSq = minDistance * minDistance;
        for (int i = 0; i < placedCount; i++)
        {
            if ((placed[i] - candidate).sqrMagnitude < minDistanceSq) return false;
        }
        return true;
    }

    void ApplyGentleDrift(int showCount)
    {
        if (fishRenderers == null || baseLocalPositions == null || driftAmplitude <= 0f || driftSpeed <= 0f) return;
        float t = Time.time * driftSpeed;
        for (int i = 0; i < showCount; i++)
        {
            if (fishRenderers[i] == null || !fishRenderers[i].gameObject.activeSelf) continue;
            float xOffset = Mathf.Sin(t + i * 1.31f) * driftAmplitude;
            float yOffset = Mathf.Cos(t * 0.87f + i * 1.73f) * driftAmplitude * 0.6f;
            fishRenderers[i].transform.localPosition = baseLocalPositions[i] + new Vector3(xOffset, yOffset, 0f);
        }
    }

    int GetPlacementSeed(string speciesId, int fishCount)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + slotIndex;
            hash = hash * 31 + fishCount;
            if (!string.IsNullOrEmpty(speciesId))
            {
                for (int i = 0; i < speciesId.Length; i++) hash = hash * 31 + speciesId[i];
            }
            return hash;
        }
    }
}
