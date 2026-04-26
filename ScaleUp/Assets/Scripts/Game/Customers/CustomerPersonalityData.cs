using UnityEngine;

[CreateAssetMenu(fileName = "CustomerPersonality", menuName = "ScaleUp/Customer Personality")]
public class CustomerPersonalityData : ScriptableObject
{
    public string personalityId;
    public string displayName;
    public Sprite portrait;

    [Header("Patience")]
    [Min(1f)] public float patienceSeconds = 30f;

    [Header("Pricing")]
    [Min(0.1f)] public float minOfferMultiplier = 1f;
    [Min(0.1f)] public float maxOfferMultiplier = 1.2f;
    [Range(0f, 1f)] public float counterOfferAcceptChance = 0.5f;

    [Header("Tipping")]
    public bool canTip = false;
    [Min(0)] public int minTip = 0;
    [Min(0)] public int maxTip = 0;

    [Header("Dialogue")]
    public string[] greetingLines;
    public string[] satisfiedLines;
    public string[] dissatisfiedLines;
    public string[] counterAcceptedLines;
    public string[] counterRejectedLines;
    public string[] goodReviewLines;
    public string[] badReviewLines;

    [Header("Fish Demand")]
    [Min(1)] public int minFishWanted = 1;
    [Min(1)] public int maxFishWanted = 2;

    public float GetRandomOfferMultiplier()
    {
        float minValue = Mathf.Min(minOfferMultiplier, maxOfferMultiplier);
        float maxValue = Mathf.Max(minOfferMultiplier, maxOfferMultiplier);
        return Random.Range(minValue, maxValue);
    }

    public int GetRandomFishCount()
    {
        int minValue = Mathf.Min(minFishWanted, maxFishWanted);
        int maxValue = Mathf.Max(minFishWanted, maxFishWanted);
        return Random.Range(minValue, maxValue + 1);
    }

    public int GetRandomTipAmount()
    {
        if (!canTip) return 0;
        int minValue = Mathf.Min(minTip, maxTip);
        int maxValue = Mathf.Max(minTip, maxTip);
        return Random.Range(minValue, maxValue + 1);
    }

    public string GetRandomGoodReviewLine(string fallback = "Great service and healthy fish.")
    {
        return PickRandomLine(goodReviewLines, fallback);
    }

    public string GetRandomBadReviewLine(string fallback = "Service was disappointing.")
    {
        return PickRandomLine(badReviewLines, fallback);
    }

    static string PickRandomLine(string[] lines, string fallback)
    {
        if (lines == null || lines.Length == 0) return fallback;
        int randomStart = Random.Range(0, lines.Length);
        for (int i = 0; i < lines.Length; i++)
        {
            int index = (randomStart + i) % lines.Length;
            if (!string.IsNullOrWhiteSpace(lines[index])) return lines[index];
        }
        return fallback;
    }
}
