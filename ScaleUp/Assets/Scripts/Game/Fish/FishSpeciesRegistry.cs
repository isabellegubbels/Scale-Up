using UnityEngine;

public class FishSpeciesRegistry : MonoBehaviour
{
    public static FishSpeciesRegistry instance;

    [SerializeField] FishSpeciesData[] species;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public FishSpeciesData GetSpecies(string speciesId)
    {
        if (species == null || string.IsNullOrEmpty(speciesId)) return null;
        for (int i = 0; i < species.Length; i++)
        {
            if (species[i] != null && species[i].speciesId == speciesId) return species[i];
        }
        return null;
    }

    public FishSpeciesData GetSpeciesAt(int index)
    {
        if (species == null || index < 0 || index >= species.Length) return null;
        return species[index];
    }

    public int SpeciesCount => species != null ? species.Length : 0;
}
