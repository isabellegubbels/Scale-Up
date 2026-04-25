using UnityEngine;

public class TankSlotBehaviour : MonoBehaviour
{
    [SerializeField] int slotIndex;
    [SerializeField] GameObject tankVisual;   // Tank, shown only when owned
    [SerializeField] GameObject tableVisual;   // Table, always visible
    [SerializeField] SpriteRenderer tankSpriteRenderer;

    static readonly Color cleanColor = new Color(0.483f, 0.690f, 0.647f, 0.627f); // #7BB0A5
    static readonly Color dirtyColor = new Color(0.483f, 0.690f, 0.439f, 0.627f); // #7BB070

    void Start() => RefreshVisibility();

    void OnEnable() => RefreshVisibility();

    void Update() => RefreshVisibility();

    public void RefreshVisibility()
    {
        bool owned = TankManager.instance != null && TankManager.instance.IsOwned(slotIndex);
        if (tankVisual != null) tankVisual.SetActive(owned);
        if (tableVisual != null) tableVisual.SetActive(true);
        if (tankSpriteRenderer != null && owned)
        {
            bool isClean = TankManager.instance == null || TankManager.instance.IsTankClean(slotIndex);
            tankSpriteRenderer.color = isClean ? cleanColor : dirtyColor;
        }
    }
}
