using UnityEngine;

public class DecorRegistry : MonoBehaviour
{
    public static DecorRegistry instance;

    [System.Serializable]
    public class DecorSceneEntry
    {
        public DecorItemData decor;
        public GameObject sceneObject;
    }

    [SerializeField] DecorSceneEntry[] entries;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start() => ApplyOwnedDecor();

    public DecorItemData GetDecor(string decorId)
    {
        if (string.IsNullOrEmpty(decorId) || entries == null) return null;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].decor != null && entries[i].decor.decorId == decorId) return entries[i].decor;
        }
        return null;
    }

    public float GetTotalOwnedBonus(DecorBonusType bonusType)
    {
        if (entries == null || GameManager.instance == null || bonusType == DecorBonusType.None) return 0f;

        float totalBonus = 0f;
        for (int i = 0; i < entries.Length; i++)
        {
            var decor = entries[i].decor;
            if (decor == null || decor.bonusType != bonusType) continue;
            if (!GameManager.instance.IsDecorOwned(decor.decorId)) continue;
            totalBonus += Mathf.Max(0f, decor.bonusValue);
        }
        return totalBonus;
    }

    public void ApplyOwnedDecor()
    {
        if (entries == null || GameManager.instance == null) return;
        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e.sceneObject == null || e.decor == null) continue;
            bool owned = GameManager.instance.IsDecorOwned(e.decor.decorId);
            e.sceneObject.SetActive(owned);
        }
    }
}
