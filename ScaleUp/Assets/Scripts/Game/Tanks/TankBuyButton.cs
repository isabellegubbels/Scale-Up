using UnityEngine;

public class TankBuyButton : MonoBehaviour
{
    [SerializeField] int slotIndex;

    public void Buy()
    {
        if (StoreManager.instance != null) StoreManager.instance.OrderTank(slotIndex);
    }
}
