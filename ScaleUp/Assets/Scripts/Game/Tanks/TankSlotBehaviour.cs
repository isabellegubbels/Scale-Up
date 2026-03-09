using UnityEngine;

public class TankSlotBehaviour : MonoBehaviour
{
    [SerializeField] int slotIndex;
    [SerializeField] GameObject tankVisual;   // Tank, shown only when owned
    [SerializeField] GameObject tableVisual;   // Table, always visible

    void Start() => RefreshVisibility();

    void OnEnable() => RefreshVisibility();

    void Update() => RefreshVisibility();

    public void RefreshVisibility()
    {
        if (tankVisual != null) tankVisual.SetActive(TankManager.instance != null && TankManager.instance.IsOwned(slotIndex));
        if (tableVisual != null) tableVisual.SetActive(true);
    }
}
