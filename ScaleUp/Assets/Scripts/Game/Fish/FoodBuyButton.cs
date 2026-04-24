using UnityEngine;

public class FoodBuyButton : MonoBehaviour
{
    public void Buy()
    {
        if (StoreManager.instance != null) StoreManager.instance.OrderFood();
    }
}
