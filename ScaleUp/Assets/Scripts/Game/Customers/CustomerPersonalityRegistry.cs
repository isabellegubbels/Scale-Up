using UnityEngine;

public class CustomerPersonalityRegistry : MonoBehaviour
{
    public static CustomerPersonalityRegistry instance;

    [SerializeField] CustomerPersonalityData[] personalities;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public CustomerPersonalityData GetById(string personalityId)
    {
        if (string.IsNullOrEmpty(personalityId) || personalities == null) return null;
        for (int i = 0; i < personalities.Length; i++)
        {
            var personality = personalities[i];
            if (personality != null && personality.personalityId == personalityId) return personality;
        }
        return null;
    }

    public CustomerPersonalityData GetRandom()
    {
        if (personalities == null || personalities.Length == 0) return null;
        int randomStart = Random.Range(0, personalities.Length);
        for (int i = 0; i < personalities.Length; i++)
        {
            int index = (randomStart + i) % personalities.Length;
            if (personalities[index] != null) return personalities[index];
        }
        return null;
    }

    public int Count => personalities != null ? personalities.Length : 0;
}
