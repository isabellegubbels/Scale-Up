using UnityEngine;

[CreateAssetMenu(fileName = "FishSpecies", menuName = "ScaleUp/Fish Species")]
public class FishSpeciesData : ScriptableObject
{
    [Tooltip("Unique id for this species (e.g. \"100\", \"101\" — used in code and save data)")]
    public string speciesId;
    public string displayName;
    [Tooltip("Cost for the player to buy this species (e.g. from supplier). Customer offer prices will be handled elsewhere.")]
    public int purchaseCost;
    public Sprite placeholderSprite;

    [Tooltip("If true, species appears in the order menu from a new game.")]
    public bool startsUnlocked;
    [Tooltip("Total lifetime fish sold needed to unlock (ignored if startsUnlocked).")]
    public int unlockAfterSales;
}
