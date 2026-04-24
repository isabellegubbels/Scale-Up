using UnityEngine;

public enum DecorBonusType
{
    None,
    TrafficBoost,
    WaitTimeReduction
}

[CreateAssetMenu(fileName = "DecorItem", menuName = "ScaleUp/Decor Item")]
public class DecorItemData : ScriptableObject
{
    public string decorId;
    public string displayName;
    public int cost;
    public Sprite icon;
    public DecorBonusType bonusType;
    [Tooltip("e.g. 0.1 = 10% when customers use this.")]
    public float bonusValue;
}
